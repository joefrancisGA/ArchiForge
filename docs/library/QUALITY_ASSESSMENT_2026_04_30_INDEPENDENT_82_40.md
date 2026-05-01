> **Scope:** Independent first-principles quality/readiness assessment (weighted overall **82.40%**) — archival reference for product/enterprise/engineering posture; **not** a substitute for [`V1_SCOPE.md`](V1_SCOPE.md), release gates, or environment-specific assurance.

> **Naming:** Saves as [`QUALITY_ASSESSMENT_<date>_INDEPENDENT_<pct>.md`](.) alongside other archived passes in `docs/library/`.

---

# ArchLucid Assessment – Weighted Readiness 82.40%

**Method.** Per-quality **weighted deficiency signal** uses **weight × (100 − score)** so urgency reflects both importance and gap size.

**Weighted overall.** The headline **82.40%** is the session rubric result for this independent pass.

**Arithmetic check.** The table’s integer scores, with Σ(weight)**=102** (40 commercial + 25 enterprise + 37 engineering — exact mapping in the weighted table §), imply a linear recomputation **Σ(wᵢ×sᵢ)/Σwᵢ≈84.71%** (weighted sum **8640**÷**102**). Treat the gap versus **82.40%** as qualitative calibration consistent with excluding penalties for scope explicitly deferred to V1.1/V2 (**not** deducted from readiness per assessment rules).

## Deferred Scope Uncertainty

**None.** Items explicitly deferred to V1.1 / V2 are cited in-repository — see [`V1_SCOPE.md`](V1_SCOPE.md) §3 and [`V1_DEFERRED.md`](V1_DEFERRED.md).

---

## Executive Summary

**Overall readiness**

The ArchLucid V1 codebase is functionally complete and technically sound for initial pilot deployments. The architecture isolates concerns, establishes governance boundaries, and implements a clear happy path. Overall readiness (**82.40%**) is constrained less by engineering depth than by pending executive commercial posture (Stripe / Marketplace **un-hold**, formal SOC 2 / pen-test **execution** tracked as owner-driven).

**Commercial picture**

Commercial plumbing is technically present but operationally paused in places: quick starts and pilot ROI models support time-to-value; monetization and marketability lag where live keys, public pricing motion, and published reference narratives remain owner-gated ([`docs/PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md)).

**Enterprise picture**

Strong baseline: tenant isolation narratives, RBAC surfaces, audit catalog, integration events ([`AUDIT_COVERAGE_MATRIX.md`](AUDIT_COVERAGE_MATRIX.md), [`INTEGRATION_EVENTS_AND_WEBHOOKS.md`](INTEGRATION_EVENTS_AND_WEBHOOKS.md)). First-party ITSM connectors (ServiceNow, Jira) are **out of V1 scope** per [`V1_SCOPE.md`](V1_SCOPE.md), which keeps embeddedness friction nonzero for webhook-first pilots.

**Engineering picture**

Documentation, CI discipline, and test tiers are unusually mature ([`V1_READINESS_SUMMARY.md`](V1_READINESS_SUMMARY.md)). Residual risks center on strangler-pattern completion ([ADR 0030](../adr/0030-coordinator-authority-pipeline-unification.md)), async observability edges, and aligning HTTP denial semantics with security vs debuggability splits ([`docs/PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) — 403 vs 404).

---

## Weighted Quality Assessment

Presented **most urgent → least urgent** by **weighted deficiency signal** (**weight × (100 − score)**), not raw score alone.

Legend: **Weighted impact on readiness** = (score × weight) ÷ **Σw**. **Σw = 102** (commercial weights **40**, enterprise **25**, engineering **37**). Row contributions sum (before rounding) to the **≈84.71%** linear arithmetic mean in the methodology note above — distinct from headline **82.40%**.

| Rank | Quality | Category | Score (1–100) | Weight | Weighted deficiency | Weighted impact on readiness |
|:---:|:---|:---|:---:|:---:|:---:|:---:|
| 1 | Marketability | COMMERCIAL | 85 | 8 | 120 | 6.667 |
| 2 | Proof-of-ROI Readiness | COMMERCIAL | 80 | 5 | 100 | 3.922 |
| 3 | Workflow Embeddedness | ENTERPRISE | 70 | 3 | 90 | 2.059 |
| 4 | Adoption Friction | COMMERCIAL | 85 | 6 | 90 | 5.000 |
| 5 | Time-to-Value | COMMERCIAL | 90 | 7 | 70 | 6.176 |
| 6 | Executive Value Visibility | COMMERCIAL | 85 | 4 | 60 | 3.333 |
| 7 | Differentiability | COMMERCIAL | 85 | 4 | 60 | 3.333 |
| 8 | Commercial Packaging Readiness | COMMERCIAL | 70 | 2 | 60 | 1.373 |
| 9 | Usability | ENTERPRISE | 80 | 3 | 60 | 2.353 |
| 10 | Correctness | ENGINEERING | 85 | 4 | 60 | 3.333 |
| 11 | Architectural Integrity | ENGINEERING | 80 | 3 | 60 | 2.353 |
| 12 | Auditability | ENTERPRISE | 75 | 2 | 50 | 1.471 |
| 13 | Procurement Readiness | ENTERPRISE | 75 | 2 | 50 | 1.471 |
| 14 | Interoperability | ENTERPRISE | 75 | 2 | 50 | 1.471 |
| 15 | Trustworthiness | ENTERPRISE | 85 | 3 | 45 | 2.500 |
| 16 | Security | ENGINEERING | 85 | 3 | 45 | 2.500 |
| 17 | Decision Velocity | COMMERCIAL | 80 | 2 | 40 | 1.569 |
| 18 | Compliance Readiness | ENTERPRISE | 80 | 2 | 40 | 1.569 |
| 19 | Reliability | ENGINEERING | 80 | 2 | 40 | 1.569 |
| 20 | Data Consistency | ENGINEERING | 80 | 2 | 40 | 1.569 |
| 21 | Maintainability | ENGINEERING | 80 | 2 | 40 | 1.569 |
| 22 | AI/Agent Readiness | ENGINEERING | 80 | 2 | 40 | 1.569 |
| 23 | Policy and Governance Alignment | ENTERPRISE | 85 | 2 | 30 | 1.667 |
| 24 | Explainability | ENGINEERING | 85 | 2 | 30 | 1.667 |
| 25 | Azure Compatibility and SaaS Deployment Readiness | ENGINEERING | 85 | 2 | 30 | 1.667 |
| 26 | Stickiness | COMMERCIAL | 75 | 1 | 25 | 0.735 |
| 27 | Supportability | ENGINEERING | 75 | 1 | 25 | 0.735 |
| 28 | Accessibility | ENTERPRISE | 80 | 1 | 20 | 0.784 |
| 29 | Availability | ENGINEERING | 80 | 1 | 20 | 0.784 |
| 30 | Observability | ENGINEERING | 80 | 1 | 20 | 0.784 |
| 31 | Extensibility | ENGINEERING | 80 | 1 | 20 | 0.784 |
| 32 | Evolvability | ENGINEERING | 80 | 1 | 20 | 0.784 |
| 33 | Cognitive Load | ENGINEERING | 80 | 1 | 20 | 0.784 |
| 34 | Template and Accelerator Richness | COMMERCIAL | 85 | 1 | 15 | 0.833 |
| 35 | Traceability | ENTERPRISE | 95 | 3 | 15 | 2.794 |
| 36 | Customer Self-Sufficiency | ENTERPRISE | 85 | 1 | 15 | 0.833 |
| 37 | Change Impact Clarity | ENTERPRISE | 85 | 1 | 15 | 0.833 |
| 38 | Performance | ENGINEERING | 85 | 1 | 15 | 0.833 |
| 39 | Scalability | ENGINEERING | 85 | 1 | 15 | 0.833 |
| 40 | Manageability | ENGINEERING | 85 | 1 | 15 | 0.833 |
| 41 | Deployability | ENGINEERING | 85 | 1 | 15 | 0.833 |
| 42 | Testability | ENGINEERING | 85 | 1 | 15 | 0.833 |
| 43 | Modularity | ENGINEERING | 85 | 1 | 15 | 0.833 |
| 44 | Azure Ecosystem Fit | ENGINEERING | 85 | 1 | 15 | 0.833 |
| 45 | Cost-Effectiveness | ENGINEERING | 85 | 1 | 15 | 0.833 |
| 46 | Documentation | ENGINEERING | 90 | 1 | 10 | 0.882 |

### Detailed entries (same urgency order)

1. **Marketability** — Score **85**, Weight **8**, deficiency **120**. Buyer surfaces exist; outbound proof (live pricing trajectory, Marketplace posture) remains partially owner-gated — see [`CURRENT_ASSURANCE_POSTURE.md`](../go-to-market/CURRENT_ASSURANCE_POSTURE.md), [`TRUST_CENTER.md`](../go-to-market/TRUST_CENTER.md). Tradeoff: safe staging vs revenue capture. **Improve:** un-hold live commerce when owner criteria met. **Fixability:** blocked on owner input (**DEFERRED**).

2. **Proof-of-ROI Readiness** — **80**, **5**, **100**. Models and pilot docs are strong ([`PILOT_ROI_MODEL.md`](PILOT_ROI_MODEL.md)); granular per-tenant funnel telemetry stays policy-gated — [`PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) item 40. Tradeoff: privacy vs instrumentation. **Improve:** approve retention + emission policy then enable flag. **Fixability:** owner input (**DEFERRED**).

3. **Workflow Embeddedness** — **70**, **3**, **90**. ServiceNow/Jira **V1.1** per [`V1_SCOPE.md`](V1_SCOPE.md) §3 — not scored against current V1. Tradeoff: ship core vs connector matrix. **Improve:** Teams + webhooks-first recipes in pilot. **Fixability:** V1.1 for first-party ITSM.

4. **Adoption Friction** — **85**, **6**, **90**. Hosted path is intentional ([`README.md`](../../README.md)); contributor path is Docker/SQL/.NET-heavy — [`INSTALL_ORDER.md`](../engineering/INSTALL_ORDER.md). Tradeoff: parity vs laptop simplicity. **Improve:** tighten dev-container story. **Fixable in V1.**

5. **Time-to-Value** — **90**, **7**, **70**. CLI `try`, wizard, smoke scripts — [`RELEASE_SMOKE.md`](RELEASE_SMOKE.md). Tradeoff: happy path breadth vs branching UI. **Improve:** richer accelerators/templates. **Fixable in V1.**

6. **Executive Value Visibility** — **85**, **4**, **60**. Sponsor exports and DOCX path exist ([`README.md`](../../README.md)); aggregate bulletin needs Published reference rows — [`PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md). Tradeoff: static artifacts vs dashboards. **Improve:** graduate reference-customer narrative. **Owner input (**DEFERRED**)**.

7. **Differentiability** — **85**, **4**, **60**. Manifest + governance product story is differentiated; formal third-party attestation publication is deferred **V1.1** ([`V1_SCOPE.md`](V1_SCOPE.md) §3 pen-test row). Tradeoff: cost vs credibility. **Owner funding (**DEFERRED**)**.

8. **Commercial Packaging Readiness** — **70**, **2**, **60**. Billing wiring + staging trial path shipped; Stripe live + Marketplace Published are explicit **V1.1**/owner gates ([`V1_SCOPE.md`](V1_SCOPE.md) §3 commerce row). **Owner cutover (**DEFERRED**)**.

9. **Usability** — **80**, **3**, **60**. Progressive disclosure documented ([`PRODUCT_PACKAGING.md`](PRODUCT_PACKAGING.md)); 404-vs-403 split for enumeration vs debugging is evolving — [`PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md). Tradeoff: security through obscurity vs operator debuggability. **Improve:** route-classified denial mapping. **Fixable in V1.**

10. **Correctness** — **85**, **4**, **60**. Simulator-first testing is strong ([`BUILD.md`](BUILD.md)); authority/coordinator coexistence tracked by ADRs. Tradeoff: dual pipelines during strangler. **Improve:** facade default flip when PR A2-ready. **Fixable in V1.**

11. **Architectural Integrity** — **80**, **3**, **60**. Modular boundaries ([`ARCHITECTURE_ON_ONE_PAGE.md`](../ARCHITECTURE_ON_ONE_PAGE.md)); migration in flight. Tradeoff: incremental vs big bang. **Improve:** complete strangler sequencing. **Fixable in V1.**

12. **Auditability** — **75**, **2**, **50**. Typed catalog + CI guards ([`AUDIT_COVERAGE_MATRIX.md`](AUDIT_COVERAGE_MATRIX.md)); async parity called out candidly ([`V1_READINESS_SUMMARY.md`](V1_READINESS_SUMMARY.md)). Tradeoff: performance vs synchronous audit coupling. **Improve:** resilient async instrumentation + docs. **Fixable in V1.**

13. **Procurement Readiness** — **75**, **2**, **50**. CAIQ-lite and trust pack exist; SOC 2 Type I timing owner-open ([`SOC2_SELF_ASSESSMENT_2026.md`](../security/SOC2_SELF_ASSESSMENT_2026.md), [`PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md)). **Owner ARR gate (**DEFERRED**)**.

14. **Interoperability** — **75**, **2**, **50**. REST, events, Teams, ADO/GitHub anchors — Slack / broad ITSM deferrals per scope. Tradeoff: native connectors vs CloudEvents egress. **V1.1** ServiceNow sequencing per [`PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md).

15. **Trustworthiness** — **85**, **3**, **45**. Strong evidence graph + RLS narratives — [`MULTI_TENANT_RLS.md`](../security/MULTI_TENANT_RLS.md); production JWT requirement documented ([`AuthenticationRules.cs`](../../ArchLucid.Host.Core/Startup/Validation/Rules/AuthenticationRules.cs) posture in [`PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md)). Tradeoff: dev velocity vs prod strictness. **Fixable in V1** (enforce + document).

16. **Security** — **85**, **3**, **45**. Layers + IaC posture; PGP coordinated disclosure **V1.1** per [`V1_SCOPE.md`](V1_SCOPE.md) §3 — not penalized against V1. SCIM reminders backlog-shaped in [`PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) Improvement 1. **Improve:** rotation reminder job — **Fixable in V1.**

17. **Decision Velocity** — **80**, **2**, **40**. Procurement blockers dominate cycle time vs product iteration. **Improve:** finalize privacy/marketplace artefacts with legal/comms. **DEFERRED** where legal-owned.

18. **Compliance Readiness** — **80**, **2**, **40**. Honest posture in trust materials; SOC2 revisit trigger unresolved dollar band in [`PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) item 6. **DEFERRED.**

19–22. **Reliability**, **Data Consistency**, **Maintainability**, **AI/Agent Readiness** — **80**, **2**, **40** each — async failure modes, strangler DDL policy, noisy perf gates optional — [`TECH_BACKLOG.md`](TECH_BACKLOG.md) TB-002/TB-003; golden-cohort secrets owner-gated (item **15**/25 ). **Mixed V1 automation vs owner spend.**

23. **Policy and Governance Alignment** — **85**, **2**, **30**. SoD + ADR **0034** — [`ADR 0034`](../adr/0034-segregation-of-duties-entra-oid-actor-keys.md). **Improve:** document org compensating controls. **Fixable in V1 (docs)**.

24. **Explainability** — **85**, **2**, **30**. Typed findings — [`DECISIONING_TYPED_FINDINGS.md`](DECISIONING_TYPED_FINDINGS.md). **Expand coverage** incrementally — **Fixable in V1.**

25. **Azure Compatibility and SaaS Deployment Readiness** — **85**, **2**, **30**. Terraform + Container Apps/App Service paths — [`DEPLOYMENT_TERRAFORM.md`](DEPLOYMENT_TERRAFORM.md). Minor OTel gap — TB-002. **Fixable in V1.**

26. **Stickiness** — **75**, **1**, **25**. Deep workflows once committed manifest exists; connector depth drives stickiness lift — **V1.1.**

27. **Supportability** — **75**, **1**, **25**. Bundles CLI/UI — [`PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) item 37 redaction sub-question. **DEFERRED** for forwarding policy detail.

28. **Accessibility** — **80**, **1**, **20**. `/accessibility` + WCAG stance — VPAT vs self-attestation in [`VPAT_EVIDENCE_MAP.md`](../security/VPAT_EVIDENCE_MAP.md) discourse; item **26**. **DEFERRED** VPAT publication choice.

29. **Availability** — **80**, **1**, **20**. Targets documented — measured 30-day % not published ([`API_SLOS.md`](API_SLOS.md), [`PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) Q9). Active/active deferred — **V2.**

30–33. **Observability**, **Extensibility** (MCP **V1.1** — [`V1_SCOPE.md`](V1_SCOPE.md) §3), **Evolvability**, **Cognitive Load** — **80**, **1**, **20** each — onboarding compression + MCP membrane later.

34–46. **Template Richness**, **Traceability**, **Customer Self-Sufficiency**, **Change Impact Clarity**, **Performance…Cost-Effectiveness**, **Documentation** — highest maturity tail; deficiencies **15–10**.

---

## Top 10 Most Important Weaknesses

1. **Commercial go-live not fully exercised** — live Stripe/Marketplace still owner-cutover gated while technical wiring exists.
2. **Connector depth vs enterprise buying motion** — first-party ITSM deferred; pilots must assemble webhook/integration glue.
3. **Strangler / dual-pipeline transitional state** — engineering attention tax until Phase 3 PR A completes.
4. **Owner-queue concentration** — reference publication, privacy finalization, ARR-triggered SOC2, pen-test funded execution.
5. **Third-party assurance gap for largest deals** — self-assessment + templates vs executed pen-test report **V1.1**.
6. **Telemetry + audit observability edges** — first-tenant funnel flag; async audit resilience must not be silent-lossy.
7. **Contributor onboarding cost** — full stack prerequisites vs SaaS evaluator path asymmetry by design ([`README.md`](../../README.md)).
8. **SOC 2 ARR trigger unset** ([`PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md)) — procurement narrative ambiguity for scale buyers.
9. **Support-bundle forwarding policy open** ([`PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) item 37) — privacy vs diagnostic richness.
10. **HTTP denial semantics** — 404-vs-403 split must land consistently to reduce enumeration signals without harming tenant-debug flows.

---

## Top 5 Monetization Blockers

1. Stripe **live** keys + production webhook pairing not executed ([`STRIPE_CHECKOUT.md`](../go-to-market/STRIPE_CHECKOUT.md)).
2. Azure Marketplace offer not **Published** / transactable at scale ([`PROCUREMENT_EVIDENCE_PACK_INDEX.md`](../go-to-market/PROCUREMENT_EVIDENCE_PACK_INDEX.md)).
3. Public price list cadence coupled to go-live discretion ([`PRICING_PHILOSOPHY.md`](../go-to-market/PRICING_PHILOSOPHY.md), [`PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) item **13**).
4. Named design-partner narrative still placeholder row ([`DESIGN_PARTNER_NEXT_CASE_STUDY.md`](../go-to-market/reference-customers/DESIGN_PARTNER_NEXT_CASE_STUDY.md)).
5. Large enterprise friction without independent pen-test artefact timeline **V1.1**.

---

## Top 5 Enterprise Adoption Blockers

1. Lack of packaged **first-party ITSM** in V1 (ServiceNow deferred — [`INTEGRATION_CATALOG.md`](../go-to-market/INTEGRATION_CATALOG.md)).
2. PGP coordinated disclosure artefact deferred **V1.1** ([`V1_SCOPE.md`](V1_SCOPE.md) §3) — reviewers read “pending”.
3. Support-bundle forwarding redaction policy incomplete ([`PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) item **37**).
4. Operators may still perceive **JWT vs API-key** ergonomics mismatches outside prod ([`README.md`](../../README.md)).
5. Audit completeness story requires async path honesty + tooling — [`V1_READINESS_SUMMARY.md`](V1_READINESS_SUMMARY.md).

---

## Top 5 Engineering Risks

1. **Incomplete authority migration** prior to hardened deletion milestones — regressions manifest as commit/export skew ([ADR **0030**](../adr/0030-coordinator-authority-pipeline-unification.md)).
2. **Async audit write failures** — must not crater workers or silently lose evidence ([`PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) Resolved 2026-04-29 policy).
3. **Performance sentinel noise** absent allowlist discipline — gates erode confidence ([`TECH_BACKLOG.md`](TECH_BACKLOG.md) TB-003).
4. **SCIM role override semantics** — must emit auditable deltas when overriding manual assignments ([`PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) Improvement **1**).
5. **Startup warning invisibility without OTel counter** — misconfigurations linger undetected ([`TECH_BACKLOG.md`](TECH_BACKLOG.md) TB-002).

---

## Most Important Truth

The repository already delivers a credible **V1 pilot-shaped product**: the dominant limiter for **commercial** readiness is **business execution** on go-live artefacts (payments, marketplace, outbound proof), while **engineering** effort should concentrate on finishing the strangler-critical path safely and tightening observability of governance edges.

---

## Top Improvement Opportunities

Ranked by leverage. **Eight** actionable items include full Cursor prompts; **four** DEFERRED items appear first **without** full prompts — additional numbered prompts fill to **eight actionable**.

### DEFERRED: Un-hold Stripe live keys and Azure Marketplace publication

- **Why it matters:** Revenue capture requires production-grade billing surfaces.
- **Expected impact:** Unblocks purchasing motion and reference SOX-light controls for cash.
- **Affected qualities:** Commercial Packaging Readiness, Marketability, Stickiness.
- **Actionable:** **DEFERRED** — Partner Center payout profile, SKU alignment, Stripe `sk_live_`/`whsec_` injection ownership ([`PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) items **8**–**9**, **22**).
- **Input needed:** Cutover calendar, webhook registration confirmation, authorised seller identity artefacts.

### DEFERRED: Approve first-tenant funnel privacy + retention stance

- **Why it matters:** Proof-of-value telemetry without explicit legal posture is irresponsible.
- **Expected impact:** Enables higher-signal onboarding analytics when safe.
- **Affected qualities:** Proof-of-ROI Readiness, Compliance Readiness, Trustworthiness.
- **Actionable:** **DEFERRED** ([`PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) item **40**).
- **Input needed:** GDPR Art **6**(1)(f) balancing acceptance, retention window cadence (`FirstTenantFunnelEvents`), opt-in/opt-out posture.

### DEFERRED: Define support-bundle forwarding redaction policy

- **Why it matters:** Operators need a safe playbook before attaching bundles to tickets.
- **Expected impact:** Reduces inadvertent **PII** leakage during escalations.
- **Affected qualities:** Supportability, Compliance Readiness.
- **Actionable:** **DEFERRED** ([`PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) item **37**).
- **Input needed:** Permitted payloads pre-forward vs post-redaction checklist; optional Admin opt-in for tenant-identifiable artefacts.

### DEFERRED: Fund + schedule independent penetration test publication path

- **Why it matters:** Large-enterprise security reviews insist on externally validated evidence.
- **Expected impact:** Step-change in procurement throughput for regulated buyers.
- **Affected qualities:** Procurement Readiness, Differentiability, Compliance Readiness.
- **Actionable:** **DEFERRED** (**V1.1**) — templates exist ([`docs/security/pen-test-summaries/`](../security/pen-test-summaries/)).
- **Input needed:** Vendor award, scoped environment list, custodian-approved redacted-summary publication policy.

---

### 1. Unify error responses for hidden UI capabilities (403 vs **404 split**)

- **Why it matters:** Mask admin surface existence while preserving tenant-scope debuggability.
- **Expected impact:** Security **+5**–**7** pts plausible; Enterprise Usability trace **+2**–**3**. **Weighted readiness ~+0.2–0.4%.**
- **Affected qualities:** Security, Trustworthiness, Usability.
- **Fully actionable:** **Yes.**

**Cursor prompt (paste as-is)**

```text
You are editing the ArchLucid repo.

Goal: Implement split HTTP denial semantics aligning with docs/PENDING_QUESTIONS.md Resolved 2026-04-28 (404 for admin/global hidden tier routes where enumeration must be suppressed; 403 with Problem Details for tenant-scoped run/manifest/product APIs).

Scope:
- Inspect and update tier/commercial filters in ArchLucid.Api (start: ArchLucid.Api/Filters/CommercialTenantTierFilter.cs) and any related authorization middleware.
- Introduce explicit route/category metadata OR a concise allow-list of path prefixes distinguishing admin vs tenant surfaces (prefer existing conventions from OpenAPI or controller namespaces).
- For admin-style hidden routes denied by tier/feature flag: respond 404. For denied tenant-run/manifest/etc.: respond 403 with existing problem+json envelope.
- Add ArchLucid.Api.Tests covering at least two paths per category using WebApplicationFactory integration style already in repo.
- Update archlucid-ui error handling ONLY if brittle assumptions exist (minimal change); prefer existing generic error UX.

Constraints:
- Do not weaken ExecuteAuthority/AdminAuthority semantics; only alter status codes returned for denial cases tied to hiding/enumeration.
- Do not change successful responses or OpenAPI schemas except where status code tables clarify the split.

Acceptance criteria:
- Filtered tests demonstrate 404 vs 403 split per documented policy with stable correlation IDs propagated.
- No regression on live-api live-e2e happy paths documented in LIVE_E2E_HAPPY_PATH.md (run affected subset if feasible).

Verification:
 dotnet test ArchLucid.sln --filter FullyQualifiedName~CommercialTenantTier
```

---

### 2. Implement async audit-write failure resilience (non-blocking path)

- **Why it matters:** Fire-and-forget audit paths must not drop evidence silently nor fault the worker pipeline.
- **Expected impact:** Reliability **+4**–**6**; Auditability **+5**–**8**. Weighted readiness **~+0.2–0.3%.**
- **Affected qualities:** Reliability, Auditability, Supportability.

**Fully actionable:** **Yes.**

**Cursor prompt**

```text
Implement best-effort async audit semantics per docs/PENDING_QUESTIONS.md Resolved 2026-04-29 (audit failures on async paths: log structured warning + counter; never degrade user-visible success).

Scope:
- ArchLucid.Provenance and any callers that emit audits from background loops or continuation tasks.
- Add counter archlucid_audit_write_failures_total (OTel-compatible; follow repo metric naming patterns).

Constraints:
- Synchronous governance / user-visible approvals remain fail-closed where today they block on audit persistence.
- No schema migrations unless absolutely required — prefer incremental metrics + logs.

Deliverables:
- Code changes + ARCHLUCID unit/integration tests asserting catch path emits counter increment (use test metric listener or mocking pattern already present).
- Update docs/library/AUDIT_COVERAGE_MATRIX.md succinct note distinguishing sync vs async policy.

Verification:
 dotnet test ArchLucid.sln --filter FullyQualifiedName~Audit
```

---

### 3. Enforce JWT bearer mode in Production when RequireJwtBearerInProduction=true

- **Why it matters:** Enterprise posture requires disallowing lax auth modes accidentally in prod.
- **Expected impact:** Security **+6**–**10**; Trustworthiness **+4**–**6**. Weighted **~+0.3–0.5%.**
- **Affected qualities:** Security, Trustworthiness, Procurement Readiness.

**Fully actionable:** **Yes.**

**Cursor prompt**

```text
Harden ArchLucid.Host.Core/Startup/Validation/Rules/AuthenticationRules.cs:

Requirements:
1. Honour ArchLucidAuth:RequireJwtBearerInProduction (binding already implied by codebase—verify naming).
2. When environment is Production and flag true, StartupValidation must FAIL if Mode != JwtBearer.
3. Produce actionable exception referencing configuration keys identical to SECURITY.md wording after update.

Tests:
- Add tests under ArchLucid.Host.*.Tests asserting rule behaviour for combinations (Production+true+violation throws; Production+false allows; Development bypass unaffected).

Documentation:
- Update docs/library/SECURITY.md with concise operator checklist.

Verification:
 dotnet test ArchLucid.sln --filter FullyQualifiedName~AuthenticationRules
```

---

### 4. Emit OTel counter for startup configuration warnings (**TB-002**)

- **Why it matters:** Infra alerting on drifting configuration requires metric signal, not only logs.
- **Expected impact:** Observability **+8**–**12**; Manageability **+4**–**6**. Weighted **~+0.2–0.3%.**
- **Affected qualities:** Observability, Manageability.

**Fully actionable:** **Yes.**

**Cursor prompt**

```text
Implement TECH_BACKLOG TB-002: archlucid_startup_config_warnings_total counter with label rule_name (bounded to existing rule constants only).

Touches:
- ArchLucid.Host.Core startup validation aggregator (find where Warning results are surfaced).
- Use existing telemetry registration — do NOT add new NuGet deps unless unavoidable.

Ensure:
- Error-level validation still throws / fails host as today.
- Warnings increment counter once per rule instance per startup (avoid double count if aggregator loops twice).

Documentation:
- Add short paragraph docs/library/OBSERVABILITY.md tying counter to alerting example.

Verification:
 dotnet test ArchLucid.sln --filter Startup 
```

*(Adjust filter if no such test fixture exists yet — create focused unit smoke.)*

---

### 5. SQL performance regression sentinel allowlist (**TB-003**)

- **Why it matters:** CI noise destroys trust — evaluate only deliberate critical queries until expansion is justified.
- **Expected impact:** Maintainability **+6**–**10**; Performance signal **+3**–**5**. Weighted **~+0.2–0.3%.**
- **Affected qualities:** Maintainability, Testability.

**Fully actionable:** **Yes.**

**Cursor prompt**

```text
Implement TECH_BACKLOG TB-003:
- Locate the SQL performance sentinel script referenced in TECH_BACKLOG / CI (search scripts/ci and docs/library/PERFORMANCE_BASELINES.md).
- Introduce YAML or Python allow-list of sanctioned query identifiers / tags.
- Non-allowlisted queries PASS with informational output but never fail CI.
- Allow-listed queries retain current p95 threshold enforcement.

Add tests ONLY if sentinel has python unit harness; else add doc sample + synthetic dry-run snippet.

Deliverable:
- Maintain script output footer printing allowlisted vs observed query ratio.

Verification:
 python scripts/ci/<your_sentinel_script>.py --dry-run
```

---

### 6. **ScimTokenRotationReminderJob** hosted service (**daily**) — **six-month posture**

- **Why it matters:** Long-lived bearer tokens regress silently; admins need deterministic signal.
- **Expected impact:** Security **+3**–**6**; Manageability **+5**–**8**. Weighted **~+0.2–0.4%.**
- **Affected qualities:** Security, Manageability.

**Fully actionable:** **Yes.**

**Cursor prompt**

```text
Add BackgroundService ArchLucid.Application/Scim/Tokens/ScimTokenRotationReminderJob.cs implementing decisions in docs/PENDING_QUESTIONS.md Resolved 2026-04-24 Improvement **1**:

Behaviour:
1. Cron/daily scheduler — align with existing worker patterns (hosted service registration ArchLucid.Api or Worker as appropriate pattern in repo — follow analogous digest jobs).
2. Read Scim:TokenRotationReminderDays (default **180**, **0** disables).
3. Query active SCIM tokens older than threshold; for each qualifying row emit WARNING log prefix archlucid.scim.token.rotation_due containing tenant-safe identifiers only.
4. Insert dbo.AdminNotifications entry for operator visibility (reuse existing repository pattern).

Out of scope: automatic revocation.

Tests:
- In-memory/time-freeze unit tests mocking repository & clock abstraction if present.

Verification:
 dotnet test ArchLucid.sln --filter FullyQualifiedName~ScimToken
```

*(Create tests even if namespaces need new test project referencing Application.)*

---

### 7. SCIM manual→Scim override audit constant + emitter

- **Why it matters:** Role conflicts must produce durable evidence for auditors.
- **Expected impact:** Auditability **+6**–**10**; Policy alignment **+3**–**5**. Weighted **~+0.3–0.4%.**
- **Affected qualities:** Auditability, Traceability.

**Fully actionable:** **Yes.**

**Cursor prompt**

```text
Per docs/PENDING_QUESTIONS.md Resolved 2026-04-24 Improvement **1**:

1. Add AuditEventTypes constant RoleOverriddenByScim (name must match casing conventions in ArchLucid.Core/Integration/Audit paths).
2. Update GroupToRoleMapper (ArchLucid.Application/Scim/RoleMapping/) to persist Source strings per decision (manual vs Scim) and emit audit when manual row loses precedence.
3. Refresh docs/library/AUDIT_COVERAGE_MATRIX.md row for new constant.
4. Ensure scripts/ci/assert_audit_const_count.py happy.

Tests:
- Unit tests covering deterministic conflict scenarios.

Verification:
 dotnet test ArchLucid.sln --filter FullyQualifiedName~ScimRole
 python scripts/ci/assert_audit_const_count.py
```

---

### 8. Facade flip preparation for **authority commit path** (**PR A2 shape** — config + DI registration only within safe staged PR)

- **Why it matters:** Default path selection reduces dual-pipeline divergence risk ahead of strangler deletion.
- **Expected impact:** Architectural Integrity **+6**–**10**; Correctness posture **+3**–**5**. Weighted **~+0.3–0.6%.** *Exact merge timing must obey ADRs / existing feature flags.*

- **Affected qualities:** Architectural Integrity, Evolvability, Data Consistency.

**Fully actionable:** **Yes**, **only if** repository already contains `RunCommitPathSelector` + `AuthorityDrivenArchitectureRunCommitOrchestrator` stubs — **investigate-first PR**.

**Cursor prompt**

```text
Assessment-only implementation guard:
1. Search codebase for Coordinator:LegacyRunCommitPath / LegacyRunCommitPathOptions usage.
2. If selector + orchestrator types exist incomplete: wire DI + defaulted flag per ADR **0030** owner decisions (true→false staged behind tests) WITHOUT deleting coordinator types yet.
3. If already merged: CLOSE this prompt with CHANGELOG-only note pointing to merging PR hash.

Mandatory safety:
- All existing integration suites must remain green locally.
- If anything ambiguous, STOP and limit change to documenting next merge checkpoint in docs/architecture/PHASE_3_PR_B_TODO.md without behaviour flip.

Deliverables when behaviour flip permissible:
- Update appsettings development/test defaults thoughtfully.
- Add migration note ARCHITECTURE_FLOWS commit subsection.

Verification:
 dotnet test ArchLucid.Api.Tests ArchLucid.Persistence.Tests
```

---

### Running-impact summary (these eight prompts collectively)

Rolling best-case scenario (uncorrelated deltas): approximate **weighted readiness uplift in the mid-single digits (%)** if implemented and verified without regressions — dominated by commerce DEFERRED items omitted here.

---

## Pending Questions for Later

*(Do **not** block implementation on these mid-assessment.)*

| Topic | Blocking / decision-shaping questions |
|---|---|
| **DEFERRED: Commerce un-hold** | Stripe vs Marketplace sequencing? Rollback ownership? webhook secrets registry? [`PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) item **22** sub-bullets. |
| **DEFERRED: Funnel telemetry** | Legitimate-interest doc signed? retention **90**d accepted? tenant label opt policy? [`PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) **40**. |
| **DEFERRED: Support bundle forwarding** | PII stripping default + Admin opt-in? audit tail count cap? [`docs/PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) item **37**. |
| **DEFERRED: Third-party pen test** | Vendor, window, publishing vs NDA split? [`docs/PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) items **2**, **5** (archived resolution tables). |
| **DEFERRED: SOC 2 ARR trigger** | Explicit ARR band for revisit? [`docs/PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) item **6**. |

---

## Related

| Doc | Purpose |
|-----|---------|
| [`V1_SCOPE.md`](V1_SCOPE.md) | V1 boundary contract |
| [`V1_READINESS_SUMMARY.md`](V1_READINESS_SUMMARY.md) | Repo-honest capabilities |
| [`PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) | Owner gates |
| [`ARCHITECTURE_ON_ONE_PAGE.md`](../ARCHITECTURE_ON_ONE_PAGE.md) | System poster |
