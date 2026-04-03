variable "enable_container_apps" {
  type        = bool
  description = "When true, deploy Log Analytics, Container Apps Environment, and API + Operator UI container apps. Keep false on laptops until you are targeting Azure."
  default     = false
}

variable "create_resource_group" {
  type        = bool
  description = "When true and enable_container_apps is true, create the resource group."
  default     = false
}

variable "resource_group_name" {
  type        = string
  description = "Resource group for Container Apps, Log Analytics, and related resources."
  default     = ""
}

variable "location" {
  type        = string
  description = "Azure region (required when create_resource_group = true)."
  default     = ""
}

variable "tags" {
  type        = map(string)
  default     = {}
  description = "Tags applied to created resources."
}

variable "log_analytics_workspace_name" {
  type        = string
  description = "Log Analytics workspace name (unique within the resource group)."
  default     = "law-archiforge-ca"
}

variable "container_app_environment_name" {
  type        = string
  description = "Container Apps managed environment name."
  default     = "cae-archiforge"
}

variable "container_apps_subnet_id" {
  type        = string
  description = "Optional subnet ID for VNet-integrated Container Apps Environment (Microsoft.App/environments delegation). Leave empty for a public-only environment endpoint."
  default     = ""
}

variable "container_apps_internal_load_balancer" {
  type        = bool
  description = "When true and container_apps_subnet_id is set, use an internal load balancer (no public environment ingress). Requires private DNS or a jump host to reach apps."
  default     = false
}

variable "api_container_app_name" {
  type        = string
  description = "Name of the API container app (must be DNS-compliant, lowercase alphanumeric and hyphens)."
  default     = "archiforge-api"
}

variable "ui_container_app_name" {
  type        = string
  description = "Name of the Operator UI container app."
  default     = "archiforge-ui"
}

variable "api_container_image" {
  type        = string
  description = "Full image reference for ArchiForge.Api (e.g. myregistry.azurecr.io/archiforge-api:2026.04.1). Required when enable_container_apps = true."
  default     = ""
}

variable "ui_container_image" {
  type        = string
  description = "Full image reference for archiforge-ui. Required when enable_container_apps = true."
  default     = ""
}

variable "api_min_replicas" {
  type        = number
  description = "Minimum API replicas. Default 2 for staging/production availability (two instances). Override to 1 for local pilots if duplicate hosted background jobs are unacceptable until leader election or a worker app exists (see README)."
  default     = 2
}

variable "api_max_replicas" {
  type        = number
  description = "Maximum API replicas."
  default     = 5
}

variable "ui_min_replicas" {
  type        = number
  description = "Minimum Operator UI replicas."
  default     = 1
}

variable "ui_max_replicas" {
  type        = number
  description = "Maximum Operator UI replicas."
  default     = 3
}

variable "api_scale_concurrent_requests" {
  type        = number
  description = "HTTP scale rule: target concurrent requests per replica before scaling out."
  default     = 10
}

variable "ui_scale_concurrent_requests" {
  type        = number
  description = "HTTP scale rule for the UI container app."
  default     = 20
}

variable "api_cpu" {
  type        = number
  description = "API container vCPU (consumption: 0.25, 0.5, 0.75, 1.0, ...)."
  default     = 0.5
}

variable "api_memory" {
  type        = string
  description = "API container memory (e.g. 1.0Gi)."
  default     = "1.0Gi"
}

variable "ui_cpu" {
  type        = number
  description = "UI container vCPU."
  default     = 0.25
}

variable "ui_memory" {
  type        = string
  description = "UI container memory."
  default     = "0.5Gi"
}

variable "api_ingress_external" {
  type        = bool
  description = "When true, API ingress allows external (internet) access, subject to environment internal LB."
  default     = true
}

variable "ui_ingress_external" {
  type        = bool
  description = "When true, UI ingress allows external access."
  default     = true
}

variable "artifact_blob_service_uri" {
  type        = string
  description = "Blob service URL for large artifact offload (maps to ArtifactLargePayload__AzureBlobServiceUri), e.g. output primary_blob_endpoint from infra/terraform-storage."
  default     = ""
}

variable "artifact_storage_account_id" {
  type        = string
  description = "Resource ID of the storage account that holds golden-manifests / artifact-bundles / artifact-contents containers. Used to grant the API container app Storage Blob Data Contributor."
  default     = ""
}
