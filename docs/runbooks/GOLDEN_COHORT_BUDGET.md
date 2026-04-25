> **Scope:** Monthly **USD** spend cap and **kill switch** for the optional **real-LLM** golden-cohort nightly path (`ARCHLUCID_GOLDEN_COHORT_REAL_LLM`). Simulator drift (default CI) is unchanged and does not hit Azure OpenAI.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Golden cohort Azure OpenAI budget and kill switch

## Decision (2026-04-22, refined 2026-04-24)

Owner Q&A ([`PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) items **15** / **25**): dedicated golden-cohort Azure OpenAI usage is capped at **$50 / calendar month**.

> **Updated 2026-04-24 (Improvement 11 — Prompt 11):** the single 90% kill switch was split into a **two-band** Q15-conditional kill-switch: **warn at 80%** ($40 MTD — workflow continues but posts an issue) and **kill at 95%** ($47.50 MTD — workflow skips the cohort run for the rest of the month, does not count as failure). Threshold ratios **0.80 / 0.95** are pinned by [`scripts/ci/assert_golden_cohort_kill_switch_present.py`](../../scripts/ci/assert_golden_cohort_kill_switch_present.py); a PR that weakens them is blocked at merge time. End-to-end operator instructions (including how to flip the gate from disabled to required and how to read the new Workbook) live in [`GOLDEN_COHORT_REAL_LLM_GATE.md`](./GOLDEN_COHORT_REAL_LLM_GATE.md).

**Currency:** the probe reads **Cost Management `ActualCost`** for the filtered resource. The numeric cap in `budget.config.json` is expressed in **USD** in-repo; ensure the subscription’s Cost Management **billing currency** matches your intent (use a USD-billed subscription for this cohort, or edit the cap and this doc to match another currency).

## Configuration (repo)

| File | Purpose |
| ---- | ------- |
| [`tests/golden-cohort/budget.config.json`](../../tests/golden-cohort/budget.config.json) | `monthlyTokenBudgetUsd` (cap), `warnThresholdPercent` (default **80** — Q15-conditional rule), `killSwitchThresholdPercent` (default **95** — Q15-conditional rule), `deploymentName` / `region` placeholders for humans (Cost Management filters by **resource ARM id**, not deployment name alone). |

**Raising the cap (owner-only PR):** edit `monthlyTokenBudgetUsd`, merge with security review, and align any buyer-facing narrative so the repo stays honest about spend. **Do not weaken `warnThresholdPercent` or `killSwitchThresholdPercent`** — those ratios are pinned at 80 / 95 by the CI guard and are the Q15-conditional rule that justifies the budget approval in the first place.

## Probe script

[`scripts/golden_cohort_budget_probe.py`](../../scripts/golden_cohort_budget_probe.py) reads the JSON config and queries **Azure Cost Management** (`ActualCost`, **MonthToDate**) for the Cognitive Services account ARM id in **`ARCHLUCID_GOLDEN_COHORT_AZURE_OPENAI_RESOURCE_ID`**, using **`requests`** only (no new Azure SDK in-repo).

**Authentication (in order):**

1. `ARCHLUCID_ARM_ACCESS_TOKEN` or `AZURE_MANAGEMENT_ACCESS_TOKEN` — bearer token for `https://management.azure.com/`.
2. Otherwise **`az account get-access-token --resource https://management.azure.com/`** (after `az login` or GitHub **`azure/login`**).

**Subscription:** `AZURE_SUBSCRIPTION_ID` if set; else parsed from the resource id path.

**Exit codes (Improvement 11 — dual-band kill-switch)**

| Code | Meaning |
| ---- | ------- |
| **0** | MTD cost **below** the warn threshold (under **80%** of cap by default). |
| **1** | MTD **≥ 80%** and **< 95%** — **WARN** band; cohort still runs and the workflow opens an issue. |
| **2** | MTD **≥ 95%** of cap — **KILL** band; cohort skipped for the rest of the month, workflow does **not** count as failure. |
| **3** | Probe could not run (missing resource id, token, or Cost Management error). |

Machine-readable lines printed for CI (the workflow greps for these):

- `EXPORT_MTD_USD=…`
- `EXPORT_BUDGET_USD=…`
- `EXPORT_WARN_THRESHOLD_USD=…` *(new — Improvement 11)*
- `EXPORT_WARN_THRESHOLD_PCT=…` *(new — Improvement 11)*
- `EXPORT_KILL_THRESHOLD_USD=…`
- `EXPORT_KILL_THRESHOLD_PCT=…` *(new — Improvement 11)*
- `EXPORT_EXIT_CODE=…`

**Local smoke (no Azure):** `ARCHLUCID_GOLDEN_COHORT_BUDGET_PROBE_SIMULATE_MTD_USD=46.5 python scripts/golden_cohort_budget_probe.py`

## GitHub Actions

Workflow: [`.github/workflows/golden-cohort-nightly.yml`](../../.github/workflows/golden-cohort-nightly.yml), job **`cohort-real-llm-gate`**.

**Both gates must pass** for the real-LLM path to be eligible:

1. Repository variable **`ARCHLUCID_GOLDEN_COHORT_REAL_LLM`** must be **`true`** (job `if:`).
2. The **budget probe** must exit **0** or **1** (MTD under the kill threshold). Exit **1** is the WARN band — the cohort runs and an issue is opened. Exit **2** SKIPS the cohort for the rest of the month (kill threshold tripped). Exit **3** skips the cohort and records a probe-failure summary (fix credentials, RBAC, or resource id).

For the operator response playbook (what to do when WARN or KILL fires) see [`GOLDEN_COHORT_REAL_LLM_GATE.md`](./GOLDEN_COHORT_REAL_LLM_GATE.md).

**Secrets / login (owner):** configure **`azure/login`** inputs (`AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`) and repository/ environment secret **`ARCHLUCID_GOLDEN_COHORT_AZURE_OPENAI_RESOURCE_ID`** (full ARM id of the **Microsoft.CognitiveServices/accounts** resource). The service principal (or federated identity) needs **Cost Management read** on the subscription (e.g. **Cost Management Reader**).

## When the kill switch fires

The detailed operator response — including the WARN vs KILL decision tree, how to acknowledge the auto-created GitHub issue, and how to read the cost-and-latency Workbook — lives in [`GOLDEN_COHORT_REAL_LLM_GATE.md`](./GOLDEN_COHORT_REAL_LLM_GATE.md). Short summary:

1. **Wait** until the next **calendar month** resets MTD actual cost (Cost Management MonthToDate), then re-run the workflow; or  
2. **Owner approval** to **temporarily raise the cap** in `budget.config.json` (PR + documented rationale). Note: only the cap is allowed to move — the warn/kill **ratios** are pinned at 80 / 95 by the CI guard and cannot be weakened; or  
3. **Reduce** usage elsewhere on the same resource if the cap is shared (not recommended—prefer a **dedicated** cohort account per decision).

## Where this does **not** apply

- **Simulator** drift (`cohort-simulator-drift`) does not call Azure OpenAI and is not gated by this probe.
- **Private keys** for OpenAI stay in the owner environment / Key Vault / GitHub secrets model you already use for the product—this runbook only gates **spend visibility** for the cohort resource.
