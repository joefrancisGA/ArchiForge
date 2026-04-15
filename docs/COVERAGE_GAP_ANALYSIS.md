# Coverage gap analysis (merged Cobertura)

**Generated:** from `coverage-gap-1a\merged\Cobertura.xml` (full `ArchLucid.sln` test run + ReportGenerator merge).

**Scope:** Production `ArchLucid.*` assemblies only; excludes `*.Tests`, TestSupport, and Benchmarks.

## Bottom five assemblies by line coverage

| Assembly | Line coverage % | Coverable lines (approx.) |
|----------|-----------------|---------------------------|
| ArchLucid.Persistence | 53.04 | 6053 |
| ArchLucid.Api | 64.54 | 13660 |
| ArchLucid.Application | 75.50 | 15252 |
| ArchLucid.Host.Core | 76.06 | 6939 |
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

### ArchLucid.Host.Core (76.06% line coverage)

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

- **Merged line coverage:** 77.81%
- **Merged branch coverage:** 63.35%

## Recent targeted tests (correctness improvement track)

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
