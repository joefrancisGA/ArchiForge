# Resilience configuration (ArchLucid)

Operators can tune retry and circuit-breaker behavior without recompiling. This document lists configuration paths, defaults, the Azure OpenAI circuit breaker state machine, emitted OpenTelemetry metrics, and example Prometheus queries.

## Configurable knobs

### Azure OpenAI circuit breakers (completion and embedding)

| Setting | Config path | Default | Valid range |
|--------|-------------|---------|-------------|
| Completion failure threshold | `AzureOpenAI:CircuitBreaker:Completion:FailureThreshold` | `5` | ≥ 1 after binding (invalid values fall back) |
| Completion open duration (seconds) | `AzureOpenAI:CircuitBreaker:Completion:DurationOfBreakSeconds` | `30` | ≥ 1 after binding |
| Embedding failure threshold | `AzureOpenAI:CircuitBreaker:Embedding:FailureThreshold` | `5` | ≥ 1 |
| Embedding open duration (seconds) | `AzureOpenAI:CircuitBreaker:Embedding:DurationOfBreakSeconds` | `30` | ≥ 1 |

**Fallback hierarchy**

1. Per-gate section: `AzureOpenAI:CircuitBreaker:Completion` or `AzureOpenAI:CircuitBreaker:Embedding` when the corresponding JSON key is present for each property.
2. Shared section: `AzureOpenAI:CircuitBreaker:FailureThreshold` and `AzureOpenAI:CircuitBreaker:DurationOfBreakSeconds` when a per-gate key is absent.
3. Code defaults: `CircuitBreakerOptions.DefaultFailureThreshold` / `DefaultDurationOfBreakSeconds`, then `ApplyDefaults()` clamps invalid values.

Deployed environments that only set the legacy flat `AzureOpenAI:CircuitBreaker` block continue to work for **both** gates.

**Why `IOptions<T>` / `IOptionsFactory<T>.Create(name)` and not `IOptionsMonitor<T>` for the gate**

`CircuitBreakerGate` is a keyed singleton with mutable internal state (`_consecutiveFailures`, `_state`, `_probeInFlight`). Reloading thresholds while that state is in flight would make behavior ambiguous (for example, changing `FailureThreshold` mid-count). Options are therefore **resolved once** when the gate is constructed. Named `Configure` / `PostConfigure` still give consistent binding and testability without hot-reloading the gate.

### SQL connection open retries

| Setting | Config path | Default | Notes |
|--------|-------------|---------|--------|
| Max retry attempts | `Persistence:SqlOpenResilience:MaxRetryAttempts` | `3` | `0` disables retries (first open only). Clamped to 0–32. |
| Base delay (ms) | `Persistence:SqlOpenResilience:BaseDelayMilliseconds` | `200` | Exponential backoff with jitter. Clamped to 1–120000. |

Used by `ResilientSqlConnectionFactory` via `SqlOpenResilienceDefaults.BuildSqlOpenRetryPipeline`.

### Agent execution handler resilience

Already bound via `IOptions<AgentExecutionResilienceOptions>` from `AgentExecution:Resilience` (`MaxConcurrentHandlers`, `PerHandlerTimeoutSeconds`). See `AgentExecutionResilienceOptions` in code for section name.

### CLI HTTP retries

| Setting | Source | Default | Notes |
|--------|--------|---------|--------|
| Max retry attempts | `archlucid.json` → `httpResilience.maxRetryAttempts` | `3` | Clamped to 0–10. `0` disables retries. |
| Initial delay (seconds) | `archlucid.json` → `httpResilience.initialDelaySeconds` | `1` | Clamped 0–300. Exponential backoff + jitter for 5xx, 429, timeouts. |

If `archlucid.json` is omitted, the CLI still attempts to load project config from the current directory for HTTP resilience when constructing `ArchLucidApiClient(string baseUrl)`.

## Circuit breaker state machine

Independent gates exist for **completion** and **embedding** (keyed `OpenAiCompletion` / `OpenAiEmbedding`).

```text
Closed --(N consecutive failures)--> Open --(after DurationOfBreakSeconds)--> HalfOpen (single probe)
   ^                                        |
   |                                        |
   +--------(probe success)-----------------+
   |
   +--------(probe failure or cancel)------> Open
```

- **Closed**: normal traffic; successes do not emit state-transition metrics (avoids noise).
- **Open**: calls are rejected until the break duration elapses; then one caller may enter **HalfOpen** as the probe.
- **HalfOpen**: only one probe at a time; concurrent callers are rejected until the probe completes, fails, or is cancelled.

## OpenTelemetry metrics

All use meter name **`ArchLucid`** (see `ArchLucidInstrumentation.MeterName`). Circuit breaker counters use the **`archlucid_`** metric name prefix.

| Metric | Type | Labels | When incremented |
|--------|------|--------|------------------|
| `archlucid_circuit_breaker_state_transitions_total` | Counter | `gate`, `from_state`, `to_state` | Only on real transitions (e.g. `Closed`→`Open`, `Open`→`HalfOpen`, `HalfOpen`→`Closed`, `HalfOpen`→`Open`). |
| `archlucid_circuit_breaker_rejections_total` | Counter | `gate` | Each `ThrowIfBroken` that throws `CircuitBreakerOpenException`. |
| `archlucid_circuit_breaker_probe_outcomes_total` | Counter | `gate`, `outcome` (`success` / `failure` / `cancelled`) | Half-open probe completion paths only. |

**Cardinality**: `gate` is bounded (currently `OpenAiCompletion` and `OpenAiEmbedding`). Do not add tenant or request identifiers to these series.

## Example Prometheus queries

```promql
# Rejection rate per gate (5m)
sum by (gate) (rate(archlucid_circuit_breaker_rejections_total[5m]))

# Transition rate into Open from Closed (completion gate)
rate(archlucid_circuit_breaker_state_transitions_total{
  gate="OpenAiCompletion",from_state="Closed",to_state="Open"
}[5m])

# Half-open probe failures (embedding)
rate(archlucid_circuit_breaker_probe_outcomes_total{
  gate="OpenAiEmbedding",outcome="failure"
}[5m])
```

## Related code

- `ArchLucid.Core.Resilience.CircuitBreakerGate`, `CircuitBreakerOptions`
- `ArchLucid.Core.Diagnostics.ArchLucidInstrumentation`
- `ArchLucid.Host.Composition.Startup.ServiceCollectionExtensions` (named options registration)
- `ArchLucid.Persistence.Connections.SqlOpenResilienceOptions`, `SqlOpenResilienceDefaults`
- `ArchLucid.Cli.CliResilienceOptions`, `CliRetryDelegatingHandler`
