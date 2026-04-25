> **Scope:** Per-tenant and host-level LLM cost **estimation** methodology (not billing invoices).

# Per-tenant cost model (estimation)

This document describes how ArchLucid **approximates** Azure OpenAI spend for operators and FinOps workflows. It is **not** a substitute for Azure Cost Management + invoice reconciliation.

## Host-level rates (`AgentExecution:LlmCostEstimation`)

Runtime cost estimates use `ILlmCostEstimator`, which applies USD-per-million rates from configuration:

- `AgentExecution:LlmCostEstimation:InputUsdPerMillionTokens`
- `AgentExecution:LlmCostEstimation:OutputUsdPerMillionTokens`

When `AgentExecution:LlmCostEstimation:Enabled` is `false`, the estimator returns no USD value (previews show a null estimate).

## Wizard preview (`GET /v1/agent-execution/cost-preview`)

The operator **new-run wizard** review step calls this endpoint to show an **illustrative upper bound** before `POST /v1/architecture/request`:

- **Mode:** `AgentExecution:Mode` — the preview card is **hidden** when the host is `Simulator`.
- **Cap:** effective `AzureOpenAI:MaxCompletionTokens` (or the default **4096** when unset/zero).
- **Tokens assumed:** a single completion scenario with **8192** assumed input (prompt + system context order-of-magnitude) and **max completion** output tokens, both passed to `ILlmCostEstimator.EstimateUsd`. Actual runs vary with agents, retries, and tool traffic — treat the figure as **order-of-magnitude**, not a quote.

## Per-tenant dashboards

Aggregated tenant spend, budgets, and anomaly detection are **out of scope** for this file; see [`CAPACITY_AND_COST_PLAYBOOK.md`](CAPACITY_AND_COST_PLAYBOOK.md) for operational capacity guidance.
