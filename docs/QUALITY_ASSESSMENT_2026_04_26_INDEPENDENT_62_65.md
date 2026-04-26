# ArchLucid Assessment – Weighted Readiness 62.65%

**Date:** 2026-04-26
**Assessor:** Independent first-principles analysis (Opus 4.6)
**Method:** Scored 46 qualities (1–100) across Commercial, Enterprise, and Engineering dimensions with owner-provided weights (total weight 102). Weighted average = 62.65%.
**Correction (same day):** Azure Compatibility and SaaS Deployment Readiness adjusted from 65→68, Deployability from 60→63, after confirming a dev environment (API + SQL + Storage) is deployed to Azure via Terraform. Improvement 9 changed from DEFERRED to actionable. Original score was 62.56%.

---

## 1. Executive Summary

### Overall Readiness

ArchLucid is a technically ambitious AI-driven architecture review platform with strong internal engineering discipline — extensive test suites (961+ backend test classes, 195 UI test files), thorough CI (24 workflows including secret scanning, DAST, mutation testing, chaos engineering), and deeply documented architecture. However, the product remains pre-revenue with no live customers, no production SaaS deployment, and critical commercial surfaces (Stripe, Azure Marketplace) in test-mode only. The weighted readiness of 62.56% reflects a system that is engineering-mature relative to its commercial maturity. The gap between "built" and "sold" is the dominant risk.

### Commercial Picture

The commercial motion is incomplete. There is no live trial funnel, no self-service signup path, and no paying customer. Pricing philosophy exists but is internal-only. The Marketplace listing and Stripe checkout are wired but held in test mode behind owner decisions. Time-to-value for a prospective buyer requires significant hand-holding — the product cannot be evaluated without either a guided demo or running Docker locally (despite the SaaS framing). The marketing site has placeholder screenshots. This is the single most impactful area of weakness.

### Enterprise Picture

Enterprise readiness is uneven. Governance, audit (78 typed events, append-only store), RBAC, SCIM 2.0, and RLS are implemented at a level that exceeds many pre-V1 products. Trust Center documentation, pen-test scaffolding, and SOC 2 self-assessment exist. However, no third-party pen test has completed, no SOC 2 attestation exists, the privacy notice is draft-only, commerce is not live, and interoperability is limited (no Jira, ServiceNow, Slack, Confluence — all deferred to V1.1/V2). Enterprise procurement will stumble on the absence of a completed third-party security assessment and live commerce.

### Engineering Picture

Engineering is the strongest dimension. The codebase demonstrates genuine discipline: Dapper over heavy ORMs, clear bounded contexts across ~55 .NET projects, typed audit events, RLS with SESSION_CONTEXT, structured CI tiers, mutation testing (Stryker), OWASP ZAP baseline, Schemathesis contract testing, k6 load tests, Simmy chaos injection, and architectural constraint tests (NetArchTest). The authority pipeline migration (ADR 0021/0030) is a well-managed strangler fig. Primary risks are: the system has never run in production under real tenant load, SQL-heavy persistence lacks production-scale validation, and the LLM integration path (real Azure OpenAI) has limited evidence of working end-to-end beyond simulator mode.

---

## 2. Weighted Quality Assessment

Ordered by weighted deficiency (most urgent first).

---

### 1. Time-to-Value — Score: 55 | Weight: 7 | Weighted Deficiency: 315

**Justification:** A buyer who arrives at the product today cannot experience value without significant friction. The cloud trial funnel (`archlucid.com/signup`) is "wired in code but not yet live in production." The local path requires Docker + SQL Server. The simulator mode produces deterministic fake results that do not demonstrate real AI capability. The `--real` mode requires an Azure OpenAI deployment the buyer does not have. The gap between "signing up" and "seeing a meaningful architecture review" is weeks, not minutes.

**Tradeoffs:** Building the full pipeline infrastructure before having customers is expensive. Simulator mode de-risks demos but undercuts believability.

**Recommendations:** Ship a hosted demo environment with pre-committed sample runs. Make the cloud trial signup live with a pre-seeded tenant. Create a 5-minute video walkthrough showing a real (non-simulated) run.

**Fixability:** V1 (demo environment, video); V1.1 (live trial funnel).

---

### 2. Adoption Friction — Score: 48 | Weight: 6 | Weighted Deficiency: 312

**Justification:** Despite extensive documentation (150+ markdown files, five-document spine, role-based onboarding), the adoption path is paradoxically high-friction. The docs assume the reader already has context about "authority pipelines," "golden manifests," and "coordinator strangler figs." A buyer landing on the repo sees 80+ top-level directories. The CLI has 20+ commands. The wizard has 7 steps across 3 macro phases. There is no "Hello World" equivalent — no single command that produces a visible, self-explanatory output in under 60 seconds against a hosted service.

**Tradeoffs:** Thoroughness of documentation creates its own complexity. The domain is genuinely complex.

**Recommendations:** Create `archlucid try --hosted` that hits a public demo API. Reduce the new-run wizard to 3 steps for a first run. Add inline tooltips explaining domain terms at point of use.

**Fixability:** V1 (wizard simplification, tooltips); V1.1 (hosted try command).

---

### 3. Marketability — Score: 62 | Weight: 8 | Weighted Deficiency: 304

**Justification:** The marketing site exists with `/why`, `/pricing`, `/get-started`, `/trust`, and `/see-it` routes. The competitive comparison and pricing pages are built. The brand-neutral category is being migrated to "AI Architecture Review Board." However: (a) no live demo or video shows the product working, (b) screenshot placeholders remain, (c) `/signup` is not functional, (d) there is no customer logo or testimonial, (e) the "why" narrative is engineering-dense rather than outcome-focused. The product is marketed as SaaS but cannot be purchased or trialed as SaaS.

**Tradeoffs:** Building marketing collateral before product-market fit risks rework. Placeholder approach is honest.

**Recommendations:** Capture and publish real screenshots from a demo run. Write 3 outcome-focused case summaries (even hypothetical design-partner scenarios). Record a 2-minute product walkthrough video.

**Fixability:** V1 (screenshots, copy improvements); V1.1 (video, case studies).

---

### 4. Proof-of-ROI Readiness — Score: 58 | Weight: 5 | Weighted Deficiency: 210

**Justification:** A `FirstValueReportBuilder` exists. An ROI bulletin template exists. `PilotRunDeltaComputer` can show before/after deltas. A `PILOT_ROI_MODEL.md` companion exists. However, none of this has been validated with a real customer. The ROI framing assumes the buyer already knows what "architecture drift" costs them — there is no calculator or benchmark data. The first-value report is a code path, not a validated artifact.

**Tradeoffs:** ROI modeling requires customer data that does not exist yet.

**Recommendations:** Build a simple web-based ROI calculator with industry benchmarks. Pre-fill the first-value report with annotated explanations of each metric. Document 3 hypothetical ROI scenarios with specific dollar amounts.

**Fixability:** V1 (calculator, scenarios); V1.1 (customer-validated metrics).

---

### 5. Workflow Embeddedness — Score: 50 | Weight: 3 | Weighted Deficiency: 150

**Justification:** GitHub Actions and Azure DevOps pipeline tasks for manifest delta are shipped. Teams notifications (one-way) are implemented. CloudEvents webhooks exist. However: Jira, ServiceNow, Confluence, and Slack are all deferred to V1.1/V2. There is no VS Code integration. The product sits outside the developer's daily workflow — it requires switching to a separate UI to create and review runs. For enterprise adoption, ITSM integration is table stakes.

**Tradeoffs:** Building connectors before validating core value is premature. Microsoft-native first is a reasonable strategy.

**Recommendations:** Prioritize the Jira connector in V1.1 planning. Document the CloudEvents webhook integration pattern with a worked example. Consider a lightweight VS Code extension that shows run status.

**Fixability:** V1.1 (Jira, ServiceNow); V2 (Slack, VS Code).

---

### 6. Executive Value Visibility — Score: 64 | Weight: 4 | Weighted Deficiency: 144

**Justification:** Executive sponsor brief exists. Exec digest composition and markdown formatting exist. A "days since first commit" badge is designed. A `/why` competitive comparison page exists. However, the executive-facing artifacts are text-heavy with no dashboards, charts, or visual summaries. There is no executive-oriented "portfolio view" showing architecture health across multiple systems. The sponsor brief is a markdown document, not an interactive experience.

**Tradeoffs:** Visual dashboards require design investment and real data.

**Recommendations:** Add a simple dashboard to the Home page showing committed manifests, finding severity distribution, and drift trend. Build a one-page PDF executive summary auto-generated from a committed run.

**Fixability:** V1 (home dashboard, PDF summary).

---

### 7. Usability — Score: 55 | Weight: 3 | Weighted Deficiency: 135

**Justification:** The operator UI uses Next.js + Mantine with progressive sidebar disclosure (essential/extended/advanced). Accessibility is taken seriously (WCAG 2.1 AA target, 35 pages scanned with axe-core, skip-to-content, aria labels). The wizard is structured but 7 steps long. The error-handling surfaces problem details. However: (a) there is no guided tour for first-time users (tour copy was pending approval, now approved but not yet live), (b) the cognitive load of the UI is high — many pages, many concepts, deep sidebar, (c) loading states use skeletons but error states may be cryptic, (d) the comparison and governance UIs are complex without clear visual hierarchy.

**Tradeoffs:** The product surface is large. Simplifying risks losing power users.

**Recommendations:** Ship the approved opt-in tour. Add contextual "what is this?" tooltips on key pages. Reduce the wizard to 3 steps for a first run (collapse identity + description + constraints).

**Fixability:** V1 (tour, tooltips, wizard simplification).

---

### 8. Trustworthiness — Score: 58 | Weight: 3 | Weighted Deficiency: 126

**Justification:** The system has extensive governance controls (pre-commit gate, approval workflows, policy packs, segregation of duties). Audit trail is durable and typed. RLS provides defense-in-depth. STRIDE threat model exists. However: (a) the LLM-generated findings have no validated accuracy baseline — the simulator produces deterministic but fabricated results, and `--real` mode has not been tested at scale, (b) no customer has validated that the architecture findings are "correct" or useful, (c) the trust center references a pen test that has been awarded but not completed, (d) the SOC 2 self-assessment is internal-only. A buyer must trust that the AI produces meaningful architecture reviews without empirical evidence.

**Tradeoffs:** Validating AI output quality requires real customers and real data.

**Recommendations:** Run 5 real architecture reviews against known-good reference architectures and publish accuracy metrics. Complete the Aeronova pen test. Document the golden-cohort drift detection results.

**Fixability:** V1 (reference architecture validation); V1.1 (pen test completion, golden cohort real-LLM results).

---

### 9. Differentiability — Score: 70 | Weight: 4 | Weighted Deficiency: 120

**Justification:** The product occupies a genuinely underserved niche: automated AI-driven architecture review with governance workflows, audit trails, and manifest versioning. The combination of knowledge graph, decision engine, typed findings, and policy packs is unique. Competitive comparison exists at `/why`. However, the differentiation is theoretical until a buyer can experience it. The "AI Architecture Review Board" positioning is still settling. There is no clear "only ArchLucid can do X" claim validated by a customer.

**Tradeoffs:** The category may not exist yet, which is both an opportunity and a risk.

**Recommendations:** Sharpen the competitive positioning to 3 bullet points. Create a "before ArchLucid / after ArchLucid" comparison using a real architecture review. File the trademark for "AI Architecture Review Board" if proceeding with that brand.

**Fixability:** V1 (positioning sharpening); V1.1 (validated before/after).

---

### 10. Correctness — Score: 72 | Weight: 4 | Weighted Deficiency: 112

**Justification:** The system has strong structural correctness: typed contracts, manifest hashing, traceability rules (`AuthorityCommitTraceabilityRules.GetLinkageGaps`), idempotent commit with retry/reconciliation, transactional persistence, and extensive property-based tests. The decision engine, finding engines, and governance workflow have dedicated test suites. However: (a) the "correctness" of AI-generated architecture findings is unvalidated — no ground-truth dataset, no human-vs-AI comparison study, (b) the authority projection builder has "known-empty" JSON fields (Services, Datastores, Relationships) per ADR 0030, meaning the manifest is structurally incomplete, (c) the coordinator→authority migration is mid-flight, creating dual-path ambiguity.

**Tradeoffs:** AI output correctness is inherently probabilistic. Structural correctness can be tested; semantic correctness requires domain expertise.

**Recommendations:** Build a golden-cohort evaluation with human-annotated reference architecture reviews. Fill the known-empty authority projection fields (PR A0.5 is in progress). Complete the coordinator strangler to eliminate dual-path ambiguity.

**Fixability:** V1 (projection fields, strangler completion); V1.1 (human-annotated evaluation).

---

### 11. Interoperability — Score: 45 | Weight: 2 | Weighted Deficiency: 110

**Justification:** Current integrations: GitHub Actions, Azure DevOps pipeline tasks, Microsoft Teams notifications (one-way), CloudEvents webhooks, REST API. Deferred: Jira (V1.1), ServiceNow (V1.1), Confluence (V1.1), Slack (V2), MCP (V1.1). No SCIM push (only inbound). No SSO federation beyond Entra ID. The integration catalog is aspirational rather than delivered.

**Tradeoffs:** V1.1 deferrals are intentional and reasonable for a pre-revenue product.

**Recommendations:** Ensure the CloudEvents webhook documentation includes a worked end-to-end example. Publish an OpenAPI spec as a downloadable artifact. Create a Zapier/Power Automate webhook template.

**Fixability:** V1 (webhook examples, OpenAPI artifact); V1.1 (Jira, ServiceNow, Confluence).

---

### 12. Decision Velocity — Score: 52 | Weight: 2 | Weighted Deficiency: 96

**Justification:** The pending questions document tracks 40+ items across multiple resolution batches. Decision-making velocity has been high (17 decisions in one pass, 14 in another). However, several critical decisions remain parked: Marketplace go-live, Stripe live keys, privacy notice finalization, PGP key generation, and the production chaos gate. The product cannot ship commercially until these are resolved.

**Tradeoffs:** Owner-gated decisions are appropriate for legal/financial items.

**Recommendations:** Create a "V1 Launch Decision Checklist" that isolates the minimum owner decisions needed before first revenue. Prioritize the top 5 blockers.

**Fixability:** V1 (checklist creation); owner-dependent (actual decisions).

---

### 13. Security — Score: 70 | Weight: 3 | Weighted Deficiency: 90

**Justification:** Strong security posture for a pre-V1 product: fail-closed `AuthSafetyGuard` for production environments, RLS with SESSION_CONTEXT, gitleaks in CI, OWASP ZAP baseline, CodeQL, Trivy (image + Terraform config), SCIM threat model, STRIDE system threat model, prompt redaction for LLM calls, content safety guard interface, API key rotation runbook, private endpoints for SQL/Blob. However: (a) no completed third-party pen test, (b) PGP key not generated, (c) privacy notice is draft, (d) `SqlGoldenManifestRepository` is excluded from code coverage (`[ExcludeFromCodeCoverage]`), meaning the most critical data path has reduced test coverage, (e) SCIM bearer tokens are self-managed (no rotation enforcement, only reminders).

**Tradeoffs:** Security maturity is proportional to the threat profile. Pre-revenue products face lower risk but must prepare for enterprise scrutiny.

**Recommendations:** Complete the Aeronova pen test. Finalize the privacy notice. Remove the `ExcludeFromCodeCoverage` on critical repositories and add integration tests. Enforce SCIM token rotation.

**Fixability:** V1 (privacy notice, coverage); V1.1 (pen test, SCIM rotation enforcement).

---

### 14. Compliance Readiness — Score: 55 | Weight: 2 | Weighted Deficiency: 90

**Justification:** SOC 2 self-assessment exists. CAIQ Lite and SIG Core pre-fills are scaffolded. DPA template exists. Subprocessors register exists. GDPR Art. 6(1)(f) processing activity is documented (draft). However: no SOC 2 Type I attestation (deferred until ARR threshold), no completed pen test, privacy notice is draft and owner-blocked, no data residency guarantees documented. Enterprise buyers in regulated industries will find gaps.

**Tradeoffs:** SOC 2 attestation costs $30K–$80K and requires ongoing commitment. Premature for a pre-revenue startup.

**Recommendations:** Complete the privacy notice finalization. Document data residency (Azure region selection). Prepare a compliance FAQ for procurement teams.

**Fixability:** V1 (privacy notice, FAQ, data residency docs); V1.1+ (SOC 2).

---

### 15. Commercial Packaging Readiness — Score: 56 | Weight: 2 | Weighted Deficiency: 88

**Justification:** Two-layer packaging (Pilot + Operate) is designed and documented. Pricing philosophy exists with four tiers. `[RequiresCommercialTenantTier]` filter is implemented. Billing controllers for Stripe and Marketplace exist. Trial limits are documented. However: (a) pricing is internal-only (not published), (b) Stripe and Marketplace are in test mode, (c) the order form template has placeholder chargeback/refund/dunning text, (d) no customer has gone through the purchase flow, (e) the tier enforcement is configuration-driven but untested with real commercial traffic.

**Tradeoffs:** Premature public pricing can anchor expectations negatively.

**Recommendations:** Decide whether to publish pricing on the marketing site. Test the full Stripe checkout flow end-to-end with a test card. Complete the order form template.

**Fixability:** V1 (checkout testing, order form); owner-dependent (pricing publication).

---

### 16. AI/Agent Readiness — Score: 60 | Weight: 2 | Weighted Deficiency: 80

**Justification:** The agent runtime supports simulator and real execution modes. `RealAgentExecutor` calls Azure OpenAI. Content safety guard interface exists. LLM cost estimation exists. Agent output quality gates are documented. Execution traces are recorded. Chaos testing exists for LLM calls (combined failure, resilience). However: (a) real-mode execution has limited production evidence — golden cohort uses simulator only, (b) `MaxCompletionTokens` is configured but LLM response quality is unvalidated, (c) no fine-tuning or prompt optimization beyond the default prompts, (d) MCP server is deferred to V1.1, (e) the agent evaluation dataset is present but not continuously validated against real outputs.

**Tradeoffs:** AI quality is expensive to validate and highly model-dependent.

**Recommendations:** Execute the golden cohort with real LLM and publish results. Create a prompt evaluation benchmark. Document the prompt engineering approach for each agent in the pipeline.

**Fixability:** V1.1 (real-LLM golden cohort, prompt benchmark).

---

### 17. Reliability — Score: 62 | Weight: 2 | Weighted Deficiency: 76

**Justification:** The system has: transient retry with exponential backoff (commit orchestrator), circuit breaker patterns, health endpoints (live/ready), RTO/RPO targets documented, geo-failover drill runbook, Simmy chaos engineering (staging-only), graceful shutdown. However: (a) no production deployment exists to validate these under real load, (b) chaos engineering is staging-only by design, (c) the data archival coordinator is documented but production execution is unvalidated, (d) the integration event outbox processor has correlation tests but no production throughput evidence.

**Tradeoffs:** Reliability testing requires production-like environments.

**Recommendations:** Run the geo-failover drill. Execute the first Simmy chaos exercise. Document the blast radius of each failure mode.

**Fixability:** V1 (chaos exercise, failover drill).

---

### 18. Procurement Readiness — Score: 62 | Weight: 2 | Weighted Deficiency: 76

**Justification:** Procurement pack CLI command exists (`archlucid procurement-pack`). Trust Center is built. CAIQ Lite and SIG Core pre-fills exist. DPA template exists. NDA-gated pen-test summary process is documented. However: (a) the procurement pack has never been sent to a real procurement team, (b) no legal entity is named for the Marketplace listing, (c) the pen test is awarded but not completed, (d) the privacy notice is draft.

**Tradeoffs:** Procurement readiness is validated by procurement teams, not by engineers.

**Recommendations:** Send the procurement pack to a friendly procurement team for feedback. Finalize the legal entity for Marketplace. Complete the privacy notice.

**Fixability:** V1 (legal entity, privacy notice); V1.1 (pen test, procurement validation).

---

### 19. Architectural Integrity — Score: 76 | Weight: 3 | Weighted Deficiency: 72

**Justification:** The architecture is well-structured: clear bounded contexts (55+ projects), C4 documentation, ADR discipline (11+ numbered records), DI registration map, NetArchTest architectural constraint tests, dual pipeline with managed strangler migration, unit of work pattern, separation of persistence from domain logic, typed contracts. The coordinator→authority migration (ADR 0021/0029/0030) is a model strangler fig with explicit gates and sub-PR decomposition. However: (a) ~55 projects for a pre-V1 product is arguably over-decomposed, (b) the dual pipeline creates temporary architectural debt, (c) some persistence projects appear to be thinly separated (Persistence.Advisory, Persistence.Alerts, etc.), (d) the `InMemory*` repositories duplicate the interface contracts without shared enforcement.

**Tradeoffs:** Over-modularization is recoverable; under-modularization is not.

**Recommendations:** Complete the coordinator strangler (Phase 3 PR A). Consider consolidating thin persistence projects per the existing proposal (`PROJECT_CONSOLIDATION_PROPOSAL_PERSISTENCE.md`).

**Fixability:** V1 (strangler completion); V1.1 (persistence consolidation).

---

### 20. Azure Compatibility and SaaS Deployment Readiness — Score: 68 | Weight: 2 | Weighted Deficiency: 64

**Justification:** Terraform roots exist for: core infra, SQL failover, Container Apps, Key Vault, Service Bus, Storage, Entra ID, OpenAI, Monitoring, Edge (Front Door), Logic Apps, Private endpoints, OTEL collector, and a pilot profile. CD pipelines exist for staging and SaaS greenfield. Container images are buildable. **A dev environment is deployed to Azure** with API + SQL + Storage provisioned via the repo's Terraform modules, confirming the IaC applies to a real subscription. However: (a) the dev environment uses DevelopmentBypass auth (not JwtBearer/Entra ID), (b) no architecture runs have been executed in Azure yet, (c) deployments are manual (CD pipeline not connected), (d) the UI and edge services (Front Door, APIM) are not yet deployed, (e) the dev environment has not validated the full stack under real auth or real LLM mode.

**Tradeoffs:** Having a working Azure dev environment is a meaningful foundation. The gap is now "exercised" rather than "deployed."

**Recommendations:** Run a full end-to-end architecture run (simulator mode) in the dev environment. Connect the CD pipeline to auto-deploy on merge. Deploy the UI alongside the API. Switch auth to JwtBearer/Entra ID to validate the production auth path.

**Fixability:** V1 (end-to-end run, CD pipeline, auth switch).

---

### 21. Explainability — Score: 66 | Weight: 2 | Weighted Deficiency: 68

**Justification:** `RunExplanationSummary` with citation-bound rendering exists. Finding evidence chains are implemented. Decision trace entries record rationale. LLM audit service captures prompt/response pairs. Explainability trace coverage analysis exists with a faithfulness heuristic. However: (a) the explanations are LLM-generated and unvalidated for accuracy, (b) the citation-bound rendering assumes structured output from the LLM that may not always be produced, (c) there is no user-facing "why did you recommend this?" interaction, (d) the faithfulness heuristic is a code path, not a validated metric.

**Tradeoffs:** Explainability of AI outputs is an active research area.

**Recommendations:** Add a "Why this finding?" button on each finding card that shows the evidence chain. Validate the faithfulness heuristic against 20 manually-reviewed cases. Publish the explainability trace coverage as a metric in the executive summary.

**Fixability:** V1 (UI button, manual validation); V1.1 (metric publication).

---

### 22. Traceability — Score: 78 | Weight: 3 | Weighted Deficiency: 66

**Justification:** Excellent traceability infrastructure: `DecisionTrace` records, `RuleAuditTrace`, manifest hash verification, provenance source finding/graph node/applied rule links, correlation IDs throughout, requirements-to-test traceability matrix (`V1_REQUIREMENTS_TEST_TRACEABILITY.md`), and `AuthorityCommitTraceabilityRules` enforcing linkage invariants at commit time. However: (a) the traceability is structural — it proves the system processed inputs correctly, not that the inputs or outputs are meaningful, (b) the requirements-test traceability matrix is a doc artifact, not an automated gate.

**Tradeoffs:** Structural traceability is achievable; semantic traceability requires human judgment.

**Recommendations:** Automate the requirements-test traceability matrix as a CI guard. Add traceability links to the UI (finding → evidence → graph node → source).

**Fixability:** V1 (CI guard, UI links).

---

### 23. Data Consistency — Score: 68 | Weight: 2 | Weighted Deficiency: 64

**Justification:** Orphan probe data consistency enforcement exists. Transactional commits via unit of work. Manifest hash integrity. Phase-1 relational dual-write with backfill capability. Idempotent commit with unique-key violation reconciliation. However: (a) the JSON + relational dual-write creates a consistency risk during the migration, (b) the orphan probe is documented but production execution frequency is undefined, (c) the `GoldenManifestPayloadBlobEnvelope` offload path introduces a secondary storage layer with its own consistency requirements, (d) no data corruption recovery runbook exists.

**Tradeoffs:** Dual-write is a known migration cost. The alternative (big-bang migration) is riskier.

**Recommendations:** Document a data corruption recovery runbook. Define orphan probe execution frequency. Add a CI test that validates round-trip consistency between JSON and relational reads.

**Fixability:** V1 (runbook, probe frequency, round-trip test).

---

### 24. Maintainability — Score: 70 | Weight: 2 | Weighted Deficiency: 60

**Justification:** Clear coding conventions (24 Cursor rules for C# style). Primary constructors used consistently. Interfaces for all services. Dapper for data access (no ORM complexity). Test structure documented. Formatting rules documented. Method documentation expectations defined. However: (a) 55+ .NET projects create significant solution complexity, (b) the `docs/` directory has 150+ markdown files across `docs/`, `docs/library/`, `docs/archive/`, creating a large documentation surface to maintain, (c) the pending questions document is 300+ lines and growing.

**Tradeoffs:** Thoroughness of documentation has a maintenance cost.

**Recommendations:** Archive resolved pending questions more aggressively. Consider a documentation pruning pass. Consolidate related .NET projects where boundaries are artificial.

**Fixability:** V1 (documentation pruning, archive cleanup).

---

### 25. Customer Self-Sufficiency — Score: 48 | Weight: 1 | Weighted Deficiency: 52

### 26. Policy and Governance Alignment — Score: 75 | Weight: 2 | Weighted Deficiency: 50

### 27. Cognitive Load — Score: 52 | Weight: 1 | Weighted Deficiency: 48

### 28. Scalability — Score: 55 | Weight: 1 | Weighted Deficiency: 45

### 29. Performance — Score: 55 | Weight: 1 | Weighted Deficiency: 45

### 30. Cost-Effectiveness — Score: 58 | Weight: 1 | Weighted Deficiency: 42

### 31. Availability — Score: 58 | Weight: 1 | Weighted Deficiency: 42

### 32. Auditability — Score: 80 | Weight: 2 | Weighted Deficiency: 40

### 33. Stickiness — Score: 60 | Weight: 1 | Weighted Deficiency: 40

### 34. Deployability — Score: 63 | Weight: 1 | Weighted Deficiency: 37

### 35. Template and Accelerator Richness — Score: 65 | Weight: 1 | Weighted Deficiency: 35

### 36. Manageability — Score: 65 | Weight: 1 | Weighted Deficiency: 35

### 37. Change Impact Clarity — Score: 65 | Weight: 1 | Weighted Deficiency: 35

### 38. Azure Ecosystem Fit — Score: 68 | Weight: 1 | Weighted Deficiency: 32

### 39. Accessibility — Score: 68 | Weight: 1 | Weighted Deficiency: 32

### 40. Evolvability — Score: 70 | Weight: 1 | Weighted Deficiency: 30

### 41. Supportability — Score: 72 | Weight: 1 | Weighted Deficiency: 28

### 42. Extensibility — Score: 72 | Weight: 1 | Weighted Deficiency: 28

### 43. Observability — Score: 74 | Weight: 1 | Weighted Deficiency: 26

### 44. Modularity — Score: 78 | Weight: 1 | Weighted Deficiency: 22

### 45. Documentation — Score: 80 | Weight: 1 | Weighted Deficiency: 20

### 46. Testability — Score: 82 | Weight: 1 | Weighted Deficiency: 18

---

## 3. Top 10 Most Important Weaknesses

1. **No live customer or revenue** — The product is commercially inert. No buyer has completed the full journey from signup to paying subscription. Every commercial quality suffers from this single fact.

2. **Cloud trial funnel is not live** — The SaaS promise is broken at the front door. `archlucid.com/signup` is wired but not deployed. A prospective buyer cannot self-serve into a trial.

3. **AI output quality is unvalidated** — The core value proposition (AI-driven architecture review) has no empirical validation. Simulator mode produces fake results. Real-mode has not been tested at scale. No human-vs-AI accuracy benchmark exists.

4. **No completed third-party security assessment** — The Aeronova pen test is awarded but not completed. Enterprise buyers in regulated industries will require this before procurement approval.

5. **Privacy notice is draft and owner-blocked** — GDPR compliance requires a finalized privacy notice. The current draft cannot be published because it awaits legal sign-off.

6. **Commerce surfaces are test-mode only** — Stripe checkout and Azure Marketplace listing are wired but not live. No buyer can purchase the product through any self-service channel.

7. **Interoperability limited to Microsoft ecosystem** — Jira, ServiceNow, Confluence, and Slack are all deferred. Enterprise customers who use Atlassian or ServiceNow have no first-party integration path in V1.

8. **Cognitive load is high for new users** — 55+ projects, 150+ docs, 7-step wizard, 20+ CLI commands, deep domain vocabulary. The learning curve for a new operator is steep.

9. **Dev environment exists but is not exercised** — A dev environment (API + SQL + Storage) is deployed to Azure via Terraform, but no architecture runs have been executed, auth is DevelopmentBypass, the UI is not deployed, and the CD pipeline is not connected. The gap is now "exercised" rather than "deployed."

10. **Dual pipeline creates temporary architectural debt** — The coordinator→authority migration (ADR 0021/0030) is well-managed but incomplete. Until Phase 3 PRs merge, the codebase maintains two commit paths, creating cognitive load and test duplication.

---

## 4. Top 5 Monetization Blockers

1. **No live trial or signup path** — Cannot acquire customers without a way to try the product. The cloud trial funnel is the most important missing commercial surface.

2. **Commerce not live** — Stripe and Marketplace are in test mode. Even if a buyer wants to pay, there is no mechanism to accept payment.

3. **No demonstrated ROI** — No customer has validated the ROI model. The first-value report exists as code but has never been delivered to a buyer. Without ROI evidence, enterprise budgets will not be allocated.

4. **No reference customer** — No logo, no testimonial, no case study with a real company name. The PLG case study row exists but no tenant has reached `Published` status.

5. **Pricing not public** — The pricing philosophy is internal. Whether to publish prices is an unresolved owner decision. Without visible pricing, inbound leads cannot self-qualify.

---

## 5. Top 5 Enterprise Adoption Blockers

1. **No completed third-party pen test** — Security-reviewed procurement in financial services, healthcare, and government will require an independent assessment before contract execution.

2. **Privacy notice is draft** — GDPR and CCPA compliance documentation is incomplete. Legal and privacy teams in enterprise buyers will flag this immediately.

3. **No SOC 2 attestation** — Self-assessment exists but no CPA attestation. Large enterprises increasingly require SOC 2 Type I as a minimum.

4. **Limited ITSM integration** — No Jira or ServiceNow connector. Enterprise IT teams need findings to flow into their existing ticketing systems.

5. **No data residency documentation** — While Azure region selection is implicit in the Terraform config, there is no explicit data residency statement for buyers in the EU, UK, or other jurisdictions with data sovereignty requirements.

---

## 6. Top 5 Engineering Risks

1. **Partially validated infrastructure** — A dev environment (API + SQL + Storage) is deployed to Azure via Terraform, proving the IaC modules apply. However, no architecture runs have been executed in Azure, auth is DevelopmentBypass (not production-like), and the UI, edge services, CD pipeline, and real LLM path remain untested. The first full-stack deployment will still surface issues around DNS, certificates, Entra app registrations, and VNET peering.

2. **LLM dependency single point of failure** — Real-mode execution depends entirely on Azure OpenAI. Model deprecation, rate limiting, content filtering, or service outage would halt all architecture reviews. No fallback model or graceful degradation exists.

3. **SQL-heavy persistence at scale** — All data (manifests, findings, graphs, audit events, conversation threads) lives in SQL Server with JSON columns. At high tenant volume, the `JSON_VALUE` queries on `GoldenManifests.MetadataJson` and the per-insert loops in `InsertGoldenManifestDecisionsRelationalAsync` (no batch insert) will become bottlenecks.

4. **Dual pipeline convergence risk** — The coordinator→authority strangler has a 2026-05-15 deadline. If the Phase 3 PRs (A0–A4) do not merge cleanly, the codebase will carry two commit paths past the deadline, creating maintenance burden and bug risk.

5. **Critical SQL repository excluded from code coverage** — `SqlGoldenManifestRepository` has `[ExcludeFromCodeCoverage]` with the justification "requires live SQL Server." This is the most critical data path in the system. The justification is valid for unit tests but does not excuse the absence of integration test coverage.

---

## 7. Most Important Truth

ArchLucid is an engineering-rich product that has been built with genuine craftsmanship and discipline, but it has never been used by a customer, never been deployed to production, and never generated revenue. The gap between "built" and "sold" is the single largest risk. Every engineering improvement has diminishing returns until the product is in front of a paying customer. The next dollar of effort should go into making the product triable, not more correct.

---

## 8. Top Improvement Opportunities

### Improvement 1: Ship a Hosted Demo Environment with Pre-Seeded Runs

**Why it matters:** The single highest-leverage action to improve Time-to-Value, Adoption Friction, and Marketability simultaneously. A buyer must be able to see the product working in under 5 minutes without installing anything.

**Expected impact:** Directly improves Time-to-Value (+15–20 pts), Adoption Friction (+10–15 pts), Marketability (+8–10 pts). Weighted readiness impact: +1.5–2.5%.

**Affected qualities:** Time-to-Value, Adoption Friction, Marketability, Executive Value Visibility, Trustworthiness.

**Status:** **Shipped in repo (2026-04-26).** A separate `DemoSeedOnStartupHostedService` is **not** required — `ArchLucidPersistenceStartup.RunSchemaBootstrapMigrationsAndOptionalDemoSeed` already runs `IDemoSeedService` when `Demo:Enabled` and `Demo:SeedOnStartup` are true. The seed creates **two** committed Contoso runs (not three). Owner decisions: public demo at `https://demo.archlucid.net`, on-demand reset only, allow visitors to create new runs (no read-only POST 403 feature).

**Delivered:**

| Artifact | Role |
|----------|------|
| `docker-compose.demo.yml` | `include` of root compose; adds `demo-hosted` profile (alongside `full-stack`), sets `ArchLucid__AgentExecution__Mode=Simulator`, passes `NEXT_PUBLIC_DEMO_URL` into the UI build |
| `docker-compose.yml` | UI build `args` optional `NEXT_PUBLIC_DEMO_URL` (empty by default) |
| `archlucid-ui/Dockerfile` | `ARG` / `ENV` for `NEXT_PUBLIC_DEMO_URL` at build time |
| `archlucid-ui/.../get-started/page.tsx` | CTA "Try the live demo" when `NEXT_PUBLIC_DEMO_URL` is a valid `https://` URL |
| `get-started.test.tsx` | Sequential tests for CTA on/off |
| `docs/runbooks/DEMO_HOSTED_DEPLOYMENT.md` | Local command, seed semantics, Azure high level, on-demand reset, cost order-of-magnitude |

**How to run locally:**

```bash
docker compose -f docker-compose.yml -f docker-compose.demo.yml --profile demo-hosted up -d --build
```

**Remaining (operator / Azure):** Point DNS and TLS for `demo.archlucid.net` at the hosted API/UI, build the UI with `NEXT_PUBLIC_DEMO_URL` for the marketing site you ship to production (if marketing is a separate app, set the build arg there to the demo operator URL).

---

### Improvement 2: DEFERRED — Make Cloud Trial Signup Live

**Title:** DEFERRED — Activate the cloud trial signup funnel at `archlucid.com/signup`

**Reason deferred:** Requires owner decisions on: (a) the DNS domain (`archlucid.com` acquisition status), (b) the production Azure subscription and Entra ID app registration, (c) the Stripe test-to-live cutover timing, (d) the landing page URL for Marketplace.

**Input needed:** Confirmation that `archlucid.com` domain is acquired and DNS is configured; the Azure subscription ID for production; the Entra ID app registration client ID; the Stripe mode (test vs live) for initial launch.

---

### Improvement 3: Capture and Publish Real Screenshots for Marketing Pages

**Why it matters:** The marketing pages (`/get-started`, `/see-it`, `/why`) have placeholder screenshot slots. Real screenshots dramatically improve credibility and reduce friction for evaluators.

**Expected impact:** Directly improves Marketability (+5–8 pts), Time-to-Value (+3–5 pts), Executive Value Visibility (+3–5 pts). Weighted readiness impact: +0.5–1.0%.

**Affected qualities:** Marketability, Time-to-Value, Executive Value Visibility, Trustworthiness.

**Status:** Actionable now.

**Cursor prompt:**

```
Capture real screenshots from the ArchLucid demo environment and replace placeholder images in the marketing pages.

Scope:
1. Using the existing Contoso Retail demo seed data, capture the following screenshots via Playwright:
   - New Run wizard (step 1 — starting point)
   - Runs list showing 3 committed runs
   - Run detail with pipeline timeline
   - Committed manifest summary with findings
   - Artifact review (in-shell preview)
2. Save screenshots to `archlucid-ui/public/images/screenshots/` with names matching the placeholder conventions: `step-1-placeholder.png` through `step-5-placeholder.png`.
3. Create a Playwright script at `archlucid-ui/e2e/capture-screenshots.spec.ts` that:
   - Starts against a running demo API with seeded data
   - Navigates to each page and waits for content to load
   - Captures full-page screenshots at 1280x800
   - Saves to the screenshots directory
4. Update `archlucid-ui/src/app/(marketing)/get-started/page.tsx` to reference the real screenshots instead of placeholder images.
5. Update `docs/BUYER_FIRST_30_MINUTES.md` to reference the same screenshots.

Constraints:
- Screenshots must use the dark mode theme (consistent brand)
- Screenshots must not contain real tenant IDs or PII — use the Contoso Retail demo data
- Do NOT modify any non-marketing pages
- Do NOT change the screenshot capture to depend on external services

Acceptance criteria:
- 5 screenshots exist in `archlucid-ui/public/images/screenshots/`
- `/get-started` page renders real screenshots instead of placeholders
- `BUYER_FIRST_30_MINUTES.md` references the screenshots
- The Playwright capture script runs successfully against a demo API
```

---

### Improvement 4: Complete the Authority Projection Known-Empty Fields (PR A0.5)

**Why it matters:** The authority pipeline produces manifests with empty Services, Datastores, and Relationships fields. This means the golden manifest — the primary deliverable — is structurally incomplete. Buyers reviewing a manifest will see missing sections.

**Expected impact:** Directly improves Correctness (+5–8 pts), Trustworthiness (+3–5 pts), Data Consistency (+2–3 pts). Weighted readiness impact: +0.4–0.7%.

**Affected qualities:** Correctness, Trustworthiness, Data Consistency, Architectural Integrity.

**Status:** Actionable now (owner decisions 35a.2 and 35f already resolved).

**Cursor prompt:**

```
Implement PR A0.5: populate authority manifest Services and Datastores from graph node properties.

Context: Per ADR 0030 sub-decisions 35a.2 and 35f, the authority pipeline must populate `GoldenManifest.Services` and `GoldenManifest.Datastores` from `GraphNode.Properties` keys `serviceType`, `runtimePlatform`, and `datastoreType`. The `AuthorityCommitProjectionBuilder` maps these onto the coordinator-shaped `Contracts.Manifest.GoldenManifest`.

Scope:
1. In `ArchLucid.Decisioning/Manifest/Mapping/DefaultGoldenManifestBuilder.cs`:
   - After the existing manifest population, iterate `TopologyResource` nodes in the graph snapshot
   - For nodes with `Properties["serviceType"]` set, create `ManifestService` entries with `ServiceType` and `RuntimePlatform` populated from node properties
   - For nodes with `Properties["datastoreType"]` set, create `ManifestDatastore` entries
   - Add these to `GoldenManifest.Services` and `GoldenManifest.Datastores`

2. In `ArchLucid.Application/Architecture/AuthorityCommitProjectionBuilder.cs`:
   - Map `Decisioning.Models.GoldenManifest.Services` → `Contracts.Manifest.GoldenManifest.Services`
   - Map `Decisioning.Models.GoldenManifest.Datastores` → `Contracts.Manifest.GoldenManifest.Datastores`

3. Update `scripts/ci/assert_authority_projection_known_empty.py` to remove `Services` and `Datastores` from the known-empty allow list.

4. Add tests in `ArchLucid.Decisioning.Tests/Manifest/Mapping/DefaultGoldenManifestBuilderServicesTests.cs`:
   - Test that graph nodes with serviceType properties produce ManifestService entries
   - Test that graph nodes with datastoreType properties produce ManifestDatastore entries
   - Test that nodes without these properties produce no entries

Constraints:
- Do NOT modify the coordinator pipeline (these fields are coordinator-only in the old path)
- Do NOT change the `InsertRelationalPhase1Async` SQL — Services/Datastores are JSON-only for now
- Use the existing `GraphNode.Properties` dictionary — do not add new node types
- Follow the existing pattern in `DefaultGoldenManifestBuilder` for null safety

Acceptance criteria:
- `scripts/ci/assert_authority_projection_known_empty.py` passes with Services and Datastores removed from the allow list
- New tests pass
- A demo commit via the authority pipeline produces a manifest with non-empty Services (when the graph has service-type nodes)
```

---

### Improvement 5: Ship the Approved Opt-In Tour

**Why it matters:** The operator UI tour was approved (all 5 steps, batch PR) but has not been shipped. First-time users have no guided introduction to the product. This directly impacts usability, adoption friction, and time-to-value.

**Expected impact:** Directly improves Usability (+5–8 pts), Adoption Friction (+3–5 pts), Time-to-Value (+2–3 pts). Weighted readiness impact: +0.4–0.8%.

**Affected qualities:** Usability, Adoption Friction, Time-to-Value, Customer Self-Sufficiency.

**Status:** Actionable now (tour copy approved per PENDING_QUESTIONS.md Improvement 5).

**Cursor prompt:**

```
Ship the approved opt-in tour by removing all TourStepPendingApproval wrappers.

Context: Per PENDING_QUESTIONS.md Improvement 5, the owner approved all five tour step copies on 2026-04-24. The decision was to batch all five in one PR (Option B).

Scope:
1. In `archlucid-ui/src/components/tour/OptInTour.tsx`:
   - Find all `<TourStepPendingApproval>` wrapper components
   - Replace each with a plain `<>` fragment (React fragment), keeping the inner content
   - Ensure each step body contains the approved copy

2. Update `archlucid-ui/src/components/tour/OptInTour.test.tsx`:
   - Remove any test assertions that check for `TourStepPendingApproval` rendering
   - Add assertions that verify each tour step renders its approved content
   - Test that the tour starts on first visit (no localStorage flag)
   - Test that the tour does not restart after dismissal (localStorage flag set)

3. If `TourStepPendingApproval` is now unused, delete the component file.

Constraints:
- Do NOT change the tour step content — use exactly the approved copy
- Do NOT change the tour trigger logic (opt-in, localStorage-based)
- Do NOT add new tour steps beyond the approved five
- Preserve the existing `data-testid` attributes on tour elements

Acceptance criteria:
- No `TourStepPendingApproval` references remain in the codebase
- All five tour steps render with approved content
- Tour starts automatically on first visit
- Tour does not restart after dismissal
- All existing OptInTour tests pass
- npm test passes
```

---

### Improvement 6: Finalize the Privacy Notice

**Why it matters:** The privacy notice is currently draft and marked as "owner-blocked" on legal sign-off. However, the draft content exists and the processing activities are documented. The scaffolding can be finalized with clear "pending legal review" markers for the specific clauses that require legal input, while making the rest of the notice live.

**Expected impact:** Directly improves Compliance Readiness (+8–10 pts), Procurement Readiness (+3–5 pts), Trustworthiness (+2–3 pts). Weighted readiness impact: +0.3–0.6%.

**Affected qualities:** Compliance Readiness, Procurement Readiness, Trustworthiness, Enterprise adoption.

**Status:** Actionable now (the executable portion: clean up the draft, add a "pending legal review" section header for the specific owner-blocked clauses, and make the non-blocked sections visible on the marketing site).

**Cursor prompt:**

```
Clean up the privacy notice draft and make non-blocked sections visible on the marketing site.

Scope:
1. In `docs/security/PRIVACY_NOTE.md`:
   - Move all completed sections (data collected, processing purposes, retention periods, data subject rights, subprocessors) out of "DRAFT" status
   - Add a clear `## Sections Pending Legal Review` header at the top listing ONLY the specific clauses that require legal sign-off (legitimate interest balancing test, per-tenant emission consent language)
   - Add a `Last updated: 2026-04-26` header
   - Ensure GDPR Art. 6(1)(f) processing activity description is complete per the existing §3.A content

2. In `archlucid-ui/src/app/(marketing)/privacy/page.tsx`:
   - Render the non-blocked sections of the privacy notice
   - Show a clearly-marked "This section is under legal review" banner for the blocked sections
   - Add a "Contact privacy@archlucid.com for questions" footer (or security@ per existing convention)

3. Update `docs/go-to-market/TRUST_CENTER.md` to link to the privacy notice page.

Constraints:
- Do NOT fabricate legal language or make legal claims
- Do NOT remove the "pending legal review" markers on blocked sections
- Keep the existing GDPR Art. 6(1)(f) analysis exactly as drafted
- Do NOT add cookie consent or tracking — the current system does not use cookies beyond session

Acceptance criteria:
- Privacy notice page renders at `/privacy` with completed sections visible
- Blocked sections show "under legal review" banner
- Trust Center links to the privacy page
- No legal claims are made for unreviewed sections
```

---

### Improvement 7: Add Batch SQL Inserts for Golden Manifest Relational Phase 1

**Why it matters:** The `SqlGoldenManifestRepository.InsertRelationalPhase1Async` method inserts assumptions, warnings, decisions, evidence links, and node links one row at a time in a loop. For a manifest with 50 assumptions + 30 decisions each with 5 evidence links, this is 230+ individual SQL roundtrips inside a transaction. This is a scaling bottleneck and a reliability risk (long transaction holding locks).

**Expected impact:** Directly improves Data Consistency (+8–10 pts), Reliability (+3–5 pts), Performance (+5–8 pts), Scalability (+5–8 pts). Weighted readiness impact: +0.3–0.5%.

**Affected qualities:** Data Consistency, Reliability, Performance, Scalability, Cost-Effectiveness.

**Status:** Actionable now.

**Cursor prompt:**

```
Refactor SqlGoldenManifestRepository to use batch SQL inserts instead of per-row loops.

Scope:
1. In `ArchLucid.Persistence/Repositories/SqlGoldenManifestRepository.cs`:
   - Replace the per-row INSERT loops in `InsertGoldenManifestAssumptionsRelationalAsync`, `InsertGoldenManifestWarningsRelationalAsync`, `InsertGoldenManifestProvSourceFindingsRelationalAsync`, `InsertGoldenManifestProvSourceGraphNodesRelationalAsync`, `InsertGoldenManifestProvAppliedRulesRelationalAsync`, and `InsertGoldenManifestDecisionsRelationalAsync` with batched inserts
   - Use Dapper's multi-row execute pattern: build a list of parameter objects and pass them to a single `ExecuteAsync` call with the INSERT statement
   - For `InsertGoldenManifestDecisionsRelationalAsync`, keep the decision INSERT + evidence links + node links as separate batches (three ExecuteAsync calls per decision batch, not per-row)

2. Add a private helper method `BatchExecuteAsync<T>(IDbConnection connection, string sql, IReadOnlyList<T> items, IDbTransaction? transaction, CancellationToken ct)` that:
   - Chunks the items into batches of 100 (SQL Server parameter limit safety)
   - Calls `ExecuteAsync` once per chunk

3. Apply the same batch pattern to `BackfillPhase1RelationalSlicesAsync`.

4. Add a test in `ArchLucid.Persistence.Tests` that verifies batch inserts produce the same relational rows as the old per-row approach (use an in-memory or test SQL database).

Constraints:
- Do NOT change the SQL INSERT statements themselves — only change the C# execution pattern
- Do NOT change the transaction boundaries — all inserts must still be within the same transaction
- Do NOT remove the `[ExcludeFromCodeCoverage]` attribute (that is a separate improvement)
- Keep the batch size configurable via a const (default 100)
- Maintain backwards compatibility with the existing `BackfillPhase1RelationalSlicesAsync` method

Acceptance criteria:
- No per-row INSERT loops remain in the relational phase-1 methods
- All existing tests pass
- A manifest with 100 assumptions inserts in 1 SQL roundtrip instead of 100
- The batch helper handles empty lists gracefully (no SQL call for 0 items)
```

---

### Improvement 8: Create a V1 Launch Decision Checklist

**Why it matters:** The pending questions document is 300+ lines with 40+ items across multiple resolution batches. There is no single, focused view of "what must be decided before first revenue." Creating this checklist improves Decision Velocity and makes the path to launch concrete.

**Expected impact:** Directly improves Decision Velocity (+10–15 pts), Commercial Packaging Readiness (+5–8 pts), Marketability (+3–5 pts). Weighted readiness impact: +0.3–0.5%.

**Affected qualities:** Decision Velocity, Commercial Packaging Readiness, Marketability, Procurement Readiness.

**Status:** Actionable now.

**Cursor prompt:**

```
Create a V1 Launch Decision Checklist that isolates the minimum owner decisions needed before first revenue.

Scope:
1. Create `docs/V1_LAUNCH_DECISION_CHECKLIST.md` with:
   - A title: "V1 Launch Decision Checklist — Minimum Decisions Before First Revenue"
   - A table with columns: Decision | Status | Owner | Blocking What | PENDING_QUESTIONS.md Reference
   - Populate from PENDING_QUESTIONS.md, including ONLY items that block revenue:
     - Marketplace publication go-live (item 8)
     - Stripe production go-live (item 9)
     - Privacy notice finalization (item 9a / Improvement 9)
     - Public price list publication (item 13)
     - Legal entity for Marketplace listing (item 8a)
     - First-paying-tenant graduation owner (item 19)
     - Domain acquisition (`archlucid.com`) status
   - A "Not Blocking V1 Launch" section listing items explicitly deferred to V1.1+ (pen test, PGP key, SOC 2, ITSM connectors, MCP)

2. Add a link to this checklist from:
   - `docs/PENDING_QUESTIONS.md` (top of file, after the scope line)
   - `docs/library/V1_SCOPE.md` §5 (Minimum release criteria)
   - `docs/library/V1_RELEASE_CHECKLIST.md`

3. Add a CI guard at `scripts/ci/assert_v1_launch_checklist_items.py` that:
   - Parses the checklist markdown table
   - Warns (not fails) if any row has Status = "Open" and the current date is past 2026-05-01
   - This is a visibility guard, not a blocking gate

Constraints:
- Do NOT duplicate the full PENDING_QUESTIONS.md content — link to it
- Do NOT add items that are V1.1+ (they are explicitly out of scope)
- Keep the checklist under 50 lines
- Do NOT modify PENDING_QUESTIONS.md content beyond adding the link

Acceptance criteria:
- `docs/V1_LAUNCH_DECISION_CHECKLIST.md` exists with 7-8 items
- Links added to PENDING_QUESTIONS.md, V1_SCOPE.md, and V1_RELEASE_CHECKLIST.md
- CI guard script runs without error
- Checklist is under 50 lines
```

---

### Improvement 9: Exercise the Existing Azure Dev Environment End-to-End

**Why it matters:** A dev environment (API + SQL + Storage) is already deployed to Azure via Terraform, but it has never executed an architecture run. Running a full end-to-end cycle in Azure — even in simulator mode — validates the entire stack beyond `terraform apply` and removes the "never used in anger" risk. This also establishes the baseline for CD pipeline connection and real-LLM testing.

**Expected impact:** Directly improves Azure Compatibility and SaaS Deployment Readiness (+5–8 pts), Reliability (+3–5 pts), Deployability (+3–5 pts). Weighted readiness impact: +0.3–0.5%.

**Affected qualities:** Azure Compatibility and SaaS Deployment Readiness, Reliability, Deployability, Availability.

**Status:** Actionable now (dev environment exists).

**Cursor prompt:**

```
Exercise the existing Azure dev environment by running a full architecture lifecycle end-to-end.

Context: A dev environment with API + SQL + Storage is already deployed to Azure via the repo's Terraform modules. Auth is DevelopmentBypass. No runs have been executed yet.

Scope:
1. Create a runbook at `docs/runbooks/DEV_ENVIRONMENT_EXERCISE.md` that documents:
   - The dev environment's Azure resource group and API endpoint URL
   - Step-by-step instructions to execute a full lifecycle:
     a. Verify health: `curl <api-url>/health/ready`
     b. Create a run: `curl -X POST <api-url>/v1/architecture/request -H "Content-Type: application/json" -d @examples/sample-request.json`
     c. Poll status: `curl <api-url>/v1/architecture/run/{runId}`
     d. Commit: `curl -X POST <api-url>/v1/architecture/run/{runId}/commit`
     e. Retrieve manifest: `curl <api-url>/v1/architecture/manifest/{version}`
   - Expected outputs at each step
   - Troubleshooting for common failures (SQL connectivity, DbUp migration status, missing seed data)

2. Create a sample request payload at `examples/sample-request.json` using the Contoso Retail scenario (matching the simulator's FakeScenarioFactory).

3. Add a script at `scripts/dev-env-smoke.ps1` (PowerShell) that:
   - Accepts a `--api-url` parameter (default: the dev environment URL)
   - Executes steps a–e above
   - Prints PASS/FAIL for each step with the response status code
   - Returns exit code 0 if all steps pass, 1 otherwise

4. Document the next steps in the runbook:
   - Switch auth from DevelopmentBypass to ApiKey (intermediate) then JwtBearer (target)
   - Connect the cd-staging-on-merge.yml pipeline
   - Deploy the UI alongside the API
   - Test with real Azure OpenAI (--real mode)

Constraints:
- Do NOT create or modify Azure resources — the environment already exists
- Do NOT change the dev environment's auth mode (that is a separate follow-on)
- Do NOT add the dev environment URL to any committed file — use a placeholder like `<DEV_API_URL>` in docs and `--api-url` parameter in scripts
- The sample request must work with Simulator execution mode

Acceptance criteria:
- `scripts/dev-env-smoke.ps1 --api-url <real-url>` passes all 5 steps against the live dev environment
- Runbook documents the full lifecycle with expected outputs
- Sample request JSON exists at examples/sample-request.json
- Next steps section documents the path to production-like auth and CD
```

---

### Improvement 10: DEFERRED — Complete the Aeronova Pen Test

**Title:** DEFERRED — Complete the awarded third-party penetration test

**Reason deferred:** The pen test execution is an external vendor engagement (Aeronova Red Team LLC). The SoW is awarded but kickoff is scheduled for 2026-05-06. The assessment, reporting, and remediation are vendor-dependent and owner-managed.

**Input needed:** Confirmation of the kickoff date, the scope boundaries (API only vs API + UI + infra), and the remediation SLA for findings.

---

## 9. Deferred Scope Uncertainty

Items explicitly deferred to V1.1 or V2 that were verified in the codebase and documentation:

- **Jira connector** — V1.1, confirmed in `V1_DEFERRED.md` §6 and `PENDING_QUESTIONS.md` item 11
- **ServiceNow connector** — V1.1, confirmed in `V1_DEFERRED.md` §6
- **Confluence connector** — V1.1, confirmed in `PENDING_QUESTIONS.md` Improvement 3 (2026-04-24)
- **Slack connector** — V2, confirmed in `V1_DEFERRED.md` §6a
- **MCP server** — V1.1, confirmed in `V1_SCOPE.md` §3 and `V1_DEFERRED.md` §6d
- **SOC 2 Type I attestation** — Deferred until ARR threshold, confirmed in `PENDING_QUESTIONS.md` item 6
- **PGP key generation** — V1.1, confirmed in `PENDING_QUESTIONS.md` item 3
- **Pen test completion** — V1.1, confirmed in `V1_DEFERRED.md`
- **Commerce un-hold (Stripe live + Marketplace publication)** — V1.1, confirmed in `V1_SCOPE.md` §3

No deferred items were referenced that could not be located in the source material.

---

## 10. Pending Questions for Later

### Improvement 1 (Hosted Demo)
- What is the target URL for the hosted demo? (e.g., `demo.archlucid.com` or `archlucid.com/demo`)
- Should the demo reset nightly or on-demand?
- Should visitors be able to create new runs in the demo, or only view pre-seeded data?

### Improvement 2 (Cloud Trial — DEFERRED)
- Has the `archlucid.com` domain been acquired?
- What is the target Azure subscription for production?
- What is the Entra ID app registration client ID?

### Improvement 4 (Authority Projection Fields)
- Are there graph nodes in the current simulator seed data that have `serviceType` and `datastoreType` properties, or do the seed data generators need to be updated first?

### Improvement 6 (Privacy Notice)
- Is `privacy@archlucid.com` a valid alias, or should the privacy contact use `security@archlucid.com`?
- Does the owner want to publish the privacy notice at `/privacy` before legal review of the blocked sections, or wait for full sign-off?

### Improvement 9 (Staging Deployment — DEFERRED)
- What is the monthly budget ceiling for staging?
- Should staging use the same Entra ID tenant as production (with different app registrations) or a separate tenant?

### General
- What is the target date for first revenue?
- Is there a named design partner actively evaluating the product?
- Has the brand decision ("AI Architecture Review Board") been finalized with trademark counsel?
