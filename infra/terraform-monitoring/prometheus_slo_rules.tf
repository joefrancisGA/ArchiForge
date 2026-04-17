# Optional Azure Monitor managed Prometheus rule group — mirrors key PromQL from
# ../prometheus/archlucid-slo-rules.yml (p99 latency, 5xx ratio, outbox depth).
# Requires an Azure Monitor workspace (scopes) scraped with the same metric names as self-hosted Prometheus.

locals {
  prometheus_slo_rule_group_enabled = var.enable_monitoring_stack && var.enable_prometheus_slo_rule_group && length(trimspace(var.azure_monitor_workspace_id)) > 0
}

data "azurerm_resource_group" "prometheus_slo" {
  count = local.prometheus_slo_rule_group_enabled ? 1 : 0
  name  = var.resource_group_name
}

resource "azurerm_monitor_alert_prometheus_rule_group" "archlucid_slo" {
  count = local.prometheus_slo_rule_group_enabled ? 1 : 0

  name                = "${var.name_prefix}-prom-slo"
  resource_group_name = var.resource_group_name
  location            = data.azurerm_resource_group.prometheus_slo[0].location
  scopes              = [var.azure_monitor_workspace_id]
  rule_group_enabled  = true
  interval            = "PT1M"

  rule {
    enabled    = true
    alert      = "ArchLucidSloHttpP99HighTf"
    severity   = 2
    for        = "PT10M"
    expression = <<-EOT
(histogram_quantile(0.99, sum(rate(http_server_request_duration_seconds_bucket[5m])) by (le)) or histogram_quantile(0.99, sum(rate(http_server_duration_milliseconds_bucket[5m])) by (le)) / 1000) > 5
EOT
    annotations = {
      summary = "HTTP p99 latency above 5s (see infra/prometheus/archlucid-slo-rules.yml ArchLucidSloHttpP99High)."
    }

    action {
      action_group_id = azurerm_monitor_action_group.ops[0].id
    }
  }

  rule {
    enabled    = true
    alert      = "ArchLucidSloHttp5xxRatioElevatedTf"
    severity   = 2
    for        = "PT10M"
    expression = <<-EOT
(
  (sum(rate(http_server_request_duration_seconds_count{http_response_status_code=~"5.."}[10m])) or sum(rate(http_server_duration_milliseconds_count{http_response_status_code=~"5.."}[10m])))
  /
  clamp_min(sum(rate(http_server_request_duration_seconds_count[10m])) or sum(rate(http_server_duration_milliseconds_count[10m])), 1e-9)
) > 0.02
EOT
    annotations = {
      summary = "HTTP 5xx ratio above 2% over 10m (see ArchLucidSloHttp5xxRatioElevated)."
    }

    action {
      action_group_id = azurerm_monitor_action_group.ops[0].id
    }
  }

  rule {
    enabled    = true
    alert      = "ArchLucidSloOutboxDepthCriticalTf"
    severity   = 1
    for        = "PT15M"
    expression = <<-EOT
(archlucid_authority_pipeline_work_pending > 500) or (archlucid_retrieval_indexing_outbox_pending > 500) or (archlucid_integration_event_outbox_publish_pending > 500)
EOT
    annotations = {
      summary = "SQL outbox depth SLO breach (any queue > 500 pending)."
    }

    action {
      action_group_id = azurerm_monitor_action_group.ops[0].id
    }
  }

  tags = var.tags
}
