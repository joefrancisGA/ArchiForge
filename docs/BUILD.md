# Build & project hygiene

See also [TEST_STRUCTURE.md](TEST_STRUCTURE.md) for test categories and filtering.

**RunComparisonController** intentionally depends on three application services (`IEndToEndReplayComparisonService`, `IEndToEndReplayComparisonSummaryFormatter`, `IEndToEndReplayComparisonExportService`) rather than a single facade, for clarity and testability.

## Full solution

```bash
dotnet restore
dotnet build
dotnet test
```

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
