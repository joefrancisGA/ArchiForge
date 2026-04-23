> **Scope:** Quarterly staging chaos game day calendar, closing-report links, and Simmy workflow alignment; production chaos remains owner-gated ([PENDING_QUESTIONS.md](../../PENDING_QUESTIONS.md) item **34**).

> **Spine doc:** [Five-document onboarding spine](../../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Game day log — quarterly staging chaos (calendar)

**Primary automation:** [`.github/workflows/simmy-chaos-scheduled.yml`](../../../.github/workflows/simmy-chaos-scheduled.yml) (cron **14:00 UTC** on each calendar row below; `workflow_dispatch` is **staging-only** — production is blocked at step one per item **34**).

**Runbook:** [`docs/runbooks/GAME_DAY_CHAOS_QUARTERLY.md`](../../runbooks/GAME_DAY_CHAOS_QUARTERLY.md) · **RTO/RPO (staging tier):** [`docs/RTO_RPO_TARGETS.md`](../../library/RTO_RPO_TARGETS.md) (≤ **4h** RTO, ≤ **1h** RPO for staging/pre-production) · **Failover:** [`docs/runbooks/DATABASE_FAILOVER.md`](../../runbooks/DATABASE_FAILOVER.md) · **Degraded behaviour:** [`docs/DEGRADED_MODE.md`](../../library/DEGRADED_MODE.md) · **Chaos tests in CI:** [`docs/CHAOS_TESTING.md`](../../library/CHAOS_TESTING.md).

When `dry_run=false`, capture the GitHub Actions run URL and TRX summaries in the **closing report** for that row. If branch protection blocks bot commits, paste the run URL into the closing report manually after the job completes.

## Calendar (next three runs)

| Date (UTC) | Environment | Scenario | Owner | Expected blast radius | RTO/RPO target (staging) | Observed RTO/RPO | Closing report |
|------------|-------------|----------|-------|------------------------|--------------------------|------------------|----------------|
| **2026-04-29** | **Staging only** | **SQL connection pool exhaustion** under live trial-signup load (synthetic k6 or controlled parallel `POST /v1/register` against staging) | Platform / on-call (assign before run) | Trial-signup path + SQL pool; **no** production traffic; Simmy/LLM chaos suites unchanged | **RTO ≤ 4h**, **RPO ≤ 1h** ([RTO_RPO_TARGETS.md](../../library/RTO_RPO_TARGETS.md) § tier table) | _Fill after run_ | [2026-04-29 — staging — SQL pool exhaustion](2026-04-29-staging-sql-pool-exhaustion.md) |
| **2026-07-29** | **Staging only** | _Scenario TBD (Q3)_ — reserve slot | Platform | Staging only | Same staging targets | _TBD_ | [2026-07-29 — staging — placeholder](2026-07-29-staging-placeholder.md) |
| **2026-10-28** | **Staging only** | _Scenario TBD (Q4)_ — reserve slot | Platform | Staging only | Same staging targets | _TBD_ | [2026-10-28 — staging — placeholder](2026-10-28-staging-placeholder.md) |

### First run scenario — expected behaviour (2026-04-29)

**Hypothesis to validate on staging (not asserted as production contract):**

1. **`GET /health/ready`** flips to **degraded / non-200** while SQL is saturated (readiness must surface dependency failure — see [DEGRADED_MODE.md](../../library/DEGRADED_MODE.md) and health wiring in runbooks).
2. **Existing in-flight commits** complete where a **unit-of-work** already holds a connection; new work that needs a pool slot **backs off or fails fast** per resilience policy (document actual status codes and bodies observed).
3. **`POST /v1/register`** under pool pressure: document whether the API returns **503** with a **`Retry-After`** header (or which ProblemDetails + status the stack actually returns) and capture **`X-Correlation-ID`** for each failed attempt. _(Trial seat middleware skips `/v1/register`; exhaustion surfaces via SQL dependency and downstream handlers — reconcile names with live `dbo.AuditEvents` during the drill.)_
4. **Audit / integration:** drill narrative uses **SeatReservationFailed** as the *operator-facing* label for “seat path could not complete because SQL was unhealthy”; **map it to the real** `dbo.AuditEvents` / integration-event type string during close-out (today’s catalog includes `com.archlucid.seat.reservation.released` for lifecycle releases — failure-side naming may differ).
5. **Operator evidence:** `dotnet run --project ArchLucid.Cli -- support-bundle --zip` against the staging API base URL should capture **correlation IDs** and health snapshots for the incident window ([CLI_USAGE.md](../../library/CLI_USAGE.md)).

**Production:** _Out of scope for this calendar._ Item **34** in [`PENDING_QUESTIONS.md`](../../PENDING_QUESTIONS.md) remains the gate for any production fault injection.

## Artifact policy

- **Preferred:** GitHub Actions artifact **`chaos-test-results`** on the workflow run.
- **Human:** paste the run URL into the closing-report **Workflow** section for the matching date.
