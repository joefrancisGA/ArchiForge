check "key_vault_name_length" {
  assert {
    condition     = !var.enable_key_vault || (length(var.key_vault_name) >= 3 && length(var.key_vault_name) <= 24)
    error_message = "key_vault_name must be 3-24 characters when enable_key_vault is true."
  }
}

check "resource_group_when_key_vault_enabled" {
  assert {
    condition     = !var.enable_key_vault || length(trimspace(var.resource_group_name)) > 0
    error_message = "resource_group_name is required when enable_key_vault is true."
  }
}

check "location_when_creating_rg" {
  assert {
    condition     = !var.enable_key_vault || !var.create_resource_group || length(trimspace(var.location)) > 0
    error_message = "location is required when create_resource_group is true."
  }
}
