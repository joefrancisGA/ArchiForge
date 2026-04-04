check "sql_failover_required_inputs" {
  assert {
    condition = !var.enable_sql_failover_group || (
      length(trimspace(var.primary_sql_server_resource_id)) > 0 &&
      length(trimspace(var.partner_sql_server_resource_id)) > 0 &&
      length(var.database_resource_ids) > 0
    )
    error_message = "With enable_sql_failover_group = true, set primary_sql_server_resource_id, partner_sql_server_resource_id, and a non-empty database_resource_ids list (primary database resource IDs)."
  }
}

check "sql_failover_placeholder_ids" {
  assert {
    condition = !var.enable_sql_failover_group || (
      !strcontains(var.primary_sql_server_resource_id, "placeholder-primary") &&
      !strcontains(var.partner_sql_server_resource_id, "placeholder-secondary")
    )
    error_message = "With enable_sql_failover_group = true, replace default placeholder SQL server resource IDs with real Microsoft.Sql/servers IDs."
  }
}

check "sql_failover_automatic_uses_grace" {
  assert {
    condition     = !var.enable_sql_failover_group || var.read_write_failover_mode != "Automatic" || var.read_write_grace_minutes >= 60
    error_message = "Automatic read/write failover requires read_write_grace_minutes >= 60."
  }
}
