> **Scope:** Superseded: Terraform state mv (Phase 7.5) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Superseded: Terraform `state mv` (Phase 7.5)

This runbook was written for **brownfield** Terraform state that still referenced historical `archiforge` resource **addresses**.

**Greenfield (no Azure deployment yet):** Phase **7.5** is complete in the main branch — `moved {}` blocks were removed, all `infra/**/*.tf` sources use `archlucid` naming, and the Consumption APIM API Azure name is **`archlucid-api`**. Re-grep **`rg "archiforge" infra --glob "*.tf"`** before merging Terraform changes (historical CI grep job retired).

**What to read instead**

| Audience | Document |
|----------|----------|
| First subscription deploy | [`docs/FIRST_AZURE_DEPLOYMENT.md`](../library/FIRST_AZURE_DEPLOYMENT.md) |
| Apply order | [`docs/REFERENCE_SAAS_STACK_ORDER.md`](../library/REFERENCE_SAAS_STACK_ORDER.md) |
| Full Terraform map | [`docs/DEPLOYMENT_TERRAFORM.md`](../library/DEPLOYMENT_TERRAFORM.md) |
| Rename checklist | [`docs/ARCHLUCID_RENAME_CHECKLIST.md`](../ARCHLUCID_RENAME_CHECKLIST.md) |

**Brownfield operators** (existing remote state with old addresses): see the archived runbook body in [`docs/archive/TERRAFORM_STATE_MV_PHASE_7_5_2026_04.md`](../archive/TERRAFORM_STATE_MV_PHASE_7_5_2026_04.md).
