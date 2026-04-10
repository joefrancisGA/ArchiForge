# NOTE: Resource addresses in this module may still use the historical `archiforge` token to avoid Terraform state disruption.
# Rename via `terraform state mv` during a planned maintenance window.
# Tracked in docs/ARCHLUCID_RENAME_CHECKLIST.md Phase 7.5.

locals {
  enabled = var.enable_sql_failover_group
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
