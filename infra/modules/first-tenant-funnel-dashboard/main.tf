# Improvement 12 (2026-04-24) — Azure Monitor Workbook for the first-tenant
# onboarding telemetry funnel.
#
# Reads the aggregated counter `archlucid_first_tenant_funnel_events_total`
# emitted by ArchLucid.Core/Diagnostics/ArchLucidInstrumentation.cs and renders
# step-by-step conversion plus the 30-minute first-finding success rate.
#
# This module does NOT flip the owner-only per-tenant emission flag
# (`Telemetry:FirstTenantFunnel:PerTenantEmission`). The dashboard is correct
# in either mode — aggregated-only is sufficient for funnel visibility per
# docs/security/PRIVACY_NOTE.md §3.A.

locals {
  workbook_enabled = var.enable_workbook && length(trimspace(var.resource_group_name)) > 0 && length(trimspace(var.application_insights_resource_id)) > 0

  workbook_data = templatefile(
    "${path.module}/workbook.tpl.json",
    {
      workbook_display_name            = var.workbook_display_name
      thirty_minute_target_percent     = var.thirty_minute_target_percent
      application_insights_resource_id = var.application_insights_resource_id
    }
  )
}

resource "azurerm_application_insights_workbook" "first_tenant_funnel" {
  count = local.workbook_enabled ? 1 : 0

  name                = var.workbook_name_guid
  resource_group_name = var.resource_group_name
  location            = var.location
  display_name        = var.workbook_display_name
  source_id           = var.application_insights_resource_id
  data_json           = local.workbook_data

  tags = merge(
    var.tags,
    {
      "archlucid:owner"                 = "platform"
      "archlucid:cost-center"           = "first-tenant-funnel"
      "archlucid:privacy-default"       = "aggregated-only"
      "archlucid:thirty-min-target-pct" = tostring(var.thirty_minute_target_percent)
    }
  )
}
