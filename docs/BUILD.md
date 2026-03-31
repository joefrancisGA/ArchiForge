# Build & project hygiene

See also [TEST_STRUCTURE.md](TEST_STRUCTURE.md) for test categories and filtering, **[TEST_EXECUTION_MODEL.md](TEST_EXECUTION_MODEL.md)** (54R) for Core / Fast core / Integration / SQL / Full regression scripts and CI alignment, and **[RELEASE_LOCAL.md](RELEASE_LOCAL.md)** (56R) for `build-release` / `package-release` / `run-readiness-check`.

**API controllers:** Keep all MVC controllers under **`ArchiForge.Api/Controllers/`** (single folder). On Windows, tools may show the same path with `\` or `/`; on Linux, Git is case-sensitive—do not introduce a second `Controllers` directory that differs only by casing or path style, or you risk duplicate types and confusing diffs.

**RunComparisonController** intentionally depends on three application services (`IEndToEndReplayComparisonService`, `IEndToEndReplayComparisonSummaryFormatter`, `IEndToEndReplayComparisonExportService`) rather than a single facade, for clarity and testability.

## Full solution

```bash
dotnet restore
dotnet build
dotnet test
```

**CI / supply chain:** GitHub Actions workflow **`.github/workflows/ci.yml`** runs **`dotnet list package --vulnerable --include-transitive`** so known-vulnerable NuGet packages fail the pipeline (see **`NEXT_REFACTORINGS.md`** item **220**). Run the same command locally after dependency changes. The workflow uses **tiered jobs** (fast .NET core, then full .NET regression with SQL, plus **Vitest** and **Playwright** for `archiforge-ui`); see **`TEST_EXECUTION_MODEL.md`**.

**Secret scanning:** The **`gitleaks`** job scans the full Git history with **`gitleaks/gitleaks-action`** and **`.gitleaks.toml`** (extends default rules; allowlists only the two documented dev/CI SQL passwords that appear verbatim in-repo). To run locally: install [gitleaks](https://github.com/gitleaks/gitleaks) and run **`gitleaks detect --source . --verbose`** from the repo root.

**SBOM (CycloneDX):** CI uploads **`sbom-dotnet`** (JSON for **`ArchiForge.Api/ArchiForge.Api.csproj`**, matching the API container surface) and **`sbom-npm`** (JSON for **`archiforge-ui`**). Regenerate locally:

```bash
dotnet tool install CycloneDX --tool-path ./.tools-cdx
./.tools-cdx/dotnet-cyclonedx ArchiForge.Api/ArchiForge.Api.csproj -o sbom-dotnet.json
# On Windows the shim may be dotnet-CycloneDX.exe instead of dotnet-cyclonedx.

cd archiforge-ui && npx @cyclonedx/cyclonedx-npm@4.2.1 --output-file sbom-npm.json --ignore-npm-errors
```

Add **`.tools-cdx/`** (or your chosen tool path) to your local ignore habits; do not commit generated BOMs unless your release process requires it.

## OpenTelemetry metrics (`ArchiForge` meter)

The API registers meter **`ArchiForge`** (`ArchiForgeInstrumentation.MeterName`). Notable series:

| Metric | Notes |
|--------|--------|
| `digest_delivery_succeeded` / `digest_delivery_failed` | Tag **`channel`**. |
| `alert_evaluation_duration_ms` | Tag **`rule_kind`**: `simple` \| `composite`. |
| `governance_resolve_duration_ms` | End-to-end **`EffectiveGovernanceResolver.ResolveAsync`** latency. |
| `governance_pack_content_deserialize_cache_hits` / `_misses` | In-resolve dedupe when the same pack **version** appears on multiple assignments (not HTTP-scope cache — see **`NEXT_REFACTORINGS.md`** §230). |

Enable **`Observability:Prometheus:Enabled`** (and exporters) as needed for scraping.

## SQL Server for integration tests (Dapper + API)

There is **no SQLite** test provider: DB-facing tests use **SQL Server** only (`Microsoft.Data.SqlClient`). Pure unit tests stay in-memory / mocked.

Shared resolution lives in **`ArchiForge.TestSupport`** (`SqlServerIntegrationTestConnections`, `SqlServerTestCatalogCommands`, `TestDatabaseEnvironment`).

### Persistence tests (`ArchiForge.Persistence.Tests`)

1. Set **`ARCHIFORGE_SQL_TEST`** to a full ADO.NET connection string (including **`Initial Catalog`**), **or**
2. On **Windows**, omit it and use **LocalDB** (`(localdb)\mssqllocaldb`, catalog **`ArchiForgePersistenceTests`**) when LocalDB is installed.

**CI:** The **`dotnet-full-regression`** job in **`.github/workflows/ci.yml`** sets **`ARCHIFORGE_SQL_TEST`** against the **SQL Server 2022** service container (the **`dotnet-fast-core`** job does not start SQL).

### API integration tests (`ArchiForge.Api.Tests`)

**`ArchiForgeApiFactory`** creates an ephemeral database per factory (`ArchiForgeTest_*` or `ArchiForgeAlertTest_*`) on the **same SQL Server instance** you configure:

| Priority | Variable | Value |
|----------|----------|--------|
| 1 | **`ARCHIFORGE_API_TEST_SQL`** | Connection string for server + auth; **`Initial Catalog`** is replaced per factory. |
| 2 | **`ARCHIFORGE_SQL_TEST`** | Reuses server and credentials; catalog name is replaced per factory (works well with CI’s single container). |
| 3 | *(Windows only)* | **`localhost`** + integrated security if neither variable is set. |
| — | Linux / macOS | **Must** set **`ARCHIFORGE_SQL_TEST`** or **`ARCHIFORGE_API_TEST_SQL`** (e.g. Docker SQL Server on `127.0.0.1,1433`). |

**Docker (local):** run SQL Server 2022 (or Azure SQL edge) and point **`ARCHIFORGE_SQL_TEST`** at it; API tests will piggyback the same server for ephemeral DBs.

Keep **one DDL source of truth** (`ArchiForge.Data/SQL/ArchiForge.sql` + **`ArchiForge.Data/Migrations/*.sql`**) and let **`DatabaseMigrator`** apply embedded migrations to each test database.

## Central Package Management (CPM)

Versions live in **`Directory.Packages.props`**. Project **`.csproj`** files use `<PackageReference Include="Id" />` without `Version=`.

After adding a new package anywhere, add a matching **`<PackageVersion>`** in **`Directory.Packages.props`** or restore will fail.

## Project reference audit (post-refactor checklist)

When you add code in project **A** that uses types from assembly **B**, **A** usually needs an explicit **`<ProjectReference Include="..\B\B.csproj" />`**. Transitive references do **not** expose **B**’s public API to **A**’s compiler.

**Policy:** Test projects must declare every assembly they reference in `using` (add an explicit **ProjectReference**). Do not rely on transitive references for compilation.

Typical gaps we fixed:

| Consumer | Often needs explicit reference to |
|----------|-----------------------------------|
| **ArchiForge.Decisioning** | **KnowledgeGraph** |
| **ArchiForge.AgentRuntime** | **AgentSimulator** |
| **ArchiForge.AgentRuntime.Tests** | **Coordinator**, **ContextIngestion**, **Decisioning**, **DecisionEngine**, **KnowledgeGraph** |
| **ArchiForge.Coordinator.Tests** | **AgentSimulator**, **ContextIngestion**, **DecisionEngine**, **Decisioning**, **KnowledgeGraph** |
| **ArchiForge.ContextIngestion.Tests** | **ContextIngestion**, **Contracts** (mapper tests) |
| **ArchiForge.DecisionEngine.Tests** | **Contracts** (for `ManifestDeltaProposal`, `AgentResult`, etc.) |

**Quick audit:** search new `using ArchiForge.*` in a project; if the namespace’s assembly is not referenced by that **.csproj**, add the project reference.

## DecisionEngine dependency bundle (single place to align versions)

**ArchiForge.DecisionEngine** intentionally references this set (see **`ArchiForge.DecisionEngine.csproj`**); versions are pinned only in **`Directory.Packages.props`**:

- **JsonSchema.Net** — runtime JSON Schema validation  
- **Microsoft.Extensions.Configuration** — `IConfiguration` overload for `AddSchemaValidation`  
- **Microsoft.Extensions.DependencyInjection.Abstractions** — `IServiceCollection` extensions  
- **Microsoft.Extensions.Logging** — `SchemaValidationService`  
- **Microsoft.Extensions.Options** + **Microsoft.Extensions.Options.ConfigurationExtensions** — options binding  

Keep these aligned on the same **10.x** line as **`Directory.Packages.props`**.

## Packages that may be unavailable on restricted feeds

If restore fails for **Microsoft.Extensions.Configuration.Memory** (or similar), prefer tests that **mock `IConfiguration`** or use **in-repo test doubles** instead of optional satellite packages.
