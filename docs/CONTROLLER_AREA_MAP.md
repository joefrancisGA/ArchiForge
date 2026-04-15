# API controller area map

`ArchLucid.Api/Controllers/` groups endpoints by **functional area** for navigation. Types remain in namespace `ArchLucid.Api.Controllers` (flat folder today; this map is the logical grouping).

| Area | Controllers |
|------|-------------|
| **Admin & diagnostics** | `AdminController`, `DiagnosticsController`, `JobsController`, `VersionController`, `DemoController`, `ScopeDebugController`, `AuthDebugController`, `DocsController` |
| **Authority & runs** | `RunsController`, `AuthorityQueryController`, `AuthorityReplayController`, `AuthorityCompareController`, `RunComparisonController`, `AnalysisReportsController`, `ExportsController` |
| **Governance** | `GovernanceController`, `GovernancePreviewController`, `GovernanceResolutionController`, `PolicyPacksController`, `ManifestsController` |
| **Alerts** | `AlertsController`, `AlertRulesController`, `AlertRoutingSubscriptionsController`, `AlertSimulationController`, `AlertTuningController`, `CompositeAlertRulesController` |
| **Advisory & learning** | `AdvisoryController`, `AdvisorySchedulingController`, `LearningController`, `RecommendationLearningController`, `ProductLearningController` |
| **Explainability & provenance** | `ExplanationController`, `ProvenanceController`, `ProvenanceQueryController`, `GraphController` |
| **Comparison & replay** | `ComparisonController`, `ComparisonsController`, `RetrievalController` |
| **Evolution** | `EvolutionController` |
| **Conversation & ask** | `AskController`, `ConversationController` |
| **Digests** | `DigestSubscriptionsController` |
| **Artifacts & export** | `ArtifactExportController`, `DocxExportController`, `AuditController` |

**Bulk operator APIs (2026-04-15):**

- `POST /v1/admin/runs/archive-by-ids` — partial-success archival by run id (max 100).
- `POST /v1/governance/approval-requests/batch-review` — approve or reject many requests (max 50, partial success).
- `POST /v1/alerts/acknowledge-batch` — acknowledge many alerts in scope (max 100, partial success).
- `POST /v1/admin/diagnostics/data-consistency/orphan-golden-manifests` — dry-run or delete orphan `GoldenManifests` (max 500; removes `ArtifactBundles` first).
- `POST /v1/admin/diagnostics/data-consistency/orphan-findings-snapshots` — dry-run or delete orphan `FindingsSnapshots` not referenced by a golden manifest (max 500).
