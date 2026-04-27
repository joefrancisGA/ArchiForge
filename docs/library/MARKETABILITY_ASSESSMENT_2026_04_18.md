> **Scope:** ArchLucid Marketability Quality Assessment — 2026-04-18 (post-trial enforcement, pricing.json, marketing pages, CMK/TDE) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# ArchLucid Marketability Quality Assessment — 2026-04-18 (post-trial enforcement, pricing.json, marketing pages, CMK/TDE)

> **Scope discipline.** This document assesses **marketability only** — i.e. whether the solution can attract buyers, win competitive evaluations, retain customers, and grow revenue in the enterprise architecture tooling market. It deliberately does **not** rescore technical quality, security posture, or operability except where they directly bear on a buyer's decision. Per the user's request, **no other solution-quality dimensions are assessed here.**

**Overall Marketability Score: 51 / 100 (weighted)** &nbsp;|&nbsp; **Unweighted average: 47 / 100**

**Prior baseline:** [docs/MARKETABILITY_ASSESSMENT_2026_04_15.md](MARKETABILITY_ASSESSMENT_2026_04_15.md) — 42.3% weighted, post-M3.

**Companion view (SaaS-only weighting):** [docs/MARKETABILITY_ASSESSMENT_2026_04_15_SAAS_ONLY.md](MARKETABILITY_ASSESSMENT_2026_04_15_SAAS_ONLY.md) — 46.1% weighted, post-Imp 1–6.

**Δ since 2026-04-15 (mixed model):** **+8.8 percentage points** (42.3% → 51.1%) — driven almost entirely by **product execution**, not new documents:

| Shipped since 04-15 | Marketability impact |
|---|---|
| `archlucid-ui/public/pricing.json` (real Team / Pro / Enterprise prices, schema-versioned) | GTM, Business Model |
| `archlucid-ui/src/app/(marketing)/{welcome,signup,signup/verify}/page.tsx` consuming positioning copy + `pricing.json` | GTM, Time-to-Value, Brand |
| `ArchLucid.Application/Tenancy/TrialLimitGate.cs`, `TrialLimitFilter`, `TrialSeatAccountant`, run-creation UoW increment, **HTTP 402 + problem+json** | Business Model, Pilot-to-Paid, Enterprise Readiness |
| Trial lifecycle transactional emails (Razor templates + worker handlers + `076_SentEmails`) | Customer Success, Time-to-Value |
| Self-service tenant provisioning + RLS bootstrap (`SqlRowLevelSecurityBypassAmbient`) | Time-to-Value, Business Model |
| Container Apps revision + canary controls | Enterprise Readiness, SaaS Reliability (companion doc) |
| CMK for storage + SQL TDE guidance | Enterprise Readiness |
| `ORDER_FORM_TEMPLATE.md` hardened from placeholder to usable commercial document | Pilot-to-Paid, GTM |
| `scripts/ci/check_pricing_single_source.py` enforcing one source of truth for prices | Brand consistency, GTM |
| OpenAPI snapshot + `ArchLucid.Api.Client` republished (B2 billing-bridge work) | Ecosystem (consumption from non-.NET) |

**Honesty boundary.** The marketing pages and pricing JSON exist **in the repo** and are wired correctly, but I have **no evidence of a live public deployment** at a marketing domain (no `archlucid.net` references, no Front Door routing config for a public marketing host, no SEO meta strategy). The leaked Stripe-style test token in commit history (`fb35bf11`, `fec717c7`) implies a billing bridge is **in progress, not GA**. Treat the +8.8-point jump as "the rails are now built and code-complete," not "the commercial motion is live." This distinction shapes most of the recommendations below.

---

## 1. Methodology

Twenty marketability dimensions, each scored **1–100**, each carrying a **weight 1–10** reflecting importance to winning and retaining paying customers in the enterprise architecture tooling market. **Same dimensions and weights** as the 2026-04-15 mixed-model assessment so deltas are comparable. Dimensions are sorted **most-needed-improvement first** by **weighted gap** = `weight × (100 − score)`.

| Range | Meaning |
|-------|---------|
| 90–100 | Market-leading — clear competitive advantage |
| 75–89  | Competitive — can win deals here |
| 60–74  | Adequate — not a deal-breaker, not a strength |
| 45–59  | Weak — losing deals because of this |
| < 45   | Critical — blocking sales or adoption |

Following the workspace's **Critique-Mode-Rule**: weaknesses first, then targeted improvements; opinionated, not generic praise.

---

## 2. Assessments (ordered by weighted improvement priority — highest gap first)

### 2.1 Differentiation and Competitive Moat — Score: 55 / 100 (Weight: 9, Weighted Gap: 405)

**Justification.** Positioning is articulated (`POSITIONING.md` defines the "AI Architecture Intelligence" category), the head-to-head competitive matrix is documented (`COMPETITIVE_LANDSCAPE.md`), the ROI model quantifies "why us" in dollars, and the integration catalog + CI/CD examples reposition ArchLucid as a *platform* rather than a chatbot. The trial-enforcement implementation (server-side gate, idempotent seat reservation, run-counter inside the same UoW) is genuinely uncommon in the AI-tooling category — most "AI for X" tools have **no usage gating at all**. That is a real, demonstrable moat-adjacent capability.

What is still missing is **defensibility that compounds without ArchLucid's effort**:

- No proprietary data advantage. Findings are produced **for** each customer; nothing flows back into a shared knowledge graph that improves the product for the next customer.
- No network effects. Customers cannot benefit from each other's anonymized run data.
- No switching costs beyond "the audit trail is in our DB" — but customers can export to CSV/DOCX, so lock-in is weak.
- No ecosystem strategy on top of the finding-engine template (`dotnet new archlucid-finding-engine` exists but has no distribution or marketplace).

**Tradeoffs.** Defensibility-by-design is expensive and frequently distracts pre-PMF teams. ArchLucid has correctly chosen *clarity* (positioning, ICP, ROI) over *moat* in this phase. But the **speed of LLM-based competitor entry** in this category is unusually high — a generic "AI architecture review" wrapper around GPT-5 can be assembled in weeks. The window for compounding-data moat design is **now**, not after PMF.

**Improvement Recommendations (ranked):**
1. **Anonymized cross-tenant pattern library** — opt-in, k-anonymized findings → "common architecture anti-patterns observed across N customers" → published quarterly. Compounds with each customer.
2. **Open the finding-engine SDK + marketplace listing** — distribution mechanism for `dotnet new archlucid-finding-engine`. Ecosystem becomes the moat.
3. **Lock the explainability trace as a published schema with a CC-BY-SA license** — make `ExplainabilityTrace` a category standard. Competitors who copy it advertise you.

---

### 2.2 Product-Market Fit Clarity — Score: 57 / 100 (Weight: 9, Weighted Gap: 387)

**Justification.** The PMF *frameworks* are now genuinely strong: `IDEAL_CUSTOMER_PROFILE.md`, `BUYER_PERSONAS.md`, `PILOT_SUCCESS_SCORECARD.md`, `PMF_VALIDATION_TRACKER.md`, `REFERENCE_NARRATIVE_TEMPLATE.md`. The 6-week pilot timeline is concrete, success criteria are tiered (minimum / target / stretch), and the ROI break-even (~180 architect-hours/year) gives a quantitative threshold. Trial enforcement now provides **observable usage data** the moment the product is live (run counts, seat counts, expiry events emit `TrialLimitExceeded` audit events).

What is still missing is the only thing that actually proves PMF: **closed pilots with named customers and documented before/after numbers**. The reference narratives (`REFERENCE_NARRATIVE_TEMPLATE.md`) are explicitly fictional templates. The PMF tracker is empty. There is no "Customer N said X about Y after Z weeks" evidence anywhere.

**Tradeoffs.** Buying PMF evidence with cash (paid pilots, design partners) is faster than waiting for organic demand. Publishing the *frameworks* publicly before having data lets prospects do the math themselves — this is good for credibility but bad if the math is wrong and gets corrected publicly.

**Improvement Recommendations (ranked):**
1. **Run two paid design-partner pilots** end-to-end against the scorecard. Publish one as a real case study (signed reference) within 60 days.
2. **Instrument the PMF tracker automatically.** Auto-populate from `dbo.AuditEvents` + run telemetry — see Improvement 1 in Section 4 below.
3. **Publish the ICP scoring rubric externally** with a self-assessment quiz — disqualifies bad-fit prospects before sales spends time on them.

---

### 2.3 Go-to-Market Readiness — Score: 62 / 100 (Weight: 10, Weighted Gap: 380)

**Justification.** This dimension has moved further than any other since 04-15. The product now has:

- **Real, structured pricing** in `archlucid-ui/public/pricing.json` (schema-versioned, three tiers, discrete numbers — not "contact us"). Current tier rates: see [docs/go-to-market/PRICING_PHILOSOPHY.md §5](../go-to-market/PRICING_PHILOSOPHY.md).
- **Marketing pages** under `archlucid-ui/src/app/(marketing)/` (`/welcome`, `/signup`, `/signup/verify`) consuming the §3 30-second pitch and §2 pillar copy from `POSITIONING.md`.
- **Hardened order-form template** (`ORDER_FORM_TEMPLATE.md`) — usable as a real commercial document, no longer a placeholder.
- **Pricing single-source CI guard** preventing price drift across docs.
- **Trial enforcement** so the trial → paid boundary is real, not aspirational.

What is missing is the **launch motion**: there is no evidence of a live `archlucid.net` (or equivalent) domain, no SEO content or sitemap, no analytics on the marketing pages, no demo-request workflow visible in the repo, no announced product launch, no press / analyst briefings. Billing is also **in progress not GA** (the gitleaks history finding on `sk_test_…` strongly suggests a Stripe integration mid-flight). A 15-minute *business* demo script (as opposed to the technical `demo-quickstart.md`) is still missing.

**Tradeoffs.** Soft-launching to a closed pilot list before the public marketing site is live is a defensible sequencing choice — it avoids spending demand-gen dollars before the funnel can convert. But every week the marketing pages sit in `archlucid-ui/` undeployed is a week of forgone organic discovery.

**Improvement Recommendations (ranked):**
1. **Deploy the marketing site publicly** — Front Door → Static Web Apps or Container Apps revision routing → custom domain → Bing/Google Search Console. The code is shipped; the deployment is not.
2. **Ship the Stripe billing bridge to GA**, with a Pricing page that reads `pricing.json` and a "Start trial" → checkout → tenant provisioning flow that lights up real revenue.
3. **Author a 15-minute *business* demo script** (not a technical walkthrough) anchored to one of the three buyer personas; record a default version so AEs/SEs can re-shoot it personalized.

---

### 2.4 Customer Success Infrastructure — Score: 48 / 100 (Weight: 7, Weighted Gap: 364)

**Justification.** The customer success motion is now **documented end-to-end**: `CUSTOMER_ONBOARDING_PLAYBOOK.md`, `CUSTOMER_HEALTH_SCORING.md`, `RENEWAL_EXPANSION_PLAYBOOK.md`. Trial-lifecycle transactional emails now actually fire from a worker (Razor templates, `076_SentEmails` table, idempotent send), which is a real in-product CS surface. `ProductLearningPilotSignals` exists.

What is missing is **automated, in-product** customer success instrumentation:
- No usage analytics or feature-adoption tracking surfacing back to a CS dashboard.
- No NPS / CSAT / in-app feedback widget.
- No customer-facing knowledge base (the 193+ docs are developer-internal in tone).
- The "brains" of `ProductLearningPilotSignals` are still deferred per prior notes.
- Health scoring is a process, not a service.

**Tradeoffs.** Building a Gainsight-class CS platform pre-revenue is wasteful. But the gap between "process documented in markdown" and "metrics computed automatically from `dbo.AuditEvents`" is small and high-leverage — the audit table already contains the raw signals.

**Improvement Recommendations (ranked):**
1. **Auto-populate the Customer Health Scorecard** from `dbo.AuditEvents` (run frequency, governance approval cadence, manifest commits, comparison usage). Surface in the operator UI as a per-tenant CS dashboard.
2. **Add an in-product feedback widget** (thumbs up/down on findings + free-text) with the responses streamed to a `dbo.ProductFeedback` table.
3. **Spin out a customer-facing docs site** (`docs.archlucid.net`) from the highest-trafficked internal docs — keep the developer docs separate.

---

### 2.5 Time-to-Value / Onboarding Experience — Score: 57 / 100 (Weight: 8, Weighted Gap: 344)

**Justification.** Self-service tenant provisioning is shipped (RLS bypass for bootstrap, `CommitSelfServiceTrialAsync`), the trial is enforced server-side, the first-run wizard is well-designed, the Docker demo (`docker-compose.demo.yml` + `scripts/demo-start.*`) eliminates SDK prerequisites for evaluators, and the signup page exists. A prospect *could* go from email → tenant → first run in minutes once the marketing site is live.

What is missing:
- **The hosted trial environment is not deployed publicly.** Until `signup` resolves to a live domain, time-to-value remains hypothetical.
- **Pre-seeded trial sample data** — the design says "auto-execute a sample run using the agent simulator so results appear without LLM cost," but I see no evidence the trial tenant gets a pre-completed run by default.
- **Guided in-product tour** is described in `TRIAL_AND_SIGNUP.md` but not implemented.
- **Cold-start prerequisite friction** for self-hosted evaluators (.NET 10 SDK + SQL Server + Docker + Node 22) is unchanged.

**Tradeoffs.** Inherent friction in self-hosted tooling is unavoidable. The Docker demo path is the right answer for evaluators. The hosted trial path is the right answer for buyers. Both exist on paper; only one (Docker demo) is proven in code today.

**Improvement Recommendations (ranked):**
1. **Deploy the hosted trial** so signup → tenant → first run is a real public flow (not just code).
2. **Pre-seed every trial tenant** with one completed sample run + manifest + governance approval so the first screen the user sees is *results*, not an empty state.
3. **Implement the in-product guided tour** (5 tooltips: findings → manifest → governance gate → comparison → export).

---

### 2.6 Multi-Cloud / Platform Breadth — Score: 35 / 100 (Weight: 5, Weighted Gap: 325)

**Justification.** Unchanged from 04-15. Azure-only. AWS and GCP are disabled in the wizard. The agent runtime is multi-vendor for LLMs but the **infrastructure** is not multi-cloud. There is no Helm chart. Terraform modules are Azure-specific.

This dimension is worth **re-weighting** for a SaaS-only motion (where it matters less because the customer doesn't choose the cloud) but for a hybrid commercial model — and especially for the consulting-firm channel partnerships hinted at in `RENEWAL_EXPANSION_PLAYBOOK.md` — Azure-only disqualifies a large fraction of the addressable market.

**Tradeoffs.** Going multi-cloud takes 2–3 quarters of platform engineering. Shipping multi-cloud *agent capabilities* (a Topology agent that reasons about AWS resources, a Cost agent that knows AWS pricing) is much cheaper and addresses the buyer's question ("does it review my AWS architecture?") without demanding the product itself runs on AWS.

**Improvement Recommendations (ranked):**
1. **Add AWS-aware Topology, Cost, and Compliance agent capabilities** (the agents *analyze* AWS even though the platform *runs on* Azure). Decouples the buyer question from the platform-engineering effort.
2. **Abstract infrastructure dependencies behind provider interfaces** — already partially true for storage (Azurite vs. Azure Blob); extend to identity, secrets, queue.
3. **Publish a Helm chart** for AKS + EKS + GKE (single chart, three values files).

---

### 2.7 Value Demonstration / ROI Articulation — Score: 55 / 100 (Weight: 7, Weighted Gap: 315)

**Justification.** The ROI framework is comprehensive (`ROI_MODEL.md`: cost-of-status-quo calculator, value lever mapping, $294K example scenario, sensitivity analysis, leadership presentation guide) and the pilot scorecard provides the measurement plan. The "Usage metering + hooks" work in commit `63ee5744` plus the trial-run counter increment in the run-creation UoW means **per-tenant run usage is now actually measured** — the raw data for ROI proof is being collected.

What is missing is the **automated leadership report**: ROI claims are still computed manually using the spreadsheet template. There is no `POST /v1.0/value-report/{tenantId}` endpoint that emits a stakeholder-grade PDF/DOCX showing *this* customer's *actual* hours-saved-vs-status-quo. There is also no published case study with real numbers.

**Tradeoffs.** Real customer numbers always disappoint relative to model estimates (sales math is optimistic). Letting customers self-compute their ROI from their own data is more credible, more durable, and removes ArchLucid from the "do you trust their math" objection.

**Improvement Recommendations (ranked):**
1. **Implement `POST /v1/value-report/{tenantId}/generate`** — auto-builds the leadership report from the customer's own runs, applies the `ROI_MODEL.md` formulas, and renders DOCX via the existing `IDocxExportService`. See Improvement 1 in Section 4 below.
2. **Per-run cost surfacing in the operator UI** — LLM tokens × model price + compute time, displayed alongside run results.
3. **Publish one real case study** (named customer or anonymized-with-permission) within 60 days.

---

### 2.8 Ecosystem and Integration Breadth — Score: 48 / 100 (Weight: 6, Weighted Gap: 312)

**Justification.** Catalog (`INTEGRATION_CATALOG.md`), SIEM export design (`SIEM_EXPORT.md`), CI/CD integration guide (`docs/integrations/CICD_INTEGRATION.md`), GitHub Actions example (`examples/github-actions/archlucid-architecture-review.yml`), Service Bus integration events with AsyncAPI 2.6 spec, OpenAPI + `ArchLucid.Api.Client` republished. The **integration story** is now compelling on paper.

What is missing is **shipped connectors**:
- No Python or JavaScript SDK (only `.NET` client).
- No Structurizr DSL, ArchiMate XML, or Terraform-state import connector despite being identified as the #1 positioning gap.
- No native ServiceNow / Jira / GitHub-Issues outbound integration (only generic webhooks).
- SCIM is on the roadmap, not shipped (Enterprise tier promises it).

**Tradeoffs.** Connectors compound: each one widens the funnel for a specific buyer segment, but each one also adds maintenance surface. Building 1 deep connector (Terraform state, with full canonical-object mapping) is more valuable than 5 shallow ones because *Terraform-state-aware* architecture review is a story competitors cannot tell.

**Improvement Recommendations (ranked):**
1. **Ship the Terraform-state import connector** (`TerraformStateConnector` → `IContextConnector` → `CanonicalObject`). This is the single highest-leverage ecosystem move because it lets ArchLucid review **what customers are actually building**, not what they describe in a brief.
2. **Auto-generate a Python SDK** from the OpenAPI spec, publish to PyPI. Costs little, opens the data-science / SRE buyer.
3. **Publish a Structurizr DSL importer** to capture the ThoughtWorks / EA-tools-adjacent crowd.

---

### 2.9 Content and Thought Leadership — Score: 27 / 100 (Weight: 4, Weighted Gap: 292)

**Justification.** Marginal change (+2 from 04-15). 200+ internal docs is a treasure trove of publishable content; **none of it is published externally**. No blog, no whitepaper, no conference talk, no analyst briefing, no DevRel motion. The "AI Architecture Intelligence" category definition is a perfect whitepaper foundation but exists only inside `POSITIONING.md`. The ROI model contains quantitative claims that would be a natural "Total Cost of Manual Architecture Review" piece.

**Tradeoffs.** Content marketing has long lead times — months to traction. Doing it pre-PMF is often premature. **But ArchLucid is competing to define a category**, and category-defining content is the single most efficient way to own a category before incumbents react. This is the wrong dimension to defer.

**Improvement Recommendations (ranked):**
1. **Publish a "State of AI-Assisted Architecture Design 2026" whitepaper** anchored to the `POSITIONING.md` §5 category definition. One artifact, high SEO value.
2. **Extract 5–10 blog posts** from existing ADRs (especially ADR 0003 RLS, ADR 0014 trial enforcement) and security/explainability docs — already written, just need editing for external audience.
3. **Open-source a non-core component** (the finding-engine template `dotnet new archlucid-finding-engine` is the obvious candidate) to seed a developer community.

---

### 2.10 User Experience Polish — Score: 52 / 100 (Weight: 6, Weighted Gap: 288)

**Justification.** Operator UI is broad (172+ `.tsx` files) and accessible (Radix UI, `aria-live`, axe rules being chased). Marketing pages added new public surface. But: the screenshots from `SCREENSHOT_GALLERY.md` are still not captured into `docs/go-to-market/screenshots/`. There is no design-system documentation. The product is internally labeled "operator shell" — back-office framing for a product being sold to enterprise architects who expect Figma-grade polish. The marketing pages use Tailwind defaults with no custom brand theme applied yet.

**Tradeoffs.** UX polish has a high opportunity cost relative to PMF execution. But in the EA tooling category the typical evaluator is *visually sophisticated* (they design diagrams for a living). Tailwind-default chrome on the marketing site is a tax on credibility.

**Improvement Recommendations (ranked):**
1. **Capture and ship the screenshot gallery** per `SCREENSHOT_GALLERY.md` — converts the asset-list into actual marketing collateral.
2. **Reframe "operator shell" → "Architecture Intelligence Console"** in all UI copy.
3. **Apply a minimal brand theme** to Tailwind (3 colors + typography) so the marketing site stops looking like a starter template.

---

### 2.11 Enterprise Readiness — Score: 65 / 100 (Weight: 7, Weighted Gap: 245)

**Justification.** Largest single-dimension lift in this round (+10). New evidence: CMK for storage + SQL TDE shipped, Container Apps revision/canary controls, RLS BLOCK predicates added (`migration 036` + the BLOCK pass), trial enforcement with append-only `TrialLimitExceeded` audit events, hardened DPA template, subprocessors register, incident-comms policy, SLA summary, SOC 2 roadmap, tenant-isolation doc, trust-center spine.

What is missing for **non-Microsoft-stack** enterprises:
- **Generic OIDC** (Okta / Auth0 / Ping) is still not shipped — the Enterprise pricing tier promises it as roadmap. This single gap disqualifies a meaningful slice of mid-to-large enterprises.
- **No SOC 2 Type I report yet** — only a roadmap.
- **No published BAA** for healthcare prospects.
- **No data-residency commitments** beyond "Azure region of your choice" implied by infra.

**Tradeoffs.** Generic OIDC is small engineering work (the `ArchLucidRoleClaimsTransformation` already abstracts claim mapping); SOC 2 is a 6–12-month program with auditor cost. Doing OIDC *now* unlocks deals; doing SOC 2 unlocks a higher class of deals later.

**Improvement Recommendations (ranked):**
1. **Ship generic OIDC** (`ArchLucidAuth:Mode = "OpenIdConnect"`, configurable Authority/ClientId/Audience/RoleClaimType, Okta + Auth0 sample appsettings) — code shape already exists.
2. **Engage a SOC 2 firm** and target Type I in 6 months. Publish the readiness assessment in the meantime.
3. **Publish a BAA template** alongside `DPA_TEMPLATE.md` for healthcare prospects.

---

### 2.12 Partner and Channel Readiness — Score: 22 / 100 (Weight: 3, Weighted Gap: 234)

**Justification.** No partner program, no SI relationships, no consulting-firm partnerships, no white-label / OEM capability, no certified-implementer training. The DOCX consulting export is a *capability* a partner could use, but there is no **structure** around that use. Low weight but very high gap.

**Tradeoffs.** Channel pre-PMF is a distraction; channel post-PMF is a 5–10× revenue multiplier. The right move is to design the structure now (so when a Big Four firm calls, there is a kit) but not to sell into channel until at least one direct reference customer exists.

**Improvement Recommendations (ranked):**
1. **Design a "Consulting Implementation Partner" kit** (commercial terms outline, customizable DOCX templates with partner branding, implementation playbook).
2. **Identify the top 5 boutique Azure architecture-consulting firms** and pre-warm them with the datasheet + ROI model.
3. **Build white-label DOCX templates** that swap the ArchLucid logo for the partner's.

---

### 2.13 Business Model Scalability — Score: 58 / 100 (Weight: 5, Weighted Gap: 210)

**Justification.** The largest backstage move in this round. Real prices in `pricing.json`, real tenant provisioning, real trial enforcement (HTTP 402, idempotent seat reservation, run-counter inside the create-run UoW), real lifecycle emails, real metering hooks. The commercial *backbone* is now a working system — not a deck.

What is missing:
- **Billing integration is not GA.** The Stripe-style token leak in commit history strongly suggests a Stripe bridge is mid-flight (commit `663fb59b`, "Status: Prompt B2 is in place"), but I cannot confirm a checkout → invoice → revenue-recognition flow that closes the loop.
- **Self-service paid conversion** — i.e. a trial customer hits the limit and can swipe a card to upgrade without sales contact — is not visible.
- **No usage-overage billing** despite `pricing.json` defining `overageRunUsd`.
- **No Azure Marketplace listing.**

**Tradeoffs.** Self-service paid conversion is the difference between an SDR-heavy sales motion (expensive) and a PLG-leverage motion (cheap). Building the Stripe bridge well now compounds quickly.

**Improvement Recommendations (ranked):**
1. **Ship Stripe billing to GA**, end-to-end: checkout → invoice → tenant tier upgrade → overage metering → dunning. The trial enforcement code already provides the upgrade trigger.
2. **List on Azure Marketplace** (transactable). Most enterprise buyers prefer Marketplace consumption against committed Azure spend.
3. **Add usage-overage metering** so the `overageRunUsd` numbers in `pricing.json` are actually billed, not just published.

---

### 2.14 Vertical / Industry Readiness — Score: 35 / 100 (Weight: 3, Weighted Gap: 195)

**Justification.** Reference-narrative templates cover financial services, tech, and healthcare scenarios but no policy-pack content ships for those verticals. Generic presets only ("Greenfield web app", "Modernize legacy system"). No SOC 2 / ISO 27001 / HIPAA / PCI-DSS control mapping in the policy-pack catalog despite the policy-pack system being technically capable.

**Tradeoffs.** Vertical specialization narrows the funnel but increases conversion. A "HIPAA architecture review" pack is dramatically more sellable to a healthcare CTO than "AI architecture review."

**Improvement Recommendations (ranked):**
1. **Ship a "Financial Services" reference policy pack** mapping findings to PCI-DSS + Federal Reserve SR 11-7 (model-risk) controls.
2. **Ship a "Healthcare" reference policy pack** mapping findings to HIPAA + HITRUST.
3. **Per-vertical demo scenarios** with vertical-appropriate sample architectures.

---

### 2.15 Community and Ecosystem — Score: 17 / 100 (Weight: 2, Weighted Gap: 166)

**Justification.** No GitHub Discussions, no Discord/Slack, no public issue tracker, no developer forum, no user group, no public roadmap. The `dotnet new archlucid-finding-engine` template is a foundation but has no distribution. Low weight but the highest single gap on the board.

**Improvement Recommendations (ranked):**
1. **Open a GitHub Discussions space** for pilot users and early adopters (zero infra cost).
2. **Publish the public roadmap** as a GitHub Project or single doc.
3. **Open-source the finding-engine template** under MIT / Apache-2.0 with example custom finding engines.

---

### 2.16 Internationalization / Localization — Score: 22 / 100 (Weight: 2, Weighted Gap: 156)

**Justification.** English-only throughout. No i18n framework in the Next.js UI. No data-residency *commitments* despite the underlying Azure infra supporting it. Acceptable for a V1 targeting English-speaking Azure customers; limits TAM for European and APAC enterprise sales.

**Improvement Recommendations (ranked):**
1. **Add `next-intl` (or equivalent) i18n framework** to the operator UI as a foundation, even if only English ships day one.
2. **Externalize user-facing strings in the API** (`.resx` per locale).
3. **Publish a data-residency commitment doc** (which Azure regions, what stays where).

---

### 2.17 Buyer Documentation — Score: 62 / 100 (Weight: 4, Weighted Gap: 152)

**Justification.** Now genuinely strong. 29-document GTM library covering trust, commercial, customer success, integrations, ICP, reference narratives, PMF. `PRODUCT_DATASHEET.md` is buyer-grade. `ROI_MODEL.md` is champion-ready. `ORDER_FORM_TEMPLATE.md` is procurement-ready.

What is missing:
- No "Why ArchLucid" *one-pager* (positioning doc is multi-page).
- No buyer-facing FAQ.
- No public-facing **changelog** that frames product velocity for buyers (the engineering changelog exists; a buyer-facing "what's new this quarter" digest does not).

**Improvement Recommendations (ranked):**
1. **Extract a "Why ArchLucid" one-pager** from the positioning doc.
2. **Publish a buyer-facing FAQ** (top 20 objections answered).
3. **Quarterly "What's new" digest** for prospects and customers, drawn from `docs/CHANGELOG.md`.

---

### 2.18 Pilot-to-Paid Conversion Path — Score: 62 / 100 (Weight: 4, Weighted Gap: 152)

**Justification.** Champion now has: ROI model + scorecard + report template + onboarding playbook + order-form template + *enforced* trial → paid boundary (HTTP 402 + 7-day read-only grace + 30-day data export per `TRIAL_AND_SIGNUP.md` §3). This is meaningfully better than 04-15. The renewal/expansion playbook is documented.

What is missing:
- **Auto-generated value report** (see ROI Articulation, Improvement 1 below).
- **Self-service tier upgrade** without sales contact (depends on Stripe GA).
- **Champion enablement kit** (executive summary template, procurement-FAQ, security-questionnaire pre-fills).

**Improvement Recommendations (ranked):**
1. **Auto-generate the leadership value report** from a tenant's own runs.
2. **Self-service upgrade** (Stripe GA dependency).
3. **Champion enablement kit** (security questionnaire pre-fills are particularly high-leverage for enterprise sales).

---

### 2.19 Brand Identity — Score: 32 / 100 (Weight: 2, Weighted Gap: 136)

**Justification.** Product name is distinctive and well-chosen ("ArchLucid"). Tagline options exist. **No logo, no visual brand guide, no color palette, no typography system.** The ArchiForge → ArchLucid rename is functionally complete in code (per `ArchLucid.sln`, `ArchLucid.*` projects, removed bridges) but Phase 7.5–7.8 (Terraform state addresses, GitHub repo, Entra app registrations, workspace path) remain — a buyer who runs `terraform state list` will still see `archiforge` resources, which undermines polish.

**Improvement Recommendations (ranked):**
1. **Commission a logo + minimal brand guide** (3 colors + typography + logo lockup) — small budget, large credibility effect.
2. **Apply the brand to the marketing pages** (custom Tailwind theme replacing defaults).
3. **Complete rename Phase 7.5–7.8** so `terraform state list` no longer leaks the old brand.

---

### 2.20 Market Timing / Category Definition — Score: 62 / 100 (Weight: 2, Weighted Gap: 76)

**Justification.** Strongest dimension absolute-score-wise. AI-assisted enterprise architecture is in a clear category-formation window. The "AI Architecture Intelligence" naming is defensible. ArchLucid is meaningfully ahead of generic-LLM "ask ChatGPT to review my architecture" wrappers because the *governance and audit* spine is real (78 typed audit events, append-only enforcement, pre-commit gate, segregation-of-duties workflows). But the window is short — once Microsoft, Google, or AWS ship native AI architecture review, the *category* will be defined by them.

**Improvement Recommendations (ranked):**
1. **Move to reference-customer announcements within 90 days** to anchor the category before incumbents catch up.
2. **Publish category-defining content** (see Content / Thought Leadership above).
3. **Brief 3 industry analysts** (Gartner, Forrester, IDC) before incumbents do.

---

## 3. Summary table (sorted by weighted gap, descending)

| Rank | Marketability Area | Weight | Score | Gap | Weighted Gap | Grade | Δ vs 04-15 |
|------|-------------------|--------|-------|-----|--------------|-------|------------|
| 1  | Differentiation & Moat            | 9  | 55 | 45 | **405** | Weak     | +5  |
| 2  | Product-Market Fit Clarity        | 9  | 57 | 43 | **387** | Weak     | +4  |
| 3  | Go-to-Market Readiness            | 10 | 62 | 38 | **380** | Adequate | +22 |
| 4  | Customer Success Infrastructure   | 7  | 48 | 52 | **364** | Weak     | +13 |
| 5  | Time-to-Value / Onboarding        | 8  | 57 | 43 | **344** | Weak     | +12 |
| 6  | Multi-Cloud / Platform Breadth    | 5  | 35 | 65 | **325** | Critical | 0   |
| 7  | Value Demo / ROI Articulation     | 7  | 55 | 45 | **315** | Weak     | +5  |
| 8  | Ecosystem & Integration Breadth   | 6  | 48 | 52 | **312** | Weak     | +8  |
| 9  | Content & Thought Leadership      | 4  | 27 | 73 | **292** | Critical | +2  |
| 10 | UX Polish                         | 6  | 52 | 48 | **288** | Weak     | +2  |
| 11 | Enterprise Readiness              | 7  | 65 | 35 | **245** | Adequate | +10 |
| 12 | Partner & Channel                 | 3  | 22 | 78 | **234** | Critical | +2  |
| 13 | Business Model Scalability        | 5  | 58 | 42 | **210** | Weak     | +16 |
| 14 | Vertical / Industry Readiness     | 3  | 35 | 65 | **195** | Critical | +5  |
| 15 | Community & Ecosystem             | 2  | 17 | 83 | **166** | Critical | +2  |
| 16 | Internationalization              | 2  | 22 | 78 | **156** | Critical | 0   |
| 17 | Buyer Documentation               | 4  | 62 | 38 | **152** | Adequate | +16 |
| 18 | Pilot-to-Paid Conversion          | 4  | 62 | 38 | **152** | Adequate | +10 |
| 19 | Brand Identity                    | 2  | 32 | 68 | **136** | Critical | +2  |
| 20 | Market Timing / Category          | 2  | 62 | 38 | **76**  | Adequate | +2  |

**Weighted score:** Σ(score × weight) / Σ(weight × 100) = **5,366 / 10,500 = 51.1%**
**Unweighted average:** 933 / 20 = **46.7 / 100**

**Score trajectory:**

| Milestone        | Weighted % | Unweighted | Δ weighted | Critical dims |
|------------------|------------|------------|------------|---------------|
| Pre-M1           | 35.0%      | 35.0       | —          | — |
| Post-M1          | 37.6%      | 36.4       | +2.6       | — |
| Post-M2          | 39.9%      | 37.3       | +2.3       | — |
| Post-M3 (04-15)  | 42.3%      | 39.8       | +2.4       | 8 of 20 |
| **Today (04-18)** | **51.1%** | **46.7**   | **+8.8**   | **6 of 20** |

**Interpretation.** The +8.8-point jump in three days is **almost entirely product-execution, not documentation**. The single largest lifts came from areas the 04-15 assessment explicitly flagged as "execution gaps, not documentation gaps": real prices in `pricing.json`, marketing/signup pages in code, server-side trial enforcement, lifecycle transactional emails, CMK + SQL TDE, and a hardened order form. **Critical-grade dimensions dropped from 8 to 6** — the remaining critical six (Multi-Cloud, Content, Partner, Vertical, Community, i18n, Brand) are all either *strategic deferrals* (Multi-Cloud, i18n, Vertical, Partner) or *small-investment wins* (Content, Community, Brand). The dimensions that matter most for **near-term revenue conversion** — GTM, Business Model, Pilot-to-Paid, Enterprise Readiness — are all now in the Adequate or Weak band. This is the first assessment in the series where the product can credibly be **sold**, not just described.

The remaining ceiling is now set by **two things only**: (1) absence of real customer pilot evidence and (2) absence of a live public commercial motion (marketing site + Stripe GA). Both are addressable in weeks, not quarters.

---

## 4. Six best improvements (ordered by weighted impact and feasibility)

These are picked for **highest weighted-gap closure per unit of engineering effort**. The first two have full Cursor prompts in §5; the rest have one-line prompt sketches because the user requested only the first two prompts.

### Improvement 1 — In-product Value Report endpoint and PMF auto-tracker

**Targets:** Value Demo / ROI (315), PMF Clarity (387), Pilot-to-Paid (152), Customer Success (364).
**Combined weighted gap addressed:** ≈ 1,218.

**What.** An in-product `POST /v1/value-report/{tenantId}/generate` endpoint that computes the leadership ROI report from the tenant's own audit events and run telemetry, applies the formulas in `ROI_MODEL.md`, and renders a stakeholder-grade DOCX via the existing `IDocxExportService`. Same pipeline auto-populates `PMF_VALIDATION_TRACKER.md` weekly via a worker job, replacing the manual scorecard step in `PILOT_SUCCESS_SCORECARD.md`.

**Why this first.** Closes four weighted gaps with one piece of work, all of them high-weight. Uses code that already exists (`IDocxExportService`, `dbo.AuditEvents`, run telemetry, the ROI formulas). Eliminates the single biggest credibility risk — "your ROI numbers are model estimates, not real customer data" — by generating the numbers from the customer's own data.

**Cursor prompt:** see §5.1 below.

---

### Improvement 2 — Public marketing site go-live + Stripe billing GA

**Targets:** GTM Readiness (380), Business Model (210), Brand Identity (136), Time-to-Value (344).
**Combined weighted gap addressed:** ≈ 1,070.

**What.** Two coordinated workstreams: (a) deploy the `archlucid-ui/src/app/(marketing)/` pages to a public domain via Front Door + Static Web Apps (or Container Apps revision) with custom domain, search-console verification, sitemap, and basic analytics; (b) finish the Stripe billing bridge to GA — Checkout for self-service paid conversion against `pricing.json`, webhook → tier upgrade → tenant attribute, overage metering against the `overageRunUsd` numbers, dunning on failed payments. Also rotate / scrub the `sk_test_…` secret from CI and history (or allowlist it as test fixture) so gitleaks stops flagging.

**Why this second.** Converts the largest amount of *built but undeployed* product into actual revenue capability. Without the public site the marketing pages are wasted. Without Stripe GA the trial → paid boundary is enforced but cannot be crossed without sales contact. Both are short-haul engineering relative to the impact.

**Cursor prompt:** see §5.2 below.

---

### Improvement 3 — Generic OIDC + SOC 2 Type I program kickoff

**Targets:** Enterprise Readiness (245), Pilot-to-Paid (152), GTM (380 partially).

**What.** Ship `ArchLucidAuth:Mode = "OpenIdConnect"` with configurable Authority/ClientId/Audience/RoleClaimType + Okta/Auth0 sample appsettings (the existing `ArchLucidRoleClaimsTransformation` already abstracts claim mapping). In parallel, engage a SOC 2 firm and target Type I in 6 months; publish the readiness assessment now.

**Cursor prompt sketch:** "Add `OpenIdConnect` mode to `ArchLucidAuthOptions`; reuse `ArchLucidRoleClaimsTransformation`; ship `appsettings.Okta.sample.json` and `appsettings.Auth0.sample.json`; tests in `ArchLucid.Host.Composition.Tests`; update `docs/SECURITY.md` and `CUSTOMER_TRUST_AND_ACCESS.md`."

---

### Improvement 4 — Terraform-state import connector

**Targets:** Ecosystem (312), Differentiation (405 partially).

**What.** `TerraformStateConnector : IContextConnector` parses `terraform show -json` output, extracts resources and dependencies, and maps to `CanonicalObject` records. ArchLucid then reviews **what customers are actually building** — a story competitors cannot match.

**Cursor prompt sketch:** "Read `ArchLucid.ContextIngestion/` for `IContextConnector` and `CanonicalObject`. Implement `TerraformStateConnector` parsing `terraform show -json` output. Tests for sample / empty / malformed state. Update `docs/CONTEXT_INGESTION.md`."

---

### Improvement 5 — Auto-populated Customer Health Scoring + in-product feedback widget

**Targets:** Customer Success (364), PMF Clarity (387 partially).

**What.** A worker job that reads `dbo.AuditEvents` + run telemetry on a schedule and writes per-tenant health scores to `dbo.TenantHealthScores`. Operator UI surfaces a per-tenant CS dashboard. An in-product thumbs-up/down widget on findings writes to `dbo.ProductFeedback` for PMF signal collection.

**Cursor prompt sketch:** "Implement `TenantHealthScoringWorker` reading audit-event categories defined in `CUSTOMER_HEALTH_SCORING.md`; persist to `dbo.TenantHealthScores`; surface in `archlucid-ui` as `app/admin/tenants/[id]/health/`. Add `<FindingFeedbackWidget />` writing to `dbo.ProductFeedback`."

---

### Improvement 6 — "State of AI-Assisted Architecture Design 2026" whitepaper + finding-engine SDK open-source

**Targets:** Content & Thought Leadership (292), Community (166), Differentiation (405 partially), Market Timing (76).

**What.** One category-defining whitepaper (anchored to `POSITIONING.md` §5) published under a real domain with author bylines and ungated PDF. In parallel, open-source the `dotnet new archlucid-finding-engine` template under MIT/Apache-2.0 to seed a developer community.

**Cursor prompt sketch:** "Draft `whitepapers/state-of-ai-architecture-2026.md` from `POSITIONING.md` §5, `COMPETITIVE_LANDSCAPE.md`, `ROI_MODEL.md`. Render to PDF via existing DOCX → PDF pipeline. Open-source `templates/dotnet-new-finding-engine/` to its own repo with MIT license, contributing guide, and one example custom engine."

---

## 5. Cursor prompts for Improvements 1 and 2

### 5.1 Cursor prompt for Improvement 1 — `value-report-and-pmf-tracker`

```
Improvement 1 — Prompt `value-report-and-pmf-tracker`

CONTEXT
- Target docs: docs/MARKETABILITY_ASSESSMENT_2026_04_18.md §4 Improvement 1.
- Read first:
  - docs/go-to-market/ROI_MODEL.md (formulas + value levers + example scenario)
  - docs/go-to-market/PILOT_SUCCESS_SCORECARD.md (metrics)
  - docs/go-to-market/PMF_VALIDATION_TRACKER.md (hypothesis schema)
  - docs/AUDIT_COVERAGE_MATRIX.md (typed audit events available)
  - ArchLucid.Application/Analysis/ for IDocxExportService usage
  - ArchLucid.Persistence/Migrations/ for the latest migration number

OBJECTIVE
Ship in-product value reporting + automated PMF tracking using telemetry the
product already collects. Eliminate the "your ROI is just a model" objection
by computing the report from the customer's own data.

WORKSTREAMS

1. New ArchLucid.Application/Value/IValueReportComputer.cs
   - InputModel { TenantId, FromUtc, ToUtc, BaselineHoursPerReview = 40 }
   - Output: ValueReport { runs, hoursSavedEstimate, governanceCycleTime,
     compliancePostureDelta, complianceFindingsBySeverity,
     manifestVelocity, breakEvenAtHours, narrative }
   - Implementation: pulls from dbo.AuditEvents + Runs + GoldenManifests +
     ComparisonRecords; applies ROI_MODEL.md formulas exactly.
   - Pure; null-safe; unit-testable without DB.

2. ArchLucid.Application/Value/ValueReportComputer.cs (Dapper-backed
   implementation behind the interface; one class per file).

3. POST /v1/value-report/{tenantId}/generate (new ValueReportController)
   - Body: { fromUtc, toUtc, format = "json" | "docx" }
   - Returns: 200 application/json or application/vnd.openxmlformats... +
     existing IDocxExportService for the DOCX path.
   - Authorization policy: ReadAuthority + tenant-scope check (operators
     can only generate reports for their own tenant; AdminAuthority can
     cross-tenant).

4. Razor / DOCX template at
   ArchLucid.Application/Templates/value-report.template.docx (or programmatic
   build via the existing DocxBuilder if no template engine is wired).
   Sections mirror the leadership presentation guide in ROI_MODEL.md §7.

5. Worker job
   ArchLucid.Worker/Jobs/PmfTrackerRefreshJob.cs:
   - Runs weekly.
   - For every active tenant: compute ValueReport for trailing 7d/30d/90d.
   - Persist to dbo.PmfTrackerSnapshots (new table — add migration
     0NN_PmfTrackerSnapshots.sql; do NOT modify existing migration files).
   - Append entries to docs/go-to-market/PMF_VALIDATION_TRACKER.md is OUT
     OF SCOPE — the doc remains the human-curated narrative. The table is
     the data source.

6. Migration: ArchLucid.Persistence/Migrations/0NN_PmfTrackerSnapshots.sql
   (use the next sequential number; mirror in sql/ArchLucid.sql per the
   workspace SQL DDL rule). Append-only; DENY UPDATE/DELETE per
   AuditEvents pattern.

7. Tests (try for 100% line coverage):
   - ArchLucid.Application.Tests/Value/ValueReportComputerTests.cs
     (Suite=Core, Category=Unit; mock Dapper)
   - ArchLucid.Api.Tests/Controllers/ValueReportControllerTests.cs
     (Suite=Core; uses ArchLucidApiFactory; AdminAuthority + ReadAuthority
     + cross-tenant denied paths)
   - ArchLucid.Persistence.Tests/Value/PmfTrackerSnapshotsRepoIntegration
     Tests.cs (Category=Integration; ephemeral SQL DB)
   - ArchLucid.Worker.Tests/Jobs/PmfTrackerRefreshJobTests.cs

8. Operator UI
   - archlucid-ui/src/app/admin/value-report/page.tsx — date picker, format
     toggle (JSON/DOCX), download button calling the new endpoint.
   - Vitest + Playwright smoke per archlucid-ui/docs/TESTING_AND_TROUBLE
     SHOOTING.md.

9. Docs
   - New docs/VALUE_REPORT.md (per Markdown-Generosity rule): what it
     computes, formulas applied, sample DOCX, security model, RLS notes.
   - Update docs/API_CONTRACTS.md with the new endpoint.
   - Update docs/go-to-market/ROI_MODEL.md to link to the in-product
     endpoint for the "champion now uses live numbers" path.
   - Update docs/ARCHITECTURE_INDEX.md.

CONSTRAINTS (project-wide rules)
- Each new class in its own file.
- Single-line throw without braces after if (per
  .cursor/rules/SingleLineThrowNoBraces.mdc).
- Blank line before every if and foreach unless first line of method.
- LINQ over foreach where it does not degrade performance.
- Concrete types over var.
- Always check nulls.
- Comment any code a 2-year developer would not understand.
- Do not modify any historical migration files (per ArchLucid-Rename rule
  guardrail).
- New migration number is the next sequential after the highest existing
  number in ArchLucid.Persistence/Migrations/; mirror the DDL in
  sql/ArchLucid.sql (single-file DDL per database, per workspace rule).
- No SMB / port 445 exposure (storage must remain via private endpoints).
- No ConfigureAwait(false) in tests.
- All infrastructure changes (if any) representable in Terraform.

ACCEPTANCE
- dotnet test ArchLucid.sln green (Suite=Core and Category=Integration on
  local SQL).
- New endpoint visible in /openapi/v1.json; ArchLucid.Api.Client
  regenerated.
- Manual smoke: POST /v1/value-report/{tenantId}/generate?format=docx
  returns a non-empty DOCX with the expected sections.
- One sample PDF rendering of the DOCX committed to
  docs/samples/value-report-sample.pdf.
```

---

### 5.2 Cursor prompt for Improvement 2 — `marketing-site-live-and-stripe-ga`

```
Improvement 2 — Prompt `marketing-site-live-and-stripe-ga`

CONTEXT
- Target docs: docs/MARKETABILITY_ASSESSMENT_2026_04_18.md §4 Improvement 2.
- Read first:
  - archlucid-ui/src/app/(marketing)/welcome/page.tsx
  - archlucid-ui/src/app/(marketing)/signup/page.tsx
  - archlucid-ui/src/app/(marketing)/signup/verify/page.tsx
  - archlucid-ui/public/pricing.json
  - docs/go-to-market/PRICING_PHILOSOPHY.md
  - docs/go-to-market/TRIAL_AND_SIGNUP.md
  - infra/ for the existing Front Door / Container Apps Terraform modules
  - .gitleaks.toml and .gitleaksignore
  - The trial enforcement code: ArchLucid.Application/Tenancy/TrialLimitGate.cs

OBJECTIVE
Two coordinated workstreams that turn the built-but-undeployed commercial
motion into a live, converting funnel:

A) Deploy the marketing/signup pages to a public domain.
B) Take Stripe billing from prompt-B2 in-progress to GA, including
   self-service paid conversion against pricing.json and overage metering
   against the overageRunUsd numbers.

WORKSTREAM A — PUBLIC MARKETING SITE GO-LIVE

A.1 Decide the hosting target via a one-page ADR
   docs/adr/0015-marketing-site-hosting.md:
   - Compare Azure Static Web Apps (preferred for the marketing pages
     because they are server-rendered Next.js with a small surface) vs
     Container Apps revision routing on the existing app.
   - Capture trade-offs (cost, cold-start, custom-domain TLS, Front Door
     integration, shared CSP with the operator UI, deploy decoupling).
   - Pick one. Default recommendation: Container Apps revision routing
     keeps one Next.js bundle, one CI/CD path, and lets us share auth
     middleware exclusion rules.

A.2 Terraform (per workspace IaC rule — all infra in Terraform)
   - infra/modules/marketing/ — Front Door route for /, /welcome, /signup,
     /signup/verify, /pricing, /pricing.json with public anonymous access.
   - Custom domain (parameterized; default: archlucid.net — variable, do
     not hardcode).
   - Managed certificate.
   - WAF policy: keep block rules; add an allow exception only for the
     marketing routes (no auth header required).
   - Application Insights routing for the marketing path with anonymous
     telemetry consent banner.
   - Tag every resource with Workload=marketing for cost separation.
   - terraform plan must be green; do NOT terraform state mv anything
     (per ArchLucid-Rename guardrail; phase 7.5 deferred).

A.3 Marketing site polish
   - Add /pricing/page.tsx that consumes /pricing.json and matches the
     three tiers from PRICING_PHILOSOPHY.md.
   - Add SEO basics: src/app/sitemap.ts, src/app/robots.ts,
     <Metadata> openGraph + twitter on every (marketing) page.
   - Add a privacy-preserving analytics stub (no third-party trackers
     until consent banner ships); default: own page-view counter to
     dbo.MarketingPageViews via a /api/marketing/track edge handler.
   - Confirm WelcomeMarketingPage already pulls copy from POSITIONING.md
     (per commit 4fab1a86); add a unit test that the visible text matches
     POSITIONING.md §3 30-second pitch (string-match on a checked snippet)
     so future positioning edits do not silently desync.

A.4 Public-domain runbook
   docs/runbooks/MARKETING_SITE_GO_LIVE.md:
   - DNS cutover steps (CNAME / A / AAAA).
   - Bing + Google Search Console verification.
   - First-week monitoring checklist.

WORKSTREAM B — STRIPE BILLING GA

B.1 Secret hygiene first
   - Confirm sk_test_… in commit history is a static fixture string.
   - Update .gitleaks.toml with a precise allowlist scoped to
     ArchLucid.Application.Tests/Configuration/ArchLucidConfiguration
     RulesTests.cs line range so future real keys are not allowlisted.
   - Add a regression test that asserts the fixture token literal lives
     ONLY in that test file (rg-style assertion in
     ArchLucid.Architecture.Tests).

B.2 Stripe configuration model
   - Add ArchLucid.Application/Billing/StripeOptions.cs (PublishableKey,
     SecretKey, WebhookSecret, PriceIds per pricing.json package id).
   - User secrets / appsettings.Production.sample.json scaffolding.
   - Do not commit live keys. Document key rotation runbook in
     docs/runbooks/STRIPE_KEY_ROTATION.md.

B.3 Checkout + webhook surfaces
   - POST /v1/billing/checkout-session — creates a Stripe Checkout Session
     mapping selected pricing.json package to the matching Stripe Price ID.
     Tenant context required (must come from authenticated trial user).
   - POST /v1/billing/webhook — Stripe webhook receiver, signature-verified
     against WebhookSecret. Handles: checkout.session.completed,
     invoice.paid, invoice.payment_failed, customer.subscription.updated,
     customer.subscription.deleted.
   - On checkout.session.completed: write to dbo.TenantSubscriptions (new
     table, append-only state log + current-state view) and flip Tenant
     .TrialStatus = "Converted", clear TrialExpiresUtc, set tier per
     PRICING_PHILOSOPHY.md.
   - Idempotency by stripe event id (dbo.StripeEventsProcessed).

B.4 Overage metering
   - ArchLucid.Worker/Jobs/UsageOverageMeteringJob.cs runs nightly.
   - For every Tenant with TrialStatus = "Converted" and tier in {Team,
     Professional}: compute (RunsThisPeriod − IncludedRunsPerMonth) and
     POST a usage record to Stripe metered subscription item.
   - Idempotent per (TenantId, BillingPeriodStartUtc).

B.5 Self-service upgrade UI
   - archlucid-ui/src/app/billing/page.tsx — "Upgrade your trial" page
     that reads /pricing.json, posts to /v1/billing/checkout-session, and
     redirects to Stripe Checkout.
   - Wire from TrialLimitFilter's 402 problem+json — when the operator UI
     receives 402, render "Upgrade now" linking to /billing.

B.6 Migrations
   - 0NN_TenantSubscriptions.sql, 0NN_StripeEventsProcessed.sql in
     ArchLucid.Persistence/Migrations/ (sequential numbers).
   - Mirror in sql/ArchLucid.sql (single-file DDL per workspace rule).
   - Append-only enforcement (DENY UPDATE/DELETE) per AuditEvents pattern.

B.7 Tests (try for 100% coverage on new code)
   - ArchLucid.Application.Tests/Billing/StripeWebhookHandlerTests.cs
     (signature verification, idempotency, all event types).
   - ArchLucid.Api.Tests/Controllers/BillingControllerTests.cs
     (checkout-session creation, webhook signature failure → 400).
   - ArchLucid.Worker.Tests/Jobs/UsageOverageMeteringJobTests.cs
     (overage math; idempotency under repeated runs).
   - ArchLucid.Persistence.Tests/Billing/TenantSubscriptionsRepo
     IntegrationTests.cs (Category=Integration).

B.8 Docs (per Markdown-Generosity rule)
   - docs/BILLING.md — architecture, lifecycle states, error contracts,
     PCI scope (we never touch card data), reconciliation runbook.
   - docs/runbooks/BILLING_INCIDENT_RESPONSE.md — failed webhook, dunning
     state, manual upgrade override.
   - Update docs/API_CONTRACTS.md with the two new endpoints.
   - Update docs/security/TRIAL_LIMITS.md to link the 402 → upgrade path.
   - Update docs/go-to-market/PRICING_PHILOSOPHY.md only by linking,
     never by restating numbers (the CI single-source check enforces).
   - ADR docs/adr/0016-stripe-billing-ga.md.

CROSS-CUTTING CONSTRAINTS
- All infra in Terraform (per workspace rule).
- No SMB / 445 exposure.
- Each new class in its own file.
- Single-line throw without braces after if.
- Blank line before every if/foreach unless first line of method.
- LINQ over foreach unless perf.
- Concrete types over var.
- Always check nulls.
- Comment any non-obvious code; do not narrate the obvious.
- No ConfigureAwait(false) in tests.
- Do not modify existing migration files; new ones use the next
  sequential number; mirror in sql/ArchLucid.sql.
- The Phase 7.5–7.8 ArchiForge → ArchLucid rename remains deferred per
  ArchLucid-Rename rule; do NOT terraform state mv in this change set.

ACCEPTANCE
- dotnet test ArchLucid.sln green (Suite=Core and the new Integration
  tests on local SQL).
- terraform plan against infra/ green; new marketing module dry-run shows
  the expected Front Door route + custom domain + WAF allowlist.
- Manual smoke (against a Stripe test account documented in BILLING.md):
  Trial user → /billing → Checkout → success → tenant tier flipped →
  402 no longer raised on next architecture run.
- Webhook signature failure returns 400; replay of the same event id is
  idempotent.
- gitleaks no longer flags the historical sk_test_… token (allowlisted to
  the specific test file + line range).
- /openapi/v1.json + ArchLucid.Api.Client regenerated; OpenAPI snapshot
  test green.
```

---

## 6. Related documents

| Doc | Use |
|-----|-----|
| [docs/MARKETABILITY_ASSESSMENT_2026_04_15.md](MARKETABILITY_ASSESSMENT_2026_04_15.md) | Prior baseline (mixed model, 42.3%) |
| [docs/MARKETABILITY_ASSESSMENT_2026_04_15_SAAS_ONLY.md](MARKETABILITY_ASSESSMENT_2026_04_15_SAAS_ONLY.md) | SaaS-only companion view (46.1%) |
| [docs/QUALITY_ASSESSMENT_2026_04_14_WEIGHTED.md](../archive/quality/2026-04-23-doc-depth-reorg/QUALITY_ASSESSMENT_2026_04_14_WEIGHTED.md) | Technical quality (orthogonal, 68.5%) |
| [docs/go-to-market/POSITIONING.md](../go-to-market/POSITIONING.md) | Positioning + pitches + category |
| [docs/go-to-market/PRICING_PHILOSOPHY.md](../go-to-market/PRICING_PHILOSOPHY.md) | Pricing single source of truth |
| [docs/go-to-market/IDEAL_CUSTOMER_PROFILE.md](../go-to-market/IDEAL_CUSTOMER_PROFILE.md) | ICP scoring rubric |
| [docs/go-to-market/PMF_VALIDATION_TRACKER.md](../go-to-market/PMF_VALIDATION_TRACKER.md) | PMF hypothesis tracker |
| [docs/go-to-market/REFERENCE_NARRATIVE_TEMPLATE.md](../go-to-market/REFERENCE_NARRATIVE_TEMPLATE.md) | Case-study templates |
| [docs/go-to-market/ORDER_FORM_TEMPLATE.md](../go-to-market/ORDER_FORM_TEMPLATE.md) | Procurement-ready order form |
| [docs/go-to-market/TRIAL_AND_SIGNUP.md](../go-to-market/TRIAL_AND_SIGNUP.md) | Trial flow design (now partly live in code) |
| [docs/security/TRIAL_LIMITS.md](../security/TRIAL_LIMITS.md) | Trial enforcement contract (402 + problem+json) |
| [docs/go-to-market/TRUST_CENTER.md](../go-to-market/TRUST_CENTER.md) | Buyer trust index |
