# NOTE: Resource addresses in this module may still use the historical `archiforge` token to avoid Terraform state disruption.
# Rename via `terraform state mv` during a planned maintenance window.
# Tracked in docs/ARCHLUCID_RENAME_CHECKLIST.md Phase 7.5.

locals {
  enabled = var.enable_container_apps

  # FinOps: merge application tag + caller tags + optional standard keys (see variables finops_*).
  merged_tags = merge(
    { Application = "ArchLucid" },
    var.tags,
    length(trimspace(var.finops_environment)) > 0 ? { Environment = trimspace(var.finops_environment) } : {},
    length(trimspace(var.finops_cost_center)) > 0 ? { CostCenter = trimspace(var.finops_cost_center) } : {}
  )

  subnet_integrated = local.enabled && length(trimspace(var.container_apps_subnet_id)) > 0

  # Single image: publish ArchLucid.Api + ArchLucid.Worker into /app. Override worker_container_image to use a different tag if needed.
  worker_effective_image = trimspace(var.worker_container_image) != "" ? var.worker_container_image : var.api_container_image

  background_jobs_durable = local.enabled && var.background_jobs_mode == "Durable"

  # KEDA-style azure-queue scale rule (Container Apps): requires a storage connection string secret (see variables).
  worker_queue_scale_enabled = local.background_jobs_durable && var.worker_enable_queue_depth_scaling && length(
    trimspace(var.worker_queue_scale_connection_string)
  ) > 0

  # Parse storage account name from blob endpoint (https://{acct}.blob.core.windows.net) for queue resource + RBAC scope alignment.
  artifact_storage_account_name_from_blob = local.enabled && length(trimspace(var.artifact_blob_service_uri)) > 0 && can(
    regex("^https://([^.]+)\\.blob\\.core\\.windows\\.net/?$", var.artifact_blob_service_uri)
  ) ? regex("^https://([^.]+)\\.blob\\.core\\.windows\\.net/?$", var.artifact_blob_service_uri)[0] : ""
}

data "azurerm_resource_group" "target" {
  count = local.enabled && !var.create_resource_group ? 1 : 0

  name = var.resource_group_name
}

resource "azurerm_resource_group" "this" {
  count = local.enabled && var.create_resource_group ? 1 : 0

  name     = var.resource_group_name
  location = var.location
  tags     = local.merged_tags
}

locals {
  resource_group_name = !local.enabled ? "" : (
    var.create_resource_group ? azurerm_resource_group.this[0].name : data.azurerm_resource_group.target[0].name
  )

  azure_location = !local.enabled ? "" : (
    var.create_resource_group ? var.location : data.azurerm_resource_group.target[0].location
  )
}

resource "azurerm_log_analytics_workspace" "container_apps" {
  count = local.enabled ? 1 : 0

  name                = var.log_analytics_workspace_name
  location            = local.azure_location
  resource_group_name = local.resource_group_name
  sku                 = "PerGB2018"
  retention_in_days   = 30
  daily_quota_gb      = var.log_analytics_daily_quota_gb > 0 ? var.log_analytics_daily_quota_gb : null
  tags                = var.tags
}

resource "azurerm_container_app_environment" "main" {
  count = local.enabled ? 1 : 0

  name                       = var.container_app_environment_name
  location                   = local.azure_location
  resource_group_name        = local.resource_group_name
  log_analytics_workspace_id = azurerm_log_analytics_workspace.container_apps[0].id
  tags                       = local.merged_tags

  infrastructure_subnet_id = local.subnet_integrated ? var.container_apps_subnet_id : null

  internal_load_balancer_enabled = local.subnet_integrated && var.container_apps_internal_load_balancer
}

resource "azurerm_container_app" "api" {
  count = local.enabled ? 1 : 0

  name                         = var.api_container_app_name
  container_app_environment_id = azurerm_container_app_environment.main[0].id
  resource_group_name          = local.resource_group_name
  revision_mode                = "Single"
  tags                         = local.merged_tags

  identity {
    type = "SystemAssigned"
  }

  template {
    min_replicas = var.api_min_replicas
    max_replicas = var.api_max_replicas

    container {
      name   = "archlucid-api"
      image  = var.api_container_image
      cpu    = var.api_cpu
      memory = var.api_memory

      env {
        name  = "ASPNETCORE_URLS"
        value = "http://0.0.0.0:8080"
      }

      env {
        name  = "Hosting__Role"
        value = "Api"
      }

      env {
        name  = "ArtifactLargePayload__Enabled"
        value = "true"
      }

      env {
        name  = "ArtifactLargePayload__BlobProvider"
        value = "AzureBlob"
      }

      env {
        name  = "ArtifactLargePayload__AzureBlobServiceUri"
        value = var.artifact_blob_service_uri
      }

      dynamic "env" {
        for_each = local.background_jobs_durable ? [1] : []
        content {
          name  = "BackgroundJobs__Mode"
          value = "Durable"
        }
      }

      dynamic "env" {
        for_each = local.background_jobs_durable ? [1] : []
        content {
          name  = "BackgroundJobs__QueueName"
          value = var.background_jobs_queue_name
        }
      }

      dynamic "env" {
        for_each = local.background_jobs_durable ? [1] : []
        content {
          name  = "BackgroundJobs__ResultsContainerName"
          value = var.background_jobs_results_container
        }
      }

      liveness_probe {
        transport = "HTTP"
        port      = 8080
        path      = "/health/live"
      }

      readiness_probe {
        transport = "HTTP"
        port      = 8080
        path      = "/health/ready"
      }
    }

    http_scale_rule {
      name                = "http-concurrency"
      concurrent_requests = var.api_scale_concurrent_requests
    }
  }

  ingress {
    allow_insecure_connections = false
    external_enabled           = var.api_ingress_external
    target_port                = 8080
    transport                  = "auto"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }
}

resource "azurerm_role_assignment" "api_blob_data_contributor" {
  count = local.enabled ? 1 : 0

  scope                = var.artifact_storage_account_id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azurerm_container_app.api[0].identity[0].principal_id
}

resource "azurerm_storage_queue" "background_jobs" {
  count = local.background_jobs_durable && local.artifact_storage_account_name_from_blob != "" ? 1 : 0

  name                 = var.background_jobs_queue_name
  storage_account_name = local.artifact_storage_account_name_from_blob
}

resource "azurerm_role_assignment" "api_queue_data_message_sender" {
  count = local.background_jobs_durable && trimspace(var.artifact_storage_account_id) != "" ? 1 : 0

  scope                = var.artifact_storage_account_id
  role_definition_name = "Storage Queue Data Message Sender"
  principal_id         = azurerm_container_app.api[0].identity[0].principal_id
}

resource "azurerm_container_app" "worker" {
  count = local.enabled ? 1 : 0

  name                         = var.worker_container_app_name
  container_app_environment_id = azurerm_container_app_environment.main[0].id
  resource_group_name          = local.resource_group_name
  revision_mode                = "Single"
  tags                         = local.merged_tags

  dynamic "secret" {
    for_each = local.worker_queue_scale_enabled ? [1] : []
    content {
      name  = "queue-scale-connection"
      value = var.worker_queue_scale_connection_string
    }
  }

  identity {
    type = "SystemAssigned"
  }

  template {
    min_replicas = var.worker_min_replicas
    max_replicas = var.worker_max_replicas

    container {
      name    = "archlucid-worker"
      image   = local.worker_effective_image
      cpu     = var.worker_cpu
      memory  = var.worker_memory
      command = ["dotnet", "ArchLucid.Worker.dll"]

      env {
        name  = "ASPNETCORE_URLS"
        value = "http://0.0.0.0:8080"
      }

      env {
        name  = "Hosting__Role"
        value = "Worker"
      }

      env {
        name  = "ArtifactLargePayload__Enabled"
        value = "true"
      }

      env {
        name  = "ArtifactLargePayload__BlobProvider"
        value = "AzureBlob"
      }

      env {
        name  = "ArtifactLargePayload__AzureBlobServiceUri"
        value = var.artifact_blob_service_uri
      }

      dynamic "env" {
        for_each = local.background_jobs_durable ? [1] : []
        content {
          name  = "BackgroundJobs__Mode"
          value = "Durable"
        }
      }

      dynamic "env" {
        for_each = local.background_jobs_durable ? [1] : []
        content {
          name  = "BackgroundJobs__QueueName"
          value = var.background_jobs_queue_name
        }
      }

      dynamic "env" {
        for_each = local.background_jobs_durable ? [1] : []
        content {
          name  = "BackgroundJobs__ResultsContainerName"
          value = var.background_jobs_results_container
        }
      }

      liveness_probe {
        transport = "HTTP"
        port      = 8080
        path      = "/health/live"
      }

      readiness_probe {
        transport = "HTTP"
        port      = 8080
        path      = "/health/ready"
      }
    }

    dynamic "custom_scale_rule" {
      for_each = local.worker_queue_scale_enabled ? [1] : []
      content {
        name             = "background-jobs-queue-depth"
        custom_rule_type = "azure-queue"
        metadata = {
          queueName   = var.background_jobs_queue_name
          queueLength = tostring(var.worker_queue_depth_target_messages_per_revision)
        }

        authentication {
          secret_name       = "queue-scale-connection"
          trigger_parameter = "connection"
        }
      }
    }
  }
}

resource "azurerm_role_assignment" "worker_blob_data_contributor" {
  count = local.enabled ? 1 : 0

  scope                = var.artifact_storage_account_id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azurerm_container_app.worker[0].identity[0].principal_id
}

resource "azurerm_role_assignment" "worker_queue_data_message_processor" {
  count = local.background_jobs_durable && trimspace(var.artifact_storage_account_id) != "" ? 1 : 0

  scope                = var.artifact_storage_account_id
  role_definition_name = "Storage Queue Data Message Processor"
  principal_id         = azurerm_container_app.worker[0].identity[0].principal_id
}

resource "azurerm_container_app" "ui" {
  count = local.enabled ? 1 : 0

  name                         = var.ui_container_app_name
  container_app_environment_id = azurerm_container_app_environment.main[0].id
  resource_group_name          = local.resource_group_name
  revision_mode                = "Single"
  tags                         = local.merged_tags

  template {
    min_replicas = var.ui_min_replicas
    max_replicas = var.ui_max_replicas

    container {
      name   = "archlucid-ui"
      image  = var.ui_container_image
      cpu    = var.ui_cpu
      memory = var.ui_memory

      env {
        name  = "PORT"
        value = "3000"
      }

      env {
        name  = "HOSTNAME"
        value = "0.0.0.0"
      }

      liveness_probe {
        transport = "HTTP"
        port      = 3000
        path      = "/"
      }

      readiness_probe {
        transport = "HTTP"
        port      = 3000
        path      = "/"
      }
    }

    http_scale_rule {
      name                = "http-concurrency"
      concurrent_requests = var.ui_scale_concurrent_requests
    }
  }

  ingress {
    allow_insecure_connections = false
    external_enabled           = var.ui_ingress_external
    target_port                = 3000
    transport                  = "auto"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }
}
