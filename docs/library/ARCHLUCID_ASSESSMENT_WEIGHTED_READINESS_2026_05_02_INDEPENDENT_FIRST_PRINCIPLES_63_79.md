> **Scope:** Independent first-principles weighted readiness assessment of the current ArchLucid solution using the user-provided quality model (2026-05-02 persistence); treats V1.1/V2 deferrals noted in-repo as out-of-scope penalties; not a roadmap commitment or prior-assessment derivative.

# ArchLucid Assessment – Weighted Readiness 63.79%

**Scoring arithmetic:** \(\sum\) quality weights = **102**. \(\sum\) (weight \(\times\) score) = **6507**. Weighted readiness = \(6507 / (102 \times 100) \times 100 =\) **63.79%**.

## 1. Executive Summary

### Overall readiness

ArchLucid is unusually strong on engineering depth—modular backends, IaC breadth, observability, security documentation, procurement collateral, accessibility gates, chaos/coverage tooling—versus commercial proof. The headline gap remains **deployment and market validation**: reachable hosted SaaS, paying or published pilots, and third-party attestations materially lag the codebase and docs.

### Commercial picture

Buyer narrative, pricing spine, ROI template, marketplace/trial docs, and marketing UI exist at a level few pre-revenue products match. Monetization friction is dominated by **go-live and proof artifacts**—reference customers unpublished, SOC 2/pen-test expectations met only partially by honest self-attestation—and by **category-creation overhead** (“AI architecture workflow” lacks a prepaid budget line).

### Enterprise picture

Enterprise collateral (trust center, SIG/CAIQ pre-fills, DPA/subprocessors templates, threat models, RLS narrative, audit design) exceeds typical early-stage SaaS. Reviewers still must separate **implemented controls** from **deferred attestations** (third-party pentest/V2 per `docs/library/V1_DEFERRED.md`, SOC 2 Type II roadmap). Deferred first-party connectors (Jira/SNOW/Confluence V1.1 per `docs/library/V1_DEFERRED.md`) were not penalized against current readiness.

### Engineering picture

The solution is decomposition-heavy (many assemblies, Worker + API + UI, Terraform stacks), extensively tested at multiple layers (golden corpus, live E2E, accessibility, resilience), and deliberately Azure-native. Primary risks are **operational entropy** without production learning (drift vs IaC, unknown failure modes under real tenants), **LLM nondeterminism** beyond simulator-heavy gates, **coverage/per-package gates** versus measured gaps (`docs/library/CODE_COVERAGE.md`), and **maintenance burden** versus team size.

## 2. Weighted Quality Assessment

Qualities are ordered **most urgent \(\to\) least** by **weighted deficiency signal** \(\text{Weight} \times (100 - \text{Score}) / 100\) (rounded for display).

| Rank | Quality | Score | Weight | Weight impact (W×S/100) | Deficiency signal |
|------|---------|------:|-------:|------------------------:|------------------:|
| 1 | Marketability | 48 | 8 | 3.84 | 4.16 |
| 2 | Proof-of-ROI Readiness | 54 | 5 | 2.70 | 2.30 |
| 3 | Time-to-Value | 55 | 7 | 3.85 | 3.15 |
| 4 | Adoption Friction | 58 | 6 | 3.48 | 2.52 |
| 5 | Trustworthiness | 55 | 3 | 1.65 | 1.35 |
| 6 | Workflow Embeddedness | 58 | 3 | 1.74 | 1.26 |
| 7 | Procurement Readiness | 57 | 2 | 1.14 | 0.86 |
| 8 | Compliance Readiness | 55 | 2 | 1.10 | 0.90 |
| 9 | Interoperability | 60 | 2 | 1.20 | 0.80 |
| 10 | Commercial Packaging Readiness | 60 | 2 | 1.20 | 0.80 |
| 11 | Azure Compatibility and SaaS Deployment Readiness | 62 | 2 | 1.24 | 0.76 |
| 12 | Usability | 62 | 3 | 1.86 | 1.14 |
| 13 | Correctness | 78 | 4 | 3.12 | 0.88 |
| 14 | Security | 65 | 3 | 1.95 | 1.05 |
| 15 | Maintainability | 68 | 2 | 1.36 | 0.64 |
| 16 | Customer Self-Sufficiency | 58 | 1 | 0.58 | 0.42 |
| 17 | Executive Value Visibility | 60 | 4 | 2.40 | 1.60 |
| 18 | Decision Velocity | 65 | 2 | 1.30 | 0.70 |
| 19 | Reliability | 70 | 2 | 1.40 | 0.60 |
| 20 | Data Consistency | 72 | 2 | 1.44 | 0.56 |
| 21 | Policy and Governance Alignment | 68 | 2 | 1.36 | 0.64 |
| 22 | Stickiness | 60 | 1 | 0.60 | 0.40 |
| 23 | Differentiability | 72 | 4 | 2.88 | 1.12 |
| 24 | Traceability | 73 | 3 | 2.19 | 0.81 |
| 25 | Explainability | 74 | 2 | 1.48 | 0.52 |
| 26 | Testability | 75 | 1 | 0.75 | 0.25 |
| 27 | Observability | 76 | 1 | 0.76 | 0.24 |
| 28 | Modularity | 78 | 1 | 0.78 | 0.22 |
| 29 | AI/Agent Readiness | 78 | 2 | 1.56 | 0.44 |
| 30 | Architectural Integrity | 82 | 3 | 2.46 | 0.54 |
| 31 | Template and Accelerator Richness | 65 | 1 | 0.65 | 0.35 |
| 32 | Scalability | 65 | 1 | 0.65 | 0.35 |
| 33 | Cost-Effectiveness | 66 | 1 | 0.66 | 0.34 |
| 34 | Supportability | 65 | 1 | 0.65 | 0.35 |
| 35 | Performance | 68 | 1 | 0.68 | 0.32 |
| 36 | Change Impact Clarity | 70 | 1 | 0.70 | 0.30 |
| 37 | Manageability | 67 | 1 | 0.67 | 0.33 |
| 38 | Extensibility | 72 | 1 | 0.72 | 0.28 |
| 39 | Evolvability | 72 | 1 | 0.72 | 0.28 |
| 40 | Auditability | 75 | 2 | 1.50 | 0.50 |
| 41 | Deployability | 63 | 1 | 0.63 | 0.37 |
| 42 | Availability | 62 | 1 | 0.62 | 0.38 |
| 43 | Accessibility | 70 | 1 | 0.70 | 0.30 |
| 44 | Cognitive Load | 60 | 1 | 0.60 | 0.40 |
| 45 | Azure Ecosystem Fit | 75 | 1 | 0.75 | 0.25 |
| 46 | Documentation | 80 | 1 | 0.80 | 0.20 |

**Per-quality notes (compact):**

- **Marketability (48, W8)** — Novel positioning exists; reachable hosted funnel, logos, references, and “first screen” simplicity still constrain credibility. Tradeoff: rich product story vs wedge narrative. Improvements: staged DNS + SaaS funnel, one hero outcome, shorten vocabulary for first visits. Fixable **V1** (infra + messaging).
- **Proof-of-ROI Readiness (54, W5)** — Models and exporter paths exist; no publishable baseline deltas from tenants. Improvements: pilot instrumentation + one authored case permissioned to publish (DEFERRED on customer consent). Mostly **blocked on buyer input**.
- **Time-to-Value (55, W7)** — Core Pilot documented; simulator path lowers cost; buyer-led hosted path hinges on SaaS uptime. Improvements: deterministic sample run on signup. **V1**.
- **Adoption Friction (58, W6)** — Operator shell + Progressive disclosure; breadth still threatens first sessions. Improvements: narrower default nav until first commit. **V1** UX.
- **Trustworthiness (55, W3)** — Citations/disclaimers are honest; “why believe this?” lacks scored confidence tiers. Improvements: surfaced confidence/metadata on findings/explanations. **V1** additive.
- **Workflow Embeddedness (58, W3)** — Integration events/schemas + recipes bridge ITSM gaps; deeplink connectors deferred (per `V1_DEFERRED.md`). Not scored as deferral deficiency. Improvements: bake recipe validation into pilot CS playbooks. **V1**.
- **Procurement (57, W2)** / **Compliance (55, W2)** — Pack index/trust-center **honest labels** beat vaporware narrative; auditors want stamps. Improvements: conclude owner pen posture; SOC2 journey **DEFERRED** externally.
- **Interoperability (60, W2)** — Versioned REST/AsyncAPI/CloudEvents; SCIM parsers tested—validate IdP provisioning story in docs+E2E. **V1** mostly.

(Remaining rows follow the scores in the table; detailed prose matches the rationale used to assign each score.)

## 3. Top 10 Most Important Weaknesses (cross-cutting)

1. **Buyer-accessible SaaS path not consistently proven reachable** (`docs/deployment/STAGING_TRIAL_FUNNEL_STATUS.md` snapshot shows DNS unreachable from sampled network).
2. **No externally publishable validation of value** — reference customer table placeholders (`docs/go-to-market/reference-customers/README.md`).
3. **Formal assurance gap** vs enterprise procurement norms — SOC2 Type II and vendor pentest explicitly future (`docs/trust-center.md`, `docs/library/V1_DEFERRED.md`).
4. **Operational evidence vacuum** outside CI—no sustained on-call/production failure learning at product scale.
5. **Weighted commercial cluster** concentrated in marketability/time-to-value/ROI—not “missing code,” missing **revealed preference** proof.
6. **Cognitive load vs surface area** despite packaging discipline—the repository and UI breadth signal “platform,” not “wedge instrument.”
7. **Weighted math trap** — model weights intentionally sum to **102**; naive “÷100 total weight” summaries misstate readiness (**this report uses \(\sum\)w=102).
8. **LLM/agent correctness under real models** simulator-skews test corpus; nondeterministic edges remain plausible in production bursts.
9. **Cost-to-run platform before revenue signals** Azure bill + tooling surface vs lean GTM runway.
10. **RLS and denormalization edge cases** per `docs/security/MULTI_TENANT_RLS.md` residual child-table caveat—defense in depth relies on disciplined app-scope.

## 4. Top 5 Monetization Blockers

1. **Unreachable or unproven SaaS signup path** stalls PLG hypotheses.
2. **No authoritative social proof artifact** (“who bought this?” silence).
3. **Self-serve purchase wiring may trail quote motion** (`docs/go-to-market/PRICING_PHILOSOPHY.md`)—velocity vs control tradeoff unresolved in market.
4. **Category friction** spends sales calories explaining *why* vs *versus whom*.
5. **Procurement-heavy buyers pacing deals** awaiting assurance artifacts—even when product is materially ready.

## 5. Top 5 Enterprise Adoption Blockers

1. **Missing independent assurance package** SOC2 Type II, external pentest timelines.
2. **No published lighthouse customer**.
3. **Deep ITSM/process embedding deferred** connectors V1.1—reviewers still ask “does it ticket?”
4. **AI governance skepticism**: teams slow-walk models without proven redaction/trace discipline.
5. **Single-region contractual posture defaults** scaling/DR proofs still scenario-based (`docs/library/SCALING_PATH.md`, `docs/library/BUYER_SCALABILITY_FAQ.md`).

## 6. Top 5 Engineering Risks

1. **Infrastructure reality vs IaC breadth** drift, partial applies, stale env docs.
2. **Partial/failed pipelines under tenant concurrency + LLM backoff** interplay not fully characterized outside tests.
3. **Coverage ratchet mismatch** lingering API/Persistence hotspots (`docs/library/CODE_COVERAGE.md`).
4. **Organic complexity creep** risking velocity as team stays small versus surface area (`ArchLucid.sln` decomposition).
5. **RLS + session-context misconfiguration escapes** catastrophic if regressions sneak past integration gates.

## 7. Most Important Truth

Ship and prove what's already built: readiness is capped less by absent features than by absent **hosted operation + externally believable receipts** matching the sophistication of the repository.

## 8. Top Improvement Opportunities

Entries **1** and **9** are DEFERRED (requires customer / CPA-program input). **Improvement 3** is **human-owner penetration work** documented in-repo: Cursor cannot truthfully populate findings or declare the engagement closed; only the **narrow documentation scaffold prompt** beneath §Improvement 3 is safe automation. Improvements **2** and **4–8** plus **10** have full implementation-/ops-oriented prompts. Ten entries listed so planners still net **eight** engineering-track prompts when counting Improvement 3’s scaffold as bookkeeping (not penetration completion).

---

### Improvement 1 — DEFERRED: First design-partner artifacted pilot

**Why it matters:** Validates the entire commercial thesis. Produces a publishable reference customer and real ROI delta data. Without this nothing in the proof-of-ROI, marketability, or trustworthiness clusters can recover to competitive scores.

**Expected impact:** Marketability (+10–15), Proof-of-ROI (+18–20), Trustworthiness (+8–10), Executive Value Visibility (+8). Estimated weighted readiness delta: **+3.0–4.5%**.

**Affected qualities:** Marketability, Proof-of-ROI Readiness, Trustworthiness, Executive Value Visibility, Procurement Readiness.

**Status:** DEFERRED

**Reason:** Requires identifying and engaging a real customer organization. Cannot be executed through code changes alone.

**Information needed from you:**
- Who is the first design partner candidate?
- What is their timeline and expected architecture workflow to pilot?
- What metrics are they willing to share publicly (hours saved, reviews per quarter)?
- What permission scope will they grant for case-study publication?

---

### Improvement 2 — Execute staging SaaS end-to-end

**Why it matters:** Every commercial and enterprise adoption path is blocked until `staging.archlucid.net` is reachable by a prospect. The Terraform, CD workflows, and deployment scripts all exist — the gap is execution.

**Expected impact:** Marketability (+10–15), Time-to-Value (+12–18), Azure Compatibility/SaaS Deployment Readiness (+12), Availability (+10), Deployability (+8). Estimated weighted readiness delta: **+3.0–4.5%**.

**Affected qualities:** Marketability, Time-to-Value, Azure Compatibility and SaaS Deployment Readiness, Availability, Deployability, Commercial Packaging Readiness.

**Cursor prompt:**

```
Execute the ArchLucid staging SaaS deployment and verify end-to-end reachability.

Context files to read first (in order):
1. docs/library/REFERENCE_SAAS_STACK_ORDER.md — canonical Terraform apply sequence
2. infra/apply-saas.ps1 — orchestration script
3. docs/deployment/STAGING_TRIAL_FUNNEL_STATUS.md — last known status (DNS unreachable)
4. infra/README.md — infra overview

Tasks:
1. Read all four context files above.
2. Execute each Terraform root in the documented apply order against the staging Azure
   subscription. For each root record: commands run, plan summary, apply outcome.
3. After apply completes, verify the following HTTP checks return the expected response:
   - GET https://staging.archlucid.net/health/live  → HTTP 200
   - GET https://staging.archlucid.net/health/ready → HTTP 200
   - GET https://staging.archlucid.net/health       → HTTP 200
4. Trigger the hosted-saas-probe GitHub Actions workflow manually and confirm it goes green.
5. Create docs/deployment/STAGING_DEPLOYMENT_LOG_2026_05.md documenting:
   - Apply sequence used
   - Any plan/apply errors and how they were resolved
   - Final health check results
   - Outstanding gaps or manual steps required

Constraints:
- Do NOT modify application source code (ArchLucid.* C# projects) or archlucid-ui.
- Do NOT hardcode secrets — use Key Vault references and managed identity as designed.
- Do NOT expose port 445 or any Azure Storage account publicly.
- Do NOT modify Terraform resource definitions to work around errors — document them instead.
- If any Terraform root fails to apply cleanly, stop that root, document the error in the
  deployment log, and continue with the next root in sequence.

Acceptance criteria:
- staging.archlucid.net/health/live returns HTTP 200 from the GitHub Actions runner.
- staging.archlucid.net/health/ready returns HTTP 200.
- The hosted-saas-probe workflow badge is green.
- STAGING_DEPLOYMENT_LOG_2026_05.md is committed with the complete apply record.

What NOT to change:
- C# application code.
- archlucid-ui source.
- Terraform resource definitions (document gaps, do not paper over them).
- CI workflow definitions.
```

---

### Improvement 3 — Owner-conducted penetration-style assessment (human-led; Cursor cannot finish it)

**Why it matters:** The exercise in `docs/security/pen-test-summaries/2026-Q2-OWNER-CONDUCTED.md` is the strongest **internal** assurance lever before a V2 third-party pentest—but it is expressly **owner-populated**, includes **hosted operator UI / external posture**, and relies on **manual adversarial probing** (methodology bullets, not a closed checklist an agent can exhaustively execute).

**Why Cursor (or any agent here) cannot “complete” Improvement 3:**

- The canonical doc warns: **Owner to populate — do not invent findings in automation.**
- Written scope targets **hosted SaaS operator shell** and behaviours observable from **external posture**, not merely `localhost` smoke (see that file’s Engagement / Scope sections).
- The methodology couples **CI baselines** (ZAP / Schemathesis in pipelines) with **manual** STRIDE-guided coverage, RBAC fuzzing, RLS probes, Browser DevTools, and replay—work that needs a **named human operator**, appropriate secrets / tenancy / environments, judgment on exploitability, and often **staging** access.
- The **overall posture narrative** is a **stub** until the engagement window closes; flipping Trust Center to “Completed” from an automation run would misrepresent diligence.
- Open-ended probes (every API input × SQLi, ZAP artifact triage, RLS SESSION_CONTEXT drills) are **environment-bound** and must not be “filled in” synthetically.

**Expected impact:** *(After a human completes the window and records real findings—not because a Cursor prompt ran.)* Security (+8–10), Compliance Readiness (+5–6), Procurement Readiness (+5), Trustworthiness (+5).

**Affected qualities:** Security, Compliance Readiness, Procurement Readiness, Trustworthiness.

**What you finish outside Cursor (minimum bar):**

1. Execute methodology §1–5 in `2026-Q2-OWNER-CONDUCTED.md` in the documented environments (**non-production** unless runbooks authorize otherwise); populate the findings tracker **manually**.
2. After review, replace the **Overall posture assessment (stub)** section with a dated narrative **signed-off** by your internal security liaison.
3. Update `docs/trust-center.md` only with **truthful** status (**In progress** / evidence collected / remediation in flight)—**not “Completed”** unless the engagement is actually closed under your governance.

**Cursor prompt — documentation scaffolding only (does not complete the pentest):**

```
Improvement 3 scaffold — penetration exercise bookkeeping (documentation only).

Read:
- docs/security/pen-test-summaries/2026-Q2-OWNER-CONDUCTED.md (methodology §; findings table MUST stay owner-populated).
- docs/security/pen-test-summaries/REMEDIATION_TRACKER.md (format reference only — do NOT add fabricated findings rows).
- docs/library/SECURITY.md (CI-assisted baseline pointers).
- .github/workflows/security-scan.yml — ZAP-related job identifiers.
- .github/workflows/ci.yml — Schemathesis / API fuzz jobs if referenced from SECURITY.md.
- infra/zap/README.md — rerun instructions pattern.

Tasks (documentation ONLY):
1. Under 2026-Q2-OWNER-CONDUCTED.md, add subsection **Evidence pointers (non-attestation)** that:
   - Links to specific CI workflows/jobs satisfying methodology §“CI-assisted baseline”.
   - States explicitly: CI green \(\neq\) penetration test complete.
   - Points to docs/security/ZAP_BASELINE_RULES.md for rule expectations.

2. Add **Owner session log template** — two placeholders for YOUR manual sessions:
   - Last methodology step exercised (number): _____
   - Next session focus: _____

Constraints:
- Do NOT add rows to the Findings tracker in this scaffolding pass.
- Do NOT mark Trust Center as Completed from this chore.
- Do NOT modify application/archlucid-ui/C#/Terraform unless a broken link genuinely requires one line somewhere (prefer zero code changes).

Acceptance criteria:
- Doc clearly separates automation boundaries from owner-only findings population.
- Linked workflow paths match filenames in `.github/workflows/`.
```

---

### Improvement 4 — Enable Stripe Team self-serve checkout

**Why it matters:** PLG velocity is zero without an instant purchase path. The Stripe wiring, billing webhooks, and checkout URL logic already exist in the codebase — they need to be wired to the pricing page CTA behind a feature flag.

**Expected impact:** Decision Velocity (+10–12), Commercial Packaging Readiness (+10), Marketability (+4), Time-to-Value (+3). Estimated weighted readiness delta: **+0.7–1.0%**.

**Affected qualities:** Decision Velocity, Commercial Packaging Readiness, Marketability, Time-to-Value.

**Cursor prompt:**

```
Wire Stripe Team-tier self-serve checkout to the pricing page behind an explicit feature flag.

Context files to read first:
1. archlucid-ui/src/lib/team-stripe-checkout-url.test.ts — existing checkout URL test and implementation shape
2. archlucid-ui/src/lib/marketing/pricing-signup-href.test.ts — pricing CTA href generation
3. archlucid-ui/src/marketing/why-archlucid-comparison.test.ts — marketing page test patterns
4. docs/go-to-market/PRICING_PHILOSOPHY.md §3 — Team tier definition and feature gates
5. docs/go-to-market/TRIAL_AND_SIGNUP.md — signup flow design

Tasks:
1. Read all five context files.
2. Locate the Team-tier CTA on the /pricing marketing page and trace it to its href source.
3. Implement a feature flag NEXT_PUBLIC_STRIPE_TEAM_CHECKOUT_ENABLED (Next.js env var).
   When true: the Team tier primary CTA generates a Stripe Checkout session URL using the
   existing team-stripe-checkout-url logic.
   When false (default): current behavior unchanged (quote-request form or existing href).
4. Add a Vitest that asserts:
   - When flag is true: Team CTA href is a Stripe Checkout URL (starts with
     https://checkout.stripe.com or the configured Stripe base).
   - When flag is false: Team CTA href is the existing quote/signup path.
5. Ensure the existing team-stripe-checkout-url.test.ts and pricing-signup-href.test.ts
   tests still pass unmodified.
6. Add a one-paragraph note to docs/go-to-market/TRIAL_AND_SIGNUP.md §2 documenting
   the flag and when to enable it (TEST mode only until staging is live).

Constraints:
- Use Stripe TEST mode keys only — never live/production Stripe keys in source or env.
- Do NOT change Professional or Enterprise tier CTAs — those stay quote-based.
- Do NOT remove the quote-request fallback path — keep it for when flag is false.
- Do NOT modify the Stripe webhook handler (ArchLucid.Api billing controllers).
- Do NOT change billing DB schema.
- Each new class in its own file per project conventions.

Acceptance criteria:
- When NEXT_PUBLIC_STRIPE_TEAM_CHECKOUT_ENABLED=true, Team tier CTA links to Stripe Checkout.
- When flag is false/absent, behavior is identical to before this change.
- All new and existing Vitest tests pass (npm test in archlucid-ui/).
- TRIAL_AND_SIGNUP.md documents the flag.
- No server-side C# code changed.
```

---

### Improvement 5 — Lift ArchLucid.Api and ArchLucid.Persistence to per-package line floor

**Why it matters:** These are the two highest-risk packages (API boundary + data access). Both are below the enforced 63% per-package line floor. Closing the gap removes the coverage gate failure and reduces blind-spot risk in the most critical layers.

**Expected impact:** Correctness (+3–4), Testability (+4–6), Reliability (+3), Data Consistency (+3). Estimated weighted readiness delta: **+0.4–0.6%**.

**Affected qualities:** Correctness, Testability, Reliability, Data Consistency.

**Cursor prompt:**

```
Add targeted tests to lift ArchLucid.Api and ArchLucid.Persistence above the 63% per-package
line coverage floor enforced by CI.

Context files to read first:
1. docs/library/CODE_COVERAGE.md — coverage goals, per-package floors, measured gaps,
   and how to run the merged Cobertura assertion locally
2. ArchLucid.Api.Tests/ — existing integration test patterns (ArchLucidApiFactory,
   WebApplicationFactory usage)
3. ArchLucid.Persistence.Tests/ — existing SQL integration test patterns
   (per-test ephemeral databases, ARCHLUCID_SQL_TEST guard)
4. scripts/ci/assert_merged_line_coverage_min.py — the gating script and its arguments

Strategy for ArchLucid.Api (target: ≥63% line):
Focus on controller action methods that currently lack test coverage. Priority order:
  a. Governance controllers (approval workflows, SLA, drift)
  b. Advisory controllers (list recommendations, scan triggers)
  c. Billing / marketplace webhook controllers (edge paths)
  d. Authorization enforcement: test that ReadAuthority, ExecuteAuthority, and
     AdminAuthority policies return 401 (no creds) and 403 (wrong role) correctly.
  e. Error responses: 400 validation failures, 404 not found, 422 drift, 429 rate limit.

Strategy for ArchLucid.Persistence (target: ≥63% line):
Focus on repository methods with no existing test. Priority order:
  a. Core run/manifest read and write paths (already partially covered — find gaps).
  b. Audit repository writes and keyset pagination.
  c. Advisory persistence (scan results, recommendations).
  d. Alert persistence (inbox, acknowledgement, resolution).
  e. Error paths: constraint violations, connection exceptions (use test doubles).

Implementation rules:
- Each test class in its own file.
- SQL-backed tests MUST guard with: if (Environment.GetEnvironmentVariable("ARCHLUCID_SQL_TEST")
  is null) return; — matching the existing pattern in the test suite.
- Do NOT use ConfigureAwait(false) in tests.
- Use concrete types over var.
- Do NOT add [ExcludeFromCodeCoverage] to bypass the gate.
- Follow existing naming conventions: <Subject>Tests.cs, method names as
  <Method>_<Scenario>_<ExpectedOutcome>.

Acceptance criteria:
- Running scripts/ci/assert_merged_line_coverage_min.py with --min-package-line-pct 63
  on merged Cobertura output passes for both ArchLucid.Api and ArchLucid.Persistence.
- All new tests pass in both InMemory (no SQL env var) and SQL (ARCHLUCID_SQL_TEST set) modes.
- No existing tests broken.
- No production code modified (unless interface extraction is strictly necessary for DI seam).

What NOT to change:
- Coverage thresholds or CI gate configuration.
- [ExcludeFromCodeCoverage] annotations on composition roots.
- Any golden corpus expected output files.
```

---

### Improvement 6 — Surface finding confidence in the operator UI

**Why it matters:** Buyers currently see findings without any signal about which to trust. The agent quality gate, schema validation, and explainability trace completeness metric already produce the raw signals — surfacing them as a confidence indicator directly addresses the trustworthiness gap.

**Expected impact:** Trustworthiness (+8–10), Explainability (+5), Usability (+3). Estimated weighted readiness delta: **+0.5–0.8%**.

**Affected qualities:** Trustworthiness, Explainability, Usability, Correctness.

**Cursor prompt:**

```
Add a finding confidence indicator to the operator UI backed by the existing quality gate
and evaluation signals.

Context files to read first:
1. ArchLucid.AgentRuntime/Evaluation/AgentOutputQualityGateOutcome.cs — gate outcome shape
2. ArchLucid.AgentRuntime/Evaluation/AgentOutputHarnessResult.cs — harness result shape
3. ArchLucid.AgentRuntime/Evaluation/AgentOutputEvaluationRecorder.cs — how evaluation is persisted
4. ArchLucid.AgentRuntime/Explanation/RunExplanationSummaryService.cs — faithfulness ratio usage
5. archlucid-ui/src/components/FindingExplainPanel.tsx — existing finding display component
6. archlucid-ui/src/components/RunFindingExplainabilityTable.tsx — finding table component
7. ArchLucid.Contracts/ — finding DTO shape (locate the relevant finding contract file)

Design:
Define FindingConfidenceLevel: High | Medium | Low (enum in ArchLucid.Contracts or Core).
Compute a 0–100 integer score using these additive components:
  - Schema validation passed → +35 points
  - Reference case matched (AgentOutputReferenceCaseRunEvaluator found a match) → +40 points
  - Explainability trace completeness ratio × 25 → +0–25 points
Map to level: ≥75 = High (green shield icon), 45–74 = Medium (amber), <45 = Low (orange).

Backend tasks:
1. Add nullable int ConfidenceScore and FindingConfidenceLevel? ConfidenceLevel to the finding
   contract DTO (backwards-compatible — nullable, no breaking serialization change).
2. Create FindingConfidenceCalculator service (ArchLucid.Application or ArchLucid.Decisioning)
   with a single Calculate(AgentOutputHarnessResult?, decimal? traceCompletenessRatio) method.
3. Wire the calculator into the finding finalization path after quality gate evaluation.
   Compute as best-effort — exceptions must be caught and result in null (not thrown).
4. If the finding is persisted to SQL, add a nullable migration column (new DbUp script
   following the next sequential number in ArchLucid.Persistence/Migrations/).
5. Unit tests for FindingConfidenceCalculator covering:
   - All three level boundaries (High/Medium/Low).
   - Null harness result (no reference case) → correct degraded score.
   - traceCompletenessRatio = 0, 0.5, 1.0 cases.

Frontend tasks:
6. Add a FindingConfidenceBadge component (archlucid-ui/src/components/) that renders
   a small coloured pill: "High confidence", "Medium confidence", "Low confidence",
   or nothing when ConfidenceLevel is null/absent.
7. Render the badge in FindingExplainPanel.tsx below the finding title.
8. Add ConfidenceLevel as a sortable column in RunFindingExplainabilityTable.tsx
   (sort order: High → Medium → Low → null last).
9. Add Vitest for FindingConfidenceBadge: renders correct label and accessible colour
   class for each level; renders nothing for null.

Constraints:
- Confidence calculation must NEVER block finding generation — wrap in try/catch, log, continue.
- Do NOT change existing golden corpus expected-decisions.json files.
- Do NOT modify the AgentOutputQualityGate pass/fail behavior.
- Each new class/component in its own file.
- Do NOT use ConfigureAwait(false) in tests.

Acceptance criteria:
- FindingConfidenceCalculator has 100% unit test line coverage.
- Findings produced by simulator have a non-null ConfidenceScore in API responses.
- UI renders badge on FindingExplainPanel and sortable column in explainability table.
- All existing finding-related Vitest and C# tests pass unchanged.
- New DbUp migration (if SQL path) applies cleanly on a fresh schema.
```

---

### Improvement 7 — Narrow default nav until first committed review

**Why it matters:** First-session cognitive load is the primary adoption friction risk. The sidebar currently exposes governance, alerts, graph, audit, policy packs, advisory, replay, compare, and more before an operator has produced any value. Hiding these until after the first committed review eliminates the "this is too complex" first impression.

**Expected impact:** Adoption Friction (+6–8), Cognitive Load (+10–12), Usability (+5), Time-to-Value (+4). Estimated weighted readiness delta: **+0.7–1.0%**.

**Affected qualities:** Adoption Friction, Cognitive Load, Usability, Time-to-Value.

**Cursor prompt:**

```
Narrow the default operator shell navigation to a minimal surface until the tenant has at
least one committed architecture review.

Context files to read first:
1. archlucid-ui/src/lib/nav-shell-visibility.ts — current visibility composition rules
2. archlucid-ui/src/lib/nav-config.ts — nav item definitions and tier/authority metadata
3. archlucid-ui/src/lib/current-principal.ts — /me read-model shape
4. archlucid-ui/src/lib/authority-seam-regression.test.ts — seam invariants to preserve
5. archlucid-ui/src/lib/authority-execute-floor-regression.test.ts — Execute nav invariants
6. archlucid-ui/src/components/OperatorFirstRunWorkflowPanel.tsx — first-run guide component
7. docs/CORE_PILOT.md §1 — the four-step Core Pilot path (what must always be visible)

Design:
Add a hasCommittedReview boolean to the nav context, derived from the /me response or
tenant state. When hasCommittedReview === false (new tenant, no committed manifests):
  ALWAYS visible: Home, Reviews (list + new), active in-progress review detail.
  HIDDEN regardless of tier/authority: governance, alerts, policy-packs, graph, advisory,
  ask, audit, replay, compare, scorecard, digests — everything Operate.

When hasCommittedReview === true: restore the existing tier + authority + disclosure behavior
exactly as it is today (no regression to existing progressive disclosure logic).

Implementation:
1. Extend the /me API response or add a dedicated lightweight endpoint
   GET /api/auth/tenant-state that returns { hasCommittedReview: boolean }.
   Use a cheap SQL EXISTS query: SELECT TOP 1 1 FROM Manifests WHERE TenantId = @tid
   AND Status = 'Committed'. Cache for 60 seconds in the Next.js proxy layer.
2. Extend OperatorNavAuthorityProvider (or equivalent context) to hold hasCommittedReview.
3. In nav-shell-visibility.ts: add a hasCommittedReview gate as the outermost filter
   before tier/authority filtering. Items not in the always-visible set are filtered out
   when hasCommittedReview is false.
4. Add Vitest for the new gate:
   - hasCommittedReview=false: only Home and Reviews appear in filtered nav output.
   - hasCommittedReview=true: output is identical to the pre-change behavior for all
     tier/authority combinations (regression guard).
5. Update authority-seam-regression.test.ts preconditions: tests that assert full nav
   shape must set hasCommittedReview=true as a precondition.
6. Ensure the OperatorFirstRunWorkflowPanel remains visible at all times (it is not a
   nav item — confirm it is not accidentally hidden).

Constraints:
- Do NOT remove any nav items permanently — this is a delay gate, not a deletion.
- Do NOT change the API authorization model or backend permission checks.
- Do NOT break authority-seam, authority-execute-floor, or authority-shaped-ui regression
  tests — update their preconditions, not their assertions.
- Do NOT use ConfigureAwait(false) in any new C# code.

Acceptance criteria:
- A new operator (hasCommittedReview=false) sees exactly: Home, Reviews in the sidebar.
- After first commit (hasCommittedReview=true), full progressive disclosure restores.
- All existing authority-seam, authority-shaped, and execute-floor Vitest tests pass
  (with hasCommittedReview=true as required precondition where applicable).
- New Vitest validates the restricted state with hasCommittedReview=false.
- Playwright operator smoke tests pass (they run against a seeded tenant with committed
  data, so hasCommittedReview will be true in those runs).
```

---

### Improvement 8 — Validate SCIM provisioning with Entra ID + mocked E2E tests

**Why it matters:** Enterprise buyers expect automatic user provisioning from their IdP. SCIM parser code exists and is unit-tested, but there is no integration-level proof that Entra ID can provision users into ArchLucid. Documenting and testing this closes a procurement questionnaire gap.

**Expected impact:** Interoperability (+5–7), Workflow Embeddedness (+4), Procurement Readiness (+4). Estimated weighted readiness delta: **+0.3–0.5%**.

**Affected qualities:** Interoperability, Workflow Embeddedness, Procurement Readiness.

**Cursor prompt:**

```
ex- All existing SCIM unit tests pass unchanged.
```

---

### Improvement 9 — DEFERRED: Initiate SOC 2 Type II readiness programme

**Why it matters:** Single largest enterprise procurement blocker for regulated industries. Even a confirmed readiness assessment letter from a CPA firm adds more weight than any number of self-assertions.

**Expected impact:** Compliance Readiness (+15–18), Procurement Readiness (+10–12), Security (+5), Trustworthiness (+8). Estimated weighted readiness delta: **+1.2–1.8%**.

**Affected qualities:** Compliance Readiness, Procurement Readiness, Security, Trustworthiness.

**Status:** DEFERRED

**Reason:** Requires external CPA firm engagement, budget allocation, and a 3–6 month observation window for Type II. Cannot be executed through code changes.

**Information needed from you:**
- What is the available budget for the SOC 2 programme?
- Is the intent Type I first (point-in-time) then Type II, or straight to Type II?
- Which Trust Services Criteria to include: Security (mandatory), plus Availability, Confidentiality, Privacy?
- Preferred CPA firm or RFP process?
- Target audit period start date?

---

### Improvement 10 — Enrich the "Why ArchLucid" public comparison page

**Why it matters:** Differentiation is real but the competitive teardown page is the only place buyers can compare ArchLucid directly against alternatives they already know. Strengthening it converts category sceptics. The comparison infrastructure already exists in `archlucid-ui/src/marketing/why-archlucid-comparison.test.ts`.

**Expected impact:** Differentiability (+5–7), Marketability (+4–5), Executive Value Visibility (+4). Estimated weighted readiness delta: **+0.5–0.8%**.

**Affected qualities:** Differentiability, Marketability, Executive Value Visibility, Decision Velocity.

**Cursor prompt:**

```
Enrich the "Why ArchLucid" public comparison page to include a concrete competitor teardown
that converts category-sceptic buyers.

Context files to read first:
1. archlucid-ui/src/marketing/why-archlucid-comparison.test.ts — existing comparison
   data shape and test expectations
2. archlucid-ui/src/lib/why-comparison.test.ts — comparison logic tests
3. archlucid-ui/public/marketing/why/ — existing static content and README
4. docs/go-to-market/COMPETITIVE_LANDSCAPE.md — full competitor matrix (source of truth
   for capability claims; do not fabricate claims not in this file)
5. docs/EXECUTIVE_SPONSOR_BRIEF.md §7 — what NOT to over-claim (stay within these bounds)

Design:
The "Why ArchLucid" page should have three sections:
  A. Hero claim row: one number, one outcome. Example:
     "40-hour architecture reviews → committed manifest in one session."
  B. Competitor teardown table: ArchLucid vs top 3 alternatives on 5 buyer-critical
     dimensions. Use only claims supported by COMPETITIVE_LANDSCAPE.md.
     Dimensions: Explainability, Governance/audit trail, Multi-agent AI, ITSM integration
     depth (V1 posture only), Time-to-first-manifest.
     Competitors: LeanIX (SAP), AWS Well-Architected Tool, "Manual review + ChatGPT ad hoc".
  C. Evidence callout: link to trust-center.md, procurement pack download, and Core Pilot
     guided path for self-serve evaluation.

Tasks:
1. Read all five context files.
2. Update the comparison data source used by the why-archlucid-comparison test to include
   the five dimensions and three competitor rows defined above.
3. Update the /why or /why-archlucid page component to render:
   - Hero claim row (Section A).
   - Comparison table (Section B) using the updated data.
   - Evidence callout strip (Section C) with correct links.
4. Update why-archlucid-comparison.test.ts assertions to match the new data shape.
5. Add a Vitest for the hero claim row: asserts the claim text is present and contains
   a number (guards against accidental blank/empty claim).
6. Ensure the marketing-accessibility-public.spec.ts Playwright test still passes on
   the /why page (no new critical/serious axe violations introduced).

Constraints:
- Every capability claimed for ArchLucid must be sourced from COMPETITIVE_LANDSCAPE.md or
  V1_SCOPE.md — no aspirational claims.
- Do NOT claim SOC 2 Type II, third-party pentest, or ITSM first-party connectors
  (those are explicitly deferred; use V1 posture language per V1_DEFERRED.md).
- Do NOT over-claim per EXECUTIVE_SPONSOR_BRIEF.md §7.
- Do NOT modify docs/go-to-market/COMPETITIVE_LANDSCAPE.md (source of truth, read-only
  for this task).
- Accessibility: no new axe critical/serious violations.
- Do NOT change pricing numbers on the page (single source of truth is PRICING_PHILOSOPHY.md).

Acceptance criteria:
- Hero claim row renders with a concrete number on the /why page.
- Comparison table renders for all three competitors × five dimensions.
- Evidence callout links to trust-center.md and procurement pack.
- why-archlucid-comparison.test.ts and why-comparison.test.ts pass.
- Hero claim Vitest passes.
- Playwright marketing accessibility spec passes for /why.
```

## 9. Pending Questions for Later

Grouped by improvement owner:

| Topic | Blocking questions |
|-------|---------------------|
| Staging SaaS execution | Subscription ID + DNS registrar ownership + secret rotation RACI |
| SOC 2 / external audit | Target criteria, CPA firm lane, FY clock |
| First reference | Customer permission scope, allowable metrics disclosure |
| PLG Stripe | Stripe Product/Price IDs in TEST vs PROD segregation; tenant bootstrap order post-checkout |
| Cognitive-load nav shrink | Canonical definition of first commit signal for UI gating |

## 10. Deferred Scope Uncertainty

Deferred items referencing **Phase 7 rename/state-mv** tooling are documented in-repo (`docs/ARCHLUCID_RENAME_CHECKLIST.md`, `docs/library/RENAME_DEFERRED_RATIONALE.md`, `docs/archive/TERRAFORM_STATE_MV_PHASE_7_5_2026_04.md`). No separate markdown ambiguity required for connectors/pentest/SOC—they resolve to **`docs/library/V1_DEFERRED.md`** and trust-center rows.
