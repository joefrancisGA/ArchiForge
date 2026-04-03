locals {
  enabled = var.enable_container_apps

  subnet_integrated = local.enabled && length(trimspace(var.container_apps_subnet_id)) > 0

  # Single image: publish ArchiForge.Worker (includes Api.dll). Override worker_container_image to use a different tag if needed.
  worker_effective_image = trimspace(var.worker_container_image) != "" ? var.worker_container_image : var.api_container_image
}

data "azurerm_resource_group" "target" {
  count = local.enabled && !var.create_resource_group ? 1 : 0

  name = var.resource_group_name
}

resource "azurerm_resource_group" "this" {
  count = local.enabled && var.create_resource_group ? 1 : 0

  name     = var.resource_group_name
  location = var.location
  tags     = var.tags
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
  tags                = var.tags
}

resource "azurerm_container_app_environment" "main" {
  count = local.enabled ? 1 : 0

  name                       = var.container_app_environment_name
  location                   = local.azure_location
  resource_group_name        = local.resource_group_name
  log_analytics_workspace_id = azurerm_log_analytics_workspace.container_apps[0].id
  tags                       = var.tags

  infrastructure_subnet_id = local.subnet_integrated ? var.container_apps_subnet_id : null

  internal_load_balancer_enabled = local.subnet_integrated && var.container_apps_internal_load_balancer
}

resource "azurerm_container_app" "api" {
  count = local.enabled ? 1 : 0

  name                         = var.api_container_app_name
  container_app_environment_id = azurerm_container_app_environment.main[0].id
  resource_group_name          = local.resource_group_name
  revision_mode                = "Single"

  identity {
    type = "SystemAssigned"
  }

  template {
    min_replicas = var.api_min_replicas
    max_replicas = var.api_max_replicas

    container {
      name   = "archiforge-api"
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

resource "azurerm_container_app" "worker" {
  count = local.enabled ? 1 : 0

  name                         = var.worker_container_app_name
  container_app_environment_id = azurerm_container_app_environment.main[0].id
  resource_group_name          = local.resource_group_name
  revision_mode                = "Single"

  identity {
    type = "SystemAssigned"
  }

  template {
    min_replicas = var.worker_min_replicas
    max_replicas = var.worker_max_replicas

    container {
      name    = "archiforge-worker"
      image   = local.worker_effective_image
      cpu     = var.worker_cpu
      memory  = var.worker_memory
      command = ["dotnet", "ArchiForge.Worker.dll"]

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
  }
}

resource "azurerm_role_assignment" "worker_blob_data_contributor" {
  count = local.enabled ? 1 : 0

  scope                = var.artifact_storage_account_id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azurerm_container_app.worker[0].identity[0].principal_id
}

resource "azurerm_container_app" "ui" {
  count = local.enabled ? 1 : 0

  name                         = var.ui_container_app_name
  container_app_environment_id = azurerm_container_app_environment.main[0].id
  resource_group_name          = local.resource_group_name
  revision_mode                = "Single"

  template {
    min_replicas = var.ui_min_replicas
    max_replicas = var.ui_max_replicas

    container {
      name   = "archiforge-ui"
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
