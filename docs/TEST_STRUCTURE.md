# Test structure (Change Set 54R)

Operator cheat sheet for **ArchLucid** .NET tests: **what each tier means** and **how to run it**. CI job names and full narrative: **[TEST_EXECUTION_MODEL.md](TEST_EXECUTION_MODEL.md)**. SQL variables and LocalDB: **[BUILD.md](BUILD.md)**.

## 54R tiers

| Tier | Meaning | Filter |
|------|---------|--------|
| **Core (corset)** | Curated high-value regression; opt-in per **class** with `[Trait("Suite", "Core")]` | `Suite=Core` |
| **Fast core** | Core tests that are **not** `Category=Slow` and **not** `Category=Integration` (typical CI first gate) | `Suite=Core&Category!=Slow&Category!=Integration` |
| **Integration** | Real API host (`WebApplicationFactory`), HTTP-level behavior | `Category=Integration` |
| **Slow** | Long-running or heavy; excluded from fast core | `Category=Slow` |
| **Full regression** | Whole solution, all traits (unless skipped in test code) | *(no filter)* |

### Property-based tests (FsCheck)

**`ArchLucid.Decisioning.Tests`**, **`ArchLucid.Application.Tests`**, and **`ArchLucid.Contracts.Tests`** reference **FsCheck** + **FsCheck.Xunit**. Classes use **`[Property]`** for invariant checks (for example **`ExplainabilityTraceCompletenessAnalyzerPropertyTests`**). They usually carry **`[Trait("Suite", "Core")]`** and run under the same **`dotnet test`** filters as other unit tests.

### Run each (.NET, repo root)

```bash
dotnet test ArchLucid.sln --filter "Suite=Core"
```

```bash
dotnet test ArchLucid.sln --filter "Suite=Core&Category!=Slow&Category!=Integration"
```

```bash
dotnet test ArchLucid.sln --filter "Category=Integration"
```

```bash
dotnet test ArchLucid.sln --filter "Category=Slow"
```

```bash
dotnet test ArchLucid.sln
```

**Configuration:** GitHub Actions .NET jobs use **`-c Release`**. Repo-root `test-*.cmd` / `.ps1` call `dotnet test` **without** `-c` (typically **Debug**). To mirror CI: `dotnet test ArchLucid.sln -c Release`.

**Windows (same filters):** `test-core.cmd`, `test-fast-core.cmd`, `test-integration.cmd`, `test-slow.cmd`, `test-full.cmd` (and `.ps1` where present).

### Release candidate packaging (56R)

**Doc:** [RELEASE_LOCAL.md](RELEASE_LOCAL.md). **Scripts:** `build-release`, `package-release`, `run-readiness-check` (`.cmd` / `.ps1` at repo root). `run-readiness-check` runs a **Release** build, **fast core** tests with `-c Release`, then **Vitest** when Node is on `PATH`. **Pilot onboarding:** [PILOT_GUIDE.md](PILOT_GUIDE.md), [OPERATOR_QUICKSTART.md](OPERATOR_QUICKSTART.md). **E2E release smoke:** [RELEASE_SMOKE.md](RELEASE_SMOKE.md) (`release-smoke.cmd`).

### SQL Server–first (Persistence Dapper)

Persistence integration against a **real** SQL Server (Dapper + DbUp migrations; **no EF**):

```bash
dotnet test ArchLucid.Persistence.Tests --filter "Category=SqlServerContainer"
```

**Script:** `test-sqlserver-integration.cmd` / `.ps1`

### Greenfield SQL boot (empty catalog + API host)

**Why:** Integration tests that use a **pre-migrated** database never exercise **`ArchLucidPersistenceStartup`** on an **empty** catalog — ordering bugs between **DbUp** and **`SqlSchemaBootstrapper`** can slip through until deploy or live CI.

**What:**

- **`ArchLucid.Api.Tests`**: **`GreenfieldSqlApiFactory`** + **`GreenfieldSqlBootIntegrationTests`** (`Suite=Core`, `Category=Integration`). Each test creates an empty database, boots the real API with **`StorageProvider=Sql`**, asserts **`/health/ready`**, **`dbo.SchemaVersions`**, and core columns.
- **CI**: job **`api-greenfield-boot`** in **`.github/workflows/ci.yml`** (Tier **1.5**) runs the API process against **`ArchLucidGreenfieldCi`** and asserts the DbUp journal.

**Local (requires SQL per [BUILD.md](BUILD.md)):**

```bash
dotnet test ArchLucid.Api.Tests --filter "FullyQualifiedName~GreenfieldSqlBoot" -c Release --settings coverage.runsettings
```

### Operator UI (`archlucid-ui/`)

**Vitest** (jsdom, fast — minimal harness):

```bash
cd archlucid-ui
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

**Live API E2E (Tier 2+ — CI `ui-e2e-live`, merge-blocking):** Playwright config `archlucid-ui/playwright.live.config.ts` runs **`e2e/live-api-*.spec.ts`**: **`live-api-journey`** (happy path), **`live-api-conflict-journey`** (200 idempotent repeat-commit, 404 missing run, audit stability), **`live-api-governance-rejection`** (reject flow + audit + UI), **`live-api-negative-paths`** (self-approval block + 404 run detail + empty create body), **`live-api-error-states`** (fake run detail, runs list, audit no-results, governance dashboard), **`live-api-advisory-flow`** (advisory scan scheduling after committed run + audit trail), **`live-api-replay-export`** (replay committed run + re-export ZIP + audit trail), **`live-api-analysis-report`** (analysis report generation + optional DOCX export + audit trail), **`live-api-policy-pack-lifecycle`** (create/assign policy pack, effective set, UI `/policy-packs`, audit **`PolicyPackCreated`**), **`live-api-compare-runs`** (two committed runs, **`GET /v1/authority/compare/runs`**, UI `/compare` query params, 404 on missing peer run). Default **`playwright.config.ts`** uses **`testIgnore: "**/live-api-*.spec.ts"`** so mock smoke (`npx playwright test`, CI **Install dependencies & run Playwright smoke**) never calls the real API (avoids **`ECONNREFUSED`** on port 5128). The happy-path spec exercises a real `ArchLucid.Api` + SQL stack (`DevelopmentBypass`, **`AgentExecution:Mode=Simulator`**): health, run create → execute → **commit** → operator **manifest** review (artifacts + bundle link) → **`GET /v1/artifacts/runs/{runId}/export`** ZIP → governance **submit + approve** (segregation: reviewer ≠ submitter) → durable **audit** event checks (`RunStarted`, `ManifestGenerated`, `GovernanceApprovalSubmitted`, `GovernanceApprovalApproved`, `RunExported`) → **`/audit`** UI search → governance workflow **Load** for the run. Helpers: `e2e/helpers/live-api-client.ts`. Narrative: **[LIVE_E2E_HAPPY_PATH.md](LIVE_E2E_HAPPY_PATH.md)**. Run locally with `npx playwright test -c playwright.live.config.ts` (starts the UI via `webServer`; you must run **`ArchLucid.Api`** on **`LIVE_API_URL`** / default `http://127.0.0.1:5128` first). CI builds Next before starting the API and sets **`LIVE_E2E_SKIP_NEXT_BUILD=1`** so the webServer does not run a second `npm run build` while the API is up (avoids OOM). See **[TEST_EXECUTION_MODEL.md](TEST_EXECUTION_MODEL.md)** for the CI job.

**API integration (`Suite=Core`):** HTTP tests for **`/v1/learning/*`**, **`/v1/product-learning/*`**, **`/v1/evolution/*`**, and related classes (e.g. **`LearningControllerTests`**, **`ProductLearningControllerTests`**, **`EvolutionControllerQueryTests`**, **`EvolutionControllerFlowTests`**, **`AskThreadIntegrationTests`**) carry **`[Trait("Suite", "Core")]`** so they are included in the **`Suite=Core`** .NET filter (they remain **`Category=Integration`**, so they still **exclude** from **fast core**).

**Scripts (repo root):** `test-ui-unit.cmd` / `.ps1` (Vitest), `test-ui-smoke.cmd` / `.ps1` (Playwright)

**Change Set 55R — focused review-workflow smoke:** component + mocked API contract tests under `archlucid-ui/src/review-workflow/`, `ShellNav`, artifact/compare helpers. Command list: [archlucid-ui/docs/TESTING_AND_TROUBLESHOOTING.md](../archlucid-ui/docs/TESTING_AND_TROUBLESHOOTING.md#3-55r--review-workflow-smoke-tests-change-set-55r). Operator context: [docs/operator-shell.md](operator-shell.md).

### Stryker.NET mutation configs (repo root)

Scheduled CI (`.github/workflows/stryker-scheduled.yml`) runs Stryker per config with **`-s ArchLucid.sln`**, reporters **`progress` / `html` / `json`**, then **`scripts/ci/assert_stryker_score_vs_baseline.py`** vs **`scripts/ci/stryker-baselines.json`** (**mutation score baselines 65.0** per module; assert tolerance **0.10** pp). Narrative and baseline refresh: **[MUTATION_TESTING_STRYKER.md](MUTATION_TESTING_STRYKER.md)**. All listed configs use thresholds **high 80 / low 70 / break 70** (`low` must be ≥ `break` per Stryker.NET; baseline regression in **`stryker-baselines.json`** is a separate script gate).

| Config file | Code project | Test project |
|-------------|--------------|--------------|
| `stryker-config.json` | `ArchLucid.Persistence` | `ArchLucid.Persistence.Tests` |
| `stryker-config.application.json` | `ArchLucid.Application` | `ArchLucid.Application.Tests` |
| `stryker-config.agentruntime.json` | `ArchLucid.AgentRuntime` | `ArchLucid.AgentRuntime.Tests` |
| `stryker-config.coordinator.json` | `ArchLucid.Coordinator` | `ArchLucid.Coordinator.Tests` |
| `stryker-config.decisioning.json` | `ArchLucid.Decisioning` | `ArchLucid.Decisioning.Tests` |

### k6 performance smoke (Tier 2c — CI automated)

Two k6 scripts run in CI via `.github/workflows/ci.yml`:

| CI job | Script | Scenarios | Write paths | Blocking |
|--------|--------|-----------|-------------|----------|
| `k6-smoke-api` | `tests/load/smoke.js` | health, runs list, version, audit search | No (read-only) | Yes (merge-blocking) |
| `k6-ci-smoke` | `tests/load/ci-smoke.js` | health, create run, list runs, audit search | **Yes** (`POST /v1/architecture/request`) | **Yes** — `assert_k6_ci_smoke_summary.py` (p95 ≤ 3000 ms, failed rate ≤ 2%) |
| `k6-soak-scheduled` | `tests/load/soak.js` | longer low-rate read-only mix | No | No (`continue-on-error`; needs secret **`ARCHLUCID_SOAK_BASE_URL`**) |

Both jobs start the API against a SQL Server service container with `DevelopmentBypass` auth and `Simulator` agent execution mode. k6 runs via the `grafana/k6:latest` Docker image (not installed as a system binary).

**Local (read + write smoke):**

```bash
BASE_URL=http://127.0.0.1:5128 k6 run tests/load/ci-smoke.js --summary-export /tmp/k6-ci-summary.json
```

**Manual full load (Compose full-stack):** `scripts/load/hotpaths.js` via `.github/workflows/load-test.yml` or `scripts/load/record_baseline.ps1` / `.sh`. See **[LOAD_TEST_BASELINE.md](LOAD_TEST_BASELINE.md)**.

### Scheduled security (Tier 4 — not xUnit)

These jobs live in **separate workflows** (weekly + manual dispatch). They do **not** use `dotnet test` traits.

| Workflow file | Tool | Target | When |
|----------------|------|--------|------|
| [`zap-baseline-strict-scheduled.yml`](../.github/workflows/zap-baseline-strict-scheduled.yml) | OWASP ZAP baseline | API HTTP surface (container) | Monday 06:00 UTC + `workflow_dispatch` |
| [`schemathesis-scheduled.yml`](../.github/workflows/schemathesis-scheduled.yml) | Schemathesis (OpenAPI-driven fuzz) | `GET /openapi/v1.json` + exercised routes | Same |

**Schemathesis narrative and local commands:** [API_FUZZ_TESTING.md](API_FUZZ_TESTING.md). **ZAP rules:** [security/ZAP_BASELINE_RULES.md](security/ZAP_BASELINE_RULES.md). **CI tier context:** [TEST_EXECUTION_MODEL.md](TEST_EXECUTION_MODEL.md) (Tier 4).

---

## SQL Server for API + Persistence tests

- **No SQLite.** Use **SQL Server** for anything that hits the DB; tests use **Dapper** and **DbUp** (`ArchLucid.Persistence/Migrations/`).
- **API integration** (`ArchLucid.Api.Tests`): factories create ephemeral databases on the configured instance — set **`ARCHLUCID_SQL_TEST`** or **`ARCHLUCID_API_TEST_SQL`** on Linux/macOS/CI; Windows may use **localhost** / LocalDB if unset (see **BUILD.md**).
- **CI** sets `ARCHLUCID_SQL_TEST` against the SQL Server service container for the full regression job.

**Example (bash):**

```bash
export ARCHLUCID_SQL_TEST='Server=127.0.0.1,1433;User Id=sa;Password=YourPassword;TrustServerCertificate=True;Initial Catalog=ArchLucidPersistenceTests'
dotnet test ArchLucid.sln
```

**PowerShell:**

```powershell
$env:ARCHLUCID_SQL_TEST = 'Server=127.0.0.1,1433;User Id=sa;Password=YourPassword;TrustServerCertificate=True;Initial Catalog=ArchLucidPersistenceTests'
dotnet test ArchLucid.sln
```

Skip SQL-container or integration slices when needed:

```bash
dotnet test ArchLucid.sln --filter "Category!=SqlServerContainer"
```

```bash
dotnet test ArchLucid.sln --filter "Category!=Integration&Category!=SqlServerContainer"
```

---

## Bounded context map (quick reference)

| Test project | Primary bounded contexts / seams exercised |
|--------------|---------------------------------------------|
| **ArchLucid.Api.Tests** | HTTP API, auth policies, ProblemDetails, CORS, comparison replay, runs, governance, alerts (integration), SQL Server–backed persistence through the real host (**DbUp** on startup). |
| **ArchLucid.Decisioning.Tests** | Findings, compliance, alerts (pure logic), advisory scheduling math, governance resolution, graph mappers, JSON persistence contracts. |
| **ArchLucid.ContextIngestion.Tests** | Connectors, parsers, deduplication, `ContextIngestionService`, delta summaries. |
| **ArchLucid.Coordinator.Tests** | Run coordination, agent fakes, **`ContextIngestionRequestMapperTests`**, **`DocxExportServiceGoldenTests`** (OpenXML anchors). |
| **ArchLucid.Application.Tests** | **`ArchitectureRunService`** execute/commit and idempotency, **`ReplayRunService`**, **`DeterminismCheckService`**, hashing helpers — Application-layer orchestration with mocked Data/Coordinator ports (**`Suite=Core`** on classes). |
| **ArchLucid.Decisioning.Tests** (`Merge/`, `Validation/`) | Schema validation, manifest merge, decision-engine v2 scenarios. |
| **ArchLucid.KnowledgeGraph.Tests** | Graph models, edge inference contracts. |
| **ArchLucid.Retrieval.Tests** | `RetrievalQueryService`, `InMemoryVectorIndex` (empty index, ranking, scope filters), **`CircuitBreakerGateTests`**, **`CircuitBreakingOpenAiEmbeddingClientTests`** (OpenAI embedding circuit breaker). |
| **ArchLucid.Persistence.Tests** | Dapper repositories against **real SQL Server** via **`ARCHLUCID_SQL_TEST`** or Windows **LocalDB**; schema from **`DatabaseMigrator`** (same DbUp migrations as production SQL Server). **`Contracts/`** abstract bases with **InMemory** + **Dapper** implementations (agent evaluations, decision nodes, coordinator manifest/trace, run exports, architecture runs, etc.). **`AuthorityRunOrchestratorTests`** exercise **`ArchLucid.Persistence.Orchestration.AuthorityRunOrchestrator`** with mocks (commit vs rollback). **`AuthorityPipelineStagesExecutorTests`** assert authority pipeline stage span parenting, `archlucid.stage.name` tags, histogram **`archlucid_authority_pipeline_stage_duration_ms`**, and error propagation (`Suite=Core`). **53R / relational cutover:** `CutoverReadinessReportTests` (unit), `CutoverReadinessSqlIntegrationTests` (SQL). Relational read behavior is covered by repository SQL integration tests (e.g. `SqlGraphSnapshotRepositorySqlIntegrationTests` for edge JSON merge). |

## API routes ↔ primary automated tests (319R)

Many flows are covered by **scenario-named** integration tests under **`ArchLucid.Api.Tests`**, not by a `*ControllerTests` class per MVC controller. Use this map when tracing a route to tests (representative examples; search the test project for the route segment or DTO name when unsure).

| Area / route prefix (typical) | Primary test classes (Api.Tests) |
|------------------------------|-----------------------------------|
| **`/v1/architecture/*`** (runs, commit, replay, determinism, traces) | **`ArchitectureRunDetailsTests`**, **`ArchitectureReplayTests`**, **`ArchitectureDeterminismTests`**, **`ArchitectureTraceTests`**, **`ArchitectureCommitConflictTests`**, **`ArchitectureControllerTests`** |
| **Comparisons & replay export** | **`ArchitectureComparisonReplayTests`**, **`ArchitectureEndToEndComparisonTests`**, **`ComparisonReplayVerifyDriftIntegrationTests`**, **`BatchReplayIntegrationTests`** |
| **Governance** | **`GovernanceControllerTests`**, **`GovernancePreviewControllerTests`**, **`GovernanceWorkflowServiceTests`** |
| **Policy packs** | **`PolicyPacksIntegrationTests`**, **`PolicyPacksAppServiceTests`** |
| **Manifests / diagrams / summaries** | **`ManifestSummaryServiceTests`**, **`ManifestDiagramServiceTests`**, **`ArchitectureDiagramTests`**, **`ArchitectureSummaryTests`** |
| **Exports & analysis reports** | **`ArchitectureAnalysisReportTests`**, **`ArchitectureAnalysisExportTests`**, **`ArchitectureExportAuditTests`** |
| **Configuration & startup** | **`ArchLucidConfigurationRulesTests`**, **`StartupConfigurationFactsReaderTests`**, **`OpenApiContractSnapshotTests`** |
| **Alerts, advisory, retrieval** (when not using dedicated factories) | Search **`Alert*`**, **`Advisory*`**, **`RetrievalQuerySmokeIntegrationTests`**, **`AskThreadIntegrationTests`** |

**Persistence / Application parity:** coordinator Data contracts and **`AuthorityRunOrchestrator`** behavior are also covered in **`ArchLucid.Persistence.Tests`** and **`ArchLucid.Application.Tests`** so logic is testable without **`WebApplicationFactory`**.

## Projects (detail)

- **ArchLucid.Api.Tests** — API integration tests using `WebApplicationFactory` (full app, **SQL Server** per factory via **`ArchLucidApiFactory`**). Heavier; use for HTTP contracts, comparison replay, exports, run-not-found, 422/409. Advisory + alerts: **`AlertLifecycleIntegrationTests`**, **`DigestDeliveryLifecycleIntegrationTests`**, **`RetrievalQuerySmokeIntegrationTests`**, **`AskThreadIntegrationTests`** with **`AlertLifecycleWebAppFactory`**. Resilience and unit-style classes: see source tree; many use **`[Trait("Category", "Unit")]`** or **`Integration`**.
- **ArchLucid.Decisioning.Tests** — Under `Validation/` and `Merge/`; unit and scenario tests; optional integration with real JSON schemas (`SchemaValidationIntegrationTests`).
- **ArchLucid.ContextIngestion.Tests** — Fast unit tests for ingestion parsers, deduplication, connectors, **`ContextIngestionService`**.
- **ArchLucid.Coordinator.Tests**, **ArchLucid.AgentRuntime.Tests**, **ArchLucid.Decisioning.Tests**, **ArchLucid.Retrieval.Tests**, etc. — Domain/component tests unless marked integration.
- **ArchLucid.Persistence.Tests** — SQL integration and contract tests (`Contracts/`); **`Category=SqlServerContainer`** for Dapper against SQL Server. Unit **`AuthorityRunOrchestratorTests`** and InMemory contract subclasses run under **`Category=Unit`** / **`Suite=Core`**.
- **ArchLucid.Application.Tests** — **`ArchitectureRunService`**, **`ReplayRunService`**, **`DeterminismCheckService`**, idempotency hashing; **`Suite=Core`** on classes.

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
dotnet test ArchLucid.sln --filter "Category=Unit"
```

**Exclude integration:**

```bash
dotnet test ArchLucid.sln --filter "Category!=Integration"
```

## Fixtures and shared setup

- **IntegrationTestBase** (Api.Tests) — `HttpClient`, `JsonOptions`, `JsonContent` via **`ArchLucidApiFactory`**.
- **ComparisonReplayTestFixture** (Api.Tests) — comparison-replay flow helpers.

**Unit-style tests in Api.Tests** that do not extend **IntegrationTestBase** are often tagged **`Category=Unit`**.

No separate unit-only test project; filter by category as above.
