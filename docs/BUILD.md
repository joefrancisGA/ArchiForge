# Build & project hygiene

See also [TEST_STRUCTURE.md](TEST_STRUCTURE.md) for test categories and filtering.

**API controllers:** Keep all MVC controllers under **`ArchiForge.Api/Controllers/`** (single folder). On Windows, tools may show the same path with `\` or `/`; on Linux, Git is case-sensitive—do not introduce a second `Controllers` directory that differs only by casing or path style, or you risk duplicate types and confusing diffs.

**RunComparisonController** intentionally depends on three application services (`IEndToEndReplayComparisonService`, `IEndToEndReplayComparisonSummaryFormatter`, `IEndToEndReplayComparisonExportService`) rather than a single facade, for clarity and testability.

## Full solution

```bash
dotnet restore
dotnet build
dotnet test
```

## OpenTelemetry metrics (`ArchiForge` meter)

The API registers meter **`ArchiForge`** (`ArchiForgeInstrumentation.MeterName`). Notable series:

| Metric | Notes |
|--------|--------|
| `digest_delivery_succeeded` / `digest_delivery_failed` | Tag **`channel`**. |
| `alert_evaluation_duration_ms` | Tag **`rule_kind`**: `simple` \| `composite`. |
| `governance_resolve_duration_ms` | End-to-end **`EffectiveGovernanceResolver.ResolveAsync`** latency. |
| `governance_pack_content_deserialize_cache_hits` / `_misses` | In-resolve dedupe when the same pack **version** appears on multiple assignments (not HTTP-scope cache — see **`NEXT_REFACTORINGS.md`** §230). |

Enable **`Observability:Prometheus:Enabled`** (and exporters) as needed for scraping.

## SQL Server vs Testcontainers (contributors)

**Default / CI-friendly:** Most **ArchiForge.Api.Tests** integration tests use **in-memory SQLite** via **`ArchiForgeApiFactory`** and a shared connection string — no Docker required.

**When you need SQL Server fidelity:** For behaviors that differ between SQLite and SQL Server (locking, types, T-SQL), prefer either:

1. **Testcontainers.MsSql** (or similar) in a dedicated test fixture that boots a throwaway container and sets **`ConnectionStrings:ArchiForge`** before building the host, or  
2. A **narrow repository integration test** project that targets a developer-owned SQL instance (document connection string via user secrets / env, never commit secrets).

Keep **one DDL source of truth** (`ArchiForge.Data/SQL/ArchiForge.sql`) and run **`DatabaseMigrator`** (or your migration tool) against the test database before tests that assume schema.

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
