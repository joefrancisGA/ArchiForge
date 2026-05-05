> **Scope:** Engineering audit mapping [PRICING_PHILOSOPHY.md §3 Feature gates](../go-to-market/PRICING_PHILOSOPHY.md) rows to repo enforcement (`RequiresCommercialTenantTier`, RBAC policies, trial limits); not a price sheet, SKU definition change, or legal interpretation.

# Feature gate enforcement vs PRICING_PHILOSOPHY §3 (audit)

**Artifact date:** 2026-05-05  
**Canonical buyer table:** [PRICING_PHILOSOPHY.md §3 Feature gates](../go-to-market/PRICING_PHILOSOPHY.md) (Team / Professional / Enterprise columns).  
**Pricing numerics:** This file intentionally contains **no** list prices or amounts; see the philosophy doc and `scripts/ci/check_pricing_single_source.py` allowlist.

## Companion GTM / billing docs (how checkout and trial relate)

| Doc | Role in this audit |
|-----|-------------------|
| [TRIAL_AND_SIGNUP.md](../go-to-market/TRIAL_AND_SIGNUP.md) | Trial is described as “Team features”; tenants remain `TenantTier.Free` until conversion — **see §3 trial alignment** below. **Parameter drift (outside §3 table):** doc specifies **30**-day duration; `TrialTenantBootstrapService` still sets `AddDays(14)` (`ArchLucid.Application/Tenancy/TrialTenantBootstrapService.cs` line ~99) — reconcile separately from packaging gates. |
| [BILLING.md](BILLING.md) | `IBillingProvider` / webhooks; maps paid checkout to tenant tier via SQL — **does not** implement per-feature rows from §3. |
| [STRIPE_CHECKOUT.md](../go-to-market/STRIPE_CHECKOUT.md) | Stripe Team SKU → persistence tier via `BillingTierCode` (see code citations). |
| [MARKETPLACE_PUBLICATION.md](../go-to-market/MARKETPLACE_PUBLICATION.md) | Plan labels must align with **Team / Professional / Enterprise**; marketplace webhooks feed the same `TenantTier` storage as Stripe when configured. |

## §1 Persistence and checkout truth model (blocks fine-grained Team vs Professional gates)

After billing activation, **both** Marketplace/Stripe **Team** and **Professional** (historically “Pro”) checkout selections persist as **`dbo.Tenants.Tier = Standard`** (`TenantTier.Standard`). **Enterprise** maps to **`TenantTier.Enterprise`**. Self-serve trials use **`TenantTier.Free`** with trial columns (`TrialRunsLimit`, etc.) — tier is **not** upgraded to Standard for the trial period.

```8:16:ArchLucid.Core/Billing/BillingTierCode.cs
    public static string FromCheckoutTier(BillingCheckoutTier tier)
    {
        return tier switch
        {
            BillingCheckoutTier.Team => nameof(TenantTier.Standard),
            BillingCheckoutTier.Pro => nameof(TenantTier.Standard),
            BillingCheckoutTier.Enterprise => nameof(TenantTier.Enterprise),
            _ => nameof(TenantTier.Standard)
        };
    }
```

**Implication:** Any `[RequiresCommercialTenantTier(TenantTier.Standard)]` route is available to **paid Team and paid Professional** alike. The API **cannot** enforce §3 rows that differ between Team and Professional **unless** a new capability dimension (plan id flag, subscription SKU column, or similar) is added — **out of scope for this audit** (no new tier definitions here).

## §2 Enforcement primitives (as-built)

| Mechanism | Where | Behavior |
|-----------|-------|----------|
| **`CommercialTenantTierFilter`** | `ArchLucid.Api/Filters/CommercialTenantTierFilter.cs` | Compares `tenant.Tier` to minimum; **Standard** → HTTP **403** + `PackagingTierInsufficient`; **Enterprise** minimum → HTTP **404** obfuscated (`ResourceNotFound`). |
| **`[RequiresCommercialTenantTier(...)]`** | Controllers listed per row below | Declares minimum **persisted** tier for controller/action. |
| **Trial write gate** | `TrialLimitGate` + `TrialActiveRequirement` on `ExecuteAuthority` / `AdminAuthority` | Blocks mutating work when trial expired or limits exceeded (HTTP **402** path via `TrialLimitProblemResponse`) — see `ArchLucid.Host.Core/Startup/ArchLucidAuthorizationPoliciesExtensions.cs` (`ExecuteAuthority` adds `TrialActiveRequirement`). |
| **RBAC policies** | `ReadAuthority`, `ExecuteAuthority`, `RequireAuditor`, `AdminAuthority`, SCIM bearer | Orthogonal to packaging column in §3; combine with tier filter where both apply. |
| **Capability claims** | e.g. `export:consulting-docx` (`ArchLucidPolicies.CanExportConsultingDocx`) | API key / principal feature slice; **not** wired as Team vs Professional SKU gate in §3 sense. |

Related inventories: [COMMERCIAL_ENFORCEMENT_DEBT.md](COMMERCIAL_ENFORCEMENT_DEBT.md), [PRODUCT_PACKAGING.md](PRODUCT_PACKAGING.md) (API vs UI shaping).

---

## §3 Row-by-row mapping (§3 Feature gates table → code)

Legend: **Aligned** = consistent with **Enterprise vs non-Enterprise** or **paid Standard vs Free** given §1 model; **SKU gap** = Team vs Professional differs in §3 but code cannot distinguish; **Doc gap** = contradicts §3 under the same model.

| §3 Feature | Documented Team | Doc Professional | Doc Enterprise | Enforcement | Primary code paths | Gap vs §3 |
|------------|-----------------|------------------|----------------|-------------|-------------------|-----------|
| Architecture runs | ✓ | ✓ | ✓ | **Trial limits + ExecuteAuthority** on mutating routes; **no** `[RequiresCommercialTenantTier]` on `RunsController` / read **`RunQueryController`** | `ArchLucid.Api/Controllers/Authority/RunsController.cs` (no tier attribute); `RunQueryController.cs` lines 35–41 (`ReadAuthority` only); `TrialLimitAuthorizationHandler` in `ArchLucid.Api/Filters/TrialLimitFilter.cs` | **Trial alignment:** Trial tenants stay **`Tier.Free`**; core run reads/writes avoid Standard tier gate — aligns with “runs” for evaluation; §3 table is silent on trial. |
| Golden manifests | ✓ | ✓ | ✓ | **`TenantTier.Standard`** minimum | `ManifestsController`: `[RequiresCommercialTenantTier(TenantTier.Standard)]` at line 32 | **Doc gap vs trial copy:** [TRIAL_AND_SIGNUP.md](../go-to-market/TRIAL_AND_SIGNUP.md) promises Team-tier manifests; **authenticated trial (`Free`) receives HTTP 403** from tier filter on manifest routes — contradicts trial narrative unless UI uses non-API/demo paths only. |
| Comparison runs | ✓ | ✓ | ✓ | **`TenantTier.Standard`** on comparison controllers | e.g. `ComparisonsController` `[RequiresCommercialTenantTier(TenantTier.Standard)]` line 48; `ComparisonController` line 31; `AuthorityCompareController` line 26; `RunComparisonController` line 32 | Same **trial vs manifest/comparison** tension as above for **`Tier.Free`**. |
| Governance approvals | — | ✓ | ✓ | **`TenantTier.Standard`** | `GovernanceController` line 36; `GovernanceResolutionController`, `GovernancePreviewController`, `GovernancePreCommitSimulationController` (all Standard — see [COMMERCIAL_ENFORCEMENT_DEBT.md](COMMERCIAL_ENFORCEMENT_DEBT.md)) | **SKU gap:** Paid **Team** (`Standard`) **has full governance APIs** — §3 marks Team as **not** included. |
| Policy packs | — | ✓ | ✓ (custom) | **`TenantTier.Standard`** on `PolicyPacksController` line 39 | No `TenantTier` check found in `ArchLucid.Decisioning` policy-pack services for **ProjectCustom vs Enterprise-only** authoring — **hypothesis:** custom pack type is not Enterprise-gated in application layer (`PolicyPackManagementService`, `PolicyPacksController.Create`). | **SKU gap:** Paid **Team** gets policy-pack APIs. **Custom Enterprise row:** no separate **Enterprise-only** enforcement located for custom packs beyond commercial packaging intent in docs. |
| Audit export (CSV) | — | ✓ | ✓ | **`TenantTier.Enterprise`** on export action only | `AuditController.ExportAudit`: `[RequiresCommercialTenantTier(TenantTier.Enterprise)]` lines 176–178; list/search `GET /v1/audit` **not** tier-gated | **Doc gap:** Professional should have CSV export per §3; persisted Professional is **`Standard`** → **export returns 404-style Enterprise gate** — **under-delivers** vs table for Professional buyers. |
| DOCX consulting export | — | ✓ | ✓ | **`TenantTier.Standard`** on controller | `AnalysisReportsController` line 49; consulting routes at lines 254–386 region (`export/docx/consulting`) | **SKU gap:** Paid **Team** receives consulting DOCX endpoints — §3 restricts to Professional+. |
| Webhook / CloudEvents | — | ✓ | ✓ | **Alert routing + Teams connectors:** `[RequiresCommercialTenantTier(TenantTier.Standard)]` | `AlertRoutingSubscriptionsController` line 33; `TeamsIncomingWebhookConnectionsController` line 27; CloudEvents envelope: `ArchLucid.Host.Core/Services/Delivery/CloudEventsWrappingWebhookPoster.cs` (config-driven); probe: `OutboundWebhookDryRunController` **no** tier attribute (lines 17–25), relies on `ExecuteAuthority` + trial handler | **SKU gap:** Paid **Team** may configure outbound alert routing / Teams — §3 reserves webhooks for Professional+. Dry-run probe has **no** `[RequiresCommercialTenantTier]` (team-tier tenants could probe URLs if they have Execute — weak signal only). |
| Service Bus integration | — | — | ✓ | **Platform-internal** marketplace webhook → Service Bus publish | `MarketplaceWebhookIntegrationEventPublisher` (`ArchLucid.Application/Billing/`); `BillingMarketplaceWebhookController` — **not** a per-tenant entitlement check for §3 | **Semantics gap:** §3 implies buyer-facing Enterprise integration; repo implements **operator/Azure** integration for billing events — **not mapped** as tenant SKU feature gate. |
| SCIM provisioning | — | — | ✓ | **Bearer SCIM scheme + Admin token issuance** without `[RequiresCommercialTenantTier]` on SCIM hosts | `ScimUsersController` / `ScimGroupsController`: `ScimWrite` policy only; `ScimTokensAdminController` (`ArchLucid.Api/Controllers/Admin/ScimTokensAdminController.cs`) — **no** Enterprise tier attribute on controller | **Doc gap:** Any tenant whose admin issues a SCIM token can use SCIM — **not Enterprise-gated** at HTTP layer. Seat limits use Enterprise-named columns (`EnterpriseSeatsLimit`) but **not** `TenantTier.Enterprise` check on token issue. |
| Dedicated support | — | — | ✓ | **None in repo** (commercial process) | N/A | **Expected gap:** §3 row is **GTM/support posture**, not API-enforceable. |

---

## §4 Minimal fix directions (if leadership confirms §3 as contract)

These are **proposals only** — no implementation in this artifact.

1. **Resolve Professional vs Enterprise audit export:** Lower `ExportAudit` minimum to **`TenantTier.Standard`** *or* introduce a dedicated **Professional+** capability flag if Team must stay excluded (requires **new** entitlement dimension — see §1).
2. **Team vs Professional splits:** Persist checkout SKU or plan id on `dbo.Tenants` (or parallel entitlement table) and replace coarse `Standard` checks for: governance, policy packs, webhooks, consulting DOCX — **acceptance tests** per endpoint family with mocked tenant rows.
3. **SCIM:** Add `[RequiresCommercialTenantTier(TenantTier.Enterprise)]` to `ScimTokensAdminController.IssueAsync` and evaluate whether SCIM controllers should reject non-Enterprise tenants.
4. **Trial vs COMMERCIAL_ENFORCEMENT_DEBT:** Either elevate trial to “effective Standard” read entitlement for manifest/comparison **read** paths documented as Team-tier, or **narrow TRIAL_AND_SIGNUP** copy to match **`Tier.Free` + tier filter** behavior.

---

## §5 References (no duplicate pricing)

- `ArchLucid.Api/Filters/CommercialTenantTierFilter.cs`
- `ArchLucid.Api/Attributes/RequiresCommercialTenantTierAttribute.cs`
- `docs/library/COMMERCIAL_ENFORCEMENT_DEBT.md`
- `docs/library/PRODUCT_PACKAGING.md`
