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

check "sql_consumption_budget_requires_scope" {
  assert {
    condition = !var.enable_sql_consumption_budget || (
      length(trimspace(local.sql_consumption_budget_resource_group_id)) > 0 && (
        length(trimspace(var.sql_consumption_budget_resource_group_id)) > 0 ||
        !strcontains(var.primary_sql_server_resource_id, "placeholder-primary")
      )
    )
    error_message = "With enable_sql_consumption_budget = true, set sql_consumption_budget_resource_group_id to the SQL resource group ARM id, or set primary_sql_server_resource_id to a real Microsoft.Sql/servers id (not the default placeholder) so the group id can be derived."
  }
}

check "sql_consumption_budget_contact_channel" {
  assert {
    condition = !var.enable_sql_consumption_budget || (
      length(var.sql_consumption_budget_contact_emails) > 0 ||
      length(var.sql_consumption_budget_contact_roles) > 0
    )
    error_message = "With enable_sql_consumption_budget = true, set sql_consumption_budget_contact_emails and/or a non-empty sql_consumption_budget_contact_roles list so Azure Cost Management can deliver notifications."
  }
}
