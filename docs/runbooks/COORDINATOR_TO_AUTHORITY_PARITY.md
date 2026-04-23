> **Scope:** Coordinator vs Authority pipeline parity evidence (ADR 0021).

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


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
| 2026-04-21 06:41 UTC | 2026-04-22 06:41 UTC | *(sample)* | - | - | - / - / - | - | auto `scripts/ci/coordinator_parity_probe.py` |
| 2026-04-22 06:42 UTC | 2026-04-23 06:42 UTC | *(sample)* | - | - | - / - / - | - | auto `scripts/ci/coordinator_parity_probe.py` |
<!-- /coordinator-parity-probe:table -->

## Phase 3 gate status (2026-04-21, updated 2026-04-22)

**ADR 0021 Phase 3 is unblocked for the pre-release window.** Gates **(i)** (30-day post-PR-A soak) and **(iv)** (14 contiguous green daily rows in the parity table above) are both **waived** per [ADR 0029](../adr/0029-coordinator-strangler-acceleration-2026-05-15.md) (owner Q&A 2026-04-21 + follow-up). Gate (iv) was waived because pre-release there is no customer traffic on either pipeline, the daily probe needs a SQL secret that only meaningfully exists post-V1, and holding the gate would create a chicken-and-egg block on shipping V1. Gate **(ii)** (`dotnet test --filter "Suite=Core|Suite=Integration"` green on `main`) **remains in force**. Gate **(iii)** is satisfied for PR A2 by the **cohort parity integration tests** documented in [`evidence/phase3/pr-a2-cohort-parity.md`](../evidence/phase3/pr-a2-cohort-parity.md); the live-API E2E workflow remains an additional regression signal on `main` but is not the sole owner of gate (iii) for this sub-PR.

**PR A2 (2026-04-22) — sub-PR evidence for gates (ii) and (iii) framing:**

| Gate | PR A2 mechanical evidence |
|------|---------------------------|
| **(ii)** | `dotnet test --filter "Suite=Core|Suite=Integration"` green on `main` (full Core + Integration CI slice). |
| **(iii)** | `ArchitectureRunCommitPathParityIntegrationTests` in `ArchLucid.Api.Tests`: two factories (`Coordinator:LegacyRunCommitPath` true vs false) run the same simulator create → execute → commit idempotency key, assert identical **traceability-bundle.zip** entry names, and assert **stable** `PilotRunDeltasResponse` fields match (findings-by-severity histogram, audit row count + truncation flag, LLM call count, demo flag, top severity string). Clocks, seconds-to-commit, `topFindingId`, and evidence-chain pointers are intentionally out of scope — see [`evidence/phase3/pr-a2-cohort-parity.md`](../evidence/phase3/pr-a2-cohort-parity.md). Live workflow E2E remains additional signal but PR A2 satisfies gate (iii) for the pre-release waiver window via this cohort. |

**Cut-over date: 2026-05-15** (latest-by; PR A may merge earlier once gates (ii) and (iii) clear). ADR 0029 Supersedes the earlier Draft [ADR 0028 — completion scaffold](../adr/0028-coordinator-strangler-completion.md).

**Both waivers expire automatically** if ArchLucid ships V1 to a paying customer before PR A merges — at that point the assistant amends ADR 0029 to restore both gates and recomputes the cut-over date. After V1 ships, any *future* coordinator-style refactor (none currently planned) must satisfy gates (i)–(iv) in full; the daily probe and runbook stay live for that purpose.

**Daily probe status.** [`coordinator-parity-daily.yml`](../../.github/workflows/coordinator-parity-daily.yml) is wired and will start populating the table above the moment the `ARCHLUCID_COORDINATOR_PARITY_ODBC` repo secret is set. Until then the table stays at `*(TBD)*` — that is now an **expected** pre-release state, not a merge blocker.

**Closing report:** *Not available — pre-release. Reopen this subsection if a future change ever restores gate (iv) (e.g., post-V1 coordinator-style refactor) and 14 contiguous zero-write days are recorded.*

## Related

- [ADR 0021 — Coordinator pipeline strangler plan](../adr/0021-coordinator-pipeline-strangler-plan.md)
- [dual-pipeline-navigator-superseded.md](../archive/dual-pipeline-navigator-superseded.md)
