> **Scope:** Independent first-principles weighted readiness assessment (2026-05-02) — scoring only; not a prior assessment update. Excludes scope explicitly deferred to V1.1/V2 per [`V1_DEFERRED.md`](V1_DEFERRED.md). Evidence: repo docs and packaging as of this date.

# ArchLucid Assessment – Weighted Readiness 76.49%

## 1. Executive Summary

### Overall readiness
ArchLucid presents as a **credible V1 pilot product**: a bounded Core Pilot (request → execute → commit → artifacts), a large **Operate** surface for analysis and governance, strong **engineering hygiene** (versioned API, OpenAPI contract tests, ZAP/Schemathesis gates, SQL RLS story, audit event catalog), and unusually thorough **documentation**. Weighted readiness **76.49%** reflects real strength in interoperability, traceability, security controls, and packaging clarity, offset by **commercial headwinds** (category noise, sales-led motion, buyer cognitive load) and **residual trust/compliance gaps that are honest but not enterprise-credential-complete** within V1 scope (no third-party pen-test publication, no SOC 2 attestation—both documented as out of scope for V1 per deferred-scope rules).

### Commercial picture
The **Pilot vs Operate** story is clear and defensible. **Proof-of-ROI readiness is a bright spot**: first-value report, pilot deltas, buyer-safe proof package contract, and executive sponsor artifacts are concretely described in [`PILOT_ROI_MODEL.md`](PILOT_ROI_MODEL.md) and [`EXECUTIVE_SPONSOR_BRIEF.md`](../EXECUTIVE_SPONSOR_BRIEF.md). **Marketability and adoption friction** remain the main commercial risks: buyers must parse progressive disclosure navigation, authority tiers, and “run” vs “architecture review” vocabulary even though the hybrid copy rule exists in [`PRODUCT_PACKAGING.md`](PRODUCT_PACKAGING.md). **Self-serve commerce live** and **published reference customers** are treated as **V1.1** per [`V1_DEFERRED.md`](V1_DEFERRED.md)—this assessment does not score them as V1 defects.

### Enterprise picture
**Traceability, auditability, and policy/governance** are materially stronger than typical early-stage SaaS: append-only audit model, typed events, governance workflows, policy packs, and explicit multi-tenant RLS documentation. **Procurement and compliance readiness** are “packaged honesty” ([`TRUST_CENTER.md`](../go-to-market/TRUST_CENTER.md), CAIQ/SIG drafts, DPA templates) rather than finished attestations—workable for **early adopters and smart pilots**, slower for **Fortune-scale security committees** seeking independent assurance. **Workflow embeddedness** favors Azure-native and API/webhook patterns; first-party Jira/ServiceNow/Slack are explicitly **V1.1/V2**, not scored against V1.

### Engineering picture
Architecture is **coherent and modular** (C4 poster in [`ARCHITECTURE_ON_ONE_PAGE.md`](../ARCHITECTURE_ON_ONE_PAGE.md), bounded containers, clear API contracts in [`API_CONTRACTS.md`](API_CONTRACTS.md)). **Security and Azure fit** are above average for the stage. **Correctness** for AI-assisted outputs still carries inherent LLM risk; mitigations (simulator defaults, evaluation hooks, explain endpoints) are present but do not eliminate reviewer skepticism. **CI testability** is serious ([`CODE_COVERAGE.md`](CODE_COVERAGE.md) describes merge-blocking merged line/branch floors). **Scalability and multi-region product guarantees** are appropriately constrained in [`V1_SCOPE.md`](V1_SCOPE.md).

### Deferred Scope Uncertainty
**None.** Explicit V1.1/V2 deferrals are consolidated in [`V1_DEFERRED.md`](V1_DEFERRED.md) (e.g., MCP V1.1, ITSM connectors V1.1, Slack V2, commerce un-hold V1.1, third-party pen test V2, PGP key V1.1, public reference customer V1.1).

---

## 2. Weighted Quality Assessment

**Method:** Score 1–100. Weight as given. Total weight **102**. Weighted readiness = Σ(score × weight) / 102 = **76.49%**. **Weighted deficiency signal** = (100 − score) × weight (higher = more urgent). **Weighted impact on readiness** = (score × weight) / 102 (percentage points toward the headline total).

Order: **most urgent → least urgent** (by weighted deficiency signal; ties broken by higher weight first).

| Quality | Score | Weight | Weighted deficiency signal | Weighted impact on readiness |
|--------|------:|-------:|----------------------------:|--------------------------:|
| Marketability | 72 | 8 | 224 | 5.65 |
| Adoption Friction | 68 | 6 | 192 | 4.00 |
| Time-to-Value | 78 | 7 | 154 | 5.35 |
| Differentiability | 70 | 4 | 120 | 2.75 |
| Correctness | 76 | 4 | 96 | 2.98 |
| Proof-of-ROI Readiness | 82 | 5 | 90 | 4.02 |
| Usability | 70 | 3 | 90 | 2.06 |
| Workflow Embeddedness | 72 | 3 | 84 | 2.12 |
| Executive Value Visibility | 80 | 4 | 80 | 3.14 |
| Trustworthiness | 74 | 3 | 78 | 2.18 |
| Decision Velocity | 65 | 2 | 70 | 1.27 |
| Compliance Readiness | 68 | 2 | 64 | 1.33 |
| Procurement Readiness | 70 | 2 | 60 | 1.37 |
| Commercial Packaging Readiness | 72 | 2 | 56 | 1.41 |
| Architectural Integrity | 82 | 3 | 54 | 2.41 |
| Reliability | 73 | 2 | 54 | 1.43 |
| AI/Agent Readiness | 74 | 2 | 52 | 1.45 |
| Security | 84 | 3 | 48 | 2.47 |
| Traceability | 85 | 3 | 45 | 2.50 |
| Data Consistency | 78 | 2 | 44 | 1.53 |
| Maintainability | 80 | 2 | 40 | 1.57 |
| Explainability | 81 | 2 | 38 | 1.59 |
| Policy and Governance Alignment | 84 | 2 | 32 | 1.65 |
| Scalability | 68 | 1 | 32 | 0.67 |
| Cognitive Load | 68 | 1 | 32 | 0.67 |
| Interoperability | 85 | 2 | 30 | 1.67 |
| Auditability | 86 | 2 | 28 | 1.69 |
| Azure Compatibility and SaaS Deployment Readiness | 86 | 2 | 28 | 1.69 |
| Customer Self-Sufficiency | 72 | 1 | 28 | 0.71 |
| Performance | 72 | 1 | 28 | 0.71 |
| Cost-Effectiveness | 74 | 1 | 26 | 0.73 |
| Availability | 75 | 1 | 25 | 0.74 |
| Stickiness | 75 | 1 | 25 | 0.74 |
| Observability | 76 | 1 | 24 | 0.75 |
| Accessibility | 78 | 1 | 22 | 0.76 |
| Extensibility | 78 | 1 | 22 | 0.76 |
| Manageability | 78 | 1 | 22 | 0.76 |
| Template and Accelerator Richness | 78 | 1 | 22 | 0.76 |
| Testability | 79 | 1 | 21 | 0.77 |
| Change Impact Clarity | 80 | 1 | 20 | 0.78 |
| Evolvability | 80 | 1 | 20 | 0.78 |
| Supportability | 82 | 1 | 18 | 0.80 |
| Deployability | 83 | 1 | 17 | 0.81 |
| Modularity | 84 | 1 | 16 | 0.82 |
| Azure Ecosystem Fit | 85 | 1 | 15 | 0.83 |
| Documentation | 88 | 1 | 12 | 0.86 |

### Per-quality detail (same urgent → least urgent order)

For each: **Justification** · **Tradeoffs** · **Improvement recommendations** · **Fix horizon**

1. **Marketability (72, w8)** — **Why:** Strong narrative docs and trust pack, but crowded “AI + governance” market; buyer must work to see wedge; sales-led motion limits PLG story vs fully live self-serve (latter V1.1-deferred, not penalized). **Tradeoffs:** Broader marketing invites scope creep accusations; narrow marketing undersells Operate. **Improve:** Single-sentence category claim tied to measurable pilot outcomes; tighten public `/get-started` and Core Pilot cohesion; publish one flagship “before/after” anonymized story without needing a named reference customer. **v1** (positioning/content).

2. **Adoption Friction (68, w6)** — **Why:** Progressive disclosure + authority tiers + packaging docs reduce casual success; determined operators succeed; distracted buyers stall. **Tradeoffs:** Simplifying the shell can hide governance value. **Improve:** Default shell to Pilot-first preset for new workspaces; reduce concurrent “show tier” switches; canonical onboarding route. **v1**.

3. **Time-to-Value (78, w7)** — **Why:** [`BUYER_FIRST_30_MINUTES.md`](../BUYER_FIRST_30_MINUTES.md) and hosted funnel support fast evaluation; org rollout adds IdP, policy, and data classification time. **Tradeoffs:** Faster demos can overpromise depth. **Improve:** Guarantee one “happy path” in-product that mirrors the five-step buyer doc end-to-end; time-boxed checklist state in UI. **v1**.

4. **Differentiability (70, w4)** — **Why:** Evidence-chain-first and audit depth differentiate technically; incumbent tools claim similar outcomes. **Cannot fix uniqueness in docs alone.** **Tradeoffs:** Differentiation via features increases surface area. **Improve:** Lead demos with immutable audit + replay guarantees vs generic “copilot” claims. **v1**.

5. **Correctness (76, w4)** — **Why:** Contract tests, golden paths, and structured findings help; LLM-assisted explain/compare remains probabilistic. **Tradeoffs:** More determinism can reduce usefulness. **Improve:** Expand golden corpus on compare/explain; explicit “deterministic vs LLM” labeling in UI; strengthen replay verify modes in sales stories. **v1** (incremental).

6. **Proof-of-ROI Readiness (82, w5)** — **Why:** [`PILOT_ROI_MODEL.md`](PILOT_ROI_MODEL.md) ties metrics to persisted fields; buyer-safe proof contract is rare and strong. **Tradeoffs:** Strong metrics can be misread as financial promises. **Improve:** Automate “proof package completeness” gating in UI before export email. **v1**.

7. **Usability (70, w3)** — **Why:** Operate pages are powerful; first-session users face layered hints and rank-gated controls. **Tradeoffs:** Oversimplification vs enterprise honesty. **Improve:** Persona-based default nav; progressive “why is this disabled?” microcopy tied to `/me`. **v1**.

8. **Workflow Embeddedness (72, w3)** — **Why:** Webhooks, Teams, CI integrations exist; deep ITSM is bridge/recipe or V1.1 connector. **Tradeoffs:** More native connectors increase security liability. **Improve:** Ship one reference “recipe” pipeline per major buyer stack (already started in docs—ensure discoverability from product). **v1** (docs + templates).

9. **Executive Value Visibility (80, w4)** — **Why:** Sponsor PDF path and executive brief are concrete. **Tradeoffs:** Executive views risk summarizing away caveats. **Improve:** One-click “sponsor view” that hides technical noise by default. **v1**.

10. **Trustworthiness (74, w3)** — **Why:** Engineering controls are serious; absence of **third-party** pen test and SOC2 **attestation** caps enterprise trust despite honest Trust Center (aligned with V1 deferrals). **Tradeoffs:** Earlier attestations burn cash. **Improve:** Make owner-conducted pen test narrative crisp and dated; expand CAIQ/SIG mapping to live links. **v1** (documentation/evidence hygiene); attestation **v1.1+**.

11. **Decision Velocity (65, w2)** — **Why:** Procurement pack automation helps; questionnaire fatigue remains; legal still reviews templates. **Tradeoffs:** Over-automated packs can mislead. **Improve:** “Procurement fast lane” one-pager: what’s signed, what’s template, what’s unavailable. **v1**.

12. **Compliance Readiness (68, w2)** — **Why:** GDPR/DPA/subprocessor docs exist; SOC 2 Type report deferred per Trust Center candor. **Tradeoffs:** Claiming too much creates legal exposure; honesty slows enterprise velocity. **Improve:** Compliance matrix pointers always point to exact repo revision used in build. **v1**.

13. **Procurement Readiness (70, w2)** — **Why:** Strong artifact index [`PROCUREMENT_EVIDENCE_PACK_INDEX.md`](../go-to-market/PROCUREMENT_EVIDENCE_PACK_INDEX.md); buyers still want independent assurance for large deals. **Tradeoffs:** Larger packs increase review fatigue without a “fast lane” map. **Improve:** `archlucid procurement-pack` manifest versioning in buyer comms. **v1**.

14. **Commercial Packaging Readiness (72, w2)** — **Why:** Tier gates and Operate narrative exist; mixed messaging risk between Pilot simplicity and Operate power. **Tradeoffs:** Pilot-first clarity can under-open expansion revenue with sophisticated buyers. **Improve:** Align pricing page language to Pilot-first motion explicitly. **v1**.

15. **Architectural Integrity (82, w3)** — **Why:** Clear container boundaries and strangler ADRs; large codebase still demands discipline. **Improve:** Keep coordinator/authority split enforced in reviews (already policy in [`V1_SCOPE.md`](V1_SCOPE.md)). **v1**.

16. **Reliability (73, w2)** — **Why:** Health endpoints, hosted probes, game-day log; production chaos explicitly gated. **Tradeoffs:** Aggressive chaos could alarm customers. **Improve:** Publish internal monthly rollup methodology clearly separated from contractual SLA. **v1** (transparency).

17. **AI/Agent Readiness (74, w2)** — **Why:** Simulator-first, eval hooks, content safety in prod-like hosts; MCP deferred V1.1. **Tradeoffs:** Stronger autonomy increases safety and scope-conflict risk. **Improve:** Thin “agent policy” doc for customers: what agents can/cannot do in V1. **v1**.

18. **Security (84, w3)** — **Why:** Fail-closed auth defaults, ZAP merge gate, Schemathesis, RLS bypass highly gated. **Tradeoffs:** Security friction for quick pilots. **Improve:** Align `README.md` operational notes with `appsettings` so no accidental “open dev defaults” misread ([`SECURITY.md`](SECURITY.md) vs older README bullets—verify consistency in prompts below). **v1**.

19. **Traceability (85, w3)** — **Why:** Evidence chains, audit events, correlation IDs. **Tradeoffs:** Richer forensic detail increases storage and export sensitivity. **Improve:** Cap audit row UI lower-bound messaging until paging fully understood (documented in [`V1_DEFERRED.md`](V1_DEFERRED.md)). **v1**.

20. **Data Consistency (78, w2)** — **Why:** SQL authoritative, consistency matrix referenced from API contracts doc. **Tradeoffs:** Stricter consistency modes can reduce throughput or flexibility. **Improve:** Continue publishing drift replay outcomes as first-class buyer trust artifact. **v1**.

21. **Maintainability (80, w2)** — **Why:** Modular projects, clear nav/doc contracts. **Tradeoffs:** More guardrails slow casual contributions. **Improve:** Keep `PRODUCT_PACKAGING.md` drift guard linked from PR template. **v1**.

22. **Explainability (81, w2)** — **Why:** `/v1/explain` family and UI sections; volume can overwhelm. **Tradeoffs:** Shorter explanations can omit audit-grade detail buyers demand. **Improve:** Summary-first pattern for findings (see improvements). **v1**.

23. **Policy and Governance Alignment (84, w2)** — **Why:** Pre-commit gate, policy packs, approvals—rare depth for V1. **Tradeoffs:** Misconfiguration can block commits—support burden. **Improve:** Simulate gate outcomes in UI before rollout. **v1**.

---

**Qualities 24–46:** Each entry below keeps **Why / Improve / fix horizon** short; **tradeoffs** are predominantly **incremental engineering investment vs buyer-visible payoff** (most are already strong; raising scores further is diminishing returns versus commercial UX).

24. **Scalability (68, w1)** — **Why:** V1 honest about single-region/active-active stance. **Improve:** Document scale ceilings with test evidence as load tests mature. **v1.1** for claims.

25. **Cognitive Load (68, w1)** — **Why:** Power exposed through progressive disclosure; still high for some personas. **Improve:** Default presets and inspect-first panels. **v1**.

26. **Interoperability (85, w2)** — **Why:** OpenAPI canonical, AsyncAPI for webhooks, CLI parity. **Improve:** None critical—keep snapshot tests green. **v1**.

27. **Auditability (86, w2)** — **Why:** Typed durable audit + export paths. **Improve:** SIEM export narrative tied to customer examples. **v1** (docs).

28. **Azure Compatibility and SaaS Deployment Readiness (86, w2)** — **Why:** Terraform modules, Azure-native ids. **Improve:** Keep `REFERENCE_SAAS_STACK_ORDER` as single deploy spine. **v1**.

29. **Customer Self-Sufficiency (72, w1)** — **Why:** Docs are deep; volume can hinder. **Improve:** “First day / first week” operator rails in-product, not only markdown. **v1**.

30. **Performance (72, w1)** — **Why:** Rate limits documented; perf doc exists but not headline strength. **Improve:** Publish latency percentiles under defined load for Professional tier (staging-backed). **v1.1** if binding.

31. **Cost-Effectiveness (74, w1)** — **Why:** LLM call counting and pilot ROI tie to cost shape. **Improve:** Tenant-visible “run cost estimate” from persisted counts (read-only). **v1** optional.

32. **Availability (75, w1)** — **Why:** Targets stated pre-contractually. **Improve:** Separate staging probe vs production SLI in buyer docs explicitly (Trust Center partially does). **v1**.

33. **Stickiness (75, w1)** — **Why:** Manifests + audit history create switching costs for active tenants. **Improve:** None urgent. **v1**.

34. **Observability (76, w1)** — **Why:** Correlation IDs, metrics hooks referenced; buyer-visible observability story thinner. **Improve:** Standard dashboards checklist for enterprise handoff. **v1** (runbooks).

35. **Accessibility (78, w1)** — **Why:** Public attestation and tooling references in Trust Center. **Improve:** Expand route coverage annually as claimed. **v1** ongoing.

36. **Extensibility (78, w1)** — **Why:** Events and webhooks; extension without MCP until V1.1. **Improve:** Example consumer repo template. **v1** (sample).

37. **Manageability (78, w1)** — **Why:** SCIM, tiers, many toggles—ops power with complexity. **Improve:** Export/import tenant config bundle for DR drills. **v1.1**.

38. **Template and Accelerator Richness (78, w1)** — **Why:** Briefs and integration templates exist. **Improve:** Curated “industry starter packs” index page. **v1**.

39. **Testability (79, w1)** — **Why:** Strong CI gates; live Playwright vs mock distinction must stay visible. **Improve:** One blessed “live SQL E2E” badge in release docs. **v1**.

40. **Change Impact Clarity (80, w1)** — **Why:** Breaking changes and ADRs present. **Improve:** Auto-link OpenAPI diff to `BREAKING_CHANGES.md`. **v1**.

41. **Evolvability (80, w1)** — **Why:** API versioning policy explicit. **Improve:** Continue deprecation headers practice. **v1**.

42. **Supportability (82, w1)** — **Why:** `doctor`, support bundle, triage docs. **Improve:** In-app “copy diagnostic package” for operators. **v1**.

43. **Deployability (83, w1)** — **Why:** DbUp migrations, compose profiles. **Improve:** Single-command SaaS smoke for internal operators documented atop `release-smoke`. **v1**.

44. **Modularity (84, w1)** — **Why:** Many assemblies; boundaries documented. **Improve:** Architecture tests enforce layer deps (if not already). **v1**.

45. **Azure Ecosystem Fit (85, w1)** — **Why:** Entra, OpenAI, Service Bus optional. **Improve:** None urgent. **v1**.

46. **Documentation (88, w1)** — **Why:** Exceptional spine and library; risk is findability not volume. **Improve:** Thin routing layer only—avoid more root MD sprawl per repo rules. **v1**.

---

## 3. Top 10 Most Important Weaknesses

1. **Buyer cognitive overload at first contact** — packaging power shows up as configuration and disclosure switches before value lands (high impact on Marketability + Adoption Friction).
2. **Category sameness risk** —without a crisp, evidence-backed wedge, buyers lump ArchLucid with generic “AI architecture assistants.”
3. **Trust ceiling without independent attestation** — controls can be strong while procurement still stalls; honestly deferred items do not remove this purchase friction in F200.
4. **LLM-dependent outputs remain probabilistic** — threatens Correctness and reviewer Trustworthiness even with mitigations.
5. **README / security messaging drift risk** — contributor README length creates opportunities for misleading “dev defaults” interpretation vs [`SECURITY.md`](SECURITY.md) fail-closed posture.
6. **Operate surface breadth vs Pilot wedge** — excellent for expansion, hazardous if evaluators wander before Core Pilot proof.
7. **Performance/scalability evidence not buyer-central** — honest V1 scope limits are right; buyers may still ask for numbers you do not publish.
8. **Customer self-sufficiency uneven** — great for readers who love docs; weaker for teams that refuse long reads.
9. **Workflow embedding relies on DIY bridges** where ITSM first-party connectors are deferred—acceptable for V1 scope but slows time-to-politics inside enterprises.
10. **“Don’t over-claim” discipline is a double-edged sword** — ethically correct per [`PILOT_ROI_MODEL.md`](PILOT_ROI_MODEL.md); sometimes hurts urgency in competitive bake-offs.

---

## 4. Top 5 Monetization Blockers

1. **Category noise + unclear decisive wedge** delaying champion formation (stalls paid pilot expansion).
2. **Procurement calendar** when legal/infosec expects SOC 2 Type II or third-party pen test **even if** you correctly defer them—deal still slips.
3. **Sales-led motion friction** if internal technical evaluators cannot self-serve past shell complexity quickly.
4. **Reference-proof asymmetry**—you can prove technical artifacts, but buyers still hunt for **peer logos** (V1.1 per deferred scope; real blocker in market even if not scored lower here per instructions).
5. **Weak “land + expand” instrumentation** from product telemetry if funnel metrics are not durable—expansion revenue depends on proving repeated use inside tenant.

---

## 5. Top 5 Enterprise Adoption Blockers

1. **Independent assurance gap** (SOC 2 report, third-party pen-test summary—explicitly V2/V1.1 pathways) versus buyer policy “must-haves.”
2. **Data residency / multi-region active/active expectations** vs stated V1 honesty in [`V1_SCOPE.md`](V1_SCOPE.md).
3. **Segregation-of-duties and break-glass stories**—RLS bypass is guarded; buyers still probe operational abuse scenarios ([`SECURITY.md`](SECURITY.md)).
4. **LLM data handling + retention** questions (prompt persistence modes) even with redaction docs—requires customer-specific DPA alignment.
5. **Integration expectations with ServiceNow/Jira** deferred to V1.1—enterprises will ask; recipes must be boringly reliable.

---

## 6. Top 5 Engineering Risks

1. **Authorization confusion** if UI shaping is treated as entitlement proof—docs warn; regressions would be severe ([`PRODUCT_PACKAGING.md`](PRODUCT_PACKAGING.md)).
2. **Drift between OpenAPI canonical and explorer OpenAPI** causing client breakage ([`API_CONTRACTS.md`](API_CONTRACTS.md)).
3. **SQL migration failures fail startup**—correct for integrity, brittle for ops if rollforward discipline lags.
4. **Audit pagination / cap semantics** misunderstood in investigations (signals as lower bound).
5. **Hosted stack configuration errors** (auth mode, content safety in prod-like) leading to false confidence or outage—mitigated by guards, still a real ops failure mode.

---

## 8. Most Important Truth

**ArchLucid is unusually “engine-real” for its stage—bounded pilot path, serious security/testing posture, and honest trust packaging—but enterprise revenue will still move at the speed of buyer trust and first-session comprehension, not at the speed of your feature depth.**

---

## 9. Top Improvement Opportunities

### 1. Reconcile README authentication guidance with SECURITY.md fail-closed posture
- **Why it matters:** Misconfigured pilots erode Trustworthiness and Correctness signals.
- **Expected impact:** Fewer dangerous misreads for new operators; cleaner procurement story.
- **Affected qualities:** Correctness, Security, Supportability, Customer Self-Sufficiency.
- **Actionable:** Yes.
- **Impact of running the prompt:** Time-to-Value (+2–4 pts), Trust (+2–3). Weighted readiness impact: **+0.15–0.25%**.

**Cursor prompt**
```
Goal: Remove contradictions between README.md “API authentication” / “Running the API” sections and docs/library/SECURITY.md regarding ApiKey enabled defaults and unauthenticated dev behavior.

Files: README.md (primary), cross-check SECURITY.md and ArchLucid.Api/appsettings.json comments if referenced from README.

Work:
1. Search README for statements that imply Authentication:ApiKey:Enabled=false allows unauthenticated full-permission access.
2. Replace with SECURITY.md-accurate wording: fail-closed until keys enabled; DevelopmentBypass only in Development host per appsettings.Development.json; cite SECURITY.md for depth.
3. Keep buyer vs contributor lanes clear—no changes to buyer paths.

Acceptance criteria:
- README does not claim open unauthenticated access contradictory to SECURITY.md.
- Single canonical pointer to SECURITY.md for auth matrix.

Constraints: Do not change runtime code. Do not edit historical migration SQL.

Out of scope: Marketing pages.
```

### 2. Canonical first-session onboarding route + redirect matrix
- **Why it matters:** Cuts Adoption Friction, Cognitive Load, Usability.
- **Expected impact:** Faster first commits in hosted funnel.
- **Affected qualities:** Adoption Friction, Cognitive Load, Usability, Time-to-Value.
- **Actionable:** Yes.
- **Impact:** Marketability (+2–4), Adoption Friction (+4–8). Weighted: **+0.35–0.65%**.

**Cursor prompt**
```
Goal: One canonical “first session” onboarding path for archlucid-ui operator shell; eliminate duplicate / ambiguous routes (e.g., onboarding vs getting-started) per nav + docs alignment.

Files: archlucid-ui/src/app/(operator) routing tree, nav-config.ts, any redirects, docs/CORE_PILOT.md and docs/BUYER_FIRST_30_MINUTES.md if hrefs change, Vitest for redirects if present.

Work:
1. Inventory onboarding entry URLs from nav-config and deep links in docs.
2. Pick ONE canonical path; add Next.js redirects from legacy paths preserving query strings.
3. Update docs links to canonical path only.

Acceptance criteria:
- Only one primary onboarding href in Pilot nav group (unless intentional alias redirects).
- Tests cover legacy path → canonical redirect with query preservation.

Constraints: Do not loosen API authorization. Do not change V1.1/V2 scope.

Out of scope: Marketing domain routes outside operator shell.
```

### 3. Default Pilot-focused navigation preset for new tenants / pre-finalization
- **Why it matters:** Reduces Cognitive Load; protects Pilot wedge.
- **Expected impact:** Higher completion of Core Pilot checklist.
- **Affected qualities:** Cognitive Load, Usability, Adoption Friction, Time-to-Value.
- **Actionable:** Yes (partial—use existing persistence hooks if documented in PRODUCT_PACKAGING).
- **Impact:** Cognitive Load (+6–10), Usability (+4–6). Weighted: **+0.25–0.45%**.

**Cursor prompt**
```
Goal: Default operator shell navigation preset to Pilot-focused for users/tenants until first finalized review package (or equivalent existing signal—do not invent new analytics).

Files: archlucid-ui (operator-nav-preset storage), any existing “finalized review” helpers cited in docs/library/PRODUCT_PACKAGING.md or run detail components, Vitest for defaults + override persistence.

Work:
1. Find existing preset + persistence mechanism (localStorage keys, etc.).
2. If product defines “finalized review package” already in code, auto-expand Operate disclosure only after that milestone; otherwise gate only on “first successful commit + artifacts viewed” minimally—prefer reusing existing helper.
3. Add tests: default preset, upgrade affordance after milestone, manual override persists.

Acceptance criteria:
- New user lands in Pilot-first preset without hiding server-side capabilities improperly (disclosure only).
- Execute+ gating unchanged; API still authoritative.

Constraints: No entitlement changes. No changes to ArchLucidPolicies.

Out of scope: Backend schema migrations.
```

### 4. Finding inspect-first summary layer (summary card + technical audit disclosure)
- **Why it matters:** Explainability and Trustworthiness for reviewers.
- **Expected impact:** Faster human validation of findings.
- **Affected qualities:** Explainability, Trustworthiness, Cognitive Load, Correctness (review loop).
- **Actionable:** Yes.
- **Impact:** Explainability (+5–8), Trust (+3–5). Weighted: **+0.20–0.35%**.

**Cursor prompt**
```
Goal: Add inspect-first summary block to finding explain surfaces before raw audit / long prose.

Files: archlucid-ui FindingExplainPanel and adjacent run/finding detail components, existing tests colocated.

Work:
1. Render top summary: title/id, severity, affected component (if available), evidence count, confidence/trace fields if available, one recommended next action.
2. Move redacted prompt/completion audit behind disclosure labeled “Technical audit details”.
3. Preserve authority checks and existing API usage.

Acceptance criteria:
- Vitest covers summary-first render + disclosure toggles accessibility attributes.

Constraints: Do not change API contracts. Do not log additional PII.

Out of scope: Changing LLM prompts.
```

### 5. Proof-package completeness UX gate before sponsor email / PDF actions
- **Why it matters:** Protects Proof-of-ROI and Marketability from self-inflicted credibility wounds.
- **Expected impact:** Fewer incomplete sponsor sends.
- **Affected qualities:** Proof-of-ROI Readiness, Executive Value Visibility, Trustworthiness.
- **Actionable:** Yes.
- **Impact:** Proof-of-ROI (+4–7), Exec Visibility (+2–4). Weighted: **+0.25–0.40%**.

**Cursor prompt**
```
Goal: In-product guard that mirrors PILOT_ROI_MODEL “buyer-safe proof package contract” before emailing/downloading sponsor PDF from run detail.

Files: archlucid-ui components for EmailRunToSponsorBanner / sponsor actions, client calls to pilot endpoints if needed (read-only checks), tests.

Work:
1. Encode checklist client-side from existing pilot-run-deltas or first-value report metadata already fetched (avoid duplicating business rules server-side if possible; if mismatch risk, add minimal read endpoint validation flag in API — only if absolutely necessary).
2. Block primary CTA with explicit missing fields list; secondary “continue anyway” is NOT allowed for external email copy—hard block only.

Acceptance criteria:
- Unit/integration tests for complete vs incomplete states.

Constraints: Do not weaken demo-data warnings. Do not bypass auth.

Out of scope: Changing PDF layout.
```

### 6. Procurement fast-lane one-pager (signed vs template vs unavailable)
- **Why it matters:** Decision Velocity and Procurement Readiness.
- **Expected impact:** Shorter security reviews for educated buyers.
- **Affected qualities:** Procurement Readiness, Decision Velocity, Compliance Readiness.
- **Actionable:** Yes.
- **Impact:** Procurement (+4–6), Decision Velocity (+5–8). Weighted: **+0.25–0.40%**.

**Cursor prompt**
```
Goal: Add buyer-facing single page under docs/go-to-market/ (not docs root—avoid root budget) titled PROCUREMENT_FAST_LANE.md with three-column matrix: Artifact | What buyer receives today | Status (Template / Owner attestation / Not available until Vx).

Files: new markdown under docs/go-to-market/, link from PROCUREMENT_EVIDENCE_PACK_INDEX.md and trust-center.md if appropriate.

Work:
1. Use only factual statements grounded in existing Trust Center + SECURITY + SOC2 roadmap posture.
2. Explicitly map SOC 2 and third-party pen-test to “V2 / ARR-gated” per TRUST_CENTER without sounding evasive.

Acceptance criteria:
- Passes doc scope header rule.
- No legal claims beyond templates without marking as template.

Constraints: Do not modify Terraform. No ArchiForge strings.

Out of scope: Filling SOC2 audit.
```

### 7. Core Pilot funnel telemetry (privacy-minimal, existing patterns)
- **Why it matters:** Monetization expansion requires activation metrics; supports Proof-of-ROI and Marketability claims with data.
- **Expected impact:** Ability to show activation curves to investors/customers.
- **Affected qualities:** Proof-of-ROI Readiness, Marketability, Explainability (internal).
- **Actionable:** Yes (follow existing diagnostics patterns—no PII in events).
- **Impact:** Marketability (+2–4), Proof-of-ROI (+3–5). Weighted: **+0.20–0.35%**.

**Cursor prompt**
```
Goal: Add durable, tenant-scoped analytics events for Core Pilot milestones (view onboarding, start run, commit success, first finding viewed, sponsor export attempted/blocked) reusing existing metering/diagnostics patterns if present.

Files: ArchLucid.Api (controller + audit or metrics sink), archlucid-ui call sites, tests.

Work:
1. Search for existing telemetry primitives (e.g., sponsor banner diagnostics endpoints) and extend consistently.
2. Document payload retention in docs/library/ short markdown + PII warning.
3. Add unit tests for event emission guards (no prompt text).

Acceptance criteria:
- Events fire only authenticated; rate limits respected.
- No sensitive architecture content logged.

Constraints: No new third-party SaaS analytics vendors.

Out of scope: Cross-tenant aggregation dashboards.
```

### 8. Live SQL E2E “release badge” documentation clarity
- **Why it matters:** Reduces Testability/reliability confusion between mock Playwright and SQL-backed gates.
- **Expected impact:** Fewer false confidence incidents; better enterprise trust in test story.
- **Affected qualities:** Testability, Trustworthiness, Reliability.
- **Actionable:** Yes.
- **Impact:** Testability (+4–6), Trust (+2–3). Weighted: **+0.15–0.25%**.

**Cursor prompt**
```
Goal: Add concise badge-friendly subsection to docs/library/RELEASE_SMOKE.md and archlucid-ui testing doc stating the single authoritative CI job names for live SQL browser E2E vs mock Playwright; link from README.

Files: docs/library/RELEASE_SMOKE.md, archlucid-ui/docs/TESTING_AND_TROUBLESHOOTING.md, README.md (short pointer only).

Acceptance criteria:
- Reader can answer “which badge proves live SQL UI path?” in one screen.
- No contradictions with V1_SCOPE statement on mock smoke.

Constraints: Do not change CI YAML unless a link is stale—prefer docs fix.

Out of scope: Rewriting tests.
```

---

## 10. Pending Questions for Later

_Organized by improvement title; blocking or decision-shaping only._

- **README auth reconciliation:** Should production pilots ever use ApiKey, or is Entra-only the mandated enterprise default? (Impacts README examples and security story.)
- **Onboarding canonical route:** Which URL is legally/marketing-canonical if `archlucid.net/get-started` differs from operator `(operator)/onboarding`?
- **Pilot preset gating signal:** What is the single authoritative “finalized review package” boolean in persisted state vs UX heuristics?
- **Proof-package gate:** Should hard-block apply to internal-only exports vs sponsor-facing only?
- **Core Pilot telemetry:** What retention window satisfies GDPR marketing-of-product analytics policy for existing tenants?
- **Procurement fast lane:** Legal approval on wording “Not available until V2” for pen-test/SOC2 rows?

---

**Scoring trace (internal):** Σ(score×weight)=**7802**, Σ(weight)=**102**, readiness=**7802/102=76.490196…%** → **76.49%**.
