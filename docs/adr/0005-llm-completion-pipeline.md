> **Scope:** ADR 0005: LLM completion pipeline (cache, circuit breaker, quota, metrics) - full detail, tables, and links in the sections below.

# ADR 0005: LLM completion pipeline (cache, circuit breaker, quota, metrics)

- **Status:** Accepted
- **Date:** 2026-04-04

## Context

Azure OpenAI calls need resilience (circuit breaker), cost control (cache, quota), and observability (tokens, traces).

## Decision

Pipeline order from the wire: **`CircuitBreaking( Caching( LlmCompletionAccounting( AzureOpenAi ) ) )`**.

- **Accounting** (scoped): pre-check quota, post-record usage, emit OTel counters (optional per-tenant labels).
- **Caching** sits inside the breaker so hits do not trip failure counting.
- **Azure** client records usage into `AsyncLocal` consumed by accounting after each call.

## Consequences

- **Positive:** Scoped `IAgentCompletionClient` works with singleton `IScopeContextProvider`; quota is tenant-aware on HTTP and ambient scope jobs.
- **Negative:** `IAgentCompletionClient` is no longer a singleton; registrations must use scopes correctly in custom hosts.
- **Parallel agent batch:** `RealAgentExecutor` runs multiple `IAgentHandler` tasks concurrently; `AmbientScopeContext.Push` (from `IScopeContextProvider.GetCurrentScope()` at batch start) keeps accounting and quota tenant-aware on thread-pool continuations without relying on `HttpContext` on every continuation.

## Links

- `docs/OPERATIONS_LLM_QUOTA.md`
