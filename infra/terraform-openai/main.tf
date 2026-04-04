locals {
  openai_consumption_budget_enabled = var.enable_openai_consumption_budget && length(trimspace(var.openai_consumption_budget_resource_group_id)) > 0

  openai_budget_filter_dimension_name = length(var.openai_consumption_budget_account_resource_ids) > 0 ? "ResourceId" : "ResourceType"

  openai_budget_filter_values = length(var.openai_consumption_budget_account_resource_ids) > 0 ? var.openai_consumption_budget_account_resource_ids : [
    "Microsoft.CognitiveServices/accounts",
  ]
}

resource "azurerm_consumption_budget_resource_group" "openai" {
  count = local.openai_consumption_budget_enabled ? 1 : 0

  name              = var.openai_consumption_budget_name
  resource_group_id = trimspace(var.openai_consumption_budget_resource_group_id)

  amount     = var.openai_consumption_budget_amount
  time_grain = "Monthly"

  time_period {
    start_date = var.openai_consumption_budget_time_period_start
  }

  filter {
    dimension {
      name   = local.openai_budget_filter_dimension_name
      values = local.openai_budget_filter_values
    }
  }

  notification {
    enabled        = true
    threshold      = 80.0
    operator       = "GreaterThan"
    threshold_type = "Actual"
    contact_emails = length(var.openai_consumption_budget_contact_emails) > 0 ? var.openai_consumption_budget_contact_emails : null
    contact_roles  = length(var.openai_consumption_budget_contact_emails) > 0 ? null : var.openai_consumption_budget_contact_roles
  }

  notification {
    enabled        = true
    threshold      = 100.0
    operator       = "GreaterThan"
    threshold_type = "Forecasted"
    contact_emails = length(var.openai_consumption_budget_contact_emails) > 0 ? var.openai_consumption_budget_contact_emails : null
    contact_roles  = length(var.openai_consumption_budget_contact_emails) > 0 ? null : var.openai_consumption_budget_contact_roles
  }
}
