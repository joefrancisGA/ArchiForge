> **Scope:** ArchForge → ArchLucid — deferred rationale (historical) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# ArchForge → ArchLucid — deferred rationale (historical)

**Status (2026-04-19):** The **rename initiative is closed.** This file is retained as a short historical note. Current state: GitHub **`joefrancisGA/ArchLucid`**, greenfield Terraform uses **`archlucid`** naming, **7.8** (local folder path) **waived** by owner.

## What was deferred and how it resolved

| Item | Resolution |
|------|------------|
| **7.5** Terraform | **Done** — greenfield IaC; see [`FIRST_AZURE_DEPLOYMENT.md`](FIRST_AZURE_DEPLOYMENT.md), [`archive/TERRAFORM_STATE_MV_PHASE_7_5_2026_04.md`](../archive/TERRAFORM_STATE_MV_PHASE_7_5_2026_04.md) for brownfield only. |
| **7.6** GitHub repo | **Done** — `joefrancisGA/ArchLucid`. |
| **7.7** Entra | **N/A greenfield** — first `terraform apply` under `infra/terraform-entra/`. |
| **7.8** Local workspace path | **Waived** — optional; local path may remain `ArchiForge`-named; does not affect product. |

## Brownfield Terraform (rare)

If remote state still used historical `*.archiforge` addresses, see [`docs/archive/TERRAFORM_STATE_MV_PHASE_7_5_2026_04.md`](../archive/TERRAFORM_STATE_MV_PHASE_7_5_2026_04.md). New subscriptions skip this.
