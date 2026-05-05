> **Scope:** Audit mapping `docs/go-to-market/PRICING_PHILOSOPHY.md` packaging rows to implemented API tier enforcement (`dbo.Tenants.Tier`).

# Commercial tier ↔ code alignment audit

**Pricing source of truth:** [PRICING_PHILOSOPHY.md](../go-to-market/PRICING_PHILOSOPHY.md) (feature gate table §3).

**Runtime tier model:** [`TenantTier`](../../ArchLucid.Core/Tenancy/TenantTier.cs) persisted on `dbo.Tenants.Tier` (`Free`, `Standard`, `Enterprise`). **`[RequiresCommercialTenantTier]`** + [`CommercialTenantTierFilter`](../../ArchLucid.Api/Filters/CommercialTenantTierFilter.cs) enforce minimum tier on decorated controllers.

## Mapping (product labels → stored enum)

| Packaging label (PRICING_PHILOSOPHY) | Typical `TenantTier` after conversion | Notes |
|--------------------------------------|---------------------------------------|-------|
| Team (incl. self-serve trial posture) | `Free` | Trial / Team-equivalent workloads use `Free`; not every Team SKU field is mirrored in enum. |
| Professional | `Standard` | Most **Operate** HTTP surfaces requiring a paid entitlement use **`TenantTier.Standard`**. |
| Enterprise | `Enterprise` | Highest gate; **`Enterprise`** minimum also uses **404 obfuscation** for anti-enumeration per filter design. |

## Feature gates declared in pricing vs primary code anchor

Representative map, not an exhaustive SKU matrix. For fuller route coverage see [`ROUTE_TIER_POLICY_NAV_MATRIX.md`](ROUTE_TIER_POLICY_NAV_MATRIX.md) and [`PRODUCT_PACKAGING.md`](PRODUCT_PACKAGING.md).

| PRICING gate (§3 table) | Code enforcement (primary) |
|-------------------------|----------------------------|
| Architecture runs | Core run lifecycle on `RunsController` / `RunQueryController` — **not** tier-filtered so pilot tenants on `Free` keep the happy path. |
| Golden manifests | Core reads aligned with runs; advanced exports/controllers may be gated separately. |
| Comparison runs | [`ComparisonController`](../../ArchLucid.Api/Controllers/Planning/ComparisonController.cs) and related compare/replay family: **`Standard`**. |
| Governance approvals / policy packs | [`GovernanceController`](../../ArchLucid.Api/Controllers/Governance/GovernanceController.cs), [`PolicyPacksController`](../../ArchLucid.Api/Controllers/Governance/PolicyPacksController.cs): **`Standard`**. |
| Audit export (CSV) | [`AuditController`](../../ArchLucid.Api/Controllers/Admin/AuditController.cs) CSV export actions: **`Enterprise`**. |
| DOCX consulting export | [`DocxExportController`](../../ArchLucid.Api/Controllers/Authority/DocxExportController.cs): **`Standard`**. |
| Webhook / CloudEvents | Teams / customer notification preference controllers at **`Standard`**; Service Bus publishers are host configuration as well as higher-tier API surfaces. |
| Service Bus integration | Primarily **host / Terraform** configuration; API routes for advanced Operate features trend **`Standard`**+ when attributed. |

## Authority commit / golden manifest schema

Contract JSON Schema validation before persistence is controlled by **`ArchLucid:AuthorityCommit:ValidateGoldenManifestSchema`** ([`AuthorityCommitSchemaValidationOptions`](../../ArchLucid.Contracts/Architecture/AuthorityCommitSchemaValidationOptions.cs)), enforced in [`AuthorityDrivenArchitectureRunCommitOrchestrator`](../../ArchLucid.Application/Runs/Orchestration/AuthorityDrivenArchitectureRunCommitOrchestrator.cs). Failures surface as **`400`** with **`ProblemTypes.ValidationFailed`** from [`RunsController.CommitRun`](../../ArchLucid.Api/Controllers/Authority/RunsController.cs). This is an **engineering quality gate**, not a commercial tier boundary.
