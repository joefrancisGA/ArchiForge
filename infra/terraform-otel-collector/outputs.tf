output "otlp_grpc_endpoint" {
  description = "gRPC OTLP endpoint (host:port) for OTEL_EXPORTER_OTLP_ENDPOINT."
  value       = var.enable_otel_deployment ? format("%s:4317", try(azurerm_container_app.otel[0].latest_revision_fqdn, "")) : ""
}

output "otlp_http_endpoint" {
  description = "HTTP OTLP URL for environments that block gRPC egress."
  value = var.enable_otel_deployment && try(azurerm_container_app.otel[0].latest_revision_fqdn, "") != "" ? format(
    "https://%s:4318",
    azurerm_container_app.otel[0].latest_revision_fqdn
  ) : ""
}

output "otel_collector_fqdn" {
  description = "Latest revision FQDN of the collector (for health checks or private DNS). Empty when the collector is not deployed."
  value       = var.enable_otel_deployment ? try(azurerm_container_app.otel[0].latest_revision_fqdn, "") : ""
}
