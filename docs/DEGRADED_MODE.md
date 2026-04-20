> **Scope:** Degraded mode — LLM and agent availability - full detail, tables, and links in the sections below.

# Degraded mode — LLM and agent availability

**Product:** ArchLucid  
**Audience:** Operators, SRE, and developers integrating the authority pipeline

## Objective

Document which capabilities remain available when Azure OpenAI (or other LLM backends) is unavailable, throttled, or circuit-broken, and how the system recovers.

## Assumptions

- Primary LLM traffic flows through `IAgentCompletionClient` with optional `FallbackAgentCompletionClient` (primary → secondary provider).
- Deterministic execution uses `DeterministicAgentSimulator` (tests and some local configurations).
- Resilience policies include HTTP retries, circuit breakers, and concurrency limits on agent execution.

## Constraints

- Explanations and “Ask” flows are inherently LLM-backed; they cannot be fully replicated without a model.
- Authority data (runs, manifests, findings snapshots) remains in SQL; degradation does not imply data loss for committed transactions.

## Architecture overview

```text
Client → API → Authority pipeline / Application services
                    │
                    ├─► Deterministic stages (ingest, graph, rules) ──► SQL
                    │
                    └─► Agent / LLM path ──► Retry → Fallback client → Circuit breaker → fail or partial result
```

## Feature availability matrix

| Feature | LLM required? | Behavior when LLM down | Recovery |
|--------|----------------|-------------------------|----------|
| Context ingestion & graph build | No | Continues (deterministic parsers/builders) | N/A |
| Rule-based / template findings | No | Continues | N/A |
| Manifest commit & golden manifest | No | Continues after pipeline stages complete | N/A |
| Governance (approve, promote, activate) | No | Continues | N/A |
| Audit log & export | No | Continues | N/A |
| Health & readiness | No | Continues | N/A |
| Operator UI shell & governance dashboard | No | Continues for non-LLM pages | N/A |
| Explanation service / aggregate explanation | Yes | Errors or empty; cache may serve prior aggregate if enabled; when LLM returns but **faithfulness** vs findings is very low, aggregate may swap to **deterministic** manifest narrative (`ArchLucid:Explanation:Aggregate`) | Restore LLM; circuit closes |
| Ask endpoint | Yes | Error response | Restore LLM |
| Agent-enhanced handlers | Yes | Blocked or skipped when breaker open; may fall back to deterministic paths where implemented | Breaker half-open → closed |
| Agent output quality gate | No (rules only) | When **`ArchLucid:AgentOutput:QualityGate:Enabled`** is **true** (default in production options), low structural/semantic scores yield **warned** / **rejected** outcomes on the evaluation path; **`appsettings.Development.json`** sets **`Enabled: false`** for faster local iteration | Toggle config + redeploy or reload where options-bound |

## Resilience chain (LLM calls)

1. **HTTP resilience** — `LlmCallResilienceDefaults` retries transient HTTP failures (e.g. 429, 5xx) where configured.
2. **Fallback provider** — `FallbackAgentCompletionClient` tries a secondary deployment/model when the primary fails.
3. **Circuit breaker** — `CircuitBreakerGate` (with `IOptionsMonitor` hot reload) opens after repeated failures, shedding load.
4. **Concurrency** — `AgentHandlerConcurrencyGate` limits parallel LLM work (bulkhead).

## Operator actions during an LLM outage

- Monitor **`/health`**, **`/health/ready`**, and detailed health JSON for circuit breaker contributors.
- Use OpenTelemetry metrics (e.g. breaker state, LLM-related counters/histograms) and logs with correlation IDs.
- Continue operating governance workflows, exports, and read-only dashboards that do not call explanation endpoints.
- Expect explanation and Ask features to error until the provider recovers or configuration is corrected.

## Recovery

- When the LLM endpoint stabilizes, the circuit breaker transitions **HalfOpen** → **Closed** after successful probes.
- Fallback decorator resumes preferring the primary when it succeeds again.
- No special “flush” is required for SQL authority data; rerun or refresh UI as needed.

## Data flow (degradation)

```text
Request → Middleware (auth, scope) → Controller
              → Application service
                    → If LLM: resilience + fallback + breaker
                    → If SQL-only: primary + optional read replica (see DATA_CONSISTENCY_MATRIX.md)
              → Response / ProblemDetails + correlation id
```

## Related documents

- [OBSERVABILITY.md](OBSERVABILITY.md) — metrics and tracing  
- [DATA_CONSISTENCY_MATRIX.md](DATA_CONSISTENCY_MATRIX.md) — consistency and read replicas  
- [API_CONTRACTS.md](API_CONTRACTS.md) — HTTP error shapes  
