> **Scope:** Independent first-principles quality assessment of ArchLucid — weighted readiness scoring across 46 qualities, cross-cutting weakness analysis, and top improvement opportunities with Cursor prompts.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# ArchLucid Assessment -- Weighted Readiness 67.41%

**Date:** 2026-04-24
**Assessor:** Independent first-principles analysis of repository code, tests, infrastructure, documentation, and commercial artifacts.
**Methodology:** Evidence-based scoring across 46 qualities in three categories (Commercial, Enterprise, Engineering), weighted per the provided model. Total weight = 100. Items explicitly deferred to V1.1 or V2 per `V1_DEFERRED.md` and `PENDING_QUESTIONS.md` owner decisions do not reduce scores.

---

## 1. Executive Summary

### Overall Readiness

ArchLucid is a **technically ambitious, architecturally coherent** SaaS product with an unusually mature documentation posture for a pre-revenue platform. The 67.41% weighted readiness reflects a product that has invested deeply in **engineering foundations** (explainability traces, provenance graphs, governance workflows, multi-agent pipelines) and **buyer narrative** (executive briefs, positioning, competitive landscape) but has not yet closed the gap between internal artifact completeness and **customer-provable, market-tested value**. The highest-weight gaps are in Time-to-Value, Adoption Friction, and Marketability -- all resolvable with focused execution rather than architectural change.

### Commercial Picture

The commercial narrative is well-articulated: "AI Architecture Intelligence" is a defensible category, the positioning doc is grounded in shipped V1 capabilities, and the packaging model (Pilot / Operate) is cognitively clean. The critical gap is **market contact** -- no paying customer, no live self-serve commerce (Stripe test mode only, per V1.1 deferral), and no production marketing site. The ROI model and pilot scorecard exist but are untested against real buyer behavior. **The product story is ready; the proof-of-story is not.**

### Enterprise Picture

Enterprise readiness is **structurally strong and operationally incomplete**. Traceability (ExplainabilityTrace, provenance, correlation IDs) and auditability (107 typed events, append-only SQL, CI guard) are genuine differentiators. Governance workflows with segregation of duties and pre-commit gates are production-grade. The gaps are in **workflow embeddedness** (no Jira/ServiceNow -- deferred V1.1), **usability** (no real-user feedback cycle), and **compliance assurance** (SOC 2 self-assessment only, no CPA attestation). Trust center scaffolding is solid but several assurance activities are correctly deferred.

### Engineering Picture

The engineering foundation is sound: 55 well-bounded C# projects, 110 Terraform files across 15+ roots, 200+ test classes, 36 Playwright e2e specs, 30 golden corpus cases, and 9 Stryker mutation targets. Architecture tests enforce dependency constraints. The primary risk is **correctness depth** -- 63% branch coverage and ~35% mutation survival mean one-third of conditional paths are unexercised. Concurrency is tested only on the governance approval path (1 of 57 mutation surfaces). Data lifecycle relies on application-enforced cascades with no database-level referential protection. These are **addressable gaps, not architectural flaws**.

---

## 2. Weighted Quality Assessment

Qualities are ordered by **weighted deficiency** (= (100 - score) * weight), from most urgent to least urgent. This ranking prioritizes low-scoring, high-weight qualities.

---

### 2.1 Time-to-Value -- Score: 58 | Weight: 7 | Weighted Deficiency: 294

**Justification:** The buyer first-30-minutes path is documented. A 7-step new-run wizard exists. The demo preview route serves real data from a seeded demo tenant. The trial funnel e2e spec (`live-api-trial-end-to-end.spec.ts`) is merge-blocking in CI. However: the self-serve trial path is in Stripe TEST mode on staging only, the marketing site is not production-deployed, and no real customer has measured time from signup to first committed manifest. The **machinery is built but not connected to a live buyer**.

**Tradeoffs:** Investing in production trial deployment risks exposing rough edges before the product is polished. Delaying it risks losing evaluator momentum.

**Recommendations:**
- Complete the staging trial funnel end-to-end validation (V1 obligation per owner decisions).
- Instrument time-to-first-commit at the tenant level so pilot outcomes are measurable.
- Ensure the demo preview and `/see-it` routes are accessible from the staging marketing site without operator credentials.

**Fixability:** V1 (staging trial validation). V1.1 (production commerce un-hold).

---

### 2.2 Marketability -- Score: 65 | Weight: 8 | Weighted Deficiency: 280

**Justification:** Strong positioning ("AI Architecture Intelligence"), well-grounded elevator pitches, competitive landscape analysis, buyer personas, and ICP with scoring matrix. The `/why-archlucid` in-product page and the anonymous `/why` marketing pack PDF provide live proof. The category definition and messaging do/don't table are unusually disciplined. However: no production marketing site, no live demo accessible to cold prospects without guided assistance, and no market validation beyond internal modeling.

**Tradeoffs:** Category creation is expensive and uncertain. Being first-mover with a clear name is valuable; being first-mover without customers risks defining a category nobody searches for.

**Recommendations:**
- Deploy the marketing site to production with the demo preview route live (owner decision on DNS and Front Door required).
- Measure inbound interest from positioning collateral when shared with even 5-10 prospects.
- Ensure the anonymous PDF pack at `/why` is downloadable from the staging marketing domain today.

**Fixability:** Partially V1 (staging exposure). Production marketing deployment requires owner decisions (DNS, certs).

---

### 2.3 Adoption Friction -- Score: 58 | Weight: 6 | Weighted Deficiency: 252

**Justification:** SaaS model eliminates infrastructure deployment for buyers. No local install required. Progressive disclosure keeps the default surface narrow. Operator quickstart and first-run walkthrough are documented. However: Azure-only platform (disqualifies AWS/GCP-only shops), English-only, 3+ architect minimum for ROI, no import connectors from existing EA tools (LeanIX, Ardoq), and no Jira/ServiceNow integration in V1. The onboarding happy path requires an architecture practice that already exists -- ArchLucid accelerates but does not create.

**Tradeoffs:** Broadening platform support (multi-cloud) would reduce friction but dilute engineering focus. Narrow ICP + deep Azure fit vs wide TAM + shallow fit.

**Recommendations:**
- Ensure the trial signup flow works without any DevOps skill -- pure SaaS self-service.
- Add an in-product "import from" or "describe your existing architecture" affordance that lowers the cold-start barrier.
- Document the CloudEvents/REST workaround for Jira/ServiceNow users explicitly in the pilot guide as a V1 bridge.

**Fixability:** V1 (trial flow, bridge docs). V1.1 (Jira/ServiceNow connectors). v2+ (multi-cloud).

---

### 2.4 Proof-of-ROI Readiness -- Score: 65 | Weight: 5 | Weighted Deficiency: 175

**Justification:** ROI model with break-even analysis (180 architect-hours/year), pilot ROI model with success metrics, first-value report builder, baseline review-cycle capture at signup, tenant value report DOCX endpoint, and sponsor banner with "Day N since first commit." The foundations are unusually strong for a pre-revenue product. However: all ROI models are theoretical -- no customer has validated the 180-hour break-even. The value report DOCX exists but has never been presented to a real sponsor.

**Tradeoffs:** Building ROI infrastructure pre-revenue risks over-engineering metrics nobody asks for. But having it ready for the first pilot eliminates the "how do we measure success?" question.

**Recommendations:**
- Validate the 180-hour break-even assumption against 2-3 informal conversations with architecture leaders.
- Ensure the `/value-report` DOCX renders with pilot-realistic data (not just demo seed).
- Pre-populate the sample aggregate ROI bulletin with plausible numbers from the ROI model.

**Fixability:** V1 (rendering, sample data). Validation requires customer contact.

---

### 2.5 Correctness -- Score: 62 | Weight: 4 | Weighted Deficiency: 152

**Justification:** 30 golden corpus decisioning cases. Agent output quality scoring with structural + semantic gates. 10 finding engines with parallel orchestration. Explanation faithfulness checking. However: branch coverage is 63%, mutation survival is ~35%, concurrency is tested on only 1 of 57 mutation surfaces (governance approval), and the internal correctness assessment scored 66/100 as of 2026-04-15. The finding engines produce outputs, but the depth of assertion on edge cases, boundary conditions, and concurrent access is insufficient for a system that produces compliance findings.

**Tradeoffs:** Higher correctness testing is expensive in CI time and development effort. The golden corpus approach is sound but covers only decisioning, not the full request-to-manifest pipeline.

**Recommendations:**
- Expand golden corpus to cover context ingestion and artifact synthesis stages.
- Add concurrency tests for the top 5 mutation-heavy controllers (run commit, policy pack create, alert rule create, archival, comparison persist).
- Target Stryker mutation score ratchet to 72% for Decisioning and Application projects.

**Fixability:** V1 (all items).

---

### 2.6 Executive Value Visibility -- Score: 68 | Weight: 4 | Weighted Deficiency: 128

**Justification:** Executive sponsor brief exists with clear "what to believe, what to try, what not to claim" structure. Why-ArchLucid page with live demo data. Sponsor email banner on run detail. Value report DOCX. Demo preview and explain routes for unauthenticated proof. Pilot ROI model companion. The narrative is well-structured for sponsor conversations.

**Tradeoffs:** Investing in executive-facing artifacts before product-market fit risks polishing materials that may need repositioning.

**Recommendations:**
- Verify the executive sponsor brief renders as a clean PDF or printable page (not just markdown).
- Ensure the value report DOCX includes a one-page executive summary section at the top.
- Add a "board-ready" one-slide summary that a sponsor can drop into a quarterly review.

**Fixability:** V1 (all items).

---

### 2.7 Workflow Embeddedness -- Score: 58 | Weight: 3 | Weighted Deficiency: 126

**Justification:** GitHub Actions manifest delta (job summary + PR comment), Azure DevOps pipeline task (job summary + PR comment + server-side), CloudEvents webhooks, Azure Service Bus integration events with AsyncAPI catalog, HMAC-signed webhooks, and a full CLI for automation. However: no Jira connector (V1.1), no ServiceNow connector (V1.1), no Slack (V2). The existing integrations require the customer to consume CloudEvents or REST API to bridge to their ITSM. For enterprises whose workflow center-of-gravity is Jira + ServiceNow, the V1 integration surface is a gap that the deferred timeline acknowledges.

**Tradeoffs:** First-party connectors are expensive to build and maintain. CloudEvents/webhooks are more flexible but shift integration burden to the customer.

**Recommendations:**
- Document the CloudEvents-to-Jira and CloudEvents-to-ServiceNow bridge pattern explicitly (not just "use webhooks").
- Ensure the GitHub Action and ADO pipeline task are published to their respective marketplaces.
- Add a webhook testing endpoint (echo / debug) so integrators can validate payloads without a live ITSM.

**Fixability:** V1 (bridge docs, webhook debug). V1.1 (Jira, ServiceNow).

---

### 2.8 Usability -- Score: 60 | Weight: 3 | Weighted Deficiency: 120

**Justification:** Operator UI with progressive disclosure (3 tiers), 7-step new-run wizard, first-run walkthrough, layer headers with "when to use" guidance, rank-aware copy, and inspect-first layout for governance pages. Accessibility scanning covers 5 top routes with merge-blocking CI. However: no real-user testing, no UX research feedback, no task-completion time measurements, and the progressive disclosure system is complex enough that internal test files run to hundreds of lines of regression guards.

**Tradeoffs:** Building extensive UX testing infrastructure before having users is premature. But launching without any user feedback risks shipping a product that is internally coherent but externally confusing.

**Recommendations:**
- Conduct 3-5 hallway usability tests with architecture practitioners (even informal, recorded screen-shares).
- Measure task completion time for the core pilot flow (create run → commit → review artifacts) with a timer.
- Simplify the nav-config regression test surface by extracting a declarative test matrix.

**Fixability:** V1 (hallway tests, timing). Formal UX research is post-V1.

---

### 2.9 Differentiability -- Score: 72 | Weight: 4 | Weighted Deficiency: 112

**Justification:** "AI Architecture Intelligence" is a new category with a defensible definition. The competitive landscape document maps ArchLucid against EA tools (LeanIX, Ardoq), ad-hoc AI (ChatGPT, Copilot), and GRC tools. The ExplainabilityTrace and governance workflow are genuine differentiators that competitors cannot easily replicate. 10 finding engines running in parallel, provenance graph, and 107 typed audit events are concrete proof points.

**Tradeoffs:** Category creation requires sustained market education. Being differentiated is necessary but not sufficient -- the category must also be one buyers search for.

**Recommendations:**
- Test the "AI Architecture Intelligence" category name with 5-10 target buyers -- does it resonate or require explanation?
- Create a one-page "Why not just use ChatGPT?" comparison that a champion can forward internally.
- Strengthen the competitive landscape with a feature matrix table (not just prose).

**Fixability:** V1 (collateral). Market validation requires buyer contact.

---

### 2.10 Trustworthiness -- Score: 68 | Weight: 3 | Weighted Deficiency: 96

**Justification:** Trust center with security overview, pen-test engagement metadata, compliance questionnaire pre-fills (CAIQ Lite, SIG Core), SOC 2 self-assessment, STRIDE threat model, DPA template, incident communications policy, and coordinated disclosure policy. Append-only audit trail with SQL DENY. Owner-conducted security self-assessment as interim posture. Pen test (Aeronova) awarded and in flight (deferred to V1.1 per owner decision -- not scored as a deficit). PGP key drop deferred to V1.1 -- not scored.

**Tradeoffs:** Self-assessments are necessary but insufficient for enterprise procurement. The trust center scaffolding is strong but the key assurance activities are correctly deferred.

**Recommendations:**
- Ensure the SOC 2 self-assessment maps explicitly to Common Criteria with gap indicators.
- Add a "Security FAQ" page to the trust center for common procurement questions.
- Verify the SIG Core pre-fill can be exported to the actual SIG workbook format.

**Fixability:** V1 (FAQ, mapping). V1.1 (pen test, PGP key per owner deferral).

---

### 2.11 Security -- Score: 70 | Weight: 3 | Weighted Deficiency: 90

**Justification:** Entra ID + JWT + API key auth. RLS with SESSION_CONTEXT. CSP headers (default-src 'none' for API). HSTS. Private endpoints for SQL and Blob. Key Vault for secrets. Gitleaks in CI. Trivy (container + IaC). CodeQL. OWASP ZAP baseline. CycloneDX SBOM. LLM prompt redaction. Fixed-time API key comparison. Security headers middleware. Auth safety guard. Placeholder API key rejection at startup. Comprehensive for a V1 product.

**Tradeoffs:** Defense-in-depth is good but adds operational complexity. Each layer (WAF, APIM, private endpoints) is optional, which means security posture varies by deployment configuration.

**Recommendations:**
- Add a "minimum security configuration" checklist that operators must complete before pilot data enters the system.
- Verify that the AuthSafetyGuard startup check covers all auth modes (not just JWT).
- Document the threat model for the LLM prompt path -- what happens if prompt redaction fails silently.

**Fixability:** V1 (all items).

---

### 2.12 Data Consistency -- Score: 55 | Weight: 2 | Weighted Deficiency: 90

**Justification:** ROWVERSION optimistic concurrency on Runs. SERIALIZABLE + UPDLOCK on governance approvals. Outbox for at-least-once integration event delivery. Orphan detection (DataConsistencyOrphanProbeHostedService). However: 56 of 57 mutation surfaces lack concurrency tests. No ON DELETE CASCADE constraints -- all referential integrity is application-enforced. Run archival cascade is explicitly incomplete (child rows may orphan). Audit events grow unbounded. Agent execution trace blobs have no lifecycle policy.

**Tradeoffs:** Application-enforced integrity is more flexible but more fragile. Database constraints are harder to change but provide a safety net.

**Recommendations:**
- Add concurrency tests for run commit, policy pack creation, and alert rule creation.
- Implement a data archival cascade that covers FindingsSnapshots, GraphSnapshots, and ArtifactBundles when a run is archived.
- Define a blob lifecycle policy for agent execution traces (e.g., 90-day retention in hot, move to cool after).

**Fixability:** V1 (concurrency tests, cascade). Blob lifecycle is operational.

---

### 2.13 Interoperability -- Score: 55 | Weight: 2 | Weighted Deficiency: 90

**Justification:** REST API with OpenAPI/Swagger. CloudEvents for integration events. Azure Service Bus topic. HMAC-signed webhooks. GitHub Actions reusable action. Azure DevOps pipeline task. CLI for automation. AsyncAPI catalog. However: Azure-only platform (no AWS/GCP topology engines), no import/export connectors for existing EA tools, and no first-party ITSM connectors in V1.

**Tradeoffs:** Deep Azure integration vs broad multi-cloud reach. First-party connectors vs webhook flexibility.

**Recommendations:**
- Publish the OpenAPI spec as a downloadable artifact from the marketing site.
- Add a "Postman collection" or equivalent for API evaluators.
- Document the CloudEvents schema with concrete examples for each event type.

**Fixability:** V1 (OpenAPI publishing, Postman collection). V1.1+ (ITSM connectors, multi-cloud).

---

### 2.14 Compliance Readiness -- Score: 60 | Weight: 2 | Weighted Deficiency: 80

**Justification:** SOC 2 self-assessment under internal CISO ownership. CAIQ Lite pre-fill for CSA STAR. SIG Core pre-fill. DPA template. Compliance matrix. OWASP ZAP baseline. Audit coverage matrix with CI guard. However: no CPA SOC 2 attestation (roadmap item), some SIG Core control families are "Partial" or "N/A."

**Tradeoffs:** CPA attestation is expensive ($50K-$150K) and premature before revenue. Self-assessment is the right V1 posture.

**Recommendations:**
- Complete the SOC 2 self-assessment gap register with remediation timelines.
- Ensure the CAIQ Lite pre-fill is exportable to the CSA STAR submission format.
- Add ISO 27001 control mapping as a companion to SOC 2.

**Fixability:** V1 (gap register, export format). Post-V1 (CPA attestation).

---

### 2.15 Commercial Packaging Readiness -- Score: 60 | Weight: 2 | Weighted Deficiency: 80

**Justification:** Clear Pilot / Operate layer model. Progressive disclosure in UI. HTTP 402 tier gating on governance and value report endpoints (`[RequiresCommercialTenantTier]`). Pricing page with Team/Professional/Enterprise tiers. Order form template. However: no live Stripe, no Marketplace listing (both V1.1 deferred per owner decision -- not scored). Entitlement enforcement is partial (selected endpoints only).

**Tradeoffs:** Soft packaging (narrative + disclosure) is appropriate for V1. Hard entitlements (SKU ↔ endpoint matrix) are a V1.1 concern.

**Recommendations:**
- Verify the HTTP 402 response body is buyer-friendly (upgrade path, pricing link).
- Ensure the pricing page accurately reflects the tier gating that exists in code.
- Add a "packaging boundary test" that asserts every `[RequiresCommercialTenantTier]` endpoint returns 402 for Trial tier.

**Fixability:** V1 (all items).

---

### 2.16 Decision Velocity -- Score: 58 | Weight: 2 | Weighted Deficiency: 84

**Justification:** Order form template exists. Pricing page exists with locked 2026 prices. DPA template. Procurement pack (CLI-downloadable). However: the sales motion is sales-led only in V1 (no self-serve purchase), which adds friction to evaluation-to-purchase velocity. The trial funnel provides evaluation capability but does not enable autonomous purchase. Per owner deferral decision, the Stripe live keys flip and Marketplace listing are V1.1 -- not scored as deficits.

**Tradeoffs:** Sales-led is appropriate for enterprise pricing ($X0K+ ACV). Self-serve is appropriate for developer/team adoption.

**Recommendations:**
- Ensure the order form template can be sent as a PDF from the operator shell.
- Add a "request pricing" or "talk to sales" CTA on the pricing page that captures lead data.
- Document the evaluation-to-purchase process for the sales-led motion.

**Fixability:** V1 (CTA, process doc).

---

### 2.17 Reliability -- Score: 62 | Weight: 2 | Weighted Deficiency: 76

**Justification:** Circuit breaker + Polly retry on LLM calls. Simmy chaos testing in CI (weekly). Health checks (live/ready). Outbox for integration events. SQL failover Terraform module. RTO/RPO targets documented. However: single-region for V1, game day exercises are scheduled but not yet executed (first staging run: 2026-04-29), and the circuit breaker only protects the LLM path.

**Tradeoffs:** Multi-region adds cost and complexity. Single-region with failover is the right V1 posture for a pre-revenue product.

**Recommendations:**
- Execute the first game day exercise (scheduled 2026-04-29) and document outcomes.
- Add circuit breaker protection to the SQL connection path (not just LLM).
- Verify the outbox retry calculator handles poison messages (permanent failures).

**Fixability:** V1 (game day, SQL resilience).

---

### 2.18 Architectural Integrity -- Score: 78 | Weight: 3 | Weighted Deficiency: 66

**Justification:** 55 C# projects with clean layer boundaries (Core, Contracts, Application, Persistence, Api, AgentRuntime, Decisioning, Coordinator, etc.). Architecture dependency constraint tests (`DependencyConstraintTests`). 24+ ADRs documenting decisions. Contracts project for shared types. Interface-first design with DI composition. Coordinator/Authority pipeline separation. The design is internally coherent and well-documented.

**Tradeoffs:** 55 projects is a large surface area for a solo/small team. The modularity is structurally sound but operationally expensive.

**Recommendations:**
- Verify that `DependencyConstraintTests` covers all prohibited cross-layer references (not just a subset).
- Add an ADR for the Authority pipeline vs Coordinator pipeline duality (if not already covered).
- Document the composition root registration order for new contributors.

**Fixability:** V1 (constraint tests, ADR).

---

### 2.19 Azure Compatibility and SaaS Deployment Readiness -- Score: 68 | Weight: 2 | Weighted Deficiency: 64

**Justification:** 110 Terraform files across 15+ roots (container-apps, edge, entra, keyvault, monitoring, openai, private, servicebus, sql-failover, storage, etc.). Dockerfiles for API and UI. Container Apps with autoscale. Front Door + WAF. App Insights + Grafana. Azure SQL with failover. Service Bus. However: no ACR push in CI, production deployment pipeline is documented but not yet executed, and multi-region is Terraform-ready but not live.

**Tradeoffs:** Over-provisioning infrastructure pre-revenue wastes money. Under-provisioning risks a bad first impression.

**Recommendations:**
- Add ACR push to the CD pipeline (`DEPLOYMENT_CD_PIPELINE.md`).
- Execute the reference SaaS stack order (`apply-saas.ps1`) on staging and document any gaps.
- Verify Terraform `plan` produces no errors for all roots against a clean subscription.

**Fixability:** V1 (ACR push, staging validation).

---

### 2.20 Maintainability -- Score: 68 | Weight: 2 | Weighted Deficiency: 64

**Justification:** One class per file. C# coding conventions (terse C# 12, primary constructors, expression-bodied members). XML doc comments being added progressively (10 documented pieces). LINQ preference. Named bounds. Interface-first. However: 2000+ C# files is a large codebase. The UI nav-config / authority-shaping regression test surface is complex. 556 markdown files is a documentation debt risk.

**Tradeoffs:** Comprehensive documentation now prevents knowledge loss later. But maintaining 556 markdown files requires discipline.

**Recommendations:**
- Add a staleness detector for markdown files (e.g., flag files not modified in 90 days for review).
- Continue the XML doc comment initiative -- prioritize the Application and Persistence layers.
- Simplify the nav-config regression tests by extracting a declarative test matrix.

**Fixability:** V1 (staleness detector, doc comments).

---

### 2.21 Procurement Readiness -- Score: 72 | Weight: 2 | Weighted Deficiency: 56

**Justification:** CLI-downloadable procurement pack ZIP. DPA template. Subprocessor list. Trust center with engagement metadata. SLA summary (99.5% availability target). Order form template. Incident communications policy. CAIQ Lite and SIG Core pre-fills. Pricing philosophy with locked 2026 prices. Comprehensive for pre-revenue.

**Tradeoffs:** Procurement readiness is a long-tail investment -- each customer may ask for different questionnaires.

**Recommendations:**
- Add VSAQ (Vendor Security Assessment Questionnaire) pre-fill for Google-shop customers.
- Ensure the procurement pack ZIP includes the latest trust center, DPA, and SLA summary.
- Add a "procurement timeline" document that sets expectations for typical evaluation cycles.

**Fixability:** V1 (VSAQ, pack validation).

---

### 2.22 Traceability -- Score: 82 | Weight: 3 | Weighted Deficiency: 54

**Justification:** ExplainabilityTrace with 5 structured fields on every finding. Provenance graph (nodes, edges, algorithms). Explanation faithfulness checking. OTel tracing with correlation IDs across HTTP and background jobs. Agent execution trace persistence in blob storage. 107 typed audit events with CI guard. Run-level `OtelTraceId`. OTel metric for trace completeness. This is the product's strongest quality.

**Tradeoffs:** Deep traceability adds storage and compute cost. The provenance graph is powerful but may be underutilized if buyers do not understand it.

**Recommendations:**
- Add a "reading a provenance graph" guide for non-technical sponsors.
- Verify that ExplainabilityTrace completeness stays above 95% across all finding engine types.
- Ensure the faithfulness checker has a regression test for adversarial cases.

**Fixability:** V1 (guide, regression test).

---

### 2.23 Explainability -- Score: 80 | Weight: 2 | Weighted Deficiency: 40

**Justification:** ExplainabilityTrace (GraphNodeIdsExamined, RulesApplied, DecisionsTaken, AlternativePathsConsidered, Notes). Structured explanation parser. Aggregate run explanations with citations. Demo explain route with provenance + citations side-by-side. Explanation faithfulness checking (token overlap + aggregate fallback). OTel metric for trace completeness. Genuinely strong for an AI product.

**Tradeoffs:** Explainability adds latency and complexity. Token-overlap faithfulness is a heuristic, not a proof.

**Recommendations:**
- Document the faithfulness threshold and what happens when it fails.
- Add a buyer-facing "how ArchLucid explains its recommendations" page.
- Consider semantic similarity in addition to token overlap for faithfulness.

**Fixability:** V1 (docs, threshold documentation). V1.1 (semantic faithfulness).

---

### 2.24 AI/Agent Readiness -- Score: 78 | Weight: 2 | Weighted Deficiency: 44

**Justification:** 4 agent types (Topology, Cost, Compliance, Critic). 10 finding engines in parallel. Multi-vendor LLM with fallback (`FallbackAgentCompletionClient`). Prompt versioning with SHA-256 catalog. Agent output quality scoring (structural + semantic). Deterministic simulator mode for CI. Content safety guard. Circuit breaker on LLM. 30 golden corpus cases. LLM cost estimation metrics.

**Tradeoffs:** Multi-vendor LLM adds complexity. Deterministic mode is essential for CI but may mask real LLM behavior.

**Recommendations:**
- Add a weekly CI job that runs golden corpus cases against a real LLM (not just deterministic mode).
- Document prompt versioning discipline for contributors.
- Expand golden corpus beyond decisioning to cover context ingestion quality.

**Fixability:** V1 (CI job, docs).

---

### 2.25 Policy and Governance Alignment -- Score: 78 | Weight: 2 | Weighted Deficiency: 44

**Justification:** Policy packs with versioning, scope assignments, and effective governance resolution. Pre-commit governance gate with configurable severity thresholds and warning-only mode. Approval workflow with segregation of duties. Approval SLA tracking with webhook escalation. Compliance drift trend. Governance preview (dry-run). Governance rationale service.

**Tradeoffs:** Rich governance may be premature for pilot-stage customers who just want findings.

**Recommendations:**
- Ensure governance can be fully disabled for pilot-stage tenants (already the case via config -- verify).
- Add a "governance maturity ramp" guide: start with findings only, add policy packs, then add approval workflow.
- Verify that the pre-commit gate warning-only mode is the default for new tenants.

**Fixability:** V1 (all items).

---

### 2.26 Auditability -- Score: 80 | Weight: 2 | Weighted Deficiency: 40

**Justification:** 107 typed audit event constants with CI guard on count. Append-only SQL store with DENY UPDATE/DELETE. Filtered search with keyset pagination. CSV export with 90-day range. Correlation ID search. Run-level audit timeline. Audit retention policy (hot/warm/cold). This is production-grade auditability.

**Tradeoffs:** Large audit volume creates storage cost. The keyset cursor on OccurredUtc only has a known tie-breaking limitation.

**Recommendations:**
- Add a test that validates the EventId tie-break under timestamp collision.
- Verify the audit export CSV includes all fields needed for SOC 2 evidence.
- Add an audit event dashboard in the Grafana stack.

**Fixability:** V1 (all items).

---

### 2.27 Cognitive Load -- Score: 55 | Weight: 1 | Weighted Deficiency: 45

**Justification:** Progressive disclosure reduces default UI surface. Two-layer buyer model (Pilot / Operate). LayerHeader with "when to use" guidance. Operator decision guide. However: 556 markdown files, complex nav-config authority shaping system, 200+ test files, and the contributor docs assume deep familiarity with C# / Azure / Terraform. For buyers, cognitive load is well-managed; for contributors, it is high.

**Tradeoffs:** Contributor cognitive load is the cost of comprehensive documentation and testing. Reducing it risks losing the documentation discipline.

**Recommendations:**
- Add a "contributor mental model" one-pager that maps the key abstractions.
- Reduce the doc root surface area (already done on 2026-04-23 -- verify the result is < 25 active files).
- Add a search/index page for the 556 markdown files (beyond `DOC_INVENTORY`).

**Fixability:** V1 (contributor mental model, search page).

---

### 2.28 Scalability -- Score: 55 | Weight: 1 | Weighted Deficiency: 45

**Justification:** Container Apps autoscale. SQL read replica support (`SqlReadReplicaConnectionStringResolver`). Service Bus for fan-out. Rate limiting with role-based partitions. However: single-region V1, no horizontal partition strategy, no load test baseline, and the hot-path read cache is application-level (not distributed).

**Tradeoffs:** Premature scaling optimization wastes effort. But having no load baseline means you cannot detect regressions.

**Recommendations:**
- Run a baseline load test (50 concurrent users, core pilot flow) and commit results.
- Document the expected tenant count and run volume for V1 capacity planning.
- Evaluate Redis as a distributed cache for the hot-path read cache (post-V1).

**Fixability:** V1 (load test baseline, capacity doc).

---

### 2.29 Template and Accelerator Richness -- Score: 55 | Weight: 1 | Weighted Deficiency: 45

**Justification:** Finding engine template project. Industry brief templates (financial services, healthcare). Demo seed with Contoso Retail Modernization. Vertical starter policy packs. Architecture request presets in wizard. However: only 2 industry briefs, limited pre-built policy packs, no "starter kit" for common architectures.

**Tradeoffs:** More templates means more maintenance. Better to have 2 excellent templates than 10 mediocre ones.

**Recommendations:**
- Add 2-3 more industry brief templates (technology/SaaS, government, retail).
- Create a "common architectures" starter pack (microservices, event-driven, data platform).
- Ensure the finding engine template project builds and tests in CI.

**Fixability:** V1 (additional briefs, starter pack).

---

### 2.30 Availability -- Score: 58 | Weight: 1 | Weighted Deficiency: 42

**Justification:** 99.5% monthly availability SLO. Health endpoints (live/ready). Rolling deployments. SQL failover Terraform module with automatic tuning. Prometheus SLO burn-rate rules. However: single-region V1, no multi-AZ evidence, game day not yet executed.

**Tradeoffs:** 99.5% SLO is honest for a V1 SaaS. Over-promising availability pre-revenue is risky.

**Recommendations:**
- Execute the first game day (2026-04-29) and publish the results.
- Verify the SQL failover module produces a working secondary.
- Add an availability dashboard to the Grafana stack.

**Fixability:** V1 (game day, dashboard).

---

### 2.31 Customer Self-Sufficiency -- Score: 58 | Weight: 1 | Weighted Deficiency: 42

**Justification:** Operator quickstart, troubleshooting guide, CLI doctor + support-bundle, health endpoints, first-run walkthrough, and pilot guide. However: no knowledge base, no FAQ, no community forum, no in-product help beyond layer headers, no chatbot.

**Tradeoffs:** Self-service support infrastructure is expensive to build and maintain pre-revenue.

**Recommendations:**
- Create a FAQ from the most common onboarding questions (extracted from existing docs).
- Add in-product tooltips on the 3 most common confusion points (if known from internal use).
- Consider a Zendesk/Freshdesk integration for ticket tracking (post-V1).

**Fixability:** V1 (FAQ). Post-V1 (knowledge base, ticket tracking).

---

### 2.32 Performance -- Score: 60 | Weight: 1 | Weighted Deficiency: 40

**Justification:** p95 < 2s API response time target. Benchmarks project (`DecisionEngineMergeBenchmarks`). LLM token metering. Hot-path read cache with configurable TTL. Load test scaffolding (`tests/load/`). However: no published load test results, no performance regression CI gate.

**Tradeoffs:** Performance optimization pre-scale is premature. But baseline measurement is necessary.

**Recommendations:**
- Run the benchmark project and commit baseline results.
- Add a p95 latency regression check to CI (alert if > 2s on key endpoints).
- Profile the manifest commit path for latency bottlenecks.

**Fixability:** V1 (baselines, CI check).

---

### 2.33 Cost-Effectiveness -- Score: 62 | Weight: 1 | Weighted Deficiency: 38

**Justification:** Per-tenant cost model documented. LLM token metering (`archlucid_llm_prompt_tokens_total`). Container Apps consumption plan. App Insights daily quota documentation. Consumption budget Terraform modules. However: cost model is "order of magnitude" sketch, not a validated projection.

**Tradeoffs:** Precise cost modeling requires production data. Sketch-level is appropriate for V1.

**Recommendations:**
- Validate the cost model against actual staging Azure spend for one month.
- Add a monthly cost review cadence to the operational runbooks.
- Document the "surprise cost" items (App Insights, egress, blob lifecycle).

**Fixability:** V1 (validation against staging, cadence doc).

---

### 2.34 Accessibility -- Score: 62 | Weight: 1 | Weighted Deficiency: 38

**Justification:** WCAG 2.1 AA target. axe-core + Playwright (merge-blocking CI). jsx-a11y ESLint. Skip-to-content link. Landmark navigation. Form labels. Focus management. 5 pages scanned. Annual review cadence. Marketing `/accessibility` page. However: only 5 pages covered, no manual keyboard testing documented, no screen reader testing.

**Tradeoffs:** Automated scanning catches ~30% of accessibility issues. Manual testing is essential for keyboard and screen reader.

**Recommendations:**
- Expand axe scanning to cover all operator routes (not just top 5).
- Add a keyboard navigation smoke test for the core pilot flow.
- Document known accessibility limitations (if any).

**Fixability:** V1 (expanded scanning, keyboard test).

---

### 2.35 Change Impact Clarity -- Score: 65 | Weight: 1 | Weighted Deficiency: 35

**Justification:** Breaking changes doc with Phase 7 rename details. CHANGELOG with per-release entries. Governance preview (what would change if activated). Manifest comparison with structured drift detection. Comparison replay with verify mode (422 on drift). However: change impact is architectural-scope, not customer-operational-scope.

**Tradeoffs:** Architecture-level change clarity is the core product. Operational change impact is secondary.

**Recommendations:**
- Add a "what changed in this release" summary to the CHANGELOG (buyer-readable, not just technical).
- Ensure manifest comparison drift clearly labels severity.

**Fixability:** V1 (all items).

---

### 2.36 Stickiness -- Score: 70 | Weight: 1 | Weighted Deficiency: 30

**Justification:** Golden manifests create versioned data assets. Audit trail is append-only. Provenance graph grows with each run. Governance workflows accumulate policy and approval history. Integration events drive downstream consumers. CLI for automation embeds into workflows. These create genuine switching costs.

**Tradeoffs:** Lock-in is a double-edged sword -- buyers may resist deep adoption if exit cost is unclear.

**Recommendations:**
- Document data portability: what can a customer export if they leave?
- Ensure the export ZIP contains all tenant data in a portable format.

**Fixability:** V1 (portability docs).

---

### 2.37 Manageability -- Score: 68 | Weight: 1 | Weighted Deficiency: 32

**Justification:** Configuration via appsettings + env vars + Key Vault references. Feature flags. Config bridge for migration with sunset documentation. Operational runbooks. Terraform for IaC. Health endpoints for operational monitoring.

**Tradeoffs:** Configuration flexibility increases operational surface area.

**Recommendations:**
- Add a configuration reference page listing all configuration keys with defaults and valid ranges.
- Verify the config bridge sunset is complete (no legacy bridges still active).

**Fixability:** V1 (config reference).

---

### 2.38 Supportability -- Score: 72 | Weight: 1 | Weighted Deficiency: 28

**Justification:** CLI `doctor` command. `support-bundle --zip`. Correlation IDs on all requests. Version endpoint. Troubleshooting guide. Operational runbooks for common scenarios. Sanitized logging.

**Tradeoffs:** Good support tooling for the engineering team; less clear how customer-facing support scales.

**Recommendations:**
- Add a "what to include in a support request" template accessible from the operator UI.

**Fixability:** V1 (template).

---

### 2.39 Deployability -- Score: 72 | Weight: 1 | Weighted Deficiency: 28

**Justification:** Dockerfiles for API and UI. Docker compose profiles. Terraform modules. CD pipeline documentation. DbUp migrations. Release smoke scripts (`release-smoke.ps1`). RC drill script.

**Tradeoffs:** SaaS model means customers do not deploy; this is internal operator concern only.

**Recommendations:**
- Verify the CD pipeline document matches the actual GitHub Actions workflow.

**Fixability:** V1.

---

### 2.40 Testability -- Score: 72 | Weight: 1 | Weighted Deficiency: 28

**Justification:** 200+ test classes. 36 Playwright e2e specs (merge-blocking). 30 golden corpus decisioning cases. Property tests (FsCheck). 9 Stryker mutation targets. Contract tests for repositories. Architecture dependency tests. Deterministic agent simulator.

**Tradeoffs:** Extensive test infrastructure but mutation survival at ~35% indicates test quality gaps.

**Recommendations:**
- Continue Stryker ratchet toward 72% target.
- Add property tests for finding engine edge cases.

**Fixability:** V1.

---

### 2.41 Extensibility -- Score: 72 | Weight: 1 | Weighted Deficiency: 28

**Justification:** Finding engine template project. Policy packs for custom rules. Integration events (CloudEvents). Webhooks with custom subscriptions. CLI extensible commands. Interface-first design for new implementations.

**Tradeoffs:** Extensibility adds maintenance burden. Template project must stay in sync with core APIs.

**Recommendations:**
- Verify the finding engine template project builds against the latest Contracts version.

**Fixability:** V1.

---

### 2.42 Evolvability -- Score: 72 | Weight: 1 | Weighted Deficiency: 28

**Justification:** 24+ ADRs. Config bridge sunset pattern. Progressive disclosure model extensible for future tiers. Clean contracts layer allows versioning. Feature flags for incremental rollout.

**Tradeoffs:** ADR discipline is strong but requires continued investment.

**Recommendations:**
- Add an ADR template reminder for architectural decisions made in chat sessions.

**Fixability:** V1.

---

### 2.43 Observability -- Score: 75 | Weight: 1 | Weighted Deficiency: 25

**Justification:** 30+ custom OTel metrics. Grafana dashboards committed (Authority, SLO, LLM usage, container apps, run lifecycle). Prometheus SLO rules. Application Insights. Structured logging (Serilog). Background job correlation across HTTP and non-HTTP paths.

**Tradeoffs:** Strong observability stack adds operational dependency (Grafana, Prometheus, OTel collector).

**Recommendations:**
- Verify all Grafana dashboards render without errors against current metrics.

**Fixability:** V1.

---

### 2.44 Documentation -- Score: 75 | Weight: 1 | Weighted Deficiency: 25

**Justification:** 556+ markdown files. Structured hierarchy (docs/, docs/library/, docs/adr/, docs/security/, docs/go-to-market/). Audience-split docs (buyer vs contributor). Doc inventory with metadata. Method documentation being added progressively (10 pieces tracked). API contracts documented.

**Tradeoffs:** 556 files risks staleness and navigation difficulty. Quality over quantity.

**Recommendations:**
- Run the doc inventory staleness check and flag files older than 90 days.
- Prioritize XML doc comments on the Application and Persistence layers.

**Fixability:** V1.

---

### 2.45 Modularity -- Score: 78 | Weight: 1 | Weighted Deficiency: 22

**Justification:** 55 projects with clear boundaries. Contracts and Contracts.Abstractions layers. Interface-first design. DI composition roots. Finding engine template. Clean separation of Api, Application, Core, Persistence, Decisioning, AgentRuntime, Coordinator, Provenance, KnowledgeGraph, Retrieval, ArtifactSynthesis, ContextIngestion.

**Recommendations:** None urgent. Maintain dependency constraint tests.

**Fixability:** N/A -- already strong.

---

### 2.46 Azure Ecosystem Fit -- Score: 82 | Weight: 1 | Weighted Deficiency: 18

**Justification:** Azure-primary per ADR 0020. Entra ID. Key Vault. Service Bus. Azure SQL. Container Apps. Front Door + WAF. App Insights + OTLP. Azure OpenAI. Managed Identity for SQL and Blob. Private endpoints. Terraform for all resources. Consumption budgets.

**Recommendations:** None urgent. Strongest engineering quality.

**Fixability:** N/A -- already strong.

---

## 3. Top 10 Most Important Weaknesses

These are cross-cutting weaknesses ranked from most serious to least serious.

1. **No customer contact or market validation.** Every commercial quality depends on assumptions about buyer behavior that have not been tested. The product story is internally complete but externally unproven. No paying customer, no live trial, no prospect feedback loop.

2. **Trial funnel is not production-accessible.** The self-serve evaluation path (signup → demo → first run) works in staging TEST mode but is not reachable by a cold prospect. The marketing site is not deployed to production. This blocks Time-to-Value, Adoption Friction, and Marketability simultaneously.

3. **Correctness depth is insufficient for compliance-grade claims.** 63% branch coverage and ~35% mutation survival mean one-third of code paths are untested. For a product that produces compliance findings and governance decisions, this is a material integrity risk.

4. **Concurrency testing covers 1 of 57 mutation surfaces.** Only the governance approval path has a dedicated race-condition test. The remaining 56 mutation endpoints (run commit, policy pack CRUD, alert rules, archival, comparison persist) have no concurrency assertions.

5. **Data lifecycle cascades are application-enforced, not database-enforced.** No ON DELETE CASCADE constraints. Run archival explicitly orphans child rows. Audit events grow unbounded. Blob traces have no lifecycle policy. This is a ticking operational bomb.

6. **Azure-only platform limits total addressable market.** V1 finding engines, topology analysis, and infrastructure modules are Azure-specific. AWS-only and GCP-only organizations are explicitly disqualified. This is an intentional V1 constraint but a significant TAM limiter.

7. **No real-user usability feedback.** The operator UI exists with progressive disclosure and accessibility scanning, but no architecture practitioner has completed the pilot flow under observation. Task-completion time is unmeasured.

8. **ITSM connector gap requires customer-side integration work.** No Jira, ServiceNow, or Slack connectors in V1. Customers must build their own bridges from CloudEvents/webhooks. This shifts integration burden onto the buyer -- exactly the wrong direction for a product selling "reduced effort."

9. **Documentation volume creates contributor cognitive load.** 556 markdown files is a significant navigation and maintenance challenge. The 2026-04-23 doc compression helped the buyer surface but the contributor surface is still large.

10. **No load test baseline or performance regression gate.** Load test scaffolding exists but no results are committed. No CI gate on p95 latency. Performance regression could appear undetected.

---

## 4. Top 5 Monetization Blockers

1. **No live self-serve purchase path.** Stripe is in test mode. Azure Marketplace is not published. A prospect cannot buy without a human-mediated sales process. This limits both velocity and volume.

2. **No reference customer or published case study.** Every enterprise buyer asks "who else is using this?" The answer today is "nobody, publicly." The reference discount mechanism exists but has no rows. (V1.1 deferred -- not scored, but still a monetization reality.)

3. **Marketing site is not production-deployed.** Cold prospects cannot discover, evaluate, or self-qualify through the website. The demo preview and why-archlucid routes exist but are staging-only.

4. **ROI model is untested against real buyer behavior.** The 180 architect-hour break-even is modeled but unvalidated. If the real break-even is 500 hours, the ICP narrows significantly.

5. **Pricing is locked but untested.** 2026 prices are committed in `PRICING_PHILOSOPHY.md` but no buyer has seen them. Price sensitivity is unknown. The re-rate mechanism exists but has no data.

---

## 5. Top 5 Enterprise Adoption Blockers

1. **No CPA SOC 2 attestation.** Self-assessment is the interim posture. Procurement teams at regulated enterprises will require a Type II report before signing. (Correctly positioned as a roadmap item, not a V1 gap.)

2. **No executed third-party penetration test.** Aeronova engagement is awarded and in flight (V1.1 deferred). Security reviewers at regulated buyers will want to see findings, even redacted. The owner-conducted self-assessment is a bridge.

3. **ITSM integration requires custom work.** Enterprises with Jira/ServiceNow as system-of-record will need to build webhook bridges. This adds implementation cost and risk to the pilot.

4. **Azure-only deployment limits buyer pool.** Enterprises with AWS or GCP as primary cloud cannot benefit from topology and cost finding engines. Multi-cloud is a TAM expander but a V1 non-goal.

5. **Single-region SaaS with no contractual SLA credits.** The 99.5% SLO is an objective, not a contractual commitment. Procurement teams will want service credits in the subscription agreement. The order form template frames this but credits are "to be defined."

---

## 6. Top 5 Engineering Risks

1. **Correctness risk in untested branches.** 37% of conditional branches have never been exercised. Finding engines that produce compliance-grade recommendations with untested edge cases create a liability risk -- a wrong finding could drive a wrong architecture decision.

2. **Concurrency failure on mutation surfaces.** 56 mutation endpoints have no race-condition tests. Under load, concurrent writes to runs, policy packs, or alert rules could produce duplicate records, lost updates, or inconsistent state. The ROWVERSION guard exists on Runs but is untested.

3. **Data orphaning during archival.** Run archival does not cascade to child tables (FindingsSnapshots, GraphSnapshots, ArtifactBundles, ComparisonRecords, AgentExecutionTraces). Over time, orphaned data accumulates storage cost and creates inconsistency.

4. **LLM prompt injection via architecture requests.** The prompt redaction mechanism is deny-list-based, not allow-list-based. A sophisticated attacker could craft an architecture request that manipulates finding engine outputs. The content safety guard exists but is not tested against adversarial inputs.

5. **Hot-path cache invalidation gap.** The application-level write-through cache has no distributed invalidation. In a multi-instance deployment, one instance may serve stale data until TTL expires after another instance writes. No test validates this boundary.

---

## 7. Most Important Truth

**ArchLucid is an engineering artifact searching for its first customer.** The product has genuine technical depth -- explainability traces, provenance graphs, governance workflows, and a multi-agent AI pipeline that most competitors cannot match. The documentation discipline is exceptional. But none of this matters commercially until a real buyer completes the pilot flow, measures value, and writes a check. Every hour spent polishing internal artifacts instead of putting the product in front of a human evaluator delays the single most important validation: does anyone want this enough to pay for it?

---

## 8. Top Improvement Opportunities

### Improvement 1: Trial Funnel Staging End-to-End Validation

**Title:** Validate the self-serve trial funnel end-to-end on staging

**Why it matters:** The trial funnel is a V1 obligation. It is the primary path from prospect interest to product experience. Without a working trial funnel, every evaluation requires human sales involvement -- which does not scale and adds weeks to Time-to-Value.

**Expected impact:** Directly improves Time-to-Value (+5-8 pts), Adoption Friction (+3-5 pts), Marketability (+3-5 pts). Weighted readiness impact: +0.8-1.5%.

**Affected qualities:** Time-to-Value, Adoption Friction, Marketability, Decision Velocity.

**Status:** Fully actionable now.

**Cursor prompt:**

> Validate the self-serve trial funnel end-to-end on the staging environment. The merge-blocking spec `archlucid-ui/e2e/live-api-trial-end-to-end.spec.ts` defines the expected flow. Your tasks:
>
> 1. Read `archlucid-ui/e2e/live-api-trial-end-to-end.spec.ts` and `docs/runbooks/TRIAL_END_TO_END.md` to understand the expected flow (signup → email verification → first run → commit → trial status).
> 2. Read `archlucid-ui/e2e/trial-funnel-test-mode.spec.ts` to understand the Stripe TEST mode path.
> 3. Verify that `ArchLucid.Api/Controllers/RegistrationController.cs` handles the baseline review cycle capture fields (`baselineReviewCycleHours`, `baselineReviewCycleSource`) per `docs/library/PILOT_GUIDE.md`.
> 4. Verify that the trial enforcement (`ArchLucid.Api/Filters/CommercialTenantTierFilter.cs`) returns HTTP 402 with an RFC 9457 problem body that includes an upgrade path and pricing link.
> 5. Verify that the trial banner in the operator UI reads `firstCommitUtc` from `GET /v1/tenant/trial-status` and displays "Day N since first commit" per `docs/library/SPONSOR_BANNER_FIRST_COMMIT_BADGE.md`.
> 6. If any of the above have gaps, fix them. Do not change the Stripe integration mode (stays TEST). Do not change the Marketplace listing state.
>
> **Acceptance criteria:**
> - `live-api-trial-end-to-end.spec.ts` passes in CI.
> - `trial-funnel-test-mode.spec.ts` passes in CI.
> - Registration endpoint accepts and persists baseline review cycle fields.
> - HTTP 402 response body includes `upgradeUrl` or equivalent.
> - Trial banner renders `firstCommitUtc` day count.
>
> **Constraints:**
> - Do not flip Stripe to live mode.
> - Do not modify the Marketplace listing.
> - Do not change auth modes.
> - Do not modify historical SQL migration files.

---

### Improvement 2: Concurrency Tests for Top Mutation Endpoints

**Title:** Add concurrency regression tests for the five highest-risk mutation endpoints

**Why it matters:** Only 1 of 57 mutation surfaces has a concurrency test. For a system that produces compliance findings with audit trails, concurrent correctness is not optional. A race condition on run commit or policy pack creation could produce duplicate records or inconsistent governance state.

**Expected impact:** Directly improves Correctness (+4-6 pts), Data Consistency (+5-8 pts), Reliability (+2-3 pts). Weighted readiness impact: +0.5-1.0%.

**Affected qualities:** Correctness, Data Consistency, Reliability, Trustworthiness.

**Status:** Fully actionable now.

**Cursor prompt:**

> Add concurrency regression tests for the five highest-risk mutation endpoints. Use the existing `GovernanceApprovalConcurrencyIntegrationTests` as the pattern (SERIALIZABLE + parallel tasks + 409 assertion). The existing `SqlServerContainer` test infrastructure and `WebApplicationFactory` patterns in `ArchLucid.Api.Tests` should be reused.
>
> Add tests for these five endpoints:
>
> 1. **Run commit** (`POST /v1/architecture/run/{runId}/commit`) — Two parallel commit requests for the same run should result in one success and one 409 Conflict (ROWVERSION guard). Test in `CommitRunConcurrencyIntegrationTests.cs` (file exists — extend it if it does not already cover parallel writers, or add a new parallel-commit test if it only covers sequential).
> 2. **Policy pack creation** (`POST /v1/policy-packs`) — Two parallel creates with the same name/scope should result in one success and one 409 (unique constraint). Create `PolicyPackConcurrencyIntegrationTests.cs`.
> 3. **Alert rule creation** (`POST /v1/alert-rules`) — Two parallel creates with the same name should result in one success and one 409. Create `AlertRuleConcurrencyIntegrationTests.cs`.
> 4. **Run archival** (`POST /v1/admin/archival/archive`) — Two parallel archival requests for the same run should be idempotent. Create `ArchivalConcurrencyIntegrationTests.cs`.
> 5. **Comparison record persist** (`POST /v1/architecture/compare` with `persist=true`) — Two parallel compares of the same runs should both succeed (no uniqueness constraint on comparisons, but both should produce valid records). Create `ComparisonPersistConcurrencyIntegrationTests.cs`.
>
> Each test should:
> - Use `Task.WhenAll` with 2-5 parallel HTTP requests.
> - Assert that exactly one succeeds when uniqueness applies, or all succeed when idempotency applies.
> - Assert the database state is consistent after all requests complete.
> - Use `do not use ConfigureAwait(false) in tests`.
>
> **Acceptance criteria:**
> - Five new or extended test files, each with at least one parallel-writer test.
> - All tests pass in CI.
> - Each test creates its own isolated run/scope (no cross-test dependency).
>
> **Constraints:**
> - Do not modify production code unless a concurrency bug is discovered during testing.
> - Do not change database migration files.
> - Reuse existing `WebApplicationFactory` and `SqlServerContainer` patterns.

---

### Improvement 3: Mutation Testing Ratchet to 72% for Decisioning and Application

**Title:** Ratchet Stryker mutation score to 72% for Decisioning and Application projects

**Why it matters:** Mutation testing reveals weak or missing assertions. The Decisioning project contains the finding engines and golden manifest builder -- the core intellectual property. The Application project contains governance, coordinator, and business logic. Moving from ~65-70% to 72% catches the most impactful surviving mutations.

**Expected impact:** Directly improves Correctness (+3-5 pts), Testability (+2-3 pts). Weighted readiness impact: +0.3-0.6%.

**Affected qualities:** Correctness, Testability, Trustworthiness.

**Status:** Fully actionable now.

**Cursor prompt:**

> Ratchet Stryker mutation scores to 72% for the Decisioning and Application projects. Follow the ratchet sequence documented in `docs/library/STRYKER_RATchet_TARGET_72.md`.
>
> 1. Read `docs/library/STRYKER_RATchet_TARGET_72.md` for the safe ratchet sequence.
> 2. Read `docs/library/MUTATION_TESTING_STRYKER.md` for the current baselines and configuration.
> 3. Read `scripts/ci/stryker-baselines.json` for the current measured scores.
> 4. Identify the surviving mutations in `ArchLucid.Decisioning` by examining the most recent Stryker HTML report or by running `dotnet dotnet-stryker -s ArchLucid.sln` with `stryker-config.decisioning.json`.
> 5. Write tests that kill the highest-impact surviving mutations in these areas (prioritize):
>    - `FindingsOrchestrator` and individual finding engine edge cases
>    - `DefaultGoldenManifestBuilder` merge logic
>    - `EffectiveGovernanceResolver` resolution logic
>    - `RecommendationGenerator` output paths
> 6. After tests pass, update `scripts/ci/stryker-baselines.json` Decisioning and Application entries to the new measured score (round down to one decimal).
> 7. If the measured score reaches 72.0+, update `stryker-config.decisioning.json` and `stryker-config.application.json` to set `thresholds.break` to 72.
>
> **Acceptance criteria:**
> - Decisioning and Application Stryker scores >= 72.0% in `stryker-baselines.json`.
> - All new tests pass.
> - `assert_stryker_score_vs_baseline.py` passes with the new baselines.
>
> **Constraints:**
> - Do not lower any existing baselines.
> - Do not modify production code to make mutations easier to kill (no test-only code paths).
> - Do not change the Stryker configuration structure (mutators, excluded files).
> - Prefer unit tests over integration tests for mutation kills.

---

### Improvement 4: DEFERRED -- Production Marketing Site Deployment

**Title:** DEFERRED -- Deploy marketing site to production domain

**Reason for deferral:** Production deployment requires owner decisions on:
- Domain acquisition and DNS configuration (`archlucid.net`)
- Azure Front Door custom domain attachment and managed certificate
- Whether to use the staging deployment as-is or create a separate production Front Door origin
- SSL certificate provisioning strategy

**Information needed from owner:**
- Has `archlucid.net` been acquired? If so, what is the DNS registrar and current nameserver configuration?
- Should the production marketing site share the same Azure subscription and resource group as the API, or a separate one?
- Is the marketing site deployment gated on any other V1 milestone?

---

### Improvement 5: Data Lifecycle Cascade for Run Archival

**Title:** Implement complete data cascade for run archival

**Why it matters:** Run archival currently orphans child rows (FindingsSnapshots, GraphSnapshots, ArtifactBundles, ComparisonRecords, AgentExecutionTraces). Over time, orphaned data accumulates storage cost and creates inconsistency. The orphan probe detects but does not remediate.

**Expected impact:** Directly improves Data Consistency (+8-10 pts), Reliability (+3-5 pts), Cost-Effectiveness (+2-3 pts). Weighted readiness impact: +0.4-0.7%.

**Affected qualities:** Data Consistency, Reliability, Cost-Effectiveness, Correctness.

**Status:** Fully actionable now.

**Cursor prompt:**

> Implement complete data cascade for run archival. The current `DataArchivalCoordinator` sets `ArchivedUtc` on the `Runs` row but does not cascade to child tables.
>
> 1. Read `ArchLucid.Api/Services/DataArchivalCoordinator.cs` (or the equivalent host service) to understand the current archival flow.
> 2. Read `docs/library/CORRECTNESS_QUALITY_ASSESSMENT_2026_04_15.md` §3 (Data Lifecycle and Cascade Completeness) for the identified gaps.
> 3. Read `ArchLucid.Persistence/Scripts/ArchLucid.sql` to understand the table relationships.
> 4. Extend the archival coordinator to cascade `ArchivedUtc` to these child tables when a run is archived:
>    - `dbo.FindingsSnapshots` (where `RunId` matches)
>    - `dbo.GraphSnapshots` (where `RunId` matches)
>    - `dbo.ArtifactBundles` (where `RunId` matches, if applicable via manifest)
>    - `dbo.ComparisonRecords` (where `BaselineRunId` or `CandidateRunId` matches)
> 5. Add a new DbUp migration script (next sequential number) that adds the cascade UPDATE for each child table. Use application-level cascade (UPDATE statement in the archival service), not database-level ON DELETE CASCADE.
> 6. Extend the existing `DataArchivalCoordinatorCorrelationTests` with tests that verify child rows are archived when a parent run is archived.
> 7. Add a test in `AdminDataConsistencyOrphanRemediationEndpointsIntegrationTests` that verifies the orphan probe detects zero orphans after a cascaded archival.
>
> **Acceptance criteria:**
> - When a run is archived, all child FindingsSnapshots, GraphSnapshots, ArtifactBundles, and ComparisonRecords for that run have `ArchivedUtc` set.
> - The archival coordinator logs the count of child rows cascaded (structured log with `CorrelationId`).
> - Existing archival tests still pass.
> - New cascade tests pass.
> - Orphan probe returns zero orphans for archived runs.
>
> **Constraints:**
> - Do not add ON DELETE CASCADE at the database level.
> - Do not modify historical migration files (001-028 or any existing).
> - Do not delete archived rows -- only set `ArchivedUtc`.
> - Use the existing `DataArchivalCoordinator` pattern (correlation, logging).

---

### Improvement 6: Golden Corpus Expansion Beyond Decisioning

**Title:** Expand golden corpus to cover context ingestion and artifact synthesis

**Why it matters:** The 30 golden corpus cases cover only the decisioning stage. Context ingestion (Terraform parsing, document connectors, topology hints) and artifact synthesis (DOCX, Mermaid diagrams, coverage summaries) have no golden-output regression tests. A change to a parser or renderer could silently produce wrong outputs.

**Expected impact:** Directly improves Correctness (+3-5 pts), AI/Agent Readiness (+2-3 pts). Weighted readiness impact: +0.3-0.5%.

**Affected qualities:** Correctness, AI/Agent Readiness, Explainability.

**Status:** Fully actionable now.

**Cursor prompt:**

> Expand the golden corpus test suite to cover context ingestion and artifact synthesis stages.
>
> 1. Read `tests/golden-corpus/decisioning/case-01/README.md` to understand the golden corpus test pattern (input → expected output → assertion).
> 2. Read `ArchLucid.Decisioning.Tests/GoldenCorpus/GoldenCorpusMaterializerTests.cs` to understand the test harness.
> 3. Read `ArchLucid.ContextIngestion/Canonicalization/ICanonicalEnricher.cs` and `ArchLucid.ContextIngestion.Tests/TerraformShowJsonInfrastructureDeclarationParserTests.cs` to understand the context ingestion pipeline.
> 4. Read `ArchLucid.ArtifactSynthesis/Generators/CoverageSummaryArtifactGenerator.cs` to understand artifact synthesis.
>
> Create the following golden corpus test cases:
>
> **Context Ingestion (5 cases):**
> - `tests/golden-corpus/ingestion/case-01/` — Terraform `show -json` output for a simple Azure resource group + storage account → expected canonical model.
> - `tests/golden-corpus/ingestion/case-02/` — Terraform output with networking (VNet, subnet, NSG) → expected topology hints.
> - `tests/golden-corpus/ingestion/case-03/` — Document connector input (markdown architecture doc) → expected structured extraction.
> - `tests/golden-corpus/ingestion/case-04/` — Empty/minimal Terraform output → expected empty canonical model (no crash).
> - `tests/golden-corpus/ingestion/case-05/` — Terraform output with unsupported resource types → expected partial parse with warnings.
>
> **Artifact Synthesis (3 cases):**
> - `tests/golden-corpus/synthesis/case-01/` — Golden manifest with findings → expected coverage summary markdown.
> - `tests/golden-corpus/synthesis/case-02/` — Golden manifest with 0 findings → expected "no findings" summary.
> - `tests/golden-corpus/synthesis/case-03/` — Golden manifest with mixed severity findings → expected severity-ordered summary.
>
> Each case should have:
> - `README.md` with scenario description
> - `input.json` (or appropriate input format)
> - `expected-output.json` (or appropriate output format)
> - A corresponding test class in the appropriate test project.
>
> **Acceptance criteria:**
> - 8 new golden corpus cases with README, input, expected output, and test.
> - All tests pass.
> - Tests use deterministic mode (no LLM calls).
>
> **Constraints:**
> - Follow the existing golden corpus directory structure.
> - Use the existing test harness patterns.
> - Do not modify existing golden corpus cases.

---

### Improvement 7: Load Test Baseline

**Title:** Establish a load test baseline for the core pilot flow

**Why it matters:** No load test results exist. Performance regressions could appear undetected. Without a baseline, the p95 < 2s SLO cannot be validated and capacity planning is guesswork.

**Expected impact:** Directly improves Performance (+5-8 pts), Scalability (+3-5 pts), Availability (+2-3 pts). Weighted readiness impact: +0.2-0.4%.

**Affected qualities:** Performance, Scalability, Availability, Reliability.

**Status:** Fully actionable now.

**Cursor prompt:**

> Establish a load test baseline for the core pilot flow using k6 or a similar tool. The `tests/load/` directory exists as scaffolding.
>
> 1. Read `tests/load/README.md` for any existing scaffolding or instructions.
> 2. Read `docs/go-to-market/SLA_SUMMARY.md` for the target SLOs (99.5% availability, p95 < 2s).
> 3. Read `docs/library/ARCHITECTURE_FLOWS.md` Flow A for the core pilot flow (request → tasks → results → commit → manifest).
>
> Create a load test script in `tests/load/` that:
>
> 1. **Scenario: Core pilot flow** — 10 virtual users, each executing: create run → execute → commit → fetch manifest → fetch artifacts. Run for 5 minutes.
> 2. **Scenario: Read-heavy** — 50 virtual users, each executing: list runs → run detail → manifest summary. Run for 5 minutes.
> 3. **Scenario: Mixed** — 5 writers (create/execute/commit) + 25 readers (list/detail/manifest). Run for 10 minutes.
>
> Each scenario should:
> - Target the local API (`http://localhost:5001` with DevelopmentBypass auth).
> - Record p50, p95, p99 latencies per endpoint.
> - Record error rate (non-2xx responses).
> - Record throughput (requests/second).
> - Output results to `tests/load/results/baseline-YYYY-MM-DD.json`.
>
> Add a `tests/load/README.md` update documenting:
> - How to run the load test.
> - What the baseline results mean.
> - How to compare future runs against the baseline.
>
> **Acceptance criteria:**
> - Load test script exists and is runnable with `k6 run tests/load/core-pilot.js` (or equivalent).
> - Baseline results committed with date stamp.
> - README documents the process.
> - p95 latency for all endpoints is recorded.
>
> **Constraints:**
> - Use DevelopmentBypass auth (no Entra/JWT setup required).
> - Target InMemory storage provider for the baseline (no SQL dependency for initial baseline).
> - Do not run against production or staging.
> - Keep test data small (10-50 runs per scenario).

---

### Improvement 8: Accessibility Coverage Expansion

**Title:** Expand accessibility scanning to all operator routes

**Why it matters:** Only 5 operator pages have axe-core scanning. The operator UI has 20+ routes. Unscanned pages may have WCAG violations that block adoption by accessibility-conscious enterprises or trigger legal obligations.

**Expected impact:** Directly improves Accessibility (+8-12 pts), Usability (+2-3 pts), Compliance Readiness (+1-2 pts). Weighted readiness impact: +0.2-0.4%.

**Affected qualities:** Accessibility, Usability, Compliance Readiness.

**Status:** Fully actionable now.

**Cursor prompt:**

> Expand accessibility scanning to cover all operator routes in the ArchLucid UI.
>
> 1. Read `ACCESSIBILITY.md` at the repo root for the current status (5 pages scanned: Home, Runs, Audit, Policy packs, Alerts).
> 2. Read `archlucid-ui/e2e/live-api-accessibility.spec.ts` for the current axe scanning pattern.
> 3. Read `archlucid-ui/src/lib/nav-config.ts` to get the full list of operator routes.
> 4. Read `archlucid-ui/src/accessibility/` for the fast Vitest + jest-axe component tests.
>
> Add axe-core scanning for these additional routes (at minimum):
>
> - `/runs/new` (New run wizard)
> - `/compare` (Compare two runs)
> - `/replay` (Replay a run)
> - `/graph` (Provenance graph)
> - `/ask` (Natural-language Ask)
> - `/advisory` (Advisory scans)
> - `/governance` (Governance workflow)
> - `/governance-resolution` (Governance resolution)
> - `/governance/dashboard` (Governance dashboard)
> - `/alert-rules` (Alert rules)
> - `/alert-routing` (Alert routing)
> - `/search` (Retrieval search)
> - `/value-report` (Value report)
> - `/digests` (Architecture digests)
>
> For each route:
> 1. Add the route to the `PAGES` array in `archlucid-ui/e2e/live-api-accessibility.spec.ts` (or the equivalent accessibility spec).
> 2. Gate on critical + serious violations only (consistent with existing pattern).
> 3. Add a fast Vitest + jest-axe component test in `archlucid-ui/src/accessibility/` for the page shell component if one does not exist.
>
> Update `ACCESSIBILITY.md` to list the newly covered pages.
>
> **Acceptance criteria:**
> - At least 14 additional routes have axe-core scanning in the e2e spec.
> - All new scans pass (no critical/serious violations, or violations are documented as known exemptions in `ACCESSIBILITY.md`).
> - `ACCESSIBILITY.md` "Pages with automated checks" table includes all covered routes.
> - CI continues to pass.
>
> **Constraints:**
> - Do not remove existing accessibility tests.
> - Do not lower the severity threshold (critical + serious remain blocking).
> - If a route requires authentication state, use the existing live-api test setup.
> - Document any new exemptions with rule ID, affected page, justification, and planned resolution date.

---

### Improvement 9: CloudEvents-to-ITSM Bridge Documentation

**Title:** Document CloudEvents-to-Jira and CloudEvents-to-ServiceNow bridge patterns

**Why it matters:** Jira and ServiceNow connectors are deferred to V1.1, but enterprise pilots may need ITSM integration from day one. Without a documented bridge pattern, customers must reverse-engineer the CloudEvents schema and build their own webhook consumer. A concrete recipe reduces integration friction and buys time until first-party connectors ship.

**Expected impact:** Directly improves Workflow Embeddedness (+3-5 pts), Adoption Friction (+2-3 pts), Customer Self-Sufficiency (+3-5 pts). Weighted readiness impact: +0.2-0.4%.

**Affected qualities:** Workflow Embeddedness, Adoption Friction, Customer Self-Sufficiency, Interoperability.

**Status:** Fully actionable now.

**Cursor prompt:**

> Create bridge documentation for CloudEvents-to-Jira and CloudEvents-to-ServiceNow integration. This is V1 documentation work -- no code changes to the core product.
>
> 1. Read `docs/library/INTEGRATION_EVENTS_AND_WEBHOOKS.md` for the current webhook and CloudEvents documentation.
> 2. Read `templates/integrations/servicenow/servicenow-incident-recipe.md` for any existing ServiceNow scaffolding.
> 3. Read `docs/go-to-market/INTEGRATION_CATALOG.md` for the planned connector status.
> 4. Read `ArchLucid.Core/Integration/` for the CloudEvents event type catalog.
>
> Create two bridge recipe documents:
>
> **`templates/integrations/jira/jira-webhook-bridge-recipe.md`:**
> - Title: "ArchLucid → Jira: Webhook Bridge Recipe (V1)"
> - Scope: One-way finding → Jira issue creation using ArchLucid CloudEvents webhooks and Jira REST API v3.
> - Include: webhook subscription setup, CloudEvents payload schema (with concrete JSON example for `com.archlucid.finding.created`), Jira issue create API call, field mapping (finding severity → Jira priority, finding title → Jira summary, finding detail → Jira description with ArchLucid run link), error handling, and a sample Azure Function or Logic App recipe.
> - Note: "This bridge is a V1 workaround. A first-party Jira connector is planned for V1.1."
>
> **`templates/integrations/jira/jira-webhook-bridge-recipe.md` equivalent for ServiceNow:**
> - Extend the existing `servicenow-incident-recipe.md` with the same level of detail: webhook subscription, CloudEvents payload, ServiceNow Incident table API call, field mapping, sample Logic App recipe.
>
> Each recipe should follow the architecture outputs format (Objective, Assumptions, Constraints, Architecture Overview, Component Breakdown, Data Flow, Security Model, Operational Considerations).
>
> **Acceptance criteria:**
> - Two bridge recipe markdown files exist under `templates/integrations/`.
> - Each recipe includes a concrete CloudEvents JSON payload example.
> - Each recipe includes a complete field mapping table.
> - Each recipe includes a sample integration implementation (Azure Function or Logic App).
> - `docs/go-to-market/INTEGRATION_CATALOG.md` references the recipes for V1 bridge guidance.
>
> **Constraints:**
> - Do not modify core product code.
> - Do not create first-party connector code (that is V1.1 scope).
> - Use real CloudEvents event types from the codebase (not invented ones).
> - Follow the architecture outputs template per workspace rules.

---

## 9. Deferred Scope Uncertainty

I was able to locate `docs/library/V1_DEFERRED.md` which comprehensively lists all deferred items across 8 sections (product learning brains, compliance audit, rename Phase 7, operator experience, infrastructure polish, ITSM connectors V1.1, chat-ops connectors V2, commercial milestones V1.1, and security/assurance milestones V1.1). No uncertainty exists about what is deferred. All scoring in this assessment respects the deferred boundaries defined in that document and the associated `PENDING_QUESTIONS.md` owner decisions dated 2026-04-23.

---

## 10. Pending Questions for Later

### Improvement 4 (DEFERRED: Production Marketing Site Deployment)
- Has `archlucid.net` been acquired? What is the current DNS registrar and nameserver configuration?
- Should the production marketing site share the Azure subscription with the API, or use a separate subscription?
- Is the production marketing deployment gated on any other V1 milestone (e.g., staging trial funnel completion)?
- What is the target go-live date for the marketing site (even approximate)?

### Improvement 1 (Trial Funnel Validation)
- Is the staging trial funnel currently accessible at `staging.archlucid.net/signup`, or is there a different staging URL?
- Has the Stripe TEST mode been configured with test webhook secrets on the staging environment?

### Proof-of-ROI Readiness
- Have any informal conversations with architecture leaders occurred to validate the 180 architect-hour/year break-even assumption?
- Is there a target pilot customer identified (even unnamed) for the first reference engagement?

### Executive Value Visibility
- Should the executive sponsor brief be renderable as a PDF from the marketing site, or is markdown sufficient for V1?
- Does the value report DOCX need a one-page executive summary prepended, or is the current structure sufficient?

### Differentiability
- Has "AI Architecture Intelligence" been tested as a category name with any target buyer (even informally)?
- Are there any competitive threats you are aware of that are not covered in `COMPETITIVE_LANDSCAPE.md`?

---

## Appendix: Score Summary Table

| Quality | Score | Weight | Weighted | Deficiency |
|---------|-------|--------|----------|------------|
| Time-to-Value | 58 | 7 | 406 | 294 |
| Marketability | 65 | 8 | 520 | 280 |
| Adoption Friction | 58 | 6 | 348 | 252 |
| Proof-of-ROI Readiness | 65 | 5 | 325 | 175 |
| Correctness | 62 | 4 | 248 | 152 |
| Executive Value Visibility | 68 | 4 | 272 | 128 |
| Workflow Embeddedness | 58 | 3 | 174 | 126 |
| Usability | 60 | 3 | 180 | 120 |
| Differentiability | 72 | 4 | 288 | 112 |
| Trustworthiness | 68 | 3 | 204 | 96 |
| Security | 70 | 3 | 210 | 90 |
| Data Consistency | 55 | 2 | 110 | 90 |
| Interoperability | 55 | 2 | 110 | 90 |
| Decision Velocity | 58 | 2 | 116 | 84 |
| Compliance Readiness | 60 | 2 | 120 | 80 |
| Commercial Packaging Readiness | 60 | 2 | 120 | 80 |
| Reliability | 62 | 2 | 124 | 76 |
| Azure Compat & SaaS Readiness | 68 | 2 | 136 | 64 |
| Maintainability | 68 | 2 | 136 | 64 |
| Procurement Readiness | 72 | 2 | 144 | 56 |
| Traceability | 82 | 3 | 246 | 54 |
| Cognitive Load | 55 | 1 | 55 | 45 |
| Scalability | 55 | 1 | 55 | 45 |
| Template & Accelerator Richness | 55 | 1 | 55 | 45 |
| AI/Agent Readiness | 78 | 2 | 156 | 44 |
| Policy & Governance Alignment | 78 | 2 | 156 | 44 |
| Availability | 58 | 1 | 58 | 42 |
| Customer Self-Sufficiency | 58 | 1 | 58 | 42 |
| Auditability | 80 | 2 | 160 | 40 |
| Explainability | 80 | 2 | 160 | 40 |
| Performance | 60 | 1 | 60 | 40 |
| Cost-Effectiveness | 62 | 1 | 62 | 38 |
| Accessibility | 62 | 1 | 62 | 38 |
| Change Impact Clarity | 65 | 1 | 65 | 35 |
| Manageability | 68 | 1 | 68 | 32 |
| Stickiness | 70 | 1 | 70 | 30 |
| Supportability | 72 | 1 | 72 | 28 |
| Deployability | 72 | 1 | 72 | 28 |
| Testability | 72 | 1 | 72 | 28 |
| Extensibility | 72 | 1 | 72 | 28 |
| Evolvability | 72 | 1 | 72 | 28 |
| Observability | 75 | 1 | 75 | 25 |
| Documentation | 75 | 1 | 75 | 25 |
| Modularity | 78 | 1 | 78 | 22 |
| Azure Ecosystem Fit | 82 | 1 | 82 | 18 |
| Architectural Integrity | 78 | 3 | 234 | 66 |
| **TOTALS** | | **100** | **6741** | **3259** |

**Weighted Readiness: 6741 / 10000 × 100 = 67.41%**
