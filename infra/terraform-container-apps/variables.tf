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

variable "finops_environment" {
  type        = string
  default     = ""
  description = "Optional Environment tag value for cost allocation (merged into resource tags when non-empty)."
}

variable "finops_cost_center" {
  type        = string
  default     = ""
  description = "Optional CostCenter tag value for chargeback (merged into resource tags when non-empty)."
}

variable "log_analytics_workspace_name" {
  type        = string
  description = "Log Analytics workspace name (unique within the resource group)."
  default     = "law-archlucid-ca"
}

variable "log_analytics_daily_quota_gb" {
  type        = number
  description = "Daily cap on Log Analytics ingestion (GB). Use 0 to omit (Azure default / no Terraform-enforced cap). Set 1–10 for FinOps guardrails in shared environments."
  default     = 0
}

variable "container_app_environment_name" {
  type        = string
  description = "Container Apps managed environment name."
  default     = "cae-archlucid"
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
  default     = "archlucid-api"
}

variable "ui_container_app_name" {
  type        = string
  description = "Name of the Operator UI container app."
  default     = "archlucid-ui"
}

variable "worker_container_app_name" {
  type        = string
  description = "Name of the background worker container app (advisory scan, archival, retrieval outbox)."
  default     = "archlucid-worker"
}

variable "worker_container_image" {
  type        = string
  description = "Image for ArchLucid.Worker (must include ArchLucid.Worker.dll; same build as API is typical). Leave empty to reuse api_container_image."
  default     = ""
}

variable "worker_min_replicas" {
  type        = number
  description = "Minimum worker replicas (use 1 so hosted background loops run in a single instance)."
  default     = 1
}

variable "worker_max_replicas" {
  type        = number
  description = "Maximum worker replicas. Raise when using durable background jobs + queue-depth scaling; workers coordinate via SQL row locks and batch dequeue."
  default     = 20
}

variable "worker_cpu" {
  type        = number
  description = "Worker container vCPU."
  default     = 0.25
}

variable "worker_memory" {
  type        = string
  description = "Worker container memory."
  default     = "0.5Gi"
}

variable "api_container_image" {
  type        = string
  description = "Full image reference for the API container (default entrypoint ArchLucid.Api.dll), e.g. myregistry.azurecr.io/archlucid-api:2026.04.1. Required when enable_container_apps = true."
  default     = ""
}

variable "ui_container_image" {
  type        = string
  description = "Full image reference for archlucid-ui. Required when enable_container_apps = true."
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

variable "background_jobs_mode" {
  type        = string
  description = "InMemory keeps export jobs in the API process. Durable uses SQL + Azure Storage Queue + the worker container app (requires Sql storage, AzureBlob artifacts, and queue RBAC)."
  default     = "InMemory"

  validation {
    condition     = contains(["InMemory", "Durable"], var.background_jobs_mode)
    error_message = "background_jobs_mode must be InMemory or Durable."
  }
}

variable "background_jobs_queue_name" {
  type        = string
  description = "Azure Storage Queue name for durable export jobs (lowercase alphanumeric and hyphens, 3–63 chars)."
  default     = "archlucid-export-jobs"
}

variable "background_jobs_results_container" {
  type        = string
  description = "Blob container for completed export job binaries (created on first upload if missing)."
  default     = "background-job-results"
}

variable "worker_enable_queue_depth_scaling" {
  type        = bool
  description = "When true and background_jobs_mode is Durable, add an azure-queue custom scale rule (KEDA) to the worker. Requires worker_queue_scale_connection_string (storage connection string used only as a Container App secret for the scaler)."
  default     = false
}

variable "worker_queue_scale_connection_string" {
  type        = string
  description = "Azure Storage connection string for the queue scaler (same account as artifact_blob_service_uri). Sensitive; leave empty to omit queue scaling. Prefer Key Vault references at the deployment layer rather than committing this value."
  default     = ""
  sensitive   = true
}

variable "worker_queue_depth_target_messages_per_revision" {
  type        = number
  description = "KEDA azure-queue rule: approximate messages per worker revision before scaling out (queue length threshold)."
  default     = 10

  validation {
    condition     = var.worker_queue_depth_target_messages_per_revision >= 1
    error_message = "worker_queue_depth_target_messages_per_revision must be at least 1."
  }
}

variable "enable_container_apps_consumption_budget" {
  type        = bool
  description = "When true and enable_container_apps is true, create an azurerm_consumption_budget_resource_group filtered to Microsoft.App/containerApps and managedEnvironments in the stack resource group."
  default     = false
}

variable "container_apps_consumption_budget_name" {
  type        = string
  description = "Budget name (unique within the resource group scope in Cost Management)."
  default     = "archlucid-container-apps-monthly"

  validation {
    condition     = length(var.container_apps_consumption_budget_name) >= 1 && length(var.container_apps_consumption_budget_name) <= 63
    error_message = "container_apps_consumption_budget_name must be 1-63 characters."
  }
}

variable "container_apps_consumption_budget_amount" {
  type        = number
  description = "Monthly budget amount in the subscription billing currency (e.g. USD)."
  default     = 500

  validation {
    condition     = var.container_apps_consumption_budget_amount > 0
    error_message = "container_apps_consumption_budget_amount must be positive."
  }
}

variable "container_apps_consumption_budget_time_period_start" {
  type        = string
  description = "Budget period start (RFC3339, first day of a month UTC). Azure requires month boundaries."
  default     = "2026-01-01T00:00:00Z"
}

variable "container_apps_consumption_budget_contact_emails" {
  type        = list(string)
  description = "Email addresses for budget alerts. When empty, contact_roles is used instead."
  default     = []
}

variable "container_apps_consumption_budget_contact_roles" {
  type        = list(string)
  description = "RBAC roles to notify when container_apps_consumption_budget_contact_emails is empty."
  default     = ["Owner"]
}

variable "api_revision_mode" {
  type        = string
  description = "Container Apps revision mode for the API app. Use Multiple to enable weighted traffic between active revisions (canary / blue-green)."
  default     = "Single"

  validation {
    condition     = contains(["Single", "Multiple"], var.api_revision_mode)
    error_message = "api_revision_mode must be Single or Multiple."
  }
}

variable "worker_revision_mode" {
  type        = string
  description = "Container Apps revision mode for the worker app."
  default     = "Single"

  validation {
    condition     = contains(["Single", "Multiple"], var.worker_revision_mode)
    error_message = "worker_revision_mode must be Single or Multiple."
  }
}

variable "ui_revision_mode" {
  type        = string
  description = "Container Apps revision mode for the operator UI app."
  default     = "Single"

  validation {
    condition     = contains(["Single", "Multiple"], var.ui_revision_mode)
    error_message = "ui_revision_mode must be Single or Multiple."
  }
}

variable "secondary_region_stack_enabled" {
  type        = bool
  description = "When true (and enable_container_apps), deploy a mirrored Container Apps stack in secondary_location for active-secondary / DR. Requires a separate resource group in that Azure region."
  default     = false
}

variable "secondary_create_resource_group" {
  type        = bool
  description = "When true with secondary_region_stack_enabled, create secondary_resource_group_name in secondary_location."
  default     = false
}

variable "secondary_resource_group_name" {
  type        = string
  description = "Resource group name in the secondary Azure region (must not be the primary resource group)."
  default     = ""
}

variable "secondary_location" {
  type        = string
  description = "Azure region for the secondary stack (paired region recommended)."
  default     = ""
}

variable "secondary_log_analytics_workspace_name" {
  type        = string
  description = "Log Analytics workspace name for the secondary Container Apps environment."
  default     = "law-archlucid-secondary"
}

variable "secondary_container_app_environment_name" {
  type        = string
  description = "Managed environment name for secondary region."
  default     = "cae-archlucid-secondary"
}

variable "secondary_container_apps_subnet_id" {
  type        = string
  description = "Optional subnet in the secondary region for VNet-integrated secondary CAE."
  default     = ""
}

variable "secondary_container_apps_internal_load_balancer" {
  type        = bool
  description = "When true and secondary_container_apps_subnet_id is set, internal LB for secondary CAE."
  default     = false
}

variable "secondary_api_container_app_name" {
  type        = string
  description = "Secondary region API container app name."
  default     = "archlucid-api-secondary"
}

variable "secondary_worker_container_app_name" {
  type        = string
  description = "Secondary region worker container app name."
  default     = "archlucid-worker-secondary"
}

variable "secondary_ui_container_app_name" {
  type        = string
  description = "Secondary region UI container app name."
  default     = "archlucid-ui-secondary"
}

variable "secondary_api_min_replicas" {
  type        = number
  description = "Minimum API replicas in the secondary region."
  default     = 1
}

variable "secondary_api_max_replicas" {
  type        = number
  description = "Maximum API replicas in the secondary region."
  default     = 5
}

variable "secondary_worker_min_replicas" {
  type        = number
  description = "Minimum worker replicas in the secondary region."
  default     = 1
}

variable "secondary_worker_max_replicas" {
  type        = number
  description = "Maximum worker replicas in the secondary region."
  default     = 10
}

variable "secondary_ui_min_replicas" {
  type        = number
  description = "Minimum UI replicas in the secondary region."
  default     = 1
}

variable "secondary_ui_max_replicas" {
  type        = number
  description = "Maximum UI replicas in the secondary region."
  default     = 3
}

variable "secondary_read_replica_connection_string" {
  type        = string
  description = "Optional SQL read-only connection string for secondary API (e.g. failover group read listener). Prefer Key Vault references at deploy time; value is sensitive in Terraform state."
  default     = ""
  sensitive   = true
}

variable "container_jobs" {
  type = map(object({
    trigger_type               = string
    cron_expression            = optional(string)
    cpu                        = optional(number, 0.25)
    memory                     = optional(string, "0.5Gi")
    command                    = optional(list(string), [])
    args                       = list(string)
    replica_timeout_in_seconds = optional(number, 1800)
    replica_retry_limit        = optional(number, 1)
    parallelism                = optional(number, 1)
    replica_completion_count   = optional(number, 1)
    env                        = optional(map(string), {})
  }))
  description = "Scheduled Container Apps Jobs (Schedule trigger only). Image defaults to worker_effective_image; entrypoint runs ArchLucid.Jobs.Cli.dll."
  default     = {}
}
