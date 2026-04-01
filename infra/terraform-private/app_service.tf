# Regional VNet integration for Linux Web App / App Service so the API resolves private DNS
# (e.g. privatelink.database.windows.net) from subnets linked to this VNet.

resource "azurerm_app_service_virtual_network_swift_connection" "web_app" {
  count = (
    local.pe_enabled &&
    length(trimspace(var.linux_web_app_id)) > 0 &&
    length(trimspace(var.web_app_vnet_integration_subnet_id)) > 0
  ) ? 1 : 0

  app_service_id = var.linux_web_app_id
  subnet_id      = var.web_app_vnet_integration_subnet_id
}
