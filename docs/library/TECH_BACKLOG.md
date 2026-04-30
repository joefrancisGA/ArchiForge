> **Scope:** Engineering-owned technical backlog items deferred from current sessions; audience is contributors and the AI assistant; not a buyer or operator document. Not a substitute for ADRs or the pending-questions owner decisions file.

# Tech backlog

Items here are **greenlit in principle** — the decision has been made and context is captured — but deferred for a future session rather than the current one. Pick any item up by searching the codebase for the files listed and applying the recorded approach.

**Priority order:** Items are listed highest → lowest priority. When picking up work, start at the top. Re-sort when new items are added: items that affect customer-visible correctness rank above ops/observability improvements, which rank above developer-experience polish.

| ID | Title | Priority driver | Size |
|----|-------|----------------|------|
| TB-001 | Harden async audit write paths (never block users) | Correctness — runs can be mislabelled `Failed` | ~2 h |
| TB-002 | OTel counter + log for production config validation warnings | Ops visibility — config misconfigurations are invisible without log polling | ~1 h |
| TB-003 | Performance regression sentinel — named-query allowlist CI gate | CI quality — prevent slow-query regressions from reaching production | ~3 h |

---

## TB-001 — Harden async audit write paths to best-effort (never block users)

**Decision (2026-04-29):** Audit write failures on async / fire-and-forget paths must not surface to the user or degrade their experience. Log the failure as a structured warning (include correlation ID and event type), increment a counter, and continue. Fail-closed behaviour is reserved for synchronous, user-visible paths where the audit record is part of the response contract (e.g. governance approval submission). See `docs/PENDING_QUESTIONS.md` — *Resolved 2026-04-29 (audit coverage on async paths)*.

**What to do:**

Three unprotected `_auditService.LogAsync` calls currently bypass `DurableAuditLogRetry` and can block users when the audit SQL write fails. Wrap each with `DurableAuditLogRetry.TryLogAsync` (the pattern already used for `RunLegacyReadyForCommitPromoted` at line 379 of `ArchitectureRunExecuteOrchestrator`):

| # | File | Event type | Risk if unwrapped |
|---|------|-----------|-------------------|
| 1 | `ArchLucid.Application/Runs/Orchestration/ArchitectureRunExecuteOrchestrator.cs` ~line 134 | `AuditEventTypes.Run.RetryRequested` | Audit SQL failure propagates to outer catch; run is mislabelled `Failed` |
| 2 | `ArchLucid.Application/Runs/Orchestration/ArchitectureRunCreateOrchestrator.cs` ~line 232 | `AuditEventTypes.RequestCreated` | Run already persisted; SQL failure returns error to user despite success |
| 3 | `ArchLucid.Application/Runs/Orchestration/ArchitectureRunCreateOrchestrator.cs` ~line 255 | `AuditEventTypes.RequestLocked` | Same as #2 |

**Also add** `archlucid_audit_write_failures_total` counter to `ArchLucidInstrumentation.cs` (label `event_type`) and increment it inside `DurableAuditLogRetry.TryLogAsync` after the final abandoned-attempt log line, so operators can alert on sustained audit drop rates without polling logs.

**Tests to add / update:**
- `ArchLucid.Application.Tests/Runs/Orchestration/` — verify that a faulting `IAuditService` stub does **not** cause `ExecuteRunAsync` or `CreateRunAsync` to throw when the fault is on these informational paths.
- `DurableAuditLogRetry` unit test for the new counter increment (use a test meter listener).

**Size estimate:** ~2 h, low blast radius, no API surface changes.

---

## TB-002 — OTel counter + log for production config validation warnings

**Decision (2026-04-29):** Startup config validation warnings should emit both a structured log line (status quo) **and** increment an OTel counter so operators can alert on them in Azure Monitor / Prometheus without grepping logs. Cardinality is bounded — rule names are code constants, not runtime strings (~8–10 rules today).

**What to do:**

1. Add `archlucid_startup_config_warnings_total` counter to `ArchLucidInstrumentation.cs` (label `rule_name`). Keep the label value a short, lowercase, underscore-separated constant name (e.g. `dev_bypass_all_enabled`, `jwt_bearer_not_required_in_production`) — never a free-form string.

2. In each validation rule class under `ArchLucid.Host.Core/Startup/Validation/Rules/` that currently calls `logger.LogWarning(...)`, also call `ArchLucidInstrumentation.RecordStartupConfigWarning(ruleName)` after the log line.

3. Add a static helper to `ArchLucidInstrumentation`:
   ```csharp
   public static void RecordStartupConfigWarning(string ruleName)
   {
       string r = string.IsNullOrWhiteSpace(ruleName) ? "unknown" : ruleName.Trim();
       StartupConfigWarningsTotal.Add(1, new TagList { { "rule_name", r } });
   }
   ```

4. Add a Terraform alert rule in `infra/modules/alerts/` that fires when `archlucid_startup_config_warnings_total` is non-zero on a `Production`-classified host (threshold: any increment in the last 5 minutes). Staging should emit a warning-severity alert only.

**Affected files:**
- `ArchLucid.Core/Diagnostics/ArchLucidInstrumentation.cs` — add counter + helper
- `ArchLucid.Host.Core/Startup/Validation/Rules/*.cs` — add counter call alongside each `LogWarning`
- `infra/modules/alerts/` — new Terraform alert rule

**Tests to add:**
- Unit test per rule: assert that a rule with a triggering condition increments the counter (use a test meter listener — same pattern as the circuit breaker counter tests).

**Size estimate:** ~1 h, zero blast radius, no API or schema changes.

---

## TB-003 — Performance regression sentinel: named-query allowlist CI gate

**Decision (2026-04-29):** SaaS product, no customer DBAs. Use a **named-query allowlist** (Option A) rather than SQL text snapshots. SQL text snapshots produce high CI noise on every whitespace / ORM / parameter change, eroding gate trust; the allowlist keeps the gate high-signal. See `docs/PENDING_QUESTIONS.md` — *Resolved 2026-04-29 (performance regression sentinel approach)*.

**What to do:**

1. Create `tests/performance/query-allowlist.json` — a JSON array of objects, one per query that must meet its p95 threshold:
   ```json
   [
     { "name": "GetRunsByTenantId",       "p95ThresholdMs": 200 },
     { "name": "AppendAuditEvent",        "p95ThresholdMs": 50  },
     { "name": "GetFindingsByRunId",      "p95ThresholdMs": 150 },
     { "name": "GetGoldenManifestById",   "p95ThresholdMs": 100 }
   ]
   ```
   Seed with the four most latency-sensitive queries identified during existing k6 / integration runs. Grow the list deliberately as new critical paths are added.

2. Create `scripts/ci/assert_query_performance.py` — reads `query-allowlist.json`, compares p95 values from a k6 / test-run output JSON against each threshold, and exits non-zero with a clear per-query diff if any threshold is exceeded.

3. Wire into `.github/workflows/ci.yml` as a non-blocking **warning** gate initially (`continue-on-error: true`); flip to blocking once the baseline numbers are stable across 3 consecutive green runs.

4. Add `archlucid_query_p95_ms` histogram to `ArchLucidInstrumentation.cs` (label `query_name`) so the same thresholds can be monitored in production Azure Monitor, not just in CI.

**Affected files:**
- `tests/performance/query-allowlist.json` — new
- `scripts/ci/assert_query_performance.py` — new
- `.github/workflows/ci.yml` — add gate step
- `ArchLucid.Core/Diagnostics/ArchLucidInstrumentation.cs` — add histogram

**Tests to add:**
- Unit test for `assert_query_performance.py`: green case (all under threshold), red case (one over), missing-query-name case (script should warn, not fail, for unknown names so new queries don't silently break CI).

**Size estimate:** ~3 h, zero blast radius, no API or schema changes.

---
