> **Scope:** Per-tenant cost model (sketch) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Per-tenant cost model (sketch)

## Objective

Give sponsors a **defensible order-of-magnitude** for monthly Azure + LLM spend for ArchLucid **without** turning the pilot into a FinOps science project.

## Assumptions

- **Azure list** public pricing (enterprise discounts apply separately).
- **LLM** is **metered** via `archlucid_llm_prompt_tokens_total` / `archlucid_llm_completion_tokens_total` and optional **`archlucid_llm_cost_usd_total`** when `AgentExecution:LlmCostEstimation:Enabled` is **true** ([../CAPACITY_AND_COST_PLAYBOOK.md](../library/CAPACITY_AND_COST_PLAYBOOK.md)).

## Constraints

- **Not** a billing entitlement model — **no** silent SKU coupling to product packaging.
- Cardinality: avoid unbounded **per-tenant** metric labels in Prometheus except bounded pilots.

## Architecture overview

**Cost drivers:** compute (Container Apps), **SQL** DTU/vCore, **blob** + egress for artifacts/traces, **Service Bus** messages, **App Insights / log** ingestion, **Azure OpenAI** tokens, optional **Front Door**.

## Component breakdown (line items)

| Line item | Pilot (order of magnitude) | Standard | Heavy |
|-----------|----------------------------|----------|-------|
| **Container Apps** (API+Worker+UI vCPU-seconds) | Lowest SKU, autoscale capped | Mid replicas | Peak replicas + always-on min |
| **Azure SQL** | Basic / small GP | GP moderate | Business Critical / larger vCore |
| **Storage (blobs)** | Few GB | 10–100 GB class | Large trace retention |
| **Service Bus** | Optional / low message volume | Standard topic load | High fan-out |
| **Application Insights** | **Surprise line item** — cap daily quota | Raised cap | Multiple envs |
| **Azure OpenAI** | **Dominated by token throughput** — few pilots vs many runs | Moderate | Multi-agent batches |
| **Front Door** | Omitted in ultra-pilot | Standard tier | WAF + rulesets |

## Data flow

**Runs** → agent batches → **token counters** + **SQL writes** → **outbox/events** → optional integrations. Each hop accrues **compute + storage + message + log** cost.

## Security model

**Private networking** reduces data-exfil risk but **does not remove** metered egress for legitimate exports — still cost-relevant.

## Operational considerations

- **Throttle** LLM with quotas (`OPERATIONS_LLM_QUOTA.md` patterns) before **scaling SQL** for pilot noise.
- **Monthly review:** follow [../CAPACITY_AND_COST_PLAYBOOK.md](../library/CAPACITY_AND_COST_PLAYBOOK.md) cadence.

## Worked example (illustrative only)

**Inputs:** 50 runs/month, ~8 LLM calls/run median, modest prompts. **Pilot profile:** single-region SQL Basic, Container Apps with `minReplicas=0`, **no** Front Door, App Insights sampling 25%. **Math:** sum Container Apps hours × tariff + SQL DTU hours + (**tokens**/1e6)×($/M input+$/M output from your Azure OpenAI list price) + log GB × ingestion rate.

Treat the result as **±40%** until you have **two months** of real meters — see **`docs/BILLING.md`** for marketplace dynamics when applicable.
