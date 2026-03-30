# Test structure

**54R suite model (Core / Fast core / Integration / SQL / Full):** see **[TEST_EXECUTION_MODEL.md](TEST_EXECUTION_MODEL.md)** — canonical filters, root scripts (`test-*.cmd` / `.ps1`), and CI mapping.

## Bounded context map (quick reference)

| Test project | Primary bounded contexts / seams exercised |
|--------------|---------------------------------------------|
| **ArchiForge.Api.Tests** | HTTP API, auth policies, ProblemDetails, CORS, comparison replay, runs, governance, alerts (integration), SQL Server–backed persistence through the real host (**DbUp** on startup). |
| **ArchiForge.Decisioning.Tests** | Findings, compliance, alerts (pure logic), advisory scheduling math, governance resolution, graph mappers, JSON persistence contracts. |
| **ArchiForge.ContextIngestion.Tests** | Connectors, parsers, deduplication, `ContextIngestionService`, delta summaries. |
| **ArchiForge.Coordinator.Tests** | Run coordination, agent fakes, **`ContextIngestionRequestMapperTests`**, **`DocxExportServiceGoldenTests`** (OpenXML anchors). |
| **ArchiForge.DecisionEngine.Tests** | Schema validation, manifest/decision JSON contracts. |
| **ArchiForge.KnowledgeGraph.Tests** | Graph models, edge inference contracts. |
| **ArchiForge.Retrieval.Tests** | `RetrievalQueryService`, `InMemoryVectorIndex` (empty index, ranking, scope filters), **`CircuitBreakerGateTests`**, **`CircuitBreakingOpenAiEmbeddingClientTests`** (OpenAI embedding circuit breaker). |
| **ArchiForge.Persistence.Tests** | Dapper repositories against **real SQL Server** via **`ARCHIFORGE_SQL_TEST`** or Windows **LocalDB**; schema from **`DatabaseMigrator`** (same DbUp migrations as production SQL Server). Includes **53R cutover** tests: `JsonFallbackPolicyTests`, `FallbackPolicyDiagnosticsTests`, `CutoverReadinessReportTests` (unit); `PolicyModeFallbackSqlIntegrationTests`, `CutoverReadinessSqlIntegrationTests` (SQL integration). |

## Projects

- **ArchiForge.Api.Tests** — API integration tests using `WebApplicationFactory` (full app, **SQL Server** per factory via **`ArchiForgeApiFactory`**). Heavier; use for HTTP contracts, comparison replay, exports, run-not-found, 422/409. Advisory + alerts: **`AlertLifecycleIntegrationTests`**, **`DigestDeliveryLifecycleIntegrationTests`**, **`RetrievalQuerySmokeIntegrationTests`** (index + query via `GET api/retrieval/search`), **`AskThreadIntegrationTests`** (POST `api/ask` with seeded run, follow-up thread, conversation listing) with **`AlertLifecycleWebAppFactory`** (`ArchiForge:StorageProvider=InMemory` + **`AdvisoryIntegrationSeed`**). Graceful shutdown: **`AdvisoryScanHostedServiceShutdownTests`** (cancellation, exception survival). Background jobs: **`InMemoryBackgroundJobQueueTests`** (retry/DLQ, eviction, channel full). Resilience: **`ApiProblemDetailsExceptionFilterTests`** (`TimeoutException` → 503, `DbException` → 503 `DatabaseUnavailable`, **`CircuitBreakerOpenException`** → 503 `CircuitBreakerOpen` + `retryAfterUtc`), **`SqlConnectionHealthCheckTests`** (Healthy/Degraded/Unhealthy), **`SqlTransientDetectorTests`**, **`ResilientSqlConnectionFactoryTests`**, **`CircuitBreakingAgentCompletionClientTests`** (completion client wraps gate; failure opens circuit). Unit-style: **`ConversationServiceTests`**, **`AdvisoryDueScheduleProcessorTests`** (`Category=Unit`).
- **ArchiForge.DecisionEngine.Tests** — Unit and scenario tests for the decision engine; optional integration tests with real JSON schemas (see `SchemaValidationIntegrationTests`).
- **ArchiForge.ContextIngestion.Tests** — Fast unit tests for ingestion parsers, deduplication, document connector warnings, delta summary builder, and **`ContextIngestionService`** (in-memory snapshot repo + fake connectors).
- **ArchiForge.Coordinator.Tests**, **ArchiForge.AgentRuntime.Tests**, **ArchiForge.Decisioning.Tests**, **ArchiForge.Retrieval.Tests**, etc. — Domain/component tests; no web stack unless explicitly added.
- **ArchiForge.Persistence.Tests** — SQL integration tests for `DapperArchitectureDigestRepository`, `DapperAlertRuleRepository`, and future Dapper round-trips. Requires **Docker** (Linux SQL Server image). After container start, tests run **`ArchiForge.Data.Infrastructure.DatabaseMigrator.Run`** so DDL matches embedded **`ArchiForge.Data/Migrations/*.sql`**, not the consolidated `ArchiForge.sql` reference script alone. **Contract tests** (`Contracts/` folder): abstract base classes (`AlertRuleRepositoryContractTests`, `ArchitectureDigestRepositoryContractTests`) define shared assertions; `InMemory*ContractTests` (`Category=Unit`) and `Dapper*ContractTests` (`Category=SqlServerContainer`) run the same suite against both implementations to guarantee InMemory ↔ Dapper parity.

## Suite=Core (high-value regression subset)

A curated set of tests is tagged at **class** level with:

```csharp
[Trait("Suite", "Core")]
```

Run the **core suite** (all tagged tests across the solution):

```bash
dotnet test ArchiForge.sln --filter "Suite=Core"
```

From the repo root, **`test-core.cmd`** / **`test-fast-core.cmd`** (and `.ps1` equivalents) wrap the same filters. For **integration**, **SQL Server persistence**, **full regression**, **operator UI unit** (`npm test` in **`archiforge-ui/`**), and **operator UI e2e smoke**, use **`test-integration`**, **`test-sqlserver-integration`**, **`test-full`**, and **`test-ui-smoke`** (see **TEST_EXECUTION_MODEL.md**). Prefer **`[Trait("Category", "…")]`** on every new `*Tests` class (`Unit`, `Integration`, `SqlServerContainer`, or `Slow`) so suite filters stay accurate.

**Fast core** — Core tests that are neither **Slow** nor **Integration** (useful for quick feedback; skips API integration and other heavy cases):

```bash
dotnet test ArchiForge.sln --filter "Suite=Core&Category!=Slow&Category!=Integration"
```

Heavy or integration-style tests may add `[Trait("Category", "Slow")]` and/or `[Trait("Category", "Integration")]` in addition to `Suite=Core` so they remain in the full core run but drop out of the fast-core filter.

## Categories (optional filtering)

All tests in **ArchiForge.Api.Tests** that extend **IntegrationTestBase** (and thus use `WebApplicationFactory`) are integration tests. They are tagged at class level with:

```csharp
[Trait("Category", "Integration")]
```

Use this to filter runs: exclude with `Category!=Integration` for faster feedback, or run only integration tests with `Category=Integration`. Other projects (e.g. **DecisionEngine.Tests**) use the same trait on individual tests that need real I/O (e.g. `SchemaValidationIntegrationTests`).

**SQL Server container tests** in **ArchiForge.Persistence.Tests** are tagged:

```csharp
[Trait("Category", "SqlServerContainer")]
```

Exclude them when Docker is unavailable (CI agents without Docker, local quick runs):

```bash
dotnet test --filter "Category!=SqlServerContainer"
```

To skip both full API integration tests and SQL container tests:

```bash
dotnet test --filter "Category!=Integration&Category!=SqlServerContainer"
```

To run only Persistence SQL integration tests:

```bash
dotnet test --filter "Category=SqlServerContainer"
```

To run only fast/unit tests (exclude integration):

```bash
dotnet test --filter "Category!=Integration"
```

To run only integration tests:

```bash
dotnet test --filter "Category=Integration"
```

## Fixtures and shared setup

- **IntegrationTestBase** (Api.Tests) — provides `HttpClient`, `JsonOptions`, and `JsonContent(object)` from `ArchiForgeApiFactory`. Tests that extend it should use the base `JsonOptions` and `JsonContent` for request/response JSON so behavior is consistent and configurable in one place.
- **ComparisonReplayTestFixture** (Api.Tests) — static helpers: `CreateRunExecuteCommitReplayAsync`, `PersistEndToEndComparisonAsync` for comparison-replay flows.

**Unit-style tests in Api.Tests:** Some tests do *not* extend **IntegrationTestBase** (e.g. **AgentResultDiffServiceTests**, **ManifestDiffServiceTests**, **ApiProblemDetailsExceptionFilterTests**, **ArchitectureApplicationServiceTests**, **DatabaseMigrationScriptTests**). They do not spin up the full API; they test services, filters, or scripts in isolation. These are tagged with `[Trait("Category", "Unit")]` so you can run only unit tests with `dotnet test --filter "Category=Unit"`.

No separate unit-only test project exists; use `[Trait("Category", "Integration")]` or `Category=Unit` and filter as above if you want to separate runs.
