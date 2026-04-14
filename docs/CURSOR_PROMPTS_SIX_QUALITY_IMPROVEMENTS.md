# Cursor prompts — six quality improvements (weighted assessment)

**Purpose:** Six **paste-ready** prompts for Cursor Agent (or Plan) sessions. They map to the highest-impact follow-ups from the weighted quality review (correctness, performance, explainability, auditability, evolvability, testability, security).

**Related docs:**

- `docs/QUALITY_ASSESSMENT.md`, `docs/QUALITY_ASSESSMENT_2026_04.md` — scoring context.
- `docs/QUALITY_IMPROVEMENT_PROMPTS.md` — older prompts (some items since implemented).
- `docs/QUALITY_IMPROVEMENT_PROMPTS_2026_04_14.md` — overlapping themes (live E2E expansion, forensics, audit, etc.). **Use this file** when you want the *six* improvements below; use the 2026-04-14 file for the alternate prompt set.

**How to use:** Paste **one** fenced block into a new Agent chat. Keep the build green; follow `.cursor/rules/ArchLucid-Rename.mdc` and **never edit historical SQL migrations 001–028** — only `ArchLucid.sql` + **new** migrations if schema changes are required.

---

## Prompt 1 — k6 write-path smoke: merge-blocking + p95 guard

```
You are working in the ArchLucid repo (ArchLucid.sln, Azure-first .NET 10 API).

**Goal:** Make the CI job `k6-ci-smoke` merge-blocking and add an explicit latency regression check on the k6 summary JSON, aligned with `docs/LOAD_TEST_BASELINE.md` and `docs/TEST_STRUCTURE.md` §k6.

**Current state (verify in tree):**
- `.github/workflows/ci.yml` — job `k6-ci-smoke` has `continue-on-error: true` and a TODO to make blocking after stable greens (target 2026-05-01).
- `tests/load/ci-smoke.js` — read + write scenarios against `BASE_URL`.
- `tests/load/smoke.js` — read-only sibling used by `k6-smoke-api` (already blocking).

**What to do:**

1. **Workflow:** Remove `continue-on-error: true` from `k6-ci-smoke` (or set `false`) once you are confident flakiness is acceptable; update the header comment in `ci.yml` Tier 2c section to state the job is merge-blocking and remove stale “non-blocking” wording.
2. **Thresholds:** After `docker run … k6 … ci-smoke.js --summary-export`, parse `k6-ci-summary.json` (same pattern as `scripts/ci/print_k6_summary_metrics.py` if useful) and **fail the step** if:
   - `http_req_failed` rate exceeds a documented cap (e.g. 0% for this synthetic job, or match existing `hotpaths.js` check rate ~0.85 philosophy — justify in commit message), and
   - `http_req_duration` **p(95)** exceeds a ceiling derived from `docs/LOAD_TEST_BASELINE.md` (e.g. 1500 ms initial guard, or the doc’s p95 × slack factor — document the number in the workflow comment and in `LOAD_TEST_BASELINE.md` “CI gate” subsection).
3. **Docs:** Update `docs/TEST_STRUCTURE.md` (k6 table), `docs/LOAD_TEST_BASELINE.md`, and if present `docs/PERFORMANCE.md` with the new gate semantics and how to tune thresholds after infra changes.
4. **Flake mitigation:** If the job becomes flaky, prefer tightening waits (health ready), reducing VUs, or widening p95 slightly with a ratchet file — do not silently re-enable `continue-on-error` without a dated comment and issue link.

**Constraints:**
- Do not point k6 at production URLs.
- Keep SQL + API env vars consistent with existing `k6-ci-smoke` job (Simulator mode, DevelopmentBypass, elevated rate limit only for that job).
- No `ConfigureAwait(false)` in tests if you add any.

**Done when:** PR shows green `k6-ci-smoke` on a clean run; documentation states the exact numeric gate; workflow comment matches behavior.
```

---

## Prompt 2 — Required model metadata on traces + golden-set agent evaluation

```
You are working in the ArchLucid repo.

**Goal:** Strengthen AI/agent forensics and regression detection by (a) making deployment/model identity **required** on persisted agent execution traces when real LLM calls occur, and (b) adding a **golden-set** (fixture-based) evaluation harness that scores persisted `ParsedResultJson` against expected shapes or expected findings — without calling an LLM in CI.

**Context (read first):**
- `docs/AGENT_TRACE_FORENSICS.md` — blob layout, `ModelDeploymentName` / `ModelVersion` on traces.
- `ArchLucid.Contracts` — `AgentExecutionTrace` (or equivalent DTO) and related options.
- `ArchLucid.AgentRuntime` — trace recorder, completion clients, `AgentOutputEvaluator` / `AgentOutputSemanticEvaluator`, `AgentOutputEvaluationRecorder`.
- `docs/AGENT_OUTPUT_EVALUATION.md` — structural + semantic scoring today.

**What to do:**

1. **Schema / contracts:** For traces produced by **real** execution paths (not the deterministic simulator), ensure `ModelDeploymentName` and `ModelVersion` are populated at write time. If either can legitimately be unknown, define a narrow sentinel documented in XML remarks — avoid silent nulls for production “Real” mode.
2. **Persistence:** If the DB columns are nullable today, prefer **application-level validation** on insert for Real mode rather than breaking existing rows; use a new migration only if you add non-null constraints with a backfill strategy (master DDL `ArchLucid.Persistence/Scripts/ArchLucid.sql` + **new** migration only — never rewrite migrations 001–028).
3. **Golden-set harness:** Add tests under `ArchLucid.AgentRuntime.Tests` or `ArchLucid.Decisioning.Tests` that load JSON fixtures from `ArchLucid.AgentRuntime.Tests/Fixtures/…` (or similar), run the existing evaluator(s), and assert score floors / key presence. Include at least one “regression” fixture that would fail if someone strips `evidenceRefs` from claims.
4. **Docs:** Update `docs/AGENT_TRACE_FORENSICS.md` and `docs/EXPLAINABILITY_TRACE_COVERAGE.md` if behavior changes; add a short “Golden-set evaluation” subsection to `docs/AGENT_OUTPUT_EVALUATION.md`.

**Constraints:**
- Do not add new external SaaS dependencies; stay on existing Azure/OpenAI abstractions.
- Keep evaluator logic deterministic for CI.

**Done when:** Real-mode trace writes always carry model identity (or documented sentinel); new tests fail on intentional fixture corruption; CI green.
```

---

## Prompt 3 — Audit coverage gaps + operator audit search UX

```
You are working in the ArchLucid repo.

**Goal:** Close **durable** `dbo.AuditEvents` coverage gaps for high-risk mutating flows called out in `docs/AUDIT_COVERAGE_MATRIX.md` / `docs/V1_DEFERRED.md`, and improve the operator UI so `/audit` uses **`GET /v1/audit/search`** capabilities (filters, keyset cursor) where the API already supports them.

**Context (read first):**
- `docs/AUDIT_COVERAGE_MATRIX.md` — “Known gaps” / export and governance rows.
- `ArchLucid.Core/Audit/AuditEventTypes.cs` — event catalog; CI anchor in the matrix HTML comment.
- `ArchLucid.Api` — audit controller(s), validators, ProblemDetails.
- `archlucid-ui/src/app/audit/` — current audit page and components.
- `docs/API_CONTRACTS.md` — audit list vs search semantics, caps, keyset.

**What to do:**

1. **Backend:** For each gap row you tackle (prioritize: analysis reports, DOCX/export-adjacent mutations, extra governance routes if listed), add `IAuditService.LogAsync` calls with correct scope fields (`RunId`, `ManifestId`, etc.) and **new** `AuditEventTypes` constants if needed — update the matrix table + CI anchor comment count when constants change.
2. **Consistency:** Ensure dual-write paths (baseline log vs durable) remain documented; do not break “audit failure must not fail the main flow” invariants where they exist today.
3. **UI:** Extend the audit page to expose search filters (event type, correlation id, run id, date range) matching the API; preserve accessibility (labels, `aria-live` for errors); reuse existing API client patterns from `archlucid-ui`.
4. **Tests:** Add or extend `ArchLucid.Api.Tests` integration tests proving new audit rows; add focused UI tests (Vitest) or Playwright smoke for the new controls if appropriate.
5. **Docs:** Update `AUDIT_COVERAGE_MATRIX.md` and `docs/operator-shell.md` if operator steps change.

**Constraints:**
- Never modify historical migration files 001–028; append new migrations + `ArchLucid.sql` if schema changes (unlikely for audit logging only).

**Done when:** Matrix “known gaps” shrink with explicit rows; UI can perform a representative filtered search against real or mocked API contract; tests cover new behavior.
```

---

## Prompt 4 — NEXT_REFACTORINGS triage + Phase 7.5 Terraform plan

```
You are working in the ArchLucid repo.

**Goal:** Reduce contributor cognitive load by (1) **triageing** `docs/NEXT_REFACTORINGS.md` into a short active backlog plus a dated archive, target **≤ ~500 lines** in the main file while preserving history; and (2) producing an **executable Phase 7.5 plan** for Terraform resource address rename / `state mv` without executing destructive operations unless the user explicitly runs Terraform.

**Context (read first):**
- `docs/NEXT_REFACTORINGS.md` — maintainer backlog; many items done or obsolete.
- `docs/ARCHLUCID_RENAME_CHECKLIST.md` — Phase 7.5–7.8 deferred items.
- `docs/RENAME_DEFERRED_RATIONALE.md` — why deferral happened.
- `infra/README.md` — Terraform roots.

**What to do:**

1. **Triage:** Move completed batches into `docs/archive/` (new or existing archive doc) with a pointer from `NEXT_REFACTORINGS.md`. For each open item: keep, defer-with-reason + date, or delete-with “obsolete” note. Update the backlog summary table at the top.
2. **Contributor experience:** Ensure `docs/START_HERE.md` decision tree still points to the right “where to add X” guidance.
3. **Phase 7.5 plan:** Add a new doc `docs/runbooks/TERRAFORM_STATE_MV_PHASE_7_5.md` (or extend `RENAME_DEFERRED_RATIONALE.md` with a runbook section) listing:
   - Which roots still use legacy resource **addresses** (e.g. `archiforge` literals in `.tf` files — grep `infra/`).
   - Ordered `terraform state mv` commands (source → target) **as templates** with placeholders for workspace/backend.
   - Preconditions: backup state, freeze deploys, verify `terraform plan` is clean after each batch.
   - Rollback narrative.
4. **Checklist:** Update `ARCHLUCID_RENAME_CHECKLIST.md` Phase 7.5 with a link to the runbook and a checkbox note “runbook added — execution pending program approval”.

**Constraints:**
- Do **not** run `terraform apply` or mutate remote state in automation from this task unless the user explicitly asks.
- One batch per session rule: this prompt is documentation + triage only unless the user expands scope.

**Done when:** `NEXT_REFACTORINGS.md` is navigable; archive holds historical detail; Phase 7.5 runbook exists and checklist cross-links it.
```

---

## Prompt 5 — Coverage ratchet + property-based tests for pure domain logic

```
You are working in the ArchLucid repo.

**Goal:** Tighten test quality signals by (1) **raising or hardening** coverage gates so weak assemblies cannot hide behind the merged average, and (2) adding **property-based** tests (FsCheck or FsCheck.Xunit for C#) for pure domain functions where random input finds edge cases faster than example-only tests.

**Context (read first):**
- `docs/CODE_COVERAGE.md` — merged line ≥71%, branch ≥50%, per-package floors via `scripts/ci/assert_merged_line_coverage_min.py`.
- `.github/workflows/ci.yml` — invocation of `assert_merged_line_coverage_min.py` with `--min-package-line-pct` (verify current values).
- `docs/TEST_STRUCTURE.md`, `docs/TEST_EXECUTION_MODEL.md`.
- Strong pure-logic targets: `ArchLucid.Decisioning` (governance resolution, manifest merge invariants), `ArchLucid.Application` hashing/idempotency helpers — pick **one** bounded area first.

**What to do:**

1. **Coverage:** Run a local merged Cobertura report (`docs/CODE_COVERAGE.md` commands), identify the **lowest** product packages above the floor. Either raise `--min-package-line-pct` by 2–5 points with tests to support it, or add a **second-tier** advisory threshold already partially implemented via `warn_below_package_line_pct` — document the policy in `CODE_COVERAGE.md`.
2. **Property tests:** Add `FsCheck` (or agreed package from `Directory.Packages.props`) to the **one** test project you touch. Write 2–4 properties with shrinking enabled, each < ~30 lines of logic, covering invariants (e.g. idempotent merge, commutative ordering where applicable, never throws on valid random inputs within model constraints).
3. **CI:** Ensure new tests run in `Suite=Core` or default regression as appropriate; keep runtime reasonable (< few seconds).
4. **Docs:** Add a short “Property-based tests” subsection to `docs/TEST_STRUCTURE.md`.

**Constraints:**
- No `ConfigureAwait(false)` in tests.
- Prefer concrete types over `var` per repo conventions where applicable.
- Do not add property tests to integration-heavy classes tied to SQL or HTTP.

**Done when:** CI coverage script args + docs match; FsCheck tests pass locally and in CI; at least one property documents the invariant in the test name or comment.
```

---

## Prompt 6 — RLS risk acceptance (concrete) + API key rotation runbook

```
You are working in the ArchLucid repo.

**Goal:** Turn residual RLS and API-key operational risks into **actionable governance artifacts**: populate `docs/security/RLS_RISK_ACCEPTANCE.md` with **concrete** uncovered tables / views (or explicit “none” with evidence), and add or refresh `docs/runbooks/API_KEY_ROTATION.md` (or equivalent) so operators can rotate keys with **zero downtime**, aligned with actual host behavior.

**Context (read first):**
- `docs/security/MULTI_TENANT_RLS.md` — especially §9 child tables / uncovered surfaces.
- `docs/security/RLS_RISK_ACCEPTANCE.md` — template sections to fill.
- `ArchLucid.Api/Authentication/ApiKeyAuthenticationHandler.cs` — already documents `IOptionsMonitor<ApiKeyAuthenticationOptions>` for reload without restart; **verify** DI registration uses options binding that supports reload (e.g. JSON file + optional Key Vault refresh).
- `ArchLucid.Api.Tests/ApiKeyAuthenticationHandlerTests.cs` — comma-separated overlapping keys / monitor tests.
- `docs/runbooks/API_KEY_ROTATION.md` — extend the existing runbook (comma-separated overlap, cutover).
- `README.md` / `SECURITY.md` — link to the runbook.

**What to do:**

1. **RLS:** Grep migrations / `ArchLucid.sql` / repository SQL for tables with tenant scope columns **not** listed in RLS policy creation. Produce a table in `RLS_RISK_ACCEPTANCE.md`: table name, why uncovered, app-layer control, risk rating, owner sign-off placeholder.
2. **API keys:** Document step-by-step rotation using **two overlapping keys** (comma-separated in config) if supported; include Kubernetes/Container Apps secret refresh sequence and how long to wait before removing the old key. Explicitly state that handler reads `IOptionsMonitor.CurrentValue` per request — if any code path caches keys incorrectly, fix it and add a test.
3. **Cross-links:** From `SYSTEM_THREAT_MODEL.md` or `SECURITY.md`, link to the updated RLS acceptance and API key runbook.

**Constraints:**
- Do not weaken Production startup validation rules.
- Never expose SMB/445 publicly (existing security rule).

**Done when:** RLS doc is no longer a hollow template for table coverage; API key runbook matches code; links from top-level security docs are updated.
```

---

## Suggested order of execution

| Order | Prompt | Why |
|------|--------|-----|
| 1 | #4 (NEXT_REFACTORINGS + Phase 7.5 plan) | Unblocks navigation; no runtime risk. |
| 2 | #6 (RLS + API key runbooks) | Fast governance win; clarifies security posture. |
| 3 | #1 (k6 blocking) | Validates CI cost/flake trade-off deliberately. |
| 4 | #5 (coverage + FsCheck) | Strengthens regression signal. |
| 5 | #3 (audit + UI) | Cross-cutting product + compliance. |
| 6 | #2 (model metadata + golden-set) | Touches schema/contracts; deserves focused review. |

You can parallelize #4 + #6 if two agents are available; the table order is safe for a single contributor.
