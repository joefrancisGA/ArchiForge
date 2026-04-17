locals {
  grafana_dashboards_enabled = var.enable_managed_grafana && var.grafana_terraform_dashboards_enabled
}

resource "grafana_folder" "archlucid" {
  count = local.grafana_dashboards_enabled ? 1 : 0

  title = "ArchLucid"

  depends_on = [azurerm_dashboard_grafana.archlucid]
}

resource "grafana_dashboard" "slo" {
  count = local.grafana_dashboards_enabled ? 1 : 0

  folder      = grafana_folder.archlucid[0].id
  config_json = file("${path.module}/../grafana/dashboard-archlucid-slo.json")
}

resource "grafana_dashboard" "llm_usage" {
  count = local.grafana_dashboards_enabled ? 1 : 0

  folder      = grafana_folder.archlucid[0].id
  config_json = file("${path.module}/../grafana/dashboard-archlucid-llm-usage.json")
}

resource "grafana_dashboard" "authority" {
  count = local.grafana_dashboards_enabled ? 1 : 0

  folder      = grafana_folder.archlucid[0].id
  config_json = file("${path.module}/../grafana/dashboard-archlucid-authority.json")
}

resource "grafana_dashboard" "container_apps_overview" {
  count = local.grafana_dashboards_enabled ? 1 : 0

  folder      = grafana_folder.archlucid[0].id
  config_json = file("${path.module}/../grafana/dashboards/archlucid-container-apps-overview.json")
}
