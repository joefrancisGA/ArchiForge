> **Scope:** First Azure deployment (ArchLucid) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# First Azure deployment (ArchLucid)

**Audience:** Platform engineers provisioning ArchLucid in Azure for the **first time** (no prior Terraform state or ArchLucid resources in the subscription).

**Last reviewed:** 2026-04-19

## Objective

Provide a **preflight checklist**, **backend configuration** for Terraform remote state, and **apply order** aligned with [`REFERENCE_SAAS_STACK_ORDER.md`](REFERENCE_SAAS_STACK_ORDER.md). **Default:** start from **[`infra/terraform-pilot/`](../../infra/terraform-pilot/README.md)** (canonical pilot profile; no Azure resources in that root). Use **per-root** applies only when you intentionally opt into the **multi-root** path documented there. This doc does not replace each root’s `README.md` — read those for variable semantics.

## Assumptions

- You have an **Azure subscription** where you can create resource groups, and a **service principal** (or user) with rights to deploy the stacks you enable.
- **Container images** exist in **ACR** (or another registry) at tags referenced in `terraform-container-apps` tfvars — build from CI or locally per [`CONTAINERIZATION.md`](CONTAINERIZATION.md).
- **No remote Terraform state** exists yet — configure a backend **before** the first `terraform apply` in each root (see below).

## Constraints

- **SMB / port 445** must not be exposed publicly; align private file shares with [`terraform-private`](../../infra/terraform-private/README.md) and org policy.
- **Least privilege:** prefer managed identities and Key Vault references over long-lived secrets in tfvars committed to git.

## Preflight — obtain before `terraform init`

| Item | Purpose |
|------|---------|
| **Subscription ID** | `az account show --query id -o tsv` — for the canonical `staging` / `production` IDs and how they map to the CD pipeline's GitHub Environment secrets, see [`AZURE_SUBSCRIPTIONS.md`](AZURE_SUBSCRIPTIONS.md). |
| **Tenant ID** | Same account / Entra tenant |
| **Service principal** (recommended) | App registration + client secret or federated credential for CI/CD |
| **Remote state backend** | Typically **Azure Storage** account + container for `.tfstate` blobs (create once; separate container or key per root is common) |
| **State backend auth** | `ARM_*` env vars or `az login` for interactive use |

### Example: backend block (per root)

Each `infra/terraform-*/` root may ship `backend.tf` or document backend in its README. Typical pattern:

```hcl
terraform {
  backend "azurerm" {
    resource_group_name  = "rg-archlucid-tfstate"
    storage_account_name = "starchlucidtfstate"
    container_name         = "tfstate"
    key                    = "private.tfstate" # unique per root
  }
}
```

Create the storage account and container **once** (portal, `az storage account create`, or a tiny bootstrap stack), then run `terraform init` with that backend config. Use a **different `key`** (or prefix) per root so state files do not collide.

## Apply order

1. **Pilot profile (default):** run `terraform init` / `plan` / `apply` in **[`infra/terraform-pilot/`](../../infra/terraform-pilot/README.md)** to validate FinOps variables and read **`nested_infrastructure_roots`** from outputs. Configure remote state for this root if you track profile state (often a small dedicated key).

2. **Multi-root (opt-in):** follow the **numbered table** in [`REFERENCE_SAAS_STACK_ORDER.md`](REFERENCE_SAAS_STACK_ORDER.md) — separate backend key per directory. Summary:

   1. `infra/terraform-private` — VNet, private endpoints, DNS.
   2. `infra/terraform-keyvault`
   3. `infra/terraform-sql-failover`
   4. `infra/terraform-storage`
   5. `infra/terraform-servicebus`
   6. `infra/terraform-logicapps` (optional Logic Apps Standard hosts)
   7. `infra/terraform-openai` (optional budgets)
   8. `infra/terraform-entra` — **creates** Entra app registrations on first apply (no brownfield rename needed).
   9. `infra/terraform-container-apps` — API, Worker, UI.
   10. `infra/terraform-edge` — Front Door / WAF.
   11. `infra/terraform` — optional Consumption APIM (API name **`archlucid-api`** in Azure).
   12. `infra/terraform-monitoring`
   13. `infra/terraform-orchestrator` (if used)

## Per-root workflow (multi-root opt-in path — repeat for each root in order)

```bash
cd infra/terraform-private   # example
terraform init               # add -backend-config=... if using partial backend
terraform plan -out=tfplan
terraform apply tfplan
```

- **First time only:** `terraform init` may prompt to create the backend; confirm the storage account exists.
- **Secrets:** pass sensitive values via `TF_VAR_*` environment variables or `-var-file` paths that are **gitignored** — never commit real secrets.

## Post-deploy smoke

After Container Apps (and optionally edge) are up:

1. Hit **`/health/live`** and **`/health/ready`** on the API base URL.
2. Fetch **`/openapi/v1.json`** and confirm contract sanity.
3. Run **`scripts/ci/cd-post-deploy-verify.sh`** if documented in [`DEPLOYMENT_CD_PIPELINE.md`](DEPLOYMENT_CD_PIPELINE.md).

## Rollback / destroy

Pre-production: destroying stacks is acceptable — `terraform destroy` per root **in reverse dependency order** (monitoring → edge → container-apps → … → private). Production: follow org change control; data-plane resources (SQL, storage) may require backups before destroy.

## Related

- [`DEPLOYMENT_TERRAFORM.md`](DEPLOYMENT_TERRAFORM.md) — full Terraform map and constraints.
- [`AZURE_SUBSCRIPTIONS.md`](AZURE_SUBSCRIPTIONS.md) — canonical subscription IDs, regions, and CD secret mapping.
- [`runbooks/TERRAFORM_STATE_MV_PHASE_7_5.md`](../runbooks/TERRAFORM_STATE_MV_PHASE_7_5.md) — stub; greenfield uses `archlucid` IaC only. Brownfield **state mv** archive: [`archive/TERRAFORM_STATE_MV_PHASE_7_5_2026_04.md`](../archive/TERRAFORM_STATE_MV_PHASE_7_5_2026_04.md).
- [`ARCHLUCID_RENAME_CHECKLIST.md`](../ARCHLUCID_RENAME_CHECKLIST.md) — Phase 7 items still deferred: GitHub repo rename (7.6), local workspace path (7.8).
