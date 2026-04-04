locals {
  enabled = var.enable_monitoring_stack

  cpu_alerts_enabled = local.enabled && var.container_cpu_nanos_threshold > 0

  api_cpu_alert = local.cpu_alerts_enabled && length(trimspace(var.api_container_app_resource_id)) > 0

  worker_cpu_alert = local.cpu_alerts_enabled && length(trimspace(var.worker_container_app_resource_id)) > 0
}

resource "azurerm_monitor_action_group" "ops" {
  count = local.enabled ? 1 : 0

  name                = "${var.name_prefix}-ops-ag"
  resource_group_name = var.resource_group_name
  short_name          = substr(replace(var.name_prefix, "-", ""), 0, 12)

  email_receiver {
    name                    = "primary"
    email_address           = var.alert_email_address
    use_common_alert_schema = true
  }

  dynamic "webhook_receiver" {
    for_each = length(trimspace(var.alert_webhook_uri)) > 0 ? [1] : []
    content {
      name                    = "webhook"
      service_uri             = var.alert_webhook_uri
      use_common_alert_schema = true
    }
  }

  tags = var.tags
}

resource "azurerm_monitor_metric_alert" "api_container_cpu_high" {
  count = local.api_cpu_alert ? 1 : 0

  name                = "${var.name_prefix}-api-cpu-high"
  resource_group_name = var.resource_group_name
  scopes              = [var.api_container_app_resource_id]
  description         = "ArchiForge API Container App average CPU (nano cores) exceeded threshold over 5 minutes."
  severity            = 2
  frequency           = "PT1M"
  window_size         = "PT5M"
  enabled             = true
  auto_mitigate       = true

  criteria {
    metric_namespace = "Microsoft.App/containerApps"
    metric_name      = "CpuUsageNanoCores"
    aggregation      = "Average"
    operator         = "GreaterThan"
    threshold        = var.container_cpu_nanos_threshold
  }

  action {
    action_group_id = azurerm_monitor_action_group.ops[0].id
  }

  tags = var.tags
}

resource "azurerm_monitor_metric_alert" "worker_container_cpu_high" {
  count = local.worker_cpu_alert ? 1 : 0

  name                = "${var.name_prefix}-worker-cpu-high"
  resource_group_name = var.resource_group_name
  scopes              = [var.worker_container_app_resource_id]
  description         = "ArchiForge Worker Container App average CPU (nano cores) exceeded threshold over 5 minutes."
  severity            = 2
  frequency           = "PT1M"
  window_size         = "PT5M"
  enabled             = true
  auto_mitigate       = true

  criteria {
    metric_namespace = "Microsoft.App/containerApps"
    metric_name      = "CpuUsageNanoCores"
    aggregation      = "Average"
    operator         = "GreaterThan"
    threshold        = var.container_cpu_nanos_threshold
  }

  action {
    action_group_id = azurerm_monitor_action_group.ops[0].id
  }

  tags = var.tags
}

resource "azurerm_dashboard_grafana" "archiforge" {
  count = var.enable_managed_grafana ? 1 : 0

  name                              = var.grafana_name
  resource_group_name               = var.resource_group_name
  location                          = var.grafana_location
  grafana_major_version             = var.grafana_major_version
  api_key_enabled                   = var.grafana_api_key_enabled
  deterministic_outbound_ip_enabled = false
  public_network_access_enabled     = true
  sku                               = "Standard"
  zone_redundancy_enabled           = false

  identity {
    type = "SystemAssigned"
  }

  tags = var.tags
}
