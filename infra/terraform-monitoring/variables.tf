variable "enable_monitoring_stack" {
  type        = bool
  description = "When true, create Azure Monitor action group and optional metric alerts. Safe default false for CI and laptops."
  default     = false
}

variable "resource_group_name" {
  type        = string
  description = "Existing resource group that holds ArchLucid Container Apps (or shared monitoring RG)."
  default     = ""
}

variable "name_prefix" {
  type        = string
  description = "Short prefix for alert rule and action group names (alphanumeric, no spaces)."
  default     = "archlucid"
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

variable "enable_application_insights" {
  type        = bool
  description = "When true and application_insights_workspace_resource_id is set, create a workspace-based Application Insights resource (connection string for ArchLucid OpenTelemetry dual export)."
  default     = false
}

variable "application_insights_name" {
  type        = string
  description = "Application Insights resource name (unique within the resource group)."
  default     = "archlucid-appinsights"
}

variable "application_insights_workspace_resource_id" {
  type        = string
  description = "Resource id of the Log Analytics workspace to link (same workspace as Container Apps LAW is typical)."
  default     = ""
}

variable "enable_managed_grafana" {
  type        = bool
  description = "When true, deploy Azure Managed Grafana (Standard SKU). Requires a clean region quota; leave false and import infra/grafana dashboards into Grafana Cloud or an existing instance."
  default     = false
}

variable "grafana_name" {
  type        = string
  description = "Azure Managed Grafana instance name (DNS segment)."
  default     = "archlucid-grafana"
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

variable "grafana_terraform_dashboards_enabled" {
  type        = bool
  description = "When true with enable_managed_grafana, provision committed JSON from infra/grafana via the Grafana Terraform provider (set grafana_url + grafana_auth from the live instance)."
  default     = false
}

variable "grafana_url" {
  type        = string
  description = "Managed Grafana base URL including scheme (copy from terraform output grafana_endpoint after the workspace exists)."
  default     = "https://127.0.0.1:1"
}

variable "grafana_auth" {
  type        = string
  description = "Grafana service account token or basic auth for the Grafana provider. Required when grafana_terraform_dashboards_enabled is true."
  default     = "terraform-validate-placeholder"
  sensitive   = true
}

variable "enable_prometheus_slo_rule_group" {
  type        = bool
  description = "When true with enable_monitoring_stack, deploy azurerm_monitor_alert_prometheus_rule_group for p99 / 5xx / outbox PromQL (requires azure_monitor_workspace_id)."
  default     = false
}

variable "azure_monitor_workspace_id" {
  type        = string
  description = "Full resource ID of the Azure Monitor workspace used as Prometheus rule group scope (Microsoft.Monitor/accounts). Empty skips rule group."
  default     = ""
}
