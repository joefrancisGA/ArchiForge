> **Scope:** ADR 0014 — Trial enforcement boundary (server-side, UoW run counter, idempotent seats) - full detail, tables, and links in the sections below.

# ADR 0014 — Trial enforcement boundary (server-side, UoW run counter, idempotent seats)

**Status:** Accepted  
**Date:** 2026-04-17

## Context

ArchLucid offers self-service trials with **run** and **seat** limits and a **time-boxed** expiry (`Tenants` trial columns, migration **072**). Enforcement must be:

- **Server-authoritative** (UI is display-only).
- **Consistent across entry points** (HTTP, future CLI/worker paths sharing persistence).
- **Safe under concurrency** (no double-charged runs, no orphaned increments).

## Decision

1. **Write gate:** Introduce **`TrialLimitGate`** in the Application layer (pure dependency on `ITenantRepository` + `TimeProvider`; no HTTP types). It throws **`TrialLimitExceededException`** with **`TrialLimitReason`** (`Expired`, `RunsExceeded`, `SeatsExceeded`).
2. **HTTP mapping:** Compose a **`TrialActive`** authorization requirement onto **ExecuteAuthority** and **AdminAuthority**. Failure yields **402** with **`application/problem+json`** and type **`https://archlucid.dev/problem/trial-expired`**, including **`traceCompleteness`**, **`correlationId`**, **`trialReason`**, **`daysRemaining`**.
3. **Run counter:** Increment **`TrialRunsUsed`** only inside the **same database transaction** as authority run **`INSERT`** when `TrialStatus = Active` (`SqlRunRepository` + shared `TryIncrementActiveTrialRunAsync`; in-memory path delegates to the same repository abstraction).
4. **Seat counter:** Persist idempotent occupant rows (**074** / `TenantTrialSeatOccupants`) and increment **`TrialSeatsUsed`** only on first claim per `(TenantId, UserId)` via **`TrialSeatAccountant`** / `TryClaimTrialSeatAsync`.
5. **Reads:** **ReadAuthority** policies do **not** include **`TrialActive`** so expired trials remain **read-only** operable.

## Alternatives considered

- **Controller filters only:** Rejected — easy to miss non-HTTP entry points and inconsistent ordering.
- **Increment run count outside UoW:** Rejected — risks orphan increments or double counts under failure/retry.
- **UI-only enforcement:** Rejected — trivially bypassed.

## Consequences

- Positive: One **obvious** authority boundary; atomic persistence for run metering; auditable **402** responses.
- Negative: Slightly more authorization surface area; SQL integration tests required for counter semantics.
- Follow-up: Workers/Service Bus consumers must invoke the same gate or rely on repository throws when they perform writes under **Active** trials.

## References

- **`docs/security/TRIAL_LIMITS.md`**
- **`docs/API_CONTRACTS.md`**
- Migration **072** (trial columns), **074** (seat occupants).
