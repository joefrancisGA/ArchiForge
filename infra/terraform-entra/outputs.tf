output "entra_registration_enabled" {
  value       = var.enable_entra_api_app
  description = "Whether the Entra app registration was created."
}

output "tenant_id" {
  value       = var.enable_entra_api_app ? data.azuread_client_config.current.tenant_id : null
  description = "Directory (tenant) ID."
}

output "api_application_client_id" {
  value       = var.enable_entra_api_app ? azuread_application.api[0].client_id : null
  description = "Application (client) ID — use as JWT audience or in OAuth metadata."
}

output "api_identifier_uri" {
  value       = var.enable_entra_api_app ? var.api_identifier_uri : null
  description = "Configured App ID URI (ArchLucidAuth:Audience in JwtBearer mode)."
}

output "api_service_principal_object_id" {
  value       = var.enable_entra_api_app ? azuread_service_principal.api[0].object_id : null
  description = "Enterprise application object id (for role assignments)."
}
