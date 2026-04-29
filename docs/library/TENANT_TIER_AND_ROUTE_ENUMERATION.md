> **Scope:** Buyer- and integrator-facing explanation of why some HTTP APIs return **404** for lower commercial tiers (anti-route-enumeration); not a full entitlement matrix, OpenAPI replacement, or pricing quote.

# Tenant tier and route enumeration (why some calls look "not found")

**Audience:** Procurement, solution architects, and API consumers evaluating or integrating with ArchLucid.

**Intent:** Lower-tier tenants often receive **`404 Not Found`** on routes that exist for higher tiers. That behavior is **deliberate** so callers cannot infer hidden product surface by probing URLs. This note explains the mechanism, what to do instead operationally, and how integrators should work with public contracts.

---

## Why **404** instead of "upgrade required"

Endpoints that require a minimum **`dbo.Tenants.Tier`** are protected by **`[RequiresCommercialTenantTier(...)]`**, enforced in **`ArchLucid.Api/Filters/CommercialTenantTierFilter`**.

The filter’s documented behavior:

- For **authenticated** requests, it loads the tenant for the current scope and compares **`tenant.Tier`** to the required minimum.
- If the tenant row is **missing** or the tier is **below** the minimum, the API returns **`404 Not Found`** with a generic **resource-not-found** problem shape so callers **cannot conclude** that the capability exists at that path (see XML summary on **`CommercialTenantTierFilter`** and **`PackagingTierProblemDetailsFactory`** — the factory name reflects historical “payment required” wording, but the **implemented** status is **404** by design).

**Unauthenticated** requests **do not** receive this tier check in the filter (they pass through to other auth layers).

For a **code-level inventory** of which controller areas use Standard vs Enterprise gating (and the **`404`** vs **`402`** naming debt), see **[COMMERCIAL_ENFORCEMENT_DEBT.md](COMMERCIAL_ENFORCEMENT_DEBT.md)**.

**Trust and packaging context:** assurance posture and commercial boundaries are summarized for buyers in **[Trust Center](../trust-center.md)**. How packaging intent relates to the product (without duplicating price tables) is in **[PRICING_PHILOSOPHY.md](../go-to-market/PRICING_PHILOSOPHY.md)** and **[PRODUCT_PACKAGING.md](PRODUCT_PACKAGING.md)**.

---

## What to do instead (Pilot vs Operate)

| Workflow | Recommended action | Primary docs |
|----------|-------------------|--------------|
| **Core Pilot** (first value: request → run → commit → review exports) | Stay on the **Core Pilot** path first. Do not treat **404** on exploratory Operate URLs as proof the route is invalid globally—it may be **tier-gated** or out of scope for the tenant. | **[CORE_PILOT.md](../CORE_PILOT.md)** · **[V1_SCOPE.md](V1_SCOPE.md)** |
| **Operate** (analysis, governance, alerts, comparisons, exports beyond the pilot minimum) | Use **[OPERATOR_DECISION_GUIDE.md](OPERATOR_DECISION_GUIDE.md)** to decide when Operate layers are appropriate; align tenant **tier** and packaging with routes you need (**[COMMERCIAL_ENFORCEMENT_DEBT.md](COMMERCIAL_ENFORCEMENT_DEBT.md)** lists gated controller families). | **[OPERATOR_DECISION_GUIDE.md](OPERATOR_DECISION_GUIDE.md)** · **[OPERATOR_ATLAS.md](OPERATOR_ATLAS.md)** |

---

## Integrators: public contracts and correlation

- **Do not** rely on scanning or guessing undocumented paths: tier-gated routes may **indistinguishably** return the same **404** shape as a missing resource.
- **Do** integrate against **documented** HTTP contracts: OpenAPI is served at **`/openapi/v1.json`** (see **[API_CONTRACTS.md](API_CONTRACTS.md)** § Contract artifacts). Treat the published spec and contract tests as the source of truth for **your** integration, subject to **auth** and **tenant tier**.
- For errors, follow **[API_CONTRACTS.md](API_CONTRACTS.md)** § **Correlation ID**: send or read **`X-Correlation-ID`**, and use **`correlationId`** in **`application/problem+json`** bodies when triaging—**404** tier responses still participate in the same correlation pattern as other problems.

---

## References (code)

- `ArchLucid.Api/Filters/CommercialTenantTierFilter.cs`
- `ArchLucid.Api/ProblemDetails/PackagingTierProblemDetailsFactory.cs`
- `ArchLucid.Api/Attributes/RequiresCommercialTenantTierAttribute.cs`
