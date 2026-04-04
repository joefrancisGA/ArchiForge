output "action_group_id" {
  description = "Azure Monitor action group resource ID (empty when monitoring stack disabled)."
  value       = try(azurerm_monitor_action_group.ops[0].id, null)
}

output "grafana_endpoint" {
  description = "HTTPS endpoint for Azure Managed Grafana (null when disabled)."
  value       = try(azurerm_dashboard_grafana.archiforge[0].endpoint, null)
}

output "grafana_principal_id" {
  description = "System-assigned managed identity principal ID for Grafana (for Azure Monitor / Log Analytics role assignments)."
  value       = try(azurerm_dashboard_grafana.archiforge[0].identity[0].principal_id, null)
}
