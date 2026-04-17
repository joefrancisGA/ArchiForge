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
