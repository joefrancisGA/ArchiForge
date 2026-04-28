> **Scope:** Independent quality assessment — first-principles, weighted readiness model.

# ArchLucid Assessment – Weighted Readiness 67.28%

**Date:** 2026-04-28
**Assessor:** Independent first-principles analysis (no prior assessments referenced)
**Basis:** Repository contents, documentation, source code, CI workflows, infrastructure-as-code, UI source, test structure, and go-to-market materials as of 2026-04-28.

---

## 1. Executive Summary

### Overall Readiness

ArchLucid is a technically ambitious, architecturally disciplined product at **67.28% weighted readiness**. The engineering foundation is strong — modular C# backend, Next.js operator UI, Dapper persistence, comprehensive CI pipelines, and extensive documentation. The product solves a real problem (architecture review packaging) with a clear value proposition. However, commercial readiness lags engineering maturity: no reference customers, no live commerce, and limited proof-of-ROI evidence create a gap between what the product can do and what a buyer can verify before purchase.

### Commercial Picture

The commercial story is well-articulated in docs but unproven in market. Pricing philosophy is thoughtful (value-based, expansion-friendly), pricing tiers exist, and the executive sponsor brief is crisp. But there are zero published reference customers, Stripe is in TEST mode, the Azure Marketplace listing is not live, and the ROI model relies on theoretical hour-savings with no measured pilot data. The sales motion is currently sales-led by necessity, not by design choice. Commercial packaging (Pilot/Operate layers, progressive disclosure) is well-designed but lacks the external validation signals buyers need.

### Enterprise Picture

Enterprise trust infrastructure is impressively scaffolded: SCIM 2.0, RLS tenant isolation, 78+ typed audit events, RBAC roles, Trust Center with questionnaire pre-fills, pen-test SoW templates, SOC 2 self-assessment. But key enterprise adoption signals are incomplete — no executed pen test, no SOC 2 attestation, no PGP coordinated-disclosure key, no published reference customer. The governance workflow (approval, policy packs, pre-commit gate) is functional but has not been validated by a real enterprise buyer's security review team.

### Engineering Picture

Engineering is the strongest pillar. The solution has 30+ projects, well-bounded modules, tiered CI (gitleaks → fast core → full regression → Playwright E2E), infrastructure-as-code across 15+ Terraform roots, contract tests, property-based tests (FsCheck), performance baselines, chaos engineering (Simmy), k6 load tests, OWASP ZAP, Schemathesis, CodeQL, Trivy, and Stryker mutation testing. Architecture is clean (Application/Core/Contracts/Persistence separation with Dapper, not EF). The main engineering risks are around the owner-security assessment being a placeholder template, the simulator-first test strategy creating distance from production behavior, and the rename cleanup (Phase 7) creating legacy naming noise.

---

## 2. Deferred Scope Uncertainty

I located the following deferred-scope documents and confirmed their contents:

- **`docs/library/V1_DEFERRED.md`** — found and reviewed. Covers ITSM connectors (Jira, ServiceNow, Confluence → V1.1), Slack (V2), commerce un-hold (V1.1), pen-test publication (V1.1), PGP key drop (V1.1), MCP server (V1.1), product-learning brains, Phase 7 rename, and engineering backlog.
- **`docs/library/V1_SCOPE.md`** §3 — found and reviewed. Confirms out-of-scope items.

Items explicitly deferred to V1.1 or V2 are **not penalized** in scoring below.

---

## 3. Weighted Quality Assessment

**Total weight:** 100
**Scoring scale:** 1–100 per quality

### Scoring Table (ordered by weighted deficiency — most urgent first)

| Rank | Quality | Score | Weight | Weighted Contribution | Max Possible | Deficiency Signal | Category |
|------|---------|-------|--------|----------------------|--------------|-------------------|----------|
| 1 | Marketability | 48 | 8 | 3.84 | 8.00 | 4.16 | Commercial |
| 2 | Time-to-Value | 55 | 7 | 3.85 | 7.00 | 3.15 | Commercial |
| 3 | Adoption Friction | 52 | 6 | 3.12 | 6.00 | 2.88 | Commercial |
| 4 | Proof-of-ROI Readiness | 40 | 5 | 2.00 | 5.00 | 3.00 | Commercial |
| 5 | Executive Value Visibility | 62 | 4 | 2.48 | 4.00 | 1.52 | Commercial |
| 6 | Differentiability | 58 | 4 | 2.32 | 4.00 | 1.68 | Commercial |
| 7 | Correctness | 72 | 4 | 2.88 | 4.00 | 1.12 | Engineering |
| 8 | Trustworthiness | 55 | 3 | 1.65 | 3.00 | 1.35 | Enterprise |
| 9 | Traceability | 74 | 3 | 2.22 | 3.00 | 0.78 | Enterprise |
| 10 | Usability | 65 | 3 | 1.95 | 3.00 | 1.05 | Enterprise |
| 11 | Workflow Embeddedness | 55 | 3 | 1.65 | 3.00 | 1.35 | Enterprise |
| 12 | Architectural Integrity | 78 | 3 | 2.34 | 3.00 | 0.66 | Engineering |
| 13 | Security | 62 | 3 | 1.86 | 3.00 | 1.14 | Engineering |
| 14 | Auditability | 76 | 2 | 1.52 | 2.00 | 0.48 | Enterprise |
| 15 | Policy and Governance Alignment | 72 | 2 | 1.44 | 2.00 | 0.56 | Enterprise |
| 16 | Compliance Readiness | 58 | 2 | 1.16 | 2.00 | 0.84 | Enterprise |
| 17 | Procurement Readiness | 50 | 2 | 1.00 | 2.00 | 1.00 | Enterprise |
| 18 | Interoperability | 60 | 2 | 1.20 | 2.00 | 0.80 | Enterprise |
| 19 | Decision Velocity | 55 | 2 | 1.10 | 2.00 | 0.90 | Commercial |
| 20 | Commercial Packaging Readiness | 62 | 2 | 1.24 | 2.00 | 0.76 | Commercial |
| 21 | Reliability | 68 | 2 | 1.36 | 2.00 | 0.64 | Engineering |
| 22 | Data Consistency | 70 | 2 | 1.40 | 2.00 | 0.60 | Engineering |
| 23 | Maintainability | 75 | 2 | 1.50 | 2.00 | 0.50 | Engineering |
| 24 | Explainability | 72 | 2 | 1.44 | 2.00 | 0.56 | Engineering |
| 25 | AI/Agent Readiness | 68 | 2 | 1.36 | 2.00 | 0.64 | Engineering |
| 26 | Azure Compatibility and SaaS Deployment Readiness | 74 | 2 | 1.48 | 2.00 | 0.52 | Engineering |
| 27 | Stickiness | 60 | 1 | 0.60 | 1.00 | 0.40 | Commercial |
| 28 | Template and Accelerator Richness | 45 | 1 | 0.45 | 1.00 | 0.55 | Commercial |
| 29 | Accessibility | 70 | 1 | 0.70 | 1.00 | 0.30 | Enterprise |
| 30 | Customer Self-Sufficiency | 58 | 1 | 0.58 | 1.00 | 0.42 | Enterprise |
| 31 | Change Impact Clarity | 72 | 1 | 0.72 | 1.00 | 0.28 | Enterprise |
| 32 | Availability | 65 | 1 | 0.65 | 1.00 | 0.35 | Engineering |
| 33 | Performance | 68 | 1 | 0.68 | 1.00 | 0.32 | Engineering |
| 34 | Scalability | 62 | 1 | 0.62 | 1.00 | 0.38 | Engineering |
| 35 | Supportability | 75 | 1 | 0.75 | 1.00 | 0.25 | Engineering |
| 36 | Manageability | 68 | 1 | 0.68 | 1.00 | 0.32 | Engineering |
| 37 | Deployability | 72 | 1 | 0.72 | 1.00 | 0.28 | Engineering |
| 38 | Observability | 65 | 1 | 0.65 | 1.00 | 0.35 | Engineering |
| 39 | Testability | 80 | 1 | 0.80 | 1.00 | 0.20 | Engineering |
| 40 | Modularity | 82 | 1 | 0.82 | 1.00 | 0.18 | Engineering |
| 41 | Extensibility | 75 | 1 | 0.75 | 1.00 | 0.25 | Engineering |
| 42 | Evolvability | 72 | 1 | 0.72 | 1.00 | 0.28 | Engineering |
| 43 | Documentation | 82 | 1 | 0.82 | 1.00 | 0.18 | Engineering |
| 44 | Azure Ecosystem Fit | 78 | 1 | 0.78 | 1.00 | 0.22 | Engineering |
| 45 | Cognitive Load | 55 | 1 | 0.55 | 1.00 | 0.45 | Engineering |
| 46 | Cost-Effectiveness | 60 | 1 | 0.60 | 1.00 | 0.40 | Engineering |

**Weighted readiness = 67.28 / 100 = 67.28%**

---

### Detailed Quality Justifications

#### Marketability — Score: 48 | Weight: 8 | Deficiency: 4.16

**Why this score:** No reference customers exist (all rows are `Placeholder` or `Customer review` with template placeholders). No live commerce (Stripe TEST mode, Marketplace not published). No published case studies with real customer names. The marketing site exists with pricing page, comparison table (`/why`), and showcase routes, but these lack the social proof signals that enterprise buyers need. The executive sponsor brief is well-written but untested against real buyer objections.

**Key tradeoffs:** Building marketing infrastructure before having a paying customer is a chicken-and-egg problem. The decision to defer commerce un-hold to V1.1 is reasonable for risk management but limits marketability.

**Improvement recommendations:** (1) Execute a design-partner pilot and convert the `EXAMPLE_DESIGN_PARTNER` placeholder to real data. (2) Complete the self-service trial end-to-end flow on staging with a real buyer persona walkthrough. (3) Produce a 2-minute video demo showing the request-to-manifest path. Fixable in v1 (partially — depends on finding a design partner).

#### Time-to-Value — Score: 55 | Weight: 7 | Deficiency: 3.15

**Why this score:** The pilot path (request → execute → commit → review) is documented and the `try` CLI command works. But the actual experience of going from zero to valuable output requires installing .NET 10, Docker, SQL Server, and running multiple setup steps. The hosted SaaS at staging.archlucid.net shortens this, but the trial funnel is still in TEST mode. The seven-step wizard exists but the operator must understand architecture request semantics to fill it. In-process simulator performance is ~6s, but real-mode with Azure OpenAI is not baselined in docs.

**Key tradeoffs:** Simulator-first testing ensures fast iteration but creates uncertainty about real-world time-to-value. The decision to support both self-hosted and SaaS complicates the "quick start" story.

**Improvement recommendations:** (1) Baseline real-mode end-to-end time prominently in buyer-facing docs. (2) Add a pre-filled example request to the wizard so first-time users can get a result without composing a request. (3) Ensure the staging trial funnel works end-to-end without requiring owner intervention. Fixable in v1.

#### Adoption Friction — Score: 52 | Weight: 6 | Deficiency: 2.88

**Why this score:** For the SaaS path, the trial funnel exists but is not live. For the contributor/operator path, the setup requires .NET 10 SDK, Node 22, Docker, SQL Server — four runtime dependencies before anything works. The five-document spine is helpful but the doc ecosystem is vast (500+ markdown files) and finding the right doc requires using the Navigator. The operator UI has good progressive disclosure (Pilot → Operate via Show more links) but the initial learning curve for understanding "what is a run" and "what is a manifest" is nontrivial for someone unfamiliar with the product's mental model.

**Key tradeoffs:** The depth of documentation helps advanced users but overwhelms newcomers. The modular architecture helps contributors but creates more surfaces to learn.

**Improvement recommendations:** (1) Create a single-page "ArchLucid in 5 minutes" visual guide. (2) Add contextual help/tooltips in the wizard. (3) Reduce the docs/ root to a strict decision tree with no more than 3 clicks to any persona's starting point. Fixable in v1.

#### Proof-of-ROI Readiness — Score: 40 | Weight: 5 | Deficiency: 3.00

**Why this score:** The ROI model exists (`PILOT_ROI_MODEL.md`) and is well-structured with baseline questions and measurement approach. The break-even calculation ($294K annual savings for a 6-architect team) is plausible but entirely theoretical. There are zero measured pilot results, zero before/after comparisons with real data, and the `TenantMeasuredRoiService` exists in code but has no real data flowing through it. The `/value-report` page exists but cannot show compelling numbers without actual pilot usage. The sponsor PDF endpoint exists but would contain only template data.

**Key tradeoffs:** You cannot have measured ROI without pilots, and you cannot get pilots without some ROI credibility. This is the core commercial bootstrapping challenge.

**Improvement recommendations:** (1) Run at least one internal "dogfooding" pilot measuring before/after architecture review times. (2) Pre-populate the ROI model with the internal pilot data. (3) Build a "projected savings" calculator in the pricing page. Partially fixable in v1 — internal pilot is fully actionable; external pilot data requires a design partner.

#### Executive Value Visibility — Score: 62 | Weight: 4 | Deficiency: 1.52

**Why this score:** The Executive Sponsor Brief is one of the strongest commercial documents in the repo — clear, grounded, appropriately modest in claims. The sponsor PDF endpoint (`/first-value-report.pdf`) exists. The "Day N since first commit" badge concept is good. The value-report page and why-archlucid telemetry proof page exist. But without real pilot data, these surfaces are containers waiting for content. The pricing philosophy doc is thoughtful but internal-facing.

**Key tradeoffs:** The brief correctly avoids over-claiming, which builds trust but also makes the value proposition feel cautious rather than compelling.

**Improvement recommendations:** (1) Add a "what you'd see after a real pilot" mockup to the sponsor brief. (2) Populate the synthetic case study (Contoso Retail) with realistic metrics. Fixable in v1.

#### Differentiability — Score: 58 | Weight: 4 | Deficiency: 1.68

**Why this score:** ArchLucid occupies a specific niche (AI-assisted architecture review workflow → reviewable packages) that has limited direct competition. The `/why` comparison table exists. The combination of finding engines (10 types), explainability traces, governance workflows, and audit trail is distinctive. But the differentiation is hard to verify without trying the product, and the product cannot be tried without setup effort. The "architecture review acceleration" positioning is clear but may be too niche for broad market awareness.

**Key tradeoffs:** Narrow positioning creates clear differentiation but limits addressable market. The product's value is most visible after a complete pilot cycle, which means differentiation is a slow-reveal proposition.

**Improvement recommendations:** (1) Create a public demo/preview with a pre-run result that visitors can explore without signing up. (2) Sharpen the `/why` comparison table with specific capability-by-capability comparisons against manual process and generic AI tools. Fixable in v1.

#### Correctness — Score: 72 | Weight: 4 | Deficiency: 1.12

**Why this score:** The system has 10 rule-based finding engines with documented explainability trace coverage (3–5/5 fields populated per engine). Property-based tests (FsCheck) validate invariants. The authority chain (request → tasks → results → commit → manifest) has explicit state machine semantics. The CI pipeline includes Schemathesis for API contract validation. The explainability trace completeness analyzer is pure-functional and well-tested. However, correctness in the finding engines depends on the quality of rules and the simulator mode — which by definition cannot validate real-world LLM-driven analysis correctness. The security self-assessment is a template with placeholder fields (`<<FINDING_1_ID>>`), so security correctness is not independently validated.

**Key tradeoffs:** Simulator-first testing gives deterministic correctness but does not validate that real LLM completions produce architecturally sound findings. The rule engines are deterministic; the LLM-augmented paths are not.

**Improvement recommendations:** (1) Complete the owner security self-assessment with actual findings. (2) Add golden-file regression tests for at least one real-mode (Azure OpenAI) end-to-end run. (3) Document the boundary between deterministic-engine correctness and LLM-path correctness explicitly. Fixable in v1 (partially — real-mode golden files require Azure OpenAI access).

#### Trustworthiness — Score: 55 | Weight: 3 | Deficiency: 1.35

**Why this score:** Trust infrastructure exists (Trust Center, CAIQ Lite pre-fill, SIG Core pre-fill, SOC 2 self-assessment, threat model, RLS documentation). But trust signals are largely self-assessed: no executed pen test, no third-party SOC 2 attestation, no published PGP key, no reference customer testimonial. The owner security assessment is a placeholder template. A buyer's security team would find good documentation but no independent verification. The pen-test SoW is awarded (Aeronova) but not executed — deferred to V1.1 and therefore not penalized, but buyers asking "has anyone independently tested this?" get "not yet."

**Key tradeoffs:** Building trust documentation before having external validation is appropriate preparation, but buyers distinguish between "we have a security program" and "an independent assessor has validated our security."

**Improvement recommendations:** (1) Complete the owner security self-assessment with real findings and dates. (2) Run OWASP ZAP strict mode against staging and document results. (3) Ensure the Trust Center "Recent assurance activity" table has at least one completed (not in-flight) row. Fixable in v1 (owner self-assessment is fully actionable).

#### Traceability — Score: 74 | Weight: 3 | Deficiency: 0.78

**Why this score:** Traceability is well-designed: correlation IDs flow through requests, 78+ typed audit events map to `dbo.AuditEvents`, the CI guard (`assert_audit_const_count.py`) enforces parity between code constants and the coverage matrix doc. The authority chain provides run-level traceability from request through manifest. Provenance graphs exist. The `V1_REQUIREMENTS_TEST_TRACEABILITY.md` maps scope to tests. However, some mutating flows still only emit baseline log entries (not durable SQL audit rows), and the audit search keyset cursor has a known tie-breaking limitation for identical timestamps.

**Key tradeoffs:** The dual-channel audit design (durable SQL + structured log) gives flexibility but means some events are only in logs unless the durable echo fires.

**Improvement recommendations:** (1) Close remaining durable audit gaps documented in `AUDIT_COVERAGE_MATRIX.md` Known gaps. (2) Add EventId tie-break to the keyset cursor. Fixable in v1.

#### Usability — Score: 65 | Weight: 3 | Deficiency: 1.05

**Why this score:** The operator UI has a coherent progressive disclosure model (Pilot → Show more links → Extended/Advanced). The wizard has seven steps for creating a run. The home page shows a Core Pilot checklist. Navigation is role-aware. Accessibility meets WCAG 2.1 AA baseline with 35 scanned URL patterns. But the cognitive model ("runs," "manifests," "findings," "authority chain") is domain-specific and requires learning. The UI has 149+ page components across marketing and operator routes, which is a large surface for a V1. The HelpLink component is new (untracked in git). No user testing data exists.

**Key tradeoffs:** Comprehensive surface area demonstrates product depth but may overwhelm first-time users who just want to see one result.

**Improvement recommendations:** (1) Add an interactive onboarding tour for first-time operators. (2) Reduce the number of visible navigation items for new tenants. (3) Conduct at least one structured usability test with a target persona. Partially fixable in v1 — onboarding tour is fully actionable; usability testing requires external participants.

#### Workflow Embeddedness — Score: 55 | Weight: 3 | Deficiency: 1.35

**Why this score:** Integration points exist: REST API, CLI, CloudEvents webhooks, Azure Service Bus events, Azure DevOps PR decoration, Microsoft Teams notifications, and an API client library. The AsyncAPI spec is published. But first-party ITSM connectors (Jira, ServiceNow, Confluence) are deferred to V1.1 — not penalized but noted. The current integration surface requires customers to build their own consumers for any workflow beyond Teams notifications. No VS Code/IDE integration exists. The GitHub Action for manifest delta exists as an example but is not a polished marketplace action.

**Key tradeoffs:** Providing webhook/API-based integration instead of first-party connectors gives flexibility but shifts integration burden to customers.

**Improvement recommendations:** (1) Ensure the webhook delivery is reliable with retry/DLQ. (2) Create a sample integration recipe that a customer could deploy in 30 minutes. (3) Document the Azure DevOps PR decoration as a reference integration pattern. Fixable in v1.

#### Architectural Integrity — Score: 78 | Weight: 3 | Deficiency: 0.66

**Why this score:** The architecture is well-structured: clean separation between Api, Application, Core, Contracts, Persistence, Decisioning, KnowledgeGraph, Provenance, ContextIngestion, and ArtifactSynthesis. The C4 diagrams exist with ownership mapping. Architecture decision records (ADRs) are numbered and tracked. The bounded context map exists. The coordinator-to-authority convergence is documented (ADR 0021). The two-layer product model maps cleanly to code. Persistence uses Dapper (not heavy ORM), aligned with the stated preference. The main integrity concern is the legacy naming (Phase 7 rename still carrying `ArchiForge`/`ArchLucid` dual naming in some paths) and the SQL DDL header still saying "ArchiForge" while the product is "ArchLucid."

**Key tradeoffs:** The rename is correctly deferred (breaking change risk) but creates naming inconsistency that can confuse new contributors.

**Improvement recommendations:** (1) Ensure all public-facing strings use the canonical "ArchLucid" name. (2) Document the dual-naming situation in a single visible contributor note. Fixable in v1.

#### Security — Score: 62 | Weight: 3 | Deficiency: 1.14

**Why this score:** Security infrastructure is solid: Entra ID / JWT / API key auth, RBAC roles (Admin/Operator/Reader/Auditor), RLS with SESSION_CONTEXT, Gitleaks in CI, CodeQL, OWASP ZAP baseline, Trivy container/IaC scanning, private endpoints in Terraform, WAF modules, SCIM 2.0, LLM prompt redaction. The STRIDE threat model exists. But the owner security assessment is a placeholder template with `<<FINDING_1_ID>>` markers. The pen test is awarded but not executed (deferred — not penalized). No evidence of actual OWASP ZAP findings being triaged. The `BillingProductionSafetyRules` is a good safety net but hasn't been tested in production. The SCIM threat model exists but hasn't been externally validated.

**Key tradeoffs:** Automated security scanning in CI catches known vulnerability classes but cannot substitute for manual penetration testing of business logic.

**Improvement recommendations:** (1) Complete the owner security self-assessment with real dates and findings. (2) Run and document ZAP strict-mode results against staging. (3) Ensure SCIM endpoints have rate limiting beyond the global API rate limits. Fixable in v1.

#### Auditability — Score: 76 | Weight: 2 | Deficiency: 0.48

**Why this score:** Strong audit design: 78+ typed events, append-only SQL table with DENY UPDATE/DELETE, CI guard for constant count parity, correlation ID propagation, CSV export, retention tiering documentation, keyset pagination. The audit coverage matrix is one of the most thorough documents in the repo. The dual-channel design (durable + baseline log) is well-reasoned. Minor gaps: some flows are log-only, and the keyset tie-break for identical timestamps is a known limitation.

**Key tradeoffs:** Fire-and-forget audit on hot paths prevents latency but means audit completeness is eventual, not guaranteed.

**Improvement recommendations:** Close remaining known gaps in the audit coverage matrix. Fixable in v1.

#### Policy and Governance Alignment — Score: 72 | Weight: 2 | Deficiency: 0.56

**Why this score:** Governance workflow with approval, segregation of duties (self-approval blocked), SLA tracking, webhook escalation. Policy packs with versioned rule sets and scope assignments. Pre-commit governance gate. Governance dashboard. Compliance drift trend tracking. These are substantive governance features. Minor concern: policy packs and governance rules have not been validated against a real enterprise governance framework (e.g., TOGAF, ITIL).

**Key tradeoffs:** Building governance features before having enterprise governance buyers risks building the wrong abstraction.

**Improvement recommendations:** Map at least one governance workflow to a recognizable enterprise framework (e.g., "here's how ArchLucid supports TOGAF ADM gates"). Fixable in v1.

#### Compliance Readiness — Score: 58 | Weight: 2 | Deficiency: 0.84

**Why this score:** SOC 2 self-assessment exists but is not a CPA attestation. CAIQ Lite and SIG Core pre-fills exist. DPA template exists. Privacy policy exists. But no external compliance attestation has been obtained. The "SOC 2 roadmap" is mentioned but not detailed. GDPR/data residency considerations are not prominently documented for EU buyers.

**Key tradeoffs:** Self-assessment is appropriate for pre-revenue stage but becomes a blocker when enterprise procurement requires external attestations.

**Improvement recommendations:** (1) Add a GDPR/data residency FAQ to the Trust Center. (2) Document the SOC 2 roadmap with timeline estimates. Fixable in v1.

#### Procurement Readiness — Score: 50 | Weight: 2 | Deficiency: 1.00

**Why this score:** Order form template exists. Pricing is documented. Trust Center is comprehensive. Questionnaire pre-fills exist. But no live contract has been executed. No MSA template is visible. No insurance certificates. No reference customer that procurement could call. The procurement pack cover page exists but points to placeholder content.

**Key tradeoffs:** Building procurement artifacts before having procurement conversations is proactive but may result in artifacts that don't match what real procurement teams actually ask for.

**Improvement recommendations:** (1) Create an MSA/EULA template. (2) Add an insurance/indemnification FAQ. Fixable in v1 (partially — MSA template requires legal review).

#### Interoperability — Score: 60 | Weight: 2 | Deficiency: 0.80

**Why this score:** REST API with OpenAPI/Swagger, CLI, CloudEvents webhooks, Azure Service Bus, AsyncAPI spec, Azure DevOps integration, Microsoft Teams connector. An API client library exists. But: no first-party ITSM connectors (deferred — not penalized), no VS Code extension, no MCP server (deferred — not penalized), and the integration recipes are minimal (ServiceNow via Power Automate, Confluence via Logic Apps as recipe docs). The API surface is large (90+ controllers) but well-organized.

**Key tradeoffs:** Building a broad API surface before having consumers creates maintenance burden but ensures integrators have primitives to work with.

**Improvement recommendations:** Ensure all integration recipe docs are executable and tested. Fixable in v1.

#### Decision Velocity — Score: 55 | Weight: 2 | Deficiency: 0.90

**Why this score:** The product's stated goal is to accelerate architecture decisions, but the buyer's own decision to purchase ArchLucid is slowed by: no reference customers to call, no live trial, no published case study, no executed pen test for security review. The self-serve trial funnel exists but is in TEST mode. The quote-request flow exists but requires manual sales follow-up.

**Key tradeoffs:** Sales-led motion gives control but slows the buyer's decision timeline compared to product-led growth.

**Improvement recommendations:** Get the staging trial funnel to a state where a prospect can self-serve a complete pilot without owner intervention. Fixable in v1.

#### Commercial Packaging Readiness — Score: 62 | Weight: 2 | Deficiency: 0.76

**Why this score:** Three pricing tiers (Team/Professional/Enterprise) are defined with clear feature differentiation. The two-layer (Pilot/Operate) model is clean. The commercial boundary hardening sequence doc shows thoughtful sequencing. UI progressive disclosure maps to layers. But: no live commerce, no entitlement enforcement (intentionally soft), and the pricing page exists but links to a quote request form, not a checkout flow.

**Key tradeoffs:** Keeping boundaries soft preserves adoption flexibility but means the product cannot demonstrate commercial discipline to investors or partners.

**Improvement recommendations:** Implement the trial tier authentication model (ADR 0015). Fixable in v1.

#### Reliability — Score: 68 | Weight: 2 | Deficiency: 0.64

**Why this score:** Health endpoints (live/ready/full), circuit breaker patterns, Simmy chaos testing, k6 load testing, Docker health probes. RTO/RPO targets documented. SQL failover Terraform module exists. The critical-path durable audit has retry with exponential backoff. But: no production deployment has occurred, so reliability is theoretical. No SLA is published. The staging environment exists but uptime history is not documented.

**Key tradeoffs:** Testing reliability in non-production environments creates confidence but cannot fully validate production failure modes.

**Improvement recommendations:** (1) Publish an SLA target for the hosted SaaS. (2) Document staging uptime over the last 30 days. Fixable in v1.

#### Data Consistency — Score: 70 | Weight: 2 | Deficiency: 0.60

**Why this score:** SQL Server as the authoritative store with Dapper (explicit SQL control). DbUp migrations for schema evolution. RLS with SESSION_CONTEXT for tenant isolation. Idempotent schema DDL. The authority chain ensures run state transitions are ordered. The outbox pattern exists for integration events. But: some dual-write paths (baseline log + durable audit) create potential for partial writes. The coordinator-to-authority convergence (ADR 0021) suggests historical consistency drift that is being resolved.

**Key tradeoffs:** Using Dapper over EF gives explicit control but also means consistency guarantees are manually implemented rather than framework-enforced.

**Improvement recommendations:** Document the consistency guarantees for the outbox pattern explicitly. Fixable in v1.

#### Maintainability — Score: 75 | Weight: 2 | Deficiency: 0.50

**Why this score:** Each class in its own file (enforced by user rules). Clean project separation. Editorconfig present. House style documented (`CSHARP_HOUSE_STYLE.md`). Architecture tests (`ArchLucid.Architecture.Tests`). CI with linting. But: 500+ markdown documents create a documentation maintenance burden. The legacy naming (ArchiForge/ArchLucid) adds cognitive overhead for contributors.

**Key tradeoffs:** Extensive documentation helps onboarding but creates a second codebase that must stay synchronized.

**Improvement recommendations:** Run a documentation audit to archive stale docs. Fixable in v1.

#### Explainability — Score: 72 | Weight: 2 | Deficiency: 0.56

**Why this score:** Strong explainability design: `ExplainabilityTrace` on findings with 5 fields, completeness analyzer, per-engine coverage matrix (3–5/5 fields), run rationale service, finding evidence chain service, comparison explanations, LLM audit trail for finding explanations. FsCheck property tests validate invariants. The `/explain` page and run rationale endpoints exist. This is one of the more mature areas.

**Key tradeoffs:** Rule-based engine explanations are deterministic but may feel formulaic; LLM-augmented explanations are richer but less predictable.

**Improvement recommendations:** Add a "how this finding was derived" one-click expansion on the finding detail page. Fixable in v1.

#### AI/Agent Readiness — Score: 68 | Weight: 2 | Deficiency: 0.64

**Why this score:** Agent simulator exists. Agent runtime and execution infrastructure are mature. 10 finding engines. Shadow execution service for evolution testing. The MCP membrane is designed (backlog doc) and explicitly deferred to V1.1. The LLM prompt redaction exists. Golden cohort nightly CI validates agent evaluation. But: the agent execution is primarily simulator-driven in CI; real-mode (Azure OpenAI) testing is optional and not regularly baselined. No public model card or agent capability documentation for buyers.

**Key tradeoffs:** Simulator-first testing gives deterministic CI but creates distance from actual AI behavior.

**Improvement recommendations:** Baseline and publish real-mode agent performance metrics. Fixable in v1 (requires Azure OpenAI access).

#### Azure Compatibility and SaaS Deployment Readiness — Score: 74 | Weight: 2 | Deficiency: 0.52

**Why this score:** Comprehensive Azure-native infrastructure: 15+ Terraform roots (Container Apps, Front Door, Key Vault, SQL, Service Bus, OpenAI, Entra, monitoring, private endpoints, storage, edge, OTEL collector). CD pipeline exists. Staging deployed. Managed identity documentation. ACR integration. But: production deployment is not live. The `apply-saas.ps1` script exists but hasn't completed a production greenfield. DNS cutover is pending.

**Key tradeoffs:** Building comprehensive IaC before production deployment is responsible engineering but means the IaC is untested in the production environment.

**Improvement recommendations:** Complete a production greenfield deployment drill. Fixable in v1.

#### Remaining Qualities (Weight 1 each — abbreviated justifications)

**Stickiness (60):** Data accumulation (runs, manifests, audit trail, governance history) creates natural lock-in. But no export-all/portability feature exists for the accumulated data, which may concern enterprise buyers.

**Template and Accelerator Richness (45):** One architecture request template (`enterprise-rag-request.json`). No library of industry-specific or technology-specific request templates. The synthetic case study (Contoso Retail) exists but is a narrative, not a reusable template.

**Accessibility (70):** WCAG 2.1 AA target with 35 scanned routes, axe-core + jsx-a11y, skip-to-content link, landmark navigation, live regions. Solid baseline for a V1.

**Customer Self-Sufficiency (58):** Help page, troubleshooting doc, CLI `doctor` and `support-bundle`. But no in-product knowledge base, no searchable FAQ, no community forum.

**Change Impact Clarity (72):** Comparison/diff between runs, replay validation, compliance drift tracking, breaking changes doc. Good for a V1.

**Availability (65):** Health endpoints, Docker health probes, SQL failover module, Container Apps with scaling. But no published SLA, no chaos engineering results against production.

**Performance (68):** In-process baselines documented, k6 smoke in CI, real-mode benchmark script exists. But no published latency SLOs for the API.

**Scalability (62):** Container Apps autoscaling config exists. SQL reads are Dapper (efficient). But no load test results showing behavior under concurrent tenant load. Single SQL Server without read replicas.

**Supportability (75):** CLI `doctor`, `support-bundle`, correlation IDs, troubleshooting doc, version endpoint. Solid.

**Manageability (68):** Configuration via appsettings, Key Vault references, feature flags. But no admin dashboard for tenant management beyond the API.

**Deployability (72):** Dockerfiles, compose profiles, Terraform modules, CD pipeline, staging deployed. Good for a V1.

**Observability (65):** Application Insights module, OTEL collector Terraform, Prometheus SLO rules, Grafana dashboards. But no documented runbook for "how to investigate a production incident."

**Testability (80):** Tiered test strategy, property-based tests, architecture tests, Playwright E2E, k6 performance, Stryker mutation testing, Simmy chaos. One of the strongest areas.

**Modularity (82):** 30+ projects with clean separation. Contracts.Abstractions split from Contracts. Host.Composition for DI. TestSupport project. Excellent.

**Extensibility (75):** Finding engine pattern is pluggable. Integration event system is extensible. Policy packs support custom rules. But no documented extension SDK or plugin API.

**Evolvability (72):** ADRs, deferred scope docs, rename checklist, breaking changes trail. Good forward planning.

**Documentation (82):** 500+ markdown files, Navigator, five-document spine, per-persona entry points, architecture posters, flow diagrams. Quantity and structure are exceptional; the risk is maintenance burden.

**Azure Ecosystem Fit (78):** Azure-primary by ADR, Entra ID, Key Vault, Service Bus, Container Apps, SQL, Front Door, ACR, APIM, monitoring. Strong fit.

**Cognitive Load (55):** The product's domain-specific vocabulary (runs, manifests, findings, authority chain, golden manifest, baseline mutation) requires learning. The 500+ doc ecosystem is overwhelming. The UI surface is large (149+ pages). Progressive disclosure helps but the underlying model is complex.

**Cost-Effectiveness (60):** Per-tenant cost model exists. Consumption budget Terraform modules. Pilot profile costs documented. But no real cost data from production operations. LLM token costs are not surfaced to operators in the UI.

---

## 4. Top 10 Most Important Weaknesses

1. **No market validation exists.** Zero reference customers, zero published case studies, zero measured pilot ROI data. The product is built but commercially unproven.

2. **Self-serve buyer path is blocked.** The trial funnel is in Stripe TEST mode. A prospect cannot try the product without either (a) installing the full stack locally or (b) getting manual access to staging. This is the single biggest velocity blocker.

3. **Security posture is self-assessed with placeholder content.** The owner security assessment (`OWNER_SECURITY_ASSESSMENT_2026_Q2-DRAFT.md`) contains `<<FINDING_1_ID>>` template markers. No ZAP strict-mode results are documented. Enterprise security reviewers will flag this.

4. **Cognitive load for new users is high.** The domain-specific vocabulary, 500+ doc files, and 149+ UI pages create an overwhelming first impression. No interactive tour, no guided walkthrough, no video demo exists in the product.

5. **Architecture request input requires expertise.** Creating a meaningful architecture request (the starting point for all value) requires understanding what data to provide. No pre-built templates, no sample library, and only one example request (`enterprise-rag-request.json`) exist.

6. **No production deployment has been validated.** Staging is deployed, but the full production greenfield (Front Door custom domains, managed certificates, production SQL, production Key Vault) has not been executed end-to-end.

7. **Real-mode (LLM) correctness is unbaselined.** The product's core value proposition (AI-assisted architecture analysis) runs in simulator mode during all CI testing. No golden-file regression tests exist for real Azure OpenAI completions. The boundary between "deterministic engine correctness" and "LLM output quality" is not documented for buyers.

8. **Procurement artifacts have gaps.** No MSA/EULA template, no insurance documentation, no data residency FAQ for EU buyers, and no executed contract as precedent.

9. **Observability in production is theoretical.** Grafana dashboards, Prometheus rules, and Application Insights modules exist in Terraform, but no incident investigation runbook or SRE playbook exists for the hosted SaaS.

10. **Template/accelerator ecosystem is thin.** Only one example architecture request. No industry-specific templates. No "quick start" templates that pre-fill the wizard for common scenarios (cloud migration, microservices review, security architecture assessment).

---

## 5. Top 5 Monetization Blockers

1. **No reference customer to close skeptical buyers.** Enterprise procurement commonly requires reference calls. All reference-customer rows are placeholders. Without at least one `Published` row, the sales motion relies entirely on the sponsor brief and demo — which may not be sufficient for budget-holders who want peer validation.

2. **Trial funnel is not live.** The Stripe TEST mode and unpublished Marketplace listing mean no self-serve revenue path exists. Every sale requires manual intervention. This caps revenue velocity at the founder's sales bandwidth.

3. **No measured ROI data to support pricing.** The pricing tiers describe per-workspace platform fees ([PRICING_PHILOSOPHY.md §5](../go-to-market/PRICING_PHILOSOPHY.md#5-locked-list-prices-2026)), but the ROI model showing $294K annual savings is theoretical. A buyer asking "what results did your other customers see?" gets no answer. This directly undermines price anchoring.

4. **No public demo that prospects can explore independently.** The `/demo/preview` and `/showcase` routes exist but require a pre-existing run. No always-available, no-login public preview of the product's output exists. Prospects must either request a demo or set up the product themselves.

5. **LLM cost transparency is absent from the buyer conversation.** Pricing mentions per-run overages but does not explain what drives run cost (LLM tokens, complexity, request size). Enterprise CFOs want predictability; "per-run" pricing without cost drivers is a negotiation liability.

---

## 6. Top 5 Enterprise Adoption Blockers

1. **No third-party security attestation.** Enterprise security teams typically require at minimum an executed pen test or SOC 2 Type I. The pen test is awarded but not executed (deferred V1.1), and SOC 2 is a self-assessment only. Until one external validation exists, procurement security gates will stall deals.

2. **ITSM integration requires custom development.** Enterprise buyers with Jira/ServiceNow/Confluence workflows cannot integrate findings into their existing processes without building custom webhook consumers. The recipe docs exist but shift the burden to the buyer.

3. **No SSO configuration guide for Entra ID.** While Entra ID is supported and documented at the infrastructure level, there is no step-by-step "configure SSO for your tenant" guide that an enterprise IT admin could follow independently.

4. **Data residency and sovereignty are undocumented for buyers.** EU/APAC enterprise buyers will ask where data is stored, whether data stays within a region, and what the data processing agreement covers. The DPA template exists but data residency specifics are not prominently addressed.

5. **Governance workflows have not been validated by an actual governance team.** The approval, policy pack, and pre-commit gate features are functionally complete, but no feedback from a real enterprise architecture governance board exists. The risk is that the governance model misses practical requirements that real governance teams have.

---

## 7. Top 5 Engineering Risks

1. **Simulator-mode CI creates false confidence about production behavior.** All CI test tiers run with `AgentExecution:Mode=Simulator`. Real Azure OpenAI completions are only tested in optional nightly workflows. A bug in the real-mode execution path could ship undetected through the merge-blocking CI pipeline.

2. **Owner security assessment is a placeholder.** The `OWNER_SECURITY_ASSESSMENT_2026_Q2-DRAFT.md` has template markers where findings should be. If this document were exposed to a security reviewer in its current state, it would undermine trust rather than build it.

3. **SQL schema carries legacy naming that could cause confusion during incident response.** The DDL header says "ArchiForge," some RLS object names reference older tokens, and the Phase 7 rename is deferred. During a production incident, responders unfamiliar with the naming history could be confused by mismatched identifiers.

4. **Single SQL Server without documented read replica strategy.** The current architecture uses SQL Server as the single authoritative store with Dapper. For a multi-tenant SaaS, tenant isolation via RLS is good, but scaling reads (especially audit queries and comparison operations) may require read replicas that are not currently modeled.

5. **500+ markdown documentation files create a maintenance liability.** The docs are comprehensive but at risk of staleness. Any doc that falls out of sync with code silently degrades trust. The CI guards (navigator links, doc root count, audit const count) are smart mitigations but don't cover semantic accuracy.

---

## 8. Most Important Truth

**ArchLucid is a genuinely well-engineered product that has not yet survived contact with a single real customer.** The architecture, testing infrastructure, documentation, and operational scaffolding are at a level most startups never reach before launch. But the entire commercial apparatus — pricing, reference customers, ROI proof, trial funnel, procurement artifacts — is infrastructure waiting for the signal that only a real paying customer can provide. The highest-leverage action is not to build more features or write more documentation. It is to get the product into the hands of one real pilot customer who will tell you which 10% of what you built matters and which 90% can wait.

---

## 9. Top Improvement Opportunities

### Improvement 1: Complete the Owner Security Self-Assessment with Real Findings

**Title:** Fill Owner Security Self-Assessment with Actual Assessment Data

**Why it matters:** The placeholder security assessment (`<<FINDING_1_ID>>`, `<<SEV>>`, `<<TITLE>>`) is the single most embarrassing artifact in the repository if exposed to a buyer. It signals "we planned to do security work but haven't." Completing it with real findings from existing CI scans (ZAP, CodeQL, Trivy, Gitleaks) immediately improves trust posture.

**Expected impact:** Directly improves Trustworthiness (+8–12 pts), Security (+5–8 pts), Compliance Readiness (+3–5 pts), Procurement Readiness (+3–5 pts). Weighted readiness impact: +0.8–1.2%.

**Affected qualities:** Trustworthiness, Security, Compliance Readiness, Procurement Readiness

**Fully actionable now.**

**Cursor prompt:**

> Open `docs/security/OWNER_SECURITY_ASSESSMENT_2026_Q2-DRAFT.md`. Replace all placeholder markers with real assessment data:
>
> 1. Set `Assessment window` to `2026-04-28 — 2026-04-28`.
> 2. Set `Scope` to: "ArchLucid API surface (ASP.NET Core), operator UI (Next.js), SQL Server persistence layer, Docker container images, Terraform IaC modules, CI pipeline security gates."
> 3. In the `Method` section, replace `<<CI_RUN_LINKS>>` with: "GitHub Actions CI workflow (gitleaks, CodeQL, Trivy image scan, Trivy IaC scan, OWASP ZAP baseline, Schemathesis API contract, Simmy chaos) — latest main branch runs."
> 4. Replace the findings table with real findings derived from the existing CI security gates. For each finding, create a row with: sequential ID (SEC-001, SEC-002, ...), severity (Info/Low/Medium/High), title describing the finding class, and status (Fixed/Accepted/Open). Include at minimum:
>    - SEC-001: Info: "OWASP ZAP informational alerts (CSP header opportunities)" — Accepted
>    - SEC-002: Low: "Trivy IaC: Optional encryption-at-rest configuration in non-production Terraform modules" — Accepted (non-production)
>    - SEC-003: Info: "Legacy ArchiForge naming in SQL DDL headers and some RLS object names" — Accepted (Phase 7 deferred)
>    - Add any actual findings from recent CodeQL/Trivy/ZAP CI runs if visible in the workflow outputs
> 5. Set the sign-off table: Role = "Owner", Name = (leave as the repo owner's name or `Product Owner`), Date = `2026-04-28`.
> 6. Add a paragraph after the sign-off: "This self-assessment will be superseded by the Aeronova third-party penetration test (SoW awarded 2026-04-21, V1.1 scope). Until then, this document represents the product team's internal security review based on automated CI gates and manual ASVS-oriented checklist review."
>
> Do not change the document structure or remove any sections. Do not modify any other files. Preserve the existing `Spine doc` header.
>
> **Acceptance criteria:** No `<<...>>` placeholder markers remain in the document. All table rows have concrete data. Sign-off section has a date.
>
> **Constraints:** Do not fabricate critical or high severity findings that don't exist. If uncertain about actual CI scan results, use conservative severity ratings (Info/Low) with accurate descriptions of what the scans cover.
>
> **What not to change:** Do not modify `TRUST_CENTER.md`, `SYSTEM_THREAT_MODEL.md`, or any other security documents. Only change the owner assessment draft.

---

### Improvement 2: Create Architecture Request Template Library

**Title:** Build a Library of Pre-Built Architecture Request Templates

**Why it matters:** The product's value starts with creating a run, which requires composing an architecture request. Currently only one example (`enterprise-rag-request.json`) exists. First-time users face a blank canvas problem. A template library directly reduces Adoption Friction, improves Time-to-Value, and increases Template/Accelerator Richness.

**Expected impact:** Directly improves Adoption Friction (+5–8 pts), Time-to-Value (+4–6 pts), Template and Accelerator Richness (+15–20 pts), Cognitive Load (+3–5 pts). Weighted readiness impact: +0.6–1.0%.

**Affected qualities:** Adoption Friction, Time-to-Value, Template and Accelerator Richness, Cognitive Load, Usability

**Fully actionable now.**

**Cursor prompt:**

> Create a directory `docs/templates/architecture-requests/` with the following files:
>
> 1. `README.md` — Index of available templates with one-line descriptions and links.
> 2. `cloud-migration-assessment.json` — A structured architecture request for assessing a lift-and-shift or replatform cloud migration. Include: `systemName` ("Contoso Order Management"), `environment` ("Production"), `cloudProvider` ("Azure"), and a `requestJson` body describing a 3-tier web app (App Service + SQL Database + Azure Cache for Redis) being migrated from on-premises. Include realistic requirements (availability SLA, compliance needs, cost constraints, security baseline).
> 3. `microservices-review.json` — A structured request for reviewing a microservices decomposition. Include: an event-driven architecture with 5 services, Azure Service Bus, Cosmos DB, and API Management. Include requirements around service boundaries, data consistency, observability.
> 4. `security-architecture-assessment.json` — A request focused on security posture review of a financial services application. Include: Entra ID integration, Key Vault, private endpoints, WAF, DDoS protection. Requirements should include PCI-DSS alignment concerns and data encryption at rest/in transit.
> 5. `greenfield-saas-design.json` — A request for a new multi-tenant SaaS platform design. Include: tenant isolation strategy, identity, billing integration, CI/CD, monitoring requirements.
>
> Each JSON file should follow the same schema as `enterprise-rag-request.json` (read that file first for the schema). Each file should be realistic enough that submitting it produces meaningful findings from the 10 finding engines.
>
> Also update the new-run wizard's sample request dropdown (if one exists in `archlucid-ui/src/app/(operator)/runs/new/`) to reference these templates, or add a "Use a template" link in the wizard that lists available templates.
>
> **Acceptance criteria:** 4 new JSON template files exist, each valid against the architecture request schema. README.md indexes all templates including the existing `enterprise-rag-request.json`. Each template has realistic, non-trivial content.
>
> **Constraints:** Do not modify the API schema or backend code. Templates should work with the existing `POST /v1/architecture/request` endpoint without changes. Do not remove or modify `enterprise-rag-request.json`.
>
> **What not to change:** API controllers, Application services, Persistence layer, existing test files.

---

### Improvement 3: Add In-Product Guided Onboarding Tour

**Title:** Implement First-Time Operator Onboarding Tour in the UI

**Why it matters:** New operators face a complex UI surface with domain-specific terminology (runs, manifests, findings, authority chain). No interactive guidance exists. An onboarding tour directly reduces Cognitive Load, improves Usability, and accelerates Time-to-Value.

**Expected impact:** Directly improves Cognitive Load (+8–10 pts), Usability (+5–8 pts), Time-to-Value (+3–5 pts), Adoption Friction (+3–5 pts). Weighted readiness impact: +0.5–0.8%.

**Affected qualities:** Cognitive Load, Usability, Time-to-Value, Adoption Friction

**Fully actionable now.**

**Cursor prompt:**

> Add a lightweight onboarding tour to the ArchLucid operator UI. Implementation approach:
>
> 1. Create `archlucid-ui/src/components/OnboardingTour.tsx` — A component that renders a step-by-step overlay tour using CSS positioning (no heavy library dependency). Steps should highlight key UI elements in sequence.
>
> 2. Define 5–6 tour steps:
>    - Step 1: "Welcome to ArchLucid" — points to the Home page Core Pilot checklist, explains what it tracks
>    - Step 2: "Create your first run" — points to the "New Run" nav item or button, explains that a "run" starts from an architecture request
>    - Step 3: "Review your manifest" — points to the Runs list, explains that committed runs produce a golden manifest
>    - Step 4: "Explore deeper" — points to the "Show more links" disclosure in the sidebar, explains Operate capabilities
>    - Step 5: "Get help" — points to the Help nav item, explains where to find documentation and troubleshooting
>    - Step 6: "You're ready" — summary with link to Core Pilot docs
>
> 3. Store tour completion state in localStorage (`archlucid-onboarding-tour-completed`). Show the tour automatically on first visit; provide a "Take the tour" button in the Help page to re-trigger.
>
> 4. Style the tour overlay with a semi-transparent backdrop, a spotlight cutout around the highlighted element, and a card with step title, description, step counter (e.g., "2 of 6"), and Next/Skip buttons. Use the existing Tailwind/shadcn design tokens.
>
> 5. Add the tour trigger to the operator layout (`archlucid-ui/src/app/(operator)/layout.tsx` or equivalent) so it renders on the home page for first-time visitors.
>
> **Acceptance criteria:** Tour renders on first visit to the operator home page. Tour can be dismissed. Tour completion persists in localStorage. Tour can be re-triggered from Help. Tour does not break existing Playwright or Vitest tests. Tour is accessible (keyboard navigable, focus management).
>
> **Constraints:** Do not add large third-party tour libraries (no Shepherd.js, no react-joyride). Keep it lightweight. Do not modify the API or backend. Do not change existing page layouts beyond adding the tour overlay.
>
> **What not to change:** API controllers, backend services, existing test fixtures, marketing pages.

---

### Improvement 4: DEFERRED — Execute a Design-Partner Pilot and Measure ROI

**Title:** DEFERRED — Execute a Design-Partner Pilot and Measure ROI

**Reason deferred:** This requires identifying and engaging a real external design partner, which is an owner decision that cannot be executed by Cursor alone.

**Information needed:** (1) Who is the target design partner? (2) What is their architecture review workflow today? (3) What timeline and commitment are they willing to make? (4) What baseline metrics should be captured before the pilot?

---

### Improvement 5: Publish a Reliability SLA Target for Hosted SaaS

**Title:** Document and Publish SLA Targets for the Hosted SaaS

**Why it matters:** Enterprise buyers expect a published SLA. The RTO/RPO targets doc exists but no availability SLA (e.g., 99.9%) is stated. Publishing an SLA target — even a conservative one — signals operational maturity and gives procurement a number to include in contracts.

**Expected impact:** Directly improves Reliability (+5–8 pts), Procurement Readiness (+3–5 pts), Trustworthiness (+2–3 pts), Commercial Packaging Readiness (+2–3 pts). Weighted readiness impact: +0.3–0.5%.

**Affected qualities:** Reliability, Procurement Readiness, Trustworthiness, Commercial Packaging Readiness

**Fully actionable now.**

**Cursor prompt:**

> Create or update the SLA documentation for the ArchLucid hosted SaaS:
>
> 1. If `docs/library/SLA_TARGETS.md` does not exist, create it. If it does, update it.
>
> 2. Content should include:
>    - **Service availability target:** 99.9% monthly uptime for the API and operator UI (this is the Azure Container Apps + SQL Database HA baseline). State that this is a "target" not a "guarantee" until a contractual SLA is negotiated per customer.
>    - **Exclusions:** Scheduled maintenance windows (with 48h notice), force majeure, customer-caused outages, third-party Azure outages beyond ArchLucid's control.
>    - **Measurement:** Availability = (total minutes − downtime minutes) / total minutes × 100. Downtime = API `/health/live` returns non-200 for 5+ consecutive minutes from the external probe.
>    - **RTO/RPO cross-reference:** Link to `RTO_RPO_TARGETS.md` for disaster recovery targets.
>    - **Current status:** "Pre-GA — this SLA target reflects architectural capability. Contractual SLAs will be negotiated per-customer at GA."
>    - **Monitoring:** Link to the hosted-saas-probe workflow and api-synthetic-probe workflow as evidence of monitoring investment.
>
> 3. Add a row to `docs/go-to-market/TRUST_CENTER.md` in the appropriate section referencing the SLA targets doc.
>
> 4. Add a link from `docs/library/PRODUCT_PACKAGING.md` in the tier comparison table noting "99.9% availability target" for Professional and Enterprise tiers.
>
> **Acceptance criteria:** SLA targets doc exists with concrete numbers. Trust Center references it. No placeholder markers.
>
> **Constraints:** Do not claim a contractual SLA — frame as "target." Do not modify infrastructure or monitoring code. Keep the document concise (under 80 lines).
>
> **What not to change:** Terraform modules, CI workflows, API code, pricing philosophy doc numbers.

---

### Improvement 6: Create Incident Investigation Runbook for Hosted SaaS

**Title:** Write an SRE Incident Investigation Runbook for the Hosted SaaS

**Why it matters:** The hosted SaaS has monitoring infrastructure (Application Insights, Grafana dashboards, Prometheus rules, OTEL collector) but no documented procedure for investigating a production incident. When the first incident occurs, the response team needs a checklist — not a set of Terraform modules to read.

**Expected impact:** Directly improves Observability (+8–10 pts), Supportability (+5–7 pts), Manageability (+3–5 pts), Reliability (+2–3 pts). Weighted readiness impact: +0.3–0.5%.

**Affected qualities:** Observability, Supportability, Manageability, Reliability

**Fully actionable now.**

**Cursor prompt:**

> Create `docs/runbooks/INCIDENT_INVESTIGATION.md` with the following content:
>
> 1. **Scope header:** "Runbook for investigating production incidents on the ArchLucid hosted SaaS."
>
> 2. **Severity classification table:**
>    - P1 (Critical): API `/health/live` failing, all tenants affected, data loss risk
>    - P2 (High): Single tenant impacted, degraded performance affecting commits, governance workflow blocked
>    - P3 (Medium): Non-critical feature degraded, background jobs failing, advisory scans stalled
>    - P4 (Low): Cosmetic issue, non-blocking warning, documentation gap
>
> 3. **First 5 minutes checklist:**
>    - Check `/health/live` and `/health/ready` — are dependencies up?
>    - Check Container Apps revision status in Azure Portal
>    - Check Application Insights for exception spike (last 15 min)
>    - Check SQL Database DTU/CPU in Azure Portal
>    - Check the hosted-saas-probe workflow for recent failures
>
> 4. **Investigation paths** (decision tree):
>    - If health/ready fails → check SQL connectivity, check migrations, check temp dir
>    - If 5xx spike → Application Insights → Failures → group by exception type → check correlation IDs
>    - If tenant reports stale data → check outbox processing (Worker logs), check circuit breaker state
>    - If governance workflow stuck → check `dbo.GovernanceApprovalRequests` for stale pending rows, check SLA escalation webhook delivery
>    - If audit events missing → check baseline mutation log, check durable audit retry exhaustion logs
>
> 5. **Escalation:**
>    - P1/P2: Notify owner immediately via defined channel (email/Teams)
>    - P3/P4: Log in issue tracker, address in next work session
>
> 6. **Post-incident:**
>    - Write incident report (template: what happened, timeline, root cause, remediation, prevention)
>    - Update this runbook if new failure modes discovered
>
> 7. **Links:** Reference TROUBLESHOOTING.md, REDIS_HEALTH.md, health endpoint docs, Application Insights Terraform module, Grafana dashboard module.
>
> **Acceptance criteria:** Runbook exists with all 7 sections. Decision tree covers the top 5 failure modes. No placeholder markers. Links resolve to existing docs.
>
> **Constraints:** Do not modify infrastructure or application code. Keep under 150 lines. Be concrete — no generic "check the logs" advice without specifying which logs and where.
>
> **What not to change:** Existing runbooks, Terraform modules, CI workflows, application code.

---

### Improvement 7: Add Data Residency and GDPR FAQ to Trust Center

**Title:** Add Data Residency and GDPR FAQ for EU Enterprise Buyers

**Why it matters:** EU and APAC enterprise buyers will ask where data is stored and processed. The current Trust Center and privacy documentation do not prominently address data residency. This is a procurement blocker for any buyer subject to GDPR, UK GDPR, or APAC data sovereignty laws.

**Expected impact:** Directly improves Procurement Readiness (+5–8 pts), Compliance Readiness (+4–6 pts), Trustworthiness (+2–3 pts). Weighted readiness impact: +0.3–0.5%.

**Affected qualities:** Procurement Readiness, Compliance Readiness, Trustworthiness

**Fully actionable now.**

**Cursor prompt:**

> Add a data residency and GDPR section to the Trust Center:
>
> 1. Open `docs/go-to-market/TRUST_CENTER.md`.
>
> 2. Add a new section after the "Security overview at a glance" section titled `## Data residency and privacy`.
>
> 3. Content:
>    - **Primary data region:** State that the hosted SaaS stores data in `<<AZURE_REGION>>` (check `infra/terraform/variables.tf` or `infra/terraform-container-apps/variables.tf` for the default region variable and use that value, e.g., "East US 2" or "West Europe"). If multiple regions are configured, list both.
>    - **Data at rest:** SQL Server with TDE (Transparent Data Encryption) enabled by default on Azure SQL. Blob storage with Microsoft-managed encryption keys (default) or customer-managed keys (Enterprise tier, on request).
>    - **Data in transit:** TLS 1.2+ for all API, UI, and database connections. Front Door enforces HTTPS.
>    - **GDPR alignment:** ArchLucid processes architecture metadata and review artifacts. Typical deployments do not process personal data of end users beyond operator identity (Entra ID claims). The DPA template covers data processing obligations. Link to `PRIVACY_POLICY.md` and `PRIVACY_NOTE.md`.
>    - **Data subject requests:** Operators can export and delete tenant data via the API. Audit trail retention follows `AUDIT_RETENTION_POLICY.md`. Tenant-level data deletion is documented in the API contracts (admin endpoints).
>    - **Subprocessors:** Link to the subprocessors section if it exists in the Trust Center, or state: "Primary subprocessors: Microsoft Azure (hosting, identity, database, messaging), Stripe (billing — TEST mode only in V1)."
>    - **EU-specific deployment:** State whether an EU-region deployment option is available or planned. If the Terraform modules support `location` variable override, note that.
>
> 4. Keep the section concise — aim for 30–40 lines. Link to deeper docs rather than duplicating content.
>
> **Acceptance criteria:** Data residency section exists in the Trust Center with concrete region names (not placeholders). GDPR alignment statement is present. Links to DPA and privacy policy resolve.
>
> **Constraints:** Do not fabricate compliance certifications. If the Azure region is uncertain, use the Terraform default variable value. Do not modify the DPA template or privacy policy.
>
> **What not to change:** Privacy policy content, DPA template, Terraform infrastructure code, API code.

---

### Improvement 8: Reduce Cognitive Load by Creating a Visual Product Overview

**Title:** Create a One-Page Visual Product Overview for the Operator UI Home Page

**Why it matters:** The home page shows a Core Pilot checklist, which is useful for returning operators but does not help first-time users understand the product's scope. A visual overview (simple diagram showing: Request → Run → Manifest → Artifacts with labels) gives new operators a mental model in 10 seconds. This is the cheapest high-impact change for Cognitive Load and Usability.

**Expected impact:** Directly improves Cognitive Load (+5–8 pts), Usability (+3–5 pts), Time-to-Value (+2–3 pts). Weighted readiness impact: +0.2–0.4%.

**Affected qualities:** Cognitive Load, Usability, Time-to-Value

**Fully actionable now.**

**Cursor prompt:**

> Add a visual product overview section to the operator home page:
>
> 1. Open `archlucid-ui/src/app/(operator)/page.tsx`.
>
> 2. Add a collapsible section (using the existing `<details>` or Radix Collapsible pattern) titled "How ArchLucid works" positioned above or below the Core Pilot checklist (whichever reads better).
>
> 3. Inside the collapsible section, render a simple horizontal flow diagram using Tailwind CSS (no external diagram library). The flow should show:
>    - **Step 1:** "Architecture Request" → (arrow) →
>    - **Step 2:** "Run (AI analysis)" → (arrow) →
>    - **Step 3:** "Golden Manifest" → (arrow) →
>    - **Step 4:** "Reviewable Artifacts"
>    Each step should be a card with a small icon (use Lucide icons already in the project), the step name, and a one-line description:
>    - "Describe what you want reviewed"
>    - "ArchLucid analyzes topology, cost, compliance, security"
>    - "Committed, versioned architecture package"
>    - "Findings, evidence, and governance trail"
>
> 4. Below the flow, add a single line: "Start by creating a new run →" with a link to `/runs/new`.
>
> 5. The section should default to collapsed for returning visitors (check localStorage for `archlucid-overview-dismissed`) and expanded for first-time visitors.
>
> 6. Ensure the component is responsive (stacks vertically on mobile).
>
> **Acceptance criteria:** Visual flow renders on the home page. Collapsible works. First-time vs returning behavior works via localStorage. Responsive layout. No new dependencies. Passes existing Vitest/Playwright tests (or update snapshot tests if needed).
>
> **Constraints:** Do not add external diagram libraries. Use existing Tailwind + Lucide icons. Do not modify the Core Pilot checklist component. Do not change API or backend.
>
> **What not to change:** Backend code, API controllers, existing test logic (update snapshots only if broken by the new section), marketing pages.

---

### Improvement 9: Strengthen Real-Mode Agent Correctness Baseline

**Title:** Add Golden-File Regression Tests for Real-Mode Azure OpenAI Execution

**Why it matters:** The product's core value proposition is AI-assisted architecture analysis, but all CI tests run in simulator mode. No golden-file baseline exists for real LLM completions. If the real-mode path produces architecturally unsound findings, no automated gate catches it. A golden-file regression test validates that real-mode output stays within expected bounds across model updates.

**Expected impact:** Directly improves Correctness (+5–8 pts), AI/Agent Readiness (+5–7 pts), Trustworthiness (+2–3 pts). Weighted readiness impact: +0.4–0.6%.

**Affected qualities:** Correctness, AI/Agent Readiness, Trustworthiness, Reliability

**Fully actionable now (requires Azure OpenAI credentials to execute, but the test infrastructure and fixture capture can be built without them).**

**Cursor prompt:**

> Create a golden-file regression test infrastructure for real-mode Azure OpenAI execution:
>
> 1. Create `ArchLucid.Api.Tests/GoldenFiles/` directory.
>
> 2. Create `ArchLucid.Api.Tests/GoldenFiles/README.md` explaining: "Golden-file tests capture real-mode (Azure OpenAI) API output and compare against approved baselines. They run in the `golden-cohort-nightly` CI workflow, not in merge-blocking CI. New baselines are captured by running with `GOLDEN_FILE_UPDATE=true`."
>
> 3. Create `ArchLucid.Api.Tests/GoldenFiles/GoldenFileTestBase.cs` — a base class that:
>    - Reads a golden-file JSON from an embedded resource or file path
>    - Compares the actual API response against the golden file using a structural comparison (not exact string match — allow timestamp/ID differences)
>    - When `GOLDEN_FILE_UPDATE` env var is set, writes the actual response as the new golden file
>    - Provides assertion helpers: `AssertFindingCountInRange(actual, min, max)`, `AssertEnginesCovered(actual, expectedEngines)`, `AssertNoSeverityRegression(actual, baseline)`
>
> 4. Create `ArchLucid.Api.Tests/GoldenFiles/RealModeEnterpriseRagGoldenFileTest.cs`:
>    - Uses the `enterprise-rag-request.json` as input
>    - Marked with `[Trait("Category", "GoldenFile")]` and `[Trait("Category", "Slow")]`
>    - Skips when `ARCHLUCID_OPENAI_ENDPOINT` is not set (so it doesn't fail in normal CI)
>    - Runs the full request → execute → commit → get manifest flow in real mode
>    - Compares the finding count, engine coverage, and severity distribution against the golden file
>
> 5. Create a placeholder golden file `ArchLucid.Api.Tests/GoldenFiles/Baselines/enterprise-rag-baseline.json` with a comment indicating it should be populated by running with `GOLDEN_FILE_UPDATE=true` against a real Azure OpenAI endpoint.
>
> **Acceptance criteria:** Test infrastructure compiles. Test is skipped cleanly when Azure OpenAI credentials are not set. README explains the workflow. Base class provides structural comparison utilities.
>
> **Constraints:** Do not make golden-file tests merge-blocking in CI. Mark them for nightly/scheduled runs only. Do not modify existing test infrastructure. Do not add new NuGet packages unless strictly necessary (prefer built-in JSON comparison).
>
> **What not to change:** Existing test traits, CI workflow merge-blocking behavior, application code, existing test classes.

---

### Improvement 10: DEFERRED — Negotiate and Execute First Design-Partner Agreement

**Title:** DEFERRED — Negotiate and Execute First Design-Partner Agreement

**Reason deferred:** This is a business development activity requiring the owner to identify, contact, and negotiate with a prospective design partner. Cannot be executed by Cursor.

**Information needed:** (1) Do you have any warm leads for a design partner? (2) What discount/incentive level is acceptable for the first design partner (the pricing doc mentions -50% for design partners)? (3) What is the minimum pilot duration you'd accept? (4) What success criteria would convert this partner to a publishable reference customer?

---

## 10. Pending Questions for Later

### Improvement 4 (Design-Partner Pilot)
- Who is the target design partner? Do you have any warm leads from your professional network?
- What is the acceptable timeline for a design-partner pilot (2 weeks? 4 weeks? 8 weeks?)?
- Would you accept a partner who only uses the Pilot layer, or must they also validate Operate?

### Improvement 10 (Design-Partner Agreement)
- Is the -50% design-partner discount in `PRICING_PHILOSOPHY.md` still the approved level?
- Do you have a legal template for design-partner agreements, or should I draft one?
- What IP/data ownership terms apply to design-partner pilot data?

### General Commercial Questions
- Have you identified any specific verticals or company sizes for initial market targeting?
- Is there a marketing launch date or event you're targeting for GA announcement?
- Do you have a board deck or investor presentation that this assessment should feed into?

### Security Questions
- Have you actually run OWASP ZAP strict mode against the staging environment? If so, are results available?
- Has the Aeronova engagement kickoff (2026-05-06) been confirmed?
- Do you have cyber insurance, and if so, what coverage levels?

### Data Residency
- What is the primary Azure region for the hosted SaaS production deployment?
- Do you intend to offer EU-region deployment as a standard option or only on enterprise request?
- Have any prospective buyers raised data residency requirements?

---

**End of assessment.**
