> **Scope:** Agent prompt regression guard - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Agent prompt regression guard

## Objective

Provide a **repeatable** local/CI hook that fails when simulator-mode agent outputs drift materially after prompt or handler changes — complementary to **Stryker** (mutation) and **line coverage**.

## Current state

| Layer | Role |
|--------|------|
| **`scripts/ci/assert_prompt_regression.py`** | Validates **`prompt_regression_baseline.json`** shape and enforces **Topology** minimum floors in the file (≥ **0.9** structural, ≥ **0.5** semantic) so the baseline cannot silently revert to all zeros. |
| **`ArchLucid.AgentRuntime.Tests/Evaluation/PromptRegressionBaselineContractTests`** | Loads the same JSON (linked into **`Fixtures/Regression/`** at build) and asserts **`golden-agent-result-valid.json`** meets the committed **Topology** mins under **`AgentOutputEvaluator`** / **`AgentOutputSemanticEvaluator`**. |
| **`scripts/ci/assert_agent_reference_baselines.py`** | Golden JSON fixture presence + parse guard (separate CI step). |

**Cost / Compliance / Critic** rows in the baseline remain **0.0** until dedicated golden fixtures and tests exist; only **Topology** is merge-blocking today.

## Usage

From repo root:

```bash
python scripts/ci/assert_prompt_regression.py
dotnet test ArchLucid.AgentRuntime.Tests -c Release --filter "FullyQualifiedName~PromptRegressionBaselineContractTests"
```

CI runs the Python step and the full test suite (including the contract test).

## Evolution

- Add per-agent golden JSON + raise **`min*ByAgentType`** for **Cost**, **Compliance**, and **Critic** when ready.
- Optional: emit **`artifacts/prompt_regression_metrics.json`** from tests and extend the Python script to diff across commits (heavier than evaluator-in-test).

## Related

- **`docs/MUTATION_TESTING_STRYKER.md`**
- **`ArchLucid.AgentRuntime.Tests/Evaluation/AgentOutputSemanticEvaluatorTests.cs`**
