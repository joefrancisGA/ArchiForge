# Mutation testing (Stryker.NET) — scaffolding

## Why

**Unit tests** prove code runs; **mutation tests** ask whether assertions would fail if the implementation changed slightly. Stryker.NET mutates compiled code and re-runs tests to highlight weak or missing assertions.

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) matching the repo `global.json`.
- **Local tool (repo root):** `.config/dotnet-tools.json` pins **`dotnet-stryker`**. Run `dotnet tool restore` once per clone, then `dotnet dotnet-stryker`.

## Configuration

The repo includes **`stryker-config.json`** at the solution root for **Persistence**, plus:

- **`stryker-config.application.json`** — `ArchLucid.Application` + `ArchLucid.Application.Tests`
- **`stryker-config.agentruntime.json`** — `ArchLucid.AgentRuntime` + `ArchLucid.AgentRuntime.Tests`
- **`stryker-config.coordinator.json`** — `ArchLucid.Coordinator` + `ArchLucid.Coordinator.Tests`
- **`stryker-config.decisioning.json`** — `ArchLucid.Decisioning` + `ArchLucid.Decisioning.Tests`

Each config enables **`json`** alongside `progress` and `html` so CI can parse **`mutation-report.json`** (mutation-testing-elements schema).

Scheduled CI runs all five targets (matrix) with **`-s ArchLucid.sln`** (avoids ambiguity when multiple `.sln` files exist), uploads **`StrykerOutput`** as an artifact, then runs **`scripts/ci/assert_stryker_score_vs_baseline.py`** against committed scores in **`scripts/ci/stryker-baselines.json`** (default tolerance **0.15** percentage points below baseline → fail). This is a **regression guard** on top of each config’s **`thresholds.break`** (still **60**).

**Why baselines must sit above `break`:** If every baseline equals **`thresholds.break` (60)**, a project can lose most of its mutation-kill rate while still passing Stryker’s own break gate, and the assert script will not fail (floor = baseline − 0.15). Baselines should reflect **observed green scores** (rounded **down** to one decimal, e.g. 78.37 → **78.3**) so slow regressions inside the passing band are caught.

### Refreshing `stryker-baselines.json` (calibrated scores)

From the repository root (after **`dotnet tool restore`**):

```bash
python3 scripts/ci/refresh_stryker_baselines.py
```

This runs **`dotnet dotnet-stryker`** once per matrix target (clears **`StrykerOutput/`** before each run), reads the newest **`mutation-report.json`**, floors each score to one decimal, and rewrites **`scripts/ci/stryker-baselines.json`** with a top-level **`_measuredDate`** (ISO date, informational only; the assert script ignores it).

- **CPU / time:** expect on the order of tens of minutes per target; run on a quiet machine or overnight.
- **Subset + merge** (re-measure one project without re-running all five):  
  `python3 scripts/ci/refresh_stryker_baselines.py --only Decisioning --merge-existing`  
  (requires an existing baseline file with the other four labels.)

**Manual fallback:** open the scheduled workflow artifact or **`StrykerOutput/**/mutation-report.json`**, read the reported mutation score, and update the matching matrix label (still apply **round-down** to one decimal). Do **not** lower baselines to silence failures without a product decision.

Full table: **[TEST_STRUCTURE.md](TEST_STRUCTURE.md)** (Stryker configs).

## Commands

From the repository root:

```bash
dotnet tool restore
dotnet dotnet-stryker -s ArchLucid.sln
dotnet dotnet-stryker -f stryker-config.application.json -s ArchLucid.sln
dotnet dotnet-stryker -f stryker-config.agentruntime.json -s ArchLucid.sln
dotnet dotnet-stryker -f stryker-config.coordinator.json -s ArchLucid.sln
dotnet dotnet-stryker -f stryker-config.decisioning.json -s ArchLucid.sln
```

## Scheduled CI

GitHub Actions workflow **`.github/workflows/stryker-scheduled.yml`** runs weekly (and on **workflow_dispatch**), restores tools, runs Stryker against **`ArchLucid.sln`**, asserts the JSON report’s score against **`scripts/ci/stryker-baselines.json`**, and uploads **`StrykerOutput`** as an artifact for review.

**HTML** and **JSON** reports are emitted under `StrykerOutput` (nested timestamp folder; **`mutation-report.json`** is discovered via glob in the assert script).

## CI

Mutation testing is **not** part of the default GitHub Actions workflow: it is slower than the Tier 1 “fast core” suite. Run it locally or in a scheduled pipeline when changing critical persistence or security code.

## Security / cost

- Runs are **CPU-heavy**; avoid parallel mutation on tiny dev VMs.
- No secrets are required; Stryker does not call Azure.

## Reliability

Flaky tests will show as “survived” or inconsistent mutants. Fix test isolation before trusting mutation scores.

On **Windows**, stop other **`dotnet test`** / **`testhost`** processes before local Stryker runs; locked **`bin\Debug\*.dll`** copies under unrelated test projects can make Stryker’s compile step fail with “file is being used by another process.”
