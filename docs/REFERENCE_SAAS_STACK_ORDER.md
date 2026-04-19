# Reference Azure SaaS stack order (Terraform)

**Objective:** Give platform engineers a **default apply order** for ArchLucid Terraform roots under `infra/`, aligned with private networking and least-privilege identity.

**Last reviewed:** 2026-04-19

**Note:** Resource **addresses** in state may still use legacy names until Phase **7.5** (`terraform state mv`) — see [runbooks/TERRAFORM_STATE_MV_PHASE_7_5.md](runbooks/TERRAFORM_STATE_MV_PHASE_7_5.md) and [ARCHLUCID_RENAME_CHECKLIST.md](ARCHLUCID_RENAME_CHECKLIST.md).

---

## Recommended apply sequence

| Order | Root | Purpose |
|------:|------|---------|
| 1 | `infra/terraform-private` | VNet, private endpoints, DNS — **foundation** for data planes. |
| 2 | `infra/terraform-keyvault` | Secrets vault (references from later roots). |
| 3 | `infra/terraform-sql-failover` | Azure SQL + optional **failover group** / consumption budget. |
| 4 | `infra/terraform-storage` | Blob/queue accounts for artifacts and jobs. |
| 5 | `infra/terraform-servicebus` | Optional durable messaging for integration consumers; optional **Logic App–scoped** topic subscriptions (governance, trial email, ChatOps, prod promotion, **Marketplace fulfillment** via `enable_logic_app_marketplace_fulfillment_subscription`) for filtered triggers. |
| 6 | `infra/terraform-logicapps` | Optional **Logic App (Standard)** hosts for Service Bus–driven edge orchestration (ADR 0019): generic **edge**, optional **governance** and **Marketplace fulfillment** dedicated sites; apply after messaging + private DNS exist. |
| 7 | `infra/terraform-openai` | Optional **budget** hooks for Azure OpenAI (resource creation may be out-of-band). |
| 8 | `infra/terraform-entra` | App registrations / consent text for API + UI. |
| 9 | `infra/terraform-container-apps` | **API + Worker + UI** workloads, managed identity wiring. |
| 10 | `infra/terraform-edge` | Front Door / WAF / routing to Container Apps. |
| 11 | `infra/terraform` | Optional **Consumption APIM** in front of public HTTPS backend — not a substitute for Premium VNet-injected APIM in all topologies. |
| 12 | `infra/terraform-monitoring` | Log Analytics, Grafana/Prometheus, alert rules, dashboards. |
| 13 | `infra/terraform-orchestrator` | Optional orchestration / automation root (if used in your fork). |

CI validates **`terraform validate`** + **Trivy config** across these roots (see `.github/workflows/ci.yml`).

---

## Post-deploy verification

After image rollout, run **`scripts/ci/cd-post-deploy-verify.sh`** against the public or private base URL ([DEPLOYMENT_CD_PIPELINE.md](DEPLOYMENT_CD_PIPELINE.md)): `/health/live`, `/health/ready` (**Healthy**), `/openapi/v1.json` (contract sanity), `/version`.

---

## Related

- [DEPLOYMENT_TERRAFORM.md](DEPLOYMENT_TERRAFORM.md) — full root map and constraints.
- [RTO_RPO_TARGETS.md](RTO_RPO_TARGETS.md) — recovery targets by tier.
- [CUSTOMER_TRUST_AND_ACCESS.md](CUSTOMER_TRUST_AND_ACCESS.md) — private data plane narrative.
