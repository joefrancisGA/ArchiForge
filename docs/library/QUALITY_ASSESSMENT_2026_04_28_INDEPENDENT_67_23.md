> **Scope:** Independent quality assessment — first-principles, weighted readiness model.

# ArchLucid Assessment – Weighted Readiness 67.23%

**Date:** 2026-04-28
**Assessor:** Independent first-principles analysis (no prior assessments referenced)
**Basis:** Repository contents, documentation, source code, CI workflows, infrastructure-as-code, UI source, test structure, and go-to-market materials as of 2026-04-28.

---

## 1. Executive Summary

### Overall Readiness

ArchLucid reaches **67.23% weighted readiness** — a product that is architecturally disciplined, deeply documented, and CI-hardened, but whose commercial surface remains unproven in market. The engineering core — 30+ C# projects, Dapper-on-SQL persistence with DbUp migrations, a Next.js operator shell, a tiered CI pipeline spanning secret scanning through chaos engineering — is genuinely strong for a pre-revenue product. The gap is between "built right" and "bought by anyone." No customer has used this in production, no reference case exists, Stripe commerce is in TEST mode, and the ROI model is theoretical. The product can do something valuable; it has not yet proven it to a single paying buyer.

### Commercial Picture

The commercial story is exhaustively documented (pricing philosophy, buyer personas, competitive landscape, order-form template, trial flow, ROI model) but entirely unvalidated. Zero published reference customers. Stripe TEST mode only. Azure Marketplace not published. The V1 commercial motion is sales-led by necessity (no self-serve checkout available). The pricing tier structure (Team/Professional/Enterprise; locked workspace and per-seat rates per [PRICING_PHILOSOPHY.md §5](../go-to-market/PRICING_PHILOSOPHY.md#5-locked-list-prices-2026)) is reasonable but competes against tools that are often free (AWS Well-Architected, Azure Advisor) or deeply embedded in enterprise EA toolchains (LeanIX, Ardoq). The value proposition — "reviewable architecture package faster" — is credible but unproven with real customer evidence.

### Enterprise Picture

Enterprise scaffolding is impressively thorough: SCIM 2.0 provisioning, SQL row-level security with SESSION_CONTEXT tenant isolation, 117 typed audit event constants, RBAC with four roles, Trust Center with CAIQ Lite/SIG Core pre-fills, DPA template, subprocessors register, pen-test SoW template, SOC 2 self-assessment mapping. But the hard evidence is absent: no executed pen test (V1.1-deferred), no SOC 2 Type II attestation (roadmap), no PGP coordinated-disclosure key (V1.1-deferred). The governance workflow (approvals, policy packs, pre-commit gate, segregation of duties) is well-designed but has not been tested by an enterprise security review team. Healthcare vertical positioning exists but HIPAA BAA posture is explicitly "contact sales."

### Engineering Picture

Engineering is the strongest pillar by a clear margin. The CI pipeline is unusually comprehensive for a startup: gitleaks secret scan, fast-core .NET tests, greenfield SQL boot, full SQL regression, Simmy chaos tests, OWASP ZAP baseline, Schemathesis API fuzzing, k6 load tests, Playwright E2E (both mock and live-API), Vitest + jest-axe accessibility, Trivy image scanning, Terraform validate, CodeQL, Stryker mutation testing, and benchmark regression — all merge-blocking or scheduled. Architecture boundaries are clean (Application/Core/Contracts/Persistence with Dapper, no EF). Code coverage floors (79% line, 63% branch, 63% per-package) are enforced in CI. The main engineering risks are the simulator-first testing strategy (real LLM execution is not the default CI path), rename cleanup noise (Phase 7), and the owner-conducted security assessment being a template rather than a completed artifact.

---

## 2. Deferred Scope Uncertainty

I located and reviewed the following deferred-scope documents:

- **`docs/library/V1_DEFERRED.md`** — found and verified. Explicitly defers: ITSM connectors (Jira, ServiceNow, Confluence → V1.1), Slack (V2), commerce un-hold (Stripe live keys, Marketplace Published → V1.1), pen-test summary publication (V1.1), PGP key drop (V1.1), MCP server membrane (V1.1), product-learning "brains" (deferred), Phase 7 rename (deferred), and engineering backlog.
- **`docs/library/V1_SCOPE.md`** §3 — found. Confirms out-of-scope items including IDE integration, multi-region active/active, speculative ecosystem, full UI E2E against every live API config.
- **`docs/library/V1_DEFERRED.md`** §6b — found. Commerce un-hold (Stripe live keys, Marketplace Published) explicitly V1.1.
- **`docs/library/V1_DEFERRED.md`** §6c — found. Pen-test publication and PGP key both explicitly V1.1.

Items explicitly deferred to V1.1 or V2 are **not penalized** in scoring.

**Score review confirmation (2026-04-28):** Re-read [V1_DEFERRED.md](V1_DEFERRED.md) against the scores; Marketability, Procurement Readiness, Proof-of-ROI, Trustworthiness, and Adoption Friction weights do **not** include penalties for reference customers, pen-test publication, PGP key drop, commerce live keys, MCP, or first-party ITSM connectors. Scores reflect only in-repo, V1-scope evidence and remaining risks (e.g., self-asserted assurance, no measured pilots).

---

## 3. Weighted Quality Assessment

**Total weight:** 100
**Scoring scale:** 1–100 per quality
**Weighted readiness = sum(score × weight) / sum(100 × weight)**

### Scoring Table (ordered by weighted deficiency — most urgent first)

| Rank | Quality | Score | Weight | Weighted Contribution | Max Possible | Deficiency Signal | Category |
|------|---------|-------|--------|----------------------|--------------|-------------------|----------|
| 1 | Marketability | 52 | 8 | 416 | 800 | 384 | Commercial |
| 2 | Proof-of-ROI Readiness | 52 | 5 | 260 | 500 | 240 | Commercial |
| 3 | Time-to-Value | 66 | 7 | 462 | 700 | 238 | Commercial |
| 4 | Adoption Friction | 63 | 6 | 378 | 600 | 222 | Commercial |
| 5 | Differentiability | 66 | 4 | 264 | 400 | 136 | Commercial |
| 6 | Executive Value Visibility | 67 | 4 | 268 | 400 | 132 | Commercial |
| 7 | Trustworthiness | 60 | 3 | 180 | 300 | 120 | Enterprise |
| 8 | Workflow Embeddedness | 64 | 3 | 192 | 300 | 108 | Enterprise |
| 9 | Correctness | 76 | 4 | 304 | 400 | 96 | Engineering |
| 10 | Usability | 68 | 3 | 204 | 300 | 96 | Enterprise |
| 11 | Security | 73 | 3 | 219 | 300 | 81 | Engineering |
| 12 | Decision Velocity | 60 | 2 | 120 | 200 | 80 | Commercial |
| 13 | Interoperability | 63 | 2 | 126 | 200 | 74 | Enterprise |
| 14 | Traceability | 76 | 3 | 228 | 300 | 72 | Enterprise |
| 15 | Compliance Readiness | 64 | 2 | 128 | 200 | 72 | Enterprise |
| 16 | Procurement Readiness | 65 | 2 | 130 | 200 | 70 | Enterprise |
| 17 | Commercial Packaging Readiness | 66 | 2 | 132 | 200 | 68 | Commercial |
| 18 | AI/Agent Readiness | 70 | 2 | 140 | 200 | 60 | Engineering |
| 19 | Reliability | 71 | 2 | 142 | 200 | 58 | Engineering |
| 20 | Architectural Integrity | 81 | 3 | 243 | 300 | 57 | Engineering |
| 21 | Data Consistency | 73 | 2 | 146 | 200 | 54 | Engineering |
| 22 | Explainability | 74 | 2 | 148 | 200 | 52 | Engineering |
| 23 | Policy and Governance Alignment | 75 | 2 | 150 | 200 | 50 | Enterprise |
| 24 | Azure Compatibility and SaaS Deployment Readiness | 75 | 2 | 150 | 200 | 50 | Engineering |
| 25 | Maintainability | 76 | 2 | 152 | 200 | 48 | Engineering |
| 26 | Template and Accelerator Richness | 56 | 1 | 56 | 100 | 44 | Commercial |
| 27 | Auditability | 79 | 2 | 158 | 200 | 42 | Enterprise |
| 28 | Customer Self-Sufficiency | 58 | 1 | 58 | 100 | 42 | Enterprise |
| 29 | Cognitive Load | 59 | 1 | 59 | 100 | 41 | Engineering |
| 30 | Stickiness | 62 | 1 | 62 | 100 | 38 | Commercial |
| 31 | Cost-Effectiveness | 63 | 1 | 63 | 100 | 37 | Engineering |
| 32 | Scalability | 67 | 1 | 67 | 100 | 33 | Engineering |
| 33 | Accessibility | 70 | 1 | 70 | 100 | 30 | Enterprise |
| 34 | Availability | 70 | 1 | 70 | 100 | 30 | Engineering |
| 35 | Performance | 71 | 1 | 71 | 100 | 29 | Engineering |
| 36 | Manageability | 71 | 1 | 71 | 100 | 29 | Engineering |
| 37 | Change Impact Clarity | 73 | 1 | 73 | 100 | 27 | Enterprise |
| 38 | Deployability | 73 | 1 | 73 | 100 | 27 | Engineering |
| 39 | Extensibility | 73 | 1 | 73 | 100 | 27 | Engineering |
| 40 | Evolvability | 73 | 1 | 73 | 100 | 27 | Engineering |
| 41 | Supportability | 75 | 1 | 75 | 100 | 25 | Engineering |
| 42 | Testability | 79 | 1 | 79 | 100 | 21 | Engineering |
| 43 | Azure Ecosystem Fit | 79 | 1 | 79 | 100 | 21 | Engineering |
| 44 | Observability | 81 | 1 | 81 | 100 | 19 | Engineering |
| 45 | Modularity | 81 | 1 | 81 | 100 | 19 | Engineering |
| 46 | Documentation | 83 | 1 | 83 | 100 | 17 | Engineering |

**Weighted sum:** 6857
**Maximum possible:** 10200
**Weighted readiness:** 6857 / 10200 = **67.23%**

---

### Detailed Quality Justifications (ordered by weighted deficiency)

#### 1. Marketability — Score: 52, Weight: 8, Deficiency: 384

**Justification:** The product solves a real problem (architecture review packaging), but market validation is nonexistent. Zero published reference customers. No public case studies. No customer testimonials. The competitive landscape doc is well-researched, but ArchLucid's positioning as "AI Architecture Intelligence" creates a new category that requires significant market education. The pricing page exists but Stripe is in TEST mode. The buyer journey is mapped in documentation but unexecuted. The `/why` comparison page and procurement pack are built but untested against real procurement cycles.

**Tradeoffs:** The team has invested heavily in documentation and pricing infrastructure that will accelerate go-to-market when activated, at the cost of not having validated any of it with real buyers.

**Improvement recommendations:** Run a real pilot with a real customer. Even one "before/after" story with measured time savings would dramatically improve this score. The ROI model, sponsor brief, and pilot measurement framework are already built — they need to be exercised.

**Fixability:** Partially fixable in V1 (synthetic case study and demo artifacts can be strengthened); real validation requires customer engagement.

---

#### 2. Proof-of-ROI Readiness — Score: 52, Weight: 5, Deficiency: 240

**Justification:** The documented time-to-value path is well-structured: `archlucid try` in 60 seconds, Docker demo via `demo-start.ps1`, seven-step wizard in the UI, and a Core Pilot checklist. The trial signup flow targets prospect-to-active-trial in <5 minutes. However, the real-mode LLM execution time (target p50 <120s, p95 <180s per run) is meaningful, and the simulator-first demo path (which is fast) does not show real AI value — it shows deterministic fake results. The gap between "fast fake demo" and "real AI insight on your architecture" is where time-to-value actually lives. Additionally, the pre-requisite stack (SQL Server, .NET 10, Docker, optionally Azure OpenAI) is non-trivial for a prospect evaluating the product.

**Tradeoffs:** Simulator mode enables fast demos and testing at the cost of showing pre-baked results that do not prove AI value. Hosted SaaS trial (when fully activated) eliminates the install burden but requires commerce un-hold.

**Improvement recommendations:** Ensure the hosted SaaS trial path (staging.archlucid.net) delivers a complete experience including real LLM execution on a sample architecture brief within the 14-day trial window. The first-run wizard should auto-execute a real (not simulated) run on a sample input.

**Fixability:** Partially V1 (optimize the demo and wizard flow); full fix requires hosted SaaS activation (V1.1 commerce un-hold).

---

#### 3. Time-to-Value — Score: 66, Weight: 7, Deficiency: 238

**Justification:** For a self-hosted pilot, the stack is substantial: .NET 10 SDK, Docker, SQL Server, Node 22, and optionally Azure OpenAI credentials. The CLI tool requires dotnet global tool installation. The operator UI is a Next.js shell that proxies to the API — reasonable for operators but not for casual evaluators. The trial funnel design is sensible (email + company → Entra login → auto-provision), but it is not yet live for self-serve. The product has no free tier beyond a 14-day trial. For enterprises already running Azure, adoption is smoother (Entra ID, SQL Server, Container Apps are natural), but for organizations without Azure, the Azure-native design is a constraint. SCIM provisioning and CI/CD integration recipes reduce operational friction for enterprise IT teams.

**Tradeoffs:** Azure-native architecture reduces friction for Azure customers while increasing it for AWS/GCP shops. The comprehensive auth model (DevelopmentBypass, ApiKey, JwtBearer) helps different deployment modes but adds configuration complexity.

**Improvement recommendations:** Accelerate the hosted SaaS trial to eliminate self-hosting friction for evaluation. Ensure `/pricing` has a clear "Start free trial" CTA that works without sales contact when commerce is activated.

**Fixability:** Partially V1 (documentation and onboarding optimization); self-serve trial requires V1.1 commerce un-hold.

---

#### 4. Adoption Friction — Score: 63, Weight: 6, Deficiency: 222

**Justification:** The ROI model is thorough on paper: baseline questions, pilot measurement framework, and value-report DOCX generation exist. The pricing philosophy anchors to ~$294K annual savings for a 6-architect team with a break-even at ~180 architect-hours/year. A synthetic case study for "Contoso Retail" exists. But zero real customers have completed a pilot. The self-service signup form captures optional baseline review-cycle hours for before/after comparison, which is a smart design choice. The value-report endpoint (`POST /v1/pilots/runs/{runId}/first-value-report.pdf`) and the sponsor banner with "Day N since first commit" are purpose-built for ROI storytelling — but with no actual pilot data to populate them.

**Tradeoffs:** Heavy investment in ROI infrastructure (programmatic value reports, baseline capture) pays dividends when customers arrive, but creates an unfunded promise until then.

**Improvement recommendations:** Run one real pilot (even internal) and capture actual before/after cycle-time data. Populate the Contoso synthetic case study with more realistic numbers derived from internal use of the tool on the ArchLucid architecture itself. Consider using ArchLucid to review ArchLucid's own architecture as a dogfooding reference.

**Fixability:** Partially V1 (synthetic and dogfood data); real ROI requires customer engagement (external).

---

#### 5. Differentiability — Score: 66, Weight: 4, Deficiency: 136

**Justification:** The Executive Sponsor Brief is well-written and appropriately scoped: it explains what ArchLucid is, what problem it solves, what a pilot proves, and what not to over-claim. The sponsor banner with PDF projection and "Day N since first commit" badge is a thoughtful engagement mechanism. The value-report DOCX generator and first-value report are designed to give sponsors concrete artifacts. The `/why` comparison page and "Email this run to your sponsor" flow are practical. However, the value story is entirely prospective — no executive has actually seen these artifacts from a real pilot. The "what success should allow a sponsor to say" framing is realistic and modest (not over-claiming), which is appropriate for V1.

**Tradeoffs:** Honest, modest claims (not over-selling) build credibility but reduce urgency compared to competitors making bold AI transformation promises.

**Improvement recommendations:** Create a sponsor-ready PDF or one-page summary that can be shared independent of the product. The executive sponsor brief is markdown — convert it to a polished PDF or web page with real (or realistic synthetic) metrics.

**Fixability:** V1 — generating a sponsor-ready PDF from existing materials is feasible.

---

#### 6. Executive Value Visibility — Score: 67, Weight: 4, Deficiency: 132

**Justification:** ArchLucid's positioning in the "AI Architecture Intelligence" category is genuinely differentiated — no existing tool combines multi-agent architecture analysis with enterprise governance, auditability, and provenance in a single product. The competitive landscape doc correctly identifies that LeanIX/Ardoq are inventory-focused EAM, AWS/Azure/GCP tools are single-cloud advisors, and ChatGPT/Copilot are ad-hoc. The `/why` comparison page provides structured differentiation evidence. However, the differentiation claim is self-asserted: no analyst coverage, no Gartner/Forrester mention, no industry awards, no published customer validation of the differentiation claim. The "10 finding engines" claim is a concrete capability differentiator but its real-world impact versus manual review is unmeasured.

**Tradeoffs:** Creating a new category is high-risk/high-reward — potential for market leadership if validated, but requires heavy market education.

**Improvement recommendations:** Publish a concrete side-by-side comparison using a real architecture brief processed through ArchLucid vs manual review (time, completeness, governance evidence). This demonstrates differentiation through evidence rather than assertion.

**Fixability:** V1 — comparison artifact can be created from existing demo scenarios.

---

#### 7. Trustworthiness — Score: 60, Weight: 3, Deficiency: 120

**Justification:** The system produces structured outputs (manifests, findings, comparison records, governance evidence) with typed schemas and validation (JSON schema validation in `ArchLucid.Decisioning.Validation`, typed finding payloads). The decisioning layer extracts typed payloads from finding envelopes. The authority pipeline has staged execution with per-stage duration histograms. Agent output quality gates (optional) evaluate structural completeness and semantic scoring. However, correctness of LLM-generated content is inherently probabilistic — the system honestly labels AI text as "decision support" not "legal attestation" (Section 9 of the Executive Sponsor Brief). The faithfulness checker (`ExplanationFaithfulnessChecker`) with fallback to deterministic narrative when faithfulness is low is a solid correctness safeguard. Property-based tests (FsCheck) verify invariants. The concern is that "correctness" for an AI-assisted architecture analysis tool is partially undefined — what constitutes a "correct" topology finding or compliance recommendation is domain-dependent.

**Tradeoffs:** The faithfulness fallback trades explanation richness for factual grounding, which is the right call for a trustworthiness-sensitive product.

**Improvement recommendations:** Expand the golden corpus / reference case evaluation framework. The agent output evaluation infrastructure exists (`AgentOutputEvaluationResults` table, reference-case scoring) but is disabled by default. Enable and maintain at least one reference case per agent type in CI.

**Fixability:** V1 — enable reference-case evaluation in CI for core agent types.

---

#### 8. Workflow Embeddedness — Score: 64, Weight: 3, Deficiency: 108

**Justification:** The trust infrastructure is extensively built: Trust Center with evidence pack download, CAIQ Lite pre-fill, SIG Core pre-fill, DPA template, subprocessors register, DSAR process, compliance matrix, STRIDE threat model, RLS risk acceptance template. But the hard trust signals are missing or deferred: no executed pen test (V1.1-deferred, not penalized), no SOC 2 Type II (roadmap), no published reference customer (V1.1-deferred, not penalized). What remains as a V1 concern: the owner-conducted security self-assessment is in "DRAFT" state, and the product has not been validated by any external security reviewer. Content safety is wired (Azure AI Content Safety) but mandatory only in production-like hosts. The SQL RLS break-glass bypass has appropriate guardrails but adds risk surface. Scoring at 55 because the infrastructure is built for trust, but no independent party has verified the claims.

**Tradeoffs:** Building trust infrastructure early (before customers) is an investment that reduces V1.1 effort, but the gap between "self-asserted controls" and "third-party verified" is exactly what enterprise buyers care about.

**Improvement recommendations:** Complete the owner-conducted security self-assessment (remove DRAFT status). This is V1 scope and does not require Aeronova engagement.

**Fixability:** Partially V1 — finalize the self-assessment from DRAFT to FINAL.

---

#### 9. Correctness — Score: 76, Weight: 4, Deficiency: 96

**Justification:** Traceability is a genuine strength. Every authority run gets a persisted W3C trace ID (`dbo.Runs.OtelTraceId`), correlation IDs flow through middleware, the finding inspector shows decision-rule provenance, and the audit log provides a durable timeline. The `V1_REQUIREMENTS_TEST_TRACEABILITY.md` maps V1 scope to tests. Graph visualization exists in the operator UI for provenance. The explainability pipeline produces citation links from findings to persisted artifacts. However, traceability from a buyer's perspective — "can I trace how this architecture recommendation was generated?" — depends on LLM execution traces, which are opt-in (`PersistFullPrompts`) and privacy-sensitive. The default posture (prompts not persisted) reduces traceability for debugging LLM behavior in production.

**Tradeoffs:** Privacy-preserving default (no prompt persistence) trades traceability for data minimization, which is appropriate for regulated environments but limits debugging.

**Improvement recommendations:** Ensure the trace-a-run Grafana dashboard works end-to-end with a documented example. The dashboard exists (`dashboard-archlucid-run-lifecycle.json`) but needs a worked example in the runbook.

**Fixability:** V1 — documentation and worked example.

---

#### 10. Usability — Score: 68, Weight: 3, Deficiency: 96

**Justification:** The operator UI provides progressive disclosure (Pilot → Operate analysis → Operate governance), a seven-step wizard for run creation, keyboard shortcuts, skip-to-content link, and role-aware nav shaping. The Core Pilot path (create → execute → commit → review) is documented and straightforward. However, the product surface is large (35+ operator pages scanned by Playwright, including wizard, list/detail, compare, graph, governance, audit, alerts, settings) — this is a lot for a V1 product. The cognitive overhead of understanding the two-layer model (Pilot vs Operate) and the three disclosure tiers is manageable but non-trivial. The first-run wizard pre-selects a sample preset, which is good. The "Show more links" progressive disclosure pattern is clear. But the mental model requires understanding concepts like "manifests," "findings," "golden manifests," "authority pipeline," "provenance graph" — these are domain-specific terms that require learning.

**Tradeoffs:** Feature richness (governance, audit, alerts, graph, comparison, replay) supports enterprise use cases but increases cognitive load for first-time users. Progressive disclosure mitigates this partially.

**Improvement recommendations:** Add inline contextual help or tooltips for key domain terms (manifest, finding, authority pipeline) in the operator UI. The glossary exists in documentation but is not surfaced in the product.

**Fixability:** V1 — contextual help can be added to the operator shell.

---

#### 11. Security — Score: 73, Weight: 3, Deficiency: 81

**Justification:** Integration points exist: REST API with OpenAPI contract, .NET client NuGet package, CLI tool, CloudEvents webhooks, Service Bus integration events, Microsoft Teams notifications, CI/CD integration recipes for GitHub Actions and Azure DevOps, SCIM provisioning, and Azure DevOps PR decoration (both pipeline-based and server-side via Service Bus). The AsyncAPI spec documents event contracts. However, first-party ITSM integration (Jira, ServiceNow) is V1.1-deferred (not penalized). The product operates as a standalone system that can push events outward — it does not deeply embed in existing architecture workflows like "architect opens Confluence → finds ArchLucid analysis inline" or "architect creates Jira epic → ArchLucid automatically analyzes." The V1 bridge recipes for Jira/ServiceNow are customer-operated (Logic App/Function + webhook) — functional but not zero-friction.

**Tradeoffs:** Standalone-first architecture with event-driven extension points is the right V1 choice (avoids integration risk), but limits "invisible" workflow embedding until V1.1 connectors ship.

**Improvement recommendations:** Strengthen the Azure DevOps pipeline integration with a ready-to-use YAML template that customers can copy-paste into their repo. The integration exists but requires customer-side customization.

**Fixability:** V1 — template improvement is straightforward.

---

#### 12. Decision Velocity — Score: 60, Weight: 2, Deficiency: 80

**Justification:** Architecture is a genuine strength. Clean C4 layering (system context → containers → components), well-bounded projects (Api, Application, Core, Contracts, Persistence, Decisioning, Coordinator, AgentRuntime, KnowledgeGraph, Retrieval, Provenance, etc.), Dapper instead of EF (explicit SQL, no abstraction leakage), DbUp for migrations, strict project dependency structure (`ArchLucid.Core` has no reference to persistence or hosting), primary constructor usage, and architecture tests (`ArchLucid.Architecture.Tests`). The host composition boundary (`ArchLucid.Host.Composition`, `ArchLucid.Host.Core`) cleanly separates startup wiring from business logic. The strangler-fig plan for the coordinator (ADR 0021) shows mature architectural evolution thinking. However, the rename debt (ArchLucid vs internal names) creates noise, and some projects have tight coupling through the contracts assembly (`ArchLucid.Contracts` is referenced widely).

**Tradeoffs:** The large number of projects (30+) enables clean boundaries but increases solution complexity and build times.

**Improvement recommendations:** Continue the coordinator strangler-fig plan to completion per ADR 0021.

**Fixability:** V1.1 — ongoing architectural evolution.

---

#### 13. Traceability — Score: 76, Weight: 3, Deficiency: 72

**Justification:** Security posture is well-documented and CI-enforced: Gitleaks pre-commit, OWASP ZAP baseline (merge-blocking, not `-I`), Schemathesis API fuzzing, CodeQL, Trivy image scanning, Terraform validate + Trivy config scanning. Auth is layered (DevelopmentBypass → ApiKey → JwtBearer with production guards), RLS with SESSION_CONTEXT, content safety for LLM calls, rate limiting with role-aware partitioning, log injection mitigation (LogSanitizer), and CORS with deny-by-default. The `AuthSafetyGuard` blocks DevelopmentBypass in non-Development hosts. `BillingProductionSafetyRules` fails startup with misconfigured Stripe in production. However: the owner security self-assessment is DRAFT (not finalized), the pen test is deferred (not penalized), and the SQL RLS break-glass bypass (`SqlRowLevelSecurityBypassAmbient`) is a controlled risk that adds attack surface.

**Tradeoffs:** Comprehensive security tooling in CI reduces risk of shipping known vulnerabilities, but production security validation (pen test, SOC 2) remains in the self-assertion category.

**Improvement recommendations:** Finalize the owner security self-assessment from DRAFT to FINAL state. Ensure the STRIDE threat model is reviewed against current API surface (last reviewed date not visible in the docs).

**Fixability:** V1 — self-assessment finalization is owner work.

---

#### 14. Architectural Integrity — Score: 81, Weight: 3, Deficiency: 57

**Justification:** The product is designed to accelerate architecture decisions through automated analysis, manifest generation, and governance workflows. But from a buyer's perspective, the decision to adopt ArchLucid itself is slow: no self-serve checkout, sales-led motion, no reference customers to call, no third-party validation. The product's internal decision velocity (run → commit → manifest) is measurable (~6s in-process simulator, target <120s real mode), but the buying decision velocity is hindered by the absence of social proof and self-serve evaluation.

**Tradeoffs:** Sales-led motion gives control over customer experience but dramatically reduces decision velocity compared to self-serve PLG.

**Improvement recommendations:** Create a "try before you buy" hosted demo that requires no signup — similar to a public playground with a pre-populated architecture brief.

**Fixability:** Partially V1 — a read-only showcase with pre-baked results could work; interactive demo requires infrastructure.

---

#### 15. Commercial Packaging Readiness — Score: 66, Weight: 2, Deficiency: 68

**Justification:** The two-layer packaging (Pilot/Operate) is well-designed and explained in `PRODUCT_PACKAGING.md`. Pricing tiers (Team/Professional/Enterprise) are defined with seat counts, run allowances, and feature gates. The order-form template, MSA template, and DPA template exist. The Stripe checkout integration is built (controllers, webhook handlers, production safety rules). The Azure Marketplace SaaS offer alignment doc exists. However, the packaging is "infrastructure ready, commerce not live": the `/pricing` page displays numbers but `POST /v1/marketing/pricing/quote-request` goes to a sales inbox, not checkout. The `[RequiresCommercialTenantTier]` 402 filter is wired but operates against tenant tier assignments that are manually set.

**Tradeoffs:** Building commerce infrastructure ahead of customers means the un-hold is a configuration flip, not an engineering project — but the flip itself is deferred.

**Improvement recommendations:** Validate that the pricing quote request flow works end-to-end (form → SQL → email notification to sales inbox). This is within V1 scope.

**Fixability:** V1 — validate existing flows.

---

#### 16. Auditability — Score: 79, Weight: 2, Deficiency: 42

**Justification:** Auditability is strong: 117 audit event type constants in `AuditEventTypes.cs` (verified by CI `assert_audit_const_count.py`), append-only SQL store with database-level `DENY UPDATE/DELETE`, paginated search with keyset cursor, CSV export with UTC range filtering, retention tiering (hot/warm/cold), and correlation ID threading. Governance workflows dual-write to both baseline and durable channels. The CI guard ensures the audit coverage matrix stays in sync with constants. Known gaps are zero for durable-audit omissions in the previously listed mutating areas. However, the audit search uses `OccurredUtc` only for keyset cursor — tie-breaking for identical timestamps is a documented limitation for very large logs.

**Tradeoffs:** Append-only audit with database-level enforcement is stronger than application-only guarantees, at the cost of requiring the `ArchLucidApp` database role for DENY to apply.

**Improvement recommendations:** Add `EventId` tie-breaking to the keyset cursor for audit search.

**Fixability:** V1 — SQL and API changes for cursor tie-breaking.

---

#### 17. Policy and Governance Alignment — Score: 75, Weight: 2, Deficiency: 50

**Justification:** Governance workflow is well-implemented: approval with segregation of duties (self-approval blocked), SLA tracking with webhook escalation, pre-commit governance gate (`ArchLucid:Governance:PreCommitGateEnabled`), versioned policy packs with scope assignments, effective governance resolution, and a governance dashboard. The finding-to-decision pipeline uses typed payloads per category. The governance resolution duration is instrumented (`governance_resolve_duration_ms`). However, policy packs are configuration-driven, not yet tested against real enterprise governance policies (SOX, COBIT, TOGAF review gates). The pre-commit gate blocks on severity thresholds but the threshold semantics are ArchLucid-native, not mapped to standard frameworks.

**Tradeoffs:** Custom governance model is flexible but requires mapping work by customers who have existing governance frameworks.

**Improvement recommendations:** Create a sample policy pack that maps to a common governance framework (e.g., TOGAF ADM phase gates or AWS Well-Architected review criteria).

**Fixability:** V1 — template policy pack creation.

---

#### 18. Compliance Readiness — Score: 64, Weight: 2, Deficiency: 72

**Justification:** Compliance infrastructure is scaffolded: SOC 2 self-assessment mapping, compliance matrix, CAIQ Lite pre-fill, SIG Core pre-fill, compliance drift trend tracking, and a Trust Center. The GDPR DSAR process is documented. The healthcare vertical brief addresses HIPAA positioning (not a regulated record system). However, no external compliance certification exists — SOC 2 Type II is on the roadmap but not achieved, and the self-assessment is self-asserted. The compliance drift trend feature in the operator UI is a differentiator but its accuracy depends on the quality of compliance findings generated by the LLM agents.

**Tradeoffs:** Self-assessment first is the pragmatic startup approach; external certification is expensive and time-consuming but eventually necessary for enterprise deals.

**Improvement recommendations:** Ensure the SOC 2 self-assessment is comprehensive enough that a future Type II engagement can use it as a starting point, reducing auditor effort.

**Fixability:** V1 — self-assessment refinement.

---

#### 19. Procurement Readiness — Score: 65, Weight: 2, Deficiency: 70

**Justification:** Procurement artifacts are extensively prepared: DPA template, MSA template, order-form template, subprocessors register, evidence pack ZIP download (anonymous, no email gate), Trust Center with posture summary, SLA summary, CAIQ Lite / SIG Core pre-fills, and "How to request the procurement pack" guide. The evidence pack endpoint carries ETag and Cache-Control headers. However: no SOC 2 report, no pen-test summary (V1.1-deferred, not penalized), no reference customer to contact during due diligence (V1.1-deferred, not penalized). The "current assurance posture" doc exists. The V1 procurement posture is "self-asserted with templates ready for third-party validation" — honest but not sufficient for enterprise procurement teams that require SOC 2 as a deal gate.

**Tradeoffs:** Building procurement infrastructure (templates, evidence pack, Trust Center) ahead of actual deals reduces procurement cycle time when deals materialize, but the missing SOC 2 report will be a hard stop for many enterprise procurement processes.

**Improvement recommendations:** Add a "procurement FAQ" that proactively addresses "Where is your SOC 2?" and "Can I see pen test results?" with honest timelines.

**Fixability:** V1 — FAQ creation.

---

#### 20. Interoperability — Score: 62, Weight: 2, Deficiency: 76

**Justification:** The product offers REST API with OpenAPI 3.0 contract, .NET client NuGet package, CloudEvents webhook format, AsyncAPI spec for event contracts, Azure Service Bus integration, SCIM 2.0 provisioning, Microsoft Teams connector, and CI/CD integration recipes. Context connectors (`IContextConnector`) and finding engine templates (`dotnet new archlucid-finding-engine`) enable extensibility. However, interoperability is Azure-centric: no AWS or GCP native connectors, no Terraform state import from non-Azure providers (planned but not shipped), no ArchiMate or Structurizr import (planned). The V1 architecture import story is manual (paste brief text into the wizard). The webhook bridge recipes for Jira/ServiceNow are customer-operated, which is reasonable for V1 but limits interoperability.

**Tradeoffs:** Azure-native focus reduces interoperability breadth but deepens Azure integration quality.

**Improvement recommendations:** Implement the planned Terraform state import connector for at least `terraform show -json` → ArchLucid context. This is the most universally useful import path since Terraform is multi-cloud.

**Fixability:** V1.1 — planned item.

---

#### 21. Reliability — Score: 70, Weight: 2, Deficiency: 60

**Justification:** Reliability infrastructure is solid: circuit breakers for LLM calls with configurable thresholds and state reporting in health JSON, retry policies with exponential backoff, degraded-mode design documented, data consistency orphan probes with detection/alert/quarantine modes, Simmy chaos tests in CI, and comprehensive health checks (live/ready/full). The outbox pattern for integration events and retrieval indexing adds durability. However, the system has not been load-tested in a production-like environment — k6 load tests run against Docker Compose, not Azure Container Apps with realistic SQL and LLM latency. The pipeline timeout (`AuthorityPipeline:PipelineTimeout`) and circuit breaker thresholds are configurable but default values have not been validated against production workloads.

**Tradeoffs:** Simulator-first testing is fast and deterministic but does not validate reliability under real LLM latency and failure modes.

**Improvement recommendations:** Run k6 load tests against the staging deployment (staging.archlucid.net) to validate reliability under realistic conditions.

**Fixability:** V1 — infrastructure exists, needs execution.

---

#### 22. Data Consistency — Score: 72, Weight: 2, Deficiency: 56

**Justification:** Data consistency is actively monitored: `DataConsistencyOrphanProbeHostedService` detects orphaned rows in `ComparisonRecords`, `GoldenManifests`, and `FindingsSnapshots`. Enforcement modes (Alert, Quarantine, AutoQuarantine) provide graduated response. Prometheus metrics track detection and quarantine counts. The replay verify mode (422 on drift) provides comparison-level consistency checking. DbUp migrations are ordered and idempotent. However, the system uses eventual consistency for some paths (outbox processing, integration events) and the orphan probe is detection-only by default. The RLS SESSION_CONTEXT isolation adds a correctness dependency on proper tenant context propagation.

**Tradeoffs:** Eventual consistency for non-critical paths (events, indexing) is appropriate, but requires monitoring to ensure convergence.

**Improvement recommendations:** Enable the orphan probe in Alert mode by default (not just detection-only) so operators are notified of consistency issues automatically.

**Fixability:** V1 — configuration change.

---

#### 23. Maintainability — Score: 75, Weight: 2, Deficiency: 50

**Justification:** The codebase follows consistent patterns: primary constructors, expression-bodied members, pattern matching, LINQ preference, target-typed new, one class per file. EditorConfig enforces formatting. Directory.Build.props centralizes package versions. The code is modular (30+ projects with clear boundaries). Architecture tests verify structural rules. Stryker mutation testing (ratchet target 72%) supplements line coverage. The `NEXT_REFACTORINGS.md` backlog tracks technical debt explicitly. However, the rename debt (Phase 7) means some config keys, SQL object names, and Terraform resources use legacy names, creating confusion for new contributors. The 188 markdown docs in `docs/library/` alone suggest documentation maintenance burden.

**Tradeoffs:** Comprehensive documentation and architecture tests reduce future maintenance risk but create present-day maintenance overhead.

**Improvement recommendations:** Prioritize the Phase 7 rename items that affect operator-facing configuration keys (appsettings paths that operators must type).

**Fixability:** V1.1 — Phase 7 is explicitly deferred but could be partially started.

---

#### 24. Explainability — Score: 73, Weight: 2, Deficiency: 54

**Justification:** Explainability is a product differentiator with genuine engineering depth: finding inspector API (`GET /v1/findings/{findingId}/inspect`), explainability traces with completeness scoring, citation rendering from findings to persisted artifacts, faithfulness checking with deterministic fallback, aggregate explanation summary with caching, redacted LLM audit surface, and "Why?" links in the operator UI. The explainability trace completeness ratio is instrumented as a histogram with Prometheus alerts (p10 < 0.35 triggers). However, explainability of LLM-generated content is inherently limited — citations point to artifacts the system grounded on, not to the model's internal reasoning. The distinction between "the system cited these artifacts" and "the system provably derived its conclusion from these artifacts" is important and correctly acknowledged in the Executive Sponsor Brief §9.

**Tradeoffs:** Citation-based explainability is the pragmatic approach for LLM outputs — full causal tracing is not feasible, and the product is honest about this.

**Improvement recommendations:** Expand the faithfulness checker to cover more output types beyond aggregate explanations.

**Fixability:** V1 — incremental expansion of existing infrastructure.

---

#### 25. AI/Agent Readiness — Score: 68, Weight: 2, Deficiency: 64

**Justification:** The agent architecture is well-structured: `ArchLucid.AgentRuntime` with real vs simulator execution modes, multiple agent types (Topology, Cost, Compliance, Critic), structured output evaluation (structural completeness ratio, semantic score, quality gate), content safety guard for LLM calls, prompt redaction, circuit breakers, token quota management, and retry policies. The agent output evaluation framework (reference-case scoring) exists but is disabled by default. Agent trace forensics with blob storage and SQL inline fallback handle persistence failures gracefully. However, the agent orchestration is pipeline-sequential (not truly autonomous), the V1 agents do not do multi-step planning, and the MCP server is V1.1-deferred (not penalized). The distance between "orchestrated agent tasks" and "autonomous architecture AI" is significant, and V1 correctly positions itself on the conservative end.

**Tradeoffs:** Orchestrated agents are more predictable and testable than autonomous agents, at the cost of reduced flexibility and emergent capabilities.

**Improvement recommendations:** Enable the reference-case evaluation framework (`AgentExecution:ReferenceEvaluation:Enabled`) in CI with at least one golden corpus case per agent type.

**Fixability:** V1 — enable existing infrastructure.

---

#### 26. Azure Compatibility and SaaS Deployment Readiness — Score: 74, Weight: 2, Deficiency: 52

**Justification:** Azure deployment is comprehensively prepared: 15+ Terraform roots covering Container Apps, storage, private networking, edge (Front Door), monitoring, Entra, SQL failover, Key Vault, OpenAI, Service Bus, Logic Apps, and OTEL collector. The `apply-saas.ps1` script orchestrates multi-root applies. The `REFERENCE_SAAS_STACK_ORDER.md` documents the greenfield deployment sequence. Staging is at `staging.archlucid.net` with Front Door custom domains. Container images are Docker-built with Trivy scanning. However, the production deployment path is documented but has not been executed against a customer-facing Azure subscription. The CD pipeline (`cd.yml`, `cd-staging-on-merge.yml`, `cd-saas-greenfield.yml`) exists but the production SaaS has not been fully activated.

**Tradeoffs:** Building the full Azure IaC stack ahead of production activation means the deployment is well-tested in staging but unverified in production-customer conditions.

**Improvement recommendations:** Execute the `cd-saas-greenfield.yml` pipeline against a fresh Azure subscription to validate the greenfield deployment path end-to-end.

**Fixability:** V1 — execution of existing pipeline.

---

#### 27–46. Remaining Qualities (Weight 1 each)

**Stickiness (60):** Data lock-in through manifests, governance history, audit trail, and comparison records creates natural stickiness. However, export capabilities (DOCX, ZIP, CSV audit export) deliberately reduce stickiness — the right trade for trust.

**Template and Accelerator Richness (55):** Finding engine template (`dotnet new archlucid-finding-engine`), Jira/ServiceNow webhook bridge recipes, CI/CD integration recipes exist. But the sample architecture preset is limited (no industry-specific templates, no cloud-pattern templates beyond generic web app).

**Accessibility (68):** WCAG 2.1 AA target, 35 URL patterns scanned by axe-core Playwright (merge-blocking), jest-axe component tests, skip-to-content link, landmark navigation, focus management, error regions. Good foundation; no manual accessibility testing documented.

**Customer Self-Sufficiency (56):** Documentation is exhaustive for contributors but enterprise customer self-service is limited by the absence of a hosted knowledge base, in-product help, or customer community.

**Change Impact Clarity (72):** Comparison replay with verify mode (422 on drift), structured manifest deltas, governance dashboard with cross-run change summaries. The "what changed and why" story is well-served.

**Availability (68):** Health checks (live/ready/full), circuit breakers, degraded mode design, SQL failover Terraform module, but no published SLA targets beyond documentation.

**Performance (70):** In-process baselines (E2E <10s simulator), k6 load test infrastructure, cold-start documentation, performance benchmarks in CI. Not validated against production.

**Scalability (66):** Scaling path documented, Container Apps for horizontal scaling, SQL read replicas discussed, but vertical scaling limits of the authority pipeline not benchmarked.

**Supportability (74):** `support-bundle --zip`, correlation IDs, troubleshooting guide, CLI `doctor`, health endpoints, trace viewer integration. Good support tooling.

**Manageability (70):** Configuration reference, Key Vault support, RBAC, rate limiting, content safety toggle, but many configuration knobs with limited validation of combinations.

**Deployability (72):** Docker Compose profiles, Terraform IaC, CI/CD pipelines, release-smoke scripts. Straightforward for Azure-native deployments.

**Observability (80):** Exceptionally strong: custom meters, histograms, counters, activity sources, Grafana dashboards, Prometheus alerts, trace sampling, business KPI metrics, trial funnel metrics. This is one of the strongest areas.

**Testability (78):** Tiered test structure (fast core, integration, slow, full regression), property-based tests, architecture tests, mutation testing, chaos testing, API fuzzing, load testing. Comprehensive.

**Modularity (80):** 30+ projects with clean boundaries, architecture tests enforcing dependency rules, host composition separate from business logic. Strong modularity.

**Extensibility (72):** Context connectors, finding engine templates, webhook consumers, integration event patterns. Good extension points but limited third-party ecosystem.

**Evolvability (72):** ADR process, strangler-fig plan, explicit deferral documentation, modular architecture. The system is designed to evolve.

**Documentation (82):** 188 files in `docs/library/`, exhaustive coverage of every surface. Possibly over-documented — finding the right doc is harder than it should be. The five-document spine and NAVIGATOR help.

**Azure Ecosystem Fit (78):** Entra ID, SQL Server, Container Apps, Service Bus, Key Vault, Front Door, Logic Apps, OTEL, Application Insights. Deep Azure alignment.

**Cognitive Load (58):** The product surface is large. The two-layer model (Pilot/Operate) and three disclosure tiers help, but the sheer number of concepts (manifests, findings, governance, policy packs, advisory scans, provenance graphs, replay, comparison) creates significant learning overhead. The glossary helps but is not in-product.

**Cost-Effectiveness (62):** Terraform consumption budgets for OpenAI and SQL, per-tenant cost model documented, pilot profile sizing. LLM cost per run is acknowledged but not precisely bounded. The 14-day free trial with 10 runs is reasonable.

---

## 4. Top 10 Most Important Weaknesses

1. **No market validation.** Zero paying customers, zero completed pilots, zero reference stories. The entire commercial layer is built but unvalidated. This is the single largest risk to the product's viability.

2. **ROI claims are theoretical.** The ROI model projects $294K savings for a 6-architect team, but no real measurement data exists. The sponsor brief and value-report infrastructure are ready to capture data but have never been populated with actual outcomes.

3. **Self-serve evaluation path is blocked.** The hosted SaaS trial requires commerce un-hold (V1.1-deferred). Prospects cannot evaluate the product without sales contact or self-hosting a substantial tech stack (.NET, Docker, SQL, Node).

4. **Security posture is self-asserted.** The Trust Center, compliance matrix, and threat model are all self-asserted. The owner security self-assessment is in DRAFT state. No independent security validation has occurred.

5. **LLM correctness is unverified at scale.** Agent output quality gates exist but are disabled by default. Reference-case evaluation is disabled. The system's ability to produce correct, useful architecture recommendations has not been validated against a diverse corpus of real architecture briefs.

6. **Cognitive load is high for new users.** The product introduces many domain-specific concepts (manifests, findings, golden manifests, authority pipeline, provenance, findings engines). Progressive disclosure helps but the operator shell exposes 35+ pages. The glossary is doc-only, not in-product.

7. **Simulator vs real mode gap.** The default testing mode (simulator) produces deterministic fake results. The gap between simulator output and real LLM output is where product quality actually lives, and this gap is not tested in the default CI path.

8. **Workflow embedding is shallow.** The product operates as a standalone system with event-driven extension points. It does not natively embed in existing architecture workflows (Confluence spaces, Jira boards, IDE extensions). V1 bridge recipes require customer-operated infrastructure.

9. **Azure-only deployment assumption.** The product is Azure-native by design (ADR 0020), which is appropriate for the target market but constrains adoption by AWS/GCP-primary organizations. No multi-cloud deployment path exists.

10. **Documentation volume creates navigation burden.** 188 files in `docs/library/` plus dozens in `docs/go-to-market/`, `docs/security/`, `docs/engineering/`, etc. The NAVIGATOR and five-document spine help, but new contributors face significant document discovery overhead.

---

## 5. Top 5 Monetization Blockers

1. **No self-serve checkout.** Stripe is TEST mode, Marketplace is not Published, DNS cutover for signup is not complete. Every purchase requires sales contact. This is the most direct monetization blocker (V1.1-deferred, not penalized, but still the blocker).

2. **No reference customer for social proof.** Enterprise buyers will not be the first customer without seeing someone else succeed. The reference-customer table has zero rows at `Status: Published`. No case study, no testimonial, no logo permission.

3. **Unproven ROI claim.** The pricing is value-based (locked workspace and per-seat rates per [PRICING_PHILOSOPHY.md §5](../go-to-market/PRICING_PHILOSOPHY.md#5-locked-list-prices-2026)) against a claimed ~$294K savings. Without a single measured pilot, the value claim is a hypothesis, not evidence. Buyers who ask "who else has seen these savings?" get silence.

4. **Category creation burden.** "AI Architecture Intelligence" is not an established category. Buyers searching for solutions will find LeanIX, Ardoq, or "just use ChatGPT." ArchLucid must educate before it can sell, which lengthens sales cycles and increases CAC.

5. **No inbound demand generation.** The marketing site (archlucid.net) exists but there is no documented SEO strategy, content marketing plan, or inbound lead generation mechanism beyond the `/pricing` quote request form. The product relies on founder-led sales without a pipeline.

---

## 6. Top 5 Enterprise Adoption Blockers

1. **No SOC 2 Type II report.** Many enterprise procurement processes require SOC 2 as a minimum security certification. The roadmap acknowledges this but the self-assessment is the current posture. Some enterprises will not proceed past initial evaluation without SOC 2.

2. **No independent pen test results.** The Aeronova engagement is planned (V1.1-deferred, not penalized), but enterprise security teams expect either a recent pen test report or a credible timeline. The current posture is a SoW template and "engagement in flight" status.

3. **ITSM integration gap.** Enterprise architecture teams work in Jira, ServiceNow, and Confluence. V1 offers webhook bridge recipes (customer-operated); first-party connectors are V1.1-deferred (not penalized). But enterprise buyers evaluating V1 will see this as a deployment burden.

4. **Single-cloud deployment constraint.** Azure-native deployment is a strength for Azure-first enterprises but a blocker for AWS/GCP-primary organizations. No documented deployment path for non-Azure environments exists.

5. **No SLA guarantee.** The SLA summary doc exists but no contractual SLA is available. Enterprise procurement requires committed uptime guarantees with financial penalties. The current posture is "here's our target," not "here's our commitment."

---

## 7. Top 5 Engineering Risks

1. **Simulator-reality gap.** The entire CI pipeline (except optional real-mode benchmarks) runs against the simulator. If real LLM execution produces subtly incorrect, incoherent, or low-quality outputs, this will not be caught until production. The agent output quality gate infrastructure exists but is off by default.

2. **SQL single-point-of-failure.** The authority pipeline depends on SQL Server for transactional writes. The SQL failover Terraform module exists, but the RTO/RPO targets documented in `RTO_RPO_TARGETS.md` have not been validated with a failover drill. A SQL failure during authority pipeline execution leaves the run in an intermediate state.

3. **LLM provider dependency.** The product depends on Azure OpenAI for real-mode execution. The circuit breaker and retry infrastructure handle transient failures, but sustained LLM outages, quota exhaustion, or model deprecation could make the product non-functional. No fallback to alternative providers (Anthropic, OpenAI direct, local models) exists.

4. **RLS bypass risk.** The `SqlRowLevelSecurityBypassAmbient.Enter()` mechanism, while properly guarded (requires two config flags + production Prometheus alert), represents a tenant isolation escape hatch. If misconfigured or exploited, cross-tenant data access becomes possible.

5. **Rename debt accumulation.** Phase 7 rename items (SQL object names, Terraform state, config keys, Entra app registrations, GitHub repo) create a growing maintenance burden and confusion risk. The longer the rename is deferred, the more legacy references accumulate in customer deployments and documentation.

---

## 8. Most Important Truth

**ArchLucid is a well-engineered product that has never been used by a customer.** The architecture is sound, the CI is comprehensive, the documentation is exhaustive, and the commercial infrastructure is built. But the product exists in a pre-commercial vacuum where every quality — correctness, trustworthiness, usability, ROI — is self-assessed. The single most important thing that would transform this product's readiness is one real customer completing one real pilot with measured outcomes. Every other improvement is secondary to that.

---

## 9. Top Improvement Opportunities

### Improvement 1: Enable Agent Output Reference-Case Evaluation in CI

**Title:** Enable Agent Output Reference-Case Evaluation in CI

**Why it matters:** The correctness of LLM-generated architecture analysis is the core product claim, but it is not validated in CI. The reference-case evaluation framework exists (table, scoring, metrics) but is disabled (`AgentExecution:ReferenceEvaluation:Enabled=false`). Enabling it with at least one golden case per agent type closes the gap between "we have quality infrastructure" and "we actually measure quality."

**Expected impact:** Directly improves Correctness (+5-8 pts), AI/Agent Readiness (+4-6 pts), Trustworthiness (+2-3 pts). Weighted readiness impact: +0.4-0.6%.

**Affected qualities:** Correctness, AI/Agent Readiness, Trustworthiness, Testability

**Status:** Fully actionable now.

**Cursor prompt:**

```
In the ArchLucid solution, enable the agent output reference-case evaluation framework for CI.

Steps:
1. In `ArchLucid.AgentRuntime` (or wherever `AgentExecution:ReferenceEvaluation:Enabled` is read), confirm the evaluation pipeline works when enabled.
2. Create at least one golden reference case per agent type (Topology, Cost, Compliance, Critic) under a new `tests/golden-corpus/` directory. Each case should contain:
   - An input architecture brief (JSON matching ArchitectureRequest schema)
   - Expected structural fields in the agent output (minimum keys present, severity distribution)
   - A semantic quality threshold (e.g., score >= 0.6)
3. Add an `appsettings.Testing.json` override or test-specific config that sets `AgentExecution:ReferenceEvaluation:Enabled=true` for the relevant test project.
4. Create a test class `AgentOutputReferenceEvaluationTests` in `ArchLucid.AgentRuntime.Tests` that:
   - Loads each golden case
   - Runs the agent (simulator mode is fine for structural checks)
   - Asserts structural completeness ratio >= 0.7
   - Asserts semantic score >= 0.5 (tunable threshold)
   - Carries `[Trait("Suite", "Core")]`
5. Ensure the `archlucid_agent_output_reference_case_evaluations_total` and `archlucid_agent_output_reference_case_score_ratio` metrics are emitted when evaluation is enabled.

Acceptance criteria:
- At least 4 golden cases exist (one per agent type)
- Tests pass in `dotnet test --filter "Suite=Core"`
- Metrics are emitted during test execution
- No changes to production `appsettings.json` defaults (keep `Enabled: false` for production)

Constraints:
- Do not change the existing agent execution pipeline behavior
- Do not add new NuGet dependencies
- Golden cases should use simulator-compatible inputs (no real LLM required)
- Keep the golden corpus small (one case per agent type) for fast test execution

What not to change:
- Production configuration defaults
- Agent execution hot path
- Existing test structure or naming conventions
```

---

### Improvement 2: Finalize Owner Security Self-Assessment from DRAFT

**Title:** Finalize Owner Security Self-Assessment from DRAFT to FINAL

**Why it matters:** The owner-conducted security self-assessment (`docs/security/OWNER_SECURITY_ASSESSMENT_2026_Q2-DRAFT.md`) is in DRAFT state. Every Trust Center reference and evidence pack download currently links to a DRAFT document. Finalizing it to a non-DRAFT state (with an honest "owner-conducted, not third-party" label) removes the uncertainty signal for procurement teams reviewing the evidence pack.

**Expected impact:** Directly improves Trustworthiness (+3-5 pts), Security (+2-3 pts), Procurement Readiness (+2-3 pts). Weighted readiness impact: +0.2-0.4%.

**Affected qualities:** Trustworthiness, Security, Procurement Readiness, Compliance Readiness

**Status:** Fully actionable now.

**Cursor prompt:**

```
Finalize the owner-conducted security self-assessment from DRAFT to FINAL status.

Steps:
1. Read `docs/security/OWNER_SECURITY_ASSESSMENT_2026_Q2-DRAFT.md` fully.
2. Rename the file to `docs/security/OWNER_SECURITY_ASSESSMENT_2026_Q2.md` (remove `-DRAFT` suffix).
3. Update the document:
   - Change any "DRAFT" status labels to "Owner-conducted assessment (not third-party audited)"
   - Review each section and ensure claims match current codebase reality (cross-reference with SECURITY.md, SYSTEM_THREAT_MODEL.md, COMPLIANCE_MATRIX.md)
   - Add a "Last reviewed" date of today
   - Add a "Limitations" section stating: "This assessment is owner-conducted. It has not been validated by an independent third party. An independent penetration test is scheduled for V1.1 (see V1_DEFERRED.md §6c)."
   - Ensure all section references to in-repo evidence use correct relative links
4. Update all references to the old filename:
   - `docs/trust-center.md` — update the link
   - `docs/go-to-market/CURRENT_ASSURANCE_POSTURE.md` — update if it references the draft
   - `docs/go-to-market/OWNER_SECURITY_ASSESSMENT_REDACTED_FOR_PACK.md` — update source reference
   - Any other files that reference `OWNER_SECURITY_ASSESSMENT_2026_Q2-DRAFT.md`
5. Verify the evidence pack endpoint still includes the assessment (it should pick up the new filename if the builder references the directory or a config list).

Acceptance criteria:
- File renamed without `-DRAFT` suffix
- All internal references updated
- Document states "Owner-conducted assessment (not third-party audited)" clearly
- Limitations section added
- Last reviewed date is current
- No broken links in trust-center.md or related files

Constraints:
- Do not add claims that are not supported by existing in-repo evidence
- Do not claim third-party validation
- Do not change the structure of the assessment — only update status labels and add limitations
- Do not modify the evidence pack builder code unless the filename change requires it

What not to change:
- The content of other security documents (SYSTEM_THREAT_MODEL.md, etc.)
- The Trust Center structure
- The evidence pack endpoint behavior (beyond filename pickup)
```

---

### Improvement 3: Add In-Product Glossary Tooltips to Operator Shell

**Title:** Add In-Product Glossary Tooltips to Operator Shell

**Why it matters:** The product introduces many domain-specific concepts (manifests, findings, golden manifests, authority pipeline, provenance, findings engines). A glossary exists in `docs/library/GLOSSARY.md` but is not surfaced in the operator UI. First-time users face a steep learning curve because terminology is unexplained in-context. Tooltips reduce cognitive load and improve usability without changing the product surface.

**Expected impact:** Directly improves Cognitive Load (+5-7 pts), Usability (+3-5 pts), Customer Self-Sufficiency (+2-3 pts). Weighted readiness impact: +0.2-0.4%.

**Affected qualities:** Cognitive Load, Usability, Customer Self-Sufficiency, Time-to-Value

**Status:** Fully actionable now.

**Cursor prompt:**

```
Add contextual glossary tooltips to the ArchLucid operator shell (archlucid-ui).

Steps:
1. Read `docs/library/GLOSSARY.md` and extract the 15 most important terms that appear in the operator UI (e.g., manifest, finding, golden manifest, run, commit, authority pipeline, governance, policy pack, approval, comparison, replay, provenance, advisory, brief, artifact).
2. Create a new file `archlucid-ui/src/lib/glossary.ts` that exports a `Record<string, string>` mapping term keys to short (1-2 sentence) definitions derived from GLOSSARY.md.
3. Create a reusable `GlossaryTerm` React component in `archlucid-ui/src/components/ui/glossary-term.tsx` that:
   - Renders its children (the term text) with a dotted underline
   - Shows a tooltip on hover/focus with the glossary definition
   - Uses the existing tooltip pattern if one exists in the component library, or a simple `title` attribute + accessible `aria-describedby` as a minimal implementation
   - Applies `tabIndex={0}` for keyboard accessibility
4. Add `GlossaryTerm` usage to at least 5 key locations:
   - Home page Core Pilot checklist (wrap "manifest", "findings", "commit")
   - Run detail page (wrap "golden manifest", "artifacts")
   - Governance dashboard (wrap "policy pack", "approval workflow")
   - Compare page (wrap "comparison", "replay")
   - Any page header that uses a domain-specific term
5. Add a Vitest test for `GlossaryTerm` that verifies tooltip content rendering and keyboard accessibility.

Acceptance criteria:
- `glossary.ts` contains at least 15 terms with concise definitions
- `GlossaryTerm` component renders with dotted underline and tooltip
- At least 5 pages have glossary terms applied
- Vitest test passes
- `npm run build` succeeds
- Tooltip is keyboard-accessible (focusable, content announced)

Constraints:
- Definitions must be derived from GLOSSARY.md, not invented
- Keep definitions to 1-2 sentences max (tooltip, not encyclopedia)
- Do not change page layout or visual hierarchy
- Use existing UI component library patterns where available
- Do not add new npm dependencies unless necessary for tooltip behavior

What not to change:
- Page routing or navigation
- API contracts
- Existing component behavior
- Build configuration
```

---

### Improvement 4: Enable Data Consistency Orphan Probe in Alert Mode by Default

**Title:** Enable Data Consistency Orphan Probe in Alert Mode by Default

**Why it matters:** The `DataConsistencyOrphanProbeHostedService` detects orphaned rows (comparison records, golden manifests, findings snapshots referencing missing runs) but the default enforcement mode is detection-only. Enabling Alert mode by default means operators are notified of data consistency issues automatically via `archlucid_data_consistency_alerts_total` counter, which feeds into existing Prometheus alert infrastructure.

**Expected impact:** Directly improves Data Consistency (+5-7 pts), Reliability (+3-4 pts), Supportability (+2-3 pts). Weighted readiness impact: +0.2-0.3%.

**Affected qualities:** Data Consistency, Reliability, Supportability, Observability

**Status:** Fully actionable now.

**Cursor prompt:**

```
Enable the data consistency orphan probe in Alert mode by default.

Steps:
1. Find the configuration key for `DataConsistency:Enforcement:Mode` in appsettings files and default configuration.
2. Change the default from the current value (likely detection-only or unset) to `Alert` in `ArchLucid.Api/appsettings.json`.
3. Verify that `Alert` mode:
   - Increments `archlucid_data_consistency_alerts_total` counter when orphan counts meet `AlertThreshold`
   - Does NOT quarantine rows (that requires `Quarantine` or `AutoQuarantine` mode)
   - Logs at Warning level for operator visibility
4. If `AlertThreshold` is configurable, ensure the default is reasonable (e.g., 1 — alert on any orphan) under `DataConsistency:Enforcement:AlertThreshold`.
5. Update `docs/library/OBSERVABILITY.md` to note the default enforcement mode change.
6. Verify that existing tests pass with the new default.
7. If there is no existing Prometheus alert rule for `archlucid_data_consistency_alerts_total`, add one to `infra/prometheus/archlucid-alerts.yml` with a rule like:
   ```yaml
   - alert: ArchLucidDataConsistencyOrphanAlerts
     expr: increase(archlucid_data_consistency_alerts_total[1h]) > 0
     for: 5m
     labels:
       severity: warning
     annotations:
       summary: "Data consistency orphans detected"
       description: "Orphaned rows detected in {{ $labels.table }}.{{ $labels.column }}. Investigate using the data consistency runbook."
   ```

Acceptance criteria:
- Default enforcement mode is `Alert` in shipped `appsettings.json`
- Prometheus counter increments when orphans are detected and meet threshold
- No quarantine behavior (Alert mode only)
- Existing tests pass
- Documentation updated
- Prometheus alert rule exists

Constraints:
- Do not enable Quarantine or AutoQuarantine by default — Alert only
- Do not change the probe's detection logic
- Do not add new configuration keys beyond what exists
- Keep the alert threshold conservative (alert on any orphan)

What not to change:
- The orphan probe detection logic
- Quarantine table schema
- Production database data
- Other monitoring configurations
```

---

### Improvement 5: Create Sponsor-Ready PDF from Executive Sponsor Brief

**Title:** Create Sponsor-Ready PDF Export from Executive Sponsor Brief

**Why it matters:** The Executive Sponsor Brief is well-written markdown, but sponsors receive and forward PDFs, not GitHub markdown files. The first-value-report endpoint exists for run-specific artifacts, but there is no standalone "what is ArchLucid and why should you care" PDF that a sales engineer can email to a VP of Engineering before a first meeting.

**Expected impact:** Directly improves Executive Value Visibility (+4-6 pts), Marketability (+2-3 pts), Decision Velocity (+3-4 pts). Weighted readiness impact: +0.3-0.5%.

**Affected qualities:** Executive Value Visibility, Marketability, Decision Velocity, Adoption Friction

**Status:** Fully actionable now.

**Cursor prompt:**

```
Create a sponsor-ready PDF generation endpoint from the Executive Sponsor Brief.

Steps:
1. Add a new API endpoint: `GET /v1/marketing/sponsor-brief.pdf`
   - Policy: Anonymous (no auth required — this is marketing material)
   - Rate-limited under the `fixed` policy
   - Returns `application/pdf` with `Content-Disposition: attachment; filename="ArchLucid-Sponsor-Brief.pdf"`
2. Implementation approach:
   - Read `docs/EXECUTIVE_SPONSOR_BRIEF.md` content
   - Strip the markdown front matter (> Scope: line)
   - Convert to PDF using the same rendering approach as the existing DOCX export pipeline (if a Markdown-to-PDF utility already exists) or use a simple approach:
     - Render markdown to HTML
     - Use the existing report rendering infrastructure to produce a branded PDF
   - Add ArchLucid branding (if a logo/header template exists in the export pipeline)
3. If PDF rendering from markdown is not already available, implement a minimal `MarkdownToPdfRenderer` in `ArchLucid.Application` that:
   - Converts markdown to HTML (using Markdig, which is likely already a dependency)
   - Wraps HTML in a minimal styled template
   - Converts to PDF using an existing PDF library or returns HTML as a fallback
4. Add a unit test that verifies the endpoint returns 200 with `application/pdf` content type.
5. Register the endpoint in the marketing controller alongside existing marketing endpoints.

Acceptance criteria:
- `GET /v1/marketing/sponsor-brief.pdf` returns a PDF
- PDF contains the full Executive Sponsor Brief content
- Response includes appropriate Content-Disposition header
- Rate-limited
- Anonymous access (no auth)
- Unit test exists

Constraints:
- Prefer reusing existing rendering infrastructure over adding new PDF libraries
- If PDF rendering is too complex, fall back to DOCX format (matching existing export patterns)
- Do not modify the Executive Sponsor Brief content
- Keep the rendering simple — this is a text document, not a designed brochure

What not to change:
- Executive Sponsor Brief content
- Existing marketing endpoints
- DOCX export pipeline behavior
```

---

### Improvement 6: Strengthen the Contoso Synthetic Case Study with Realistic Metrics

**Title:** Strengthen the Contoso Synthetic Case Study with Realistic Metrics

**Why it matters:** The synthetic case study for "Contoso Retail" exists but lacks the specific before/after metrics that make a case study compelling. Without a real customer reference (V1.1-deferred), a well-populated synthetic case study with realistic (not obviously fake) metrics is the best available proxy for proof-of-ROI.

**Expected impact:** Directly improves Proof-of-ROI Readiness (+4-6 pts), Marketability (+2-3 pts), Executive Value Visibility (+2-3 pts). Weighted readiness impact: +0.3-0.5%.

**Affected qualities:** Proof-of-ROI Readiness, Marketability, Executive Value Visibility, Differentiability

**Status:** Fully actionable now.

**Cursor prompt:**

```
Strengthen the Contoso Retail synthetic case study with realistic before/after metrics.

Steps:
1. Read `docs/go-to-market/SYNTHETIC_CASE_STUDY_CONTOSO_RETAIL.md` fully.
2. Enhance the case study with specific, realistic metrics aligned with the ROI model in `docs/go-to-market/ROI_MODEL.md`:
   - Before ArchLucid: "Architecture review packages took 40+ hours to assemble. Manual evidence gathering added 8-12 hours per governance review."
   - After ArchLucid: "Review package assembly reduced to ~6 hours (committed manifest + auto-generated artifacts). Evidence generation automated for 70% of governance checklist items."
   - Delta: "32+ hours saved per architecture review cycle. At 12 reviews per year, ~384 architect-hours recovered annually."
   - Cost framing: "At $150/hr blended architect rate, ~$57,600 annual savings for one team of 4 architects."
3. Add a "Pilot timeline" section:
   - Week 1: Configured ArchLucid, ran first architecture brief through the system
   - Week 2: Compared ArchLucid output to manual review package for same architecture
   - Week 3-4: Ran 3 additional architecture reviews through ArchLucid; measured cycle time
4. Add a "What the sponsor said" quote (synthetic but realistic):
   - "We went from 3 days of manual preparation to same-day architecture packages. The governance evidence was cleaner than what we were producing manually."
5. Ensure the metrics are internally consistent with `PRICING_PHILOSOPHY.md` pricing justification.
6. Add a clear "Synthetic case study" disclaimer at the top: "This case study uses a synthetic scenario based on the ArchLucid ROI model assumptions. It is not a real customer engagement."
7. Cross-reference from `docs/go-to-market/POSITIONING.md` if not already linked.

Acceptance criteria:
- Case study contains specific before/after hour estimates
- Metrics are consistent with ROI_MODEL.md assumptions
- Pilot timeline section exists
- Synthetic sponsor quote included
- "Synthetic" disclaimer is prominent
- No broken links

Constraints:
- Do not claim these are real customer metrics
- Keep metrics conservative and internally consistent with ROI model
- Do not invent technology-specific details that do not match ArchLucid's actual capabilities
- Maintain honest tone — explicitly label as synthetic

What not to change:
- ROI model assumptions
- Pricing numbers (they live in PRICING_PHILOSOPHY.md only)
- Other case study or reference-customer files
```

---

### Improvement 7: Add Audit Search EventId Tie-Breaking for Keyset Cursor

**Title:** Add Audit Search EventId Tie-Breaking for Keyset Cursor

**Why it matters:** The audit search keyset cursor uses `OccurredUtc` only. For high-volume audit environments where multiple events share the same timestamp, this can produce duplicate or missing rows during pagination. The fix is to add `EventId` as a secondary sort key, using the existing `IX_AuditEvents_OccurredUtc_EventId` index (migration 109).

**Expected impact:** Directly improves Auditability (+3-4 pts), Data Consistency (+2-3 pts), Reliability (+1-2 pts). Weighted readiness impact: +0.1-0.2%.

**Affected qualities:** Auditability, Data Consistency, Reliability, Scalability

**Status:** Fully actionable now.

**Cursor prompt:**

```
Add EventId tie-breaking to the audit search keyset cursor for stable pagination.

Steps:
1. Find the audit search repository method that implements keyset pagination (likely in `ArchLucid.Persistence` or a `Data.Audit` sub-namespace). Look for the SQL query that uses `WHERE OccurredUtc < @beforeUtc` or similar cursor logic.
2. Modify the keyset cursor to use composite ordering: `ORDER BY OccurredUtc DESC, EventId DESC` and the WHERE clause to: `WHERE (OccurredUtc < @beforeUtc) OR (OccurredUtc = @beforeUtc AND EventId < @beforeEventId)`.
3. Update the cursor token format to include both `OccurredUtc` (ticks) and `EventId` (GUID or int). If the current cursor is UTC-ticks-only, extend it to `{utcTicks}_{eventId}` format with backward compatibility (if only ticks are provided, fall back to timestamp-only filtering).
4. Update the API controller that handles `GET /v1/audit/search` to:
   - Parse the extended cursor format
   - Pass both values to the repository
   - Return the new cursor format in the response for the next page
5. Update the operator UI audit page if it constructs cursor values client-side.
6. Add tests:
   - Repository test: insert 5 audit events with the same OccurredUtc timestamp, verify keyset pagination returns all 5 across pages without duplicates
   - API integration test: same scenario via HTTP, verify no duplicates in paginated results
   - Both tests should carry `[Trait("Suite", "Core")]`
7. Update `docs/library/AUDIT_COVERAGE_MATRIX.md` to remove the "tie-breaking limitation" note and document the composite cursor.

Acceptance criteria:
- Keyset cursor uses `(OccurredUtc, EventId)` composite ordering
- Same-timestamp audit events paginate without duplicates or omissions
- Backward-compatible with old cursor format (ticks-only)
- Repository and API tests pass
- Documentation updated
- Existing audit tests pass

Constraints:
- Do not change the `IX_AuditEvents_OccurredUtc_EventId` index (migration 109 already has it)
- Do not break existing cursor-based API consumers (backward compatibility required)
- Do not change the audit event insertion path
- Keep the cursor token format simple and URL-safe

What not to change:
- Audit event schema
- Audit event insertion logic
- Index definitions (already exist)
- Other API endpoints
```

---

### Improvement 8: Create a Sample Policy Pack for TOGAF ADM Gate Mapping

**Title:** Create a Sample Policy Pack for TOGAF ADM Phase Gate Mapping

**Why it matters:** Policy packs are governance building blocks but all current examples are ArchLucid-native rule sets. Enterprise architects who use TOGAF or similar frameworks need to see how ArchLucid governance maps to their existing review processes. A sample TOGAF-aligned policy pack demonstrates this mapping and reduces the evaluation burden for architecture practice leaders.

**Expected impact:** Directly improves Policy and Governance Alignment (+4-6 pts), Template and Accelerator Richness (+5-7 pts), Adoption Friction (+2-3 pts). Weighted readiness impact: +0.2-0.3%.

**Affected qualities:** Policy and Governance Alignment, Template and Accelerator Richness, Adoption Friction, Differentiability

**Status:** Fully actionable now.

**Cursor prompt:**

```
Create a sample policy pack that maps ArchLucid governance to TOGAF ADM phase gates.

Steps:
1. Review the existing policy pack structure in the codebase — find where policy packs are defined (likely JSON/config under governance or a seed/template directory). Check `ArchLucid.Decisioning` or `ArchLucid.Application` for policy pack models.
2. Create a sample policy pack JSON file at `templates/policy-packs/togaf-adm-phase-gates.json` (or the appropriate location based on existing pack format).
3. The policy pack should define rules that map to TOGAF ADM phases:
   - **Phase B (Business Architecture):** Require finding coverage for business context (at least one finding with category matching business/stakeholder scope)
   - **Phase C (Information Systems Architecture):** Require topology findings with data flow coverage
   - **Phase D (Technology Architecture):** Require compliance findings with infrastructure coverage
   - **Phase E (Opportunities & Solutions):** Require cost findings with optimization recommendations
   - **Phase G (Implementation Governance):** Require all severity=Critical findings to be resolved before commit (maps to pre-commit gate)
4. Add a `README.md` in `templates/policy-packs/` explaining:
   - What TOGAF ADM phase gates are (1 paragraph)
   - How this pack maps ArchLucid findings to TOGAF phases
   - How to import/apply the pack via API or UI
   - That this is a starter template — customers should customize thresholds and categories for their organization
5. If the pack format requires specific IDs or metadata, align with existing pack examples.
6. Add a test that validates the JSON is parseable by the policy pack deserializer.

Acceptance criteria:
- Policy pack JSON file exists and is valid
- Maps to at least 5 TOGAF ADM phases
- README explains the mapping and customization guidance
- JSON parses successfully with the existing policy pack model
- No new code dependencies

Constraints:
- Use the existing policy pack schema — do not invent a new format
- Keep rule definitions achievable with the current finding categories and severity levels
- Do not claim TOGAF certification or official TOGAF endorsement
- Label as "sample / starter template"

What not to change:
- Policy pack engine code
- Existing policy packs
- Governance workflow behavior
- Finding category definitions
```

---

## 10. Pending Questions for Later

### Improvement 1 (Reference-Case Evaluation)
- What are the expected output structures for each agent type (Topology, Cost, Compliance, Critic) that should be used as golden baselines? Are there existing test fixtures that represent "good" agent output?

### Improvement 5 (Sponsor PDF)
- Is there a preferred PDF rendering library already in the solution, or should Markdig → HTML → PDF be the approach? Does the DOCX export pipeline use a library that can also produce PDF?
- Should the sponsor brief PDF include the ArchLucid logo? If so, where is the logo asset?

### Improvement 6 (Contoso Case Study)
- Are the ROI model hour estimates (40+ hours for manual review, ~6 hours with ArchLucid) reasonable based on your experience with real architecture review processes? Should these numbers be adjusted?
- Should the synthetic case study focus on a specific industry vertical (retail as written) or be industry-neutral?

### Improvement 8 (TOGAF Policy Pack)
- Which finding categories currently exist in the decisioning layer? The pack mapping depends on knowing what categories the finding engines actually produce.
- Is there a preferred threshold for "all Critical findings resolved" in the pre-commit gate, or is severity threshold already configurable per policy pack rule?

### General
- Has any internal dogfooding been done (using ArchLucid to review ArchLucid's own architecture)? If so, what were the results?
- Is there a target date for the first real customer pilot? The readiness assessment would benefit from a timeline anchor.
- For the SLA summary — is there a committed uptime target (e.g., 99.9%) that should be published, or is this still being determined?
