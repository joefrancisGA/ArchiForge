> **Scope:** Trial limits (runs, seats, expiry) - full detail, tables, and links in the sections below.

# Trial limits (runs, seats, expiry)

## Objective

Enforce SaaS **trial quotas** and **read-only after expiry** for tenants whose `dbo.Tenants` trial columns (see migration **072**) indicate an **Active** self-service trial. Clients must not rely on the UI: limits are **authoritative on the server**.

## Assumptions

- Trial state is stored in SQL (`TrialStatus`, `TrialExpiresUtc`, `TrialRunsLimit` / `TrialRunsUsed`, `TrialSeatsLimit` / `TrialSeatsUsed`).
- **Converted** or **non-trial** tenants are not blocked by this gate.
- Billing conversion (`POST /v1/tenant/convert`) must remain callable while on trial (see **`SkipTrialWriteLimit`** on that route).

## Constraints

- **ReadAuthority** endpoints stay available when the trial is expired or exhausted (**read-only** posture).
- **ExecuteAuthority** and **AdminAuthority** writes require an **active** trial within limits, or a converted/paid posture.
- RLS and tenant scope rules are unchanged; this layer is **orthogonal** policy on top of existing authorization.

## Architecture overview

| Node | Role |
|------|------|
| **`TrialLimitGate`** (`ArchLucid.Application.Tenancy`) | Pure gate: loads tenant, throws **`TrialLimitExceededException`** with **`TrialLimitReason`**. |
| **Authorization** (`TrialActive` requirement + handlers in **`ArchLucid.Api` / `ArchLucid.Host.Core`**) | Runs before controller actions for policies that include **`TrialActive`**. |
| **`TrialSeatAccountant`** | Reserves a trial seat idempotently per `(TenantId, UserId)` after sign-in / scope resolution path. |
| **`IRunRepository.SaveAsync`** (SQL + in-memory) | **Atomically** increments `TrialRunsUsed` for **Active** trials in the **same transaction** as run insert (Dapper UoW). |

**Edges:** HTTP pipeline (authorization), persistence (runs, seats), audit (402 responses).

## Component breakdown

- **Interfaces:** `ITenantRepository` (`TryIncrementActiveTrialRunAsync`, `TryClaimTrialSeatAsync`, …), `IRunRepository`.
- **Services:** `TrialLimitGate`, `TrialSeatAccountant`, tenant repositories.
- **Data models:** `TenantRecord` trial fields; **`TenantTrialSeatOccupants`** (migration **074**) for idempotent seat claims.
- **Orchestration:** ASP.NET authorization + middleware (`TrialSeatReservationMiddleware`) ordered after authentication and before authorization.

## Data flow

1. **Write request** hits an endpoint protected by **ExecuteAuthority** / **AdminAuthority**, which includes **`TrialActive`**.
2. **`TrialLimitGate.GuardWriteAsync`** loads the tenant and may throw **`TrialLimitExceededException`**.
3. **Run creation:** `SqlRunRepository.SaveAsync` opens a transaction → `TryIncrementActiveTrialRunAsync` → `INSERT` run → commit (no orphan increments).
4. **Seat claim:** after authentication, middleware/accountant claims a seat once per user per tenant; duplicate claims are ignored at the database level.

## Security model

- **Soft boundary:** expiry is not a hard auth failure; it is a **commercial / tenancy** boundary. Users remain authenticated; **writes** are refused.
- **402 Payment Required** + **`application/problem+json`** with stable **`type`**: `https://archlucid.dev/problem/trial-expired` (see **`docs/API_CONTRACTS.md`** and **`docs/API_ERROR_CONTRACT.md`**).
- **Break-glass:** do not disable trial checks via configuration in production; conversion and support processes are the supported exits.

## Operational considerations

- **Metrics / audits:** **`TrialLimitExceeded`** audit when the API returns **402** (see implementation filters).
- **SQL integration tests:** `DapperRunRepositoryTrialIncrementTests` validate atomic run counter behavior when SQL is configured locally.
- **ADRs:** **`docs/adr/0014-trial-enforcement-boundary.md`**.

## Related documentation

- **`docs/API_CONTRACTS.md`** — normative HTTP examples.
- **`docs/API_ERROR_CONTRACT.md`** — Problem Details extensions and correlation.
