> **Scope:** Independent first-principles quality assessment — weighted readiness model; not a roadmap, not derived from prior assessments.

# ArchLucid Assessment -- Weighted Readiness 65.38%

**Date:** 2026-04-29
**Assessor:** Independent first-principles (no prior assessment referenced)
**Codebase snapshot:** Git working tree as of 2026-04-29 08:00 UTC-4

---

## 1. Executive Summary

### Overall Readiness

ArchLucid is a genuinely novel product at an intermediate stage of commercial maturity. The core engineering -- multi-agent architecture pipeline, manifest lifecycle, audit trail, governance workflow -- is structurally sound and internally coherent. The product has a credible first-pilot story and substantial documentation depth. However, the hosted SaaS funnel is not yet reachable from the development network, commercial enforcement is incomplete, and coverage/testing gaps remain in key persistence and API layers. The weighted readiness of **65.38%** reflects a product that can demonstrate value in controlled settings but has material gaps before unattended revenue generation.

### Commercial Picture

The product has clear positioning, pricing tiers, procurement artifacts, and a competitive landscape analysis. The fundamental commercial blocker is that the self-service trial funnel (`staging.archlucid.net`) does not resolve -- meaning zero unattended buyer acquisition is possible today. Stripe checkout integration exists in code but has not been validated end-to-end against a live endpoint. Commercial tier enforcement (`[RequiresCommercialTenantTier]`) is wired but uses 404 masking rather than explicit upgrade prompts, which will suppress conversion signals. No reference customer or case study exists beyond synthetic Contoso data.

### Enterprise Picture

Enterprise readiness is above average for a pre-GA product. The security posture (RBAC, RLS, OWASP ZAP, Schemathesis, gitleaks, CodeQL, Trivy, STRIDE model) is strong for this stage. SOC 2 Type I is funded but not yet started. Pen-test SoW is awarded with kickoff scheduled. Audit trail (126 event types, append-only SQL enforcement, CI guard) is production-grade. Governance workflows, approval SLAs, and policy packs are functional. The gap is third-party attestation: no independent audit report, no completed pen-test, no reference customer willing to speak.

### Engineering Picture

Architecture is well-decomposed across ~50 .NET projects with clear layering (Core, Contracts, Application, Persistence, Api, AgentRuntime, Decisioning, etc.). 32 ADRs document significant decisions. CI is comprehensive (tiered: secret scan, fast core, full SQL regression, chaos, k6, UI unit, UI e2e, Docker/Trivy, Terraform validate, OWASP ZAP, Schemathesis, 40+ doc/lint guards). Code coverage is ~73% line / ~59% branch (local measurement), below the strict CI gates of 79%/63%. The `ArchLucid.Persistence` package is at ~40% line coverage, significantly below the 63% floor. The staging environment is not DNS-reachable, which means hosted deployment has not been validated end-to-end.

---

## 2. Weighted Quality Assessment

Total weight pool: 102. Qualities ordered by **weighted deficiency** (weight x (100 - score) / total weight), most urgent first.

### 2.1 Marketability
- **Score:** 52
- **Weight:** 8
- **Weighted contribution:** 4.16 / 8.00
- **Weighted deficiency:** 3.84

**Justification:** The product has clear positioning, a competitive landscape doc, elevator pitches, and buyer personas. However, zero live customers exist. The staging funnel does not resolve DNS. No completed pen-test or SOC 2 report is available for procurement. The marketing site is not reachable. Without a functioning self-service path or a single reference customer, marketability is severely constrained regardless of how good the internal artifacts are.

**Tradeoffs:** Building more marketing collateral vs. making the funnel actually work. The team has invested heavily in docs and positioning -- the bottleneck is now operational proof.

**Recommendations:** Fix DNS/Front Door for staging.archlucid.net. Complete a single real customer pilot with publishable results. Shift effort from doc polish to funnel operationalization. *V1 fixable.*

---

### 2.2 Time-to-Value
- **Score:** 60
- **Weight:** 7
- **Weighted contribution:** 4.20 / 7.00
- **Weighted deficiency:** 2.80

**Justification:** The `archlucid try` CLI command promises a 60-second first experience. The Core Pilot wizard and in-product checklist are well-designed. Real-mode benchmarks show ~2 minutes to committed manifest with Azure OpenAI. But the hosted trial path is broken (DNS not resolving). The Docker-only path requires .NET 10 SDK + Docker -- not a quick start for non-developers. The simulator mode works but produces deterministic/fake outputs that may underwhelm a buyer evaluating AI quality.

**Tradeoffs:** Simulator-first approach gives deterministic testing and zero LLM cost but undermines the "wow" moment for prospects who expect real AI output.

**Recommendations:** Get the hosted trial funnel working. Ensure the first-run experience uses real AOAI (even with a budget cap) so prospects see genuine analysis, not canned responses. *V1 fixable.*

---

### 2.3 Adoption Friction
- **Score:** 55
- **Weight:** 6
- **Weighted contribution:** 3.30 / 6.00
- **Weighted deficiency:** 2.70

**Justification:** For hosted SaaS buyers, the intended friction is low (sign up, see pre-seeded data, run wizard). But this path does not work today. For self-hosted evaluators, the requirements are substantial: .NET 10 SDK, Docker, SQL Server, Node 22. The configuration reference has 60+ keys. The documentation volume (522 markdown files under docs/) is overwhelming -- the five-doc spine helps but the sheer mass creates cognitive overhead for new contributors or evaluators.

**Tradeoffs:** Comprehensive documentation vs. discoverability. The team has tried to solve this with layered entry points (START_HERE, FIRST_5_DOCS, NAVIGATOR), but the volume itself signals complexity.

**Recommendations:** Reduce the number of prerequisites for first evaluation. Consider a hosted demo that requires zero local setup. *V1 fixable.*

---

### 2.4 Proof-of-ROI Readiness
- **Score:** 62
- **Weight:** 5
- **Weighted contribution:** 3.10 / 5.00
- **Weighted deficiency:** 1.90

**Justification:** The ROI model is well-designed with baseline capture, delta measurement, and a structured proof-of-value snapshot assembly process. The first-value report builder, sponsor one-pager PDF, and executive digest features are implemented. However, all ROI figures are modeled, not measured from real customer usage. The $294K savings claim is based on a 6-architect team model -- no customer has validated this. The "Day N since first commit" badge is a nice touch but needs a real tenant to matter.

**Tradeoffs:** Having a detailed ROI framework without validation data vs. waiting for customers before building the framework.

**Recommendations:** Run the ROI model against the first real pilot and publish anonymized results. *V1.1 -- requires real customer data.*

---

### 2.5 Correctness
- **Score:** 72
- **Weight:** 4
- **Weighted contribution:** 2.88 / 4.00
- **Weighted deficiency:** 1.12

**Justification:** The core pipeline (request -> execute -> commit -> manifest) works correctly in simulator and real mode. Findings engine (10 types), decisioning, manifest finalization with transactional SQL, and idempotency hashing are tested. Property-based tests exist for status transitions and idempotency. However, coverage is below target (73% vs 79% line gate), and the persistence layer at ~40% coverage is a significant blind spot. The manifest finalization uses SQL error codes (50001-50006) for conflict detection -- well-designed but lightly tested against actual concurrent scenarios. Explanation faithfulness checking uses a token overlap heuristic that is acknowledged as approximate.

**Tradeoffs:** Simulator-first testing gives deterministic correctness validation but may miss real-mode edge cases (LLM output parsing, timeout handling, token quota).

**Recommendations:** Lift persistence coverage to 63% floor. Add concurrency tests for manifest finalization. *V1 fixable.*

---

### 2.6 Executive Value Visibility
- **Score:** 65
- **Weight:** 4
- **Weighted contribution:** 2.60 / 4.00
- **Weighted deficiency:** 1.40

**Justification:** The Executive Sponsor Brief is well-crafted and appropriately cautious. The first-value report, sponsor one-pager PDF, and value report features exist. The "why ArchLucid" evidence pack and procurement-facing proof surfaces are implemented. The `/demo/explain` route for provenance visualization is a strong proof point. However, no executive has actually used these materials. The value report builder produces output but has no external validation.

**Tradeoffs:** Building executive-facing materials before having executives to show them to.

**Recommendations:** Get the demo/explain route working on a reachable URL. Validate sponsor brief with at least one actual sponsor. *V1 fixable.*

---

### 2.7 Differentiability
- **Score:** 70
- **Weight:** 4
- **Weighted contribution:** 2.80 / 4.00
- **Weighted deficiency:** 1.20

**Justification:** ArchLucid genuinely occupies a novel intersection: multi-agent AI architecture analysis + enterprise governance + full audit trail + explainability traces. The competitive landscape analysis correctly identifies that no incumbent combines all three pillars. The ExplainabilityTrace with 5 structured fields, provenance graph, and 126 typed audit events are real differentiators. The pre-commit governance gate with segregation of duties is enterprise-grade. Weak point: the AI agent quality itself is unproven in production -- the structural/semantic quality scores are self-assessed, not externally validated.

**Tradeoffs:** Deep investment in governance and audit infrastructure (which competitors lack) vs. polishing the AI analysis quality (which buyers see first).

**Recommendations:** Publish agent output quality benchmarks against a standard architecture review corpus. *V1.1.*

---

### 2.8 Architectural Integrity
- **Score:** 78
- **Weight:** 3
- **Weighted contribution:** 2.34 / 3.00
- **Weighted deficiency:** 0.66

**Justification:** The architecture is well-decomposed: clear separation between Core (no persistence references), Contracts/Abstractions, Application (orchestration), Persistence (Dapper, not ORM), AgentRuntime, Decisioning, ContextIngestion, KnowledgeGraph, ArtifactSynthesis, Provenance, and Retrieval. 32 ADRs document key decisions. The coordinator-to-authority strangler pattern (ADRs 0021, 0028, 0029, 0030) shows mature architectural evolution. Architecture tests exist (`ArchLucid.Architecture.Tests`). The primary constructor style is consistently applied. The layered CI (Tier 0-3b) reflects architectural boundaries.

**Tradeoffs:** The number of projects (~50) creates build-time and cognitive overhead but enforces boundaries.

**Recommendations:** Complete the coordinator strangler migration (ADR 0028/0029). *V1 fixable.*

---

### 2.9 Security
- **Score:** 75
- **Weight:** 3
- **Weighted contribution:** 2.25 / 3.00
- **Weighted deficiency:** 0.75

**Justification:** Strong for pre-GA: OWASP ZAP baseline (merge-blocking), Schemathesis API fuzzing, CodeQL security-extended, gitleaks, Trivy (container + IaC), CycloneDX SBOMs. Auth model is sound (Entra ID JWT + API key + dev bypass with production guards). SQL RLS with SESSION_CONTEXT is implemented. LLM prompt redaction exists. Content safety guard is fail-closed in production. HTTP security headers are set. STRIDE threat model exists. Pen-test SoW is awarded. Gaps: no completed pen-test results, no SOC 2 attestation, PGP key not yet published, security.txt not yet served.

**Tradeoffs:** Comprehensive automated scanning (which is strong) vs. independent human validation (which is pending).

**Recommendations:** Complete pen-test engagement and publish redacted summary. *V1 fixable (timeline: May-June 2026).*

---

### 2.10 Traceability
- **Score:** 80
- **Weight:** 3
- **Weighted contribution:** 2.40 / 3.00
- **Weighted deficiency:** 0.60

**Justification:** This is one of ArchLucid's strongest areas. ExplainabilityTrace with 5 structured fields on every finding. Provenance graph with typed nodes and edges. 126 audit event types with CI guard against drift. Agent execution trace persistence (prompt/response forensics). Decision trace repository. Committed authority chain persistence. Correlation IDs throughout the pipeline. The `CommittedManifestTraceabilityRulesTests` validate trace integrity.

**Tradeoffs:** Rich traceability infrastructure with no external validator yet.

**Recommendations:** Demonstrate the trace chain to an auditor or compliance reviewer in a pilot. *V1 fixable.*

---

### 2.11 Usability
- **Score:** 68
- **Weight:** 3
- **Weighted contribution:** 2.04 / 3.00
- **Weighted deficiency:** 0.96

**Justification:** The operator UI has good structural design: Core Pilot wizard, progressive disclosure, LayerHeader strips, rank-aware shaping, keyboard shortcuts, command palette. WCAG 2.1 AA targeting with 35 axe-scanned pages. Skip-to-content, landmark navigation, and focus management are implemented. However, the UI complexity (role-aware shaping, two-tier progressive disclosure, Execute+ mutation gating) creates substantial cognitive load for new operators. The README for the UI alone is ~250 lines of dense shaping logic documentation, which suggests the UX model itself may be over-engineered for V1.

**Tradeoffs:** Enterprise-grade role-aware UX (which procurement likes) vs. simplicity for first-time operators (which conversion requires).

**Recommendations:** Simplify the first-run experience to hide role-aware complexity until after first pilot success. *V1 fixable.*

---

### 2.12 Workflow Embeddedness
- **Score:** 58
- **Weight:** 3
- **Weighted contribution:** 1.74 / 3.00
- **Weighted deficiency:** 1.26

**Justification:** Integration events via Azure Service Bus (11 schema types, AsyncAPI spec, CloudEvents format). Azure DevOps pipeline tasks exist. GitHub Action for manifest delta PR comments. REST API with OpenAPI spec. Webhook recipes documented. However, first-party ITSM connectors (Jira, ServiceNow, Confluence) are explicitly deferred to V1.1. Slack connector deferred to V2. Teams connector exists. No SSO beyond Entra ID is production-tested (Okta/Auth0 docs exist but are configuration guides, not tested integrations).

**Tradeoffs:** Webhook/API-first approach (extensible but requires customer effort) vs. first-party connectors (lower friction but higher build cost). Deferring Jira/ServiceNow to V1.1 is a deliberate scope choice.

**Recommendations:** Validate the Teams connector end-to-end. Build the ServiceNow V1.1 connector (already prioritized). *V1.1.*

---

### 2.13 Trustworthiness
- **Score:** 60
- **Weight:** 3
- **Weighted contribution:** 1.80 / 3.00
- **Weighted deficiency:** 1.20

**Justification:** The product has strong trustworthiness infrastructure: RBAC, RLS, audit trail, governance workflows, approval SLAs, segregation of duties. The explanation faithfulness checker and fallback mechanism show awareness of LLM unreliability. Content safety guards are fail-closed in production. However: no third-party attestation exists. No customer has used the system in production. Agent output quality is self-measured. The "trusted baseline" demo uses synthetic data with an explicit honesty boundary -- which is commendable transparency but also means no real trust has been earned yet.

**Tradeoffs:** Building trust infrastructure (which is strong) vs. earning external trust markers (which require time and customers).

**Recommendations:** Complete SOC 2 readiness engagement. Get one customer through a full pilot with measurable outcomes. *V1.1.*

---

### 2.14 Reliability
- **Score:** 68
- **Weight:** 2
- **Weighted contribution:** 1.36 / 2.00
- **Weighted deficiency:** 0.64

**Justification:** Circuit breaker pattern implemented with audit integration. Health checks (live/ready/full). Data consistency orphan probe with configurable enforcement modes (Warn/Alert/Quarantine). Idempotency hashing for run creation. Transactional manifest finalization with SQL error code handling. Simmy chaos testing in CI. However: staging environment is not reachable, so production reliability is unproven. RTO/RPO targets are documented but not tested. SQL failover Terraform exists but is not validated. No incident history exists (because no production exists).

**Tradeoffs:** Infrastructure for reliability (circuit breakers, health checks, chaos tests) without operational proof.

**Recommendations:** Get staging environment running and validate failover. *V1 fixable.*

---

### 2.15 Data Consistency
- **Score:** 70
- **Weight:** 2
- **Weighted contribution:** 1.40 / 2.00
- **Weighted deficiency:** 0.60

**Justification:** Transactional unit-of-work pattern for manifest finalization. SQL error codes for conflict detection. Data consistency orphan probe (hosted service) monitors referential integrity across ComparisonRecords, GoldenManifests, FindingsSnapshots vs Runs. Quarantine mode for detected orphans. OTel metrics for orphan detection and alerts. Manifest hash service for content integrity. However, the probe is detection-only by default (Warn mode in code, Alert in config). No automated repair. Persistence coverage at ~40% means many data paths are not exercised in tests.

**Tradeoffs:** Detection-first approach (safe, observable) vs. auto-repair (risky but complete).

**Recommendations:** Lift persistence test coverage. Validate orphan probe against intentional FK violations. *V1 fixable.*

---

### 2.16 Maintainability
- **Score:** 72
- **Weight:** 2
- **Weighted contribution:** 1.44 / 2.00
- **Weighted deficiency:** 0.56

**Justification:** Strong code conventions enforced by Cursor rules (12 CSharp-Terse rules, primary constructors, expression-bodied members, pattern matching, guard clauses). EditorConfig with consistent formatting. Central package management (Directory.Packages.props). Each class in its own file. LINQ preferred over foreach. Good modularity with clear project boundaries. 522 docs files is a maintenance burden but the doc-scope-header CI guard and doc-root-size budget help. The rename from ArchiForge to ArchLucid was completed with CI guards against regression.

**Tradeoffs:** Extensive rule system and CI guards improve consistency but add onboarding friction for new contributors.

**Recommendations:** Consolidate redundant documentation. *V1 fixable.*

---

### 2.17 Explainability
- **Score:** 76
- **Weight:** 2
- **Weighted contribution:** 1.52 / 2.00
- **Weighted deficiency:** 0.48

**Justification:** ExplainabilityTrace with 5 structured fields (examined, rules applied, decisions taken, reasoning, constraints). Explanation faithfulness checking with token overlap heuristic and aggregate fallback. Citation references emitted with kind labels. Provenance graph visualization. Finding evidence chain service. Advisory scan trace completeness ratio measured via OTel histogram. The deterministic fallback when LLM narrative faithfulness is low is a mature design choice.

**Tradeoffs:** Heuristic faithfulness checking (fast, deterministic) vs. LLM-based evaluation (expensive, potentially circular).

**Recommendations:** Add human evaluation of explanation quality during first pilot. *V1.1.*

---

### 2.18 AI/Agent Readiness
- **Score:** 72
- **Weight:** 2
- **Weighted contribution:** 1.44 / 2.00
- **Weighted deficiency:** 0.56

**Justification:** Four specialized agents (Topology, Cost, Compliance, Critic). Agent output structural completeness scoring. Semantic quality scoring. Quality gate with configurable warn/reject thresholds. Prompt injection fixture validation in CI. LLM token quota tracking. Multi-vendor LLM support with fallback. Delegating completion provider. Agent execution trace persistence. Content safety guard. However: no production validation of agent quality. No A/B testing framework. No human evaluation loop. Golden cohort approach (cost dashboard, budget probe, kill switch) shows cost awareness.

**Tradeoffs:** Building agent infrastructure (quality gates, traces, safety) without proving agent output quality in real use.

**Recommendations:** Run agents against a diverse corpus of real architecture requests and publish quality metrics. *V1.1.*

---

### 2.19 Azure Compatibility and SaaS Deployment Readiness
- **Score:** 55
- **Weight:** 2
- **Weighted contribution:** 1.10 / 2.00
- **Weighted deficiency:** 0.90

**Justification:** Extensive Terraform: 14 root modules (container-apps, sql-failover, edge/Front Door, keyvault, monitoring, openai, entra, servicebus, storage, private networking, otel-collector, logic-apps, orchestrator, pilot). Azure Container Apps with secondary region. Grafana dashboards in Terraform. Prometheus SLO rules. Application Insights. Front Door with marketing routes. But: staging.archlucid.net does not resolve. The full SaaS stack (Front Door -> Container Apps -> SQL -> blob) has not been validated end-to-end from an external network. `apply-saas.ps1` exists but its execution status is unknown. The gap between infrastructure code and operational proof is the critical issue.

**Tradeoffs:** Comprehensive IaC (which is correct) without operational validation (which is required).

**Recommendations:** Execute apply-saas.ps1 against staging and validate the full funnel. *V1 fixable -- critical path.*

---

### 2.20 Auditability
- **Score:** 82
- **Weight:** 2
- **Weighted contribution:** 1.64 / 2.00
- **Weighted deficiency:** 0.36

**Justification:** 126 typed audit event constants with CI guard. Append-only SQL enforcement (DENY UPDATE/DELETE on dbo.AuditEvents). Durable + baseline mutation dual-channel. Paginated audit search with keyset cursor. Bulk export (JSON/CSV) with UTC range and row cap. Audit retention tiering (hot/warm/cold) documented. Audit coverage matrix with CI-enforced name parity. Per-run audit timeline service. This is production-grade audit infrastructure.

**Tradeoffs:** Strong audit system with no auditor having reviewed it.

**Recommendations:** Include audit trail walkthrough in pen-test scope. *V1 fixable.*

---

### 2.21 Policy and Governance Alignment
- **Score:** 74
- **Weight:** 2
- **Weighted contribution:** 1.48 / 2.00
- **Weighted deficiency:** 0.52

**Justification:** Pre-commit governance gate with severity thresholds. Approval workflow with segregation of duties. Approval SLA tracking with webhook escalation. Policy packs with versioning and scope assignments. Effective governance resolution. Compliance drift trend tracking. Governance dashboard. Governance lineage service. The governance workflow dual-writes to durable audit. Good design. Gap: no customer has configured or used governance in production.

**Tradeoffs:** Full governance system built before any customer needs it -- risk of over-engineering vs. being ready when enterprise buyers ask.

**Recommendations:** Validate governance workflow with a pilot customer's actual approval chain. *V1.1.*

---

### 2.22 Compliance Readiness
- **Score:** 58
- **Weight:** 2
- **Weighted contribution:** 1.16 / 2.00
- **Weighted deficiency:** 0.84

**Justification:** SOC 2 self-assessment exists with gap register. CAIQ-lite and SIG Core questionnaire drafts are pre-filled. DSAR process documented. DPA template exists. Privacy policy and subprocessors list exist. Trust center index. But: no SOC 2 report (Type I scoping funded for Q2-Q3 2026). No completed pen-test. No GDPR DPA signed with any customer. Compliance readiness is thorough in documentation but lacks third-party validation.

**Tradeoffs:** Pre-filling compliance artifacts (which saves time later) vs. actually completing the attestation process (which requires budget and time).

**Recommendations:** Execute SOC 2 readiness consultant engagement per the funded timeline. *V1.1 (Q3 2026).*

---

### 2.23 Procurement Readiness
- **Score:** 62
- **Weight:** 2
- **Weighted contribution:** 1.24 / 2.00
- **Weighted deficiency:** 0.76

**Justification:** Procurement evidence pack index exists. Order form template. MSA outline. DPA template. Trust center. Security overview and threat model. Pricing philosophy with three tiers. Azure Marketplace SaaS offer documentation. BUT: no signed customer. No SOC 2 report. No completed pen-test. No legal review of templates. The evidence pack is comprehensive in structure but lacks the artifacts that procurement teams require to sign: independent audit, pen-test results, reference customers.

**Tradeoffs:** Building the procurement "shell" before having the content to fill it.

**Recommendations:** Complete pen-test, then update procurement pack with real results. *V1 fixable.*

---

### 2.24 Interoperability
- **Score:** 65
- **Weight:** 2
- **Weighted contribution:** 1.30 / 2.00
- **Weighted deficiency:** 0.70

**Justification:** REST API with OpenAPI spec. AsyncAPI for integration events. CloudEvents format. Azure Service Bus topic. SCIM v2 service provider (ADR 0032). Azure DevOps pipeline tasks. GitHub Action. Webhook recipes. API versioning (URL path segment). JSON schema versioning for events. However: first-party ITSM connectors deferred. SSO tested only with Entra. No SAML federation tested. SCIM is documented but implementation status unclear.

**Tradeoffs:** API-first interoperability (flexible but requires integration effort) vs. pre-built connectors (easier but more to maintain).

**Recommendations:** Validate SCIM provisioning end-to-end with Okta or Azure AD. *V1.1.*

---

### 2.25 Decision Velocity
- **Score:** 68
- **Weight:** 2
- **Weighted contribution:** 1.36 / 2.00
- **Weighted deficiency:** 0.64

**Justification:** The pipeline (request -> execute -> commit) can complete in ~2 minutes with real AOAI. Governance gate gives immediate pass/warn/block feedback. Compare-two-runs provides structured deltas. Advisory scans run on schedule. The approval workflow has SLA tracking. Good foundation for accelerating architecture decisions. But no production measurement of actual decision cycle compression.

**Tradeoffs:** System-measured speed (API latency) vs. organizational decision speed (which includes human review time).

**Recommendations:** Measure total cycle time in a real pilot, not just API response time. *V1.1.*

---

### 2.26 Commercial Packaging Readiness
- **Score:** 58
- **Weight:** 2
- **Weighted contribution:** 1.16 / 2.00
- **Weighted deficiency:** 0.84

**Justification:** Three pricing tiers defined (Team $199/mo, Professional $899/mo, Enterprise custom). Trial parameters specified (14 days, 3 seats, 10 runs). Stripe integration code exists. Azure Marketplace SaaS offer documented. Billing provider abstraction (ADR 0016). `BillingProductionSafetyRules` CI guard. BUT: tier enforcement uses 404 masking (no upgrade prompt). No metering infrastructure validated. Stripe webhook handling exists in tests but not against live Stripe. The marketplace listing guard exists but no listing is published. Quote request flow exists but the email routing (CRM) is unresolved.

**Tradeoffs:** Building billing infrastructure before having billing customers.

**Recommendations:** Validate Stripe TEST checkout end-to-end against a reachable staging environment. *V1 fixable.*

---

### 2.27 Availability
- **Score:** 62
- **Weight:** 1
- **Weighted contribution:** 0.62 / 1.00
- **Weighted deficiency:** 0.38

**Justification:** 99.9% monthly availability target documented. Health endpoints (live/ready/full). Synthetic probes in GitHub Actions. SQL failover Terraform. Container Apps with secondary region. Front Door for edge routing. BUT: staging is not DNS-reachable. No production deployment exists. RTO/RPO targets are documented but untested. Availability is designed but not demonstrated.

**Recommendations:** Validate synthetic probes against a running staging environment. *V1 fixable.*

---

### 2.28 Performance
- **Score:** 70
- **Weight:** 1
- **Weighted contribution:** 0.70 / 1.00
- **Weighted deficiency:** 0.30

**Justification:** Performance baselines documented: E2E < 10s in-process, manifest p95 164ms, commit ~763ms. k6 load tests exist (CI smoke, real-mode benchmark). Rate limiting implemented with role-aware partitioning. Hot path cache with Redis support. Benchmark scripts for real-mode wall-clock measurement. Performance is measured but only in controlled environments.

**Recommendations:** Run k6 against staging with realistic concurrency. *V1 fixable.*

---

### 2.29 Scalability
- **Score:** 62
- **Weight:** 1
- **Weighted contribution:** 0.62 / 1.00
- **Weighted deficiency:** 0.38

**Justification:** Azure Container Apps with autoscaling. Secondary region in Terraform. SQL failover module. Redis cache for horizontal API instances. Background worker separation. Outbox pattern for async fan-out. BUT: no load testing at scale. The 2,000 run/mo Enterprise "fair-use soft cap" suggests scale limits are undetermined. Multi-tenant RLS adds per-query overhead that is not benchmarked at scale.

**Recommendations:** Run load tests simulating multi-tenant concurrent usage. *V1.1.*

---

### 2.30 Supportability
- **Score:** 72
- **Weight:** 1
- **Weighted contribution:** 0.72 / 1.00
- **Weighted deficiency:** 0.28

**Justification:** `archlucid doctor` CLI command. `archlucid support-bundle --zip`. Correlation IDs on all requests. Configuration validation (`archlucid config check`). Troubleshooting guide. Incident investigation runbook. Version endpoint. Pilot reporting guide. Good foundation for support operations.

**Recommendations:** Add support-bundle anonymization verification in CI. *V1 fixable.*

---

### 2.31 Manageability
- **Score:** 68
- **Weight:** 1
- **Weighted contribution:** 0.68 / 1.00
- **Weighted deficiency:** 0.32

**Justification:** 60+ configuration keys with a catalog and CLI validation. Hosting roles (Api/Worker/Combined). Feature flags for content safety, metering, demo mode. Configuration summary API endpoint. AppSettings layering (base, Development, Production, Staging, Advanced). BUT: 60+ config keys is a lot for V1. No configuration UI -- all via files/env vars.

**Recommendations:** Prioritize the 10 most common configuration scenarios in a quick-start guide. *V1 fixable.*

---

### 2.32 Deployability
- **Score:** 60
- **Weight:** 1
- **Weighted contribution:** 0.60 / 1.00
- **Weighted deficiency:** 0.40

**Justification:** Docker images with multi-stage builds. docker-compose with profiles (dev, full-stack, demo). Terraform modules for Azure. DbUp automatic migrations. CI builds and publishes images. Package-release scripts. BUT: the deployment has not been validated end-to-end on Azure. apply-saas.ps1 exists but its success is unknown. No documented deployment runbook beyond Terraform plan/apply.

**Recommendations:** Execute a complete deployment to staging and document the steps as a runbook. *V1 fixable.*

---

### 2.33 Observability
- **Score:** 78
- **Weight:** 1
- **Weighted contribution:** 0.78 / 1.00
- **Weighted deficiency:** 0.22

**Justification:** Custom OTel meter with 30+ instruments. Business KPI metrics. Pipeline stage duration histograms. Agent output quality metrics. Data consistency metrics. LLM token/cost metrics. Prometheus alert rules in Terraform. Grafana dashboards in Terraform. Application Insights integration. OTel collector Terraform module. Structured logging throughout. This is mature observability infrastructure.

**Recommendations:** Validate Grafana dashboards render correctly with real data. *V1 fixable.*

---

### 2.34 Testability
- **Score:** 70
- **Weight:** 1
- **Weighted contribution:** 0.70 / 1.00
- **Weighted deficiency:** 0.30

**Justification:** 19 test projects. Tiered CI (Tier 0-3b). Property-based tests. Mutation testing (Stryker configs for multiple modules). Integration tests with SQL. Architecture tests. UI unit tests (Vitest). UI e2e tests (Playwright). Axe accessibility tests. k6 load tests. Agent quality evaluation script. Coverage gates. BUT: coverage is below target. Persistence layer severely under-tested. Some test infrastructure (coverage directories, result files) is checked into the repo, suggesting test hygiene issues.

**Recommendations:** Clean up coverage artifacts from the repo. Focus coverage effort on Persistence. *V1 fixable.*

---

### 2.35 Stickiness
- **Score:** 65
- **Weight:** 1
- **Weighted contribution:** 0.65 / 1.00
- **Weighted deficiency:** 0.35

**Justification:** Committed manifests create version history. Governance approvals create audit trail that is hard to replicate elsewhere. Comparison/replay features build on historical data. Advisory scans and digests create ongoing value. Knowledge graph accumulates context. The product becomes more valuable over time as the architecture history grows. But no customer has experienced this retention effect yet.

**Recommendations:** Design a "cost of switching" narrative for sales. *V1.1.*

---

### 2.36 Template and Accelerator Richness
- **Score:** 50
- **Weight:** 1
- **Weighted contribution:** 0.50 / 1.00
- **Weighted deficiency:** 0.50

**Justification:** One sample architecture request template (Greenfield web app). One synthetic case study (Contoso Retail). Finding engine template with test project. Integration recipe documentation. BUT: only one preset/template means every customer beyond the demo scenario must start from scratch. No industry-specific templates. No compliance-framework-specific policy packs shipped by default.

**Tradeoffs:** Building templates without knowing what real customers need vs. having too few templates to make the product feel ready.

**Recommendations:** Create 3-5 architecture request presets covering common scenarios (cloud migration, microservices decomposition, security review). *V1 fixable.*

---

### 2.37 Accessibility
- **Score:** 72
- **Weight:** 1
- **Weighted contribution:** 0.72 / 1.00
- **Weighted deficiency:** 0.28

**Justification:** WCAG 2.1 AA target. 35 pages scanned with axe-core. eslint-plugin-jsx-a11y. Skip-to-content link. Language attribute. Landmark navigation. Form labels. Focus management. Error regions. Keyboard shortcuts. No known exemptions. accessibility@archlucid.net alias configured. Merge-blocking CI for critical/serious violations. Good for this stage.

**Recommendations:** Commission a manual WCAG audit with assistive technology users. *V1.1.*

---

### 2.38 Customer Self-Sufficiency
- **Score:** 58
- **Weight:** 1
- **Weighted contribution:** 0.58 / 1.00
- **Weighted deficiency:** 0.42

**Justification:** Comprehensive documentation exists but is overwhelming in volume (522 files). Five-doc spine helps. In-product wizard and checklist guide the first run. Troubleshooting guide exists. Support bundle for diagnostics. BUT: no in-product help system. No knowledge base. No community forum. No chatbot. Customer support path is unclear (email only via security@archlucid.net).

**Recommendations:** Add a customer support email/path distinct from security reporting. Create a FAQ from common pilot questions. *V1 fixable.*

---

### 2.39 Change Impact Clarity
- **Score:** 70
- **Weight:** 1
- **Weighted contribution:** 0.70 / 1.00
- **Weighted deficiency:** 0.30

**Justification:** CHANGELOG.md with detailed entries. BREAKING_CHANGES.md. API versioning with supported-versions headers. Manifest diff service for two-run comparison. Golden manifest versioning with increment. Governance lineage service. Strong version tracking for architecture artifacts. Good change communication infrastructure.

**Recommendations:** Publish CHANGELOG as a release notes page on the marketing site. *V1 fixable.*

---

### 2.40 Modularity
- **Score:** 78
- **Weight:** 1
- **Weighted contribution:** 0.78 / 1.00
- **Weighted deficiency:** 0.22

**Justification:** ~50 projects with clear boundaries. Core has no persistence references. Contracts separated from implementation. Finding engine template for extensibility. Storage provider abstraction (InMemory/Sql). Agent execution mode abstraction (Simulator/Real). Hosting role separation (Api/Worker/Combined). Well-structured.

**Recommendations:** No immediate action needed.

---

### 2.41 Extensibility
- **Score:** 68
- **Weight:** 1
- **Weighted contribution:** 0.68 / 1.00
- **Weighted deficiency:** 0.32

**Justification:** Finding engine template for custom engines. Integration event schema versioning. Webhook recipes. Policy pack system with custom packs. Configuration-driven feature toggling. Agent abstraction for potential new agent types. BUT: no plugin API. No SDK for external extensions. Custom finding engines require .NET knowledge and repo access.

**Recommendations:** Document the finding engine extension point as a public API contract. *V1.1.*

---

### 2.42 Evolvability
- **Score:** 72
- **Weight:** 1
- **Weighted contribution:** 0.72 / 1.00
- **Weighted deficiency:** 0.28

**Justification:** 32 ADRs showing architectural evolution. Coordinator strangler pattern (4 ADRs) demonstrates ability to evolve safely. API versioning infrastructure. Database migration via DbUp with ordered scripts (001-109+). V1 scope contract explicitly separates current from future. Deferred items documented with clear boundaries. Commercial boundary hardening sequence shows evolution planning. Schema versioning for integration events.

**Recommendations:** No immediate action needed.

---

### 2.43 Documentation
- **Score:** 75
- **Weight:** 1
- **Weighted contribution:** 0.75 / 1.00
- **Weighted deficiency:** 0.25

**Justification:** 522 markdown files. Comprehensive coverage of architecture, security, operations, go-to-market, deployment, quality. Five-doc spine for navigation. CI guards on doc hygiene (scope headers, root budget, link integrity, naming conventions). Architecture poster (C4). AsyncAPI spec. OpenAPI via Swagger. The volume is both a strength (completeness) and weakness (discoverability, maintenance burden).

**Recommendations:** Audit docs for staleness and consolidate redundant files. *V1 fixable.*

---

### 2.44 Azure Ecosystem Fit
- **Score:** 78
- **Weight:** 1
- **Weighted contribution:** 0.78 / 1.00
- **Weighted deficiency:** 0.22

**Justification:** Azure-primary platform (ADR 0020). Entra ID for identity. SQL Server with RLS. Azure OpenAI. Azure Service Bus. Azure Container Apps. Azure Front Door. Azure Key Vault. Application Insights. Azure Blob Storage. Azurite for local dev. Terraform modules for all Azure services. Azure Marketplace SaaS offer planned. Logic Apps for edge orchestration. Cosmos DB optional integration. Strong Azure alignment.

**Recommendations:** Validate Azure Marketplace listing publication. *V1 fixable.*

---

### 2.45 Cognitive Load
- **Score:** 52
- **Weight:** 1
- **Weighted contribution:** 0.52 / 1.00
- **Weighted deficiency:** 0.48

**Justification:** The operator UI's role-aware shaping system is complex: two disclosure tiers, three authority ranks, four UI shaping surfaces, deprecated shim names, 15+ Vitest regression files for seam maintenance. The UI README's shaping section alone is several thousand words. 60+ configuration keys. 522 doc files. The product attempts to solve this with progressive disclosure and wizard guidance, but the underlying model imposes significant cognitive overhead on contributors and advanced operators. First-time users may not feel this, but anyone maintaining or extending the system will.

**Tradeoffs:** Enterprise-ready role-based UX creates inherent complexity. The team has invested heavily in documentation and tests to manage it, but the complexity is structural.

**Recommendations:** Create a simplified contributor onboarding that hides role-aware shaping details until needed. *V1 fixable.*

---

### 2.46 Cost-Effectiveness
- **Score:** 68
- **Weight:** 1
- **Weighted contribution:** 0.68 / 1.00
- **Weighted deficiency:** 0.32

**Justification:** Golden cohort cost dashboard in Terraform. Budget probes with kill-switch (warn 80% / kill 95%). Per-tenant cost model documented. LLM cost estimation with configurable USD/token rates. Pilot profile for cost-aware deployment. Consumption budget Terraform resources. Token quota tracking. Simulator mode eliminates LLM cost during development. Good cost awareness infrastructure.

**Recommendations:** Validate per-tenant cost model against actual Azure billing. *V1.1.*

---

## Weighted Readiness Calculation

| Quality | Score | Weight | Weighted |
|---------|-------|--------|----------|
| Marketability | 52 | 8 | 4.16 |
| Time-to-Value | 60 | 7 | 4.20 |
| Adoption Friction | 55 | 6 | 3.30 |
| Proof-of-ROI Readiness | 62 | 5 | 3.10 |
| Executive Value Visibility | 65 | 4 | 2.60 |
| Differentiability | 70 | 4 | 2.80 |
| Correctness | 72 | 4 | 2.88 |
| Architectural Integrity | 78 | 3 | 2.34 |
| Security | 75 | 3 | 2.25 |
| Traceability | 80 | 3 | 2.40 |
| Usability | 68 | 3 | 2.04 |
| Workflow Embeddedness | 58 | 3 | 1.74 |
| Trustworthiness | 60 | 3 | 1.80 |
| Reliability | 68 | 2 | 1.36 |
| Data Consistency | 70 | 2 | 1.40 |
| Maintainability | 72 | 2 | 1.44 |
| Explainability | 76 | 2 | 1.52 |
| AI/Agent Readiness | 72 | 2 | 1.44 |
| Azure Compat & SaaS Deploy | 55 | 2 | 1.10 |
| Auditability | 82 | 2 | 1.64 |
| Policy & Governance Alignment | 74 | 2 | 1.48 |
| Compliance Readiness | 58 | 2 | 1.16 |
| Procurement Readiness | 62 | 2 | 1.24 |
| Interoperability | 65 | 2 | 1.30 |
| Decision Velocity | 68 | 2 | 1.36 |
| Commercial Packaging Readiness | 58 | 2 | 1.16 |
| Availability | 62 | 1 | 0.62 |
| Performance | 70 | 1 | 0.70 |
| Scalability | 62 | 1 | 0.62 |
| Supportability | 72 | 1 | 0.72 |
| Manageability | 68 | 1 | 0.68 |
| Deployability | 60 | 1 | 0.60 |
| Observability | 78 | 1 | 0.78 |
| Testability | 70 | 1 | 0.70 |
| Stickiness | 65 | 1 | 0.65 |
| Template & Accelerator Richness | 50 | 1 | 0.50 |
| Accessibility | 72 | 1 | 0.72 |
| Customer Self-Sufficiency | 58 | 1 | 0.58 |
| Change Impact Clarity | 70 | 1 | 0.70 |
| Modularity | 78 | 1 | 0.78 |
| Extensibility | 68 | 1 | 0.68 |
| Evolvability | 72 | 1 | 0.72 |
| Documentation | 75 | 1 | 0.75 |
| Azure Ecosystem Fit | 78 | 1 | 0.78 |
| Cognitive Load | 52 | 1 | 0.52 |
| Cost-Effectiveness | 68 | 1 | 0.68 |
| **TOTAL** | | **102** | **65.38** |

**Weighted Overall Readiness: 65.38%** (6669 / 102 x 100)

---

## 3. Top 10 Most Important Weaknesses

1. **The hosted SaaS funnel is not operational.** staging.archlucid.net does not resolve. This blocks all self-service acquisition, trial conversion, and live product demonstration. Every marketing, sales, and GTM investment is wasted until this works. This is not a code quality issue -- it is an infrastructure/operations gap.

2. **Zero real customers or reference deployments exist.** All evidence is synthetic (Contoso demo), self-assessed, or modeled. No buyer, operator, or auditor has validated the product in real use. This makes every quality claim unfalsifiable and every ROI figure theoretical.

3. **Third-party security attestation is absent.** No SOC 2 report. No completed pen-test. No independent security review results. Enterprise procurement will not proceed without at least one of these. The pen-test SoW is awarded but not executed.

4. **Commercial tier enforcement is incomplete and non-converting.** `[RequiresCommercialTenantTier]` returns 404 (not 402) intentionally, but this means lower-tier users get a generic "not found" with no upgrade prompt. Combined with no validated Stripe checkout and no published Marketplace listing, the product cannot generate revenue even if someone wanted to pay.

5. **Code coverage is below CI gates.** 73% line / 59% branch vs. 79%/63% gates. Persistence at ~40% is severely under-tested. This means the CI gates are aspirational rather than enforced, which undermines the testing discipline the project otherwise demonstrates.

6. **Agent output quality is unvalidated in production.** Four agents produce findings, but output quality is measured only by self-implemented structural/semantic scoring. No human evaluation. No external benchmark. No A/B comparison. Buyers must trust the AI output based on demo data alone.

7. **Documentation volume creates cognitive overhead.** 522 markdown files is extraordinary for a pre-GA product. While individually well-structured, the mass itself is a burden: contributors struggle to find the right doc, naming conventions are complex, and staleness risk is high. The five-doc spine and CI guards help but do not eliminate the problem.

8. **First-run experience depends on infrastructure that is not validated.** The intended buyer path (sign up at archlucid.net -> trial -> wizard -> first run) is designed but not functional. The fallback path (Docker + .NET SDK) requires developer tooling that enterprise architects may not have.

9. **Role-aware UI shaping is over-engineered for V1.** The shaping system (two disclosure tiers, three authority ranks, four shaping surfaces, deprecated shim names, 15+ regression test files) is enterprise-grade complexity for a product with zero users. This complexity slows feature development and increases maintenance burden without providing proportional V1 value.

10. **No incident response or operational muscle exists.** Runbooks are documented but never tested. No on-call rotation. No incident communication has ever been sent. No monitoring alerts have ever fired against real traffic. The operational muscle is entirely theoretical.

---

## 4. Top 5 Monetization Blockers

1. **Self-service trial funnel is broken.** staging.archlucid.net does not resolve. Without a working hosted trial, the product cannot acquire customers without manual sales intervention. This is the #1 revenue blocker.

2. **Stripe checkout is untested end-to-end.** The billing code exists (BillingProductionSafetyRules, webhook handlers, Marketplace abstraction) but has never processed a real or test payment through a reachable endpoint. No checkout = no revenue.

3. **No reference customer for social proof.** Enterprise buyers need to see that someone else succeeded first. Synthetic case studies (Contoso) are not credible substitutes. The ROI model is unvalidated. Without at least one publishable pilot outcome, each new prospect must be convinced from zero.

4. **Commercial tier enforcement does not drive upgrade behavior.** 404 masking prevents lower-tier users from knowing a capability exists behind an upgrade. No "upgrade to unlock" prompt. No in-product upsell path. Even if billing worked, conversion signals are suppressed.

5. **Marketplace listing is not published.** Azure Marketplace SaaS offer documentation and CI guards exist, but no listing is live. This blocks the Azure-native channel (co-sell, Marketplace transact, MACC commitment credits) that the Azure-first strategy depends on.

---

## 5. Top 5 Enterprise Adoption Blockers

1. **No SOC 2 or equivalent attestation.** The self-assessment is honest and the gap register is transparent, but enterprise procurement teams need a third-party opinion letter. Type I is funded but not started. Until this exists, ArchLucid is disqualified from many enterprise RFPs.

2. **No completed pen-test.** The SoW is awarded (Aeronova Red Team LLC, kickoff 2026-05-06) but results do not exist. Security reviewers will ask for this and receive a placeholder. The 2026-Q2 redacted summary template is ready but empty.

3. **SSO federation is untested beyond Entra ID.** Okta and Auth0 configuration guides exist as documentation, but no integration test validates these paths. Enterprise buyers with non-Microsoft identity providers will require proof.

4. **No SCIM provisioning validation.** ADR 0032 describes the SCIM v2 service provider design, and code exists, but end-to-end provisioning with a real identity provider has not been demonstrated. Enterprises with automated user lifecycle management will require this.

5. **Data residency and multi-region are documented but unproven.** Terraform modules exist for secondary region and SQL failover. BUT: no multi-region deployment has been validated. Enterprise buyers with data sovereignty requirements need proof, not plans.

---

## 6. Top 5 Engineering Risks

1. **Persistence layer is severely under-tested (40% coverage).** This is the data path -- runs, manifests, governance records, audit events all flow through `ArchLucid.Persistence`. A bug here corrupts customer data. The 63% per-package gate is not being met, meaning CI is either bypassing the gate or the gate is not enforced on this package.

2. **Manifest finalization concurrency under real load is untested.** The SQL error code approach (50001-50006) for conflict detection is well-designed but tested only in unit tests with mocked repositories. Under real concurrent commits, SQL Server behavior (lock escalation, deadlocks, RLS interaction) may differ. The transactional boundary spans multiple tables.

3. **LLM prompt injection defense is fixture-only.** The CI prompt injection test (`eval_agent_quality.py --prompt-injection-only`) validates against a fixed fixture set. Adversarial prompt injection is a moving target. Content safety guard (Azure AI Content Safety) is fail-closed in production, which is good, but the prompt redaction deny-list approach may miss novel attack vectors.

4. **DbUp migration chain integrity at scale.** 109+ ordered SQL migration scripts run on startup. If a migration fails partway through, the API throws and does not start (correct fail-fast behavior). BUT: rollback is not automated. A bad migration in production requires manual intervention. No migration dry-run or pre-flight check exists.

5. **Coordinator strangler migration is incomplete.** ADRs 0021, 0028, 0029, 0030 document a multi-phase strangler pattern migrating from coordinator to authority pipeline. The migration is in progress. Incomplete strangler migrations create dual-path code that is harder to test and reason about. Until this completes, the codebase has unnecessary complexity.

---

## 7. Most Important Truth

**ArchLucid has built the infrastructure for a commercially viable enterprise product but has not yet proven it works outside its own development environment.** The engineering is mature, the documentation is exhaustive, and the architectural decisions are sound. But the staging environment does not resolve, no customer has used the product, no auditor has reviewed it, and no payment has been processed. The gap is not capability -- it is operational proof. The single highest-leverage action is making the hosted SaaS funnel work and getting one real customer through a complete pilot.

---

## 8. Top Improvement Opportunities

### Improvement 1: Make staging.archlucid.net resolvable and validate the full SaaS funnel

**Why it matters:** Every commercial, enterprise, and trust metric depends on having a reachable product. Zero progress on customer acquisition, trial conversion, or procurement evidence is possible while the hosted environment is unreachable.

**Expected impact:** Unblocks trial signups, demo links, synthetic probe monitoring, and Stripe checkout validation. Directly improves Marketability (+15-20 pts), Time-to-Value (+10-15 pts), Adoption Friction (+10 pts), Azure Compat & SaaS Deploy (+15-20 pts), Deployability (+10 pts), Availability (+10 pts). Weighted readiness impact: +3.0-5.0%.

**Affected qualities:** Marketability, Time-to-Value, Adoption Friction, Azure Compatibility and SaaS Deployment Readiness, Deployability, Availability, Commercial Packaging Readiness, Executive Value Visibility.

**Status:** DEFERRED

**Reason:** This requires Azure subscription access, DNS configuration, and Front Door custom domain setup. The specific Azure subscription, resource group, and DNS zone are not visible in the codebase -- only referenced in Terraform variables and `apply-saas.ps1`.

**Information needed:** (1) Which Azure subscription and resource group host the staging environment. (2) What DNS provider manages archlucid.net. (3) Whether `infra/apply-saas.ps1` has ever been successfully executed and what blocked it. (4) Whether Azure Container Apps are deployed and what image registry they pull from.

---

### Improvement 2: Lift ArchLucid.Persistence test coverage from ~40% to 63%+

**Why it matters:** The persistence layer is the data integrity backbone. Every run, manifest, audit event, and governance record passes through it. At 40% coverage, significant code paths are untested, including SQL-backed repositories that handle real transactions, RLS enforcement, and concurrent access patterns.

**Expected impact:** Directly improves Data Consistency (+8-10 pts), Correctness (+5-8 pts), Reliability (+5 pts), Testability (+5 pts). Weighted readiness impact: +0.6-1.0%.

**Affected qualities:** Data Consistency, Correctness, Reliability, Testability, Maintainability.

**Status:** Actionable now.

**Cursor prompt:**

```
Add unit and integration tests to lift ArchLucid.Persistence test coverage from ~40% line to at least 63% line.

Focus areas (in priority order):
1. ArchLucid.Persistence/Repositories/ — test every public method of SqlRunRepository, SqlGoldenManifestRepository, SqlFindingsSnapshotRepository, SqlAuditRepository, and SqlGovernanceRepository against the InMemory provider (for unit tests) and SQL Server (for integration tests gated on ARCHLUCID_SQL_TEST).
2. ArchLucid.Persistence/Migrations/ — verify that the DbUp migration runner handles both fresh (greenfield) and incremental scenarios without error.
3. ArchLucid.Persistence/Data/ — test Dapper mapping for any custom type handlers or query builders.

Constraints:
- Do NOT modify any production code in ArchLucid.Persistence — only add test files under ArchLucid.Persistence.Tests.
- Integration tests that require SQL must be gated with [Trait("Category", "SqlIntegration")] and skip when ARCHLUCID_SQL_TEST is not set.
- Do NOT use ConfigureAwait(false) in tests.
- Each test class must be in its own file.
- Use primary constructors where appropriate.
- Follow the existing test patterns in ArchLucid.Persistence.Tests for fixture and factory setup.

Acceptance criteria:
- Running `dotnet test ArchLucid.Persistence.Tests` with ARCHLUCID_SQL_TEST set produces >=63% line coverage for the ArchLucid.Persistence assembly.
- All new tests pass locally.
- No existing tests are modified or broken.
```

---

### Improvement 3: Validate Stripe TEST checkout end-to-end

**Why it matters:** Revenue generation requires a working payment path. The billing code exists but has never processed a payment through a reachable endpoint. Validating this removes the #2 monetization blocker.

**Expected impact:** Directly improves Commercial Packaging Readiness (+10-15 pts), Marketability (+5-8 pts), Procurement Readiness (+3-5 pts). Weighted readiness impact: +0.5-0.8%.

**Affected qualities:** Commercial Packaging Readiness, Marketability, Procurement Readiness, Time-to-Value.

**Status:** DEFERRED

**Reason:** Requires a reachable staging environment (blocked on Improvement 1) and a Stripe TEST mode API key configured in the staging environment. The Stripe account setup and key provisioning require owner action.

**Information needed:** (1) Whether a Stripe account exists with TEST mode enabled. (2) Whether `STRIPE_SECRET_KEY` and `STRIPE_WEBHOOK_SECRET` are configured in staging Key Vault. (3) Whether the pricing page at staging.archlucid.net/pricing is wired to create Stripe checkout sessions.

---

### Improvement 4: Create 3-5 architecture request presets for common scenarios

**Why it matters:** Currently only one sample preset exists (Greenfield web app). Every prospect beyond the demo scenario must craft their own architecture request from scratch, which increases time-to-value and adoption friction. Pre-built presets covering common enterprise architecture scenarios dramatically reduce the barrier to first meaningful output.

**Expected impact:** Directly improves Template & Accelerator Richness (+20-25 pts), Time-to-Value (+5-8 pts), Adoption Friction (+5 pts), Marketability (+3-5 pts). Weighted readiness impact: +0.6-1.0%.

**Affected qualities:** Template and Accelerator Richness, Time-to-Value, Adoption Friction, Marketability, Customer Self-Sufficiency.

**Status:** Actionable now.

**Cursor prompt:**

```
Create 4 new architecture request preset JSON files under templates/architecture-requests/ alongside any existing presets. Each preset must be a valid architecture request body that can be submitted to POST /v1/architecture/request.

Create the following presets:

1. cloud-migration-lift-and-shift.json — A 3-tier .NET monolith on-premises being migrated to Azure with minimal refactoring (App Service, Azure SQL, Azure Storage). Include typical constraints: maintain existing auth, minimize downtime, budget ceiling $50K/year.

2. microservices-decomposition.json — An e-commerce platform breaking a monolith into microservices (order service, inventory service, payment service, notification service). Include Azure Container Apps hosting, Service Bus for async messaging, API Management for gateway.

3. data-platform-modernization.json — A legacy data warehouse being modernized to a lakehouse architecture (Azure Data Lake, Synapse Analytics, Event Hubs for real-time ingestion). Include compliance constraints: GDPR data residency, PII handling, 7-year retention.

4. zero-trust-network-architecture.json — A regulated financial services company implementing zero-trust networking (Azure Front Door, Private Link, NSGs, Azure Firewall, Conditional Access). Include SOC 2 and PCI-DSS compliance requirements.

Each preset must include:
- A realistic "brief" field (3-5 sentences describing the architecture request).
- A "systemContext" object with relevant technology stack, team size, and constraints.
- A "complianceRequirements" array where relevant.

Also create templates/architecture-requests/README.md listing all available presets with one-sentence descriptions and a note that operators can use these via the New Run wizard or API.

Constraints:
- JSON must be valid and parseable.
- Field names must match the existing ArchitectureRequest contract (check ArchLucid.Contracts for the schema).
- Do NOT modify any existing preset files.
- Keep each preset under 80 lines of JSON.

Acceptance criteria:
- Each preset can be submitted to a running API and produces a valid run.
- README.md lists all presets including the existing one.
```

---

### Improvement 5: Simplify first-run contributor onboarding documentation

**Why it matters:** 522 docs files creates severe cognitive load for new contributors. The five-doc spine helps but contributors still encounter dense role-aware shaping documentation, deprecated shim references, and cross-linked packaging semantics when they try to make their first change. A simplified contributor onboarding path that hides advanced complexity reduces onboarding time and maintenance burden.

**Expected impact:** Directly improves Cognitive Load (+8-10 pts), Customer Self-Sufficiency (+5-8 pts), Maintainability (+3-5 pts), Usability (+3 pts). Weighted readiness impact: +0.3-0.5%.

**Affected qualities:** Cognitive Load, Customer Self-Sufficiency, Maintainability, Documentation, Usability.

**Status:** Actionable now.

**Cursor prompt:**

```
Create docs/library/CONTRIBUTOR_QUICK_START.md — a concise (under 100 lines) contributor quick-start that gets a new developer from clone to running tests in under 15 minutes.

Structure:
1. Prerequisites (3-5 bullet points: .NET 10 SDK, Docker, Node 22, SQL Server via Docker)
2. Clone and build (3 commands max)
3. Start dependencies: `docker compose up -d` (SQL + Azurite + Redis)
4. Run fast tests: `dotnet test ArchLucid.sln --filter "Category!=Slow & Category!=SqlIntegration"`
5. Start the API: `dotnet run --project ArchLucid.Api`
6. Start the UI: `cd archlucid-ui && npm ci && npm run dev`
7. First change tutorial: "Add a comment to any test file, run `dotnet test <that-project>`, verify it passes"
8. Where to go next: link to ARCHITECTURE_INDEX.md and CONTRIBUTOR_PERSONA_TABLE.md

The doc scope header must be: `> **Scope:** New contributor quick-start — clone to running tests in 15 minutes.`

Constraints:
- Do NOT reference role-aware shaping, commercial packaging, or UI authority composition — those are advanced topics.
- Do NOT duplicate content from existing docs — link to INSTALL_ORDER.md, FIRST_30_MINUTES.md for details.
- Keep the language direct and action-oriented.
- Maximum 100 lines including headers and blank lines.
- Do NOT modify any existing files.

Acceptance criteria:
- A new developer with the prerequisites installed can follow the doc from top to bottom and have a green test run + running API + running UI within 15 minutes.
- No broken links.
- Passes `python scripts/ci/check_doc_scope_header.py` (has scope header on first non-empty line).
```

---

### Improvement 6: Add customer support contact path distinct from security reporting

**Why it matters:** The only published contact is security@archlucid.net for vulnerability reports and accessibility@archlucid.net for a11y barriers. No customer support channel exists. Pilot customers need a clear path to report issues, ask questions, and request help that is distinct from security disclosure.

**Expected impact:** Directly improves Customer Self-Sufficiency (+8-10 pts), Supportability (+5 pts), Marketability (+2-3 pts), Trustworthiness (+2-3 pts). Weighted readiness impact: +0.2-0.4%.

**Affected qualities:** Customer Self-Sufficiency, Supportability, Marketability, Trustworthiness.

**Status:** Actionable now.

**Cursor prompt:**

```
Add a customer support contact path:

1. In SECURITY.md, add a section after "Reporting vulnerabilities" titled "Product support" with:
   - "For product questions, pilot support, or issue reports, email **support@archlucid.net**."
   - "This is not for security vulnerabilities — use security@archlucid.net for those."

2. In docs/TROUBLESHOOTING.md (if it exists) or docs/library/PILOT_GUIDE.md, add a "Getting help" section at the bottom:
   - Support email: support@archlucid.net
   - Include version (`GET /version`), correlation ID, and support bundle when reporting issues
   - Link to docs/library/PILOT_GUIDE.md#when-you-report-an-issue for the reporting template

3. In archlucid-ui/public/.well-known/security.txt, if it exists, add a comment line:
   # Product support: support@archlucid.net

Constraints:
- Do NOT modify the vulnerability reporting process.
- Do NOT create new files unless TROUBLESHOOTING.md doesn't exist (in which case add the section to PILOT_GUIDE.md).
- Keep additions concise (3-5 lines per file).

Acceptance criteria:
- At least two user-facing documents mention support@archlucid.net as the product support contact.
- security@archlucid.net remains the vulnerability disclosure path.
- No existing content is removed or reworded.
```

---

### Improvement 7: Add concurrency tests for manifest finalization

**Why it matters:** ManifestFinalizationService is the critical transactional boundary — it commits golden manifests, persists findings snapshots, writes decision traces, and publishes integration events within a SQL transaction. The SQL error code approach (50001-50006) handles conflicts but is tested only with mocked repositories. Real concurrent commits with RLS, lock escalation, and deadlock potential are untested.

**Expected impact:** Directly improves Correctness (+5-8 pts), Reliability (+5 pts), Data Consistency (+5 pts). Weighted readiness impact: +0.4-0.6%.

**Affected qualities:** Correctness, Reliability, Data Consistency, Testability.

**Status:** Actionable now.

**Cursor prompt:**

```
Add concurrency tests for ManifestFinalizationService in ArchLucid.Application.Tests/Runs/Finalization/.

Create a new file: ManifestFinalizationConcurrencyTests.cs

Test scenarios:
1. Two concurrent commits to the same run — one should succeed, the other should get SqlConcurrencyConflict (error 50006) or SqlCommittedDifferentManifest (50002).
2. Two concurrent commits to different runs — both should succeed independently.
3. Commit while run is in an unexpected status — should get SqlBadRunStatus (50003).
4. Rapid sequential commits to the same run — verify idempotency (same manifest hash = success, different hash = conflict).

Implementation approach:
- Use the InMemory provider with Thread-safe wrappers OR the SQL integration path gated on ARCHLUCID_SQL_TEST.
- For SQL tests, use Task.WhenAll with two concurrent FinalizeAsync calls sharing the same runId.
- Verify that exactly one call returns Committed and the other returns the appropriate conflict result.
- Use [Trait("Category", "Slow")] for tests that exercise real concurrency.

Constraints:
- Do NOT modify ManifestFinalizationService.cs or any production code.
- Each test class in its own file.
- Use primary constructors.
- Do NOT use ConfigureAwait(false).
- Follow existing test patterns in the same directory (ManifestFinalizationServiceTests.cs) for fixture setup.

Acceptance criteria:
- At least 4 concurrency scenarios tested.
- Tests pass reliably (no flaky timing-dependent assertions — use deterministic synchronization or retry with bounded time).
- Tests document expected behavior in XML comments.
```

---

### Improvement 8: Create a customer FAQ from common pilot evaluation questions

**Why it matters:** New evaluators ask the same questions: "Does it work with our IdP?", "What data do you store?", "Can I export my data?", "What happens when the trial ends?". A FAQ reduces support burden, improves self-sufficiency, and signals product maturity.

**Expected impact:** Directly improves Customer Self-Sufficiency (+8-10 pts), Adoption Friction (+3-5 pts), Marketability (+2-3 pts). Weighted readiness impact: +0.2-0.4%.

**Affected qualities:** Customer Self-Sufficiency, Adoption Friction, Marketability, Time-to-Value.

**Status:** Actionable now.

**Cursor prompt:**

```
Create docs/library/FAQ.md — a customer-facing FAQ document covering the most common pilot evaluation questions.

Structure the FAQ in these sections:

## Getting Started
- Q: How do I start a trial? → Link to TRIAL_AND_SIGNUP.md, note 14-day trial, no credit card.
- Q: What are the system requirements? → Hosted SaaS (browser only) or self-hosted (.NET 10, Docker, SQL Server).
- Q: How long does first value take? → Link to CORE_PILOT.md, note ~2 minutes to committed manifest.

## Security and Compliance
- Q: What authentication does ArchLucid support? → Entra ID (JWT), API key, SSO via Entra. Link to SECURITY.md.
- Q: Is data encrypted? → At rest (SQL TDE, Azure Storage encryption) and in transit (HTTPS/TLS). Link to trust-center.md.
- Q: Do you have SOC 2? → Self-assessment available; Type I engagement funded for 2026. Link to SOC2_SELF_ASSESSMENT_2026.md.
- Q: Can I export my data? → Yes: API, CLI, CSV/JSON audit export, DOCX/ZIP artifact exports.

## Integration
- Q: Does ArchLucid integrate with Jira/ServiceNow? → V1: via webhooks/API. V1.1: first-party connectors planned. Link to V1_DEFERRED.md §6.
- Q: What about SSO with Okta? → Configuration guide available. Link to SSO_OKTA_CONFIGURATION.md.

## Pricing and Licensing
- Q: What does it cost? → Link to PRICING_PHILOSOPHY.md for tiers. Do NOT restate prices.
- Q: What happens when the trial ends? → 7-day read-only, 30-day data export, then deletion.

## Product
- Q: Is this just ChatGPT for architecture? → No — structured multi-agent pipeline with governance, audit trail, and explainability. Link to POSITIONING.md.
- Q: Can I use my own LLM? → Azure OpenAI is the supported provider. Multi-vendor support exists in code (FallbackAgentCompletionClient).

Doc scope header: `> **Scope:** Customer-facing FAQ for pilot evaluation — common questions and canonical answers with links.`

Constraints:
- Do NOT restate pricing numbers — link to PRICING_PHILOSOPHY.md.
- Every answer should be 1-3 sentences max, with a link to the authoritative source.
- Do NOT invent capabilities that are not in V1_SCOPE.md.
- Maximum 120 lines.

Acceptance criteria:
- At least 12 Q&A pairs covering the four sections.
- All links resolve to existing files.
- Passes doc scope header CI check.
- No pricing numbers appear in the file.
```

---

### Improvement 9 (DEFERRED): Complete pen-test engagement and publish redacted summary

**Title:** DEFERRED — Complete pen-test engagement and publish results

**Reason:** The pen-test SoW is awarded to Aeronova Red Team LLC with kickoff 2026-05-06. This is an external engagement that cannot be executed by Cursor.

**Information needed:** (1) Confirmation that the kickoff occurred on schedule. (2) The redacted summary document when the engagement completes. (3) Any material findings that require code remediation before the summary can be published.

---

### Improvement 10 (DEFERRED): Validate SCIM v2 provisioning end-to-end with a real IdP

**Title:** DEFERRED — Validate SCIM provisioning with Okta or Azure AD

**Reason:** SCIM v2 service provider code exists (ADR 0032, ScimPatchOpEvaluatorTests, ScimFilterParserTests), but end-to-end validation requires an external IdP (Okta or Azure AD) configured for SCIM provisioning against ArchLucid's SCIM endpoint. This requires IdP account access and configuration that is not available in the codebase.

**Information needed:** (1) Whether an Okta or Azure AD test tenant is available. (2) The SCIM endpoint URL path configured in the API. (3) Whether the SCIM endpoint requires a bearer token or API key for the provisioning agent.

---

## 9. Deferred Scope Uncertainty

Items explicitly marked as deferred to V1.1 or V2 were located and verified in `docs/library/V1_DEFERRED.md`:
- ITSM connectors (Jira, ServiceNow, Confluence) -> V1.1 (documented in V1_DEFERRED §6)
- Slack connector -> V2 (documented in V1_DEFERRED §6a)
- Cross-tenant analytics -> deferred (documented in V1_DEFERRED §1)
- Product learning planning bridge -> deferred (documented in V1_DEFERRED §1)

No scoring penalty was applied for these deferred items.

---

## 10. Pending Questions for Later

### Improvement 1 (Staging Funnel)
- Has `infra/apply-saas.ps1` ever been executed successfully?
- What Azure subscription hosts the staging resources?
- What DNS provider manages archlucid.net?
- Are Container Apps deployed? What image do they pull?

### Improvement 3 (Stripe Checkout)
- Does a Stripe account exist with TEST mode enabled?
- Are `STRIPE_SECRET_KEY` and `STRIPE_WEBHOOK_SECRET` configured in staging Key Vault?
- Has the `/pricing` page ever rendered in a browser against a live backend?

### General
- What is the current CI status on the main branch? Are the coverage gates being enforced or bypassed?
- Has any external party (advisor, pilot prospect, investor) seen the product running?
- Is there a target date for first customer pilot?
- What is the operational budget for Azure hosting (staging + production)?
