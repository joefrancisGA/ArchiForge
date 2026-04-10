resource "azurerm_virtual_network" "main" {
  count = local.pe_enabled ? 1 : 0

  name                = var.virtual_network_name
  location            = local.pe_location
  resource_group_name = local.pe_resource_group_name
  address_space       = var.vnet_address_space
  tags                = var.tags
}

resource "azurerm_subnet" "private_endpoints" {
  count = local.pe_enabled ? 1 : 0

  name                 = var.private_endpoints_subnet_name
  resource_group_name  = local.pe_resource_group_name
  virtual_network_name = azurerm_virtual_network.main[0].name
  address_prefixes     = [var.private_endpoints_subnet_prefix]

  private_endpoint_network_policies = "Disabled"
}

resource "azurerm_private_dns_zone" "sql" {
  count = local.pe_enabled ? 1 : 0

  name                = "privatelink.database.windows.net"
  resource_group_name = local.pe_resource_group_name
  tags                = var.tags
}

resource "azurerm_private_dns_zone" "blob" {
  count = local.pe_enabled ? 1 : 0

  name                = "privatelink.blob.core.windows.net"
  resource_group_name = local.pe_resource_group_name
  tags                = var.tags
}

resource "azurerm_private_dns_zone_virtual_network_link" "sql" {
  count = local.pe_enabled ? 1 : 0

  name                  = "sql-dns-link"
  resource_group_name   = local.pe_resource_group_name
  private_dns_zone_name = azurerm_private_dns_zone.sql[0].name
  virtual_network_id    = azurerm_virtual_network.main[0].id
  tags                  = var.tags
}

resource "azurerm_private_dns_zone_virtual_network_link" "blob" {
  count = local.pe_enabled ? 1 : 0

  name                  = "blob-dns-link"
  resource_group_name   = local.pe_resource_group_name
  private_dns_zone_name = azurerm_private_dns_zone.blob[0].name
  virtual_network_id    = azurerm_virtual_network.main[0].id
  tags                  = var.tags
}

resource "azurerm_private_endpoint" "sql" {
  count = local.pe_enabled ? 1 : 0

  name                = "pe-archlucid-sql"
  location            = local.pe_location
  resource_group_name = local.pe_resource_group_name
  subnet_id           = azurerm_subnet.private_endpoints[0].id
  tags                = var.tags

  private_service_connection {
    name                           = "psc-sql"
    private_connection_resource_id = var.sql_server_id
    is_manual_connection           = false
    subresource_names              = ["sqlServer"]
  }

  private_dns_zone_group {
    name                 = "sql-zones"
    private_dns_zone_ids = [azurerm_private_dns_zone.sql[0].id]
  }
}

resource "azurerm_private_endpoint" "blob" {
  count = local.pe_enabled ? 1 : 0

  name                = "pe-archlucid-blob"
  location            = local.pe_location
  resource_group_name = local.pe_resource_group_name
  subnet_id           = azurerm_subnet.private_endpoints[0].id
  tags                = var.tags

  private_service_connection {
    name                           = "psc-blob"
    private_connection_resource_id = var.storage_account_id
    is_manual_connection           = false
    subresource_names              = ["blob"]
  }

  private_dns_zone_group {
    name                 = "blob-zones"
    private_dns_zone_ids = [azurerm_private_dns_zone.blob[0].id]
  }
}

resource "azurerm_private_dns_zone" "search" {
  count = local.pe_enabled && length(trimspace(var.search_service_id)) > 0 ? 1 : 0

  name                = "privatelink.search.windows.net"
  resource_group_name = local.pe_resource_group_name
  tags                = var.tags
}

resource "azurerm_private_dns_zone_virtual_network_link" "search" {
  count = local.pe_enabled && length(trimspace(var.search_service_id)) > 0 ? 1 : 0

  name                  = "search-dns-link"
  resource_group_name   = local.pe_resource_group_name
  private_dns_zone_name = azurerm_private_dns_zone.search[0].name
  virtual_network_id    = azurerm_virtual_network.main[0].id
  tags                  = var.tags
}

resource "azurerm_private_endpoint" "search" {
  count = local.pe_enabled && length(trimspace(var.search_service_id)) > 0 ? 1 : 0

  name                = "pe-archlucid-search"
  location            = local.pe_location
  resource_group_name = local.pe_resource_group_name
  subnet_id           = azurerm_subnet.private_endpoints[0].id
  tags                = var.tags

  private_service_connection {
    name                           = "psc-search"
    private_connection_resource_id = var.search_service_id
    is_manual_connection           = false
    subresource_names              = ["searchService"]
  }

  private_dns_zone_group {
    name                 = "search-zones"
    private_dns_zone_ids = [azurerm_private_dns_zone.search[0].id]
  }
}
