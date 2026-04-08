# LLM per-call retry and circuit breaker

## Objective

Protect Azure OpenAI completion and embedding calls with a **Polly retry** layer **inside** the circuit breaker decorators (`CircuitBreakingAgentCompletionClient`, `CircuitBreakingOpenAiEmbeddingClient`). Transient faults (rate limits, 5xx, network, HTTP timeouts) are retried with exponential backoff and jitter **before** a single failure is recorded on the gate. This avoids tripping the breaker on short-lived outages while still opening the circuit on sustained failures.

## Assumptions

- Azure OpenAI is the primary LLM path in production; failures surface as `HttpRequestException`, `ClientResultException` (Azure SDK / `System.ClientModel`), or `TaskCanceledException` when the HTTP stack times out (token not cancelled).
- Operators tune behavior via `AgentExecution:Resilience` without code changes.
- Chaos validation runs in CI (including a weekly scheduled workflow); Simmy is not used in production.

## Constraints

- Retry must not wrap the circuit breaker **outside** (no retry-on-open-circuit loops).
- User-initiated cancellation (`OperationCanceledException` with requested cancellation) must not be retried and must not count as a breaker failure in the same way as dependency faults (see decorator implementation). The decorators call `ThrowIfCancellationRequested()` on the caller token **before** entering the Polly retry pipeline so a pre-cancelled call never hits the inner client even if Polly forwards a different token to the callback.
- `InvalidOperationException` (e.g. empty assistant payload) and `CircuitBreakerOpenException` are not retried.

## Architecture overview

`RealAgentExecutor` applies bulkhead concurrency and a per-handler timeout. The **LLM HTTP boundary** is the `IAgentCompletionClient` / `IOpenAiEmbeddingClient` pipeline registered in host composition. The circuit breaker decorator sits **outside** the inner Azure SDK client; the **retry pipeline** sits **inside** the try/catch that calls `RecordSuccess` / `RecordFailure` so the gate sees the outcome **after** retries.

```mermaid
flowchart LR
  subgraph executor [RealAgentExecutor]
    BG[Concurrency gate]
    TO[Timeout pipeline]
  end
  subgraph completion [Completion stack example]
    CB[CircuitBreakingAgentCompletionClient]
    RP[Polly retry LlmCallResilienceDefaults]
    CACHE[Caching / accounting layers]
    AZ[AzureOpenAiCompletionClient]
  end
  executor --> CB
  CB --> RP
  RP --> CACHE
  CACHE --> AZ
  AZ --> API[Azure OpenAI API]
```

Embeddings follow the same idea: `CircuitBreakingOpenAiEmbeddingClient` → retry → `AzureOpenAiEmbeddingClient`.

## Component breakdown

| Component | Role |
|-----------|------|
| `AgentExecutionResilienceOptions` | `LlmCallMaxRetryAttempts`, `LlmCallBaseDelayMilliseconds`, `LlmCallMaxDelaySeconds`; `Normalize()` clamps ranges. |
| `LlmCallResilienceDefaults.BuildLlmRetryPipeline` | Stateless `ResiliencePipeline` with exponential backoff + jitter; `ShouldRetryLlmException` classifies exceptions. |
| `CircuitBreakingAgentCompletionClient` | `ThrowIfBroken` → `ExecuteAsync` through retry → `RecordSuccess` / `RecordFailure` / `RecordCallCancelled`. |
| `CircuitBreakingOpenAiEmbeddingClient` | Same for `EmbedAsync` / `EmbedManyAsync`. |
| `ArchLucidInstrumentation.LlmCallRetries` | Counter `archlucid_llm_call_retries_total` (tags: `gate`, `attempt`, `exception_type`). |

## Data flow

1. Caller invokes `CompleteJsonAsync` / `EmbedAsync` with a `CancellationToken` (often linked to handler timeout).
2. Gate rejects immediately if open (`CircuitBreakerOpenException`).
3. Retry pipeline runs the inner delegate; on transient failure Polly waits (backoff + jitter) and retries until success or exhaustion.
4. Success → `RecordSuccess`. Uncaught exception after retries → `RecordFailure` (one tick). User cancellation path → `RecordCallCancelled` where applicable.

## Security model

- No secrets in logs; retry logging uses exception type and attempt metadata.
- Same auth and network boundaries as the inner Azure client (private endpoints, managed identity or API key per deployment).

## Operational considerations

- **Metrics:** Use `archlucid_llm_call_retries_total` to detect rising transient pressure (e.g. 429 storms).
- **Latency trade-off:** Worst case adds several backoff intervals before surfacing failure; per-handler timeout remains the outer bound (default 900s).
- **Configuration:** See table below; invalid values are clamped at runtime via `Normalize()`.

### Configuration (`AgentExecution:Resilience`)

| Property | Default | Valid range (after `Normalize`) | Meaning |
|----------|---------|-----------------------------------|---------|
| `MaxConcurrentHandlers` | (existing) | — | Bulkhead for parallel handlers. |
| `PerHandlerTimeoutSeconds` | (existing) | — | Outer timeout for each handler. |
| `LlmCallMaxRetryAttempts` | `3` | 0–10 | Retry attempts **after** the first call; `0` disables retry (`ResiliencePipeline.Empty`). |
| `LlmCallBaseDelayMilliseconds` | `500` | 50–30_000 | Base delay for exponential backoff. |
| `LlmCallMaxDelaySeconds` | `10` | 1–120 | Cap for a single backoff wait. |

### Exception classification (retry vs terminal)

| Exception / condition | Retry? | Notes |
|-------------------------|--------|-------|
| `HttpRequestException` with status 429, 500, 502, 503, 504 | Yes | Rate limit and server errors. |
| `HttpRequestException` without status | Yes | Treated as network-level. |
| `HttpRequestException` 4xx other than 429 | No | Client errors. |
| `ClientResultException` with status 429 / 5xx listed above | Yes | Azure SDK HTTP wrapper. |
| `TaskCanceledException`, token **not** cancelled | Yes | Typical HTTP timeout. |
| `OperationCanceledException` / cancel with user token | No | Intentional shutdown. |
| `InvalidOperationException` | No | e.g. empty assistant message — retry wastes tokens. |
| `CircuitBreakerOpenException` | No | Circuit already open. |

### Circuit breaker interaction

- **One logical call** that exhausts all retries produces **one** `RecordFailure` (one step toward open).
- A call that eventually succeeds after retries produces **one** `RecordSuccess` (helps half-open recovery).

State machine (simplified):

```mermaid
stateDiagram-v2
  [*] --> Closed
  Closed --> Open : failures reach threshold\n(after retries per call)
  Open --> HalfOpen : break duration elapsed
  HalfOpen --> Closed : probe success
  HalfOpen --> Open : probe failure
```

## Simmy chaos testing

- **Local:** `dotnet test ArchLucid.AgentRuntime.Tests --filter "FullyQualifiedName~Simmy|FullyQualifiedName~Chaos"`
- **Persistence (SQL/blob):** `dotnet test ArchLucid.Persistence.Tests --filter "FullyQualifiedName~Simmy|FullyQualifiedName~Chaos"`
- **Scheduled CI:** `.github/workflows/simmy-chaos-scheduled.yml` (Wed 05:00 UTC) runs the same filters and uploads TRX artifacts.

To add a scenario: follow `LlmCallRetrySimmyTests`, `LlmCallChaosEndToEndTests`, `SqlOpenResilienceSimmyTests`, or `BlobStoreSimmyChaosTests` — compose **retry outside chaos** (or match production order documented in those tests) and assert inner call counts and breaker state.

## References

- `docs/RESILIENCE_CONFIGURATION.md` — broader resilience and circuit breaker options.
- `ArchLucid.Persistence/Connections/SqlOpenResilienceDefaults.cs` — SQL open retry pattern.
- `ArchLucid.Cli/CliRetryDelegatingHandler.cs` — CLI HTTP retry pattern.
