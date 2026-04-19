resource "azurerm_role_assignment" "api_servicebus_sender" {
  count = trimspace(var.api_managed_identity_principal_id) != "" ? 1 : 0

  scope                = azurerm_servicebus_namespace.integration.id
  role_definition_name = "Azure Service Bus Data Sender"
  principal_id         = var.api_managed_identity_principal_id
}

resource "azurerm_role_assignment" "worker_servicebus_sender" {
  count = trimspace(var.worker_managed_identity_principal_id) != "" ? 1 : 0

  scope                = azurerm_servicebus_namespace.integration.id
  role_definition_name = "Azure Service Bus Data Sender"
  principal_id         = var.worker_managed_identity_principal_id
}

resource "azurerm_role_assignment" "worker_servicebus_receiver" {
  count = trimspace(var.worker_managed_identity_principal_id) != "" ? 1 : 0

  scope                = azurerm_servicebus_namespace.integration.id
  role_definition_name = "Azure Service Bus Data Receiver"
  principal_id         = var.worker_managed_identity_principal_id
}

resource "azurerm_role_assignment" "governance_logic_app_servicebus_receiver" {
  count = var.enable_logic_app_governance_approval_subscription && trimspace(var.governance_logic_app_managed_identity_principal_id) != "" ? 1 : 0

  scope                = azurerm_servicebus_namespace.integration.id
  role_definition_name = "Azure Service Bus Data Receiver"
  principal_id         = var.governance_logic_app_managed_identity_principal_id
}
