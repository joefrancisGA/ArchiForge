# ArchLucid — Weighted Quality Assessment

**Date:** 2026-04-12  
**Basis:** Source code, documentation, CI configuration, Terraform modules, test projects, and architecture artifacts as of HEAD.  
**Methodology:** Each quality dimension is scored 1–100, multiplied by its weight. Sections are ordered by **weighted gap** (weight x (100 - score)) so the areas with the most impactful improvement opportunity appear first.

---

## Summary table

| # | Quality | Weight | Score | Weighted score | Weighted gap | Verdict |
|---|---------|--------|-------|----------------|--------------|---------|
| 1 | Correctness | 8 | 72 | 576 | 224 | Good foundations, gaps in live E2E |
| 2 | Architectural integrity | 7 | 80 | 560 | 140 | Strong layering, some historical splits |
| 3 | Traceability | 7 | 78 | 546 | 154 | Excellent correlation; some UI gaps |
| 4 | Security | 6 | 75 | 450 | 150 | Defense in depth present; gaps remain |
| 5 | Explainability | 6 | 73 | 438 | 162 | Trace coverage matrix, LLM-dependent |
| 6 | Evolvability | 6 | 74 | 444 | 156 | Good config gates; rename debt |
| 7 | Policy & governance alignment | 5 | 76 | 380 | 120 | Approval workflows, promotion ordering |
| 8 | Reliability | 5 | 71 | 355 | 145 | Resilience layers present; no HA proof |
| 9 | Data consistency | 5 | 74 | 370 | 130 | Documented matrix; archival cascades weak |
| 10 | Auditability | 5 | 72 | 360 | 140 | 66 event types; known gaps acknowledged |
| 11 | AI/Agent readiness | 4 | 70 | 280 | 120 | Simulator + real paths; prompt versioning early |
| 12 | Maintainability | 4 | 73 | 292 | 108 | Modular; NEXT_REFACTORINGS backlog long |
| 13 | Cognitive load | 4 | 68 | 272 | 128 | Rich docs offset by system complexity |
| 14 | Usability | 4 | 65 | 260 | 140 | Operator shell functional; limited UX polish |
| 15 | Testability | 3 | 78 | 234 | 66 | 18 test projects, mutation, chaos |
| 16 | Modularity | 3 | 80 | 240 | 60 | 30 projects, clear boundaries |
| 17 | Observability | 3 | 82 | 246 | 54 | OTel, Prometheus, Grafana, business KPIs |
| 18 | Interoperability | 3 | 71 | 213 | 87 | OpenAPI + AsyncAPI; limited inbound connectors |
| 19 | Scalability | 3 | 62 | 186 | 114 | Single-region design; read replicas partial |
| 20 | Deployability | 2 | 77 | 154 | 46 | Docker, Terraform, CD pipelines |
| 21 | Manageability | 2 | 74 | 148 | 52 | CLI + support bundle; admin surface thin |
| 22 | Availability | 2 | 63 | 126 | 74 | HA documented, not proven |
| 23 | Performance | 2 | 64 | 128 | 72 | Caching exists; no load-test baseline |
| 24 | Cost-effectiveness | 2 | 68 | 136 | 64 | FinOps docs; no runtime cost telemetry |
| 25 | Extensibility | 1 | 76 | 76 | 24 | Finding engine template; plugin model absent |
| 26 | Documentation | 1 | 88 | 88 | 12 | 210 markdown files; exceptional coverage |
| 27 | Accessibility | 1 | 58 | 58 | 42 | Axe gates on 5 routes; many routes uncovered |
| | **Totals** | **107** | | **7,641** | **2,688** | **Weighted average: 71.4 / 100** |

---

## Detailed assessment (ordered by weighted gap, highest first)

---

### 1. Correctness — Score: 72 / 100 (weight 8, gap 224)

**What it means:** The system produces correct results under documented conditions.

**Justification:**

- **Strengths:** 18 .NET test projects (~608 test source files), tiered CI (Tier 0–3b), OpenAPI contract snapshot tests that fail on drift, Stryker mutation testing at a 70% break threshold across 5 configurations, Simmy chaos tests in CI (blocking), integration tests against `WebApplicationFactory`, and SQL Server container tests.
- **Weaknesses:** UI E2E (Playwright) is largely **mock-backed** — the V1 readiness summary explicitly warns "do not treat it as SQL-backed UI proof." No end-to-end correctness test spans API + real SQL + real UI in CI. The `NEXT_REFACTORINGS.md` file lists **2,100+ lines** of known issues/improvements, several of which are correctness-relevant (e.g., missing replay validation tests). The rename introduced risk — 2,243 C# source files were touched across phases; while CI guards exist, subtle behavioral regressions from config/namespace changes are possible.
- **Trade-off:** Mock-backed UI tests run fast and deterministically but leave an API ↔ UI integration gap.

**Improvements:**
1. Add at least one CI job that runs Playwright against the real C# API with SQL (not mocks) for the happy-path operator journey.
2. Close the specific correctness gaps called out in `NEXT_REFACTORINGS.md` items 9 (replay validation test) and 10 (create-run-execute helper).
3. Raise Stryker break threshold from 70 to 80 for domain assemblies (`Decisioning`, `Application`).

---

### 2. Explainability — Score: 73 / 100 (weight 6, gap 162)

**What it means:** Decisions made by the system (especially AI/agent outputs) can be understood and audited by humans.

**Justification:**

- **Strengths:** `ExplainabilityTrace` on every `Finding` (5 fields, target 4/5 populated). `ExplainabilityTraceCompletenessAnalyzer` produces per-engine and overall ratios. OTel histogram `archlucid_explainability_trace_completeness_ratio`. Provenance graph in the UI. `GET /v1/explain/runs/{runId}/explain` and `/aggregate` endpoints with stakeholder narrative.
- **Weaknesses:** `AlternativePathsConsidered` intentionally empty for all rule-based engines (reserved for LLM-style). Explanation and Ask endpoints are fully LLM-dependent — when LLM is down, these return errors with no fallback summary. No explanation diffing between two runs (comparison explanation exists but is separate). Prompt reproducibility is early-stage (catalog + SHA-256, but no stored prompt snapshot per run for forensic replay).
- **Trade-off:** Deferring `AlternativePathsConsidered` keeps rule engines simple but leaves a gap in multi-branch decision justification.

**Improvements:**
1. Store the exact prompt text + model version alongside each agent execution trace so explanations are forensically reproducible.
2. Provide a cached or deterministic fallback explanation summary when LLM is unavailable (e.g., template-based from findings + manifest structure).
3. Populate `AlternativePathsConsidered` for at least the topology and compliance engines where branch logic exists.

---

### 3. Evolvability — Score: 74 / 100 (weight 6, gap 156)

**What it means:** The system can adapt to new requirements without disproportionate rework.

**Justification:**

- **Strengths:** Config-gated features (`ArchLucid:StorageProvider`, `HotPathCache:Enabled`, `IntegrationEvents:*`, `FallbackLlm:Enabled`). Clear phase-based rename checklist. ADRs document trade-off rationale. API versioning with deprecation headers. `schemaVersion` on integration event payloads. Finding engine template (`dotnet new archlucid-finding-engine`). Interfaces and DI throughout.
- **Weaknesses:** Rename phases 7.5–7.8 are deferred indefinitely (Terraform `state mv`, repo rename, Entra, workspace path) — every new feature must navigate stale resource addresses. `NEXT_REFACTORINGS.md` has ~90 numbered items, many non-trivial. Dual namespace in `ArchLucid.Persistence` (Data vs authority) adds friction for contributors extending persistence. No plugin/extension point system beyond finding engines.
- **Trade-off:** Config gates enable incremental rollout but accumulate combinatorial complexity (many optional features interact).

**Improvements:**
1. Schedule and execute Phase 7.5 (Terraform `state mv`) to eliminate the ongoing tax of stale resource addresses.
2. Triage `NEXT_REFACTORINGS.md` — close, defer-with-reason, or delete items that are no longer relevant; the 90-item backlog signals evolvability friction.
3. Introduce a lightweight plugin/extension manifest beyond the finding engine template (e.g., custom connectors, custom export formats).

---

### 4. Traceability — Score: 78 / 100 (weight 7, gap 154)

**What it means:** Every artifact or decision can be traced back to its origin.

**Justification:**

- **Strengths:** `X-Correlation-ID` on all requests, propagated to OTel spans and Serilog. Background job correlation (synthetic `correlation.id` for outbox, archival, integration events). `OtelTraceId` on run records linked to the UI. Activity sources for 8+ subsystems. CLI `trace <runId>` opens the trace viewer. Provenance graph (snapshots → findings → decisions → manifest → artifacts). Audit event types carry `RunId` / `ManifestId` / `ArtifactId`.
- **Weaknesses:** No end-to-end lineage view that connects a *user request* to *every downstream side effect* (integration events, webhooks, advisory scans spawned). UI provenance is coordinator-only; authority pipeline stage spans are OTel-only (no UI visualization). `AgentExecutionTrace` records exist but are not surfaced in the operator UI beyond the provenance graph.
- **Trade-off:** The current design prioritizes OTel-backend traceability (Jaeger/Tempo) over in-product trace visualization.

**Improvements:**
1. Surface authority pipeline stage durations and outcomes in the operator UI run detail (not just OTel backends).
2. Add a "trace explorer" page that shows the full DAG from request → run → integration events → downstream consumers.
3. Link audit events to their originating OTel trace ID for cross-channel lookup.

---

### 5. Security — Score: 75 / 100 (weight 6, gap 150)

**What it means:** The system protects data confidentiality, integrity, and availability against threats.

**Justification:**

- **Strengths:** Three auth modes (DevelopmentBypass, JwtBearer, ApiKey) with role-based policies. SQL RLS with `SESSION_CONTEXT` (defense in depth). Private endpoint Terraform modules. Gitleaks in CI (Tier 0). CodeQL analysis. Trivy image scan + Terraform config scan. Production config safety rules (no wildcard CORS, HMAC on webhooks). `DENY UPDATE/DELETE` on audit table. HMAC-signed webhook delivery. Threat models under `docs/security/`. CycloneDX SBOM generation.
- **Weaknesses:** API key auth mode uses static shared secrets (not rotatable without restart until `IOptionsMonitor`). No documented secret rotation runbook for API keys. RLS is on "covered tables" only — child tables without denormalized scope columns are not protected. `DevelopmentBypass` could accidentally reach production if `ASPNETCORE_ENVIRONMENT` is misconfigured (startup rule mitigates but depends on correct env tagging). No penetration test results referenced. No CSP headers documented for the UI.
- **Trade-off:** RLS adds latency per connection (`SESSION_CONTEXT` set); partial coverage means some cross-tenant queries on uncovered tables rely solely on application-layer enforcement.

**Improvements:**
1. Add a secret rotation runbook and implement `IOptionsMonitor` for API key hot-reload so rotation does not require restart.
2. Extend RLS to cover additional tables that carry tenant scope columns (or document the explicit exclusion list with risk acceptance).
3. Add Content-Security-Policy headers to the Next.js UI and document CSP configuration.

---

### 6. Reliability — Score: 71 / 100 (weight 5, gap 145)

**What it means:** The system operates correctly under adverse conditions and recovers gracefully.

**Justification:**

- **Strengths:** Polly retry + circuit breaker on LLM calls (per-call retry inside CB, configurable thresholds). LLM model-level fallback (`FallbackAgentCompletionClient`). SQL connection open retries. Simmy chaos tests in CI (blocking). Degraded-mode documentation. Transactional outbox for integration events. Health checks (liveness + readiness + data archival). `CircuitBreakerGate` with `IOptionsMonitor` hot reload.
- **Weaknesses:** No load test or soak test in CI — reliability under sustained load is untested. Archival cascade is incomplete (child snapshots not always cascaded). No automated failover drill in CI. The weekly Simmy cron is a "second line of defense" that tests the same filter as CI — no additional coverage. No chaos testing for the UI proxy layer. No documented SLA/SLO enforcement in production (Prometheus rules exist but no automated SLO-violation alerting on the actual deployment).
- **Trade-off:** Chaos tests validate individual resilience primitives but do not test systemic failure scenarios (e.g., simultaneous LLM + SQL degradation).

**Improvements:**
1. Add a k6/Locust load-test baseline to CI (even a short smoke) to catch regression in throughput and error rates under concurrency.
2. Implement an automated failover drill script that runs against staging monthly and records RTO/RPO.
3. Add chaos tests for combined failure scenarios (SQL + LLM both degraded simultaneously).

---

### 7. Auditability — Score: 72 / 100 (weight 5, gap 140)

**What it means:** All significant actions are recorded and retrievable for compliance review.

**Justification:**

- **Strengths:** 66 `AuditEventTypes` constants in `ArchLucid.Core`. CI guard (count anchor) forces developers to update the matrix when types change. Durable SQL audit (`dbo.AuditEvents`) with append-only enforcement (`DENY UPDATE/DELETE`). Bulk export API with CSV + JSON, 90-day windows, rate limiting. Baseline mutation audit channel (structured logging). Circuit breaker audit bridge (fire-and-forget).
- **Weaknesses:** Known gaps in audit coverage acknowledged in `AUDIT_COVERAGE_MATRIX.md` and `V1_DEFERRED.md` — "some mutating flows do not emit `dbo.AuditEvents`." Baseline mutation audit goes to logs only, not durable SQL — a log pipeline failure loses that evidence. No audit UI for search/filter in the operator shell (API exists at `GET /v1/audit/search` but UI coverage is basic list only). No tamper-evident mechanism on SQL audit rows beyond `DENY` (no cryptographic chaining or hash).
- **Trade-off:** Fire-and-forget circuit breaker audit avoids hot-path latency but means some audit rows may be lost under extreme SQL pressure.

**Improvements:**
1. Close the remaining audit coverage gaps for mutating flows identified in the matrix (prioritize governance and export paths).
2. Promote baseline mutation audit from structured logging to durable SQL (`dbo.AuditEvents`) for at least the highest-severity mutations.
3. Add audit search/filter UI to the operator shell (the API endpoint exists; wire it to the `/audit` page).

---

### 8. Usability — Score: 65 / 100 (weight 4, gap 140)

**What it means:** The system is easy and efficient for operators to use.

**Justification:**

- **Strengths:** Operator shell with runs list, detail, manifest summary, artifact review, compare, replay, graph, governance dashboard. `OperatorFirstRunWorkflowPanel` guides new operators. `OperatorTryNext` consistent copy across pages. CLI with `doctor`, `support-bundle`, `trace`, `status`, `artifacts`. Confirmation dialogs for destructive actions (Radix Alert Dialog). Loading/empty/error states documented.
- **Weaknesses:** UI is described as a "thin shell" — functional but not polished. 278 TS/TSX source files but limited interactive feedback (no toast/notification system beyond inline messages). No dark mode. Limited keyboard navigation documentation. No user preferences or customizable views. The operator must understand both coordinator and authority pipelines — dual pipeline complexity leaks into the UI. No progressive disclosure for advanced features. No guided troubleshooting workflow in the UI (relies on CLI `doctor` and docs).
- **Trade-off:** "Thin shell" approach minimizes UI maintenance but caps operator productivity.

**Improvements:**
1. Add a toast/notification system for async operation feedback (run completion, governance approvals).
2. Implement progressive disclosure — hide advanced features (provenance graph, replay modes) behind expandable sections.
3. Add an in-app guided troubleshooting flow that surfaces `doctor` checks and health endpoint results.

---

### 9. Architectural integrity — Score: 80 / 100 (weight 7, gap 140)

**What it means:** The system's structure faithfully reflects its intended design principles.

**Justification:**

- **Strengths:** 30 .NET projects with clear layering (Core → Contracts → Application → Persistence → Api). NetArchTest enforcement in CI (15 facts, `Suite=Core`). 13 ADRs documenting key decisions. Dual pipeline navigator doc. DI registration map. C4 diagrams (context, container, component). Host composition separates domain wiring from API concerns. Persistence split (Data, Coordination, Runtime, Integration, Advisory, Alerts) for bounded contexts. Architecture constraint tests enforce Tier 1–4 dependency rules.
- **Weaknesses:** `ArchLucid.Persistence` namespace hosts both workflow data access and authority persistence — the doc acknowledges "two namespaces inside one assembly" as historical. Dual manifest/trace repository interfaces (`IGoldenManifestRepository` exists in both `Decisioning.Interfaces` and `Persistence.Data.Repositories`) add conceptual overhead. The `NEXT_REFACTORINGS.md` item 11 notes that contracts service-interface split is deferred. Some controllers are large (e.g., `ArchitectureController` covers most `/v1/architecture/*` endpoints).
- **Trade-off:** Keeping persistence in one assembly avoids project proliferation but violates single-responsibility at the assembly level.

**Improvements:**
1. Split the dual `IGoldenManifestRepository` into explicitly named interfaces (e.g., `IAuthorityManifestRepository` vs `ICoordinatorManifestRepository`) to eliminate the fully-qualified-type-name registration workaround.
2. Break `ArchitectureController` into focused controllers per subdomain (runs, comparisons, replay, diagnostics).
3. Complete the persistence assembly split — separate `ArchLucid.Persistence.Data` into its own assembly.

---

### 10. Data consistency — Score: 74 / 100 (weight 5, gap 130)

**What it means:** Data remains accurate, complete, and coherent across all paths.

**Justification:**

- **Strengths:** `DATA_CONSISTENCY_MATRIX.md` explicitly documents consistency guarantees per aggregate. `ROWVERSION` for optimistic concurrency on runs and key tables. `IArchLucidUnitOfWork` for transactional authority writes. Transactional outbox for integration events. Hot-path cache invalidation on write paths with `OUTPUT` scope columns. Read-replica staleness expectations documented. Dual persistence retired (ADR 0012 complete).
- **Weaknesses:** Archival cascade is incomplete — child snapshots are not always cascaded in the same SQL statement. "Application-enforced consistency" for coordinator artifacts means referential integrity depends on code correctness, not database constraints. Hot-path cache has TTL-bound staleness risk for changes outside documented write paths. `TransactionScope` is not used — `IArchLucidUnitOfWork` is the standard, but cross-repository atomicity for new features requires discipline.
- **Trade-off:** Application-enforced referential integrity avoids FK cascade complexity but opens a window for orphaned rows if code paths are missed.

**Improvements:**
1. Add database-level FK constraints (or a scheduled reconciliation job) for coordinator artifacts → `dbo.Runs` to prevent orphaned records.
2. Implement archival cascade for child snapshots within the same SQL transaction as run archival.
3. Add a data consistency health check that periodically validates referential integrity between authority and coordinator tables.

---

### 11. Cognitive load — Score: 68 / 100 (weight 4, gap 128)

**What it means:** How much mental effort is needed to understand and work with the system.

**Justification:**

- **Strengths:** Exceptional documentation effort (210 markdown files). `START_HERE.md` as a single entry point. `GOLDEN_PATH.md` with role-based lanes. `DUAL_PIPELINE_NAVIGATOR.md` for the hardest concept. `ONBOARDING_HAPPY_PATH.md` traces one request. `CSHARP_TO_REACT_ROSETTA.md` for cross-stack developers. Glossary with 20 domain terms. Week-one tickets per role. `CODE_MAP.md` for where to open first.
- **Weaknesses:** The system has two pipelines (coordinator + authority), two manifest repository interfaces with the same name, a 90-item refactoring backlog, 30 .NET projects, 8 Terraform roots, and a multi-phase rename still in progress. A new developer must understand: DI composition, config gates, dual pipelines, RLS, OTel, Stryker, Simmy, DbUp, and the rename checklist. The `ARCHITECTURE_COMPONENTS.md` file itself warns about dual namespace confusion. The rename history adds noise (bridge code, legacy keys, allowlists).
- **Trade-off:** Thorough documentation compensates somewhat, but the inherent domain + technical complexity is high.

**Improvements:**
1. Create a visual "system map poster" (single Mermaid diagram) that shows all 30 projects, their dependencies, and which pipeline they belong to.
2. Consolidate rename artifacts — remove bridge code and legacy fallback documentation once phases 7.5–7.8 complete.
3. Add a "contributor decision tree" to `START_HERE.md`: "Adding a new finding engine? → go here. Adding a new API endpoint? → go here. Changing persistence? → go here."

---

### 12. AI/Agent readiness — Score: 70 / 100 (weight 4, gap 120)

**What it means:** How well the system supports AI/agent-driven workflows and evolves with AI capabilities.

**Justification:**

- **Strengths:** `IAgentCompletionClient` / `ILlmProvider` abstraction with descriptor metadata. `DeterministicAgentSimulator` for tests. `FallbackAgentCompletionClient` for model failover. Circuit breaker + retry on LLM calls. Agent prompt versioning (catalog + SHA-256). `AgentExecutionTrace` with repro fields. Concurrency gate (bulkhead). Token usage telemetry. Finding engine template for custom engines.
- **Weaknesses:** Prompt snapshots are not stored per-run (catalog + SHA exists, but the exact prompt text sent is not persisted for forensic replay). No A/B testing framework for prompt variants. No structured evaluation/scoring harness for agent output quality. No support for streaming responses. `AlternativePathsConsidered` empty for all engines. No multi-model orchestration (single primary + single fallback only). No agent memory or context window management strategy documented.
- **Trade-off:** Deterministic simulator enables fast test cycles but means real LLM behavior is only tested manually or in staging.

**Improvements:**
1. Persist exact prompt text + model response per agent execution for forensic replay and quality regression detection.
2. Build a structured agent output evaluation harness (expected vs actual findings, scoring rubric).
3. Add streaming support for the Ask endpoint to improve perceived latency for interactive use.

---

### 13. Policy & governance alignment — Score: 76 / 100 (weight 5, gap 120)

**What it means:** The system enforces organizational policies and governance workflows.

**Justification:**

- **Strengths:** Governance workflow: approval → promotion → environment activation with segregation-of-duties enforcement (`GovernanceSelfApprovalBlocked`). Policy packs with versioning and assignment. Effective governance merge at run time. Compliance drift trend API. `GovernanceEnvironmentOrder` for promotion ordering (dev → test → prod only). Governance audit events (durable). Compliance rule packs. `GET /v1/governance/compliance-drift-trend`.
- **Weaknesses:** No policy-as-code integration (e.g., OPA/Rego). Governance UI is a dashboard — no inline approval workflow in the run detail flow. No SLA/deadline enforcement on pending approvals (approvals can hang indefinitely). No delegation or escalation model. No policy pack impact analysis (what would change if a pack is applied retroactively). Compliance findings are post-hoc, not preventive (no pre-commit policy gate that blocks a run).
- **Trade-off:** Post-hoc compliance keeps the run pipeline fast but means non-compliant manifests can be committed and must be caught later.

**Improvements:**
1. Add an optional pre-commit governance gate that blocks commit when critical policy violations exist.
2. Implement approval SLA with configurable escalation (auto-escalate after N hours).
3. Integrate with an external policy engine (OPA) for organization-specific rules beyond the built-in rule packs.

---

### 14. Scalability — Score: 62 / 100 (weight 3, gap 114)

**What it means:** The system handles increasing load without degradation.

**Justification:**

- **Strengths:** Read replica routing for list/dashboard queries. Hot-path cache (`IHotPathReadCache`) for single-row reads. Rate limiting with three policies (fixed, expensive, replay). Pagination on run and alert lists. Transactional outbox decouples event publishing. Worker process for async background work. Container Apps with revision-based scaling.
- **Weaknesses:** No load test baseline — throughput limits are unknown. Single SQL Server primary for all writes. No sharding or partitioning strategy documented. LLM calls are a serial bottleneck per agent handler (concurrency gate helps but is process-local, not distributed). No horizontal scaling guidance for the API beyond "deploy more revisions." Audit export is capped at 10,000 rows/90-day windows — large tenants need many sequential calls. No queue-based work distribution for agent execution across nodes.
- **Trade-off:** The architecture is vertically scalable (bigger SQL, more Container Apps replicas) but not horizontally partitioned.

**Improvements:**
1. Run a load test (k6 or similar) and publish baseline throughput numbers for the core operator path.
2. Document a horizontal scaling architecture for multi-node agent execution (queue-based distribution).
3. Add SQL table partitioning guidance for `dbo.AuditEvents` and `dbo.Runs` for high-volume tenants.

---

### 15. Maintainability — Score: 73 / 100 (weight 4, gap 108)

**What it means:** How easy it is to fix bugs, add features, and keep the system healthy.

**Justification:**

- **Strengths:** Modular project structure (30 projects). `.editorconfig` + `dotnet format`. XML doc conventions. Central package management (`Directory.Packages.props`). `FORMATTING.md` for style. Test trait system (Suite, Category). Stryker mutation gates. Architecture constraint tests. CI rename guards (C# + TS grep). DI registration map.
- **Weaknesses:** `NEXT_REFACTORINGS.md` is 2,100+ lines — a maintenance backlog this large signals accumulated technical debt. Dual manifest repository names require fully-qualified DI registration. The rename checklist has been running for 8+ days with 7.5–7.8 indefinitely deferred. Some test projects have thin coverage (e.g., `ArchLucid.Provenance.Tests`, `ArchLucid.Retrieval.Tests`). `ArchitectureController` is a large controller handling many concerns.
- **Trade-off:** Documenting debt is better than hiding it, but the backlog size may discourage contributors.

**Improvements:**
1. Triage and prune `NEXT_REFACTORINGS.md` — remove completed items, defer-with-date items that are truly deferred, and prioritize the top 10.
2. Split `ArchitectureController` into focused controllers to improve maintainability of the API surface.
3. Add coverage gates per test project (not just solution-wide) to prevent thin-coverage projects from hiding behind aggregate numbers.

---

### 16. Interoperability — Score: 71 / 100 (weight 3, gap 87)

**What it means:** The system works with other systems and standards.

**Justification:**

- **Strengths:** OpenAPI (two generators: Microsoft + Swashbuckle). AsyncAPI 2.6 for webhooks. CloudEvents envelope for webhook delivery. Integration events via Azure Service Bus with JSON Schema catalog. HMAC-signed webhooks. `X-Correlation-ID` header convention. CSV + JSON export formats. Bruno collection for manual testing. SARIF report format (gitleaks). CycloneDX SBOM.
- **Weaknesses:** No inbound connector/adapter framework (ingestion is code-only, not configurable). No GraphQL or gRPC surface. No webhook subscription management API (webhook URLs are config-only). No standard event mesh integration beyond Service Bus. Integration event consumer is logging-only by default — no out-of-box handlers for common targets (Slack, Teams, email). No SCIM or external identity sync.
- **Trade-off:** CloudEvents + Service Bus cover many enterprise patterns, but the lack of inbound connectors limits integration with existing architecture tools.

**Improvements:**
1. Add a webhook subscription management API so consumers can register/unregister dynamically.
2. Build at least one reference integration event handler (e.g., Teams/Slack notification on run completion).
3. Consider a lightweight inbound connector interface for importing architecture artifacts from common tools (Structurizr, Draw.io, etc.).

---

### 17. Availability — Score: 63 / 100 (weight 2, gap 74)

**What it means:** The system is accessible when needed.

**Justification:**

- **Strengths:** Health checks (liveness + readiness + data archival + dual-persistence reconciliation). RTO/RPO targets documented per tier (dev < staging < production). Database failover runbook. Container Apps revision-based deployment. Auto-failover group guidance for SQL. Synthetic API probe (scheduled `curl`).
- **Weaknesses:** No proven HA deployment — targets are documented as "guidance, not contractual SLAs." No multi-region active-active design. Single worker process (leader-elected outbox) is a SPOF for async processing. No automated failover drill results documented. Synthetic probe only checks `/health/live` and `/version` — not the full operator path. No SLO burn-rate alerting in the deployed Prometheus rules (recording rules exist but enforcement is operator-configured).
- **Trade-off:** Documenting HA patterns is valuable, but without proven drills, the stated RTO < 1 hour / RPO < 5 minutes is aspirational.

**Improvements:**
1. Run and document a failover drill against staging with measured RTO/RPO.
2. Add multi-instance worker support (or document the blast radius of single-leader failure).
3. Extend synthetic probes to cover the core operator path (create run, list runs, not just health).

---

### 18. Performance — Score: 64 / 100 (weight 2, gap 72)

**What it means:** The system responds within acceptable time bounds.

**Justification:**

- **Strengths:** Hot-path read cache with configurable TTL. ROWVERSION-keyed explanation cache (avoids stale reads). Read replica routing for dashboard queries. Rate limiting prevents abuse. `ArchLucid.Benchmarks` project exists. Pagination on list endpoints. Agent execution concurrency gate.
- **Weaknesses:** No published latency baselines (p50, p95, p99) for any endpoint. No load test in CI. Benchmarks project exists but no results are published or tracked. `OFFSET/FETCH` pagination may degrade on deep pages (no keyset pagination fallback). LLM calls dominate latency but have no documented target (timeout configurable but no SLO). No response compression documented. No database query performance monitoring (no slow-query alerting).
- **Trade-off:** Caching improves read latency but adds complexity (TTL staleness, invalidation paths).

**Improvements:**
1. Establish and publish p95 latency targets for key endpoints (runs list, run detail, commit, export).
2. Add a CI performance regression gate (k6 smoke test with assertion on p95).
3. Implement keyset pagination as an alternative to OFFSET/FETCH for deep page navigation on large tables.

---

### 19. Testability — Score: 78 / 100 (weight 3, gap 66)

**What it means:** The system can be effectively tested at all levels.

**Justification:**

- **Strengths:** 18 test projects, ~608 test source files. 78 UI test files (Vitest + Playwright). 5 Stryker configs (mutation testing). Simmy chaos tests (CI-blocking + weekly scheduled). SQL Server container tests. `WebApplicationFactory` integration tests. `DeterministicAgentSimulator`. `InMemory` storage provider for fast tests. `ArchLucid.TestSupport` shared fixtures. OpenAPI contract snapshot test. Architecture constraint tests (NetArchTest). JSON round-trip tests for contracts. Finding payload codec tests.
- **Weaknesses:** UI E2E is mock-backed (not testing real API integration). No property-based testing (e.g., FsCheck). Coverage gate is 70% merged line minimum — above average but not aggressive. No visual regression testing for UI. Schemathesis API fuzz testing is "light" in CI. Some test projects may have thin coverage (no per-project gate).
- **Trade-off:** Mock-backed E2E gives deterministic results but may miss real integration bugs.

**Improvements:**
1. Add per-project coverage thresholds (not just solution-wide 70%) to prevent coverage holes.
2. Introduce property-based testing for domain logic (findings, governance resolution, manifest merge).
3. Add API fuzz testing with full Schemathesis coverage in a scheduled CI job (beyond the "light" mode).

---

### 20. Cost-effectiveness — Score: 68 / 100 (weight 2, gap 64)

**What it means:** The system delivers value without unnecessary resource consumption.

**Justification:**

- **Strengths:** FinOps playbook referenced in docs. Consumption budgets in Terraform (Container Apps, SQL failover). LLM token usage telemetry. Hot-path cache reduces redundant LLM calls. InMemory storage provider eliminates SQL cost for dev. Rate limiting prevents abuse-driven cost spikes. `ReplayComparisonCostEstimator` provides relative cost bands before execution.
- **Weaknesses:** No runtime cost dashboard (LLM spend, SQL DTU, blob storage) — token telemetry exists but no aggregation or alerting. No per-tenant cost attribution. Audit export 10K-row cap means operators may hammer the API for large exports (cost amplification). No Azure cost anomaly alerting in Terraform. No documented cost model for operators (e.g., "each run costs approximately X LLM tokens").
- **Trade-off:** Token telemetry exists but isn't surfaced in operator dashboards, reducing cost visibility.

**Improvements:**
1. Build a cost dashboard (Grafana) that aggregates LLM token usage, SQL DTU, and blob storage by tenant.
2. Document a per-run cost model so operators can forecast spend.
3. Add Azure cost anomaly alerting in the monitoring Terraform module.

---

### 21. Modularity — Score: 80 / 100 (weight 3, gap 60)

**What it means:** The system is composed of well-bounded, independently evolvable modules.

**Justification:**

- **Strengths:** 30 .NET projects with clear layering. Persistence split into 6 sub-assemblies (Data, Coordination, Runtime, Integration, Advisory, Alerts). Host composition separates domain DI from API concerns. NetArchTest enforces dependency rules. Finding engine template for custom extensions. Separate Worker project for async processing. Config-gated feature modules.
- **Weaknesses:** `ArchLucid.Application` is a large project (runs, replay, comparison, export, governance, advisory, digest, alert services all in one assembly). `ArchitectureController` spans multiple concerns. Dual repository interfaces in different assemblies for the same concept. No runtime module loading (all modules are compile-time linked).
- **Trade-off:** More assemblies increase build time and project management overhead; the current split balances granularity and practicality.

**Improvements:**
1. Consider splitting `ArchLucid.Application` into subdomain assemblies (Runs, Governance, Advisory) when team boundaries emerge.
2. Extract comparison/replay services into a dedicated assembly.

---

### 22. Observability — Score: 82 / 100 (weight 3, gap 54)

**What it means:** The system's internal state can be understood from external outputs.

**Justification:**

- **Strengths:** OpenTelemetry with custom meter (`ArchLucid`, 15+ instruments). 8 custom `ActivitySource` names. Serilog structured logging with correlation. Prometheus recording rules and alert rules. Grafana dashboard JSON templates. Business KPI metrics (runs created, findings produced, LLM calls per run, explanation cache hit ratio). Azure Monitor Prometheus rule group (Terraform). Synthetic API probe. Health endpoint hierarchy (live/ready/all).
- **Weaknesses:** No centralized log aggregation config shipped (operators must configure sinks). No distributed tracing UI in the product (relies on external Jaeger/Tempo). No anomaly detection. No SLO burn-rate automation. Limited UI-side telemetry (no RUM/performance monitoring for the operator shell).
- **Trade-off:** Relying on external observability backends (Prometheus, Grafana, Tempo) is appropriate for an enterprise product but means out-of-box visibility depends on operator setup.

**Improvements:**
1. Ship a `docker-compose` profile with Grafana + Tempo + Prometheus preconfigured for instant local observability.
2. Add RUM (Real User Monitoring) to the operator UI for client-side performance telemetry.

---

### 23. Manageability — Score: 74 / 100 (weight 2, gap 52)

**What it means:** The system can be operated and administered efficiently.

**Justification:**

- **Strengths:** CLI (`doctor`, `support-bundle`, `trace`, `status`, `artifacts`). Health endpoints. Admin APIs (integration outbox dead letters, retry). Configuration hot-reload via `IOptionsMonitor`. Support bundle with manifest and triage catalog. Runbooks for failover, migration, advisory, replay, SLO.
- **Weaknesses:** No admin UI (all admin operations are API/CLI-only). No bulk operations in the API (e.g., bulk archive, bulk retry). No configuration validation UI. No operational dashboard in the product (relies on external Grafana).
- **Trade-off:** CLI-first admin is simpler to build but adds friction for non-CLI-comfortable operators.

**Improvements:**
1. Add an admin section to the operator UI for outbox dead letters, health status, and configuration review.
2. Implement bulk archive and bulk retry APIs for operational efficiency.

---

### 24. Deployability — Score: 77 / 100 (weight 2, gap 46)

**What it means:** The system can be reliably deployed to target environments.

**Justification:**

- **Strengths:** Dockerfiles for API and UI. `docker-compose` with profiles (full-stack). 8 Terraform roots covering edge, APIM, SQL failover, Container Apps, monitoring, storage, private endpoints, Entra, Service Bus, OpenAI. GitHub Actions CI/CD (build → push → deploy → smoke). DbUp migrations on startup. Post-deploy verification script. `DEPLOYMENT_RUNBOOK.md` for operators. `CONTAINERIZATION.md` with image security notes. Trivy scan on Docker images and Terraform config.
- **Weaknesses:** No blue-green or canary deployment documented. Terraform `state mv` for rename is deferred — new deployments may hit stale resource addresses. No helm chart (Container Apps–specific). No environment promotion automation (staging → production is manual). CD pipeline only targets Container Apps (no AKS, App Service alternative).
- **Trade-off:** Container Apps focus simplifies the CD pipeline but limits deployment target flexibility.

**Improvements:**
1. Document a canary or blue-green deployment pattern for Container Apps.
2. Add environment promotion automation (staging smoke → auto-promote to production with approval gate).

---

### 25. Accessibility — Score: 58 / 100 (weight 1, gap 42)

**What it means:** The system is usable by people with disabilities.

**Justification:**

- **Strengths:** WCAG 2.1 AA stated target. `@axe-core/playwright` gates critical/serious violations. 5 routes scanned (Home, Runs, Audit, Policy packs, Alerts). `eslint-plugin-jsx-a11y`. Contrast fixes applied. `aria-disabled` on pagination. `aria-live="polite"` on run progress tracker. Radix UI components (focus trapping, keyboard handling). `ACCESSIBILITY.md` with expansion checklist.
- **Weaknesses:** Only 5 of ~25 routes have axe scans. No screen reader testing documented. No keyboard-only navigation testing. No high-contrast mode. No reduced-motion support documented. No VPAT/ACR published. Accessibility is weight-1, reflecting its current priority, but the 5-route coverage means most of the UI is unverified.
- **Trade-off:** Axe catches structural violations but does not test usability (task completion by assistive technology users).

**Improvements:**
1. Extend axe scans to all routes (at least 15 more).
2. Add keyboard-only navigation testing for the core operator flow.

---

### 26. Extensibility — Score: 76 / 100 (weight 1, gap 24)

**What it means:** The system can be extended with new capabilities by third parties or future teams.

**Justification:**

- **Strengths:** Finding engine template (`dotnet new archlucid-finding-engine`). Integration events with JSON Schema catalog for downstream consumers. OpenAPI for API clients. AsyncAPI for webhook consumers. Config-gated features allow opt-in without code changes. DI-based architecture makes swapping implementations straightforward. `ILlmProvider` abstraction allows new LLM backends.
- **Weaknesses:** No plugin runtime (extensions must be compiled into the host). No webhook subscription API (config-only). No custom report/export format extension point. No marketplace or extension registry concept.
- **Trade-off:** Compile-time extension is simpler and more secure than runtime plugin loading but limits third-party extension without source access.

**Improvements:**
1. Add a custom export format extension point (register additional `IExportFormatter` implementations via DI).

---

### 27. Documentation — Score: 88 / 100 (weight 1, gap 12)

**What it means:** The system is well-documented for all audiences.

**Justification:**

- **Strengths:** 210 markdown files. Structured index (`ARCHITECTURE_INDEX.md`). Role-based onboarding (developer, SRE, security). `START_HERE.md` → `GOLDEN_PATH.md` → role-specific tickets. C4 diagrams (PNG + Mermaid). 13 ADRs. Glossary. Changelog. Operator quickstart. Troubleshooting guide. Runbooks (7+). API contracts. Test structure. Dual pipeline navigator. DI registration map. `CSHARP_TO_REACT_ROSETTA.md`. V1 scope contract + release checklist + readiness summary. `docs/archive/` for historical context.
- **Weaknesses:** Some docs reference stale names (rename in progress). `NEXT_REFACTORINGS.md` is 2,100+ lines — more a dumping ground than a curated backlog. No auto-generated API reference from XML docs. Some cross-links may be broken as files move.
- **Trade-off:** Comprehensive documentation is expensive to maintain; the risk is staleness over time.

**Improvements:**
1. Add a CI link-checker for markdown cross-references.

---

## Top six improvements (highest impact across all dimensions)

| Priority | Improvement | Primary qualities improved | Expected impact |
|----------|-------------|---------------------------|-----------------|
| **1** | **Add a live-API E2E test in CI** (Playwright against real C# API + SQL for the operator happy path) | Correctness (8), Usability (4), Reliability (5) | Closes the biggest single evidence gap — today's mock-backed E2E cannot prove the system works end-to-end. Weighted quality impact: ~40 points. |
| **2** | **Persist exact prompt text + model response per agent execution** and build a structured output evaluation harness | Explainability (6), AI/Agent readiness (4), Traceability (7), Auditability (5) | Enables forensic replay of AI decisions, quality regression detection, and regulatory evidence for AI-generated outputs. Weighted quality impact: ~35 points. |
| **3** | **Run and publish a load-test baseline** (k6 or similar, even a short smoke in CI with p95 assertions) | Performance (2), Reliability (5), Scalability (3), Correctness (8) | Unknown throughput limits are a risk multiplier across multiple dimensions. Even a 60-second smoke test with 10 virtual users would establish a regression baseline. Weighted quality impact: ~30 points. |
| **4** | **Close audit coverage gaps** for mutating flows + add audit search UI to operator shell | Auditability (5), Policy & governance (5), Security (6), Usability (4) | The product acknowledges gaps; closing them — especially for governance and export paths — directly supports compliance pilots. The API already exists; wiring the UI is incremental. Weighted quality impact: ~28 points. |
| **5** | **Triage and prune `NEXT_REFACTORINGS.md`** + complete Phase 7.5 (Terraform `state mv`) | Evolvability (6), Maintainability (4), Cognitive load (4) | A 2,100-line backlog and indefinitely deferred rename phases are the top contributors to perceived complexity and contributor hesitation. Weighted quality impact: ~25 points. |
| **6** | **Add a pre-commit governance gate** (optional, blocks commit when critical policy violations exist) | Policy & governance (5), Correctness (8), Security (6) | Post-hoc compliance means non-compliant manifests can be committed. An optional pre-commit gate gives governance teams a preventive control without slowing teams that don't need it. Weighted quality impact: ~22 points. |

---

## Methodology notes

- Scores reflect **evidence found in the repository** (code, docs, CI config, Terraform), not claimed capabilities.
- "Known gaps" that are **documented and acknowledged** receive partial credit (the team is aware and has a plan).
- Deferred items with no timeline receive less credit than items with a documented schedule.
- UI assessment is based on source code structure and documentation, not live user testing.

**Overall weighted average: 71.4 / 100** — a solid V1-stage product with strong documentation and architecture, but meaningful gaps in live validation, AI forensics, and operational proof under load.
