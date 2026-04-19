output "logic_app_id" {
  description = "Resource ID of the Logic App (Standard) when deployed."
  value       = try(azurerm_logic_app_standard.edge[0].id, null)
}

output "logic_app_principal_id" {
  description = "System-assigned managed identity principal ID (for Service Bus RBAC) when deployed."
  value       = try(azurerm_logic_app_standard.edge[0].identity[0].principal_id, null)
}

output "logic_storage_account_id" {
  description = "Backing storage account ID when deployed."
  value       = try(azurerm_storage_account.logic[0].id, null)
}
