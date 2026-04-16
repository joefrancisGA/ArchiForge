locals {
  application_insights_enabled = var.enable_application_insights && length(trimspace(var.resource_group_name)) > 0 && length(
    trimspace(var.application_insights_workspace_resource_id)
  ) > 0
}

data "azurerm_resource_group" "insights" {
  count = local.application_insights_enabled ? 1 : 0

  name = var.resource_group_name
}

resource "azurerm_application_insights" "archlucid" {
  count = local.application_insights_enabled ? 1 : 0

  name                = var.application_insights_name
  location            = data.azurerm_resource_group.insights[0].location
  resource_group_name = var.resource_group_name
  application_type    = "web"
  workspace_id        = var.application_insights_workspace_resource_id
  retention_in_days   = 90
  sampling_percentage = 100

  tags = var.tags
}
