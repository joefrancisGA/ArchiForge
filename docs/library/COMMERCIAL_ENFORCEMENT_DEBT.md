> **Scope:** For engineers and reviewers: documents current commercial-tier API enforcement behavior; not an entitlement matrix, SKU roadmap, or policy for changing 404 vs 402 semantics.

# Commercial tier enforcement (`[RequiresCommercialTenantTier]`) — as-built

**Date:** 2026-04-28

## Behavior

Endpoints decorated with `[RequiresCommercialTenantTier(TenantTier.Standard)]` (or **`Enterprise`** where noted) are filtered by **`CommercialTenantTierFilter`**, which loads **`dbo.Tenants.Tier`** for the current scope.

When the authenticated principal’s tenant tier is **below** the required tier, `PackagingTierProblemDetailsFactory.CreatePaymentRequired` returns an **`HTTP 404 Not Found`** body with `ProblemTypes.ResourceNotFound` — **not** `402 Payment Required`. This is intentional so lower-tier callers cannot infer that a capability exists on the route (see XML doc on `PackagingTierProblemDetailsFactory`).

When the tenant row is missing, the filter also returns **`404`**.

Unauthenticated requests **pass through** the tier filter (no tier check).

## Controllers / routes using the attribute — inventory

### `TenantTier.Standard` (controller scope)

| Area | Controllers (source under `ArchLucid.Api/Controllers/`) |
|------|--------------------------------------------------------|
| Alerts | `AlertsController`, `AlertRulesController`, `AlertRoutingSubscriptionsController`, `CompositeAlertRulesController`, `AlertTuningController`, `AlertSimulationController` |
| Advisory / digests / learning | `AdvisoryController`, `AdvisorySchedulingController`, `DigestSubscriptionsController`, `LearningController`, `ProductLearningController`, `RecommendationLearningController` |
| Authority / runs (Operate exports & compare) | `AuthorityCompareController`, `AuthorityReplayController`, `ArtifactExportController`, `DocxExportController`, `ExportsController`, `RunComparisonController`, `AnalysisReportsController`, `ProvenanceQueryController` |
| Planning / analysis workloads | `GraphController`, `ComparisonController`, `ComparisonsController`, `ExplanationController`, `RetrievalController`, `AskController`, `ConversationController`, `ProvenanceController`, `FindingFeedbackController` |
| Findings | `FindingInspectController` |
| Evolution | `EvolutionController` |
| Governance | `GovernanceController`, `GovernanceResolutionController`, `GovernancePreviewController`, `ManifestsController`, `PolicyPacksController` |
| Diagnostics | `OperatorTaskSuccessDiagnosticsController` |
| Notifications / integrations | `CustomerNotificationChannelPreferencesController`, `TeamsIncomingWebhookConnectionsController` |
| Pilots | `PilotsBoardPackController`, `TenantCostEstimateController` |
| Tenancy ROI / lifecycle | `TenantMeasuredRoiController`, `TenantExecDigestPreferencesController`, `TenantCustomerSuccessController` |
| Value reports | `ValueReportController` |

> **Note:** `ComparisonController` appears once under `Planning/` (pairwise compare). `ComparisonsController` is the extended run/export comparison history API on `v1/architecture`.

### `TenantTier.Standard` (selected action only — Pilot host)

| Source | Action | Route (family) |
|--------|--------|----------------|
| `PilotsController` | `PostSponsorOnePager` | `POST v1/pilots/runs/{runId}/sponsor-one-pager` |

### `TenantTier.Enterprise` (action scope)

| Source | Action | Notes |
|--------|--------|-------|
| `AuditController` | `ExportAudit` | `GET v1/audit/export` — CSV/JSON export. Other `AuditController` list/search routes are **not** tier-gated. |

## Debt / follow-ons (not in this doc’s scope)

- **SKU ↔ full endpoint matrix:** Packaging docs describe packaging intent; this file tracks **code-level** attributes. Controllers without the attribute are intentionally **not** listed here.
- **`402` vs `404` naming:** `PackagingTierProblemDetailsFactory` is named for “payment required” but implements **404** responses by design — avoid renaming without a breaking-change review.

## References

- `ArchLucid.Api/Filters/CommercialTenantTierFilter.cs`
- `ArchLucid.Api/ProblemDetails/PackagingTierProblemDetailsFactory.cs`
- `ArchLucid.Api/Attributes/RequiresCommercialTenantTierAttribute.cs`
- `ArchLucid.Api.Tests/CommercialPackagingMetadataTests.cs` (metadata regression)
- `ArchLucid.Api.Tests/Planning/PlanningGovernanceCommercialSmokeIntegrationTests.cs` (gated route smoke)
- `ArchLucid.Api.Tests/Alerts/AlertsCommercialTierPackagingIntegrationTests.cs` (404 vs 200 on `GET /v1/alerts` with `InMemoryTenantRepository` tier overrides)
- `ArchLucid.Api.Tests/CommercialTierIntegrationTestTenant.cs` (tier mutation helper for `ArchLucidApiFactory` + InMemory hosts)
