> **Scope:** Coverage gap analysis (merged Cobertura) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Coverage gap analysis (merged Cobertura)

**Generated:** from `coverage-gap-1a\merged\Cobertura.xml` (full `ArchLucid.sln` test run + ReportGenerator merge).

**Scope:** Production `ArchLucid.*` assemblies only; excludes `*.Tests`, TestSupport, and Benchmarks.

## Bottom five assemblies by line coverage

| Assembly | Line coverage % | Coverable lines (approx.) |
|----------|-----------------|---------------------------|
| ArchLucid.Persistence | 53.04 | 6053 |
| ArchLucid.Api | 64.54 | 13662 |
| ArchLucid.Application | 75.50 | 15252 |
| ArchLucid.Host.Core | 76.17 | 6983 |
| ArchLucid.AgentRuntime | 79.34 | 5841 |

## Files with most uncovered lines (top three per assembly above)

### ArchLucid.Persistence (53.04% line coverage)

| Rank | File | Uncovered line entries |
|------|------|------------------------|
| 1 | `ArchLucid.Persistence\GoldenManifests\GoldenManifestPhase1RelationalRead.cs` | 258 |
| 2 | `ArchLucid.Persistence\GraphSnapshots\GraphSnapshotRelationalRead.cs` | 198 |
| 3 | `ArchLucid.Persistence\Findings\FindingsSnapshotRelationalRead.cs` | 185 |

### ArchLucid.Api (64.54% line coverage)

| Rank | File | Uncovered line entries |
|------|------|------------------------|
| 1 | `ArchLucid.Api\Controllers\AdvisoryController.cs` | 143 |
| 2 | `ArchLucid.Api\Services\Evolution\EvolutionSimulationService.cs` | 119 |
| 3 | `ArchLucid.Api\Controllers\AuthorityQueryController.cs` | 117 |

### ArchLucid.Application (75.50% line coverage)

| Rank | File | Uncovered line entries |
|------|------|------------------------|
| 1 | `ArchLucid.Application\Analysis\EndToEndReplayComparisonExportService.cs` | 257 |
| 2 | `ArchLucid.Application\Runs\Orchestration\ArchitectureRunCommitOrchestrator.cs` | 175 |
| 3 | `ArchLucid.Application\Explanation\RunRationaleService.cs` | 169 |

### ArchLucid.Host.Core (76.17% line coverage)

| Rank | File | Uncovered line entries |
|------|------|------------------------|
| 1 | `ArchLucid.Host.Core\Jobs\BackgroundJobQueueProcessorHostedService.cs` | 102 |
| 2 | `ArchLucid.Host.Core\Hosted\AuthorityPipelineWorkProcessor.cs` | 80 |
| 3 | `ArchLucid.Host.Core\Startup\WorkerHostPipelineExtensions.cs` | 68 |

### ArchLucid.AgentRuntime (79.34% line coverage)

| Rank | File | Uncovered line entries |
|------|------|------------------------|
| 1 | `ArchLucid.AgentRuntime\AgentExecutionTraceRecorder.cs` | 62 |
| 2 | `ArchLucid.AgentRuntime\ComplianceAgentHandler.cs` | 55 |
| 3 | `ArchLucid.AgentRuntime\CriticAgentHandler.cs` | 55 |

## Merged totals (reference)

- **Merged line coverage:** 77.82%
- **Merged branch coverage:** 63.38%

## Recent targeted tests (correctness improvement track)

- **2026-04-23 — Persistence relational readers + Api authority/advisory/evolution (coverage gap follow-up):** **`GoldenManifestPhase1RelationalReadComplianceWhitespaceDirectSqlIntegrationTests`** (whitespace `ComplianceJson` → empty **`ComplianceSection`**); **`GoldenManifestPhase1RelationalReadOrderedDecisionsNonMonotonicInsertDirectSqlIntegrationTests`** (relational decisions **`ORDER BY SortOrder`** with inverted insert order + split evidence/node children); **`GraphSnapshotRelationalReadJsonMergeLabelFromEdgesJsonDirectSqlIntegrationTests`** (`EdgesJson` label/property merge when relational edge rows exist but **`GraphSnapshotEdgeProperties`** is empty); **`FindingsSnapshotRelationalReadLegacyJsonFallbackMatrixDirectSqlIntegrationTests`** (`[Theory]` / `[InlineData]` — malformed legacy JSON vs. empty **`Findings`** list when **`FindingRecords`** is absent). **`AdvisoryControllerSecurityIntegrationTests`** (API-key mode anonymous **`401`** on list recommendations; Reader role **`403`** on recommendation action). **`AuthorityQueryControllerListRunsPagedIntegrationTests`**, **`AuthorityQueryControllerProblemDetailsIntegrationTests`**, **`AuthorityQueryControllerAnonymousIntegrationTests`** (paged vs. array list for slug **`EnterpriseRag`**, whitespace `projectId` **400** body, unknown run summary **404**, provenance **422** via **`AdvisoryIntegrationSeed`** incomplete chain, anonymous list **401**). **`EvolutionSimulationServiceInvalidPlanSnapshotJsonTests`**, **`EvolutionSimulationServiceEvaluateLinkedRunsHappyPathTests`** (`PlanSnapshotJson` **`null`** JSON → **`InvalidOperationException`**; **`SimulateCandidateWithEvaluationAsync`** with linked run id, evaluation envelope, **`DeleteByCandidateAsync`**). *Assembly line % in the tables above and **`coverage-gap-1a/merged/Cobertura.xml`** remain tied to the last full-solution coverage merge until CI or a local green `dotnet test ArchLucid.sln` with **`coverage.runsettings`** + ReportGenerator refresh completes.*
- **2026-04-20 — Persistence relational readers + Core concurrency:** **`GoldenManifestPhase1RelationalReadBranchMatrixDirectSqlIntegrationTests`**, **`GraphSnapshotRelationalReadBranchMatrixDirectSqlIntegrationTests`**, **`FindingsSnapshotRelationalReadBranchMatrixDirectSqlIntegrationTests`** (SQL branch matrix, 15 cases each via `[InlineData]` / `[Theory]`); **`ArchitectureRequestIdempotencyConcurrencyIntegrationTests`** (16× same idempotency key); **`CommitRunConcurrencyIntegrationTests`** (8× parallel commit, reconciled `200` + same manifest version); **`GovernanceApprovalConcurrencyIntegrationTests`** (32× approve); **`PolicyPackPublishConcurrencyIntegrationTests`** (8× publish). Parallel admin **HTTP** archive-by-ids remains covered at the repository layer by **`SqlRunRepositoryArchiveByIdsConcurrencyTests`** (`ArchLucid.Persistence.Tests`). **Stryker:** **`stryker-config.persistence.json`**, scheduled workflow **Persistence** matrix entry, **`stryker-config.json`** thresholds aligned (**70 / 55 / 55**). *Merged Cobertura line/branch % for `ArchLucid.Persistence` in the tables above stay at the last full-solution merge until you re-run `dotnet test ArchLucid.sln` with coverage and refresh `coverage-gap-1a/merged/Cobertura.xml`.*
- **2026-04-17 — Improvement 1 (`lowest-assembly-tests` slice, Api / Evolution):** **`EvolutionSimulationServiceTests`** — Moq repositories + **`IArchitectureAnalysisService`** / **`ISimulationEvaluationService`**; **`CreateCandidateFromImprovementPlanAsync`** throws **`EvolutionResourceNotFoundException`** with **`ProblemTypes.LearningImprovementPlanNotFound`** when plan missing; **`RunShadowEvaluationAsync`** with empty **`LinkedArchitectureRunIds`** updates candidate to **`Simulated`**, returns no simulation rows, never calls analysis/evaluation or run insert/delete.
- **2026-04-17 — Improvement 1 (`lowest-assembly-tests` slice, Api / Advisory):** **`AdvisoryControllerListRecommendationsIntegrationTests`** — **`GET /v1/advisory/runs/{runId}/recommendations`**: **200** + empty **`RecommendationRecordResponse`** list for a never-seeded run id; same after architecture **commit** before **`GET …/improvements`** (no persisted recommendations yet).
- **2026-04-17 — Improvement 1 (`lowest-assembly-tests` slice, Application / export):** **`EndToEndReplayComparisonExportServiceTests`** — Moq **`IEndToEndReplayComparisonSummaryFormatter`**; **`GenerateMarkdown`** **short** vs **default** (separator, **`## Run Metadata Diff`**, **`### Interpretation Notes`** / **`### Warnings`**); **`GenerateHtml`** **short** omits extended sections (**`Run Metadata Diff`**, interpretation lists).
- **2026-04-17 — Improvement 1 (`lowest-assembly-tests` slice, Persistence / Findings):** **`FindingsSnapshotRelationalReadOrderedAlternativePathsDirectSqlIntegrationTests`** — one **`FindingRecords`** row plus two **`FindingTraceAlternativePaths`** rows inserted with **non-monotonic** **`SortOrder`** (1 then 0); **`LoadRelationalSnapshotAsync`** returns **`ExplainabilityTrace.AlternativePathsConsidered`** in **`ORDER BY SortOrder`** order.
- **2026-04-17 — Improvement 1 (`lowest-assembly-tests` slice, Persistence / GoldenManifest):** **`GoldenManifestPhase1RelationalReadOrderedAssumptionsDirectSqlIntegrationTests`** — two **`GoldenManifestAssumptions`** rows inserted with **non-monotonic** **`SortOrder`** (1 then 0); **`HydrateAsync`** returns **`Assumptions`** in **`ORDER BY SortOrder`** order; **`AssumptionsJson`** ignored when relational rows exist.
- **2026-04-16 — Improvement 1 (`lowest-assembly-tests` slice, Persistence / Graph):** **`GraphSnapshotRelationalReadOrderedWarningsNoEdgesDirectSqlIntegrationTests`** — no relational nodes or edges; two **`GraphSnapshotWarnings`** rows inserted with **non-monotonic** **`SortOrder`** (1 then 0); asserts **`HydrateAsync`** returns warnings in **`ORDER BY SortOrder`** order and ignores **`WarningsJson`** on the relational path.
- **2026-04-16 — Weighted improvements 1–6 (verification + carry-over):** **`scripts/ci/assert_v1_traceability.py`** (per-assembly “no test matches” no longer zeroes whole solution; UTF-8 stdout; ASCII-safe logging). **`coverage_gap_analysis.py`** doc refresh from **`coverage-gap-1a/merged/Cobertura.xml`**. **Persistence:** migration **073** + **`SqlRunRepository`** archival cascade to **`ArtifactBundles`** / **`AgentExecutionTraces`** / **`ComparisonRecords`**; **`SqlRunRepositoryArchivalExtendedCascadeTests`**. **API:** **`ApiControllerMutationPolicyGuardTests`**, **`ApiVersioningReaderRoutingTests`**, combined **`QueryString`** + **`Header`** **`api-version`** readers. **Host.Composition:** **`AuthSafetyGuardTests`** + **`ArchLucidAuthorizationPoliciesRegistrationTests`** (regression for **Imp2** prompts).
- **2026-04-15 — Improvement 1 (branch / fallback branches, Persistence):** **`GoldenManifestPhase1RelationalReadWhitespaceJsonFallbackDirectSqlIntegrationTests`** — no relational slice rows + **whitespace/empty** **AssumptionsJson** / **WarningsJson** / **ProvenanceJson** / **DecisionsJson** + **ComplianceJson** whitespace → empty lists / default **ComplianceSection** (**`FallbackDeserializeList`**, **`FallbackDeserializeProvenance`**, **`FallbackDeserializeDecisions`**, **`DeserializeCompliance`**). **`GraphSnapshotRelationalReadJsonMergePartialEdgeDirectSqlIntegrationTests`** — **EdgesJson** merge on when **GraphSnapshotEdgeProperties** is empty; relational edge **e-sql-only** absent from **EdgesJson** → **`jsonById.TryGetValue`** false (no label/prop merge). **`FindingsSnapshotRelationalReadMinimalChildrenDirectSqlIntegrationTests`** — single **FindingRecord** with no child tables → empty **RelatedNodeIds** / **RecommendedActions** / **Properties** / **ExplainabilityTrace** slices.
- **2026-04-15 — Six improvement prompts (single session):** Run archival **SQL cascade** (**`066_GoldenManifestsFindingsSnapshots_ArchivedUtc`**, **`SqlRunRepository`** transactional batch + by-id); **`DataArchivalOrphanProbeSqlIntegrationTests`** asserts **`ArchivedUtc`** on **`dbo.GoldenManifests`** / **`dbo.FindingsSnapshots`**. **`DurableAuditLogRetry`** + **`DurableAuditLogRetryTests`**; **`ArchitectureRunCreateOrchestrator`** uses retry for **`CoordinatorRunCreated`**. **`IntegrationEventOutboxProcessorTests.ProcessPendingBatchAsync_processes_multiple_entries_in_one_batch`**; **`DataArchivalCoordinatorTests.RunOnceAsync_when_all_retention_non_positive_skips_archival_paths`**. Outbox convergence: **`archlucid:slo:integration_event_outbox_oldest_age_seconds`** + **`ArchLucidIntegrationEventOutboxConvergenceSlow`** (60s / 5m), **`docs/API_SLOS.md`** § Outbox convergence. **`stryker-config.persistence-coordination.json`** + scheduled workflow matrix + **`stryker-baselines.json`** (**PersistenceCoordination** 65%). **`ApiControllerProblemDetailsSourceGuardTests`** — bare **`Conflict()`** / **`BadRequest()`** guard.
- **2026-04-15 — Correctness prompts 1 / 2 / 3 / 5 / 6 (session):** **`ManifestVersionIncrementRules`** + **`ManifestVersionIncrementPropertyTests`**; **`ArchitectureRunStatusTransitionPropertyTests`**; **`AlertDeliveryCompositeKeyPropertyTests`**; **`AgentExecutionTraceRecorderRecordAsyncEdgeTests`**; **`DapperArchitectureRunIdempotencyRepositoryContractTests.TryInsert_parallel_same_key_only_one_wins`** (SQL); **`DataArchivalOrphanProbeSqlIntegrationTests`** (orphan probe SQL mirrored from **`DataConsistencyOrphanProbeSql`**, post-**`DataArchivalCoordinator`**); **`IntegrationEventPublishingTests`** + **`CircuitBreakerGateAuditCallbackTests`** (fatal-exception filters on **`IntegrationEventPublishing`**, **`CircuitBreakerGate`**, **`CircuitBreakerAuditBridge`**). Cursor rule **`.cursor/rules/SingleLineThrowNoBraces.mdc`** (single-line **`throw`** without braces when it is the only statement).
- **2026-04-15 — Improvement 1 (prompts `coverage-gap-report`, `lowest-assembly-tests`, `governance-workflow-fscheck`):** ReportGenerator merge from **`coverage-gap-1a/**/coverage.cobertura.xml`** after a **partial** full-solution run (**`GreenfieldSqlBootIntegrationTests`** failed once with SQL “Operation cancelled by user” — re-run **`dotnet test ArchLucid.sln`** with coverage for a clean merge). **`GoldenManifestPhase1RelationalReadDirectSqlIntegrationTests`**: relational **decisions** with **no** **GoldenManifestDecisionEvidenceLinks** / **GoldenManifestDecisionNodeLinks**; **provenance** with **GoldenManifestProvenanceAppliedRules** only (findings + graph nodes empty, JSON provenance loses to relational rules). **`GovernanceWorkflowDryRunSubmissionPropertyTests`**: FsCheck dry **`SubmitApprovalRequestAsync`** → **Submitted** shape, **`CreateAsync` / baseline / durable audit** never called (valid dev→test and test→prod pairs).
- **2026-04-15 — doc refresh:** Full **`coverage-gap-report`** pipeline (solution test + ReportGenerator + **`scripts/ci/coverage_gap_analysis.py`**). Merged totals above; **ArchLucid.Persistence** remains the lowest assembly (~53% line).
- **2026-04-15 — tests:** **`GoldenManifestPhase1RelationalReadDirectSqlIntegrationTests`** — relational **decisions** (**GoldenManifestDecisionEvidenceLinks** / **GoldenManifestDecisionNodeLinks**, **SortOrder**), **provenance** from **GoldenManifestProvenanceSourceGraphNodes** + **GoldenManifestProvenanceAppliedRules** without source-finding rows, relational **warnings** + **provenance source findings**, JSON fallbacks (**AssumptionsJson**, **ProvenanceJson**, **DecisionsJson**) when relational slice rows are absent. **`GraphSnapshotRelationalReadDirectSqlIntegrationTests`** — **GraphSnapshotWarnings** override + **EdgesJson** merge when **GraphSnapshotEdgeProperties** is empty. **`FindingsSnapshotRelationalReadDirectSqlIntegrationTests`** — full relational **FindingRecords** path. **`RunLifecycleStatePropertyTests`** (`ArchLucid.Application.Tests`) — FsCheck **`CommitRunAsync`** gates. **`GovernanceWorkflowTransitionConflictPropertyTests`** — concurrent terminal peer → **`GovernanceApprovalReviewConflictException`**; invalid env pairs on **`SubmitApprovalRequestAsync`**. **`GovernanceWorkflowSegregationAndPromotionPropertyTests`** — **`PromoteAsync`** rejects approval **ManifestVersion** mismatch. **`scripts/ci/coverage_gap_analysis.py`** — **`ValueError`** handler uses the correct file path variable.
- **2026-04-14:** Extended **`GoldenManifestPhase1RelationalReadDirectSqlIntegrationTests`** with relational **warnings** and **provenance source findings** (SQL). **`AlertEvaluatorDeduplicationKeyPropertyTests`** — dedupe keys for **`CriticalRecommendationCount`** and **`NewComplianceGapCount`** (`ArchLucid.Decisioning.Tests`).

## How to refresh

Narrative bullets under **Recent targeted tests** live in `docs/COVERAGE_GAP_ANALYSIS_RECENT.md` and are merged by this script when that file exists.

```powershell
dotnet test ArchLucid.sln -c Release --settings coverage.runsettings `
  --collect:"XPlat Code Coverage" --results-directory .\coverage-gap-1a
dotnet tool restore
dotnet reportgenerator "-reports:coverage-gap-1a/**/coverage.cobertura.xml" "-targetdir:coverage-gap-1a/merged" "-reporttypes:Cobertura"
python scripts/ci/coverage_gap_analysis.py
```
