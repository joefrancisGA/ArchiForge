locals {
  sql_budget_server_id_trimmed = trimspace(var.primary_sql_server_resource_id)

  # Derives /subscriptions/{sub}/resourceGroups/{rg} from a Microsoft.Sql/servers ARM id when override is unset.
  sql_budget_rg_capture = try(
    regex(
      "^/subscriptions/([0-9a-fA-F-]{36})/resourceGroups/([^/]+)/providers/Microsoft\\.Sql/servers/[^/]+$",
      local.sql_budget_server_id_trimmed
    ),
    []
  )

  sql_budget_rg_id_from_server = length(local.sql_budget_rg_capture) == 2 ? "/subscriptions/${local.sql_budget_rg_capture[0]}/resourceGroups/${local.sql_budget_rg_capture[1]}" : ""

  sql_consumption_budget_resource_group_id = trimspace(var.sql_consumption_budget_resource_group_id) != "" ? trimspace(var.sql_consumption_budget_resource_group_id) : local.sql_budget_rg_id_from_server

  sql_consumption_budget_enabled = var.enable_sql_consumption_budget && length(trimspace(local.sql_consumption_budget_resource_group_id)) > 0
}

resource "azurerm_consumption_budget_resource_group" "sql" {
  count = local.sql_consumption_budget_enabled ? 1 : 0

  name              = var.sql_consumption_budget_name
  resource_group_id = local.sql_consumption_budget_resource_group_id

  amount     = var.sql_consumption_budget_amount
  time_grain = "Monthly"

  time_period {
    start_date = var.sql_consumption_budget_time_period_start
  }

  filter {
    dimension {
      name = "ResourceType"
      values = [
        "Microsoft.Sql/servers",
        "Microsoft.Sql/servers/databases",
      ]
    }
  }

  notification {
    enabled        = true
    threshold      = 80.0
    operator       = "GreaterThan"
    threshold_type = "Actual"
    contact_emails = length(var.sql_consumption_budget_contact_emails) > 0 ? var.sql_consumption_budget_contact_emails : null
    contact_roles  = length(var.sql_consumption_budget_contact_emails) > 0 ? null : var.sql_consumption_budget_contact_roles
  }

  notification {
    enabled        = true
    threshold      = 100.0
    operator       = "GreaterThan"
    threshold_type = "Forecasted"
    contact_emails = length(var.sql_consumption_budget_contact_emails) > 0 ? var.sql_consumption_budget_contact_emails : null
    contact_roles  = length(var.sql_consumption_budget_contact_emails) > 0 ? null : var.sql_consumption_budget_contact_roles
  }
}
