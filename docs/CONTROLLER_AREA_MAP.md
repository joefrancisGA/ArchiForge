# API controller area map

**Purpose:** Navigate **`ArchLucid.Api/Controllers`** without moving 50+ files on disk. Each controller stays in the flat folder; this table is the **logical** grouping for reviews and onboarding.

| Area | Controllers |
|------|-------------|
| **Authority / runs** | `RunsController`, `AuthorityQueryController`, `AuthorityReplayController`, `AuthorityCompareController`, `ManifestsController`, `GraphController`, `ExplanationController` |
| **Compare / export** | `ComparisonController`, `ComparisonsController`, `RunComparisonController`, `ExportsController`, `ArtifactExportController`, `DocxExportController`, `AnalysisReportsController` |
| **Governance** | `GovernanceController`, `GovernancePreviewController`, `GovernanceResolutionController`, `PolicyPacksController` |
| **Advisory / learning** | `AdvisoryController`, `AdvisorySchedulingController`, `DigestSubscriptionsController`, `LearningController`, `RecommendationLearningController`, `ProductLearningController`, `EvolutionController` |
| **Alerts** | `AlertsController`, `AlertRulesController`, `CompositeAlertRulesController`, `AlertRoutingSubscriptionsController`, `AlertSimulationController`, `AlertTuningController` |
| **Retrieval / Ask** | `RetrievalController`, `AskController`, `ConversationController` |
| **Provenance / audit** | `ProvenanceController`, `ProvenanceQueryController`, `AuditController` |
| **Admin / jobs / demo** | `AdminController`, `JobsController`, `DiagnosticsController`, `DemoController`, `ScopeDebugController` |
| **Auth / docs / version** | `AuthDebugController`, `DocsController`, `VersionController` |

**DTOs** in the same folder (`AssignPolicyPackRequest`, `CreatePolicyPackRequest`, …) are not controllers; they sit beside controllers for historical layout.

## Related

- **`docs/API_VERSIONING.md`** — `v1` routing and `[ApiVersion]` conventions.
- **`docs/CODE_MAP.md`** — broader file index.
