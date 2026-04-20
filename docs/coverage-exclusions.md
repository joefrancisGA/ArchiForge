> **Scope:** Code Coverage Exclusions - full detail, tables, and links in the sections below.

# Code Coverage Exclusions

This document describes the classes and methods excluded from code coverage via `[ExcludeFromCodeCoverage]` and the justification for each exclusion.

## Enforced CI coverage gates

After the full-solution run, ReportGenerator merges Coverlet fragments to **`Cobertura.xml`**; **`scripts/ci/assert_merged_line_coverage_min.py`** enforces the floors below in **`.github/workflows/ci.yml`** job **`.NET: full regression (SQL)`** (`dotnet-full-regression`). Parsing and the product filter are in **`scripts/ci/coverage_cobertura.py`**. **`is_product_archlucid_package()`** applies the per-package gate only to production **`ArchLucid.*`** assemblies (excludes test projects and **`ArchLucid.TestSupport`**); packages with zero coverable `<line/>` rows are skipped.

| Metric | Threshold | Script | CI job | Failure behavior |
|--------|-----------|--------|--------|------------------|
| Merged **line** | **79%** | [`scripts/ci/assert_merged_line_coverage_min.py`](../scripts/ci/assert_merged_line_coverage_min.py) (positional `79` = `--min-line-pct`) | `.NET: full regression (SQL)` | Exit **1** if root `line-rate` × 100 is below the floor. |
| Merged **branch** | **63%** | same (`--min-branch-pct 63`) | same | Exit **1** if root `branch-rate` × 100 is below the floor. |
| Per-product **line** | **63%** | same (`--min-package-line-pct 63`; script default **60**) | same | Exit **1** if any gated **`ArchLucid.*`** product package is below the floor or has coverable lines but missing `line-rate`. **No** **`--skip-package-line-gate`** in CI (every gated package must meet the floor). |

**Advisory per-package band (non-blocking):** When **`--warn-below-package-line-pct`** (default **70**) is greater than **`--min-package-line-pct`**, packages that **pass** the merge floor but sit **below** the advisory ceiling get plain-text lines written to **`--annotations-file`** (e.g. **`coverage-annotations-assert.txt`** in the **`coverage-metrics`** artifact). The **`coverage-pr-comment`** job appends that file to **`coverage-annotations.txt`** and emits each line as a GitHub **`::warning::`** for visibility. This does **not** fail the build.

**Exit 2** (script-wide): merged file missing/unparseable, or root **`line-rate`** or **`branch-rate`** missing so gates cannot be evaluated without silently passing.

**Rationale:** **79 / 63 / 63** is the **strict profile** (see **`docs/CODE_COVERAGE.md`**): merge-blocking gates track product quality on the merged Cobertura tree, including thin entrypoints such as **`ArchLucid.Jobs.Cli`**. If CI is red, add tests (or justified **`[ExcludeFromCodeCoverage]`** per **Exclusion Policy** below), then re-run full regression — do not lower floors without explicit sign-off.

**PR comment:** **`scripts/ci/build_coverage_pr_comment.py`** lists any product **`ArchLucid.*`** package under the per-package merge floor as the **same CI gate** as [`assert_merged_line_coverage_min.py`](../scripts/ci/assert_merged_line_coverage_min.py) on merged Cobertura (not a separate “warning” threshold).

**Exclusions in `coverage.runsettings`:** Tier-5 excludes (generated **OpenAPI** client, **NSwag** output, etc.) shrink denominators for merged Cobertura; gates still apply to the merged tree. See the **`coverage.runsettings`** file at repo root for the current exclude list.

**Stryker:** Scheduled mutation runs use multiple **`stryker-config*.json`** files (Persistence, Application, Decisioning, Coordinator, AgentRuntime); baseline scores are asserted via **`scripts/ci/assert_stryker_score_vs_baseline.py`** against **`stryker-baselines.json`**. Narrative: **[MUTATION_TESTING_STRYKER.md](MUTATION_TESTING_STRYKER.md)**.

## Exclusion Policy

Code is excluded from coverage only when:

1. It is a **thin wrapper** around an external SDK or service that cannot be exercised without a live external dependency (e.g., Azure OpenAI, SQL Server, external CLI tools).
2. It is a **pure data-transfer object** (DTO) used for Dapper row mapping with no logic (auto-properties only).
3. It is **application startup wiring** that is exercised by integration tests against `WebApplicationFactory` but not by unit tests.
4. The effort to unit-test the code **exceeds the risk** it represents, and the code is covered by integration or E2E tests instead.

Code with testable pure logic is **never** excluded, even when it lives in a class that also has untestable infrastructure code. In those cases, only the untestable method is excluded (e.g., `SqlSchemaBootstrapper.EnsureSchemaAsync`).

Exclusions change Cobertura denominators; CI still enforces **merged line**, **merged branch**, and **per-product line** gates on the merged report (see **Enforced CI coverage gates**).

---

## Category 1: Azure SDK / External Service Thin Wrappers

These classes delegate directly to Azure SDKs or HTTP clients with minimal or no branching logic. Testing them requires live Azure endpoints or HTTP servers.

| Class | Assembly | Justification |
|-------|----------|---------------|
| `AzureOpenAiCompletionClient` | AgentRuntime | Wraps `Azure.AI.OpenAI.ChatClient` |
| `AzureOpenAiEmbeddingClient` | Retrieval | Wraps `Azure.AI.OpenAI.EmbeddingClient` |
| `AzureOpenAiEmbeddingService` | Retrieval | Passthrough adapter over `IOpenAiEmbeddingClient` |
| `AzureAiSearchVectorIndex` | Retrieval | Passthrough adapter over `IAzureSearchClient` |
| `NotConfiguredAzureSearchClient` | Retrieval | Sentinel; every method throws `InvalidOperationException` |
| `HttpWebhookPoster` | Api | POSTs JSON via `IHttpClientFactory`; delivery channels mock `IWebhookPoster` |

## Category 2: Configuration / Options DTOs

| Class | Assembly | Justification |
|-------|----------|---------------|
| `AzureOpenAiOptions` | AgentRuntime | Config-binding DTO (`IOptions<T>`) with no logic |
| `SqlServerOptions` / nested settings | Persistence | Config-binding DTO (`IOptions<T>`) with no logic |

## Category 3: SQL Connection / RLS Infrastructure

These classes open live SQL Server connections, execute `sp_set_session_context`, or manage `SqlTransaction` lifecycle. They require a running SQL Server instance.

| Class | Assembly | Justification |
|-------|----------|---------------|
| `SqlConnectionFactory` | Persistence | Opens `SqlConnection` |
| `SqlConnectionFactory` | Data | Opens `SqlConnection` via `IConfiguration` |
| `RlsSessionContextApplicator` | Persistence | Executes `sp_set_session_context` via `SqlCommand` |
| `SessionContextSqlConnectionFactory` | Persistence | Decorator over `ResilientSqlConnectionFactory` + RLS applicator |
| `DapperArchLucidUnitOfWork` | Persistence | Wraps `IDbConnection`/`IDbTransaction` commit/rollback |
| `DapperArchLucidUnitOfWorkFactory` | Persistence | Opens connection and begins transaction |

**Note:** `ResilientSqlConnectionFactory` is **not** excluded because its `ComputeDelay` method contains testable exponential-backoff logic.

## Category 4: SQL-Dependent Repository Implementations

All Dapper/SQL repository classes that execute queries against SQL Server. Each implements an interface that has a corresponding `InMemory*` implementation tested by unit tests.

### ArchLucid.Persistence (29 classes)

- `DapperProductLearningPlanningRepository`
- `DapperProductLearningPilotSignalRepository`
- `DapperPolicyPackAssignmentRepository`, `DapperPolicyPackRepository`, `DapperPolicyPackVersionRepository`
- `DapperConversationThreadRepository`, `DapperConversationMessageRepository`
- `DapperArchitectureDigestRepository`, `DapperRecommendationRepository`, `DapperRecommendationLearningProfileRepository`, `DapperDigestSubscriptionRepository`, `DapperDigestDeliveryAttemptRepository`, `DapperAdvisoryScanScheduleRepository`, `DapperAdvisoryScanExecutionRepository`
- `DapperCompositeAlertRuleRepository`, `DapperAlertRuleRepository`, `DapperAlertRoutingSubscriptionRepository`, `DapperAlertRecordRepository`, `DapperAlertDeliveryAttemptRepository`
- `DapperAuditRepository`
- `DapperRetrievalIndexingOutboxRepository`
- `SqlRunRepository`, `SqlFindingsSnapshotRepository`, `SqlDecisionTraceRepository`, `SqlGraphSnapshotRepository`, `SqlArtifactBundleRepository`, `SqlGoldenManifestRepository`, `SqlContextSnapshotRepository`
- `SqlProvenanceSnapshotRepository`

### ArchLucid.Persistence — workflow Dapper repositories (`ArchLucid.Persistence.Data.Repositories`, 17 classes)

- `AgentEvaluationRepository`, `AgentEvidencePackageRepository`, `AgentExecutionTraceRepository`, `AgentResultRepository`, `AgentTaskRepository`
- `ArchitectureRequestRepository`, `ArchitectureRunIdempotencyRepository`, `ArchitectureRunRepository`
- `ComparisonRecordRepository`, `DecisionNodeRepository`, `DecisionTraceRepository`
- `EvidenceBundleRepository`, `GoldenManifestRepository`
- `GovernanceApprovalRequestRepository`, `GovernanceEnvironmentActivationRepository`, `GovernancePromotionRecordRepository`
- `RunExportRecordRepository`

## Category 5: SQL-Dependent Service Classes

| Class | Assembly | Justification |
|-------|----------|---------------|
| `SqlCutoverReadinessService` | Persistence | Every method runs aggregate SQL queries via Dapper |
| `SqlRelationalBackfillService` | Persistence | Scans SQL tables and inserts relational slices via Dapper |

## Category 6: Dapper Row-Mapping DTOs

Pure data-transfer objects used by Dapper for SQL result mapping. They contain only `{ get; init; }` auto-properties with no methods or logic.

| File | Classes | Assembly |
|------|---------|----------|
| `ProductLearningPlanningSqlRows.cs` | `ProductLearningScopeSqlRow`, `ProductLearningImprovementThemeSqlRow`, `ProductLearningImprovementPlanSqlRow`, `ProductLearningImprovementPlanSignalLinkSqlRow`, `ProductLearningImprovementPlanArtifactLinkSqlRow` | Persistence |
| `ProductLearningPilotSignalSqlRows.cs` | `FeedbackAggregateSqlRow`, `ArtifactOutcomeTrendSqlRow`, `RepeatedCommentThemeSqlRow` | Persistence |
| `GraphSnapshotStorageRow.cs` | `GraphSnapshotStorageRow` | Persistence |
| `GoldenManifestStorageRow.cs` | `GoldenManifestStorageRow` | Persistence |
| `FindingsSnapshotStorageRow.cs` | `FindingsSnapshotStorageRow` | Persistence |
| `ContextSnapshotStorageRow.cs` | `ContextSnapshotStorageRow` | Persistence |
| `ArtifactBundleStorageRow.cs` | `ArtifactBundleStorageRow` | Persistence |

## Category 7: Process-External / Filesystem Tools

| Class | Assembly | Justification |
|-------|----------|---------------|
| `MermaidCliDiagramImageRenderer` | Application | Requires `mmdc` CLI tool installed on host |
| `FileSystemDocumentLogoProvider` | Application | Reads logo files from local filesystem |

## Category 8: Application Startup / CLI Dispatch

| Class | Assembly | Justification |
|-------|----------|---------------|
| `Program` (partial) | Api | ASP.NET startup wiring; tested via `WebApplicationFactory` integration tests |
| `Program` (static) | Cli | CLI argument dispatch and console I/O |
| `Program` (static) | Jobs.Cli | ACA Jobs composition root (`WebApplication` + DI + schema bootstrap); **`JobsCommandLine`** is unit-tested in **`ArchLucid.Jobs.Cli.Tests`** |
| `ApiKeyAuthenticationHandler` | Api | ASP.NET authentication handler; tested via HTTP pipeline integration tests |

## Category 8b: CLI API-orchestration subcommands (`ArchLucid.Cli`)

These `internal static` command entry points wire console output to `ArchLucidApiClient` (already excluded in this assembly). Unit-testing them end-to-end would duplicate HTTP client tests; `SupportBundleCollector`, `ManifestValidator`, `CliCommandShared`, and related helpers retain line coverage.

| Class | Assembly | Justification |
|-------|----------|---------------|
| `ComparisonsCommand` | Cli | Comparisons / replay / diagnostics against API |
| `DoctorCommand` | Cli | Multi-probe readiness against API |
| `RunCommand` | Cli | Run workflow + API + filesystem |
| `DevUpCommand` | Cli | Host `docker compose`; environment-specific |
| `SupportBundleCommand` | Cli | Thin wrapper over `SupportBundleCollector` (tested) |
| `ArtifactsCommand` | Cli | Artifact download via API |
| `StatusCommand` | Cli | Run status via API |
| `SubmitCommand` | Cli | Agent result POST via API |
| `CommitCommand` | Cli | Commit manifest via API |
| `SeedCommand` | Cli | Seed path via API |
| `HealthCommand` | Cli | Reachability via `ArchLucidApiClient` |

## Category 9: Method-Level Exclusions

| Class | Method | Justification |
|-------|--------|---------------|
| `SqlSchemaBootstrapper` | `EnsureSchemaAsync` | Reads file and executes SQL batches. `SplitGoBatches` remains testable and is **not** excluded. |

## Category 10: Assembly-Level Exclusions

| Assembly | Justification |
|----------|---------------|
| `ArchLucid.TestSupport` | Test infrastructure (fakes, builders, helpers); not production code |

---

## Coverage Results After Exclusions

| Metric | Before Exclusions | After Exclusions | Delta |
|--------|-------------------|------------------|-------|
| Line coverage | 62.5% | **73%** | +10.5pp |
| Branch coverage | ~50% | **58.6%** | +8.6pp |
| Method coverage | ~72% | **81.6%** | +9.6pp |
| Assemblies | 19 | 17 | -2 |
| Classes | ~1050 | 905 | -145 |

The improvement is due to removing untestable SQL infrastructure code from the denominator, giving an accurate picture of how well the testable codebase is covered.

## Cobertura merge (why no Coverlet `<Threshold>`)

`coverage.runsettings` does **not** set Coverlet `<Threshold>`: collectors run **per test assembly**, so a single assembly-wide threshold would not match solution-wide coverage. CI merges fragments with ReportGenerator in **.NET: full regression (SQL)**, then applies the gates in **Enforced CI coverage gates**. **`scripts/ci/build_coverage_pr_comment.py`** reuses **`coverage_cobertura.py`** for PR summary text (per-package **63%** line matches **`assert_merged_line_coverage_min.py`**).

### Tracking — packages under the 63% per-product line floor

Merged **`Cobertura.xml`** is produced only after a successful **full regression** test run (see **`.github/workflows/ci.yml`** → **`.NET: full regression (SQL)`**). If that job fails **`assert_merged_line_coverage_min.py`** on the per-package gate, the script stdout lists each offending **`ArchLucid.*`** package and its line percentage.

**Remediation:** Add or extend tests for that assembly, adjust **`[ExcludeFromCodeCoverage]`** only per **Exclusion Policy** above, or open a time-bound exemption with an explicit tracking item (issue/ADR) — do not weaken the gate without product sign-off.
