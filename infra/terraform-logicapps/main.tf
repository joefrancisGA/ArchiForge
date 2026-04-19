locals {
  tags = merge({ Workload = "archlucid-logic-apps" }, var.tags)
}

resource "azurerm_storage_account" "logic" {
  count = var.enable_logic_apps ? 1 : 0

  name                     = var.storage_account_name
  resource_group_name      = var.resource_group_name
  location                 = var.location
  account_tier             = "Standard"
  account_replication_type = "ZRS"
  min_tls_version          = "TLS1_2"

  allow_nested_items_to_be_public = false

  tags = local.tags
}

resource "azurerm_storage_share" "logic_workflow" {
  count = var.enable_logic_apps ? 1 : 0

  name                 = var.storage_share_name
  storage_account_name = azurerm_storage_account.logic[0].name
  quota                = 5120
}

resource "azurerm_service_plan" "logic" {
  count = var.enable_logic_apps ? 1 : 0

  name                = var.app_service_plan_name
  resource_group_name = var.resource_group_name
  location            = var.location
  os_type             = "Windows"
  sku_name            = "WS1"

  tags = local.tags
}

resource "azurerm_logic_app_standard" "edge" {
  count = var.enable_logic_apps ? 1 : 0

  name                       = var.logic_app_name
  location                   = var.location
  resource_group_name        = var.resource_group_name
  app_service_plan_id        = azurerm_service_plan.logic[0].id
  storage_account_name       = azurerm_storage_account.logic[0].name
  storage_account_access_key = azurerm_storage_account.logic[0].primary_access_key
  storage_account_share_name = azurerm_storage_share.logic_workflow[0].name
  version                    = "~4"
  https_only                 = true

  identity {
    type = "SystemAssigned"
  }

  site_config {
    always_on = false
  }

  tags = local.tags
}
