# Cursor prompts for top-6 quality improvements (2026-04-14 assessment)

Five standalone prompts (improvements #1–#4 and #6 from `docs/QUALITY_ASSESSMENT_2026_04_14.md`; improvement #5 — Terraform `state mv` + `NEXT_REFACTORINGS.md` triage — is excluded by request). Each prompt is self-contained and ready to paste into a Cursor Agent session.

> **Key difference from the April 12 prompts** (`docs/QUALITY_IMPROVEMENT_PROMPTS.md`): every item referenced in those prompts has been partially or fully implemented since. These updated prompts target the **remaining gaps** identified by the April 14 assessment and reference the current codebase state.

---

## Prompt 1 — Expand live-API E2E coverage (Correctness, Reliability, Usability)

```
Expand the live-API E2E Playwright suite to cover additional operator workflows beyond the existing happy-path, conflict, governance-rejection, negative-paths, and error-states specs. The existing five specs in `archlucid-ui/e2e/live-api-*.spec.ts` prove core create→execute→commit→govern→audit flows, but the April 14 quality assessment identified that advisory, replay, export comparison, and policy pack operator paths have no live-API E2E coverage.

**Context (do NOT modify these existing files unless noted):**
- `archlucid-ui/e2e/live-api-journey.spec.ts` — happy path (create→commit→manifest UI→export→govern→audit)
- `archlucid-ui/e2e/live-api-conflict-journey.spec.ts` — idempotent commit + 404 on missing run
- `archlucid-ui/e2e/live-api-governance-rejection.spec.ts` — submit→reject flow + audit + invalid-transition 400s
- `archlucid-ui/e2e/live-api-negative-paths.spec.ts` — self-approval 400, unknown run 404, empty body 400/422
- `archlucid-ui/e2e/live-api-error-states.spec.ts` — UI resilience pages under real API (no mock)
- `archlucid-ui/e2e/helpers/live-api-client.ts` — typed API helper functions
- `archlucid-ui/playwright.live.config.ts` — live config (all live specs share this)
- `.github/workflows/ci.yml` → `ui-e2e-live` job (merge-blocking)

**What to build:**

1. **`archlucid-ui/e2e/live-api-advisory-flow.spec.ts`** (`live-api-advisory-flow` describe):
   - After a committed run exists (reuse the create→execute→commit pattern from the journey spec):
     a. `POST /v1/advisory/scans` to schedule an advisory scan for the run's project scope. Assert 200/201 or 409 (if already exists — soft assert).
     b. `GET /v1/advisory/scans?projectId=…` — list scans, verify the scheduled scan appears.
     c. Navigate to the operator UI `/advisory` page — verify it loads without error (heading visible, no `OperatorApiProblem` alert).
     d. Verify audit: `GET /v1/audit/search?runId=…` includes `AdvisoryScanScheduled`.

2. **`archlucid-ui/e2e/live-api-replay-export.spec.ts`** (`live-api-replay-export` describe):
   - After a committed run:
     a. `POST /v1/replay/run/{runId}` (or equivalent replay endpoint) — execute a replay. Assert 200.
     b. `GET /v1/artifacts/runs/{runId}/export` — verify the ZIP export still works post-replay. Assert 200 and `content-type` includes `zip` or `octet-stream`.
     c. Verify audit includes `ReplayExecuted` and `RunExported` for this run.
     d. Navigate to `/runs/{runId}` in the UI — verify the run detail still renders correctly after replay.

3. **`archlucid-ui/e2e/live-api-analysis-report.spec.ts`** (`live-api-analysis-report` describe):
   - After a committed run:
     a. `POST /v1/reports/analysis` (with the committed run's `runId`) — generate an analysis report. Assert 200.
     b. Verify the response JSON includes expected sections (e.g., `manifestVersion`, section flags).
     c. Verify audit includes `ArchitectureAnalysisReportGenerated`.
     d. If the DOCX export endpoint exists (`POST /v1/exports/docx`), assert it returns 200 with `application/vnd.openxmlformats-officedocument.wordprocessingml.document` content type.

4. **Add helpers to `live-api-client.ts`** as needed for any new API calls (follow the existing pattern: typed function + raw variant for negative paths).

5. **Update `docs/LIVE_E2E_HAPPY_PATH.md`:**
   - Add rows to the spec table for the three new spec files.
   - Add new steps to the sequence diagram if appropriate.

6. **Update `docs/TEST_STRUCTURE.md`:**
   - Add the new specs to the live E2E section.
   - Note that the advisory and replay specs exercise additional audit event types.

**Constraints:**
- Do NOT modify existing specs — only add new files and extend `live-api-client.ts`.
- All new specs must use `test.describe('live-api-…', …)` naming.
- Use `test.setTimeout(180_000)` — advisory and replay operations may be slow.
- Each spec must check `GET /health/ready` in `beforeAll` and fail fast with a clear message if the API is not up.
- If an endpoint does not exist or returns 404 (e.g., advisory scheduling not wired in the current build), use `test.skip()` with a clear message — do NOT fail the CI run for unimplemented features.
- Use `test.info().annotations` to record `e2e-run-id` and other context for CI triage (follow the pattern in `live-api-journey.spec.ts`).
- Follow the existing TypeScript style (strict types, `expect.soft` for non-critical checks, `throwIfNotOk` for critical steps).
- The CI `ui-e2e-live` job already runs all `live-api-*.spec.ts` files via the live config — no CI changes needed.
```

---

## Prompt 2 — Mandatory forensic prompt snapshots + semantic evaluation harness (Explainability, AI/Agent Readiness, Traceability, Auditability)

> **Implementation status:** Product code and tests cover this prompt (blob retries with failure counter + `BlobUploadFailed`, semantic evaluator + OTel, API fields, docs). Blob retry backoff matches the spec below: **500 ms fixed** between attempts, **3** tries per blob.

```
Enhance the agent trace forensics system to make prompt blob persistence more reliable and build a semantic evaluation layer on top of the existing structural evaluator. The April 14 quality assessment found that while `PersistFullPrompts` defaults to true and blob storage is wired, the upload is fire-and-forget with no retry, and the evaluation harness only checks structural JSON key presence — there is no semantic scoring of agent output quality.

**Context (existing code — read these first):**
- `ArchLucid.AgentRuntime/AgentExecutionTraceRecorder.cs` — records traces; uploads full prompts to blob if `PersistFullPrompts` is true; fire-and-forget on failure.
- `ArchLucid.AgentRuntime/AgentExecutionTraceStorageOptions.cs` — config: `PersistFullPrompts` (default true).
- `ArchLucid.AgentRuntime/Evaluation/IAgentOutputEvaluator.cs` — structural evaluator interface: `Evaluate(traceId, parsedResultJson, agentType)` → `AgentOutputEvaluationScore`.
- `ArchLucid.AgentRuntime/Evaluation/AgentOutputEvaluator.cs` — checks for expected top-level JSON keys.
- `ArchLucid.AgentRuntime/Evaluation/AgentOutputEvaluationRecorder.cs` — loads traces, scores them, emits OTel metrics.
- `ArchLucid.Persistence/Migrations/053_AgentExecutionTrace_FullPromptBlobKeys.sql` — columns for blob keys + model metadata.
- `docs/AGENT_TRACE_FORENSICS.md` — blob layout and retrieval docs.
- `docs/AGENT_OUTPUT_EVALUATION.md` — structural evaluation architecture docs.

**What to build:**

1. **Retry-capable prompt blob persistence** (`AgentExecutionTraceRecorder`):
   - Replace the single fire-and-forget blob upload with a retry of up to 2 additional attempts (3 total) with 500ms backoff between retries.
   - If all retries fail, log a warning (existing behavior) AND emit an OTel counter `archlucid_agent_trace_blob_upload_failures_total` with tags `{ agent_type, blob_type }` (blob_type = "system_prompt" | "user_prompt" | "response").
   - Add a boolean `BlobUploadFailed` property to `AgentExecutionTrace` (nullable, default null). Set to `true` when all retries fail. This allows operators to query for traces with missing blobs.
   - Add a SQL migration for the new column: `{next_migration_number}_AgentExecutionTrace_BlobUploadFailed.sql` (follow the existing numbering convention — check the highest migration number in `ArchLucid.Persistence/Migrations/` and increment).
   - Update `ArchLucid.Persistence/Scripts/ArchLucid.sql` with the same column.

2. **Semantic evaluation layer** — new interface and implementation:
   - `ArchLucid.AgentRuntime/Evaluation/IAgentOutputSemanticEvaluator.cs`:
     ```csharp
     public interface IAgentOutputSemanticEvaluator
     {
         AgentOutputSemanticScore Evaluate(string traceId, string? parsedResultJson, AgentType agentType);
     }
     ```
   - `ArchLucid.AgentRuntime/Evaluation/AgentOutputSemanticScore.cs`:
     - `string TraceId`
     - `AgentType AgentType`
     - `double ClaimsQualityRatio` — fraction of claims that have non-empty `evidence` or `evidenceRefs`.
     - `double FindingsQualityRatio` — fraction of findings with non-empty `severity`, `description`, AND `recommendation`.
     - `int EmptyClaimCount` — claims with no evidence.
     - `int IncompleteFindingCount` — findings missing severity, description, or recommendation.
     - `double OverallSemanticScore` — weighted average: (ClaimsQuality * 0.4 + FindingsQuality * 0.6), or 0 if both denominators are 0.
   - `ArchLucid.AgentRuntime/Evaluation/AgentOutputSemanticEvaluator.cs`:
     - Parse `parsedResultJson` as a `JsonDocument`.
     - Extract the `claims` array: for each claim, check if it has a non-empty `evidenceRefs` (array with at least one element) or `evidence` (non-empty string). Count claims with and without evidence.
     - Extract the `findings` array: for each finding, check that `severity` is a non-empty string, `description` length > 10 chars, and `recommendation` length > 5 chars. Count complete vs incomplete.
     - Compute ratios and overall score.
     - If JSON is unparseable or arrays are missing, return score 0 with appropriate flags.

3. **Wire semantic evaluator into the recorder** (`AgentOutputEvaluationRecorder`):
   - After structural scoring, also run `IAgentOutputSemanticEvaluator.Evaluate`.
   - Emit OTel histogram `archlucid_agent_output_semantic_score` with `agent_type` tag.
   - Log a warning when `OverallSemanticScore < 0.3`.

4. **Expose via existing API** — in the `GET .../run/{runId}/agent-evaluation` endpoint:
   - Add semantic scores to the response alongside structural scores.
   - Add per-trace `blobUploadFailed` flag so the UI can show a warning icon for traces with missing forensic data.

5. **Tests (all `[Trait("Suite", "Core")]`):**
   - `AgentOutputSemanticEvaluatorTests`:
     - Valid JSON with 3 claims (2 with evidence, 1 without) → ClaimsQualityRatio = 2/3.
     - Valid JSON with 4 findings (3 complete, 1 missing recommendation) → FindingsQualityRatio = 3/4.
     - Empty/null JSON → score 0.
     - JSON with no claims/findings arrays → score 0.
     - Verify OverallSemanticScore weighted calculation.
   - `AgentExecutionTraceRecorderReproTests` — add:
     - Test that blob upload retries 3 times on failure.
     - Test that `BlobUploadFailed` is set to true after all retries fail.
     - Test that `archlucid_agent_trace_blob_upload_failures_total` counter is incremented on failure.
   - `AgentOutputEvaluationRecorderTests` — add:
     - Test that semantic scores are recorded alongside structural scores.
     - Test that warning is logged when semantic score < 0.3.

6. **Documentation:**
   - Update `docs/AGENT_OUTPUT_EVALUATION.md`: add a "Semantic evaluation" section explaining the scoring model, thresholds, and OTel metric names.
   - Update `docs/AGENT_TRACE_FORENSICS.md`: add a "Reliability" section explaining the retry behavior and `BlobUploadFailed` query pattern.
   - Update `docs/OBSERVABILITY.md`: add the new metrics (`archlucid_agent_trace_blob_upload_failures_total`, `archlucid_agent_output_semantic_score`).

**Constraints:**
- Do NOT remove or modify the existing structural evaluator — the semantic evaluator is additive.
- Do NOT change `AgentExecutionTraceStorageOptions.PersistFullPrompts` default (keep true).
- Do NOT use an LLM for evaluation — this must be purely deterministic JSON inspection.
- The retry must not block the agent execution pipeline longer than 3 seconds total (retries are acceptable because the blob upload is already after trace persistence).
- Follow existing code style: concrete types over var, null checks, one blank line before if/foreach, each class in its own file, LINQ over foreach where performance is acceptable.
- Each new class must be in its own file.
```

---

## Prompt 3 — Load-test baseline with CI regression gate (Performance, Reliability, Scalability, Correctness)

```
Extend the existing k6 load test suite with write-path scenarios and wire a CI regression gate that publishes baseline metrics. The April 14 quality assessment found that while `tests/load/smoke.js` exists with read-only scenarios and `scripts/load/hotpaths.js` covers create-run under manual dispatch, there is no automated CI smoke that includes the write path, and no published p95 regression gate in the PR workflow.

**Context (existing code — read these first):**
- `tests/load/smoke.js` — read-only k6 smoke: health, runs_list, version, audit_search. Scenarios use `ramping-vus` with thresholds on `http_req_duration` p95 and `http_req_failed` rate.
- `scripts/load/hotpaths.js` — full write-path k6 script (create run, list runs, manifest, comparisons, retrieval search) used by the manual `load-test.yml` workflow.
- `.github/workflows/load-test.yml` — manual-dispatch workflow running `hotpaths.js` against Docker Compose full-stack.
- `docs/LOAD_TEST_BASELINE.md` — baseline documentation with architecture and component breakdown.
- `.github/workflows/ci.yml` — main CI pipeline (no k6 job currently).

**What to build:**

1. **`tests/load/ci-smoke.js`** — a new k6 script designed specifically for CI that combines read and write paths in a short, deterministic run:
   - **Scenarios (all use DevelopmentBypass auth):**
     a. `health` — GET `/health/live` + `/health/ready` (5 VUs, 20s, `constant-vus`).
     b. `create_run` — POST `/v1/architecture/request` with a minimal valid body (generate unique `requestId` per iteration using `__VU` + `__ITER`). Assert 200/201. (2 VUs, 30s, `constant-vus`). This is the critical write-path baseline.
     c. `list_runs` — GET `/v1/architecture/runs` (3 VUs, 20s, `constant-vus`).
     d. `audit_search` — GET `/v1/audit/search?take=20` (2 VUs, 20s, `constant-vus`).
   - **Thresholds (regression gate):**
     - `http_req_failed`: rate < 0.02 (2% tolerance for CI variability).
     - `http_req_duration{k6ci:health_live}`: p(95) < 500ms; `http_req_duration{k6ci:health_ready}`: p(95) < 1500ms (ready probes dependencies; separate from live).
     - `http_req_duration{k6ci:create_run}`: p(95) < 3000ms (write path is slower; include agent simulator time).
     - `http_req_duration{k6ci:list_runs}`: p(95) < 1500ms.
     - `http_req_duration{k6ci:audit_search}`: p(95) < 1500ms.
   - Use `k6ci` as the tag namespace (distinct from `k6smoke` in `smoke.js`).
   - Add `X-Correlation-ID: k6-ci-{scenario}-{__VU}-{__ITER}` header on every request.
   - Create run body should match the structure used in live E2E: `{ requestId, description, systemName, environment, cloudProvider, constraints, requiredCapabilities, assumptions, priorManifestVersion }`.
   - Export summary: `--summary-export /tmp/k6-ci-summary.json`.
   - Total runtime must stay under 60 seconds.

2. **CI job** — add a new job `k6-ci-smoke` to `.github/workflows/ci.yml`:
   - **Depends on:** `dotnet-fast-core` (so the API image is built). If the CI uses a SQL Server service container pattern, reuse it; otherwise, add one (same approach as `ui-e2e-live`).
   - **Steps:**
     a. Checkout.
     b. Setup .NET (reuse existing step).
     c. Start SQL Server service container (if not inherited from a dependent job).
     d. Create database: `sqlcmd` to create `ArchLucidK6Ci` database.
     e. Start API: `dotnet run --project ArchLucid.Api --configuration Release` with env vars:
        - `ConnectionStrings__ArchLucid=Server=127.0.0.1;Database=ArchLucidK6Ci;User Id=sa;Password=…;TrustServerCertificate=True`
        - `ArchLucidAuth__Mode=DevelopmentBypass`
        - `AgentExecution__Mode=Simulator`
        - Background the process.
     f. Wait for `GET /health/ready` (retry loop, 60s timeout).
     g. Install k6 (use the Grafana k6 APT repo pattern from `load-test.yml`).
     h. Run: `k6 run tests/load/ci-smoke.js --summary-export /tmp/k6-ci-summary.json --env BASE_URL=http://127.0.0.1:5128`.
     i. Upload `/tmp/k6-ci-summary.json` as artifact `k6-ci-smoke-summary`.
     j. Print summary to job step output using `scripts/ci/print_k6_summary_metrics.py` if it exists, or `cat /tmp/k6-ci-summary.json | python3 -m json.tool`.
   - **Initially `continue-on-error: true`** with a comment: `# TODO: make blocking after 2 weeks of stable green runs (target: 2026-05-01)`.
   - **Timeout:** 10 minutes (`timeout-minutes: 10`).

3. **Documentation:**
   - Update `docs/LOAD_TEST_BASELINE.md`:
     - Add a new section "CI smoke (automated)" explaining the `ci-smoke.js` script, its scenarios, thresholds, and how it differs from the manual `hotpaths.js` workflow.
     - Note that write-path baselines from CI are established by `create_run` scenario.
   - Update `docs/TEST_STRUCTURE.md`:
     - Add a row for k6 CI smoke in the test tier table (Tier: CI Performance, Filter: `k6 run tests/load/ci-smoke.js`, Purpose: regression gate for API latency baselines).

**Constraints:**
- Do NOT modify `tests/load/smoke.js` or `scripts/load/hotpaths.js` — the new script is additive.
- Do NOT modify `load-test.yml` — the manual workflow stays separate.
- The CI job must not require Docker Compose — it uses the same pattern as the live E2E job (dotnet run + service container SQL).
- k6 is a Go binary — do NOT add it to `package.json`.
- All requests must include `X-Correlation-ID` headers for trace correlation.
- The `create_run` scenario must be safe for concurrent VUs — each iteration uses a unique `requestId` so there are no conflicts.
- Keep total CI job time under 5 minutes including API startup.
- The thresholds are deliberately generous for CI (GitHub Actions runners have variable performance). They should be tightened after baseline data is collected.
```

---

## Prompt 4 — Promote baseline mutation audit to durable SQL + strengthen audit UI (Auditability, Policy & Governance, Security, Usability)

```
Promote the most important baseline mutation audit events from log-only (`IBaselineMutationAuditService`) to durable SQL (`IAuditService`), and enhance the operator audit UI page with OTel trace correlation and agent trace linkage. The April 14 quality assessment found that the audit page already has clear filters, export CSV, load more, and summary heading — but baseline orchestration events (run create/execute/commit mutations) are only in structured logs, not queryable in the audit UI.

**Context (existing code — read these first):**
- `docs/AUDIT_COVERAGE_MATRIX.md` — maps operations to audit signals; baseline mutation events are log-only.
- `ArchLucid.Application/Runs/Orchestration/ArchitectureRunCreateOrchestrator.cs` — calls `IBaselineMutationAuditService.RecordAsync` with `AuditEventTypes.Baseline.Architecture.RunCreated`.
- `ArchLucid.Application/Runs/Orchestration/ArchitectureRunExecuteOrchestrator.cs` — calls baseline audit with `Architecture.RunStarted`, `Architecture.RunExecuteSucceeded`.
- `ArchLucid.Application/Runs/Orchestration/ArchitectureRunCommitOrchestrator.cs` — calls baseline audit with `Architecture.RunCompleted`.
- `ArchLucid.Application/Common/BaselineMutationAuditService.cs` — writes to `ILogger` only (no SQL).
- `ArchLucid.Core/Audit/AuditEventTypes.cs` — Core constants + `Baseline.*` nested constants.
- `archlucid-ui/src/app/audit/page.tsx` — audit UI page with search, clear filters, export CSV, load more, summary heading.
- `archlucid-ui/src/app/audit/audit-ui-helpers.ts` — `canExportAuditCsv`, `formatAuditSummaryHeading`.
- `archlucid-ui/src/lib/api.ts` — `searchAuditEvents`, `downloadAuditExportCsv`, `getAuditEventTypes` functions.

**What to build:**

1. **Promote four baseline orchestration events to durable audit:**
   In these orchestrators, add `IAuditService.LogAsync` calls **alongside** the existing `IBaselineMutationAuditService.RecordAsync` calls (do NOT remove the baseline calls — they serve a different purpose for structured log consumers):

   a. `ArchitectureRunCreateOrchestrator` — add `IAuditService.LogAsync` with event type `AuditEventTypes.RunCreated` (new constant). DataJson: `{ requestId, projectId, systemName }`.
   b. `ArchitectureRunExecuteOrchestrator` — add `IAuditService.LogAsync` with event type `AuditEventTypes.RunExecuteStarted` (new constant). DataJson: `{ runId, agentMode }`.
   c. `ArchitectureRunExecuteOrchestrator` — on success, add `IAuditService.LogAsync` with event type `AuditEventTypes.RunExecuteSucceeded` (new constant). DataJson: `{ runId, agentCount, elapsedMs }`.
   d. `ArchitectureRunCommitOrchestrator` — the happy-path commit completion currently only emits baseline audit. Add `IAuditService.LogAsync` with event type `AuditEventTypes.RunCommitCompleted` (new constant). DataJson: `{ runId, manifestVersion, goldenManifestId }`. NOTE: The existing `AuthorityRunOrchestrator` already emits `RunStarted` and `RunCompleted` for the authority pipeline — these new events cover the *coordinator* orchestration steps, which are distinct.

   For each new constant:
   - Add to `ArchLucid.Core/Audit/AuditEventTypes.cs`.
   - Update the `<!-- audit-core-const-count:69 -->` marker in `docs/AUDIT_COVERAGE_MATRIX.md` to the new count.
   - Add a row to the "Operations → durable audit" table.

2. **Add OTel trace ID to audit event display:**
   - The `AuditEvent` model (check `ArchLucid.Core/Audit/AuditEvent.cs` or `ArchLucid.Contracts`) may or may not have an `OtelTraceId` property. If it does, wire it to the UI. If it doesn't:
     a. Add `OtelTraceId` (string?, nullable) to the audit event model.
     b. In `AuditService.LogAsync`, capture `Activity.Current?.TraceId.ToString()` and set it on the event before persistence.
     c. Add a SQL migration for the new column if needed.
   - In the audit UI page (`page.tsx`), for each audit event card, display the OTel trace ID (if non-null) as a monospace label below the correlation ID line. Format: `Trace: {traceId}` (truncated to first 16 chars with full value in a `title` tooltip).

3. **Add "View agent traces" link in audit UI:**
   - For audit events that have a `runId`, add a small link: `[Agent traces]` → navigates to `/runs/{runId}#agent-traces` (or whatever anchor the run detail page uses for the trace section).
   - This connects the audit timeline to the forensic agent trace view.

4. **Tests:**
   - In `ArchLucid.Application.Tests/` (or appropriate test project), add tests for each orchestrator verifying:
     - When the orchestration succeeds, `IAuditService.LogAsync` is called with the expected event type and DataJson shape.
     - When the orchestration fails, the failure audit event is emitted (if applicable) and the durable audit call does not throw (use the existing `Try/catch` pattern if the orchestrator has one).
   - In `archlucid-ui/` Vitest tests:
     - Verify the OTel trace ID is displayed when present in the audit event data.
     - Verify the "Agent traces" link renders for events with a `runId`.
   - All .NET tests tagged `[Trait("Suite", "Core")]`.

5. **Documentation:**
   - Update `docs/AUDIT_COVERAGE_MATRIX.md`:
     - Move the four promoted operations from the "Baseline mutation logging only" table to the "Operations → durable audit" table.
     - Update the Core constant count.
     - Add a note in "Design notes" explaining why dual-write (baseline + durable) is intentional: baseline remains for log-aggregation consumers; durable is for the operator audit UI.
   - Update `docs/operator-shell.md` (or relevant UI docs):
     - Note the OTel trace ID display on audit events.
     - Note the "Agent traces" link for run-linked audit events.

**Constraints:**
- Do NOT remove `IBaselineMutationAuditService.RecordAsync` calls — add `IAuditService.LogAsync` alongside them (dual-write pattern, same as `GovernanceWorkflowService`).
- Do NOT change the existing `searchAuditEvents` API signature.
- Do NOT modify historical SQL migration files.
- The durable audit calls must not throw if they fail — wrap in try/catch with warning log (follow the pattern used elsewhere in the codebase for audit calls on hot paths).
- Follow existing code style: concrete types over var, null checks, one blank line before if/foreach, LINQ over foreach.
- Each new class/type must be in its own file.
```

---

## Prompt 5 — Configurable pre-commit governance gate with severity thresholds + approval SLA (Policy & Governance, Correctness, Security)

```
Extend the existing pre-commit governance gate to support configurable severity thresholds (not just Critical) and add an approval SLA with notification escalation. The April 14 quality assessment found that `PreCommitGovernanceGate` already blocks on Critical findings with a config gate (`ArchLucid:Governance:PreCommitGateEnabled`), but the severity level is hardcoded and there is no approval SLA to prevent governance bottlenecks.

**Context (existing code — read these first):**
- `ArchLucid.Application/Governance/PreCommitGovernanceGate.cs` — current gate: loads policy pack assignments with `BlockCommitOnCritical=true`; collects `FindingSeverity.Critical` findings; returns `PreCommitGateResult`.
- `ArchLucid.Contracts/Governance/PreCommitGovernanceGateOptions.cs` — config: `PreCommitGateEnabled` bool only.
- `ArchLucid.Contracts/Governance/PreCommitGateResult.cs` — result: `Blocked`, `Reason`, `BlockingFindingIds`, `PolicyPackId`.
- `ArchLucid.Contracts/Governance/IPreCommitGovernanceGate.cs` — interface.
- `ArchLucid.Persistence/Migrations/054_PolicyPackAssignments_BlockCommitOnCritical.sql` — existing migration adding `BlockCommitOnCritical` column.
- `ArchLucid.Application.Tests/Governance/PreCommitGovernanceGateTests.cs` — existing tests.
- `ArchLucid.Application/Governance/GovernanceWorkflowService.cs` — approval/rejection/promotion workflow.
- `docs/PRE_COMMIT_GOVERNANCE_GATE.md` — existing feature documentation.
- `ArchLucid.Decisioning/Models/FindingSeverity.cs` — enum: `Info`, `Warning`, `Error`, `Critical`.

**What to build:**

### Part A — Configurable severity threshold

1. **Extend `PolicyPackAssignment` model and DDL:**
   - Add `BlockCommitMinimumSeverity` (int?, nullable) to the `PolicyPackAssignment` model/entity. When null AND `BlockCommitOnCritical` is true, behavior is unchanged (blocks on Critical only — backward compatible). When set, it represents the minimum `FindingSeverity` (as int enum value) that triggers a block: e.g., `3` = Critical, `2` = Error, `1` = Warning.
   - Add SQL migration `{next}_PolicyPackAssignments_BlockCommitMinimumSeverity.sql`:
     ```sql
     ALTER TABLE dbo.PolicyPackAssignments
       ADD BlockCommitMinimumSeverity INT NULL;
     ```
   - Update `ArchLucid.Persistence/Scripts/ArchLucid.sql` with the same column.
   - Update the Dapper repository (read/write) and InMemory repository for the new field.

2. **Update `PreCommitGovernanceGate.EvaluateAsync`:**
   - When evaluating an enforcing assignment:
     - If `BlockCommitMinimumSeverity` is set, use it as the threshold: block on any finding with `(int)severity >= assignment.BlockCommitMinimumSeverity`.
     - If `BlockCommitMinimumSeverity` is null but `BlockCommitOnCritical` is true, block on Critical only (existing behavior).
     - If neither is set, allow.
   - Update `PreCommitGateResult` to include a new property: `FindingSeverity MinimumBlockingSeverity` (the threshold that was applied). This helps operators understand why their commit was blocked at a particular severity level.
   - In the `Reason` string, include the severity label: e.g., "3 Error+ finding(s) block commit per policy pack assignment (pack abc123, minimum severity: Error)."

3. **Add a warning-only mode** to `PreCommitGateResult`:
   - New property: `bool WarnOnly` (default false). When true, the gate returns a 200 (commit proceeds) but includes warnings in the response body.
   - `PreCommitGovernanceGateOptions` gets a new property: `WarnOnlySeverities` (`FindingSeverity[]`, default empty). If the effective blocking severity is in this array, the gate sets `WarnOnly=true` instead of `Blocked=true`.
   - The commit orchestrator should: if `WarnOnly`, proceed with commit but include gate warnings in the commit response, and emit audit event `AuditEventTypes.GovernancePreCommitWarned` (new constant) instead of `GovernancePreCommitBlocked`.

### Part B — Approval SLA with escalation notification

4. **Approval SLA model:**
   - Add `ApprovalSlaHours` (int?, nullable) to the governance options: `ArchLucid:Governance:ApprovalSlaHours` (default null = no SLA).
   - Add `ApprovalSlaEscalationWebhookUrl` (string?, nullable): webhook URL to notify when SLA is breached.

5. **SLA tracking on approval requests:**
   - Add `SlaDeadlineUtc` (DateTime?, nullable) to the governance approval request model (and DDL migration).
   - When `GovernanceWorkflowService` creates an approval request and `ApprovalSlaHours` is configured, set `SlaDeadlineUtc = UtcNow + TimeSpan.FromHours(slaHours)`.

6. **SLA breach detection (background check):**
   - Create `ArchLucid.Application/Governance/ApprovalSlaMonitor.cs`:
     - Method: `CheckAndEscalateAsync(CancellationToken)`.
     - Query pending approval requests where `SlaDeadlineUtc <= UtcNow` and `Status == Pending`.
     - For each breached request:
       a. Emit audit event `AuditEventTypes.GovernanceApprovalSlaBreached` (new constant).
       b. If `ApprovalSlaEscalationWebhookUrl` is configured, POST a JSON notification: `{ approvalRequestId, runId, requestedBy, slaDeadlineUtc, breachedByMinutes }`. Use HMAC signing if the webhook secret config key is set (`ArchLucid:Governance:EscalationWebhookSecret`). Follow the existing HMAC signing pattern in the codebase.
       c. Set a flag on the approval request (`SlaBreachNotifiedUtc`) to avoid repeat notifications.
   - This should be wired as a periodic background task (e.g., called from the worker host or a timer-based service) — but for this prompt, just create the class with the method. The wiring into the host can be a follow-up.

7. **Tests (all `[Trait("Suite", "Core")]`):**
   - `PreCommitGovernanceGateTests` — add:
     - Test: `BlockCommitMinimumSeverity = Error (2)` with Error findings → blocked.
     - Test: `BlockCommitMinimumSeverity = Error (2)` with only Warning findings → allowed.
     - Test: `BlockCommitMinimumSeverity = null`, `BlockCommitOnCritical = true` → blocks on Critical only (backward compatibility).
     - Test: `WarnOnlySeverities` includes current threshold → `WarnOnly = true`, `Blocked = false`.
   - `ApprovalSlaMonitorTests`:
     - Test: pending request past SLA deadline → audit event emitted + webhook POST attempted.
     - Test: pending request before SLA deadline → no action.
     - Test: already-notified request → no repeat notification.
     - Test: no webhook URL configured → audit event emitted, no HTTP call.
   - `GovernanceWorkflowServiceTests` — add:
     - Test: `ApprovalSlaHours` configured → `SlaDeadlineUtc` set on created approval request.
     - Test: `ApprovalSlaHours` not configured → `SlaDeadlineUtc` null.

8. **Documentation:**
   - Update `docs/PRE_COMMIT_GOVERNANCE_GATE.md`:
     - Add "Configurable severity thresholds" section explaining `BlockCommitMinimumSeverity` and backward compatibility.
     - Add "Warning-only mode" section explaining `WarnOnlySeverities`.
     - Add "Approval SLA" section explaining the SLA hours config, deadline tracking, breach detection, and webhook escalation.
   - Update `docs/AUDIT_COVERAGE_MATRIX.md` with new event types: `GovernancePreCommitWarned`, `GovernanceApprovalSlaBreached`. Update the Core constant count.
   - Update `docs/API_CONTRACTS.md`:
     - Note that commit response may include `governanceWarnings` when `WarnOnly` is true.
   - Add a `CHANGELOG.md` entry for the configurable gate and SLA features.

**Constraints:**
- Do NOT break backward compatibility — when `BlockCommitMinimumSeverity` is null and `BlockCommitOnCritical` is true, behavior must be identical to current behavior.
- Do NOT change the existing `PreCommitGateEnabled` config gate — when false, no evaluation occurs (zero cost).
- The SLA monitor must be idempotent — re-running it must not send duplicate notifications (check `SlaBreachNotifiedUtc`).
- The webhook POST must not block governance operations — it should be fire-and-forget with try/catch and warning log on failure.
- Do NOT modify historical SQL migration files.
- Follow existing code style: concrete types over var, null checks, one blank line before if/foreach, each class in its own file, LINQ over foreach.
- Use HMAC-SHA256 for webhook signing (follow the existing pattern in the codebase if one exists; otherwise use `System.Security.Cryptography.HMACSHA256`).
```

---

## Usage notes

- Each prompt is **self-contained** — paste one at a time into a Cursor Agent session.
- Prompts reference **specific files, types, and conventions** from the ArchLucid codebase as of 2026-04-14 so the agent has maximum context.
- Each prompt includes constraints to prevent common pitfalls (breaking existing tests, removing existing code, changing unrelated code).
- The prompts are ordered by priority from the quality assessment (highest weighted impact first).
- **Prompt #5 (Terraform `state mv` + backlog triage) is excluded** — it requires external coordination and manual review that is not well-suited to an automated agent session.
