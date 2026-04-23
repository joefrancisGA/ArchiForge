> **Scope:** Reference Azure SaaS stack order (Terraform) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Reference Azure SaaS stack order (Terraform)

**Objective:** Give platform engineers a **default apply order** for ArchLucid Terraform roots under `infra/`, aligned with private networking and least-privilege identity.

**Last reviewed:** 2026-04-21

**Note:** Greenfield IaC uses **`archlucid`** resource labels and example names; CI rejects the substring `archiforge` in any `infra/**/*.tf` file. First deploy: [FIRST_AZURE_DEPLOYMENT.md](FIRST_AZURE_DEPLOYMENT.md). Brownfield **state mv** (legacy state only): [archive/TERRAFORM_STATE_MV_PHASE_7_5_2026_04.md](../archive/TERRAFORM_STATE_MV_PHASE_7_5_2026_04.md).

**Default primary region (2026-04-21):** **`centralus`** for new production Terraform applies (`infra/terraform-container-apps` and related roots) unless data-residency or latency requirements dictate otherwise. Document exceptions in the environment README.

**Subscription mapping:** see [`AZURE_SUBSCRIPTIONS.md`](AZURE_SUBSCRIPTIONS.md) for the canonical `staging` / `production` / greenfield-CI subscription IDs and the GitHub Environment secret each one maps to. Do **not** hard-code subscription IDs in `infra/**/*.tf` or example tfvars — `azure/login@v2` exports `ARM_SUBSCRIPTION_ID` for every Terraform step in the CD pipeline.

---

## Default path: `infra/terraform-pilot` (canonical profile)

Use **[`infra/terraform-pilot/`](../../infra/terraform-pilot/README.md)** as the **single default Terraform entry** in this repository:

- **Opinionated FinOps knobs** (`pilot_monthly_budget_usd`, `app_insights_sampling_percent`, …) live in that root’s variables.
- **`nested_infrastructure_roots`** (Terraform **output**) lists the **same nested order** as the advanced table below — use `terraform output` from `terraform-pilot` when you need machine-readable sequencing without reading docs.
- This root **does not create Azure resources**; it collapses operational guidance into one `terraform plan`/`apply` for profile validation and outputs.

**Script default:** [`infra/apply-saas.ps1`(../../infra/apply-saas.ps1) runs **only** `terraform-pilot` unless you pass **`-MultiRoot`** (opt-in multi-root path).

---

## Advanced (opt-in): multi-root separate state

Apply each directory below **in order** with **its own backend key** when you need **separate state files** per stack (blast-radius isolation, team ownership). This is the **legacy** operator workflow — still fully supported.

| Order | Root | Purpose |
|------:|------|---------|
| 1 | `infra/terraform-private` | VNet, private endpoints, DNS — **foundation** for data planes. |
| 2 | `infra/terraform-keyvault` | Secrets vault (references from later roots). |
| 3 | `infra/terraform-sql-failover` | Azure SQL + optional **failover group** / consumption budget. |
| 4 | `infra/terraform-storage` | Blob/queue accounts for artifacts and jobs. |
| 5 | `infra/terraform-servicebus` | Optional durable messaging for integration consumers; optional **Logic App–scoped** topic subscriptions (governance, trial email, ChatOps, prod promotion, **Marketplace fulfillment** via `enable_logic_app_marketplace_fulfillment_subscription`) for filtered triggers. |
| 6 | `infra/terraform-logicapps` | Optional **Logic App (Standard)** hosts (ADR 0019): **edge**, optional dedicated sites for **governance**, **Marketplace fulfillment**, **trial lifecycle email**, **incident ChatOps**, **promotion customer notify**; apply after messaging + private DNS exist. |
| 7 | `infra/terraform-openai` | Optional **budget** hooks for Azure OpenAI (resource creation may be out-of-band). |
| 8 | `infra/terraform-entra` | App registrations / consent text for API + UI. |
| 9 | `infra/terraform-container-apps` | **API + Worker + UI** workloads, managed identity wiring. |
| 10 | `infra/terraform-edge` | Front Door / WAF / routing to Container Apps. |
| 11 | `infra/terraform` | Optional **Consumption APIM** in front of public HTTPS backend — not a substitute for Premium VNet-injected APIM in all topologies. |
| 12 | `infra/terraform-monitoring` | Log Analytics, Grafana/Prometheus, alert rules, dashboards. |
| 13 | `infra/terraform-orchestrator` | Optional orchestration / automation root (if used in your fork). |

CI validates **`terraform validate`** + **Trivy config** across these roots (see `.github/workflows/ci.yml`) **and** `infra/terraform-pilot`.

---

## SaaS-shaped API profile (optional)

| Artifact | Purpose |
|----------|---------|
| [`ArchLucid.Api/appsettings.SaaS.json`(../../ArchLucid.Api/appsettings.SaaS.json) | Optional settings file chained from `Program.cs` after base `appsettings*.json` — **no secrets** in repo; API keys remain **off** until you wire keys + flip `Authentication:ApiKey:Enabled`. |

---

## GitHub Actions repository variables (hosted probes)

| Variable | Used by | Purpose |
|----------|---------|---------|
| **`ARCHLUCID_STAGING_BASE_URL`** | [`.github/workflows/hosted-saas-probe.yml`(../../.github/workflows/hosted-saas-probe.yml) | Public HTTPS origin for scheduled `curl` checks against `/health/live` and `/health/ready` (example: `https://staging.archlucid.com`). When unset, the workflow **skips** probes so forks do not fail. |
| **`ARCHLUCID_GOLDEN_COHORT_BASELINE_LOCKED`** | [`.github/workflows/golden-cohort-nightly.yml`(../../.github/workflows/golden-cohort-nightly.yml) | When `true`, runs simulator drift after the JSON contract job. |
| **`ARCHLUCID_GOLDEN_COHORT_REAL_LLM`** | `golden-cohort-nightly.yml` | When `true`, runs the Azure Cost Management budget probe + optional real-LLM gate tests (requires secrets + owner budget approval). |

---

## Post-deploy verification

After image rollout, run **`scripts/ci/cd-post-deploy-verify.sh`** against the public or private base URL ([DEPLOYMENT_CD_PIPELINE.md](DEPLOYMENT_CD_PIPELINE.md)): `/health/live`, `/health/ready` (**Healthy**), `/openapi/v1.json` (contract sanity), `/version`.

---

## Buyer CI integrations (GitHub + Azure DevOps)

Manifest delta surfaces (`GET /v1/compare`) ship as **GitHub composite actions** and **Azure Pipelines templates** in-repo — see the navigator in **[`integrations/GITHUB_ACTION_MANIFEST_DELTA.md`](../integrations/GITHUB_ACTION_MANIFEST_DELTA.md)** (links to GitHub + Azure DevOps + optional server-side Worker path).

## Related

- [DEPLOYMENT_TERRAFORM.md](DEPLOYMENT_TERRAFORM.md) — full root map and constraints.
- [RTO_RPO_TARGETS.md](RTO_RPO_TARGETS.md) — recovery targets by tier.
- [CUSTOMER_TRUST_AND_ACCESS.md](CUSTOMER_TRUST_AND_ACCESS.md) — private data plane narrative.
- [AZURE_SUBSCRIPTIONS.md](AZURE_SUBSCRIPTIONS.md) — canonical subscription IDs, regions, and CD secret mapping.
