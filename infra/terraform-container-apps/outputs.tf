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

output "api_system_assigned_principal_id" {
  description = "Object ID of the API container app system-assigned managed identity (for extra RBAC beyond blob offload)."
  value       = try(azurerm_container_app.api[0].identity[0].principal_id, null)
}

output "worker_system_assigned_principal_id" {
  description = "Object ID of the worker container app system-assigned managed identity."
  value       = try(azurerm_container_app.worker[0].identity[0].principal_id, null)
}

output "worker_container_app_fqdn" {
  description = "FQDN of the latest worker revision when ingress is enabled (null if internal-only without public hostname)."
  value       = try(azurerm_container_app.worker[0].latest_revision_fqdn, null)
}

output "ui_container_app_fqdn" {
  description = "FQDN of the latest Operator UI revision."
  value       = try(azurerm_container_app.ui[0].latest_revision_fqdn, null)
}

output "ui_https_url" {
  description = "HTTPS URL for the Operator UI."
  value       = try("https://${azurerm_container_app.ui[0].latest_revision_fqdn}", null)
}

output "container_apps_consumption_budget_id" {
  description = "Resource id of the Container Apps consumption budget when enable_container_apps_consumption_budget is true; otherwise null."
  value       = try(azurerm_consumption_budget_resource_group.container_apps[0].id, null)
}
