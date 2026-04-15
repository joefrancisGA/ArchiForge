# ArchLucid Marketability Quality Assessment — 2026-04-15 (post-M1 + M2)

**Overall Marketability Score: 55 / 100** | Weighted: **39.9%**

This is a **marketability** assessment — not a technical quality assessment (see `docs/QUALITY_ASSESSMENT_2026_04_14_WEIGHTED.md`, 68.5%). Marketability measures whether the solution can attract buyers, win competitive evaluations, retain customers, and grow revenue in the enterprise architecture tooling market.

**Prior versions:** `docs/archive/MARKETABILITY_ASSESSMENT_2026_04_15_PRE_M2.md` (pre-M2, 37.6% weighted).

**What changed since last assessment:** Improvement M1 (positioning, personas, competitive landscape) and M2 (product datasheet, screenshot gallery) delivered five documents into `docs/go-to-market/`.

---

## Methodology

Twenty marketability dimensions scored 1–100. Each carries a weight (1–10) reflecting importance to winning and retaining paying customers. Dimensions ordered by **weighted improvement priority** (weight × gap), most-needed-improvement first.

| Range | Meaning |
|-------|---------|
| 90–100 | Market-leading — clear competitive advantage |
| 75–89 | Competitive — can win deals in this area |
| 60–74 | Adequate — not a deal-breaker but not a strength |
| 45–59 | Weak — losing deals because of this |
| Below 45 | Critical — blocking sales or adoption |

---

## Assessments (ordered by weighted improvement priority)

### 1. Go-to-Market Readiness — Score: 40 / 100 (Weight: 10, Weighted Gap: 600)

**Justification:**
- No pricing model, licensing strategy, or packaging tiers.
- No marketing website or landing page.
- **Implemented (M1):** Competitive positioning document (`COMPETITIVE_LANDSCAPE.md`) — 10-competitor matrix, head-to-head differentiation, positioning gaps.
- **Implemented (M1):** Positioning statement, value pillars, elevator pitches, category definition, messaging guidelines (`POSITIONING.md`).
- **Implemented (M2):** Product datasheet (`PRODUCT_DATASHEET.md`) — 2-page buyer-facing collateral ready for PDF export.
- No demo script beyond the technical `demo-quickstart.md`.
- No free trial, freemium tier, or self-service signup pathway.
- ArchiForge remnants remain in Terraform addresses and workspace path.

**Tradeoffs:** GTM readiness requires product marketing investment that may be intentionally deferred until PMF is validated. However, the M1+M2 documents now provide the minimum collateral for a sales conversation.

**Improvement Recommendations:**
1. Define pricing model (per-seat, per-run, platform fee + consumption) and packaging tiers.
2. Develop a 15-minute demo script that tells a business story (not just technical walkthrough).
3. Build a landing page or single-page marketing site using content from the datasheet and positioning docs.

---

### 2. Product-Market Fit Clarity — Score: 47 / 100 (Weight: 9, Weighted Gap: 477)

**Justification:**
- **Implemented (M1):** Buyer personas document (`BUYER_PERSONAS.md`) — three personas with pain points, evaluation criteria, objections, demo priorities, and buying dynamics.
- **Implemented (M1):** Category definition and best-fit/worst-fit scenarios in `COMPETITIVE_LANDSCAPE.md` §6 and `POSITIONING.md` §5.
- `PRODUCT_LEARNING.md` captures pilot feedback signals but no synthesized PMF learnings from actual pilots.
- No documented ICP (Ideal Customer Profile): company size, industry verticals, regulatory environment.
- `PILOT_GUIDE.md` focuses on technical setup, not business success criteria.

**Tradeoffs:** PMF hypotheses exist now (via personas and positioning) but are untested against real buyer data.

**Improvement Recommendations:**
1. Write a PMF hypothesis document with testable success criteria.
2. Formalize an ICP from the buyer personas (company size, industry, regulatory posture).
3. Create a pilot success scorecard with business-outcome metrics.

---

### 3. Value Demonstration / ROI Articulation — Score: 30 / 100 (Weight: 7, Weighted Gap: 490)

**Justification:**
- No ROI model or TCO calculator.
- No case studies, testimonials, or pilot success stories.
- Per-run LLM cost tracking is a known gap — ROI cannot be demonstrated without cost-per-outcome data.
- The datasheet (M2) articulates value but does not quantify it.
- No "measuring success" guidance for pilot champions.

**Tradeoffs:** ROI articulation requires pilot data. The framework for measurement should be designed now.

**Improvement Recommendations:**
1. Build an ROI model template: inputs (team size, review frequency, cycle time), outputs (time saved, compliance gaps caught).
2. Add run-level metrics that feed ROI measurement: elapsed time, finding count, LLM cost.
3. Create a pilot success measurement guide.

---

### 4. Customer Success Infrastructure — Score: 32 / 100 (Weight: 7, Weighted Gap: 476)

**Justification:**
- No usage analytics, feature adoption tracking, or health scoring.
- `ProductLearningPilotSignals` exists but "brains" are deferred.
- No in-app feedback mechanism, NPS/CSAT survey, or customer community.
- No customer-facing knowledge base (docs are developer-internal).
- Support bundle and `doctor` are diagnostic tools, not customer-facing support.

**Tradeoffs:** Heavy customer success tooling is premature for pre-revenue. But basic feedback loops are needed for pilot-to-paid conversion.

**Improvement Recommendations:**
1. Create a customer-facing documentation site separate from developer docs.
2. Add in-product usage analytics (anonymous, opt-out).
3. Build a feedback mechanism into the operator UI (thumbs up/down on findings).

---

### 5. Differentiation and Competitive Moat — Score: 50 / 100 (Weight: 9, Weighted Gap: 450)

**Justification:**
- **Implemented (M1):** Head-to-head differentiation tables for 5 competitor pairs (`COMPETITIVE_LANDSCAPE.md` §4). Category definition in `POSITIONING.md` §5.
- **Implemented (M1):** Value pillars frame `ExplainabilityTrace` as a business benefit, not just a technical feature.
- **Implemented (M2):** Datasheet makes capabilities accessible to non-technical evaluators.
- No moat strategy: no proprietary data advantage, no network effects, no switching costs.
- No ecosystem strategy to build defensibility through community or marketplace.

**Tradeoffs:** Defensibility too early distracts from PMF. But "why us" is now articulated and available for every sales conversation.

**Improvement Recommendations:**
1. Articulate a "10x better" claim with evidence from pilot data.
2. Design a data moat strategy: findings and learning signals compound over time for each customer.
3. Create an ecosystem strategy around the finding engine template.

---

### 6. Time-to-Value / Onboarding Experience — Score: 45 / 100 (Weight: 8, Weighted Gap: 440)

**Justification:**
- Prerequisites are steep: .NET 10 SDK, SQL Server, Docker Desktop, Node.js 22+.
- First-run wizard is shipped and well-designed (7 steps, presets, live tracking).
- `PILOT_GUIDE.md` is thorough but assumes a technical reader.
- No hosted demo/sandbox environment.
- `demo-quickstart.md` requires database configuration — not a 5-minute demo.
- **Implemented (M2):** Screenshot gallery provides a visual preview for prospects who cannot yet run the product.

**Tradeoffs:** Self-hosted software has inherent setup friction. A zero-config Docker demo would dramatically reduce time-to-first-impression.

**Improvement Recommendations:**
1. Build a zero-config Docker demo (`docker-compose.demo.yml`) with pre-seeded data.
2. Create a "5-minute value" video walkthrough.
3. Reduce minimum time-to-first-run to under 10 minutes.

---

### 7. Ecosystem and Integration Breadth — Score: 40 / 100 (Weight: 6, Weighted Gap: 360)

**Justification:**
- Integration surface exists: REST API, OpenAPI, CloudEvents, webhooks, Service Bus, CLI, .NET API client.
- No SDK for non-.NET consumers (Python, JavaScript).
- No connectors to existing architecture tools (Structurizr, ArchiMate, CMDB, Terraform state).
- No ITSM integration (ServiceNow, Jira).
- No CI/CD pipeline examples (GitHub Actions, Azure DevOps).
- AsyncAPI spec exists.

**Tradeoffs:** Broad integration follows PMF. Import from existing tools is critical for adoption.

**Improvement Recommendations:**
1. Build import connectors for top 3 architecture artifact formats (Structurizr DSL, ArchiMate XML, Terraform state).
2. Publish Python and JavaScript SDKs from the OpenAPI spec.
3. Create CI/CD integration examples.

---

### 8. Multi-Cloud / Platform Breadth — Score: 35 / 100 (Weight: 5, Weighted Gap: 325)

**Justification:**
- Azure-only. Other cloud providers disabled as "coming soon" in the wizard.
- Disqualifies AWS-primary and GCP-primary customers (>50% of market).
- Agent runtime designed for multi-vendor LLM but infrastructure is Azure-native.
- No Helm chart for Kubernetes deployment.

**Tradeoffs:** Azure-native is a defensible V1 focus but limits addressable market severely.

**Improvement Recommendations:**
1. Add AWS topology/cost/compliance agent capabilities.
2. Abstract infrastructure dependencies behind provider interfaces.
3. Create a Helm chart for Kubernetes deployment.

---

### 9. Enterprise Readiness — Score: 55 / 100 (Weight: 7, Weighted Gap: 315)

**Justification:**
- Strong foundations: Entra ID, RLS, private endpoints, STRIDE threat model, RBAC, audit trail, ZAP/Schemathesis.
- `CUSTOMER_TRUST_AND_ACCESS.md` is well-structured. 14 ADRs.
- No SOC 2 report or readiness assessment. No compliance framework mappings.
- No GDPR/CCPA DPA template. No BAA. No data residency guarantees.
- Entra-only SSO — blocks non-Microsoft-stack enterprises.
- No SLA commitment document.

**Tradeoffs:** For Azure-first customers the posture is reasonable for V1. Broader enterprise sales need SSO and compliance documentation.

**Improvement Recommendations:**
1. Create a SOC 2 readiness gap analysis.
2. Add generic OIDC support (Okta, Auth0, Ping).
3. Publish a DPA template and data residency doc.

---

### 10. User Experience Polish — Score: 50 / 100 (Weight: 6, Weighted Gap: 300)

**Justification:**
- Operator UI has good breadth: 172 `.tsx` files across runs, manifests, governance, compare, graph, planning, alerts, wizard, search.
- Dark mode, keyboard shortcuts, Radix UI, `aria-live`, sidebar navigation, collapsible groups.
- **Implemented (M2):** Screenshot gallery with 10 capture briefs, annotation guidance, and output conventions provides a path to visual collateral.
- No design system documentation or component gallery.
- UI is labeled "operator shell" — back-office framing, not product-grade.
- No screenshot-based walkthrough yet (gallery defines what to capture but screenshots not yet taken).

**Tradeoffs:** UX polish follows PMF. But in the EA market, non-technical buyers evaluate on visual impression.

**Improvement Recommendations:**
1. Execute the screenshot capture brief and produce the annotated set.
2. Reframe the UI from "operator shell" to "Architecture Intelligence Console."
3. Build a design system doc (color tokens, component gallery).

---

### 11. Content and Thought Leadership — Score: 25 / 100 (Weight: 4, Weighted Gap: 300)

**Justification:**
- No blog, articles, conference talks, whitepapers, or webinar recordings.
- 193+ internal docs is extensive knowledge that could be externalized — none is published.
- No SEO-optimized content. No DevRel presence.
- **Implemented (M1):** Category definition ("AI Architecture Intelligence") in `POSITIONING.md` §5 could serve as the foundation for a category-defining whitepaper.

**Tradeoffs:** Content marketing is premature before PMF. But defining the category through content is a massive advantage in a nascent market.

**Improvement Recommendations:**
1. Extract 5–10 blog posts from internal docs (ADRs, security model, explainability, governance workflow).
2. Create a "State of AI-Assisted Architecture Design" whitepaper.
3. Open-source a non-core component to build developer community.

---

### 12. Business Model Scalability — Score: 42 / 100 (Weight: 5, Weighted Gap: 290)

**Justification:**
- Multi-tenant RLS exists. No self-service provisioning.
- No usage metering or billing integration. Per-run LLM costs not tracked.
- No marketplace listing. Not operable as SaaS without additional platform engineering.
- No white-labeling or OEM capability.

**Tradeoffs:** Building SaaS infrastructure before PMF is premature. Understanding unit economics now is critical for pricing.

**Improvement Recommendations:**
1. Implement per-run cost tracking (LLM tokens × model price + compute time).
2. Design a self-service tenant provisioning workflow.
3. Create an Azure Marketplace listing plan.

---

### 13. Buyer Documentation — Score: 40 / 100 (Weight: 4, Weighted Gap: 240)

**Justification:**
- All 193+ docs are written for developers/SREs/security engineers.
- **Implemented (M2):** Product datasheet (`PRODUCT_DATASHEET.md`) is the first buyer-facing document. Written for CTO audience.
- **Implemented (M1):** Positioning and personas docs provide buyer-facing language and framing.
- No architecture overview for non-technical stakeholders beyond the datasheet.
- No "Why ArchLucid" standalone one-pager (positioning doc contains the content but is multi-page).

**Tradeoffs:** Developer docs should stay developer-focused. Buyer docs now have a foundation in `go-to-market/`.

**Improvement Recommendations:**
1. Create a "Why ArchLucid" one-pager extracted from the positioning doc.
2. Create a capability matrix comparing ArchLucid to manual architecture review.
3. Build a buyer-facing FAQ document.

---

### 14. Pilot-to-Paid Conversion Path — Score: 40 / 100 (Weight: 4, Weighted Gap: 240)

**Justification:**
- `PILOT_GUIDE.md` and `OPERATOR_QUICKSTART.md` provide good technical onboarding.
- **Implemented (M1):** Buyer personas include demo priorities and objection responses — useful for conversion conversations.
- No commercial pilot agreement template. No pilot success criteria tied to business outcomes.
- No "pilot → production" upgrade path. No account expansion playbook.
- No champion enablement kit.

**Tradeoffs:** Conversion is a sales process problem as much as a product problem. Product infrastructure that supports conversion is essential.

**Improvement Recommendations:**
1. Create a pilot-to-production upgrade guide.
2. Build a "value report" generated from pilot data.
3. Write a champion enablement kit (executive summary, business case template, procurement FAQ).

---

### 15. Partner and Channel Readiness — Score: 20 / 100 (Weight: 3, Weighted Gap: 240)

**Justification:**
- No partner program, SI relationships, or consulting firm partnerships.
- No white-label or OEM capability. No implementation partner documentation.
- DOCX export for consulting templates suggests awareness of use case but no partnership structure.

**Tradeoffs:** Premature. But the channel strategy informs product design (customizable templates, partner branding).

**Improvement Recommendations:**
1. Design a consulting firm partnership model.
2. Create customizable DOCX templates for partner branding.
3. Document a partner implementation guide.

---

### 16. Vertical / Industry Readiness — Score: 30 / 100 (Weight: 3, Weighted Gap: 210)

**Justification:**
- No industry-specific policy packs, compliance mappings, or demo scenarios.
- Policy pack system is flexible enough but no reference implementations.
- Generic presets only ("Greenfield web app," "Modernize legacy system").

**Tradeoffs:** Vertical specialization follows horizontal PMF. But "do you support our regulatory requirements" is a sales qualification question.

**Improvement Recommendations:**
1. Create policy pack reference implementations for top 2 verticals (financial services, healthcare).
2. Map finding categories to SOC 2 / ISO 27001 controls.
3. Build one industry-specific demo scenario.

---

### 17. Community and Ecosystem — Score: 15 / 100 (Weight: 2, Weighted Gap: 170)

**Justification:**
- No open-source community, developer forum, Discord/Slack, public issue tracker, or user group.
- `dotnet new archlucid-finding-engine` template is a foundation but no distribution mechanism.

**Tradeoffs:** Community requires product maturity. Even a small early-adopter group provides invaluable feedback.

**Improvement Recommendations:**
1. Establish a GitHub Discussions or Discord for pilot users.
2. Open-source the finding engine template and SDK.

---

### 18. Internationalization / Localization — Score: 22 / 100 (Weight: 2, Weighted Gap: 156)

**Justification:**
- English-only throughout. No i18n framework in the Next.js UI. No data residency options.

**Tradeoffs:** i18n follows demand. For V1 targeting English-speaking Azure customers, acceptable but limits TAM.

**Improvement Recommendations:**
1. Add i18n framework to the UI as a foundation.
2. Externalize user-facing strings in the API.

---

### 19. Brand Identity — Score: 30 / 100 (Weight: 2, Weighted Gap: 140)

**Justification:**
- Product name "ArchLucid" is distinctive and well-chosen.
- No logo, visual brand, color palette, or typography system.
- Rename incomplete (Terraform addresses, workspace path).
- UI uses Tailwind defaults.
- **Implemented (M1):** Tagline options in `POSITIONING.md` §6.

**Tradeoffs:** Brand investment follows PMF. Minimal brand (logo + 3 colors) improves professional perception significantly.

**Improvement Recommendations:**
1. Commission or create a logo and minimal brand guide.
2. Apply brand to the UI (custom Tailwind theme).

---

### 20. Market Timing / Category Definition — Score: 60 / 100 (Weight: 2, Weighted Gap: 80)

**Justification:**
- Timing is favorable: AI-assisted enterprise architecture is emerging with high interest.
- **Implemented (M1):** "AI Architecture Intelligence" category explicitly defined in `POSITIONING.md` §5 with positioning diagram.
- Category is crowded with point solutions but ArchLucid's combination is unique.
- No evidence of urgency or go-to-market timeline in product docs.

**Tradeoffs:** Early is advantageous but requires aggressive market education.

**Improvement Recommendations:**
1. Move quickly to establish reference customers.
2. Publish category-defining content before incumbents catch up.

---

## Summary Table (sorted by weighted gap, descending)

| Rank | Marketability Area | Weight | Score | Gap | Weighted Gap | Grade | M1/M2 Impact |
|------|-------------------|--------|-------|-----|-------------|-------|-------------|
| 1 | **Go-to-Market Readiness** | 10 | 40 | 60 | **600** | Critical | M1 +10, M2 +2 |
| 2 | **Value Demo / ROI** | 7 | 30 | 70 | **490** | Critical | — |
| 3 | **Product-Market Fit Clarity** | 9 | 47 | 53 | **477** | Weak | M1 +12 |
| 4 | **Customer Success Infra** | 7 | 32 | 68 | **476** | Critical | — |
| 5 | **Differentiation & Moat** | 9 | 50 | 50 | **450** | Weak | M1 +10, M2 +2 |
| 6 | **Time-to-Value / Onboarding** | 8 | 45 | 55 | **440** | Weak | M2 visual preview |
| 7 | **Ecosystem & Integration** | 6 | 40 | 60 | **360** | Critical | — |
| 8 | **Multi-Cloud / Platform** | 5 | 35 | 65 | **325** | Critical | — |
| 9 | **Enterprise Readiness** | 7 | 55 | 45 | **315** | Weak | — |
| 10 | **UX Polish** | 6 | 50 | 50 | **300** | Weak | M2 +2 |
| 11 | **Content & Thought Leadership** | 4 | 25 | 75 | **300** | Critical | M1 category foundation |
| 12 | **Business Model Scalability** | 5 | 42 | 58 | **290** | Critical | — |
| 13 | **Buyer Documentation** | 4 | 40 | 60 | **240** | Critical | M1 +5, M2 +5 |
| 14 | **Pilot-to-Paid Conversion** | 4 | 40 | 60 | **240** | Critical | M1 persona demo priorities |
| 15 | **Partner & Channel** | 3 | 20 | 80 | **240** | Critical | — |
| 16 | **Vertical / Industry** | 3 | 30 | 70 | **210** | Critical | — |
| 17 | **Community & Ecosystem** | 2 | 15 | 85 | **170** | Critical | — |
| 18 | **Internationalization** | 2 | 22 | 78 | **156** | Critical | — |
| 19 | **Brand Identity** | 2 | 30 | 70 | **140** | Critical | M1 taglines |
| 20 | **Market Timing** | 2 | 60 | 40 | **80** | Adequate | M1 +5 (category defined) |

**Overall weighted marketability score:** 4,227 / 10,600 = **39.9%** (was 35.0% pre-M1, 37.6% pre-M2)

**Unweighted average:** 37.3 / 100

**Score trajectory:**

| Milestone | Weighted % | Unweighted avg |
|-----------|-----------|----------------|
| Pre-M1 | 35.0% | 35.0 |
| Post-M1 | 37.6% | 36.4 |
| Post-M2 | 39.9% | 37.3 |

**Interpretation:** Two improvement cycles have raised the weighted score by 4.9 percentage points (35.0% → 39.9%). The most significant gains are in the three highest-weighted areas: GTM (+12), PMF (+12), and Differentiation (+12). The product now has minimum viable positioning collateral (competitive landscape, personas, positioning, datasheet, screenshot brief). The largest remaining gaps are **Value Demo / ROI** (490), **Customer Success Infrastructure** (476), and **Time-to-Value** (440) — these require a mix of documentation and product engineering work.

---

## Six Best Improvements (ordered by weighted impact and feasibility)

### Improvement 1: Product Positioning and Competitive Analysis — Status: DONE (M1)

**Delivered:** `COMPETITIVE_LANDSCAPE.md`, `BUYER_PERSONAS.md`, `POSITIONING.md`.

**Impact:** GTM 28→40, PMF 35→47, Differentiation 38→50.

---

### Improvement 2: Product Datasheet and Screenshot Gallery — Status: DONE (M2)

**Delivered:** `PRODUCT_DATASHEET.md`, `SCREENSHOT_GALLERY.md`.

**Impact:** GTM +2, UX Polish +2, Buyer Documentation +5. First externally-shareable collateral.

---

### Improvement 3: ROI Model and Pilot Success Scorecard (Value Demo + Pilot Conversion; combined weighted gap: 730)

**What:** Create an ROI model template and pilot success measurement guide — the tools a champion needs to justify a purchase.

**Cursor Prompts:**

```
Improvement M3 — Prompt `roi-model`

Create docs/go-to-market/ROI_MODEL.md with:

1. Objective: Help pilot champions build a business case for purchasing ArchLucid.

2. Cost of the status quo (inputs to collect from the customer):
   - Number of architecture reviews per quarter
   - Average hours per review (architect time + stakeholder review + documentation)
   - Average architect fully-loaded cost per hour
   - Number of compliance gaps found in production (post-deployment)
   - Average cost of a compliance remediation
   - Number of architecture inconsistencies across teams

3. ArchLucid value model (mapped to product capabilities):
   - Time reduction: architecture review cycle from X weeks to Y hours
   - Consistency improvement: standardized findings across all reviews
   - Compliance shift-left: findings before deployment, not after
   - Audit trail: automatic vs. manual documentation
   - Knowledge reuse: comparison/replay across iterations

4. ROI calculation template with example scenario

5. Intangible benefits section

Ground claims in actual V1_SCOPE.md capabilities. Use conservative estimates.
```

```
Improvement M3 — Prompt `pilot-success-scorecard`

Create docs/go-to-market/PILOT_SUCCESS_SCORECARD.md with:

1. Quantitative metrics (measure before and after):
   - Time to complete an architecture review
   - Number of findings per review
   - Percentage of findings with full explainability trace
   - Governance compliance rate

2. Qualitative metrics (stakeholder interviews, 1-5 scale)

3. Data collection plan (6-week pilot timeline)

4. Success criteria (minimum, target, stretch)

5. Report template for the champion to present to leadership

Reference PILOT_GUIDE.md and PRODUCT_LEARNING.md for existing data collection.
```

---

### Improvement 4: Zero-Config Docker Demo (Time-to-Value; weighted gap: 440)

**What:** One-command Docker experience with pre-seeded data for 5-minute prospect evaluation.

**Cursor Prompt:**

```
Improvement M4 — Prompt `zero-config-demo`

1. Read docker-compose.yml and docs/CONTAINERIZATION.md.

2. Create docker-compose.demo.yml overlay that sets:
   Demo:Enabled=true, Demo:SeedOnStartup=true, AgentExecution:Mode=Simulator,
   ArchLucidAuth:Mode=DevelopmentBypass, UI proxy to API container.

3. Create scripts/demo-start.ps1 and scripts/demo-start.sh:
   check Docker → compose up → wait for health → open browser.

4. Create docs/go-to-market/DEMO_QUICKSTART.md (buyer-facing):
   prerequisites (Docker only), one command, 5-minute walkthrough, cleanup.

Do not change production docker-compose.yml. Demo overlay is additive.
```

---

### Improvement 5: Generic OIDC Support (Enterprise Readiness; weighted gap: 315)

**What:** Add generic OIDC provider support (Okta, Auth0, Ping) alongside Entra ID.

**Cursor Prompt:**

```
Improvement M5 — Prompt `generic-oidc-auth`

1. Read ArchLucid.Host.Core/Startup/ for existing auth (AddArchLucidAuth,
   ArchLucidAuthOptions, ArchLucidRoleClaimsTransformation).

2. Add ArchLucidAuth:Mode "OpenIdConnect" with configurable:
   Authority, ClientId, Audience, RoleClaimType, role value mappings.

3. Reuse ArchLucidRoleClaimsTransformation for provider-specific claim mapping.

4. Add appsettings.Okta.sample.json and appsettings.Auth0.sample.json.

5. Add tests in ArchLucid.Host.Composition.Tests.

6. Update docs/SECURITY.md and CUSTOMER_TRUST_AND_ACCESS.md.

Keep JwtBearer mode as-is. New mode is additive.
```

---

### Improvement 6: CI/CD Integration Examples and Terraform Import (Ecosystem; weighted gap: 360)

**What:** GitHub Actions and Azure DevOps pipeline examples plus a Terraform state import connector.

**Cursor Prompts:**

```
Improvement M6 — Prompt `cicd-integration-examples`

1. Create docs/integrations/CICD_INTEGRATION.md:
   why, pattern (PR → ArchLucid run → findings as PR comment → governance gate).

2. Create examples/github-actions/archlucid-review.yml.

3. Create examples/azure-devops/archlucid-review.yml.

4. Document API calls used. Templates for customization.
```

```
Improvement M6 — Prompt `terraform-state-import`

1. Read ArchLucid.ContextIngestion/ for IContextConnector and CanonicalObject.

2. Create TerraformStateConnector.cs:
   parse terraform show -json output, extract resources/dependencies,
   map to CanonicalObject records.

3. Tests: sample state, empty state, malformed JSON.

4. Update docs/CONTEXT_INGESTION.md.
```

---

## Related documents

| Doc | Use |
|-----|-----|
| `docs/go-to-market/COMPETITIVE_LANDSCAPE.md` | 10-competitor analysis (M1) |
| `docs/go-to-market/BUYER_PERSONAS.md` | Three buyer personas (M1) |
| `docs/go-to-market/POSITIONING.md` | Positioning, pitches, category definition (M1) |
| `docs/go-to-market/PRODUCT_DATASHEET.md` | 2-page buyer-facing datasheet (M2) |
| `docs/go-to-market/SCREENSHOT_GALLERY.md` | 10-screenshot capture brief (M2) |
| `docs/QUALITY_ASSESSMENT_2026_04_14_WEIGHTED.md` | Technical quality assessment (68.5%) |
| `docs/archive/MARKETABILITY_ASSESSMENT_2026_04_15_PRE_M2.md` | Prior assessment (37.6%) |
