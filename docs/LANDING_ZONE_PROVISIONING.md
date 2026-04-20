> **Scope:** Azure landing zone provisioning (ArchLucid) - full detail, tables, and links in the sections below.

# Azure landing zone provisioning (ArchLucid)

## Objective

Provide a **repeatable, script-driven** path to validate and (optionally) apply ArchLucid Terraform roots in **safe dependency order**, without merging unrelated stacks into a single Terraform state.

## Assumptions

- Operators have Azure CLI, Terraform 1.5+, and rights to the target subscription.
- Each root under `infra/terraform-*` keeps **its own backend** (or local state for experiments).
- Production cuts over private SQL/storage before disabling public endpoints (see `infra/terraform-private`).

## Constraints

- **Do not expose SMB (port 445)** on the public internet; align with `docs/CUSTOMER_TRUST_AND_ACCESS.md`.
- `terraform apply` without review can destroy resources — default automation uses **validate-only** mode.
- Greenfield IaC uses **`archlucid`** naming; first subscription deploy: [`docs/FIRST_AZURE_DEPLOYMENT.md`](FIRST_AZURE_DEPLOYMENT.md). Brownfield **state mv** (legacy state only): [`docs/archive/TERRAFORM_STATE_MV_PHASE_7_5_2026_04.md`](archive/TERRAFORM_STATE_MV_PHASE_7_5_2026_04.md).

## Architecture overview

**Nodes:** Terraform roots (storage, private networking, Container Apps, Entra, edge, monitoring, …).

**Edges:** Script order encodes soft dependencies (for example storage before Container Apps when large-payload offload is enabled).

**Flows:** `init` → `validate` → optional `plan`/`apply` per root.

## Component breakdown

| Artifact | Role |
|----------|------|
| `scripts/provision-landing-zone.ps1` | Windows entry: ordered `terraform` invocations. |
| `scripts/provision-landing-zone.sh` | POSIX entry: same order, validate-only by default. |
| `infra/environments/*.example.tfvars` | Non-secret sketches; copy into per-root `terraform.tfvars` or pass `-var-file`. |
| `infra/terraform-orchestrator/` | Minimal root for CI `terraform validate` / `fmt` only. |

## Data flow

1. Operator selects environment tier (dev / staging / prod) and prepares tfvars.
2. Run `.\scripts\provision-landing-zone.ps1 -ValidateOnly` (or `-DryRun` to print steps only).
3. For real infrastructure, configure remote backends per root, then re-run with `-ValidateOnly:$false` and inspect `terraform plan` per root before `-Apply`.

## Security model

- Secrets live in Key Vault / pipeline stores — not committed tfvars.
- Private endpoints and managed identity SQL are **staging/prod** defaults; dev may relax with explicit risk acceptance.

## Operational considerations

- Add CI coverage: `.github/workflows/ci.yml` includes `infra/terraform-orchestrator` in the Terraform validate matrix.
- After apply, run API smoke (`GET /health/ready`, `GET /version`) and `docs/V1_RELEASE_CHECKLIST.md` gates.

## Related

- `infra/README.md`
- `docs/GOLDEN_PATH.md`
- `docs/DEPLOYMENT_TERRAFORM.md`
