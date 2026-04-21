> **Scope:** Mutation testing (Stryker.NET) — scaffolding - full detail, tables, and links in the sections below.

# Mutation testing (Stryker.NET) — scaffolding

## Why

**Unit tests** prove code runs; **mutation tests** ask whether assertions would fail if the implementation changed slightly. Stryker.NET mutates compiled code and re-runs tests to highlight weak or missing assertions.

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) matching the repo `global.json`.
- **Local tool (repo root):** `.config/dotnet-tools.json` pins **`dotnet-stryker`**. Run `dotnet tool restore` once per clone, then `dotnet dotnet-stryker`.

## Configuration

The repo includes **`stryker-config.persistence.json`** (and the equivalent root **`stryker-config.json`**) for **Persistence** (scheduled CI label **Persistence**; thresholds **high 70 / low 55 / break 55**), plus:

- **`stryker-config.application.json`** — `ArchLucid.Application` + `ArchLucid.Application.Tests`
- **`stryker-config.agentruntime.json`** — `ArchLucid.AgentRuntime` + `ArchLucid.AgentRuntime.Tests`
- **`stryker-config.coordinator.json`** — `ArchLucid.Coordinator` + `ArchLucid.Coordinator.Tests`
- **`stryker-config.decisioning.json`** — `ArchLucid.Decisioning` + `ArchLucid.Decisioning.Tests`
- **`stryker-config.persistence-coordination.json`** — `ArchLucid.Persistence.Coordination` + `ArchLucid.Persistence.Tests` (scheduled workflow label **PersistenceCoordination**)
- **`stryker-config.api.json`** — `ArchLucid.Api` + `ArchLucid.Api.Tests` (scheduled workflow label **Api**, added 2026-04-20). HTTP wiring code is mutation-rich, so this config starts at an honest **`thresholds.break = 55`** floor and a baseline of **55.0**, not the **70** used by the older configs. The intent is to ratchet upward on every `refresh_stryker_baselines.py` run that follows a coverage uplift PR — see § "API target (advisory ratchet)" below.

Each config enables **`json`** alongside `progress` and `html` so CI can parse **`mutation-report.json`** (mutation-testing-elements schema).

**Latest ratchet (2026-04-17):** committed **`stryker-baselines.json`** scores for **Persistence**, **Application**, **AgentRuntime**, **Coordinator**, **Decisioning**, and **PersistenceCoordination** were raised **+3.0** percentage points (**67.0 → 70.0**) toward the **75** stretch goal, without changing **`thresholds.break`** (**70**) in the Stryker configs.

Scheduled CI runs all **seven** matrix targets (Persistence, Application, AgentRuntime, Coordinator, Decisioning, PersistenceCoordination, Api) with **`-s ArchLucid.sln`** (avoids ambiguity when multiple `.sln` files exist), uploads **`StrykerOutput`** as an artifact, then runs **`scripts/ci/assert_stryker_score_vs_baseline.py`** against committed scores in **`scripts/ci/stryker-baselines.json`** (default tolerance **0.10** percentage points below baseline → fail). This is a **regression guard** on top of each config’s **`thresholds.break`** (mostly **70**; Api starts at **55** — see § API target).

**Why baselines should track observed scores:** When baseline and **`thresholds.break`** are equal (currently **70**), the assert script still enforces **baseline − 0.10** pp. Prefer **observed green scores** from **`refresh_stryker_baselines.py`** (rounded **down** to one decimal, e.g. 78.37 → **78.3**) so regressions inside the passing band are caught; ratchet **up** when measured scores justify it (e.g. **72**), never down without a product decision. See **`docs/STRYKER_RATchet_TARGET_72.md`** for a safe sequence to move baselines and **`thresholds.low` / `thresholds.break`** to **72** without breaking the scheduled workflow.

### Refreshing `stryker-baselines.json` (calibrated scores)

From the repository root (after **`dotnet tool restore`**):

```bash
python3 scripts/ci/refresh_stryker_baselines.py
```

This runs **`dotnet dotnet-stryker`** once per matrix target (clears **`StrykerOutput/`** before each run), reads the newest **`mutation-report.json`**, floors each score to one decimal, and rewrites **`scripts/ci/stryker-baselines.json`** with a top-level **`_measuredDate`** (ISO date, informational only; the assert script ignores it).

- **CPU / time:** expect on the order of tens of minutes per target; run on a quiet machine or overnight.
- **Subset + merge** (re-measure one project without re-running the full matrix):  
  `python3 scripts/ci/refresh_stryker_baselines.py --only Decisioning --merge-existing`  
  (requires an existing baseline file with the other **STRYKER_TARGETS** labels.)

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
dotnet dotnet-stryker -f stryker-config.persistence-coordination.json -s ArchLucid.sln
dotnet dotnet-stryker -f stryker-config.api.json -s ArchLucid.sln
```

## API target (advisory ratchet)

`stryker-config.api.json` is the newest target. Its baseline of **55.0** and `thresholds.break = 55` are intentionally lower than the **70** used by the older modules: the `ArchLucid.Api` assembly is dominated by HTTP wiring (controllers, middleware, model binding, problem-details mapping) which has a high mutant density relative to assertion-rich domain code.

**Why advisory:** the new target is added to the scheduled matrix but is treated as advisory in practice — failures should be triaged as test gaps, not merge blockers, until the baseline has been ratcheted at least twice. The intended ratchet sequence is:

1. Land the next ArchLucid.Api coverage uplift PR (more controller / middleware unit tests).
2. Run `python3 scripts/ci/refresh_stryker_baselines.py --only Api --merge-existing` from the repo root after `dotnet tool restore`.
3. Commit the updated `stryker-baselines.json` (the script floors to one decimal and writes `_measuredDate`).
4. Repeat at the next coverage uplift; once the measured score sits comfortably above **65**, raise `thresholds.break` in `stryker-config.api.json` to **65** in a follow-up PR.

The long-term target is to bring the API config in line with the other modules at **70 / 70**, but only as observed scores justify each step.

## Scheduled CI

GitHub Actions workflow **`.github/workflows/stryker-scheduled.yml`** runs weekly (and on **workflow_dispatch**), restores tools, runs Stryker against **`ArchLucid.sln`**, asserts the JSON report’s score against **`scripts/ci/stryker-baselines.json`**, and uploads **`StrykerOutput`** as an artifact for review.

**HTML** and **JSON** reports are emitted under `StrykerOutput` (nested timestamp folder; **`mutation-report.json`** is discovered via glob in the assert script).

## Per-PR differential

Workflow **`.github/workflows/stryker-pr.yml`** runs on **`pull_request`** to **`main`** / **`master`** (not merge-blocking on the default CI path).

| Step | Behavior |
|------|----------|
| **Plan** | `scripts/ci/stryker_pr_plan.py` diffs `base.sha...head.sha` and maps touched paths to one or more Stryker configs (same **seven** targets as the weekly matrix, including **Api**). |
| **Triggers full matrix** | Any `stryker-config*.json` at repo root, `stryker-baselines.json`, assert script, this planner, `stryker-pr.yml` / `stryker-scheduled.yml`, or `.config/dotnet-tools.json`. |
| **Run** | For each selected target: `dotnet dotnet-stryker -f <config> -s ArchLucid.sln --since:<base_sha>` so only mutants in the PR diff are exercised (faster than a full run). |
| **Assert** | Same `scripts/ci/assert_stryker_score_vs_baseline.py` as the weekly job, plus **`--allow-zero-denominator`** when the diff scope has no scored mutants (skip compare instead of failing at 0%). |
| **Merge impact** | Jobs use **`continue-on-error: true`** until baselines and noise are acceptable; check the workflow log and artifacts if you need a green signal. |

**Concurrency:** one plan + matrix per PR (`concurrency` group cancels superseded pushes).

**Fork PRs:** checkout uses the head SHA and fetches the base SHA explicitly so `git diff` and Stryker’s `--since` work for forks.

## CI

Mutation testing is **not** part of the default merge-blocking **`ci.yml`** pipeline: it is slower than the Tier 1 “fast core” suite. Use the **PR differential** workflow above for early feedback, the **weekly** scheduled workflow for full regression, or run locally when changing critical persistence or security code.

## Security / cost

- Runs are **CPU-heavy**; avoid parallel mutation on tiny dev VMs.
- No secrets are required; Stryker does not call Azure.

## Baseline ratchet policy

Baselines in **`scripts/ci/stryker-baselines.json`** should only move **up** — never lowered without a product decision. When the next scheduled Stryker run succeeds, refresh baselines using **`python3 scripts/ci/refresh_stryker_baselines.py`** and commit the result. This creates a ratchet effect: each green run raises the floor and prevents future regressions.

Current target: raise all modules to **70+** (aligned with **`thresholds.break`**) by end of Q2 2026. Track progress by comparing baseline values after each weekly run.

## Reliability

Flaky tests will show as “survived” or inconsistent mutants. Fix test isolation before trusting mutation scores.

On **Windows**, stop other **`dotnet test`** / **`testhost`** processes before local Stryker runs; locked **`bin\Debug\*.dll`** copies under unrelated test projects can make Stryker’s compile step fail with “file is being used by another process.”
