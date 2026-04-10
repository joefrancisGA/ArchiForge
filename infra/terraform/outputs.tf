output "api_management_enabled" {
  description = "Whether API Management was requested via enable_api_management."
  value       = var.enable_api_management
}

output "api_management_gateway_url" {
  description = "Public APIM gateway URL (when deployed)."
  value       = var.enable_api_management ? "https://${azurerm_api_management.archiforge[0].name}.azure-api.net" : null
}

output "api_management_api_base_path" {
  description = "Relative path segment for the ArchLucid API on the gateway."
  value       = var.enable_api_management ? var.apim_api_path_suffix : null
}

output "api_management_principal_id" {
  description = "APIM system-assigned managed identity principal id (for Key Vault / backend auth wiring)."
  value       = var.enable_api_management ? azurerm_api_management.archiforge[0].identity[0].principal_id : null
}

output "api_management_tenant_id" {
  description = "Azure AD tenant id for the APIM identity."
  value       = var.enable_api_management ? azurerm_api_management.archiforge[0].identity[0].tenant_id : null
}
