# ArchLucid Assessment – Weighted Readiness 61.91%

**Assessor:** Independent first-principles analysis (Opus 4.6)
**Date:** 2026-04-25
**Codebase snapshot:** As of 2026-04-25 10:45 AM ET
**Method:** Direct inspection of all source projects, documentation, infrastructure, tests, CI/CD, and go-to-market materials. No prior assessment scores referenced. No subagents used.

---

## 1. Executive Summary

### Overall Readiness

ArchLucid is a technically ambitious, architecturally disciplined AI-assisted architecture workflow system at approximately 62% weighted readiness (61.91%) for commercial launch. The engineering foundation is substantially built — 30+ .NET projects, ~200+ UI test files, 25 CI workflows, 110 Terraform files across 13+ roots, and a documentation corpus exceeding 450 markdown files. The gap between "technically built" and "buyer-accessible" is the dominant risk. The product works; the deployment of buyer-facing surfaces (live funnel, pricing page, Stripe wiring) is the critical path item.

### Commercial Picture

The product has strong intellectual substance with comprehensive commercial scaffolding. Positioning, packaging, pricing philosophy, ROI models, and go-to-market materials exist as detailed internal documents. The buyer-facing funnel (`archlucid.net/signup → first run`) is not yet live in production. The `<<placeholder copy>>` markers in `BUYER_FIRST_30_MINUTES.md` confirm the self-serve evaluation path remains incomplete. Stripe integration code exists but is not production-wired. Azure Marketplace SaaS fulfillment infrastructure is partially built. Market validation activities (reference customers, case studies, analyst coverage) are explicitly v1.1 scope. The remaining v1 commercial gap is deploying the buyer-facing infrastructure that already exists in code.

### Enterprise Picture

Enterprise-grade infrastructure is impressively deep for a pre-launch product: SQL RLS for tenant isolation, RBAC with three authority levels, append-only durable audit with 78+ typed event constants, governance workflows with approval SLAs, policy packs, SCIM 2.0 design (V1.1), SOC 2 self-assessment, STRIDE threat model, trust center with evidence pack download, DPA template, CAIQ/SIG pre-fills, and a pen-test SoW awarded. Third-party validation activities (pen test completion, SOC 2 attestation, PGP key publication) are v1.1. The v1 enterprise posture is the self-asserted trust infrastructure, which is comprehensive and honestly labeled. The remaining v1 gap is the privacy notice (owner-blocked).

### Engineering Picture

The engineering is the strongest dimension. Clean architecture with proper bounded contexts (Core, Contracts, Decisioning, Persistence, Application, Api, AgentRuntime, KnowledgeGraph, Provenance, ArtifactSynthesis, ContextIngestion, Coordinator). Dapper for lightweight data access. DbUp migrations. Primary constructors and modern C# idioms. 180+ test classes across .NET, 74+ `.test.ts` and 114+ `.test.tsx` files on the UI side. Stryker mutation testing configs. k6 load tests. Schemathesis API fuzzing. Simmy chaos testing. Architecture guard tests (`ArchLucid.Architecture.Tests`). CycloneDX SBOM generation. The CI pipeline is genuinely mature — tiered from secret scanning through chaos injection. The main engineering concern is that the system's correctness under real LLM workloads (non-simulator) is undertested in CI, and the coordinator/authority pipeline is architecturally complex enough that its behavior under edge cases deserves more hardening.

---

## 2. Weighted Quality Assessment

**Total weight:** 102. Each quality's weighted contribution = (Score × Weight). Readiness = Σ(Score × Weight) / (100 × 102) × 100.

**V1.1 scope exclusion (2026-04-25):** Reference customers, case studies, testimonials, analyst coverage, conference presence, and market-validated buyer perception testing are explicitly out of scope for v1 and in scope for v1.1. These items do not reduce the v1 readiness score.

Qualities are ordered by **weighted deficiency signal** (Weight × (100 − Score) / 100), highest urgency first.

---

### 2.1. Marketability — Score: 53 | Weight: 8 | Weighted Deficiency: 3.76

**Justification:** The product has a clear positioning statement, executive sponsor brief, competitive landscape document, and go-to-market folder with 20+ files. The "AI Architecture Intelligence platform" positioning is clear and the three-pillar value story is well-grounded in code. However, the public-facing SaaS funnel is not live in production. The buyer first-30-minutes document contains `<<placeholder copy>>` markers. There is no evidence of a working `archlucid.net` with actual signup flow. No live pricing page.

**V1.1 scope exclusion:** Reference customers, case studies, testimonials, analyst coverage, conference presence, and market-validated buyer perception testing are explicitly v1.1 activities and are not penalized here.

**Tradeoffs:** The positioning, packaging, and go-to-market documentation are comprehensive for a pre-launch product. The remaining v1 gap is deployment of the buyer-facing infrastructure (funnel, pricing, placeholder copy).

**Recommendations:**
- Make `archlucid.net/signup` live with the Stripe test-mode flow end-to-end
- Replace all `<<placeholder copy>>` markers with real content
- Publish the pricing page on the live marketing site

**Fixability:** V1 (all remaining items)

---

### 2.2. Time-to-Value — Score: 45 | Weight: 7 | Weighted Deficiency: 3.85

**Justification:** The `archlucid try` CLI command gives contributors a single-command first-value loop. The `devcontainer` boots to first value on open. The Docker demo with Contoso seed data exists. But for the actual *buyer* — the person ArchLucid is selling to — time-to-value is undefined. The cloud signup path is not live. The buyer-first-30-minutes document is placeholder-heavy. A buyer who discovers ArchLucid today cannot self-serve into a committed manifest without either (a) cloning the repo and running .NET/Docker or (b) requesting a guided demo. For a SaaS product, this is a critical gap. The internal-operator path is solid; the external-buyer path does not yet exist in production.

**Tradeoffs:** Prioritizing the contributor path first made sense during development. But every week the buyer path stays non-functional, the window for organic adoption narrows.

**Recommendations:**
- Ship the hosted trial funnel to production
- Pre-seed the trial tenant so the first run is visible within 60 seconds of signup
- Wire the first-run wizard with the vertical picker using existing `templates/briefs/` slugs

**Fixability:** V1 (all items are designed and partially implemented)

---

### 2.3. Adoption Friction — Score: 45 | Weight: 6 | Weighted Deficiency: 3.30

**Justification:** For contributors/operators, friction is moderate — Docker compose, SQL Server, .NET 10 SDK, Node 22 are all well-documented with `INSTALL_ORDER.md` and the devcontainer. For buyers, friction is currently infinite — they cannot adopt without developer assistance. The product packaging is two-layer (Pilot/Operate) which is clean, but the Pilot path itself requires understanding run lifecycle, agent types, manifests, and governance concepts before creating value. The wizard exists but is untested with naive users. There is no onboarding wizard validation data. The 14-day trial design exists on paper but not in production.

**Tradeoffs:** Architecture tooling inherently carries domain complexity. But the wizard + pre-seeded sample run pattern is designed to solve this; it just needs to ship.

**Recommendations:**
- Complete the hosted signup → auto-seed → first-value-report loop
- Add a "What you're looking at" contextual help panel on the first committed run
- Measure wizard completion rate once live

**Fixability:** V1

---

### 2.4. Correctness — Score: 72 | Weight: 4 | Weighted Deficiency: 1.12

**Justification:** The decisioning engine (`DecisionEngineV2`) uses weighted argument resolution with deterministic scoring. Finding engines are pluggable via `IFindingEngine` with 10+ implementations. The `FindingsOrchestrator` runs them in parallel. The `ExplainabilityTrace` provides structured audit per finding. Structured explanation parsing gracefully handles malformed LLM output. Schema validation via JSON Schema exists. The golden manifest is a versioned immutable artifact. All of this works under the simulator. The concern is real-LLM correctness: the `golden-cohort-nightly.yml` CI workflow runs simulator drift checks, and `--strict-real` is opt-in. Real-mode execution with actual Azure OpenAI is not part of the default CI gate. Agent output quality scoring exists but the quality gate thresholds are configuration-driven and untested against real-world architecture briefs outside the Contoso demo. Explanation faithfulness checking uses token overlap heuristics — a reasonable v1 approach but not validated against human judgment.

**Tradeoffs:** Simulator-first testing is the right default for cost and determinism. But it means the product's core value proposition (AI-generated architecture findings) is validated against canned responses, not actual LLM behavior.

**Recommendations:**
- Expand golden-cohort real-LLM nightly to cover the 5 most common vertical presets
- Add human-evaluation labels to a small set of real-LLM findings to calibrate the faithfulness checker
- Document the quality gate threshold rationale in an ADR

**Fixability:** V1 (cohort expansion), V1.1 (human eval labels)

---

### 2.5. Proof-of-ROI Readiness — Score: 65 | Weight: 5 | Weighted Deficiency: 1.75

**Justification:** The `PILOT_ROI_MODEL.md` is one of the strongest commercial documents in the repo — it defines baseline questions, measurement criteria, and a sponsor-level value story. The value-report DOCX generation with review-cycle deltas is implemented. The first-value PDF builder exists. The "Day N since first commit" badge is designed. The `archlucid_trial_signup_baseline_skipped_total` metric exists. The ROI measurement infrastructure is comprehensive and ready for data.

**V1.1 scope exclusion:** All pilot validation — including exercising the model against a real pilot, producing actual before/after measurements, and establishing real numbers for metrics like "median hours from architecture request to reviewable package" — is a v1.1 activity. The v1 deliverable is the measurement infrastructure and ROI model design.

**Tradeoffs:** The ROI infrastructure is built to a high standard. The model is credible and well-designed. Its value will be proven when exercised against real pilots in v1.1.

**Recommendations:**
- Verify the value-report DOCX renders correctly from a committed Contoso run
- Have a non-engineer read the first-value PDF and flag unclear language

**Fixability:** V1

---

### 2.6. Executive Value Visibility — Score: 68 | Weight: 4 | Weighted Deficiency: 1.28

**Justification:** The executive sponsor brief is well-written with appropriate hedging ("should show improvement") rather than overclaiming. The sponsor one-pager PDF export exists. The "Email this run to your sponsor" banner is implemented. Value reports with review-cycle deltas are built. The ROI model exists. The `/why-archlucid` page and PDF pack exist. The sponsor narrative surfaces are comprehensive and internally consistent.

**V1.1 scope exclusion:** Validating whether these artifacts actually communicate value to executives — including having any external person confirm they resonate — is a v1.1 activity. All such artifacts are self-generated pre-launch; external validation requires external people. The v1 deliverable is the completeness and internal consistency of the artifact set.

**Tradeoffs:** The artifacts are built, the narrative is calibrated, and the voice is consistent across surfaces. Technical verification (rendering, links) is the remaining v1 concern.

**Recommendations:**
- Verify the sponsor PDF renders correctly from a real committed run
- Verify all links in the `/why-archlucid` pack resolve correctly

**Fixability:** V1

---

### 2.7. Differentiability — Score: 62 | Weight: 4 | Weighted Deficiency: 1.52

**Justification:** The competitive landscape document exists (`COMPETITIVE_LANDSCAPE.md`). The three-pillar positioning (AI-native analysis, auditable decision trail, enterprise governance) is distinctive and grounded in implemented capabilities. The technical implementation genuinely differentiates — most architecture tools are documentation-first, not analysis-first. The `/why` comparison table and `/why-archlucid` pack are built. The `ExplainabilityTrace` with structured evidence on every finding is a real differentiator from ChatGPT/Copilot-based architecture advice. Proof points reference working code, not aspirational features.

**V1.1 scope exclusion:** Buyer-validated differentiation (market-tested positioning, competitive win data) is a v1.1 activity. The v1 deliverable is the positioning materials and the underlying technical differentiation.

**Tradeoffs:** The technical differentiation is genuine and well-documented. The competitive landscape analysis is thorough.

**Recommendations:**
- Verify the `/why` comparison table for accuracy against actual competitor capabilities (desk research, not buyer conversations)
- Ensure the three-pillar positioning appears on the live marketing site

**Fixability:** V1

---

### 2.8. Architectural Integrity — Score: 78 | Weight: 3 | Weighted Deficiency: 0.66

**Justification:** The architecture is genuinely well-structured. Clean bounded contexts: Core (interfaces + domain), Contracts (DTOs), Decisioning (findings + governance + alerts), Persistence (Dapper repos + migrations), Application (orchestration + exports), Api (controllers + auth), AgentRuntime, KnowledgeGraph, Provenance, ArtifactSynthesis, ContextIngestion, Coordinator. Interface-first design with 42+ interfaces in Core alone. The `ArchLucid.Architecture.Tests` project enforces dependency constraints between assemblies. The Host.Composition layer handles DI composition separately from business logic. The Contracts.Abstractions project exists for cross-cutting concerns. ADRs exist (29+ in `docs/adr/`). The C4 architecture poster is clear. The container diagram maps cleanly to the project structure.

Minor concerns: The Coordinator project exists alongside the Authority Run Orchestrator, suggesting an ongoing strangler pattern (ADR 0029 confirms this is planned). The number of persistence sub-projects (Persistence, Persistence.Advisory, Persistence.Alerts, Persistence.Coordination, Persistence.Integration, Persistence.Runtime) may be over-decomposed for the current stage.

**Tradeoffs:** The architecture is arguably over-engineered for a pre-revenue product. But it is internally consistent, well-documented, and the decomposition reflects genuine domain boundaries rather than arbitrary splitting.

**Recommendations:**
- Complete the coordinator strangler acceleration (ADR 0029) to reduce the dual-path complexity
- Consider consolidating Persistence sub-projects if they do not serve independent deployment or team boundaries

**Fixability:** V1.1 (coordinator), V2 (persistence consolidation if warranted)

---

### 2.9. Security — Score: 78 | Weight: 3 | Weighted Deficiency: 0.66

**Justification:** Security posture is strong: JWT + API key auth with fail-closed defaults, SQL RLS with `SESSION_CONTEXT`, STRIDE threat model, OWASP ZAP in CI, Schemathesis API fuzzing, gitleaks secret scanning, Trivy container/IaC scanning, CodeQL, CycloneDX SBOM, prompt redaction before LLM calls, security headers (CSP, X-Frame-Options, X-Content-Type-Options), HSTS, fixed-time comparison for API keys, private endpoints for SQL/Blob, pen-test SoW awarded. The `SECURITY.md` is well-structured with proper disclosure process.

Gaps: Privacy notice is owner-blocked. The RLS implementation has a risk acceptance document, which is honest but also means known limitations exist. Content Safety Guard exists as an interface but Azure Content Safety integration is optional.

**V1.1 scope exclusion:** Pen test completion and publication of redacted results, SOC 2 attestation (Type I or II), PGP key publication for encrypted disclosure, and bug bounty program are all out of scope for v1.

**Tradeoffs:** The security scaffolding is enterprise-grade. The v1 security posture — automated scanning, threat modeling, RLS, auth, prompt redaction — is comprehensive for launch. Third-party validation activities are appropriately sequenced for v1.1.

**Recommendations:**
- Finalize and publish the privacy notice

**Fixability:** V1

---

### 2.10. Traceability — Score: 73 | Weight: 3 | Weighted Deficiency: 0.81

**Justification:** Traceability is a first-class design concern. Every finding carries an `ExplainabilityTrace` with 5 structured fields. The provenance graph connects evidence → decisions → manifest entries → artifacts. Decision traces are persisted in two layers (coordinator and authority). The audit event catalog has 78+ typed constants with CI-enforced count reconciliation. The `CommittedManifestTraceabilityRules` enforce chain integrity on commit. The run pipeline audit timeline service provides a chronological view. Correlation IDs flow from client through API to logs. Agent execution traces (prompt/response) persist to blob storage for forensics.

The gap is in cross-run traceability and lineage. Individual runs are well-traced; the relationship between runs over time (e.g., "this run supersedes that run") is handled through comparison replay but lacks a first-class lineage model.

**Tradeoffs:** Single-run traceability is excellent. Cross-run lineage is a reasonable V1.1 target.

**Recommendations:**
- Add a lightweight run-lineage concept (parent/predecessor) for teams tracking design evolution
- Ensure the provenance graph is surfaced in the sponsor PDF, not just the operator UI

**Fixability:** V1.1

---

### 2.11. Usability — Score: 55 | Weight: 3 | Weighted Deficiency: 1.35

**Justification:** The operator UI is a Next.js shell with progressive disclosure (Pilot → Operate), contextual help content, glossary tooltips, keyboard shortcuts, breadcrumbs, skeleton loading states, layer headers with guidance copy, a first-run wizard with step validation, and collapsible sections. Accessibility baseline exists (axe-core on 5 pages, skip-to-content, ARIA landmarks, focus management). The `OperatorFirstRunWorkflowPanel` provides structured onboarding. The `ContextualHelp` component is tested.

But: No real user has used it. No usability testing data exists. The wizard is validated by unit tests, not user observation. The progressive disclosure model (three tiers of nav visibility) may confuse users who don't understand why some links are hidden. The "Show more links" mental model requires the user to know they're missing something. No onboarding tour is live (tour copy is approved but wrappers still pending removal). The domain complexity (runs, manifests, findings, governance, policy packs, approval workflows) is substantial — the glossary has 50+ terms. For an architecture audience this may be acceptable; for a broader enterprise audience it may not.

**Tradeoffs:** Architecture tooling is inherently complex. But competitor tools (Structurizr, IcePanel) have invested heavily in visual simplicity. ArchLucid's text-heavy, table-heavy approach may alienate visual thinkers.

**Recommendations:**
- Complete the tour copy batch PR (all 5 steps approved, wrappers pending removal)
- Conduct 3-5 moderated usability sessions with target-persona users
- Add a "What is this?" overlay on the first committed run page

**Fixability:** V1 (tour), V1.1 (usability testing)

---

### 2.12. Workflow Embeddedness — Score: 50 | Weight: 3 | Weighted Deficiency: 1.50

**Justification:** Integration infrastructure exists: CloudEvents webhooks, Azure Service Bus integration events with catalog and schemas, AsyncAPI spec, GitHub Action for CI integration, Azure DevOps pipeline task, Microsoft Teams notifications. The API is versioned and documented via OpenAPI. The CLI can be used in pipelines. Jira/ServiceNow/Confluence connectors are designed but explicitly deferred to V1.1. Slack is V2.

The gap: for most enterprise architecture teams, "workflow" means Jira, Confluence, ServiceNow, and maybe Slack/Teams. Teams notifications exist. Everything else is webhooks or REST API, which means the customer does the integration work. The Azure DevOps integration exists but most architecture teams are not DevOps-centric. The GitHub Action is useful for DevOps teams but architecture review typically happens outside CI pipelines.

**Tradeoffs:** Webhooks + REST API is the right V1 foundation. But enterprise buyers expect first-party connectors to their existing tools, not a "build your own integration" model.

**Recommendations:**
- Prioritize Jira connector in V1.1 planning — it's the most common architecture backlog tool
- Document 2-3 webhook integration recipes (Jira via Zapier/Power Automate, Confluence via webhook → API)

**Fixability:** V1.1 (Jira/Confluence per deferred scope), V1 (recipes)

---

### 2.13. Trustworthiness — Score: 68 | Weight: 3 | Weighted Deficiency: 0.96

**Justification:** The trust center exists with evidence pack download, self-assessment, threat model, RLS documentation, DPA template, subprocessors register, and audit coverage matrix. The `ExplainabilityTrace` on findings provides per-finding trust evidence. Explanation faithfulness checking exists (token overlap heuristic). The governance workflow with approval SLAs adds process trust. The durable audit log is append-only by design. The honest labeling ("self-asserted", "engagement in flight") in the trust center is itself a trust signal.

Gaps: The explanation faithfulness checker uses a heuristic that may not correlate with actual faithfulness.

**V1.1 scope exclusion:** Customer testimonials, analyst validation, SOC 2 attestation, completed pen test results, and PGP key publication are all v1.1 activities. The v1 deliverable is the self-asserted trust infrastructure — which is comprehensive: trust center, evidence pack, threat model, DPA, subprocessors register, audit coverage matrix, and honest labeling of current status.

**Tradeoffs:** Self-asserted trust with honest labeling ("self-asserted", "engagement in flight") is the appropriate v1 posture. Third-party validation is sequenced for v1.1.

**Recommendations:**
- Improve explanation faithfulness checking beyond token overlap heuristic (e.g., structured field presence validation)

**Fixability:** V1

---

### 2.14. Reliability — Score: 70 | Weight: 2 | Weighted Deficiency: 0.60

**Justification:** Health checks exist at three levels (live, ready, full). Circuit breaker pattern for Azure OpenAI with configurable break duration. Polly resilience policies via `SqlOpenResilienceDefaults`. Simmy chaos testing in CI. Geo-failover drill runbook exists. SQL failover Terraform with automatic tuning. Redis health runbook. Game day chaos quarterly runbook. API SLOs defined (99.5% / 30 days). Prometheus SLO rules in Terraform. Hosted SaaS probe workflow runs scheduled health checks.

Gaps: No production deployment means no production reliability data. The SLOs are defined but unmeasured. The geo-failover drill is a runbook, not evidence of a completed drill. The chaos testing is in CI, not against production-like infrastructure.

**Tradeoffs:** The reliability engineering is mature for pre-production. Production reliability is inherently unknowable until production.

**Recommendations:**
- Run the geo-failover drill against staging and document results
- Collect baseline SLO metrics from staging for at least 30 days before GA

**Fixability:** V1

---

### 2.15. Data Consistency — Score: 70 | Weight: 2 | Weighted Deficiency: 0.60

**Justification:** `DATA_CONSISTENCY_MATRIX.md` exists (referenced in glossary). SQL transactions via `IArchLucidUnitOfWork`. Idempotency hashing for run creation (`ArchitectureRunIdempotencyHashing`). `IDistributedCreateRunIdempotencyLock` for distributed coordination. `RunCreateIdempotencyGateCache` for hot-path dedup. Comparison persistence has concurrency tests. The outbox pattern for integration events ensures at-least-once delivery. `SqlUniqueConstraintViolationDetector` handles duplicate detection.

Gaps: The dual-manifest-trace repository (ADR 0010) adds complexity — coordinator and authority layers both write traces, which requires careful coordination. The in-memory repositories used in tests don't fully replicate SQL transaction semantics. The archival coordinator with cascade delete (`SqlRunRepositoryArchivalExtendedCascadeTests`) is complex enough to have edge cases.

**Tradeoffs:** The consistency model is well-thought-out for SQL Server. The dual-write patterns add complexity that needs production validation.

**Recommendations:**
- Add an integration test that exercises the full unit-of-work with concurrent commits on the same run
- Validate the archival cascade under load with orphan detection

**Fixability:** V1

---

### 2.16. Maintainability — Score: 75 | Weight: 2 | Weighted Deficiency: 0.50

**Justification:** Clean project structure with each class in its own file. Interface-first design. Central package management via `Directory.Packages.props`. `.editorconfig` for formatting. Architecture guard tests enforcing dependency constraints. CI guards for docs size, naming conventions, navigator links, audit constant counts. Stryker mutation testing configurations. Code coverage reporting. The `NEXT_REFACTORINGS.md` document exists. The `BREAKING_CHANGES.md` tracks API changes. The changelog exists.

The codebase is large (30+ production projects, 20+ test projects) but each project has a clear responsibility. The coordinator strangler pattern (ADR 0029) is explicit about technical debt management.

**Tradeoffs:** The system is maintainable by the original developer. Maintainability by a new team member is uncertain — the 450+ docs and 30+ projects create a substantial onboarding surface despite the contributor spine.

**Recommendations:**
- Complete the single-page contributor index (`CONTRIBUTOR_ON_ONE_PAGE.md` with 100-line CI cap)
- Add an architecture decision log summary for the top 10 decisions a new contributor needs to understand

**Fixability:** V1

---

### 2.17. Explainability — Score: 74 | Weight: 2 | Weighted Deficiency: 0.52

**Justification:** `StructuredExplanation` with reasoning, evidence refs, confidence, alternatives considered, and caveats. `StructuredExplanationParser` gracefully handles non-JSON LLM output. `ExplainabilityTrace` with 5 structured fields on every finding. Explanation faithfulness checking with token overlap heuristic and aggregate fallback. `TraceCompletenessScore` measured and exported as OTel metric. `RunRationale` provides run-level reasoning. The `/demo/explain` route renders provenance graph and citations-bound aggregate explanation side-by-side. `FindingExplainPanel` component in the UI. Decision trace entries with rule audit traces.

The gap: faithfulness checking is heuristic-based (token overlap), not validated against human judgment. The explanation quality under real LLM (non-simulator) is not systematically evaluated. The structured explanation schema is clean but the actual content quality depends entirely on the LLM prompt quality, which is configuration-driven.

**Tradeoffs:** For V1, heuristic faithfulness checking is pragmatic. The risk is that explanations *look* structured but contain LLM confabulation that the heuristic misses.

**Recommendations:**
- Add 5-10 human-labeled explanation faithfulness examples to the golden corpus
- Document the prompt design rationale for each agent type

**Fixability:** V1.1

---

### 2.18. AI/Agent Readiness — Score: 68 | Weight: 2 | Weighted Deficiency: 0.64

**Justification:** Multi-agent pipeline with 4 specialized agents (Topology, Cost, Compliance, Critic). `AgentRuntime` project with `FallbackAgentCompletionClient`, `CachingAgentCompletionClient`. Agent output quality scoring with configurable gates. Agent execution trace recording. Simulator mode for deterministic testing. Azure Content Safety Guard integration (optional). LLM prompt redaction. Mixed-mode support (real + simulator per agent type). Agent evaluation datasets nightly CI workflow. MCP and agent ecosystem backlog document exists.

Gaps: The agent pipeline is tightly coupled to Azure OpenAI. No multi-provider LLM abstraction beyond the Azure OpenAI client. The agent simulation is comprehensive but the real-mode agent behavior is opaque — no structured evaluation of real LLM output quality beyond the golden cohort. The MCP integration backlog is just a document, not implemented. The agent prompts are configuration-driven but no prompt regression testing exists against prompt drift.

**Tradeoffs:** Azure-first is aligned with the stated platform strategy. But LLM provider lock-in is a commercial risk if buyers are committed to other providers.

**Recommendations:**
- Add prompt regression tests against the golden corpus for each agent type
- Document the agent prompt design and expected output schema per agent

**Fixability:** V1

---

### 2.19. Azure Compatibility and SaaS Deployment Readiness — Score: 62 | Weight: 2 | Weighted Deficiency: 0.76

**Justification:** 110 Terraform files across 13+ roots covering the full Azure stack: Container Apps, Front Door, Key Vault, SQL failover, Service Bus, Entra ID, APIM, monitoring, storage, private endpoints, OpenAI. The `terraform-pilot` root provides a single entry point. `apply-saas.ps1` script exists. CD pipeline (`cd.yml`, `cd-staging-on-merge.yml`, `cd-saas-greenfield.yml`). Container images with Docker compose for dev. Managed identity wiring. Consumption budget resources.

Gaps: The production SaaS deployment is not live (`archlucid.net` with Front Door custom domains not wired per documentation). The staging environment (`staging.archlucid.net`) status is unclear from the codebase alone — the hosted-saas-probe workflow exists but is conditional on repository variables. The greenfield CD workflow exists but has not been exercised against a clean subscription. No evidence of a completed `terraform apply` against production infrastructure.

**Tradeoffs:** The infrastructure-as-code is comprehensive. The infrastructure-as-deployed is incomplete.

**Recommendations:**
- Complete the staging deployment with Front Door custom domains
- Run the greenfield CD workflow against a clean subscription and document the result
- Wire `archlucid.net` DNS to Front Door

**Fixability:** V1

---

### 2.20. Auditability — Score: 72 | Weight: 2 | Weighted Deficiency: 0.56

**Justification:** 78+ typed audit event constants in `AuditEventTypes.cs` with CI-enforced count reconciliation. Append-only `dbo.AuditEvents` table. `IAuditService` interface with retry queue. Durable audit dual-write for governance workflow. Audit coverage matrix document tracking all mutating paths. Audit event filter for search. Audit UI page with keyset pagination. Run pipeline audit timeline service. Tenant-scoped audit with RLS.

Gaps: Keyset pagination ties on identical timestamps are a known limitation. No immutable audit log export to external SIEM. No audit log tamper-detection (hash chain or similar). The audit log is in the same SQL database as operational data — no separation of control and data planes for audit.

**Tradeoffs:** SQL-based audit with RLS is pragmatic for V1. Separation of audit storage is a V1.1/V2 concern that scales with customer count.

**Recommendations:**
- Add EventId tie-breaking for keyset pagination under high-volume audit scenarios
- Document the audit retention policy for customer-facing contracts

**Fixability:** V1 (tie-breaking), V1.1 (SIEM export)

---

### 2.21. Policy and Governance Alignment — Score: 65 | Weight: 2 | Weighted Deficiency: 0.70

**Justification:** Policy packs with versioning, dry runs, and changelog. Effective governance resolver with project → workspace → tenant precedence. Pre-commit governance gates. Approval workflows with SLA monitoring. Governance dashboard. Compliance drift trend service. Compliance rule pack loader with validation. 17+ governance-related SQL migration. Feature flags interface for tier enforcement.

Gaps: Policy packs are internal to ArchLucid — no import from external GRC tools. Compliance evaluators are self-contained — no integration with OPA, Rego, or industry-standard policy engines. The governance model is sound but proprietary, which limits adoption by organizations with existing governance tooling.

**Tradeoffs:** A proprietary governance model allows tight integration with the manifest lifecycle. But it adds adoption friction for organizations already invested in OPA/Rego/Sentinel/etc.

**Recommendations:**
- Add an OPA/Rego import path for compliance rule packs as a V1.1 feature
- Document how ArchLucid governance relates to existing GRC frameworks in buyer-facing materials

**Fixability:** V1.1 (OPA import), V1 (documentation)

---

### 2.22. Compliance Readiness — Score: 62 | Weight: 2 | Weighted Deficiency: 0.76

**Justification:** SOC 2 self-assessment exists. SOC 2 roadmap with quarterly milestones through Type II. CAIQ Lite and SIG Core pre-fills. DPA template. Subprocessors register. Compliance matrix document. Evidence pack with one-click ZIP download. Pen-test SoW awarded to Aeronova Red Team LLC. The compliance infrastructure is comprehensive for a pre-launch product.

Gaps: No HIPAA BAA. No FedRAMP. No ISO 27001. Privacy notice is owner-blocked. For enterprise buyers in regulated industries, the self-assessment will need to be supplemented with attestations over time.

**V1.1 scope exclusion:** SOC 2 attestation (Type I or II), completed pen test results, redacted pen-test summary in evidence pack, and PGP key publication are all v1.1 activities. The v1 deliverable is the compliance infrastructure: self-assessment, roadmap, pre-fills, DPA, evidence pack framework.

**Tradeoffs:** The compliance infrastructure is built and the roadmap is credible. For v1 pilots with non-regulated buyers, the self-assessment plus evidence pack is sufficient.

**Recommendations:**
- Formalize and publish the privacy notice

**Fixability:** V1

---

### 2.23. Procurement Readiness — Score: 52 | Weight: 2 | Weighted Deficiency: 0.96

**Justification:** Trust center with evidence pack download. DPA template. Subprocessors register. CAIQ Lite and SIG Core pre-fills. Pricing philosophy document. Stripe checkout integration code. Azure Marketplace SaaS fulfillment infrastructure (partial). "How to request procurement pack" document.

Gaps: No published pricing page on the live site. No MSA template. No SLA commitment beyond internal SLO targets. No insurance documentation. No DUNS number or organizational registration artifacts. Stripe is coded but not production-wired. Azure Marketplace listing is not live. The procurement pack is downloadable only from the API endpoint — not from a public marketing page without authentication (the trust center links assume the API is live).

**V1.1 scope exclusion:** An executed customer contract is a v1.1 artifact (requires a customer). Battle-testing procurement materials requires buyer engagement.

**Tradeoffs:** Procurement materials are comprehensive for pre-launch. The first real procurement conversation will refine them.

**Recommendations:**
- Publish the pricing page on the live marketing site
- Create an MSA template
- Ensure the evidence pack ZIP endpoint is live and accessible without authentication

**Fixability:** V1 (pricing page, MSA template, endpoint)

---

### 2.24. Interoperability — Score: 55 | Weight: 2 | Weighted Deficiency: 0.90

**Justification:** REST API with OpenAPI spec. AsyncAPI spec for integration events. CloudEvents webhook format. GitHub Action for CI. Azure DevOps pipeline task. API client NuGet package (`ArchLucid.Api.Client`). CLI as .NET global tool. Docker images.

Gaps: No GraphQL. No gRPC. No Terraform provider. No SDK for Python/JavaScript/Go. ITSM connectors (Jira, ServiceNow, Confluence) deferred to V1.1. No SCIM 2.0 yet (V1.1). No SSO configuration guide for non-Entra IdPs (Okta, Auth0, PingFederate). The OpenAPI spec exists but no published API client libraries beyond the .NET one.

**Tradeoffs:** REST + OpenAPI is the universal starting point. Multi-SDK generation from OpenAPI is a known fast-follow. The V1.1 ITSM connector commitment is the right prioritization.

**Recommendations:**
- Generate TypeScript and Python API clients from the OpenAPI spec
- Add SSO configuration guides for Okta and Auth0

**Fixability:** V1 (client gen), V1.1 (SSO guides, ITSM connectors)

---

### 2.25. Decision Velocity — Score: 60 | Weight: 2 | Weighted Deficiency: 0.80

**Justification:** The run lifecycle (request → execute → commit) can complete in seconds under the simulator. The `run --quick` CLI flag seeds and commits in one step. Real-mode execution time depends on LLM response latency. The operator UI wizard guides through run creation in a few clicks. The "second run paste" feature allows quick iteration.

Gaps: The time from "buyer decides to evaluate" to "buyer sees first value" is currently unmeasured and likely > 30 minutes (signup not live). The time from "architecture request" to "reviewable package" under real-mode execution is not benchmarked. k6 load test results exist but measure API throughput, not end-to-end user journey time.

**Tradeoffs:** Decision velocity under simulator is fast. Decision velocity under real-mode with LLM latency, quality gates, and governance workflows is the realistic measure.

**Recommendations:**
- Benchmark the end-to-end time from wizard submission to committed manifest under real-mode
- Set a target for "under 5 minutes from request to committed manifest" and measure against it

**Fixability:** V1

---

### 2.26. Commercial Packaging Readiness — Score: 53 | Weight: 2 | Weighted Deficiency: 0.94

**Justification:** Pricing philosophy document with tier definitions (Free trial, Team, Enterprise). Product packaging reference with two-layer model. UI shaping per tier (visibility + capability). Billing provider abstraction (ADR 0016) with Stripe and Azure Marketplace implementations. Trial parameters defined (14 days, 3 seats, 10 runs). Feature gates in the API.

Gaps: No live pricing page. No published tier comparison. Stripe checkout not production-wired. Azure Marketplace listing not live. The tier boundaries are defined but enforcement is partial — `FUTURE_PACKAGING_ENFORCEMENT.md` describes future evolution, implying current enforcement is incomplete. The `RequiresCommercialTenantTierAttribute` exists but coverage across all Enterprise-tier endpoints is untested.

**V1.1 scope exclusion:** Pricing validation with target buyers is a v1.1 activity.

**Tradeoffs:** The packaging model is well-designed. Production wiring and enforcement audit are the remaining v1 gaps.

**Recommendations:**
- Wire Stripe checkout in staging with test keys end-to-end
- Complete tier enforcement coverage audit
- Publish the tier comparison on the live marketing site

**Fixability:** V1

---

### 2.27. Availability — Score: 68 | Weight: 1 | Weighted Deficiency: 0.32

**Justification:** Multi-region Container Apps in Terraform (secondary region). SQL failover group. Front Door for edge routing and failover. Health probes at three levels. Redis health runbook. Geo-failover drill runbook. Hosted SaaS probe workflow.

Gaps: No production deployment to measure actual availability. Geo-failover drill is documented but not evidenced as executed. No published SLA/uptime commitment.

**Fixability:** V1

---

### 2.28. Performance — Score: 65 | Weight: 1 | Weighted Deficiency: 0.35

**Justification:** k6 load tests (smoke, soak, per-tenant burst). Rate limiting at three levels (fixed, expensive, replay). Caching repositories (`CachingRunRepository`, `CachingGoldenManifestRepository`). Hot-path read cache interface. SQL read replica connection string resolver. Benchmarks project exists. k6 summary JSON exists.

Gaps: No published performance benchmarks. No P95/P99 latency targets. The k6 results exist as a JSON file but are not analyzed or baselined.

**Fixability:** V1

---

### 2.29. Scalability — Score: 60 | Weight: 1 | Weighted Deficiency: 0.40

**Justification:** Multi-tenant architecture with SQL RLS. Container Apps with autoscaling. Service Bus for async fan-out. Redis for caching. Read replicas. Background worker for heavy processing. Data archival coordinator for lifecycle management.

Gaps: No load testing against multi-tenant scenarios. No published tenant count ceiling. The single SQL database per deployment model limits horizontal scaling. No sharding strategy documented.

**Fixability:** V1.1

---

### 2.30. Supportability — Score: 62 | Weight: 1 | Weighted Deficiency: 0.38

**Justification:** Troubleshooting guide with quick matrix. Support bundle command (`support-bundle --zip`). Problem Details (RFC 9457) with `supportHint` fields. Correlation IDs. CLI doctor command. Detailed health check response writer. Sanitized logger extensions.

Gaps: No ticketing system integration. No support SLA. No knowledge base. No self-service support portal.

**Fixability:** V1.1

---

### 2.31. Manageability — Score: 65 | Weight: 1 | Weighted Deficiency: 0.35

**Justification:** Configuration via `appsettings.json` with environment overrides. Feature flags interface. Admin controller. Tenant provisioning service. Trial lifecycle management. Configuration key catalog with requirement kinds. Operator atlas mapping every UI route to API + CLI.

Gaps: No admin UI for configuration management. No tenant management dashboard. No self-service configuration for operators beyond appsettings.

**Fixability:** V1.1

---

### 2.32. Deployability — Score: 65 | Weight: 1 | Weighted Deficiency: 0.35

**Justification:** Docker compose for dev. Container images. CD pipelines (3 workflows). Terraform across 13+ roots. Post-deploy verification script. Build release scripts. Package release scripts. Greenfield baseline migration runner.

Gaps: No blue-green or canary deployment documented. No rollback procedure documented. The CD pipeline exists but production deployment has not been executed.

**Fixability:** V1

---

### 2.33. Observability — Score: 67 | Weight: 1 | Weighted Deficiency: 0.33

**Justification:** OpenTelemetry metrics (trace completeness, LLM cost USD, trial funnel, outbox depth). Application Insights in Terraform. Prometheus SLO rules. Grafana dashboards in Terraform. OTel collector in Terraform. Log sanitization. Correlation IDs. Build provenance on `/version`.

Gaps: No distributed tracing integration documented end-to-end. No custom dashboard for operator-facing diagnostics. No alert routing for infrastructure issues (only architecture alerts).

**Fixability:** V1

---

### 2.34. Testability — Score: 76 | Weight: 1 | Weighted Deficiency: 0.24

**Justification:** 180+ test classes in .NET. 74+ `.test.ts` and 114+ `.test.tsx` files. Architecture guard tests. Stryker mutation testing (8 configs). k6 load tests. Schemathesis API fuzzing. Simmy chaos testing. Playwright e2e. axe-core accessibility tests. Golden corpus regression tests. Contract tests for repositories. Property-based tests (FsCheck-style naming). Coverage reporting with CI comments.

Gaps: Real-LLM test coverage is opt-in nightly, not merge-blocking. No visual regression testing. No consumer-driven contract tests for the API.

**Fixability:** V1 (consumer contracts), V1.1 (visual regression)

---

### 2.35. Modularity — Score: 78 | Weight: 1 | Weighted Deficiency: 0.22

**Justification:** 30+ projects with clear boundaries. Interface-first design. Finding engine plugin discovery. Finding engine template project. Host.Composition for DI wiring separate from business logic. Contracts.Abstractions for cross-cutting concerns. Each class in its own file per user rule.

**Fixability:** N/A (strong)

---

### 2.36. Extensibility — Score: 70 | Weight: 1 | Weighted Deficiency: 0.30

**Justification:** Finding engine plugin architecture with discovery. Template project for custom finding engines. Billing provider abstraction. Secret provider abstraction. Content safety guard abstraction. Connector publishing interface. Integration event handler interface.

Gaps: No public extension API or SDK. Plugin discovery is internal. No marketplace for third-party extensions.

**Fixability:** V1.1

---

### 2.37. Evolvability — Score: 72 | Weight: 1 | Weighted Deficiency: 0.28

**Justification:** 29+ ADRs documenting major decisions. API versioning with `v1` path segments and `Asp.Versioning.Mvc`. Breaking changes document. Changelog. V1 scope contract with explicit deferred items. Strangler pattern for coordinator migration (ADR 0029). Config bridge sunset document.

**Fixability:** N/A (strong)

---

### 2.38. Documentation — Score: 72 | Weight: 1 | Weighted Deficiency: 0.28

**Justification:** 450+ markdown files. Five-document contributor spine. Navigator with 15 common tasks. Architecture poster. Glossary. Runbooks (12+). Security docs (10+). Go-to-market docs (10+). ADRs (29+). API contracts doc. SQL scripts doc. Test structure doc. The 2026-04-23 reorganization compressed `/docs` root to ~20 active files with depth in `docs/library/`.

Gaps: The sheer volume creates navigation difficulty despite the doc navigator. Some docs reference each other circularly. The buyer-facing docs have placeholder copy. The doc inventory exists but 450+ files is daunting for any newcomer.

**Fixability:** V1 (placeholder removal)

---

### 2.39. Azure Ecosystem Fit — Score: 72 | Weight: 1 | Weighted Deficiency: 0.28

**Justification:** Azure-native by design (ADR 0020). Entra ID for identity. Azure SQL. Azure Blob Storage. Azure Service Bus. Azure OpenAI. Azure Container Apps. Azure Front Door. Azure Key Vault. Azure APIM. Application Insights. Managed identity. Private endpoints.

Gaps: No Azure Marketplace SaaS listing live. No Azure Verified Partner status. No integration with Azure DevOps Boards (only pipeline task).

**Fixability:** V1.1

---

### 2.40. Cognitive Load — Score: 55 | Weight: 1 | Weighted Deficiency: 0.45

**Justification:** The product surface is large: runs, manifests, findings, decision traces, provenance, comparisons, replays, governance, policy packs, approvals, alerts, advisory, knowledge graph, conversation/ask, evolution simulation, value reports, pilot signals. Progressive disclosure helps but does not fully mitigate the conceptual burden. The glossary has 50+ terms. The Pilot path is designed to be narrow, but the Operate path is wide.

Gaps: No cognitive load measurement. No user journey mapping validated by users. The "Show more links" model requires the user to trust that the default view is sufficient.

**Fixability:** V1.1 (measurement), V2 (simplification if needed)

---

### 2.41. Stickiness — Score: 55 | Weight: 1 | Weighted Deficiency: 0.45

**Justification:** Committed manifests with versioning create a document trail. Governance workflows create process dependency. Audit log creates compliance dependency. Integration events allow downstream automation. Value reports create sponsor-facing proof that gets shared.

Gaps: No data export that locks customers in (good for trust, neutral for stickiness). No community, forum, or user group. No marketplace. No network effects.

**Fixability:** V1.1

---

### 2.42. Template and Accelerator Richness — Score: 58 | Weight: 1 | Weighted Deficiency: 0.42

**Justification:** Template briefs exist for 6 verticals (financial-services, healthcare, public-sector, public-sector-us, retail, saas). Finding engine template project. Compliance rule pack. Example manifest delta workflows. Demo seed with Contoso Retail.

Gaps: Only 6 vertical templates. No community-contributed templates. No template marketplace. The templates are brief presets, not full architecture patterns or reference architectures.

**Fixability:** V1 (add 3-4 more verticals), V1.1 (template richness)

---

### 2.43. Accessibility — Score: 60 | Weight: 1 | Weighted Deficiency: 0.40

**Justification:** WCAG 2.1 AA target stated. axe-core scanning on 5 operator pages. Skip-to-content link. Language attribute. Landmark navigation. Form labels. Focus management. `eslint-plugin-jsx-a11y` in ESLint. CI enforcement for critical/serious violations. Annual review cadence. Accessibility email alias.

Gaps: Only 5 pages scanned (out of 30+ routes). No VPAT/ACR published. No keyboard-only navigation testing. No screen reader testing. Marketing pages have separate axe tests but coverage is partial.

**Fixability:** V1 (expand page coverage), V1.1 (VPAT)

---

### 2.44. Customer Self-Sufficiency — Score: 45 | Weight: 1 | Weighted Deficiency: 0.55

**Justification:** Troubleshooting guide. Support bundle. CLI doctor command. Contextual help in UI. Glossary tooltips. First-run workflow panel.

Gaps: No knowledge base. No FAQ. No community forum. No chatbot. No in-app help search. The buyer-facing self-serve path is not live. A customer today cannot self-serve without developer assistance.

**Fixability:** V1 (funnel live), V1.1 (knowledge base)

---

### 2.45. Change Impact Clarity — Score: 68 | Weight: 1 | Weighted Deficiency: 0.32

**Justification:** Two-run comparison with structured deltas. Comparison replay with drift verification. `BeforeAfterDelta` UI components. Manifest versioning. Breaking changes document. Changelog. Evolution simulation with shadow execution.

Gaps: No automated change impact notification for downstream consumers. No "what changed since your last visit" summary in the UI.

**Fixability:** V1.1

---

### 2.46. Cost-Effectiveness — Score: 58 | Weight: 1 | Weighted Deficiency: 0.42

**Justification:** Per-tenant cost model document. Pilot profile document. Consumption budget Terraform resources. Golden-cohort cost dashboard Terraform module. LLM daily tenant budget options. LLM token quota options. Cost preview endpoint (AllowAnonymous). Agent execution cost preview card in wizard. Pricing philosophy with tier-based economics.

Gaps: No actual cost data from production. The cost model is theoretical. LLM cost per run under real-mode is estimated (~$1.25 for `try --real`) but not measured at scale. No cost optimization recommendations for high-volume tenants.

**Fixability:** V1 (measure real costs), V1.1 (optimization guidance)

---

### Weighted Readiness Calculation

| Quality | Score | Weight | Contribution |
|---------|-------|--------|-------------|
| Marketability | 53 | 8 | 424 |
| Time-to-Value | 45 | 7 | 315 |
| Adoption Friction | 45 | 6 | 270 |
| Proof-of-ROI Readiness | 65 | 5 | 325 |
| Executive Value Visibility | 68 | 4 | 272 |
| Differentiability | 62 | 4 | 248 |
| Correctness | 72 | 4 | 288 |
| Architectural Integrity | 78 | 3 | 234 |
| Security | 78 | 3 | 234 |
| Traceability | 73 | 3 | 219 |
| Usability | 55 | 3 | 165 |
| Workflow Embeddedness | 50 | 3 | 150 |
| Trustworthiness | 68 | 3 | 204 |
| Reliability | 70 | 2 | 140 |
| Data Consistency | 70 | 2 | 140 |
| Maintainability | 75 | 2 | 150 |
| Explainability | 74 | 2 | 148 |
| AI/Agent Readiness | 68 | 2 | 136 |
| Azure SaaS Deployment | 62 | 2 | 124 |
| Auditability | 72 | 2 | 144 |
| Policy & Governance | 65 | 2 | 130 |
| Compliance Readiness | 62 | 2 | 124 |
| Procurement Readiness | 52 | 2 | 104 |
| Interoperability | 55 | 2 | 110 |
| Decision Velocity | 60 | 2 | 120 |
| Commercial Packaging | 53 | 2 | 106 |
| Availability | 68 | 1 | 68 |
| Performance | 65 | 1 | 65 |
| Scalability | 60 | 1 | 60 |
| Supportability | 62 | 1 | 62 |
| Manageability | 65 | 1 | 65 |
| Deployability | 65 | 1 | 65 |
| Observability | 67 | 1 | 67 |
| Testability | 76 | 1 | 76 |
| Modularity | 78 | 1 | 78 |
| Extensibility | 70 | 1 | 70 |
| Evolvability | 72 | 1 | 72 |
| Documentation | 72 | 1 | 72 |
| Azure Ecosystem Fit | 72 | 1 | 72 |
| Cognitive Load | 55 | 1 | 55 |
| Stickiness | 55 | 1 | 55 |
| Template Richness | 58 | 1 | 58 |
| Accessibility | 60 | 1 | 60 |
| Customer Self-Sufficiency | 45 | 1 | 45 |
| Change Impact Clarity | 68 | 1 | 68 |
| Cost-Effectiveness | 58 | 1 | 58 |
| **TOTAL** | | **102** | **Σ = 6315** |

**Readiness = 6315 / (100 × 102) × 100 = 61.91%**

---

## 3. Top 10 Most Important Weaknesses

Ranked by cross-cutting severity, not just quality names.

### 1. No live buyer-facing SaaS funnel

The self-serve path from `archlucid.net/signup` through first committed manifest does not exist in production. Every commercial quality (Marketability, Time-to-Value, Adoption Friction, Proof-of-ROI, Procurement) is degraded by this single gap. The code exists; the deployment does not.

### 2. Engineering-to-deployment ratio is high

30+ production projects, 450+ docs, 110 Terraform files, 25 CI workflows — the engineering is comprehensive, but the buyer-facing deployment (live funnel, live pricing, Stripe wiring) has not caught up. The opportunity cost of continued engineering without deploying what exists is substantial.

### 3. Privacy notice remains owner-blocked

The privacy notice content remains "pending legal sign-off." For a SaaS product collecting user data (email, company name, architecture briefs), the absence of a finalized privacy notice is a legal and procurement blocker that is entirely within the owner's control.

### 4. Real-LLM correctness is undertested

The core value proposition — AI-generated architecture findings — runs against simulated responses in CI. The golden cohort real-LLM gate is opt-in nightly, not merge-blocking. Prompt regression testing does not exist. The gap between "simulator says it works" and "real LLM produces trustworthy output" is unmeasured.

### 5. Pricing and packaging are designed but not deployed

Tier definitions, pricing philosophy, Stripe integration, Azure Marketplace code all exist. The pricing page is not live. Stripe checkout is not production-wired. The packaging is well-designed on paper; it needs to be deployed so buyers can evaluate it.

### 6. ITSM integration gap creates workflow isolation

Architecture teams use Jira for backlogs, Confluence for documentation, ServiceNow for change management. All three connectors are deferred to V1.1. Until they ship, ArchLucid is an island that requires manual copy-paste to integrate with existing workflows.

### 7. Cognitive complexity limits self-serve adoption

50+ glossary terms, 30+ operator routes, three nav disclosure tiers, five authority levels. The domain is inherently complex, but the product does not yet have evidence that its progressive disclosure model actually reduces cognitive load for new users. The onboarding tour is approved but not yet live.

### 8. Privacy notice is owner-blocked

The privacy notice content remains "pending legal sign-off." For a SaaS product collecting user data (email, company name, architecture briefs), the absence of a finalized privacy notice is a legal and procurement blocker.

### 9. No production deployment evidence

Terraform, Docker, CD pipelines all exist, but no evidence of a production-tier deployment with real traffic. The system's reliability, performance, and scalability characteristics under production conditions are entirely unknown.

### 10. Multi-language API client gap

Only a .NET API client library exists. Target buyers whose teams use Python, TypeScript, or Go cannot easily integrate. The OpenAPI spec exists to auto-generate these, but the generation has not been done.

---

## 4. Top 5 Monetization Blockers

### 1. No live self-serve signup flow

Revenue cannot flow until prospects can sign up, trial, and convert. The Stripe integration is coded. The trial parameters are designed. The checkout flow is documented. None of it is production-wired.

### 2. No pricing page on the live site

Buyers cannot evaluate price/value fit without seeing prices. The pricing philosophy document exists internally. The marketing route `/pricing` exists in code. But `archlucid.net/pricing` is not live.

### 3. No MSA or commercial contract template

The DPA template exists. No Master Service Agreement template exists. The first sales conversation will require ad-hoc legal work.

### 4. Azure Marketplace listing not live

For Azure-native enterprise buyers, Marketplace is the preferred procurement path (consolidated billing, existing enterprise agreements). The Marketplace SaaS fulfillment infrastructure is partially built but not listed.

### 5. Placeholder copy in buyer-facing documents

The buyer first-30-minutes guide contains `<<placeholder copy>>` markers. Prospects encountering these during evaluation will immediately question product maturity. Every placeholder is a conversion risk.

---

## 5. Top 5 Enterprise Adoption Blockers

### 1. SCIM 2.0 not yet available

Enterprise identity management requires automated user provisioning. SCIM 2.0 is designed and committed for V1.1 but not shipped. Organizations with mandatory SCIM compliance cannot adopt V1.

### 2. No ITSM connectors

Jira, ServiceNow, and Confluence are table-stakes integrations for enterprise architecture workflows. All are deferred to V1.1. Enterprise buyers evaluating V1 must accept manual integration via webhooks.

### 3. Single-region default deployment

While multi-region Terraform exists, the default deployment is single-region. Enterprise buyers with data residency requirements (EU, APAC) need explicit multi-region guidance and validated deployment patterns.

### 4. Privacy notice not finalized

The privacy notice is owner-blocked. Enterprise procurement and legal review typically require a published privacy notice before contract execution.

### 5. No SSO configuration guides for non-Entra IdPs

Enterprise buyers using Okta, Auth0, or PingFederate have no documented SSO setup path. Entra ID is supported, but the broader IdP ecosystem is undocumented.

---

## 6. Top 5 Engineering Risks

### 1. Simulator-real divergence

The simulator produces deterministic, well-formed outputs. Real LLMs produce variable, sometimes malformed outputs. The gap between simulator-tested code paths and real-LLM code paths is a correctness risk that grows with every feature built against simulator assumptions.

### 2. Coordinator-authority dual-path complexity

The ongoing strangler pattern from Coordinator to Authority Run Orchestrator (ADR 0029) means two execution paths coexist. This doubles the surface area for bugs in the run lifecycle until the strangler completes.

### 3. Audit event constant drift

While CI enforces count reconciliation, the dual-direction check (code → docs and docs → code) is a Python script that depends on regex parsing of C# constants. If the constant naming pattern changes, the guard silently passes.

### 4. SQL single-database bottleneck

All tenants, all runs, all audit events, all governance data share a single SQL database with RLS. This is correct for V1 but creates a scaling ceiling. The archival coordinator helps but does not eliminate the single-point-of-saturation risk.

### 5. LLM provider lock-in

The agent runtime is tightly coupled to Azure OpenAI (`AzureOpenAI:Endpoint`, `AzureOpenAI:ApiKey`, `AzureOpenAI:DeploymentName`). The `FallbackAgentCompletionClient` falls back within Azure OpenAI, not to alternative providers. If Azure OpenAI experiences outages or pricing changes, there is no provider-level failover.

---

## 7. Most Important Truth

**ArchLucid is a comprehensively engineered product whose buyer-facing deployment infrastructure has not caught up with its engineering completeness.** The architecture is clean. The test coverage is serious. The documentation is exhaustive. The positioning, packaging, and go-to-market materials are well-designed. But the live SaaS funnel, pricing page, and Stripe wiring are not deployed. The gap is not "build more" — it is "deploy what exists." The engineering is ahead of the deployment. Once the buyer-facing surfaces are live, the product is ready for its first customers (v1.1 scope).

---

## 8. Top Improvement Opportunities

### Improvement 1: Ship the Hosted Trial Funnel to Production

**Why it matters:** Every commercial quality is gated on buyers being able to self-serve. The code exists. The infrastructure exists. The deployment does not.

**Expected impact:** Directly improves Marketability (+15-20 pts), Time-to-Value (+15-20 pts), Adoption Friction (+10-15 pts), Customer Self-Sufficiency (+10-15 pts), Procurement Readiness (+5 pts). Weighted readiness impact: +2.5-4.0%.

**Affected qualities:** Marketability, Time-to-Value, Adoption Friction, Customer Self-Sufficiency, Procurement Readiness, Commercial Packaging

**Status:** Fully actionable now.

**Cursor Prompt:**

```
Goal: Make the hosted SaaS trial funnel live on staging.archlucid.net
with working signup, tenant provisioning, sample run pre-seed, and
first-value-report.

Read first:
- docs/go-to-market/TRIAL_AND_SIGNUP.md
- docs/library/REFERENCE_SAAS_STACK_ORDER.md
- docs/BUYER_FIRST_30_MINUTES.md
- infra/terraform-container-apps/main.tf
- infra/terraform-edge/frontdoor.tf
- .github/workflows/cd-staging-on-merge.yml
- docker-compose.yml (full-stack profile)

Tasks:
1. Verify the cd-staging-on-merge.yml workflow builds and pushes
   container images to ACR (or identify what blocks it).
2. Verify terraform-container-apps has the API + Worker + UI
   container definitions with correct image references.
3. Verify terraform-edge has Front Door custom domain configuration
   for staging.archlucid.net.
4. Verify the API startup path with StorageProvider=Sql and
   Demo:SeedOnStartup=true provisions sample data for new tenants.
5. Create a deployment checklist document at
   docs/deployment/STAGING_DEPLOYMENT_CHECKLIST.md listing every
   prerequisite (DNS, ACR, Key Vault secrets, Entra app reg,
   SQL connection, Service Bus) with verify commands.
6. Ensure the hosted-saas-probe.yml workflow will pass once staging
   is live (ARCHLUCID_STAGING_BASE_URL set).

Acceptance criteria:
- Checklist document exists with all prerequisites enumerated
- Each prerequisite has a verification command or curl
- The checklist references the correct Terraform root for each
  resource
- No new Terraform resources are created — this is a checklist
  for applying existing IaC

Constraints:
- Do NOT modify Terraform files
- Do NOT modify CI/CD workflows
- Do NOT create new infrastructure resources
- Do NOT change application code
- This is documentation and verification only

Do not change: Any .tf file, any .yml workflow file, any .cs file,
any .ts/.tsx file.
```

---

### Improvement 2: Replace All Buyer-Facing Placeholder Copy

**Why it matters:** The `<<placeholder copy>>` markers in `BUYER_FIRST_30_MINUTES.md` signal an unfinished product to any evaluator who finds the repo.

**Expected impact:** Directly improves Marketability (+5-8 pts), Time-to-Value (+3-5 pts), Executive Value Visibility (+3-5 pts). Weighted readiness impact: +0.6-1.2%.

**Affected qualities:** Marketability, Time-to-Value, Executive Value Visibility, Documentation

**Status:** Fully actionable now.

**Cursor Prompt:**

```
Goal: Replace all <<placeholder copy>> markers in buyer-facing
documents with real, consultative-voice copy that does not invent
customer names, testimonials, or unverified claims.

Read first:
- docs/BUYER_FIRST_30_MINUTES.md
- docs/EXECUTIVE_SPONSOR_BRIEF.md (voice reference)
- docs/CORE_PILOT.md (step content reference)
- docs/library/PRODUCT_PACKAGING.md (layer model)
- docs/PENDING_QUESTIONS.md (Resolved 2026-04-23 Q1-Q5)

Tasks:
1. In docs/BUYER_FIRST_30_MINUTES.md, replace each
   <<placeholder copy — replace before external use>> marker with
   2-3 sentences of consultative, pragmatic copy describing what
   the step does and what the evaluator should expect to see.
2. Use the same voice as EXECUTIVE_SPONSOR_BRIEF.md — pragmatic,
   no overclaiming, grounded in what V1 actually does.
3. For Step 1 (Sign in): describe the Entra ID / Google sign-in
   experience and mention no credit card required.
4. For Step 2 (Pick a vertical): name the 6 available verticals
   from templates/briefs/ and explain the purpose of the picker.
5. For Step 3 (Run a sample): explain that ArchLucid pre-populates
   and runs analysis automatically — no upload needed for first run.
6. For Step 4 (Read your first finding): explain what a typed
   finding looks like — category, severity, evidence, explanation.
7. For Step 5 (Decide next): describe the two paths — invite a
   colleague for a second run, or start a guided pilot.

Acceptance criteria:
- Zero <<placeholder>> markers remain in the file
- No customer names, testimonials, or unverified ROI claims
- Copy aligns with the two-layer Pilot/Operate model
- Copy does not ask the buyer to install anything
- File still links to archlucid.net/get-started for screenshots

Constraints:
- Do NOT change the five-step structure
- Do NOT add a "talk to a human" CTA (V1.1 per PENDING_QUESTIONS)
- Do NOT modify any other file

Do not change: Any file other than docs/BUYER_FIRST_30_MINUTES.md.
```

---

### Improvement 3: Wire Stripe Checkout End-to-End in Staging

**Why it matters:** Revenue requires a working payment path. Stripe integration code exists but has never processed a test transaction in a deployed environment.

**Expected impact:** Directly improves Procurement Readiness (+8-12 pts), Commercial Packaging (+8-10 pts), Marketability (+3-5 pts). Weighted readiness impact: +0.5-1.0%.

**Affected qualities:** Procurement Readiness, Commercial Packaging, Marketability, Monetization

**Status:** Fully actionable now.

**Cursor Prompt:**

```
Goal: Create a Stripe staging end-to-end verification runbook that
an operator can follow to wire and test Stripe Checkout in test mode
against a staging ArchLucid deployment.

Read first:
- docs/go-to-market/STRIPE_CHECKOUT.md
- docs/library/BILLING.md (if it exists; search for BILLING.md)
- ArchLucid.Persistence/Billing/Stripe/StripeBillingProvider.cs
- ArchLucid.Api/Controllers/Billing/BillingCheckoutController.cs
- archlucid-ui/public/pricing.json (if it exists)

Tasks:
1. Read the existing STRIPE_CHECKOUT.md and identify any gaps
   between the documented steps and the actual code.
2. Create docs/runbooks/STRIPE_STAGING_E2E_VERIFICATION.md with:
   a. Prerequisites (Stripe test-mode account, staging API deployed,
      Key Vault or env vars for Stripe secrets)
   b. Step-by-step: configure API secrets, register webhook, create
      test product/price, update pricing.json, trigger checkout,
      verify webhook receipt, verify SQL state
   c. Verification queries: SQL checks for BillingWebhookEvents,
      BillingSubscriptions, tenant tier promotion
   d. Rollback: how to reset test state
3. Ensure the runbook references the correct API route, controller,
   and Stripe event types.

Acceptance criteria:
- Runbook exists at docs/runbooks/STRIPE_STAGING_E2E_VERIFICATION.md
- Every step has a concrete command or action (no vague instructions)
- SQL verification queries are included
- Rollback steps are included
- The runbook references STRIPE_CHECKOUT.md for context

Constraints:
- Do NOT modify any source code
- Do NOT create Stripe resources
- Do NOT change pricing.json
- This is a runbook only

Do not change: Any .cs, .ts, .tsx, .tf, or .yml file.
```

---

### Improvement 4: Complete the Onboarding Tour (5-Step Batch PR)

**Why it matters:** The onboarding tour is approved by the owner (all 5 copies), but `TourStepPendingApproval` wrappers still need removal. This directly improves the first-use experience for new operators.

**Expected impact:** Directly improves Usability (+5-8 pts), Time-to-Value (+3-5 pts), Adoption Friction (+3-5 pts), Cognitive Load (+2-3 pts). Weighted readiness impact: +0.5-0.9%.

**Affected qualities:** Usability, Time-to-Value, Adoption Friction, Cognitive Load

**Status:** Fully actionable now.

**Cursor Prompt:**

```
Goal: Remove all TourStepPendingApproval wrappers from the onboarding
tour component, making all 5 approved tour steps live.

Read first:
- docs/PENDING_QUESTIONS.md (Improvement 5 — Resolved 2026-04-24,
  option B batch all five in one PR)
- archlucid-ui/src/components/OptInTour.tsx (if it exists)

Tasks:
1. Find the OptInTour component (or equivalent tour component) in
   archlucid-ui/src/components/.
2. Identify all TourStepPendingApproval wrapper elements.
3. Replace each <TourStepPendingApproval> wrapper with a plain
   React fragment <> so the tour step content renders directly.
4. Update the corresponding test file to remove any pending-approval
   assertions and add assertions that tour content renders.
5. Run the Vitest suite for the affected component to verify.

Acceptance criteria:
- Zero TourStepPendingApproval elements remain in the component
- All 5 tour steps render their approved copy directly
- Tests pass with updated assertions
- No other components are modified

Constraints:
- Do NOT change tour step copy content
- Do NOT change tour step order
- Do NOT add new tour steps
- Do NOT modify any backend code

Do not change: Any .cs file, any .tf file, any non-tour .tsx file.
```

---

### Improvement 5: Expand Accessibility Scanning to 15 Pages

**Why it matters:** Only 5 of 30+ operator pages have accessibility scanning. Expanding coverage reduces legal and adoption risk for public-sector and enterprise buyers.

**Expected impact:** Directly improves Accessibility (+10-15 pts), Compliance Readiness (+2-3 pts), Usability (+2-3 pts). Weighted readiness impact: +0.2-0.4%.

**Affected qualities:** Accessibility, Compliance Readiness, Usability

**Status:** Fully actionable now.

**Cursor Prompt:**

```
Goal: Expand axe-core accessibility scanning from 5 pages to 15
pages by adding the 10 most-visited operator routes to the
accessibility test suite.

Read first:
- ACCESSIBILITY.md (root)
- archlucid-ui/e2e/accessibility.spec.ts (or the equivalent
  Playwright accessibility test file)
- archlucid-ui/src/accessibility/ (Vitest axe component tests)
- archlucid-ui/src/lib/nav-config.ts (all operator routes)

Tasks:
1. Read nav-config.ts to identify all operator routes.
2. Identify the 10 highest-value routes not already scanned:
   prioritize /runs/new (wizard), /runs/[runId] (run detail),
   /compare, /governance, /governance/dashboard,
   /settings/tenant, /advisory, /graph, /ask, and /value-report.
3. Add these 10 routes to the PAGES array in the Playwright
   accessibility spec file (same pattern as existing entries).
4. For routes requiring authentication context, use the same
   auth setup pattern as existing tests.
5. For routes requiring data (e.g., /runs/[runId]), use the
   existing mock or demo-seed pattern.
6. Update ACCESSIBILITY.md to list all 15 scanned pages.

Acceptance criteria:
- 15 pages listed in the PAGES array
- All new entries follow the same pattern as existing entries
- ACCESSIBILITY.md table updated to show 15 pages
- No existing tests broken
- No new axe violations introduced (if violations exist in new
  pages, document them as known issues in ACCESSIBILITY.md)

Constraints:
- Do NOT fix accessibility violations in this PR — only add scanning
- Do NOT modify the axe configuration or severity thresholds
- Do NOT change any page component code

Do not change: Any .cs file, any page component .tsx file,
any .tf file.
```

---

### Improvement 6: DEFERRED — First External Pilot with Measured Outcomes

**Title:** DEFERRED — First External Pilot with Measured Outcomes

**Reason deferred:** This requires identifying and engaging an external design partner willing to run a real architecture brief through ArchLucid and share before/after cycle-time measurements. No code change can create this outcome.

**Information needed from owner:**
- Target design partner organization (or criteria for selection)
- Whether the pilot uses the hosted staging environment or a dedicated deployment
- Who leads the pilot engagement (founder, sales engineer, or technical advisor)
- Acceptable timeline for pilot completion and case study production

---

### Improvement 7: Add Prompt Regression Tests for Agent Pipeline

**Why it matters:** The agent prompts are the core intellectual property. Prompt drift (accidental changes that degrade output quality) is invisible without regression tests. The golden corpus exists but does not test prompt-to-output quality systematically.

**Expected impact:** Directly improves Correctness (+5-8 pts), AI/Agent Readiness (+5-8 pts), Trustworthiness (+3-5 pts), Reliability (+2-3 pts). Weighted readiness impact: +0.4-0.8%.

**Affected qualities:** Correctness, AI/Agent Readiness, Trustworthiness, Reliability

**Status:** Fully actionable now.

**Cursor Prompt:**

```
Goal: Add prompt regression tests that verify agent prompt templates
produce structurally valid output against the golden corpus, using
the existing simulator infrastructure.

Read first:
- ArchLucid.AgentRuntime/ (agent handler implementations)
- ArchLucid.AgentSimulator/ (simulator responses)
- ArchLucid.ContextIngestion.Tests/GoldenCorpus/
  IngestionGoldenCorpusRegressionTests.cs
- ArchLucid.Core/GoldenCorpus/
  RealLlmOutputStructuralValidator.cs
- .github/workflows/agent-eval-datasets-nightly.yml

Tasks:
1. In ArchLucid.AgentRuntime.Tests, create a new test class
   AgentPromptRegressionTests.cs.
2. For each agent type (Topology, Cost, Compliance, Critic),
   add a test that:
   a. Constructs the prompt using the same template the agent
      handler uses
   b. Feeds it to the simulator
   c. Parses the output using RealLlmOutputStructuralValidator
   d. Asserts structural validity (non-empty, valid JSON where
      expected, required fields present)
3. Add a [Trait("Suite", "Core")] attribute so these run in the
   fast CI tier.
4. Add a test that asserts the prompt template hash has not changed
   since last baseline (store hashes in a .json file alongside
   the test class). If the hash changes, the test fails with a
   message asking the developer to update the baseline and review
   the prompt change for quality impact.

Acceptance criteria:
- AgentPromptRegressionTests.cs exists with 4+ test methods
- All tests pass with current simulator responses
- Prompt hash baseline file exists
- Hash-change test provides actionable failure message
- Tests are tagged Suite=Core

Constraints:
- Do NOT modify any agent prompt templates
- Do NOT modify the simulator responses
- Do NOT add real LLM calls in these tests
- Tests must run without network access

Do not change: Any prompt template, any simulator response file,
any non-test .cs file.
```

---

### Improvement 8: Create MSA and Commercial Contract Template

**Why it matters:** The first sales conversation requires a commercial contract. The DPA exists but no MSA template exists. Legal review of a new MSA takes weeks; having a template ready eliminates one blocker from the first deal cycle.

**Expected impact:** Directly improves Procurement Readiness (+8-12 pts), Commercial Packaging (+3-5 pts), Marketability (+2-3 pts). Weighted readiness impact: +0.3-0.6%.

**Affected qualities:** Procurement Readiness, Commercial Packaging, Marketability

**Status:** Fully actionable now.

**Cursor Prompt:**

```
Goal: Create a Master Service Agreement template for ArchLucid
SaaS subscriptions, aligned with the existing DPA template and
pricing philosophy.

Read first:
- docs/go-to-market/DPA_TEMPLATE.md
- docs/go-to-market/PRICING_PHILOSOPHY.md (if it exists; search
  for PRICING_PHILOSOPHY)
- docs/go-to-market/SUBPROCESSORS.md
- docs/library/API_SLOS.md (SLO targets for SLA section)
- docs/EXECUTIVE_SPONSOR_BRIEF.md (product description language)

Tasks:
1. Create docs/go-to-market/MSA_TEMPLATE.md with standard SaaS MSA
   sections:
   a. Definitions (aligned with glossary and DPA)
   b. Scope of Service (reference EXECUTIVE_SPONSOR_BRIEF §1)
   c. Subscription Terms (reference pricing philosophy tiers)
   d. Service Level Agreement (reference API_SLOS.md targets —
      use "Target" not "Guarantee" language for V1)
   e. Data Processing (cross-reference DPA_TEMPLATE.md)
   f. Security (cross-reference trust-center.md)
   g. Intellectual Property
   h. Limitation of Liability
   i. Term and Termination
   j. General Provisions
2. Mark all sections that require legal review with
   <<LEGAL REVIEW REQUIRED>> markers.
3. Use the same formatting and tone as DPA_TEMPLATE.md.
4. Add a header noting this is a template, not executed, and
   requires legal counsel review.

Acceptance criteria:
- MSA_TEMPLATE.md exists at docs/go-to-market/MSA_TEMPLATE.md
- All 10 sections (a-j) are present with substantive draft content
- <<LEGAL REVIEW REQUIRED>> markers on liability, IP, and
  indemnification sections at minimum
- Cross-references to DPA, trust center, and SLO docs use relative
  links
- No fabricated legal citations or precedent references

Constraints:
- Do NOT present this as a final legal document
- Do NOT copy language from third-party MSA templates verbatim
- Do NOT modify any existing file

Do not change: Any existing file.
```

---

### Improvement 9: Publish Pricing Page on Marketing Site

**Why it matters:** Buyers cannot evaluate price/value without seeing pricing. The marketing route `/pricing` exists in code. The pricing data structure exists. The page needs to be wired and verified.

**Expected impact:** Directly improves Marketability (+5-8 pts), Commercial Packaging (+5-8 pts), Procurement Readiness (+3-5 pts). Weighted readiness impact: +0.5-0.8%.

**Affected qualities:** Marketability, Commercial Packaging, Procurement Readiness

**Status:** Fully actionable now.

**Cursor Prompt:**

```
Goal: Verify the marketing pricing page renders correctly with
current tier data and fix any issues preventing it from displaying
properly.

Read first:
- archlucid-ui/src/app/(marketing)/pricing/ (page component)
- archlucid-ui/public/pricing.json
- archlucid-ui/src/lib/pricing-types.ts
- archlucid-ui/src/components/marketing/
  MarketingTierPricingSection.test.tsx
- docs/go-to-market/PRICING_PHILOSOPHY.md (if it exists)

Tasks:
1. Read the pricing page component and pricing.json to understand
   the current tier data structure.
2. Verify pricing.json has complete data for all tiers (Free Trial,
   Team, Enterprise) with feature lists, pricing, and CTA labels.
3. If pricing.json has placeholder or empty values, populate them
   with the tier definitions from PRICING_PHILOSOPHY.md.
4. Run the MarketingTierPricingSection test to verify rendering.
5. If the Stripe checkout URL field in pricing.json is empty,
   set it to a descriptive placeholder like
   "https://checkout.stripe.com/placeholder-replace-before-launch"
   with a code comment explaining it must be replaced.
6. Verify the /pricing route renders without errors in dev mode.

Acceptance criteria:
- pricing.json has complete data for all tiers
- No empty or null required fields
- Stripe checkout URL has a clearly-labeled placeholder if not yet
  available
- Pricing section test passes
- Page renders three tier cards with features, pricing, and CTAs

Constraints:
- Do NOT change tier definitions without referencing
  PRICING_PHILOSOPHY.md
- Do NOT add real Stripe URLs
- Do NOT modify the pricing component layout or design

Do not change: Page component layout, any .cs file, any .tf file.
```

---

### Improvement 10: Benchmark End-to-End Real-Mode Execution Time

**Why it matters:** The product's decision velocity claim is untested. Measuring the actual time from wizard submission to committed manifest under real-mode execution provides a concrete data point for buyer conversations and identifies performance bottlenecks.

**Expected impact:** Directly improves Decision Velocity (+5-8 pts), Performance (+3-5 pts), Proof-of-ROI Readiness (+2-3 pts). Weighted readiness impact: +0.2-0.5%.

**Affected qualities:** Decision Velocity, Performance, Proof-of-ROI Readiness, Time-to-Value

**Status:** Fully actionable now.

**Cursor Prompt:**

```
Goal: Add a benchmarking script that measures end-to-end execution
time from architecture request submission to committed manifest
under both simulator and real-mode, producing a JSON summary.

Read first:
- ArchLucid.Benchmarks/ (existing benchmark project)
- ArchLucid.Cli/Commands/ (CLI command implementations)
- docker-compose.real-aoai.yml
- docs/library/FIRST_REAL_VALUE.md (if it exists)

Tasks:
1. Create a PowerShell script at scripts/benchmark-e2e-time.ps1
   that:
   a. Accepts a -Mode parameter (Simulator or Real)
   b. Starts a timer
   c. Calls POST /v1/architecture/request with the Contoso Retail
      brief (or the default sample brief)
   d. Polls GET /v1/architecture/run/{runId} until status is
      ready for commit (or timeout after 5 minutes)
   e. Calls POST /v1/architecture/run/{runId}/commit
   f. Stops the timer
   g. Outputs a JSON object with: mode, totalMs, requestMs,
      executionMs, commitMs, runId, timestamp
2. Add a -Repeat parameter (default 1) for multiple runs.
3. Add a -OutputFile parameter for saving results.
4. Add error handling for API failures with clear error messages.
5. Document the script in a short section at the top of the file.

Acceptance criteria:
- Script exists at scripts/benchmark-e2e-time.ps1
- Works with both Simulator and Real modes
- Produces valid JSON output
- Handles API errors gracefully
- Documents prerequisites (API running, connection string set)
- Does not require any code changes to the API or CLI

Constraints:
- Do NOT modify any application code
- Do NOT add dependencies
- Script should work with a running API instance

Do not change: Any .cs, .ts, .tsx, or .tf file.
```

---

## 9. Deferred Scope Uncertainty

The V1/V1.1/V2 deferred items are well-documented in `docs/library/V1_DEFERRED.md` and `docs/PENDING_QUESTIONS.md`. All referenced deferred items (Jira, Confluence, ServiceNow, Slack, SCIM 2.0, OPA import, pen-test publication, PGP key, privacy notice) were located and verified in the source material. No deferred scope was penalized in scoring. **No deferred scope uncertainty exists.**

---

## 10. Pending Questions for Later

### Improvement 1 (Staging Deployment)
- What is the current state of the ACR (Azure Container Registry) — does it have valid pushed images?
- Are the Entra app registrations for staging already created via `terraform-entra`?
- Is the staging SQL Server accessible from Container Apps via private endpoint?

### Improvement 3 (Stripe Staging)
- Does a Stripe test-mode account already exist for ArchLucid?
- Are the Stripe secret key and webhook signing secret already in Key Vault for staging?

### Improvement 6 (External Pilot — DEFERRED)
- Who is the target design partner?
- What vertical are they in?
- What is the acceptable timeline for pilot completion?

### Improvement 8 (MSA Template)
- Does ArchLucid have outside legal counsel available for MSA review?
- Are there jurisdiction-specific requirements (Delaware, UK, EU)?
- What is the target contract value range for the MSA's liability caps?

### Improvement 9 (Pricing Page)
- Are the prices in PRICING_PHILOSOPHY.md approved for public display?
- Is annual billing the only option, or should monthly also be shown?
- Should the Enterprise tier show "Contact sales" or a specific price?
