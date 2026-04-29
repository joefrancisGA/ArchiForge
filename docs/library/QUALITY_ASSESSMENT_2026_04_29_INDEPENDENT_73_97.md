> **Scope:** Independent weighted product/engineering readiness assessment (2026-04-29) — scores, rationale, and improvement prompts; not an implementation contract.

# ArchLucid Assessment – Weighted Readiness 73.97%

## Method

- **Scores:** Each quality scored **1–100** (higher is better).
- **Weights:** Taken exactly from the assessment brief (Commercial + Enterprise + Engineering).
- **Weight sum:** **102** (Commercial **40**, Enterprise **25**, Engineering **37** — verifying terms listed in the assessment brief sum to this denominator).
- **Weighted overall readiness:** \(\sum_i (\text{score}_i \times \text{weight}_i) / 102\) → **73.97%** (two decimal places).
- **Deferred scope:** Items explicitly deferred to **V1.1** or **V2** in [V1_DEFERRED.md](V1_DEFERRED.md) / [V1_SCOPE.md](V1_SCOPE.md) **do not reduce** the scores below (reference-customer publication, commerce “un-hold,” executed pen-test publication, PGP key drop, MCP membrane, first-party Jira/ServiceNow/Confluence, Slack connector).
- **Independence:** No prior assessment scores or conclusions were consulted.

### Deferred Scope Uncertainty

- **None.** Deferred boundaries are documented in [V1_DEFERRED.md](V1_DEFERRED.md) with explicit tables (ITSM V1.1, Slack V2, commercial milestones §6b, assurance §6c, MCP §6d, etc.).

---

## Executive Summary

### Overall readiness

ArchLucid presents as a **seriously engineered** Azure-first SaaS platform: bounded modules, Dapper/SQL authority plane, rich observability, durable audit, progressive UI disclosure, and **defense-in-depth** CI (secret scan, OpenAPI snapshot, ZAP, Schemathesis, mutation testing, Playwright live API + axe). Weighted readiness **73.97%** reflects **strong engineering execution** with **commercial proof still thin** (no published reference customers; PMF tracker placeholders; buyer motion still partly sales-led until live commerce milestones land on their stated release window).

### Commercial picture

**Marketability and adoption friction** dominate weighted deficiency: the story is credible on paper ([POSITIONING.md](../go-to-market/POSITIONING.md), [EXECUTIVE_SPONSOR_BRIEF.md](../EXECUTIVE_SPONSOR_BRIEF.md)), but **third-party validation is still mostly prospective**—pricing and procurement packs exist, yet **evidence rows are empty** in [PMF_VALIDATION_TRACKER.md](../go-to-market/PMF_VALIDATION_TRACKER.md), and [reference-customers/README.md](../go-to-market/reference-customers/README.md) has **no `Published`** rows (explicitly **out of V1** per [V1_DEFERRED.md](V1_DEFERRED.md) §6b—does **not** reduce this score). **Time-to-value** is good for contributors (CLI `try`, Docker paths) and reasonable for hosted pilots, but **enterprise onboarding cognitive load** remains non-trivial.

### Enterprise picture

**Traceability, auditability, and governance mechanics** are materially ahead of typical early-stage AI tools: typed audit catalog with CI guards ([AUDIT_COVERAGE_MATRIX.md](AUDIT_COVERAGE_MATRIX.md)), segregation-of-duties paths ([GOVERNANCE.md](GOVERNANCE.md)), STRIDE summary ([SYSTEM_THREAT_MODEL.md](../security/SYSTEM_THREAT_MODEL.md)), RLS posture ([MULTI_TENANT_RLS.md](../security/MULTI_TENANT_RLS.md)), accessibility enforcement ([ACCESSIBILITY_AUDIT.md](ACCESSIBILITY_AUDIT.md)). **Compliance readiness** is constrained by **process attestation gaps** (SOC 2 roadmap vs opinion—see [SOC2_ROADMAP.md](../go-to-market/SOC2_ROADMAP.md)); **procurement readiness** is strong on artifacts but **buyer diligence still converges on “show me independent assurance.”**

### Engineering picture

**Correctness guardrails** are unusually strong for this category (golden corpus under `tests/golden-corpus/`, broad API integration tests, chaos/load probes). **Operational maturity** is credible (SLO docs [API_SLOS.md](API_SLOS.md), Prometheus rules under `infra/prometheus/`). Residual risks concentrate in **LLM-dependent behaviors**, **scaling proof at tenant extremes**, and **keeping composite CI honest** (JWT live lane currently non-blocking per [LIVE_E2E_HAPPY_PATH.md](LIVE_E2E_HAPPY_PATH.md)).

---

## Weighted Quality Assessment

**Ordering:** Qualities listed **most urgent → least urgent**, where urgency \(\approx \textbf{weight} \times (100 - \textbf{score})\) (**weighted deficiency signal**). Ties broken by higher nominal weight, then lower score.

| Rank | Quality | Score | Weight | Weighted deficiency signal |
|-----:|---------|------:|-------:|---------------------------:|
| 1 | Marketability | 70 | 8 | 240 |
| 2 | Adoption Friction | 65 | 6 | 210 |
| 3 | Time-to-Value | 75 | 7 | 175 |
| 4 | Proof-of-ROI Readiness | 68 | 5 | 160 |
| 5 | Executive Value Visibility | 72 | 4 | 112 |
| 6 | Differentiability | 76 | 4 | 96 |
| 7 | Workflow Embeddedness | 70 | 3 | 90 |
| 8 | Correctness | 79 | 4 | 84 |
| 9 | Usability | 73 | 3 | 81 |
| 10 | Compliance Readiness | 62 | 2 | 76 |
| 11 | Trustworthiness | 76 | 3 | 72 |
| 12 | Procurement Readiness | 65 | 2 | 70 |
| 13 | Security | 80 | 3 | 60 |
| 14 | Decision Velocity | 70 | 2 | 60 |
| 15 | Architectural Integrity | 81 | 3 | 57 |
| 16 | Commercial Packaging Readiness | 72 | 2 | 56 |
| 17 | Traceability | 82 | 3 | 54 |
| 18 | Interoperability | 75 | 2 | 50 |
| 19 | Data Consistency | 75 | 2 | 50 |
| 20 | Reliability | 76 | 2 | 48 |
| 21 | Maintainability | 77 | 2 | 46 |
| 22 | AI/Agent Readiness | 77 | 2 | 46 |
| 23 | Azure Compatibility and SaaS Deployment Readiness | 79 | 2 | 42 |
| 24 | Explainability | 80 | 2 | 40 |
| 25 | Cognitive Load | 60 | 1 | 40 |
| 26 | Policy and Governance Alignment | 82 | 2 | 36 |
| 27 | Template and Accelerator Richness | 66 | 1 | 34 |
| 28 | Auditability | 84 | 2 | 32 |
| 29 | Stickiness | 68 | 1 | 32 |
| 30 | Performance | 69 | 1 | 31 |
| 31 | Scalability | 70 | 1 | 30 |
| 32 | Customer Self-Sufficiency | 71 | 1 | 29 |
| 33 | Availability | 73 | 1 | 27 |
| 34 | Extensibility | 73 | 1 | 27 |
| 35 | Cost-Effectiveness | 73 | 1 | 27 |
| 36 | Manageability | 74 | 1 | 26 |
| 37 | Change Impact Clarity | 75 | 1 | 25 |
| 38 | Evolvability | 75 | 1 | 25 |
| 39 | Supportability | 76 | 1 | 24 |
| 40 | Accessibility | 79 | 1 | 21 |
| 41 | Deployability | 79 | 1 | 21 |
| 42 | Documentation | 79 | 1 | 21 |
| 43 | Azure Ecosystem Fit | 80 | 1 | 20 |
| 44 | Modularity | 81 | 1 | 19 |
| 45 | Observability | 82 | 1 | 18 |
| 46 | Testability | 85 | 1 | 15 |

**Tie-breaks (same deficiency):** higher **weight** first; if still tied, lower **score** first; if still tied, **alphabetical** by quality name.

Below, each quality follows the requested structure (**score / weight / weighted impact / justification / tradeoffs / improvements / fix horizon**).

---

### Commercial

#### Marketability — Score **70**, Weight **8**

- **Weighted impact on readiness:** +5.49 pts of max (+70×8/102).
- **Why:** Narrative and packaging are unusually complete for a repo-native product; however **external validation artifacts remain thin** (PMF table placeholders; reference table without published rows—**not scored as a V1 deficit** per explicit deferral, but **still hurts pure marketability** versus competitors with logos).
- **Tradeoffs:** Investing in polished marketing before hardened buyer proof risks **premature positioning**; the repo correctly favors **pilot-first** mechanics.
- **Improvements:** Publish **one** non-sensitive “baseline vs outcome” anonymized pilot appendix (even without customer logo); tighten homepage proof paths to **staging demo** routes documented in [POSITIONING.md](../go-to-market/POSITIONING.md).
- **Fix horizon:** **v1** for anonymized metrics; **logo-grade references → V1.1** per deferral.

#### Time-to-Value — Score **75**, Weight **7**

- **Weighted impact:** +5.15 pts.
- **Why:** `dotnet run --project ArchLucid.Cli -- try`, compose paths, and `/getting-started` reduce cold-start friction; real enterprises still pay integration tax (Entra, networking).
- **Tradeoffs:** Faster hosted onboarding can conflict with **security reviews** and **tenant isolation** rigor.
- **Improvements:** Time-boxed **first-session checklist** surfaced in-product ([ONBOARDING_WIZARD.md](ONBOARDING_WIZARD.md)) linked from operator home.
- **Fix horizon:** **v1**.

#### Adoption Friction — Score **65**, Weight **6**

- **Weighted impact:** +3.82 pts.
- **Why:** Operator shell breadth + progressive disclosure helps, but **surface area remains large**; self-hosted path expects mature Azure/SQL literacy.
- **Tradeoffs:** Removing advanced surfaces reduces **discoverability** for sophisticated buyers.
- **Improvements:** Role-based **default dashboards** (architecture reviewer vs operator) using existing `/me` shaping ([PRODUCT_PACKAGING.md](PRODUCT_PACKAGING.md)).
- **Fix horizon:** **v1** UI iteration.

#### Proof-of-ROI Readiness — Score **68**, Weight **5**

- **Weighted impact:** +3.33 pts.
- **Why:** Strong measurement scaffolding ([PILOT_ROI_MODEL.md](PILOT_ROI_MODEL.md)); PMF tracker rows still **Pending**—execution gap, not tooling gap.
- **Tradeoffs:** Over-automation of ROI claims creates **credibility risk** if baselines are sloppy.
- **Improvements:** Pilot scorecard templates wired to **SQL-backed metrics** already described in docs—populate tracker after each pilot exit interview.
- **Fix horizon:** **v1** process; **repeatable ROI proof library → grows post-pilot**.

#### Executive Value Visibility — Score **72**, Weight **4**

- **Weighted impact:** +2.82 pts.
- **Why:** Sponsor brief + ROI companion exist; executives still must trust **aggregates** built from pilot discipline.
- **Tradeoffs:** Executive dashboards can disclose **sensitive throughput** if mis-scoped.
- **Improvements:** One-page **board pack** PDF narrative aligned with shipped endpoints (see existing API tests referencing board pack).
- **Fix horizon:** **v1** polish.

#### Differentiability — Score **76**, Weight **4**

- **Weighted impact:** +2.98 pts.
- **Why:** Genuine differentiation: authority pipeline + durable audit + governance workflow vs chat tools ([COMPETITIVE_LANDSCAPE.md](../go-to-market/COMPETITIVE_LANDSCAPE.md)).
- **Tradeoffs:** Differentiators require **buyer education** (longer sales cycle).
- **Improvements:** Competitor matrix kept synchronized with **OpenAPI** and **V1_SCOPE** on each release (CI doc guard already partially enforced).
- **Fix horizon:** **v1**.

#### Decision Velocity — Score **70**, Weight **2**

- **Weighted impact:** +1.37 pts.
- **Why:** Pricing quote endpoint + single-source pricing CI reduce internal thrash; enterprise procurement still human-heavy.
- **Tradeoffs:** Faster quoting without CRM hygiene increases **order errors**.
- **Improvements:** Automated routing of quote requests (currently partially dependent on mail config—see [PRICING_PHILOSOPHY.md](../go-to-market/PRICING_PHILOSOPHY.md)).
- **Fix horizon:** **v1** engineering where CRM destination known; **FULL CRM ownership DEFERRED** if sales tooling choice unset.

#### Commercial Packaging Readiness — Score **72**, Weight **2**

- **Weighted impact:** +1.41 pts.
- **Why:** Tier table + packaging reference solid; UI shaping ≠ entitlement hardening by design ([COMMERCIAL_BOUNDARY_HARDENING_SEQUENCE.md](COMMERCIAL_BOUNDARY_HARDENING_SEQUENCE.md)).
- **Tradeoffs:** Hard enforcement too early risks **pilot friction**.
- **Improvements:** Packaging lint against `/me` read-model drift ([archlucid-ui/README.md](../../archlucid-ui/README.md)).
- **Fix horizon:** **v1** continuous.

#### Stickiness — Score **68**, Weight **1**

- **Weighted impact:** +0.67 pts.
- **Why:** Manifest versioning + governance hooks encourage ongoing use; limited **workflow lock-in** without ITSM connectors (**V1.1** scope).
- **Tradeoffs:** Stickiness via integrations increases **support burden**.
- **Improvements:** Expand webhook recipes under `schemas/integration-events/` with copy-paste Logic Apps samples ([INTEGRATION_EVENTS_AND_WEBHOOKS.md](INTEGRATION_EVENTS_AND_WEBHOOKS.md)).
- **Fix horizon:** **v1**.

#### Template and Accelerator Richness — Score **66**, Weight **1**

- **Weighted impact:** +0.65 pts.
- **Why:** Templates exist (`templates/` tree); richness still behind mature EA tooling marketplaces.
- **Tradeoffs:** More templates require **ongoing QA** against pipeline drift.
- **Improvements:** Publish **minimum three** golden-path templates with CI validation snapshots.
- **Fix horizon:** **v1.1** for marketplace-grade breadth.

---

### Enterprise

#### Traceability — Score **82**, Weight **3**

- **Weighted impact:** +2.41 pts.
- **Why:** Finding inspector + provenance graph + audit correlation IDs ([EXPLAINABILITY.md](EXPLAINABILITY.md)).
- **Tradeoffs:** Full traces increase **storage + privacy** obligations.
- **Improvements:** Expand traceability bundle ZIP regression coverage ([TraceabilityBundleZipEndpointTests](../../ArchLucid.Api.Tests/TraceabilityBundleZipEndpointTests.cs) patterns).
- **Fix horizon:** **v1**.

#### Usability — Score **73**, Weight **3**

- **Weighted impact:** +2.15 pts.
- **Why:** Progressive disclosure helps; dense governance surfaces still challenge new operators.
- **Tradeoffs:** Simplification risks hiding **controls auditors expect**.
- **Improvements:** Scenario-based **guided tours** stored as MD + lightweight UI anchors (no new framework).
- **Fix horizon:** **v1**.

#### Workflow Embeddedness — Score **70**, Weight **3**

- **Weighted impact:** +2.06 pts.
- **Why:** Service Bus + webhook contracts exist; **first-party ITSM** deferred **V1.1** (**not** penalized here beyond honest interoperability expectations).
- **Tradeoffs:** Deep integrations multiply **secrets handling** and connector SLAs.
- **Improvements:** Customer-authored connector cookbook using existing Authority-shaped payloads ([V1_DEFERRED.md](V1_DEFERRED.md) §6 hard rule).
- **Fix horizon:** **v1** docs + samples; **first-party connectors V1.1**.

#### Trustworthiness — Score **76**, Weight **3**

- **Weighted impact:** +2.24 pts.
- **Why:** Strong technical trust mechanics; **third-party pen test publication deferred V1.1** per [V1_DEFERRED.md](V1_DEFERRED.md) §6c (**not** counted as V1 gap).
- **Tradeoffs:** Higher assurance artifacts slow **release cadence**.
- **Improvements:** Keep Trust Center rows aligned with **live** CI badges and probe workflows ([TRUST_CENTER.md](../go-to-market/TRUST_CENTER.md)).
- **Fix horizon:** **v1** continuity; **external pen test → V1.1**.

#### Auditability — Score **84**, Weight **2**

- **Weighted impact:** +1.65 pts.
- **Why:** Dual-channel clarity + append-only posture documented ([AUDIT_COVERAGE_MATRIX.md](AUDIT_COVERAGE_MATRIX.md)).
- **Tradeoffs:** Baseline vs durable channels confuse **SOC analysts** without training.
- **Improvements:** Export **training appendix** for auditors (what lands in `dbo.AuditEvents` vs logs).
- **Fix horizon:** **v1**.

#### Policy and Governance Alignment — Score **82**, Weight **2**

- **Weighted impact:** +1.61 pts.
- **Why:** Segregation-of-duties + dry-run paths ([GOVERNANCE.md](GOVERNANCE.md)).
- **Tradeoffs:** Stricter policies reduce **developer autonomy**.
- **Improvements:** Simulation fixtures for policy packs in CI ([policy pack lifecycle E2E](../../archlucid-ui/e2e/live-api-policy-pack-lifecycle.spec.ts) patterns).
- **Fix horizon:** **v1**.

#### Compliance Readiness — Score **62**, Weight **2**

- **Weighted impact:** +1.22 pts.
- **Why:** CAIQ Lite pre-fill exists ([CAIQ_LITE_2026.md](../security/CAIQ_LITE_2026.md)); **SOC 2 opinion** remains future milestone ([SOC2_ROADMAP.md](../go-to-market/SOC2_ROADMAP.md)).
- **Tradeoffs:** Policy paperwork without code reduces credibility; code without paperwork fails **enterprise procurement**.
- **Improvements:** Evidence pointers per CAIQ row linking Terraform + CI jobs.
- **Fix horizon:** **v1** docs; **SOC 2 Type II → program timeline**.

#### Procurement Readiness — Score **65**, Weight **2**

- **Weighted impact:** +1.27 pts.
- **Why:** Procurement pack folder + trust center structure; still founder-led diligence bandwidth.
- **Tradeoffs:** Over-producing paperwork misallocates **engineering hours**.
- **Improvements:** Automate **SIG / CAIQ** diff checks on each release tag.
- **Fix horizon:** **v1** lightweight automation.

#### Interoperability — Score **75**, Weight **2**

- **Weighted impact:** +1.47 pts.
- **Why:** REST `/v1`, AsyncAPI catalog, CLI—solid; niche integrations rely on **customer effort**.
- **Tradeoffs:** Every new integration surface expands **attack surface**.
- **Improvements:** Contract tests against **AsyncAPI** samples in CI ([schemas/integration-events/catalog.json](../../schemas/integration-events/catalog.json)).
- **Fix horizon:** **v1**.

#### Accessibility — Score **79**, Weight **1**

- **Weighted impact:** +0.77 pts.
- **Why:** axe live E2E enforcement ([ACCESSIBILITY_AUDIT.md](ACCESSIBILITY_AUDIT.md)).
- **Tradeoffs:** Strict axe gates slow **UI iteration** unless budgeted.
- **Improvements:** Expand component-level axe coverage list when new routes ship.
- **Fix horizon:** **v1**.

#### Customer Self-Sufficiency — Score **71**, Weight **1**

- **Weighted impact:** +0.70 pts.
- **Why:** Docs are deep ([docs/library/](.)); volume hurts task completion for occasional operators.
- **Tradeoffs:** Cutting docs reduces **supportability** for advanced workflows.
- **Improvements:** Task-oriented “recipe cards” linking to one runbook each ([runbooks/](../runbooks/)).
- **Fix horizon:** **v1**.

#### Change Impact Clarity — Score **75**, Weight **1**

- **Weighted impact:** +0.74 pts.
- **Why:** CHANGELOG + breaking changes narrative exists.
- **Tradeoffs:** Too-frequent breaking changes erode **CLI/API consumer trust**.
- **Improvements:** Auto summarize OpenAPI diff alongside snapshot tests ([OPENAPI_CONTRACT_DRIFT.md](OPENAPI_CONTRACT_DRIFT.md) patterns).
- **Fix horizon:** **v1**.

---

### Engineering

#### Correctness — Score **79**, Weight **4**

- **Weighted impact:** +3.10 pts.
- **Why:** Golden corpus + broad integration tests + deterministic simulator paths reduce regressions; LLM real mode remains inherently variable.
- **Tradeoffs:** Higher assurance adds CI **minutes** and flake management costs.
- **Improvements:** Expand golden corpus edge cases for ambiguous governance outcomes (`tests/golden-corpus/decisioning/`).
- **Fix horizon:** **v1**.

#### Architectural Integrity — Score **81**, Weight **3**

- **Weighted impact:** +2.38 pts.
- **Why:** Clear seams (Api/Application/Persistence/Worker), ADRs present ([docs/adr/](../adr/)).
- **Tradeoffs:** Many assemblies increase **navigation overhead** for newcomers.
- **Improvements:** Maintain architecture poster automation checks vs code references ([ARCHITECTURE_ON_A_PAGE.md](ARCHITECTURE_ON_A_PAGE.md)).
- **Fix horizon:** **v1**.

#### Security — Score **80**, Weight **3**

- **Weighted impact:** +2.35 pts.
- **Why:** Fail-closed defaults, ZAP/Schemathesis/CodeQL, STRIDE, billing webhook verification patterns ([SECURITY.md](SECURITY.md)).
- **Tradeoffs:** Strict defaults slow **first-run** for sloppy dev environments (mitigated by DevelopmentBypass guardrails).
- **Improvements:** Ensure JWT Playwright lane reaches parity importance ([LIVE_E2E_HAPPY_PATH.md](LIVE_E2E_HAPPY_PATH.md)).
- **Fix horizon:** **v1** CI hardening.

#### Reliability — Score **76**, Weight **2**

- **Weighted impact:** +1.49 pts.
- **Why:** Transactional outbox patterns + chaos/simmy hooks + alerts ([OBSERVABILITY.md](OBSERVABILITY.md)).
- **Tradeoffs:** Higher reliability engineering lifts **cloud bill**.
- **Improvements:** Align burn-rate alerts with customer-visible SLO doc ([API_SLOS.md](API_SLOS.md)).
- **Fix horizon:** **v1**.

#### Data Consistency — Score **75**, Weight **2**

- **Weighted impact:** +1.47 pts.
- **Why:** Orphan probes + enforcement modes instrumented ([OBSERVABILITY.md](OBSERVABILITY.md)).
- **Tradeoffs:** Aggressive quarantine blocks **valid edge workflows** if mis-tuned.
- **Improvements:** Runbook linking Prometheus alerts to remediation ([DATA_CONSISTENCY_MATRIX.md](DATA_CONSISTENCY_MATRIX.md) if referenced—else add cross-links).
- **Fix horizon:** **v1**.

#### Maintainability — Score **77**, Weight **2**

- **Weighted impact:** +1.51 pts.
- **Why:** Consistent Dapper repositories; modular services.
- **Tradeoffs:** Strict modular rules slightly raise **PR coordination** overhead.
- **Improvements:** Continue mutation-testing ratchet ([MUTATION_TESTING_STRYKER.md](MUTATION_TESTING_STRYKER.md)).
- **Fix horizon:** **v1**.

#### Explainability — Score **80**, Weight **2**

- **Weighted impact:** +1.57 pts.
- **Why:** Inspector endpoints + citation-bound rendering ([EXPLAINABILITY.md](EXPLAINABILITY.md)).
- **Tradeoffs:** Faithfulness heuristics can confuse **statistical stakeholders** if oversold.
- **Improvements:** UI chip coverage metrics exported already—surface **threshold warnings** in operator shell when faithfulness low.
- **Fix horizon:** **v1**.

#### AI/Agent Readiness — Score **77**, Weight **2**

- **Weighted impact:** +1.51 pts.
- **Why:** Simulator + quality gates + prompt redaction counters ([OBSERVABILITY.md](OBSERVABILITY.md)).
- **Tradeoffs:** Real-mode costs + vendor drift remain existential product risks.
- **Improvements:** Expand offline prompt-injection regression coverage ([AI_AGENT_PROMPT_REGRESSION.md](AI_AGENT_PROMPT_REGRESSION.md)).
- **Fix horizon:** **v1**.

#### Azure Compatibility and SaaS Deployment Readiness — Score **79**, Weight **2**

- **Weighted impact:** +1.55 pts.
- **Why:** Large Terraform footprint (`infra/terraform*/`), Container Apps/SQL/FD patterns.
- **Tradeoffs:** Azure-first posture limits **non-Azure** buyers unless neutral deployment story expands.
- **Improvements:** Partner-certified reference deployment checklist ([REFERENCE_SAAS_STACK_ORDER.md](REFERENCE_SAAS_STACK_ORDER.md)).
- **Fix horizon:** **v1**.

#### Availability — Score **73**, Weight **1**

- **Weighted impact:** +0.72 pts.
- **Why:** Health endpoints + probes; multi-region story depends on operator investment.
- **Tradeoffs:** HA increases **cost**.
- **Improvements:** Synthetic probes documented vs Terraform outputs ([API_SLOS.md](API_SLOS.md)).
- **Fix horizon:** **v1** ops.

#### Performance — Score **69**, Weight **1**

- **Weighted impact:** +0.68 pts.
- **Why:** Benchmarks exist ([ArchLucid.Benchmarks](../../ArchLucid.Benchmarks)); production-wide latency SLO proof varies by tenant workload.
- **Tradeoffs:** Aggressive caching risks **staleness** on governance reads.
- **Improvements:** Add CI threshold on k6 smoke outputs ([tests/load/ci-smoke.js](../../tests/load/ci-smoke.js)).
- **Fix horizon:** **v1**.

#### Scalability — Score **70**, Weight **1**

- **Weighted impact:** +0.69 pts.
- **Why:** Tenant isolation patterns documented; extreme multi-team scale still theory-backed.
- **Tradeoffs:** Premature shard design adds **complexity** without revenue.
- **Improvements:** Document scaling breakpoints using queue depth metrics ([OBSERVABILITY.md](OBSERVABILITY.md)).
- **Fix horizon:** **v1.1** for hard sharding unless driven by paying tenants.

#### Supportability — Score **76**, Weight **1**

- **Weighted impact:** +0.75 pts.
- **Why:** CLI support bundle + correlation IDs ([README.md](../../README.md)).
- **Tradeoffs:** Rich diagnostics risk **PII leakage** if mishandled.
- **Improvements:** Sanitizer regression tests expansion ([SECURITY.md](SECURITY.md) pointers).
- **Fix horizon:** **v1**.

#### Manageability — Score **74**, Weight **1**

- **Weighted impact:** +0.73 pts.
- **Why:** Many configuration surfaces—powerful but complex ([CONFIGURATION_REFERENCE.md](CONFIGURATION_REFERENCE.md)).
- **Tradeoffs:** Safer defaults vs flexibility tension persists.
- **Improvements:** Configuration profiles (“pilot”, “regulated”) as documented presets only (no silent behavior change).
- **Fix horizon:** **v1** docs.

#### Deployability — Score **79**, Weight **1**

- **Weighted impact:** +0.78 pts.
- **Why:** Compose + Terraform + release scripts ([RELEASE_LOCAL.md](RELEASE_LOCAL.md)).
- **Tradeoffs:** Multiple paths increase **support matrix**.
- **Improvements:** Single-page decision tree: compose vs Azure pilot ([INSTALL_ORDER.md](../engineering/INSTALL_ORDER.md)).
- **Fix horizon:** **v1**.

#### Observability — Score **82**, Weight **1**

- **Weighted impact:** +0.80 pts.
- **Why:** Broad meters + business KPI hooks ([OBSERVABILITY.md](OBSERVABILITY.md)).
- **Tradeoffs:** Cardinality discipline requires ongoing FinOps attention ([CAPACITY_AND_COST_PLAYBOOK.md](CAPACITY_AND_COST_PLAYBOOK.md)).
- **Improvements:** Grafana dashboard parity checklist vs OTel names ([infra/grafana/](../../infra/grafana/)).
- **Fix horizon:** **v1**.

#### Testability — Score **85**, Weight **1**

- **Weighted impact:** +0.83 pts.
- **Why:** Tiered tests + golden corpus + mutation testing ([TEST_EXECUTION_MODEL.md](TEST_EXECUTION_MODEL.md)).
- **Tradeoffs:** Heavy CI increases **contributor laptop burden** without wrappers (`test.ps1`).
- **Improvements:** Keep `Suite=Core` discipline tight—avoid orphan flaky filters.
- **Fix horizon:** **v1**.

#### Modularity — Score **81**, Weight **1**

- **Weighted impact:** +0.79 pts.
- **Why:** Many focused assemblies + contracts separation.
- **Tradeoffs:** IDE/test discovery slower on huge solutions—acceptable tradeoff at current scale.
- **Improvements:** Architecture tests enforcing dependency direction ([ArchLucid.Architecture.Tests](../../ArchLucid.Architecture.Tests)).
- **Fix horizon:** **v1**.

#### Extensibility — Score **73**, Weight **1**

- **Weighted impact:** +0.72 pts.
- **Why:** Policy packs + templates; advanced extensibility remains engineer-facing.
- **Tradeoffs:** Plugin models invite **unsupported forks**.
- **Improvements:** Clarify supported extension surfaces in one doc ([templates/README.md](../../templates/README.md)).
- **Fix horizon:** **v1** docs.

#### Evolvability — Score **75**, Weight **1**

- **Weighted impact:** +0.74 pts.
- **Why:** Forward migrations via DbUp + master DDL discipline ([SQL_SCRIPTS.md](SQL_SCRIPTS.md)).
- **Tradeoffs:** Migration ordering mistakes are **high severity**—tests mitigate but don't eliminate.
- **Improvements:** Continue SQL integration tier patterns ([TEST_EXECUTION_MODEL.md](TEST_EXECUTION_MODEL.md)).
- **Fix horizon:** **v1**.

#### Documentation — Score **79**, Weight **1**

- **Weighted impact:** +0.78 pts.
- **Why:** Large curated library + CI guards on docs hygiene.
- **Tradeoffs:** Volume increases **time-to-find** for busy operators (see Cognitive Load).
- **Improvements:** NAVIGATOR link checks already run—add quarterly pruning of stale cross-links.
- **Fix horizon:** **v1**.

#### Azure Ecosystem Fit — Score **80**, Weight **1**

- **Weighted impact:** +0.78 pts.
- **Why:** Entra, MI, Key Vault patterns pervasive.
- **Tradeoffs:** Reduces appeal for teams demanding **non-Azure primary**.
- **Improvements:** Explicit “neutral portability” boundaries in ADRs ([ADR 0020](../adr/0020-azure-primary-platform-permanent.md)).
- **Fix horizon:** **v1** positioning clarity.

#### Cognitive Load — Score **60**, Weight **1**

- **Weighted impact:** +0.59 pts.
- **Why:** Powerful product + dense docs + layered packaging increases mental overhead for new operators ([PRODUCT_PACKAGING.md](PRODUCT_PACKAGING.md)).
- **Tradeoffs:** Oversimplification hides **governance truth**.
- **Improvements:** Progressive disclosure audits & fewer synonyms in UI strings ([layer-guidance.ts](../../archlucid-ui/src/lib/layer-guidance.ts) patterns).
- **Fix horizon:** **v1**.

#### Cost-Effectiveness — Score **73**, Weight **1**

- **Weighted impact:** +0.72 pts.
- **Why:** Token metering + playbook exists ([CAPACITY_AND_COST_PLAYBOOK.md](CAPACITY_AND_COST_PLAYBOOK.md)); LLM dominates unpredictable pilot bills.
- **Tradeoffs:** Hard budgets can block **high-value runs**.
- **Improvements:** Tenant-facing budget warnings using existing quota instrumentation ([PER_TENANT_COST_MODEL.md](../deployment/PER_TENANT_COST_MODEL.md)).
- **Fix horizon:** **v1**.

---

## Top 10 Most Important Weaknesses

1. **Commercial proof density is thin versus narrative strength** — PMF tracker unfilled; logos deferred by policy but market still demands proof ([PMF_VALIDATION_TRACKER.md](../go-to-market/PMF_VALIDATION_TRACKER.md), [reference-customers/README.md](../go-to-market/reference-customers/README.md)).
2. **Enterprise assurance posture is “engineering-strong / attestation-in-flight”** — SOC 2 opinion not yet in hand ([SOC2_ROADMAP.md](../go-to-market/SOC2_ROADMAP.md)).
3. **Operator cognitive load vs capability depth** — Progressive disclosure helps but cannot erase intrinsic workflow complexity ([PRODUCT_PACKAGING.md](PRODUCT_PACKAGING.md)).
4. **JWT authentication parity not merge-blocking in CI** — Signal-only lane risks auth regressions ([LIVE_E2E_HAPPY_PATH.md](LIVE_E2E_HAPPY_PATH.md)).
5. **Performance/scalability evidence is scenario-based, not universal** — Benchmarks + k6/soak exist but not a single guaranteed ceiling under all tenant mixes ([tests/load/](../../tests/load/)).
6. **LLM variability remains an inherent correctness risk** despite gates — Quality gates mitigate but cannot eliminate nondeterminism in real mode ([OBSERVABILITY.md](OBSERVABILITY.md)).
7. **Compliance questionnaires partial / manual** — CAIQ/SIG depend on human narrative maintenance ([CAIQ_LITE_2026.md](../security/CAIQ_LITE_2026.md)).
8. **Cost predictability for heavy pilots** — Illustrative pricing defaults flagged in cost preview docs ([PER_TENANT_COST_MODEL.md](../deployment/PER_TENANT_COST_MODEL.md)).
9. **Commercial boundary soft vs hard** — UI shaping without entitlement enforcement is intentional but requires careful messaging discipline ([COMMERCIAL_BOUNDARY_HARDENING_SEQUENCE.md](COMMERCIAL_BOUNDARY_HARDENING_SEQUENCE.md)).
10. **Documentation sprawl** — Excellent coverage increases discovery friction ([DOC_INVENTORY_2026_04_25.md](DOC_INVENTORY_2026_04_25.md), [NAVIGATOR.md](../NAVIGATOR.md)).

---

## Top 5 Monetization Blockers

1. **Absent published buyer proof at scale** — Deferred logo milestones acknowledged; still slows enterprise procurement velocity (**process + evidence**, not missing code).
2. **Sales-led commerce path until live un-hold window** — Stripe TEST + Marketplace publication deferred **V1.1** per [V1_DEFERRED.md](V1_DEFERRED.md) §6b (**owner-driven**, intentionally out of V1 scoring penalty).
3. **SOC 2 / assurance timeline uncertainty** — Buyers budget PO against vendor assurance posture ([SOC2_STATUS_PROCUREMENT.md](../go-to-market/SOC2_STATUS_PROCUREMENT.md)).
4. **Category education tax** — “AI Architecture Intelligence” requires sponsor alignment beyond engineering champions ([POSITIONING.md](../go-to-market/POSITIONING.md)).
5. **Land-and-expand friction without ITSM connectors** — Covered by webhook/API path for V1; enterprise buyers still compare to ServiceNow/Jira-native incumbents (**V1.1** scope).

---

## Top 5 Enterprise Adoption Blockers

1. **Independent assurance artifacts incomplete vs engineering maturity** — Roadmaps exist; CPA opinion pending ([SOC2_ROADMAP.md](../go-to-market/SOC2_ROADMAP.md)).
2. **Procurement evidence scatter** — Strong repo artifacts; buyers still must assemble diligence packages manually without a guided portal beyond Trust Center stubs ([TRUST_CENTER.md](../go-to-market/TRUST_CENTER.md)).
3. **Operational complexity of Azure footprint** — Terraform breadth is good; customer skill mismatch remains ([infra/README.md](../../infra/README.md)).
4. **Residual LLM data-handling scrutiny** — Prompt trace storage + redaction must be explained repeatedly ([AGENT_TRACE_FORENSICS.md](AGENT_TRACE_FORENSICS.md) if referenced).
5. **Baseline organizational friction** — EA processes vary; tool success depends on pilot charter discipline ([BUYER_JOURNEY.md](../go-to-market/BUYER_JOURNEY.md)).

---

## Top 5 Engineering Risks

1. **Auth regression risk** — JWT lane non-blocking in CI ([LIVE_E2E_HAPPY_PATH.md](LIVE_E2E_HAPPY_PATH.md)).
2. **LLM nondeterminism** — Mitigations exist; fundamental residual remains ([OBSERVABILITY.md](OBSERVABILITY.md)).
3. **Multi-tenant isolation operational misconfiguration** — Correct patterns documented; human error remains ([MULTI_TENANT_RLS.md](../security/MULTI_TENANT_RLS.md)).
4. **Async pipeline poison messages / outbox backlog** — Operational hazards if worker scaling lags ([OBSERVABILITY.md](OBSERVABILITY.md), worker hosted services).
5. **Cost surprises under real-mode load** — Token bursts + observability ingest ([CAPACITY_AND_COST_PLAYBOOK.md](CAPACITY_AND_COST_PLAYBOOK.md)).

---

## Most Important Truth

**ArchLucid’s readiness gap is not primarily “missing features”—it is missing externally-legible commercial and assurance momentum proportional to the sophistication already present in code, tests, and infrastructure.**

---

## Top Improvement Opportunities

Ranked by leverage (highest first). All eight below are **fully actionable without founder-only external gates**.

### 1) Make JWT Playwright parity merge-blocking

- **Why it matters:** Authentication regressions are existential; today JWT live E2E is explicitly non-blocking ([LIVE_E2E_HAPPY_PATH.md](LIVE_E2E_HAPPY_PATH.md)).
- **Expected impact:** Reduces enterprise adoption blocker risk; catches cross-scheme regressions early.
- **Affected qualities:** Security (+4–8), Correctness (+2–4), Reliability (+2–3). **Weighted readiness impact:** ~**+0.35–0.55%** (depends on starting scores).

**Cursor prompt:**

```text
Goal: Promote the JWT-based Playwright live E2E lane from signal-only to merge-blocking parity with ApiKey/DevelopmentBypass where feasible.

Scope:
- Read docs/library/LIVE_E2E_HAPPY_PATH.md and docs/LIVE_E2E_JWT_SETUP.md for prerequisites.
- Inspect .github/workflows/ci.yml jobs ui-e2e-live-jwt (or equivalent): remove continue-on-error: true unless a tracked flaky test remains; if flaky, stabilize tests first.
- Ensure secrets documented: LIVE_JWT_TOKEN, ARCHLUCID_PROXY_BEARER_TOKEN alignment with Next proxy.
- Update LIVE_E2E_HAPPY_PATH.md § CI jobs to reflect merge-blocking status.

Constraints:
- Do not weaken ApiKey or DevelopmentBypass gates.
- Do not print secrets in logs.

Acceptance criteria:
- JWT lane fails PR merge on deterministic failures same as other merge-blocking UI jobs.
- Docs updated; link from docs/library/TEST_EXECUTION_MODEL.md if needed.

What not to change:
- Core auth policy semantics in ArchLucid.Host.Core except test-discovered bugs.
```

---

### 2) Close the “live vs mocked Playwright” honesty gap in contributor onboarding

- **Why it matters:** Release smoke optional Playwright uses mocks; live SQL-backed truth remains separate ([RELEASE_SMOKE.md](RELEASE_SMOKE.md))—contributors can misunderstand coverage.
- **Expected impact:** Fewer false confidence incidents; better Adoption Friction / Cognitive Load.
- **Affected qualities:** Adoption Friction (+3–5), Cognitive Load (+3–5), Testability (+2–3). **Weighted readiness impact:** ~**+0.25–0.40%**.

**Cursor prompt:**

```text
Goal: Make the distinction between mock-backed Playwright vs live-api Playwright impossible to miss for contributors.

Scope:
- Edit docs/library/RELEASE_SMOKE.md and archlucid-ui/docs/TESTING_AND_TROUBLESHOOTING.md §8 to cross-link LIVE_E2E_HAPPY_PATH.md with explicit “does / does not prove SQL-backed parity” table.
- Add a short callout box near release-smoke.ps1 description in README.md (repo root) pointing to the distinction.

Constraints:
- No behavioral changes to scripts unless fixing misleading wording/help text.

Acceptance criteria:
- A new contributor can answer: “Does passing release-smoke.ps1 prove live API parity?” in one minute.

What not to change:
- CI job matrices beyond documentation unless clearly broken.
```

---

### 3) Add a Prometheus/Grafana panel bundle for authority pipeline stage latency + outbox depth

- **Why it matters:** Operational credibility ties directly to enterprise trust; metrics exist ([OBSERVABILITY.md](OBSERVABILITY.md)) but buyers/operators need curated dashboards.
- **Expected impact:** Supportability (+4–6), Manageability (+3–5), Observability (+3–5). **Weighted readiness impact:** ~**+0.20–0.35%**.

**Cursor prompt:**

```text
Goal: Ship a Grafana dashboard JSON (or Terraform provisioning snippet) that charts:
- archlucid_authority_pipeline_stage_duration_ms by stage/outcome
- archlucid_authority_pipeline_work_pending gauge
- archlucid_data_consistency_orphans_detected_total vs alerts

Scope:
- Use infra/grafana/dashboards/README.md conventions.
- Reference metric names exactly from ArchLucid.Core.Diagnostics.ArchLucidInstrumentation via docs/library/OBSERVABILITY.md.
- Add docs/runbooks entry linking dashboard panels to remediation steps (queue backlog, SQL tier, worker scaling).

Constraints:
- No new metric names without code change (dashboard-only).

Acceptance criteria:
- Dashboard loads in Grafana import JSON workflow documented.
- Panels labeled with “what good looks like” thresholds referencing infra/prometheus/archlucid-alerts.yml where applicable.

What not to change:
- Production Terraform defaults without documenting vars.
```

---

### 4) Operationalize PMF_VALIDATION_TRACKER.md without inventing customer names

- **Why it matters:** Empty tables signal “no learning loop” to investors/customers even when product works ([PMF_VALIDATION_TRACKER.md](../go-to-market/PMF_VALIDATION_TRACKER.md)).
- **Expected impact:** Proof-of-ROI Readiness (+4–7), Marketability (+2–4). **Weighted readiness impact:** ~**+0.25–0.40%**.

**Cursor prompt:**

```text
Goal: Make PMF_VALIDATION_TRACKER.md usable with anonymized pilot IDs (no customer names) and explicit “unknown/pending” semantics.

Scope:
- Update docs/go-to-market/PMF_VALIDATION_TRACKER.md tables with guidance rows using anonymized identifiers (Pilot A/B) and instructions for population cadence.
- Cross-link to docs/library/PILOT_ROI_MODEL.md measurement sections.
- Add a short “Ethics / confidentiality” note describing what can/cannot be published pre-logo.

Constraints:
- Do not fabricate numeric pilot outcomes.

Acceptance criteria:
- Team can fill tracker after internal dogfood runs without legal review for anonymized aggregates.

What not to change:
- Deferred reference-customer publication policy in V1_DEFERRED.md.
```

---

### 5) CAIQ Lite “Partial” rows: link evidence artifacts per row

- **Why it matters:** Procurement friction concentrates where questionnaires say “partial” without pointers ([CAIQ_LITE_2026.md](../security/CAIQ_LITE_2026.md)).
- **Expected impact:** Compliance Readiness (+5–8), Procurement Readiness (+3–6). **Weighted readiness impact:** ~**+0.25–0.40%**.

**Cursor prompt:**

```text
Goal: For each “Partial” row in docs/security/CAIQ_LITE_2026.md, add a concrete Evidence link (existing markdown path, CI job name, or Terraform module path). If evidence truly absent, label as explicit backlog item with owner-facing TODO in docs/PENDING_QUESTIONS.md format (but do not block merge—documentation-only).

Scope:
- Edit docs/security/CAIQ_LITE_2026.md only (+ minimal cross-links).

Constraints:
- Follow docs scope header rule for edited files.

Acceptance criteria:
- No row remains “Partial” without either evidence link or explicit gap statement + next step.

What not to change:
- SOC2 roadmap dates/claims.
```

---

### 6) Tier k6 CI smoke thresholds to fail on regressions

- **Why it matters:** Performance score reflects limited universal proof; simplest guard is thresholds on existing scripts ([tests/load/ci-smoke.js](../../tests/load/ci-smoke.js)).
- **Expected impact:** Performance (+5–8), Reliability (+2–4). **Weighted readiness impact:** ~**+0.15–0.25%**.

**Cursor prompt:**

```text
Goal: Add configurable thresholds to tests/load/ci-smoke.js (and documented env vars) matching docs/library/API_SLOS.md latency discussions where applicable.

Scope:
- Inspect .github/workflows/ci.yml k6 jobs for how env vars are passed.
- Implement `--threshold` blocks in k6 with sane defaults for CI noise; allow override via env for staging soak.

Constraints:
- Do not point scheduled soak jobs at production tenants by default.

Acceptance criteria:
- CI fails if p95 latency exceeds threshold on smoke profile.

What not to change:
- Load test scenarios beyond smoke unless necessary.
```

---

### 7) Cognitive load: consolidate synonyms in LayerHeader / operate hints

- **Why it matters:** Packaging narrative is strong but vocabulary multiplicity hurts usability ([PRODUCT_PACKAGING.md](PRODUCT_PACKAGING.md)).
- **Expected impact:** Cognitive Load (+6–10), Usability (+3–5). **Weighted readiness impact:** ~**+0.15–0.25%**.

**Cursor prompt:**

```text
Goal: Reduce confusing synonyms between Pilot vs Operate layers in UI hints.

Scope:
- Edit archlucid-ui/src/lib/layer-guidance.ts and OperateCapabilityHints.tsx (and related tests) to align wording with docs/library/PRODUCT_PACKAGING.md §two layers.
- Update Vitest regressions if strings asserted.

Constraints:
- No API behavior changes.

Acceptance criteria:
- Fewer distinct phrases meaning the same gate; accessibility roles unchanged.

What not to change:
- Authority logic or nav authority mappings.
```

---

### 8) Golden corpus: add one governance edge-case regression

- **Why it matters:** Governance correctness underpins enterprise adoption; corpus protects merges ([tests/golden-corpus/decisioning/](../../tests/golden-corpus/decisioning/)).
- **Expected impact:** Correctness (+3–5), Architectural Integrity (+2–3). **Weighted readiness impact:** ~**+0.12–0.20%**.

**Cursor prompt:**

```text
Goal: Add tests/golden-corpus/decisioning/case-32/ with input.json + expected findings/decisions/audit-types mirroring an edge scenario described in docs/library/GOVERNANCE.md (choose one: duplicate reject attempt / dry-run adjacent behavior if representable deterministically).

Scope:
- Follow existing GoldenCorpusRegressionTests patterns in ArchLucid.Decisioning.Tests.

Constraints:
- Deterministic only—no LLM.

Acceptance criteria:
- dotnet test ArchLucid.Decisioning.Tests includes new case with stable outputs.

What not to change:
- Historical migrations or golden corpus directories for unrelated cases.
```

---

## Pending Questions for Later

Organized by improvement title (blocking / decision-shaping only):

- **JWT Playwright merge-blocking**
  - Are there known flaky tests or secret availability gaps on GitHub-hosted runners blocking promotion?

- **k6 thresholds**
  - What latency/error targets does the business want to advertise externally vs keep internal-only?

- **PMF tracker operationalization**
  - What anonymization standard is acceptable for public excerpts (ranges only vs rounded hours)?

- **CAIQ evidence linking**
  - Which questionnaires must remain lawyer-reviewed before linking operational evidence from private repos?

---

**Weighted readiness calculation note:** \(\frac{\sum_i s_i w_i}{102} = 73.97\%\) using the scores listed per-quality in this document’s urgency table (Commercial **40**, Enterprise **25**, Engineering **37**).
