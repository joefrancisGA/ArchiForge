> **Scope:** Buyer- and integrator-facing explanation of commercial tier gates on HTTP APIs (**404** for Enterprise-only probes, **403** for tenant-visible Standard gaps); not a full entitlement matrix, OpenAPI replacement, or pricing quote.

## Tenant tier and route enumeration (tier-gated APIs)


**Audience:** Procurement, solution architects, and API consumers evaluating or integrating with ArchLucid.

**Intent:** **`TenantTier.Standard`** gated routes return **`403 Forbidden`** with an explicit entitlement problem (`PackagingTierInsufficient`) for authenticated callers below tier. **`TenantTier.Enterprise`** gated routes remain **`404 Not Found`** (generic not-found shape) so admin-only probes cannot confirm hidden URLs. Tenant-missing stays **`404`**. Unauthenticated callers skip the tier filter and hit other auth layers.

---

## Responses by minimum tier (**Standard** vs **Enterprise**)

Endpoints that require a minimum **`dbo.Tenants.Tier`** are protected by **`[RequiresCommercialTenantTier(...)]`**, enforced in **`ArchLucid.Api/Filters/CommercialTenantTierFilter`**:

- Loads **`tenant.Tier`** for the current scope (**authenticated** only).
- **Missing tenant row** → **`404 Not Found`** (`ResourceNotFound`).
- **Below minimum tier:**
  - minimum **`Enterprise`** → **`404`** via **`PackagingTierProblemDetailsFactory.CreateObfuscatedNotFound`** (**anti-route-enumeration**).
  - minimum **`Standard`** → **`403`** via **`PackagingTierProblemDetailsFactory.CreateTenantProductInsufficientTier`**.

Factories and remarks: **`CommercialTenantTierFilter`**, **`PackagingTierProblemDetailsFactory`**.

For a **controller inventory** by tier, see **[COMMERCIAL_ENFORCEMENT_DEBT.md](COMMERCIAL_ENFORCEMENT_DEBT.md)**.

**Trust and packaging context:** assurance posture and commercial boundaries are summarized for buyers in **[Trust Center](../trust-center.md)**. How packaging intent relates to the product (without duplicating price tables) is in **[PRICING_PHILOSOPHY.md](../go-to-market/PRICING_PHILOSOPHY.md)** and **[PRODUCT_PACKAGING.md](PRODUCT_PACKAGING.md)**.

---

## What to do instead (Pilot vs Operate)

| Workflow | Recommended action | Primary docs |
|----------|-------------------|--------------|
| **Core Pilot** (first value: request → run → commit → review exports) | Stay on the **Core Pilot** path first. Do not treat **403**/**404** on exploratory Operate URLs as proof the route is invalid globally—they may be **tier-gated** or out of scope for the tenant. | **[CORE_PILOT.md](../CORE_PILOT.md)** · **[V1_SCOPE.md](V1_SCOPE.md)** |
| **Operate** (analysis, governance, alerts, comparisons, exports beyond the pilot minimum) | Use **[OPERATOR_DECISION_GUIDE.md](OPERATOR_DECISION_GUIDE.md)** to decide when Operate layers are appropriate; align tenant **tier** and packaging with routes you need (**[COMMERCIAL_ENFORCEMENT_DEBT.md](COMMERCIAL_ENFORCEMENT_DEBT.md)** lists gated controller families). | **[OPERATOR_DECISION_GUIDE.md](OPERATOR_DECISION_GUIDE.md)** · **[OPERATOR_ATLAS.md](OPERATOR_ATLAS.md)** |

---

## Integrators: public contracts and correlation

- **Do not** rely on scanning undocumented paths: **Enterprise-only** probes can look identical to **`404`** on missing resources.
- **`403`** **`PackagingTierInsufficient`** on **`Standard`** capabilities is deliberate — treat **`detail`** + **`problem type`** as entitlement guidance, not a generic auth failure alone.
- **Do** integrate against **documented** HTTP contracts: OpenAPI is served at **`/openapi/v1.json`** (see **[API_CONTRACTS.md](API_CONTRACTS.md)** § Contract artifacts). Treat the published spec and contract tests as the source of truth for **your** integration, subject to **auth** and **tenant tier**.
- For errors, follow **[API_CONTRACTS.md](API_CONTRACTS.md)** § **Correlation ID**: send or read **`X-Correlation-ID`**, and use **`correlationId`** in **`application/problem+json`** bodies when triaging—**tier-gated** entitlement responses (**403** on Standard capabilities; **404** shapes on Enterprise-only routes and missing tenants) participate in the same correlation pattern as other problems.

---

## References (code)

- `ArchLucid.Api/Filters/CommercialTenantTierFilter.cs`
- `ArchLucid.Api/ProblemDetails/PackagingTierProblemDetailsFactory.cs`
- `ArchLucid.Api/Attributes/RequiresCommercialTenantTierAttribute.cs`
