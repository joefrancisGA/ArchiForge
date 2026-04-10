# NOTE: Resource addresses in this module may still use the historical `archiforge` token to avoid Terraform state disruption.
# Rename via `terraform state mv` during a planned maintenance window.
# Tracked in docs/ARCHLUCID_RENAME_CHECKLIST.md Phase 7.5.

locals {
  apim_enabled = var.enable_api_management

  apim_rg_name = local.apim_enabled ? var.resource_group_name : ""

  apim_location = local.apim_enabled ? (
    var.create_resource_group ? var.location : data.azurerm_resource_group.apim_target[0].location
  ) : ""
}

data "azurerm_resource_group" "apim_target" {
  count = local.apim_enabled && !var.create_resource_group ? 1 : 0
  name  = var.resource_group_name
}

resource "azurerm_resource_group" "apim" {
  count    = local.apim_enabled && var.create_resource_group ? 1 : 0
  name     = var.resource_group_name
  location = var.location
  tags     = var.tags
}
