> **Scope:** Chaos-style resilience tests - full detail, tables, and links in the sections below.

# Chaos-style resilience tests

**Objective**: Catch regressions in retry and circuit-breaker wiring before production incidents.

## CI and scheduled runs

| Mechanism | When | Purpose |
|-----------|------|---------|
| **Main CI — job `Resilience: Simmy chaos tests` (`chaos-tests`)** | Every push / PR to `main` or `master`, after **`.NET: full regression (SQL)`** succeeds | Runs `ArchLucid.AgentRuntime.Tests` and `ArchLucid.Persistence.Tests` with the same Simmy/Chaos FQN filter as `simmy-chaos-scheduled.yml`; uploads TRX as artifact **`chaos-test-results`**. |
| **`.github/workflows/simmy-chaos-scheduled.yml`** | Weekly cron + manual dispatch | Second line of defense; same test filter today; can be extended later for longer or parameterized suites. |

**Triage when `chaos-tests` fails**

1. Open the failed workflow run → job **Resilience: Simmy chaos tests** → expand the failing step (**Run AgentRuntime chaos tests** or **Run Persistence chaos tests**).
2. Download artifact **`chaos-test-results`** and open the `.trx` in Visual Studio Test Explorer, `trx-viewer`, or CI log output for the failing test name.
3. Reproduce locally:
   - `dotnet test ArchLucid.AgentRuntime.Tests --filter "FullyQualifiedName~Simmy|FullyQualifiedName~Chaos"`
   - `dotnet test ArchLucid.Persistence.Tests --filter "FullyQualifiedName~Simmy|FullyQualifiedName~Chaos"`

## CI enforcement

Simmy chaos tests run in the **`chaos-tests`** job are **CI-blocking**: a failing job fails the workflow and blocks merging the PR (the job does not use `continue-on-error`). If a chaos test flakes, **re-run the failed workflow job first**; if the failure persists, fix the underlying resilience gap (or stabilize the test) rather than weakening the gate.

**Approach in this repo**

1. **HTTP (CLI)**: `ArchLucid.Cli.Tests.CliRetryDelegatingHandlerTests` sends a **500** on the first attempt and **200** on the next, asserting Polly retries via `CliRetryDelegatingHandler`.
2. **SQL + blob (Polly Simmy)**: `ArchLucid.Persistence.Tests` — `SqlOpenResilienceSimmyTests` composes `SqlOpenResilienceDefaults` with Simmy `ChaosFault` (transient `SqlException`); `BlobStoreSimmyChaosTests` retries `IOException` on a synthetic `IArtifactBlobStore` write path. Test projects reference **`Polly.Extensions`** so Simmy builder extensions resolve (Simmy types live in `Polly.Core`; extensions are surfaced via that package).
3. **LLM latency (Simmy)**: `ArchLucid.AgentRuntime.Tests.SimmyChaosPipelineTests` — `ChaosLatency` under a short Polly **timeout** (fails fast), plus SQL-style retry + fault composition (mirrors completion client protection patterns).
4. **Agent execution bulkhead + timeout**: `ArchLucid.AgentRuntime.Tests.AgentExecutionResilienceTests` — process-wide `IAgentHandlerConcurrencyGate` (semaphore) and per-handler `ResiliencePipeline` timeout on `RealAgentExecutor` (configured under `AgentExecution:Resilience`).
5. **Combined transient shapes (SQL + HTTP)**: `ArchLucid.AgentRuntime.Tests.CombinedFailureChaosTests` — one outer retry pipeline tolerates **alternating** transient `SqlException` and HTTP **429** faults (mixed-dependency incident shape).

**Operational chaos** (staging)

- Run controlled drills documented in `docs/runbooks/DATABASE_FAILOVER.md` and measure RTO/RPO; pair with Prometheus rules in `infra/prometheus/archlucid-alerts.yml` and `archlucid-slo-rules.yml`, and optional **Azure Monitor Prometheus rule groups** from `infra/terraform-monitoring/prometheus_slo_rules.tf` when `enable_prometheus_slo_rule_group` is set.
