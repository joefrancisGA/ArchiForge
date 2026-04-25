# ArchLucid Assessment – Weighted Readiness 68.49%

**Date:** 2026-04-24
**Basis:** Independent first-principles review of repository contents (code, docs, tests, infrastructure, UI, go-to-market materials). No prior assessments referenced. Items explicitly deferred to V1.1 or V2 are treated as out of scope and do not reduce scores.

**Correction note (same session):** Initial scores improperly penalized 9 qualities for V1.1-deferred items (no live customers, no published case studies, no executed pen test, no live commerce). Corrected per the explicit rules in `V1_DEFERRED.md` §6b, which state that V1 quality assessments do not charge points against Marketability, Proof-of-ROI Readiness, Differentiability, Trustworthiness, Procurement Readiness, Adoption Friction, Decision Velocity, or Commercial Packaging Readiness for those items. Security also adjusted (pen test is V1.1 per §6c). Net impact: +3.14 percentage points (65.35% → 68.49%).

---

## 1. Executive Summary

### Overall Readiness

ArchLucid is a substantive, architecturally well-structured AI-assisted architecture workflow system with strong engineering fundamentals, thorough documentation, and clear product boundaries. The V1 scope is well-defined with honest deferrals. The product is **technically ready for a sales-led pilot** with a narrow buyer persona. The weighted readiness of **68.49%** reflects strong engineering depth and mature commercial packaging, offset by high cognitive load, limited real-LLM validation, and gaps in workflow embeddedness and usability that are within V1 scope to address.

### Commercial Picture

The product narrative is clear and well-articulated — the executive sponsor brief, positioning, and ROI model are mature. The three value pillars (AI-native analysis, auditable decision trail, enterprise governance) are concrete and defensible. The pricing model has three well-defined tiers with CI-enforced pricing single-source-of-truth and marketplace alignment. The trial funnel works in TEST mode on staging. The commercial motion is sales-led by design (commerce un-hold deferred to V1.1). Scoring the V1-scope commercial artifacts on their own merits — positioning, packaging, ROI methodology, executive messaging, order form, evidence pack — the commercial picture is solid for a product at this stage. The highest-weight gap is Time-to-Value, where the path from signup to first meaningful value still requires significant conceptual onboarding.

### Enterprise Picture

Enterprise governance, audit, and traceability are strong — 118 typed audit events, append-only enforcement, policy packs, approval workflows with segregation of duties. Trust center scaffolding is honest and structured with correct status labels. Workflow embeddedness is adequate for V1 (REST API, CLI, GitHub Actions, ADO, Teams, CloudEvents) but limited by the V1.1 deferral of first-party ITSM connectors. Usability is acceptable with progressive disclosure but cognitive load remains a concern — the large feature surface and 577+ docs create discovery overhead.

### Engineering Picture

Engineering is ArchLucid's strongest dimension. The codebase has 2000+ C# files across 52 well-bounded projects with clean layering. Test infrastructure is comprehensive: 200+ test classes, property-based tests (FsCheck), 38 golden corpus cases, integration tests against real SQL, Playwright E2E (mock + live API), chaos testing (Simmy), and architecture tests. Observability has 50+ custom OpenTelemetry instruments. Infrastructure-as-code covers 15 Terraform root modules. Security engineering is solid with STRIDE threat modeling, OWASP ZAP + Schemathesis in CI, RLS with SESSION_CONTEXT, LLM prompt redaction, and defense-in-depth patterns. The main engineering risks are: no production operational history, coordinator-to-authority migration still in progress, and real-LLM validation limited to opt-in paths.

---

## 2. Deferred Scope Uncertainty

The deferred items are well-documented and located. The following files clearly identify deferred scope:

- `docs/library/V1_DEFERRED.md` — Consolidated deferred inventory (located and read)
- `docs/library/V1_SCOPE.md` §3 — Explicit out-of-scope table (located and read)
- `docs/PENDING_QUESTIONS.md` — Owner-gated decisions (located and read)

All V1.1/V2 deferrals referenced in scoring (reference customer, commerce un-hold, pen test, PGP key, Jira/ServiceNow/Confluence/Slack connectors) have corresponding entries in these files. **No deferred-scope uncertainty exists.**

---

## 3. Weighted Quality Assessment

Qualities ordered from most urgent (highest weighted deficiency) to least urgent.

**Corrected urgency order (top 15 by weighted deficiency):**

| Rank | Quality | Score | Weight | Weighted Deficiency |
|------|---------|-------|--------|---------------------|
| 1 | Time-to-Value | 65 | 7 | 245 |
| 2 | Adoption Friction | 62 | 6 | 228 |
| 3 | Marketability | 72 | 8 | 224 |
| 4 | Proof-of-ROI Readiness | 70 | 5 | 150 |
| 5 | Correctness | 70 | 4 | 120 |
| 6 | Executive Value Visibility | 70 | 4 | 120 |
| 7 | Usability | 60 | 3 | 120 |
| 8 | Workflow Embeddedness | 62 | 3 | 114 |
| 9 | Differentiability | 74 | 4 | 104 |
| 10 | Trustworthiness | 70 | 3 | 90 |
| 11 | Compliance Readiness | 60 | 2 | 80 |
| 12 | Traceability | 73 | 3 | 81 |
| 13 | Security | 74 | 3 | 78 |
| 14 | Decision Velocity | 62 | 2 | 76 |
| 15 | Interoperability | 62 | 2 | 76 |

---

### Marketability — Score: 72 | Weight: 8 | Weighted Deficiency: 224

**Justification:** The product positioning is strong — three clear value pillars, competitive landscape doc, `/why` comparison page with sourced incumbent-aligned PDF bundle, elevator pitches at 30-second and 60-second lengths. The executive sponsor brief is mature, honest, and explicitly scopes what not to over-claim. The marketing site has structured routes (`/pricing`, `/get-started`, `/why`, `/trust`, `/demo/explain`). Demo preview endpoint serves pre-composed JSON for instant evaluation. Board-pack PDF endpoint provides leadership reporting. The product category ("AI Architecture Intelligence") is clearly defined with proof points mapped to shipped V1 capabilities. Reference customer and live commerce are V1.1 items and are not scored here.

**Tradeoffs:** The marketing asset portfolio is strong for a V1 product. The `/why` comparison page and evidence pack ZIP show procurement awareness. The main V1-scope gap is the absence of a short-form demo video and a structured sales playbook for founder-led conversations.

**Improvement Recommendations:** Create a structured competitive proof document with verifiable feature comparisons. Develop a "first 5 conversations" playbook for founder-led sales. Produce a short-form product demo video script.

**Fixability:** V1 (marketing materials and playbook are fully actionable).

---

### Adoption Friction — Score: 62 | Weight: 6 | Weighted Deficiency: 228

**Justification:** SaaS delivery eliminates infrastructure installation for buyers — they never touch Docker, SQL, or .NET. The `archlucid try` one-command demo exists for contributors. The 4-step core pilot is clear. The trial funnel works in TEST mode on staging for sales-engineer-led evaluation. The buyer-facing first-30-minutes path is documented with a marketing route. But the doc surface is vast (577+ markdown files) — discovering the right document requires understanding a complex hierarchy. Configuration has many options across `appsettings.json`. Progressive disclosure helps manage feature discovery, but the underlying surface area creates cognitive overhead. Live self-serve checkout is V1.1 and is not scored here.

**Tradeoffs:** Feature richness creates adoption friction. The progressive disclosure system mitigates this well. The SaaS model eliminates the worst friction (infrastructure) but shifts it to conceptual onboarding. The V1-scope gap is doc discoverability and in-product guidance, not commerce.

**Improvement Recommendations:** Create a "docs navigator" — a single interactive page that asks 3 questions and links directly to the right doc. Reduce the number of docs visible from any entry point. Add in-product contextual help tooltips for complex concepts.

**Fixability:** V1 (docs and UI changes are fully actionable).

---

### Time-to-Value — Score: 65 | Weight: 7 | Weighted Deficiency: 245

**Justification:** The `archlucid try` command delivers a committed manifest in ~60 seconds with zero configuration. The core pilot is 4 clear steps. `SECOND_RUN.toml` enables rapid second-run with real data. The buyer-facing first-30-minutes path exists (stub + marketing route). The demo preview endpoint serves pre-composed JSON for instant evaluation. But for a real customer, the path from signup to first real value requires understanding architecture requests, run lifecycle, and manifest semantics. The SaaS trial funnel works in TEST mode on staging but isn't serving real evaluators.

**Tradeoffs:** Speed to first demo value is excellent. Speed to first *meaningful* value (a committed manifest from the customer's own architecture) requires architectural literacy. This is inherent to the domain, not a product deficiency.

**Improvement Recommendations:** Harden the trial funnel end-to-end observability. Add a "first real value" tracking metric that measures wall-clock time from signup to first committed manifest. Create a guided in-product walkthrough.

**Fixability:** V1 (observable and automatable).

---

### Proof-of-ROI Readiness — Score: 70 | Weight: 5 | Weighted Deficiency: 150

**Justification:** The ROI model is well-constructed with clear measurement methodology (before/after baselines, 6 concrete metrics, scoring table). The worked example ROI artifact is reproducible from the Contoso demo with a published PDF and inline metrics mirror. The value report DOCX generator automates sponsor reporting with annualized projections including architect-hour savings, LLM cost window, and net annualized value. The per-tenant cost model exists. The pilot success scorecard maps directly to the core pilot deliverable. The sponsor-level value story is simple, honest, and defensible ("we got from request to reviewable output faster"). Customer-validated ROI data requires a reference customer, which is a V1.1 item and is not scored here.

**Tradeoffs:** The measurement infrastructure is complete — when a real pilot runs, data collection will be automatic. The ROI model uses conservative estimates (break-even at ~180 architect-hours/year). The V1-scope gap is the absence of a lean scorecard that operators can fill without the full model.

**Improvement Recommendations:** Add a "pilot health dashboard" that surfaces time-to-committed-manifest trends per tenant. Create a lean pilot success scorecard that doesn't require the full ROI model. These are V1-scope improvements to the measurement tooling, independent of customer data.

**Fixability:** V1 (dashboard and scorecard are fully actionable).

---

### Differentiability — Score: 74 | Weight: 4 | Weighted Deficiency: 104

**Justification:** The multi-agent pipeline + governance + explainability traces + provenance graph + 118-event audit trail is genuinely differentiated from both manual architecture review and generic AI chat tools. The competitive landscape document exists with sourced comparisons. The `/why` comparison page provides a structured side-by-side with PDF download and CI sync guard. The positioning statement is clear with three concrete value pillars mapped to shipped capabilities. The "AI Architecture Intelligence" category is well-defined with proof points (10 finding engine types, multi-vendor LLM support, quality gates, deterministic simulator mode). Market-level validation (analyst recognition, customer stories) requires reference customers, which is a V1.1 item and is not scored here.

**Tradeoffs:** The product is genuinely differentiated at the technical and product level. The V1-scope gap is in articulating specific scenarios where the structured pipeline produces materially different outcomes vs. ad-hoc approaches — moving from "we're different" to "here's exactly how the output differs."

**Improvement Recommendations:** Create a "why not X" comparison for the 3 most likely alternative approaches (manual review boards, general-purpose AI assistants, architecture documentation tools). Document specific scenarios where ArchLucid's structured pipeline produces materially different outcomes than ad-hoc approaches.

**Fixability:** V1 (documentation and positioning are actionable).

---

### Correctness — Score: 70 | Weight: 4 | Weighted Deficiency: 120

**Justification:** Testing infrastructure is strong: 200+ test classes, property-based tests (FsCheck for invariant checking), 38 golden corpus cases (30 decisioning + 5 ingestion + 3 synthesis), determinism check service, schema validation, agent output quality gates (structural completeness + semantic scoring). Simulator mode enables deterministic testing. Golden cohort lock-baseline captures manifest fingerprints for regression detection. But real-LLM validation is limited — the golden cohort real-LLM gate requires owner budget approval and Azure OpenAI deployment. The `--strict-real` mode exists but is opt-in. The quality gate on agent output is configurable but thresholds are not published.

**Tradeoffs:** Deterministic testing via simulator is excellent for regression but doesn't validate real AI output quality. The dual-mode approach (simulator for CI, real for validation) is correct architecture but real-mode coverage is thin.

**Improvement Recommendations:** Harden the golden cohort real-LLM automation. Add deterministic assertions on real-LLM output structure (even if content varies). Publish quality gate thresholds in configuration documentation.

**Fixability:** V1 (automation is actionable; some aspects require AOAI deployment).

---

### Executive Value Visibility — Score: 70 | Weight: 4 | Weighted Deficiency: 120

**Justification:** The executive sponsor brief is mature, honest, and well-structured (what to claim, what not to over-claim, what success should allow a sponsor to say). The sponsor PDF with first-commit badge is a good artifact. The email-run-to-sponsor banner provides in-product sponsor communication. The `/pricing` page exists. The board-pack PDF endpoint provides leadership reporting. The why-ArchLucid comparison supports procurement conversations. The value report DOCX automates sponsor ROI reporting.

**Tradeoffs:** Executive visibility artifacts are strong for a pre-revenue product. The main gap is that all evidence is self-generated, not customer-sourced.

**Improvement Recommendations:** Add a "sponsor dashboard" view that aggregates executive-relevant metrics across runs. Create a one-page sponsor leave-behind template (separate from the full brief).

**Fixability:** V1 (UI and doc changes).

---

### Usability — Score: 60 | Weight: 3 | Weighted Deficiency: 120

**Justification:** The operator UI has thoughtful progressive disclosure (essential/extended/advanced tiers). The seven-step wizard for new runs guides input. The pipeline timeline provides execution visibility. Sidebar navigation is role-aware. But the feature surface is large — governance, alerts, policy packs, advisory, graph, replay, compare, audit, digests, compliance drift — creating discovery overhead even with progressive disclosure. The operator atlas maps every route but reading it is itself a cognitive task. axe-core accessibility enforcement in CI is good. But no user testing or usability studies documented.

**Tradeoffs:** Feature completeness vs. usability simplicity. The progressive disclosure system is the right architecture for managing this tension, but the underlying surface area is large for a first-time user.

**Improvement Recommendations:** Add contextual help or guided discovery to the operator UI. Create a "what to look at next" recommendation based on the user's current state (e.g., "you have a committed manifest — try Compare or Export next"). Reduce the number of visible options at any decision point.

**Fixability:** V1 (UI changes are actionable).

---

### Workflow Embeddedness — Score: 62 | Weight: 3 | Weighted Deficiency: 114

**Justification:** V1 ships with meaningful integration points: REST API (OpenAPI 3.0), .NET API client (NSwag), CLI, GitHub Actions manifest delta, Azure DevOps pipeline task + PR decoration, Microsoft Teams notifications (Logic Apps), CloudEvents webhooks, Azure Service Bus, AsyncAPI spec. SCIM 2.0 provisioning for identity. Integration recipes for Jira and ServiceNow as customer-operated bridges. But first-party ITSM connectors are V1.1. No architecture import connectors (Structurizr, ArchiMate, Terraform state) are shipped. No IDE integration (VS Code extension).

**Tradeoffs:** The webhook + REST API foundation enables customer-built integrations for any target system. First-party connectors reduce friction but increase maintenance surface. The V1.1 deferral of ITSM connectors is reasonable given the current engineering bandwidth.

**Improvement Recommendations:** Create detailed integration recipe documentation for the 3 most common workflow patterns (PR review gate, governance notification, architecture import from existing tools). Add a CloudEvents consumer example that demonstrates end-to-end event handling.

**Fixability:** V1 (documentation and examples are actionable; first-party connectors are V1.1).

---

### Trustworthiness — Score: 70 | Weight: 3 | Weighted Deficiency: 90

**Justification:** The trust center is well-structured with honest status labels (self-asserted, V1.1-scheduled, engagement in flight). The evidence pack ZIP provides one-click procurement artifact download with ETag caching. CAIQ Lite and SIG Core pre-fills demonstrate security questionnaire readiness. The STRIDE threat model covers the full product boundary including RAG-specific threats. RLS risk acceptance is documented. Owner-conducted security self-assessment exists. AI output is honestly positioned as "decision support, not legal attestation" with citation links to persisted artifacts. Compliance matrix, managed identity patterns, and tenant isolation documentation are thorough. Pen test execution, SOC 2 attestation, and reference customer publication are V1.1 items and are not scored here.

**Tradeoffs:** The trust posture is honest and well-documented for what V1 delivers. The "self-asserted" labeling builds credibility by not over-claiming. The V1-scope gap is in surfacing CI security evidence (ZAP results, Schemathesis output) on the trust center rather than just linking to process docs.

**Improvement Recommendations:** Add OWASP ZAP scan result summaries to the trust center. Create a "what we test in CI" security posture summary. These surface existing evidence more effectively.

**Fixability:** V1 (documentation surfacing is actionable).

---

### Security — Score: 74 | Weight: 3 | Weighted Deficiency: 78

**Justification:** Security engineering is solid. STRIDE threat model covers the full system boundary plus RAG-specific threats. OWASP ZAP baseline + Schemathesis run in CI. Gitleaks pre-receive prevents secret commits. LLM prompt redaction with deny-list is enabled by default in shipped appsettings. RLS with SESSION_CONTEXT for tenant isolation with risk acceptance documented. Managed identity patterns documented. Private endpoint Terraform modules. Development bypass has explicit guardrails (non-production only, startup warnings). Billing webhook verification (Stripe HMAC + Marketplace JWT) with idempotency keys. Trial auth with lockout + role gates. Append-only audit with database-level DENY UPDATE/DELETE. Log sanitization (CWE-117). CORS deny-by-default. Rate limiting across 3 policies. Pen test execution, SOC 2 attestation, and PGP key are V1.1 items and are not scored here.

**Tradeoffs:** Defense-in-depth is well-implemented across the full stack. The V1-scope gap is in systematic authorization boundary testing — verifying that RBAC policies enforce correctly across all endpoints.

**Improvement Recommendations:** Add security-focused integration tests that specifically test authorization boundaries (ensure Reader cannot commit, ensure tenant isolation prevents cross-tenant data access). Document the CI security gate inventory (what runs, what it catches, what it doesn't).

**Fixability:** V1 (test expansion and documentation are actionable).

---

### Traceability — Score: 73 | Weight: 3 | Weighted Deficiency: 81

**Justification:** Traceability is a product strength. Every finding carries a 5-field ExplainabilityTrace. The provenance graph connects evidence → decisions → manifest entries → artifacts. Citation links in aggregate explanations allow reviewers to trace claims to sources. Decision traces persist the full reasoning chain. Agent execution traces capture prompts and responses for forensics. Explanation faithfulness checking (token overlap heuristic) with metric emission provides automated quality signals. The `RunRationale` captures run-level reasoning.

**Tradeoffs:** Strong structured traceability is the key differentiator. The faithfulness heuristic is imperfect but transparent.

**Improvement Recommendations:** Add a "trace completeness report" per run that surfaces which findings have strong vs. weak traces. Create a trace visualization in the operator UI that shows the full provenance chain for a single finding.

**Fixability:** V1 (UI and report generation changes).

---

### Compliance Readiness — Score: 60 | Weight: 2 | Weighted Deficiency: 80

**Justification:** SOC 2 self-assessment with Common Criteria mapping exists. CAIQ Lite (CSA) and SIG Core (Shared Assessments) pre-fills are available. Compliance matrix document exists. OWASP ZAP baseline in CI. Compliance drift trend tracking in the operator UI. But no formal attestation (SOC 2 Type I/II, ISO 27001) exists or is in progress for V1. Pen test deferred to V1.1. The self-assessment is labeled as "not a substitute for a CPA SOC 2 report."

**Tradeoffs:** Self-assessment infrastructure is the right pre-revenue investment. Formal compliance programs require recurring spend and auditor relationships that are premature for a product without paying customers.

**Improvement Recommendations:** Create a SOC 2 Type I readiness gap analysis that identifies the top 5 control gaps between current state and a Type I engagement. This doesn't require an auditor — just an honest self-assessment against the Trust Services Criteria.

**Fixability:** V1 (gap analysis is actionable; actual engagement is V1.1+).

---

### Decision Velocity — Score: 62 | Weight: 2 | Weighted Deficiency: 76

**Justification:** The pre-commit governance gate enables automated go/no-go decisions based on finding severity thresholds with warning-only mode. Approval workflows with SLA tracking enforce timely review. Webhook escalation on SLA breach pushes decisions forward. The governance dashboard surfaces pending approvals and policy change summary. Policy packs define decision criteria with versioning and scope assignments. Effective governance resolution computes the active policy for a given scope. Live commerce features are V1.1 and are not scored here. The V1-scope gap is configuration complexity: setting up governance workflows requires understanding policy packs, approval chains, severity thresholds, and SLA parameters.

**Tradeoffs:** Configurable governance is powerful but complex. Most V1 pilots should use the pre-commit gate only (simple on/off with severity threshold).

**Improvement Recommendations:** Create a "governance quickstart" that shows the minimum viable governance configuration (just the pre-commit gate with a single severity threshold). Add configuration validation that warns when governance is misconfigured.

**Fixability:** V1 (documentation and validation are actionable).

---

### Interoperability — Score: 62 | Weight: 2 | Weighted Deficiency: 76

**Justification:** OpenAPI 3.0 contract with versioned routes. .NET API client generated from NSwag. AsyncAPI spec for event contracts. CloudEvents envelope format for webhooks and Service Bus. CLI for scripting. SCIM 2.0 for identity provisioning. But no architecture import connectors (Structurizr DSL, ArchiMate XML, Terraform state parsing) are shipped. SIEM export in CEF/syslog format is planned but not available. No SDK for non-.NET languages.

**Tradeoffs:** The OpenAPI + CloudEvents foundation enables any integration, but customers must build the connector themselves for non-shipped targets. First-party import connectors would reduce time-to-value for customers with existing architecture artifacts.

**Improvement Recommendations:** Create a Python SDK (or OpenAPI client generation recipe) for non-.NET consumers. Add a Terraform state import example using the existing `TerraformShowJsonInfrastructureDeclarationParser`.

**Fixability:** V1 (partial — SDK generation recipe is actionable; full SDKs are larger scope).

---

### Procurement Readiness — Score: 72 | Weight: 2 | Weighted Deficiency: 56

**Justification:** DPA template exists. Subprocessors register is published. Evidence pack ZIP with ETag caching is available via anonymous API endpoint (no email gate). Order form template with pricing tiers. CAIQ Lite and SIG Core pre-fills for security questionnaires. Trust center with honest status labels and last-reviewed dates. Quote request endpoint for pre-checkout engagement. SOC 2 self-assessment with Common Criteria mapping. Compliance matrix. "How to request the procurement pack" guide. SOC 2 procurement status doc explicitly states "not yet issued." Reference customer, executed pen test, and SOC 2 attestation are V1.1 items and are not scored here.

**Tradeoffs:** The procurement artifact portfolio is comprehensive for V1 scope. The honest "self-asserted" labeling and explicit "not yet issued" posture build credibility. The V1-scope gap is a procurement FAQ that pre-answers common questions.

**Improvement Recommendations:** Create a "procurement FAQ" that pre-answers the 10 most common security questionnaire questions with links to evidence. Add a timeline estimate for SOC 2 Type I to the Trust Center.

**Fixability:** V1 (documentation is actionable).

---

### Architectural Integrity — Score: 76 | Weight: 3 | Weighted Deficiency: 72

**Justification:** 52 well-bounded C# projects with clean layering: Core (no external dependencies), Contracts (DTOs), Application (orchestration), Api (HTTP surface), Persistence (Dapper + DbUp), plus domain-specific projects (Decisioning, AgentRuntime, KnowledgeGraph, Provenance, ArtifactSynthesis, ContextIngestion). 33+ ADRs documenting architectural decisions. Contracts.Abstractions separated for minimal dependency surface. Host.Composition + Host.Core provide clean DI composition. The coordinator-to-authority pipeline migration is well-documented with a strangler pattern (ADR 0021/0029/0030). Architecture tests enforce project reference discipline. The authority pipeline unification is in progress but well-tracked.

**Tradeoffs:** The architecture is over-modularized for the current team size (52 projects for what appears to be a solo/small team), but this pays dividends in testability and boundary enforcement. The coordinator-to-authority migration adds transient complexity.

**Improvement Recommendations:** Complete the coordinator-to-authority migration (ADR 0030 PRs A0-A4) to reduce the dual-pipeline surface area. Add an architecture fitness function test that verifies no new coordinator-only endpoints are introduced.

**Fixability:** V1 (migration is already tracked and partially complete).

---

### Commercial Packaging Readiness — Score: 74 | Weight: 2 | Weighted Deficiency: 52

**Justification:** Three tiers defined (Team $436/mo, Professional $2331/mo, Enterprise custom) with clear feature gates per tier. Run allowances with overage pricing. Order form template. Marketplace alignment CI guard (`assert_marketplace_pricing_alignment.py`). Pricing single-source-of-truth with CI enforcement (`check_pricing_single_source.py`). Quote request endpoint for pre-checkout engagement. Billing wiring complete (`BillingStripeWebhookController`, `BillingMarketplaceWebhookController`, `BillingCheckoutController`, `BillingProductionSafetyRules`). The `[RequiresCommercialTenantTier]` filter returns 402 Payment Required. Trial signup TEST-mode plumbing is in place. The $436/mo Team entry point is within the discretionary budget guardrail. Conversion data and live keys are V1.1 items and are not scored here.

**Tradeoffs:** The packaging infrastructure is complete and CI-enforced. The V1-scope gap is minimal — the billing wiring, pricing, feature gates, and order form are all ready for the V1.1 commerce un-hold.

**Improvement Recommendations:** No code changes needed — the packaging infrastructure is complete for V1 scope.

**Fixability:** N/A (V1 scope is complete for packaging infrastructure).

---

### Reliability — Score: 66 | Weight: 2 | Weighted Deficiency: 68

**Justification:** Circuit breakers (Polly) with configurable options and monitoring. Simmy chaos testing with quarterly game day cadence. Health checks (live/ready) with dependency awareness. Data consistency probes with orphan detection and quarantine. Idempotency keys on create run. Transactional outbox for event processing. Retry policies for durable audit and LLM calls. Hot-path cache with write-through invalidation. But no production operational history. No SLA evidence. Chaos testing is staging-only. RTO/RPO targets are documented but not validated.

**Tradeoffs:** The reliability engineering infrastructure is strong for a product without production traffic. The chaos testing framework (Simmy) is particularly forward-thinking. But reliability is ultimately proven in production.

**Improvement Recommendations:** Run a structured reliability drill: boot the full stack, inject failures via Simmy, verify health check transitions and circuit breaker behavior. Document the drill results as evidence.

**Fixability:** V1 (drill execution is actionable).

---

### Azure Compatibility and SaaS Deployment Readiness — Score: 68 | Weight: 2 | Weighted Deficiency: 64

**Justification:** 15 Terraform root modules covering: core infrastructure, SQL failover, Container Apps (with secondary region), Front Door + WAF, Key Vault, Service Bus, APIM, Logic Apps, OTel Collector, Entra ID + External ID, storage, monitoring (App Insights + Prometheus SLO rules + Grafana), pilot, private networking, edge/marketing routes. Consumption budgets on Container Apps and SQL failover. `apply-saas.ps1` orchestrates multi-root deployment. Reference SaaS stack order documented. But the hosted SaaS is not fully operational — staging exists but production DNS and Front Door custom domains are not wired. No production deployment has been executed.

**Tradeoffs:** The IaC investment is substantial and well-structured. The gap is operational — the infrastructure code exists but hasn't been applied to a production environment.

**Improvement Recommendations:** Create a `terraform plan` validation script that verifies all 15 roots plan cleanly against a minimal variable set. Add a Terraform pre-commit hook that runs `terraform validate` on changed modules.

**Fixability:** V1 (validation automation is actionable; production deployment is operational).

---

### Data Consistency — Score: 70 | Weight: 2 | Weighted Deficiency: 60

**Justification:** Explicit data consistency matrix documenting every aggregate's consistency model. Orphan detection probe with quarantine capability. Optimistic concurrency via ROWVERSION. Transactional outbox with enqueue tied to commit. Read-replica staleness expectations documented. Archival cascades with same-transaction child archival. Hot-path cache invalidation on documented write paths. Run authority convergence completed (ADR 0012). Read-replica routing with configurable fallback.

**Tradeoffs:** The data consistency documentation is unusually thorough for a product at this stage. The orphan probe + quarantine pattern is defensive and well-designed.

**Improvement Recommendations:** Add a data consistency integration test that verifies orphan probe detection and quarantine for all configured table pairs. Create a "data consistency runbook" for operators that covers the most common drift scenarios.

**Fixability:** V1 (tests and documentation are actionable).

---

### Policy and Governance Alignment — Score: 72 | Weight: 2 | Weighted Deficiency: 56

**Justification:** Policy packs with versioning and scope assignments. Effective governance resolution that computes the active policy for a given scope. Pre-commit governance gate with configurable severity thresholds and warning-only mode. Approval workflows with segregation of duties (self-approval blocked). SLA tracking with deadline calculation and webhook escalation on breach. Governance dashboard surfaces pending approvals and policy change summary. Governance resolution API. Five vertical starter policy packs.

**Tradeoffs:** The governance system is comprehensive for a V1 product. Most enterprises will start with the pre-commit gate only and adopt policy packs later.

**Improvement Recommendations:** Add a governance configuration validator that checks for common misconfigurations (e.g., SLA window too short, no escalation webhook configured, approval chain with a single approver).

**Fixability:** V1 (validation logic is actionable).

---

### Maintainability — Score: 72 | Weight: 2 | Weighted Deficiency: 56

**Justification:** CI guards enforce doc-code synchronization (audit event count, pricing single-source, marketplace alignment, contributor page line cap, docs root file count, buyer-first-30-minutes sync). Consistent project structure across 52 projects. Coding rules defined and applied (cursor rules for C# style). Module boundaries enforced by architecture tests. Well-documented migration strategy (DbUp). Breaking changes tracked separately. Changelog maintained.

**Tradeoffs:** The CI guard approach to doc-code sync is effective but adds maintenance cost when adding new constants or documents. The 52-project structure is maintainable but requires discipline to avoid project proliferation.

**Improvement Recommendations:** Add a "project dependency diagram" auto-generated from `.csproj` references that is checked into docs and refreshed by CI. This aids new contributors in understanding the codebase.

**Fixability:** V1 (tooling is actionable).

---

### AI/Agent Readiness — Score: 72 | Weight: 2 | Weighted Deficiency: 56

**Justification:** Multi-agent pipeline with 4 specialized agents (Topology, Cost, Compliance, Critic) plus 10 finding engine types. Agent output quality scoring (structural completeness + semantic quality) with configurable quality gates. Fallback completion client for LLM provider resilience. LLM cost estimation. Simulator mode for deterministic testing without LLM costs. `--strict-real` mode for CI. Multi-vendor LLM support. Agent execution trace recording for forensics. Prompt redaction for PII/secrets.

**Tradeoffs:** The dual-mode agent architecture (simulator + real) is the correct design for balancing test determinism with real AI validation. The quality gate infrastructure is forward-thinking but thresholds need real-world calibration.

**Improvement Recommendations:** Publish the quality gate threshold configuration with documentation on how to tune it. Add agent output quality trend reporting across runs.

**Fixability:** V1 (configuration documentation and reporting are actionable).

---

### Explainability — Score: 73 | Weight: 2 | Weighted Deficiency: 54

**Justification:** ExplainabilityTrace with 5 structured fields on every finding. Trace completeness measured by OTel metric. Provenance graph with nodes, edges, and graph algorithms. Citation references in aggregate explanations with UI chips. Explanation faithfulness checking (token overlap heuristic with aggregate fallback). Agent execution trace persistence (prompt/response in blob storage). Run rationale captures run-level reasoning. Decision trace entry for per-decision reasoning.

**Tradeoffs:** The explainability infrastructure is comprehensive. The faithfulness heuristic is transparent about its limitations (token overlap, not semantic understanding). The fallback from LLM narrative to deterministic manifest text when faithfulness is low is honest engineering.

**Improvement Recommendations:** Add a "low faithfulness" alert that notifies operators when aggregate explanations repeatedly fall back to deterministic text (could indicate prompt or model degradation).

**Fixability:** V1 (alerting is actionable).

---

### Cognitive Load — Score: 48 | Weight: 1 | Weighted Deficiency: 52

**Justification:** 577+ markdown files across docs, library, security, go-to-market, engineering, integrations, runbooks, ADRs, and archive. Even the README is 386 lines with nested details sections. The operator UI has progressive disclosure but surfaces governance, alerts, policy packs, advisory, graph, replay, compare, audit, digests, compliance drift, knowledge graph, provenance, and more. The doc navigation requires understanding the hub-and-spoke model (READ_THIS_FIRST → START_HERE → spine → library). Five different contributor personas have different entry points. The architecture has 52 C# projects. Configuration has dozens of options across multiple `appsettings` sections.

**Tradeoffs:** Cognitive load is the tax on completeness. The product is genuinely feature-rich, and the documentation is genuinely thorough. Reducing cognitive load without reducing capability requires better information architecture and in-context guidance, not feature removal.

**Improvement Recommendations:** Create a "zero-scroll" single-page contributor index (100 lines max, already CI-guarded). Add an interactive doc finder. Reduce the README to a minimal surface with expandable sections. Add in-product "learn more" links that point to specific doc sections rather than entire documents.

**Fixability:** V1 (documentation restructuring and UI guidance are actionable).

---

### Auditability — Score: 76 | Weight: 2 | Weighted Deficiency: 48

**Justification:** 118 typed audit event constants in a single catalog with CI guard. Append-only SQL with database-level DENY UPDATE/DELETE on `dbo.AuditEvents`. Dual audit channels (durable SQL + baseline mutation log). CSV export with UTC range and row cap. Keyset pagination for large audit logs. Correlation ID threading. Retention tiering (hot/warm/cold) with operator-scheduled archival. Filtered search with EventType, ActorUserId, RunId, CorrelationId. Governance dual-write (baseline + durable).

**Tradeoffs:** The audit system is enterprise-grade. The CI guard on constant count prevents silent drift. The append-only database enforcement is the right pattern for audit integrity.

**Improvement Recommendations:** Add an audit completeness integration test that verifies every mutating API endpoint emits at least one audit event. This closes the last coverage gap.

**Fixability:** V1 (test expansion is actionable).

---

### Customer Self-Sufficiency — Score: 58 | Weight: 1 | Weighted Deficiency: 42

**Justification:** CLI `doctor` and `health` commands. Support bundle with auto-redaction. Troubleshooting doc with FAQs. Pilot guide with step-by-step instructions. Operator quickstart with copy-paste commands. Version endpoint for support attribution. But no in-product help system, no chatbot, no knowledge base searchable from within the product. Support bundle download is behind the `/admin/support` page (requires ExecuteAuthority).

**Tradeoffs:** The support tooling is solid for a pre-revenue product. In-product help systems are expensive to build and maintain.

**Improvement Recommendations:** Add a "common issues" panel to the operator UI that surfaces the top 5 troubleshooting steps based on the current page context.

**Fixability:** V1 (UI addition is actionable).

---

### Availability — Score: 60 | Weight: 1 | Weighted Deficiency: 40

**Justification:** Health endpoints (live/ready/full). RTO/RPO targets documented. Geo-failover drill runbook. SQL failover group Terraform. Secondary region Container Apps. But no production uptime history. No SLA evidence. Failover drills documented but results not published.

**Tradeoffs:** Availability infrastructure is in place but unproven.

**Improvement Recommendations:** Run a structured failover drill and document results. Publish RTO/RPO targets on the Trust Center.

**Fixability:** V1 (drill execution is actionable).

---

### Accessibility — Score: 62 | Weight: 1 | Weighted Deficiency: 38

**Justification:** axe-core Playwright enforcement for critical/serious WCAG 2.1 AA violations in CI. Component-level axe in Vitest. Marketing public route coverage. Pages covered include runs, manifests, compare, replay, ask, advisory, graph, audit, policy packs, alerts, governance, onboarding, trial signup. But VPAT publication deferred. No user testing with assistive technology. No keyboard navigation testing beyond axe checks.

**Tradeoffs:** Automated accessibility checking catches the most common violations but does not validate the full assistive technology experience.

**Improvement Recommendations:** Add keyboard navigation E2E tests for the core pilot flow (create run, view status, commit, review artifacts).

**Fixability:** V1 (test expansion is actionable).

---

### Template and Accelerator Richness — Score: 62 | Weight: 1 | Weighted Deficiency: 38

**Justification:** 5 vertical starter templates (public sector EU, public sector US, healthcare, financial services, and more in `templates/briefs/`). Finding engine template for custom engines. Policy packs per vertical. Architecture request wizard presets. `SECOND_RUN.toml` template for quick follow-on runs. Integration recipes (Jira, ServiceNow bridges). But templates are starter-level, not deeply customizable. No template marketplace or community contribution model.

**Tradeoffs:** Starter templates lower time-to-first-run. Deep customization is premature for V1.

**Improvement Recommendations:** Add a "template gallery" page to the operator UI that shows available templates with descriptions and use cases.

**Fixability:** V1 (UI addition is actionable).

---

### Scalability — Score: 62 | Weight: 1 | Weighted Deficiency: 38

**Justification:** Multi-tenant design. Container Apps with auto-scaling. Service Bus for async work decoupling. Read-replica routing for read-heavy workloads. Rate limiting (3 policies). Fair-use soft cap at 2000 runs/month on Enterprise tier. But no load test results published. The `tests/load/` directory exists with a README but no published baselines. No documented throughput limits.

**Tradeoffs:** The architecture supports scaling, but scaling claims are untested.

**Improvement Recommendations:** Run a basic load test (e.g., 50 concurrent runs in simulator mode) and publish the results as a baseline. Document the expected throughput limits per deployment size.

**Fixability:** V1 (load testing is actionable).

---

### Cost-Effectiveness — Score: 62 | Weight: 1 | Weighted Deficiency: 38

**Justification:** Per-tenant cost model documented. Pilot deployment profile with resource sizing. LLM cost estimation with per-call tracking. Golden cohort budget controls ($50/month with warn at 80% / kill at 95%). Consumption budget Terraform on Container Apps and SQL failover. Cost preview endpoint (`GET /v1/agent-execution/cost-preview`). But no production cost data. No unit economics validated with real traffic.

**Tradeoffs:** Cost planning infrastructure is strong. Actual cost optimization requires production traffic patterns.

**Improvement Recommendations:** No immediate code changes — the cost infrastructure is adequate. When production traffic begins, add a cost-per-run trend dashboard.

**Fixability:** V1 (monitoring infrastructure is in place).

---

### Performance — Score: 64 | Weight: 1 | Weighted Deficiency: 36

**Justification:** Hot-path read cache (memory or Redis) with TTL and write-through invalidation. Read-replica routing for read-heavy workloads. Rate limiting across 3 policies. Benchmarks project (`ArchLucid.Benchmarks`) exists. Authority pipeline stage duration histogram. But no published performance baselines. No response time targets documented. No performance regression tests in CI.

**Tradeoffs:** Performance infrastructure exists but benchmarks are not published or enforced.

**Improvement Recommendations:** Add a performance baseline test that asserts the core pilot flow (create → execute → commit → retrieve) completes within a target time in simulator mode.

**Fixability:** V1 (baseline test is actionable).

---

### Extensibility — Score: 66 | Weight: 1 | Weighted Deficiency: 34

**Justification:** Finding engine template for custom engines. Custom policy packs. Webhook/CloudEvents integration points. AsyncAPI contract for event consumers. Plugin architecture is implicit (DI-based) rather than explicit. No extension marketplace. No public extension API beyond the REST API.

**Tradeoffs:** DI-based extensibility is adequate for V1. An explicit plugin model would be premature.

**Improvement Recommendations:** Document the "how to add a finding engine" process as a contributor guide, using the existing template as the example.

**Fixability:** V1 (documentation is actionable).

---

### Stickiness — Score: 66 | Weight: 1 | Weighted Deficiency: 34

**Justification:** Manifests accumulate value over time (comparison history, drift detection). Governance records create compliance dependencies. Knowledge graph grows with each run. 118-event audit trail is hard to replicate. Integration events create ecosystem ties. Value reports track ROI over time. But stickiness requires usage, which requires customers.

**Tradeoffs:** The data model creates natural switching costs. This is a strength that compounds with usage.

**Improvement Recommendations:** Add a "tenant health" metric that tracks data accumulation (manifests committed, audit events generated, governance decisions made) to demonstrate stickiness during pilots.

**Fixability:** V1 (metric addition is actionable).

---

### Change Impact Clarity — Score: 67 | Weight: 1 | Weighted Deficiency: 33

**Justification:** Comparison replay with structural golden-manifest deltas. Manifest diff service. Changelog with user-visible changes. Breaking changes documented separately. Before/after delta component in UI. ADRs for architectural decisions. Version endpoint for support attribution.

**Tradeoffs:** Change tracking is well-implemented at the product level. The changelog discipline is good.

**Improvement Recommendations:** No immediate changes needed — change impact clarity is adequate for V1.

**Fixability:** N/A (adequate).

---

### Manageability — Score: 67 | Weight: 1 | Weighted Deficiency: 33

**Justification:** Feature flags for gradual rollout. Operator atlas mapping routes to API/CLI/authority. Configuration documentation across multiple guides. Staged configuration overlays (base + development + user secrets). Admin APIs for management operations. Scope debug controller for troubleshooting. Support bundle with configuration snapshot.

**Tradeoffs:** Configuration surface is large but well-documented. The operator atlas is a strong management aid.

**Improvement Recommendations:** Add a configuration validation endpoint that reports misconfigured or conflicting settings at startup.

**Fixability:** V1 (validation endpoint is actionable).

---

### Deployability — Score: 68 | Weight: 1 | Weighted Deficiency: 32

**Justification:** Docker compose profiles (dev, full-stack, real-aoai). 15 Terraform root modules. DbUp auto-migration on startup. Container images with Dockerfiles. Release scripts (build, package, readiness check, release smoke). devcontainer for zero-setup contributor onboarding. GitHub Actions CI/CD. `apply-saas.ps1` for multi-root Terraform deployment.

**Tradeoffs:** The deployment tooling is comprehensive. The main gap is that production deployment hasn't been validated end-to-end.

**Improvement Recommendations:** Create a "deployment checklist" that can be run against any target environment (local, staging, production) with pass/fail assertions.

**Fixability:** V1 (checklist automation is actionable).

---

### Evolvability — Score: 70 | Weight: 1 | Weighted Deficiency: 30

**Justification:** 33+ ADRs documenting architectural decisions with status tracking. Explicit deferrals with rationale. Versioned API with deprecation headers and sunset dates. Strangler pattern for coordinator-to-authority migration. Changelog and breaking changes doc. Feature flags for gradual feature introduction. V1/V1.1/V2 release-window planning.

**Tradeoffs:** The evolvability infrastructure is mature. The ADR discipline and deprecation header approach demonstrate long-term thinking.

**Improvement Recommendations:** No immediate changes needed — evolvability is well-addressed.

**Fixability:** N/A (adequate).

---

### Supportability — Score: 72 | Weight: 1 | Weighted Deficiency: 28

**Justification:** Support bundle (`archlucid support-bundle --zip`) with auto-redaction of secrets. Correlation IDs across requests. CLI diagnostics (`doctor`, `health`). Problem details with support hints in API responses. Version endpoint. Troubleshooting doc. Pilot guide with issue reporting instructions (version, correlation ID, logs, bundle).

**Tradeoffs:** Supportability tooling is strong for a V1 product.

**Improvement Recommendations:** No immediate changes needed.

**Fixability:** N/A (adequate).

---

### Observability — Score: 74 | Weight: 1 | Weighted Deficiency: 26

**Justification:** 50+ custom OpenTelemetry instruments including business KPIs (runs volume, findings mix, LLM batch intensity, explanation cache effectiveness). Application Insights integration. Prometheus SLO rules Terraform. Grafana dashboard Terraform. First-tenant funnel dashboard module. Golden cohort cost dashboard module. Authority pipeline stage duration histogram. Circuit breaker state transition counters. Agent output quality histograms.

**Tradeoffs:** Observability infrastructure is comprehensive. The business KPI metrics are forward-thinking.

**Improvement Recommendations:** No immediate changes needed — observability is a strength.

**Fixability:** N/A (adequate).

---

### Modularity — Score: 74 | Weight: 1 | Weighted Deficiency: 26

**Justification:** 52 projects with clean boundaries. Core has no external dependencies. Contracts.Abstractions separated for minimal dependency surface. Host.Composition and Host.Core provide clean DI composition. Architecture tests enforce project reference discipline. Finding engine template demonstrates plugin-style modularity.

**Tradeoffs:** Potentially over-modularized for current team size, but maintainability benefits are real.

**Improvement Recommendations:** No immediate changes needed.

**Fixability:** N/A (adequate).

---

### Azure Ecosystem Fit — Score: 74 | Weight: 1 | Weighted Deficiency: 26

**Justification:** Entra ID + External ID for identity. Azure SQL with failover groups. Blob Storage with private endpoints. Service Bus for messaging. Key Vault for secrets. Container Apps for hosting. Front Door + WAF for edge. APIM for API management. Logic Apps Standard for workflow (Teams fan-out). Application Insights + Azure Monitor for observability. Azure OpenAI for LLM. Managed identity patterns documented. ADR 0020 declares Azure as the permanent primary platform.

**Tradeoffs:** Deep Azure alignment is the right choice for the target market. It may limit non-Azure adoption, but the product is explicitly positioned as Azure-native.

**Improvement Recommendations:** No immediate changes needed — Azure ecosystem fit is a strength.

**Fixability:** N/A (adequate).

---

### Testability — Score: 78 | Weight: 1 | Weighted Deficiency: 22

**Justification:** 200+ test classes across 17 test projects. Property-based tests (FsCheck) for invariant checking. 38 golden corpus cases (30 decisioning, 5 ingestion, 3 synthesis). Integration tests against real SQL Server. Contract tests for repository interfaces. E2E Playwright (mock + live API). Simmy chaos tests. Architecture tests for project reference discipline. Load test scaffolding. API greenfield boot tests. Coverage settings files. Test tier system (Core/Fast Core/Integration/Slow/Full).

**Tradeoffs:** Test infrastructure is a genuine strength. The multi-tier test system enables fast CI feedback with thorough regression coverage.

**Improvement Recommendations:** No immediate changes needed — testability is a top strength.

**Fixability:** N/A (adequate).

---

### Documentation — Score: 80 | Weight: 1 | Weighted Deficiency: 20

**Justification:** 577+ markdown files covering: architecture (C4 poster, containers, components, flows), API contracts, CLI usage, operator atlas, security (STRIDE, threat models, RLS, compliance), go-to-market (positioning, pricing, ROI), engineering (install order, first 30 minutes, build, test structure), integrations (Teams, GitHub Actions, ADO, SCIM), runbooks (agent failures, data archival, coordinator parity, infrastructure ops, trial funnel), ADRs (33+). Doc inventory with last-modified metadata. Library reorganization with CI-enforced root file cap.

**Tradeoffs:** Documentation is extremely thorough — possibly over-documented for the current product stage. This creates cognitive load (scored separately) but ensures that knowledge is captured. The recent library reorganization helped by reducing the docs root from ~200 to ~20 active files.

**Improvement Recommendations:** No immediate changes to documentation volume — the thoroughness is an asset. Focus cognitive load improvements on navigation and discoverability rather than content reduction.

**Fixability:** N/A (documentation is a strength).

---

## 4. Top 10 Most Important Weaknesses

*Note: Items explicitly deferred to V1.1 or V2 (reference customer, commerce un-hold, pen test, PGP key, ITSM connectors) are out of scope and are not listed as weaknesses.*

1. **Cognitive overload for new users** — 577+ docs, 52 projects, dozens of configuration options, and a rich feature surface create significant mental overhead. Progressive disclosure mitigates but doesn't eliminate this. Even doc navigation requires understanding a hub-and-spoke model. This affects Adoption Friction, Usability, Time-to-Value, and Customer Self-Sufficiency.

2. **Time-to-first-meaningful-value requires conceptual onboarding** — The `archlucid try` demo delivers a manifest in 60 seconds, but getting a customer from signup to their first *meaningful* committed manifest (using their own architecture) requires understanding architecture requests, run lifecycle, and manifest semantics. This is inherent to the domain but could be reduced with better in-product guidance. Affects Time-to-Value, Adoption Friction.

3. **Real-LLM output quality is unvalidated at scale** — The simulator-based testing is excellent for regression, but real AI output quality (topology analysis accuracy, cost estimation precision, compliance gap detection completeness) has limited validation. Quality gates exist but thresholds are uncalibrated. This affects Correctness, AI/Agent Readiness.

4. **No production operational history** — Availability, reliability, and performance claims are based on infrastructure and architecture, not operational evidence. No SLA data. No incident history. No failover drill results published. This constrains Availability, Reliability, Performance.

5. **Complex governance configuration** — The governance system (policy packs, approval chains, pre-commit gates, SLA tracking, alert rules) is powerful but complex to configure correctly. This adds friction for pilot operators who want simple governance without reading multiple docs. Affects Decision Velocity, Usability.

6. **No in-product contextual help** — Users discovering features must leave the UI to read documentation. No tooltips, guided walkthroughs, or contextual "learn more" links exist in the operator UI. This increases time-to-value for every new feature a user encounters. Affects Usability, Customer Self-Sufficiency, Time-to-Value.

7. **Coordinator-to-Authority migration still in progress** — The dual pipeline (ADR 0021/0029/0030) adds transient complexity. PRs A0-A4 are in progress with a 2026-05-15 sunset date. Until complete, the codebase carries two parallel run-commit paths. Affects Architectural Integrity, Maintainability, Correctness.

8. **Limited architecture import capability** — No connectors for importing existing architecture artifacts (Structurizr DSL, ArchiMate XML, Terraform state) are shipped. Customers with existing architecture documentation must manually describe their systems. Affects Workflow Embeddedness, Interoperability, Adoption Friction.

9. **Performance baselines not published or enforced** — No response time targets are documented. No performance regression tests in CI. The benchmarks project exists but baselines are not published. Affects Performance, Reliability.

10. **Single-developer bus factor** — The commit history and documentation patterns suggest a single primary contributor. The 52-project architecture and 577+ docs create significant key-person risk. Knowledge transfer to additional contributors would be a significant effort. Affects Maintainability, Evolvability.

---

## 5. Top 5 Monetization Blockers

*Note: Reference customer, live commerce un-hold, and pen test publication are V1.1 items and are not listed here.*

1. **Conceptual onboarding barrier for first-time evaluators.** The product's value proposition requires understanding architecture requests, run lifecycle, manifests, and findings — concepts that don't exist in most evaluators' current workflow. Without in-product guided discovery, the time from "interested" to "convinced" is longer than it should be.

2. **Sales-led motion without sales tooling.** The commercial motion is sales-led (by design for V1), but there's no CRM integration, no lead scoring, no demo booking flow, no sales playbook beyond the executive sponsor brief. The quote request endpoint exists but forward-routing is undefined.

3. **No short-form demo artifact.** There is no product demo video, interactive demo, or self-paced walkthrough that a prospect can consume without scheduling a call. The demo preview endpoint serves JSON, not a visual experience. Every evaluation requires founder time.

4. **ROI measurement methodology is untested in practice.** The measurement infrastructure is complete (value report DOCX, pilot success scorecard, before/after baselines), but the methodology has not been tested with a real pilot operator. The "fill in your before/after numbers" workflow may be more friction than expected.

5. **Privacy notice finalization is owner-blocked.** Enterprise procurement in GDPR jurisdictions will gate on a finalized privacy notice. The current draft status means some procurement conversations cannot proceed to contract stage until this is resolved.

---

## 6. Top 5 Enterprise Adoption Blockers

*Note: SOC 2 attestation, executed pen test, and ITSM connectors are V1.1 items and are not listed as V1 adoption blockers.*

1. **Privacy notice is still draft.** Enterprise procurement teams, especially in EU/GDPR jurisdictions, will review the privacy notice before signing. The current draft status and owner-blocked finalization means the privacy posture is incomplete for formal procurement.

2. **No in-product guided onboarding for enterprise operators.** Enterprise operators evaluating ArchLucid for their team need to understand the product quickly. The current onboarding relies on external documentation. An in-product tour or guided wizard walkthrough would reduce time-to-evaluation for procurement-stage demos.

3. **Residual RLS coverage gaps.** RLS with SESSION_CONTEXT is implemented and threat-modeled, but the residual uncovered tables documented in `MULTI_TENANT_RLS.md` §9 are a known gap. Enterprise security teams doing a deep review will flag these as incomplete isolation boundaries.

4. **Complex configuration surface for pilot setup.** Enterprise operators setting up a pilot must navigate auth modes, storage providers, agent execution modes, governance settings, and integration options. The configuration documentation is thorough but spread across multiple files. A single "enterprise pilot configuration guide" would reduce implementation friction.

5. **No WCAG VPAT published.** Enterprise procurement teams with accessibility requirements (particularly US federal and regulated industries) will ask for a Voluntary Product Accessibility Template. The automated axe-core checks exist, but the formal VPAT document does not.

---

## 7. Top 5 Engineering Risks

1. **Coordinator-to-Authority migration incomplete.** The strangler pattern (ADR 0021/0029/0030) is well-tracked but PRs A0-A4 are in progress. The dual pipeline adds complexity, test burden, and potential for divergent behavior. The 2026-05-15 sunset date creates pressure. If the migration stalls, the codebase carries permanent technical debt in the form of two parallel run-commit paths.

2. **Real-LLM output quality is a latent correctness risk.** The product's value proposition depends on AI agents producing relevant, accurate architecture analysis. The simulator mode ensures deterministic testing but doesn't validate real output quality. The quality gate thresholds are uncalibrated against real-world architecture diversity. A false sense of correctness could emerge from excellent simulator tests.

3. **Single-developer bus factor.** The commit history, decision patterns, and documentation style suggest a single primary contributor. The 52-project architecture, 577+ docs, and extensive feature surface create significant key-person risk. Knowledge transfer to additional contributors would be a significant effort despite thorough documentation.

4. **Database schema evolution under production traffic.** DbUp migrations run automatically on startup, which is convenient for development but risky under production traffic. Migrations 001-050 are historical; new migrations must be backward-compatible during rolling deployments. No blue-green or canary deployment pattern is documented for the API + migration combination.

5. **LLM provider dependency and cost uncertainty.** The product depends on Azure OpenAI for real-mode operation. Token costs are estimated but not validated at scale. The cost-per-run varies with architecture complexity, prompt length, and model selection. A customer with complex architectures could see unexpectedly high LLM costs. The consumption budget controls ($50/month golden cohort cap) are defensive but production-scale costs are unknown.

---

## 8. Most Important Truth

**ArchLucid's V1 engineering and product packaging are mature; the highest-leverage V1 improvements are usability and cognitive load reduction, not more features.** The codebase, documentation, governance system, audit trail, security posture, and commercial packaging are substantively complete for V1 scope. The remaining V1-scope gaps are not about missing capabilities — they are about making existing capabilities easier to discover, understand, and use. The cognitive load imposed by 577+ docs, 52 projects, and a rich feature surface is the single largest drag on readiness. Every improvement that reduces the number of decisions a new user must make — contextual help, doc navigation, guided onboarding, governance quickstart — will move the needle more than any new feature.

---

## 9. Top Improvement Opportunities

### Improvement 1: Cognitive Load Reduction — Interactive Doc Navigator

**Title:** Cognitive Load Reduction — Interactive Doc Navigator

**Why it matters:** 577+ docs create significant discovery overhead. The hub-and-spoke navigation model (READ_THIS_FIRST → START_HERE → spine → library) requires understanding the model before using it. New evaluators, contributors, and operators spend time finding the right doc instead of doing their work. This drags Adoption Friction, Usability, Time-to-Value, and Cognitive Load.

**Expected impact:** Directly improves Cognitive Load (+8-12 pts), Adoption Friction (+3-5 pts), Usability (+2-4 pts), Time-to-Value (+1-3 pts). Weighted readiness impact: +0.3-0.6%.

**Affected qualities:** Cognitive Load, Adoption Friction, Usability, Time-to-Value, Customer Self-Sufficiency.

**Status:** Fully actionable now.

**Cursor prompt:**

```
Create a single-page interactive doc navigator at `docs/NAVIGATOR.md` that replaces the current multi-hop doc discovery model with a direct Q&A format.

Requirements:
1. Create `docs/NAVIGATOR.md` with exactly this structure:
   - A heading "ArchLucid Doc Navigator"
   - A table with three columns: "I want to..." | "Start here" | "Time"
   - Rows covering the 15 most common tasks (create a run, configure governance, set up auth, run tests, deploy to Azure, review security posture, understand architecture, configure alerts, export artifacts, compare runs, set up integrations, troubleshoot issues, understand pricing, read the API contract, prepare for procurement)
   - Each row links to ONE document (not a chain)
   - Time column shows estimated read time in minutes

2. Update `docs/READ_THIS_FIRST.md`:
   - Add a line after the first heading: "**Quick lookup:** [Doc Navigator](NAVIGATOR.md) — find the right doc in one click."

3. Update `docs/START_HERE.md`:
   - Add a "Quick lookup" link to NAVIGATOR.md in the Assumptions section

4. Add a CI guard `scripts/ci/assert_navigator_links_valid.py`:
   - Parse every link in NAVIGATOR.md
   - Verify each linked file exists
   - Fail CI if any link is broken
   - Register in `.github/workflows/ci.yml` as a merge-blocking step

Constraints:
- NAVIGATOR.md must be under 80 lines
- Every link must be a relative path to an existing file
- Do not reorganize any existing docs — this is additive
- Do not change any existing doc content beyond the two small additions above

Acceptance criteria:
- `docs/NAVIGATOR.md` exists with 15 rows covering the common tasks listed above
- Each row links to exactly one existing doc
- CI guard passes (all links valid)
- READ_THIS_FIRST.md and START_HERE.md both reference the navigator
```

---

### Improvement 2: Trial Funnel Observability and Error Resilience

**Title:** Trial Funnel Observability and Error Resilience

**Why it matters:** The self-serve trial funnel is the product's future PLG engine. It works in TEST mode on staging but lacks production-grade observability and error handling. When the commerce un-hold happens (V1.1), the funnel must be reliable from day one. Hardening it now reduces V1.1 risk and improves the staging evaluation experience for sales-engineer-led demos.

**Expected impact:** Directly improves Adoption Friction (+3-5 pts), Time-to-Value (+2-3 pts), Reliability (+2-3 pts), Marketability (+1-2 pts). Weighted readiness impact: +0.4-0.7%.

**Affected qualities:** Adoption Friction, Time-to-Value, Reliability, Marketability, Commercial Packaging Readiness.

**Status:** Fully actionable now.

**Cursor prompt:**

```
Harden the trial funnel observability and error resilience for the self-serve trial signup flow.

Context: The trial funnel is wired in code (`BillingCheckoutController`, `RegistrationController`, trial signup page) and works in TEST mode on staging. This improvement adds observability, error handling, and resilience — NOT live keys or production deployment.

Requirements:

1. Add a `TrialFunnelHealthProbe` hosted service in `ArchLucid.Api`:
   - Periodically (every 5 minutes when `Demo:Enabled=true`) verifies the demo preview endpoint responds with 200
   - Emits `archlucid_trial_funnel_health_probe_total` counter with labels `outcome` (success/failure)
   - Logs a warning when the probe fails 3 consecutive times
   - Register in `Program.cs` only when `Demo:Enabled` is true

2. Add structured error handling to `RegistrationController`:
   - If registration fails, return a `ProblemDetails` response with a correlation ID and a user-friendly message
   - Emit `archlucid_trial_registration_failures_total` counter with label `reason` (validation/conflict/internal)
   - Ensure all failure paths emit a durable audit event (`TrialRegistrationFailed`)

3. Add the `TrialRegistrationFailed` constant to `AuditEventTypes.cs`:
   - Update the const count comment in `AUDIT_COVERAGE_MATRIX.md`
   - Add a row to the durable audit table in `AUDIT_COVERAGE_MATRIX.md`

4. Add unit tests:
   - `TrialFunnelHealthProbeTests` — probe success/failure scenarios
   - Registration failure audit event emission test
   - Add `[Trait("Suite", "Core")]` to new tests

Constraints:
- Do not modify the Stripe or Marketplace webhook controllers
- Do not add any new configuration keys beyond `Demo:Enabled` (already exists)
- Do not change the registration API contract (request/response shapes)
- Do not touch `appsettings.json` defaults

Acceptance criteria:
- Health probe runs when Demo is enabled, emits metrics
- Registration failures emit audit events and structured ProblemDetails
- All new code has unit tests with Suite=Core trait
- AUDIT_COVERAGE_MATRIX.md count comment updated
- `dotnet test --filter "Suite=Core"` passes
```

---

### Improvement 3: Golden Corpus Real-LLM Validation Gate Hardening

**Title:** Golden Corpus Real-LLM Validation Gate Hardening

**Why it matters:** The product's correctness depends on real AI output quality, which the simulator cannot validate. The golden cohort real-LLM gate exists but is opt-in and requires owner provisioning. Hardening the automation ensures that when the AOAI deployment is provisioned, the validation gate works reliably. This also adds structural assertions that work even with variable LLM content.

**Expected impact:** Directly improves Correctness (+5-8 pts), AI/Agent Readiness (+3-5 pts), Trustworthiness (+2-3 pts). Weighted readiness impact: +0.5-0.8%.

**Affected qualities:** Correctness, AI/Agent Readiness, Trustworthiness, Reliability.

**Status:** Fully actionable now (automation hardening; AOAI provisioning remains owner-only).

**Cursor prompt:**

```
Harden the golden corpus real-LLM validation gate so that when the Azure OpenAI deployment is provisioned, the automation is reliable and produces actionable results.

Context: `golden-cohort-nightly.yml` has a placeholder `cohort-real-llm-gate` job. `archlucid golden-cohort lock-baseline` captures SHA-256 fingerprints. The budget ($50/month, warn 80%, kill 95%) is approved. What's missing is structural validation of real-LLM output that doesn't depend on content matching.

Requirements:

1. Create `ArchLucid.Core.Tests/GoldenCorpus/RealLlmOutputStructuralValidator.cs`:
   - Static method `ValidateAgentResultStructure(string agentType, string resultJson)` → `ValidationResult`
   - Checks: JSON is valid, required top-level keys present (per agent type), finding array is non-empty, each finding has ExplainabilityTrace fields
   - Returns structured result with pass/fail per check and descriptive messages
   - Does NOT check content — only structure

2. Create `ArchLucid.Core.Tests/GoldenCorpus/RealLlmOutputStructuralValidatorTests.cs`:
   - Test each agent type (Topology, Cost, Compliance, Critic) with valid and invalid JSON
   - Test missing required keys, empty findings, missing ExplainabilityTrace
   - `[Trait("Suite", "Core")]`

3. Update `ArchLucid.Cli/Commands/GoldenCohortDriftCommand.cs`:
   - When `--strict-real` is passed and results are from real-LLM execution, run `RealLlmOutputStructuralValidator` on each agent result
   - If any structural validation fails, exit with non-zero code and emit the validation result as JSON to stdout
   - Add `--structural-only` flag that skips SHA comparison and only runs structural validation

4. Add documentation section to `docs/runbooks/GOLDEN_COHORT_REAL_LLM_GATE.md`:
   - Section "Structural validation" explaining what is checked and why
   - Section "Interpreting failures" with examples of common structural issues
   - Link to the validator source

Constraints:
- Do not modify the golden cohort budget controls
- Do not provision Azure OpenAI resources
- Do not change the SHA-256 fingerprint comparison logic
- Do not change the nightly workflow YAML (that requires owner enabling)

Acceptance criteria:
- `RealLlmOutputStructuralValidator` validates all 4 agent types
- Tests cover valid, invalid, and edge cases for each agent type
- CLI `golden-cohort drift --strict-real --structural-only` runs structural validation
- Runbook documents the new structural validation
- `dotnet test --filter "Suite=Core"` passes
```

---

### Improvement 4: Operator UI Contextual Help System

**Title:** Operator UI Contextual Help System

**Why it matters:** The operator UI has many features behind progressive disclosure, but users discovering a feature for the first time have no in-context guidance. They must leave the UI to read docs. Adding contextual help tooltips and "learn more" links reduces cognitive load, improves usability, and decreases time-to-value for the core pilot flow.

**Expected impact:** Directly improves Usability (+5-7 pts), Cognitive Load (+4-6 pts), Time-to-Value (+2-3 pts), Customer Self-Sufficiency (+3-5 pts). Weighted readiness impact: +0.4-0.7%.

**Affected qualities:** Usability, Cognitive Load, Time-to-Value, Customer Self-Sufficiency, Adoption Friction.

**Status:** Fully actionable now.

**Cursor prompt:**

```
Add a contextual help system to the ArchLucid operator UI for the core pilot flow pages.

Context: The operator UI (`archlucid-ui/`) uses Next.js with progressive disclosure. Users discovering features need in-context guidance without leaving the UI. This adds help tooltips and "learn more" links for the 5 most important pages in the core pilot flow.

Requirements:

1. Create `archlucid-ui/src/components/ContextualHelp.tsx`:
   - A small `(?)` icon component that shows a tooltip on hover/click
   - Props: `helpKey: string` (maps to help content), `placement?: 'top' | 'right' | 'bottom' | 'left'`
   - Tooltip shows a short paragraph (max 2 sentences) + optional "Learn more →" link
   - Accessible: `role="tooltip"`, `aria-describedby`, keyboard accessible (Enter/Space to toggle)
   - Styled consistently with the existing design system

2. Create `archlucid-ui/src/lib/contextual-help-content.ts`:
   - Export a `Record<string, { text: string; learnMoreUrl?: string }>` with help content for these keys:
     - `new-run-wizard` — "Create an architecture request that describes the system you want ArchLucid to analyze."
     - `run-pipeline-status` — "The pipeline shows each AI agent's progress. When all steps complete, the run is ready to commit."
     - `commit-manifest` — "Committing produces a versioned golden manifest and synthesizes artifacts. This is the primary pilot deliverable."
     - `manifest-review` — "Review the manifest's decisions, findings, and structured metadata. Download artifacts for offline review."
     - `governance-gate` — "When enabled, the governance gate checks findings against severity thresholds before allowing commit."
   - `learnMoreUrl` values should be relative paths like `/docs/CORE_PILOT.md#step-1` (rendered as GitHub links in the SaaS version)

3. Add `<ContextualHelp>` to these pages:
   - `archlucid-ui/src/app/(operator)/runs/new/` — next to the wizard title
   - Run detail page — next to the pipeline timeline heading
   - Commit button area — next to the commit action
   - Manifest review section — next to the artifacts table heading
   - Governance gate section (if visible) — next to the gate status

4. Create `archlucid-ui/src/components/ContextualHelp.test.tsx`:
   - Renders tooltip on click
   - Renders learn-more link when provided
   - Accessible: has correct ARIA attributes
   - Keyboard accessible

5. Create `archlucid-ui/src/lib/contextual-help-content.test.ts`:
   - All help keys have non-empty text
   - Text is under 200 characters
   - learnMoreUrl values (when present) start with `/`

Constraints:
- Do not modify existing component props or API
- Use existing design system colors and typography
- Do not add any new npm dependencies
- Tooltip must be dismissable by clicking outside or pressing Escape
- Do not add help content for Operate-layer features (only core pilot flow)

Acceptance criteria:
- ContextualHelp component renders correctly with all props
- Help content covers the 5 core pilot flow touchpoints
- All tests pass (`npm test`)
- axe accessibility checks pass on the ContextualHelp component
- No visual regression on existing pages (help icons are small and unobtrusive)
```

---

### Improvement 5: Integration Recipe Expansion for Workflow Embeddedness

**Title:** Integration Recipe Expansion for Workflow Embeddedness

**Why it matters:** Enterprise architects need ArchLucid to fit into their existing workflows. V1 ships webhooks and REST API, but customers need concrete examples of the 3 most common integration patterns. Detailed recipes reduce the implementation burden for customers who need ITSM integration before the V1.1 first-party connectors arrive.

**Expected impact:** Directly improves Workflow Embeddedness (+4-6 pts), Interoperability (+3-4 pts), Adoption Friction (+2-3 pts). Weighted readiness impact: +0.3-0.5%.

**Affected qualities:** Workflow Embeddedness, Interoperability, Adoption Friction, Customer Self-Sufficiency.

**Status:** Fully actionable now.

**Cursor prompt:**

```
Expand the integration recipe library with 3 detailed, copy-paste-ready integration recipes for the most common enterprise workflow patterns.

Context: `templates/integrations/` has Jira and ServiceNow bridge recipes. These are outlines. This improvement adds complete, tested recipes with code samples that customers can deploy immediately.

Requirements:

1. Create `templates/integrations/pr-review-gate/README.md`:
   - Title: "Architecture Review Gate for Pull Requests"
   - Pattern: GitHub Actions / ADO Pipeline triggers ArchLucid run on PR, posts findings as PR comment, blocks merge if critical findings exist
   - Include: complete GitHub Actions workflow YAML, complete ADO Pipeline YAML
   - Include: shell script that calls the ArchLucid API (create run, wait for completion, check findings, post comment)
   - Include: configuration table (API URL, API key env var, severity threshold)
   - Include: troubleshooting section (common errors, correlation ID for debugging)
   - Reference: existing `integrations/github-action-manifest-delta/` for the delta posting pattern

2. Create `templates/integrations/governance-notification/README.md`:
   - Title: "Governance Notification Pipeline"
   - Pattern: CloudEvents webhook receives `com.archlucid.governance.approval.submitted` → routes to Teams/email/Slack (via webhook bridge)
   - Include: Azure Logic App template JSON (Standard SKU)
   - Include: Teams Adaptive Card JSON template showing approval details
   - Include: email template (HTML) with approval details and action link
   - Include: webhook receiver validation (HMAC signature check)
   - Reference: existing `templates/integrations/jira/jira-webhook-bridge-recipe.md` for the HMAC pattern

3. Create `templates/integrations/architecture-import/README.md`:
   - Title: "Import Existing Architecture into ArchLucid"
   - Pattern: Convert Terraform state / ARM template / manual description into ArchLucid architecture request
   - Include: PowerShell script that runs `terraform show -json` and transforms output into ArchLucid request body
   - Include: ARM template export + transformation script
   - Include: manual CSV-to-brief template for organizations without IaC
   - Reference: existing `ArchLucid.ContextIngestion/Infrastructure/TerraformShowJsonInfrastructureDeclarationParser.cs`

4. Add a `templates/integrations/README.md` index:
   - Table listing all integration recipes with description, complexity, and time estimate
   - Include both new and existing recipes

Constraints:
- All code samples must be copy-paste ready (no placeholder tokens that require understanding the codebase)
- Use `ARCHLUCID_API_URL` and `ARCHLUCID_API_KEY` as the standard env var names
- Do not create any new C# code — recipes use the existing REST API
- Recipes must work with the shipped V1 API surface (no unreleased endpoints)
- Each recipe must include a "verify it works" section with a curl command

Acceptance criteria:
- 3 new recipe directories under `templates/integrations/`
- Each recipe has a complete README.md with working code samples
- Index README.md lists all integration recipes
- All curl commands reference real V1 API endpoints
- HMAC validation pattern is consistent across recipes
```

---

### Improvement 6: Security Authorization Boundary Tests

**Title:** Security Authorization Boundary Tests

**Why it matters:** The RBAC system (Admin/Operator/Reader/Auditor) protects mutating operations. But authorization boundary testing — verifying that a Reader cannot commit a run, a non-admin cannot seed demo data, and tenant isolation prevents cross-tenant access — is implicit rather than systematically asserted. Adding explicit boundary tests strengthens Security, Correctness, and Trustworthiness.

**Expected impact:** Directly improves Security (+4-6 pts), Correctness (+2-3 pts), Trustworthiness (+2-3 pts). Weighted readiness impact: +0.3-0.5%.

**Affected qualities:** Security, Correctness, Trustworthiness, Auditability.

**Status:** Fully actionable now.

**Cursor prompt:**

```
Add systematic authorization boundary integration tests that verify RBAC and tenant isolation enforcement.

Context: `ArchLucid.Api.Tests` uses `ArchLucidApiFactory` with `WebApplicationFactory`. Auth modes are configurable. The API has policies: ReadAuthority (Reader+), ExecuteAuthority (Operator+), AdminAuthority (Admin only). Tenant isolation uses RLS with SESSION_CONTEXT.

Requirements:

1. Create `ArchLucid.Api.Tests/Security/AuthorizationBoundaryTests.cs`:
   - Test class with `[Trait("Suite", "Core")]` and `[Trait("Category", "Integration")]`
   - Configure the API factory with ApiKey auth mode enabled
   - Tests:
     a. Reader key cannot POST to `/v1/architecture/request` (expects 403)
     b. Reader key cannot POST to `/v1/architecture/run/{runId}/commit` (expects 403)
     c. Reader key cannot POST to `/v1/demo/seed` (expects 403)
     d. Reader key CAN GET `/v1/architecture/run/{runId}` (expects 200 or 404, not 403)
     e. Non-admin key cannot access admin-only endpoints (expects 403)
     f. No API key returns 401 on protected endpoints
     g. Valid API key on health endpoints returns 200 (health is not protected)

2. Create `ArchLucid.Api.Tests/Security/TenantIsolationSmokeTests.cs`:
   - Test class with `[Trait("Suite", "Core")]` and `[Trait("Category", "Integration")]`
   - Tests (when using Sql storage and RLS is configured):
     a. Create a run with tenant A scope
     b. Query runs with tenant B scope
     c. Verify tenant B cannot see tenant A's run
     d. Verify tenant A can see its own run

3. Add documentation `docs/security/AUTHORIZATION_BOUNDARY_TEST_INVENTORY.md`:
   - Table listing each authorization boundary test with endpoint, expected behavior, and test class
   - Link to the test files

Constraints:
- Use the existing `ArchLucidApiFactory` pattern from `ArchLucid.Api.Tests`
- Do not modify any controller authorization attributes
- Do not add new auth modes or keys
- Tests must work with the in-memory storage provider for fast CI execution
- Tenant isolation tests may be skipped (`[SkipIfNoSql]`) when SQL is not available

Acceptance criteria:
- All authorization boundary tests pass with `dotnet test --filter "Suite=Core&Category=Integration"`
- Reader cannot write, non-admin cannot admin, unauthenticated gets 401
- Tenant isolation smoke passes when SQL + RLS is configured
- Documentation lists all boundary tests
- No existing tests broken
```

---

### Improvement 7: Production SaaS Validation Script Suite

**Title:** Production SaaS Validation Script Suite

**Why it matters:** The Terraform modules, Container Apps configs, and deployment scripts exist but have never been validated against a production-like environment. A validation script suite that runs `terraform plan` on all roots, verifies deployment prerequisites, and checks configuration consistency reduces the risk of V1.1 deployment failures and improves Azure SaaS Deployment Readiness.

**Expected impact:** Directly improves Azure Compatibility and SaaS Deployment Readiness (+4-6 pts), Deployability (+3-4 pts), Reliability (+2-3 pts). Weighted readiness impact: +0.3-0.5%.

**Affected qualities:** Azure Compatibility and SaaS Deployment Readiness, Deployability, Reliability, Availability.

**Status:** Fully actionable now.

**Cursor prompt:**

```
Create a production SaaS validation script suite that verifies all Terraform roots plan cleanly and deployment prerequisites are met.

Context: `infra/` has 15 Terraform root modules. `apply-saas.ps1` orchestrates deployment. No systematic validation exists that all roots are internally consistent without requiring actual Azure credentials.

Requirements:

1. Create `scripts/validate-saas-infra.ps1`:
   - For each Terraform root in `infra/terraform-*` and `infra/modules/*`:
     a. Run `terraform init -backend=false` (skip remote state)
     b. Run `terraform validate`
     c. Capture pass/fail per root
   - Print a summary table: root name | init result | validate result
   - Exit with non-zero if any root fails validation
   - Add a `-Root` parameter to validate a single root (for development)

2. Create `scripts/validate-saas-config-consistency.ps1`:
   - Verify that all Terraform roots reference the same provider versions (compare `versions.tf` across roots)
   - Verify that variable names used across roots are consistently typed
   - Verify that `infra/apply-saas.ps1` references all roots that exist
   - Print warnings for any inconsistencies
   - Exit with non-zero if critical inconsistencies found

3. Create `scripts/ci/assert_terraform_roots_valid.py`:
   - CI guard that runs the validation and fails the merge if any root is invalid
   - Register in `.github/workflows/ci.yml` as a merge-blocking step (requires terraform CLI)
   - Add a `needs: [build]` dependency so it runs after the build job

4. Add documentation `docs/engineering/SAAS_INFRA_VALIDATION.md`:
   - Explain what the validation checks
   - How to run locally
   - How to add a new Terraform root and have it automatically included

Constraints:
- Do not require Azure credentials — use `terraform validate` only (no plan/apply)
- Do not modify any existing Terraform files
- PowerShell scripts must work on both Windows and Linux (pwsh)
- CI job may be `continue-on-error: true` initially until all roots are verified clean

Acceptance criteria:
- `scripts/validate-saas-infra.ps1` validates all 15+ Terraform roots
- Config consistency script identifies provider version mismatches
- CI guard registered and runs (even if continue-on-error initially)
- Documentation explains the validation approach
- All scripts run without Azure credentials
```

---

### Improvement 8: Performance Baseline Test for Core Pilot Flow

**Title:** Performance Baseline Test for Core Pilot Flow

**Why it matters:** No performance baselines are published or enforced. The core pilot flow (create → execute → commit → retrieve) has no response time targets. Adding a baseline test creates a regression gate and provides evidence for performance claims. This improves Performance, Correctness, and Reliability.

**Expected impact:** Directly improves Performance (+5-8 pts), Correctness (+2-3 pts), Reliability (+2-3 pts), Scalability (+2-3 pts). Weighted readiness impact: +0.2-0.4%.

**Affected qualities:** Performance, Correctness, Reliability, Scalability.

**Status:** Fully actionable now.

**Cursor prompt:**

```
Add a performance baseline test for the core pilot flow that asserts end-to-end timing and creates a published baseline.

Context: `ArchLucid.Benchmarks` project exists but no published baselines. The core pilot flow (create run → execute → commit → retrieve manifest) is the critical path. Simulator mode provides deterministic timing.

Requirements:

1. Create `ArchLucid.Api.Tests/Performance/CorePilotFlowPerformanceTests.cs`:
   - Test class with `[Trait("Suite", "Core")]` and `[Trait("Category", "Slow")]`
   - Uses `ArchLucidApiFactory` with Simulator execution mode and InMemory storage
   - Test `CorePilotFlow_CompletesWithinTarget`:
     a. Start stopwatch
     b. POST `/v1/architecture/request` — create a run
     c. POST `/v1/architecture/run/{runId}/seed-fake-results` — seed results (dev mode)
     d. POST `/v1/architecture/run/{runId}/commit` — commit
     e. GET `/v1/architecture/manifest/{version}` — retrieve
     f. Stop stopwatch
     g. Assert total time < 10 seconds (generous for in-process simulator with in-memory storage)
     h. Log individual step timings to test output
   - Test `ManifestRetrieval_CompletesWithin500ms`:
     a. After a committed run exists
     b. GET manifest 10 times
     c. Assert p95 < 500ms

2. Create `docs/library/PERFORMANCE_BASELINES.md`:
   - Table: operation | target | measured (placeholder until CI populates) | environment
   - Rows: create run, seed results, commit, retrieve manifest, manifest p95
   - Note: baselines are in-process simulator + in-memory — not representative of production SQL

3. Update `docs/library/TEST_STRUCTURE.md`:
   - Add a row for "Performance baseline" tier under the existing tier table
   - Reference the new test class

Constraints:
- Use in-memory storage and simulator mode — no SQL dependency
- Timing targets must be generous (this is a regression gate, not a benchmark)
- Do not add BenchmarkDotNet or other benchmarking frameworks — use simple Stopwatch
- Do not modify any existing controller or service code
- Tests should be idempotent and not depend on execution order

Acceptance criteria:
- Performance test passes with `dotnet test --filter "Category=Slow"`
- Core pilot flow completes in under 10 seconds (in-memory + simulator)
- Manifest retrieval p95 under 500ms
- PERFORMANCE_BASELINES.md documents the targets
- TEST_STRUCTURE.md references the new tier
```

---

### DEFERRED: Improvement 9 — Real Customer ROI Validation Instrumentation

**Title:** DEFERRED: Real Customer ROI Validation Instrumentation

**Reason deferred:** No real customer or pilot exists to generate ROI data. The instrumentation can be built, but there is no data to validate until a real architecture team uses the product on a real project.

**Information needed from owner:** (a) Identity of the first pilot customer or design partner. (b) Whether the pilot will use real-LLM mode or simulator. (c) Whether the customer consents to anonymized ROI data collection. (d) What "architect-hours saved" measurement methodology the customer will use (self-report, time tracking, or estimation).

---

### DEFERRED: Improvement 10 — Privacy Notice Finalization

**Title:** DEFERRED: Privacy Notice Finalization

**Reason deferred:** The privacy notice content is a legal/compliance owner-only item. The assistant can scaffold drafts but cannot make binding legal commitments. The privacy notice at `archlucid-ui/src/app/(marketing)/privacy/page.tsx` and `docs/security/PRIVACY_NOTICE_DRAFT.md` remain in DRAFT status pending legal sign-off (PENDING_QUESTIONS.md Improvement 9, 2026-04-24).

**Information needed from owner:** (a) Legal review and approval of the privacy notice draft content. (b) Confirmation of the data processing activities table (especially the first-tenant funnel per-tenant emission under GDPR Art. 6(1)(f)). (c) Confirmation of the data retention periods. (d) Whether to use an external privacy policy hosting service or self-host. (e) Cookie consent approach for the marketing site.

---

## 10. Pending Questions for Later

### Improvement 1 (Doc Navigator)
- No blocking questions. Fully actionable.

### Improvement 2 (Trial Funnel)
- No blocking questions. Fully actionable.

### Improvement 3 (Golden Corpus Real-LLM)
- When will the Azure OpenAI deployment be provisioned for the golden cohort? (Not blocking the automation work, but blocks actual real-LLM validation runs.)
- What model deployment name should be used as the default for golden cohort runs? (gpt-4o or gpt-4.1?)

### Improvement 4 (Contextual Help)
- Are there preferred copy guidelines for in-product help text? (Can proceed with proposed text and iterate.)
- Should the "Learn more" links point to GitHub docs or a future hosted docs site?

### Improvement 5 (Integration Recipes)
- Is there a preferred Logic Apps Standard SKU and region for the governance notification recipe?
- Should the PR review gate recipe include an Azure DevOps extension manifest, or just pipeline YAML?

### Improvement 6 (Authorization Boundary Tests)
- Are there any authorization boundaries that should NOT be tested (e.g., internal admin endpoints used only by background jobs)?
- Should tenant isolation tests create real SQL databases or use a shared test database with RLS?

### Improvement 7 (SaaS Validation)
- Should the Terraform validation CI job block merges immediately, or start in warn-mode?
- Are there any Terraform roots that are known to be incomplete and should be excluded from validation initially?

### Improvement 8 (Performance Baseline)
- What timing target is appropriate for the core pilot flow against real SQL? (The 10-second target is for in-memory only.)
- Should performance baselines be published in the Trust Center or kept internal?

### Improvement 9 (ROI Validation — DEFERRED)
- See the DEFERRED section above for the full list of needed inputs.

### Improvement 10 (Privacy Notice — DEFERRED)
- See the DEFERRED section above for the full list of needed inputs.
