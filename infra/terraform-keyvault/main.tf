# Optional Key Vault for ArchLucid secrets (ArchLucid:Secrets:Provider=KeyVault) and storage CMK keys.
# Resource addresses may retain historical tokens; rename via terraform state mv when coordinated.

locals {
  enabled = var.enable_key_vault

  resource_group_name = local.enabled ? (
    var.create_resource_group ? azurerm_resource_group.this[0].name : data.azurerm_resource_group.target[0].name
  ) : ""

  azure_location = local.enabled ? (
    var.create_resource_group ? var.location : data.azurerm_resource_group.target[0].location
  ) : ""

  tenant_id_effective = length(trimspace(var.tenant_id)) > 0 ? var.tenant_id : data.azurerm_client_config.current.tenant_id
}

data "azurerm_client_config" "current" {}

data "azurerm_resource_group" "target" {
  count = local.enabled && !var.create_resource_group ? 1 : 0

  name = var.resource_group_name
}

resource "azurerm_resource_group" "this" {
  count = local.enabled && var.create_resource_group ? 1 : 0

  name     = var.resource_group_name
  location = var.location
  tags     = var.tags
}

resource "azurerm_key_vault" "archlucid" {
  count = local.enabled ? 1 : 0

  name                       = var.key_vault_name
  location                   = local.azure_location
  resource_group_name        = local.resource_group_name
  tenant_id                  = local.tenant_id_effective
  sku_name                   = var.sku_name
  soft_delete_retention_days = 90
  purge_protection_enabled   = true

  rbac_authorization_enabled  = true
  enabled_for_disk_encryption = false

  public_network_access_enabled = false

  tags = var.tags
}

resource "azurerm_role_assignment" "vault_secrets_officer" {
  for_each = local.enabled ? toset(var.admin_object_ids) : []

  scope                = azurerm_key_vault.archlucid[0].id
  role_definition_name = "Key Vault Secrets Officer"
  principal_id         = each.value
}
