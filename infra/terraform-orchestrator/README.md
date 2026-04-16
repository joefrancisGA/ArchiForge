# Terraform orchestrator (validate-only root)

This directory is a **minimal Terraform configuration** used for:

- `terraform fmt -check` / `terraform validate` in CI (no Azure credentials required).
- Documentation anchor: stacks are **not** merged into one state file here.

## Why not one mega-root?

Each stack under `infra/terraform-*` owns its own backend and lifecycle (private networking, Container Apps, Entra, edge, storage, monitoring, SQL failover, OpenAI, Key Vault). Combining them would force `terraform state mv` coordination across unrelated blast radii.

Use **`scripts/provision-landing-zone.ps1`** (or **`.sh`**) with `-ValidateOnly` (default) or `-Apply` to run `init` + `plan`/`apply` per root in the order documented in **`docs/LANDING_ZONE_PROVISIONING.md`**.

## Local commands

```bash
cd infra/terraform-orchestrator
terraform init -backend=false
terraform validate
terraform fmt -check
```
