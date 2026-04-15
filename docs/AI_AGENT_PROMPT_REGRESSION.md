# Agent prompt regression guard (scaffolding)

## Objective

Provide a **repeatable** local/CI hook that fails when simulator-mode agent outputs drift materially after prompt or handler changes — complementary to **Stryker** (mutation) and **line coverage**.

## Current state

- **`scripts/ci/assert_prompt_regression.py`** — compares **`ArchLucid.AgentRuntime.Tests`** structural/semantic score summaries against **`scripts/ci/prompt_regression_baseline.json`** when that file is present.
- Baseline values are **placeholders** until you capture a green run on your machine and commit updated numbers (see script header).

## Usage

From repo root (after `dotnet` tests have produced the log artifact path expected by the script, or after extending the script to run `dotnet test` itself):

```bash
python scripts/ci/assert_prompt_regression.py
```

## Evolution

- Wire the script into **`.github/workflows/ci.yml`** as a separate job after full regression when baseline discipline is agreed.
- Prefer parsing **structured test output** or a dedicated **metrics JSON** emitted by a small test helper over scraping free-text logs.

## Related

- **`docs/MUTATION_TESTING_STRYKER.md`**
- **`ArchLucid.AgentRuntime.Tests/Evaluation/AgentOutputSemanticEvaluatorTests.cs`**
