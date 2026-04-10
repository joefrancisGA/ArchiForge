# NOTE: Resource addresses in this file use the historical `archiforge` token to avoid Terraform state disruption.
# Rename via `terraform state mv` during a planned maintenance window.
# Tracked in docs/ARCHLUCID_RENAME_CHECKLIST.md Phase 7.5.

locals {
  apim_resource_group_name = local.apim_enabled ? (
    var.create_resource_group ? azurerm_resource_group.apim[0].name : data.azurerm_resource_group.apim_target[0].name
  ) : ""

  apim_import_block = local.apim_enabled ? (
    trimspace(var.apim_openapi_spec_url) != "" ? {
      content_format = "openapi-link"
      content_value  = trimspace(var.apim_openapi_spec_url)
      } : {
      content_format = "openapi"
      content_value  = file("${path.module}/openapi/apim-bootstrap.yaml")
    }
  ) : null

  backend_url_normalized = local.apim_enabled ? trimsuffix(var.archlucid_api_backend_url, "/") : ""
}

resource "azurerm_api_management" "archiforge" {
  count = local.apim_enabled ? 1 : 0

  name                = var.apim_name
  location            = local.apim_location
  resource_group_name = local.apim_resource_group_name
  publisher_name      = var.apim_publisher_name
  publisher_email     = var.apim_publisher_email
  sku_name            = "Consumption_0"
  tags                = var.tags

  identity {
    type = "SystemAssigned"
  }
}

resource "azurerm_api_management_api" "archiforge" {
  count = local.apim_enabled ? 1 : 0

  name                = "archiforge-api"
  api_management_name = azurerm_api_management.archiforge[0].name
  resource_group_name = local.apim_resource_group_name
  revision            = "1"
  display_name        = "ArchLucid API"
  path                = var.apim_api_path_suffix
  protocols           = ["https"]

  service_url = local.backend_url_normalized

  import {
    content_format = local.apim_import_block.content_format
    content_value  = local.apim_import_block.content_value
  }

  depends_on = [azurerm_api_management.archiforge]
}
