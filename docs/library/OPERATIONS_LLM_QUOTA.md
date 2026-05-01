> **Scope:** Operations — LLM token quota and metrics - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Operations — LLM token quota and metrics

**Last reviewed:** 2026-05-01

## Configuration

| Key | Purpose |
|-----|---------|
| `LlmTokenQuota:Enabled` | Turn on sliding-window per-tenant limits. |
| `LlmTokenQuota:WindowMinutes` | Window length (1–1440). |
| `LlmTokenQuota:MaxPromptTokensPerTenantPerWindow` | Cap on **input** tokens summed in the window (0 = unlimited). |
| `LlmTokenQuota:MaxCompletionTokensPerTenantPerWindow` | Cap on **output** tokens summed in the window (0 = unlimited). |
| `LlmTokenQuota:AssumedMaxPromptTokensPerRequest` | Pre-flight guard before usage is known. |
| `LlmTokenQuota:AssumedMaxCompletionTokensPerRequest` | Pre-flight guard before usage is known. |
| `LlmMonthlyTenantDollarBudget:Enabled` | Turn on UTC-month **estimated USD** limits per tenant (requires **`AgentExecution:LlmCostEstimation`** with **positive** USD/M rates). |
| `LlmMonthlyTenantDollarBudget:IncludedUsdPerUtcMonth` | “Included” band; warn fires at `IncludedUsdPerUtcMonth * WarnFraction`. |
| `LlmMonthlyTenantDollarBudget:HardCutoffUsdPerUtcMonth` | Block real-mode completions when **cumulative estimated USD** this UTC month would exceed this value (pre-call uses assumed token upper bounds). |
| `LlmMonthlyTenantDollarBudget:WarnFraction` | Fraction of **included** USD at which **`LlmTenantMonthlyDollarBudgetApproaching`** is logged (once per tenant per UTC month). |
| `LlmMonthlyTenantDollarBudget:AssumedMaxPromptTokensPerRequest` | Pre-flight USD reservation before usage returns. |
| `LlmMonthlyTenantDollarBudget:AssumedMaxCompletionTokensPerRequest` | Pre-flight USD reservation before usage returns. |
| `LlmTelemetry:RecordPerTenantTokens` | Emit Prometheus series with `tenant_id` label (raises cardinality — enable only for bounded tenant counts). |

When quota is exceeded, the API returns **429** with problem type `#llm-token-quota-exceeded`. When the server can compute a retry instant (sliding-window expiry or next UTC-day budget boundary), the problem payload may include a **`retryAfterUtc`** extension (same shape as circuit-breaker **`retryAfterUtc`**). **Monthly dollar budget** uses **next UTC month start** for **`retryAfterUtc`** when the hard cutoff trips. OpenTelemetry counter **`archlucid_llm_quota_exceeded_total`** increments once per rejected pre-call (accounting decorator). Agent execution traces persist **`FailureReasonCode`=`LlmTokenQuotaExceeded`** when quota ends a handler.

## Metrics

- Aggregate (default): `archlucid_llm_prompt_tokens_total`, `archlucid_llm_completion_tokens_total` without tenant labels.
- Per-tenant (optional): same metric names **with** `tenant_id` label when `LlmTelemetry:RecordPerTenantTokens` is true.

Use Grafana dashboard **`infra/grafana/dashboard-archlucid-authority.json`** (LLM panels) and **`dashboard-archlucid-slo.json`** for HTTP latency objectives.

## FinOps

Combine these metrics with Azure Cost Management tags from Terraform (`finops_environment`, `finops_cost_center` in `infra/terraform-container-apps`).
