variable "enable_monitoring_stack" {
  type        = bool
  description = "When true, create Azure Monitor action group and optional metric alerts. Safe default false for CI and laptops."
  default     = false
}

variable "resource_group_name" {
  type        = string
  description = "Existing resource group that holds ArchiForge Container Apps (or shared monitoring RG)."
  default     = ""
}

variable "name_prefix" {
  type        = string
  description = "Short prefix for alert rule and action group names (alphanumeric, no spaces)."
  default     = "archiforge"
}

variable "alert_email_address" {
  type        = string
  description = "Primary operations email for the shared action group (required when enable_monitoring_stack is true)."
  default     = ""
}

variable "alert_webhook_uri" {
  type        = string
  description = "Optional HTTPS webhook (Teams Incoming Webhook, PagerDuty, etc.) registered on the action group."
  default     = ""
  sensitive   = true
}

variable "api_container_app_resource_id" {
  type        = string
  description = "Full Azure resource ID of the API Container App (Microsoft.App/containerApps/...). Empty skips API CPU alert."
  default     = ""
}

variable "worker_container_app_resource_id" {
  type        = string
  description = "Full Azure resource ID of the Worker Container App. Empty skips worker CPU alert."
  default     = ""
}

variable "container_cpu_nanos_threshold" {
  type        = number
  description = "CpuUsageNanoCores average threshold (5m window). Example: 0.5 vCPU ≈ 500000000. Set 0 to skip CPU metric alerts even when resource IDs are set."
  default     = 0
}

variable "tags" {
  type        = map(string)
  default     = {}
  description = "Tags applied to created resources."
}

variable "enable_managed_grafana" {
  type        = bool
  description = "When true, deploy Azure Managed Grafana (Standard SKU). Requires a clean region quota; leave false and import infra/grafana dashboards into Grafana Cloud or an existing instance."
  default     = false
}

variable "grafana_name" {
  type        = string
  description = "Azure Managed Grafana instance name (DNS segment)."
  default     = "archiforge-grafana"
}

variable "grafana_location" {
  type        = string
  description = "Azure region for Managed Grafana (can differ from Container Apps region)."
  default     = "eastus2"
}

variable "grafana_api_key_enabled" {
  type        = bool
  description = "Allow Grafana API keys on the managed instance (needed for some automation)."
  default     = true
}

variable "grafana_major_version" {
  type        = string
  description = "Azure Managed Grafana major version supported in your region (provider-validated, e.g. 11 or 12)."
  default     = "11"

  validation {
    condition     = contains(["11", "12"], var.grafana_major_version)
    error_message = "grafana_major_version must be a major version supported by the azurerm provider (e.g. 11 or 12)."
  }
}
