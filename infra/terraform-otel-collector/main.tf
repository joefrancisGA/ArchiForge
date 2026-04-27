# OpenTelemetry collector on Azure Container Apps (tail-sampling → Azure Monitor).
# Security: public ingress is optional; default external_enabled=false keeps OTLP in-VNet.
# Scalability: min/max replicas; CPU/memory are conservative.
# Reliability: liveness on health extension; revision_mode Single for predictable rollouts.
# If enable_otel_deployment is false, no resources are created (local validate/CI only).

locals {
  otel_name = "archlucid-otel"
  # Application Insights connection string is embedded in the rendered YAML; keep this local sensitive in plans.
  otel_config_yaml = <<-EOT
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: 0.0.0.0:4317
      http:
        endpoint: 0.0.0.0:4318
processors:
  batch: {}
  tail_sampling:
    decision_wait: 10s
    num_traces: 50000
    expected_new_traces_per_sec: 100
    policies:
      - name: errors-always
        type: status_code
        status_code:
          status_codes: [ERROR]
      - name: slow-roots
        type: latency
        latency:
          threshold_ms: ${var.tail_sampling_min_root_duration_ms}
      - name: always-authority-llm
        type: string_attribute
        string_attribute:
          key: otel.library.name
          values: ${jsonencode(var.tail_sampling_always_keep_activity_sources)}
      - name: head-based-fallback
        type: probabilistic
        probabilistic:
          sampling_percentage: ${var.tail_sampling_default_ratio * 100}
exporters:
  azuremonitor:
    connection_string: "${var.application_insights_connection_string}"
extensions:
  health_check:
    endpoint: 0.0.0.0:13133
service:
  extensions: [health_check]
  pipelines:
    traces:
      receivers: [otlp]
      processors: [tail_sampling, batch]
      exporters: [azuremonitor]
EOT
}

resource "azurerm_container_app" "otel" {
  count = var.enable_otel_deployment ? 1 : 0

  name                         = local.otel_name
  resource_group_name          = var.resource_group_name
  container_app_environment_id = var.container_apps_environment_id
  revision_mode                = "Single"

  secret {
    name  = "otel-yaml"
    value = local.otel_config_yaml
  }
  template {
    min_replicas = 1
    max_replicas = 3
    container {
      name    = "otel"
      image   = "otel/opentelemetry-collector-contrib:0.102.1"
      cpu     = 0.5
      memory  = "1.0Gi"
      command = ["/otelcol-contrib", "--config=env:OTEL_CONFIG"]
      env {
        name        = "OTEL_CONFIG"
        secret_name = "otel-yaml"
      }
      liveness_probe {
        transport = "HTTP"
        port      = 13133
        path      = "/"
      }
    }
  }
  ingress {
    allow_insecure_connections = false
    external_enabled           = var.otel_ingress_external_enabled
    target_port                = 4317
    transport                  = "auto"
    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }
  identity {
    type = "SystemAssigned"
  }
}
