locals {
  container_apps_consumption_budget_enabled = local.enabled && var.enable_container_apps_consumption_budget

  container_apps_budget_resource_group_id = local.container_apps_consumption_budget_enabled ? (
    var.create_resource_group ? azurerm_resource_group.this[0].id : data.azurerm_resource_group.target[0].id
  ) : ""
}

resource "azurerm_consumption_budget_resource_group" "container_apps" {
  count = local.container_apps_consumption_budget_enabled ? 1 : 0

  name              = var.container_apps_consumption_budget_name
  resource_group_id = local.container_apps_budget_resource_group_id

  amount     = var.container_apps_consumption_budget_amount
  time_grain = "Monthly"

  time_period {
    start_date = var.container_apps_consumption_budget_time_period_start
  }

  filter {
    dimension {
      name = "ResourceType"
      values = [
        "Microsoft.App/containerApps",
        "Microsoft.App/managedEnvironments",
      ]
    }
  }

  notification {
    enabled        = true
    threshold      = 80.0
    operator       = "GreaterThan"
    threshold_type = "Actual"
    contact_emails = length(var.container_apps_consumption_budget_contact_emails) > 0 ? var.container_apps_consumption_budget_contact_emails : null
    contact_roles  = length(var.container_apps_consumption_budget_contact_emails) > 0 ? null : var.container_apps_consumption_budget_contact_roles
  }

  notification {
    enabled        = true
    threshold      = 100.0
    operator       = "GreaterThan"
    threshold_type = "Forecasted"
    contact_emails = length(var.container_apps_consumption_budget_contact_emails) > 0 ? var.container_apps_consumption_budget_contact_emails : null
    contact_roles  = length(var.container_apps_consumption_budget_contact_emails) > 0 ? null : var.container_apps_consumption_budget_contact_roles
  }
}
