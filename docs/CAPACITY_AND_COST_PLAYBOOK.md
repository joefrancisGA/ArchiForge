# Capacity and cost playbook

## 1. Objective

Give operators a **first-principles** way to scale ArchLucid/ArchLucid and control **Azure spend** without over-provisioning from day one.

## 2. Assumptions

- **Traffic grows unevenly**; teams may lack perfect forecasts.
- **Private networking** and **managed identity** are preferred over shared keys.

## 3. Constraints

- **Reliability** targets are environment-specific; this playbook suggests **indicators**, not universal SLOs.
- **FinOps** tags (`llm_provider`, `llm_deployment`) exist on token counters when enabled — watch **cardinality** in Prometheus.

## 4. Architecture overview

**Bottleneck classes:** API CPU/memory, Worker throughput, **SQL DTU/vCore**, **LLM token rate**, **Service Bus** throughput, **egress** from blob/diagnostic logs.

## 5. Component breakdown

| Layer | Scale signal | Knob (examples) |
|-------|--------------|------------------|
| **Container Apps** | CPU throttling, revision restarts | Increase CPU/memory; split API vs Worker replicas; min replicas in prod. |
| **SQL** | DTU/vCore maxed, long query store | Scale tier; index/outbox retention; archive cold runs. |
| **Outboxes** | Gauges in `ArchLucid` meter | Add worker instances; fix poison messages; use admin DLQ tools. |
| **LLM** | `archlucid_llm_*_tokens_total` | Cheaper deployment, caching, smaller prompts, quota per tenant. |
| **Front Door / APIM** | 429/latency at edge | Caching rules, rate limits, regional PoPs. |

## 6. Data flow

User/API load → compute → SQL write path → outbox/async → external integrations. **Cost** accrues on **compute hours**, **SQL**, **LLM tokens**, **egress**, and **observability retention**.

## 7. Security model

- Scaling **out** must not **widen** blast radius: maintain **private endpoints**, **least-privilege** RBAC, and **secret rotation** when adding replicas or regions.

## 8. Operational considerations

- **Start minimal:** single revision, modest SQL tier, Worker replica count aligned with outbox depth alerts (`infra/prometheus/archlucid-alerts.yml`).
- **Evolve:** separate **read scaling** (cached read models) from **write scaling** (partitioning hot tenants, job sharding) when metrics justify.
- **Review monthly:** top queries, token dashboards, unused environments, log retention.

## 9. Monthly FinOps review cadence

Schedule a **60–90 minute** monthly review owned by **platform + FinOps** (or engineering lead until a dedicated role exists). Goal: catch drift in spend and guardrails before invoice shock.

| Checkpoint | What to verify | Where / how |
|------------|----------------|-------------|
| **Consumption budgets** | Alerts received at **80% actual** and **100% forecasted**; amounts still bracket realistic spend | Azure Cost Management → budgets; stacks: `infra/terraform-sql-failover`, `terraform-container-apps`, `terraform-openai` — start from **`production.tfvars.example`** and tune `*_amount` |
| **Log Analytics cap** | Ingestion under **`daily_quota_gb`** or documented override | Container Apps stack: `log_analytics_daily_quota_gb` (workspace in `terraform-container-apps`; see comment in `terraform-monitoring/main.tf`) |
| **Edge WAF** | Front Door WAF **enabled** in production; managed rule version matches intent | `infra/terraform-edge` — `enable_front_door_waf`, `front_door_waf_default_rule_set_version` in **`production.tfvars.example`** |
| **LLM token quotas** | **`LlmTokenQuota:Enabled`** true in production; limits still generous enough for peak tenants | `ArchLucid.Api/appsettings.Production.json`; correlate with Prometheus `archlucid_llm_*_tokens_total` |
| **Idle capacity** | Non-prod environments scaled down or destroyed; replica mins justified | Container Apps min replicas; SQL tier; Grafana/Managed Grafana opt-in |

**Outputs:** short notes (what changed, what to tune next month) in the team channel or ticket; open work items only when a threshold or architecture change is required.

**Risk note:** Tight budgets or low log caps can **block or truncate** legitimate usage. Prefer **generous defaults** in examples, then **tighten** using two months of Cost Management data.
