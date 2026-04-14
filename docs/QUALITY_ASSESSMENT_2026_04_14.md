# ArchLucid -- Weighted Quality Assessment (2026-04-14)

**Date:** 2026-04-14
**Assessor:** Independent code review via full codebase exploration
**Basis:** Source code (1,659 src files, 795 test files), 187 documentation files, 68 Terraform files, 55+ SQL migrations, CI/CD configuration, UI source (164 TSX files), and architecture artifacts as of HEAD.
**Methodology:** Each quality dimension is scored 1-100, multiplied by its weight. Sections are ordered by **weighted gap** (weight x (100 - score)) so the areas with the most impactful improvement opportunity appear first.

---

## Summary table

| # | Quality | Weight | Score | Weighted Score | Weighted Gap | Verdict |
|---|---------|--------|-------|----------------|--------------|---------|
| 1 | Correctness | 8 | 70 | 560 | 240 | Strong test infra; live E2E and rename risk gaps |
| 2 | Explainability | 6 | 72 | 432 | 168 | Trace completeness analyzer strong; LLM-dependent, forensics weak |
| 3 | Evolvability | 6 | 73 | 438 | 162 | Config gates and ADRs solid; rename debt and 90-item backlog drag |
| 4 | Traceability | 7 | 77 | 539 | 161 | Excellent OTel + correlation; UI trace visualization limited |
| 5 | Security | 6 | 74 | 444 | 156 | Defense in depth present; RLS gaps and static API keys |
| 6 | Reliability | 5 | 70 | 350 | 150 | Resilience primitives in place; no systemic proof under load |
| 7 | Architectural Integrity | 7 | 79 | 553 | 147 | NetArchTest-enforced layering; dual namespace and large controller |
| 8 | Usability | 4 | 64 | 256 | 144 | Functional operator shell; UX polish and feedback gaps |
| 9 | Data Consistency | 5 | 73 | 365 | 135 | Documented matrix; archival cascade and FK gaps |
| 10 | Cognitive Load | 4 | 67 | 268 | 132 | Exceptional docs offset by inherent domain complexity |
| 11 | Auditability | 5 | 74 | 370 | 130 | 69 event types with CI guard; fire-and-forget edges |
| 12 | Policy & Governance Alignment | 5 | 75 | 375 | 125 | Approval workflow complete; preventive controls missing |
| 13 | AI/Agent Readiness | 4 | 69 | 276 | 124 | Simulator + fallback solid; evaluation harness and streaming absent |
| 14 | Scalability | 3 | 61 | 183 | 117 | Scale-aware architecture; untested and vertically bounded |
| 15 | Maintainability | 4 | 72 | 288 | 112 | Good tooling; 2,100-line backlog signals accumulated debt |
| 16 | Interoperability | 3 | 70 | 210 | 90 | CloudEvents + OpenAPI + AsyncAPI; no inbound connectors |
| 17 | Availability | 2 | 62 | 124 | 76 | HA documented; no proven drills or multi-leader worker |
| 18 | Performance | 2 | 63 | 126 | 74 | Caching present; zero published latency baselines |
| 19 | Testability | 3 | 77 | 231 | 69 | 18 test projects + mutation testing; UI E2E mostly mocked |
| 20 | Cost-Effectiveness | 2 | 67 | 134 | 66 | FinOps docs and token telemetry; no runtime cost dashboard |
| 21 | Modularity | 3 | 79 | 237 | 63 | 30 projects with enforced boundaries; Application assembly oversized |
| 22 | Observability | 3 | 81 | 243 | 57 | Best-in-class OTel + Prometheus + Grafana; no RUM |
| 23 | Manageability | 2 | 73 | 146 | 54 | CLI-first admin strong; no admin UI |
| 24 | Deployability | 2 | 76 | 152 | 48 | Docker + Terraform + CD; no blue-green/canary |
| 25 | Accessibility | 1 | 57 | 57 | 43 | Axe gates on 5 of ~25 routes; most UI unverified |
| 26 | Extensibility | 1 | 75 | 75 | 25 | Finding engine template; no plugin runtime |
| 27 | Documentation | 1 | 87 | 87 | 13 | 187 markdown files; exceptional breadth |
| | **Totals** | **107** | | **7,519** | **2,781** | **Weighted average: 70.3 / 100** |

---

## Detailed assessment (ordered by weighted gap, highest first)

---

### 1. Correctness -- Score: 70 / 100 (weight 8, weighted gap 240)

**What it means:** The system produces correct results under documented conditions and its behavior matches its specification.

**Justification:**

- **Strengths:** 18 .NET test projects with 795 test files yield an approximate 1:2.1 test-to-source ratio. Tiered CI (Tier 0-4) with gitleaks, fast core, full regression + SQL, Simmy chaos (blocking), UI unit + E2E. Stryker mutation testing across 5 configurations at a 70% break threshold and 65% baseline score. OpenAPI contract snapshot tests catch schema drift at merge time. SQL Server container integration tests validate Dapper queries against real schema. Architecture constraint tests (15 NetArchTest facts) prevent structural regressions. `WebApplicationFactory`-based integration tests exercise the full HTTP pipeline. Live API E2E (`ui-e2e-live` CI job) exercises the operator happy path against a real SQL stack (health, create, execute, commit, manifest, export, governance, audit).
- **Weaknesses:** The live E2E test covers exactly **one** happy path -- the remaining Playwright specs (mock-backed) cannot prove API-SQL-UI integration. The product rename (phases 1-7) touched over 2,200 C# source files; while CI guards exist (rename grep, architecture constraints), the combinatorial risk of subtle config/namespace behavioral changes is non-trivial. `NEXT_REFACTORINGS.md` contains **2,100+ lines** including correctness-relevant items (e.g., missing replay validation test at item 9, incomplete create-run-execute helper at item 10). Stryker baseline of 65.0 per module means roughly 35% of mutations survive -- mutants in domain logic (`Decisioning`, `Application`) are particularly consequential. No property-based testing (FsCheck or similar) for domain invariants.
- **Trade-off:** Mock-backed UI E2E provides speed and determinism at the cost of integration confidence. The single live E2E happy path is a good start but leaves most operator flows unvalidated against the real stack.

**Improvements:**
1. Expand the live E2E suite to cover at least the negative-path journey (run-not-found, commit conflict, self-approval block) against the real API + SQL.
2. Raise Stryker break thresholds to 80% for `Decisioning` and `Application` -- these are the highest-consequence domain assemblies.
3. Introduce property-based testing for manifest merge, governance resolution, and findings engine output invariants.

---

### 2. Explainability -- Score: 72 / 100 (weight 6, weighted gap 168)

**What it means:** Decisions made by the system -- especially AI/agent outputs -- can be understood, audited, and justified to humans.

**Justification:**

- **Strengths:** `ExplainabilityTrace` on every `Finding` with 5 structured fields (`GraphNodeIdsExamined`, `RulesApplied`, `DecisionsTaken`, `AlternativePathsConsidered`, `Notes`). Target coverage: 4/5 fields populated for rule-based engines (documented in trace coverage matrix). `ExplainabilityTraceCompletenessAnalyzer` produces per-engine and aggregate ratios, pure functions suitable for tests. OTel histogram `archlucid_explainability_trace_completeness_ratio` (per-scan). `ExplanationFaithfulnessChecker` provides a heuristic overlap signal between explanation text and finding traces. Provenance graph in the operator UI (layered SVG: snapshots -> findings -> decisions -> manifest -> artifacts). `GET /v1/explain/runs/{runId}/aggregate` provides stakeholder-facing narrative.
- **Weaknesses:** `AlternativePathsConsidered` is **intentionally empty** for all rule-based engines (10 engines; reserved for LLM-style engines that do not yet exist). Agent execution traces optionally store full prompts in blob storage, but this is the **product default** only when `PersistFullPrompts` is true -- the blob pointer columns may be null if uploads fail. There is no stored **prompt snapshot** per run that ties a specific prompt version to a specific output for forensic replay. Explanation and Ask endpoints return errors with no fallback when LLM is unavailable. No explanation diffing between two runs (comparison explanation exists separately). The faithfulness checker is a coarse token-overlap heuristic, not semantic entailment.
- **Trade-off:** Deferring `AlternativePathsConsidered` avoids placeholder noise for rule-based engines but leaves a structural gap in multi-branch decision justification. Optional blob persistence balances cost against forensic completeness.

**Improvements:**
1. Persist exact prompt text + model version + raw response as a mandatory part of `AgentExecutionTrace` (not optional blob) so that every agent output is forensically reproducible.
2. Provide a cached or template-based fallback explanation when LLM is unavailable (e.g., generate narrative from findings + manifest structure deterministically).
3. Implement explanation diffing -- given two runs, show what changed in the explanation and why, tied to the underlying finding/manifest differences.

---

### 3. Evolvability -- Score: 73 / 100 (weight 6, weighted gap 162)

**What it means:** The system can adapt to new requirements, technologies, and organizational changes without disproportionate rework.

**Justification:**

- **Strengths:** Config-gated features (`StorageProvider`, `HotPathCache:Enabled`, `IntegrationEvents:*`, `FallbackLlm:Enabled`, `AuthorityPipeline:Mode`) enable incremental rollout. 13 ADRs document trade-off rationale with context. API versioning (`/v1/`) with deprecation headers. `schemaVersion` on integration event payloads. Finding engine template (`dotnet new archlucid-finding-engine`) for custom extensions. Interfaces and DI throughout. Phase-based rename checklist with clear deferred-with-reason tracking. `IOptionsMonitor` hot reload for circuit breaker and rate limit configuration.
- **Weaknesses:** Rename phases **7.5-7.8 are indefinitely deferred** -- Terraform `state mv` for resource addresses, GitHub repo rename, Entra app registration rename, workspace root path. Every new Terraform change must navigate stale resource addresses (`azurerm_api_management.archiforge`). `NEXT_REFACTORINGS.md` contains ~90 numbered items spanning 2,100+ lines -- many non-trivial. Dual namespace in `ArchLucid.Persistence` (Data vs authority) adds contributor friction. No plugin/extension point system beyond finding engines. Config gate combinatorics are growing (many optional features interact, but interaction testing is ad-hoc).
- **Trade-off:** Config gates enable safe incremental rollout but accumulate combinatorial complexity. Documenting deferred items is better than hiding them, but indefinite deferral of 7.5-7.8 creates ongoing tax.

**Improvements:**
1. Schedule and execute Phase 7.5 (Terraform `state mv`) -- this is the single highest-leverage evolvability action because it eliminates daily friction for every infrastructure change.
2. Triage `NEXT_REFACTORINGS.md` aggressively: close completed items, defer-with-date remaining ones, delete obsolete entries. Target reducing the list to 30 active items.
3. Document config gate interaction matrix -- which feature flags are mutually exclusive, which compound, and which combinations are tested in CI.

---

### 4. Traceability -- Score: 77 / 100 (weight 7, weighted gap 161)

**What it means:** Every artifact, decision, or system event can be traced back to its origin and forward to its effects.

**Justification:**

- **Strengths:** `X-Correlation-ID` on all requests, propagated to OTel spans and Serilog enrichers. Background job correlation (synthetic `correlation.id` for outbox, archival, integration events). `OtelTraceId` persisted on `dbo.Runs` at creation time (migration 052), never overwritten. 8 custom `ActivitySource` names covering authority runs, advisory scans, retrieval indexing, agent handling, LLM completion, outbox processing, integration events, and data archival. CLI `trace <runId>` opens the trace viewer. Provenance graph in the UI (snapshots -> findings -> decisions -> manifest -> artifacts). Audit events carry `RunId`, `ManifestId`, `ArtifactId`. Grafana dashboard `dashboard-archlucid-run-lifecycle.json` provides template-variable `runId` drill-down. Indexes on `dbo.AuditEvents` for `CorrelationId` and `RunId` (migration 055).
- **Weaknesses:** No end-to-end lineage view connecting a user request to **every** downstream side effect (integration events, webhooks, advisory scans spawned). Authority pipeline stage spans are OTel-only -- no UI visualization of stage durations or outcomes. `AgentExecutionTrace` records are not surfaced in the operator UI beyond the provenance graph node. Audit events do not carry the originating OTel trace ID, so cross-channel lookup (audit -> trace backend) requires manual correlation. No causal trace across runs (e.g., "this advisory scan was triggered by that run's completion").
- **Trade-off:** Prioritizing OTel-backend traceability (Jaeger/Tempo) over in-product visualization is appropriate for an infrastructure product, but it means operators without a trace backend get limited trace visibility.

**Improvements:**
1. Surface authority pipeline stage durations and outcomes directly in the operator UI run detail page (not just external OTel backends).
2. Store `OtelTraceId` on `dbo.AuditEvents` so operators can navigate from any audit row to the corresponding distributed trace.
3. Add a "trace explorer" page to the operator UI showing the full DAG from request -> run -> integration events -> downstream effects.

---

### 5. Security -- Score: 74 / 100 (weight 6, weighted gap 156)

**What it means:** The system protects data confidentiality, integrity, and availability against anticipated threats.

**Justification:**

- **Strengths:** Three auth modes (DevelopmentBypass with non-production guardrail, JwtBearer/Entra, ApiKey) with role-based authorization policies (`CanCommitRuns`, `CanSeedResults`, etc.). SQL RLS via `SESSION_CONTEXT` (defense in depth, opt-in per `SqlServer:RowLevelSecurity:ApplySessionContext`). Private endpoint Terraform modules. Gitleaks secret scan in CI (Tier 0, merge-blocking). CodeQL analysis (separate workflow). Trivy image scan and Terraform config scan for misconfigurations (HIGH/CRITICAL, exit-code 1). OWASP ZAP baseline (CI + weekly schedule, warnings = failure). Schemathesis API fuzz testing (scheduled). `LogSanitizer` for CWE-117 log injection. `DENY UPDATE/DELETE` on `dbo.AuditEvents` (migration 051). HMAC-SHA256 signed webhook delivery. Production configuration safety rules (`ArchLucidConfigurationRules.CollectProductionSafetyErrors` -- no wildcard CORS, HMAC required for webhooks). STRIDE threat model (system-wide + Ask/RAG-specific). CycloneDX SBOM generation.
- **Weaknesses:** API key auth uses **static shared secrets** -- no rotation without restart (pending `IOptionsMonitor` for API key hot-reload). No documented secret rotation runbook for API keys (Key Vault secret rotation runbook exists for certs but not API keys). RLS covers **selected tables only** -- child tables without denormalized scope columns rely solely on application-layer enforcement. `DevelopmentBypass` guards depend on `ASPNETCORE_ENVIRONMENT` being correctly set -- a misconfigured production deploy could bypass auth entirely. No penetration test results referenced in the repository. No CSP (Content-Security-Policy) headers documented for the Next.js UI. Prompt injection risk acknowledged in threat model but prompt redaction is still a backlog item.
- **Trade-off:** RLS adds per-connection latency (`SESSION_CONTEXT` set on each open) and partial coverage means some cross-tenant queries on uncovered tables rely on code correctness alone. Static API keys are simpler to implement but create a rotation blind spot.

**Improvements:**
1. Implement `IOptionsMonitor` for API key hot-reload and document a secret rotation runbook so API key rotation does not require a restart.
2. Extend RLS to all tables carrying tenant scope columns, or document the explicit exclusion list with a signed-off risk acceptance.
3. Add Content-Security-Policy headers to the Next.js UI and enforce them in CI (e.g., a Playwright test that verifies CSP response headers).

---

### 6. Reliability -- Score: 70 / 100 (weight 5, weighted gap 150)

**What it means:** The system operates correctly under adverse conditions and recovers gracefully from failures.

**Justification:**

- **Strengths:** Polly retry + circuit breaker on LLM calls (`CircuitBreakerGate` with `IOptionsMonitor` hot reload). `FallbackAgentCompletionClient` for model-level failover. SQL connection open retries (`ResilientSqlConnectionFactory`). Simmy chaos tests in CI (blocking on PR, weekly schedule). Degraded-mode documentation with feature availability matrix (`DEGRADED_MODE.md`). Transactional outbox for integration events with dead-letter and admin retry. Health checks: liveness, readiness, detailed JSON with circuit breaker state. `AgentHandlerConcurrencyGate` (bulkhead). Data archival health check on readiness probe.
- **Weaknesses:** **No load test or soak test** in CI -- reliability under sustained concurrent load is entirely unknown. No automated failover drill results documented (runbooks exist but no evidence of execution). Simmy chaos tests validate individual resilience primitives (e.g., LLM retry) but do not test **systemic** failure scenarios (e.g., SQL + LLM both degraded, or outbox backlog + concurrent run commits). Single worker process with leader election is a SPOF for async processing. Archival cascade is incomplete -- child snapshots are not reliably cascaded, which can leave the database in a structurally inconsistent state after partial archival. No chaos testing for the UI proxy layer.
- **Trade-off:** Individual resilience primitives (retry, circuit breaker, fallback) are well-implemented, but the absence of systemic stress testing means the composition of these primitives under combined failure is unverified.

**Improvements:**
1. Add a k6 or Locust load-test smoke to CI (even 60 seconds with 10 virtual users) to establish a regression baseline for throughput and error rates.
2. Run and document a combined failure drill (SQL degraded + LLM breaker open + outbox backlog) against staging, recording recovery behavior and timelines.
3. Add multi-instance worker support or document the explicit blast radius and MTTR of single-leader worker failure.

---

### 7. Architectural Integrity -- Score: 79 / 100 (weight 7, weighted gap 147)

**What it means:** The system's actual structure faithfully reflects its intended design principles and constraints.

**Justification:**

- **Strengths:** 30+ .NET projects with explicit layering (Core -> Contracts -> Application -> Persistence -> Api). NetArchTest enforcement with **15 constraint facts** in `DependencyConstraintTests` covering Tier 1 (foundation isolation), Tier 2 (persistence sub-module boundaries), Tier 3 (domain hexagonal boundary), and Tier 4 (CLI isolation). A source-scan rule prevents direct `IIntegrationEventPublisher.PublishAsync` calls outside authorized wrappers. 13 ADRs documenting key decisions with rationale. C4 diagrams (context, container, component). Host composition (`ArchLucid.Host.Composition`) separates domain DI wiring from API concerns. Persistence split into 6 sub-assemblies (Data, Coordination, Runtime, Integration, Advisory, Alerts) for bounded context alignment. Architecture-on-a-page document with Mermaid diagram. Dual pipeline navigator document.
- **Weaknesses:** `ArchLucid.Persistence` hosts **two namespaces** inside one conceptual area (Data for run/commit workflow vs authority/decisioning ports) -- the `ARCHITECTURE_COMPONENTS.md` explicitly acknowledges this as historical. Dual `IGoldenManifestRepository` and `IDecisionTraceRepository` interfaces exist in both `Decisioning.Interfaces` and `Persistence.Data.Repositories`, requiring fully-qualified DI registration to avoid confusion. `ArchitectureController` is a large controller handling ~15 endpoint groups under `/v1/architecture/*` (runs, commit, replay, comparisons, drift, diagnostics). `ArchLucid.Application` is a monolithic assembly containing runs, replay, comparison, export, governance, advisory, digest, alert, and learning services.
- **Trade-off:** Keeping persistence sub-modules in shared assemblies avoids project proliferation and build-time cost, but violates single-responsibility at the assembly level and increases cognitive load for new contributors.

**Improvements:**
1. Split `ArchitectureController` into focused controllers per subdomain (e.g., `RunsController`, `ComparisonController`, `ReplayController`, `DiagnosticsController`).
2. Rename the dual `IGoldenManifestRepository` interfaces to explicitly distinguish authority vs coordinator contexts (e.g., `IAuthorityManifestRepository` vs `ICoordinatorManifestRepository`).
3. Extract the `ArchLucid.Persistence.Data` namespace into its own assembly to resolve the dual-namespace confusion.

---

### 8. Usability -- Score: 64 / 100 (weight 4, weighted gap 144)

**What it means:** The system is easy, efficient, and satisfying for operators to use.

**Justification:**

- **Strengths:** Operator shell (Next.js) with 164 TSX components covering runs list, run detail, manifest summary, artifact review, compare, replay, graph viewer, governance dashboard, policy packs, alerts, advisory, audit, digests, search, evolution review, and planning. `OperatorFirstRunWorkflowPanel` guides new operators through first-run steps. `OperatorTryNext` provides consistent next-action copy across pages. CLI with `doctor`, `support-bundle`, `trace`, `status`, `artifacts` commands. Confirmation dialogs for destructive actions (Radix AlertDialog). Loading, empty, and error states documented. Keyboard shortcuts with `ShortcutHint` component.
- **Weaknesses:** UI is explicitly described as a "thin shell" -- functional but not polished. No toast/notification system for async operation feedback (run completion, governance approvals, alert triggers). No dark mode. Limited progressive disclosure -- advanced features (provenance graph, replay modes, evolution simulation) are presented alongside basic features without hierarchical organization. The operator must understand both coordinator and authority pipelines -- dual pipeline complexity leaks into the UI. No guided troubleshooting workflow in the UI (relies on CLI `doctor` and docs). No user preferences or customizable views. No inline help or contextual documentation links.
- **Trade-off:** "Thin shell" minimizes UI maintenance burden and allows rapid iteration on API-first capabilities, but caps operator productivity and perceived product quality.

**Improvements:**
1. Add a toast/notification system for async operation feedback (run completion, governance state changes, alert triggers).
2. Implement progressive disclosure -- collapse advanced features behind expandable sections and add a "power user" toggle.
3. Add in-app guided troubleshooting that surfaces `doctor` checks and health endpoint results without requiring CLI access.

---

### 9. Data Consistency -- Score: 73 / 100 (weight 5, weighted gap 135)

**What it means:** Data remains accurate, complete, and coherent across all read and write paths.

**Justification:**

- **Strengths:** `DATA_CONSISTENCY_MATRIX.md` explicitly documents consistency guarantees per aggregate (strongly consistent, transactionally outboxed, eventually aligned). `ROWVERSION` for optimistic concurrency on `dbo.Runs` and selected tables (409 Conflict on collision). `IArchLucidUnitOfWork` as the standard transactional boundary for mutating authority SQL. Transactional outbox for integration events (enqueue tied to commit transaction). Hot-path cache (`IHotPathReadCache`) invalidation on documented write paths using `OUTPUT` scope columns. Read-replica staleness expectations documented with expected lag ranges. Dual persistence retired (ADR 0012 complete; `ArchitectureRuns` table dropped in migration 049). Read-replica routing documented for list/dashboard queries with operator guidance for perceived staleness.
- **Weaknesses:** Archival cascade is **incomplete** -- child snapshots are not always cascaded in the same SQL statement as run archival. **Application-enforced** referential integrity for coordinator artifacts means data consistency depends on code correctness, not database constraints (migration 047 dropped legacy FKs; no replacement constraints). Hot-path cache has TTL-bound staleness risk for changes made outside documented repository write paths (e.g., ad-hoc SQL). `TransactionScope` is not used -- `IArchLucidUnitOfWork` is the standard, but cross-repository atomicity for new features requires developer discipline and awareness.
- **Trade-off:** Application-enforced referential integrity avoids FK cascade complexity and enables flexible schema evolution, but opens a window for orphaned rows if a code path is missed or a new writer is added without cache invalidation.

**Improvements:**
1. Add a scheduled reconciliation job that validates referential integrity between authority and coordinator tables and reports orphaned rows.
2. Implement archival cascade for child snapshots (context, graph, findings) within the same SQL transaction as run archival.
3. Add a database-level FK constraint (or a CI test that asserts referential integrity) for critical parent-child relationships.

---

### 10. Cognitive Load -- Score: 67 / 100 (weight 4, weighted gap 132)

**What it means:** How much mental effort is required for a new developer, operator, or contributor to understand and work with the system.

**Justification:**

- **Strengths:** Exceptional documentation effort: 187 markdown files, `START_HERE.md` as single entry point, `GOLDEN_PATH.md` with role-based lanes, `DUAL_PIPELINE_NAVIGATOR.md` for the hardest concept, `ONBOARDING_HAPPY_PATH.md` tracing one request end-to-end, `CSHARP_TO_REACT_ROSETTA.md` for cross-stack developers, `GLOSSARY.md` with 20+ domain terms, week-one tickets per role (`day-one-developer.md`, `day-one-sre.md`, `day-one-security.md`), `CODE_MAP.md` for where to open the code first, `ARCHITECTURE_INDEX.md` as full doc map.
- **Weaknesses:** The system has **two** conceptual pipelines (coordinator + authority) with different persistence interfaces, naming conventions, and mental models. 30 .NET projects, 8 Terraform roots, 6 persistence sub-assemblies, and a multi-phase rename still in progress. A new developer must understand: DI composition (`Host.Composition`), config gates (multiple interacting flags), dual pipelines, RLS (`SESSION_CONTEXT`), OTel instrumentation, Stryker mutation testing, Simmy chaos, DbUp migration discipline, the rename checklist, and the distinction between durable audit and baseline mutation logging. Dual `IGoldenManifestRepository` interfaces with the same name in different namespaces require fully-qualified type awareness. The 2,100-line `NEXT_REFACTORINGS.md` file acts more as a dumping ground than a curated backlog, creating noise for contributors trying to understand what is important.
- **Trade-off:** Documentation compensates significantly for structural complexity, but cognitive load is ultimately driven by the number of concepts a contributor must hold simultaneously. More docs cannot fully offset 30-project, dual-pipeline, multi-flag complexity.

**Improvements:**
1. Create a single-page "contributor decision tree" in `START_HERE.md`: "Adding a new finding engine? -> go here. Adding a new API endpoint? -> go here. Changing persistence? -> go here. Modifying governance? -> go here."
2. Produce a visual system map poster (single Mermaid diagram) showing all projects, their pipeline membership, and allowed dependency directions.
3. Prune the rename artifacts -- remove bridge code documentation and legacy fallback guidance once phases 7.5-7.8 complete, to reduce noise.

---

### 11. Auditability -- Score: 74 / 100 (weight 5, weighted gap 130)

**What it means:** All significant state-changing actions are recorded and retrievable for compliance review.

**Justification:**

- **Strengths:** 69 `AuditEventTypes` constants in `ArchLucid.Core.Audit` with a **CI guard** (`audit-core-const-count` anchor in `AUDIT_COVERAGE_MATRIX.md` verified in CI). Durable SQL audit (`dbo.AuditEvents`) with append-only enforcement (`DENY UPDATE/DELETE` via migration 051). Bulk export API (`GET /v1/audit/export`) with CSV/JSON, 90-day windows, 10K-row cap, `Content-Disposition: attachment`. Baseline mutation audit channel (structured `ILogger` lines). Circuit breaker audit bridge (fire-and-forget to avoid hot-path latency). Indexes on `CorrelationId` (filtered) and `RunId` (filtered) added in migration 055. Audit search API (`GET /v1/audit/search`) with filters. Dual-write pattern in `GovernanceWorkflowService` (both `IAuditService` and `IBaselineMutationAuditService`). Known gaps section in the matrix now shows **0 open gaps** (previously tracked items resolved).
- **Weaknesses:** Baseline mutation audit goes to **structured logs only**, not durable SQL -- a log pipeline failure loses that evidence channel. Fire-and-forget circuit breaker audit means some audit rows may be lost under extreme SQL pressure. No tamper-evident mechanism on SQL audit rows beyond `DENY` (no cryptographic chaining, hash, or append-only ledger). The operator UI audit page is a basic list -- the API supports search/filter but the UI does not fully expose those capabilities. `DENY UPDATE/DELETE` depends on the `ArchLucidApp` database role existing -- the migration skips if the role is absent, meaning dev/test environments have no protection.
- **Trade-off:** Fire-and-forget circuit breaker audit avoids hot-path latency (correct priority) but creates a small window where audit evidence can be lost. `DENY` enforcement depends on operational role setup.

**Improvements:**
1. Promote the highest-severity baseline mutation audit events from structured logging to durable SQL (`dbo.AuditEvents`).
2. Wire the full audit search/filter API capabilities into the operator UI audit page (the API endpoints already exist).
3. Add a CI test that verifies `DENY UPDATE/DELETE` is present in the master DDL script (`ArchLucid.sql`) to prevent accidental removal.

---

### 12. Policy & Governance Alignment -- Score: 75 / 100 (weight 5, weighted gap 125)

**What it means:** The system enforces organizational policies and governance workflows with appropriate controls.

**Justification:**

- **Strengths:** Governance workflow: approval -> promotion -> environment activation with **segregation of duties** enforcement (`GovernanceSelfApprovalBlocked` audit event and exception). Policy packs with versioning, assignment, and archival. Effective governance merge at run time with conflict detection. `GovernanceEnvironmentOrder` for promotion ordering (dev -> test -> prod only). Governance audit events (6 durable event types). Compliance drift trend API (`GET /v1/governance/compliance-drift-trend`). Governance preview (manifest diff) and dry-run (workflow gate validation without side effects). Policy pack change log (migration 050). Pre-commit governance gate (`GovernancePreCommitBlocked` audit event in `ArchitectureRunCommitOrchestrator`).
- **Weaknesses:** No policy-as-code integration (e.g., OPA/Rego for organization-specific rules). No SLA or deadline enforcement on pending approvals -- approvals can hang indefinitely with no escalation. No delegation model (who can approve when the designated reviewer is unavailable). No policy pack impact analysis (what would change if a pack is applied or removed retroactively). Compliance findings are predominantly **post-hoc** -- the pre-commit gate exists but is optional and does not block on individual finding severity by default. Governance UI is a dashboard, not an inline workflow -- operators must navigate between the run detail and governance pages.
- **Trade-off:** Post-hoc compliance keeps the run pipeline fast and avoids blocking on every commit, but means non-compliant manifests can be committed and must be caught by advisory scans or manual review.

**Improvements:**
1. Implement approval SLA with configurable escalation (auto-notify after N hours, auto-escalate to alternate reviewer after 2N hours).
2. Make the pre-commit governance gate severity-configurable (e.g., block on `Critical` findings, warn on `Error`, allow `Warning`/`Info`).
3. Add policy pack impact analysis API: "if I assign/remove this pack, how many existing runs would change status?"

---

### 13. AI/Agent Readiness -- Score: 69 / 100 (weight 4, weighted gap 124)

**What it means:** How well the system supports AI/agent-driven workflows and can evolve with AI capabilities.

**Justification:**

- **Strengths:** `IAgentCompletionClient` / `ILlmProvider` abstraction with descriptor metadata (`LlmProviderDescriptor`, `LlmProviderAuthScheme`). `DeterministicAgentSimulator` for fast, reproducible test cycles. `FallbackAgentCompletionClient` for primary -> secondary model failover. Circuit breaker + retry on LLM calls with hot-reloadable configuration. Agent prompt versioning (catalog templates + SHA-256). `AgentExecutionTrace` with optional full-prompt blob storage. Token usage telemetry (`archlucid_llm_*` counters). Concurrency gate (bulkhead). Agent output structural completeness metric (`archlucid_agent_output_structural_completeness_ratio`). Schema validation on agent result JSON (`AgentResultSchemaViolation` audit event).
- **Weaknesses:** No **A/B testing framework** for prompt variants -- prompt changes are all-or-nothing. No **structured evaluation harness** for agent output quality (no scoring rubric, no automated comparison against expected outputs). No **streaming support** for the Ask endpoint (full response only, poor perceived latency for interactive use). `AlternativePathsConsidered` empty for all 10 engines. Single primary + single fallback model only (no multi-model orchestration, no model routing by task type). No agent memory or context window management strategy documented. Blob storage for full prompts is fire-and-forget -- upload failures leave null pointer columns, creating silent forensic gaps.
- **Trade-off:** The deterministic simulator enables fast test cycles but means real LLM behavioral drift (model updates, prompt sensitivity) is only detected in staging or production.

**Improvements:**
1. Build a structured agent output evaluation harness: define expected outputs for reference inputs, score actual outputs against a rubric, track scores over time to detect model/prompt regression.
2. Add streaming support for the Ask endpoint to improve perceived latency for interactive operator use.
3. Implement per-run forensic prompt persistence as a non-optional (not fire-and-forget) part of `AgentExecutionTrace` -- retry blob uploads or fall back to inline storage.

---

### 14. Scalability -- Score: 61 / 100 (weight 3, weighted gap 117)

**What it means:** The system handles increasing load, data volume, and tenant count without degradation.

**Justification:**

- **Strengths:** Read replica routing (`ReadReplicaRoutedConnectionFactory`) for list/dashboard queries. Hot-path read cache (`IHotPathReadCache`) for single-row reads (memory or Redis). Rate limiting with three policies (`fixed`, `expensive`, `replay`). Pagination on run, alert, and audit lists. Transactional outbox decouples event publishing from request latency. Worker process for async background work. Container Apps with revision-based horizontal scaling.
- **Weaknesses:** **No load test baseline** -- throughput limits, breaking points, and scaling behavior are entirely unknown. Single SQL Server primary for all writes (no write sharding or partitioning strategy). LLM calls are a serial bottleneck per agent handler (concurrency gate is process-local, not distributed across instances). No horizontal scaling guidance beyond "deploy more Container Apps replicas." Audit export capped at 10K rows / 90-day windows -- large tenants need many sequential calls. No queue-based work distribution for agent execution across nodes. `OFFSET/FETCH` pagination may degrade on deep pages of large tables.
- **Trade-off:** The architecture is vertically scalable (bigger SQL tier, more Container Apps replicas) but not horizontally partitioned. This is appropriate for V1/pilot but will require architectural changes for multi-tenant SaaS at scale.

**Improvements:**
1. Run a load test (k6) and publish baseline numbers: max concurrent runs, p95 latency under load, SQL DTU saturation point.
2. Document a horizontal scaling architecture for multi-node agent execution (Service Bus queue-based work distribution).
3. Add keyset pagination as an alternative to `OFFSET/FETCH` for deep page navigation on `dbo.Runs` and `dbo.AuditEvents`.

---

### 15. Maintainability -- Score: 72 / 100 (weight 4, weighted gap 112)

**What it means:** How easy it is to fix bugs, add features, and keep the system healthy over time.

**Justification:**

- **Strengths:** Modular project structure (30 projects). `.editorconfig` + `dotnet format` for style consistency. Central package management (`Directory.Packages.props`). `FORMATTING.md` for code conventions. Test trait system (`Suite`, `Category`) for granular test selection. Stryker mutation gates. Architecture constraint tests (15 facts). CI rename guards (C# + TS grep). DI registration map. Coverage reports per CI job with collapsible summaries. `test-*.cmd` / `.ps1` scripts for local test execution.
- **Weaknesses:** `NEXT_REFACTORINGS.md` at 2,100+ lines is a **maintenance burden in itself** -- a backlog this large signals accumulated technical debt and makes prioritization difficult. The rename checklist has been running for 10+ days with phases 7.5-7.8 indefinitely deferred, adding ongoing maintenance overhead. Some test projects have thin coverage relative to their source projects (e.g., `ArchLucid.Provenance.Tests`). `ArchitectureController` handles ~15 endpoint groups -- changes to one group risk regressions in others. No per-project coverage gates (only solution-wide 70% merged line minimum).
- **Trade-off:** Documenting debt openly (in `NEXT_REFACTORINGS.md`) is far better than hiding it, but the sheer volume may discourage new contributors who interpret it as systemic instability.

**Improvements:**
1. Add per-project coverage thresholds to CI (not just solution-wide) to prevent thin-coverage projects from hiding behind aggregate numbers.
2. Prune `NEXT_REFACTORINGS.md` to a curated list of 20-30 prioritized items; archive completed/obsolete entries.
3. Split `ArchitectureController` into smaller, focused controllers to reduce change-blast-radius.

---

### 16. Interoperability -- Score: 70 / 100 (weight 3, weighted gap 90)

**What it means:** The system works with other systems, standards, and protocols.

**Justification:**

- **Strengths:** OpenAPI specification (two generators: Microsoft + Swashbuckle). AsyncAPI 2.6 for webhook event schemas. CloudEvents envelope for webhook delivery. Integration events via Azure Service Bus with JSON Schema catalog. HMAC-SHA256 signed webhooks. `X-Correlation-ID` header convention. CSV + JSON export formats. Bruno collection for manual API testing. SARIF report format (gitleaks). CycloneDX SBOM (both .NET NuGet and npm). `W3C traceparent` response headers.
- **Weaknesses:** No inbound connector/adapter framework -- context ingestion is code-only, not configurable or pluggable. No GraphQL or gRPC surface. Webhook URLs are config-only -- no webhook subscription management API for dynamic registration. Integration event consumer is logging-only by default -- no out-of-box handlers for common targets (Slack, Teams, email, ITSM). No SCIM or external identity sync. Export formats are limited to JSON, CSV, DOCX, and ZIP -- no SARIF, JUnit, or standard architecture exchange formats (e.g., C4 model JSON, Structurizr workspace).
- **Trade-off:** CloudEvents + Service Bus cover most enterprise messaging patterns, but the lack of inbound connectors limits integration with existing architecture tools and source-of-truth systems.

**Improvements:**
1. Add a webhook subscription management API (`POST /v1/webhooks`, `DELETE /v1/webhooks/{id}`) so consumers can register dynamically.
2. Build one reference integration event handler (e.g., Teams/Slack notification on run completion) as a proof point.
3. Define a pluggable inbound connector interface for importing architecture artifacts from common tools (Structurizr, Draw.io, ArchiMate).

---

### 17. Availability -- Score: 62 / 100 (weight 2, weighted gap 76)

**What it means:** The system is accessible when needed, with defined recovery objectives.

**Justification:**

- **Strengths:** Health checks (liveness, readiness, detailed JSON with circuit breaker state). RTO/RPO targets documented per environment tier. Database failover runbook. Container Apps revision-based deployment with rollback (`CD_ROLLBACK_ON_SMOKE_FAILURE`). Auto-failover group guidance for Azure SQL. Synthetic API probe (scheduled `curl` to `/health/live` and `/version`).
- **Weaknesses:** No **proven** HA deployment -- targets are documented as "guidance, not contractual SLAs." No multi-region active-active design. Single worker process with leader election is a SPOF for all async processing (outbox, advisory, indexing). No automated failover drill results documented. Synthetic probe checks `/health/live` and `/version` only -- not the core operator path. No SLO burn-rate alerting enforcement (Prometheus recording rules exist but operator-configured). No documented recovery procedure for partial outbox failure (dead letters exist but no automated re-drive).
- **Trade-off:** Documenting HA patterns and RTO/RPO targets is valuable but aspirational without proven drills. The single-leader worker design simplifies coordination but creates a bottleneck and SPOF.

**Improvements:**
1. Run and document a failover drill against staging with measured RTO and RPO, comparing results to documented targets.
2. Add multi-instance worker capability (distributed lock or queue-based partitioning) to eliminate the single-leader SPOF.
3. Extend synthetic probes to exercise the core operator path (create run, list runs, commit) beyond health endpoints.

---

### 18. Performance -- Score: 63 / 100 (weight 2, weighted gap 74)

**What it means:** The system responds within acceptable time bounds for interactive and batch operations.

**Justification:**

- **Strengths:** Hot-path read cache with configurable TTL. `ROWVERSION`-keyed explanation cache (avoids stale reads). Read replica routing for dashboard queries. Rate limiting prevents abuse-driven resource exhaustion. `ArchLucid.Benchmarks` project exists. Pagination on list endpoints. Agent execution concurrency gate limits parallel LLM work.
- **Weaknesses:** **No published latency baselines** (p50, p95, p99) for any endpoint. No load test in CI. Benchmarks project exists but no results are published or regression-tracked. `OFFSET/FETCH` pagination will degrade on deep pages of large tables (no keyset pagination fallback). LLM calls dominate run latency but have no documented latency target (timeout is configurable but no SLO). No response compression documented. No slow-query alerting or database query performance monitoring.
- **Trade-off:** Caching and read replicas improve typical-case read latency but add complexity (TTL staleness, invalidation coordination). Without baselines, there's no way to detect performance regressions.

**Improvements:**
1. Establish and publish p95 latency targets for the 5 most critical endpoints (runs list, run detail, commit, export, audit search).
2. Add a CI performance regression gate (k6 smoke test with p95 assertion against the baseline).
3. Implement keyset pagination for `dbo.Runs` and `dbo.AuditEvents` list endpoints to maintain performance on deep pages.

---

### 19. Testability -- Score: 77 / 100 (weight 3, weighted gap 69)

**What it means:** The system can be effectively and efficiently tested at all levels.

**Justification:**

- **Strengths:** 18 test projects with 795 test files. 78 UI test files (Vitest + Playwright). 5 Stryker mutation testing configurations. Simmy chaos tests (CI-blocking + weekly). SQL Server container integration tests. `WebApplicationFactory` integration tests. `DeterministicAgentSimulator` for reproducible agent behavior. `InMemory` storage provider for fast unit/integration tests without SQL. `ArchLucid.TestSupport` shared fixtures. OpenAPI contract snapshot test (drift detection). Architecture constraint tests (15 NetArchTest facts + source scan). JSON round-trip tests for persistence contracts. Finding payload codec tests. `ComparisonReplayTestFixture` for reusable flow helpers. Tiered CI with clear test-selection filters (`Suite=Core`, `Category=Integration`, `Category=SqlServerContainer`).
- **Weaknesses:** UI E2E is predominantly **mock-backed** (only one live API spec exercises real SQL). No property-based testing. Coverage gate is 70% merged line minimum -- above average but not aggressive for domain logic. No visual regression testing for UI. Schemathesis API fuzz testing runs in "light" mode. Some test projects have thin coverage. No per-project coverage gate (only solution-wide aggregate).
- **Trade-off:** Mock-backed E2E provides deterministic, fast CI results but may miss real integration bugs that only manifest with actual HTTP + SQL + serialization interactions.

**Improvements:**
1. Add per-project coverage thresholds (e.g., 85% for `Decisioning`, `Application`, `Core`; 70% for infrastructure projects).
2. Introduce property-based testing (FsCheck) for domain logic invariants (manifest merge, governance resolution, finding engine outputs).
3. Add full Schemathesis API fuzz coverage in a scheduled CI job (not just "light" mode).

---

### 20. Cost-Effectiveness -- Score: 67 / 100 (weight 2, weighted gap 66)

**What it means:** The system delivers value without unnecessary resource consumption.

**Justification:**

- **Strengths:** FinOps playbook referenced. Consumption budgets in Terraform for Container Apps and SQL failover. LLM token usage telemetry (`archlucid_llm_*` counters). Hot-path cache reduces redundant LLM calls and SQL queries. `InMemory` storage provider eliminates SQL cost for development. Rate limiting prevents abuse-driven cost spikes. `ReplayComparisonCostEstimator` provides relative cost bands before execution. Explanation cache with hit/miss metrics.
- **Weaknesses:** No runtime cost dashboard (LLM spend, SQL DTU, blob storage) -- token telemetry exists but no aggregation or alerting. No per-tenant cost attribution. Audit export 10K-row cap forces repeated API calls for large exports (cost amplification). No Azure cost anomaly alerting in Terraform. No documented per-run cost model for operators.
- **Trade-off:** Token telemetry infrastructure exists but is not surfaced in operator-facing dashboards, reducing day-to-day cost visibility.

**Improvements:**
1. Build a Grafana cost dashboard aggregating LLM token usage, SQL DTU, and blob storage metrics by tenant/workspace.
2. Document a per-run cost model (approximate LLM tokens, SQL operations, blob storage) so operators can forecast spend.
3. Add Azure cost anomaly alerting in the monitoring Terraform module.

---

### 21. Modularity -- Score: 79 / 100 (weight 3, weighted gap 63)

**What it means:** The system is composed of well-bounded, independently evolvable modules.

**Justification:**

- **Strengths:** 30+ .NET projects with explicit dependency rules. Persistence split into 6 sub-assemblies. Host composition separates domain DI from API concerns. NetArchTest enforces boundaries at 4 tiers. Finding engine template for custom extensions. Worker process decoupled from API. Config-gated feature modules. `ArchLucid.Contracts.Abstractions` separated from `ArchLucid.Contracts` for service port interfaces.
- **Weaknesses:** `ArchLucid.Application` is an oversized assembly containing runs, replay, comparison, export, governance, advisory, digest, alert, learning, and planning services. Dual repository interfaces in different assemblies for the same concept add unnecessary coupling complexity. No runtime module loading -- all modules are compile-time linked.
- **Trade-off:** More assemblies increase build time, project graph complexity, and CI duration. The current 30-project split is a reasonable balance but `Application` is overdue for subdivision.

**Improvements:**
1. Split `ArchLucid.Application` into subdomain assemblies (e.g., `Application.Runs`, `Application.Governance`, `Application.Advisory`) when team boundaries or build-time pressure justify it.
2. Extract comparison/replay services into a dedicated assembly to decouple them from core run orchestration.

---

### 22. Observability -- Score: 81 / 100 (weight 3, weighted gap 57)

**What it means:** The system's internal state can be understood from external outputs (metrics, logs, traces, health).

**Justification:**

- **Strengths:** OpenTelemetry with custom `ArchLucid` meter (15+ instruments: histograms, counters, observable gauges). 8 custom `ActivitySource` names registered. Serilog structured logging with correlation enrichment. Prometheus recording rules (`archlucid-slo-rules.yml`) and alert rules (`archlucid-alerts.yml`). 5 Grafana dashboard JSON templates committed to the repo (authority pipeline, SLO, LLM usage, Container Apps overview, run lifecycle). Business KPI metrics (runs created, findings produced by severity, LLM calls per run, explanation cache hit ratio). Azure Monitor Prometheus rule group in Terraform. Health endpoint hierarchy (live/ready/detailed with circuit breaker state). Synthetic API probe. Persisted trace IDs on runs. Per-agent output structural completeness and parse failure metrics.
- **Weaknesses:** No centralized log aggregation configuration shipped -- operators must configure Serilog sinks independently. No distributed tracing UI in the product (relies on external Jaeger/Tempo/Application Insights). No anomaly detection. No SLO burn-rate automation. No UI-side telemetry (no RUM/performance monitoring for the operator shell). No log-based alerting configuration shipped.
- **Trade-off:** Relying on external observability backends is appropriate for an enterprise product, but out-of-box visibility depends entirely on operator setup maturity.

**Improvements:**
1. Ship a `docker-compose` observability profile with Grafana + Tempo + Prometheus preconfigured for instant local visibility.
2. Add Real User Monitoring (RUM) to the operator UI for client-side performance telemetry and error tracking.

---

### 23. Manageability -- Score: 73 / 100 (weight 2, weighted gap 54)

**What it means:** The system can be efficiently operated and administered.

**Justification:**

- **Strengths:** CLI with `doctor` (configuration validation), `support-bundle` (diagnostic package), `trace` (trace viewer link), `status` (API health), `artifacts` (bundle inspection). Health endpoints with JSON detail. Admin APIs for integration outbox dead letters and retry. `IOptionsMonitor` hot reload for circuit breaker, rate limit, and cache configuration. Support bundle with manifest and triage catalog. 7+ runbooks covering failover, migration, advisory, replay, SLO, cert rotation, and data archival.
- **Weaknesses:** No admin UI -- all administrative operations require CLI or direct API calls. No bulk operations in the API (e.g., bulk archive, bulk retry dead letters, bulk policy pack assignment). No configuration validation UI. No operational dashboard in the product (Grafana is external).
- **Trade-off:** CLI-first admin is simpler to build and automate, but adds friction for operators who are not CLI-comfortable or need to delegate operations to less technical staff.

**Improvements:**
1. Add an admin section to the operator UI covering outbox dead letters, health status, and configuration review.
2. Implement bulk archive and bulk retry APIs for operational efficiency at scale.

---

### 24. Deployability -- Score: 76 / 100 (weight 2, weighted gap 48)

**What it means:** The system can be reliably deployed to target environments.

**Justification:**

- **Strengths:** Dockerfiles for API, UI, and Worker. `docker-compose` with profiles (full-stack local development). 8 Terraform roots covering all Azure infrastructure. GitHub Actions CI/CD (build -> Docker push -> Container Apps deploy -> post-deploy smoke). DbUp migrations on startup (forward-only). Post-deploy verification script (`cd-post-deploy-verify.sh`: health, readiness, OpenAPI, version, synthetic path). Rollback on smoke failure (`CD_ROLLBACK_ON_SMOKE_FAILURE`). Trivy scan on Docker images and Terraform config. `DEPLOYMENT_RUNBOOK.md` for operators.
- **Weaknesses:** No blue-green or canary deployment pattern documented. Terraform `state mv` for rename is deferred -- new deployments may encounter stale resource addresses. No Helm chart (Container Apps-specific only). No environment promotion automation (staging -> production is manual approval). CD pipeline targets Container Apps exclusively (no AKS, App Service, or bare-VM alternative).
- **Trade-off:** Container Apps focus simplifies the CD pipeline and aligns with the Azure-first design principle, but limits deployment target flexibility for organizations with different infrastructure preferences.

**Improvements:**
1. Document a canary or blue-green deployment pattern using Container Apps traffic splitting.
2. Add environment promotion automation (staging smoke pass -> auto-promote to production with manual approval gate).

---

### 25. Accessibility -- Score: 57 / 100 (weight 1, weighted gap 43)

**What it means:** The system is usable by people with disabilities.

**Justification:**

- **Strengths:** WCAG 2.1 AA stated target. `@axe-core/playwright` gates critical/serious violations in CI. 5 routes scanned (Home, Runs, Audit, Policy packs, Alerts). `eslint-plugin-jsx-a11y` linter. Contrast fixes applied. `aria-disabled` on pagination. `aria-live="polite"` on run progress tracker. Radix UI components (built-in focus trapping, keyboard handling). `ACCESSIBILITY.md` with expansion checklist.
- **Weaknesses:** Only **5 of ~25** UI routes have axe scans -- most of the UI is unverified. No screen reader testing documented. No keyboard-only navigation testing. No high-contrast mode. No reduced-motion support documented. No VPAT/ACR published. Many interactive components (graph viewer, provenance diagram, comparison views) are SVG-based without documented accessibility considerations.
- **Trade-off:** Axe-core catches structural WCAG violations automatically but does not test real usability (task completion using assistive technology).

**Improvements:**
1. Extend axe scans to all UI routes (at minimum: manifests, governance, advisory, digests, compare, replay, graph, onboarding, evolution, planning, search).
2. Add keyboard-only navigation testing for the core operator flow (create run, review manifest, export artifacts).

---

### 26. Extensibility -- Score: 75 / 100 (weight 1, weighted gap 25)

**What it means:** The system can be extended with new capabilities by third parties or future teams.

**Justification:**

- **Strengths:** Finding engine template (`dotnet new archlucid-finding-engine`). Integration events with JSON Schema catalog for downstream consumers. OpenAPI for API client generation. AsyncAPI for webhook consumers. Config-gated features enable opt-in without code changes. DI-based architecture makes implementation swapping straightforward. `ILlmProvider` abstraction allows new LLM backends. `IExportFormatters` for output format variety.
- **Weaknesses:** No plugin runtime -- extensions must be compiled into the host. No webhook subscription API. No custom report/export format extension point (export formatters exist but no documented extension mechanism). No marketplace or extension registry concept.
- **Trade-off:** Compile-time extension is simpler, more secure, and more debuggable than runtime plugin loading, but limits third-party extension without source access.

**Improvements:**
1. Document a formal extension developer guide covering: custom finding engines, custom LLM providers, and custom export formatters with DI registration instructions.

---

### 27. Documentation -- Score: 87 / 100 (weight 1, weighted gap 13)

**What it means:** The system is documented for all audiences (developers, operators, security, executives).

**Justification:**

- **Strengths:** 187 markdown files. Structured document index (`ARCHITECTURE_INDEX.md`). Role-based onboarding (`day-one-developer`, `day-one-sre`, `day-one-security`). C4 diagrams (context, container, component) with both PNG and Mermaid. 13 ADRs with rationale and status tracking. Glossary with 20+ domain terms. Changelog. Operator quickstart. Troubleshooting guide. 7+ runbooks (failover, migration, advisory, replay, SLO, cert rotation, archival). API contracts. Test structure. Dual pipeline navigator. DI registration map. `CSHARP_TO_REACT_ROSETTA.md`. V1 scope contract + release checklist + readiness summary. `docs/archive/` for historical context. Inline XML doc conventions. Security docs (`SECURITY.md`, `SYSTEM_THREAT_MODEL.md`, `ASK_RAG_THREAT_MODEL.md`, `MULTI_TENANT_RLS.md`).
- **Weaknesses:** Some docs reference stale names (rename in progress). `NEXT_REFACTORINGS.md` at 2,100+ lines is more a dumping ground than a curated backlog. No auto-generated API reference from XML doc comments. Some cross-doc links may be broken as files move.
- **Trade-off:** Comprehensive documentation requires ongoing maintenance; the risk is staleness over time. The volume itself can overwhelm newcomers if navigation is not clear.

**Improvements:**
1. Add a CI markdown link-checker to catch broken cross-references.

---

## Top six improvements (highest weighted impact across all dimensions)

| Priority | Improvement | Primary Qualities Improved (weights) | Rationale |
|----------|-------------|---------------------------------------|-----------|
| **1** | **Expand live-API E2E tests:** Add negative-path and governance-rejection Playwright specs against real C# API + SQL to CI. The current single happy-path live spec leaves most operator flows unvalidated against the real stack. | Correctness (8), Reliability (5), Usability (4) | The single largest weighted gap is Correctness at 240 points. The gap between mock-backed and real-stack UI behavior is the primary driver. Each additional live spec reduces this gap with compounding returns across reliability and usability confidence. **Estimated weighted impact: ~40 points.** |
| **2** | **Persist mandatory forensic prompt snapshots and build an agent output evaluation harness:** Store exact prompt text, model version, and raw response as a non-optional part of `AgentExecutionTrace`. Build a scoring harness that compares actual agent outputs against reference inputs with a rubric, tracking quality scores over time. | Explainability (6), AI/Agent Readiness (4), Traceability (7), Auditability (5) | Four high-weight dimensions benefit simultaneously. Without forensic prompt snapshots, agent outputs are not reproducible -- a critical gap for compliance, debugging, and quality regression detection. The evaluation harness enables data-driven prompt engineering instead of ad-hoc manual review. **Estimated weighted impact: ~35 points.** |
| **3** | **Run and publish a load-test baseline with CI regression gate:** Use k6 or similar to establish throughput and p95 latency baselines for the 5 most critical endpoints. Add a CI smoke test with p95 assertions. | Performance (2), Reliability (5), Scalability (3), Correctness (8) | Unknown throughput limits are a risk multiplier. Even a 60-second smoke test with 10 virtual users would establish whether the system can serve its documented operator path under minimal concurrency. Prevents performance regressions from landing undetected. **Estimated weighted impact: ~30 points.** |
| **4** | **Close audit coverage gaps and wire audit search UI:** Promote baseline mutation audit to durable SQL for high-severity events. Wire the existing `GET /v1/audit/search` API filters and capabilities into the operator UI audit page. | Auditability (5), Policy & Governance (5), Security (6), Usability (4) | The product acknowledges audit gaps; closing them for governance and export paths directly supports compliance pilots. The API already supports full search/filter -- the operator UI just needs wiring. This is high-leverage incremental work. **Estimated weighted impact: ~28 points.** |
| **5** | **Execute Terraform `state mv` (Phase 7.5) and triage `NEXT_REFACTORINGS.md`:** Schedule and complete the Terraform resource address rename. Aggressively prune the 2,100-line refactoring backlog to 20-30 curated, prioritized items. | Evolvability (6), Maintainability (4), Cognitive Load (4) | These are the two largest contributors to perceived technical debt and contributor friction. Phase 7.5 eliminates daily friction for every infrastructure change. A curated backlog signals intentionality and reduces cognitive load for newcomers trying to understand system health. **Estimated weighted impact: ~25 points.** |
| **6** | **Implement configurable pre-commit governance gate with severity thresholds:** Make the existing `GovernancePreCommitBlocked` gate configurable by finding severity (e.g., block on `Critical`, warn on `Error`, allow `Warning`/`Info`). Add approval SLA with notification escalation. | Policy & Governance (5), Correctness (8), Security (6) | Post-hoc compliance means non-compliant manifests can be committed. A configurable pre-commit gate gives governance teams a preventive control without slowing teams that opt out. Approval SLA prevents governance bottlenecks from blocking delivery indefinitely. **Estimated weighted impact: ~22 points.** |

---

## Methodology notes

- Scores reflect **evidence found in the repository** (source code, docs, CI config, Terraform, test projects), not claimed or planned capabilities.
- "Known gaps" that are **documented and acknowledged** (e.g., in `AUDIT_COVERAGE_MATRIX.md`, `V1_READINESS_SUMMARY.md`) receive partial credit -- the team's awareness and tracking reduce risk.
- Deferred items with **no timeline** receive less credit than items with a documented schedule.
- UI assessment is based on source code structure, component inventory (164 TSX files), and documentation -- not live user testing.
- This assessment is independent of the April 12 assessment in `QUALITY_ASSESSMENT.md`. Score differences reflect updated evidence and independent judgment, not disagreement with methodology.

**Overall weighted average: 70.3 / 100** -- a solid V1-stage product with exceptional documentation and observability, strong architectural guardrails, but meaningful gaps in live validation, AI forensics, performance baselines, and operational proof under load. The most impactful improvements are live E2E expansion, mandatory prompt forensics, and a load-test baseline -- together these would close approximately 105 weighted gap points across the highest-weight dimensions.
