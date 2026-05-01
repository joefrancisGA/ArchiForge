> **Scope:** Independent first-principles quality assessment of ArchLucid — weighted readiness score, prioritized weaknesses, monetization/enterprise/engineering blockers, and improvement prompts for Cursor.

# ArchLucid Assessment – Weighted Readiness 67.89%

**Date:** 2026-05-01  
**Method:** First-principles review of codebase (~4,195 C# source files, 49 projects, 110 Terraform files, 73 UI pages, 39 Playwright specs, 554+ documentation files). No prior assessment referenced.

---

## Executive Summary

### Overall Readiness
ArchLucid scores **67.89%** weighted readiness — a product with strong engineering bones and deep governance/audit discipline, but commercially blocked by an absence of real customers, live payment flow, and third-party trust attestation. The delta between "functionally impressive" and "purchasable" remains the critical gap.

### Commercial Picture
The pricing model is thoughtful and value-anchored. Documentation reads like a mature company. However, zero revenue, no reference customers, placeholder Stripe URLs, and an Azure-only platform constraint limit addressable market to ~30% of enterprise architecture teams. Time-to-value for a *non-guided* customer is measured in days, not minutes — a severe adoption friction problem for a product without a sales team.

### Enterprise Picture
Security architecture is legitimately strong (RLS, RBAC, fail-closed auth, rate limiting, content safety, STRIDE model, ZAP/Schemathesis in CI). Governance depth (approval workflows, segregation of duties, policy packs) exceeds most competitors. Missing: SOC 2 attestation, real pen-test report, and ITSM connectors that enterprise buyers treat as table stakes.

### Engineering Picture
Architecturally coherent, well-tested (hundreds of test classes, property-based testing, mutation testing, chaos engineering), observable (50+ custom metrics), and deployable (13 Terraform roots, CI/CD, container-first). Risks: data consistency relies on detection-not-prevention for orphans, the UI is a thin shell over a deep API surface, and the system's correctness depends heavily on LLM output quality that is only heuristically validated.

---

## Weighted Quality Assessment

### Scoring Table

| # | Quality | Score | Weight | Weighted | Deficiency Signal |
|---|---------|-------|--------|----------|-------------------|
| 1 | Marketability | 49 | 8 | 3.84 | 4.00 |
| 2 | Time-to-Value | 53 | 7 | 3.64 | 3.22 |
| 3 | Adoption Friction | 49 | 6 | 2.88 | 3.00 |
| 4 | Proof-of-ROI Readiness | 57 | 5 | 2.79 | 2.11 |
| 5 | Correctness | 75 | 4 | 2.94 | 0.98 |
| 6 | Executive Value Visibility | 67 | 4 | 2.63 | 1.29 |
| 7 | Differentiability | 78 | 4 | 3.06 | 0.86 |
| 8 | Architectural Integrity | 85 | 3 | 2.50 | 0.44 |
| 9 | Security | 81 | 3 | 2.38 | 0.56 |
| 10 | Traceability | 87 | 3 | 2.56 | 0.38 |
| 11 | Usability | 61 | 3 | 1.79 | 1.15 |
| 12 | Workflow Embeddedness | 53 | 3 | 1.56 | 1.38 |
| 13 | Trustworthiness | 64 | 3 | 1.88 | 1.06 |
| 14 | Reliability | 78 | 2 | 1.53 | 0.43 |
| 15 | Data Consistency | 75 | 2 | 1.47 | 0.49 |
| 16 | Maintainability | 81 | 2 | 1.59 | 0.37 |
| 17 | Explainability | 80 | 2 | 1.57 | 0.39 |
| 18 | AI/Agent Readiness | 73 | 2 | 1.43 | 0.53 |
| 19 | Azure Compat & SaaS Deploy | 86 | 2 | 1.69 | 0.27 |
| 20 | Decision Velocity | 64 | 2 | 1.25 | 0.71 |
| 21 | Commercial Packaging Readiness | 66 | 2 | 1.29 | 0.67 |
| 22 | Auditability | 89 | 2 | 1.75 | 0.22 |
| 23 | Policy & Governance Alignment | 84 | 2 | 1.65 | 0.31 |
| 24 | Compliance Readiness | 66 | 2 | 1.29 | 0.67 |
| 25 | Procurement Readiness | 62 | 2 | 1.22 | 0.75 |
| 26 | Interoperability | 51 | 2 | 1.00 | 0.96 |
| 27 | Stickiness | 67 | 1 | 0.66 | 0.32 |
| 28 | Template & Accelerator Richness | 65 | 1 | 0.64 | 0.34 |
| 29 | Accessibility | 74 | 1 | 0.73 | 0.25 |
| 30 | Customer Self-Sufficiency | 60 | 1 | 0.59 | 0.39 |
| 31 | Change Impact Clarity | 75 | 1 | 0.74 | 0.25 |
| 32 | Availability | 79 | 1 | 0.78 | 0.21 |
| 33 | Performance | 75 | 1 | 0.74 | 0.25 |
| 34 | Scalability | 71 | 1 | 0.70 | 0.28 |
| 35 | Supportability | 78 | 1 | 0.76 | 0.22 |
| 36 | Manageability | 75 | 1 | 0.74 | 0.25 |
| 37 | Deployability | 81 | 1 | 0.79 | 0.19 |
| 38 | Observability | 85 | 1 | 0.83 | 0.15 |
| 39 | Testability | 86 | 1 | 0.84 | 0.14 |
| 40 | Modularity | 82 | 1 | 0.80 | 0.18 |
| 41 | Extensibility | 78 | 1 | 0.76 | 0.22 |
| 42 | Evolvability | 75 | 1 | 0.74 | 0.25 |
| 43 | Documentation | 87 | 1 | 0.85 | 0.13 |
| 44 | Azure Ecosystem Fit | 86 | 1 | 0.84 | 0.14 |
| 45 | Cognitive Load | 54 | 1 | 0.53 | 0.45 |
| 46 | Cost-Effectiveness | 67 | 1 | 0.66 | 0.32 |

**Total weighted score: 67.89 / 100**

---

### Quality Details (ordered by urgency — weighted deficiency)

---

#### 1. Marketability — Score: 49 | Weight: 8 | Deficiency: 4.00

**Justification:** Zero customers, zero revenue, placeholder Stripe URLs, no SOC 2, no published case study. The product cannot be purchased through a self-service flow today. Azure-only constraint excludes >50% of market. No marketplace listing is live.

**Tradeoffs:** Building product depth before commercial infrastructure is defensible for a technical founder — but the gap is now existential for revenue timelines.

**Recommendations:**
- Complete Stripe payment link so self-serve purchases are real
- Close one design partner before any further feature work
- Publish the Azure Marketplace SaaS offer

**Fixability:** V1 (commercial packaging work, not engineering)

---

#### 2. Time-to-Value — Score: 53 | Weight: 7 | Deficiency: 3.22

**Justification:** First meaningful value (a committed golden manifest) requires: provisioning infrastructure (SQL + Container Apps or local docker compose), configuring auth, understanding a seven-step wizard, and either waiting for simulator output or configuring Azure OpenAI credentials. Self-serve trial exists in engineering but the signup-to-value path takes hours, not minutes. No hosted sandbox where a prospect can click and see real output immediately.

**Tradeoffs:** The system's depth (audit, RLS, governance) adds setup weight that can't be removed without hollowing the product.

**Recommendations:**
- Create a hosted demo environment with pre-seeded runs (partially exists at `/demo/preview` — make it the front door)
- Reduce minimum viable deployment to a single container with SQLite or in-memory for evaluation
- Add a "see results in 60 seconds" onboarding path

**Fixability:** V1 for demo path; V1.1 for simplified deployment

---

#### 3. Adoption Friction — Score: 49 | Weight: 6 | Deficiency: 3.00

**Justification:** The product requires: .NET runtime knowledge for self-hosting, SQL Server, Azure OpenAI subscription (for real mode), Terraform fluency (for production), and learning a new seven-step wizard. No inbound data connectors (Terraform state, ArchiMate, CMDB import) means customers must re-describe their architecture from scratch. Entra-only SSO blocks non-Microsoft shops.

**Tradeoffs:** Azure-native positioning is a deliberate bet — but it compounds adoption friction for the majority of the market.

**Recommendations:**
- Ship a Terraform state import connector (highest leverage inbound data source)
- Add generic OIDC support (beyond Entra)
- Create a one-command hosted trial URL that bypasses infrastructure setup entirely

**Fixability:** V1 for OIDC; V1.1 for import connectors

---

#### 4. Proof-of-ROI Readiness — Score: 57 | Weight: 5 | Deficiency: 2.11

**Justification:** An ROI model exists with solid math ($294K savings for 6-architect team). Baseline capture fields exist on registration. Pilot scorecard exists. Value report endpoint exists. However: zero real data validates these numbers. No customer has completed a pilot. The ROI narrative is entirely theoretical.

**Tradeoffs:** The ROI framework is more developed than most pre-revenue startups, but without a single data point from reality, it remains a hypothesis.

**Recommendations:**
- Complete one guided pilot to collect real time-to-manifest data
- Build a comparison dashboard showing "before ArchLucid" vs "with ArchLucid" metrics from the first real customer
- Publish synthetic case study findings as a placeholder until real data exists (Contoso exists but is marked synthetic)

**Fixability:** V1.1 (first customer acquisition is deferred to V1.1; V1 work focuses on synthetic case study polish and ROI framework readiness)

---

#### 5. Executive Value Visibility — Score: 67 | Weight: 4 | Deficiency: 1.29

**Justification:** Executive sponsor brief exists, ROI bulletin exists, value report endpoint exists, pilot scorecard exists, "email run to sponsor" feature exists. The infrastructure for executive communication is built. Gap: no executive has actually seen these artifacts. The executive view is untested with real buyers. Board pack PDF endpoint exists but content is synthetic.

**Tradeoffs:** Investing in executive artifacts before having executives to show them to is a bet on sales readiness.

**Recommendations:**
- Validate the executive sponsor brief with 2-3 real VP Eng/CTO prospects
- Ensure the value report page loads in <3 seconds with real data and can be shared as a URL without login

**Fixability:** V1 (validation only, not engineering)

---

#### 6. Correctness — Score: 75 | Weight: 4 | Deficiency: 0.98

**Justification:** The system's "correctness" has strong structural guardrails: (1) Simulator mode for deterministic testing (2) Golden corpus validation tests (3) Agent output structural completeness metrics (4) Faithfulness heuristic (5) Semantic quality score (6) Property-based tests with FsCheck (7) Hundreds of integration tests. The pipeline mechanics (ingest → graph → findings → decisioning → artifacts) are well-tested for data flow correctness. However: no systematic evaluation against ground-truth architecture reviews exists. The product cannot prove its *analytical* findings are correct in the general case — only that its findings are structurally well-formed and self-consistent.

**Tradeoffs:** This is an inherent limitation of LLM-based systems. The mitigation (explainability trace, confidence scores, human review workflow) is appropriate but does not eliminate the correctness risk.

**Recommendations:**
- Build a ground-truth evaluation corpus from real architecture reviews (requires customer data)
- Add a "finding precision/recall" metric from pilot feedback loops
- Make the existing product learning signals (trusted/rejected/revised) visible in aggregate as a correctness proxy

**Fixability:** V1.1 (requires real data)

---

#### 7. Usability — Score: 61 | Weight: 3 | Deficiency: 1.15

**Justification:** 73 page routes exist in the UI. Seven-step wizard is complete. Progressive disclosure exists. Role-aware shaping exists. However: the UI is self-described as a "thin shell," competitive landscape doc notes "Ardoq has mature, polished graph and scenario visualization. ArchLucid's UI is functional but self-described as a 'thin shell.'" No design system. Cognitive load is high (45 score on its own metric). 554+ docs suggest feature surface far exceeds what a single operator can navigate without training.

**Tradeoffs:** Deep API with thin UI is defensible for a developer-tool company — but ArchLucid's buyer persona includes non-technical executive architects.

**Recommendations:**
- Conduct 3 usability tests with target-persona users on the critical path (create run → review findings → commit)
- Reduce the number of visible navigation items at first login by 50%
- Add contextual guidance (already partially exists as LayerHeader) on the 5 most-used pages

**Fixability:** V1 for disclosure reduction; V1.1 for design system

---

#### 8. Workflow Embeddedness — Score: 53 | Weight: 3 | Deficiency: 1.38

**Justification:** REST API, CloudEvents webhooks, Service Bus, GitHub Actions integration, Azure DevOps pipeline integration, Teams notifications, and SCIM exist. However: no Jira, no ServiceNow, no Confluence, no Slack — the four tools most enterprise architecture teams use daily. Customers must build their own bridges via Logic Apps or webhook consumers. The product sits adjacent to workflows rather than inside them.

**Tradeoffs:** V1.1 commits ServiceNow, Jira, and Confluence — but V1 ships without them.

**Recommendations:**
- Accelerate ServiceNow connector to V1 timeframe (highest ITSM ask)
- Ensure the CloudEvents → Jira bridge recipe is tested end-to-end and documented with screenshots
- Add a Slack webhook bridge recipe (same pattern as Teams, minimal code)

**Fixability:** V1.1 for first-party connectors; V1 for improved recipes

---

#### 9. Trustworthiness — Score: 64 | Weight: 3 | Deficiency: 1.06

**Justification:** The product has: STRIDE threat model, RLS, fail-closed auth, content safety, prompt redaction, rate limiting. The product lacks: SOC 2 Type II attestation, published pen-test report (engagement awarded but not delivered), real customer testimonials, any social proof. An enterprise security reviewer will ask "who else runs this in production?" and the answer is "no one." Self-assessment is not third-party attestation.

**Tradeoffs:** SOC 2 costs $30-80K and takes 3-6 months. The pen test is in flight (Aeronova, kickoff 2026-05-06). These are on track but not delivered.

**Recommendations:**
- Complete pen test engagement and publish redacted summary
- Begin SOC 2 Type II engagement immediately after pen test delivery
- Add "trust timeline" visualization showing when each attestation will be available

**Fixability:** Blocked on vendor timelines (pen test in flight)

---

#### 10. Differentiability — Score: 78 | Weight: 4 | Deficiency: 0.86

**Justification:** The competitive landscape analysis identifies a genuine gap: "No current competitor delivers all three [AI analysis + auditable decisions + governance workflow]." The product's combination of multi-agent pipeline, explainability traces, provenance graph, and governance gates is unique. The hard comparison table (7 claims, all verified against codebase) is defensible. Gap: this differentiation is invisible to a buyer until they deploy and run the system — no hosted demo makes the differentiation tangible in < 5 minutes.

**Tradeoffs:** Deep technical differentiation vs. surface-level demo appeal.

**Recommendations:**
- Build a 90-second video showing the unique value chain: request → agent analysis → traced findings → governance gate → committed manifest
- Ensure the `/demo/preview` route shows real traced findings with provenance, not just structure

**Fixability:** V1

---

#### 11. Interoperability — Score: 51 | Weight: 2 | Deficiency: 0.96

**Justification:** No inbound data connectors (cannot import from Terraform state, ArchiMate, CMDB, cloud APIs). No Structurizr import. No ArchiMate import. Azure-only cloud analysis. API is OpenAPI-documented and well-structured. .NET client SDK generated from spec. AsyncAPI for events. But the product is fundamentally isolated from the data sources enterprise architecture teams already have.

**Tradeoffs:** Building outbound integration (webhooks, events) before inbound (import) prioritizes governance workflow over data onboarding.

**Recommendations:**
- Ship `terraform show -json` → ArchLucid context connector as first inbound connector
- Define an ArchiMate XML import specification (even if implementation is V1.1)
- Expose a "bulk context upload" API endpoint for customers who want to push data from any source

**Fixability:** V1 for Terraform import; V1.1 for ArchiMate

---

#### 12. Decision Velocity — Score: 64 | Weight: 2 | Deficiency: 0.71

**Justification:** The product accelerates architecture decision-making (that's its purpose), but the decision velocity for the *buyer* to decide to purchase is impaired by: high setup cost, no immediate demo, no self-serve payment, and a 6-week guided pilot process. Enterprise procurement timelines will be 3-6 months minimum.

**Tradeoffs:** Enterprise sales cycles are inherently slow — but self-serve adoption can happen in parallel.

**Recommendations:**
- Enable Team-tier Stripe self-checkout so small teams can buy without a sales call
- Reduce the trial-to-paid conversion path to under 14 days

**Fixability:** V1

---

#### 13. Compliance Readiness — Score: 66 | Weight: 2 | Deficiency: 0.67

**Justification:** SOC 2 self-assessment exists (internal ownership). CAIQ Lite pre-fill exists. SIG Core pre-fill exists. Pen test is in flight. Audit trail is durable and append-only. 78 typed audit events. However: no actual SOC 2 report, no completed pen test deliverable, no ISO 27001. For regulated industries (financial services, healthcare — the identified best-fit scenarios), these are hard blockers.

**Tradeoffs:** Compliance attestation before revenue is expensive but necessary for the target market.

**Recommendations:**
- Complete and publish pen-test redacted summary (in flight)
- Begin SOC 2 Type II readiness (auditor selection needed)
- Create compliance timeline roadmap for prospect conversations

**Fixability:** Partially V1 (pen test); SOC 2 is a 6-month horizon

---

#### 14. Procurement Readiness — Score: 62 | Weight: 2 | Deficiency: 0.75

**Justification:** Procurement evidence pack index exists. Order form template exists. MSA template exists. Trust center with assurance activity table exists. NDA-gated pen test summary path defined. However: procurement pack is "static ZIP" — not a live trust portal. No VSA/vendor assessment completed by a real buyer. No actual signed order form. The procurement materials are well-structured templates with no real-world validation.

**Tradeoffs:** Having templates ready is better than not — but they're untested against real procurement processes.

**Recommendations:**
- Run the procurement pack through a mock security review with a CISO contact
- Add a "request procurement pack" button to the trust center page that doesn't require email
- Ensure all templates have date stamps and version numbers

**Fixability:** V1

---

#### 15. Commercial Packaging Readiness — Score: 66 | Weight: 2 | Deficiency: 0.67

**Justification:** Three tiers (Team/Professional/Enterprise) well-defined. Feature gates documented. Locked prices documented with re-rate plan. Stripe checkout URL is a placeholder. Azure Marketplace offer is documented but not published. Metering infrastructure exists (Prometheus counters, run tracking). Gap: no live billing, no live marketplace listing, no tested upgrade/downgrade path.

**Tradeoffs:** Pricing documentation maturity far exceeds implementation maturity.

**Recommendations:**
- Replace Stripe placeholder URL with real Payment Link
- Test the full trial → Team → Professional upgrade path end-to-end
- Publish the Azure Marketplace SaaS offer (even as private preview)

**Fixability:** V1

---

#### 16. AI/Agent Readiness — Score: 73 | Weight: 2 | Deficiency: 0.53

**Justification:** Four agent types (Topology, Cost, Compliance, Critic). Multi-vendor LLM via `ILlmProvider` with fallback chain. Simulator mode for deterministic testing. Content safety guard. Prompt redaction. Circuit breaker with audit callbacks. Agent output quality gate. However: agent quality is heuristically evaluated, not ground-truth validated. No automated regression detection for prompt quality degradation beyond structural completeness. Daily tenant budget tracker exists but LLM cost control is per-tenant, not global.

**Tradeoffs:** Building agent infrastructure (safety, resilience, caching) before agent quality validation is defensible for V1.

**Recommendations:**
- Establish a golden evaluation corpus (10 architecture scenarios with expected findings)
- Add prompt regression detection beyond structural metrics
- Implement automated A/B testing for prompt versions

**Fixability:** V1.1 for eval corpus; V2 for A/B testing

---

#### 17. Cognitive Load — Score: 54 | Weight: 1 | Deficiency: 0.45

**Justification:** 554+ documentation files. 73 UI page routes. Seven-step wizard. Two product layers with sub-navigation groups. 112 API controllers. The mental model required to operate this system is enormous for a V1 product. Progressive disclosure exists but the underlying complexity is still visible through navigation depth and documentation volume.

**Tradeoffs:** Depth of capability creates inherent cognitive load — the question is whether the "on-ramp" is narrow enough.

**Recommendations:**
- Reduce visible documentation entry points to 5 (already exists as FIRST_5_DOCS.md — enforce it)
- Collapse the operator shell to 8 navigation items at first login (pilot-only view)
- Add a "complexity budget" CI check: fail if primary navigation exceeds N items without explicit exception

**Fixability:** V1

---

## Top 10 Most Important Weaknesses

| Rank | Weakness | Impact |
|------|----------|--------|
| 1 | **No paying customers or live billing flow** | Cannot generate revenue; cannot validate product-market fit; cannot prove ROI claims |
| 2 | **No live self-serve trial-to-paid conversion** | Stripe URL is placeholder; Team tier cannot be purchased without manual intervention |
| 3 | **No third-party security attestation delivered** | SOC 2 absent; pen test in flight but not delivered; regulated buyers cannot clear security review |
| 4 | **Azure-only + Entra-only constrains market to ~30%** | >50% of enterprises are AWS/GCP-primary; non-Microsoft IdP shops are blocked |
| 5 | **No inbound data connectors** | Customers must re-describe architecture from scratch; cannot import existing Terraform/CMDB/ArchiMate |
| 6 | **Time from signup to first value is hours, not minutes** | Infrastructure setup, auth config, and wizard completion before any output appears |
| 7 | **No ITSM integration in V1** | Findings stay trapped in ArchLucid instead of flowing to Jira/ServiceNow where teams work |
| 8 | **Agent correctness is unvalidated against ground truth** | No evidence that AI findings are actually correct beyond structural well-formedness |
| 9 | **UI is a thin shell over 112 API controllers** | Non-technical buyer personas (enterprise architects, VPs) encounter functional but undifferentiated UI |
| 10 | **Zero social proof** | No testimonials, no logos, no case studies, no community — buyer must trust documentation alone |

---

## Top 5 Monetization Blockers

| Rank | Blocker | Why It Blocks Revenue |
|------|---------|----------------------|
| 1 | **Stripe checkout is a placeholder** | Cannot collect money from willing buyers |
| 2 | **No reference customer** | Prospects cannot talk to peers; 15% discount cannot be earned; trust gap persists |
| 3 | **$15K guided pilot requires sales motion** | No sales team exists; pilot fee collection requires manual invoicing |
| 4 | **Azure Marketplace not live** | Enterprise buyers with Azure spending commitments cannot route through existing procurement channels |
| 5 | **ROI model is unvalidated** | "Save $294K/year" claim has zero customer evidence; CFOs will discount it to zero |

---

## Top 5 Enterprise Adoption Blockers

| Rank | Blocker | Why It Blocks Enterprise Buyers |
|------|---------|-------------------------------|
| 1 | **No SOC 2 Type II report** | Security review teams cannot approve without third-party attestation for data-handling products |
| 2 | **No ITSM connector (Jira/ServiceNow)** | Enterprise architecture workflows terminate in ITSM systems; ArchLucid findings must bridge manually |
| 3 | **Entra-only SSO** | Organizations on Okta/PingFederate/Auth0 cannot federate identity without custom work |
| 4 | **No inbound data connector** | Enterprises with existing CMDB/Terraform/ArchiMate repositories cannot onboard data automatically |
| 5 | **Single-region Azure deployment only** | Data residency requirements (EU, APAC) cannot be met without multi-region availability |

---

## Top 5 Engineering Risks

| Rank | Risk | Potential Failure Mode |
|------|------|----------------------|
| 1 | **LLM output quality degradation** | Model version changes (Azure OpenAI updates) silently reduce finding quality; no automated regression gate beyond structural metrics |
| 2 | **Data consistency orphan detection is reactive, not preventive** | Orphaned rows are detected by probe and optionally quarantined — but the write path doesn't prevent them; cascade failures could corrupt the authority chain |
| 3 | **Single-tenant SQL under high concurrency** | Run commits use optimistic concurrency (RowVersionStamp) but the golden manifest commit path could deadlock under parallel tenant operations on shared SQL |
| 4 | **Content safety bypass in non-production** | `NullContentSafetyGuard` in development means prompt injection attacks are only caught in production-like hosts; staging gaps could mask real vulnerabilities |
| 5 | **Infrastructure complexity (13 Terraform roots)** | Deployment surface area is enormous for a single-person operation; misconfiguration in one root can cascade silently to dependent roots |

---

## Most Important Truth

**ArchLucid is an architecture assessment product that has never assessed a real customer's architecture.** Every capability, metric, ROI claim, and pilot model is built on synthetic data and simulator output. Until one real enterprise architect uses the system on their real architecture and calls the result "useful," the product's value proposition is a hypothesis, not a fact. The single highest-leverage action is to get one real user to complete one real run and provide honest feedback — everything else is optimization of an unvalidated assumption.

---

## Top Improvement Opportunities

### Improvement 1: V1.1 — Close First Design Partner Customer

**Title:** V1.1 — Close first design partner and complete guided pilot  
**Why it matters:** Validates product-market fit, generates real ROI data, unlocks reference discount gate, provides testimonial content, and proves agent correctness against real architecture.  
**Expected impact:** Directly improves Marketability (+15-20 pts), Proof-of-ROI (+20 pts), Trustworthiness (+10 pts). Weighted readiness impact: +2.0-3.0%.  
**Affected qualities:** Marketability, Proof-of-ROI Readiness, Trustworthiness, Executive Value Visibility  
**V1.1 scope:** Requires founder sales activity, prospect identification, and contractual agreement — cannot be executed by Cursor and is explicitly out of scope for V1.  
**Information needed (V1.1 planning):** Target prospect list, outreach strategy, and timeline for first conversations.

---

### Improvement 2: Activate Stripe Self-Serve Checkout

**Title:** Activate Stripe self-serve checkout for Team tier  
**Why it matters:** Removes the single biggest monetization blocker — buyers literally cannot pay today. Unblocks self-serve revenue and removes the "−10% self-serve discount" from the price stack.  
**Expected impact:** Directly improves Marketability (+8-10 pts), Commercial Packaging Readiness (+10-12 pts), Decision Velocity (+5 pts). Weighted readiness impact: +1.2-1.8%.  
**Affected qualities:** Marketability, Commercial Packaging Readiness, Decision Velocity, Time-to-Value  
**Whether actionable now:** YES — fully actionable

**Cursor Prompt:**

```
Complete the Stripe self-serve checkout integration for the Team tier.

Context:
- `docs/go-to-market/PRICING_PHILOSOPHY.md` contains a placeholder URL: `"teamStripeCheckoutUrl": "https://checkout.stripe.com/placeholder-replace-before-launch"`
- The trial-to-paid E2E test exists: `archlucid-ui/e2e/live-api-trial-end-to-end.spec.ts`
- `ArchLucid.Api/Controllers/Billing/BillingCheckoutController.cs` exists
- `ArchLucid.Api/Controllers/Billing/BillingStripeWebhookController.cs` exists

Tasks:
1. In `ArchLucid.Api/Controllers/Billing/BillingCheckoutController.cs`, ensure the checkout session creation endpoint creates a real Stripe Checkout Session (not a placeholder redirect) using the `Billing:Stripe:PriceIdPro` config key pattern but for Team tier (`Billing:Stripe:PriceIdTeam`).
2. Add configuration key `Billing:Stripe:PriceIdTeam` to `appsettings.json` (empty string default — operators must configure).
3. Add startup configuration validation in `ArchLucidConfigurationRules` that warns (not errors) when `Billing:Stripe:PriceIdTeam` is empty in production-like environments.
4. Update `archlucid-ui/src/app/(marketing)/pricing/page.tsx` to use a real checkout redirect (POST to `/v1/billing/checkout/team`) instead of linking to the placeholder URL.
5. Add a unit test in `ArchLucid.Api.Tests` that verifies the checkout endpoint returns 400 when the Stripe Price ID is not configured.

Acceptance criteria:
- Team tier "Subscribe" button on pricing page initiates a Stripe Checkout session
- Missing Stripe configuration produces a startup warning (not crash)
- E2E test can be extended to hit the endpoint (returns 400 without real Stripe config — that's correct)

Constraints:
- Do not hard-code any Stripe Price IDs or API keys in source
- Do not modify the pricing philosophy doc numbers
- Do not change Professional or Enterprise tier flows
- Do not remove or modify the existing webhook controller

Impact: Directly improves Marketability (+8-10 pts), Commercial Packaging (+10-12 pts), Decision Velocity (+5 pts). Weighted readiness impact: +1.2-1.8%.
```

---

### Improvement 3: Create Hosted Demo with Pre-Seeded Output

**Title:** Create instant-access hosted demo with pre-seeded run output  
**Why it matters:** A prospect should see real ArchLucid output (traced findings, provenance, manifest) within 60 seconds of landing on the site. Currently they must deploy infrastructure first. This is the #1 time-to-value fix.  
**Expected impact:** Directly improves Time-to-Value (+12-15 pts), Adoption Friction (+8-10 pts), Executive Value Visibility (+5 pts). Weighted readiness impact: +1.5-2.0%.  
**Affected qualities:** Time-to-Value, Adoption Friction, Executive Value Visibility, Marketability  
**Whether actionable now:** YES — fully actionable

**Cursor Prompt:**

```
Enhance the existing demo preview route to serve as the primary prospect entry point.

Context:
- `/demo/preview` route exists at `archlucid-ui/src/app/(marketing)/demo/preview/page.tsx`
- `ArchLucid.Api/Controllers/Demo/DemoViewerController.cs` exists
- `ArchLucid.Api/Controllers/Demo/DemoExplainController.cs` exists
- Demo seed data exists (referenced in PILOT_GUIDE.md)
- Marketing route `/see-it` exists at `archlucid-ui/src/app/(marketing)/see-it/page.tsx`
- Marketing route `/live-demo` exists at `archlucid-ui/src/app/(marketing)/live-demo/page.tsx`

Tasks:
1. In `archlucid-ui/src/app/(marketing)/see-it/page.tsx`, ensure the page prominently links to `/demo/preview` with a CTA like "See a real architecture review" and clearly labels it as no-signup-required.
2. In the demo preview page, add a guided annotation layer (3-4 callout tooltips) that highlights: (a) the traced findings with evidence, (b) the provenance graph link, (c) the governance gate result, (d) the manifest download.
3. Add a "Try with your own architecture" CTA at the bottom of the demo preview that links to `/signup`.
4. Ensure the demo preview loads without authentication (verify DemoViewerController allows anonymous access).
5. Add a Playwright test in `archlucid-ui/e2e/` that verifies the demo preview page loads, shows finding cards, and the CTA links work.

Acceptance criteria:
- A visitor to `/see-it` can reach a full demo run view in one click
- Demo preview shows real traced findings, provenance link, and manifest summary without login
- Page load time < 3 seconds (no heavy API calls beyond the demo seed endpoint)
- Guided annotations highlight the 4 key value propositions

Constraints:
- Do not modify the demo seed data format
- Do not require authentication for the demo viewer
- Do not add new API endpoints — use existing demo controllers
- Do not change the operator shell layout or navigation

Impact: Directly improves Time-to-Value (+12-15 pts), Adoption Friction (+8-10 pts), Executive Value Visibility (+5 pts). Weighted readiness impact: +1.5-2.0%.
```

---

### Improvement 4: Add Generic OIDC SSO Support

**Title:** Add generic OIDC authentication provider support beyond Entra ID  
**Why it matters:** Entra-only SSO blocks adoption at non-Microsoft-stack enterprises (Okta, Auth0, PingFederate). This is called out as competitive weakness #5 and is a hard enterprise adoption blocker.  
**Expected impact:** Directly improves Adoption Friction (+6-8 pts), Interoperability (+8-10 pts), Procurement Readiness (+3-5 pts). Weighted readiness impact: +0.8-1.2%.  
**Affected qualities:** Adoption Friction, Interoperability, Procurement Readiness, Marketability  
**Whether actionable now:** YES — fully actionable

**Cursor Prompt:**

```
Add generic OIDC authentication support alongside existing Entra ID JWT bearer.

Context:
- Auth configuration is in `ArchLucid.Api/appsettings.json` under `ArchLucidAuth`
- Auth modes are: DevelopmentBypass, ApiKey, JwtBearer
- `ArchLucid.Host.Core` registers authorization policies
- `ArchLucidAuthorizationPoliciesExtensions.AddArchLucidAuthorizationPolicies` exists
- Role claims transform via `ArchLucidRoleClaimsTransformation`
- Current JWT validation uses Entra-specific authority/audience from `JwtBearer:*` config keys

Tasks:
1. Add a new auth mode value `GenericOidc` to the `ArchLucidAuth:Mode` configuration (alongside existing `JwtBearer`, `ApiKey`, `DevelopmentBypass`).
2. Add configuration section `Authentication:GenericOidc` with keys: `Authority` (string, required), `ClientId` (string, required), `Audience` (string, optional — defaults to ClientId), `RoleClaimType` (string, optional — defaults to `roles`), `NameClaimType` (string, optional — defaults to `name`).
3. When `ArchLucidAuth:Mode=GenericOidc`, register `AddAuthentication().AddJwtBearer()` using the generic OIDC discovery endpoint (`{Authority}/.well-known/openid-configuration`).
4. Map the configured `RoleClaimType` to ArchLucid role names using the existing `ArchLucidRoleClaimsTransformation` — add a configurable claim-to-role mapping in `Authentication:GenericOidc:RoleMappings` (dictionary of external claim value → ArchLucidRoles value).
5. Add `ArchLucidConfigurationRules` validation: when mode is `GenericOidc`, require `Authority` and `ClientId` to be non-empty.
6. Add unit tests in `ArchLucid.Api.Tests` covering: (a) startup validation fails without Authority, (b) role mapping transforms external claims correctly, (c) GenericOidc mode registers JWT validation with correct authority.
7. Document in `docs/library/SECURITY.md` under a new "Generic OIDC" section.

Acceptance criteria:
- An operator can configure Okta or Auth0 as the OIDC provider with 3 config keys
- Role mapping from external claims to ArchLucid roles works correctly
- Existing Entra JWT path is unchanged when mode is `JwtBearer`
- Startup fails fast with clear error when GenericOidc is configured without Authority

Constraints:
- Do not break existing JwtBearer (Entra) mode
- Do not remove DevelopmentBypass or ApiKey modes
- Do not modify trial auth paths (those use a separate mechanism)
- Keep the same RBAC model (Admin/Operator/Reader/Auditor)
- Do not add external NuGet packages beyond what ASP.NET Core provides

Impact: Directly improves Adoption Friction (+6-8 pts), Interoperability (+8-10 pts), Procurement Readiness (+3-5 pts). Weighted readiness impact: +0.8-1.2%.
```

---

### Improvement 5: DEFERRED — Complete Pen Test and Publish Redacted Summary

**Title:** DEFERRED — Complete Aeronova pen test and publish redacted summary  
**Why it matters:** Third-party security attestation is the top enterprise adoption blocker. The pen test is awarded but not delivered (kickoff 2026-05-06). Until the redacted summary is available, regulated buyers cannot proceed through security review.  
**Expected impact:** Directly improves Trustworthiness (+12-15 pts), Compliance Readiness (+10-12 pts), Procurement Readiness (+8-10 pts). Weighted readiness impact: +1.0-1.5%.  
**Affected qualities:** Trustworthiness, Compliance Readiness, Procurement Readiness, Marketability  
**Reason deferred:** Pen test engagement is in flight with external vendor (Aeronova Red Team LLC, kickoff 2026-05-06). Cannot be accelerated by Cursor.  
**Information needed:** Expected delivery timeline from Aeronova; whether remediation cycles are included in scope or require separate engagement.

---

### Improvement 6: Build Terraform State Import Connector

**Title:** Build Terraform state JSON import as first inbound data connector  
**Why it matters:** No inbound data connectors is competitive weakness #1 and enterprise blocker #4. Terraform state is the most universal architecture data format among cloud-native teams (ArchLucid's target buyer). Importing existing infrastructure eliminates the "re-describe your architecture" adoption friction.  
**Expected impact:** Directly improves Interoperability (+12-15 pts), Adoption Friction (+8-10 pts), Time-to-Value (+5-7 pts). Weighted readiness impact: +1.0-1.5%.  
**Affected qualities:** Interoperability, Adoption Friction, Time-to-Value, Workflow Embeddedness  
**Whether actionable now:** YES — fully actionable

**Cursor Prompt:**

```
Implement a Terraform state JSON import connector that populates ArchLucid context from `terraform show -json` output.

Context:
- Context ingestion exists in `ArchLucid.ContextIngestion/` project
- `IContextConnector` interface exists (referenced in INTEGRATION_CATALOG.md)
- Finding engine template exists: `templates/archlucid-finding-engine/`
- Context snapshots store `CanonicalObjectsJson` per DATA_MODEL.md
- Canonical objects have types, properties, and relationships per the ingestion pipeline
- `ArchLucid.ContextIngestion.Tests/` has existing test patterns

Tasks:
1. Create `ArchLucid.ContextIngestion/Connectors/TerraformStateConnector.cs` implementing `IContextConnector`.
2. Parse `terraform show -json` output format (root_module → resources → each resource has type, name, provider, values, depends_on).
3. Map Terraform resources to ArchLucid canonical objects: resource type → CanonicalObjectType, resource attributes → properties, depends_on → relationships/edges.
4. Map common Azure resource types specifically (azurerm_resource_group, azurerm_app_service, azurerm_sql_server, azurerm_storage_account, azurerm_virtual_network) to meaningful ArchLucid topology nodes.
5. Add an API endpoint `POST /v1/context/import/terraform` that accepts the JSON body from `terraform show -json` and feeds it through the connector into a context snapshot for the current run.
6. Add unit tests in `ArchLucid.ContextIngestion.Tests/` for: (a) basic resource parsing, (b) dependency edge creation, (c) Azure resource type mapping, (d) malformed input rejection.
7. Add an integration test that imports a sample Terraform state and verifies canonical objects are created.
8. Create `docs/integrations/TERRAFORM_STATE_IMPORT.md` documenting the connector, supported resource types, and usage with `terraform show -json | curl -X POST`.

Acceptance criteria:
- `terraform show -json` output from a typical Azure deployment (5-20 resources) produces meaningful canonical objects
- Dependencies are represented as edges in the context snapshot
- Invalid JSON returns 400 with a descriptive error
- The connector handles both flat and nested module structures

Constraints:
- Do not import sensitive values (mark tfstate `sensitive` fields as redacted)
- Do not require Terraform CLI to be installed on the ArchLucid host
- Do not create a new project — add to existing ArchLucid.ContextIngestion
- Do not modify existing context ingestion pipeline — add a new connector alongside existing ones
- Limit V1 support to azurerm provider; document other providers as future work

Impact: Directly improves Interoperability (+12-15 pts), Adoption Friction (+8-10 pts), Time-to-Value (+5-7 pts). Weighted readiness impact: +1.0-1.5%.
```

---

### Improvement 7: Reduce Cognitive Load — Collapse Operator Shell Default View

**Title:** Collapse operator shell to pilot-essential navigation at first login  
**Why it matters:** 73 page routes and deep navigation create overwhelming first impressions. Progressive disclosure exists but the default view still shows too much. Cognitive load (45/100) and usability (48/100) are both dragged down by surface area exposure.  
**Expected impact:** Directly improves Cognitive Load (+10-12 pts), Usability (+8-10 pts), Time-to-Value (+3-5 pts). Weighted readiness impact: +0.4-0.7%.  
**Affected qualities:** Cognitive Load, Usability, Time-to-Value, Adoption Friction  
**Whether actionable now:** YES — fully actionable

**Cursor Prompt:**

```
Reduce the operator shell default navigation to pilot-essential items only, with a clear disclosure mechanism for advanced features.

Context:
- Navigation configuration lives in `archlucid-ui/src/lib/nav-config.ts`
- Nav groups are: `pilot`, `operate-analysis`, `operate-governance`
- Progressive disclosure exists via "Show more links" per PRODUCT_PACKAGING.md
- Role-aware shaping exists via `nav-shell-visibility.ts` and `current-principal.ts`
- First-run wizard exists at `/runs/new`
- Onboarding page exists at `/onboarding`

Tasks:
1. In `archlucid-ui/src/lib/nav-config.ts`, add a `defaultVisible: boolean` property to each nav link. Set `defaultVisible: true` ONLY for these items:
   - Home (dashboard)
   - New run (create)
   - Runs list (reviews)
   - Getting started / Help
   - Settings (tenant)
   Maximum 6-8 items visible by default.
2. In `nav-shell-visibility.ts`, filter links by `defaultVisible` unless the user has explicitly toggled "Show all" (persist preference in localStorage key `archlucid-nav-expanded`).
3. Add a visually distinct "Show all features" toggle at the bottom of the sidebar that expands to show all nav groups.
4. When the toggle is collapsed (default), show a subtle count badge: "12 more features available"
5. Ensure the first-run wizard (`/runs/new`) is always prominent — large icon or primary button treatment in the sidebar.
6. Add a Vitest test that verifies default navigation renders ≤ 8 items.
7. Add a Playwright test that verifies expanding the toggle reveals the full nav.

Acceptance criteria:
- New users see ≤ 8 navigation items at first login
- Existing users who have previously expanded retain their preference
- All routes remain accessible (just not all visible in sidebar by default)
- "Show all features" toggle is clear and not hidden

Constraints:
- Do not remove any routes or pages
- Do not change URL paths
- Do not modify the role-aware filtering logic
- Do not change the nav group IDs (pilot, operate-analysis, operate-governance)
- Preserve the existing progressive disclosure for operate groups
- Do not modify API endpoints

Impact: Directly improves Cognitive Load (+10-12 pts), Usability (+8-10 pts), Time-to-Value (+3-5 pts). Weighted readiness impact: +0.4-0.7%.
```

---

### Improvement 8: Implement Data Consistency Prevention at Write Path

**Title:** Add FK-enforced data consistency on critical write paths  
**Why it matters:** Current orphan detection is reactive (probe + optional quarantine). The write path for golden manifests and findings snapshots can create rows referencing non-existent `RunId` values. This is engineering risk #2 — cascade failures could corrupt the authority chain.  
**Expected impact:** Directly improves Data Consistency (+8-10 pts), Reliability (+3-5 pts), Correctness (+2-3 pts). Weighted readiness impact: +0.3-0.5%.  
**Affected qualities:** Data Consistency, Reliability, Correctness, Trustworthiness  
**Whether actionable now:** YES — fully actionable

**Cursor Prompt:**

```
Add foreign key constraints on critical authority-chain tables to prevent orphaned rows at write time.

Context:
- Data model in `docs/library/DATA_MODEL.md` describes the relationship: RunId → GoldenManifests, FindingsSnapshots, ComparisonRecords
- Orphan detection exists via `DataConsistencyOrphanProbeHostedService` with metrics
- DDL lives in `ArchLucid.Persistence/Scripts/ArchLucid.sql`
- DbUp migrations are sequential numbered files in `ArchLucid.Persistence/Migrations/`
- Per user rules: "All SQL DDL should be in a single file for each database"
- The master DDL file is the source of truth; migrations are incremental

Tasks:
1. Create a new migration file (next sequence number after existing migrations) that adds:
   - `ALTER TABLE dbo.GoldenManifests ADD CONSTRAINT FK_GoldenManifests_Runs FOREIGN KEY (RunId) REFERENCES dbo.Runs(RunId);`
   - `ALTER TABLE dbo.FindingsSnapshots ADD CONSTRAINT FK_FindingsSnapshots_Runs FOREIGN KEY (RunId) REFERENCES dbo.Runs(RunId);`
   - `ALTER TABLE dbo.GraphSnapshots ADD CONSTRAINT FK_GraphSnapshots_Runs FOREIGN KEY (RunId) REFERENCES dbo.Runs(RunId);`
   - `ALTER TABLE dbo.ContextSnapshots ADD CONSTRAINT FK_ContextSnapshots_Runs FOREIGN KEY (RunId) REFERENCES dbo.Runs(RunId);`
   Each with `IF NOT EXISTS` guard pattern.
2. Update `ArchLucid.Persistence/Scripts/ArchLucid.sql` to include the same FK constraints in the table definitions.
3. In the application code where runs are archived/deleted (likely `DataArchivalHostedService` or similar), ensure the delete order respects FK constraints (children before parent).
4. Add a test in `ArchLucid.Persistence.Tests` or `ArchLucid.Architecture.Tests` that verifies FK relationships exist on the critical tables.
5. Update `docs/library/DATA_MODEL.md` to document the FK constraints in the authority chain section.

Acceptance criteria:
- Attempting to insert a GoldenManifest/FindingsSnapshot/GraphSnapshot/ContextSnapshot with a non-existent RunId fails with a SQL constraint violation
- Existing data in test databases does not violate the new constraints (migration includes a check)
- Archival/delete operations respect FK order
- The orphan probe metrics should drop to zero once FKs are active

Constraints:
- Do not modify historical migration files (001-028 per workspace rules)
- Do not add CASCADE DELETE (explicit application-controlled deletion only)
- Do not add FKs on ComparisonRecords.LeftRunId/RightRunId (those may reference archived runs)
- Guard the migration with IF NOT EXISTS so it's idempotent
- Test with both empty and seeded databases

Impact: Directly improves Data Consistency (+8-10 pts), Reliability (+3-5 pts), Correctness (+2-3 pts). Weighted readiness impact: +0.3-0.5%.
```

---

### Improvement 9: Build Agent Quality Evaluation Corpus

**Title:** Create ground-truth evaluation corpus for agent finding quality  
**Why it matters:** Agent correctness is scored 62/100 because there is no evidence findings are actually correct — only that they are well-formed. A golden evaluation corpus with expected findings enables automated quality regression detection when prompts or models change.  
**Expected impact:** Directly improves Correctness (+8-10 pts), AI/Agent Readiness (+5-7 pts), Trustworthiness (+3-5 pts). Weighted readiness impact: +0.5-0.8%.  
**Affected qualities:** Correctness, AI/Agent Readiness, Trustworthiness, Reliability  
**Whether actionable now:** YES — partially actionable (synthetic scenarios; real customer data would improve it further)

**Cursor Prompt:**

```
Create a ground-truth evaluation corpus for agent finding quality with 5 synthetic architecture scenarios and expected findings.

Context:
- Agent types: Topology, Cost, Compliance, Critic
- Golden corpus validation exists: `ArchLucid.Core.Tests/GoldenCorpus/RealLlmOutputStructuralValidatorTests.cs`
- Agent prompt regression testing exists: `docs/library/AI_AGENT_PROMPT_REGRESSION.md`
- Eval script exists: `scripts/ci/eval_agent_quality.py`
- Simulator mode produces deterministic outputs for testing
- Finding types have typed payloads per category

Tasks:
1. Create directory `tests/eval-corpus/` with a README explaining the corpus purpose and structure.
2. Create 5 scenario JSON files, each containing:
   - `input`: A complete ArchitectureRequest body representing a common architecture pattern
   - `expectedFindings`: Array of expected findings with: category, minimum severity, key evidence phrases that should appear
   - `unexpectedFindings`: Array of findings that would indicate a quality problem (false positives)
   - `metadata`: scenario name, complexity level, primary agent under test
3. Scenarios should cover:
   - Scenario 1: Simple 3-tier web app (expect: no critical findings, basic topology confirmation)
   - Scenario 2: Microservices without API gateway (expect: topology finding about missing gateway)
   - Scenario 3: Database without backup configuration (expect: compliance finding about backup policy)
   - Scenario 4: Over-provisioned VM sizing (expect: cost finding about right-sizing)
   - Scenario 5: Multi-region without failover (expect: topology + compliance findings about availability)
4. Add a Python script `scripts/ci/eval_agent_corpus.py` that:
   - Runs each scenario through the API (simulator mode) or reads pre-recorded outputs
   - Compares actual findings against expected/unexpected lists
   - Reports precision (% of actual findings that are expected) and recall (% of expected findings that appeared)
   - Exits non-zero if precision or recall drops below configurable thresholds (default: 60% for V1)
5. Add documentation in `docs/library/AGENT_EVAL_CORPUS.md` explaining how to add new scenarios and interpret results.

Acceptance criteria:
- 5 scenario files exist with realistic architecture inputs
- Evaluation script runs against simulator output and produces a precision/recall report
- Script can be integrated into CI (runs after agent tests)
- Each scenario has at least 3 expected findings and 2 unexpected findings

Constraints:
- Do not use real customer data (all scenarios are synthetic)
- Do not require Azure OpenAI for corpus evaluation (use simulator/deterministic mode)
- Do not modify existing agent prompts or handlers
- Do not block CI on corpus results initially (run as informational; make blocking in V1.1)
- Keep scenario files under 500 lines each

Impact: Directly improves Correctness (+8-10 pts), AI/Agent Readiness (+5-7 pts), Trustworthiness (+3-5 pts). Weighted readiness impact: +0.5-0.8%.
```

---

### Improvement 10: Publish Azure Marketplace SaaS Offer (Private Preview)

**Title:** Publish Azure Marketplace SaaS offer as private preview  
**Why it matters:** Enterprise buyers with Azure MACC commitments prefer marketplace purchasing. The marketplace listing also provides discovery, trust signals (Microsoft verified), and simplified procurement. Monetization blocker #4.  
**Expected impact:** Directly improves Marketability (+5-7 pts), Procurement Readiness (+5-7 pts), Commercial Packaging Readiness (+5 pts). Weighted readiness impact: +0.6-0.9%.  
**Affected qualities:** Marketability, Procurement Readiness, Commercial Packaging Readiness, Decision Velocity  
**Whether actionable now:** YES — partially actionable (manifest and configuration can be prepared; actual publication requires Partner Center access)

**Cursor Prompt:**

```
Prepare the Azure Marketplace SaaS offer manifest and landing page for private preview publication.

Context:
- `docs/AZURE_MARKETPLACE_SAAS_OFFER.md` exists (or similar — search for marketplace docs)
- Marketplace tier names are guarded by CI: `scripts/ci/assert_marketplace_pricing_alignment.py`
- Tier names: Team, Professional, Enterprise
- Landing page requirement: Azure Marketplace SaaS offers require a landing page URL for subscription activation
- Webhook requirement: Azure sends subscription lifecycle events (subscribe, unsubscribe, suspend, reinstate)
- `ArchLucid.Api/Controllers/Billing/BillingStripeWebhookController.cs` shows existing webhook pattern
- `docs/runbooks/MARKETPLACE_PUBLISHER_IDENTITY.md` exists

Tasks:
1. Create `ArchLucid.Api/Controllers/Marketplace/AzureMarketplaceWebhookController.cs` that handles Azure Marketplace SaaS fulfillment API v2 webhook events:
   - POST subscription notifications (ChangePlan, ChangeQuantity, Suspend, Reinstate, Unsubscribe)
   - Validate the marketplace token against Azure AD
   - Log and acknowledge events (respond 200 OK)
   - Store subscription state in a new table or existing tenant mechanism
2. Create `ArchLucid.Api/Controllers/Marketplace/AzureMarketplaceLandingController.cs`:
   - GET endpoint that receives the marketplace token after purchase
   - Resolves the subscription via Marketplace Fulfillment API
   - Redirects to the operator shell with tenant provisioned
3. Add a SQL migration for `dbo.MarketplaceSubscriptions` (SubscriptionId, OfferId, PlanId, TenantId, Status, ActivatedUtc, etc.)
4. Update `ArchLucid.Persistence/Scripts/ArchLucid.sql` with the new table.
5. Add configuration section `Marketplace:Azure` with keys: `LandingPageUrl`, `WebhookUrl`, `TenantId` (publisher Entra tenant), `ClientId`, `ClientSecret` (for token validation).
6. Add unit tests for webhook event parsing and landing page token resolution.
7. Create `docs/runbooks/AZURE_MARKETPLACE_SAAS_FULFILLMENT.md` documenting the activation flow.

Acceptance criteria:
- Marketplace webhook endpoint accepts and acknowledges subscription events
- Landing page endpoint resolves marketplace tokens and provisions tenant
- Configuration validation fails fast with clear error when marketplace keys are missing but mode is enabled
- All three tiers (Team/Professional/Enterprise) are mapped in the plan resolution logic

Constraints:
- Do not hard-code any Azure AD credentials
- Do not auto-activate Enterprise tier (require manual approval)
- Do not modify existing Stripe billing paths
- Guard all marketplace endpoints behind a feature flag (`Marketplace:Azure:Enabled`, default false)
- Do not publish to Partner Center (that requires manual action) — prepare the code only

Impact: Directly improves Marketability (+5-7 pts), Procurement Readiness (+5-7 pts), Commercial Packaging Readiness (+5 pts). Weighted readiness impact: +0.6-0.9%.
```

---

## Deferred Scope Uncertainty

All items identified as "V1.1" or "V2" in `docs/library/V1_DEFERRED.md` were successfully located and reviewed. The deferred scope is clearly documented:

- **V1.1:** ServiceNow, Jira, Confluence connectors; product learning "brains" (theme derivation, plan-draft builder)
- **V2:** Slack connector; cross-tenant analytics; BYOK; air-gapped deployment

No scoring penalties were applied for these deferred items.

---

## Pending Questions for Later

### Improvement 1 (Close First Design Partner — V1.1)
- Who are the 3-5 most promising prospects in your network?
- Is there a timeline for first outreach?
- Would you accept a free pilot (no $15K) to accelerate first customer acquisition?
- These questions are for V1.1 planning; they do not block V1 readiness scoring.

### Improvement 5 (Pen Test)
- What is Aeronova's expected delivery timeline after kickoff?
- Does the engagement include a remediation cycle or just findings?
- Will findings be remediatable before the redacted summary is published?

### General
- Is there a sales hire planned, or is the founder the sole commercial motion?
- What is the runway (months of cash) available for pre-revenue investment?
- Is multi-cloud (AWS/GCP analysis) on the roadmap for any specific version, or is it a "someday" item?
