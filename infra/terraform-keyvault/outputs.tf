output "key_vault_id" {
  value       = try(azurerm_key_vault.archlucid[0].id, "")
  description = "Resource id of the Key Vault when created."
}

output "key_vault_uri" {
  value       = try(azurerm_key_vault.archlucid[0].vault_uri, "")
  description = "HTTPS URI for ArchLucid:Secrets:KeyVaultUri."
}
