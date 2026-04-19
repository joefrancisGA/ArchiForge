# Scheduled Container Apps Jobs (ArchLucid.Jobs.Cli --job <slug>).
# Event-triggered (KEDA) jobs: extend this file when adding Service Bus / Cosmos scalers (ADR 0018).

locals {
  scheduled_container_jobs = {
    for k, j in var.container_jobs : k => j
    if j.trigger_type == "Schedule"
  }
}

check "container_jobs_schedule_only" {
  assert {
    condition = length(var.container_jobs) == 0 || alltrue([
      for j in var.container_jobs : j.trigger_type == "Schedule"
    ])
    error_message = "container_jobs: only trigger_type = \"Schedule\" is implemented in this module revision (Event/Manual: extend jobs.tf per ADR 0018)."
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

resource "azurerm_role_assignment" "scheduled_job_blob_data_contributor" {
  for_each = local.enabled && trimspace(var.artifact_storage_account_id) != "" ? local.scheduled_container_jobs : {}

  scope                = var.artifact_storage_account_id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azurerm_container_app_job.scheduled[each.key].identity[0].principal_id
}
