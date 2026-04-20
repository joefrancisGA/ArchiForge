> **Scope:** Coordinator vs Authority pipeline parity evidence (ADR 0021).

# Coordinator → Authority parity runbook

**Audience:** Platform / SRE + architecture reviewers.

**Objective:** Capture **measurable parity** between the Coordinator and Authority pipelines while ADR 0021 phases execute (latency, audit volume, replay outcomes).

## Cadence

| Environment | Minimum frequency | Owner |
|-------------|-------------------|-------|
| Staging | Weekly during strangler | Platform |
| Production | Weekly while both pipelines accept writes | Platform |

## Metrics to record

| Metric | Source | Notes |
|--------|--------|-------|
| p95 / p99 API latency (`POST /v1/architecture/request`, `POST …/execute`, `POST …/commit`) | Application Insights or Grafana | Split by pipeline discriminator in logs where available. |
| Audit row ingest rate | `dbo.AuditEvents` count / hour | Expect temporary uplift during Phase 2 dual-write. |
| Replay parity | `POST /v1/architecture/run/{id}/replay` verify mode | Record 422 drift payloads when mismatched. |

## Template (fill per window)

| Window start (UTC) | Window end (UTC) | Tenant sample | Coordinator p95 ms | Authority p95 ms | Audit rows/hr | Replay parity OK? | Notes |
|--------------------|------------------|-----------------|----------------------|------------------|-----------------|---------------------|-------|
| *(TBD)* | *(TBD)* | *(TBD)* | | | | | |

## Related

- [ADR 0021 — Coordinator pipeline strangler plan](../adr/0021-coordinator-pipeline-strangler-plan.md)
- [DUAL_PIPELINE_NAVIGATOR.md](../DUAL_PIPELINE_NAVIGATOR.md)
