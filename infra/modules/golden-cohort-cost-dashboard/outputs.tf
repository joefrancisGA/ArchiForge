output "workbook_id" {
  description = "Resource ID of the Azure Monitor Workbook (empty when enable_workbook is false)."
  value       = length(azurerm_application_insights_workbook.golden_cohort_cost_latency) > 0 ? azurerm_application_insights_workbook.golden_cohort_cost_latency[0].id : ""
}

output "workbook_display_name" {
  description = "Display name of the provisioned Workbook (echoed back for portal navigation links)."
  value       = var.workbook_display_name
}

output "kill_switch_thresholds" {
  description = "Kill-switch thresholds rendered onto the Workbook (warn/kill percent of cap). Useful for Terraform output assertions in CI."
  value = {
    monthly_budget_usd            = var.monthly_budget_usd
    warn_threshold_percent        = var.warn_threshold_percent
    kill_switch_threshold_percent = var.kill_switch_threshold_percent
  }
}
