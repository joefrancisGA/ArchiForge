locals {
  enabled = var.enable_storage_account

  resource_group_name = local.enabled ? (
    var.create_resource_group ? azurerm_resource_group.this[0].name : data.azurerm_resource_group.target[0].name
  ) : ""

  location = local.enabled ? (
    var.create_resource_group ? var.location : data.azurerm_resource_group.target[0].location
  ) : ""
}

data "azurerm_resource_group" "target" {
  count = local.enabled && !var.create_resource_group ? 1 : 0
  name  = var.resource_group_name
}

resource "azurerm_resource_group" "this" {
  count    = local.enabled && var.create_resource_group ? 1 : 0
  name     = var.resource_group_name
  location = var.location
  tags     = var.tags
}

resource "azurerm_storage_account" "artifacts" {
  count = local.enabled ? 1 : 0

  name                     = var.storage_account_name
  resource_group_name      = local.resource_group_name
  location                 = local.location
  account_tier             = "Standard"
  account_kind             = "StorageV2"
  account_replication_type = var.account_replication_type

  public_network_access_enabled   = var.public_network_access_enabled
  allow_nested_items_to_be_public = false

  blob_properties {
    versioning_enabled = true
    delete_retention_policy {
      days = 30
    }
    container_delete_retention_policy {
      days = 7
    }
  }

  identity {
    type = "SystemAssigned"
  }

  tags = var.tags
}

resource "azurerm_storage_account_customer_managed_key" "artifacts" {
  count = local.enabled && var.customer_managed_key_enabled && length(trimspace(var.customer_managed_key_id)) > 0 ? 1 : 0

  storage_account_id = azurerm_storage_account.artifacts[0].id
  key_vault_key_id   = var.customer_managed_key_id
}

resource "azurerm_storage_container" "golden_manifests" {
  count = local.enabled ? 1 : 0

  name                  = "golden-manifests"
  storage_account_id    = azurerm_storage_account.artifacts[0].id
  container_access_type = "private"
}

resource "azurerm_storage_container" "artifact_bundles" {
  count = local.enabled ? 1 : 0

  name                  = "artifact-bundles"
  storage_account_id    = azurerm_storage_account.artifacts[0].id
  container_access_type = "private"
}

resource "azurerm_storage_container" "artifact_contents" {
  count = local.enabled ? 1 : 0

  name                  = "artifact-contents"
  storage_account_id    = azurerm_storage_account.artifacts[0].id
  container_access_type = "private"
}
