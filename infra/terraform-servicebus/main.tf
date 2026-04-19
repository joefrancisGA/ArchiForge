locals {
  pe_enabled = var.enable_private_endpoint && trimspace(var.private_endpoints_subnet_id) != ""
}

resource "azurerm_servicebus_namespace" "integration" {
  name                = var.namespace_name
  location            = var.location
  resource_group_name = var.resource_group_name
  sku                 = var.sku
  zone_redundant      = var.zone_redundant
  tags                = var.tags
}

resource "azurerm_servicebus_topic" "integration_events" {
  name         = var.topic_name
  namespace_id = azurerm_servicebus_namespace.integration.id

  requires_duplicate_detection            = true
  duplicate_detection_history_time_window = "PT10M"
}

resource "azurerm_servicebus_subscription" "worker" {
  name     = var.worker_subscription_name
  topic_id = azurerm_servicebus_topic.integration_events.id

  max_delivery_count = var.max_delivery_count

  dead_lettering_on_message_expiration = true
}

resource "azurerm_servicebus_subscription" "external" {
  name     = var.external_subscription_name
  topic_id = azurerm_servicebus_topic.integration_events.id

  max_delivery_count = var.max_delivery_count

  dead_lettering_on_message_expiration = true
}

resource "azurerm_servicebus_subscription" "logic_app_governance_approval" {
  count = var.enable_logic_app_governance_approval_subscription ? 1 : 0

  name     = var.logic_app_governance_approval_subscription_name
  topic_id = azurerm_servicebus_topic.integration_events.id

  max_delivery_count = var.max_delivery_count

  dead_lettering_on_message_expiration = true
}

resource "azurerm_servicebus_subscription_rule" "logic_app_governance_approval_default" {
  count = var.enable_logic_app_governance_approval_subscription ? 1 : 0

  name            = "$Default"
  subscription_id = azurerm_servicebus_subscription.logic_app_governance_approval[0].id
  filter_type     = "SqlFilter"
  sql_filter      = "event_type = 'com.archlucid.governance.approval.submitted'"
}

resource "azurerm_private_endpoint" "servicebus" {
  count = local.pe_enabled ? 1 : 0

  name                = "pe-${var.namespace_name}"
  location            = var.location
  resource_group_name = var.resource_group_name
  subnet_id           = var.private_endpoints_subnet_id
  tags                = var.tags

  private_service_connection {
    name                           = "psc-servicebus"
    private_connection_resource_id = azurerm_servicebus_namespace.integration.id
    is_manual_connection           = false
    subresource_names              = ["namespace"]
  }

  dynamic "private_dns_zone_group" {
    for_each = length(var.private_dns_zone_ids) > 0 ? [1] : []
    content {
      name                 = "servicebus-zones"
      private_dns_zone_ids = var.private_dns_zone_ids
    }
  }
}
