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

### Automated probe (nightly — `scripts/ci/coordinator_parity_probe.py`)

Mechanical counts from `dbo.AuditEvents` (last 24h window): **legacy coordinator** (`CoordinatorRun*`) / **canonical** (`Run.*`) / **authority** (`RunStarted`, `RunCompleted`). Latency columns remain manual until wired. The workflow `.github/workflows/coordinator-parity-daily.yml` upserts this block (markers must stay stable).

<!-- coordinator-parity-probe:table -->
| Window start (UTC) | Window end (UTC) | Tenant sample | Coordinator p95 ms | Authority p95 ms | Audit rows/hr | Replay parity OK? | Notes |
|--------------------|------------------|-----------------|----------------------|------------------|-----------------|---------------------|-------|
<!-- /coordinator-parity-probe:table -->

## Phase 3 gate status (2026-04-21)

**ADR 0021 Phase 3 is merge-blocked:** the template above still contains only `*(TBD)*` placeholders — there is **no** 14-day contiguous window with **Coordinator-pipeline writes = 0**. Until Platform fills daily rows here, gate **(iv)** fails and coordinator code **must not** be deleted. See [ADR 0022 — blocked record](../adr/0022-coordinator-phase3-deferred.md) and [`artifacts/phase3/gate-verification.md`](../../artifacts/phase3/gate-verification.md).

**Closing report:** *Not available — reopen this subsection after 14 contiguous zero-write days are recorded and ADR 0022 is superseded by a “Phase 3 shipped” ADR.*

## Related

- [ADR 0021 — Coordinator pipeline strangler plan](../adr/0021-coordinator-pipeline-strangler-plan.md)
- [DUAL_PIPELINE_NAVIGATOR.md](../DUAL_PIPELINE_NAVIGATOR.md)
