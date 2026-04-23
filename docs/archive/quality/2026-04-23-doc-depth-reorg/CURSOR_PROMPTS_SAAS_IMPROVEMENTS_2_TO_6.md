> Archived 2026-04-23 — superseded by [docs/START_HERE.md](../START_HERE.md) and the current assessment pair under ``docs/``. Kept for audit trail.

> **Scope:** Cursor prompts — SaaS-only improvements 2–6 - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Cursor prompts — SaaS-only improvements 2–6

Executable prompts for the remaining five SaaS-only marketability improvements identified in `docs/MARKETABILITY_ASSESSMENT_2026_04_15_SAAS_ONLY.md`. **Improvement 1 (Trust Center spine)** is complete.

Each improvement section lists self-contained prompts. Run them in order within an improvement; across improvements, order is by priority (2 first, then 3, etc.). Each session should end by updating `docs/ARCHLUCID_RENAME_CHECKLIST.md` progress log and cross-linking new docs.

---

## Improvement 2 — Publish SaaS operational posture

**Goal:** Give buyers and their security reviewers a **concrete** view of how ArchLucid operates as a service — uptime commitments, backup/DR, data residency, and a status communication channel. This addresses the **#2 weighted gap** (SaaS platform, score 25, weight 9).

**Dimensions affected:** SaaS platform & reliability, enterprise readiness, pilot-to-paid conversion.

### Prompt 2a — Buyer-facing SLO and SLA summary

```
Create `docs/go-to-market/SLA_SUMMARY.md` — a buyer-readable summary of ArchLucid's
service level commitments as a SaaS vendor.

**Source material (read first):**
- `docs/API_SLOS.md` — internal SLO table (99.5% availability, p95 latency, error rate)
- `docs/go-to-market/TRUST_CENTER.md` — existing trust index
- `docs/go-to-market/INCIDENT_COMMUNICATIONS_POLICY.md` — severity + comms timelines

**Structure:**
1. One-paragraph commitment: "ArchLucid targets [X]% monthly availability for the
   production API…"
2. SLO table translated to buyer language — avoid Prometheus/OTel references:
   - Availability: target, measurement, what counts as downtime
   - Latency: p95 under 2s (initial guardrail)
   - Planned maintenance: notice window commitment (e.g. 72 hours)
3. Service credits (placeholder section — note "to be defined in commercial SLA;
   this document describes targets, not contractual obligations").
4. Exclusions — scheduled maintenance, force majeure, customer-caused issues.
5. How we measure — brief mention of synthetic probes, internal monitoring;
   link to `docs/API_SLOS.md` for engineering depth.
6. Incident response — link to `INCIDENT_COMMUNICATIONS_POLICY.md`.
7. Status page placeholder: "[TBD — URL for public status page]".

**Tone:** Confident but honest. Use "target" and "objective" where there is no
contractual SLA yet; do not claim guarantees that don't exist.

Cross-link from `docs/go-to-market/TRUST_CENTER.md` (add row in trust documents table).
```

### Prompt 2b — Backup, DR, and data lifecycle summary

```
Create `docs/go-to-market/BACKUP_AND_DR.md` — buyer-facing summary of ArchLucid's
backup, disaster recovery, and data lifecycle posture.

**Source material (read first):**
- `docs/runbooks/GEO_FAILOVER_DRILL.md` — internal geo-failover drill
- `docs/DEPLOYMENT_TERRAFORM.md` — infrastructure layout
- `docs/security/MULTI_TENANT_RLS.md` — data isolation
- `docs/go-to-market/SUBPROCESSORS.md` — Azure services and data residency
- `infra/terraform-sql-failover/` — SQL failover group Terraform module

**Structure:**
1. Objective: state backup and recovery posture honestly.
2. Backup schedule and retention:
   - Azure SQL: point-in-time restore window (state Azure defaults or configured
     values; if not configured, note "Azure SQL default" and link to Microsoft docs)
   - Blob storage: versioning/soft-delete if configured; otherwise state "not yet
     configured" and roadmap note
3. Disaster recovery:
   - SQL failover group (reference `infra/terraform-sql-failover/`)
   - RTO/RPO targets: state current best estimate or "to be formalized" with
     realistic placeholder (e.g. RPO < 5 min, RTO < 1 hour for SQL failover)
   - Geo-failover drill: note that an internal drill runbook exists and is
     exercised periodically; do NOT link the internal runbook directly in
     buyer-facing doc
4. Data lifecycle:
   - Retention defaults ("keep until archived/deleted by operator workflow")
   - Data deletion on termination — link to `DPA_TEMPLATE.md` §9
   - Archival capabilities (runs, manifests — `ArchivedUtc` columns)
5. What we do NOT claim (yet):
   - Cross-region active-active
   - Customer-controlled backup schedules
   - Blob geo-replication (unless configured)

Cross-link from `TRUST_CENTER.md`.
```

### Prompt 2c — Status page and operational transparency plan

```
Create `docs/go-to-market/OPERATIONAL_TRANSPARENCY.md` — plan for public operational
transparency as a SaaS vendor.

**Structure:**
1. Why: buyers need confidence that outages will be visible, not hidden.
2. Status page options (evaluate briefly):
   - Hosted: Atlassian Statuspage, Instatus, Cachet (self-hosted)
   - Minimal: GitHub repo with status updates + RSS feed
   - Recommendation: start with simplest option that shows current status,
     incident history, and upcoming maintenance
3. Integration points:
   - Prometheus/Grafana alerts → status page updates (manual initially;
     automate later)
   - `INCIDENT_COMMUNICATIONS_POLICY.md` severity levels → status page
     component states (operational, degraded, major outage, maintenance)
4. Components to track on status page:
   - API availability
   - UI availability
   - Agent pipeline (run execution)
   - Authentication (Entra)
   - Background processing (worker)
5. Implementation plan (phased):
   - Phase 1: Choose provider, create page, add to `TRUST_CENTER.md`
   - Phase 2: Manual incident updates aligned with comms policy
   - Phase 3: Automated uptime checks feeding the page
6. Placeholder: add "[TBD — status page URL]" in `SLA_SUMMARY.md` and
   `INCIDENT_COMMUNICATIONS_POLICY.md`

This is a planning document, not an implementation. No code changes.
Cross-link from `TRUST_CENTER.md`.
```

---

## Improvement 3 — Unblock commercial motion

**Goal:** Define how ArchLucid is **priced, purchased, and billed** so that interested prospects can become paying customers without a fully custom enterprise sales cycle. Addresses the **#1 weighted gap** (GTM, score 30, weight 10).

**Dimensions affected:** GTM, business model & scalability, pilot-to-paid conversion.

### Prompt 3a — Pricing philosophy and packaging tiers

```
Create `docs/go-to-market/PRICING_PHILOSOPHY.md` — ArchLucid's pricing strategy
and packaging tier design.

**Source material (read first):**
- `docs/go-to-market/ROI_MODEL.md` — cost structure, break-even analysis (180
  architect-hours/year), value levers
- `docs/go-to-market/BUYER_PERSONAS.md` — three personas with budget authority ranges
- `docs/go-to-market/COMPETITIVE_LANDSCAPE.md` — competitor pricing where known
- `docs/go-to-market/PILOT_SUCCESS_SCORECARD.md` — pilot measurement framing

**Structure:**
1. Pricing principles:
   - Value-based, not cost-plus
   - Predictable for buyer budgeting
   - Expansion-friendly (grow with usage, not just headcount)
   - Competitive with manual architecture review cost (anchor to ROI model)
2. Pricing model options (evaluate trade-offs for each):
   - Per-seat (architect): simple, predictable, but caps adoption
   - Per-run (usage): aligns with value, but unpredictable for buyers
   - Platform fee + consumption: predictable base + usage upside
   - Recommendation with reasoning
3. Packaging tiers (3 tiers — naming TBD):
   - Starter / Team: small teams, basic features, X runs/month
   - Professional: governance workflows, advanced analysis, Y runs/month
   - Enterprise: custom SLA, SSO beyond Entra, dedicated support, custom limits
   - For each tier: included features, limits, target persona, price range
     placeholder
4. Pilot pricing: how free/discounted pilots map to tiers; link to
   `PILOT_SUCCESS_SCORECARD.md`
5. Expansion levers: what drives account growth (more seats, more runs,
   more workspaces, governance add-ons)
6. What is NOT included (professional services, custom connectors, training)

**Important:** Use placeholder price ranges (e.g. "$X–$Y/seat/month") — do not
commit to specific numbers. This is a strategy document, not a public price list.
The goal is internal alignment before external publication.
```

### Prompt 3b — Trial and signup experience design

```
Create `docs/go-to-market/TRIAL_AND_SIGNUP.md` — design for ArchLucid's self-serve
trial and signup experience.

**Source material (read first):**
- `docs/go-to-market/PRICING_PHILOSOPHY.md` (from prompt 3a)
- `docs/go-to-market/DEMO_QUICKSTART.md` — existing Docker demo (seller-led)
- `docs/go-to-market/BUYER_PERSONAS.md` — who signs up and why

**Structure:**
1. Goal: prospect → active trial in < 5 minutes with no sales contact required.
2. Signup flow design (user journey):
   - Landing page → email + company → Entra social login or email/password →
     provision tenant → first-run wizard → pre-loaded sample run →
     "your first architecture review" guided experience
3. Trial parameters:
   - Duration (e.g. 14 days)
   - Feature limits (e.g. Starter tier features, N runs, single workspace)
   - Data: pre-seeded sample project vs empty workspace (recommend pre-seeded
     using Docker demo seed data pattern)
   - What happens at trial end (read-only access? data export? deletion timeline?)
4. Technical requirements (high level, not implementation):
   - Multi-tenant provisioning automation
   - Trial-specific feature flags
   - Usage metering for trial-to-paid conversion tracking
   - Billing integration touchpoints (Stripe, Azure Marketplace, or similar)
5. Conversion triggers:
   - In-app upgrade prompts (approaching limits, trial expiration)
   - Email nurture sequence (days 1, 3, 7, 12)
   - Champion enablement: auto-generated pilot scorecard stub
6. Relationship to Docker demo: Docker demo is for **seller-led** evaluation;
   trial is for **buyer-led** self-serve. Both should offer similar first impressions.

This is a design document for product and engineering planning. No code changes.
```

### Prompt 3c — Order form and MSA template

```
Create `docs/go-to-market/ORDER_FORM_TEMPLATE.md` — a minimal order form / MSA
pattern for SMB-midmarket ArchLucid SaaS subscriptions.

**Important disclaimer at top:** "This is a working template. It does NOT constitute
legal advice. Qualified legal counsel must review before use."

**Source material (read first):**
- `docs/go-to-market/PRICING_PHILOSOPHY.md` (from prompt 3a)
- `docs/go-to-market/DPA_TEMPLATE.md` — existing DPA (referenced by order form)
- `docs/go-to-market/SLA_SUMMARY.md` — SLO targets (from prompt 2a)

**Structure:**
1. Order form fields:
   - Customer legal entity, contact, billing address
   - Selected tier and quantity (seats / workspaces)
   - Subscription term (annual / monthly)
   - Pricing (placeholder "$___/seat/month")
   - Start date, renewal terms (auto-renew with notice period)
2. Incorporated terms:
   - Link to Terms of Service (placeholder URL)
   - Link to DPA (`DPA_TEMPLATE.md`)
   - Link to SLA targets (`SLA_SUMMARY.md`)
   - Acceptable Use Policy (placeholder)
3. Payment terms: Net 30, invoicing, accepted methods
4. Termination: notice period, data export/deletion per DPA §9
5. Signature block

Keep it short (1–2 pages equivalent). The goal is to reduce friction for
deals < $50K ARR where a full enterprise MSA is overkill.

Cross-link from `TRUST_CENTER.md` if appropriate.
```

---

## Improvement 4 — Customer success minimum viable

**Goal:** Build the minimum infrastructure so that customers do not churn silently. Addresses **Customer success & retention** (score 33, weight 8) — the **#4 weighted gap**.

**Dimensions affected:** Customer success, pilot-to-paid conversion, time-to-value.

### Prompt 4a — Customer onboarding checklist and playbook

```
Create `docs/go-to-market/CUSTOMER_ONBOARDING_PLAYBOOK.md` — structured onboarding
checklist and playbook for new ArchLucid SaaS customers.

**Source material (read first):**
- `docs/go-to-market/PILOT_SUCCESS_SCORECARD.md` — pilot timeline and metrics
- `docs/OPERATOR_QUICKSTART.md` — existing technical quickstart
- `docs/PILOT_GUIDE.md` — existing pilot guide
- `docs/go-to-market/BUYER_PERSONAS.md` — who is onboarding

**Structure:**
1. Onboarding phases (align with pilot scorecard 6-week timeline):
   - **Week 0 (Pre-launch):** Tenant provisioned, SSO configured, admin account
     active, welcome email sent, kickoff call scheduled
   - **Week 1 (Foundation):** Admin completes first-run wizard, team invited,
     first sample run executed, success criteria agreed (per scorecard)
   - **Week 2–3 (Adoption):** First real architecture review submitted,
     governance workflow configured, team completes 3+ runs
   - **Week 4–5 (Expansion):** Comparison runs, policy packs explored,
     governance approvals in production use
   - **Week 6 (Review):** Pilot scorecard completed, results presented to
     leadership, renewal/expansion discussion
2. For each phase: checklist items, owner (customer vs ArchLucid), definition
   of done, common blockers and resolution
3. Touchpoint schedule: kickoff call, week 1 check-in, mid-pilot review,
   end-of-pilot scorecard review
4. Health signals (what to watch for):
   - Green: runs per week increasing, multiple operators active, governance used
   - Yellow: single operator only, no governance, declining run frequency
   - Red: no runs after week 2, support tickets with no resolution, trial
     about to expire with no engagement
5. Handoff to steady-state: link to renewal playbook (prompt 4c)

Cross-link from `TRUST_CENTER.md` and `PILOT_SUCCESS_SCORECARD.md`.
```

### Prompt 4b — Customer health scoring framework

```
Create `docs/go-to-market/CUSTOMER_HEALTH_SCORING.md` — framework for measuring
customer health in the ArchLucid SaaS product.

**Source material (read first):**
- `docs/go-to-market/PILOT_SUCCESS_SCORECARD.md` — pilot metrics (quantitative)
- `docs/go-to-market/CUSTOMER_ONBOARDING_PLAYBOOK.md` (from prompt 4a)
- `docs/PRODUCT_LEARNING.md` — existing product learning signals

**Structure:**
1. Purpose: detect churn risk early; identify expansion opportunities; give CS
   team a single health score per account.
2. Health dimensions and signals (map to data sources where possible):
   - **Engagement:** runs per week, unique active operators, governance actions
     (data source: `dbo.Runs`, `dbo.AuditEvents`)
   - **Breadth:** finding engine types used, comparison runs, export frequency
     (data source: run metadata, audit events)
   - **Quality:** average agent output quality score, trace completeness ratio
     (data source: OTel metrics)
   - **Governance adoption:** approval requests created/resolved, policy packs
     configured (data source: governance tables, audit)
   - **Support:** ticket volume, severity distribution, time-to-resolution
     (data source: external support tool — placeholder)
3. Scoring model (simple weighted composite):
   - Each dimension: 1–5 scale
   - Weights: engagement (30%), breadth (20%), quality (15%), governance (20%),
     support (15%)
   - Composite: 1–5 mapped to health status (healthy / needs attention / at risk)
4. Implementation phases:
   - Phase 1 (manual): CS team fills in spreadsheet monthly from SQL queries
     and support data
   - Phase 2 (semi-automated): scheduled SQL report emailed to CS
   - Phase 3 (in-product): admin dashboard with health metrics
5. Action playbooks per health status:
   - Healthy: expansion conversation, case study request
   - Needs attention: proactive check-in, training offer, feature guidance
   - At risk: escalation to account exec, executive sponsor engagement

This is a CS process document. No code changes required for Phase 1.
```

### Prompt 4c — Renewal and expansion playbook

```
Create `docs/go-to-market/RENEWAL_EXPANSION_PLAYBOOK.md` — playbook for renewals
and account expansion.

**Source material (read first):**
- `docs/go-to-market/CUSTOMER_HEALTH_SCORING.md` (from prompt 4b)
- `docs/go-to-market/ROI_MODEL.md` — ROI framework for expansion justification
- `docs/go-to-market/PRICING_PHILOSOPHY.md` (from prompt 3a)
- `docs/go-to-market/PILOT_SUCCESS_SCORECARD.md` — success evidence

**Structure:**
1. Renewal timeline (annual subscription assumed):
   - 90 days before: health score review, usage trend analysis
   - 60 days before: renewal conversation with champion, value review
     using ROI model actuals vs pilot projections
   - 30 days before: commercial terms, pricing adjustment if tier change
   - Renewal date: signed order form, billing update
2. Expansion triggers:
   - New team or business unit requesting access (add seats/workspaces)
   - Governance adoption expanding (policy packs, approval workflows)
   - New use case (compliance reviews, cost optimization, security reviews)
   - Pilot scorecard results exceeding stretch goals
3. Expansion motion:
   - Champion enablement: provide updated ROI model with actual data
   - Executive sponsor: present value report to CTO/VP
   - Technical: provision additional workspaces, configure SSO for new groups
4. Churn prevention:
   - At-risk signals from health scoring → immediate CS intervention
   - Exit interview template (if customer churns — learn why)
   - Win-back strategy for former customers (feature updates, re-trial)
5. Metrics to track:
   - Net revenue retention (NRR) target: placeholder (e.g. > 110%)
   - Gross churn rate target
   - Expansion revenue as % of total

Cross-link from `CUSTOMER_ONBOARDING_PLAYBOOK.md`.
```

---

## Improvement 5 — Integrations as product

**Goal:** Frame ArchLucid's integration surface as **vendor-managed connectors** rather than "install our agent in your environment." Addresses **Technology ecosystem** (score 38, weight 6).

**Dimensions affected:** Technology ecosystem, differentiation, time-to-value.

### Prompt 5a — Integration catalog and connector roadmap

```
Create `docs/go-to-market/INTEGRATION_CATALOG.md` — buyer-facing catalog of
ArchLucid integrations and connector roadmap.

**Source material (read first):**
- `docs/API_CONTRACTS.md` — existing REST API surface
- `docs/go-to-market/COMPETITIVE_LANDSCAPE.md` §5 — positioning gaps (no
  inbound connectors, no ITSM, no CI/CD examples)
- `docs/SECURITY.md` — auth modes (Entra, API key)
- `ArchLucid.ContextIngestion/Interfaces/IContextConnector.cs` — connector interface
- Existing CloudEvents / webhooks / Service Bus integration

**Structure:**
1. Integration philosophy: "ArchLucid connects to your tools — you do not run
   our agents in your infrastructure."
2. Available today (V1):
   - REST API (OpenAPI 3.0) — link to `/openapi/v1.json`
   - .NET API client NuGet package
   - CLI (`archlucid` command)
   - Webhook / CloudEvents outbound notifications
   - Optional Service Bus integration events
   - AsyncAPI contract
3. Authentication for integrations:
   - Entra ID (JWT) — recommended
   - API keys — automation use cases
   - Link to `TENANT_ISOLATION.md` and `TRUST_CENTER.md`
4. Planned connectors (roadmap — use "[Planned]" tags):
   - **Identity:** SCIM provisioning for user/group sync
   - **Architecture import:** Structurizr DSL, ArchiMate XML, Terraform state
   - **ITSM:** Jira, Azure DevOps — finding → ticket sync
   - **Observability export:** SIEM-friendly audit log export (CEF, syslog)
   - **CI/CD:** GitHub Actions and Azure DevOps pipeline templates
5. Build your own: `IContextConnector` interface, finding engine template
   (`dotnet new archlucid-finding-engine`)
6. Request an integration: contact placeholder

Cross-link from `TRUST_CENTER.md` and `POSITIONING.md`.
```

### Prompt 5b — CI/CD integration examples

```
Create CI/CD integration example files and documentation.

**Source material (read first):**
- `docs/API_CONTRACTS.md` — API endpoints for creating runs, checking status
- `docs/go-to-market/INTEGRATION_CATALOG.md` (from prompt 5a)
- `ArchLucid.Api.Client/` — .NET API client

**Deliverables:**

1. Create `examples/github-actions/archlucid-architecture-review.yml`:
   - Triggers on PR (architecture-related file changes)
   - Calls ArchLucid API: create run → poll for completion → fetch findings
   - Posts findings summary as PR comment
   - Fails the check if findings above configurable severity threshold
   - Uses secrets: `ARCHLUCID_API_URL`, `ARCHLUCID_API_KEY`
   - Include clear comments explaining each step

2. Create `examples/azure-devops/archlucid-architecture-review.yml`:
   - Same flow adapted for Azure DevOps pipelines
   - Uses pipeline variables for API URL and key

3. Create `docs/integrations/CICD_INTEGRATION.md`:
   - Why: shift architecture review left into the PR workflow
   - Pattern: PR change → ArchLucid run → findings as feedback → governance gate
   - Setup instructions for each platform (GitHub Actions, Azure DevOps)
   - Configuration options (severity threshold, which file changes trigger)
   - Security: API key management, least-privilege role (Reader or Operator)
   - Limitations: V1 does not auto-detect architecture drift from code;
     runs require explicit context input

Cross-link from `INTEGRATION_CATALOG.md` and `README.md`.
```

### Prompt 5c — Audit log export for SIEM integration

```
Create `docs/go-to-market/SIEM_EXPORT.md` — buyer-facing documentation for
ArchLucid audit log export suitable for SIEM ingestion.

**Source material (read first):**
- `docs/AUDIT_COVERAGE_MATRIX.md` — 81 typed audit events, append-only SQL
- `docs/SECURITY.md` — audit and PII sections
- `docs/API_CONTRACTS.md` — `GET /v1/audit/export` endpoint

**Structure:**
1. What is exported: typed audit events with correlation IDs, timestamps,
   actor identity, scope (tenant/workspace/project), event payload
2. Export methods available today:
   - CSV export via `GET /v1/audit/export` (requires Auditor or Admin role)
   - CloudEvents / webhook for real-time forwarding (if configured)
   - Service Bus integration events
3. SIEM integration patterns:
   - Splunk: HTTP Event Collector (HEC) consuming webhook/CloudEvents
   - Microsoft Sentinel: Azure Function bridging Service Bus → Log Analytics
   - Generic: CSV scheduled pull → SIEM file ingestion
4. Event schema: document key fields (eventType, occurredUtc, tenantId,
   workspaceId, correlationId, actor, payload)
5. Retention: link to `DPA_TEMPLATE.md` and `SECURITY.md` retention section
6. Roadmap: native SIEM connector, CEF/syslog output format

Cross-link from `INTEGRATION_CATALOG.md` and `TRUST_CENTER.md`.
```

---

## Improvement 6 — Narrow ICP and proof

**Goal:** Define the **Ideal Customer Profile** precisely and create reference narratives that prove SaaS-delivered value. Addresses **PMF evidence** (score 50, weight 9) and **differentiation** (score 50, weight 9).

**Dimensions affected:** PMF evidence, differentiation, pilot-to-paid, content.

### Prompt 6a — Ideal Customer Profile

```
Create `docs/go-to-market/IDEAL_CUSTOMER_PROFILE.md` — ArchLucid's Ideal Customer
Profile (ICP) for SaaS-only positioning.

**Source material (read first):**
- `docs/go-to-market/BUYER_PERSONAS.md` — three personas with org context
- `docs/go-to-market/ROI_MODEL.md` — break-even analysis (180 architect-hours/year)
- `docs/go-to-market/COMPETITIVE_LANDSCAPE.md` — market context
- `docs/go-to-market/PRICING_PHILOSOPHY.md` (from prompt 3a, if available)

**Structure:**
1. ICP definition: the company profile where ArchLucid delivers maximum value
   and has highest win probability.
2. Firmographic criteria:
   - Company size: [range, e.g. 500–10,000 employees]
   - Industry verticals: [primary 2–3, e.g. financial services, technology,
     healthcare — with reasoning]
   - Geography: [initial focus, e.g. English-speaking, Azure-primary]
   - Architecture team size: [minimum, e.g. 3+ architects]
   - Cloud posture: Azure-primary or Azure-significant (V1 constraint)
3. Behavioral / situational criteria:
   - Active architecture review practice (not aspirational)
   - Compliance or audit pressure on architecture decisions
   - Growth or modernization initiative driving review volume
   - Pain from inconsistent, undocumented, or slow architecture reviews
4. Disqualifiers (poor fit for V1):
   - AWS-only or GCP-only (no Azure)
   - No established architecture practice (need to create one first)
   - Require air-gapped / on-premises deployment
   - Fewer than 3 architects (ROI threshold not met per model)
5. ICP scoring matrix: give each criterion a weight and scoring guide so
   sales can qualify leads quickly
6. Relationship to personas: map ICP firmographics to which persona(s)
   are the champion, economic buyer, and technical evaluator

Cross-link from `BUYER_PERSONAS.md` and `POSITIONING.md`.
```

### Prompt 6b — Customer reference narrative templates

```
Create `docs/go-to-market/REFERENCE_NARRATIVE_TEMPLATE.md` — template for
writing customer reference narratives (case studies) for ArchLucid.

**Source material (read first):**
- `docs/go-to-market/IDEAL_CUSTOMER_PROFILE.md` (from prompt 6a)
- `docs/go-to-market/ROI_MODEL.md` — value levers and metrics
- `docs/go-to-market/PILOT_SUCCESS_SCORECARD.md` — what was measured

**Structure:**
1. Template sections (each narrative follows this pattern):
   - **Customer profile:** Industry, size, architecture team, cloud posture
     (map to ICP criteria)
   - **Challenge:** What pain drove the evaluation (map to persona pain points)
   - **Solution:** How ArchLucid was deployed and used (SaaS-only framing —
     "provisioned a tenant," not "installed the product")
   - **Results:** Quantitative outcomes mapped to pilot scorecard metrics
     (time savings, review quality, governance adoption, audit trail value)
   - **Quote:** Champion pull-quote (placeholder until real quotes available)
   - **What's next:** Expansion plans (more teams, more use cases)
2. Three example narratives (fictional but realistic, based on ICP and personas):
   - **Narrative A — Financial services firm (500 architects, compliance-driven):**
     Persona 1 (Enterprise Architect) champion; governance workflow is the hero;
     audit trail satisfies internal audit
   - **Narrative B — Technology company (200 engineers, modernization wave):**
     Persona 2 (Platform Engineering Lead) champion; time savings and consistency
     are the hero; comparison runs detect drift
   - **Narrative C — Healthcare SaaS vendor (50 engineers, security review mandate):**
     Persona 3 (CTO/VP Engineering) is champion; compliance findings and
     explainability traces satisfy security review board
3. Usage guidance:
   - Replace fictional details with real customer data after pilots complete
   - Highlight that examples are "representative scenarios" until permission-based
     customer references are available
   - Format for: website, PDF, sales deck, one-pager

Cross-link from `POSITIONING.md` and `COMPETITIVE_LANDSCAPE.md`.
```

### Prompt 6c — PMF validation tracker

```
Create `docs/go-to-market/PMF_VALIDATION_TRACKER.md` — a structured tracker
mapping pilot results to product-market fit hypotheses.

**Source material (read first):**
- `docs/go-to-market/PILOT_SUCCESS_SCORECARD.md` — measurement framework
- `docs/go-to-market/IDEAL_CUSTOMER_PROFILE.md` (from prompt 6a)
- `docs/go-to-market/ROI_MODEL.md` — value hypotheses
- `docs/PRODUCT_LEARNING.md` — existing learning signals

**Structure:**
1. PMF hypotheses (derived from positioning and ROI model):
   - H1: Architecture reviews take > 40 hours and ArchLucid reduces this by > 50%
   - H2: Governance workflows reduce compliance review cycles by > 30%
   - H3: Audit trail eliminates "who approved this?" questions entirely
   - H4: Finding quality (explainability + evidence) meets or exceeds manual review
   - H5: Time to first actionable output < 1 hour (SaaS advantage)
2. Evidence tracker table:
   | Hypothesis | Pilot | ICP match | Scorecard metric | Baseline | Result | Status |
   One row per pilot per hypothesis.
3. Synthesis rules:
   - Hypothesis validated: 3+ pilots with positive signal
   - Hypothesis invalidated: 3+ pilots with negative signal
   - Hypothesis inconclusive: mixed or insufficient data
4. Product implications: what to build, change, or drop based on results
5. Go-to-market implications: which claims can be made publicly, which
   narratives to emphasize, which to retire

This is a living document. Populate placeholder rows; fill with real data
as pilots are executed.

Cross-link from `PILOT_SUCCESS_SCORECARD.md` and `PRODUCT_LEARNING.md`.
```

---

## Execution plan

| Priority | Improvement | Prompts | Dependencies | Primary dimensions |
|----------|-------------|---------|--------------|-------------------|
| 1 | **Imp 2 — SaaS operational posture** | 2a, 2b, 2c | Imp 1 done | SaaS platform (25→target 35+) |
| 2 | **Imp 3 — Commercial motion** | 3a, 3b, 3c | Imp 2a (SLA for order form) | GTM (30→target 42+), business model (25→target 35+) |
| 3 | **Imp 4 — Customer success** | 4a, 4b, 4c | Imp 3a (pricing for renewal) | Customer success (33→target 42+) |
| 4 | **Imp 5 — Integrations** | 5a, 5b, 5c | None (independent) | Tech ecosystem (38→target 48+) |
| 5 | **Imp 6 — ICP + proof** | 6a, 6b, 6c | Imp 3a (pricing for ICP), Imp 4a (onboarding for narratives) | PMF (50→target 58+), differentiation (50→target 55+) |

**Within each improvement:** Execute prompts in order (a → b → c). Each creates documents that later prompts reference.

**Across improvements:** Imp 2 and Imp 5 are independent and can run in parallel. Imp 3 benefits from Imp 2a. Imp 4 benefits from Imp 3a. Imp 6 benefits from Imp 3a and Imp 4a.

**Session hygiene:** After each prompt execution, update `docs/ARCHLUCID_RENAME_CHECKLIST.md` progress log, cross-link new docs from `docs/go-to-market/TRUST_CENTER.md` and relevant related-documents tables, and regenerate the SaaS-only marketability assessment with updated scores.
