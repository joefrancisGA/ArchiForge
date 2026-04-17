# Phase 7.5 (monitoring stack): migrate Terraform state addresses from historical `archiforge`
# labels to `archlucid` without replacing Azure/Grafana resources. See
# docs/runbooks/TERRAFORM_STATE_MV_PHASE_7_5.md and docs/ARCHLUCID_RENAME_CHECKLIST.md §7.5.

moved {
  from = azurerm_dashboard_grafana.archiforge
  to   = azurerm_dashboard_grafana.archlucid
}

moved {
  from = grafana_folder.archiforge
  to   = grafana_folder.archlucid
}

moved {
  from = azurerm_monitor_alert_prometheus_rule_group.archiforge_slo
  to   = azurerm_monitor_alert_prometheus_rule_group.archlucid_slo
}
