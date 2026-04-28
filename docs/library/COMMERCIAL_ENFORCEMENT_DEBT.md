# Commercial tier enforcement (`[RequiresCommercialTenantTier]`) — as-built

**Date:** 2026-04-28  
**Scope:** Document what the API does today — no entitlement matrix expansion.

## Behavior

Endpoints decorated with `[RequiresCommercialTenantTier(TenantTier.Standard)]` (or **`Enterprise`** for CSV export) are filtered by **`CommercialTenantTierFilter`**, which loads **`dbo.Tenants.Tier`** for the current scope.

When the authenticated principal’s tenant tier is **below** the required tier, `PackagingTierProblemDetailsFactory.CreatePaymentRequired` returns an **`HTTP 404 Not Found`** body with `ProblemTypes.ResourceNotFound` — **not** `402 Payment Required`. This is intentional so lower-tier callers cannot infer that a capability exists on the route (see XML doc on `PackagingTierProblemDetailsFactory`).

When the tenant row is missing, the filter also returns **`404`**.

Unauthenticated requests **pass through** the tier filter (no tier check).

## Controllers / routes using the attribute (current inventory)

| Area | Minimum tier | Source file |
|------|--------------|-------------|
| Audit CSV export | Enterprise | `ArchLucid.Api/Controllers/Admin/AuditController.cs` (`ExportAudit`) |
| Advisory | Standard | `AdvisoryController.cs`, `AdvisorySchedulingController.cs` |
| Authority compare / replay | Standard | `AuthorityCompareController.cs`, `AuthorityReplayController.cs` |
| Governance (core, resolution, preview) | Standard | `GovernanceController.cs`, `GovernanceResolutionController.cs`, `GovernancePreviewController.cs` |
| Manifests | Standard | `ManifestsController.cs` |
| Policy packs | Standard | `PolicyPacksController.cs` |
| Pilots (scorecard endpoint) | Standard | `PilotsController.cs` (selected action) |
| Pilots board pack | Standard | `PilotsBoardPackController.cs` |
| Tenant cost estimate | Standard | `TenantCostEstimateController.cs` |
| Tenant value report | Standard | `ValueReportController.cs` |

## Debt / follow-ons (not in this doc’s scope)

- **SKU ↔ full endpoint matrix:** Packaging docs describe that not every endpoint is gated; expanding coverage is tracked under product packaging hardening docs.
- **`402` vs `404` naming:** `PackagingTierProblemDetailsFactory` is named for “payment required” but implements **404** responses by design — avoid renaming without a breaking-change review.

## References

- `ArchLucid.Api/Filters/CommercialTenantTierFilter.cs`
- `ArchLucid.Api/ProblemDetails/PackagingTierProblemDetailsFactory.cs`
- `ArchLucid.Api/Attributes/RequiresCommercialTenantTierAttribute.cs`
