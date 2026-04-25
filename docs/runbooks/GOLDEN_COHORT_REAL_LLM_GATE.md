> **Scope:** End-to-end operator instructions for the optional **real-LLM** golden-cohort nightly path: how to flip the gate from disabled to required, how to respond when the **kill-switch** trips, and how to read the cost-and-latency Workbook. Pair with [`GOLDEN_COHORT_BUDGET.md`](./GOLDEN_COHORT_BUDGET.md) for the budget mechanics.

> **Spine doc.** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.

# Golden cohort real-LLM gate — operator runbook

## 1. What this gate does

The `cohort-real-llm-gate` job in [`.github/workflows/golden-cohort-nightly.yml`](../../.github/workflows/golden-cohort-nightly.yml) runs the 20-row golden cohort against the **dedicated Azure OpenAI deployment** so the Simulator-baselined SHAs in `tests/golden-cohort/cohort.json` are continuously validated against the real model. It is gated on **two** conditions, both of which must be true:

1. The repository variable `ARCHLUCID_GOLDEN_COHORT_REAL_LLM` is `"true"`.
2. [`scripts/golden_cohort_budget_probe.py`](../../scripts/golden_cohort_budget_probe.py) returns an exit code that allows the job to proceed (see § 3).

The Q15 ($50/month) approval was **conditional on the kill-switch being shipped** ([`PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) Q15). If the kill-switch is bypassed, real-LLM execution must revert to disabled until the kill-switch is restored.

## 2. Flip the gate from disabled → required (one-line change)

After the dedicated Azure OpenAI deployment exists in the production subscription **and** the protected GitHub Environment has the secrets injected (both owner-only operational tasks per Q15), promotion is a single edit:

```diff
   cohort-real-llm-gate:
     needs: cohort-contract
-    if: ${{ vars.ARCHLUCID_GOLDEN_COHORT_REAL_LLM == 'true' }}
+    if: ${{ vars.ARCHLUCID_GOLDEN_COHORT_REAL_LLM == 'true' }}
+    # Promote from optional → required by removing the conditional once the
+    # deployment + secrets exist. After promotion, mark this job as required
+    # in the branch protection rule for `main`.
     runs-on: ubuntu-latest
```

Then, in the GitHub branch-protection rule for `main`, **add `cohort-real-llm-gate` to the required status checks**. That is the entire promotion. **Do not** flip it in the same PR that ships the deployment — separate the two so a single PR can be reverted.

## 3. Probe exit-code semantics (the kill-switch)

| Exit code | MTD spend (default cap = $50) | Workflow behavior | Issue created? |
| --------- | ----------------------------- | ----------------- | -------------- |
| **0** | < **80%** of cap (< $40) | Cohort runs normally | No |
| **1** | ≥ **80%** and < **95%** ($40 ≤ MTD < $47.50) | Cohort **still runs** (yellow band); workflow summary shows WARN | **Yes** — title `Golden cohort kill-switch WARN — YYYY-MM-DD` |
| **2** | ≥ **95%** of cap (≥ $47.50) | Cohort **SKIPPED** for the rest of the month; workflow does **not** count as failure | **Yes** — title `Golden cohort kill-switch KILL — SKIPPED — YYYY-MM-DD` |
| **3** | Probe failed (auth, RBAC, network) | Cohort skipped; workflow does **not** count as failure | No (probe was unable to attribute spend) |

Threshold ratios **0.80 / 0.95** are pinned by [`scripts/ci/assert_golden_cohort_kill_switch_present.py`](../../scripts/ci/assert_golden_cohort_kill_switch_present.py); a PR that weakens them is blocked at merge time.

## 4. Responding to a kill-switch trip

### When **WARN** (exit 1) fires

1. The auto-created issue carries the workflow run URL and the MTD/warn/kill USD numbers.
2. Open the **Workbook** (§ 5) and scan the daily token-count trend — if a single day spiked, look for runaway prompt loops in the most recent cohort scenario JSON deltas.
3. **Decision tree:**
   * **Spend trajectory < cap** by month-end → no action; close the issue with a short comment ("expected drift, on-track for $X by EOM").
   * **Spend trajectory crosses cap** → file an owner decision: temporarily raise `monthlyTokenBudgetUsd` (PR + rationale) or pause the gate by flipping `ARCHLUCID_GOLDEN_COHORT_REAL_LLM` to `false`.

### When **KILL — SKIPPED** (exit 2) fires

1. The cohort is skipped for the **rest of the calendar month**. Each subsequent nightly run will re-emit the kill state and either reopen or append to the daily issue (deduped by date).
2. **Do nothing** until the next month resets MTD — that is the safe default and is exactly what the Q15-conditional rule is protecting.
3. If staying offline for the rest of the month is **not acceptable** (e.g., a release window depends on the cohort), the owner can:
   * Raise `monthlyTokenBudgetUsd` in `tests/golden-cohort/budget.config.json` (PR + rationale + security review). The CI guard does **not** restrict the cap value, only the warn/kill ratios — so a cap raise is allowed.
   * Or move the cohort to a fresh `Microsoft.CognitiveServices/accounts` account with its own MTD (rare; recommended only when the existing account is shared with non-cohort workloads).
4. **Do not** weaken `warnThresholdPercent` or `killSwitchThresholdPercent` to "buy room" — that is exactly what the CI guard refuses (Q15-conditional rule).

### When **probe failed** (exit 3) fires

1. Inspect the workflow log — the probe prints the reason on stderr (missing `ARCHLUCID_GOLDEN_COHORT_AZURE_OPENAI_RESOURCE_ID`, RBAC denial, Cost Management 5xx, etc.).
2. The cohort is skipped (safe default — without an MTD signal we cannot honor the kill-switch).
3. Fix the underlying cause (RBAC: ensure the federated identity has **Cost Management Reader** on the subscription) and rerun the workflow with `workflow_dispatch`.

## 5. Reading the Workbook

The cost-and-latency Workbook is provisioned by the Terraform module [`infra/modules/golden-cohort-cost-dashboard/`](../../infra/modules/golden-cohort-cost-dashboard/README.md) inside the existing App Insights resource group. From the Azure portal:

1. Open the Application Insights resource → **Workbooks** → **"ArchLucid — Golden cohort real-LLM cost & latency"**.
2. Tiles, in order:
   * **Header** — restates the cap, warn %, kill %, and links back to this runbook.
   * **Kill-switch banner** — `enabled (warn=80% / kill=95%)` when the CI guard is in place. Shows `BYPASSED` only if the variables in Terraform have been overridden (which the module's `validation { }` blocks at plan time).
   * **Month-to-date spend (USD)** — daily MTD trend from the probe's `customMetrics.golden_cohort_mtd_usd`.
   * **Per-scenario p50/p95/p99 latency** — bar chart from `customMetrics.golden_cohort_latency_p*_ms`, one bar per cohort scenario.
   * **Daily token-count trend** — line chart from `customMetrics.golden_cohort_token_count`, useful for catching prompt-bloat well before MTD spend reflects it.
   * **Footer** — explains exactly which CI script / file feeds each tile.
3. The Workbook is **read-only by default** (`isLocked: true`) — only the cohort-ops role on the subscription can edit. To modify the queries, fork the Workbook in the portal and propose the JSON change as a PR against `infra/modules/golden-cohort-cost-dashboard/workbook.tpl.json`.

## 6. Stop-and-ask boundaries (do **not** automate these)

These are explicitly listed in `docs/CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_23_73_20.md` Prompt 11. They remain owner-only:

* **Provisioning the dedicated Azure OpenAI deployment** — Cognitive Services account, deployment name, model SKU, region quota.
* **Injecting the Azure OpenAI secret** into the protected GitHub Environment.
* **Flipping `cohort-real-llm-gate`** from `if:` (optional) to no-`if:` (required). That is the one-line change in § 2 and must be a separate PR after the deployment exists.

## 7. Structural validation (real-LLM output)

When the optional [`../../ArchLucid.Core/GoldenCorpus/RealLlmOutputStructuralValidator.cs`](../../ArchLucid.Core/GoldenCorpus/RealLlmOutputStructuralValidator.cs) gate is used, automation checks **only JSON shape** for each `AgentResult` returned by the API (after execute). It does **not** compare claim text, finding messages, or category strings to a golden string — those remain covered by the locked manifest SHA and finding-category multiset in the standard drift path.

**What is checked**

- The payload is valid JSON and the root is an object.
- Required top-level **AgentResult** properties are present: `resultId`, `taskId`, `runId`, `agentType`, `claims`, `evidenceRefs`, `confidence`, `createdUtc`, `findings` (camelCase, matching [`ArchLucid.Contracts.Agents.AgentResult`](../../ArchLucid.Contracts/Agents/AgentResult.cs) serialization).
- `agentType` in JSON matches the expected agent (Topology / Cost / Compliance / Critic), including enum-as-number when the API emits an integer.
- `findings` is a **non-empty** array (the cohort is expected to surface at least one finding per result for the gate to be meaningful).
- Each element of `findings` has a `trace` object (ExplainabilityTrace) with the list-shaped fields `graphNodeIdsExamined`, `rulesApplied`, `decisionsTaken`, `alternativePathsConsidered`, and `notes` (each a JSON array; empty arrays are valid). `sourceAgentExecutionTraceId` is optional and may be null or omitted.
- **No** assertion is made on the *contents* of strings or arrays (only that required keys exist and lists are JSON arrays).

**Why this shape**

Real models can paraphrase text while still being “correct” for product semantics; comparing raw strings is brittle. Structural checks catch systematic wiring failures (missing explainability, empty finding sets, wrong envelope) that would make MTD cost and the Workbook hard to trust without content-level flakiness.

**CLI entry points**

- `archlucid golden-cohort drift --strict-real` — when the shell is configured for a real-LLM API host (`ARCHLUCID_GOLDEN_COHORT_REAL_LLM` and/or `ARCHLUCID_AGENT_EXECUTION_MODE` / `AgentExecution__Mode=Real`) and the run has not recorded simulator fallback, run SHA + category drift **and** per-result structural validation. Any structural failure yields exit code 4 and prints a **JSON** report to stdout.
- `archlucid golden-cohort drift --structural-only` — skips **SHA-256 and category** checks (same [manifest fingerprinting](../../ArchLucid.Cli/Commands/GoldenCohortDriftCommand.cs) code path is simply not used for comparison); only structural validation and API connectivity. Combine with `--strict-real` to enforce the real-LLM shell + no-fallback rules and structural checks together.

**Unit tests** live under [`../../ArchLucid.Core.Tests/GoldenCorpus/RealLlmOutputStructuralValidatorTests.cs`](../../ArchLucid.Core.Tests/GoldenCorpus/RealLlmOutputStructuralValidatorTests.cs) and use `[Trait("Suite", "Core")]`; they cover valid/invalid/edge cases for all four agent types.

> Note: the structural validator **implementation** is in `ArchLucid.Core` (so the CLI and tests can share it). The `*.Tests` project contains the tests only.

## 8. Interpreting structural failures

| Symptom in JSON / stderr | Likely cause | What to do |
| ------------------------ | ------------ | ---------- |
| `jsonSyntax` check failed | Truncated body, non-JSON error page, or gzip/stream handling issue | Re-run with `--json` on a failing HTTP client, verify `/v1/architecture/run/{runId}` returns JSON, confirm proxy is not returning HTML. |
| `topLevelKeys` or `findingsNonEmpty` | Omitted contract field or empty `findings` from the executor | Inspect coordinator/agent pipeline for the agent type; real-LLM path must still emit a full `AgentResult` contract. |
| `agentTypeMatch` | Mismatched or missing `agentType` on a result row | Check task/result mapping for the run; each result should match the task’s agent. |
| `findingTrace` / `traceLists` | Missing `trace` or a Explainability list field is not a JSON array | Ensure persistence/serialization of ExplainabilityTrace (see decisioning models) is wired for real execution. |
| `sourceAgentExecutionTraceId` with wrong type | Value is neither `null` nor a string | Fix serializer or model to emit a string or null. |
| Exit 4 with `code: "realModeFellBackToSimulator"` | The API recorded a real-LLM attempt that fell back to the simulator | Fix Azure OpenAI reachability, quota, or deployment name; the strict gate refuses to treat output as “real-LLM validated” in that case. |
| `strict-real` refused before connect | Real-LLM env not set in the shell | Export `ARCHLUCID_GOLDEN_COHORT_REAL_LLM` or set agent execution mode to `Real` for the CLI process, as documented above. |

**Example: truncated trace** (single finding with `graphNodeIdsExamined` as a string instead of an array) fails the `traceLists` check with a message pointing at the offending `findings[i].trace` path — fix the type in the result builder, not the text of graph node ids.

## 9. Related files

| File | Purpose |
| ---- | ------- |
| [`../../ArchLucid.Core/GoldenCorpus/RealLlmOutputStructuralValidator.cs`](../../ArchLucid.Core/GoldenCorpus/RealLlmOutputStructuralValidator.cs) | Structural JSON validation (no content matching) |
| [`../../ArchLucid.Core.Tests/GoldenCorpus/RealLlmOutputStructuralValidatorTests.cs`](../../ArchLucid.Core.Tests/GoldenCorpus/RealLlmOutputStructuralValidatorTests.cs) | Core suite tests for the validator |
| [`../../ArchLucid.Cli/Commands/GoldenCohortDriftCommand.cs`](../../ArchLucid.Cli/Commands/GoldenCohortDriftCommand.cs) | `archlucid golden-cohort drift` (SHA drift + optional `--strict-real` / `--structural-only`) |
| [`tests/golden-cohort/budget.config.json`](../../tests/golden-cohort/budget.config.json) | `monthlyTokenBudgetUsd`, `warnThresholdPercent: 80`, `killSwitchThresholdPercent: 95` |
| [`scripts/golden_cohort_budget_probe.py`](../../scripts/golden_cohort_budget_probe.py) | The MTD probe; emits exit codes 0/1/2/3 |
| [`scripts/ci/assert_golden_cohort_kill_switch_present.py`](../../scripts/ci/assert_golden_cohort_kill_switch_present.py) | Merge-blocking guard for the Q15-conditional rule |
| [`scripts/ci/test_golden_cohort_budget_probe.py`](../../scripts/ci/test_golden_cohort_budget_probe.py) | Probe threshold-parsing unit tests |
| [`scripts/ci/tests/test_assert_golden_cohort_kill_switch_present.py`](../../scripts/ci/tests/test_assert_golden_cohort_kill_switch_present.py) | CI guard self-test |
| [`infra/modules/golden-cohort-cost-dashboard/`](../../infra/modules/golden-cohort-cost-dashboard/README.md) | Terraform module for the Workbook |
| [`.github/workflows/golden-cohort-nightly.yml`](../../.github/workflows/golden-cohort-nightly.yml) | Nightly workflow with the gated job |
| [`docs/runbooks/GOLDEN_COHORT_BUDGET.md`](./GOLDEN_COHORT_BUDGET.md) | Budget config / Cost Management mechanics |
