> Archived 2026-04-23 — superseded by [docs/START_HERE.md](../START_HERE.md) and the current assessment pair under ``docs/``. Kept for audit trail.

> **Scope:** ArchLucid Weighted Quality Assessment — 2026-04-14 - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# ArchLucid Weighted Quality Assessment — 2026-04-14

**Methodology:** Each quality dimension is scored 1–100 based on evidence from the repository (code, tests, docs, CI/CD, Terraform, UI). Dimensions are ordered by **weighted improvement priority** (weight x gap-from-100), so the areas that matter most and have the most room to grow appear first.

**Total weighted score:** see summary table at end.

---

## Scoring Legend

| Range | Meaning |
|-------|---------|
| 90–100 | Excellent — best-in-class, minor polish only |
| 75–89 | Strong — solid foundation, targeted gaps |
| 60–74 | Adequate — functional but material gaps exist |
| 45–59 | Weak — significant improvement needed |
| Below 45 | Critical — blocking or high-risk gaps |

---

## Assessments (ordered by weighted improvement priority)

### 1. Correctness — Score: 68 / 100 (Weight: 8, Weighted Gap: 256)

**Justification:**
- The solution compiles and CI enforces full regression (Tier 2) with SQL Server, xUnit, and chaos tests (Tier 2b). OpenAPI contract snapshot tests catch accidental API drift.
- Line coverage gate is **74%** with branch coverage at **57%** — below the project's own stated aspiration of 100%.
- Stryker mutation score baseline is **65%** with a target ratchet to 72%, meaning ~35% of mutations survive — a significant fraction of logical paths lack meaningful correctness assertions.
- Idempotency on run creation is described as "best-effort under extreme duplicate-key races" (Data Consistency Matrix), signaling known correctness gaps under concurrency.
- Some coordinator artifact cascades are application-enforced with acknowledged gaps: "downstream rows may remain until a dedicated cleanup job or future migration."
- No formal property-based testing framework beyond FsCheck for ExplainabilityTrace; broader domain invariants (governance state machine, alert dedup, run lifecycle) lack property tests.

**Tradeoffs:** Higher mutation coverage is expensive in CI minutes (Stryker is already scheduled, not per-PR). Coverage floors balance speed vs. safety.

**Improvement Recommendations:**
1. Raise line coverage to 80% and branch coverage to 65% — target the lowest-covered assemblies first (Persistence sub-modules, AgentRuntime).
2. Expand property-based tests (FsCheck) to governance state transitions, alert deduplication, and run lifecycle invariants.
3. Add concurrency/race-condition tests for idempotency key collisions using parallel test harnesses.
4. Raise Stryker baseline to 72% per the existing ratchet plan, then target 78%.

---

### 2. Traceability — Score: 72 / 100 (Weight: 7, Weighted Gap: 196)

**Justification:**
- Strong provenance model: `ArchLucid.Provenance` project with `ProvenanceBuilder`, `ProvenanceNode`, `ProvenanceEdge`, `ProvenanceCompletenessAnalyzer`, and graph algorithms. Dedicated `ProvenanceController` and `ProvenanceQueryController` API surfaces.
- `ExplainabilityTrace` on every `Finding` records `GraphNodeIdsExamined`, `RulesApplied`, `DecisionsTaken`, `AlternativePathsConsidered`, `Notes` — completeness measured by `ExplainabilityTraceCompletenessAnalyzer` with OTel metric.
- `dbo.Runs.OtelTraceId` persists W3C trace ID at creation for post-hoc distributed trace lookup (CLI `trace` command, UI deep link).
- Agent execution traces with blob storage + SQL inline fallback for full prompt/response forensics.
- **Gaps:** `AlternativePathsConsidered` stays empty for all rule-based engines (reserved for LLM-style). Coordinator artifact orphan detection is detection-only (no remediation). Run archival cascade is incomplete — child snapshot/manifest rows may linger. No formal requirements traceability matrix (requirements → tests → code).

**Tradeoffs:** Full requirements traceability matrices are expensive to maintain for an evolving product. The provenance graph provides decision-level traceability that most competitors lack.

**Improvement Recommendations:**
1. Populate `AlternativePathsConsidered` for at least the top 2 LLM-backed engines.
2. Add orphan remediation (not just detection) for coordinator artifacts after run archival.
3. Create a lightweight requirements → test mapping for V1 scope items.

---

### 3. Architectural Integrity — Score: 74 / 100 (Weight: 7, Weighted Gap: 182)

**Justification:**
- Clean layered architecture enforced by **NetArchTest** (`DependencyConstraintTests`): Core and Contracts are verified leaves; Decisioning, KnowledgeGraph, ContextIngestion, ArtifactSynthesis, and CLI are hexagonally isolated from Persistence. Persistence sub-module boundaries (Coordination, Integration, Runtime, Advisory, Alerts) are assembly-reference tested.
- `IArchLucidUnitOfWork` pattern with Dapper for transactional consistency — well-structured, no Entity Framework.
- Integration event publishing is architecturally guarded: a source-scan test prevents direct `PublishAsync` calls outside authorized wrappers.
- `Contracts.Abstractions` split separates service ports from DTOs.
- **Gaps:** ~~Dual solution files and orphan `ArchiForge/` trees~~ — **addressed 2026-04-15** (Improvement 3): repo root keeps **`ArchLucid.sln` only**; stray **`ArchiForge/`** stub sources removed; VS Code/REST **`ArchLucid.Api.http`**. `Host.Composition` has only `GlobalUsings.cs` visible (DI registration logic may be spread). The rename leaves Terraform resource addresses as `archiforge` (deferred 7.5). Architecture on a Page is solid but the C4 diagram is a simple flowchart, not a formal C4 model.

**Tradeoffs:** The rename is incremental by design (one batch per session). Removing legacy directories risks breaking ongoing branches. Formal C4 requires tooling adoption.

**Improvement Recommendations:**
1. Remove or archive the legacy `ArchiForge.*` directories to eliminate cognitive confusion.
2. Create formal C4 model files (Structurizr DSL or similar) beyond the Mermaid approximation.
3. Consolidate to a single `.sln` file once the rename is stable.
4. Document the Host.Composition DI registration graph more explicitly (the `DI_REGISTRATION_MAP.md` exists but may not reflect current state).

---

### 4. Explainability — Score: 73 / 100 (Weight: 6, Weighted Gap: 162)

**Justification:**
- Dedicated `ExplanationController` with aggregate explanation summaries, per-run explanations, and explanation faithfulness checking (token overlap heuristic with OTel metric).
- `ExplainabilityTrace` is a first-class citizen on every `Finding` — rare in enterprise architecture tools.
- Provenance graph visualization in UI (`ProvenanceGraphDiagram`) with layered SVG, click-to-scroll, and type/color legend.
- `AgentOutputEvaluationRecorder` scores structural completeness and semantic quality of agent outputs.
- **Gaps:** Faithfulness is token-overlap only — embedding-based faithfulness is explicitly deferred to V2+. Aggregate faithfulness fallback (deterministic text) fires when LLM narrative scores low, but the heuristic is coarse. No user-facing "why did this decision happen" narrative for individual findings beyond trace fields. The explanation system does not yet support multi-language explanations.

**Tradeoffs:** Embedding-based faithfulness adds cost, latency, and PII review burden. Token overlap is a reasonable MVP signal.

**Improvement Recommendations:**
1. Add a user-facing narrative builder for individual findings that composes `ExplainabilityTrace` fields into readable text.
2. Implement the embedding-based faithfulness checker behind a feature flag for staging validation.
3. Add faithfulness trend alerting (Prometheus rule on `archlucid_explanation_faithfulness_ratio` p10 drop).

---

### 5. Security — Score: 70 / 100 (Weight: 6, Weighted Gap: 180)

**Justification:**
- Defense in depth: default-deny on controllers, Entra ID / JWT / API key modes, SQL RLS with `SESSION_CONTEXT`, log injection mitigation (`LogSanitizer`), OWASP ZAP baseline in CI and weekly, Schemathesis API fuzzing, CodeQL with custom log-sanitizer models, Gitleaks secret scanning (Tier 0), Trivy image + IaC scanning.
- Private endpoints for storage; no public SMB (port 445) — explicit workspace rule enforced.
- STRIDE threat model referenced (`SYSTEM_THREAT_MODEL.md`). API key rotation with comma-separated overlap documented. Database-level `DENY UPDATE/DELETE` on audit events.
- **Gaps:** RLS is documented as having "residual risk" (template acceptance doc exists). `DevelopmentBypass` auth mode exists for local dev — if misconfigured in production, it's a full bypass. No DAST in the per-PR pipeline (ZAP is weekly + CI-gate, but Schemathesis is scheduled-only). No explicit RBAC model beyond `ReadAuthority` — role granularity is coarse. Self-approval segregation of duties exists for governance but not uniformly across all admin operations. Some audit coverage gaps documented in `AUDIT_COVERAGE_MATRIX.md`. PII retention for conversation threads needs explicit policy.

**Tradeoffs:** Full DAST per PR would add 15–30 min to CI. DevelopmentBypass enables rapid local iteration. Coarse RBAC simplifies initial deployment.

**Improvement Recommendations:**
1. Add a startup guard that prevents `DevelopmentBypass` mode in production environments (fail-fast with an environment variable check).
2. Move Schemathesis fuzzing into per-PR CI (even a quick subset).
3. Define granular RBAC roles (ReadOnly, Operator, Admin, Auditor) with per-controller authorization policies.
4. Close the documented audit coverage gaps for all mutating governance and admin operations.

---

### 6. Evolvability — Score: 68 / 100 (Weight: 6, Weighted Gap: 192)

**Justification:**
- Plugin-like architecture with `Contracts.Abstractions` for cross-cutting service ports. Feature flags (`FeatureManagementAuthorityPipelineModeResolver`). Configuration bridges for the rename enable old and new keys simultaneously.
- Modular persistence split (6 sub-modules: base, Advisory, Alerts, Coordination, Integration, Runtime) — clean seams for future decomposition.
- Agent runtime designed for multi-vendor LLM (`ILlmProvider`, `LlmProviderDescriptor`, fallback chain). Prompt versioning with SHA-256 catalogs.
- **Gaps:** The rename-in-progress leaves a messy dual-directory structure that makes evolution harder. No API versioning beyond `/v1/` prefix (no `/v2/` path or header-based versioning strategy documented — ADR 0013 exists but implementation unclear). The 40+ legacy `ArchiForge.*` directories are dead weight. No plugin or extension point mechanism for custom finding engines or connectors. Database schema is forward-only migrations with DbUp — schema rollback requires manual intervention.

**Tradeoffs:** Extension points add complexity before product-market fit is proven. DbUp forward-only is simpler than EF migrations with rollback.

**Improvement Recommendations:**
1. Define and implement an `IFindingEngine` plugin interface for custom/customer engines.
2. Add API versioning strategy (header-based or URL-based for `/v2/`) with deprecation policy.
3. Complete the rename cleanup to unblock clean evolution.
4. Add database migration rollback scripts for at least the last 5 migrations.

---

### 7. Policy and Governance Alignment — Score: 72 / 100 (Weight: 5, Weighted Gap: 140)

**Justification:**
- Rich governance model: approval requests, manifest promotions, environment activation, segregation of duties (self-approval blocked with audit), dry-run mode, governance preview (manifest diff).
- Policy packs with assignments, versioning, applicability engines, and coverage engines.
- Effective governance resolution service documented.
- **Gaps:** No formal policy-as-code integration (OPA/Rego or similar). Governance workflow does not support multi-level approval chains (single approver). No governance SLA enforcement (time-to-approve tracking). Policy pack creation/management is API-only — no UI workflow documented. No compliance framework mappings (SOC 2, ISO 27001 control references).

**Tradeoffs:** Multi-level approvals and policy-as-code add significant complexity. For V1/pilot, single-approver with segregation of duties is defensible.

**Improvement Recommendations:**
1. Add compliance framework control mappings to policy packs (SOC 2, ISO 27001 control IDs).
2. Implement time-to-approve SLA tracking with alert rules.
3. Design a multi-approver workflow for production promotions.

---

### 8. Reliability — Score: 74 / 100 (Weight: 5, Weighted Gap: 130)

**Justification:**
- Polly resilience: circuit breakers (completion, embedding, fallback), SQL connection retry with exponential backoff, CLI HTTP retry, agent execution bulkhead + timeout. All configurable at runtime via `IOptionsMonitor`.
- Chaos testing with Simmy (SQL + HTTP transient faults, LLM latency, combined failure shapes) — CI-blocking.
- Health checks: `/health/live`, `/health/ready`, `/health` (detailed with circuit breaker state). Run-golden-manifest consistency health check. Data consistency orphan probe.
- Transactional outbox for integration events with dead-letter and admin retry.
- LLM model fallback (`FallbackAgentCompletionClient`) for multi-deployment resilience.
- **Gaps:** No formal SLA targets documented for the API (API_SLOS.md exists but targets are aspirational). Read-replica staleness "usually under 5 seconds" is not bounded by an SLO. No explicit RTO/RPO validation in CI — documented in runbooks but not automated. Failover drill is manual. No retry on audit event persistence failures (fire-and-forget for circuit breaker audit).

**Tradeoffs:** Automated failover drills require dedicated infrastructure. Fire-and-forget audit on the hot path is a deliberate latency vs. completeness tradeoff.

**Improvement Recommendations:**
1. Define and enforce concrete SLIs/SLOs (p99 latency, availability %) with Prometheus alerting rules.
2. Add automated RTO/RPO validation as a scheduled CI job (database failover simulation).
3. Add async retry queue for failed audit event persistence.

---

### 9. Data Consistency — Score: 76 / 100 (Weight: 5, Weighted Gap: 120)

**Justification:**
- Explicit Data Consistency Matrix with clear guarantees per aggregate (transactional, outboxed, eventual).
- `IArchLucidUnitOfWork` for transactional authority SQL — no `TransactionScope` in product code (verified repo-wide scan).
- `ROWVERSION`-based optimistic concurrency on `dbo.Runs` and selected tables → 409 on conflicts.
- Hot-path read cache with invalidation on documented write paths and TTL backstop.
- Read-replica routing with documented staleness expectations.
- **Gaps:** Archival cascade is incomplete — child snapshot/manifest rows may remain after parent archival. Orphan detection is detection-only (no auto-remediation). Cache invalidation does not cover writes outside repository methods (ad-hoc SQL, future writers). No eventual consistency convergence SLO (how long until outbox items are processed?).

**Tradeoffs:** Auto-remediation of orphans risks data loss if the detection logic has false positives. Cascade completeness is a V2 concern.

**Improvement Recommendations:**
1. Add cascade logic for child snapshots/manifest rows during run archival.
2. Define outbox processing latency SLO and add alerting.
3. Add orphan auto-remediation with dry-run + confirmation workflow.

---

### 10. Auditability — Score: 75 / 100 (Weight: 5, Weighted Gap: 125)

**Justification:**
- Durable SQL audit (`dbo.AuditEvents`) with append-only enforcement (`DENY UPDATE/DELETE` on `ArchLucidApp` role). 78 typed audit event constants with CI guard (count anchor in markdown, validated in CI).
- Paginated list, filtered search, and bulk export (JSON/CSV with 90-day windows). Retention policy documented (hot/warm/cold tiers).
- Baseline mutation audit (structured ILogger). Governance dual-write (durable + baseline channels).
- Circuit breaker audit bridge with fire-and-forget design.
- **Gaps:** Documented known gaps in audit coverage for some mutating flows. No automated archival of old audit rows (operator-initiated only). No immutability/WORM enforcement in Terraform for audit export blobs. CLI does not yet wrap the export endpoint. No audit trail for configuration changes or feature flag toggles. No tamper-evident verification (hash chain or similar).

**Tradeoffs:** WORM storage requires subscription-level Azure Policy alignment. Hash chains add write latency. Audit CLI is low-priority vs. API-first.

**Improvement Recommendations:**
1. Close remaining audit coverage gaps (see `AUDIT_COVERAGE_MATRIX.md` known gaps).
2. Add Terraform-managed immutable blob storage for audit exports.
3. Add configuration change auditing for security-relevant settings.

---

### 11. Cognitive Load — Score: 65 / 100 (Weight: 4, Weighted Gap: 140)

**Justification:**
- Extensive documentation: 193+ markdown files in `docs/`, ADRs, runbooks, onboarding guides, architecture index, code map, contributor onboarding, glossary.
- `START_HERE.md`, `CODE_MAP.md`, `ARCHITECTURE_INDEX.md` provide navigation aids.
- Test execution model is well-structured with named suites and clear CI mapping.
- **Gaps:** 193+ docs is itself a cognitive load problem — no clear reading order or "which 5 docs do I read first?" The dual `ArchiForge.*` / `ArchLucid.*` directory structure is deeply confusing for newcomers (40+ stale directories). The solution has 50+ projects — high project count for a V1 product. Navigation between 50 API controllers is challenging. **Mitigated (2026-04-15):** superseded quality snapshots live under **`docs/archive/`** with short stubs at the old paths; **canonical** assessment is **`docs/QUALITY_ASSESSMENT_2026_04_14_WEIGHTED.md`** (see **`docs/CURSOR_PROMPTS_WEIGHTED_IMPROVEMENT_3.md`** § `archive-superseded-quality-assessments`).

**Tradeoffs:** Comprehensive docs prevent knowledge silos but require curation. Many projects enable fine-grained dependency control but increase navigation cost.

**Improvement Recommendations:**
1. Create a "First 5 Docs" page that links the essential reading in priority order.
2. Remove or archive stale `ArchiForge.*` directories.
3. ~~Consolidate or archive superseded quality assessment files.~~ **Done (2026-04-15):** see **`docs/archive/`** + stubs + **`docs/CURSOR_PROMPTS_WEIGHTED_IMPROVEMENT_3.md`**.
4. Add controller grouping or API area documentation to navigate 50 controllers.

---

### 12. AI/Agent Readiness — Score: 66 / 100 (Weight: 4, Weighted Gap: 136)

**Justification:**
- Agent runtime with multi-vendor LLM seam (`ILlmProvider`, `IAgentCompletionClient`), prompt versioning (SHA-256 catalog), agent output structural/semantic scoring, quality gates, and reference-case evaluation.
- Agent execution traces with full prompt/response blob persistence + SQL inline fallback.
- Four agent types: Topology, Cost, Compliance, Critic — with per-agent-type metrics.
- Agent simulator for testing without real LLM calls.
- **Gaps:** No agent memory or conversation context across runs (each run is stateless). No prompt A/B testing framework. Prompt regression is **partially** automated (**Topology** golden floors in **`prompt_regression_baseline.json`** + **`PromptRegressionBaselineContractTests`** + **`assert_prompt_regression.py`**); Cost/Compliance/Critic baselines remain **0.0** until fixtures land. **IContentSafetyGuard** scaffolding exists; **no** Azure Content Safety client is wired yet (stub fails closed when **`Enabled: true`**). No multi-turn agent capabilities. No model performance comparison tooling.

**Tradeoffs:** Stateless agents are simpler to reason about and scale. Guardrails add latency. A/B testing requires traffic splitting infrastructure.

**Improvement Recommendations:**
1. ~~Enable quality gates by default~~ **Done (2026-04-15):** base **`appsettings.json`** **`QualityGate:Enabled`** **true**; **`appsettings.Development.json`** opts out.
2. ~~Add automated prompt regression detection~~ **Partially done:** Topology golden floors + CI script policy + **`PromptRegressionBaselineContractTests`**; extend to Cost/Compliance/Critic when golden fixtures exist.
3. Add content safety guardrails (**Azure AI Content Safety** or equivalent client wired into **`IContentSafetyGuard`** — scaffolding only today).
4. Design multi-turn agent capability for iterative architecture refinement.

---

### 13. Maintainability — Score: 71 / 100 (Weight: 4, Weighted Gap: 116)

**Justification:**
- Strong code conventions: `.editorconfig`, concrete types over `var`, LINQ preference, null checks, one class per file, modular methods.
- Comprehensive test infrastructure: `TestSupport` project with unit-of-work test doubles, test fixture management, skippable tests for optional infrastructure.
- Scripts for local and CI parity: `test-fast-core.cmd`, `test-full.cmd`, `test-ui-unit.cmd`, etc.
- Dependabot for dependency updates. CycloneDX SBOM generation.
- **Gaps:** Stale dual-directory structure increases maintenance burden. Multiple overlapping docs on the same topic (quality assessments, test structure). No automated code quality metrics dashboard (SonarQube or similar). Rename bridge code adds maintenance cost — sunset timeline not enforced.

**Tradeoffs:** Bridge code enables gradual migration without breaking existing deployments. Code quality dashboards add CI infrastructure cost.

**Improvement Recommendations:**
1. Set a hard sunset date for configuration bridges (e.g., 2026-07-01) and enforce via startup warnings.
2. Consolidate overlapping documentation files.
3. Consider adding SonarQube or similar for automated code quality trending.

---

### 14. Usability — Score: 64 / 100 (Weight: 4, Weighted Gap: 144)

**Justification:**
- Operator UI (Next.js/React) with governance workflow, provenance graph visualization, run progress tracker (`aria-live`), confirmation dialogs (Radix), keyboard shortcuts.
- CLI with comprehensive commands: create, compare, replay, export, trace, doctor, support-bundle.
- API with OpenAPI spec, health endpoints, version endpoint, admin diagnostics.
- **Gaps:** UI documentation references components but no screenshot-based walkthrough exists in the repo. No first-run wizard implementation (doc exists: `FIRST_RUN_WIZARD.md` — design only). CLI does not wrap audit export. No bulk operations UI (batch approval, batch archival). Demo/onboarding requires manual API key setup. No dark mode toggle (though Radix supports it). Error messages may not be consistently user-friendly across all 50 controllers.

**Tradeoffs:** Screenshot-based docs rot quickly. First-run wizards are significant UX investment. Bulk operations add API complexity.

**Improvement Recommendations:**
1. Implement the first-run wizard from the existing design doc.
2. Add consistent error message formatting across all controllers (RFC 9457 Problem Details everywhere).
3. Add bulk operations for common admin tasks (batch archival, batch approval review).

---

### 15. Observability — Score: 82 / 100 (Weight: 3, Weighted Gap: 54)

**Justification:**
- Comprehensive OTel instrumentation: 30+ custom metrics (histograms, counters, gauges), 8 custom activity sources, trace tags, W3C trace propagation.
- Business-level KPI metrics: runs created, findings by severity, LLM calls per run, explanation cache hit ratio, agent output quality scores.
- Grafana dashboards committed in repo (authority, SLO, LLM usage, container apps, run lifecycle).
- Prometheus recording rules and SLO rules in Terraform. Prometheus alert rules for outbox depth and circuit breakers.
- Sampling strategy documented with head-based vs. tail-based guidance.
- Health JSON includes circuit breaker state for operational triage without Prometheus.
- **Gaps:** No centralized log aggregation configuration (Serilog sinks are configured but aggregation backend is operator-choice). No distributed tracing visualization tool bundled (operator must configure). No automated anomaly detection on metrics. Sampling ratio for production authority runs requires collector-level config.

**Improvement Recommendations:**
1. Add anomaly detection rules for key metrics (authority pipeline duration drift, finding count anomalies).
2. Bundle a default Grafana provisioning Terraform module for quick-start observability.

---

### 16. Testability — Score: 73 / 100 (Weight: 3, Weighted Gap: 81)

**Justification:**
- Well-structured test suites: Core, Fast Core, Integration, SQL Server, Full Regression, UI Unit, UI E2E Smoke, Chaos. Named traits enable precise filtering.
- 815 test files across 17+ test projects. Property-based tests (FsCheck). Architecture constraint tests (NetArchTest).
- Live E2E tests against real API + SQL (DevelopmentBypass, ApiKey, JWT modes). k6 performance smoke (merge-blocking).
- `TestSupport` project with reusable test doubles.
- **Gaps:** 74% line coverage / 57% branch coverage is below the 100% aspiration. Stryker at 65% baseline. No contract tests for integration boundaries (Pact or similar). No visual regression testing for UI. Live E2E JWT mode is `continue-on-error` (not merge-blocking). Test data builders or object mothers are not consistently used across test projects.

**Improvement Recommendations:**
1. Introduce contract testing (Pact) for API client / API boundary.
2. Add visual regression testing for key UI pages (Playwright screenshots).
3. Standardize test data builders across all test projects.

---

### 17. Modularity — Score: 77 / 100 (Weight: 3, Weighted Gap: 69)

**Justification:**
- 26+ ArchLucid.* projects with clear responsibility boundaries. Persistence split into 6 sub-modules. Contracts separated from Abstractions.
- NetArchTest enforces boundaries at compile time. Assembly-reference tests for persistence sub-modules.
- Hexagonal isolation: domain projects (Decisioning, KnowledgeGraph, ContextIngestion, ArtifactSynthesis) verified independent of persistence.
- **Gaps:** Some feature modules (e.g., advisory, alerts, governance) span multiple projects without a clear bounded context boundary document. The 50+ controller files in `ArchLucid.Api` suggest the API layer could benefit from area-based modularization. The `Host.Composition` project's DI graph is not visually documented.

**Improvement Recommendations:**
1. Group API controllers into area folders (Governance, Authority, Advisory, Admin, etc.).
2. Document bounded context boundaries with a context map diagram.

---

### 18. Interoperability — Score: 68 / 100 (Weight: 3, Weighted Gap: 96)

**Justification:**
- OpenAPI v1 spec with contract snapshot testing (drift detection in CI). REST API with JSON.
- Integration events with canonical `com.archlucid.*` CloudEvents types. Service Bus optional. Webhook support.
- CLI for automation. API client project (`ArchLucid.Api.Client`) for .NET consumers.
- CSV and JSON audit export. DOCX export for consulting templates. ZIP artifact export.
- **Gaps:** No GraphQL or gRPC alternatives. No webhook signature verification documented. No event schema registry (schema evolution is ad-hoc). No SDK for non-.NET consumers (Python, JavaScript). API versioning strategy is underdeveloped (only `/v1/`). No SCIM or directory sync integration. No SSO federation beyond Entra ID.

**Improvement Recommendations:**
1. Publish event schemas to a schema registry (e.g., Azure Schema Registry).
2. Add webhook signature verification (HMAC-SHA256).
3. Generate API client SDKs for Python and JavaScript from the OpenAPI spec.

---

### 19. Scalability — Score: 66 / 100 (Weight: 3, Weighted Gap: 102)

**Justification:**
- Container Apps deployment with consumption-based scaling. Read-replica routing for read-heavy paths. Hot-path caching with Redis or memory provider.
- SQL indexing documented (`SQL_INDEX_INVENTORY.md`, `SQL_TOP5_QUERY_PLANS.md`). Outbox pattern for async processing.
- Agent execution bulkhead (semaphore) for concurrency control.
- **Gaps:** No horizontal partitioning / sharding strategy for SQL. No queue-based autoscaling configuration documented. k6 soak test is `continue-on-error` and not merge-blocking. No load test baseline beyond the smoke (5 VUs, 60s). Rate limiting exists but capacity modeling is aspirational. No CDN configuration for UI static assets documented. Blob storage used for agent traces but no lifecycle policy for cleanup.

**Improvement Recommendations:**
1. Implement blob lifecycle policies for agent trace storage (auto-tier/delete after retention period).
2. Add a proper load test suite with realistic traffic patterns (beyond 5 VU smoke).
3. Document horizontal scaling boundaries and when to shard.

---

### 20. Deployability — Score: 76 / 100 (Weight: 2, Weighted Gap: 48)

**Justification:**
- Dockerfiles for API and UI. Docker compose profiles. CI/CD workflows (ci.yml, cd.yml, cd-staging-on-merge.yml).
- Post-deploy verification: health checks, OpenAPI, version, synthetic smoke. Rollback via Container Apps revision deactivation.
- Terraform modules for 10+ Azure resource categories. DbUp migrations for database schema.
- Release scripts: `run-readiness-check.ps1`, `release-smoke.ps1`, `package-release.ps1`.
- **Gaps:** No blue-green or canary deployment strategy documented. No infrastructure-as-code for the CI/CD pipeline itself (GitHub Actions config is YAML, not Terraform). No deployment slot management. Worker app rollback requires secret configuration. No automated database migration rollback.

**Improvement Recommendations:**
1. Add canary deployment support with traffic splitting in Container Apps.
2. Add database migration rollback scripts for the last N migrations.

---

### 21. Manageability — Score: 70 / 100 (Weight: 2, Weighted Gap: 60)

**Justification:**
- Runbooks for 10+ operational scenarios (database failover, API key rotation, agent execution failures, advisory scan failures, alert delivery, data archival, infrastructure ops, Redis health, secret rotation, trace-a-run).
- Admin controller with diagnostics. CLI `doctor` and `support-bundle` commands.
- Configuration is runtime-reloadable for circuit breakers. Feature flags for pipeline modes.
- **Gaps:** No centralized configuration management UI. No operational dashboard beyond Grafana (no admin panel in the operator UI for system health overview). No automated capacity planning recommendations. Data archival coordinator exists but audit archival is not yet included.

**Improvement Recommendations:**
1. Add a system health overview page to the operator UI.
2. Extend data archival coordinator to include audit events.

---

### 22. Availability — Score: 69 / 100 (Weight: 2, Weighted Gap: 62)

**Justification:**
- SQL failover group Terraform module. Container Apps with multi-revision support. Health checks for liveness and readiness.
- RTO/RPO targets documented. Geo-failover drill runbook exists.
- Degraded mode documentation describes graceful degradation paths.
- **Gaps:** No multi-region active-active deployment configuration. Failover drill is manual (not automated in CI). No chaos engineering in production (only in test). No SLA guarantee documented (aspirational only). Read-replica failover behavior during primary outage not explicitly tested.

**Improvement Recommendations:**
1. Automate geo-failover drill as a scheduled CI workflow.
2. Document and test read-replica behavior during primary outage.

---

### 23. Performance — Score: 67 / 100 (Weight: 2, Weighted Gap: 66)

**Justification:**
- k6 API smoke test in CI (merge-blocking, 5 VUs, p95 ≤ 2000ms). k6 soak test (scheduled, not blocking).
- Hot-path read caching (memory/Redis) for runs, golden manifests, policy packs, explanation summaries.
- SQL query plan documentation (`SQL_TOP5_QUERY_PLANS.md`). SQL index inventory.
- Benchmarks project (`ArchLucid.Benchmarks`) exists.
- **Gaps:** p95 ≤ 2000ms target is generous for an API. No p99 targets. Soak test is not merge-blocking. No performance regression detection (automated comparison of k6 results across commits). Benchmarks project exists but integration into CI is unclear. No client-side performance budget for UI.

**Improvement Recommendations:**
1. Tighten p95 target to ≤ 500ms for non-pipeline endpoints and add p99 targets.
2. Add automated performance regression detection by comparing k6 results across commits.

---

### 24. Cost-Effectiveness — Score: 70 / 100 (Weight: 2, Weighted Gap: 60)

**Justification:**
- Consumption budget Terraform module for Container Apps and SQL failover. Capacity and cost playbook documented. AI Search SKU guidance.
- LLM completion caching to reduce Azure OpenAI consumption. Explanation cache (hit/miss metrics).
- Container Apps consumption-based pricing by default.
- **Gaps:** No cost alerting in Terraform beyond budget. No FinOps tagging strategy documented. No per-tenant cost attribution. LLM token usage metrics exist but no cost-per-run calculation. No cost optimization recommendations engine.

**Improvement Recommendations:**
1. Add Azure cost alerting at 80% and 100% of budget thresholds.
2. Implement per-run LLM cost tracking (token count × model pricing).

---

### 25. Extensibility — Score: 70 / 100 (Weight: 1, Weighted Gap: 30)

**Justification:**
- Interface-driven design (`IAgentCompletionClient`, `IRunRepository`, `IAuditService`, etc.) — all major services are behind interfaces.
- Finding engines are polymorphic (9+ engine types). Agent types are extensible.
- Integration events enable downstream consumers. Webhook support.
- **Gaps:** No formal plugin API or extension registry. No custom connector framework. No marketplace or extension catalog. Finding engines cannot be loaded from external assemblies.

**Improvement Recommendations:**
1. Design an `IFindingEngine` extension point that loads from external assemblies.

---

### 26. Documentation — Score: 80 / 100 (Weight: 1, Weighted Gap: 20)

**Justification:**
- 193+ markdown files covering architecture, operations, security, testing, deployment, onboarding, runbooks, ADRs, API contracts, and more.
- Structured sections follow the mandated format (Objective, Assumptions, Constraints, etc.).
- Diagrams (Mermaid) in architecture docs. Glossary. Code map. Architecture index.
- Onboarding paths: day-one-developer, day-one-SRE, day-one-security.
- **Gaps:** Quantity may exceed discoverability. Some docs may be stale after rapid iteration. No automated doc-link checker. Multiple overlapping quality assessments.

**Improvement Recommendations:**
1. Add a doc-link validation CI step (check for broken internal links).
2. Add "last reviewed" dates to all operational runbooks.

---

### 27. Accessibility — Score: 62 / 100 (Weight: 1, Weighted Gap: 38)

**Justification:**
- WCAG 2.1 AA target stated. Radix UI components for accessible dialogs. `aria-live` region for run progress. eslint-plugin-jsx-a11y.
- Keyboard shortcuts documented. `ConfirmationDialog` uses alert-dialog pattern (focus trap, no passive dismiss).
- **Gaps:** No automated axe-core scanning in Playwright E2E (referenced as "axe Playwright gates" but implementation unclear in test files). Limited ARIA documentation. No color contrast verification in CI. No screen reader testing documented. Accessibility supplement doc is thin (3 patterns only).

**Improvement Recommendations:**
1. Add axe-core accessibility scanning to Playwright E2E tests.
2. Add color contrast verification for the design system.
3. Conduct and document screen reader testing for key workflows.

---

## Summary Table (sorted by weighted gap, descending)

| Rank | Quality Area | Weight | Score | Gap (100-Score) | Weighted Gap | Grade |
|------|-------------|--------|-------|-----------------|--------------|-------|
| 1 | **Correctness** | 8 | 68 | 32 | **256** | Adequate |
| 2 | **Traceability** | 7 | 72 | 28 | **196** | Adequate |
| 3 | **Evolvability** | 6 | 68 | 32 | **192** | Adequate |
| 4 | **Architectural Integrity** | 7 | 74 | 26 | **182** | Adequate |
| 5 | **Security** | 6 | 70 | 30 | **180** | Adequate |
| 6 | **Explainability** | 6 | 73 | 27 | **162** | Adequate |
| 7 | **Usability** | 4 | 64 | 36 | **144** | Adequate |
| 8 | **Cognitive Load** | 4 | 65 | 35 | **140** | Adequate |
| 9 | **Policy & Governance** | 5 | 72 | 28 | **140** | Adequate |
| 10 | **AI/Agent Readiness** | 4 | 66 | 34 | **136** | Adequate |
| 11 | **Reliability** | 5 | 74 | 26 | **130** | Adequate |
| 12 | **Auditability** | 5 | 75 | 25 | **125** | Strong |
| 13 | **Data Consistency** | 5 | 76 | 24 | **120** | Strong |
| 14 | **Maintainability** | 4 | 71 | 29 | **116** | Adequate |
| 15 | **Scalability** | 3 | 66 | 34 | **102** | Adequate |
| 16 | **Interoperability** | 3 | 68 | 32 | **96** | Adequate |
| 17 | **Testability** | 3 | 73 | 27 | **81** | Adequate |
| 18 | **Modularity** | 3 | 77 | 23 | **69** | Strong |
| 19 | **Performance** | 2 | 67 | 33 | **66** | Adequate |
| 20 | **Availability** | 2 | 69 | 31 | **62** | Adequate |
| 21 | **Manageability** | 2 | 70 | 30 | **60** | Adequate |
| 22 | **Cost-Effectiveness** | 2 | 70 | 30 | **60** | Adequate |
| 23 | **Observability** | 3 | 82 | 18 | **54** | Strong |
| 24 | **Deployability** | 2 | 76 | 24 | **48** | Strong |
| 25 | **Accessibility** | 1 | 62 | 38 | **38** | Adequate |
| 26 | **Extensibility** | 1 | 70 | 30 | **30** | Adequate |
| 27 | **Documentation** | 1 | 80 | 20 | **20** | Strong |

**Overall weighted score:** 7,397 / 10,800 = **68.5%** (sum of weight × score / sum of weight × 100)

**Unweighted average:** 71.0 / 100

---

## Six Best Improvements (ordered by weighted impact)

These are the improvements that would deliver the most value per unit of effort, considering both the weight of the quality area and the feasibility of the improvement.

### Improvement 1: Raise Test Coverage and Mutation Score (Correctness, Weight 8)

**Target:** Move line coverage from 74% → 82%, branch coverage 57% → 65%, Stryker from 65% → 75%.

**Why this is the best improvement:** Correctness carries the highest weight (8) and has the largest weighted gap (256). Every percentage point of coverage gained here has 8x the impact of a weight-1 improvement. The infrastructure (Coverlet, Stryker, CI gates) already exists — this is about writing tests, not building frameworks.

**Approach:**
1. Run `dotnet test` with coverage collection and identify the 5 lowest-covered assemblies.
2. For each: write unit tests for untested public methods, focusing on error/edge paths.
3. Add property-based tests (FsCheck) for governance state machine transitions and alert deduplication.
4. Raise CI gates in `ci.yml` as coverage improves.

**Cursor prompts** (paste-ready; stable slugs for bookmarks and checklist cross-refs):

```
Improvement 1 — Prompt `coverage-gap-report`

Regenerate docs/COVERAGE_GAP_ANALYSIS.md from a merged Cobertura file (do not hand-edit
the tables). Preferred path:

1. From repo root, run a full Release test pass with Coverlet and merge to one Cobertura
   (see "How to refresh" inside docs/COVERAGE_GAP_ANALYSIS.md and docs/CODE_COVERAGE.md).
2. Place the merged Cobertura at coverage-gap-1a/merged/Cobertura.xml (or adjust
   scripts/ci/coverage_gap_analysis.py to accept a CLI path if you use a different folder,
   then run the script once and commit only the doc + any script argv support).
3. Run: python scripts/ci/coverage_gap_analysis.py
4. Commit the updated docs/COVERAGE_GAP_ANALYSIS.md if values changed.

This is analysis-only unless you add argv support to the script; no production code changes
required for a simple refresh.
```

```
Improvement 1 — Prompt `lowest-assembly-tests`

Read docs/COVERAGE_GAP_ANALYSIS.md. For the #1 lowest-covered production assembly,
work the three files listed with the most uncovered lines. For each file:

1. Read the source and list public surface area that lacks meaningful tests.
2. Add tests in the matching *.Tests project. For Dapper relational readers under
   ArchLucid.Persistence, prefer SQL Server integration tests (existing patterns:
   SqlServerContainer / ArchLucid.Persistence.Tests) over heavy mocking of ADO.NET,
   unless the file is already fully unit-testable.
3. Cover happy path, null/empty inputs, boundaries, and error paths. Use FluentAssertions
   and concrete types (not var). Follow sibling tests in the same test project.
4. Add [Trait("Suite", "Core")] and [Trait("Category", "Unit")] (or Integration where
   appropriate) on each test class.
5. Build and run the affected test project.

Target: materially raise line coverage for that assembly. Do not use ConfigureAwait(false)
in tests. Each test class in its own file.
```

```
Improvement 1 — Prompt `governance-workflow-fscheck`

Extend property-based coverage in ArchLucid.Decisioning.Tests (see existing
Governance/GovernanceWorkflowSegregationAndPromotionPropertyTests.cs) for the governance
approval workflow around GovernanceWorkflowService. Assert invariants such as:

1. Submitted approval requests start in the expected pending/reviewable state.
2. Self-approval is blocked (ordinal case-insensitive) with GovernanceSelfApprovalException.
3. Terminal states (approved/rejected) reject duplicate approve/reject transitions.
4. Production promotion requires a matching approved request for run/manifest/environment.

Use NSubstitute (or TestSupport doubles) for repositories and services. Add
[Trait("Suite", "Core")] and [Trait("Category", "Unit")]. Do not use ConfigureAwait(false)
in tests. Each new test class in its own file.
```

---

### Improvement 2: Harden Security Guardrails (Security, Weight 6)

**Target:** Prevent DevelopmentBypass in production, add per-PR API fuzzing, define RBAC roles.

**Why this is the best improvement #2:** Security has weight 6 and a weighted gap of 180. The DevelopmentBypass risk is the single most dangerous gap in the codebase — a misconfiguration could expose the entire API without authentication.

**Status (implemented):** As of 2026-04-14 the three approach items are in the tree:

1. **`AuthSafetyGuard.GuardAllDevelopmentBypasses`** (alias **`GuardDevelopmentBypassInProduction`**) — `ArchLucid.Host.Core/Startup/AuthSafetyGuard.cs`; invoked from **`ArchLucid.Api/Program.cs`** before **`AddArchLucidAuth`**; blocks **`DevelopmentBypass`** and **`Authentication:ApiKey:DevelopmentBypassAll`** in Production (**`IHostEnvironment`** or **`ARCHLUCID_ENVIRONMENT`**) — see **docs/SECURITY.md** § DevelopmentBypass production guard.
2. **Per-PR Schemathesis** — **`.github/workflows/ci.yml`** job **`api-schemathesis-light`** (`--phases=examples`, OpenAPI checks); full fuzz remains on the weekly workflow (see **docs/API_FUZZ_TESTING.md**).
3. **RBAC** — **`ArchLucid.Core/Authorization/ArchLucidRoles.cs`**, **`ArchLucidPolicies.cs`**, **`AddArchLucidAuthorizationPolicies`** in **`ArchLucid.Host.Core/Startup/ArchLucidAuthorizationPoliciesExtensions.cs`**; API controllers use **`[Authorize(Policy = …)]`**; registration tests in **`ArchLucid.Host.Composition.Tests`**.

**Verification (re-run anytime):** `dotnet test ArchLucid.Host.Composition.Tests/ArchLucid.Host.Composition.Tests.csproj -c Release` — expect **`AuthSafetyGuardTests`** and **`ArchLucidAuthorizationPoliciesRegistrationTests`** green (18 tests as of 2026-04-15).

**Cursor prompts** (paste-ready; use for regression checks or future hardening):

```
Improvement 2 — Prompt `dev-bypass-production-guard`

Verify the DevelopmentBypass production guard is still correct:

1. Open ArchLucid.Host.Core/Startup/AuthSafetyGuard.cs — GuardAllDevelopmentBypasses
   must throw InvalidOperationException in Production when ArchLucidAuth:Mode is DevelopmentBypass,
   or when Authentication:ApiKey:DevelopmentBypassAll is true (same Production detection:
   IHostEnvironment.IsProduction() or ARCHLUCID_ENVIRONMENT Production).
2. Open ArchLucid.Api/Program.cs — GuardAllDevelopmentBypasses must run before AddArchLucidAuth
   (no separate post-build DevelopmentBypassAll block).
3. Run: dotnet test ArchLucid.Host.Composition.Tests/ArchLucid.Host.Composition.Tests.csproj -c Release --filter "FullyQualifiedName~AuthSafetyGuardTests"
4. Confirm docs/SECURITY.md still documents the guard.

If any check fails, fix code or docs in the same change set.
```

```
Improvement 2 — Prompt `rbac-policies-and-sensitive-controllers`

Verify RBAC wiring and sensitive surfaces:

1. Run: dotnet test ArchLucid.Host.Composition.Tests/ArchLucid.Host.Composition.Tests.csproj -c Release --filter "FullyQualifiedName~ArchLucidAuthorizationPoliciesRegistrationTests"
2. Spot-check that high-risk controllers still declare explicit policies — at minimum:
   GovernanceController, AdminController, PolicyPacksController, AdvisorySchedulingController,
   AuditController (export → RequireAuditor where applicable).
3. Confirm docs/SECURITY.md § Role-based access control (RBAC) matches ArchLucidRoles /
   ArchLucidPolicies and AddArchLucidAuthorizationPolicies.

When adding new mutating or export endpoints, apply the strictest appropriate policy and
extend registration tests if you add a new policy name.
```

---

### Improvement 3: Clean Up Rename Artifacts to Reduce Cognitive Load (Cognitive Load, Weight 4 + Evolvability, Weight 6)

**Target:** Remove stale `ArchiForge.*` tree artifacts, consolidate to a single `.sln`, and publish a calendar target for legacy configuration key handling.

**Status (2026-04-15):** **Implemented** for in-repo rename debris: removed duplicate **`ArchiForge.sln`** (re-confirmed removed; repo root keeps **`ArchLucid.sln` only**), deleted orphan **`ArchiForge/`** stub sources (duplicates of `ArchLucid.*` files), renamed **`ArchLucid.Api/ArchiForge.Api.http`** → **`ArchLucid.Api.http`**, aligned **`docs/CONFIG_BRIDGE_SUNSET.md`** with **`ArchLucidLegacyConfigurationWarnings.LegacyConfigurationKeysHardEnforcementNoEarlierThan`** (`2027-07-01`) and extended the startup warning text. **Doc consolidation:** archived **`QUALITY_ASSESSMENT.md`**, **`QUALITY_ASSESSMENT_2026_04.md`**, **`QUALITY_ASSESSMENT_2026_04_14.md`** to **`docs/archive/`** with stubs at the former paths; paste-ready prompts in **`docs/CURSOR_PROMPTS_WEIGHTED_IMPROVEMENT_3.md`**. Terraform resource addresses and workspace path remain deferred per checklist **7.5–7.8**.

**Why:** This improvement addresses both Cognitive Load (weighted gap 140) and Evolvability (weighted gap 192) simultaneously. Leftover rename paths mislead contributors and tooling.

---

### Improvement 4: Strengthen Traceability with Requirements Mapping (Traceability, Weight 7)

**Target:** Create a lightweight requirements-to-test traceability matrix for V1 scope items and implement orphan remediation for run archival cascades.

**Why:** Traceability has weight 7 and a 196 weighted gap. The provenance infrastructure is strong but lacks the "last mile" of requirements → implementation → test linkage.

**Status (2026-04-15):** **Partially implemented.** Traceability matrix: **`docs/V1_REQUIREMENTS_TEST_TRACEABILITY.md`**. Orphan **detection** extended: **`DataConsistencyOrphanProbeHostedService`** now probes **`LeftRunId`** and **`RightRunId`**; counter labels **`table`** + **`column`**. Operator **remediation** runbook: **`docs/runbooks/COMPARISON_RECORD_ORPHAN_REMEDIATION.md`**. Paste-ready prompts: **`docs/CURSOR_PROMPTS_WEIGHTED_IMPROVEMENTS_3_TO_6.md`** (`v1-requirements-traceability-matrix`, `comparison-orphan-probe-and-remediation`).

---

### Improvement 5: Add Individual Finding Explanations (Explainability, Weight 6)

**Target:** Build a user-facing narrative for individual findings that composes `ExplainabilityTrace` fields into readable text, and enable faithfulness trend alerting.

**Why:** Explainability has weight 6 and a 162 weighted gap. The infrastructure exists (trace fields, completeness analyzer) but the user-facing narrative is missing.

**Status (2026-04-15):** **Partially implemented.** **`FindingExplainabilityNarrativeBuilder`** + **`FindingExplainabilityResult.NarrativeText`** on **`GET .../findings/{id}/explainability`**. Unit tests: **`FindingExplainabilityNarrativeBuilderTests`**. Prometheus: **`archlucid-explainability`** group with **`ArchLucidExplanationFaithfulnessFallbackTrend`** in **`infra/prometheus/archlucid-alerts.yml`**. Prompts: **`finding-explainability-narrative`** in **`docs/CURSOR_PROMPTS_WEIGHTED_IMPROVEMENTS_3_TO_6.md`**.

---

### Improvement 6: Implement First-Run Wizard and Consistent Error Formatting (Usability, Weight 4)

**Target:** Implement the first-run wizard from the existing design doc, and add RFC 9457 Problem Details consistently across all controllers.

**Why:** Usability has weight 4 and a 144 weighted gap. The first-run wizard design already exists — this is pure implementation. Consistent error formatting is a quick win that improves every API consumer's experience.

**Status (2026-04-15):** **Partially implemented.** First-run wizard already ships at **`archlucid-ui`** **`/runs/new`** (wizard schema + presets); full parity sweep remains (**`first-run-wizard-parity`** prompt). RFC 9457 Problem Details: **`AdminController`**, **`JobsController`**, **`DemoController`** now return Problem+JSON for the remaining bare **`NotFound()`** / **`Conflict(info)`** paths. Broader controller sweep: **`rfc9457-controller-sweep`** in **`docs/CURSOR_PROMPTS_WEIGHTED_IMPROVEMENTS_3_TO_6.md`**.

---

## Cursor prompts for Improvements 1–6

Improvement 1 uses **`coverage-gap-report`**, **`lowest-assembly-tests`**, **`governance-workflow-fscheck`**. Improvement 2 uses **`dev-bypass-production-guard`** and **`rbac-policies-and-sensitive-controllers`**. Improvement 3: **`docs/CURSOR_PROMPTS_WEIGHTED_IMPROVEMENT_3.md`** (rename, single `.sln`, legacy sunset, archive quality docs). **Improvements 3–6** (verification bundle + 4–6 prompts): **`docs/CURSOR_PROMPTS_WEIGHTED_IMPROVEMENTS_3_TO_6.md`**.

The fenced blocks below duplicate Improvement 3 prompts **`rename-artifacts-single-sln`**, **`rename-artifacts-no-stub-archiforge-src`**, and **`legacy-config-sunset-constant`** for inline reading only.

```
Improvement 3 — Prompt `rename-artifacts-single-sln`

Keep a single solution entry point:

1. Repo root must contain exactly one product solution: ArchLucid.sln (no ArchiForge.sln).
2. CI/scripts/docs that reference a solution must use ArchLucid.sln — grep for ".sln" if a duplicate appears.
3. Run: dotnet build ArchLucid.sln -c Release --nologo

If a second .sln reappears, delete the duplicate or merge project entries, then re-run the build.
```

```
Improvement 3 — Prompt `rename-artifacts-no-stub-archiforge-src`

Remove contributor confusion from orphan product paths:

1. There must be no ArchiForge/ directory tree holding .cs sources under the repo root (stale copies of ArchLucid.*).
2. API REST client scratch file should be ArchLucid.Api/ArchLucid.Api.http (not ArchiForge.Api.http).
3. Expected intentional literals: scripts/ci/archiforge-rename-allowlist*.txt, historical docs, Terraform addresses (*.archiforge), RLS SQL object names — do not delete those.

If orphan sources exist, delete them after confirming equivalent types/tests live under ArchLucid.*; run dotnet build ArchLucid.sln -c Release.
```

```
Improvement 3 — Prompt `legacy-config-sunset-constant`

Align legacy key warnings with a published sunset target:

1. Open ArchLucid.Host.Core/Configuration/ArchLucidLegacyConfigurationWarnings.cs — confirm
   LegacyConfigurationKeysHardEnforcementNoEarlierThan and the warning template reference docs/CONFIG_BRIDGE_SUNSET.md.
2. Open docs/CONFIG_BRIDGE_SUNSET.md — § Sunset timeline must match the same calendar date as the code constant.
3. When changing the date, update both files in one change set and add release-note intent if enforcement is planned.

Optional: grep ArchLucid.Api/Program.cs and ArchLucid.Worker/Program.cs for LogIfLegacyKeysPresent to ensure warnings still run after host build.
```

---
