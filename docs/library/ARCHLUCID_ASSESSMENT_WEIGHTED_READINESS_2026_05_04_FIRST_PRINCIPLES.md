> **Scope:** Independent first-principles weighted readiness assessment (2026-05-04) — scores, weights, composite %, ordered findings, and improvement prompts; not an implementation contract and not prior-assessment continuation.

# ArchLucid Assessment – Weighted Readiness 70.21%

**Method:** Each quality scored 1–100. Weighted composite = Σ(Score × Weight) ÷ Σ(Weights). **Σ(Weights) = 102** per the supplied model. **Weighted impact** on the composite = (Score × Weight) ÷ 102 (percentage points toward the 0–100 headline). **Weighted deficiency signal** = Weight × (100 − Score) (higher = more urgent given weight).

**Headline scope rule:** Items explicitly deferred to **V1.1** or **V2** in [`V1_SCOPE.md`](V1_SCOPE.md) / [`V1_DEFERRED.md`](V1_DEFERRED.md) (e.g. first-party Jira/ServiceNow/Slack, MCP membrane, live Stripe/Marketplace un-hold, signed design partner, CPA SOC 2, third-party pen-test publication, PGP key drop) **do not reduce** the scores below—they are treated as out-of-scope for V1 headline readiness unless they materially block what V1 claims today.

---

## 1. Title

**ArchLucid Assessment – Weighted Readiness 70.21%**

---

## 2. Executive Summary

### Overall readiness

ArchLucid is **engineering-strong and documentation-strong** for a complex B2B workflow product: bounded contexts, merge-blocking API/UI gates, SQL-first persistence with RLS directionally correct, rich observability and audit narrative, and an honest Trust Center. The **weighted composite is 70.21%**, dragged most by **commercial adoption and cognitive load** (broad surface area, heavy evaluator mental model) and by **residual security/scalability edges** (RLS not universal on all child tables; tenant health-score refresh is **O(tenants × queries)** in code). V1 is **credible as a sales-led pilot platform**; it is **not yet frictionless** for self-serve expansion or lowest-touch enterprise procurement without human assist.

### Commercial picture

The **Pilot → Operate** story is coherent ([`PRODUCT_PACKAGING.md`](PRODUCT_PACKAGING.md), [`EXECUTIVE_SPONSOR_BRIEF.md`](../EXECUTIVE_SPONSOR_BRIEF.md)), and **time-to-first-manifest** is intentionally shortened via simulator defaults and Core Pilot ([`CORE_PILOT.md`](../CORE_PILOT.md)). **Proof-of-ROI** is partially systematized ([`PILOT_ROI_MODEL.md`](PILOT_ROI_MODEL.md), first-value reports) but still depends on human baselines for several sponsor claims. **Differentiability** in-market is plausible (manifest + governance + audit + Azure-native posture) but **not self-evident** to a cold buyer without guided narrative. **Self-serve Team Stripe CTA** and live commerce posture remain intentionally guarded ([`TRIAL_AND_SIGNUP.md`](../go-to-market/TRIAL_AND_SIGNUP.md), [`V1_DEFERRED.md`](V1_DEFERRED.md) §6b)—**not scored as a V1 deficit** per scope contract, but it **still shapes real revenue timing** outside headline readiness.

### Enterprise picture

**Auditability, policy/governance, and traceability** are above median versus typical early-stage vendors ([`AUDIT_COVERAGE_MATRIX.md`](AUDIT_COVERAGE_MATRIX.md), [`V1_SCOPE.md`](V1_SCOPE.md) §2.9–2.12). **Trust** is honest: SOC 2 is **self-assessed + roadmap**, not CPA-attested ([`TRUST_CENTER.md`](../go-to-market/TRUST_CENTER.md))—**procurement friction** remains predictable for strict RFP shops **without** treating that as a V1 headline deduction per deferral rules. **RLS** is real on many authority tables but **explicitly incomplete** on legacy/coordinator child tables ([`MULTI_TENANT_RLS.md`](../security/MULTI_TENANT_RLS.md) §9), which matters for **security reviewer psychology** even when application-layer scoping is correct.

### Engineering picture

**Testability and deployability** are standout: `Suite=Core` corset, **merge-blocking live Playwright** against SQL-backed API ([`TEST_STRUCTURE.md`](TEST_STRUCTURE.md)), release smoke discipline ([`RELEASE_SMOKE.md`](RELEASE_SMOKE.md)), k6 gates ([`API_SLOS.md`](API_SLOS.md)), OpenAPI snapshot enforcement, and Terraform roots with a canonical pilot orchestration path ([`REFERENCE_SAAS_STACK_ORDER.md`](REFERENCE_SAAS_STACK_ORDER.md)). **Scalability** has a concrete hot spot: `RefreshAllTenantHealthScoresAsync` loops all tenants and issues **multiple COUNT queries per tenant** ([`SqlTenantCustomerSuccessRepository.cs`](../../ArchLucid.Persistence/CustomerSuccess/SqlTenantCustomerSuccessRepository.cs)). **Defense-in-depth for stickiness reads** relies on explicit `WHERE TenantId = …` without applying `IRlsSessionContextApplicator` ([`SqlOperatorStickinessSnapshotReader.cs`](../../ArchLucid.Persistence/CustomerSuccess/SqlOperatorStickinessSnapshotReader.cs)), which is acceptable only while every path is provably parameterized—**RLS application would reduce reviewer fear**.

### Deferred Scope Uncertainty

**No material uncertainty:** deferred items cited here are locatable in [`V1_DEFERRED.md`](V1_DEFERRED.md) and [`V1_SCOPE.md`](V1_SCOPE.md) §3.

---

## 3. Weighted Quality Assessment

**Ordering:** most urgent → least urgent by **weighted deficiency signal** = Weight × (100 − Score).

For each quality: **Score** | **Weight** | **Weighted deficiency signal** | **Weighted impact** = (Score × Weight) ÷ 102 | **Justification** | **Tradeoffs** | **Improvement recommendations** | **Fix horizon**

---

### Adoption Friction — Score **52** · Weight **6** · Deficiency **288** · Impact **3.06**

- **Justification:** The product packs **Pilot + Operate** with large API/UI surfaces ([`PRODUCT_PACKAGING.md`](PRODUCT_PACKAGING.md)); progressive disclosure helps but **onboarding still requires** identity, tenancy, SQL in non-SaaS installs, and literacy in “run vs architecture review” vocabulary ([`README.md`](../../README.md), [`CORE_PILOT.md`](../CORE_PILOT.md)). Evaluators face **tooling + domain** load before sponsor-safe proof exists.
- **Tradeoffs:** Narrowing the default UX further improves friction but can **hide** governance value that closes Enterprise deals later.
- **Improvements:** Single **Day-0 checklist** in-product linking to Core Pilot-only scope; tighten **empty states** to one recommended path; keep Operate behind disclosure but add **“why hidden”** microcopy once ([`OPERATOR_DECISION_GUIDE.md`](OPERATOR_DECISION_GUIDE.md)).
- **Fix horizon:** **V1** (mostly UI/docs).

### Marketability — Score **69** · Weight **8** · Deficiency **248** · Impact **5.41**

- **Justification:** Strong **internal narrative consistency** (sponsor brief, packaging, trust index) but **cold outbound marketability** still depends on demos and procurement storytelling; category is noisy (AI + architecture). Pricing/discount stack is explicit ([`PRICING_PHILOSOPHY.md`](../go-to-market/PRICING_PHILOSOPHY.md))—helpful honesty, not mass-market packaging.
- **Tradeoffs:** Broader claims increase pipeline but **violate** the sponsor brief’s non-overclaim stance ([`EXECUTIVE_SPONSOR_BRIEF.md`](../EXECUTIVE_SPONSOR_BRIEF.md) §7).
- **Improvements:** Ship **one vertical-specific proof page** (reuse marketing pack endpoints per brief) with **customer-safe evidence rules** ([`PILOT_ROI_MODEL.md`](PILOT_ROI_MODEL.md) demo banner discipline).
- **Fix horizon:** **V1** (GTM assets), deeper vertical truth **V1.1+**.

### Proof-of-ROI Readiness — Score **66** · Weight **5** · Deficiency **170** · Impact **3.24**

- **Justification:** Computed deltas for time-to-manifest, findings, LLM calls, audit row counts exist ([`PILOT_ROI_MODEL.md`](PILOT_ROI_MODEL.md) §4.1), plus “buyer-safe proof package contract.” Several **high-value measures remain operator-qualitative** (manual prep reduction, decision trace completeness).
- **Tradeoffs:** More auto-metrics increase **PII/process capture** risk and implementation cost.
- **Improvements:** Add **structured pilot closure fields** (optional JSON) captured at run/archive time; map to scorecard + value report sections; keep **explicit confidence** labeling ([`PILOT_ROI_MODEL.md`](PILOT_ROI_MODEL.md) §4.1.1).
- **Fix horizon:** **V1** for skeleton; maturity **V1.1**.

### Time-to-Value — Score **76** · Weight **7** · Deficiency **168** · Impact **5.22**

- **Justification:** Core Pilot is intentionally **one-session capable** with simulator defaults ([`CORE_PILOT.md`](../CORE_PILOT.md)); hosted trial targets **< 5 minutes** ([`TRIAL_AND_SIGNUP.md`](../go-to-market/TRIAL_AND_SIGNUP.md)). Real LLM path adds latency/cost gates (Tier 3 SLO) ([`API_SLOS.md`](API_SLOS.md)).
- **Tradeoffs:** Faster demos push simulator-first; **real-mode** raises TTV and support burden.
- **Improvements:** Instrument **median wall-clock** from signup → first committed manifest (already partially funnel-metrics in observability docs—tighten dashboard + alert).
- **Fix horizon:** **V1**.

### Differentiability — Score **58** · Weight **4** · Deficiency **168** · Impact **2.27**

- **Justification:** Differentiation is **credible but not automatic**: governance + append-only audit + deterministic packaging is rare early; still must explain **why not Copilot + Confluence + Jira** without sounding generic.
- **Tradeoffs:** Sharper positioning shrinks TAM on paper.
- **Improvements:** Publish **comparison narratives** with evidence links (already pointed to in sponsor brief)—ensure **/why** and pack PDF stay **current with V1_SCOPE** claims.
- **Fix horizon:** **V1** (content), continuous.

### Workflow Embeddedness — Score **58** · Weight **3** · Deficiency **126** · Impact **1.71**

- **Justification:** Strong **Microsoft-native** path (Teams notifications, GitHub/ADO templates per SaaS stack doc); first-party **Jira/ServiceNow/Slack** bridges are **V1.1/V2** per [`V1_DEFERRED.md`](V1_DEFERRED.md) **(not a headline deduction)**—still means **embedded ITSM** is often **customer-built** in V1.
- **Tradeoffs:** First-party connectors reduce services load but explode **long-tail** requirements.
- **Improvements:** Promote **Power Automate / webhook recipes** as default enterprise path ([`V1_DEFERRED.md`](V1_DEFERRED.md) V1 customer-owned bridges).
- **Fix horizon:** **V1** (docs/templates); connectors **V1.1+** per scope.

### Executive Value Visibility — Score **70** · Weight **4** · Deficiency **120** · Impact **2.75**

- **Justification:** Sponsor PDF/DOCX paths exist (brief §One-shot sponsor PDF; value report references). Clarity depends on tenant data quality and avoiding demo-run leakage ([`PILOT_ROI_MODEL.md`](PILOT_ROI_MODEL.md) redaction banner).
- **Tradeoffs:** More exec dashboards pull focus from **Pilot wedge**.
- **Improvements:** Standardize **one executive artifact** per pilot (template + mandatory fields checklist) stored with run id + manifest version.
- **Fix horizon:** **V1**.

### Usability — Score **62** · Weight **3** · Deficiency **114** · Impact **1.82**

- **Justification:** Progressive disclosure and authority seams are **well-tested** ([`PRODUCT_PACKAGING.md`](PRODUCT_PACKAGING.md) §Contributor drift guard), but **Operate** still introduces parallel mental models (analysis vs governance).
- **Tradeoffs:** Simplifying UI risks obscuring **required** governance controls for some buyers.
- **Improvements:** Page-level **“Pilot-only mode”** preset hiding extended/advanced nav; validate with **task completion metrics** (existing checklist rail is a start—extend to completion % not just counters).
- **Fix horizon:** **V1**.

### Correctness — Score **73** · Weight **4** · Deficiency **108** · Impact **2.86**

- **Justification:** Broad automated coverage and honest limits on UI/API parity ([`RELEASE_SMOKE.md`](RELEASE_SMOKE.md)); audit matrix shows **catalogued gaps** (e.g. `ManifestSuperseded` unused) ([`AUDIT_COVERAGE_MATRIX.md`](AUDIT_COVERAGE_MATRIX.md) Known gaps). LLM outputs are inherently non-deterministic; mitigations exist (schema checks, quality gate metrics).
- **Tradeoffs:** Hard reject gates reduce false positives but can **block** useful outputs.
- **Improvements:** Expand **golden cohort** scenarios for authority/commit idempotency; keep simulator vs real-mode parity tests explicit ([`V1_SCOPE.md`](V1_SCOPE.md) §3).
- **Fix horizon:** **V1** continuous.

### Security — Score **68** · Weight **3** · Deficiency **96** · Impact **2.00**

- **Justification:** RLS + session context design is serious ([`MULTI_TENANT_RLS.md`](../security/MULTI_TENANT_RLS.md)); **known uncovered tables** documented (defense-in-depth gap vs SQL injection classes). Operational risk: **ApplySessionContext=false** default until enabled ([`MULTI_TENANT_RLS.md`](../security/MULTI_TENANT_RLS.md) §8)—**deployment-dependent** posture.
- **Tradeoffs:** Turning RLS **ON** without full coverage can break legacy paths; full denormalization is costly.
- **Improvements:** Prioritize **high-risk child tables** for scope denorm + policy attachment; ensure **stickiness reads** apply session context (see Engineering risks).
- **Fix horizon:** **V1** for highest-risk; full coverage multi-phase.

### Trustworthiness — Score **70** · Weight **3** · Deficiency **90** · Impact **2.06**

- **Justification:** Honest about LLM limits ([`EXECUTIVE_SPONSOR_BRIEF.md`](../EXECUTIVE_SPONSOR_BRIEF.md) §9). Strong durable audit story; CPA SOC2 explicitly deferred ([`V1_DEFERRED.md`](V1_DEFERRED.md) §6c)—**procurement realism**, not V1 headline deduction per rules.
- **Tradeoffs:** More third-party attestations increase cost and cycle time.
- **Improvements:** Keep **assessment + pen-test posture** aligned to Trust Center rows; ensure “owner-conducted” window closes with published internal summary pointers.
- **Fix horizon:** **V1** narrative; CPA **post–V1.1** per docs.

### Architectural Integrity — Score **76** · Weight **3** · Deficiency **72** · Impact **2.24**

- **Justification:** Clear container map and ownership ([`ARCHITECTURE_ON_ONE_PAGE.md`](../ARCHITECTURE_ON_ONE_PAGE.md Coordinator↔Authority direction in ADRs referenced across docs). Some **historical dual paths** still require maintainer discipline ([`V1_SCOPE.md`](V1_SCOPE.md) §3 coordinator strangler note).
- **Tradeoffs:** Faster shipping preserved legacy surfaces; tightening removes flexibility.
- **Improvements:** Enforce **ADR 0021** gate in code review checklist; keep **unified read façade** narrative executable.
- **Fix horizon:** **V1** maintenance; major cuts **V1.1+**.

### Decision Velocity — Score **68** · Weight **2** · Deficiency **64** · Impact **1.33**

- **Justification:** Procurement pack CLI + indices accelerate diligence ([`TRUST_CENTER.md`](../go-to-market/TRUST_CENTER.md)); quote-request path exists ([`PRICING_PHILOSOPHY.md`](../go-to-market/PRICING_PHILOSOPHY.md)).
- **Tradeoffs:** Automating legal/procurement beyond templates creates liability.
- **Improvements:** Pre-fill **CAIQ/SIG** hotter paths from actual CI artifacts; shorten “first answer pack” to **10 pages max** executive summary.
- **Fix horizon:** **V1**.

### Procurement Readiness — Score **70** · Weight **2** · Deficiency **60** · Impact **1.37**

- **Justification:** Strong artifact index ([`PROCUREMENT_PACK_INDEX.md`](../go-to-market/PROCUREMENT_PACK_INDEX.md)); gaps are predictable (no CPA SOC2 **report**).
- **Tradeoffs:** Over-claiming certifications backfires in diligence.
- **Improvements:** Maintain **single canonical assurance wording** file links fresh; add **objection playbook** links on Trust Center rows.
- **Fix horizon:** **V1** docs; CPA **later**.

### Data Consistency — Score **71** · Weight **2** · Deficiency **58** · Impact **1.39**

- **Justification:** Orphan probes + metrics + optional quarantine ([`DATA_CONSISTENCY_ENFORCEMENT.md`](../data-consistency/DATA_CONSISTENCY_ENFORCEMENT.md), [`OBSERVABILITY.md`](OBSERVABILITY.md)); comparison records FK-backed per matrix notes.
- **Tradeoffs:** Quarantine mode creates operational debt if unmanned.
- **Improvements:** Tie **dashboards** to `archlucid_data_consistency_*` metrics with **runbook** links; rehearse quarterly ops review.
- **Fix horizon:** **V1**.

### AI/Agent Readiness — Score **71** · Weight **2** · Deficiency **58** · Impact **1.39**

- **Justification:** Circuit breakers, trace persistence, quality gate metrics ([`OBSERVABILITY.md`](OBSERVABILITY.md)); simulator path strong. Agent ecosystem membrane **MCP** is **V1.1** ([`V1_DEFERRED.md`](V1_DEFERRED.md) §6d)—not a V1 deduction.
- **Tradeoffs:** More autonomy increases **governance + spend** risk.
- **Improvements:** Document **allowed real-mode** baselines per tenant; keep monthly **$ guardrails** visible in support bundles ([`PRICING_PHILOSOPHY.md`](../go-to-market/PRICING_PHILOSOPHY.md) hosted AOAI band).
- **Fix horizon:** **V1** ops.

### Commercial Packaging Readiness — Score **72** · Weight **2** · Deficiency **56** · Impact **1.41**

- **Justification:** Tier gates exist (`[RequiresCommercialTenantTier]` narrative in [`PRODUCT_PACKAGING.md`](PRODUCT_PACKAGING.md)); **live commerce un-hold V1.1** per [`V1_DEFERRED.md`](V1_DEFERRED.md) §6b—**not** subtracted from V1 headline here. Narrative tension: packaging section also describes **soft** commercial boundaries historically—verify buyer-facing copy matches **actual HTTP gates**.
- **Tradeoffs:** Harder gates reduce exploratory usage; softer gates confuse procurement.
- **Improvements:** Add **internal matrix**: route → tier gate → policy → nav disclosure (single table).
- **Fix horizon:** **V1**.

### Explainability — Score **72** · Weight **2** · Deficiency **56** · Impact **1.41**

- **Justification:** Citations + faithfulness metrics exist ([`OBSERVABILITY.md`](OBSERVABILITY.md)); sponsor brief states limits ([`EXECUTIVE_SPONSOR_BRIEF.md`](../EXECUTIVE_SPONSOR_BRIEF.md) §9).
- **Tradeoffs:** More explanation UI increases **cognitive load**.
- **Improvements:** Require **minimum citation coverage** before sponsor PDF send (UI guard + server validation).
- **Fix horizon:** **V1**.

### Traceability — Score **82** · Weight **3** · Deficiency **54** · Impact **2.41**

- **Justification:** Typed audit catalog + CI anchors ([`AUDIT_COVERAGE_MATRIX.md`](AUDIT_COVERAGE_MATRIX.md)); correlation IDs standard ([`README.md`](../../README.md)).
- **Tradeoffs:** Durable audit failures are fire-and-forget on some hot paths—by design—can surprise investigators.
- **Improvements:** Monitor **`archlucid_audit_write_failures_total`** in prod dashboards as Tier-1 signal ([`AUDIT_COVERAGE_MATRIX.md`](AUDIT_COVERAGE_MATRIX.md) notes).
- **Fix horizon:** **V1** ops.

### Maintainability — Score **73** · Weight **2** · Deficiency **54** · Impact **1.43**

- **Justification:** Modular assemblies, explicit test tiers; docs are extensive (also a burden).
- **Tradeoffs:** High doc surface can drift without CI guards (many guards already exist).
- **Improvements:** Continue **doc scope header** enforcement; reduce duplicate narratives between README and library by linking not copying.
- **Fix horizon:** **V1**.

### Compliance Readiness — Score **74** · Weight **2** · Deficiency **52** · Impact **1.45**

- **Justification:** CAIQ/SIG templates, DPA template, DSAR process indexed ([`TRUST_CENTER.md`](../go-to-market/TRUST_CENTER.md)).
- **Tradeoffs:** Template ≠ executed agreement; customers still run legal review.
- **Improvements:** Add **data map** pointers from Trust Center to **PII retention** docs for Ask/conversations.
- **Fix horizon:** **V1** docs.

### Cognitive Load — Score **52** · Weight **1** · Deficiency **48** · Impact **0.51**

- **Justification:** Even with packaging discipline, the **capability inventory** is large ([`PRODUCT_PACKAGING.md`](PRODUCT_PACKAGING.md)); new operators must learn **run IDs**, tiers, authority ranks, and optional governance.
- **Tradeoffs:** Aggressive hiding can starve advanced users.
- **Improvements:** **Role-based default presets** + **“first session”** guided path only (hide everything else).
- **Fix horizon:** **V1** UI.

### Interoperability — Score **77** · Weight **2** · Deficiency **46** · Impact **1.51**

- **Justification:** Versioned REST, CLI tool, integration events + AsyncAPI ([`README.md`](../../README.md)), SCIM surface in scope ([`V1_SCOPE.md`](V1_SCOPE.md) §2.12). First-party ITSM beyond webhooks deferred (scope).
- **Tradeoffs:** Every new connector increases CVE/support surface.
- **Improvements:** Publish **minimum automation recipes** for top 3 enterprise patterns (event → ticket, export → SharePoint) using existing contracts.
- **Fix horizon:** **V1** recipes; connectors **V1.1+**.

### Reliability — Score **77** · Weight **2** · Deficiency **46** · Impact **1.51**

- **Justification:** SLOs, synthetic probes story, k6 CI smoke ([`API_SLOS.md`](API_SLOS.md)); hosted probes optional via repo variables ([`REFERENCE_SAAS_STACK_ORDER.md`](REFERENCE_SAAS_STACK_ORDER.md)).
- **Tradeoffs:** Strong gates slow iteration if environments drift.
- **Improvements:** Ensure **production** enables external synthetic secrets consistently (operational, not code).
- **Fix horizon:** **V1** ops.

### Scalability — Score **54** · Weight **1** · Deficiency **46** · Impact **0.53**

- **Justification:** Scheduler path `RefreshAllTenantHealthScoresAsync` is **sequential multi-query per tenant** ([`SqlTenantCustomerSuccessRepository.cs`](../../ArchLucid.Persistence/CustomerSuccess/SqlTenantCustomerSuccessRepository.cs)); design doc already flags O(N·Q) risk ([`CUSTOMER_SUCCESS_PERSISTENCE_DESIGN.md`](CUSTOMER_SUCCESS_PERSISTENCE_DESIGN.md)).
- **Tradeoffs:** Batch SQL moves logic into DB (harder to unit-test) but reduces tail latency and cloud DB round trips.
- **Improvements:** Single **set-based refresh** stored procedure + one call from host.
- **Fix horizon:** **V1** (before multi-hundred-tenant production scale).

### Policy and Governance Alignment — Score **78** · Weight **2** · Deficiency **44** · Impact **1.53**

- **Justification:** Pre-commit gate, approvals, policy packs ([`V1_SCOPE.md`](V1_SCOPE.md)); good fit for regulated architecture practice.
- **Tradeoffs:** Heavy policy can stall pilots—keep default **off** for first session ([`CORE_PILOT.md`](../CORE_PILOT.md)).
- **Improvements:** **Simulation endpoints** already exist—bundle a **default policy starter pack** for Professional tier trials.
- **Fix horizon:** **V1** packaging.

### Customer Self-Sufficiency — Score **58** · Weight **1** · Deficiency **42** · Impact **0.57**

- **Justification:** Docs are deep; **contributor** vs **buyer** paths are separated ([`README.md`](../../README.md))—good—but pilots still ping humans for SQL/auth edge cases in self-hosted modes.
- **Tradeoffs:** More self-help increases doc maintenance.
- **Improvements:** Add **troubleshooting decision tree** to `docs/TROUBLESHOOTING.md` keyed to `release-smoke` triage blocks.
- **Fix horizon:** **V1**.

### Azure Compatibility and SaaS Deployment Readiness — Score **81** · Weight **2** · Deficiency **38** · Impact **1.59**

- **Justification:** Terraform ordering for private SQL, messaging, Container Apps, Front Door ([`REFERENCE_SAAS_STACK_ORDER.md`](REFERENCE_SAAS_STACK_ORDER.md)); `appsettings.SaaS.json` pattern exists.
- **Tradeoffs:** Multi-root Terraform increases operational overhead vs pilot root.
- **Improvements:** Add **Bicep/Terraform cross-reference** only where required—avoid dual truth; prefer single entry `terraform-pilot` outputs for sequencing.
- **Fix horizon:** **V1** docs.

### Template and Accelerator Richness — Score **66** · Weight **1** · Deficiency **34** · Impact **0.65**

- **Justification:** Templates exist (`templates/`, GitHub/ADO integrations referenced); not yet “marketplace of accelerators.”
- **Tradeoffs:** Too many templates without CI = stale assets.
- **Improvements:** Curate **5** vertical brief starters with smoke-tested `second-run` compatibility ([`CORE_PILOT.md`](../CORE_PILOT.md) Step 5).
- **Fix horizon:** **V1** incremental.

### Stickiness — Score **68** · Weight **1** · Deficiency **32** · Impact **0.67**

- **Justification:** Manifest + audit + exports create switching costs; health scoring intended ([`CUSTOMER_SUCCESS_PERSISTENCE_DESIGN.md`](CUSTOMER_SUCCESS_PERSISTENCE_DESIGN.md)).
- **Tradeoffs:** Stickiness via data lock-in can trigger **procurement fear**—pair with export + DPA clarity.
- **Improvements:** Make **export bundles** a one-click Core Pilot completion banner action.
- **Fix horizon:** **V1**.

### Accessibility — Score **68** · Weight **1** · Deficiency **32** · Impact **0.67**

- **Justification:** Trust Center row + live axe routes in CI ([`TEST_STRUCTURE.md`](TEST_STRUCTURE.md), [`TRUST_CENTER.md`](../go-to-market/TRUST_CENTER.md)).
- **Tradeoffs:** Full WCAG on every advanced admin page is expensive.
- **Improvements:** Expand **axe** coverage to top 5 revenue routes (trial signup, pricing, operator home).
- **Fix horizon:** **V1** test expansion.

### Performance — Score **71** · Weight **1** · Deficiency **29** · Impact **0.70**

- **Justification:** k6 thresholds; named-query allowlist discipline ([`OBSERVABILITY.md`](OBSERVABILITY.md), [`TEST_STRUCTURE.md`](TEST_STRUCTURE.md)).
- **Tradeoffs:** Strict perf gates can flake in shared CI SQL.
- **Improvements:** Add **dashboard** for top 10 slow queries by `query_name` histogram.
- **Fix horizon:** **V1** ops.

### Cost-Effectiveness — Score **71** · Weight **1** · Deficiency **29** · Impact **0.70**

- **Justification:** LLM budget warnings + hosted planning band ([`PRICING_PHILOSOPHY.md`](../go-to-market/PRICING_PHILOSOPHY.md) §3.2); simulator-first reduces burn.
- **Tradeoffs:** Hard spend caps anger power users during spikes.
- **Improvements:** Tenant-facing **budget dial** UI (read-only first) surfacing month-to-date estimated USD.
- **Fix horizon:** **V1** small.

### Auditability — Score **86** · Weight **2** · Deficiency **28** · Impact **1.69**

- **Justification:** Append-only + deny migration path ([`AUDIT_COVERAGE_MATRIX.md`](AUDIT_COVERAGE_MATRIX.md)); strong CI pairing philosophy.
- **Tradeoffs:** UX latency if audit writes ever become awaited broadly (currently mitigated).
- **Improvements:** Close **catalogue-only** gaps when features land (`ManifestSuperseded`) to prevent stakeholder confusion.
- **Fix horizon:** **V1** when supersession ships.

### Change Impact Clarity — Score **72** · Weight **1** · Deficiency **28** · Impact **0.71**

- **Justification:** `CHANGELOG.md`, breaking changes doc referenced from README; OpenAPI snapshots catch HTTP drift.
- **Tradeoffs:** Faster iteration sometimes outruns buyer-facing breaking-change narrative.
- **Improvements:** Add **“Pilot impact”** subsection to CHANGELOG for operator-visible changes only.
- **Fix horizon:** **V1** process.

### Manageability — Score **73** · Weight **1** · Deficiency **27** · Impact **0.72**

- **Justification:** Many knobs documented (`CONFIGURATION_REFERENCE` linked across docs); production safety rules for billing ([`V1_DEFERRED.md`](V1_DEFERRED.md) §6b references).
- **Tradeoffs:** More runtime flags increase misconfiguration risk; partially mitigated by startup warnings metrics ([`OBSERVABILITY.md`](OBSERVABILITY.md)).
- **Improvements:** Group **SaaS profile** knobs into one table in `appsettings.SaaS.json` docs page.
- **Fix horizon:** **V1**.

### Evolvability — Score **73** · Weight **1** · Deficiency **27** · Impact **0.72**

- **Justification:** ADRs + strangler plan; backlog references ([`V1_SCOPE.md`](V1_SCOPE.md)).
- **Tradeoffs:** Long-lived dual paths increase cognitive load for contributors.
- **Improvements:** Time-box **Phase-7 rename** items explicitly when program ready ([`V1_DEFERRED.md`](V1_DEFERRED.md) §3).
- **Fix horizon:** **V1.1+** execution, **V1** planning only.

### Availability — Score **74** · Weight **1** · Deficiency **26** · Impact **0.73**

- **Justification:** Targets and probe philosophy documented ([`API_SLOS.md`](API_SLOS.md)); staging chaos on calendar ([`TRUST_CENTER.md`](../go-to-market/TRUST_CENTER.md)).
- **Tradeoffs:** Production chaos remains explicitly gated—limits learning.
- **Improvements:** Game-day reports linked from status communications policy for buyer confidence.
- **Fix horizon:** **V1** ops narrative.

### Extensibility — Score **75** · Weight **1** · Deficiency **25** · Impact **0.74**

- **Justification:** Webhooks/events, policy packs, retrieval hooks—good extension seams.
- **Tradeoffs:** Extension without versioning breaks consumers; OpenAPI snapshot mitigates.
- **Improvements:** Publish **semver** guidance for JSON integration payloads alongside AsyncAPI.
- **Fix horizon:** **V1** docs.

### Deployability — Score **77** · Weight **1** · Deficiency **23** · Impact **0.75**

- **Justification:** DbUp on start, compose, CD verify script ([`RELEASE_SMOKE.md`](RELEASE_SMOKE.md) CD section); greenfield boot tests ([`TEST_STRUCTURE.md`](TEST_STRUCTURE.md)).
- **Tradeoffs:** Auto-migrate on startup can surprise operators—documented as fail-closed.
- **Improvements:** Add **rollback play** to CD workflow docs referencing revision deactivation ([`RELEASE_SMOKE.md`](RELEASE_SMOKE.md)).
- **Fix horizon:** **V1** runbooks.

### Supportability — Score **79** · Weight **1** · Deficiency **21** · Impact **0.77**

- **Justification:** `doctor`, support bundle, correlation headers ([`README.md`](../../README.md)); triage blocks in smoke scripts.
- **Tradeoffs:** Support bundles risk sensitive data—redaction discipline required (recent changelog notes on redaction).
- **Improvements:** Automated **bundle size limits** + secret scanner preflight locally.
- **Fix horizon:** **V1**.

### Observability — Score **81** · Weight **1** · Deficiency **19** · Impact **0.79**

- **Justification:** Broad metric catalog + Grafana JSON in-repo ([`OBSERVABILITY.md`](OBSERVABILITY.md)).
- **Tradeoffs:** Exporters off by default in dev—prod misconfig can blind teams ([`OBSERVABILITY.md`](OBSERVABILITY.md) §Export path).
- **Improvements:** Add **terraform default** enabling Azure Monitor in SaaS profile with cost caps.
- **Fix horizon:** **V1** IaC.

### Modularity — Score **81** · Weight **1** · Deficiency **19** · Impact **0.79**

- **Justification:** Clean split Api/Application/Persistence/UI; contracts assembly exists.
- **Tradeoffs:** Project count raises build graph complexity—acceptable at current scale.
- **Improvements:** Enforce **dependency direction** tests in `ArchLucid.Architecture.Tests` when new top-level packages added.
- **Fix horizon:** **V1**.

### Azure Ecosystem Fit — Score **82** · Weight **1** · Deficiency **18** · Impact **0.80**

- **Justification:** Entra, AOAI, Service Bus optional, Key Vault references ([`TRUST_CENTER.md`](../go-to-market/TRUST_CENTER.md), [`ARCHITECTURE_ON_ONE_PAGE.md`](../ARCHITECTURE_ON_ONE_PAGE.md)).
- **Tradeoffs:** Tight coupling to Azure can stall non-Azure prospects—acceptable given stated strategy.
- **Improvements:** Document **BYO model** boundaries without implying unsupported deployments.
- **Fix horizon:** **V1** docs.

### Documentation — Score **83** · Weight **1** · Deficiency **17** · Impact **0.81**

- **Justification:** Exceptionally complete; CI guards on headers and pricing single-source ([`PRICING_PHILOSOPHY.md`](../go-to-market/PRICING_PHILOSOPHY.md)).
- **Tradeoffs:** Volume can obscure the **5-doc spine**—discipline required ([`README.md`](../../README.md)).
- **Improvements:** Quarterly **doc inventory** pruning pass automated via link checker job.
- **Fix horizon:** **V1** hygiene.

### Testability — Score **85** · Weight **1** · Deficiency **15** · Impact **0.83**

- **Justification:** Core corset + live E2E + k6 + scheduled security workflows ([`TEST_STRUCTURE.md`](TEST_STRUCTURE.md)).
- **Tradeoffs:** Live suites increase flake cost; mitigated by separation from mock Playwright ([`RELEASE_SMOKE.md`](RELEASE_SMOKE.md)).
- **Improvements:** Keep **one** canonical “merge blocking list” graphic in `TEST_EXECUTION_MODEL` linked from README only once.
- **Fix horizon:** **V1** maintenance.

---

## 4. Top 10 Most Important Weaknesses (Cross-Cutting)

1. **Pilot friction vs surface-area grandeur** — packaging helps, but the **product is still “big”** relative to first-session success ([`PRODUCT_PACKAGING.md`](PRODUCT_PACKAGING.md), [`CORE_PILOT.md`](../CORE_PILOT.md)).
2. **Category + buyer proof** — differentiation is real but **not automatic**; cold inbound needs **verticalized, customer-permissioned** receipts ([`EXECUTIVE_SPONSOR_BRIEF.md`](../EXECUTIVE_SPONSOR_BRIEF.md), [`PILOT_ROI_MODEL.md`](PILOT_ROI_MODEL.md) demo honesty rules).
3. **RLS coverage gaps** — documented uncovered tables **increase diligence effort** even when app authZ is correct ([`MULTI_TENANT_RLS.md`](../security/MULTI_TENANT_RLS.md) §9).
4. **Customer success scalability** — tenant health refresh pattern is **operationally fragile at scale** ([`SqlTenantCustomerSuccessRepository.cs`](../../ArchLucid.Persistence/CustomerSuccess/SqlTenantCustomerSuccessRepository.cs)).
5. **Defense-in-depth on stickiness SQL** — reader skips explicit session-context application ([`SqlOperatorStickinessSnapshotReader.cs`](../../ArchLucid.Persistence/CustomerSuccess/SqlOperatorStickinessSnapshotReader.cs)); safe only if every future edit preserves tenant predicates.
6. **LLM-dependent perception** — correct technical mitigations exist, but **buyer trust** still requires human-readable “citations required” discipline ([`EXECUTIVE_SPONSOR_BRIEF.md`](../EXECUTIVE_SPONSOR_BRIEF.md) §9).
7. **Observability exporter dependency** — metrics exist in-process but require correct **Azure Monitor/OTLP/Prometheus** wiring in prod ([`OBSERVABILITY.md`](OBSERVABILITY.md)).
8. **Dual-path architecture history** — strangler pattern is healthy but demands **ongoing vigilance** ([`V1_SCOPE.md`](V1_SCOPE.md) §3).
9. **Procurement pace vs product honesty** — Trust Center is strong for **integrity** but **slow** for “checkbox complete” teams (SOC2 CPA deferred by policy, not omission) ([`V1_DEFERRED.md`](V1_DEFERRED.md) §6c).
10. **Release-smoke vs live UI parity illusion** — documented risk: **mock Playwright ≠ SQL-backed UI truth** ([`RELEASE_SMOKE.md`](RELEASE_SMOKE.md)).

---

## 5. Top 5 Monetization Blockers

1. **Category education cost** — buyers must **understand** manifest/governance value vs generic AI assistants (sales-led compensation).
2. **Proof-of-ROI still partially human** — slows **CFO/champion** alignment ([`PILOT_ROI_MODEL.md`](PILOT_ROI_MODEL.md) qualitative rows).
3. **Commercial complexity** — Team vs Professional vs Enterprise mapping + trial limits requires **guided selling** ([`PRICING_PHILOSOPHY.md`](../go-to-market/PRICING_PHILOSOPHY.md)).
4. **Sales-cycle trust assets** — absence of **published reference** and CPA SOC2 is **acceptable for V1 headline scoring** per deferrals but still **lengthens** enterprise cycles in practice ([`V1_DEFERRED.md`](V1_DEFERRED.md) §6b–6c).
5. **Self-serve Stripe CTA gating** — intentionally off by default; **reduces PLG** until staging validation completes ([`TRIAL_AND_SIGNUP.md`](../go-to-market/TRIAL_AND_SIGNUP.md) §2.2)—**V1 motion** remains **sales-assisted**.

---

## 6. Top 5 Enterprise Adoption Blockers

1. **CPA SOC 2 report + ISO** not claimed — procurement “checkbox” friction **remains** ([`TRUST_CENTER.md`](../go-to-market/TRUST_CENTER.md)); headline scoring per repo rules: **informational**, not V1 defect.
2. **Pen-test posture** — owner-conducted V1, third-party **V2** ([`V1_DEFERRED.md`](V1_DEFERRED.md) §6c); strict CISO shops may stall.
3. **Data residency / AI data handling** — subprocessors + AOAI paths require **legal review** ([`TRUST_CENTER.md`](../go-to-market/TRUST_CENTER.md), [`SUBPROCESSORS.md`](../go-to-market/SUBPROCESSORS.md)).
4. **RLS incompleteness narrative** — security teams may demand **DB-layer completeness** before trusting shared infrastructure ([`MULTI_TENANT_RLS.md`](../security/MULTI_TENANT_RLS.md)).
5. **ITSM embedding** — first-party ServiceNow/Jira **V1.1**; Teams/webhooks help Microsoft-centric buyers but **not all** ([`V1_DEFERRED.md`](V1_DEFERRED.md) §6–6a).

---

## 7. Top 5 Engineering Risks

1. **O(N·Q) health score refresh** — tail latency + cost under tenant growth ([`SqlTenantCustomerSuccessRepository.cs`](../../ArchLucid.Persistence/CustomerSuccess/SqlTenantCustomerSuccessRepository.cs)).
2. **RLS misconfiguration / pool stale context** — operational hazard if toggling `ApplySessionContext` without policy state coordination ([`MULTI_TENANT_RLS.md`](../security/MULTI_TENANT_RLS.md) §8).
3. **Stickiness SQL without session context applicator** — maintainability risk if queries drift ([`SqlOperatorStickinessSnapshotReader.cs`](../../ArchLucid.Persistence/CustomerSuccess/SqlOperatorStickinessSnapshotReader.cs)).
4. **Audit fire-and-forget on hot paths** — correct for latency; **risk** is silent loss—must monitor failure metrics ([`AUDIT_COVERAGE_MATRIX.md`](AUDIT_COVERAGE_MATRIX.md) design notes).
5. **LLM trace storage + privacy** — blob + inline fallback paths are powerful; misconfiguration could **over-expose** prompts/responses in exports ([`OBSERVABILITY.md`](OBSERVABILITY.md) §Agent execution trace).

---

## 8. Most Important Truth

**ArchLucid already behaves like a serious enterprise *platform attempt* (SQL, RLS direction, audit, Terraform, CI depth), but it will still win or lose commercially on whether a normal pilot team can produce *sponsor-defensible proof* fast enough—and that proof is only partly automated today.**

---

## 9. Top Improvement Opportunities

Ranked by leverage. **All eight below are fully actionable without new owner decisions.** (No `DEFERRED` items included.)

### 1) Set-based batch refresh for tenant health scores

- **Why it matters:** Removes a **linear scalability** footgun before customer-success dashboards become load-bearing ([`SqlTenantCustomerSuccessRepository.cs`](../../ArchLucid.Persistence/CustomerSuccess/SqlTenantCustomerSuccessRepository.cs)).
- **Expected impact:** Lowers DB round trips from **O(tenants × 6)** to **O(1)** (plus one MERGE/batch); reduces stuck worker risk.
- **Affected qualities:** Scalability (+15–20 pts), Reliability (+3–6), Cost-Effectiveness (+3–5), Maintainability (+2–4). **Weighted readiness impact: +0.35–0.55%** (approx.).
- **Actionable:** Yes.

**Cursor prompt (paste as-is):**

```text
Goal: Replace per-tenant multi-query loop in SqlTenantCustomerSuccessRepository.RefreshAllTenantHealthScoresAsync with a single set-based SQL batch operation.

Context:
- Current code: ArchLucid.Persistence/CustomerSuccess/SqlTenantCustomerSuccessRepository.cs (foreach tenant + multiple COUNT queries + dbo.sp_TenantHealthScores_Upsert per tenant).
- Design intent: docs/library/CUSTOMER_SUCCESS_PERSISTENCE_DESIGN.md §6 batch MERGE sketch.

Requirements:
1. Add a new numbered migration under ArchLucid.Persistence/Migrations/ implementing dbo.sp_TenantHealthScores_BatchRefresh (or equivalent name) that:
   - Uses set-based SQL (CTEs) to compute engagement/breadth/quality/governance/support/composite for all tenants in one invocation.
   - Reuses scoring logic consistent with TenantHealthScoringCalculator (either call the same formulas via SQL expressions or document intentional parity + add tests).
   - Upserts dbo.TenantHealthScores for all computed rows in one batch (MERGE or equivalent idempotent pattern).
2. Mirror the DDL into ArchLucid.Persistence/Scripts/ArchLucid.sql per repo conventions.
3. Change RefreshAllTenantHealthScoresAsync to: enter SqlRowLevelSecurityBypassAmbient, open one connection, apply IRlsSessionContextApplicator as today, execute the new SP once, done.
4. Tests: extend ArchLucid.Persistence.Tests (SQL integration) to verify N tenants produce N rows with expected ordering and that round-trip count of SQL commands is bounded (e.g., mock/diagnostic or trace via SqlConnection — choose a practical assertion).
5. Constraints: parameterized SQL; no SMB/445; preserve existing public API; follow C# whitespace rules (single blank line before if/foreach in methods); keep classes one file each.
6. Do not change business meaning of composite scores without updating docs/library/CUSTOMER_SUCCESS_PERSISTENCE_DESIGN.md.

Acceptance criteria:
- dotnet test archlucid.sln -c Release --filter relevant passes.
- Refresh path performs a single batch proc call for all tenants (aside from listing tenants if still required — prefer incorporating tenant list inside SQL if feasible).
```

---

### 2) Apply RLS session context to operator stickiness snapshot reads

- **Why it matters:** Aligns defense-in-depth with documented RLS posture; reduces **future regression** risk ([`SqlOperatorStickinessSnapshotReader.cs`](../../ArchLucid.Persistence/CustomerSuccess/SqlOperatorStickinessSnapshotReader.cs), [`MULTI_TENANT_RLS.md`](../security/MULTI_TENANT_RLS.md)).
- **Expected impact:** Security (+4–7), Trustworthiness (+2–4), Architectural Integrity (+1–3). **Weighted readiness impact: +0.15–0.25%** (approx.).
- **Actionable:** Yes.

**Cursor prompt:**

```text
Goal: Harden SqlOperatorStickinessSnapshotReader by applying IRlsSessionContextApplicator on each opened connection (tenant-scoped reads), matching other CustomerSuccess repositories.

Files:
- ArchLucid.Persistence/CustomerSuccess/SqlOperatorStickinessSnapshotReader.cs
- ArchLucid.Host.Composition/Configuration/SqlStorageProviderRegistrar.cs (DI constructor wiring)
- Add/adjust tests under ArchLucid.Persistence.Tests (reuse RLS integration patterns from RlsArchLucidScopeIntegrationTests where possible).

Requirements:
1. Change primary constructor to accept ISqlConnectionFactory + IRlsSessionContextApplicator (null checks).
2. Before queries in GetOperatorSignalsAsync and GetFunnelSnapshotAsync: await _rlsSessionContextApplicator.ApplyAsync(connection, ct).
3. Keep explicit TenantId/WorkspaceId/ProjectId predicates — defense in depth.
4. Add regression test proving that when RLS policy is ON and session context targets tenant A, a stickiness query cannot read tenant B rows (mirror existing RLS tests).
5. Constraints: no behavior change intended for correctly scoped calls; do not weaken SQL parameterization.

Acceptance criteria:
- CI-style: dotnet test for affected projects passes.
- New test fails if ApplyAsync is removed/disabled.
```

---

### 3) Core Pilot “single path” home panel (reduce Adoption Friction / Cognitive Load)

- **Why it matters:** Surfaces **one** mandatory sequence for first session; reduces wander into Operate ([`CORE_PILOT.md`](../CORE_PILOT.md)).
- **Expected impact:** Adoption Friction (+6–10), Cognitive Load (+8–12), Usability (+3–6). **Weighted readiness impact: +0.55–0.85%** (approx.).
- **Actionable:** Yes.

**Cursor prompt:**

```text
Goal: In archlucid-ui, strengthen the operator Home experience to default new tenants into Core Pilot only (four steps), with explicit links to docs/CORE_PILOT.md anchors and OPERATOR_ATLAS routes.

Scope:
- archlucid-ui/src — Home page components only (no API contract changes).
- Reuse existing checklist/rail telemetry if present; do not add tenant-identifying tags beyond existing privacy posture.

Requirements:
1. Add a prominent CorePilotNextSteps card for tenants with zero committed runs:
   - Steps: Create request → Execute → Commit → Review package (map to real routes in nav-config).
2. For tenants with ≥1 commit, collapse card into a compact “Core Pilot complete — optional next: Operate” CTA.
3. Add Vitest coverage for visibility rules (mock /me responses as needed).
4. Constraints: respect authority-seam tests; do not bypass API authZ; no emoji; follow existing design tokens/components.
5. Do not change pricing or marketing routes.

Acceptance criteria:
- npm test (Vitest) passes for archlucid-ui.
- New tests prove default-first-run shows only Pilot essentials messaging (no Operate deep links in the card body).
```

---

### 4) Procurement one-pager: “route → tier gate → policy → nav disclosure” matrix

- **Why it matters:** Resolves real buyer confusion between **UI visibility** and **HTTP gates** ([`PRODUCT_PACKAGING.md`](PRODUCT_PACKAGING.md) four-boundary table).
- **Expected impact:** Procurement Readiness (+4–7), Commercial Packaging (+3–6), Trustworthiness (+2–4). **Weighted readiness impact: +0.12–0.22%** (approx.).
- **Actionable:** Yes.

**Cursor prompt:**

```text
Goal: Add a single authoritative table documenting packaging enforcement for buyers and reviewers.

Implement:
1. Create docs/library/ROUTE_TIER_POLICY_NAV_MATRIX.md with scope header per CI rules.
2. Table columns: HTTP route family (grouped), RequiresCommercialTenantTier? (yes/no/which), ArchLucidPolicies authorization, operator nav-config entry (href + tier + requiredAuthority), notes on 404-vs-403 behavior.
3. Seed the matrix with top 25 Pilot-critical routes + top 15 Operate routes (from PRODUCT_PACKAGING.md inventories).
4. Link the matrix from docs/go-to-market/PROCUREMENT_FAST_LANE.md and docs/library/PRODUCT_PACKAGING.md §4 boundary rules.

Constraints:
- Do not invent behavior — verify against ArchLucid.Api controller attributes and CommercialTenantTierFilter patterns by searching code.
- If uncertain, mark cell as “verify pending” with file pointer.

Acceptance criteria:
- python scripts/ci/check_doc_scope_header.py passes for the new file.
- No broken links from the two modified docs.
```

---

### 5) Grafana + alert hook for data-consistency counters

- **Why it matters:** Orphan drift becomes a **silent integrity** issue without paging ([`DATA_CONSISTENCY_ENFORCEMENT.md`](../data-consistency/DATA_CONSISTENCY_ENFORCEMENT.md), [`OBSERVABILITY.md`](OBSERVABILITY.md)).
- **Expected impact:** Reliability (+3–6), Supportability (+3–5), Data Consistency (+2–4). **Weighted readiness impact: +0.08–0.15%** (approx.).
- **Actionable:** Yes.

**Cursor prompt:**

```text
Goal: Make observability/dashboards explicitly cover archlucid_data_consistency_* metrics end-to-end.

Implement:
1. Update infra/grafana/dashboard-archlucid-authority.json (or the most appropriate existing dashboard) with panels for:
   - archlucid_data_consistency_orphans_detected_total
   - archlucid_data_consistency_alerts_total
   - archlucid_data_consistency_orphans_quarantined_total
2. Add/extend prometheus rules in infra/prometheus/archlucid-alerts.yml with a warning alert when alerts_total increases over a window in staging/prod profiles (avoid noise: document threshold rationale in comments).
3. Link panels from docs/runbooks/DATA_CONSISTENCY_ENFORCEMENT.md.

Constraints:
- Do not introduce new cloud services; JSON/YAML only.
- Keep label cardinality safe.

Acceptance criteria:
- terraform validate-related docs unchanged except references; if repo has dashboard JSON validators, run them.
- Alert rule names are unique and documented.
```

---

### 6) Proof-of-ROI: structured “pilot closeout” DTO + audit event (lightweight)

- **Why it matters:** Moves qualitative sponsor claims into **queryable** records tied to tenant/run ([`PILOT_ROI_MODEL.md`](PILOT_ROI_MODEL.md)).
- **Expected impact:** Proof-of-ROI (+6–10), Executive Value Visibility (+3–6), Traceability (+2–4). **Weighted readiness impact: +0.35–0.55%** (approx.).
- **Actionable:** Yes.

**Cursor prompt:**

```text
Goal: Add an optional pilot closeout capture API that persists structured JSON + emits a durable audit event.

Requirements:
1. Design minimal DTO: baseline hours, qualitative scores 1–5 for 3 questions (speed/manpack/traceability), optional free text capped (length limit), runId optional.
2. Add POST /v1/pilots/closeout (name aligned with existing Pilots controller families) protected by appropriate policy.
3. Persist to a new table via migration + ArchLucid.sql mirror (tenant scoped).
4. Emit IAuditService event with redaction rules: do not store secrets; truncate free text; include correlation id.
5. Tests: ArchLucid.Api.Tests integration covering happy path + validation failures.

Constraints:
- No SMB/445; SQL Server only; RLS session context respected.
- Follow single migration file conventions.

Acceptance criteria:
- OpenAPI snapshot regenerated if required by repo workflow.
- CI core tests pass for the new route.
```

---

### 7) Expand accessibility CI routes (pricing + get-started + operator home)

- **Why it matters:** Enterprise buyers increasingly file **VPAT / WCAG** questions early ([`TRUST_CENTER.md`](../go-to-market/TRUST_CENTER.md)).
- **Expected impact:** Accessibility (+10–15), Procurement Readiness (+2–4). **Weighted readiness impact: +0.10–0.18%** (approx.).
- **Actionable:** Yes.

**Cursor prompt:**

```text
Goal: Extend merge-blocking axe/live accessibility coverage to highest-traffic public + operator entry routes.

Files:
- archlucid-ui/e2e/live-api-accessibility*.spec.ts (follow TEST_STRUCTURE.md naming)
- archlucid-ui/docs/TESTING_AND_TROUBLESHOOTING.md update list

Requirements:
1. Add routes: /pricing, /get-started (or actual marketing paths used), operator /home (or correct path) behind auth fixture patterns already used in live suite.
2. Keep secrets out of logs; reuse existing live harness.
3. Update docs with any new env requirements.

Acceptance criteria:
- Local documented command runs green against LIVE_API_URL when configured.
- No flake: use stable selectors already in codebase.
```

---

### 8) Release-smoke vs live E2E “truth ladder” callout in pilot docs

- **Why it matters:** Prevents false confidence in handoffs ([`RELEASE_SMOKE.md`](RELEASE_SMOKE.md)).
- **Expected impact:** Adoption Friction (-friction: +4–7), Customer Self-Sufficiency (+4–7), Correctness perception (+2–4). **Weighted readiness impact: +0.08–0.14%** (approx.).
- **Actionable:** Yes.

**Cursor prompt:**

```text
Goal: Reduce pilot miscommunication by documenting the verification ladder explicitly where pilots look first.

Implement:
1. Update docs/library/PILOT_GUIDE.md and docs/CORE_PILOT.md with a short boxed “Verification ladder” section:
   Level A: run-readiness-check
   Level B: release-smoke.ps1 (API+CLI+artifacts)
   Level C: CI parity live Playwright (live-api-*.spec.ts) — required for UI+SQL truth
2. Link to docs/library/RELEASE_SMOKE.md#release-smoke-ui-sql-parity and docs/library/LIVE_E2E_HAPPY_PATH.md.
3. Add doc scope header compliance if editing headers are needed.

Constraints:
- No code changes required; docs only.

Acceptance criteria:
- python scripts/ci/check_doc_scope_header.py passes for touched docs under docs/** (except archive).
```

---

## 10. Pending Questions for Later

_Organized by improvement title; blocking or materially decision-shaping only._

1. **Set-based batch refresh for tenant health scores**
   - Do we **require** scoring parity to remain in C# (`TenantHealthScoringCalculator`) for auditability, or is SQL-side duplication acceptable with golden tests?

2. **RLS session context on stickiness reads**
   - Should stickiness reads route through **read replica** factory when available (alignment with CUSTOMER_SUCCESS_PERSISTENCE_DESIGN recommendations), or stay primary to simplify consistency?

3. **Core Pilot home panel**
   - What is the **canonical** “first landing route” name in production (`/home` vs `/`)—confirm to avoid broken links in the card?

4. **Route/tier/policy matrix**
   - Should the matrix be **generated** from code attributes in a follow-on (single source of truth), or remain human-maintained for V1?

5. **Pilot closeout DTO**
   - Legal/privacy: is **any** free-text capture allowed for enterprise tenants without explicit DPA classification updates?

6. **Accessibility route expansion**
   - Which **auth path** is canonical for live accessibility tests (JWT vs ApiKey vs DevelopmentBypass) across customer-representative environments?

7. **Grafana/alert thresholds for data consistency**
   - What **baseline orphan count** is considered normal in staging seeds vs alerting noise?





Shell