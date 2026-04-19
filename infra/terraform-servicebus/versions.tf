terraform {
  required_version = ">= 1.5.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      # Provider 4.x removed `zone_redundant` on `azurerm_servicebus_namespace`; keep 3.x until the root is migrated.
      version = ">= 3.80.0, < 4.0.0"
    }
  }
}
