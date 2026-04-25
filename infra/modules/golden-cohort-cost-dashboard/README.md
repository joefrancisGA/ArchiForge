# `golden-cohort-cost-dashboard` Terraform module

> **Stop-and-ask boundary (Prompt 11):** this module **does not** provision the dedicated Azure
> OpenAI deployment, and **does not** inject any secret. Both remain owner-only operational tasks
> per [`docs/PENDING_QUESTIONS.md`](../../../docs/PENDING_QUESTIONS.md) **Q15**.

Provisions an **Azure Monitor Workbook** that visualises the dedicated golden-cohort
Azure OpenAI gate:

| Tile | Source | Purpose |
| ---- | ------ | ------- |
| Month-to-date spend (USD) | `customMetrics.golden_cohort_mtd_usd` (App Insights) | Track $/month against the $50 cap before the kill threshold (95%) trips |
| Per-scenario p50 / p95 / p99 latency | `customMetrics.golden_cohort_latency_p*_ms` | Catch regressions in cohort scenario response times |
| Daily token-count trend | `customMetrics.golden_cohort_token_count` | Spot prompt-bloat / runaway loops before they show up in spend |
| Kill-switch banner | static (warn / kill percent shown) | Single-glance answer to "is the kill-switch armed at 80% / 95%?" |

## Why a Workbook (not Grafana / a Power BI report)

- **Single workspace.** The repo's existing App Insights + Log Analytics pair is the source of
  every cost and latency metric we already collect; a Workbook lives in the same blade and
  inherits the same RBAC.
- **Read-only by default.** Workbooks support `isLocked: true` at the JSON level — only the
  cohort-ops role can edit. Grafana would require a separate identity model.
- **No new secret.** The Workbook reads from Cost Management + customMetrics that already exist;
  Grafana / Power BI would need a new data-source credential.

## Inputs

See [`variables.tf`](./variables.tf). The two threshold variables are pinned by `validation { ... }`
to **80** and **95** because they are the [Q15-conditional](../../../docs/PENDING_QUESTIONS.md#q15)
rule — weakening them in Terraform would silently desync from the kill-switch enforced by
[`scripts/ci/assert_golden_cohort_kill_switch_present.py`](../../../scripts/ci/assert_golden_cohort_kill_switch_present.py).

## Outputs

| Output | Use |
| ------ | --- |
| `workbook_id` | Pin downstream resources / role assignments to the Workbook. |
| `workbook_display_name` | Echoed into navigation links / runbook. |
| `kill_switch_thresholds` | Object useful for asserting threshold parity in Terraform output checks. |

## Wiring example

```hcl
module "golden_cohort_cost_dashboard" {
  source = "../../modules/golden-cohort-cost-dashboard"

  enable_workbook                  = true
  resource_group_name              = data.azurerm_resource_group.monitoring.name
  location                         = data.azurerm_resource_group.monitoring.location
  application_insights_resource_id = azurerm_application_insights.archlucid.id
  azure_openai_resource_id         = "/subscriptions/.../providers/Microsoft.CognitiveServices/accounts/archlucid-cohort-openai"
  monthly_budget_usd               = 50
  warn_threshold_percent           = 80
  kill_switch_threshold_percent    = 95

  tags = local.common_tags
}
```

## Operator workflow

1. Workbook URL appears in the Azure portal under the App Insights resource → **Workbooks** →
   "ArchLucid — Golden cohort real-LLM cost & latency".
2. The cost tile is populated by `scripts/golden_cohort_budget_probe.py` (the same probe that
   gates the workflow). When the warn band trips, a GitHub issue is opened automatically; the
   Workbook gives the owner the historical context to decide whether to ride it out or pause.
3. Latency tiles are populated when the .NET cohort tests upload their per-scenario JSON
   (`ARCHLUCID_GOLDEN_COHORT_LATENCY_REPORT_PATH`) and an owner-side ingestion job copies it
   into App Insights customMetrics. That ingestion job is intentionally **not** in this module
   (it would require an App Insights connection string).

See [`docs/runbooks/GOLDEN_COHORT_REAL_LLM_GATE.md`](../../../docs/runbooks/GOLDEN_COHORT_REAL_LLM_GATE.md)
for end-to-end operator instructions.
