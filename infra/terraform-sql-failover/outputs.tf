output "failover_group_id" {
  description = "Azure resource ID of the failover group when enable_sql_failover_group is true; otherwise null."
  value       = local.enabled ? azurerm_mssql_failover_group.this[0].id : null
}

output "read_write_listener_fqdn" {
  description = "Read/write listener hostname for application connection strings (follows current primary after failover). Format: {failover_group_name}.database.windows.net"
  value       = local.enabled ? "${azurerm_mssql_failover_group.this[0].name}.database.windows.net" : null
}

output "read_only_listener_fqdn" {
  description = "Read-only listener hostname for routing read-heavy queries to the failover group secondary. Format: {failover_group_name}.secondary.database.windows.net"
  value       = local.enabled ? "${azurerm_mssql_failover_group.this[0].name}.secondary.database.windows.net" : null
}

output "failover_group_name" {
  description = "Failover group name (same as listener label before .database.windows.net) when enabled; otherwise null."
  value       = local.enabled ? azurerm_mssql_failover_group.this[0].name : null
}

output "primary_sql_server_resource_id" {
  description = "Primary server resource ID when the failover group is enabled; otherwise null (avoids leaking placeholder defaults)."
  value       = local.enabled ? var.primary_sql_server_resource_id : null
}

output "partner_sql_server_resource_id" {
  description = "Partner server resource ID when the failover group is enabled; otherwise null."
  value       = local.enabled ? var.partner_sql_server_resource_id : null
}
