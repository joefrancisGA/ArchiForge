# Container Apps Jobs (ArchLucid.Jobs.Cli --job <slug>).
# Schedule = cron; Event = KEDA-backed scale rules (Service Bus, Cosmos, storage queue, etc.).

locals {
  scheduled_container_jobs = {
    for k, j in var.container_jobs : k => j
    if j.trigger_type == "Schedule"
  }

  event_container_jobs = {
    for k, j in var.container_jobs : k => j
    if j.trigger_type == "Event"
  }
}

check "container_jobs_trigger_types" {
  assert {
    condition = length(var.container_jobs) == 0 || alltrue([
      for j in var.container_jobs : contains(["Schedule", "Event"], j.trigger_type)
    ])
    error_message = "container_jobs: trigger_type must be \"Schedule\" or \"Event\"."
  }
}

check "container_jobs_cron_required_for_schedule" {
  assert {
    condition = length(local.scheduled_container_jobs) == 0 || alltrue([
      for j in local.scheduled_container_jobs : j.cron_expression != null && trimspace(j.cron_expression) != ""
    ])
    error_message = "container_jobs: cron_expression is required when trigger_type is Schedule."
  }
}

check "container_jobs_event_rules_required" {
  assert {
    condition = length(local.event_container_jobs) == 0 || alltrue([
      for j in local.event_container_jobs : length(coalesce(j.event_scale_rules, [])) > 0
    ])
    error_message = "container_jobs: Event jobs require a non-empty event_scale_rules list (KEDA metadata + custom_rule_type)."
  }
}

resource "azurerm_container_app_job" "scheduled" {
  for_each = local.enabled ? local.scheduled_container_jobs : {}

  name                         = each.key
  location                     = local.azure_location
  resource_group_name          = local.resource_group_name
  container_app_environment_id = azurerm_container_app_environment.main[0].id
  tags                         = local.merged_tags

  replica_timeout_in_seconds = coalesce(each.value.replica_timeout_in_seconds, 1800)
  replica_retry_limit        = coalesce(each.value.replica_retry_limit, 1)

  identity {
    type = "SystemAssigned"
  }

  schedule_trigger_config {
    cron_expression          = each.value.cron_expression
    parallelism              = coalesce(each.value.parallelism, 1)
    replica_completion_count = coalesce(each.value.replica_completion_count, 1)
  }

  template {
    container {
      name   = "job"
      image  = local.worker_effective_image
      cpu    = coalesce(each.value.cpu, 0.25)
      memory = coalesce(each.value.memory, "0.5Gi")

      command = length(coalesce(each.value.command, [])) > 0 ? each.value.command : ["dotnet", "ArchLucid.Jobs.Cli.dll"]
      args    = each.value.args

      dynamic "env" {
        for_each = coalesce(each.value.env, {})
        content {
          name  = env.key
          value = env.value
        }
      }
    }
  }
}

resource "azurerm_container_app_job" "event_driven" {
  for_each = local.enabled ? local.event_container_jobs : {}

  name                         = each.key
  location                     = local.azure_location
  resource_group_name          = local.resource_group_name
  container_app_environment_id = azurerm_container_app_environment.main[0].id
  tags                         = local.merged_tags

  replica_timeout_in_seconds = coalesce(each.value.replica_timeout_in_seconds, 1800)
  replica_retry_limit        = coalesce(each.value.replica_retry_limit, 1)

  identity {
    type = "SystemAssigned"
  }

  event_trigger_config {
    parallelism              = coalesce(each.value.parallelism, 1)
    replica_completion_count = coalesce(each.value.replica_completion_count, 1)

    scale {
      polling_interval_in_seconds = coalesce(each.value.event_polling_interval_seconds, 30)
      max_executions              = coalesce(each.value.event_max_executions, 10)
      min_executions              = coalesce(each.value.event_min_executions, 0)

      dynamic "rules" {
        for_each = coalesce(each.value.event_scale_rules, [])
        content {
          name             = rules.value.name
          custom_rule_type = rules.value.custom_rule_type
          metadata         = rules.value.metadata

          dynamic "authentication" {
            for_each = coalesce(rules.value.auth, [])
            content {
              secret_name       = authentication.value.secret_name
              trigger_parameter = authentication.value.trigger_parameter
            }
          }
        }
      }
    }
  }

  template {
    container {
      name   = "job"
      image  = local.worker_effective_image
      cpu    = coalesce(each.value.cpu, 0.25)
      memory = coalesce(each.value.memory, "0.5Gi")

      command = length(coalesce(each.value.command, [])) > 0 ? each.value.command : ["dotnet", "ArchLucid.Jobs.Cli.dll"]
      args    = each.value.args

      dynamic "env" {
        for_each = coalesce(each.value.env, {})
        content {
          name  = env.key
          value = env.value
        }
      }
    }
  }
}

resource "azurerm_role_assignment" "scheduled_job_blob_data_contributor" {
  for_each = local.enabled && trimspace(var.artifact_storage_account_id) != "" ? local.scheduled_container_jobs : {}

  scope                = var.artifact_storage_account_id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azurerm_container_app_job.scheduled[each.key].identity[0].principal_id
}

resource "azurerm_role_assignment" "event_job_blob_data_contributor" {
  for_each = local.enabled && trimspace(var.artifact_storage_account_id) != "" ? local.event_container_jobs : {}

  scope                = var.artifact_storage_account_id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azurerm_container_app_job.event_driven[each.key].identity[0].principal_id
}
