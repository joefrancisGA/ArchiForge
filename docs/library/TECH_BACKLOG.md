> **Scope:** Engineering-owned technical backlog items deferred from current sessions; audience is contributors and the AI assistant; not a buyer or operator document. Not a substitute for ADRs or the pending-questions owner decisions file.

# Tech backlog

Items here are **greenlit in principle** — the decision has been made and context is captured — but deferred for a future session rather than the current one. Pick any item up by searching the codebase for the files listed and applying the recorded approach.

**Priority order:** Items are listed highest → lowest priority. When picking up work, start at the top. Re-sort when new items are added: items that affect customer-visible correctness rank above ops/observability improvements, which rank above developer-experience polish.

| ID | Title | Priority driver | Size |
|----|-------|----------------|------|
| TB-001 | Harden async audit write paths (never block users) | **Complete** — landed + regression tests | Done |
| TB-002 | OTel counter + log for production config validation warnings | **Complete** — counter + Host.Core startup paths + Composition.Tests + alerts module stub | Done |
| TB-003 | Performance regression sentinel — named-query allowlist CI gate | CI quality — prevent slow-query regressions from reaching production | ~3 h |
| TB-004 | Wire OTel exporters + verify agent-output metrics; add Azure alerts | Ops / release bar — conservative quality posture needs visible trends (`archlucid_agent_output_*`) | ~1–2 h |
| TB-005 | AI-assisted owner pen-test support (Cursor agent) | Security / V1 assurance — structured help for 2026-Q2 owner exercise | Ongoing (time-boxed sessions) |
| TB-006 | Type-migrate `dbo.ComparisonRecords` run id columns → `UNIQUEIDENTIFIER` + FK to `dbo.Runs` | Referential correctness — orphans are detection-only until types align (ADR-0012 / migration 047) | ~4–8 h |

---

## TB-001 — Harden async audit write paths to best-effort (never block users)

**Status:** **Shipped** in mainline (`DurableAuditLogRetry.TryLogAsync` on all three paths, `archlucid_audit_write_failures_total`, `ArchitectureRunExecuteOrchestratorRetryRequestedAuditTests`, `ArchitectureRunCreateOrchestratorInformationalAuditBestEffortTests`, `DurableAuditLogRetryTests`). Retained verbatim below as the specification audit trail.

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

**Status:** **Shipped** in mainline (`archlucid_startup_config_warnings_total`, `RecordStartupConfigWarning`, `StartupValidationWarningRuleNames`, `infra/modules/alerts/`). **`Startup/Validation/Rules/*.cs`** use error collection (no `LogWarning` today); advisory metrics are wired on **`AuthSafetyGuard`**, **`LlmPromptRedactionProductionWarningPostConfigure`**, **`RlsBypassPolicyBootstrap`**, **`ArchLucidPersistenceStartup`** (missing SQL connection string), plus existing **`ProductionLikeHostingMisconfigurationAdvisor`** / **`ArchLucidLegacyConfigurationWarnings`**.

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

## TB-004 — Wire OTel exporters + verify agent-output metrics; add Azure alerts

**Decision / context (2026-05-01):** Product stance for agent quality favors a **conservative** release bar; **`archlucid_agent_output_*`** histograms and **`archlucid_agent_output_quality_gate_total`** must reach a backend before **trend charts** or **email alerts** are possible. Code already emits metrics after successful execute; **`ObservabilityExtensions`** exports when App Insights connection string, OTLP endpoint, or Prometheus scrape is configured (`docs/library/OBSERVABILITY.md` § *Export path configuration*).

**What to do (checklist):**

1. **Per environment (staging → production):** Set **at least one** of:
   - **`APPLICATIONINSIGHTS_CONNECTION_STRING`** (preferred on Azure), or **`ApplicationInsights:ConnectionString`**, or **`Observability:AzureMonitor:ApplicationInsightsConnectionString`** on the **API** host; or
   - Non-empty **`Observability:Otlp:Endpoint`** (+ **`Protocol`** / **`Headers`** as needed); or
   - **`Observability:Prometheus:Enabled`** with scrape auth credentials and a scraper pointing at **`/metrics`** (trusted network only).

2. **`ArchLucid.Worker`:** If running Worker in the same subscription, apply the **same** exporter settings so worker-originated telemetry is not orphaned.

3. **Smoke verification:** After deploy, run **one full execute**; in **Application Insights → Metrics** (or OTLP sink), confirm **`archlucid_agent_output_semantic_score`**, **`archlucid_agent_output_structural_completeness_ratio`**, and **`archlucid_agent_output_quality_gate_total`** appear (Azure may normalize names — search by meter / namespace).

4. **Alerts:** Create **Azure Monitor metric alerts** (or Grafana rules) + **Action group → email** — e.g. semantic **p10** over 24h below agreed floor, or elevated **`rejected`** rate on **`quality_gate_total`**. Product does not ship pre-built rules.

5. **Optional:** Deploy **`infra/terraform-otel-collector`** for tail sampling; lower **`Observability:Tracing:SamplingRatio`** affects **traces**, not the agent-output **metric** path — document any sampling choice for on-call.

**Reference docs:** `docs/library/AGENT_OUTPUT_EVALUATION.md` §9; `docs/quality/MANUAL_QA_CHECKLIST.md` §8.4.

**Size estimate:** ~1–2 h of ops / Terraform / portal work (no mandatory code change unless exporter wiring gaps are found).

---

## TB-005 — AI-assisted owner pen-test support (Cursor agent)

**Context (2026-05-01):** External third-party penetration testing is **V2**; **V1** relies on an **owner-conducted** exercise documented in [`docs/security/pen-test-summaries/2026-Q2-OWNER-CONDUCTED.md`](../security/pen-test-summaries/2026-Q2-OWNER-CONDUCTED.md), aligned with [`docs/security/PENTEST_EXTERNAL_UI_CHECKLIST.md`](../security/PENTEST_EXTERNAL_UI_CHECKLIST.md) and [`docs/security/SYSTEM_THREAT_MODEL.md`](../security/SYSTEM_THREAT_MODEL.md). Target window **~2026-06-15**, after repeatable builds, UI stability, and reliable Azure deploy — see also [`QUALITY_ASSESSMENT_2026_05_01_INDEPENDENT_68_20.md`](QUALITY_ASSESSMENT_2026_05_01_INDEPENDENT_68_20.md) § *Pending Questions*.

**Owner bar (recorded for assessments):** Remediate **material** findings before calling the engagement complete; **do not** refresh public posture until **Critical** and **High** are cleared; **track** all security issues in-repo (findings table + PR links).

**What the coding agent can do (pick up in chat):**

1. **Runbooks & coverage** — Expand checklist-driven sessions from the docs above so testing is repeatable (auth, RBAC, RLS, injection classes, IDOR, session / CSRF-relevant UI flows).
2. **Negative cases from code** — Given a route, controller, or policy class, propose **edge cases** (headers, roles, tenant scope, stale tokens) consistent with implementation.
3. **CI artefacts** — Help interpret **OWASP ZAP** and **Schemathesis** output; separate false positives vs likely issues; suggest tracker wording at **high level** (no public exploit recipes unless you explicitly want them in a non-public artefact).
4. **Tracker hygiene** — Structure findings rows (severity, summary, owner, PR, retest) for [`2026-Q2-OWNER-CONDUCTED.md`](../security/pen-test-summaries/2026-Q2-OWNER-CONDUCTED.md).
5. **Posture text** — When retests are green, draft **stub → final** narrative that matches what was run and fixed and stays consistent with [`docs/go-to-market/TRUST_CENTER.md`](../go-to-market/TRUST_CENTER.md).

**Explicit limits:** The agent does **not** autonomously attack **archlucid.net** or Azure; **you** run tools in your environments and supply redacted logs or behaviour descriptions. This backlog item is **not** a substitute for a **V2** third-party report.

**Size estimate:** Ongoing — budget **30–60 min sessions** per surface or CI failure cluster; close the item when the 2026-Q2 owner tracker is complete and posture text is updated.

---

## TB-006 — Type-migrate ComparisonRecords.LeftRunId / RightRunId → UNIQUEIDENTIFIER + FK to dbo.Runs

**Context:** Migration **`047_DropForeignKeysToArchitectureRuns.sql`** dropped FKs because legacy coordinator columns (`NVARCHAR(64)`) could not reference `dbo.Runs.RunId` (`UNIQUEIDENTIFIER`). Orphan remediation for **`ComparisonRecords`** remains **probe plus admin delete** (`DataConsistencyOrphanRemediationSql`). Authority chain FK parity ships as **`134_FK_Authority_Chain_Runs_DbUpParity.sql`** aligned with **`ArchLucid.sql`**; comparison rows are still the structural gap until types align.

**What to do:**

1. **Brownfield hygiene:** Rows whose `LeftRunId` / `RightRunId` are empty, non-guid strings, or do not resolve to **`dbo.Runs`** must be counted and remediated (admin **`/v1/admin/diagnostics`** comparison orphan path or scripted delete using the same predicates as probes).
2. **Schema:** Alter **`LeftRunId`** / **`RightRunId`** to **`UNIQUEIDENTIFIER NULL`**, preferably in a phased migration with explicit rollback.
3. **Constraints:** Add **`FK_ComparisonRecords_Runs_LeftRunId`** / **`FK_ComparisonRecords_Runs_RightRunId`** (`ON DELETE NO ACTION`; nullable columns imply FK applies only where non-null). Follow conditional add plus orphan guards as in **`092_FK_Outbox_Alerts_Batch1.sql`**.
4. **Code:** Comparison persistence and probes stop using **`TRY_CONVERT`** string probes; repositories write **`uniqueidentifier`**.
5. **Tests:** Persistence test that invalid run references fail **`INSERT`**; optional positive orphan detection analogue to **`DataConsistencyOrphanProbePositiveDetectionSqlIntegrationTests`**.

**Affected areas:** `ArchLucid.Persistence/Migrations/`, `ArchLucid.Persistence/Scripts/ArchLucid.sql`, `ArchLucid.Host.Core/DataConsistency/*`, coordinator comparison repositories.

**Size estimate:** ~4–8 h depending on brownfield orphan volume and rollout coordination.

---


---

## TB-007 � LLM correctness boundary: three remaining gaps after 2026-05-01 session

**Context:** The quality assessment sessions identified the LLM correctness boundary as the highest engineering risk. Three gaps were documented and partially addressed. The items below require either owner decisions or operational prerequisites before they can be closed.

### Gap A � Promote cohort-real-llm-gate to a required PR status check

**Status:** Blocked on owner task. The Azure OpenAI deployment (rchlucid-golden-cohort in eastus) must be provisioned and the GitHub protected-Environment secret (ARCHLUCID_GOLDEN_COHORT_AZURE_OPENAI_KEY or federated identity) injected before the gate can be promoted. See docs/runbooks/GOLDEN_COHORT_REAL_LLM_GATE.md � 2 and � 6 for the one-line promotion change and the stop-and-ask boundary.

**What to do (once deployment exists):**
1. Inject secret into the protected Environment per PENDING_QUESTIONS.md Q15.
2. Add cohort-real-llm-gate to the required status checks in the main branch protection rule.
3. Open a separate PR (not the same as the deployment PR) for the promotion.

### Gap B � Enable EnforceOnReject after product decision

**Status:** The AgentOutputQualityGateOptions.EnforceOnReject flag was added (2026-05-01) and defaults to alse. Enabling it causes AgentOutputEvaluationRecorder to throw AgentOutputQualityGateRejectedException when an agent trace scores below the reject thresholds, which propagates through AgentOutputTraceEvaluationHook.AfterSuccessfulExecuteAsync and will abort the post-execute step for the run.

**Decision needed:** Does a quality gate rejection block the pilot user's run from completing, or is it operator-only telemetry? If blocking: enable the flag in ppsettings.SaaS.json under ArchLucid:AgentOutput:QualityGate:EnforceOnReject: true and define the user-facing error contract. If telemetry-only: document the decision and close this item.

### Gap C � Eval corpus has no real-mode scenarios

**Status:** All three scenarios in 	ests/eval-corpus/ have "mode": "simulator" in their qualityEvidence block. The eval_agent_corpus.py CI script runs against simulator agent result fixtures. There are no CI-run checks that assert on real-model finding quality against expected keyword patterns.

**What to do:**
1. Add at least one eval-corpus scenario with "mode": "real" and expectedFindings keyword checks meaningful for real model output.
2. Wire a nightly or post-deploy job that runs eval_agent_corpus.py against the real-mode API (similar to the golden cohort gate).
3. Gate this on the same ARCHLUCID_GOLDEN_COHORT_REAL_LLM variable and budget probe as the cohort gate.

**Affected areas:** 	ests/eval-corpus/, scripts/ci/eval_agent_corpus.py, .github/workflows/golden-cohort-nightly.yml.

**Size estimate:** Gap A ~1 h (operational, no code). Gap B ~2 h (decision + config + error contract). Gap C ~4 h (scenario authoring + workflow wiring).
