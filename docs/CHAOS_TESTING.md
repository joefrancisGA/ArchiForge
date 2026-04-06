# Chaos-style resilience tests

**Objective**: Catch regressions in retry and circuit-breaker wiring before production incidents.

**Approach in this repo**

1. **HTTP (CLI)**: `ArchiForge.Cli.Tests.CliRetryDelegatingHandlerTests` sends a **500** on the first attempt and **200** on the next, asserting Polly retries via `CliRetryDelegatingHandler`.
2. **SQL / LLM**: Production code uses `ResilientSqlConnectionFactory` and `CircuitBreakerGate` for OpenAI calls; extend with similar **deterministic fault** handlers where you need coverage (fail-first `DelegatingHandler` or SQL connection wrapper in tests).

**Optional tooling**

- [Polly Simmy](https://www.thepollyproject.org/) (or ecosystem chaos packages) can inject latency and faults across call graphs; we keep the default suite dependency-light and use explicit handlers first.

**Operational chaos** (staging)

- Run controlled drills documented in `docs/runbooks/DATABASE_FAILOVER.md` and measure RTO/RPO; pair with Prometheus rules in `infra/prometheus/archiforge-alerts.yml` and `archiforge-slo-rules.yml`.
