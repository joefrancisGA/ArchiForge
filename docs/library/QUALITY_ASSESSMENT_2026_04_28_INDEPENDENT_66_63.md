> **Scope:** Independent first-principles quality assessment (ArchLucid V1) — weighted readiness 66.63%; not a prior assessment delta, not legal advice; deferred V1.1/V2 per `V1_DEFERRED.md` excluded from score penalties.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md).


# ArchLucid Assessment – Weighted Readiness 66.63%

**Assessment date:** 2026-04-28  
**Method:** Repository and documentation evidence only; no subagents; no reference to prior assessment scores or conclusions.  
**Weighted overall:** **6796 / 10200 = 66.63%** (sum of Score × Weight across 46 qualities, divided by sum of 100 × Weight).

**Category roll-ups:**

| Category | Weight sum | Weighted points | % of category max |
|----------|------------|-----------------|-------------------|
| Commercial | 4000 | 2680 | 67.00% |
| Enterprise | 2500 | 1600 | 64.00% |
| Engineering | 3700 | 2516 | 67.99% |

---

## 1. Executive Summary

**Overall readiness:** ArchLucid presents as a **serious V1-shaped enterprise product** — multi-agent pipeline, SQL-backed authority state, governance, audit exports, Terraform-hosted SaaS story, and unusually deep internal documentation. Weighted readiness **66.63%** reflects **above-average engineering and traceability** dragged down by **adoption friction, time-to-value proof in real LLM mode, ROI publication, workflow embedding versus incumbents’ ITSM suites, and residual correctness/test-depth risk** on high-branch-count paths. Intentional deferrals documented in [`V1_DEFERRED.md`](V1_DEFERRED.md) (live commerce un-hold, published reference customer, pen-test summary publication, PGP key, MCP, first-party Jira/ServiceNow, Slack) are **not scored as V1 gaps**.

**Commercial picture:** Positioning as **“AI Architecture Intelligence”** is clear and differentiated on paper ([`POSITIONING.md`](../go-to-market/POSITIONING.md), [`COMPETITIVE_LANDSCAPE.md`](../go-to-market/COMPETITIVE_LANDSCAPE.md)). Packaging and pricing narrative exist ([`PRICING_PHILOSOPHY.md`](../go-to-market/PRICING_PHILOSOPHY.md), [`PRODUCT_PACKAGING.md`](PRODUCT_PACKAGING.md)). **Revenue friction** remains **buyer education** (new category), **Azure-centrism**, and **absence of at-scale customer proof** in public artifacts (explicitly V1.1-deferred per `V1_DEFERRED.md`). **Sales-led** motion is honest; self-serve trial plumbing and merge-blocking live E2E exist per [`V1_READINESS_SUMMARY.md`](V1_READINESS_SUMMARY.md).

**Enterprise picture:** **Traceability and audit mechanics are strengths** (typed events, append-only posture, matrices, export APIs). **Procurement-facing packs** are advanced for a pre-revenue repo ([`PROCUREMENT_EVIDENCE_PACK_INDEX.md`](../go-to-market/PROCUREMENT_EVIDENCE_PACK_INDEX.md), [`CURRENT_ASSURANCE_POSTURE.md`](../go-to-market/CURRENT_ASSURANCE_POSTURE.md)). **Trust and compliance** still rely on **roadmaps and internal self-assessments** where buyers want **independent attestations** (SOC 2 Type I in-flight; pen-test redacted summary V1.1-deferred per policy). **Workflow embeddedness** underperforms **EAM/ITSM incumbents** on connector breadth; V1 expects **webhooks + REST** for gaps explicitly deferred to V1.1/V2.

**Engineering picture:** **Modular .NET solution**, Dapper-oriented persistence, **strict CI** (coverage floors, ZAP, CodeQL, Trivy, audit constant guards, doc header budgets). **Correctness risk** concentrates in **branch/mutation depth** and **concurrency/edge paths** called out historically ([`CORRECTNESS_QUALITY_ASSESSMENT_2026_04_15.md`](CORRECTNESS_QUALITY_ASSESSMENT_2026_04_15.md), [`DATA_CONSISTENCY_MATRIX.md`](DATA_CONSISTENCY_MATRIX.md)). **Operational maturity** (observability metrics, runbooks) is strong; **multi-region product guarantees** are explicitly non-goals for V1 ([`V1_SCOPE.md`](V1_SCOPE.md)).

---

## 2. Weighted Quality Assessment

**Ordering:** Qualities sorted by **weighted deficiency signal** = Weight × (100 − Score). Higher = more urgent (weighted importance of the gap).

**Notation:**

- **Weighted deficiency signal:** Weight × (100 − Score). Larger = more urgent to fix.
- **Weighted contribution to max readiness:** (Score × Weight) ÷ 10200 × 100 (percentage points of the 100% ceiling attributable to this dimension).

---

### 2.1 Commercial (ordered by urgency)

#### 1) Adoption Friction — Score 60 / Weight 6

| Field | Value |
|-------|------|
| Weighted deficiency signal | **240** |
| Weighted contribution to max readiness | **3.53%** |

**Justification:** Self-hosted path implies **SQL, containers or App Service stack, auth configuration, optional Azure OpenAI for real mode**. Enterprise pilots require **Entra alignment, RLS posture, and operator training** across Pilot vs Operate surfaces ([`PRODUCT_PACKAGING.md`](PRODUCT_PACKAGING.md)). Progressive disclosure helps but **surface area remains large** for first-time teams.

**Tradeoffs:** Frictionless defaults (e.g. `DevelopmentBypass`) reduce pilot setup but **fight enterprise security reviewers**; fail-closed production defaults are correct but **increase upfront work**.

Improvements: guided **Core Pilot checklist** in operator shell; **wizard resumption**; tighten “first 30 minutes” docs to one executable path; **hosted trial** polish (merge-blocking E2E already per `V1_READINESS_SUMMARY.md`).  
**Fixability:** **v1** (engineering + docs).

---

#### 2) Time-to-Value — Score 68 / Weight 7

| Field | Value |
|-------|------|
| Weighted deficiency signal | **224** |
| Weighted contribution to max readiness | **4.67%** |

**Justification:** **Simulator mode** delivers sub-minute demos; **real mode** is **30s–5min** design target ([`REAL_MODE_BENCHMARK.md`](REAL_MODE_BENCHMARK.md)) but **environment-dependent** (TPM, model, region). Trial design targets **&lt; 5 minutes** ([`TRIAL_AND_SIGNUP.md`](../go-to-market/TRIAL_AND_SIGNUP.md)); **proving that in buyer-controlled environments** is not automatic.

**Tradeoffs:** Faster demos favor **simulator + seeded data**; buyer trust **needs real LLM receipts** — cost and variance rise.

Improvements: **publish repeatable benchmark artifacts** (k6 / scripted) with environment metadata; in-product **“time to first manifest”** tile using server metrics; **pre-seeded sample run** guaranteed on first login.  
**Fixability:** **v1**.

---

#### 3) Marketability — Score 72 / Weight 8

| Field | Value |
|-------|------|
| Weighted deficiency signal | **224** |
| Weighted contribution to max readiness | **5.65%** |

**Justification:** Strong **category narrative**, proof routes (`/why-archlucid`, `/demo/explain`), structured claims in [`POSITIONING.md`](../go-to-market/POSITIONING.md). **Azure-only** and **new category** increase **sales-cycle education cost**. **Public reference customer** explicitly **V1.1** per [`V1_DEFERRED.md`](V1_DEFERRED.md) — **not** penalized here.

**Tradeoffs:** Sharp differentiation vs EAM tools **narrows ICP**; claiming multi-cloud would be dishonest.

Improvements: **sponsor-ready proof pack** (single PDF/JSON backed by live metrics); **vertical wedge** one-pagers (healthcare brief exists — replicate pattern); tighten “do/don’t” messaging in GTM docs.  
**Fixability:** **v1** (content + light API aggregation).

---

#### 4) Proof-of-ROI Readiness — Score 66 / Weight 5

| Field | Value |
|-------|------|
| Weighted deficiency signal | **170** |
| Weighted contribution to max readiness | **3.24%** |

**Justification:** [`ROI_MODEL.md`](../go-to-market/ROI_MODEL.md), [`PILOT_ROI_MODEL.md`](PILOT_ROI_MODEL.md), baseline fields on registration — **strong template foundation**. **Customer-measured deltas** and **published case studies** remain thin in-repo (synthetic templates exist; **named reference publication V1.1-deferred** — not penalized).

**Tradeoffs:** Credible ROI **requires customer data**; overly precise claims **create legal/review risk**.

Improvements: **aggregate pilot scorecard API** + export; **before/after dashboard** wired to stored baselines; **worked example** with explicit assumptions banner.  
**Fixability:** **v1** for instrumentation; **blocked** on customer numbers for *external* proof labels.

---

#### 5) Executive Value Visibility — Score 68 / Weight 4

| Field | Value |
|-------|------|
| Weighted deficiency signal | **128** |
| Weighted contribution to max readiness | **2.67%** |

**Justification:** [`EXECUTIVE_SPONSOR_BRIEF.md`](../EXECUTIVE_SPONSOR_BRIEF.md), board pack endpoints referenced in packaging, **Why ArchLucid** snapshot flows — **good scaffolding**. Execs still **must trust technical intermediaries** unless **one-glance KPI surfacing** is ubiquitous in-product.

**Tradeoffs:** Rich exec UI **adds maintenance**; too-simple KPIs **misrepresent** probabilistic LLM outputs.

Improvements: **single JSON “sponsor evidence pack”** endpoint + **/why-archlucid** hardening with live tenant-safe aggregates; **email-to-sponsor** polish.  
**Fixability:** **v1**.

---

#### 6) Differentiability — Score 74 / Weight 4

| Field | Value |
|-------|------|
| Weighted deficiency signal | **104** |
| Weighted contribution to max readiness | **2.90%** |

**Justification:** **Explainability trace + governance + append-only audit** is a **credible wedge** vs ad-hoc AI and vs EAM catalogs ([`COMPETITIVE_LANDSCAPE.md`](../go-to-market/COMPETITIVE_LANDSCAPE.md)). **Connector breadth** intentionally lags incumbents (some **V1.1/V2** — not penalized).

**Tradeoffs:** Saying “we replace LeanIX” would be wrong; **complement** positioning is slower to land budget.

Improvements: **side-by-side RFP language** (checklist: governance, audit, model routing, deterministic replay); **export bundles** that drop into customer doc systems *without* native Confluence connector yet.  
**Fixability:** **v1** (docs + exports); native Confluence **v1.1**.

---

#### 7) Decision Velocity — Score 58 / Weight 2

| Field | Value |
|-------|------|
| Weighted deficiency signal | **84** |
| Weighted contribution to max readiness | **1.14%** |

**Justification:** **Quote request** path and **order form** exist; **live checkout un-hold V1.1-deferred** per [`V1_DEFERRED.md`](V1_DEFERRED.md) — **not** penalized. Remaining friction is **enterprise security/legal cycle**, not missing **402/Commerce wiring** in code.

**Tradeoffs:** Faster procurement **needs** standardized assurance packs vs **custom DPA** reviews.

Improvements: **pre-filled SIG/CAIQ** already strong — add **“common objections + answers”** one-pager per industry.  
**Fixability:** **v1** (sales enablement).

---

#### 8) Commercial Packaging Readiness — Score 64 / Weight 2

| Field | Value |
|-------|------|
| Weighted deficiency signal | **72** |
| Weighted contribution to max readiness | **1.25%** |

**Justification:** Tiers and **feature matrix** documented; **code enforcement** uses `[RequiresCommercialTenantTier]` with **intentional 404** obfuscation ([`COMMERCIAL_ENFORCEMENT_DEBT.md`](COMMERCIAL_ENFORCEMENT_DEBT.md)). **SKU ↔ endpoint matrix** completeness remains a **maintained inventory problem**.

**Tradeoffs:** Strict gating **breaks sloppy demos**; loose gating **creates entitlement disputes**.

Improvements: **expand attribute coverage** + **contract tests** per tier; document **404 vs 402** behavior for sales engineers.  
**Fixability:** **v1**.

---

#### 9) Stickiness — Score 68 / Weight 1

| Field | Value |
|-------|------|
| Weighted deficiency signal | **32** |
| Weighted contribution to max readiness | **0.67%** |

**Justification:** **Golden manifests, findings history, governance records, audit** create switching costs **when adopted**. Early pilot stickiness **depends on workflow habit**, not raw features.

**Tradeoffs:** Stickiness via **data lock-in** must be paired with **export/exit** clarity for trust.

Improvements: **scheduled digests**, **Teams** route depth ([`V1_SCOPE.md`](V1_SCOPE.md)); **comparison replay** in CI templates.  
**Fixability:** **v1**.

---

#### 10) Template and Accelerator Richness — Score 58 / Weight 1

| Field | Value |
|-------|------|
| Weighted deficiency signal | **42** |
| Weighted contribution to max readiness | **0.57%** |

**Justification:** CLI + **seven-step** request flow + **Contoso** demo seed — decent. **Industry archetypes** and **policy pack starter sets** could be richer for **non-greenfield** buyers.

**Tradeoffs:** More templates **increase maintenance** when engines evolve.

Improvements: **versioned “starter packs”** (policy + sample request JSON); **vertical snippets** in docs.  
**Fixability:** **v1** (content).

---

### 2.2 Enterprise (ordered by urgency)

#### 11) Workflow Embeddedness — Score 55 / Weight 3

| Field | Value |
|-------|------|
| Weighted deficiency signal | **135** |
| Weighted contribution to max readiness | **1.62%** |

**Justification:** **GitHub Actions + Azure DevOps** decoration paths exist; **Teams** first-party; **webhooks/CloudEvents** for custom consumers. **Jira/ServiceNow first-party V1.1**; **Slack V2** ([`V1_DEFERRED.md`](V1_DEFERRED.md)) — **not** penalized. Still, **buyers compare to ServiceNow-native workflows**.

**Tradeoffs:** Native ITSM connectors **explode test matrix**; webhooks **push work to customer integration teams**.

Improvements: **Jira webhook bridge recipe** (V1-legal pattern); **ServiceNow scripted REST** example; **“minimum viable integration”** architecture diagram in buyer deck.  
**Fixability:** **v1** (docs + samples).

---

#### 12) Usability — Score 62 / Weight 3

| Field | Value |
|-------|------|
| Weighted deficiency signal | **114** |
| Weighted contribution to max readiness | **1.82%** |

**Justification:** **Progressive disclosure** and **role-aware nav** are real ([`PRODUCT_PACKAGING.md`](PRODUCT_PACKAGING.md)). **Operate** depth **overwhelms** new operators; **Pilot guide** redirects to quickstart ([`PILOT_GUIDE.md`](PILOT_GUIDE.md)).

**Tradeoffs:** Simpler UX **hides governance value**; exposing everything **raises cognitive load**.

Improvements: **first-run wizard** state machine; contextual **LayerHeader** text everywhere; **task-focused home**.  
**Fixability:** **v1**.

---

#### 13) Trustworthiness — Score 64 / Weight 3

| Field | Value |
|-------|------|
| Weighted deficiency signal | **108** |
| Weighted contribution to max readiness | **1.88%** |

**Justification:** **Strong internal assurance story** ([`CURRENT_ASSURANCE_POSTURE.md`](../go-to-market/CURRENT_ASSURANCE_POSTURE.md), [`SECURITY.md`](SECURITY.md)). **Third-party pen-test report public summary V1.1-deferred** — **not** penalized. Buyers still anchor on **external attestation**.

**Tradeoffs:** Faster trust **requires spend** (assessments, audits); delaying **preserves burn** but **slows enterprise sales**.

Improvements: ship **interim owner assessment** in every procurement ZIP; **explicit model risk disclaimers** adjacent to findings UI.  
**Fixability:** **v1** (packaging); **v1.1** for external pen-test summary per deferral.

---

#### 14) Traceability — Score 76 / Weight 3

| Field | Value |
|-------|------|
| Weighted deficiency signal | **72** |
| Weighted contribution to max readiness | **2.24%** |

**Justification:** **Explainability trace**, **provenance graph**, **finding inspector**, OTel completeness metrics ([`EXPLAINABILITY.md`](EXPLAINABILITY.md), [`OBSERVABILITY.md`](OBSERVABILITY.md)) — **above typical AI SaaS**.

**Tradeoffs:** Deeper traces **increase PII surface**; redaction **complicates forensics**.

Improvements: unify **“single drill-down link”** from every finding to inspector + manifest node; **correlation id** surfacing in UI exports.  
**Fixability:** **v1**.

---

#### 15) Auditability — Score 72 / Weight 2

| Field | Value |
|-------|------|
| Weighted deficiency signal | **56** |
| Weighted contribution to max readiness | **1.41%** |

**Justification:** **Matrix + append-only + export** ([`AUDIT_COVERAGE_MATRIX.md`](AUDIT_COVERAGE_MATRIX.md)). **Dual channels** (durable SQL vs baseline log-only) create **reviewer questions** unless matrix is exact.

**Tradeoffs:** Mandatory durable audit on every path **risks availability** on audit store outages; **async best-effort** paths exist by design.

Improvements: **close high-value gaps** (mutations with only baseline log) where **compliance commitments** require durable rows; **runbook** for “audit missed” recovery.  
**Fixability:** **v1** (targeted code).

---

#### 16) Policy and Governance Alignment — Score 70 / Weight 2

| Field | Value |
|-------|------|
| Weighted deficiency signal | **60** |
| Weighted contribution to max readiness | **1.37%** |

**Justification:** **Policy packs, pre-commit gate, Segregation of Duties** are **real product depth** ([`V1_SCOPE.md`](V1_SCOPE.md)). Customer policy maturity varies; **pack authoring** still requires **expert users**.

**Tradeoffs:** More automation in policy authoring **increases hallucination risk** unless grounded in customer corpora.

Improvements: **starter policy packs** + import validation UX; **export “policy satisfaction evidence”** with manifest.  
**Fixability:** **v1**.

---

#### 17) Compliance Readiness — Score 58 / Weight 2

| Field | Value |
|-------|------|
| Weighted deficiency signal | **84** |
| Weighted contribution to max readiness | **1.14%** |

**Justification:** **CAIQ-lite, SIG Core, SOC2 self-assessment, DSAR process** exist. **No SOC 2 Type I in hand yet** (in-flight per assurance posture) — **real procurement friction**.

**Tradeoffs:** Compliance spend **lags revenue** in startups; absence **blocks regulated buyers**.

Improvements: **complete Type I trajectory**; map **controls to shipped features** in one table (partially exists — tighten deltas).  
**Fixability:** **v1** for artifacts; **blocked** on auditor calendar/budget for opinion.

---

#### 18) Procurement Readiness — Score 60 / Weight 2

| Field | Value |
|-------|------|
| Weighted deficiency signal | **80** |
| Weighted contribution to max readiness | **1.18%** |

**Justification:** **Evidence pack index**, DPA/MSA templates, subprocessors — **strong**. Buyers still **customize** everything.

**Tradeoffs:** Over-standardization **slows weird deals**; under-standardization **kills velocity**.

Improvements: **FAQ appendix** for **Azure Data Processing**, **subprocessor change notification**, **retention**.  
**Fixability:** **v1**.

---

#### 19) Interoperability — Score 58 / Weight 2

| Field | Value |
|-------|------|
| Weighted deficiency signal | **84** |
| Weighted contribution to max readiness | **1.14%** |

**Justification:** **REST + webhooks + SCIM** strong; **MCP V1.1-deferred** ([`V1_DEFERRED.md`](V1_DEFERRED.md)) — **not** penalized. **No universal EA repository import** — honest gap for “brownfield EA” buyers.

**Tradeoffs:** Connectors **multiply CVE and authN surfaces**.

Improvements: **OpenAPI client generation** examples; **postman collections** maintained in CI.  
**Fixability:** **v1**.

---

#### 20) Accessibility — Score 65 / Weight 1

| Field | Value |
|-------|------|
| Weighted deficiency signal | **35** |
| Weighted contribution to max readiness | **0.64%** |

**Justification:** **Merge-blocking axe** on live E2E paths ([`ACCESSIBILITY_AUDIT.md`](ACCESSIBILITY_AUDIT.md)). **Full WCAG journey** beyond instrumented pages **not guaranteed**.

**Tradeoffs:** Stricter gates **slow UI iteration**; loosening **risks regulated customers**.

Improvements: expand **PAGES** coverage; **manual keyboard audit** for governance tables.  
**Fixability:** **v1**.

---

#### 21) Customer Self-Sufficiency — Score 60 / Weight 1

| Field | Value |
|-------|------|
| Weighted deficiency signal | **40** |
| Weighted contribution to max readiness | **0.59%** |

**Justification:** **CLI doctor/support-bundle**, deep docs — power users succeed. **Average enterprise** still opens tickets without **guided triage trees**.

**Tradeoffs:** Self-serve depth **can expose footguns** (RLS bypass flags, dangerous admin APIs).

Improvements: **in-product “what broke?” decision tree** linked to **correlation id**; **operator health dashboard** presets.  
**Fixability:** **v1**.

---

#### 22) Change Impact Clarity — Score 68 / Weight 1

| Field | Value |
|-------|------|
| Weighted deficiency signal | **32** |
| Weighted contribution to max readiness | **0.67%** |

**Justification:** [`BREAKING_CHANGES.md`](../../BREAKING_CHANGES.md), ADRs — good. Frequent **rename phases** (Phase 7) create **operator confusion** if not **uniformly finished**.

**Tradeoffs:** Aggressive refactors **improve long-term clarity** at **short-term migration cost**.

Improvements: **semver + API deprecation headers** policy enforcement in CI where applicable.  
**Fixability:** **v1**.

---

### 2.3 Engineering (ordered by urgency)

#### 23) Correctness — Score 64 / Weight 4

| Field | Value |
|-------|------|
| Weighted deficiency signal | **144** |
| Weighted contribution to max readiness | **2.51%** |

**Justification:** **Strict merged coverage gates** in CI per [`CODE_COVERAGE.md`](CODE_COVERAGE.md); historical snapshots showed **merged branch % below aspiration** on local runs; **mutation survival** material. **Governance concurrency** has targeted tests; **many mutation surfaces** lack parallel tests ([`CORRECTNESS_QUALITY_ASSESSMENT_2026_04_15.md`](CORRECTNESS_QUALITY_ASSESSMENT_2026_04_15.md)).

**Tradeoffs:** More integration tests **lengthen CI**; heavier property tests **raise flakiness**.

Improvements: **Persistence relational read tests**; **API branch coverage** on validation + error paths; **409 concurrency** tests on hot entities.  
**Fixability:** **v1**.

---

#### 24) Security — Score 70 / Weight 3

| Field | Value |
|-------|------|
| Weighted deficiency signal | **90** |
| Weighted contribution to max readiness | **2.06%** |

**Justification:** **ZAP + CodeQL + Trivy + Schemathesis** posture ([`SECURITY.md`](SECURITY.md), [`CURRENT_ASSURANCE_POSTURE.md`](../go-to-market/CURRENT_ASSURANCE_POSTURE.md)). **Content safety** mandatory in production-like hosts. **RLS** documented; **break-glass** paths monitored.

**Tradeoffs:** **Defense in depth** increases **ops cost**; **pen-test NDA model** **slows** uninformed buyers (V1.1 public summary deferred — not scored).

Improvements: **continuous API fuzz** on more phases if budget allows; **SBOM diff** in release notes.  
**Fixability:** **v1** engineering; **v1.1** external pen-test packaging per deferral.

---

#### 25) Architectural Integrity — Score 72 / Weight 3

| Field | Value |
|-------|------|
| Weighted deficiency signal | **84** |
| Weighted contribution to max readiness | **2.12%** |

**Justification:** **ADRs**, strangler patterns, **authority convergence** complete per [`DATA_CONSISTENCY_MATRIX.md`](DATA_CONSISTENCY_MATRIX.md). **Dual manifest interface families** still require **careful contributor onboarding** ([`ARCHITECTURE_COMPONENTS.md`](ARCHITECTURE_COMPONENTS.md)).

**Tradeoffs:** Big-bang unification **would stall shipping**; gradual stranglers **leave seams**.

Improvements: **Roslyn/architecture test** enforcing “no new coordinator-only HTTP routes” per [`V1_SCOPE.md`](V1_SCOPE.md).  
**Fixability:** **v1**.

---

#### 26) Reliability — Score 66 / Weight 2

| Field | Value |
|-------|------|
| Weighted deficiency signal | **68** |
| Weighted contribution to max readiness | **1.29%** |

**Justification:** **Health endpoints, circuit breakers, retries** documented; **outbox** semantics **at-least-once** honest ([`DATA_CONSISTENCY_MATRIX.md`](DATA_CONSISTENCY_MATRIX.md)). **Audit path** sometimes **best-effort async** — correct tradeoff but **auditors ask**.

**Tradeoffs:** Exactly-once **everywhere** is **often infeasible** with external LLMs and email/webhooks.

Improvements: **idempotency keys** on webhook consumers expanded; **chaos test** list execution quarterly.  
**Fixability:** **v1**.

---

#### 27) Data Consistency — Score 66 / Weight 2

| Field | Value |
|-------|------|
| Weighted deficiency signal | **68** |
| Weighted contribution to max readiness | **1.29%** |

**Justification:** **Orphan probe + quarantine** metrics ([`OBSERVABILITY.md`](OBSERVABILITY.md)); **read replica lag** documented; **cache invalidation** bounded. **Application-level FK** discipline **relies on repo correctness**.

**Tradeoffs:** DB-enforced cascades **complicate sharding/partitioning** later.

Improvements: **periodic consistency report** endpoint for admins; expand **integration tests** for archival cascades.  
**Fixability:** **v1**.

---

#### 28) Maintainability — Score 68 / Weight 2

| Field | Value |
|-------|------|
| Weighted deficiency signal | **64** |
| Weighted contribution to max readiness | **1.33%** |

**Justification:** **House style, modular projects, LINQ preference** — consistent **contributor signals**. **Large solution** can **slow inner-loop builds** for casual contributors.

**Tradeoffs:** More projects **clarifies seams** but **taxes CI graph**.

Improvements: **local fast test** scripts already — publish **“3 commands to green”** in root README fold-out.  
**Fixability:** **v1**.

---

#### 29) Explainability — Score 72 / Weight 2

| Field | Value |
|-------|------|
| Weighted deficiency signal | **56** |
| Weighted contribution to max readiness | **1.41%** |

**Justification:** **Inspector endpoint**, faithfulness metrics, citation chips — **strong**. Narratives can still **feel LLM-ish** when faithfulness falls back.

**Tradeoffs:** Deterministic fallback text **is safer** but **less engaging**.

Improvements: **always show “evidence chips”** even when narrative is fallback; **explicit UI badge** when heuristic faithfulness low.  
**Fixability:** **v1**.

---

#### 30) AI/Agent Readiness — Score 68 / Weight 2

| Field | Value |
|-------|------|
| Weighted deficiency signal | **64** |
| Weighted contribution to max readiness | **1.33%** |

**Justification:** **Multi-provider**, **simulator**, **quality gate hooks**, **prompt regression** CI ([`SECURITY.md`](SECURITY.md)). **Agents are orchestrated, not autonomous** — correct boundary ([`POSITIONING.md`](../go-to-market/POSITIONING.md)).

**Tradeoffs:** More autonomy **increases incident blast radius** in regulated accounts.

Improvements: **tool-call allowlists** if/when expanding agent tool surfaces; **eval harness** expansion per [`AGENT_OUTPUT_EVALUATION.md`](AGENT_OUTPUT_EVALUATION.md).  
**Fixability:** **v1**.

---

#### 31) Azure Compatibility and SaaS Deployment Readiness — Score 72 / Weight 2

| Field | Value |
|-------|------|
| Weighted deficiency signal | **56** |
| Weighted contribution to max readiness | **1.41%** |

**Justification:** **14 Terraform modules** narrative ([`CURRENT_ASSURANCE_POSTURE.md`](../go-to-market/CURRENT_ASSURANCE_POSTURE.md)), Container Apps, SQL, Front Door — **credible Day-0–2 story**. **Customer-specific networking** still **custom work**.

**Tradeoffs:** Fully generic K8s **not promised** — reduces **portability market**.

Improvements: **`apply-saas.ps1` smoke** recorded as mandatory pilot gate where hosted; **environment parity checklist**.  
**Fixability:** **v1**.

---

#### 32) Cognitive Load — Score 58 / Weight 1

| Field | Value |
|-------|------|
| Weighted deficiency signal | **42** |
| Weighted contribution to max readiness | **0.57%** |

**Justification:** Two Operate groups + **Execute+** floor **is correct engineering** but **taxes new users** ([`PRODUCT_PACKAGING.md`](PRODUCT_PACKAGING.md)).

**Tradeoffs:** Oversimplification **hides governance**; over-documentation **walls of text**.

Improvements: **task-based nav mode** (“I need to compare two runs”) vs **module nav**.  
**Fixability:** **v1** (UX experiment).

---

#### 33) Availability — Score 64 / Weight 1

| Field | Value |
|-------|------|
| Weighted deficiency signal | **36** |
| Weighted contribution to max readiness | **0.63%** |

**Justification:** **99.9% hosted target** narrative ([`SLA_TARGETS.md`](SLA_TARGETS.md)); **probes** exist. **Pre-GA** honesty helps; **customers still ask for DR proof**.

**Tradeoffs:** Multi-region active/active **explicitly not V1** — correct deferral ([`V1_SCOPE.md`](V1_SCOPE.md)).

Improvements: align **API-only SLO doc** vs **API+UI** doc or explain delta in one paragraph (**known doc tension** in [`SLA_TARGETS.md`](SLA_TARGETS.md)).  
**Fixability:** **v1**.

---

#### 34) Performance — Score 66 / Weight 1

| Field | Value |
|-------|------|
| Weighted deficiency signal | **34** |
| Weighted contribution to max readiness | **0.65%** |

**Justification:** **In-process perf tests** meaningful for regressions; **real-mode** dominated by LLM ([`PERFORMANCE_BASELINES.md`](PERFORMANCE_BASELINES.md), [`REAL_MODE_BENCHMARK.md`](REAL_MODE_BENCHMARK.md)).

**Tradeoffs:** Aggressive caching **risks staleness**; weak caching **burns tokens**.

Improvements: publish **one official staging k6 artifact** post-`load-test` workflow; track **p95 execute time** per model deployment.  
**Fixability:** **v1**.

---

#### 35) Scalability — Score 62 / Weight 1

| Field | Value |
|-------|------|
| Weighted deficiency signal | **38** |
| Weighted contribution to max readiness | **0.61%** |

**Justification:** **Horizontal Container Apps**, **read replicas**, **outbox** — plausible. **In-memory job queue** caveat for single-instance paths.

**Tradeoffs:** Service Bus everywhere **raises cost** for tiny tenants.

Improvements: **document scale thresholds** where **must enable Service Bus**; **load test** for N concurrent runs.  
**Fixability:** **v1**.

---

#### 36) Supportability — Score 70 / Weight 1

| Field | Value |
|-------|------|
| Weighted deficiency signal | **30** |
| Weighted contribution to max readiness | **0.69%** |

**Justification:** **Support bundle**, correlation, **runbooks** — strong for vendor ops.

**Tradeoffs:** Rich internal logs **risk PII leakage** if bundles mishandled.

Improvements: **bundle redactor** mode for external sharing.  
**Fixability:** **v1**.

---

#### 37) Manageability — Score 64 / Weight 1

| Field | Value |
|-------|------|
| Weighted deficiency signal | **36** |
| Weighted contribution to max readiness | **0.63%** |

**Justification:** Many knobs **correctly in config**; **admin surfaces** exist but **distributed** across operator pages.

**Tradeoffs:** Single “control plane” UI **large build** for V1.

Improvements: **admin hub** page summarizing **feature flags + quotas**; **config export** (sanitized).  
**Fixability:** **v1**.

---

#### 38) Deployability — Score 68 / Weight 1

| Field | Value |
|-------|------|
| Weighted deficiency signal | **32** |
| Weighted contribution to max readiness | **0.67%** |

**Justification:** **Docker/Compose + Terraform** — good. **Real enterprise deploy** still needs **Key Vault, private endpoints, Entra** wiring discipline.

**Tradeoffs:** One-click **only possible** for **opinionated** reference stacks.

Improvements: **`terraform validate` CI** already — add **reference cost estimate** per module README.  
**Fixability:** **v1**.

---

#### 39) Observability — Score 72 / Weight 1

| Field | Value |
|-------|------|
| Weighted deficiency signal | **28** |
| Weighted contribution to max readiness | **0.71%** |

**Justification:** **Rich custom metrics** ([`OBSERVABILITY.md`](OBSERVABILITY.md)), Grafana in repo.

**Tradeoffs:** Metric cardinality **can explode** with irresponsible labels — watch **`agent_type`** style patterns.

Improvements: **SLO burn rate** dashboards linked from Trust Center internal appendix.  
**Fixability:** **v1**.

---

#### 40) Testability — Score 66 / Weight 1

| Field | Value |
|-------|------|
| Weighted deficiency signal | **34** |
| Weighted contribution to max readiness | **0.65%** |

**Justification:** **Trait-based test tiers**, SQL containers — mature. **Flaky tests** are the enemy at this scale.

**Tradeoffs:** More E2E **adds flake**; fewer E2E **miss regressions**.

Improvements: **weekly flake report** from CI; **quarantine policy** with ticket link.  
**Fixability:** **v1**.

---

#### 41) Modularity — Score 70 / Weight 1

| Field | Value |
|-------|------|
| Weighted deficiency signal | **30** |
| Weighted contribution to max readiness | **0.69%** |

**Justification:** **Clear bounded contexts** documented; **Composition root** patterns used.

**Tradeoffs:** **Too many small assemblies** can confuse new hires.

Improvements: **dependency-cruiser** or equivalent **arch tests** for forbidden edges (incremental).  
**Fixability:** **v1**.

---

#### 42) Extensibility — Score 68 / Weight 1

| Field | Value |
|-------|------|
| Weighted deficiency signal | **32** |
| Weighted contribution to max readiness | **0.67%** |

**Justification:** **Policy packs, alert rules, LLM providers** — extension points are real.

**Tradeoffs:** Extension without **versioning** yields **silent breakage**.

Improvements: **semantic versioning** for **policy pack schema**; migration tooling for packs.  
**Fixability:** **v1**.

---

#### 43) Evolvability — Score 68 / Weight 1

| Field | Value |
|-------|------|
| Weighted deficiency signal | **32** |
| Weighted contribution to max readiness | **0.67%** |

**Justification:** **DbUp migrations** + **master DDL** discipline ([`SQL_DDL_DISCIPLINE.md`](SQL_DDL_DISCIPLINE.md)); ADRs track **reasoning**.

**Tradeoffs:** **Historical migration immutability** increases **doc debt** for new contributors — acceptable trade.

Improvements: **“start here for schema change”** flowchart in [`SQL_DDL_DISCIPLINE.md`](SQL_DDL_DISCIPLINE.md).  
**Fixability:** **v1**.

---

#### 44) Documentation — Score 76 / Weight 1

| Field | Value |
|-------|------|
| Weighted deficiency signal | **24** |
| Weighted contribution to max readiness | **0.75%** |

**Justification:** **Large library**, CI scope headers, inventories — unusually strong.

**Tradeoffs:** **Finding the right doc** is hard — navigation **is the product**.

Improvements: **role-based doc portals** (SRE vs SE vs buyer) — thin index pages only.  
**Fixability:** **v1**.

---

#### 45) Azure Ecosystem Fit — Score 74 / Weight 1

| Field | Value |
|-------|------|
| Weighted deficiency signal | **26** |
| Weighted contribution to max readiness | **0.73%** |

**Justification:** **Entra, SCIM, AOAI, ACA, SQL, Service Bus** — **native-first** strategy executed.

**Tradeoffs:** **Weak story for non-Azure** buyers — intentional.

Improvements: **AWS/GCP** honestly marked **not V1** in RFP boilerplate (avoid time waste).  
**Fixability:** **v1** (positioning).

---

#### 46) Cost-Effectiveness — Score 64 / Weight 1

| Field | Value |
|-------|------|
| Weighted deficiency signal | **36** |
| Weighted contribution to max readiness | **0.63%** |

**Justification:** **Simulator + caching + quotas**; **per-tenant cost** endpoints exist per commercial inventory ([`COMMERCIAL_ENFORCEMENT_DEBT.md`](COMMERCIAL_ENFORCEMENT_DEBT.md)).

**Tradeoffs:** Aggressive savings **hurt output quality**; conservative models **raise COGS**.

Improvements: **tenant-level COGS dashboard** + export for FinOps review.  
**Fixability:** **v1**.

---

## 3. Top 10 Most Important Weaknesses (cross-cutting)

1. **Adoption friction vs surface area** — Rich Operate layer + real Azure dependencies + auth modes = **long path to confident production use** for median teams.
2. **Proof of value in real LLM mode** — Simulator proves architecture; **buyers bet on real analysis latency/cost/quality**, still **environment-variable heavy**.
3. **ROI story is template-strong, customer-measurement-weak** — credible worksheets exist; **externally verifiable deltas** scarce (reference deferred to V1.1 by policy).
4. **Workflow embedding gap vs ITSM-centric enterprises** — Webhooks help; **teams mentally compare** to Jira/ServiceNow depth (first-party deferred V1.1).
5. **Correctness confidence tails** — High branch-count domains (persistence readers, API validation matrices) need **more adversarial tests**.
6. **Compliance attestation lag** — Artifacts are good; **independent SOC 2 / pen-test consumable by procurement** still catching up (pen-test public summary V1.1 per deferral).
7. **Cognitive load / operator overwhelm** — Progressive disclosure helps; **new user path** still easy to get lost between Pilot and Operate.
8. **Dual audit channels story** — Durable SQL vs baseline log-only paths require **careful buyer explanation** to avoid “false sense of tamper evidence”.
9. **Hosted SLO narrative split** — API-only vs API+UI targets **need single-page reconciliation** for picky buyers ([`SLA_TARGETS.md`](SLA_TARGETS.md)).
10. **Interoperability with brownfield EA repositories** — No “import from LeanIX/Ardoq” story — **positioning must set expectations**.

---

## 4. Top 5 Monetization Blockers

1. **Category creation tax** — Buyers must learn **AI Architecture Intelligence**; **longer education** than point tools.
2. **Buyer-side proof density** — Without **published named wins** (V1.1-deferred), **expansion is founder-led trust transfer**.
3. **Azure-centrism** — Non-Azure shops **pause** or **discount** the solution regardless of technical merit.
4. **Procurement clock** — Strong drafts exist, but **independent attestations** still **gate POs** in regulated segments.
5. **Operational proof for “replace meeting-based review”** — Needs **repeatable time-to-manifest evidence** in **customer-like** configs, not just lab.

---

## 5. Top 5 Enterprise Adoption Blockers

1. **Security review depth** — LLM data flows, retention, **subprocessors**, and **RLS operational proof** are heavy questions.
2. **ITSM/workflow expectations** — Approvals in ArchLucid **don’t replace** ServiceNow/Jira **unless** integrated (first-party V1.1).
3. **Compliance packaging timing** — SIG/CAIQ help; **SOC 2 Type I** timing still a **gating question** for many GRC teams.
4. **Identity integration complexity** — Entra + SCIM is great when customer **has clean IHRP practices**; **messy HR = costly** onboarding.
5. **Brownfield architecture data** — Import gaps mean **first value** may require **manual context entry** — raises change-management pain.

---

## 6. Top 5 Engineering Risks

1. **Branch / edge-path correctness** — Under-tested conditionals in **manifest/findings/governance** could emit **wrong compliance posture**.
2. **Concurrency on hot entities** — Limited **parallel-write tests** outside known governance paths.
3. **Eventually consistent reads** — Replica lag + caches; UI must **avoid deceptive freshness** on critical approvals.
4. **Audit durability semantics** — Fire-and-forget / retry exhaustion paths are **explicit** — **must match** contractual promises in deals.
5. **LLM dependency failures** — Circuit breakers help; **degraded output quality** still a **product incident**, not just an infra blip.

---

## 7. Most Important Truth

**ArchLucid’s readiness is “strong prototype core with enterprise-grade paperwork and telemetry,” but revenue and broad enterprise adoption still hinge on shortening the distance between that technical depth and a buyer’s emotionally simple proof: a fast, credible, real-mode architecture review package that survives procurement and slots into existing IT workflows — without requiring the buyer to become a power user of your entire Operate surface on week one.**

---

## 8. Deferred Scope Uncertainty

**None.** All intentional V1.1/V2 deferrals reviewed in [`V1_DEFERRED.md`](V1_DEFERRED.md) with explicit exclusions from V1 scoring. No orphan “deferred” references found without a documented home.

---

## 9. Top Improvement Opportunities (8 actionable; full Cursor prompts)

Below: ranked by **leverage**. All eight are **fully actionable now** (no `DEFERRED` items). **Impact estimates** are directional.

---

### 9.1 Persistence + Api test coverage uplift (highest engineering leverage)

**Why it matters:** Raises confidence in **relational reconstruction** and **HTTP edge behavior** where defects become **wrong architecture conclusions**.

**Expected impact:** Correctness **+8–12**, Testability **+5–8**, Reliability **+2–4**. **Weighted readiness impact:** **+0.5–0.7%**.

**Affected qualities:** Correctness, Testability, Reliability (secondary: Maintainability).

**Fully actionable now:** Yes.

**Cursor prompt:**

```text
You are working in the ArchLucid repo at the workspace root.

Goal: Raise merged test coverage by adding targeted tests for ArchLucid.Persistence relational read paths and ArchLucid.Api validation/error branches, prioritizing the highest-risk uncovered lines.

Constraints:
- Follow docs/CSharp house style (guard clauses, is null patterns, primary constructors where appropriate).
- Prefer LINQ where it does not harm readability in tests.
- Do not weaken CI coverage gates in .github/workflows/ci.yml or scripts/ci/assert_merged_line_coverage_min.py.
- Do not modify historical SQL migration files (001–028) or edit ArchLucid.Persistence/Scripts for schema — tests only unless a bug is proven.
- Do not add ConfigureAwait(false) in tests (user rule).

Steps:
1. Read docs/library/CODE_COVERAGE.md and docs/coverage-exclusions.md for the authoritative gate expectations.
2. Use coverage artifacts or dotnet test with coverage locally to identify the lowest-covered areas in:
   - ArchLucid.Persistence (focus relational reads / mappers called on hot paths)
   - ArchLucid.Api (focus controller branches: 400/404/409 paths, paging clamps, auth failures)
3. Add minimal tests that assert observable behavior (outputs/status codes), not implementation details.
4. If you find a real defect, fix minimally and add regression test; otherwise tests only.

Acceptance criteria:
- dotnet test for affected test projects passes.
- Per-package line coverage for ArchLucid.Persistence and ArchLucid.Api trends upward; aim to eliminate the worst gaps called out in recent CODE_COVERAGE.md notes without skipping gates.
- No new compiler or analyzer warnings in edited files.

Out of scope:
- UI/e2e refactors, Terraform, marketing docs.
```

**Impact of running the prompt:** Stronger regression safety on **manifest/findings/read APIs**; fewer production surprises during pilot scale-up.

---

### 9.2 Guided Core Pilot checklist + signup baseline UX

**Why it matters:** Directly attacks **weighted adoption friction** and **time-to-value**.

**Expected impact:** Adoption Friction **+8–12**, Time-to-Value **+5–8**, Usability **+4–6**. **Weighted readiness impact:** **+1.0–1.5%**.

**Affected qualities:** Adoption Friction, Time-to-Value, Usability, Cognitive Load.

**Fully actionable now:** Yes.

**Cursor prompt:**

```text
Workspace: ArchLucid monorepo.

Goal: Add an operator-shell “Core Pilot checklist” experience and wire optional signup baseline hours UX consistent with docs/library/PILOT_ROI_MODEL.md §3.1 and PILOT_GUIDE.md.

Constraints:
- archlucid-ui: follow existing patterns (App Router, server components where used today, Vitest for units).
- API remains authoritative; UI must handle 401/403 gracefully.
- Accessibility: do not introduce new axe critical/serious violations; run archlucid-ui npm run test:axe-components where applicable.
- Do not change pricing numbers — link to docs only.

Implementation sketch (adjust after reading code):
1. Add a ChecklistCard component on the operator home (or getting-started) that tracks: storage configured → API healthy → first run created → executed → committed → artifact viewed.
2. Use GET /v1/diagnostics/operator-task-success-rates or existing trial/status endpoints if already present; if gaps exist, add a small read-only diagnostics endpoint in ArchLucid.Api *only if necessary* with tests.
3. If POST /v1/register already accepts baselineReviewCycleHours, add optional fields to the signup UI with validation mirroring API rules from PILOT_GUIDE.md.

Acceptance criteria:
- New unit tests for any new TS helpers and API tests for any new endpoint.
- npm test (or scoped package scripts used in CI) passes for archlucid-ui unit suite you touch.
- docs: add a short “how to use checklist” section under docs/library/PILOT_GUIDE.md or OPERATOR_QUICKSTART.md with > **Scope:** header compliance.

Out of scope:
- Stripe live keys, marketplace publish, Terraform.
```

**Impact of running the prompt:** Faster **first successful manifest** for real operators; better **ROI baseline capture** without waiting for API-only pilots.

---

### 9.3 Real-mode benchmark hardening + sponsor proof snapshot doc

**Why it matters:** Gives sales/pilot leads a **repeatable latency story** beyond simulator.

**Expected impact:** Proof-of-ROI Readiness **+5–8**, Marketability **+3–5**, Time-to-Value **+2–4**. **Weighted readiness impact:** **+0.5–0.7%**.

**Affected qualities:** Proof-of-ROI Readiness, Marketability, Performance (light).

**Fully actionable now:** Yes (script/docs — running k6 against staging optional).

**Cursor prompt:**

```text
Goal: Make real-mode time-to-value evidence reproducible and publishable from repo artifacts.

Files:
- scripts/benchmark-real-mode-e2e.ps1
- docs/library/REAL_MODE_BENCHMARK.md
- tests/load/README.md (if present)

Tasks:
1. Enhance benchmark-real-mode-e2e.ps1 to write a deterministic JSON artifact to ./artifacts/benchmark-real-mode-latest.json (create artifacts/ if needed, gitignored appropriately) including timestamps, mode, runId, phase timings, environment metadata flags (no secrets).
2. Update REAL_MODE_BENCHMARK.md with: how to run, how to interpret artifact, and explicit “lab vs staging vs prod-like” disclaimers.
3. Add docs/library/PROOF_OF_VALUE_SNAPSHOT.md (> **Scope:** one line) that explains how to assemble: benchmark artifact + PILOT_ROI_MODEL measurement table + link to OBSERVABILITY explainability completeness metric — for sponsor packs.

Constraints:
- Never print secrets; redact connection info.
- Keep docs root budget: new doc belongs in docs/library/.

Acceptance criteria:
- pwsh script runs in simulator mode in CI-friendly way (no AOAI required) and produces JSON.
- Markdown passes doc scope header check pattern used in repo.

Out of scope:
- Changing LLM orchestration logic without failing tests.
```

**Impact of running the prompt:** **Defensible proof workflow** for champions building internal slides.

---

### 9.4 Jira webhook bridge recipe (V1-allowed pattern)

**Why it matters:** **Workflow embeddedness** without waiting for V1.1 first-party connector.

**Expected impact:** Workflow Embeddedness **+5–8**, Interoperability **+3–5**, Adoption Friction **+2–3**. **Weighted readiness impact:** **+0.3–0.5%**.

**Affected qualities:** Workflow Embeddedness, Interoperability.

**Fully actionable now:** Yes.

**Cursor prompt:**

```text
Goal: Document and template a customer-owned Jira integration that consumes ArchLucid outbound webhooks (CloudEvents) and creates issues via Jira REST API — without adding a first-party Jira connector to product code.

Deliverables:
1. New doc: docs/integrations/JIRA_WEBHOOK_BRIDGE.md with > **Scope:** line per CI doc rules.
2. New example script: tools/integrations/jira-webhook-bridge-example.ps1 (or .js if repo convention prefers) — clearly marked non-production sample, with TODOs for auth.
3. Update docs/go-to-market/INTEGRATION_CATALOG.md to point to the recipe for V1 bridging.

Content requirements:
- Show example payload mapping: finding id, severity, title, deep link back to ArchLucid run/finding UI pattern.
- Security: use secrets from environment, recommend IP allow lists / mTLS posture at high level (no inventing unreal Azure SKUs).
- Note V1.1 first-party connector commitment without promising dates.

Acceptance criteria:
- docs/ci doc scope script passes for new/edited docs.
- Example script is safe-by-default (no hard-coded secrets).

Out of scope:
- Shipping Jira OAuth in ArchLucid.Api.
```

**Impact of running the prompt:** **Shortens** “workflow fit” objections in **Jira-centric** shops.

---

### 9.5 Durable audit closure for selected baseline-only paths

**Why it matters:** Procurement asks **“what tampering story do I have?”**

**Expected impact:** Auditability **+6–8**, Traceability **+3–5**, Compliance Readiness **+2–3**. **Weighted readiness impact:** **+0.3–0.5%**.

**Affected qualities:** Auditability, Traceability, Compliance Readiness.

**Fully actionable now:** Yes.

**Cursor prompt:**

```text
Goal: Reduce durable-audit gaps for high-risk mutation paths identified in docs/library/AUDIT_COVERAGE_MATRIX.md (read Known gaps / dual-channel notes carefully).

Constraints:
- Do not break hot-path latency budgets; prefer async durable audit patterns already used elsewhere.
- Must update AUDIT_COVERAGE_MATRIX.md and AuditEventTypes.cs count anchor if new constants — follow scripts/ci/assert_audit_const_count.py rules.
- Each new IAuditService call site needs tests proving an AuditEvents row is appended (integration or unit with test doubles per existing patterns).

Steps:
1. Pick up to 3 mutating flows that are compliance-critical and currently log-only on baseline channel.
2. Add IAuditService.LogAsync with stable event types and minimal redacted DataJson.
3. Update matrix + any operator-facing audit docs.

Acceptance criteria:
- Full .NET test suites you touch pass.
- CI audit const guard remains green.
- No PII expansion in DataJson; follow redaction norms in docs/runbooks/LLM_PROMPT_REDACTION.md where applicable.

Out of scope:
- Rewriting all baseline mutation logs.
```

**Impact of running the prompt:** **Cleaner SOC2 narrative** for “tamper-evident operations” claims.

---

### 9.6 First-run wizard state machine (resume via localStorage)

**Why it matters:** **Usability** for first week is the adoption bottleneck.

**Expected impact:** Usability **+6–8**, Cognitive Load **+5–7**, Adoption Friction **+3–5**. **Weighted readiness impact:** **+0.4–0.6%**.

**Affected qualities:** Usability, Cognitive Load, Adoption Friction.

**Fully actionable now:** Yes.

**Cursor prompt:**

```text
Implement a guided first-run wizard in archlucid-ui for the Core Pilot path described in docs/library/V1_SCOPE.md §4.1.

Requirements:
- Steps: configure → health/version check → create run → execute → commit → review artifacts (wording can match CORE_PILOT.md).
- Persist wizard progress in localStorage with schema version key namespaced (e.g. archlucid.corePilotWizard.v1).
- Must not block expert users — easy “dismiss / don’t show again” with accessibility (aria-modal, focus trap if modal).
- Use existing API client hooks; do not bypass auth.

Tests:
- Add Vitest tests for reducer/state machine and localStorage edge cases.

Docs:
- Update archlucid-ui/README.md “In-product guidance” section briefly.

Acceptance criteria:
- npm test passes for targeted tests.
- No new critical/serious axe issues on affected pages (use existing patterns).

Out of scope:
- Rewriting nav-config authority model.
```

**Impact of running the prompt:** Fewer **abandoned pilots** halfway through execution.

---

### 9.7 Commercial tier enforcement hardening

**Why it matters:** Prevents **entitlement drift** between **docs and code**.

**Expected impact:** Commercial Packaging Readiness **+6–8**, Correctness (commercial) **+3–5**. **Weighted readiness impact:** **+0.1–0.2%**.

**Affected qualities:** Commercial Packaging Readiness, Correctness.

**Fully actionable now:** Yes.

**Cursor prompt:**

```text
Read docs/library/COMMERCIAL_ENFORCEMENT_DEBT.md and docs/library/PRODUCT_PACKAGING.md tier gates.

Goal: Align code enforcement with documented Professional/Enterprise boundaries.

Tasks:
1. Inventory controllers/actions that should require RequiresCommercialTenantTier per PRODUCT_PACKAGING feature matrix but do not.
2. Add attributes + regression tests expecting 404 packaging responses for under-tier tenants (per PackagerProblemDetails semantics — do not change 404 vs 402 story without BREAKING_CHANGES.md).
3. Update COMMERCIAL_ENFORCEMENT_DEBT.md table to reflect new inventory.

Constraints:
- Follow ArchLucid.Api existing filter registration patterns.
- Tests must be deterministic; use existing auth test fixtures.

Acceptance criteria:
- ArchLucid.Api.Tests passes for new tests.
- No unintended changes to public OpenAPI without updating contracts per repo rules.

Out of scope:
- Entitlement billing provider changes.
```

**Impact of running the prompt:** **Cleaner enterprise deals** — fewer **“we saw it in UI but API disagrees”** incidents.

---

### 9.8 Sponsor evidence pack API + /why-archlucid hardening

**Why it matters:** Execs **don’t read ADRs** — they read **one chart**.

**Expected impact:** Executive Value Visibility **+6–8**, Marketability **+2–3**, Proof-of-ROI **+2–4**. **Weighted readiness impact:** **+0.3–0.5%**.

**Affected qualities:** Executive Value Visibility, Marketability, Proof-of-ROI Readiness.

**Fully actionable now:** Yes.

**Cursor prompt:**

```text
Add GET /v1/pilots/sponsor-evidence-pack (name bikeshed ok) returning a JSON DTO with:
- tenant-safe aggregates: runs count, committed manifests count, findings severity histogram, governance approvals counts if readable with current auth
- explainability trace completeness summary if available from existing metrics endpoints/services
- optional delta fields if tenant baseline review cycle exists

Constraints:
- Must respect tenant scoping and existing policies; default deny.
- Add ArchLucid.Api.Tests coverage for authz + happy path.
- Wire archlucid-ui /why-archlucid page to consume this endpoint where it currently relies only on snapshot endpoints — keep DemoEnabled behavior unchanged for marketing-only hosts.

Docs:
- Update docs/go-to-market/POSITIONING.md §4 only if needed to reference new endpoint (no marketing superlatives).

Acceptance criteria:
- CI green for API tests.
- No secrets in JSON.

Out of scope:
- Building PDF server-side beyond existing endpoints.
```

**Impact of running the prompt:** **Faster internal approvals** to expand pilot → paid.

---

## 10. Pending Questions for Later

Organized by improvement title — **blocking or decision-shaping only**.

| Improvement | Questions (answer when you have time) |
|-------------|---------------------------------------|
| Persistence + Api test coverage uplift | Which **3 customer defect classes** matter most for pilot SLA: findings, governance merges, or export/compare? (Prioritizes test focus.) |
| Guided checklist + signup baseline | Should baseline hours be **required** for trial, or remain optional forever? (Privacy vs ROI sharpness — see `TRIAL_BASELINE_PRIVACY_NOTE.md`.) |
| Real-mode benchmark + proof snapshot | What is the **approved public staging hostname** and **data handling policy** for publishing benchmark artifacts externally? |
| Jira webhook bridge | Default auth: **API token** vs **OAuth** for the recipe — which does the first 3 customers use? |
| Durable audit closure | Which **3 workflows** are contractually required to be durable-audited in your first paid deal? |
| First-run wizard | Should wizard be **on by default** for all new tenants or only **Trial** tier? |
| Commercial tier enforcement | Is **404 obfuscation** still the desired enterprise behavior when tier is insufficient — any customer demands **402** transparency? |
| Sponsor evidence pack | Which **KPI set** does your exec sponsor actually want in week-1 — **time saved** vs **defects prevented** vs **governance SLAs**? |

---

## 11. Related

- [`V1_SCOPE.md`](V1_SCOPE.md) — V1 contract  
- [`V1_DEFERRED.md`](V1_DEFERRED.md) — intentional exclusions from V1 scoring  
- [`V1_READINESS_SUMMARY.md`](V1_READINESS_SUMMARY.md) — repo’s own readiness framing  

**Change control:** Update scores only with a new dated assessment file; keep deferral exclusions synchronized with `V1_DEFERRED.md`.
