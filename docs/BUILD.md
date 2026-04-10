# Build & project hygiene

> **Product naming:** Documentation refers to the product as **ArchLucid**. Phase 7 retired legacy `ArchiForge*` configuration and CLI naming; see `docs/ARCHLUCID_RENAME_CHECKLIST.md` for deferred items (Terraform state, repo path, etc.).

See also [TEST_STRUCTURE.md](TEST_STRUCTURE.md) for test categories and filtering, **[TEST_EXECUTION_MODEL.md](TEST_EXECUTION_MODEL.md)** (54R) for Core / Fast core / Integration / SQL / Full regression scripts and CI alignment, and **[RELEASE_LOCAL.md](RELEASE_LOCAL.md)** (56R) for `build-release` / `package-release` / `run-readiness-check`.

**API controllers:** Keep all MVC controllers under **`ArchLucid.Api/Controllers/`** (single folder). On Windows, tools may show the same path with `\` or `/`; on Linux, Git is case-sensitive—do not introduce a second `Controllers` directory that differs only by casing or path style, or you risk duplicate types and confusing diffs.

**RunComparisonController** intentionally depends on three application services (`IEndToEndReplayComparisonService`, `IEndToEndReplayComparisonSummaryFormatter`, `IEndToEndReplayComparisonExportService`) rather than a single facade, for clarity and testability.

## Compiler quality (warnings as errors)

Root **`Directory.Build.props`** enables:

| Property | Purpose |
|----------|---------|
| **`TreatWarningsAsErrors`** | **All warnings are errors** — the build fails on any C# compiler or analyzer warning. |
| **`AnalysisLevel`** | `latest` — use the current Roslyn analyzer baseline shipped with the SDK. |
| **`EnforceCodeStyleInBuild`** | IDE-style analyzers (`.editorconfig`) run during **`dotnet build`**, not only in the editor. |

**Suppressions:** Do **not** disable warnings globally in `Directory.Build.props`. If a warning is **known-acceptable** for a single project only (e.g. NSwag-generated code, a constrained test utility), add a `NoWarn` property to that project’s **`.csproj`** (e.g. `NoWarn` with `$(NoWarn)` plus specific `CA`/`CS` codes) and **document the reason** in an XML comment beside it (or extend this section). **`ArchLucid.Api.Client`** already suppresses **1591** (missing XML documentation) for generated HTTP client types; add further codes there **only** when they come from **`Generated/`** output after OpenAPI/NSwag changes.

Verify locally with a Release build (matches typical CI compile strictness):

```bash
dotnet build ArchLucid.sln -c Release
```

## Full solution

```bash
dotnet restore
dotnet build
dotnet test
```

**Dev Container:** Optional VS Code / Cursor setup lives in **`.devcontainer/`** — .NET 10 + Node 22; start **`docker compose up -d`** on the host for SQL, Azurite, and Redis (see **`docs/DEVCONTAINER.md`**).

**Finding-engine template:** Install the local template with **`dotnet new install ./templates/archlucid-finding-engine`**, then **`dotnet new archlucid-finding-engine -n MyFindingEngine`** (class library + xUnit tests).

**CI / supply chain:** GitHub Actions workflow **`.github/workflows/ci.yml`** runs **`dotnet list package --vulnerable --include-transitive`** so known-vulnerable NuGet packages fail the pipeline (see **`NEXT_REFACTORINGS.md`** item **220**). Run the same command locally after dependency changes. The workflow uses **tiered jobs** (fast .NET core, then full .NET regression with SQL, plus **Vitest** and **Playwright** for `archlucid-ui`); see **`TEST_EXECUTION_MODEL.md`**.

**Secret scanning:** The **`gitleaks`** job scans the full Git history with **`gitleaks/gitleaks-action`** and **`.gitleaks.toml`** (extends default rules; allowlists only the two documented dev/CI SQL passwords that appear verbatim in-repo). To run locally: install [gitleaks](https://github.com/gitleaks/gitleaks) and run **`gitleaks detect --source . --verbose`** from the repo root.

**SBOM (CycloneDX):** CI uploads **`sbom-dotnet`** (JSON for **`ArchLucid.Api/ArchLucid.Api.csproj`**, matching the API container surface) and **`sbom-npm`** (JSON for **`archlucid-ui`**). Regenerate locally:

```bash
dotnet tool install CycloneDX --tool-path ./.tools-cdx
./.tools-cdx/dotnet-cyclonedx ArchLucid.Api/ArchLucid.Api.csproj -o sbom-dotnet.json
# On Windows the shim may be dotnet-CycloneDX.exe instead of dotnet-cyclonedx.

cd archlucid-ui && npx @cyclonedx/cyclonedx-npm@4.2.1 --output-file sbom-npm.json --ignore-npm-errors
```

Add **`.tools-cdx/`** (or your chosen tool path) to your local ignore habits; do not commit generated BOMs unless your release process requires it.

## OpenTelemetry metrics (`ArchLucid` meter)

The API registers meter **`ArchLucid`** (`ArchLucidInstrumentation.MeterName`). Notable series:

| Metric | Notes |
|--------|--------|
| `digest_delivery_succeeded` / `digest_delivery_failed` | Tag **`channel`**. |
| `alert_evaluation_duration_ms` | Tag **`rule_kind`**: `simple` \| `composite`. |
| `governance_resolve_duration_ms` | End-to-end **`EffectiveGovernanceResolver.ResolveAsync`** latency. |
| `governance_pack_content_deserialize_cache_hits` / `_misses` | In-resolve dedupe when the same pack **version** appears on multiple assignments (not HTTP-scope cache — see **`NEXT_REFACTORINGS.md`** §230). |
| `archlucid_llm_prompt_tokens_total` / `archlucid_llm_completion_tokens_total` | Aggregate by default; with **`LlmTelemetry:RecordPerTenantTokens=true`**, also emitted **with** `tenant_id` label (cardinality). |

Enable **`Observability:Prometheus:Enabled`** (and exporters) as needed for scraping. SLO-oriented Grafana: **`infra/grafana/dashboard-archlucid-slo.json`**.

## SQL Server for integration tests (Dapper + API)

There is **no SQLite** test provider: DB-facing tests use **SQL Server** only (`Microsoft.Data.SqlClient`). Pure unit tests stay in-memory / mocked.

Shared resolution lives in **`ArchLucid.TestSupport`** (`SqlServerIntegrationTestConnections`, `SqlServerTestCatalogCommands`, `TestDatabaseEnvironment`).

### Persistence tests (`ArchLucid.Persistence.Tests`)

1. Set **`ARCHLUCID_SQL_TEST`** to a full ADO.NET connection string (including **`Initial Catalog`**), **or**
2. On **Windows**, omit it and use **LocalDB** (`(localdb)\mssqllocaldb`, catalog **`ArchLucidPersistenceTests`**) when LocalDB is installed.

**CI:** The **`dotnet-full-regression`** job in **`.github/workflows/ci.yml`** sets **`ARCHLUCID_SQL_TEST`** against the **SQL Server 2022** service container (the **`dotnet-fast-core`** job does not start SQL). The **`dotnet-fast-core`** job **depends on** the Terraform **`terraform-validate-private`** and **`terraform-validate-public-stacks`** jobs so invalid IaC fails before the .NET corset runs.

### Application layer unit tests (`ArchLucid.Application.Tests`)

- **`Suite=Core`** / **`Category=Unit`**: hashing/idempotency helpers and **`ArchitectureRunService`** idempotency paths (replay vs **`ConflictException`**) using mocked coordinators and repositories—no SQL required.

### API integration tests (`ArchLucid.Api.Tests`)

**One-command local SQL regression (Docker):** from repo root run **`scripts/run-full-regression-docker-sql.ps1`** (Windows) or **`scripts/run-full-regression-docker-sql.sh`** (Linux/macOS). These start **`docker compose`** `sqlserver`, set **`ARCHLUCID_SQL_TEST`** to match **`docker-compose.yml`** credentials, then run **`dotnet test ArchLucid.sln`**.

**`ArchLucidApiFactory`** creates an ephemeral database per factory (`ArchLucidTest_*` or `ArchLucidAlertTest_*`) on the **same SQL Server instance** you configure:

| Priority | Variable | Value |
|----------|----------|--------|
| 1 | **`ARCHLUCID_API_TEST_SQL`** | Connection string for server + auth; **`Initial Catalog`** is replaced per factory. |
| 2 | **`ARCHLUCID_SQL_TEST`** | Reuses server and credentials; catalog name is replaced per factory (works well with CI’s single container). |
| 3 | *(Windows only)* | **`localhost`** + integrated security if neither variable is set. |
| — | Linux / macOS | **Must** set **`ARCHLUCID_SQL_TEST`** or **`ARCHLUCID_API_TEST_SQL`** (e.g. Docker SQL Server on `127.0.0.1,1433`). |

**Docker (local):** run SQL Server 2022 (or Azure SQL edge) and point **`ARCHLUCID_SQL_TEST`** at it; API tests will piggyback the same server for ephemeral DBs.

Keep **one DDL source of truth** (`ArchLucid.Persistence/Scripts/ArchLucid.sql` + **`ArchLucid.Persistence/Migrations/*.sql`**) and let **`DatabaseMigrator`** apply embedded migrations to each test database.

## Central Package Management (CPM)

Versions live in **`Directory.Packages.props`**. Project **`.csproj`** files use `<PackageReference Include="Id" />` without `Version=`.

After adding a new package anywhere, add a matching **`<PackageVersion>`** in **`Directory.Packages.props`** or restore will fail.

## Project reference audit (post-refactor checklist)

When you add code in project **A** that uses types from assembly **B**, **A** usually needs an explicit **`<ProjectReference Include="..\B\B.csproj" />`**. Transitive references do **not** expose **B**’s public API to **A**’s compiler.

**Policy:** Test projects must declare every assembly they reference in `using` (add an explicit **ProjectReference**). Do not rely on transitive references for compilation.

Typical gaps we fixed:

| Consumer | Often needs explicit reference to |
|----------|-----------------------------------|
| **ArchLucid.Decisioning** | **KnowledgeGraph** |
| **ArchLucid.AgentRuntime** | **AgentSimulator** |
| **ArchLucid.AgentRuntime.Tests** | **Coordinator**, **ContextIngestion**, **Decisioning**, **KnowledgeGraph** |
| **ArchLucid.Coordinator.Tests** | **AgentSimulator**, **ContextIngestion**, **Decisioning**, **KnowledgeGraph** |
| **ArchLucid.ContextIngestion.Tests** | **ContextIngestion**, **Contracts** (mapper tests) |
| **ArchLucid.Decisioning.Tests** | **Contracts** (for `ManifestDeltaProposal`, `AgentResult`, etc.) |

**Quick audit:** search new `using ArchLucid.*` in a project; if the namespace’s assembly is not referenced by that **.csproj**, add the project reference.

## Decisioning — JSON Schema / options dependency bundle

**ArchLucid.Decisioning** references this set for `SchemaValidationService` and options binding (see **`ArchLucid.Decisioning.csproj`**); versions are pinned only in **`Directory.Packages.props`**:

- **JsonSchema.Net** — runtime JSON Schema validation  
- **Microsoft.Extensions.Configuration** — `IConfiguration` overload for `AddSchemaValidation`  
- **Microsoft.Extensions.DependencyInjection.Abstractions** — `IServiceCollection` extensions  
- **Microsoft.Extensions.Logging** — `SchemaValidationService`  
- **Microsoft.Extensions.Options** + **Microsoft.Extensions.Options.ConfigurationExtensions** — options binding  

Keep these aligned on the same **10.x** line as **`Directory.Packages.props`**.

## Packages that may be unavailable on restricted feeds

If restore fails for **Microsoft.Extensions.Configuration.Memory** (or similar), prefer tests that **mock `IConfiguration`** or use **in-repo test doubles** instead of optional satellite packages.
