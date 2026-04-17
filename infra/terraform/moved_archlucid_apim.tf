# Phase 7.5 (APIM only, root stack): migrate Terraform state addresses from historical
# `archiforge` labels to `archlucid` without replacing Azure resources. On the next
# `terraform plan` / `terraform apply`, Terraform records these moves automatically.
# See docs/runbooks/TERRAFORM_STATE_MV_PHASE_7_5.md and docs/ARCHLUCID_RENAME_CHECKLIST.md §7.5.
# Monitoring stack uses `moved_archlucid_monitoring.tf` the same way.

moved {
  from = azurerm_api_management.archiforge
  to   = azurerm_api_management.archlucid
}

moved {
  from = azurerm_api_management_api.archiforge
  to   = azurerm_api_management_api.archlucid
}
