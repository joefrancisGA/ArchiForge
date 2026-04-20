> **Scope:** Runbook: data_archival readiness check - full detail, tables, and links in the sections below.

# Runbook: `data_archival` readiness check

**Last reviewed:** 2026-04-16

## What it is

The API registers an ASP.NET Core health check named **`data_archival`** (readiness tag **`ready`**). It reflects the last outcome of the **data archival hosted service** (`DataArchivalHostedService`) when archival is enabled in configuration.

**Nodes (conceptual):**

- **Configuration** (`DataArchival:Enabled`) → **Health evaluator** (`DataArchivalHostHealthCheck`) ← **Iteration state** (`DataArchivalHostHealthState`) ← **Hosted loop** (`DataArchivalHostedService` → `IDataArchivalCoordinator`).

## Status meanings

| Configuration | Hosted state | Health result |
|---------------|--------------|---------------|
| `DataArchival:Enabled` = **false** | (any) | **Healthy** — archival is intentionally off; no signal about data age. |
| **Enabled** | No iteration has completed yet | **Healthy** — startup or first interval not elapsed; not a failure. |
| **Enabled** | Last iteration **succeeded** | **Healthy** — description includes last success timestamp. |
| **Enabled** | Last iteration **failed** | **Degraded** — description includes failure time and a short error summary. |

**Why Degraded (not Unhealthy):** the API process and database can still serve traffic; archival is a background hygiene loop. Operators should investigate, but traffic need not be cut immediately unless policy says otherwise.

## Triage

1. **Confirm config** — `DataArchival:Enabled`, retention days, and `IntervalHours` in the deployed environment (App Service / container env / Key Vault–backed settings).
2. **Read the health payload** — `GET` the readiness endpoint you use in production (for example `/health/ready` if mapped that way) and inspect the `data_archival` entry description for the exception type and message fragment.
3. **Check application logs** — correlate timestamps with `DataArchivalHostedService` / `DataArchivalCoordinator` errors (SQL timeouts, permission denied, invalid retention, etc.).
4. **Check SQL** — archival touches soft-archive columns on authority-related tables; verify connectivity, RLS/session context if applicable, and that migrations defining `ArchivedUtc` (or equivalent) are applied.

## Recovery

1. **Fix root cause** — typical fixes: restore SQL connectivity, correct connection string or firewall, grant required DML rights, resolve deadlock/timeout (scale or tune), fix bad configuration values.
2. **Redeploy or restart** — not always required; the next successful iteration clears **Degraded** automatically when `MarkLastIterationSucceeded` runs.
3. **Temporary mitigation** — if archival must be stopped while investigating, set **`DataArchival:Enabled`** to **`false`** (document the risk: older runs/digests/threads remain visible per product rules until archival resumes).

## Related documentation

- Soft archival feature overview: backlog item **243** in `docs/NEXT_REFACTORINGS.md` (Data & persistence).
- Migrations: `028_ArchivalSoftFlags.sql` and `ArchLucid.sql` (single DDL discipline per `docs/SQL_DDL_DISCIPLINE.md`).
