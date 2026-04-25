output "workbook_id" {
  description = "Resource ID of the Azure Monitor Workbook (empty when enable_workbook is false)."
  value       = length(azurerm_application_insights_workbook.first_tenant_funnel) > 0 ? azurerm_application_insights_workbook.first_tenant_funnel[0].id : ""
}

output "workbook_display_name" {
  description = "Display name of the provisioned Workbook (echoed back for portal navigation links)."
  value       = var.workbook_display_name
}

output "thirty_minute_target_percent" {
  description = "Target conversion percent rendered onto the Workbook; useful for Terraform output assertions in CI."
  value       = var.thirty_minute_target_percent
}
