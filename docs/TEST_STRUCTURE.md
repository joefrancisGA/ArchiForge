# Test structure (Change Set 54R)

Operator cheat sheet for **what each tier means** and **how to run it**. CI job names and full narrative: **[TEST_EXECUTION_MODEL.md](TEST_EXECUTION_MODEL.md)**. SQL variables and LocalDB: **[BUILD.md](BUILD.md)**.

## 54R tiers

| Tier | Meaning | Filter |
|------|---------|--------|
| **Core (corset)** | Curated high-value regression; opt-in per **class** with `[Trait("Suite", "Core")]` | `Suite=Core` |
| **Fast core** | Core tests that are **not** `Category=Slow` and **not** `Category=Integration` (typical CI first gate) | `Suite=Core&Category!=Slow&Category!=Integration` |
| **Integration** | Real API host (`WebApplicationFactory`), HTTP-level behavior | `Category=Integration` |
| **Slow** | Long-running or heavy; excluded from fast core | `Category=Slow` |
| **Full regression** | Whole solution, all traits (unless skipped in test code) | *(no filter)* |

### Run each (.NET, repo root)

```bash
dotnet test ArchiForge.sln --filter "Suite=Core"
```

```bash
dotnet test ArchiForge.sln --filter "Suite=Core&Category!=Slow&Category!=Integration"
```

```bash
dotnet test ArchiForge.sln --filter "Category=Integration"
```

```bash
dotnet test ArchiForge.sln --filter "Category=Slow"
```

```bash
dotnet test ArchiForge.sln
```

**Configuration:** GitHub Actions .NET jobs use **`-c Release`**. Repo-root `test-*.cmd` / `.ps1` call `dotnet test` **without** `-c` (typically **Debug**). To mirror CI: `dotnet test ArchiForge.sln -c Release`.

**Windows (same filters):** `test-core.cmd`, `test-fast-core.cmd`, `test-integration.cmd`, `test-slow.cmd`, `test-full.cmd` (and `.ps1` where present).

### Release candidate packaging (56R)

**Doc:** [RELEASE_LOCAL.md](RELEASE_LOCAL.md). **Scripts:** `build-release`, `package-release`, `run-readiness-check` (`.cmd` / `.ps1` at repo root). `run-readiness-check` runs a **Release** build, **fast core** tests with `-c Release`, then **Vitest** when Node is on `PATH`. **Pilot onboarding:** [PILOT_GUIDE.md](PILOT_GUIDE.md), [OPERATOR_QUICKSTART.md](OPERATOR_QUICKSTART.md). **E2E release smoke:** [RELEASE_SMOKE.md](RELEASE_SMOKE.md) (`release-smoke.cmd`).

### SQL Server–first (Persistence Dapper)

Persistence integration against a **real** SQL Server (Dapper + DbUp migrations; **no EF**):

```bash
dotnet test ArchiForge.Persistence.Tests --filter "Category=SqlServerContainer"
```

**Script:** `test-sqlserver-integration.cmd` / `.ps1`

### Operator UI (`archiforge-ui/`)

**Vitest** (jsdom, fast — minimal harness):

```bash
cd archiforge-ui
npm ci
npm test
```

```bash
npm run test:watch
```

**Playwright** smoke (browser, slower):

```bash
npm run test:e2e
```

**Scripts (repo root):** `test-ui-unit.cmd` / `.ps1` (Vitest), `test-ui-smoke.cmd` / `.ps1` (Playwright)

**Change Set 55R — focused review-workflow smoke:** component + mocked API contract tests under `archiforge-ui/src/review-workflow/`, `ShellNav`, artifact/compare helpers. Command list: [archiforge-ui/docs/TESTING_AND_TROUBLESHOOTING.md](../archiforge-ui/docs/TESTING_AND_TROUBLESHOOTING.md#3-55r--review-workflow-smoke-tests-change-set-55r). Operator context: [docs/operator-shell.md](operator-shell.md).

---

## SQL Server for API + Persistence tests

- **No SQLite.** Use **SQL Server** for anything that hits the DB; tests use **Dapper** and **DbUp** (`ArchiForge.Persistence/Migrations/`).
- **API integration** (`ArchiForge.Api.Tests`): factories create ephemeral databases on the configured instance — set **`ARCHIFORGE_SQL_TEST`** or **`ARCHIFORGE_API_TEST_SQL`** on Linux/macOS/CI; Windows may use **localhost** / LocalDB if unset (see **BUILD.md**).
- **CI** sets `ARCHIFORGE_SQL_TEST` against the SQL Server service container for the full regression job.

**Example (bash):**

```bash
export ARCHIFORGE_SQL_TEST='Server=127.0.0.1,1433;User Id=sa;Password=YourPassword;TrustServerCertificate=True;Initial Catalog=ArchiForgePersistenceTests'
dotnet test ArchiForge.sln
```

**PowerShell:**

```powershell
$env:ARCHIFORGE_SQL_TEST = 'Server=127.0.0.1,1433;User Id=sa;Password=YourPassword;TrustServerCertificate=True;Initial Catalog=ArchiForgePersistenceTests'
dotnet test ArchiForge.sln
```

Skip SQL-container or integration slices when needed:

```bash
dotnet test ArchiForge.sln --filter "Category!=SqlServerContainer"
```

```bash
dotnet test ArchiForge.sln --filter "Category!=Integration&Category!=SqlServerContainer"
```

---

## Bounded context map (quick reference)

| Test project | Primary bounded contexts / seams exercised |
|--------------|---------------------------------------------|
| **ArchiForge.Api.Tests** | HTTP API, auth policies, ProblemDetails, CORS, comparison replay, runs, governance, alerts (integration), SQL Server–backed persistence through the real host (**DbUp** on startup). |
| **ArchiForge.Decisioning.Tests** | Findings, compliance, alerts (pure logic), advisory scheduling math, governance resolution, graph mappers, JSON persistence contracts. |
| **ArchiForge.ContextIngestion.Tests** | Connectors, parsers, deduplication, `ContextIngestionService`, delta summaries. |
| **ArchiForge.Coordinator.Tests** | Run coordination, agent fakes, **`ContextIngestionRequestMapperTests`**, **`DocxExportServiceGoldenTests`** (OpenXML anchors). |
| **ArchiForge.Application.Tests** | **`ArchitectureRunService`** execute/commit and idempotency, **`ReplayRunService`**, **`DeterminismCheckService`**, hashing helpers — Application-layer orchestration with mocked Data/Coordinator ports (**`Suite=Core`** on classes). |
| **ArchiForge.DecisionEngine.Tests** | Schema validation, manifest/decision JSON contracts. |
| **ArchiForge.KnowledgeGraph.Tests** | Graph models, edge inference contracts. |
| **ArchiForge.Retrieval.Tests** | `RetrievalQueryService`, `InMemoryVectorIndex` (empty index, ranking, scope filters), **`CircuitBreakerGateTests`**, **`CircuitBreakingOpenAiEmbeddingClientTests`** (OpenAI embedding circuit breaker). |
| **ArchiForge.Persistence.Tests** | Dapper repositories against **real SQL Server** via **`ARCHIFORGE_SQL_TEST`** or Windows **LocalDB**; schema from **`DatabaseMigrator`** (same DbUp migrations as production SQL Server). **`Contracts/`** abstract bases with **InMemory** + **Dapper** implementations (agent evaluations, decision nodes, coordinator manifest/trace, run exports, architecture runs, etc.). **`AuthorityRunOrchestratorTests`** exercise **`ArchiForge.Persistence.Orchestration.AuthorityRunOrchestrator`** with mocks (commit vs rollback). Includes **53R cutover** tests: `JsonFallbackPolicyTests`, `FallbackPolicyDiagnosticsTests`, `CutoverReadinessReportTests` (unit); `PolicyModeFallbackSqlIntegrationTests`, `CutoverReadinessSqlIntegrationTests` (SQL integration). |

## API routes ↔ primary automated tests (319R)

Many flows are covered by **scenario-named** integration tests under **`ArchiForge.Api.Tests`**, not by a `*ControllerTests` class per MVC controller. Use this map when tracing a route to tests (representative examples; search the test project for the route segment or DTO name when unsure).

| Area / route prefix (typical) | Primary test classes (Api.Tests) |
|------------------------------|-----------------------------------|
| **`/v1/architecture/*`** (runs, commit, replay, determinism, traces) | **`ArchitectureRunDetailsTests`**, **`ArchitectureReplayTests`**, **`ArchitectureDeterminismTests`**, **`ArchitectureTraceTests`**, **`ArchitectureCommitConflictTests`**, **`ArchitectureControllerTests`** |
| **Comparisons & replay export** | **`ArchitectureComparisonReplayTests`**, **`ArchitectureEndToEndComparisonTests`**, **`ComparisonReplayVerifyDriftIntegrationTests`**, **`BatchReplayIntegrationTests`** |
| **Governance** | **`GovernanceControllerTests`**, **`GovernancePreviewControllerTests`**, **`GovernanceWorkflowServiceTests`** |
| **Policy packs** | **`PolicyPacksIntegrationTests`**, **`PolicyPacksAppServiceTests`** |
| **Manifests / diagrams / summaries** | **`ManifestSummaryServiceTests`**, **`ManifestDiagramServiceTests`**, **`ArchitectureDiagramTests`**, **`ArchitectureSummaryTests`** |
| **Exports & analysis reports** | **`ArchitectureAnalysisReportTests`**, **`ArchitectureAnalysisExportTests`**, **`ArchitectureExportAuditTests`** |
| **Configuration & startup** | **`ArchiForgeConfigurationRulesTests`**, **`StartupConfigurationFactsReaderTests`**, **`OpenApiContractSnapshotTests`** |
| **Alerts, advisory, retrieval** (when not using dedicated factories) | Search **`Alert*`**, **`Advisory*`**, **`RetrievalQuerySmokeIntegrationTests`**, **`AskThreadIntegrationTests`** |

**Persistence / Application parity:** coordinator Data contracts and **`AuthorityRunOrchestrator`** behavior are also covered in **`ArchiForge.Persistence.Tests`** and **`ArchiForge.Application.Tests`** so logic is testable without **`WebApplicationFactory`**.

## Projects (detail)

- **ArchiForge.Api.Tests** — API integration tests using `WebApplicationFactory` (full app, **SQL Server** per factory via **`ArchiForgeApiFactory`**). Heavier; use for HTTP contracts, comparison replay, exports, run-not-found, 422/409. Advisory + alerts: **`AlertLifecycleIntegrationTests`**, **`DigestDeliveryLifecycleIntegrationTests`**, **`RetrievalQuerySmokeIntegrationTests`**, **`AskThreadIntegrationTests`** with **`AlertLifecycleWebAppFactory`**. Resilience and unit-style classes: see source tree; many use **`[Trait("Category", "Unit")]`** or **`Integration`**.
- **ArchiForge.DecisionEngine.Tests** — Unit and scenario tests; optional integration with real JSON schemas (`SchemaValidationIntegrationTests`).
- **ArchiForge.ContextIngestion.Tests** — Fast unit tests for ingestion parsers, deduplication, connectors, **`ContextIngestionService`**.
- **ArchiForge.Coordinator.Tests**, **ArchiForge.AgentRuntime.Tests**, **ArchiForge.Decisioning.Tests**, **ArchiForge.Retrieval.Tests**, etc. — Domain/component tests unless marked integration.
- **ArchiForge.Persistence.Tests** — SQL integration and contract tests (`Contracts/`); **`Category=SqlServerContainer`** for Dapper against SQL Server. Unit **`AuthorityRunOrchestratorTests`** and InMemory contract subclasses run under **`Category=Unit`** / **`Suite=Core`**.
- **ArchiForge.Application.Tests** — **`ArchitectureRunService`**, **`ReplayRunService`**, **`DeterminismCheckService`**, idempotency hashing; **`Suite=Core`** on classes.

## Class-level traits (authors)

Use **`[Trait("Suite", "Core")]`** only when a class is intentionally part of the corset. Also set **`[Trait("Category", "Unit")]`**, **`Integration`**, **`SqlServerContainer`**, or **`Slow`** so fast core / integration / SQL filters stay accurate.

A class name may include *Integration* while **`Category=Unit`** when it does not use **`WebApplicationFactory`** / HTTP (e.g. in-process service wiring). Traits govern filters, not filenames.

**Core + slow or integration:** A class may be `Suite=Core` and also `Category=Slow` or `Category=Integration` — it runs in full Core and full regression, but **drops out of fast core**.

## Categories (extra filters)

**Integration** (typical pattern in Api.Tests):

```csharp
[Trait("Category", "Integration")]
```

**SQL Server container** (Persistence):

```csharp
[Trait("Category", "SqlServerContainer")]
```

**Unit-only slice:**

```bash
dotnet test ArchiForge.sln --filter "Category=Unit"
```

**Exclude integration:**

```bash
dotnet test ArchiForge.sln --filter "Category!=Integration"
```

## Fixtures and shared setup

- **IntegrationTestBase** (Api.Tests) — `HttpClient`, `JsonOptions`, `JsonContent` via **`ArchiForgeApiFactory`**.
- **ComparisonReplayTestFixture** (Api.Tests) — comparison-replay flow helpers.

**Unit-style tests in Api.Tests** that do not extend **IntegrationTestBase** are often tagged **`Category=Unit`**.

No separate unit-only test project; filter by category as above.
