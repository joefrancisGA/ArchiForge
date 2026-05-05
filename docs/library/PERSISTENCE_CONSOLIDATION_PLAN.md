> **Scope:** Engineering plan only — conditional merge of `ArchLucid.Persistence.Alerts` and `ArchLucid.Persistence.Advisory` into `ArchLucid.Persistence.Runtime`; not an executed refactor, baseline metrics capture guide, nor a SKU or API contract doc.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you are sizing persistence assembly consolidation against build/CI cost.

## 1. Objective

Provide a repeatable procedure to merge **`ArchLucid.Persistence.Alerts`** and **`ArchLucid.Persistence.Advisory`** into **`ArchLucid.Persistence.Runtime`** **only when** empirical evidence shows that **solution build wall-clock** or **core CI tier duration** is a recurring bottleneck attributable to persistence assembly fragmentation.

## 2. Assumptions

- **Measured pain:** Consolidation is **not** the default refactor; justify it with before/after numbers (see §7).
- **Single target assembly:** Consumers today reference **`ArchLucid.Persistence.Runtime`** plus **Alerts** and/or **Advisory** indirectly (for example **`ArchLucid.Host.Composition`**, **`ArchLucid.Host.Core`**, **`ArchLucid.Application`**). The consolidation goal is **one persistence “runtime facets” assembly** carrying today’s Alerts + Advisory + Runtime types unless a later ADR splits concerns differently (for example bounded contexts needing separate deployments).
- **No behavior change:** File moves + project reference cleanup must preserve namespaces and DI bindings unless an explicit semantic change request exists.

## 3. Constraints

- **Risk vs reward:** Larger assemblies can slow incremental compile in some edits; merging helps **graph depth** (fewer hops) more than necessarily **every** edit-test loop.
- **InternalsVisibleTo / test seams:** Today **`ArchLucid.Persistence.Alerts`** exposes **`InternalsVisibleTo`** for **`ArchLucid.Decisioning.Tests`** and **`ArchLucid.Persistence.Tests`**; **`ArchLucid.Persistence.Advisory`** does not list the same set. Merging requires **re-auditing** InternalsVisibleTo on the combined project so tests keep compiling without widening production surface unintentionally.
- **Package references:** Merge **`PackageReference`** and **`ProjectReference`** sets from **Alerts**, **Advisory**, and **Runtime** into one **`.csproj`**, then remove duplicate edges from consuming projects.

## 4. Architecture Overview

**Today (simplified):**

```text
Host.Composition / Host.Core / Application / Api.Tests / …
        → ArchLucid.Persistence.Runtime  → ArchLucid.Persistence.Advisory
        → ArchLucid.Persistence.Alerts   (via Host.Composition and some test projects)
        → shared: ArchLucid.Persistence, ArchLucid.Persistence.Integration, …
```

**Target:** One **`ArchLucid.Persistence.Runtime`** project contains all types currently in **Runtime**, **Alerts**, and **Advisory**; solution and downstream **`.csproj`** files reference **Runtime** only for that surface.

## 5. Component Breakdown

| Assembly today | Role (summary) | Merge action |
|----------------|----------------|--------------|
| **`ArchLucid.Persistence.Runtime`** | Runtime persistence orchestration; already references **Advisory** | Absorb **Alerts** + **Advisory** source; drop child **ProjectReference** to **Advisory** once absorbed |
| **`ArchLucid.Persistence.Advisory`** | Advisory persistence (Dapper, SQL) | Move sources under **`ArchLucid.Persistence.Runtime/`** (or subfolder convention matching repo norms); delete **Advisory** **`.csproj`** when graph is clean |
| **`ArchLucid.Persistence.Alerts`** | Alert persistence | Same as **Advisory** |

**Consumers to re-point (non-exhaustive — re-run grep before executing):**

- **`ArchLucid.Host.Composition`**
- **`ArchLucid.Host.Core`**
- **`ArchLucid.Application`**
- **`ArchLucid.AgentRuntime.Tests`**, **`ArchLucid.Api.Tests`**, **`ArchLucid.Architecture.Tests`**, **`ArchLucid.Decisioning.Tests`**, **`ArchLucid.Persistence.Tests`**

If **`ArchLucid.sln`** (or alternate entry solutions) omit standalone entries for Alerts/Advisory, consolidation still reduces **project reference fan-out** in **`.csproj`** files that transitively build them.

## 6. Data Flow

Not applicable — this document describes **repository packaging**, not runtime data pipelines.

## 7. Operational Considerations (baselines + merge steps)

### 7a. Baselines to capture **before** and **after**

Record **median of three** local runs on a clean **`obj`** / comparable machine profile; store results in the PR description or ADR appendix.

| Signal | Command / source | Notes |
|--------|------------------|--------|
| **Solution Release build** | `dotnet build ArchLucid.sln -c Release` | Wall-clock from cold vs warm **`obj`**; note whether SQL or integration tests ran in parallel locally |
| **CI Tier 1 (fast core) proxy** | `dotnet test ArchLucid.sln --no-build -c Release --settings coverage.runsettings --filter "Suite=Core&Category!=Slow&Category!=Integration&Category!=GoldenCorpusRecord" --collect:"XPlat Code Coverage"` | Mirrors **`.github/workflows/ci.yml`** *dotnet-fast-core* style filter (verify filter string against current workflow if it drifted) |
| **CI Tier 2 (full regression) proxy** | `dotnet test ArchLucid.sln --no-build -c Release --settings coverage.runsettings --collect:"XPlat Code Coverage"` | Heavier — optional for the **decision gate**, required before merge once consolidation is undertaken |
| **GitHub Actions job duration** | Workflow run history for **`CI`** · jobs **“.NET fast core”** / **“.NET full regression”** | Compare **p50/p90** across main vs feature branch runs after the change merges |

Promote consolidation only when at least **one** holds: materially lower **`dotnet build ArchLucid.sln`** after warm cache, materially lower Tier 1 job **p90**, or a measured reduction in **project-reference hop count** paired with reviewer agreement that CI noise dominates.

### 7b. Merge procedure (outline)

1. **Inventory:** `grep` / solution explorer for **`ArchLucid.Persistence.Alerts`** and **`ArchLucid.Persistence.Advisory`** in all **`.csproj`** files.
2. **Physical move:** Move **`.cs`** files from **`ArchLucid.Persistence.Alerts`** and **`ArchLucid.Persistence.Advisory`** into **`ArchLucid.Persistence.Runtime`** (preserve **`RootNamespace`** if required for existing type names — today all use **`ArchLucid.Persistence`**).
3. **Project file:** Fold **packages** and **references** into **`ArchLucid.Persistence.Runtime.csproj`**; resolve duplicates (same **`PackageReference`** line once).
4. **Consumers:** Remove **ProjectReference** lines pointing at Alerts/Advisory; ensure **Runtime** remains referenced where Alerts/Advisory were direct.
5. **Delete:** Remove **`ArchLucid.Persistence.Alerts.csproj`** and **`ArchLucid.Persistence.Advisory.csproj`** and empty folders once **no** references remain. Add/remove **solution** folders if those projects were listed explicitly.
6. **Validate:**
   - `dotnet build ArchLucid.sln -c Release`
   - `dotnet test ArchLucid.Persistence.Tests/ArchLucid.Persistence.Tests.csproj -c Release`
   - plus **`ArchLucid.Host.Composition.Tests`** / **`Decisioning.Tests`** / **`Architecture.Tests`** as affected by InternalsVisibleTo

### 7c. `dotnet` commands (copy-ready)

Capture baselines:

```powershell
dotnet build ArchLucid.sln -c Release

dotnet test ArchLucid.sln --no-build -c Release `
  --settings coverage.runsettings `
  --filter "Suite=Core&Category!=Slow&Category!=Integration&Category!=GoldenCorpusRecord" `
  --collect:"XPlat Code Coverage"
```

Post-merge sanity (same Tier 2 style as CI full regression):

```powershell
dotnet test ArchLucid.sln --no-build -c Release --settings coverage.runsettings --collect:"XPlat Code Coverage"
```

## 8. Security Model

Packaging consolidation does **not** change credential handling. After moves, verify **SQL** parameterization patterns and **`InternalsVisibleTo`** lists did not widen test-only internals to unexpected friend assemblies.

## 9. Scalability / reliability / cost

- **Scalability:** Neutral at runtime — fewer assemblies does not change query paths.
- **Reliability:** Build graph simplification reduces **ordering** hazards in parallel builds slightly; regressions surface as compile errors rather than flaky behavior.
- **Cost:** Potential **minor** CI time savings when restore/compile dominates; quantify with §7a before spending merge-conflict churn.
