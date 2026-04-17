# Cursor prompts for top-6 quality improvements

Six standalone prompts, one per improvement from the archived assessment [`docs/archive/QUALITY_ASSESSMENT.md`](archive/QUALITY_ASSESSMENT.md) (stub: [`docs/QUALITY_ASSESSMENT.md`](QUALITY_ASSESSMENT.md)). Each is ready to paste into a Cursor Agent session.

---

## Prompt 1 — Live-API E2E test in CI

```
Add a Playwright E2E test that runs against the real C# API with SQL — not mock-backed — covering the core operator happy path. Here is the scope:

**What to build:**

1. A new Playwright spec file `archlucid-ui/e2e/live-api-journey.spec.ts` that exercises:
   - Navigate to the Runs page, verify the list loads (even if empty).
   - Create a run via the UI "New run" flow (or POST to `/v1/architecture/request` directly and navigate to its detail page).
   - Verify the run detail page renders with status.
   - Navigate to the Audit page and confirm at least one `RunStarted` audit event appears.
   - Navigate to the Governance dashboard and confirm it loads without error.

2. A new CI job in `.github/workflows/ci.yml` called `ui-e2e-live` that:
   - Depends on `dotnet-full-regression` (Tier 2).
   - Starts a SQL Server service container (same pattern as the existing `dotnet-full-regression` job).
   - Runs `dotnet run --project ArchLucid.Api` with `ArchLucid:StorageProvider=Sql` and `ArchLucidAuth:Mode=DevelopmentBypass`, backgrounded.
   - Waits for `/health/ready` to return 200.
   - Runs `npm ci && npx playwright install --with-deps chromium` in `archlucid-ui/`.
   - Sets `NEXT_PUBLIC_API_BASE_URL` / `ARCHLUCID_API_BASE_URL` to the running API.
   - Runs `npx playwright test e2e/live-api-journey.spec.ts`.
   - Uploads Playwright report as an artifact.

**Constraints:**
- Do NOT modify existing mock-backed Playwright specs.
- Use the existing `DevelopmentBypass` auth mode so no API key setup is needed.
- The test should be tagged so it can be filtered separately: add `test.describe('live-api-journey', ...)`.
- Use `test.setTimeout(120_000)` because the API startup + first run may be slow.
- If the API is not reachable, the test should fail with a clear message, not hang.
- Keep the CI job non-blocking initially (`continue-on-error: true`) so it does not gate merges until the test is proven stable. Add a comment noting it should be made blocking after 2 weeks of green runs.
- Follow the existing Playwright config patterns in `archlucid-ui/playwright.config.ts`.
- Update `docs/TEST_STRUCTURE.md` with a new row for "Live API E2E" explaining the tier, filter, and purpose.
```

---

## Prompt 2 — Persist exact prompt text + model response per agent execution

```
Enhance AgentExecutionTraceRecorder to store full (unsanitized) prompt and response text alongside the existing truncated fields, and add a structured output evaluation harness. Here is the scope:

**Context:**
- `ArchLucid.AgentRuntime/AgentExecutionTraceRecorder.cs` already records traces via `IAgentExecutionTraceRepository.CreateAsync`. It truncates `SystemPrompt`, `UserPrompt`, and `RawResponse` to 8192 chars.
- `AgentExecutionTrace` in `ArchLucid.Contracts/Agents/AgentExecutionTrace.cs` carries prompt template id, version, SHA-256, and release label — but NOT the full prompt text.
- The quality assessment identified this as the #2 improvement: "Persist exact prompt text + model response per agent execution for forensic replay and quality regression detection."

**What to build:**

1. **New fields on `AgentExecutionTrace`** (in `ArchLucid.Contracts`):
   - `FullSystemPromptBlobKey` (string?, nullable) — blob storage key for the full system prompt.
   - `FullUserPromptBlobKey` (string?, nullable) — blob storage key for the full user prompt.
   - `FullResponseBlobKey` (string?, nullable) — blob storage key for the full response.
   - `ModelDeploymentName` (string?, nullable) — the Azure OpenAI deployment name used.
   - `ModelVersion` (string?, nullable) — model version string if available.

2. **Blob storage for full prompt/response** (config-gated):
   - Add a config gate `AgentExecution:TraceStorage:PersistFullPrompts` (default `false`).
   - When enabled, `AgentExecutionTraceRecorder.RecordAsync` uploads full system prompt, user prompt, and raw response to blob storage via `IArtifactBlobStore` (already exists in the codebase) under a path like `agent-traces/{runId}/{traceId}/system-prompt.txt`, `.../user-prompt.txt`, `.../response.txt`.
   - Store the blob keys on the trace record. The existing truncated fields remain for quick inspection without blob reads.

3. **New migration `052_AgentExecutionTrace_FullPromptBlobKeys.sql`**:
   - Add nullable `FullSystemPromptBlobKey`, `FullUserPromptBlobKey`, `FullResponseBlobKey`, `ModelDeploymentName`, `ModelVersion` columns to the agent execution trace table.
   - Update `ArchLucid.Persistence/Scripts/ArchLucid.sql` with the same columns.

4. **Update the Dapper repositories** (`AgentExecutionTraceRepository` and `InMemoryAgentExecutionTraceRepository`) to read/write the new fields.

5. **Tests:**
   - `AgentExecutionTraceRecorderReproTests` — add a test that verifies when `PersistFullPrompts=true`, full text is uploaded to blob and blob keys are set on the trace.
   - Add a test that verifies when `PersistFullPrompts=false`, blob keys remain null and no blob writes occur.
   - Add contract tests for the new columns in both Dapper and InMemory repos.

6. **Documentation:**
   - Update `docs/OBSERVABILITY.md` to note the new config gate.
   - Create `docs/AGENT_TRACE_FORENSICS.md` explaining the full prompt storage model, blob path convention, how to retrieve a prompt for forensic replay, and privacy/retention considerations (prompts may contain PII; link to audit retention policy for blob lifecycle).

**Constraints:**
- Do NOT remove the existing truncation — keep the truncated fields for lightweight reads.
- Do NOT change the `MaxContentLength` constant.
- The blob upload must be fire-and-forget (do not block the agent execution pipeline on blob I/O). If the upload fails, log a warning and leave the blob key fields null.
- Follow the existing migration numbering convention (next is 052).
- Follow existing code style: concrete types over var, null checks, one blank line before if/foreach, each class in its own file.
```

---

## Prompt 3 — Load-test baseline with k6

```
Add a k6 load-test smoke suite and CI job that establishes a performance regression baseline for the core operator API path. Here is the scope:

**What to build:**

1. **k6 test script** at `tests/load/smoke.js`:
   - Target the ArchLucid API at a configurable `BASE_URL` (environment variable, default `http://localhost:5128`).
   - Scenarios (all use DevelopmentBypass auth, no API key needed):
     a. `health` — GET `/health/live` and `/health/ready` (ramp to 10 VUs, 30s duration).
     b. `runs_list` — GET `/v1/authority/projects/00000000-0000-0000-0000-000000000001/runs?page=1&pageSize=20` (5 VUs, 30s).
     c. `version` — GET `/version` (5 VUs, 30s).
     d. `audit_search` — GET `/v1/audit/search?maxResults=50` (3 VUs, 30s).
   - Thresholds:
     - `http_req_duration` p95 < 500ms for health and version.
     - `http_req_duration` p95 < 2000ms for runs_list and audit_search.
     - `http_req_failed` < 1% overall.
   - Add `X-Correlation-ID: k6-smoke-{scenario}-{__VU}-{__ITER}` header on every request.
   - Use `k6/http` and `check()` for status assertions.
   - Export results as JSON (`--out json=results.json`) for CI artifact upload.

2. **CI job** in `.github/workflows/ci.yml`:
   - Job name: `Performance: k6 smoke (API baseline)`.
   - Depends on `dotnet-full-regression` (SQL container available).
   - Steps:
     a. Start the API (same background pattern as the live E2E prompt — `dotnet run --project ArchLucid.Api` with Sql storage, DevelopmentBypass auth).
     b. Wait for `/health/ready`.
     c. Install k6: `sudo gpg -k && sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys ...` or use the grafana/k6 Docker image.
     d. Run `k6 run tests/load/smoke.js --out json=/tmp/k6-results.json`.
     e. Upload `/tmp/k6-results.json` as artifact `k6-smoke-results`.
   - Mark `continue-on-error: true` initially with a comment to make blocking after baseline is established.

3. **Documentation:**
   - Create `docs/PERFORMANCE_TESTING.md` explaining:
     - How to run the k6 smoke locally (`k6 run tests/load/smoke.js`).
     - What the thresholds mean and how to adjust them.
     - How to interpret the JSON results.
     - How this relates to `docs/PERFORMANCE.md` (caching) and `docs/API_SLOS.md` (SLOs).
   - Add a row to the CI tier table in `docs/TEST_EXECUTION_MODEL.md`.

**Constraints:**
- Do NOT add k6 to the Node.js package.json — it is a standalone Go binary.
- The test must be idempotent and read-only (no POST requests that mutate state in the smoke suite).
- Keep the total test duration under 2 minutes so CI stays fast.
- Use `scenarios` (not simple `options.vus/duration`) so each endpoint group has its own thresholds.
- Follow the existing CI job naming convention (e.g., "Performance: k6 smoke (API baseline)").
```

---

## Prompt 4 — Close audit coverage gaps + audit search UI

```
Close the remaining audit coverage gap for comparison/export persistence and enhance the operator UI audit page with full search/filter capabilities. Here is the scope:

**Context:**
- `docs/AUDIT_COVERAGE_MATRIX.md` notes one remaining gap: `ExportsController` `POST .../exports/compare/summary` with `persist: true` records via `IComparisonAuditService` only (comparison audit tables) and does NOT emit a Core `ReplayExportRecorded` row to `dbo.AuditEvents`.
- The operator UI `/audit` page (`archlucid-ui/src/app/audit/page.tsx`) already has `eventType`, `fromUtc`, `toUtc`, `correlationId`, `actorUserId`, and `runId` filter fields, plus a search function calling `searchAuditEvents`. However, it lacks: date range picker UX, export-to-CSV button, pagination, and clear filter reset.

**What to build:**

1. **Close the comparison-persist audit gap:**
   - In `ExportsController` (or the service it delegates to), when `persist: true` on the comparison summary POST, also call `IAuditService.LogAsync` with event type `AuditEventTypes.ReplayExportRecorded` (or a new `ComparisonSummaryExportRecorded` constant if semantically clearer).
   - Add the new constant to `ArchLucid.Core/Audit/AuditEventTypes.cs` if needed.
   - Update the `<!-- audit-core-const-count:66 -->` marker in `docs/AUDIT_COVERAGE_MATRIX.md` to reflect the new count.
   - Add a row to the "Operations → durable audit" table in the matrix doc.
   - Remove the "Note" about this gap from the "Known gaps" section.
   - Add a unit test in `ArchLucid.Api.Tests` that verifies the audit event is emitted when persist is true, and NOT emitted when persist is false.

2. **Enhance the audit UI page** (`archlucid-ui/src/app/audit/page.tsx`):
   - Add a "Clear filters" button that resets all filter fields and re-runs the search.
   - Add a "Export CSV" button that calls `GET /v1/audit/export` with the current filter's `fromUtc`/`toUtc` range and `Accept: text/csv`, then triggers a browser download of the response. If `fromUtc`/`toUtc` are not set, show a tooltip "Set a date range to enable export" and disable the button.
   - Add pagination: use the existing `AUDIT_PAGE_SIZE` (200) as the page size; show "Load more" at the bottom when `hasMoreResults` is true (this already exists — verify it works correctly).
   - Add a summary line above the results table: "Showing {count} events" (or "Showing {count}+ events" when hasMore is true).
   - Add Vitest tests for the new UI behaviors (clear filters, export button disabled state, summary line text).

3. **Documentation:**
   - Update `docs/AUDIT_COVERAGE_MATRIX.md` with the closed gap and new count.
   - Update `docs/operator-shell.md` with a short note about the enhanced audit page capabilities.

**Constraints:**
- Do NOT change the existing `searchAuditEvents` API function signature — add new functions if needed.
- The CSV export should use the browser's native download mechanism (create an anchor element with blob URL).
- Follow the existing UI patterns: `OperatorApiProblem` for errors, `OperatorLoadingNotice` for loading states.
- Keep the audit page as a single file (no extraction to components) unless it exceeds 400 lines after changes.
- The audit event for comparison persist must include `DataJson` with at least `{ sourceExportRecordId, comparisonId }`.
```

---

## Prompt 5 — Triage NEXT_REFACTORINGS.md + complete Phase 7.5 Terraform state mv

```
Triage the NEXT_REFACTORINGS.md backlog and prepare the Phase 7.5 Terraform state mv. Here is the scope:

**Part A — Triage NEXT_REFACTORINGS.md:**

The file `docs/NEXT_REFACTORINGS.md` is 2,100+ lines with ~90 numbered items. Many are completed, obsolete, or lack actionable next steps. This makes the backlog intimidating and hard to prioritize.

1. Read the entire file.
2. For each item (numbered sections 8+), classify it as one of:
   - **Done** — already implemented based on current codebase evidence. Move to the archive section at the bottom with a "(completed)" note and the date.
   - **Active** — still relevant, actionable, and not yet done. Keep in place.
   - **Deferred** — relevant but not V1 priority. Add a one-line "(deferred: reason)" note.
   - **Obsolete** — no longer relevant due to architecture changes. Move to archive with "(obsolete: reason)".
3. After classification, add a summary section at the top of the file:
   ```
   ## Backlog summary (as of YYYY-MM-DD)
   | Status | Count |
   |--------|-------|
   | Active | N |
   | Deferred | N |
   | Done (archived) | N |
   | Obsolete (archived) | N |
   ```
4. Reorder the Active items by rough priority (correctness/security first, then architecture, then polish).

**Part B — Phase 7.5 Terraform state mv preparation:**

The rename checklist (`docs/ARCHLUCID_RENAME_CHECKLIST.md`) item 7.5 says: "Terraform `state mv` operations for renamed resources (coordinate with deploy window) — deferred."

1. Create `docs/runbooks/TERRAFORM_STATE_MV_PHASE_7_5.md` with:
   - A complete inventory of Terraform resource addresses that still use `archiforge` in their address (search all `.tf` files under `infra/` for resource blocks with `archiforge` in the resource name).
   - For each resource, document the exact `terraform state mv` command needed (old address → new address).
   - Group by Terraform root (e.g., `infra/terraform-container-apps/`, `infra/terraform/`, etc.).
   - Include pre-flight checks (backup state, plan with no changes expected after mv).
   - Include a rollback section (state mv back if something goes wrong).
   - Include a validation section (terraform plan should show no changes after all moves).
   - Note that this requires a maintenance window and should be coordinated with the deploy team.

2. Update `docs/ARCHLUCID_RENAME_CHECKLIST.md` item 7.5:
   - Change from "deferred" to "deferred — runbook ready: see `docs/runbooks/TERRAFORM_STATE_MV_PHASE_7_5.md`"
   - Do NOT mark it as complete — the actual state mv has not been executed.

**Constraints:**
- Do NOT execute any terraform commands — this is documentation/preparation only.
- Do NOT modify any .tf files — the resource addresses in Terraform code stay as-is until state mv is executed.
- Do NOT modify historical SQL migration files (001–028 or any others).
- When classifying NEXT_REFACTORINGS items as "Done", verify by checking the codebase (grep for the type/method/file mentioned). Do not guess.
- Keep the build green — no code changes, only documentation.
```

---

## Prompt 6 — Optional pre-commit governance gate

```
Add an optional pre-commit governance gate that blocks `POST /v1/architecture/run/{runId}/commit` when critical policy violations exist. Here is the scope:

**Context:**
- Today, governance is post-hoc: the authority pipeline produces findings (including compliance violations), but commit proceeds regardless. The quality assessment identified this as improvement #6: "An optional pre-commit gate gives governance teams a preventive control."
- `ArchitectureRunCommitOrchestrator` in `ArchLucid.Application/Runs/Orchestration/` handles the commit flow.
- `GovernanceWorkflowService` in `ArchLucid.Application/Governance/` handles approval/promotion workflows.
- `FindingsSnapshot` contains all findings with severity levels (Info, Warning, Error, Critical).
- Policy packs are assigned per project and resolved at run time via effective governance merge.

**What to build:**

1. **New interface** `IPreCommitGovernanceGate` in `ArchLucid.Contracts`:
   ```csharp
   public interface IPreCommitGovernanceGate
   {
       Task<PreCommitGateResult> EvaluateAsync(string runId, CancellationToken cancellationToken = default);
   }
   ```

2. **`PreCommitGateResult`** in `ArchLucid.Contracts`:
   - `bool Blocked` — true if commit should be prevented.
   - `string? Reason` — human-readable explanation (e.g., "3 Critical findings from ComplianceFindingEngine block commit per policy pack 'SOC2-baseline'").
   - `IReadOnlyList<string> BlockingFindingIds` — ids of findings that caused the block.
   - `string? PolicyPackId` — the policy pack that enforced the gate, if applicable.

3. **Implementation** `PreCommitGovernanceGate` in `ArchLucid.Application/Governance/`:
   - Load the findings snapshot for the run.
   - Load the effective policy pack assignments for the run's project scope.
   - Check if any assigned policy pack has `BlockCommitOnCritical=true` (new bool property on policy pack or policy pack assignment — add to the model and DDL).
   - If yes and the findings snapshot contains any `Critical` severity findings, return `Blocked=true` with the details.
   - If no policy packs enforce the gate, or no critical findings exist, return `Blocked=false`.

4. **Wire into the commit orchestrator:**
   - In `ArchitectureRunCommitOrchestrator`, before the existing commit logic, call `IPreCommitGovernanceGate.EvaluateAsync`.
   - If `Blocked`, return a `409 Conflict` with problem details type `#governance-pre-commit-blocked`, including the reason and blocking finding ids.
   - Add an audit event: `AuditEventTypes.GovernancePreCommitBlocked` with DataJson containing the gate result.

5. **Config gate:** `ArchLucid:Governance:PreCommitGateEnabled` (default `false`). When false, the gate is not called at all (zero overhead for teams that don't want it).

6. **New migration** `053_PolicyPackAssignment_BlockCommitOnCritical.sql`:
   - Add `BlockCommitOnCritical BIT NOT NULL DEFAULT 0` to the policy pack assignment table.
   - Update `ArchLucid.Persistence/Scripts/ArchLucid.sql`.

7. **Tests:**
   - `PreCommitGovernanceGateTests` — verify blocked when critical findings + policy pack enforces; not blocked when no critical findings; not blocked when policy pack does not enforce; not blocked when config gate is disabled.
   - `ArchitectureRunCommitOrchestratorTests` — verify 409 response when gate blocks; verify commit proceeds when gate allows; verify gate is not called when config is disabled.
   - All tests tagged `[Trait("Suite", "Core")]`.

8. **Documentation:**
   - Create `docs/PRE_COMMIT_GOVERNANCE_GATE.md` explaining the feature, config, policy pack setup, finding severity mapping, and trade-offs (blocks pipeline velocity vs prevents non-compliant commits).
   - Update `docs/API_CONTRACTS.md` with the new 409 problem type `#governance-pre-commit-blocked`.
   - Update `docs/AUDIT_COVERAGE_MATRIX.md` with the new audit event type.
   - Update `docs/V1_SCOPE.md` section 2.10 (optional features) to mention the pre-commit gate.
   - Add a row to the `CHANGELOG.md`.

**Constraints:**
- The gate must be zero-cost when disabled (no findings load, no policy pack resolution).
- The gate must NOT modify any data — it is a read-only check.
- Use the existing `IScopeContextProvider` for tenant/workspace/project scoping.
- Follow existing patterns: concrete types over var, null checks, one blank line before if/foreach, each class in its own file, LINQ over foreach.
- Do NOT change existing commit behavior when the gate is disabled — existing tests must pass unchanged.
- The 409 response must include `correlationId` in problem details (same pattern as other problem responses).
```

---

## Usage notes

- Each prompt is **self-contained** — paste one at a time into a Cursor Agent session.
- Prompts reference specific files, patterns, and conventions from the ArchLucid codebase so the agent has maximum context.
- Each prompt includes constraints to prevent common pitfalls (breaking existing tests, changing unrelated code, etc.).
- The prompts are ordered by priority from the quality assessment (highest weighted impact first).

---

## Implementation status (as of 2026-04-17)

Prompts **2–6** below were **executed in prior work**; this table is the canonical “where it landed” map. Re-run verification after large refactors.

| Prompt | Intent (original spec) | Where it lives in the repo | Notes vs original fenced spec |
|--------|------------------------|------------------------------|--------------------------------|
| **2** | Full prompt/response blob pointers + model metadata; config-gated persistence; migration; tests; `AGENT_TRACE_FORENSICS.md` | `ArchLucid.Contracts/Agents/AgentExecutionTrace.cs`; `ArchLucid.AgentRuntime/AgentExecutionTraceRecorder.cs`; `ArchLucid.Persistence/Migrations/053_AgentExecutionTrace_FullPromptBlobKeys.sql` (+ later **`062`** inline forensic columns per `CHANGELOG.md`); `docs/AGENT_TRACE_FORENSICS.md`, `docs/OBSERVABILITY.md`; `AgentExecutionTraceRecorderReproTests` | Spec cited migration **`052`**; shipped as **`053`** (+ **`062`**). **`PersistFullPrompts`** / storage defaults evolved—trust **`appsettings*.json`** + `AgentExecutionTraceStorageOptions` + CHANGELOG over this prompt’s literal defaults. |
| **3** | Read-only k6 smoke, scenarios + thresholds, CI job, `PERFORMANCE_TESTING.md` | `tests/load/smoke.js`; `docs/PERFORMANCE_TESTING.md`; `.github/workflows/ci.yml` jobs **`k6-smoke-api`** (operator path) and **`k6-ci-smoke`** (read+write baseline) | Paths use **`/v1/architecture/runs`** and **`take=`** on audit search to match current API; job names differ from the placeholder string in the prompt. |
| **4** | Core `dbo.AuditEvents` on comparison persist + richer `/audit` UI | `ExportsController.CompareExportRecordsSummary` → **`AuditEventTypes.ComparisonSummaryPersisted`** + `DataJson`; `archlucid-ui/src/app/audit/page.tsx` + `audit-ui-helpers.ts`; `docs/AUDIT_COVERAGE_MATRIX.md` | Spec suggested **`ReplayExportRecorded`**; product uses **`ComparisonSummaryPersisted`** (clearer semantics). |
| **5** | `NEXT_REFACTORINGS` triage + Phase 7.5 runbook (docs only) | `docs/NEXT_REFACTORINGS.md` (short active table + pointer to **`docs/archive/NEXT_REFACTORINGS_ARCHIVE_2026_04_15.md`**); **`docs/runbooks/TERRAFORM_STATE_MV_PHASE_7_5.md`**; `docs/ARCHLUCID_RENAME_CHECKLIST.md` §7.5 “runbook ready” | Execution of **`terraform state mv`** / remote **`plan`** remains **operator-owned** when Azure state exists. |
| **6** | Optional pre-commit governance gate + migration + docs | `ArchLucid.Contracts/Governance/IPreCommitGovernanceGate.cs`, `PreCommitGateResult.cs`; `ArchLucid.Application/Governance/PreCommitGovernanceGate.cs`; `ArchitectureRunCommitOrchestrator`; `ArchLucid.Persistence/Migrations/054_*` (policy assignment **`BlockCommitOnCritical`**); `docs/PRE_COMMIT_GOVERNANCE_GATE.md`, `docs/API_CONTRACTS.md`; tests under **`PreCommitGovernanceGateTests`**, **`ArchitectureRunCommitPipelineIntegrationTests`**, **`ArchitectureRunServiceExecuteCommitTests`** | Spec cited migration **`053`** for policy column; shipped as **`054`** sequence. |

**Prompt 1** in this file (live-API Playwright + CI job) is **separate**; track in `docs/TEST_STRUCTURE.md` / `archlucid-ui/e2e/` if you extend it.
