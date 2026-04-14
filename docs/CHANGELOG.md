# ArchLucid changelog

Release entries newest-first. Each section condenses the detailed prompt logs preserved in `docs/archive/`.

> **Design-session logs:** The full incremental prompt records live in
> `docs/archive/CHANGE_SET_55R_SUMMARY.md` through `CHANGE_SET_59R.md`.
> Read those when you need exact delivery scope or deferred-backlog decisions.

---

## 2026-04-14 — Configurable severity thresholds + approval SLA with escalation

**Added:** Configurable **`BlockCommitMinimumSeverity`** on `PolicyPackAssignment` (SQL **`057`**) — allows blocking commits at any `FindingSeverity` level, not just Critical. When null with `BlockCommitOnCritical=true`, behavior is unchanged.

**Added:** **Warning-only mode** via `ArchLucid:Governance:WarnOnlySeverities` — severities in this list trigger `GovernancePreCommitWarned` audit event but allow commit to proceed. Enables phased enforcement rollout.

**Added:** **Approval SLA** via `ArchLucid:Governance:ApprovalSlaHours` — new approval requests receive `SlaDeadlineUtc`. **`ApprovalSlaMonitor`** detects breaches, emits `GovernanceApprovalSlaBreached` audit events, and sends HMAC-signed webhook escalation notifications. SQL **`058`** adds `SlaDeadlineUtc` and `SlaBreachNotifiedUtc` to `GovernanceApprovalRequests`.

**Tests:** `PreCommitGovernanceGateTests` — configurable severity threshold (block on Error, allow Warning-only, legacy Critical-only fallback, warn-only mode). `ApprovalSlaMonitorTests` — SLA breach audit, before-deadline skip, already-notified skip, no-webhook audit-only, SLA-not-configured skip.

**Docs:** Updated `PRE_COMMIT_GOVERNANCE_GATE.md` (severity thresholds, warning mode, approval SLA sections). Updated `AUDIT_COVERAGE_MATRIX.md` (`GovernancePreCommitWarned`, `GovernanceApprovalSlaBreached` rows; count 73→75).

---

## 2026-04-13 — Stryker enforcement tightening + pre-commit gate tests

**Tests:** **`ArchitectureRunServiceExecuteCommitTests`** — commit path throws **`PreCommitGovernanceBlockedException`** when the gate blocks; happy path when allowed; gate skipped when disabled. **`ArchitectureRunCommitPipelineIntegrationTests`** — real **`PreCommitGovernanceGate`** blocks commit without persisting manifest and emits **`GovernancePreCommitBlocked`** audit; allows commit when findings are non-critical. **`PreCommitGovernanceGateTests`** — edge cases (unparseable run id, missing snapshot id, non-enforcing assignment, disabled assignment, missing snapshot row, multiple critical ids, assignment tie-break).

**Stryker:** Raised committed baselines **`62.0` → `65.0`** in **`scripts/ci/stryker-baselines.json`** for all five matrix labels. Tightened scheduled workflow assert tolerance **`0.15` → `0.10`** pp. Documented baseline ratchet policy in **`MUTATION_TESTING_STRYKER.md`**; noted baselines in **`TEST_STRUCTURE.md`**; added Tier **4c** row in **`TEST_EXECUTION_MODEL.md`**.

---

## 2026-04-12 — Quality prompts batch (live E2E docs, k6, trace blobs, audit UI, pre-commit gate, Terraform runbook)

**Added:** Optional **pre-commit governance gate** (`ArchLucid:Governance:PreCommitGateEnabled`, `PolicyPackAssignment.BlockCommitOnCritical`, SQL **`054`**), **`#governance-pre-commit-blocked`** problem type, durable audit **`GovernancePreCommitBlocked`**.

**Added:** **Agent execution trace** full-text blob persistence behind **`AgentExecution:TraceStorage:PersistFullPrompts`** (async blob writes + **`PatchBlobStorageFieldsAsync`**), SQL **`053`**, contract fields on **`AgentExecutionTrace`**.

**Added:** CI job **Performance: k6 smoke (API baseline)** (`tests/load/smoke.js`, non-blocking) and docs **`PERFORMANCE_TESTING.md`**.

**Changed:** Operator **Audit** page — **Clear filters** re-queries, **Export CSV**, summary line, helpers + Vitest; **`ComparisonSummaryPersisted`** audit matrix row; **`ExportsControllerCompareSummaryAuditTests`** usings fix.

**Docs:** **`AGENT_TRACE_FORENSICS.md`**, **`PRE_COMMIT_GOVERNANCE_GATE.md`**, **`TEST_STRUCTURE`** live E2E row, **`TEST_EXECUTION_MODEL`** k6/live rows, **`operator-shell`** audit section, Phase **7.5** Terraform runbook **`TERRAFORM_STATE_MV_PHASE_7_5.md`**, **`NEXT_REFACTORINGS`** backlog summary table.

---

## 2026-04-13 — Governance drift trend, promotion ordering, pipeline timeout, RunId, docs, Schemathesis PR

**Added:** **`GET /v1/governance/compliance-drift-trend`** and **`ComplianceDriftTrendService`** (time-bucketed policy pack change log aggregates). Operator UI **`ComplianceDriftChart`** on the governance dashboard (last 30 days, daily buckets).

**Changed:** Governance **promotions** and **approval requests** must follow **dev → test → prod** single steps (**`GovernanceEnvironmentOrder`**).

**Added:** **`AuthorityPipelineOptions`** (`AuthorityPipeline:PipelineTimeout`, default 5 minutes; **`TimeSpan.Zero`** disables). Authority orchestrator uses a linked cancellation source; timeouts roll back, log, and increment **`archlucid_authority_pipeline_timeouts_total`**.

**Added:** Strongly typed **`RunId`** (**`ArchLucid.Core.Identity`**) with **`System.Text.Json`** converter (incremental adoption; **`Guid`** remains the primary wire/storage shape until migrated).

**Docs:** **`DEGRADED_MODE.md`**; **`START_HERE.md`** reading order + documentation tiers + degraded-mode link; **`DATA_CONSISTENCY_MATRIX.md`** read-replica lag section; **`docs/archive/README.md`** and **`ARCHITECTURE_INDEX.md`** archive pointers; **`API_FUZZ_TESTING.md`** PR vs scheduled Schemathesis; **`UI_COMPONENTS.md`** **`ComplianceDriftChart`**.

**CI:** **`api-schemathesis-light`** job in **`ci.yml`** (Schemathesis **examples** phase only).

---

## 2026-04-12 — LogSanitizer (CWE-117)

**Added:** **`LogSanitizer`** utility for CWE-117 log injection prevention. Applied to string-typed HTTP input in the global exception handler, **`RunsController`** (**`CreateRun`** **`RequestId`**), and **`GovernanceController`** (**`Promote`** **`RunId`**).

---

## 2026-04-12 — Governance confirmations and run progress UI

**Added:** Confirmation dialogs for governance promote and activate actions via reusable **`ConfirmationDialog`** component.

**Added:** Real-time run progress tracker on run detail page — polls pipeline stages (context, graph, findings, manifest) with progress bar and badges for in-progress runs. See **`docs/UI_COMPONENTS.md`**.

---

## 2026-04-12 — Business KPI metrics and aggregate explanation caching

**Added:** Aggregate explanation caching via **`CachingRunExplanationSummaryService`** — eliminates redundant LLM calls on repeated run-detail aggregate explanation views when **`HotPathCache`** is enabled (keyed by run id + **`ROWVERSION`**; TTL from **`HotPathCacheOptions`**).

**Added:** Business-level OpenTelemetry metrics — **`archlucid_runs_created_total`**, **`archlucid_findings_produced_total`** (label **`severity`**), **`archlucid_llm_calls_per_run`** (histogram per agent batch), **`archlucid_explanation_cache_hits_total`** / **`archlucid_explanation_cache_misses_total`** (cache effectiveness; derive hit ratio in Prometheus/Grafana). See **`docs/OBSERVABILITY.md`** and recording rule **`archlucid:explanation_cache_hit_ratio`** in **`infra/prometheus/archlucid-slo-rules.yml`**.

---

## 2026-04-12 — IFeatureFlags and LLM fallback client

Introduced **`IFeatureFlags`** abstraction for testable feature flag evaluation. Added **`FallbackAgentCompletionClient`** for automatic LLM model failover on **429** / **5xx**.

---

## 2026-04-12 — Persisted run trace ID and CLI trace command

Persisted OpenTelemetry trace ID in **`dbo.Runs`** (Migration **052**). Added **`archlucid trace <runId>`** CLI command for post-hoc distributed trace lookup. Surfaced creation-time trace link in run detail UI.

---

## 2026-04-12 — Stryker mutation baselines

Raised Stryker mutation score baselines from 62% to 70% across all five modules (Persistence, Application, AgentRuntime, Coordinator, Decisioning).

---

## 2026-04-12 — Audit export and retention policy

Added audit export endpoint (`GET /v1/audit/export`) with CSV/JSON support and 90-day range limit. Created audit retention policy document (`docs/AUDIT_RETENTION_POLICY.md`). Database-enforced append-only on `dbo.AuditEvents` (Migration **051**).

---

## 2026-04-12 — CI hardening

CI hardening: Simmy chaos tests now block PRs (burn-in complete). Per-package line coverage gate raised from 50% to 60%.

Added Schemathesis API fuzz testing as a scheduled CI workflow against the OpenAPI spec. Operator docs: `docs/API_FUZZ_TESTING.md`; execution model and test matrix updated for Tier 4 (ZAP + Schemathesis).

---

## 2026-04-12 — Aggregate run explanation

Added aggregate run explanation endpoint (`/v1/explain/runs/{runId}/aggregate`) with theme summaries, risk posture, confidence score, and explanation provenance. Surfaced in run detail UI.

---

## Phase 7 — ArchLucid rename (code-level)

**Area:** Rename / operator breaking changes  
**Summary:** Removed legacy **`ArchiForge*`** configuration keys, **`ARCHIFORGE_*`** / UI OIDC storage bridges, and renamed CLI manifest (`archlucid.json`), global tool command (`archlucid`), SQL DDL file (`ArchLucid.sql`), and dev Docker/compose defaults. **`com.archiforge.*` integration event type strings are no longer emitted or aliased** — only canonical **`com.archlucid.*`** types apply. See **`BREAKING_CHANGES.md`** for migration steps. Terraform resource **addresses** using the historical **`archiforge`** token remain until a planned `state mv` (checklist 7.5); the APIM backend URL **variable** is now **`archlucid_api_backend_url`**.

---

## 59R — Learning-to-planning bridge

**Area:** Product learning / planning  
**Key deliverables:**

- `032_ProductLearningPlanningBridge.sql` (DbUp) + `ArchLucid.sql` parity — SQL tables for improvement themes, plans, and junction links to runs/signals/artifacts.
- Contracts under `ArchLucid.Contracts/ProductLearning/Planning/`.
- `IProductLearningPlanningRepository`, Dapper + in-memory implementations, DI registration.
- Unit tests: `ProductLearningPlanningRepositoryTests`.
- Docs: `SQL_SCRIPTS.md`, `DATA_MODEL.md`, this file.

**Intentionally deferred:** deterministic theme-derivation service, plan-draft builder with priority score.

---

## 58R — Product learning dashboard and improvement triage

**Area:** Operator tooling / product feedback  
**Key deliverables:**

- `ProductLearningPilotSignals` SQL table + Dapper and in-memory repositories.
- Aggregation services: `IProductLearningFeedbackAggregationService`, `IProductLearningImprovementOpportunityService`, `IProductLearningDashboardService`.
- HTTP API: `GET /v1/product-learning/summary`, `/improvement-opportunities`, `/artifact-outcome-trends`, `/triage-queue`, `/report` (Markdown/JSON).
- Operator UI: **Pilot feedback** page (`/product-learning`), export links.
- Tests: aggregation, ranking, parser, API, report-builder (`ChangeSet=58R` / `ProductLearning` filter tags).
- Docs: `PRODUCT_LEARNING.md`; updated `PILOT_GUIDE.md`, `OPERATOR_QUICKSTART.md`, `README.md`.

**Constraints:** No autonomous adaptation; human-entered signals only; scoped to tenant/workspace/project.

---

## 57R — Operator-journey E2E (Playwright)

**Area:** UI test harness  
**Key deliverables:**

- `e2e/fixtures/` — typed JSON payloads aligned with all UI coercion helpers.
- `e2e/helpers/route-match.ts`, `register-operator-api-routes.ts`, `operator-journey.ts` — centralised route dispatch and journey navigation.
- Specs: `smoke`, `compare-proxy-mock`, `run-manifest-journey`, `compare-journey`, `compare-stale-input-warning`, `manifest-empty-artifacts`.
- `e2e/mock-archlucid-api-server.ts` + `e2e/start-e2e-with-mock.ts` — loopback HTTP mock on port 18765 for RSC pages; `playwright.config.ts` `webServer` updated.
- `tsx` devDependency for TS mock runner; `e2e/tsconfig.json` + `npm run typecheck:e2e`.
- `-RunPlaywright` flag added to `release-smoke.ps1` / `.cmd`.
- Docs: `archlucid-ui/docs/TESTING_AND_TROUBLESHOOTING.md` (section 8 rewritten).

---

## 56R — Release-candidate hardening and pilot readiness

**Area:** Configuration, observability, packaging, operator docs  
**Key deliverables:**

- Fail-fast config validation (`ArchLucidConfigurationRules`) before DbUp; SQL connection required only when `StorageProvider=Sql`.
- `/health/live` (minimal) + `/health/ready` (DB, schema files, compliance pack, writable temp) + `/health` (all) with `DetailedHealthCheckResponseWriter` (enriched JSON including `version`, `commitSha`, `totalDurationMs`).
- Startup non-secret configuration snapshot log (toggle `Hosting:LogStartupConfigurationSummary`).
- `GET /version` endpoint (`VersionController`, `[AllowAnonymous]`): `application`, `informationalVersion`, `assemblyVersion`, `fileVersion`, `commitSha`, `runtimeFramework`, `environment`.
- `BuildProvenance` + `BuildInfoResponse` (Core): parses `CommitSha` from `+{sha}` suffix of informational version; CI stamps `SourceRevisionId=$(git rev-parse HEAD)`.
- API `ProblemSupportHints` (`extensions.supportHint`); CLI `CliOperatorHints` (`Next:` lines); UI proxy `502/503 supportHint`.
- `archlucid support-bundle` CLI command (folder + optional `--zip`): `README.txt`, `manifest.json` (v1.1 + `triageReadOrder`), `build.json`, `health.json`, `api-contract.json` (bounded OpenAPI probe), `config-summary.json`, `environment.json`, `workspace.json`, `references.json`, `logs.json`.
- Local scripts: `build-release`, `package-release`, `run-readiness-check`, `release-smoke` (`.cmd` + `.ps1`); `scripts/OperatorDiagnostics.ps1` (structured triage output).
- Release handoff artifacts in `artifacts/release/`: `metadata.json` (schema 1.1), `release-manifest.json`, `checksums-sha256.txt`, `PACKAGE-HANDOFF.txt`.
- Docs added: `PILOT_GUIDE.md`, `OPERATOR_QUICKSTART.md`, `TROUBLESHOOTING.md`, `RELEASE_LOCAL.md`, `RELEASE_SMOKE.md`, `CLI_USAGE.md`.

---

## 55R — Operator shell coherence

**Area:** UI shell  
**Key deliverables:**

- Shared navigation, breadcrumbs, and operator messaging patterns across home, runs, run/manifest detail, graph, compare, replay, and artifact review.
- Canonical manifest-scoped artifact URLs; `GET /runs/{runId}/artifacts/{artifactId}` resolves manifest then redirects.
- Compare page: sequential legacy-then-structured fetches; UI explains fetch order vs. on-page review order; optional AI explanation; stale-input warning when run IDs drift.
- Coercion/guard helpers for operator-facing JSON.
- Vitest smoke coverage: API wiring (list/descriptor/compare/explain), shell nav, key review components.

---

## How to add a changelog entry

1. Add a new `## <version> — <title>` section **above** the previous one.
2. Use the subsections: **Area**, **Key deliverables**, and (optionally) **Intentionally deferred**.
3. Keep entries to a navigable summary; put fine-grained prompt records in a new `docs/archive/CHANGE_SET_<id>.md` file and link from here.
