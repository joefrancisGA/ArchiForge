# Improvement 11 (2026-04-24) — Azure Monitor Workbook for the golden-cohort real-LLM gate.
#
# This module provisions ONLY the Workbook (and reads from an existing App Insights resource).
# It does NOT provision the Azure OpenAI deployment, and it does NOT inject any secret —
# both remain owner-only operational tasks per PENDING_QUESTIONS Q15.

locals {
  workbook_enabled = var.enable_workbook && length(trimspace(var.resource_group_name)) > 0 && length(trimspace(var.application_insights_resource_id)) > 0

  workbook_data = templatefile(
    "${path.module}/workbook.tpl.json",
    {
      workbook_display_name            = var.workbook_display_name
      monthly_budget_usd               = var.monthly_budget_usd
      warn_threshold_percent           = var.warn_threshold_percent
      kill_switch_threshold_percent    = var.kill_switch_threshold_percent
      application_insights_resource_id = var.application_insights_resource_id
    }
  )
}

resource "azurerm_application_insights_workbook" "golden_cohort_cost_latency" {
  count = local.workbook_enabled ? 1 : 0

  name                = var.workbook_name_guid
  resource_group_name = var.resource_group_name
  location            = var.location
  display_name        = var.workbook_display_name
  source_id           = var.application_insights_resource_id
  data_json           = local.workbook_data

  tags = merge(
    var.tags,
    {
      "archlucid:owner"                = "platform"
      "archlucid:cost-center"          = "golden-cohort-real-llm"
      "archlucid:kill-switch-warn-pct" = tostring(var.warn_threshold_percent)
      "archlucid:kill-switch-kill-pct" = tostring(var.kill_switch_threshold_percent)
    }
  )
}
