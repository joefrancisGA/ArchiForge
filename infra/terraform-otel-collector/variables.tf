variable "enable_otel_deployment" {
  type        = bool
  default     = false
  description = "When true, create the collector Container App. Keep false in CI/validate until an Application Insights connection string and Container Apps environment id are available."
}

variable "resource_group_name" {
  description = "Existing resource group hosting the Container Apps environment."
  type        = string
}

variable "location" {
  description = "Azure region; should match the API region to avoid cross-region trace egress."
  type        = string
}

variable "container_apps_environment_id" {
  description = "Resource ID of the Container Apps environment from infra/terraform-container-apps."
  type        = string
}

variable "application_insights_connection_string" {
  description = "Application Insights connection string from infra/terraform-monitoring (sensitive)."
  type        = string
  sensitive   = true
}

variable "tail_sampling_default_ratio" {
  description = "Head-based sampling ratio for traces that do not match an always-keep rule."
  type        = number
  default     = 0.10

  validation {
    condition     = var.tail_sampling_default_ratio >= 0 && var.tail_sampling_default_ratio <= 1
    error_message = "tail_sampling_default_ratio must be between 0 and 1."
  }
}

variable "tail_sampling_always_keep_activity_sources" {
  description = "ActivitySource names whose traces are always retained at 100% (regardless of sampling ratio)."
  type        = list(string)
  default = [
    "ArchLucid.AuthorityRun",
    "ArchLucid.Agent.LlmCompletion",
  ]
}

variable "tail_sampling_min_root_duration_ms" {
  description = "Root-span wall-clock minimum (ms) above which a trace is always retained."
  type        = number
  default     = 2000
}

variable "otel_ingress_external_enabled" {
  type        = bool
  default     = false
  description = "When false, the collector ingress is internal-only to the environment (VNet-integrated dev clusters). Set true for dev-only or when Front Door is not required."
}
