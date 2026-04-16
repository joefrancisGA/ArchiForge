# Infrastructure operations (Terraform roots)

**Last reviewed:** 2026-04-16

## Objective

Give operators a single place to orient on **multiple Terraform roots**, what each is for, and how to validate or triage them without applying blindly.

## Assumptions

- You have Azure credentials and subscription context appropriate for the stack (`az login`, correct subscription).
- Remote state backends (if enabled) are already configured for your environment; CI runs `terraform init -backend=false` and `validate` only.

## Constraints

- Production changes follow your change window and approval process; this document does not replace enterprise change management.
- SMB (port 445) must not be exposed publicly; storage integration should follow private endpoints and network boundaries (see workspace security rules).

## Architecture overview (nodes)

| Node (directory) | Purpose |
|------------------|---------|
| `infra/terraform` | Core Azure resources (e.g. App Service, optional APIM consumption). |
| `infra/terraform-edge` | Edge / WAF (e.g. Front Door Standard + WAF) when enabled. |
| `infra/terraform-entra` | Optional Entra ID application registration and app roles. |
| `infra/terraform-private` | VNet, private DNS, private endpoints for SQL and Blob. |

**Edges:** each root consumes variables and outputs documented in `infra/README.md`; the API and data plane depend on DNS and private connectivity when `enable_private_data_plane` (or equivalent) is true.

## Component breakdown

- **Interfaces:** Terraform variables and outputs per root; no runtime code here.
- **Services:** Azure resources declared per module.
- **Data models:** N/A (IaC only).
- **Orchestration:** CI job `terraform-validate-public-stacks` (main / edge / entra) plus `terraform-validate-private` for the private stack; local `terraform plan` before apply.

## Operational flow

1. **Validate locally (no backend):**  
   `terraform init -backend=false && terraform validate && terraform fmt -check -recursive` in the target directory.
2. **Plan:**  
   `terraform plan` with the correct `tfvars` for the environment.
3. **Apply:**  
   Only after review; capture outputs (gateway URLs, identity IDs, endpoint FQDNs) in your CMDB or secret store as required.

## Security model

- Least privilege for deployment principals; separate plan vs apply where possible.
- Private data plane stacks assume public access to SQL/Blob is disabled after cutover (see private stack docs).

## Reliability and cost

- **Reliability:** Prefer staged rollout (dev → staging → prod) and health checks on App Service / Front Door backends after changes.
- **Cost:** APIM consumption, Front Door, and private endpoints affect monthly burn; review SKUs in `infra/README.md` and Azure pricing before enabling flags.

## When things go wrong

- **Validate fails in CI:** Fix HCL or provider version mismatches; ensure required variables have defaults for validate-only runs.
- **Plan shows unexpected destroys:** Stop; review state and module upgrades; avoid auto-apply.
- **Entra / JWT issues after Entra stack changes:** See `docs/CUSTOMER_TRUST_AND_ACCESS.md` and API `ArchLucidAuth` settings.

## Scalability

Terraform roots are intentionally split so teams can enable edge or Entra independently of the core app stack; scale limits are Azure SKU–specific (documented in module READMEs).
