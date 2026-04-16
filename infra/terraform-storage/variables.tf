variable "enable_storage_account" {
  type        = bool
  description = "When true, create the storage account and blob containers for large artifact offload (API ArtifactLargePayload)."
  default     = false
}

variable "create_resource_group" {
  type    = bool
  default = false
}

variable "resource_group_name" {
  type        = string
  description = "Resource group for the storage account."
  default     = ""
}

variable "location" {
  type        = string
  description = "Azure region (required when create_resource_group is true)."
  default     = ""
}

variable "storage_account_name" {
  type        = string
  description = "Globally unique storage account name (3-24 chars, lowercase alphanumeric)."
  default     = ""
}

variable "account_replication_type" {
  type        = string
  description = "LRS for dev; ZRS or GRS for production resilience."
  default     = "LRS"
}

variable "public_network_access_enabled" {
  type        = bool
  description = "Set false when access is only via private endpoint (see terraform-private)."
  default     = true
}

variable "tags" {
  type    = map(string)
  default = {}
}

variable "customer_managed_key_enabled" {
  type        = bool
  description = "When true and customer_managed_key_id is set, enable CMK on the artifacts storage account via azurerm_storage_account_customer_managed_key."
  default     = false
}

variable "customer_managed_key_id" {
  type        = string
  description = "Full Azure Resource Manager id of the Key Vault key version used for storage encryption (see docs/runbooks/CMK_ENCRYPTION.md)."
  default     = ""
}
