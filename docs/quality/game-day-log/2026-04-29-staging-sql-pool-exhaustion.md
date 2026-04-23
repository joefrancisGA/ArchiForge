> **Scope:** Closing report scaffold for **2026-04-29** staging chaos game day — SQL connection pool exhaustion under trial-signup load. Fill after the drill; links to [`README.md`](README.md) calendar row.

> **Spine doc:** [Five-document onboarding spine](../../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Game day close-out — 2026-04-29 — Staging — SQL pool exhaustion (trial-signup load)

| Field | Value |
|-------|-------|
| **Date (UTC)** | 2026-04-29 |
| **Environment** | Staging only |
| **Scenario** | SQL connection pool exhaustion under live trial-signup load |
| **RTO/RPO target (staging)** | RTO ≤ **4h**, RPO ≤ **1h** ([RTO_RPO_TARGETS.md](../../library/RTO_RPO_TARGETS.md)) |
| **Workflow run** | _Paste GitHub Actions run URL after `simmy-chaos-scheduled.yml` (or manual `dotnet test` evidence if workflow skipped)._ |

## Expected behaviour (validate / correct during close-out)

Per [`README.md`](README.md) § first-run scenario:

1. **`GET /health/ready`** — _Expected:_ non-success / degraded while SQL pool is saturated. _Observed:_ _TBD_
2. **In-flight commits (UoW)** — _Expected:_ transactions that already hold a connection complete; new work needing a pool slot fails or waits per policy. _Observed:_ _TBD_
3. **`POST /v1/register`** — _Expected under drill hypothesis:_ **503** with **`Retry-After`** where the API surfaces back-pressure; capture **`X-Correlation-ID`**. _(Trial seat middleware skips `/v1/register`; exhaustion is via SQL in the registration path — record actual status, body, and headers.)_ _Observed:_ _TBD_
4. **Audit / integration** — _Hypothesis:_ failure surfaces as a seat- or SQL-related event (e.g. operator wording **SeatReservationFailed**); **verify** emitted type strings in `dbo.AuditEvents` / integration feed — do not copy placeholder names if the DB shows something else. _Observed:_ _TBD_
5. **`support-bundle`** — _Expected:_ archive includes correlation IDs for the window ([CLI_USAGE.md](../../library/CLI_USAGE.md)). _Observed:_ _TBD_

## Actual symptoms observed

| Symptom | Evidence (metric / log / screenshot) |
|---------|--------------------------------------|
| _Placeholder — fill after run_ | |
| _Placeholder_ | |

## Recovery

| Metric | Target | Actual |
|--------|--------|--------|
| Time to healthy **`/health/ready`** | ≤ staging RTO (**4h**) | _TBD_ |
| Data loss window (if any) | ≤ staging RPO (**1h**) | _TBD_ |

## Runbooks

| Runbook section | Worked? | Notes |
|-----------------|---------|-------|
| [`GAME_DAY_CHAOS_QUARTERLY.md`](../../runbooks/GAME_DAY_CHAOS_QUARTERLY.md) — pre-flight | _TBD_ | |
| [`GAME_DAY_CHAOS_QUARTERLY.md`](../../runbooks/GAME_DAY_CHAOS_QUARTERLY.md) — abort criteria | _TBD_ | |
| [`DATABASE_FAILOVER.md`](../../runbooks/DATABASE_FAILOVER.md) — relevant subsection | _TBD_ | |
| [`DEGRADED_MODE.md`](../../library/DEGRADED_MODE.md) | _TBD_ | |

## What we changed in the runbook because of this

_Placeholder paragraph — after the drill, record one concrete doc change (or explicit “no change”) with PR / commit reference._
