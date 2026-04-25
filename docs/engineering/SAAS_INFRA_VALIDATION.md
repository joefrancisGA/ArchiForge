# SaaS infrastructure validation (Terraform)

**Purpose** — run **offline** checks so every Terraform “root” (deployable `infra/terraform*` tree plus `infra/modules/*` packages) **initializes** and **validates** without Azure credentials, and so **layout** matches `infra/apply-saas.ps1` and shared conventions.

**Prerequisites**

- [Terraform CLI](https://www.terraform.io/downloads) ≥ 1.8
- [PowerShell 7+](https://github.com/PowerShell/PowerShell) (`pwsh`) on the PATH
- **No** `az login` or other cloud credentials; uses `terraform init -backend=false` and `terraform validate` only (no `plan` / `apply`).

## What the scripts do

| Script | Checks |
|--------|--------|
| [scripts/validate-saas-infra.ps1](../../scripts/validate-saas-infra.ps1) | For each discovered root: `terraform init -backend=false` → `terraform validate`. Produces a **root \| init \| validate** summary table. Exits `1` if any step fails. |
| [scripts/validate-saas-config-consistency.ps1](../../scripts/validate-saas-config-consistency.ps1) | (1) Every `infra/...` path **quoted** in [infra/apply-saas.ps1](../../infra/apply-saas.ps1) exists. (2) **Warns** if a stack directory `infra/terraform*` is **not** listed in `apply-saas` (e.g. new `terraform-otel-collector`). (3) **azurerm** `version` constraints in each stack’s `versions.tf` are compared. (4) **Warns** if the same `variable` name has different `type` lines across `variables.tf` (best-effort). Use `-Strict` to fail on provider version drift. |

**CI** — [scripts/ci/assert_terraform_roots_valid.py](../../scripts/ci/assert_terraform_roots_valid.py) runs the two PowerShell entrypoints in order. Registered in [`.github/workflows/ci.yml`](../../.github/workflows/ci.yml) (job **SaaS: Terraform roots validate**) with `continue-on-error: true` until every root is known clean, then you can remove that flag to make it merge-blocking.

## How to run locally (repo root)

**All roots (full table)**

```bash
pwsh -File scripts/validate-saas-infra.ps1
pwsh -File scripts/validate-saas-config-consistency.ps1
# Optional: fail on provider version drift
pwsh -File scripts/validate-saas-config-consistency.ps1 -Strict
```

**One root (development)**

```bash
pwsh -File scripts/validate-saas-infra.ps1 -Root terraform-pilot
# or
pwsh -File scripts/validate-saas-infra.ps1 -Root infra/terraform-otel-collector
```

**Python (same as CI)**

```bash
python3 scripts/ci/assert_terraform_roots_valid.py
```

## Adding a new Terraform root and getting “included”

1. **Create the directory** under `infra/`, e.g. `infra/terraform-foo/`, with at least one `*.tf` file (and typically `versions.tf` / `providers.tf`).
2. **Run** `validate-saas-infra.ps1` and fix any validate errors.
3. **List the stack in** [infra/apply-saas.ps1](../../infra/apply-saas.ps1) — add a line to ` $multiRootSequence` (or `$pilotProfileOnly` for non-Azure pilot-only profiles) in **dependency order**; see [docs/library/REFERENCE_SAAS_STACK_ORDER.md](../library/REFERENCE_SAAS_STACK_ORDER.md) if you maintain ordering.
4. **Re-run** `validate-saas-config-consistency.ps1` and ensure there is **no** warning for “stack on disk is not in apply-saas”.

**Modules** — subfolders under `infra/modules/*` with `.tf` are validated by `validate-saas-infra.ps1` automatically; they are not added to `apply-saas` unless you intentionally deploy them as a standalone run.

## Count of roots (≈ 17+)

- **Stacks:** all `infra/terraform*` directories with `*.tf` (including [infra/terraform](../../infra/terraform) and e.g. [infra/terraform-otel-collector](../../infra/terraform-otel-collector)).
- **Reusable module packages:** each `infra/modules/<name>` that contains `*.tf`.

**Difference vs existing CI** — the workflow already runs `terraform init -backend=false` / `validate` / `fmt` / Trivy on a **matrix** of stacks. This suite **adds** modules, **adds** stacks that may be missing from the matrix (e.g. new roots), and **enforces** alignment with `apply-saas.ps1` without editing `.tf` files in this task.

## Operational notes

- `terraform init -backend=false` avoids configuring the remote `backend` block; validation does not touch real state.
- If a root references provider plugins you have not used before, the first `init` may **download** providers to `.terraform/providers` (local cache) — that is still offline from Azure and expected.
- Trivy / `terraform fmt` are **not** in these scripts; keep using the existing CI steps or add them in a follow-up if you want one script to do everything.
