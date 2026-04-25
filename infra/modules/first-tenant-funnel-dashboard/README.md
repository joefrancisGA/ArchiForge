# `first-tenant-funnel-dashboard` Terraform module

> **Improvement 12 (2026-04-24).** Sibling to
> [`infra/modules/golden-cohort-cost-dashboard`](../golden-cohort-cost-dashboard/README.md).
> Provisions an **Azure Monitor Workbook** that visualises the
> first-tenant onboarding telemetry funnel.

> **Privacy posture.** This module **does not** flip the owner-only feature flag
> `Telemetry:FirstTenantFunnel:PerTenantEmission`. The funnel is fully analyzable
> in **aggregated-only** mode (the default) and that is what every tile here uses.
> See [`docs/security/PRIVACY_NOTE.md`](../../../docs/security/PRIVACY_NOTE.md) §3.A
> and [`docs/PENDING_QUESTIONS.md`](../../../docs/PENDING_QUESTIONS.md) **Q40**.

## Why a Workbook (not a separate dashboard tool)

- **Single source of truth.** The funnel events are emitted as App Insights
  `customMetrics` (`archlucid_first_tenant_funnel_events_total`). A Workbook
  reads them in-place — no extra connector, no extra credential.
- **Read-only by default.** `isLocked: true` in the JSON keeps casual viewers
  from breaking the queries; only the platform-ops role can edit.
- **Shares posture with Improvement 11.** Same provider, same RBAC, same
  variable shape — operators only need to learn one module.

## Tiles

| Tile | Source | Purpose |
| ---- | ------ | ------- |
| Funnel events — totals by event | `customMetrics.archlucid_first_tenant_funnel_events_total` (label `event`) | Volume per step |
| Conversion vs signup (% of signups reaching each step) | Same counter, KQL ratio | Single-glance funnel shape |
| 30-minute first-finding success rate (vs target) | Ratio of `thirty_minute_milestone` / `signup` | Improvement 12 success metric |
| Daily funnel volume | Same counter, daily bin | Spot regressions / growth |

## Inputs

See [`variables.tf`](./variables.tf). Notable:

- `enable_workbook` defaults to **`false`** so this module is safe to load on
  CI / laptops with no real Azure subscription.
- `thirty_minute_target_percent` defaults to **`60`**. This is a placeholder
  baseline — the real target is owner-set per Q40.

## Outputs

| Output | Use |
| ------ | --- |
| `workbook_id` | Pin downstream resources / role assignments to the Workbook. |
| `workbook_display_name` | Echoed into navigation links / runbook. |
| `thirty_minute_target_percent` | Useful for Terraform output assertions in CI. |

## Wiring example

```hcl
module "first_tenant_funnel_dashboard" {
  source = "../../modules/first-tenant-funnel-dashboard"

  enable_workbook                  = true
  resource_group_name              = data.azurerm_resource_group.monitoring.name
  location                         = data.azurerm_resource_group.monitoring.location
  application_insights_resource_id = azurerm_application_insights.archlucid.id
  thirty_minute_target_percent     = 60

  tags = local.common_tags
}
```

## Operator workflow

1. Workbook URL appears in the Azure portal under the App Insights resource →
   **Workbooks** → "ArchLucid — First-tenant onboarding funnel".
2. Every tile is populated automatically once any tenant interacts with the
   onboarding surfaces (signup form, opt-in tour, run wizard, commit button,
   finding view) — no extra ingestion job is required because the metric is
   emitted by the API itself.
3. If the owner ever flips
   `Telemetry:FirstTenantFunnel:PerTenantEmission` to `true`, the Workbook
   continues to work unchanged and the per-tenant rows in
   `dbo.FirstTenantFunnelEvents` become available for ad-hoc tenant-scoped
   investigations (subject to the privacy review tracked under Q40).
