# Code Coverage Exclusions

This document describes the classes and methods excluded from code coverage via `[ExcludeFromCodeCoverage]` and the justification for each exclusion.

## Exclusion Policy

Code is excluded from coverage only when:

1. It is a **thin wrapper** around an external SDK or service that cannot be exercised without a live external dependency (e.g., Azure OpenAI, SQL Server, external CLI tools).
2. It is a **pure data-transfer object** (DTO) used for Dapper row mapping with no logic (auto-properties only).
3. It is **application startup wiring** that is exercised by integration tests against `WebApplicationFactory` but not by unit tests.
4. The effort to unit-test the code **exceeds the risk** it represents, and the code is covered by integration or E2E tests instead.

Code with testable pure logic is **never** excluded, even when it lives in a class that also has untestable infrastructure code. In those cases, only the untestable method is excluded (e.g., `SqlSchemaBootstrapper.EnsureSchemaAsync`).

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
| `ApiKeyAuthenticationHandler` | Api | ASP.NET authentication handler; tested via HTTP pipeline integration tests |

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

## Repo-wide minimum (merged Cobertura)

`coverage.runsettings` intentionally does **not** set Coverlet `<Threshold>`: VSTest runs collectors **per test assembly**, so a single global percentage would fail assemblies that only cover part of the tree. CI merges Cobertura files with ReportGenerator (see `.github/workflows/ci.yml`), then **`scripts/ci/assert_merged_line_coverage_min.py`** enforces **70%** merged **line** coverage on `Cobertura.xml` from that merge step.
