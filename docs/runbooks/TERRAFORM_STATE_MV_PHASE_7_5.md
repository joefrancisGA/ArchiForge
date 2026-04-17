# Runbook: Terraform `state mv` for Phase 7.5 (`archiforge` resource addresses)

**Last reviewed:** 2026-04-17

## Objective

Rename **Terraform resource addresses** that still contain the historical token **`archiforge`** to **`archlucid`** (or another chosen label) **without** destroying cloud objects — using **`terraform state mv`** **or** Terraform **`moved`** blocks (root **`infra/terraform`**: **`moved_archlucid_apim.tf`**; **`infra/terraform-monitoring`**: **`moved_archlucid_monitoring.tf`**).

## Preconditions

- **Maintenance window** agreed with the deploy team.
- **Remote state** backup (or snapshot) and a local **`terraform state pull > backup.tfstate`** where policy allows.
- **No `.tf` edits** in the same change as the moves (this runbook is **state-only** preparation; apply code renames in a **separate** planned change after moves, if desired).

## Inventory (as of 2026-04-14)

Addresses below are **representative**; always run **`terraform state list`** in each root to confirm. Module / workspace prefixes (e.g. **`module.apim.`**) must be prepended to match your layout.

### `infra/terraform/` (APIM)

| Current address | Suggested new address |
|-----------------|------------------------|
| `azurerm_api_management.archiforge[0]` | `azurerm_api_management.archlucid[0]` |
| `azurerm_api_management_api.archiforge[0]` | `azurerm_api_management_api.archlucid[0]` |

Example (from repo root, inside the configured workspace for this stack):

```bash
terraform state mv 'azurerm_api_management.archiforge[0]' 'azurerm_api_management.archlucid[0]'
terraform state mv 'azurerm_api_management_api.archiforge[0]' 'azurerm_api_management_api.archlucid[0]'
```

### `infra/terraform-monitoring/`

| Current address | Suggested new address |
|-----------------|------------------------|
| `azurerm_dashboard_grafana.archiforge[0]` | `azurerm_dashboard_grafana.archlucid[0]` |
| `grafana_folder.archiforge[0]` | `grafana_folder.archlucid[0]` |
| `azurerm_monitor_alert_prometheus_rule_group.archiforge_slo[0]` | `azurerm_monitor_alert_prometheus_rule_group.archlucid_slo[0]` |

**IaC `moved` blocks (preferred for Terraform ≥ 1.5):** repo includes **`infra/terraform-monitoring/moved_archlucid_monitoring.tf`** paired with **`archlucid`** resource labels — on the next `terraform plan`, state rewrites without replacing Grafana / rule group resources. The imperative commands below remain valid for older workflows.

```bash
terraform state mv 'azurerm_dashboard_grafana.archiforge[0]' 'azurerm_dashboard_grafana.archlucid[0]'
terraform state mv 'grafana_folder.archiforge[0]' 'grafana_folder.archlucid[0]'
terraform state mv 'azurerm_monitor_alert_prometheus_rule_group.archiforge_slo[0]' 'azurerm_monitor_alert_prometheus_rule_group.archlucid_slo[0]'
```

### Other roots

Several modules carry only **comments** noting historical naming (`terraform-container-apps`, `terraform-edge`, `terraform-private`, `terraform-openai`, `terraform-entra`, `terraform-sql-failover`, root **`terraform/main.tf`**) without **`archiforge`** resource **names** in `.tf` at this sweep. Re-grep before execution:

```bash
rg "archiforge" infra --glob "*.tf"
```

## Validation

After all moves for a root:

```bash
terraform plan -detailed-exitcode
```

Expect **no** changes attributable to renames. **`detailed-exitcode`** **0** = empty plan; **2** = non-empty (investigate before apply).

## Rollback

Reverse each move:

```bash
terraform state mv 'azurerm_api_management.archlucid[0]' 'azurerm_api_management.archiforge[0]'
```

Restore from **state backup** only if the session produced an inconsistent state (coordinate with platform team).

## Notes

- **Do not** run **`terraform apply`** solely to “fix” names — **`state mv`** is the safe path.
- After state alignment, a **follow-up** PR may rename resources in **`.tf`** files to match addresses (that PR must keep **`plan`** empty by pairing code edits with the **already-moved** state).
