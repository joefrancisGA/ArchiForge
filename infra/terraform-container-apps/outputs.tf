output "log_analytics_workspace_id" {
  description = "Resource ID of the Log Analytics workspace backing the Container Apps environment."
  value       = try(azurerm_log_analytics_workspace.container_apps[0].id, null)
}

output "container_app_environment_id" {
  description = "Resource ID of the Container Apps managed environment."
  value       = try(azurerm_container_app_environment.main[0].id, null)
}

output "container_app_environment_default_domain" {
  description = "Default DNS suffix for apps in this environment (useful for internal DNS)."
  value       = try(azurerm_container_app_environment.main[0].default_domain, null)
}

output "api_container_app_fqdn" {
  description = "FQDN of the latest API revision (ingress hostname)."
  value       = try(azurerm_container_app.api[0].latest_revision_fqdn, null)
}

output "api_https_url" {
  description = "HTTPS URL for the API (configure UI proxy / env to this host)."
  value       = try("https://${azurerm_container_app.api[0].latest_revision_fqdn}", null)
}

output "ui_container_app_fqdn" {
  description = "FQDN of the latest Operator UI revision."
  value       = try(azurerm_container_app.ui[0].latest_revision_fqdn, null)
}

output "ui_https_url" {
  description = "HTTPS URL for the Operator UI."
  value       = try("https://${azurerm_container_app.ui[0].latest_revision_fqdn}", null)
}
