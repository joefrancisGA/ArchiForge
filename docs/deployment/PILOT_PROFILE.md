> **Scope:** Pilot Terraform profile (cost-aware) - full detail, tables, and links in the sections below.

# Pilot Terraform profile (cost-aware)

## Objective

Run a **short-lived** ArchLucid environment (single region, reduced HA) to prove **Core Pilot** value without paying the full **production** multi-stack bill ([REFERENCE_SAAS_STACK_ORDER.md](../REFERENCE_SAAS_STACK_ORDER.md)).

## Assumptions

- **≤ 30-day** pilot window, **one tenant** (or two for A/B), **non-production** data classification.
- Team accepts **weaker RTO/RPO** than [../RTO_RPO_TARGETS.md](../RTO_RPO_TARGETS.md) production tiers.

## Constraints

- **Identity:** Entra ID (External ID or corporate tenant) — no anonymous public write paths.
- **Networking:** **Private endpoints** for data plane; **no SMB** on public internet (port 445).
- **Security:** API auth remains **`ApiKey`** or **`JwtBearer`** — **never** `DevelopmentBypass` in a deployed environment.
- **IaC:** Terraform roots under `infra/terraform-*` — CI forbids the substring `archiforge` in `infra/**/*.tf`.

## Architecture overview

**Nodes:** edge (optional) → Container Apps (API, Worker, UI) → Azure SQL → Storage/Key Vault → optional Service Bus / OpenAI / monitoring.

**Edges:** HTTPS ingress → API → SQL write path → authority pipeline → LLM (optional) → observability export.

## Component breakdown (pilot vs production)

| Layer | Pilot stance | Production reference |
|-------|--------------|----------------------|
| **Front Door / WAF** | **Omit** or smallest SKU; use Container Apps direct FQDN + TLS for internal pilots | `infra/terraform-edge` |
| **SQL** | Single region **Basic / S0–S2**; **no failover group** | `infra/terraform-sql-failover` |
| **Container Apps** | `minReplicas=0` acceptable in dev/pilot; cap `maxReplicas` low | `infra/terraform-container-apps` |
| **Monitoring** | Sample Application Insights / logs at **≤ 25–50%** documented rate; watch cardinality | `infra/terraform-monitoring` |
| **OpenAI** | Lower TPM quota; correlate with `archlucid_llm_*_tokens_total` | `infra/terraform-openai` |
| **Logic Apps / orchestrator** | Often **omitted** until integration hooks are in scope | `infra/terraform-logicapps`, `infra/terraform-orchestrator` |

## Data flow

Same logical path as production (request → run → manifest / artifacts); **only SLOs and spend** change.

## Security model

Private endpoints for **Key Vault** and **Storage**; least-privilege managed identities per workload. Secrets never in `terraform.tfvars` committed to git.

## Operational considerations

- **Apply order** still follows [../REFERENCE_SAAS_STACK_ORDER.md](../REFERENCE_SAAS_STACK_ORDER.md) — pilot **skips** optional roots rather than reordering network foundations.
- **Graduate** to production by **promoting tfvars + enabling** secondary pieces (Front Door, SQL failover, higher monitoring retention) — not by silent URL hacks.
- **Starter root:** [`infra/terraform-pilot/README.md`](../../infra/terraform-pilot/README.md) documents composition intent (thin wrapper; roots remain modular).

## Related

- [PER_TENANT_COST_MODEL.md](PER_TENANT_COST_MODEL.md) — line-item sketch.
- [../FIRST_AZURE_DEPLOYMENT.md](../FIRST_AZURE_DEPLOYMENT.md) — first land.
