> **Scope:** Independent first-principles quality assessment of ArchLucid as it stands on **2026-04-25**. Scored from the repository's current state without reference to any prior assessments, scores, or conclusions.

# ArchLucid Assessment – Weighted Readiness 71.35%

**Date:** 2026-04-25
**Assessor:** Independent first-principles review (Opus 4.6)
**Basis:** Repository contents at commit time — 53 C# projects (~3,337 source files), 20 test projects, Next.js operator UI, 110 Terraform files, 176+ library docs, 24 CI workflows, and go-to-market artifacts.

---

## 1. Executive Summary

### Overall Readiness

ArchLucid is a **credible V1 product** that delivers a working end-to-end architecture workflow: request → execute → commit → manifest → review. The codebase demonstrates genuine engineering discipline — tiered CI, coverage gates, OpenAPI contract drift detection, k6 performance baselines, accessibility scanning, and durable audit infrastructure. At 71.35% weighted readiness, the product is **viable for sales-led pilots** but faces real friction in self-serve adoption, proof-of-ROI automation, and commercial packaging that will constrain the transition from founder-led sales to scalable revenue.

### Commercial Picture

The product has clear differentiation in a market gap (AI architecture + enterprise governance) and a well-articulated pricing philosophy. However, **zero published reference customers**, **no live Stripe checkout**, and **no Azure Marketplace listing** collectively mean every sale requires founder handholding. The self-serve trial funnel is technically wired but not yet live. Proof-of-ROI surfaces exist but require operator effort to populate baselines. Until the commerce un-hold (V1.1) and at least one published reference customer, revenue velocity will be slow.

### Enterprise Picture

Enterprise readiness is **stronger than typical for a V1**. The trust center, DPA template, CAIQ Lite pre-fill, SIG Core pre-fill, SOC 2 self-assessment, RLS, RBAC, audit log (78 event types), governance workflows, and SCIM provisioning address real procurement checklist items. The gaps are in third-party attestation (no SOC 2 Type II; pen test deferred to V1.1), ITSM connectors (Jira/ServiceNow deferred to V1.1), and the four overlapping onboarding routes that create confusion for new operators.

### Engineering Picture

The architecture is well-decomposed (Coordinator, Decisioning, Persistence, Application, Contracts, KnowledgeGraph, Provenance, etc.) with clear boundaries. Test infrastructure is mature: tiered CI, coverage ratchets (79% line / 63% branch targets), Stryker mutation testing, k6 load tests, Playwright E2E, axe accessibility scans, Schemathesis API fuzzing, and Simmy chaos testing. The main engineering risks are: code coverage currently below stated targets (~73% line, ~59% branch locally), the Persistence layer at only ~40% coverage, and the UI inconsistency where Search/Ask pages bypass the design system.

---

## 2. Weighted Quality Assessment

**Total weight:** 100
**Weighted score:** 71.35 / 100

Qualities are ordered from **most urgent** (highest weighted deficiency) to **least urgent**.

---

### Marketability — Score: 62 · Weight: 8 · Weighted Deficiency: 3.04

**Justification:** The product has a clear value proposition ("shorten path from architecture request to reviewable package") and a well-defined competitive landscape doc. However: zero published reference customers (all rows are Placeholder/Customer review), no live commerce (Stripe test-mode only), no Azure Marketplace listing (V1.1), and no completed pen-test summary to share with procurement. The marketing site exists but the trial funnel is not live. The `/why` comparison page and competitive contrast doc are strong assets, but without a single provable customer win, every sales conversation starts from scratch.

**Tradeoffs:** Investing in reference customer acquisition vs. feature depth. The 15% reference discount is already budgeted but has no customer to apply it to.

**Recommendations:**
- Close at least one design-partner pilot to `Published` status
- Complete commerce un-hold (V1.1) to enable self-serve revenue
- Publish redacted pen-test summary after Aeronova engagement completes
- Fixable in: V1.1 (commerce), ongoing (reference customers — requires business development, not code)

---

### Adoption Friction — Score: 60 · Weight: 6 · Weighted Deficiency: 2.40

**Justification:** Four overlapping onboarding routes (`/onboarding`, `/onboarding/start`, `/onboard`, `/getting-started`) with three distinct wizard implementations create genuine confusion. `NEXT_PUBLIC_DOCS_BASE_URL` is commonly unset, making help links non-functional. The CLI requires .NET 10 SDK. The first-run experience depends on Docker for SQL Server. Search and Ask pages use raw HTML elements instead of the design system, creating visual inconsistency. The product has strong progressive disclosure (three-tier nav) but the initial landing is cluttered.

**Tradeoffs:** Consolidating onboarding routes risks breaking bookmarked URLs; adding redirects is the safe path.

**Recommendations:**
- Consolidate to a single canonical onboarding route with redirects from legacy paths
- Set `NEXT_PUBLIC_DOCS_BASE_URL` in default configuration
- Refactor Search/Ask pages to use design system components
- Fixable in: V1

---

### Proof-of-ROI Readiness — Score: 58 · Weight: 5 · Weighted Deficiency: 2.10

**Justification:** The PILOT_ROI_MODEL.md is thorough and the value-report DOCX generation exists. However, ROI proof still requires manual baseline capture by the operator. The automated review-cycle delta (captured optionally at signup) is a good start but the worked example ROI doc uses synthetic/modeled data. No real customer telemetry validates the claimed 40+ hour savings. The first-value report and sponsor PDF are technically functional but untested with real sponsors.

**Tradeoffs:** Automating ROI capture too aggressively may produce misleading numbers; the current manual-baseline approach is honest but creates adoption friction.

**Recommendations:**
- Add automatic time-to-commit measurement (already partially captured in telemetry) to the value report
- Create a real worked example from a design partner pilot
- Fixable in: V1 (telemetry wiring), V1.1 (real customer data)

---

### Time-to-Value — Score: 63 · Weight: 7 · Weighted Deficiency: 2.59

**Justification:** The `archlucid try` one-command experience is excellent for contributors, and the Docker demo path works. But for a **buyer**, the hosted trial funnel is not live. A buyer evaluating the product must either: (a) wait for founder-led demo, or (b) install .NET 10 SDK + Docker locally. The demo preview page exists but the actual self-serve signup flow is test-mode. Once past setup, time-to-first-commit is genuinely fast (request → fake results → commit in one `--quick` command), but the wall between "interested prospect" and "seeing first value" is too high without the hosted SaaS funnel.

**Tradeoffs:** Rushing the hosted funnel live without the pen test and commerce un-hold creates security/commercial risk vs. the cost of slow prospect conversion.

**Recommendations:**
- Prioritize hosted SaaS funnel activation (staging.archlucid.com is already configured)
- Ensure demo preview page works without authentication for top-of-funnel
- Fixable in: V1.1 (commerce un-hold), partially V1 (demo preview hardening)

---

### Executive Value Visibility — Score: 65 · Weight: 4 · Weighted Deficiency: 1.40

**Justification:** The EXECUTIVE_SPONSOR_BRIEF is well-written and honest about what not to over-claim. The sponsor PDF, first-value report, and "Email this run to your sponsor" banner are real product features. The value-report DOCX with ROI estimates exists. However, there is no dashboard or automated executive summary that a sponsor can see without operator action. The value story requires the operator to generate and forward artifacts manually.

**Tradeoffs:** Building a sponsor-facing portal is a significant investment; the current operator-mediated model is appropriate for V1.

**Recommendations:**
- Consider a lightweight sponsor-view URL (read-only, token-gated) for V1.1
- Ensure the first-value PDF renders correctly and is visually polished
- Fixable in: V1 (PDF polish), V1.1 (sponsor portal)

---

### Differentiability — Score: 72 · Weight: 4 · Weighted Deficiency: 1.12

**Justification:** ArchLucid genuinely occupies a market gap: no existing tool combines AI agent orchestration with enterprise governance, auditability, and provenance for architecture decisions. The competitive landscape doc is thorough and grounded. The finding inspector, explainability traces, citation chips, and faithfulness metrics are real differentiators. The knowledge graph, provenance tracing, and governance workflows go beyond what any incumbent offers in this space.

**Tradeoffs:** The differentiation story is strong on paper but unproven with buyers. Risk of "too novel" — buyers may not have a budget category for this.

**Recommendations:**
- Lead with the competitive comparison in sales conversations
- Frame against the manual architecture review cost (40+ hours), not against EAM tools
- Fixable in: V1 (positioning refinement)

---

### Usability — Score: 68 · Weight: 3 · Weighted Deficiency: 0.96

**Justification:** Strong shell architecture (sticky header, breadcrumbs, skip-to-main, collapsible sidebar, command palette, keyboard shortcuts, dark mode, 21 loading states). But: Search/Ask pages use raw HTML instead of design system, four overlapping onboarding routes, no inline form validation, no undo/redo affordance, and `NEXT_PUBLIC_DOCS_BASE_URL` commonly unset. The progressive disclosure model is well-implemented but the inconsistencies in the two most cognitively demanding pages (Search and Ask) undermine trust.

**Tradeoffs:** Refactoring Search/Ask is moderate effort but high visual impact.

**Recommendations:**
- Refactor Search and Ask pages to use design system components (Input, Button, Textarea, Card)
- Consolidate onboarding routes
- Add inline validation to the new-run wizard
- Fixable in: V1

---

### Traceability — Score: 72 · Weight: 3 · Weighted Deficiency: 0.84

**Justification:** Strong: 78 typed audit events, append-only SQL store, CSV export, finding inspector with persisted artifacts, explainability traces, citation chips, decision traces, governance workflow audit trail. The V1_REQUIREMENTS_TEST_TRACEABILITY.md maps scope items to tests. Correlation IDs flow through the pipeline. However, some mutating flows still lack durable audit events (documented as known gaps in AUDIT_COVERAGE_MATRIX.md), and the audit search cursor has a known timestamp tie-breaking limitation.

**Tradeoffs:** Closing every audit gap increases write amplification; the current known-gap documentation is honest.

**Recommendations:**
- Close remaining audit gaps documented in AUDIT_COVERAGE_MATRIX.md for authority-critical paths
- Fixable in: V1

---

### Workflow Embeddedness — Score: 60 · Weight: 3 · Weighted Deficiency: 1.20

**Justification:** The product supports CloudEvents webhooks, REST API, CLI, and Azure DevOps Work Items as integration points. However: no Jira connector (V1.1), no ServiceNow connector (V1.1), no Confluence connector (V1.1), no Slack connector (V2), no MCP server (V1.1), no VS Code extension. The integration catalog shows mostly "Planned" or "V1.1" status. Microsoft Teams notifications are the only first-party chat-ops surface. For enterprises that live in Jira/ServiceNow/Slack, adoption requires custom webhook consumers — significant friction.

**Tradeoffs:** Building first-party connectors is expensive; the webhook + REST approach is the honest V1 position. Properly deferred to V1.1.

**Recommendations:**
- Prioritize Jira connector in V1.1 (highest enterprise demand)
- Document clear webhook-to-Jira recipes for V1 customers
- Fixable in: V1.1 (connectors), V1 (recipes/docs)

---

### Trustworthiness — Score: 72 · Weight: 3 · Weighted Deficiency: 0.84

**Justification:** The trust center is well-structured with honest status labels (self-asserted vs. third-party confirmed). SOC 2 self-assessment exists with a documented roadmap. DPA template, subprocessors register, CAIQ Lite, and SIG Core pre-fills are real procurement assets. The "citations vs. proof" section in the sponsor brief is commendably honest about LLM limitations. RLS, RBAC, and tenant isolation are documented. The pen test is explicitly deferred to V1.1 and not scored here. The remaining gap is the absence of any third-party attestation (SOC 2 Type II is on the roadmap but not yet engaged).

**Tradeoffs:** SOC 2 Type II takes 6-12 months; the self-assessment is the right interim posture.

**Recommendations:**
- Begin SOC 2 Type II preparation with a CPA firm when revenue supports it
- Fixable in: V1.1+ (SOC 2)

---

### Correctness — Score: 76 · Weight: 4 · Weighted Deficiency: 0.96

**Justification:** The decisioning layer uses typed findings with strongly-typed payloads per category. The manifest merge logic has dedicated tests. OpenAPI contract drift detection catches accidental breaks. The data consistency orphan probe detects and can remediate orphaned records. Governance workflow has FsCheck/property tests. The agent output quality gate evaluates structural completeness and semantic quality. However: code coverage is below targets (73% line vs. 79% target, 59% branch vs. 63% target), the Persistence layer is at ~40% coverage, and the simulator mode means most CI testing never exercises real LLM paths.

**Tradeoffs:** Increasing coverage in Persistence requires SQL integration test infrastructure; the current gap is documented.

**Recommendations:**
- Lift ArchLucid.Persistence coverage from ~40% toward the 63% per-package floor
- Lift ArchLucid.Api coverage toward the 79% target
- Fixable in: V1

---

### Architectural Integrity — Score: 78 · Weight: 3 · Weighted Deficiency: 0.66

**Justification:** Well-decomposed into 53 projects with clear boundaries (Contracts, Contracts.Abstractions, Core, Application, Coordinator, Decisioning, Persistence split into domain-specific sub-projects, KnowledgeGraph, Provenance, Retrieval, AgentRuntime, ArtifactSynthesis, ContextIngestion, Host.Composition, Host.Core, Worker, Jobs.Cli, Cli, Api, Api.Client). Architecture tests exist in ArchLucid.Architecture.Tests. ADRs are documented. The authority pipeline / coordinator split follows a strangler pattern (ADR 0021). The two-layer packaging model (Pilot/Operate) is consistently applied. However: some complexity from the rename (ArchLucid → ArchLucid, Phase 7 deferred), and the 176+ docs library is extensive but potentially overwhelming.

**Tradeoffs:** The architecture is genuinely well-structured for a V1; the rename cleanup is cosmetic, not structural.

**Recommendations:**
- Complete Phase 7 rename when organizationally ready
- Consider consolidating the persistence sub-projects if the split creates maintenance burden
- Fixable in: V1.1 (rename), evaluate (persistence consolidation)

---

### Security — Score: 74 · Weight: 3 · Weighted Deficiency: 0.78

**Justification:** Strong baseline: RBAC (three roles, five policies), RLS with SESSION_CONTEXT, fixed-time comparison for API keys, LLM prompt redaction, gitleaks secret scanning, CodeQL, Trivy (image + Terraform config), OWASP ZAP baseline, STRIDE threat model, CORS deny-by-default, rate limiting, private endpoint Terraform modules, no SMB/445 exposure. The pen test and PGP key for coordinated disclosure are explicitly deferred to V1.1 and not scored here. The remaining gap is that `appsettings.Development.json` DevelopmentBypass mode lacks a runtime guard to prevent accidental production use.

**Tradeoffs:** Security posture is strong for a pre-revenue V1; the DevelopmentBypass guard is the highest-leverage V1 fix.

**Recommendations:**
- Add a startup guard that prevents DevelopmentBypass in production environments
- Fixable in: V1 (startup guard)

---

### Auditability — Score: 74 · Weight: 2 · Weighted Deficiency: 0.52

**Justification:** 78 typed audit events in append-only SQL, CSV export, audit log with filtering, governance workflow dual-write to durable audit. The AUDIT_COVERAGE_MATRIX.md tracks known gaps with specific rows. Correlation IDs flow through the pipeline. However, some mutating flows lack durable audit events, and the audit search cursor has a timestamp tie-breaking limitation.

**Tradeoffs:** The known-gap documentation is honest; closing gaps incrementally is the right approach.

**Recommendations:**
- Close high-priority audit gaps (authority-critical mutation paths)
- Fixable in: V1

---

### Policy and Governance Alignment — Score: 72 · Weight: 2 · Weighted Deficiency: 0.56

**Justification:** Governance workflows with segregation of duties, SLA tracking, webhook escalation. Pre-commit governance gate configurable by severity threshold. Policy packs with versioning and scope assignments. Governance dashboard with cross-run pending approvals. However, governance is an Operate-layer feature that requires explicit enablement, and no real customer has validated the governance workflow in production.

**Tradeoffs:** Governance depth is impressive for V1 but untested in real enterprise environments.

**Recommendations:**
- Validate governance workflow with a design partner pilot
- Fixable in: V1 (validation), no code changes needed

---

### Compliance Readiness — Score: 69 · Weight: 2 · Weighted Deficiency: 0.62

**Justification:** SOC 2 self-assessment exists with a documented roadmap. CAIQ Lite and SIG Core pre-fills are real assets. DPA template and subprocessors register exist. Compliance matrix and evidence pack are documented. The trust center is well-organized. The pen test is explicitly deferred to V1.1 and not scored here. The remaining gap is the absence of third-party attestation (SOC 2 Type II, ISO 27001) — the self-assessment posture may not be sufficient for enterprise buyers in highly regulated industries.

**Tradeoffs:** SOC 2 Type II is expensive and time-consuming; the self-assessment is the right V1 posture.

**Recommendations:**
- Begin SOC 2 Type II preparation when revenue supports it
- Fixable in: V1.1+ (attestations)

---

### Procurement Readiness — Score: 67 · Weight: 2 · Weighted Deficiency: 0.66

**Justification:** DPA template, subprocessors, CAIQ Lite, SIG Core, MSA template, order form template, procurement pack cover, evidence pack ZIP — these are real and well-structured. The HOW_TO_REQUEST_PROCUREMENT_PACK.md is buyer-friendly. The pen test is explicitly deferred to V1.1 and not scored here. The remaining gaps are: no SOC 2 Type II letter, no published reference customers, and no live Marketplace listing.

**Tradeoffs:** Remaining gaps are V1.1 or business-development items, not code gaps.

**Recommendations:**
- Close reference customer gap via founder dogfooding
- Activate Azure Marketplace SaaS offer when Partner Center verification is complete
- Fixable in: V1.1 (marketplace), ongoing (reference customers)

---

### Interoperability — Score: 62 · Weight: 2 · Weighted Deficiency: 0.76

**Justification:** REST API with OpenAPI/Swagger, AsyncAPI for webhooks, CloudEvents envelope, Bruno collection, CLI, Azure DevOps Work Items. However: no Jira/ServiceNow/Confluence connectors (V1.1), no Slack (V2), no MCP (V1.1), no VS Code extension, no SIEM native integration (webhook-based only). The API is well-documented but the integration catalog is mostly "Planned."

**Tradeoffs:** Properly deferred; webhook + REST is the honest V1 position.

**Recommendations:**
- Prioritize Jira connector for V1.1
- Document webhook recipes for common ITSM tools
- Fixable in: V1.1

---

### Reliability — Score: 73 · Weight: 2 · Weighted Deficiency: 0.54

**Justification:** Circuit breakers for Azure OpenAI (configurable, hot-reloadable), SQL connection open retries with exponential backoff, agent execution handler resilience, CLI HTTP retries, data consistency orphan probe, outbox convergence monitoring, degraded mode documentation, Simmy chaos testing in CI. RTO/RPO targets documented by tier. However: no formal SRE error budget, chaos testing is periodic (not continuous), and multi-region active/active is not a V1 guarantee.

**Tradeoffs:** Reliability infrastructure is strong for a V1; multi-region is properly deferred.

**Recommendations:**
- Define formal SLI/SLO targets beyond the current documentation targets
- Fixable in: V1.1

---

### Data Consistency — Score: 72 · Weight: 2 · Weighted Deficiency: 0.56

**Justification:** Orphan probe with detection, alerting, quarantine modes. Admin remediation API with dry-run. Dual-persistence row reconciliation tests. DbUp migrations with ordered scripts. Comparison replay with drift verification mode. However: the orphan probe is detection-only by default (remediation requires explicit admin action), and the data consistency enforcement modes (Alert, Quarantine, AutoQuarantine) add operational complexity.

**Tradeoffs:** Conservative default (detection-only) is the right posture; auto-remediation would be risky.

**Recommendations:**
- Document clear operator runbooks for each enforcement mode
- Fixable in: V1

---

### Maintainability — Score: 74 · Weight: 2 · Weighted Deficiency: 0.52

**Justification:** Well-decomposed project structure, central package management (Directory.Packages.props), editor config, consistent coding patterns. Architecture tests enforce structural rules. DI registration map documented. Code map exists. Coverage exclusions documented with categories. However: 53 projects may be over-decomposed (e.g., five Persistence sub-projects), and the 176+ docs could benefit from consolidation.

**Tradeoffs:** The decomposition enables independent evolution but increases cognitive load for contributors.

**Recommendations:**
- Evaluate whether Persistence sub-projects should be consolidated
- Fixable in: V1.1

---

### Explainability — Score: 74 · Weight: 2 · Weighted Deficiency: 0.52

**Justification:** Finding inspector with persisted artifacts, explainability traces, citation chips with kind labels, faithfulness metrics, aggregate explanation with confidence scores, theme summaries, risk posture. The "citations vs. proof" disclaimer is honest. LLM audit trail (redacted) available via separate API. However: faithfulness ratio is a heuristic (token overlap), not a formal verification. The explanation may fall back to deterministic manifest text when faithfulness is low.

**Tradeoffs:** Honest fallback to deterministic text when LLM quality is low is the right design choice.

**Recommendations:**
- Consider adding confidence interval or uncertainty indicators to explanations
- Fixable in: V1.1

---

### AI/Agent Readiness — Score: 68 · Weight: 2 · Weighted Deficiency: 0.64

**Justification:** Multi-agent orchestration (topology, cost, compliance, critic agents), agent output quality gate (structural completeness + semantic score), agent trace forensics (blob upload with inline fallback), prompt redaction, LLM token quota management, circuit breakers, simulator mode for testing. Agent evaluation datasets with nightly CI. However: the agents are currently task-driven (not autonomous), the simulator is the default (real LLM path is opt-in), and MCP server is deferred to V1.1. No agent marketplace or third-party agent support.

**Tradeoffs:** Task-driven orchestration is appropriate for V1; autonomous planning is properly deferred.

**Recommendations:**
- Validate real LLM agent quality with the design partner
- Fixable in: V1 (validation)

---

### Azure Compatibility and SaaS Deployment Readiness — Score: 70 · Weight: 2 · Weighted Deficiency: 0.60

**Justification:** 110 Terraform files across 15+ stacks (container-apps, SQL failover, edge/Front Door, storage, monitoring, Key Vault, Service Bus, OpenAI, OTEL collector, Entra, private networking, logic apps, orchestrator, pilot). CD pipeline exists. Managed identity documented. Container Apps with secondary region option. However: Terraform state mv (Phase 7.5) is pending, no live production deployment documented, ACR push not yet in CI, and the hosted SaaS probe badge suggests staging is configured but production custom domains are not yet live.

**Tradeoffs:** Infrastructure breadth is impressive; the gap is operational activation, not design.

**Recommendations:**
- Complete hosted SaaS activation (Front Door custom domains, ACR push in CI)
- Fixable in: V1

---

### Decision Velocity — Score: 72 · Weight: 2 · Weighted Deficiency: 0.56

**Justification:** The product does accelerate time-to-decision for architecture reviews when the pipeline runs. Commit produces a manifest in seconds. The comparison and replay features enable rapid "what changed" analysis. However: the value is only realized after the setup hurdle, and the lack of a live trial funnel means decision velocity for the buying decision itself is slow.

**Tradeoffs:** Product decision velocity is high; commercial decision velocity is low.

**Recommendations:**
- Focus on reducing buyer decision friction (live trial, reference customers)
- Fixable in: V1.1

---

### Commercial Packaging Readiness — Score: 60 · Weight: 2 · Weighted Deficiency: 0.80

**Justification:** Pricing philosophy is thorough (three tiers, platform fee + seats model, discount stack). Order form template exists. Stripe checkout is wired but test-mode. Azure Marketplace SaaS offer alignment doc exists. The `[RequiresCommercialTenantTier]` 402 filter is implemented. However: no live Stripe keys, no published Marketplace listing, no production DNS cutover for signup. The commercial motion is sales-led with `/pricing` displaying numbers and ORDER_FORM_TEMPLATE.md driving quote-to-cash. The gap between "pricing documented" and "buyer can purchase" is the primary blocker.

**Tradeoffs:** Commerce un-hold requires owner actions (Partner Center verification, tax profile, payout account) that cannot be automated.

**Recommendations:**
- Complete Partner Center seller verification and tax profile
- Activate Stripe live keys on staging first
- Fixable in: V1.1 (owner-gated)

---

### Accessibility — Score: 72 · Weight: 1 · Weighted Deficiency: 0.28

**Justification:** WCAG 2.1 Level AA target. Merge-blocking axe-core/Playwright scans on 35 URL patterns. Vitest + jest-axe component-level checks. Skip-to-content link, language attribute, landmark navigation, form labels, focus management, error regions with role="alert", route announcer. However: Search/Ask pages use raw HTML without focus rings or dark mode support, creating accessibility regressions on the most cognitively demanding pages.

**Tradeoffs:** The automated scanning infrastructure is strong; the Search/Ask regression is fixable.

**Recommendations:**
- Refactor Search/Ask to use design system components with proper focus management
- Fixable in: V1

---

### Customer Self-Sufficiency — Score: 63 · Weight: 1 · Weighted Deficiency: 0.37

**Justification:** CLI doctor, support bundle, health endpoints, troubleshooting doc, glossary tooltips, contextual help popovers. However: `NEXT_PUBLIC_DOCS_BASE_URL` commonly unset (help links broken), no in-app knowledge base or FAQ, support relies on email + correlation ID. The operator atlas is comprehensive but lives in docs, not in the product.

**Tradeoffs:** In-app help is high effort for V1; the docs + CLI diagnostics are adequate for pilot customers.

**Recommendations:**
- Set NEXT_PUBLIC_DOCS_BASE_URL in default configurations
- Fixable in: V1

---

### Change Impact Clarity — Score: 70 · Weight: 1 · Weighted Deficiency: 0.30

**Justification:** Comparison replay with structured golden-manifest deltas, AI-powered comparison explanations, drift verification mode, CHANGELOG.md, BREAKING_CHANGES.md. The compare page shows what changed between runs. However: no visual diff highlighting in the UI (text-based diffs only), and the comparison explanation is LLM-generated (with faithfulness caveats).

**Tradeoffs:** Visual diff would be a nice-to-have; the current text-based approach is functional.

**Recommendations:**
- Consider adding visual diff highlighting for manifest comparisons
- Fixable in: V1.1

---

### Stickiness — Score: 70 · Weight: 1 · Weighted Deficiency: 0.30

**Justification:** The governance workflow, audit trail, manifest versioning, and comparison history create real switching costs once adopted. Product learning and recommendation learning create data gravity. However: with no live customers, stickiness is theoretical. The deferred "brains" (product learning planning bridge) would increase stickiness but are not in V1.

**Tradeoffs:** Stickiness features exist but are untested with real customers.

**Recommendations:**
- Validate stickiness with design partner usage patterns
- Fixable in: V1 (validation)

---

### Template and Accelerator Richness — Score: 55 · Weight: 1 · Weighted Deficiency: 0.45

**Justification:** The new-run wizard has sample presets. The demo seed provides a Contoso trusted-baseline scenario. The CLI `new` command creates a project skeleton. However: only one sample preset is documented, no industry-specific templates, no architecture pattern library, and no pre-built policy packs for common compliance frameworks (SOC 2, HIPAA, PCI-DSS).

**Tradeoffs:** Templates are high-value for adoption but require domain expertise to create well.

**Recommendations:**
- Create 3-5 industry or framework-specific architecture request templates
- Create pre-built policy packs for SOC 2 and common compliance frameworks
- Fixable in: V1.1

---

### Availability — Score: 73 · Weight: 1 · Weighted Deficiency: 0.27

**Justification:** Health endpoints (liveness, readiness, full), Container Apps with health probes, Front Door for edge routing, auto-failover group support, secondary region Terraform. However: no formal SLA published (SLA_SUMMARY.md exists but is internal), no multi-region active/active guarantee, and the hosted SaaS is not yet in production.

**Tradeoffs:** Availability infrastructure is well-designed; operational activation is pending.

**Recommendations:**
- Publish customer-facing SLA when hosted SaaS goes live
- Fixable in: V1.1

---

### Performance — Score: 72 · Weight: 1 · Weighted Deficiency: 0.28

**Justification:** k6 operator-path smoke with merge-blocking thresholds (p95 < 2000ms, p99 < 5000ms), per-tenant burst testing weekly, read-through caching with configurable TTL, hot-path cache with row-version invalidation. Cold start and trimming documented. However: no published performance benchmarks, and the k6 thresholds are generous (2s p95).

**Tradeoffs:** Performance is adequate for pilot workloads; optimization should follow real production data.

**Recommendations:**
- Tighten k6 thresholds as baseline data accumulates
- Fixable in: V1.1

---

### Scalability — Score: 68 · Weight: 1 · Weighted Deficiency: 0.32

**Justification:** Scaling path documented (single catalog → per-tenant DB → multi-region). Read replica factory exists. Per-tenant burst testing validates concurrent tenant load. Elastic pool option described. However: single-tenant-per-DB is deferred, multi-region is optional, and no real multi-tenant production data validates the scaling assumptions.

**Tradeoffs:** Scaling is properly designed for evolution; V1 doesn't need multi-tenant production scale.

**Recommendations:**
- Validate scaling assumptions during design partner pilot
- Fixable in: V1 (validation)

---

### Supportability — Score: 72 · Weight: 1 · Weighted Deficiency: 0.28

**Justification:** CLI doctor, support bundle (with --zip), version endpoint, correlation IDs, troubleshooting guide, pilot guide with "when you report an issue" section. Bruno collection for manual smoke. However: no ticketing system integration, no telemetry-based alerting for customer issues, and support relies on email.

**Tradeoffs:** Email + correlation ID is appropriate for V1 pilot support.

**Recommendations:**
- Consider Zendesk/Freshdesk integration for V1.1
- Fixable in: V1.1

---

### Manageability — Score: 68 · Weight: 1 · Weighted Deficiency: 0.32

**Justification:** Extensive configuration surface (appsettings, environment variables, user secrets), hot-reloadable circuit breaker options, admin API endpoints, SCIM provisioning, operations admin documentation. However: configuration sprawl (many knobs across many sections), no central configuration management (Azure App Configuration deferred), and the admin API surface is large.

**Tradeoffs:** Configuration flexibility enables diverse deployments but increases cognitive load.

**Recommendations:**
- Document a "minimum viable configuration" for each deployment scenario
- Fixable in: V1

---

### Deployability — Score: 71 · Weight: 1 · Weighted Deficiency: 0.29

**Justification:** Dockerfiles, docker-compose profiles, Terraform modules, CD pipeline, DbUp auto-migration, devcontainer. Release smoke script and RC drill script. However: ACR push not in CI, production deployment not documented end-to-end, and the Terraform state mv (Phase 7.5) is pending.

**Tradeoffs:** Deployment infrastructure is well-designed; operational activation is the gap.

**Recommendations:**
- Add ACR push to CI pipeline
- Document end-to-end production deployment guide
- Fixable in: V1

---

### Observability — Score: 76 · Weight: 1 · Weighted Deficiency: 0.24

**Justification:** Comprehensive OpenTelemetry instrumentation (25+ custom metrics), Prometheus SLO rules, Grafana dashboards in Terraform, Application Insights integration, business-level KPI metrics, agent trace forensics, data consistency monitoring, circuit breaker state metrics, LLM cost tracking. However: observability depends on Prometheus/Grafana being provisioned; no built-in observability dashboard in the product.

**Tradeoffs:** External observability tooling is the right architecture; built-in dashboards would be a V1.1 feature.

**Recommendations:**
- Include Grafana dashboard provisioning in the hosted SaaS setup
- Fixable in: V1

---

### Testability — Score: 75 · Weight: 1 · Weighted Deficiency: 0.25

**Justification:** 20 test projects, tiered CI (8 tiers), coverage ratchets, Stryker mutation testing (8 config files), k6 load tests, Playwright E2E (mock + live), Vitest + jest-axe, Schemathesis API fuzzing, Simmy chaos testing, golden cohort nightly, agent eval datasets nightly, coordinator parity daily. FsCheck property tests for governance. However: coverage below targets (~73% line vs. 79%), Persistence at ~40%, and some tests fail locally without SQL.

**Tradeoffs:** The testing infrastructure is impressive; the gap is coverage depth, not breadth.

**Recommendations:**
- Lift Persistence coverage to meet per-package floor
- Fixable in: V1

---

### Modularity — Score: 78 · Weight: 1 · Weighted Deficiency: 0.22

**Justification:** 53 projects with clear boundaries, Contracts.Abstractions split, five Persistence sub-projects, Host.Composition for DI wiring, separate Worker process. Architecture tests enforce boundary rules. However: the decomposition may be slightly over-granular (5 Persistence sub-projects for what could be 2-3).

**Tradeoffs:** Fine-grained decomposition aids independent evolution; may increase build time and cognitive load.

**Recommendations:**
- Evaluate Persistence sub-project consolidation if maintenance burden warrants
- Fixable in: V1.1

---

### Extensibility — Score: 70 · Weight: 1 · Weighted Deficiency: 0.30

**Justification:** HOWTO_FINDING_ENGINE_PLUGINS.md exists for extending the finding engine. CloudEvents webhooks enable external integration. Policy packs are versioned and scopable. Integration events with schema registry. However: no plugin marketplace, no third-party agent support, MCP deferred to V1.1.

**Tradeoffs:** The extension points exist; the ecosystem is not yet mature.

**Recommendations:**
- Document extension patterns more thoroughly
- Fixable in: V1

---

### Evolvability — Score: 72 · Weight: 1 · Weighted Deficiency: 0.28

**Justification:** ADRs document architectural decisions. The strangler pattern for coordinator → authority migration is well-documented. API versioning with deprecation policy. Breaking changes documented. Feature gates for progressive rollout. However: the Phase 7 rename adds technical debt, and the 176+ docs need governance to prevent drift.

**Tradeoffs:** The architectural evolution strategy is sound; execution depends on team velocity.

**Recommendations:**
- Complete Phase 7 rename to reduce ongoing confusion
- Fixable in: V1.1

---

### Documentation — Score: 73 · Weight: 1 · Weighted Deficiency: 0.27

**Justification:** Extraordinary volume: 176+ library docs, architecture poster, operator atlas, five-document onboarding spine, glossary, troubleshooting, API contracts, CLI usage, 43 go-to-market docs. Docs root size guard in CI (≤32). Navigator link validation. However: the sheer volume creates cognitive overload. Multiple overlapping entry points. Some docs reference "ArchLucid" and "ArchLucid" inconsistently. The FIRST_5_DOCS spine is a good attempt at curation but the doc tree is deep.

**Tradeoffs:** Comprehensive documentation is an asset; discoverability is the problem.

**Recommendations:**
- Consolidate overlapping entry points
- Complete the rename for consistency
- Fixable in: V1

---

### Azure Ecosystem Fit — Score: 74 · Weight: 1 · Weighted Deficiency: 0.26

**Justification:** Azure-native by ADR (Entra ID, Azure SQL, Container Apps, Front Door, Key Vault, Service Bus, Storage, Azure OpenAI, Application Insights, Managed Grafana). Terraform for all infrastructure. Azure Marketplace SaaS offer documented. However: no live Marketplace listing, and the Azure-first stance may limit non-Azure enterprise adoption.

**Tradeoffs:** Azure-native is the right strategic choice; multi-cloud support would dilute focus.

**Recommendations:**
- Activate Azure Marketplace listing when ready
- Fixable in: V1.1

---

### Cognitive Load — Score: 64 · Weight: 1 · Weighted Deficiency: 0.36

**Justification:** Progressive disclosure helps (three-tier nav), but: 176+ docs, four onboarding routes, Search/Ask pages with inconsistent UI, large configuration surface, complex governance workflow that most pilots won't need on Day 1. The operator atlas and glossary tooltips help. LayerContextStrip and "Back to Core Pilot" escape hatch are good. However, a new operator can easily get lost in the Operate layer before proving Pilot value.

**Tradeoffs:** The progressive disclosure model is the right architecture; enforcement needs strengthening.

**Recommendations:**
- Consolidate onboarding routes
- Improve first-run guidance to keep operators in Pilot until first commit
- Fixable in: V1

---

### Cost-Effectiveness — Score: 70 · Weight: 1 · Weighted Deficiency: 0.30

**Justification:** Per-tenant cost model documented. LLM cost tracking metrics. Consumption budgets in Terraform. Pilot profile documented. Simulator mode avoids LLM cost during development/testing. However: no real production cost data, and the pricing model's break-even at ~180 architect-hours/year is theoretical.

**Tradeoffs:** Cost modeling is appropriate for V1; real data will refine it.

**Recommendations:**
- Validate cost model during design partner pilot
- Fixable in: V1 (validation)

---

## 3. Weighted Score Calculation

| Quality | Score | Weight | Weighted Score | Weighted Deficiency |
|---------|-------|--------|----------------|---------------------|
| Marketability | 62 | 8 | 4.96 | 3.04 |
| Time-to-Value | 63 | 7 | 4.41 | 2.59 |
| Adoption Friction | 60 | 6 | 3.60 | 2.40 |
| Proof-of-ROI Readiness | 58 | 5 | 2.90 | 2.10 |
| Executive Value Visibility | 65 | 4 | 2.60 | 1.40 |
| Differentiability | 72 | 4 | 2.88 | 1.12 |
| Correctness | 76 | 4 | 3.04 | 0.96 |
| Architectural Integrity | 78 | 3 | 2.34 | 0.66 |
| Security | 74 | 3 | 2.22 | 0.78 |
| Traceability | 72 | 3 | 2.16 | 0.84 |
| Usability | 68 | 3 | 2.04 | 0.96 |
| Workflow Embeddedness | 60 | 3 | 1.80 | 1.20 |
| Trustworthiness | 72 | 3 | 2.16 | 0.84 |
| Reliability | 73 | 2 | 1.46 | 0.54 |
| Data Consistency | 72 | 2 | 1.44 | 0.56 |
| Maintainability | 74 | 2 | 1.48 | 0.52 |
| Explainability | 74 | 2 | 1.48 | 0.52 |
| AI/Agent Readiness | 68 | 2 | 1.36 | 0.64 |
| Azure Compatibility and SaaS Deployment Readiness | 70 | 2 | 1.40 | 0.60 |
| Auditability | 74 | 2 | 1.48 | 0.52 |
| Policy and Governance Alignment | 72 | 2 | 1.44 | 0.56 |
| Compliance Readiness | 69 | 2 | 1.38 | 0.62 |
| Procurement Readiness | 67 | 2 | 1.34 | 0.66 |
| Interoperability | 62 | 2 | 1.24 | 0.76 |
| Decision Velocity | 72 | 2 | 1.44 | 0.56 |
| Commercial Packaging Readiness | 60 | 2 | 1.20 | 0.80 |
| Accessibility | 72 | 1 | 0.72 | 0.28 |
| Customer Self-Sufficiency | 63 | 1 | 0.63 | 0.37 |
| Change Impact Clarity | 70 | 1 | 0.70 | 0.30 |
| Stickiness | 70 | 1 | 0.70 | 0.30 |
| Template and Accelerator Richness | 55 | 1 | 0.55 | 0.45 |
| Availability | 73 | 1 | 0.73 | 0.27 |
| Performance | 72 | 1 | 0.72 | 0.28 |
| Scalability | 68 | 1 | 0.68 | 0.32 |
| Supportability | 72 | 1 | 0.72 | 0.28 |
| Manageability | 68 | 1 | 0.68 | 0.32 |
| Deployability | 71 | 1 | 0.71 | 0.29 |
| Observability | 76 | 1 | 0.76 | 0.24 |
| Testability | 75 | 1 | 0.75 | 0.25 |
| Modularity | 78 | 1 | 0.78 | 0.22 |
| Extensibility | 70 | 1 | 0.70 | 0.30 |
| Evolvability | 72 | 1 | 0.72 | 0.28 |
| Documentation | 73 | 1 | 0.73 | 0.27 |
| Azure Ecosystem Fit | 74 | 1 | 0.74 | 0.26 |
| Cognitive Load | 64 | 1 | 0.64 | 0.36 |
| Cost-Effectiveness | 70 | 1 | 0.70 | 0.30 |
| **TOTAL** | — | **100** | **71.35** | **28.65** |

---

## 4. Top 10 Most Important Weaknesses

These are cross-cutting weaknesses, not repetitions of individual quality names.

### 1. No Live Commerce Path (Weighted Deficiency: ~3.8 across Marketability, Commercial Packaging, Time-to-Value)

The entire revenue generation pipeline — Stripe live keys, Azure Marketplace listing, production DNS cutover for signup — is wired but not activated. Every sale requires founder intervention. This is the single largest constraint on the business.

### 2. Zero Published Reference Customers (Weighted Deficiency: ~2.5 across Marketability, Proof-of-ROI, Trustworthiness, Procurement)

All three reference-customer rows are Placeholder or Customer review. Without a single provable win, every enterprise sales conversation starts from zero credibility. The −15% reference discount is budgeted but has no customer to validate it.

### 3. Self-Serve Trial Funnel Not Live (Weighted Deficiency: ~2.3 across Time-to-Value, Adoption Friction, Commercial Packaging)

The hosted trial funnel is technically wired (staging.archlucid.com configured, Entra social login designed, demo seed automated) but not activated. Prospects cannot self-evaluate. This creates a fatal bottleneck in the buyer journey for anyone not already in a founder-led conversation.

### 4. Onboarding Route Fragmentation (Weighted Deficiency: ~1.8 across Adoption Friction, Usability, Cognitive Load)

Four overlapping onboarding routes with three distinct wizard implementations create confusion and wasted effort. A new operator may land on any of these, not realize the others exist, and repeat work or miss steps. This directly undermines the "first 30 minutes" experience.

### 5. No Third-Party Security Attestation (Weighted Deficiency: ~0.9 across Compliance, Procurement, Trustworthiness)

No SOC 2 Type II attestation has been issued. The self-assessment, STRIDE threat model, and trust center are strong interim assets. The pen test is explicitly deferred to V1.1 and not scored. Some enterprise buyers in regulated industries will require third-party attestation before signing.

### 6. Design System Inconsistency on High-Traffic Pages (Weighted Deficiency: ~1.3 across Usability, Accessibility, Cognitive Load)

Search and Ask pages — two of the most cognitively demanding pages — use raw HTML elements (raw `<input>`, `<button>`, `<textarea>`, inline `style={}`) instead of the design system (Input, Button, Textarea, Card). This creates visual inconsistency, missing focus rings, no dark mode support, and accessibility regression.

### 7. Code Coverage Below Stated Targets (Weighted Deficiency: ~1.2 across Correctness, Testability, Reliability)

Measured locally at ~73% line (target: 79%) and ~59% branch (target: 63%). ArchLucid.Persistence is at ~40% (target: 63%). The CI ratchet gates are set but not met, meaning the strict profile would fail if actually enforced at stated thresholds.

### 8. Help Links Broken by Default (Weighted Deficiency: ~0.9 across Usability, Customer Self-Sufficiency, Adoption Friction)

`NEXT_PUBLIC_DOCS_BASE_URL` is commonly unset, making every contextual help "Open documentation" link non-functional. Glossary tooltips and help popovers exist but point to nowhere when the env var is missing.

### 9. ITSM Integration Gap (Weighted Deficiency: ~0.8 across Workflow Embeddedness, Interoperability)

No first-party Jira, ServiceNow, or Confluence connectors. Enterprise teams that live in these tools must build custom webhook consumers. Properly deferred to V1.1 but still creates adoption friction for enterprises with established ITSM workflows.

### 10. ROI Proof Depends on Manual Baselines (Weighted Deficiency: ~0.7 across Proof-of-ROI, Executive Value Visibility)

The ROI model requires operators to manually capture baseline metrics before the pilot. The automated review-cycle delta from signup is optional and uses modeled estimates as default. No real customer telemetry validates the claimed savings. The value-report DOCX exists but produces modeled rather than measured numbers.

---

## 5. Top 5 Monetization Blockers

### 1. Commerce Un-Hold (Stripe + Marketplace)

No live Stripe keys in production, no Published Azure Marketplace SaaS offer, no production DNS cutover for `signup.archlucid.com`. The entire purchasing flow is test-mode. This is the single most important monetization blocker and requires owner actions (Partner Center verification, tax profile, payout account).

### 2. Zero Reference Customers

Without a single published case study, every sales conversation requires the founder to build credibility from scratch. The −15% reference discount cannot be retired. Enterprise procurement teams will ask "who else uses this?" and get no answer.

### 3. Self-Serve Trial Funnel Not Live

Product-led growth is impossible without a live trial funnel. The current sales-led motion requires founder time for every prospect. PLG is the path to scalable revenue; it is wired but not active.

### 4. Proof-of-ROI Requires Manual Effort

The ROI model is theoretical until a real customer validates it. The value-report DOCX generates modeled numbers, not measured outcomes. A sponsor needs a credible "we saved X hours" story, and the product doesn't automatically produce one from real telemetry.

### 5. No Third-Party Attestation for Procurement

Some enterprise buyers will not sign a contract without a SOC 2 Type II report or equivalent third-party attestation. The self-assessment and trust center are strong interim assets, and the pen test is on the V1.1 roadmap. This may slow deals in regulated industries.

---

## 6. Top 5 Enterprise Adoption Blockers

### 1. No SOC 2 Type II Attestation

The self-assessment and roadmap exist, but no CPA has issued a Type II report. Large enterprises in finance, healthcare, and government often require SOC 2 Type II as a procurement gate. Timeline: 6-12 months from engagement.

### 2. No Third-Party Security Attestation

No SOC 2 Type II or equivalent third-party attestation has been issued. Security reviewers in enterprise procurement may require this to complete their diligence. The STRIDE threat model, ZAP baseline, and self-assessment are strong V1 assets. The pen test is on the V1.1 roadmap.

### 3. No ITSM Connectors (Jira / ServiceNow)

Enterprise implementation teams will ask "how does this integrate with our Jira/ServiceNow workflow?" and the answer is "build a custom webhook consumer." This is properly deferred to V1.1 but is a real friction point for enterprises with mandated ITSM workflows.

### 4. Onboarding Confusion

Four overlapping onboarding routes with inconsistent wizard implementations. An enterprise implementation team deploying ArchLucid for the first time will hit confusion on Day 1. This is fixable in V1 and should be.

### 5. No Published SLA

SLA_SUMMARY.md exists internally but is not customer-facing. Enterprise procurement teams expect a contractual SLA with defined uptime commitments and remediation terms. The RTO/RPO targets are internal planning documents, not customer commitments.

---

## 7. Top 5 Engineering Risks

### 1. Code Coverage Below Ratchet Targets

If the strict profile (79% line, 63% branch, 63% per-package) were enforced today, CI would fail. The gap is documented but unresolved. ArchLucid.Persistence at ~40% is a particular concern for a data-critical application. Risk: undetected bugs in persistence layer.

### 2. DevelopmentBypass Mode Lacks Production Guard

`appsettings.Development.json` enables DevelopmentBypass, which authenticates all requests as DevUserId with DevRole. If `ASPNETCORE_ENVIRONMENT=Development` is accidentally set in production, all authentication is bypassed. No startup guard prevents this. Risk: complete auth bypass in misconfigured production deployment.

### 3. Simulator-Default Testing Masks Real LLM Quality

Most CI tests run with the agent simulator, never exercising real Azure OpenAI paths. The agent eval datasets nightly job exists but the quality gate (structural completeness + semantic score) operates on simulated output. Risk: production LLM quality issues not caught until customer-facing.

### 4. Multi-Tenant RLS Depends on Application Context

Row-level security uses SQL SESSION_CONTEXT set by the application layer. If the application fails to set context (bug, middleware bypass, or new code path), tenant isolation depends on application-level scoping alone. The RLS_RISK_ACCEPTANCE.md acknowledges this. Risk: cross-tenant data exposure in edge cases.

### 5. Terraform State Drift from Rename

The Phase 7.5 `state mv` is deferred. Terraform resource addresses may still contain legacy names, creating confusion and potential drift between state files and actual resource names. Risk: deployment errors or state corruption during infrastructure changes.

---

## 8. Most Important Truth

**ArchLucid is a well-engineered product with no customers.** The engineering quality — architecture, testing infrastructure, security posture, documentation depth — is genuinely impressive for a pre-revenue V1. But the commercial path from "working product" to "first dollar of revenue" is blocked by five owner-gated decisions (Stripe live keys, Marketplace publication, Partner Center verification, pen-test completion, and reference customer acquisition) that no amount of engineering will resolve. The highest-leverage action right now is not writing more code — it is closing a design-partner pilot and activating the commerce pipeline.

---

## 9. Top Improvement Opportunities

### Improvement 1: Consolidate Onboarding Routes to a Single Canonical Path

**Title:** Consolidate Four Overlapping Onboarding Routes

**Why it matters:** Four overlapping routes (`/onboarding`, `/onboarding/start`, `/onboard`, `/getting-started`) with three distinct wizard implementations create Day-1 confusion for every new operator. This directly hurts Adoption Friction, Usability, and Cognitive Load — three of the highest weighted deficiencies.

**Expected impact:** Directly improves Adoption Friction (+5-7 pts), Usability (+3-4 pts), Cognitive Load (+3-5 pts). Weighted readiness impact: +0.5-0.8%.

**Affected qualities:** Adoption Friction, Usability, Cognitive Load, Time-to-Value, Customer Self-Sufficiency

**Status:** Fully actionable now.

**Cursor Prompt:**

```
Consolidate the four overlapping onboarding routes in archlucid-ui into a single canonical path.

Context:
- The following routes currently exist with separate implementations:
  - /onboarding (OnboardingWizardClient)
  - /onboarding/start (variant of above)
  - /onboard (OnboardWizardClient — separate component)
  - /getting-started (OperatorFirstRunWorkflowPanel)
- This creates confusion for new operators who may land on any path.

Tasks:
1. Choose `/getting-started` as the canonical route (it uses OperatorFirstRunWorkflowPanel, the most recent implementation aligned with the Core Pilot checklist).
2. Add Next.js `redirect()` from `/onboarding`, `/onboarding/start`, and `/onboard` to `/getting-started` in each of their `page.tsx` files. Use permanent redirects (308).
3. Update `nav-config.ts` to point any onboarding references to `/getting-started`.
4. Update the sidebar, home page, and any component that links to the old routes.
5. Remove the `OnboardingWizardClient` and `OnboardWizardClient` components if they are no longer referenced after the redirect.
6. Keep the `OperatorFirstRunWorkflowPanel` as the single wizard implementation.
7. Update any tests that reference the old routes to use `/getting-started`.

Files to examine:
- archlucid-ui/src/app/(operator)/onboarding/
- archlucid-ui/src/app/(operator)/onboard/
- archlucid-ui/src/app/(operator)/getting-started/
- archlucid-ui/src/lib/nav-config.ts
- archlucid-ui/src/components/OnboardingWizardClient.tsx (if exists)
- archlucid-ui/src/components/OnboardWizardClient.tsx (if exists)
- archlucid-ui/src/components/OperatorFirstRunWorkflowPanel.tsx

Acceptance criteria:
- Only one onboarding wizard implementation remains active
- /onboarding, /onboarding/start, /onboard all 308-redirect to /getting-started
- nav-config.ts references only /getting-started
- All existing tests pass
- No dead component code remains

Constraints:
- Do NOT change the OperatorFirstRunWorkflowPanel implementation
- Do NOT change the Core Pilot checklist steps
- Do NOT modify the API or backend
- Preserve any deep-link query parameters through the redirect
```

---

### Improvement 2: Refactor Search and Ask Pages to Use Design System Components

**Title:** Refactor Search/Ask Pages to Use Design System

**Why it matters:** These are two of the most cognitively demanding operator pages, yet they use raw HTML elements (raw `<input>`, `<button>`, `<textarea>`, inline `style={}`) instead of the design system (Input, Button, Textarea, Card). This creates visual inconsistency, missing focus rings, no dark-mode support, and accessibility regression. Fixing this improves Usability, Accessibility, and Cognitive Load.

**Expected impact:** Directly improves Usability (+4-6 pts), Accessibility (+3-4 pts), Cognitive Load (+2-3 pts). Weighted readiness impact: +0.3-0.5%.

**Affected qualities:** Usability, Accessibility, Cognitive Load, Adoption Friction

**Status:** Fully actionable now.

**Cursor Prompt:**

```
Refactor the Search (/search) and Ask (/ask) pages in archlucid-ui to use design system components instead of raw HTML elements.

Context:
- These pages currently use raw <input>, <button>, <textarea>, and inline style={} instead of the project's design system components (Input, Button, Textarea, Card from @/components/ui/).
- This creates visual inconsistency with the rest of the operator shell, missing focus rings, no dark-mode support, and accessibility regression.
- The rest of the shell uses shadcn/ui components via @/components/ui/.

Tasks:
1. In the Search page (archlucid-ui/src/app/(operator)/search/page.tsx and any client components):
   - Replace raw <input> with <Input> from @/components/ui/input
   - Replace raw <button> with <Button> from @/components/ui/button
   - Replace inline style={} with Tailwind utility classes consistent with the shell
   - Wrap results in <Card> from @/components/ui/card where appropriate
   - Ensure focus-visible styles match the shell

2. In the Ask page (archlucid-ui/src/app/(operator)/ask/page.tsx and any client components):
   - Replace raw <textarea> with <Textarea> from @/components/ui/textarea
   - Replace raw <button> with <Button> from @/components/ui/button
   - Replace raw <input> with <Input> from @/components/ui/input
   - Replace inline style={} with Tailwind utility classes
   - Wrap content areas in <Card> where appropriate
   - Ensure focus-visible styles match the shell

3. Verify dark mode works correctly on both pages.
4. Run vitest and ensure no regressions.

Files to modify:
- archlucid-ui/src/app/(operator)/search/page.tsx
- archlucid-ui/src/app/(operator)/search/ (any client components)
- archlucid-ui/src/app/(operator)/ask/page.tsx
- archlucid-ui/src/app/(operator)/ask/ (any client components)

Acceptance criteria:
- Zero raw <input>, <button>, <textarea> elements on Search and Ask pages
- Zero inline style={} on these pages
- Both pages render correctly in light and dark mode
- Focus rings are visible on all interactive elements
- All existing tests pass
- axe accessibility scan passes on both pages

Constraints:
- Do NOT change the API calls or data flow
- Do NOT change the functionality of search or ask
- Use only existing design system components from @/components/ui/
- Preserve all existing keyboard shortcuts and focus behavior
```

---

### Improvement 3: Set NEXT_PUBLIC_DOCS_BASE_URL in Default Configurations

**Title:** Fix Broken Help Links by Setting NEXT_PUBLIC_DOCS_BASE_URL

**Why it matters:** When `NEXT_PUBLIC_DOCS_BASE_URL` is unset (which is the common case), every contextual help "Open documentation" link, glossary tooltip "Learn more" deep-link, and help popover is non-functional. This directly undermines the help system that was carefully built into the product.

**Expected impact:** Directly improves Customer Self-Sufficiency (+5-8 pts), Usability (+2-3 pts), Adoption Friction (+2-3 pts). Weighted readiness impact: +0.2-0.4%.

**Affected qualities:** Customer Self-Sufficiency, Usability, Adoption Friction, Cognitive Load

**Status:** Fully actionable now.

**Cursor Prompt:**

```
Fix broken help links in archlucid-ui by setting NEXT_PUBLIC_DOCS_BASE_URL in default configurations.

Context:
- NEXT_PUBLIC_DOCS_BASE_URL is commonly unset, making every contextual help "Open documentation" link non-functional.
- Glossary tooltips and help popovers point to paths like "/docs/library/GLOSSARY.md" but without a base URL these are not clickable or functional.
- The help system (contextual ?, glossary tooltips, "Learn more" links) was carefully built but is broken by default.

Tasks:
1. Add NEXT_PUBLIC_DOCS_BASE_URL to archlucid-ui/.env (or .env.local.example if that pattern is used):
   - Default value: "https://github.com/joefrancisGA/ArchLucid/blob/main" (the public GitHub repo)
   - This makes all doc links functional out of the box

2. Add NEXT_PUBLIC_DOCS_BASE_URL to archlucid-ui/.env.development if it exists:
   - Same default value

3. Add NEXT_PUBLIC_DOCS_BASE_URL to docker-compose.yml and docker-compose.demo.yml for the UI service:
   - Same default value

4. Add NEXT_PUBLIC_DOCS_BASE_URL to the devcontainer configuration if applicable.

5. Add a note to archlucid-ui/README.md documenting this env var and its purpose.

6. Verify that help links render as clickable URLs when the env var is set.

Files to modify:
- archlucid-ui/.env (create if not exists, or .env.example)
- archlucid-ui/.env.development (if exists)
- docker-compose.yml (UI service environment)
- docker-compose.demo.yml (UI service environment)
- archlucid-ui/README.md (add documentation)
- .devcontainer/ files if applicable

Acceptance criteria:
- NEXT_PUBLIC_DOCS_BASE_URL is set to a working default in all standard configurations
- Help links render as clickable URLs pointing to the correct docs
- Glossary "Learn more" links are functional
- No changes to the help system code itself — only configuration

Constraints:
- Do NOT change the help system components or link construction logic
- Do NOT hardcode URLs in components — use the existing env var pattern
- The default should work for the public GitHub repo
```

---

### Improvement 4: Lift ArchLucid.Persistence Test Coverage to Per-Package Floor

**Title:** Lift Persistence Layer Test Coverage from ~40% to 63%

**Why it matters:** ArchLucid.Persistence is at ~40% line coverage against a 63% per-package floor target. This is the data access layer for a product whose core value proposition depends on data integrity (manifests, audit events, governance records). Low coverage in the persistence layer means undetected bugs in the most critical data path.

**Expected impact:** Directly improves Correctness (+4-6 pts), Testability (+3-4 pts), Reliability (+2-3 pts), Data Consistency (+2-3 pts). Weighted readiness impact: +0.4-0.6%.

**Affected qualities:** Correctness, Testability, Reliability, Data Consistency

**Status:** Fully actionable now.

**Cursor Prompt:**

```
Increase test coverage for ArchLucid.Persistence and its sub-projects to meet the 63% per-package line coverage floor.

Context:
- ArchLucid.Persistence is at ~40% line coverage vs. the 63% per-package floor enforced by CI.
- The persistence layer handles runs, manifests, governance, alerts, advisory, audit events — the most critical data paths.
- Sub-projects: ArchLucid.Persistence, ArchLucid.Persistence.Advisory, ArchLucid.Persistence.Alerts, ArchLucid.Persistence.Coordination, ArchLucid.Persistence.Integration, ArchLucid.Persistence.Runtime.
- Tests live in ArchLucid.Persistence.Tests.

Tasks:
1. Identify the lowest-coverage classes in each Persistence sub-project by examining the existing coverage report or running:
   dotnet test ArchLucid.Persistence.Tests -c Release --settings coverage.runsettings --collect:"XPlat Code Coverage"

2. For each low-coverage Dapper repository class, add unit tests that cover:
   - Happy path (successful query/insert/update)
   - Null/empty input guards
   - Edge cases (missing records, duplicate keys)
   - Use the existing test patterns in ArchLucid.Persistence.Tests (e.g., mock IDbConnectionFactory)

3. For DbUp migration helpers and schema bootstrap code, add tests for:
   - Successful migration execution
   - Migration ordering validation
   - Schema version stamping

4. Target: every ArchLucid.Persistence* assembly should reach ≥63% line coverage.

5. Run the coverage assertion script to verify:
   python scripts/ci/assert_merged_line_coverage_min.py --min-package-line-pct 63

Files to examine:
- ArchLucid.Persistence/
- ArchLucid.Persistence.Advisory/
- ArchLucid.Persistence.Alerts/
- ArchLucid.Persistence.Coordination/
- ArchLucid.Persistence.Integration/
- ArchLucid.Persistence.Runtime/
- ArchLucid.Persistence.Tests/

Acceptance criteria:
- Every ArchLucid.Persistence* assembly reaches ≥63% line coverage
- All new tests follow existing test patterns and conventions
- No existing tests broken
- Tests do not require a running SQL Server (use mock/stub patterns from existing tests)

Constraints:
- Do NOT change production code to increase coverage (no [ExcludeFromCodeCoverage] additions)
- Do NOT lower the coverage floor
- Follow existing test naming conventions
- Do not use ConfigureAwait(false) in tests
- Each test class must be in its own file
```

---

### Improvement 5: Add DevelopmentBypass Production Guard

**Title:** Add Startup Guard to Prevent DevelopmentBypass in Production

**Why it matters:** If `ASPNETCORE_ENVIRONMENT=Development` is accidentally set in a production deployment, DevelopmentBypass mode authenticates all requests as DevUserId with full permissions — complete authentication bypass. This is a critical security risk with no runtime protection.

**Expected impact:** Directly improves Security (+5-7 pts), Trustworthiness (+2-3 pts), Compliance Readiness (+2-3 pts). Weighted readiness impact: +0.3-0.5%.

**Affected qualities:** Security, Trustworthiness, Compliance Readiness, Reliability

**Status:** Fully actionable now.

**Cursor Prompt:**

```
Add a startup guard that prevents DevelopmentBypass auth mode from being active in production-like environments.

Context:
- ArchLucidAuth mode "DevelopmentBypass" is configured in appsettings.Development.json and authenticates ALL requests as DevUserId with DevRole.
- If ASPNETCORE_ENVIRONMENT=Development is accidentally set in production, all authentication is bypassed.
- No runtime guard prevents this today.

Tasks:
1. In the API startup (ArchLucid.Api or ArchLucid.Host.Composition, wherever auth is configured), add a guard that:
   - Detects when DevelopmentBypass mode is active AND the hosting environment is NOT Development
   - Throws an InvalidOperationException with a clear message: "DevelopmentBypass auth mode is not allowed outside Development environments. Set ArchLucidAuth:Mode to ApiKey or JwtBearer."
   - This check should run during service configuration, before the app starts accepting requests

2. Add a second softer guard:
   - When DevelopmentBypass is active in Development environment, log a Warning: "DevelopmentBypass auth mode is active. All requests are authenticated as {DevUserId}. Do not use in production."

3. Add unit tests in ArchLucid.Host.Composition.Tests:
   - Test that DevelopmentBypass + non-Development environment throws InvalidOperationException
   - Test that DevelopmentBypass + Development environment logs a warning but starts successfully
   - Test that ApiKey and JwtBearer modes are not affected by the guard

Files to examine:
- ArchLucid.Host.Composition/ (auth registration)
- ArchLucid.Api/Program.cs or Startup
- ArchLucid.Host.Composition.Tests/

Acceptance criteria:
- API refuses to start if DevelopmentBypass is configured outside Development environment
- API logs a warning when DevelopmentBypass is active in Development
- ApiKey and JwtBearer modes are unaffected
- At least 3 unit tests cover the guard behavior
- Existing tests pass (they run in Development environment, so DevelopmentBypass continues to work)

Constraints:
- Do NOT change how DevelopmentBypass works in Development environment
- Do NOT change the auth configuration schema
- Do NOT affect any other auth mode
- Keep the guard in the composition/startup layer, not in middleware
```

---

### Improvement 6: DEFERRED — Close First Design-Partner Pilot to Published Reference Customer

**Title:** DEFERRED — Acquire First Published Reference Customer

**Reason it is deferred:** This requires business development activity (identifying a design partner, running a real pilot, obtaining permission to publish a case study) that cannot be executed by code changes. No Cursor prompt can close a customer deal.

**Specific information needed from you later:**
- Which design partner prospects are in the pipeline?
- Is the EXAMPLE_DESIGN_PARTNER row in the reference-customers table a real company or purely fictional?
- What is the timeline for the Aeronova pen-test completion (required by some design partners)?
- Are there any enterprise contacts who have expressed interest in a pilot?

---

### Improvement 7: DEFERRED — Complete Commerce Un-Hold (Stripe Live Keys + Marketplace Publication)

**Title:** DEFERRED — Activate Live Commerce Pipeline

**Reason it is deferred:** Requires owner actions that cannot be performed by an assistant: Partner Center seller verification, tax profile completion, payout account setup, Stripe live key activation. These are gated on legal and financial processes.

**Specific information needed from you later:**
- What is the status of Partner Center seller verification?
- Has the tax profile been submitted?
- Is the payout account configured?
- When should Stripe live keys be flipped (staging first, then production)?

---

### Improvement 8: Add Inline Validation to New-Run Wizard

**Title:** Add Step-Level Inline Validation to New-Run Wizard

**Why it matters:** The new-run wizard currently provides no inline validation feedback on text inputs. Validation errors only appear as post-submit API error callouts. Adding step-level validation before "Next" reduces failed submissions and improves the first-run experience — critical for the Time-to-Value and Adoption Friction qualities.

**Expected impact:** Directly improves Adoption Friction (+2-3 pts), Usability (+2-3 pts), Time-to-Value (+1-2 pts). Weighted readiness impact: +0.2-0.3%.

**Affected qualities:** Adoption Friction, Usability, Time-to-Value, Cognitive Load

**Status:** Fully actionable now.

**Cursor Prompt:**

```
Add inline validation to the new-run wizard in archlucid-ui so that each step validates before allowing "Next."

Context:
- The new-run wizard at /runs/new currently has no inline validation on text inputs.
- Validation errors only surface as post-submit API error callouts.
- The wizard uses react-hook-form and zod (both are already project dependencies).
- The API requires: system name (min 2 chars), environment, cloud provider, and brief (min 10 chars from inputs/brief.md schema).

Tasks:
1. Find the new-run wizard component (likely under archlucid-ui/src/app/(operator)/runs/new/ or components/).

2. Define a zod schema for each wizard step's inputs:
   - Step 1 (system info): systemName required, min 2 chars; environment required; cloudProvider required
   - Step 2 (brief): brief required, min 10 chars
   - Other steps: validate as appropriate based on the existing form fields

3. Wire react-hook-form with zodResolver for each step.

4. Show inline validation errors below each input field using the design system's error styling:
   - Use red text below the input (matching the existing error callout pattern)
   - Show errors when the field is touched and invalid (not on initial render)
   - Clear errors when the user corrects the input

5. Disable the "Next" button when the current step has validation errors.

6. Add vitest tests for the validation schema (zod schema unit tests).

Files to examine:
- archlucid-ui/src/app/(operator)/runs/new/
- archlucid-ui/src/components/ (wizard-related components)

Acceptance criteria:
- Each wizard step validates inline before allowing navigation to the next step
- Validation errors appear below the relevant input field
- "Next" button is disabled when validation fails
- Errors clear when the user corrects input
- Empty required fields show "Required" message on blur
- Min-length fields show character count feedback
- All existing wizard tests pass
- New zod schema tests cover each validation rule

Constraints:
- Do NOT change the API validation behavior
- Do NOT change the wizard step structure or flow
- Use react-hook-form + zod (already dependencies)
- Use existing design system error styling patterns
- Preserve all existing keyboard navigation
```

---

### Improvement 9: DEFERRED — Complete Aeronova Pen Test and Publish Redacted Summary

**Title:** DEFERRED — Complete Pen Test and Publish Redacted Summary

**Reason it is deferred:** The pen-test engagement is with an external vendor (Aeronova Red Team LLC). Scheduling, funding, and NDA-gated publication require owner coordination.

**Specific information needed from you later:**
- What is the current status of the Aeronova engagement?
- Is the SoW funded and scheduled?
- What is the expected timeline for assessment completion?
- What finding categories should be named in the redacted summary (per PENDING_QUESTIONS.md Q11)?

---

### Improvement 10: Create Pre-Built Policy Pack Templates for Common Compliance Frameworks

**Title:** Create SOC 2 and Common Compliance Policy Pack Templates

**Why it matters:** The policy packs feature exists but ships with no pre-built templates. Enterprise buyers adopting governance need to build policy packs from scratch. Shipping 2-3 pre-built templates (SOC 2, cloud-native best practices, architecture review checklist) dramatically reduces time-to-governance-value and differentiates against competitors that offer only empty frameworks.

**Expected impact:** Directly improves Template and Accelerator Richness (+10-15 pts), Time-to-Value (+2-3 pts), Adoption Friction (+1-2 pts). Weighted readiness impact: +0.2-0.3%.

**Affected qualities:** Template and Accelerator Richness, Time-to-Value, Adoption Friction, Differentiability

**Status:** Fully actionable now.

**Cursor Prompt:**

```
Create 3 pre-built policy pack templates that ship with ArchLucid and are available for immediate use in governance workflows.

Context:
- The policy packs feature supports versioned rule sets with scope assignments and effective governance resolution.
- Currently no pre-built templates ship with the product — operators must build from scratch.
- Policy packs are stored in SQL and managed via the API (POST /v1/governance/policy-packs).
- The demo seed (docs/library/demo-quickstart.md) may already create a sample policy pack for Contoso.

Tasks:
1. Review the existing policy pack schema and API to understand the required format:
   - Examine ArchLucid.Decisioning (governance/policy pack models)
   - Examine any existing seed data for policy pack structure

2. Create 3 policy pack template JSON files under a new directory: templates/policy-packs/
   
   a. soc2-architecture-controls.json — SOC 2 Common Criteria mapped to architecture review:
      - CC6.1: Logical access controls (check: network segmentation findings)
      - CC6.6: Boundary protection (check: WAF/firewall findings)
      - CC7.1: Monitoring (check: observability findings)
      - CC8.1: Change management (check: manifest versioning)
      - 8-12 rules total

   b. cloud-architecture-best-practices.json — Cloud-agnostic architecture review:
      - Multi-AZ/region resilience
      - Encryption at rest and in transit
      - Least-privilege IAM
      - Cost optimization signals
      - Disaster recovery posture
      - 10-15 rules total

   c. architecture-review-checklist.json — General architecture review gate:
      - All agent types provided results
      - No critical findings unaddressed
      - Manifest includes cost estimates
      - Compliance checks complete
      - Topology includes HA considerations
      - 6-10 rules total

3. Create a README.md in templates/policy-packs/ explaining:
   - What each template covers
   - How to import via API (POST /v1/governance/policy-packs with the JSON body)
   - How to customize after import
   - That templates are starting points, not compliance certifications

4. If the demo seed already creates a policy pack, ensure the templates are compatible with the same schema.

Acceptance criteria:
- 3 well-structured policy pack template JSON files exist
- Each template is valid against the policy pack API schema
- README explains import process
- Templates use realistic severity thresholds
- Templates are clearly labeled as starting points, not certifications

Constraints:
- Do NOT change the policy pack API or schema
- Do NOT automatically import templates on startup (leave that as an operator choice)
- Do NOT claim these templates provide compliance certification
- Use conservative severity thresholds (warn rather than block by default)
- Follow the existing JSON naming conventions in the codebase
```

---

## 10. Pending Questions for Later

### Improvement 6 — First Published Reference Customer
- Which design partner prospects are currently in the pipeline?
- Is EXAMPLE_DESIGN_PARTNER a real company or purely fictional placeholder?
- What is the realistic timeline for a pilot → case study → publication cycle?

### Improvement 7 — Commerce Un-Hold
- What is the status of Partner Center seller verification?
- Has the tax profile been submitted for Azure Marketplace?
- When should Stripe live keys be flipped (staging first)?
- Is `signup.archlucid.com` DNS under your control?

### Improvement 9 — Pen Test
- What is the current engagement status with Aeronova?
- Is the SoW funded?
- What finding categories should be named in the redacted summary?

### General Architecture
- Is the 53-project decomposition creating real maintenance burden, or is it working well in practice?
- Should the Persistence sub-projects be consolidated, or does the separation enable independent evolution?
- Is the Phase 7 rename (ArchLucid → ArchLucid) purely cosmetic, or does it cause real confusion for contributors?

### Commercial
- What is the current state of the archlucid.com domain and DNS?
- Is there a CRM or sales pipeline tool in use?
- What is the target timeline for first revenue?

---

## Deferred Scope Uncertainty

The following deferred items are referenced in V1_SCOPE.md and V1_DEFERRED.md and were successfully located:
- ITSM connectors (Jira, ServiceNow, Confluence) → V1.1, documented in V1_DEFERRED.md §6
- Slack connector → V2, documented in V1_DEFERRED.md §6a
- MCP server → V1.1, documented in V1_DEFERRED.md §6d
- Commerce un-hold → V1.1, documented in V1_DEFERRED.md §6b (V1_SCOPE.md §3)
- Pen-test publication → V1.1, documented in V1_DEFERRED.md §6c
- PGP key → V1.1, documented in V1_DEFERRED.md §6c
- Phase 7 rename → deferred, documented in ARCHLUCID_RENAME_CHECKLIST.md
- Product learning "brains" → deferred, documented in V1_DEFERRED.md §1
- Multi-region active/active → not V1, documented in V1_SCOPE.md §3

All deferred items were located in source materials. No deferred scope uncertainty exists.
