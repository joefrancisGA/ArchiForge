> **Scope:** ArchLucid.Contracts vs ArchLucid.Contracts.Abstractions (2026-04-07) - full detail, tables, and links in the sections below.

# ArchLucid.Contracts vs ArchLucid.Contracts.Abstractions (2026-04-07)

## Objective

Split **service interfaces** out of `ArchLucid.Contracts` into `ArchLucid.Contracts.Abstractions` so consumers that only need DTOs can reference `ArchLucid.Contracts` without pulling in service abstractions.

## Dependency rule

- `ArchLucid.Contracts.Abstractions` → references → `ArchLucid.Contracts`
- `ArchLucid.Contracts` does **not** reference `ArchLucid.Contracts.Abstractions`

## Inventory (Step 0)

All `public interface` declarations in `ArchLucid.Contracts` were found (8 types). None were single-consumer-only; each is used by at least two of: `ArchLucid.Persistence`, `ArchLucid.Application`, `ArchLucid.Api`, `ArchLucid.Host.Composition`.

| Interface | Destination |
|-----------|-------------|
| `IImprovementThemeExtractionService` | `ArchLucid.Contracts.Abstractions` |
| `IImprovementPlanPrioritizationService` | `ArchLucid.Contracts.Abstractions` |
| `IImprovementPlanningService` | `ArchLucid.Contracts.Abstractions` |
| `IProductLearningImprovementOpportunityService` | `ArchLucid.Contracts.Abstractions` |
| `IProductLearningFeedbackAggregationService` | `ArchLucid.Contracts.Abstractions` |
| `IProductLearningDashboardService` | `ArchLucid.Contracts.Abstractions` |
| `ISimulationEngine` | `ArchLucid.Contracts.Abstractions` |
| `ICandidateChangeSetService` | `ArchLucid.Contracts.Abstractions` |

## Namespace strategy

File paths moved under `ArchLucid.Contracts.Abstractions/` mirroring the old layout. **Namespaces unchanged** (`ArchLucid.Contracts.*`) so existing `using` directives in application code did not require updates.

## XML documentation in `ArchLucid.Contracts`

Because `ArchLucid.Contracts` cannot reference abstractions, `see cref` to moved interfaces in DTO XML comments was replaced with `<c>…</c>` in:

- `ProductLearningTriageOptions.cs`
- `ProductLearningAggregationSnapshot.cs`
- `ProductLearning/Planning/ImprovementPlan.cs`
- `Evolution/SimulationRequest.cs`

## Project references added

Projects that compile against the moved interfaces now reference `ArchLucid.Contracts.Abstractions` **in addition to** `ArchLucid.Contracts`:

- `ArchLucid.Application`
- `ArchLucid.Api`
- `ArchLucid.Persistence`
- `ArchLucid.Host.Composition`

Other projects keep only `ArchLucid.Contracts` if they use DTOs alone (or receive abstractions transitively through the graph).

## Verification

- `dotnet build ArchLucid.sln`
- `dotnet build ArchLucid.Contracts/ArchLucid.Contracts.csproj --no-dependencies` (confirms no cycle)
- `dotnet test --filter "Suite=Core&Category!=Slow&Category!=Integration"`

## Surprises / deviations

- `ArchLucid.Contracts` previously contained **only** these eight public interfaces; there were no marker interfaces or other `public interface` types left behind.
- `InternalsVisibleTo` was not present on `ArchLucid.Contracts.csproj`; none was added to Abstractions (no internal test surface for these interfaces).
