> **Scope:** Superseded: Terraform state mv (Phase 7.5) - full detail, tables, and links in the sections below.

# Superseded: Terraform `state mv` (Phase 7.5)

This runbook was written for **brownfield** Terraform state that still referenced historical `archiforge` resource **addresses**.

**Greenfield (no Azure deployment yet):** Phase **7.5** is complete in the main branch — `moved {}` blocks were removed, all `infra/**/*.tf` sources use `archlucid` naming, and the Consumption APIM API Azure name is **`archlucid-api`**. CI fails if the substring `archiforge` appears in any Terraform (`.tf`) file under `infra/`.

**What to read instead**

| Audience | Document |
|----------|----------|
| First subscription deploy | [`docs/FIRST_AZURE_DEPLOYMENT.md`](../FIRST_AZURE_DEPLOYMENT.md) |
| Apply order | [`docs/REFERENCE_SAAS_STACK_ORDER.md`](../REFERENCE_SAAS_STACK_ORDER.md) |
| Full Terraform map | [`docs/DEPLOYMENT_TERRAFORM.md`](../DEPLOYMENT_TERRAFORM.md) |
| Rename checklist | [`docs/ARCHLUCID_RENAME_CHECKLIST.md`](../ARCHLUCID_RENAME_CHECKLIST.md) |

**Brownfield operators** (existing remote state with old addresses): see the archived runbook body in [`docs/archive/TERRAFORM_STATE_MV_PHASE_7_5_2026_04.md`](../archive/TERRAFORM_STATE_MV_PHASE_7_5_2026_04.md).
