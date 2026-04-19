# SQL failover / optional consumption budget — Terraform resource labels use `archlucid` naming (greenfield IaC).
# Rename via `terraform state mv` during a planned maintenance window.
# Tracked in docs/ARCHLUCID_RENAME_CHECKLIST.md Phase 7.5.

locals {
  enabled = var.enable_sql_failover_group

  # Server-level automatic tuning (inherits to all databases unless a DB overrides).
  sql_automatic_tuning_primary_eligible = (
    var.enable_sql_automatic_tuning &&
    length(trimspace(var.primary_sql_server_resource_id)) > 0 &&
    !strcontains(var.primary_sql_server_resource_id, "placeholder-primary")
  )

  sql_automatic_tuning_partner_eligible = (
    var.enable_sql_automatic_tuning &&
    length(trimspace(var.partner_sql_server_resource_id)) > 0 &&
    !strcontains(var.partner_sql_server_resource_id, "placeholder-secondary")
  )
}

resource "azurerm_mssql_failover_group" "this" {
  count = local.enabled ? 1 : 0

  name      = var.failover_group_name
  server_id = var.primary_sql_server_resource_id

  partner_server {
    id = var.partner_sql_server_resource_id
  }

  databases = var.database_resource_ids

  read_write_endpoint_failover_policy {
    mode = var.read_write_failover_mode

    grace_minutes = var.read_write_failover_mode == "Automatic" ? var.read_write_grace_minutes : null
  }

  readonly_endpoint_failover_policy_enabled = var.readonly_endpoint_failover_enabled

  tags = var.tags
}
