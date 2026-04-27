> **Scope:** Independent weighted quality assessment of ArchLucid as it stands in this repository on 2026-04-21. Weighted overall score: **68.60% as originally scored on 2026-04-21**, **re-scored 2026-04-23 to 70.53%** after the owner deferred the *first named, public reference customer* milestone to V1.1 (see **§0.2**), then **re-scored again 2026-04-23 to 71.71%** after the owner deferred the *commerce un-hold (Stripe live + Marketplace go-live)* milestone to V1.1 (see **§0.3**). Companion Cursor prompts: [`CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_68_60.md`](CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_68_60.md).

> **Spine doc:** [Five-document onboarding spine](FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# ArchLucid Quality Assessment — 2026-04-21 (re-scored 2026-04-23 → 71.71%)

**Audience:** Product leadership, sponsoring exec, engineering leads, GTM owners.

**Method.** Each quality is scored 1–100 from a fresh inspection of the repository (source projects, Terraform stacks, docs, CI gates, runbooks, ADRs, templates, GTM material) on 2026-04-21. Weights come from the request. Items the owner has formally **deferred to V1.1 / V2** (per [`V1_DEFERRED.md`](library/V1_DEFERRED.md), [`V1_SCOPE.md`](library/V1_SCOPE.md) §3, and the **Resolved** table in [`PENDING_QUESTIONS.md`](PENDING_QUESTIONS.md)) are **excluded** from the readiness score — they are not held against ArchLucid here.

**Independence.** This file does **not** consult earlier `QUALITY_ASSESSMENT_*` outputs. Where my judgement happens to align with a previous one, that is convergent evidence, not citation.

**Ordering rule.** Sections appear **most-improvement-needed first**. "Improvement need" is `(100 − score) × weight` so a 38-point gap on a weight-8 quality outranks the same gap on a weight-1 quality.

**Weight arithmetic.** The supplied weights total **102** (Commercial 40 + Enterprise 25 + Engineering 37). The weighted percent is `Σ(score × weight) ÷ (102 × 100)`. Bucket sub-totals also use their bucket weight as the denominator so they read as 0–100 percentages.

---

## 0. Headline

| Bucket | Weight share | Original (2026-04-21) | Re-score #1 (2026-04-23 — reference customer) | Re-score #2 (2026-04-23 — commerce un-hold) |
|--------|--------------|------------------------|------------------------------------------------|----------------------------------------------|
| **Commercial** | 40 / 102 | 2,633 / 4,000 = 65.83% | 2,807 / 4,000 = **70.18%** | **2,927 / 4,000 = 73.18%** |
| **Enterprise** | 25 / 102 | 1,706 / 2,500 = 68.24% | 1,729 / 2,500 = **69.16%** | 1,729 / 2,500 = 69.16% (unchanged) |
| **Engineering** | 37 / 102 | 2,658 / 3,700 = 71.84% | 2,658 / 3,700 = 71.84% (unchanged) | 2,658 / 3,700 = 71.84% (unchanged) |
| **Total** | 102 / 102 | 6,997 / 10,200 = **68.60%** | 7,194 / 10,200 = **70.53%** | **7,314 / 10,200 = 71.71%** |

**Plain-English read (re-scored twice on 2026-04-23).** Engineering is the strongest column and continues to outpace the other two. Enterprise/governance posture is solid for V1 and reads stronger after re-score #1 because the *first named, public reference customer* — a multi-quality external-trust gap that previously depressed five qualities at once — is **owner-deferred to V1.1** (see §0.2). Commercial readiness then reads materially stronger after re-score #2 because the *commerce un-hold* (Stripe live keys flipped + Marketplace listing published) — which previously depressed three more qualities — is **also owner-deferred to V1.1** (see §0.3); under V1's contract, the commercial motion is **sales-led** (`/pricing` displays numbers, `ORDER_FORM_TEMPLATE.md` drives quote-to-cash, the trial funnel runs in **Stripe TEST mode on staging** as a sales-engineer-led product evaluation). The remaining live-V1 external-trust gap is now narrowly bounded to **no executed pen test summary** and **no PGP key on `security@archlucid.dev`** — both addressed by Improvement 6. The score moves materially the day Improvement 6 lands.

---

## 0.1 SaaS-framing addendum (added 2026-04-23)

> **SaaS truth.** ArchLucid is a **SaaS** product. **Customers, evaluators, and sponsors never install Docker, SQL, .NET, Node, Terraform, or any local tool.** The only surfaces a customer ever touches are the **public website** (`archlucid.net` — signup, marketing, demo preview), the **operator UI** (after sign-in), and the **Azure portal** (only for their own subscription identity / billing artefacts that Azure already exposes — they do not run Terraform or `apply-saas.ps1`).
>
> Local Docker / SQL / devcontainer / `archlucid try` / `dev up` are **internal contributor tooling**. They live for ArchLucid engineers; they are never asked of a customer.

This rewires three numeric items above. The other 27 quality scores stand.

| Item | Wording in §1 | Why the SaaS framing changes the read | Adjustment |
|------|---------------|----------------------------------------|------------|
| **§1.2 Adoption Friction (60/100)** | "Evaluator friction is **low** — `FIRST_30_MINUTES.md` is Docker-only, `archlucid try` is one command." | A SaaS evaluator never installs Docker. The real evaluator path is the **cloud trial funnel** at `archlucid.net/signup → /demo/preview`, which is **not yet live in production**. So evaluator friction is materially **higher** than scored — every prospect either signs a sales call or downloads a contributor toolchain. | Treat §1.2 as **closer to 50/100** under SaaS framing. **Improvement 2** (live trial funnel) is therefore the **single highest-leverage commercial item** — re-prioritise above Improvement 1 if the team has only one slot this quarter. |
| **§1.27 Azure Compatibility & SaaS Deployment Readiness (74/100)** | Recommendation: "Promote `apply-saas.ps1` into a documented '**buyer onboarding path**'…" | A SaaS buyer **does not run Terraform**. `apply-saas.ps1` is the path **ArchLucid itself** uses to deploy our own hosted production subscription. Calling it a "buyer onboarding path" miscasts internal operator tooling as a customer experience. | Replace the recommendation with: "Document `apply-saas.ps1` as the **internal ArchLucid operator path** for standing up new ArchLucid hosting environments (multi-region GA, isolated EU stack, gov-cloud variant). The **buyer onboarding path is the trial funnel** — see Improvement 2." Score unchanged; the artefact is real, only its intended audience is corrected. |
| **§1.30 Customer Self-Sufficiency (70/100)** | One-line read: "Operator quickstart, doctor, support-bundle, troubleshooting, auto-migrate, runbooks." | These are mostly **contributor** / on-prem-style affordances. A SaaS customer cannot SSH or run a CLI against the host. "Self-sufficiency" for them means: pause / change plan / invite users / rotate API key / see audit log / **download support bundle from the UI**. | Re-define the quality as **in-product self-service**. Today the operator UI does cover plan management, user invites, API-key rotation, and audit log viewing; it does **not** yet expose support-bundle download or trial-pause as in-product flows. Treat §1.30 as **closer to 60/100** under SaaS framing and add an explicit follow-on: surface `archlucid support-bundle` as an authenticated UI download. |

**What does not change.**

- All eight Improvements in §3 stay; their **internal mechanics** are correct because they are already cloud-funnel-shaped (Improvement 2 = trial funnel, Improvement 3 = cloud baseline-capture, Improvement 4 = governance UI, etc.). The SaaS framing only **raises the priority** of Improvement 2.
- Engineering qualities (§1.18–§1.30 for the Engineering bucket) are unaffected — Docker / SQL / Terraform are correct **inside the build and deploy pipelines**; the SaaS framing only forbids those tools from appearing on the **buyer's** path.
- Internal operator runbooks (e.g., `REFERENCE_SAAS_STACK_ORDER.md`, `apply-saas.ps1`, `engineering/INSTALL_ORDER.md`, `engineering/FIRST_30_MINUTES.md`, `engineering/BUILD.md`, `engineering/CONTAINERIZATION.md`, `engineering/DEVCONTAINER.md`, `engineering/DEPLOYMENT.md`) keep their full Docker / SQL / Terraform content; they were moved to **`docs/engineering/`** on 2026-04-23 (see CHANGELOG) and carry an **Audience banner** clarifying they are for ArchLucid contributors and internal operators, not customers. Stub redirects remain at the old paths (`docs/INSTALL_ORDER.md`, `docs/FIRST_30_MINUTES.md`, `docs/library/BUILD.md`, etc.) so existing bookmarks survive.

**Buyer-facing first-30-minutes doc.** Today there is **no** customer-facing equivalent of `engineering/FIRST_30_MINUTES.md` (i.e., a "30 minutes from `archlucid.net` landing page → signed in → first sample run → first finding" walkthrough that names zero local tools). Drafting the **copy** is owner-controlled (marketing / brand voice). The **wiring** is part of Improvement 2 (`docs/runbooks/TRIAL_FUNNEL_END_TO_END.md` already documents the developer-facing smoke; the customer-facing variant is a stop-and-ask). Logged as PENDING_QUESTIONS item 36 and **resolved 2026-04-23** to **both** repo doc + marketing route, **vertical-picker-first** preset.

---

## 0.2 Reference-customer-deferral re-score addendum (added 2026-04-23)

> **Owner deferral.** On 2026-04-23 the owner explicitly deferred the *first named, public reference customer* milestone — at least one row in [`docs/go-to-market/reference-customers/README.md`](go-to-market/reference-customers/README.md) at `Status: Published`, with a published case study and customer-permissioned logo on the marketing site — to **V1.1**. See [`docs/library/V1_DEFERRED.md`](library/V1_DEFERRED.md) §6b and [`docs/PENDING_QUESTIONS.md`](PENDING_QUESTIONS.md) **Resolved 2026-04-23 (Reference-customer publication scope)**.
>
> Per the assessment's own rule (§ "Items the owner has formally **deferred to V1.1 / V2** … are **excluded** from the readiness score — they are not held against ArchLucid here"), the qualities that were depressed by this missing milestone must be **re-scored on V1's real contract**, not on a V1.1 obligation.

This rewires **five** numeric items in §1. The other 25 quality scores stand. The CI guard `scripts/ci/check_reference_customer_status.py` continues in `continue-on-error: true` warn-mode for the entire V1 window — that warn-mode is now correctly framed as the V1 contract, not a "should flip soon" signal.

| §1 item | Original score (2026-04-21) | Re-scored (2026-04-23) | Delta × Weight = Numerator delta | Why the deferral changes the read |
|---------|------------------------------|--------------------------|------------------------------------|-------------------------------------|
| **§1.1 Marketability** (weight 8) | 62 / 100 | **75 / 100** | +13 × 8 = **+104** | The full V1 marketing kit is in-repo (sponsor brief, three-layer packaging, vertical briefs, screenshot gallery, trust center, public marketing routes, citation seam test) and is **honest about the absence of customer logos** — which now matches the V1.1 deferral, not a V1 deficit. The kit is V1-complete on its own terms. |
| **§1.4 Proof-of-ROI Readiness** (weight 5) | 65 / 100 | **75 / 100** | +10 × 5 = **+50** | The V1 contract is **the model + the plumbing + the templates** — `PILOT_ROI_MODEL.md`, `ROI_MODEL.md`, `PilotRunDeltaComputer`, value-report DOCX renderer, evidence-pack template, soft-required `baselineReviewCycleHours` capture (Improvement 3), aggregate ROI bulletin **template** with min-N privacy guards. Customer-supplied baselines are a V1.1 emergent property; V1 ships the substrate. |
| **§1.5 Differentiability** (weight 4) | 65 / 100 | **70 / 100** | +5 × 4 = **+20** | The V1 differentiation surface (`/why` page, `COMPETITIVE_LANDSCAPE.md`, dual-pipeline navigator, `/demo/preview`, `/demo/explain`, citation-protected comparison rows, downloadable side-by-side PDF in Improvement 5) does **not** require a public reference logo to function. The V1.1 reference customer adds external proof; the V1 differentiation surface stands on the artefacts the product produces. |
| **§1.6 Trustworthiness** (weight 3) | 58 / 100 | **63 / 100** | +5 × 3 = **+15** | The "missing third-party signal" list shrinks from {SOC 2 attestation, executed pen test, named reference logo} to {executed pen test} for V1 — SOC 2 was already deferred to ~$1M ARR (item 6 resolution) and reference-logo is now V1.1. The remaining V1 trust gap is narrower and more focused; Improvement 6 covers it. |
| **§1.16 Procurement Readiness** (weight 2) | 62 / 100 | **66 / 100** | +4 × 2 = **+8** | The V1 procurement pack is real (DPA, subprocessors, SLA summary, security.txt, CAIQ Lite, SIG Core, owner security assessment, pen test SoW, downloadable procurement-pack ZIP). The "no signed reference logos" bullet was the third of three; the other two (executed pen test, SOC 2) are already correctly scoped — pen test is a V1 obligation (Improvement 6), SOC 2 is owner-resolved to ~$1M ARR. |

**Numerator change.** +104 + 50 + 20 + 15 + 8 = **+197 points** on the weighted numerator (out of 10,200).

**New weighted total.** 6,997 + 197 = **7,194 / 10,200 = 70.53%**.

**Bucket arithmetic.**

- **Commercial** (weight 40): adds Marketability +104, ROI +50, Differentiability +20 = +174. New numerator 2,633 + 174 = **2,807 / 4,000 = 70.18%**.
- **Enterprise** (weight 25): adds Trustworthiness +15, Procurement +8 = +23. New numerator 1,706 + 23 = **1,729 / 2,500 = 69.16%**.
- **Engineering** (weight 37): unchanged. **2,658 / 3,700 = 71.84%**.
- **Verify total:** 2,807 + 1,729 + 2,658 = 7,194 ✓

**Knock-on edits in this same addendum.**

- **§2.1 Top weaknesses** — the "No published reference customer" entry is **removed from the V1 weakness list** (it is a V1.1 commitment, not a V1 weakness); a previously runner-up entry promotes to maintain a list of 10. See updated §2.1.
- **§2.2 Top monetization blockers** — same treatment: the reference-customer entry is removed from the **V1 monetization blocker list**; a runner-up promotes to maintain a list of 5. See updated §2.2.
- **§2.5 Most Important Truth** — the "three owner-controlled events that move the score" is updated to **two** (Marketplace listing live; pen test executed and redacted summary published). The reference-customer event is moved to a separate V1.1 sentence.
- **§3 Improvement 1** is converted to **DEFERRED — V1.1**. No Cursor prompt is generated for it (per the operating rule for DEFERRED items). A new **Improvement 9 — Quarterly board-pack PDF endpoint + monthly digest preset** is added so the actionable improvement count remains 8.

**What does *not* change.**

- The 25 quality scores not listed in the table above stand at their original values.
- All Improvements 2–8 stay (their internal mechanics are unaffected by this deferral).
- Engineering bucket and the Engineering scores all stand.
- This addendum re-scores **only** the 2026-04-21 assessment; archived assessments under `docs/archive/quality/` are correct *for their dates* and are **not** retroactively re-scored.

---

## 0.3 Commerce-un-hold-deferral re-score addendum (added 2026-04-23, after §0.2)

> **Owner deferral.** On 2026-04-23 — same day as the §0.2 reference-customer deferral, fourth scope decision of the day — the owner explicitly deferred the *commerce un-hold* milestone — Stripe **live** API keys flipped on, the Azure Marketplace SaaS offer transitioned to `Published` in Partner Center, and DNS cutover for `signup.archlucid.net` to the production Front Door custom domain — to **V1.1**. See [`docs/library/V1_DEFERRED.md`](library/V1_DEFERRED.md) §6b (commerce-un-hold row), [`docs/library/V1_SCOPE.md`](library/V1_SCOPE.md) §3 (new "Out of scope for V1" row), and [`docs/PENDING_QUESTIONS.md`](PENDING_QUESTIONS.md) **Resolved 2026-04-23 (Commerce un-hold scope)**.
>
> Per the assessment's own rule, the qualities that were depressed by the absence of live commerce must be **re-scored on V1's real contract** — sales-led adoption with `/pricing` displaying numbers, `ORDER_FORM_TEMPLATE.md` driving quote-to-cash, and the trial funnel running in **Stripe TEST mode on staging** as a sales-engineer-led product evaluation.

This rewires **three** numeric items in §1, on top of the five rewired in §0.2. The other 22 quality scores stand. The trial funnel TEST-mode end-to-end work (Improvement 2) **stays a live V1 obligation** — only the "flip TEST → live" final gate is V1.1-deferred.

| §1 item | Score after §0.2 | Re-scored after §0.3 (2026-04-23) | Delta × Weight = Numerator delta | Why the deferral changes the read |
|---------|-------------------|-------------------------------------|------------------------------------|-------------------------------------|
| **§1.2 Adoption Friction** (weight 6) | 60 / 100 | **70 / 100** | +10 × 6 = **+60** | Under V1's sales-led contract, "real paid-adoption friction is high because Stripe is in TEST mode and the Marketplace listing isn't live" stops being a V1 deficit — it becomes a V1.1 commitment. Contributor / internal-engineer friction is low; sales-engineer-led product evaluation through the trial funnel TEST-mode (Improvement 2 — **still V1**) is the V1 path. The buyer evaluator path is no longer rate-limited by a missing live-checkout — it's rate-limited only by Improvement 2 landing on staging. |
| **§1.12 Decision Velocity** (weight 2) | 55 / 100 | **70 / 100** | +15 × 2 = **+30** | The original 55 was rate-limited by "every prospect still needs a human conversation to get a contract." That **is** the V1 design under sales-led adoption — `/pricing` displays numbers, `ORDER_FORM_TEMPLATE.md` is ready, the order-form workflow is documented. The "no self-serve checkout" weakness was double-charging V1 for what is explicitly a V1.1 commitment. |
| **§1.15 Commercial Packaging Readiness** (weight 2) | 60 / 100 | **75 / 100** | +15 × 2 = **+30** | The V1 packaging surface is materially complete: three named tiers, single source of truth on prices (`PRICING_PHILOSOPHY.md`), `ORDER_FORM_TEMPLATE.md`, DPA, SLA summary, subprocessors, Stripe abstraction (ADR 0016), Marketplace alignment doc, packaging layer enforcement plan, `[RequiresCommercialTenantTier]` filter returning **402 Payment Required** with `ProblemTypes.PackagingTierInsufficient`, and the `BillingProductionSafetyRules` startup gate that makes the V1.1 un-hold safe. "Listing not live, Stripe not in prod" is no longer the V1 contract; it is the V1.1 commitment. |

**Numerator change.** +60 + 30 + 30 = **+120 points** on the weighted numerator (out of 10,200), on top of §0.2's +197.

**New weighted total.** 7,194 + 120 = **7,314 / 10,200 = 71.71%**.

**Bucket arithmetic.**

- **Commercial** (weight 40): adds Adoption Friction +60, Decision Velocity +30, Commercial Packaging +30 = +120. New numerator 2,807 + 120 = **2,927 / 4,000 = 73.18%**.
- **Enterprise** (weight 25): unchanged from §0.2. **1,729 / 2,500 = 69.16%**.
- **Engineering** (weight 37): unchanged. **2,658 / 3,700 = 71.84%**.
- **Verify total:** 2,927 + 1,729 + 2,658 = 7,314 ✓

**Knock-on edits in this same addendum.**

- **§2.1 Top weaknesses** — the "Marketplace listing not live" entry is **removed from the V1 weakness list** (it is a V1.1 commitment); the "Trial signup funnel not live in production" entry is **rescoped to "not live on staging in TEST mode"** (the V1 obligation, narrower than the original). One runner-up promotes to maintain a list of 10. See updated §2.1.
- **§2.2 Top monetization blockers** — three entries are removed ("Marketplace listing not published", "Stripe live keys not flipped", "No public price page transition from displayed to transactable" — all are the same V1.1-deferred milestone). Three runner-ups promote to maintain a list of 5. See updated §2.2.
- **§2.5 Most Important Truth** — the "two owner-controlled events that move the score" is updated to **one** (executed pen test + redacted-summary publication). The commerce un-hold event is moved into the V1.1 sentence alongside the reference customer.
- **§3 Improvement 2** stays actionable but its owner-gate note is updated: "Switching from Stripe TEST to live keys" is now explicitly **V1.1-deferred**, not just owner-only.
- **§3 Improvement 4** is converted to **DEFERRED — V1.1**. No Cursor prompt is generated for it. A new **Improvement 10 — Governance dry-run / what-if mode** is added to keep the actionable improvement count at 8 (Improvements 1 and 4 are now both DEFERRED; Improvements 9 and 10 are added; net actionable count = 2, 3, 5, 6, 7, 8, 9, 10 = **8** ✓).

**What does *not* change.**

- The 22 quality scores not listed in either §0.2's table or §0.3's table stand at their original values.
- All Improvements 2, 3, 5, 6, 7, 8 stay (their internal mechanics are unaffected by this deferral).
- Engineering bucket and the Engineering scores all stand.
- The trial funnel TEST-mode end-to-end work (Improvement 2) **stays a live V1 obligation** — runbook, CLI smoke (`archlucid trial smoke`), Playwright spec, and the on-page first-value loop all stay V1.
- The `BillingProductionSafetyRules` startup gate **stays shipped in V1**. Its purpose is to make the V1.1 un-hold safe.
- Archived assessments under `docs/archive/quality/` are correct *for their dates* and are **not** retroactively re-scored.

---

## 1. Quality scores — ordered by improvement impact

> Throughout, "the repo" means the source tree at `c:\ArchiForge\ArchiForge` on 2026-04-21.

For each quality I report the score, the weight, the **gap × weight** improvement-impact, justification grounded in repo evidence, the trade-off accepted by the current design, and a concrete recommendation. The eight largest improvements are also distilled into Cursor prompts in **§3** and the companion file.

---

### 1.1 Marketability — Score **62 / 100** (re-scored 2026-04-23 → **75 / 100**) · Weight **8** · Impact **304** → **200**

> **Re-score note (2026-04-23).** The original justification below stays accurate as a 2026-04-21 read of "the marketing site cannot yet quote a real logo or measured customer ROI delta." The owner has since deferred that obligation to **V1.1** (see §0.2). On V1's real contract — narrative kit + sponsor brief + three-layer packaging + vertical briefs + screenshot gallery + trust center + public marketing routes + citation-protected comparison rows + an honest non-claim of customer logos — Marketability scores **75 / 100**.

**Justification.** The full kit of a sellable narrative is now in-repo: executive sponsor brief, three-layer product packaging (Core Pilot / Operate (analysis workloads) / Operate (governance and trust)), competitive landscape doc, vertical briefs for five industries, screenshot gallery, trust center, public marketing routes for `/why`, `/pricing`, `/signup`, `/demo/preview`, and `/welcome` (`archlucid-ui/src/app/(marketing)/`), and a citation seam test that fails if competitive comparison rows lose their proof footnote. What is **still missing** is **external proof on the page**: every row in [`docs/go-to-market/reference-customers/README.md`](go-to-market/reference-customers/README.md) is `Placeholder` or `Customer review`; **no row is `Published`**, so the merge-blocking CI guard is still in advisory mode and the −15% reference discount in `PRICING_PHILOSOPHY.md` § 5.4 is still notional. The marketing site cannot yet quote a real logo or measured customer ROI delta.

**Trade-off.** The team explicitly chose narrative honesty over inflation (sponsor brief refuses transformation claims, vertical briefs refuse uncited statistics). That protects long-term trust but caps short-term magnetism.

**Recommendation.** See **Improvement 1** in §3 — graduate the **First paying tenant (PLG)** row in `reference-customers/README.md` from `Placeholder` to `Customer review` (then later `Published`, owner-only). The single state change flips the CI guard merge-blocking and is the moment marketability moves measurably.

---

### 1.2 Adoption Friction — Score **60 / 100** (re-scored 2026-04-23 → **70 / 100**) · Weight **6** · Impact **240** → **180**

> **Re-score note (2026-04-23 §0.3).** Under V1's sales-led contract — confirmed by the commerce-un-hold deferral — "production funnel needs DNS cutover, Front Door custom domain, Stripe live keys, and Marketplace certification" stops being a V1 deficit and becomes a V1.1 commitment. The buyer evaluator path is no longer rate-limited by missing live-checkout — it is rate-limited only by Improvement 2 (trial funnel TEST-mode on staging) landing, **which stays a live V1 obligation**. Re-scored to **70 / 100**.

**Justification (contributor friction only — see §0.1 SaaS-framing addendum).** Contributor / internal-engineer friction is **low**: [`docs/engineering/FIRST_30_MINUTES.md`](engineering/FIRST_30_MINUTES.md) is Docker-only, `archlucid try` is a one-command first-value loop, the `.devcontainer/` boots in the same posture. Note: this score is **for contributors**, not for SaaS buyers — under the SaaS framing in §0.1 the buyer-facing equivalent is the trial funnel, which is **not yet live**, so buyer adoption friction is materially higher (effectively closer to **50/100** until Improvement 2 ships). Real **paid-adoption** friction remains high: the trial signup page exists at `archlucid-ui/src/app/(marketing)/signup/page.tsx` and `POST /v1/register` is wired, but the production funnel still needs DNS cutover, Front Door custom domain, Stripe live keys, and Marketplace certification — none of which are live (per the **Still open** list in [`PENDING_QUESTIONS.md`](PENDING_QUESTIONS.md), items 8, 9, and 22). Self-hosting is **out of scope** by owner decision (Resolved 2026-04-21 in `PENDING_QUESTIONS.md`), which is correct for a SaaS product but means a customer who insists on BYO-cluster is turned away by design.

**Trade-off.** No customer-shipped containers means a clean SaaS contract; some prospects will still ask.

**Recommendation.** See **Improvement 2** in §3 — wire the trial funnel end-to-end against Stripe **TEST** mode on `staging.archlucid.net` so a prospect can complete signup → first sample run without a sales call. Owner action then flips Stripe live mode behind a feature flag.

---

### 1.3 Time-to-Value — Score **75 / 100** · Weight **7** · Impact **175**

**Justification.** "Clone to committed manifest" is genuinely fast: `archlucid try` plus the simulator-agent demo emits a sponsor-shareable Markdown first-value report within ~10 minutes locally, and the operator-shell post-commit banner can email a sponsor PDF straight from the run page. The repo even shows a "Day N since first commit" badge on the sponsor banner sourced from `dbo.Tenants.TrialFirstManifestCommittedUtc`. The **measurable** ROI value (review-cycle hours saved) is computed by `ValueReportReviewCycleSectionFormatter` and surfaced in the value-report DOCX. What is missing is **field-validated** time-to-value: the model is accurate to the implementation, but no real customer's hours-saved curve has been published yet.

**Trade-off.** The repo invests heavily in *the artifact that proves value* (manifest + delta + provenance) over flashy first-touch UI. That is the right long-term investment but means the first-90-seconds wow factor depends on the sample preset.

**Recommendation.** Pre-seed **one vertical-aligned sample run per industry brief** during trial signup so the user sees industry-relevant findings within 90 seconds; emit a `time-to-first-committed-manifest` metric on the tenant row; quote it in the sponsor banner. Five vertical briefs already exist (`templates/briefs/{financial-services,healthcare,retail,saas,public-sector}/`) so the wiring is small.

---

### 1.4 Proof-of-ROI Readiness — Score **65 / 100** (re-scored 2026-04-23 → **75 / 100**) · Weight **5** · Impact **175** → **125**

> **Re-score note (2026-04-23).** "Zero customer-supplied baselines populated" is correctly **a V1.1 emergent property**, not a V1 deficit, after the reference-customer deferral (§0.2). On V1's real contract — the model + the plumbing + the templates + the soft-required `baselineReviewCycleHours` capture (Improvement 3) — Proof-of-ROI Readiness scores **75 / 100**.

**Justification.** The plumbing is here: [`PILOT_ROI_MODEL.md`](library/PILOT_ROI_MODEL.md) defines the six measurement axes; [`go-to-market/ROI_MODEL.md`](go-to-market/ROI_MODEL.md) carries the dollar baseline (~$294K savings for a 6-architect team) with three-year TCO sensitivity; the value-report DOCX renderer is shipped; `EVIDENCE_PACK.md` and `REFERENCE_EVIDENCE_PACK_TEMPLATE.md` give a single-page measured-delta format; `PilotRunDeltaComputer` (`ArchLucid.Application/Pilots/PilotRunDeltaComputer.cs`) computes per-run deltas the builders consume. The gap is empirical: **zero customer-supplied baselines** are populated; every quoted number is from the model.

**Trade-off.** Conservative model defaults avoid over-claim but make every buyer's headline number look identical.

**Recommendation.** Make `baselineReviewCycleHours` **soft-required** at trial signup (skippable but defaulted to "I don't know — use model"); surface a **before/after panel** on the operator dashboard once a tenant has one committed run. Publish a **sanitized aggregate ROI bulletin** quarterly. See **Improvement 3** in §3.

---

### 1.5 Differentiability — Score **65 / 100** (re-scored 2026-04-23 → **70 / 100**) · Weight **4** · Impact **140** → **120**

> **Re-score note (2026-04-23).** The V1 differentiation surface (`/why` + `COMPETITIVE_LANDSCAPE.md` + dual-pipeline navigator + `/demo/preview` + `/demo/explain` + citation-protected comparison rows + Improvement 5's downloadable side-by-side PDF) does not require a public reference logo to function. Reference-customer-derived external proof is now a V1.1 commitment (§0.2). Differentiability re-scored to **70 / 100** on V1's real contract.

**Justification.** [`COMPETITIVE_LANDSCAPE.md`](go-to-market/COMPETITIVE_LANDSCAPE.md) makes a defensible claim: ArchLucid is the only candidate that combines AI agent orchestration with enterprise governance, auditability, and provenance for **design-time** architecture. The repo backs the claim: decision traces, golden manifests, replay, comparison drift, dual-pipeline navigator, an anonymous `/demo/preview` cached commit page (ADR 0027), `/demo/explain` provenance + citations route, and a public `/why` marketing page (`archlucid-ui/src/app/(marketing)/why/page.tsx`) with a citation-protected comparison table. What is **still missing** is an external-facing **side-by-side artifact pack** that a buyer can read in two minutes — "this is the package ArchLucid hands an architecture review board; here is what LeanIX or Ardoq would have handed them for the same input."

**Trade-off.** The team has wisely refused competitor takedowns from the seat of the pants — but that leaves the differentiation buried in product behaviour rather than visible in shareable PDF form.

**Recommendation.** Extend `/why` with a one-click **downloadable comparison artifact** (PDF) that bundles the ArchLucid run package side-by-side with a public-data scaffold of what an incumbent would produce for the same input. Already partially shipped — see **Improvement 5** in §3 for the small extension.

---

### 1.6 Trustworthiness — Score **58 / 100** (re-scored 2026-04-23 → **63 / 100**) · Weight **3** · Impact **126** → **111**

> **Re-score note (2026-04-23).** The "missing third-party signal" list shrinks from {SOC 2 attestation, executed pen test, named reference logo} to {executed pen test} for V1 — SOC 2 was already deferred to ~$1M ARR (item 6) and named reference logo is now V1.1 (§0.2). The V1 trust gap is narrower and Improvement 6 is the focused fix. Trustworthiness re-scored to **63 / 100**.

**Justification.** The repo is honest: SOC 2 deferred (interim self-assessment + roadmap), owner-led security self-assessment (`docs/security/OWNER_SECURITY_ASSESSMENT_2026_Q2-DRAFT.md`), pen test SoW awarded but not yet executed, no published redacted summary, no PGP key on `security@archlucid.dev` (only `security.txt` exists at `archlucid-ui/public/.well-known/security.txt`). Procurement-grade documents (CAIQ Lite, SIG Core, DPA template, subprocessors list) are pre-filled. Engineering-side trust signals are strong: RLS with `SESSION_CONTEXT`, append-only `dbo.AuditEvents`, fail-closed API key default, ZAP + Schemathesis + CodeQL in CI, prompt redaction. The remaining gap is the **independent third-party signal** — a SOC 2 attestation, an executed pen test, and at least one named reference logo.

**Trade-off.** Refusing to brand the self-assessment as a pen test is the right ethical choice but loses the marketing line buyers want.

**Recommendation.** Execute the awarded pen test (Aeronova SoW) and publish the redacted summary; generate the PGP key for `security@archlucid.dev`; either commit a SOC 2 Type I observation-period start date or replace the SOC 2 references with an explicit "interim self-assessment, attestation roadmap on request" treatment. See **Improvement 6** in §3.

---

### 1.7 Workflow Embeddedness — Score **62 / 100** · Weight **3** · Impact **114**

**Justification.** GitHub Action and Azure DevOps task for manifest delta are shipped, with sticky PR-comment companion actions for both (`integrations/github-action-manifest-delta-pr-comment/`, `integrations/azure-devops-task-manifest-delta-pr-comment/`). Five Logic Apps Standard workflow templates are in `infra/terraform-logicapps/workflows/`. Service Bus + AsyncAPI + webhooks documented and signed (HMAC). **ServiceNow and Confluence are explicitly out of scope** (Resolved 2026-04-21) — that is a defensible product call but it concentrates embeddedness in the Microsoft ecosystem only. **No Microsoft Teams connector exists** today (verified — no Teams artifact under `integrations/` or `infra/terraform-logicapps/workflows/`).

**Trade-off.** Microsoft-first focus reduces surface area but also reduces total addressable market.

**Recommendation.** Ship a **Microsoft Teams notification connector** (item 11/23 in `PENDING_QUESTIONS.md`) — sits naturally on top of the existing webhook pipeline and Logic Apps Standard pattern; the single highest-traffic Microsoft surface ArchLucid does not yet land in. See **Improvement 7** in §3.

---

### 1.8 Correctness — Score **72 / 100** · Weight **4** · Impact **112**

**Justification.** Coverage gates are real: `.github/workflows/ci.yml` enforces **79% line / 63% branch** on the merged report and **63% line on product packages**; Schemathesis hits the OpenAPI surface; 21 test projects (every domain assembly has a paired `.Tests`); mutation testing on a ratchet across `Application`, `Application-Governance`, `Persistence`, `Persistence-Coordination`, `Coordinator`, `Decisioning`, `Decisioning-Merge`, `AgentRuntime`, and `Api` (`stryker-config.*.json`); contract-snapshot tests for the OpenAPI v1 surface. The **golden cohort** (`tests/golden-cohort/cohort.json`) defines 20 representative architecture requests but the `expectedCommittedManifestSha256` values are still **all zeros** — only the JSON contract is asserted today; real drift detection is not actually live (`.github/workflows/golden-cohort-nightly.yml` runs a contract test, not a manifest-SHA comparison). The real-LLM cohort run is gated on owner budget approval (item 15/25 in `PENDING_QUESTIONS.md`).

**Trade-off.** Heavy structural testing buys regression confidence but does not prove the AI agents make the right call on a novel input.

**Recommendation.** Lock baseline SHAs from a single approved simulator-mode run; flip the nightly workflow from "contract" to "manifest drift report"; publish `docs/quality/golden-cohort-drift-latest.md` overwritten on each run; the real-LLM extension still stops at owner budget. See **Improvement 8** in §3.

---

### 1.9 Architectural Integrity — Score **65 / 100** · Weight **3** · Impact **105**

**Justification.** Bounded contexts are documented (`docs/PROJECT_MAP.md`, `docs/bounded-context-map.md`, `docs/ARCHITECTURE_COMPONENTS.md`). NetArchTest enforces dependency rules. `DualPipelineRegistrationDisciplineTests` is a build-blocking guard against silent cross-wiring of the duplicate-named `IGoldenManifestRepository` and `IDecisionTraceRepository` interface families. ADRs 0001–0027 are numbered and current; ADR 0021 (coordinator strangler) is `Accepted`; ADR 0022 records Phase 3 deferral with mechanical exit-gate verification under `evidence/phase3/gate-verification.md`; ADR 0028 has not yet been drafted because the **completion date** is owner-only (item 24 in `PENDING_QUESTIONS.md`). Coordinator deprecation headers (RFC 9745 / RFC 8594 / RFC 8288) ship on every mutating coordinator route. The dual interface families remain the single largest cognitive-load tax — every new engineer has to learn the dual map before they can navigate the codebase.

**Trade-off.** Convergence is hard, partial convergence avoids breaking changes — but the cost shows up in cognitive load and integrity scores until the strangler completes.

**Recommendation.** Generate the migrate/keep/delete inventory; add a regression CI guard that fails the build when non-test references to the coordinator interface family go **up** vs a checked-in baseline; draft ADR 0028 once the owner names a completion date. **Item 24** in `PENDING_QUESTIONS.md` is the single owner-only blocker.

---

### 1.10 Executive Value Visibility — Score **75 / 100** · Weight **4** · Impact **100**

**Justification.** Sponsor banner on run-detail, sponsor PDF endpoint (`POST /v1/pilots/runs/{runId}/first-value-report.pdf`), value-report DOCX, sponsor one-pager PDF, executive sponsor brief, "Day N since first commit" badge, **weekly executive digest email** (`ExecDigestComposer` + `ExecDigestWeeklyHostedService`, `dbo.TenantExecDigestPreferences` migration **103**, recipient + IANA-tz preferences, `/v1/notifications/exec-digest/unsubscribe` token round-trip, `/settings/exec-digest` UI). The artefacts a sponsor needs are all here and reachable from the operator UI **and** arriving in their inbox without operator action. Remaining gap: no **board-pack PDF** template that consolidates a quarter's runs into a single deck.

**Recommendation.** Add a `POST /v1/pilots/board-pack.pdf` quarterly digest that wraps the existing exec-digest + value-report into a single deliverable.

---

### 1.11 Usability — Score **68 / 100** · Weight **3** · Impact **96**

**Justification.** Operator UI is organised around a clear three-tier model with progressive disclosure, role-aware shaping via `/api/auth/me`, keyboard shortcut provider, breadcrumbs, an onboarding wizard at `/onboard`, a help panel, and Vitest seam tests guarding the layering (`authority-seam-regression.test.ts`, `authority-execute-floor-regression.test.ts`, `authority-shaped-ui-regression.test.ts`). What I cannot evaluate from the repo: **task-completion rates with real users**. The total surface area is large (≥ 50 routes under `(operator)`), which raises the bar for a first-time user.

**Recommendation.** Add a `task-success` telemetry signal (e.g. "first run committed within session", emitted as `archlucid_first_session_completed_total`, already partially wired) and chart it in the operator dashboard so we see actual usability rather than inferring it. Run moderated usability sessions with the design partner pipeline (currently `Customer review`) before the next minor release.

---

### 1.12 Decision Velocity — Score **55 / 100** (re-scored 2026-04-23 → **70 / 100**) · Weight **2** · Impact **90** → **60**

> **Re-score note (2026-04-23 §0.3).** "Every prospect still needs a human conversation to get a contract" **is** the V1 design under sales-led adoption — confirmed by the commerce-un-hold deferral. `/pricing` displays numbers, `ORDER_FORM_TEMPLATE.md` is ready, the order-form workflow is documented; the "no self-serve checkout" weakness was double-charging V1 for what is explicitly a V1.1 commitment. Re-scored to **70 / 100**.

**Justification.** Public `/pricing` page now renders (`archlucid-ui/src/app/(marketing)/pricing/page.tsx`) so a prospect can see the Team / Professional / Enterprise table without a sales call. Marketplace listing is **not** live (item 8). Stripe is wired (`BillingStripeWebhookController`, `BillingMarketplaceWebhookController`, `BillingCheckoutController`) but production go-live policy decisions remain owner-only (item 9). Every prospect therefore still needs a human conversation to get a contract.

**Recommendation.** Ship the Marketplace listing; flip Stripe to live keys behind a feature flag. Until then, ensure `/pricing` includes a **quote-on-request** form that auto-emails the order-form template — at least it removes a calendar round-trip. See **Improvement 4** in §3 (combines marketplace + Stripe live readiness with safety guards the assistant can ship today).

---

### 1.13 Compliance Readiness — Score **55 / 100** · Weight **2** · Impact **90**

**Justification.** GDPR DPA template, subprocessors list, CAIQ Lite, SIG Core, RLS posture (`security/MULTI_TENANT_RLS.md`), audit catalog with 101 typed events, vertical policy packs for five industries — all present. **No certification yet** (SOC 2 deferred, no ISO 27001, no FedRAMP/StateRAMP). The five vertical policy packs (financial-services, healthcare, retail, saas, public-sector) are functional accelerators rather than compliance certifications.

**Recommendation.** Publish a clear "Where ArchLucid is in the compliance journey" page on the marketing site (interim self-assessment, attestation roadmap, what is and is not in scope) so buyers can see the picture instead of chasing artefacts. Item 17 in `PENDING_QUESTIONS.md` (US public-sector variant) is the next owner-only call.

---

### 1.14 Security — Score **72 / 100** · Weight **3** · Impact **84**

**Justification.** RLS with `SESSION_CONTEXT`, fail-closed API keys (shipped JSON has `Enabled=false`), JwtBearer + Entra, ZAP baseline (scheduled strict mode), Schemathesis, prompt redaction with production-warning post-configure, threat models (system + Ask/RAG), Key Vault, gitleaks pre-receive, never-expose-SMB rule enforced, security.txt, CodeQL, SBOM (`sbom-test.json`), Simmy chaos workflow. **No external pen test executed**, **no PGP key**, **SOC 2 not attested**. Engineering security is solid; external assurance is light.

**Recommendation.** Same as §1.6 — execute the awarded pen test, publish redacted summary, generate the PGP key. Verify `.github/dependabot.yml` is on for `Directory.Packages.props` (Central Package Management) and that vulnerability-gate auto-fix PRs are enabled.

---

### 1.15 Commercial Packaging Readiness — Score **60 / 100** (re-scored 2026-04-23 → **75 / 100**) · Weight **2** · Impact **80** → **50**

> **Re-score note (2026-04-23 §0.3).** "Listing not live, Stripe not in prod" is no longer the V1 contract; it is the V1.1 commitment. The V1 packaging surface is materially complete: three named tiers, single source of truth on prices, `ORDER_FORM_TEMPLATE.md`, DPA, SLA summary, subprocessors, Stripe abstraction, Marketplace alignment doc, packaging layer enforcement plan, `[RequiresCommercialTenantTier]` 402 filter, `BillingProductionSafetyRules` startup gate. Re-scored to **75 / 100**.

**Justification.** Three named tiers, single source of truth on prices (`PRICING_PHILOSOPHY.md`), `ORDER_FORM_TEMPLATE.md`, DPA, SLA summary, subprocessors, Stripe abstraction (ADR 0016), Marketplace alignment doc, packaging layer enforcement plan, `[RequiresCommercialTenantTier]` filter returning **402 Payment Required** with `ProblemTypes.PackagingTierInsufficient`. **Listing not live, Stripe not in prod.**

**Recommendation.** Same as §1.12 — ship the Marketplace listing and flip Stripe live keys behind a feature flag. See **Improvement 4**.

---

### 1.16 Procurement Readiness — Score **62 / 100** (re-scored 2026-04-23 → **66 / 100**) · Weight **2** · Impact **76** → **68**

> **Re-score note (2026-04-23).** "No signed reference logos" was the third of three procurement-pack gaps (the other two: executed pen test summary, SOC 2 attestation). Pen test stays a V1 obligation (Improvement 6); SOC 2 is owner-resolved to ~$1M ARR; reference logos are now V1.1 (§0.2). On V1's real contract, the procurement pack is materially complete. Re-scored to **66 / 100**.

**Justification.** DPA, subprocessors, SLA summary, security.txt, CAIQ Lite, SIG Core, OWNER security assessment draft, pen test SoW awarded — all present. Trust Center page exists. **No signed reference logos**, **no executed pen test summary**, **no SOC 2 attestation** to attach to a procurement packet.

**Recommendation.** Bundle the existing artifacts into a **single downloadable procurement pack** (ZIP) under `/security-trust/procurement-pack` so a procurement officer can grab everything at once instead of clicking through ten linked docs.

---

### 1.17 Traceability — Score **78 / 100** · Weight **3** · Impact **66**

**Justification.** Correlation IDs end-to-end (`X-Correlation-ID`), `V1_REQUIREMENTS_TEST_TRACEABILITY.md`, `AUDIT_COVERAGE_MATRIX.md` (101 audit constants), `scripts/ci/assert_v1_traceability.py`, dual-write durable audit on coordinator paths, manifest provenance graph, comparison/replay artefacts, run forensics. Strong.

**Recommendation.** Add a `GET /v1/runs/{runId}/traceability-bundle` that returns a single ZIP containing audit rows, decision traces, manifest, and comparison delta — useful for both internal forensics and customer audit hand-off.

---

### 1.18 Reliability — Score **70 / 100** · Weight **2** · Impact **60**

**Justification.** Health endpoints (`/health/live`, `/health/ready`, `/health`), retry/circuit breaker for LLM (`docs/LLM_RETRY_AND_CIRCUIT_BREAKER.md`), idempotency on outbox + email (e.g. `exec-digest:{tenant}:{iso-week}`), transactional outboxes, Simmy chaos workflow (`.github/workflows/simmy-chaos-scheduled.yml`), RTO/RPO targets documented, degraded-mode runbook, support bundle, k6 soak.

**Recommendation.** Promote the Simmy chaos schedule from an isolated workflow into a quarterly **game day** with a published outcome runbook stub.

---

### 1.19 Interoperability — Score **70 / 100** · Weight **2** · Impact **60**

**Justification.** REST API + OpenAPI snapshot, integration events using **CloudEvents** envelope, AsyncAPI 2.6 spec, signed webhooks (HMAC), GitHub Actions, Azure DevOps tasks, Logic Apps Standard templates, public `ArchLucid.Api.Client` library with NSwag generation. Microsoft-ecosystem-leaning by deliberate scope decision.

**Recommendation.** Document a **REST + webhook recipe** for one common non-Microsoft target (Slack or Jira) using only what already ships — proves the product is open even where the first-party connector isn't.

---

### 1.20 AI/Agent Readiness — Score **68 / 100** · Weight **2** · Impact **64**

**Justification.** AgentRuntime + Simulator, real LLM accounting (`LlmCompletionAccountingClient`), prompt redaction with metrics, agent execution traces, golden cohort scaffold, agent-eval-datasets-nightly workflow, AI search SKU guidance, schema validation service for agent results. Real-LLM golden cohort run gated on owner budget (item 15/25).

**Recommendation.** Pair with **Improvement 8** below — once baseline SHAs are locked the cohort becomes a real signal; the real-LLM extension stops at owner budget approval.

---

### 1.21 Auditability — Score **80 / 100** · Weight **2** · Impact **40**

**Justification.** 101 typed audit events in `AuditEventTypes`, append-only `dbo.AuditEvents`, dual-write durable audit on mutating coordinator paths (`CoordinatorRunCatalogDurableDualWrite`, `CoordinatorRunFailedDurableAudit`), CSV export from operator UI, audit search with keyset cursor, `AUDIT_COVERAGE_MATRIX.md` tracking known gaps (currently zero open), `assert_no_audit_events_nolock.py` CI guard, `audit-core-const-count` snapshot.

**Recommendation.** The known limitation that the keyset cursor uses `OccurredUtc` only (no `EventId` tie-break) is documented in `V1_DEFERRED.md` §4. Promote the EventId tie-break refinement into a numbered V1.1 backlog ticket so it doesn't get lost.

---

### 1.22 Policy and Governance Alignment — Score **78 / 100** · Weight **2** · Impact **44**

**Justification.** `GovernanceWorkflowService` with approval workflow, segregation of duties (self-approval blocked), SLA tracking, webhook escalation on breach, configurable severity thresholds, warning-only mode (phased rollout), `ApprovalSlaMonitor`, pre-commit governance gate, versioned policy packs with scope assignments, governance dashboard, vertical policy packs for five industries.

**Recommendation.** Add a **governance dry-run** mode that scores a candidate manifest against current policy assignments **without** writing the audit trail or blocking commit — useful for "what would happen if I tightened this threshold?" what-if analysis.

---

### 1.23 Cognitive Load — Score **58 / 100** · Weight **1** · Impact **42**

**Justification.** 200+ docs files, 50+ projects, dual coordinator/authority interface families, three-layer UI model, two persistence families. The repository **does** mitigate this with `FIRST_5_DOCS.md`, `ARCHITECTURE_ON_ONE_PAGE.md`, `OPERATOR_ATLAS.md`, `DUAL_PIPELINE_NAVIGATOR.md`, scope headers on every doc (CI-enforced), `CONCEPTS.md` vocabulary guard, `bounded-context-map.md`, but a new contributor still has to read several maps before being productive. ~~The `IMPROVEMENTS_COMPLETE.md` file at the repo root is also a stale orphan from an earlier change set — its "Run `dotnet restore`" instructions and `ArchLucid.DecisionEngine.csproj` references no longer match the current solution layout (verified — there is no `ArchLucid.DecisionEngine` project).~~ **Resolved 2026-04-23:** the stale `IMPROVEMENTS_COMPLETE.md` is no longer at the repo root — verified absent via `Test-Path` 2026-04-23 owner Q&A pass; question 34 closed because the file was already removed in a prior cleanup.

**Recommendation.** (1) ~~Remove or archive the stale `IMPROVEMENTS_COMPLETE.md` at repo root~~ — **already removed 2026-04-23 (verified absent)**. (2) Add an "I have 30 minutes — what do I read?" path to `FIRST_5_DOCS.md` that picks **three** docs maximum and links to one navigator each.

---

### 1.24 Data Consistency — Score **75 / 100** · Weight **2** · Impact **50**

**Justification.** `DATA_CONSISTENCY_MATRIX.md`, RLS, dual-write audit, transactional outbox, DbUp migrations with **rollback scripts** (`Rollback/R*.sql`), `SQL_SCRIPTS.md`, single-source DDL (`ArchLucid.Persistence/Scripts/ArchLucid.sql`), `assert_rollback_scripts_exist.py` CI guard, schema versions table.

**Recommendation.** Add a CI guard that fails the build when a new `00x_*.sql` migration ships **without** a paired `Rollback/R0xx_*.sql` — extends the existing rollback-presence assertion to cover net-new migrations specifically.

---

### 1.25 Maintainability — Score **75 / 100** · Weight **2** · Impact **50**

**Justification.** Modular projects, primary constructors, terse C# rules enforced (`.cursor/rules/CSharp-Terse-*.mdc`), docs index, DI discipline tests, `.editorconfig`, `Directory.Build.props`, `Directory.Packages.props` for central package management, NetArchTest dependency rules.

**Recommendation.** Add a CI guard that fails when a new project is added to the solution **without** a paired `*.Tests` project (covers the test-coverage discipline at the structural layer).

---

### 1.26 Explainability — Score **72 / 100** · Weight **2** · Impact **56**

**Justification.** `EXPLANATION_SCHEMA.md`, `FindingEvidenceChainService` + `/v1/architecture/run/{runId}/findings/{findingId}/evidence-chain`, `/v1/explain/runs/{runId}/aggregate` (executive aggregate explanation + citations), `/v1/provenance`, citations bound to LLM outputs, Ask/RAG threat model, demo `/demo/explain` route showing provenance + citations side by side.

**Recommendation.** Add a **per-finding "Explain this" panel** in the operator UI (item 8 in the previous prompt set) so the LLM completion + redacted prompt + supporting evidence show inline next to the finding. This is the single missing piece — the underlying data is already collected.

---

### 1.27 Azure Compatibility and SaaS Deployment Readiness — Score **74 / 100** · Weight **2** · Impact **52**

**Justification.** Terraform stacks for Container Apps, Front Door, Logic Apps, edge orchestration; CD pipelines (`cd.yml`, `cd-staging-on-merge.yml`, `cd-saas-greenfield.yml`); OIDC login (no client secrets); Key Vault wiring; `AZURE_SUBSCRIPTIONS.md` as single source of truth (production sub `aab65184-...` recorded); `FIRST_AZURE_DEPLOYMENT.md`; SaaS-profile appsettings; default region `centralus`; Marketplace + Stripe controllers wired; `apply-saas.ps1` orchestrator; `Demo:Enabled` feature gate prevents demo leak in production.

**Recommendation.** Promote `apply-saas.ps1` into a documented **"buyer onboarding path"** that takes a fresh subscription ID and produces a usable hosted ArchLucid in ≤ 60 minutes; today the stack composition is reference-grade but operator-focused.

---

### 1.28 Documentation — Score **82 / 100** · Weight **1** · Impact **18**

**Justification.** 200+ docs files, scope headers enforced by CI (`scripts/ci/check_doc_scope_header.py`), CHANGELOG newest-first, ADRs numbered and current, runbooks indexed, `CONCEPTS.md` vocabulary guard, link-check CI, archive directory for superseded docs. This is the strongest quality in the entire assessment.

**Recommendation.** None tactical — keep the discipline. Maintain the ratio of "navigator-style" map docs to detail docs as the corpus grows.

---

### 1.29 Testability — Score **80 / 100** · Weight **1** · Impact **20**

**Justification.** 21 test projects, simulator agents, `ArchLucid.TestSupport` doubles, contract snapshot tests, deterministic test mode, `coverage.runsettings`, multiple Stryker scopes, `archlucid-ui` Vitest + Playwright (mock + live).

**Recommendation.** None tactical.

---

### 1.30 Other engineering qualities (rapid roll-up)

| Quality | Score | Weight | Impact | One-line read |
|---|---|---|---|---|
| **Modularity** | 78 | 1 | 22 | Bounded contexts mapped; one class per file enforced; primary constructors used |
| **Supportability** | 78 | 1 | 22 | `doctor`, `support-bundle`, correlation IDs, runbooks, `/version` — strong |
| **Azure Ecosystem Fit** | 78 | 1 | 22 | Entra, Key Vault, Service Bus, Container Apps, Front Door, Logic Apps, ADO tasks |
| **Change Impact Clarity** | 78 | 1 | 22 | `BREAKING_CHANGES.md`, deprecation headers, `API_VERSIONING.md`, comparison/replay |
| **Deployability** | 75 | 1 | 25 | Terraform, Docker, compose, CD workflows, install order, release-smoke |
| **Observability** | 75 | 1 | 25 | OpenTelemetry, instrumentation catalog, metrics, traces, logs |
| **Manageability** | 72 | 1 | 28 | Operations docs, admin endpoints, governance config, CLI doctor |
| **Extensibility** | 72 | 1 | 28 | Plugin pattern, DI registration map, API versioning, finding-engine plugins |
| **Performance** | 70 | 1 | 30 | k6 smoke/soak/burst, query plans, index inventory, scaling path, capacity playbook |
| **Evolvability** | 70 | 1 | 30 | ADRs, deprecation headers, breaking changes log, API versioning, strangler |
| **Cost-Effectiveness** | 70 | 1 | 30 | Per-tenant cost model, capacity playbook, LLM quota, AI Search SKU guidance, centralus default |
| **Accessibility** | 70 | 1 | 30 | WCAG 2.2 AA target, axe Playwright, jest-axe Vitest, keyboard shortcuts; no formal VPAT yet |
| **Customer Self-Sufficiency** | 70 | 1 | 30 | Operator quickstart, doctor, support-bundle, troubleshooting, auto-migrate, runbooks |
| **Scalability** | 68 | 1 | 32 | `SCALING_PATH.md`, capacity playbook, per-tenant cost model, RLS for multi-tenant |
| **Availability** | 65 | 1 | 35 | Health, RTO/RPO, multi-region docs (not GA per V1 scope), Front Door, Service Bus |
| **Stickiness** | 65 | 1 | 35 | Manifest history, audit, governance, learning profile, exec digest, pre-commit gate |
| **Template/Accelerator Richness** | 72 | 1 | 28 | Five vertical starters with briefs + policy packs, trial wizard preset |

---

## 2. Top weaknesses, blockers, risks, and the most important truth

### 2.1 Top 10 most important weaknesses (ranked by impact × weight)

> **Re-ranked twice on 2026-04-23.** Two reference-customer-related entries were removed in re-rank #1 (§0.2). Re-rank #2 (§0.3) removes the "Marketplace listing not live" entry (now a V1.1 commitment) and rescopes the trial-funnel entry to its V1 obligation only (TEST-mode on staging). The list below is the **V1-only** weakness ranking after both deferrals. One runner-up promotes to maintain a list of 10.

1. **Trial signup funnel not live on staging in TEST mode.** *(Rescoped 2026-04-23 §0.3 — the V1 obligation is staging end-to-end in Stripe TEST mode; the production live-keys flip is V1.1-deferred per §0.3.)* Page exists, endpoint exists, Stripe TEST not yet wired through the staging hostname end-to-end. Addressed by **Improvement 2**.
2. **No third-party pen test summary published.** SoW awarded; redacted-summary skeleton waits on assessor delivery.
3. **No PGP key for `security@archlucid.dev`.** Trust Center references PGP; key file (`archlucid-ui/public/.well-known/pgp-key.txt`) is missing.
4. **Golden cohort SHAs are placeholders** — nightly workflow asserts contract only, not actual manifest drift. Real signal is one approved baseline-lock run away.
5. **Coordinator strangler not finished.** ADR 0021 Phase 3 deferred per ADR 0022 exit gates; dual interface families remain a teaching tax on every new engineer.
6. **No board-pack PDF / monthly executive digest preset.** Weekly digest exists; quarterly board-grade roll-up does not. Addressed by **Improvement 9** (added 2026-04-23 §0.2).
7. ~~**Stale `IMPROVEMENTS_COMPLETE.md` at repo root.** References a non-existent `ArchLucid.DecisionEngine` project — small but visible inconsistency.~~ **Resolved 2026-04-23 — file already absent at repo root** (verified). Removed from the top-10 list as a finding; replaces with the next runner-up if a fresh assessment is run.
8. **No regression CI for strangler progress.** Coordinator interface family count can silently grow back without a guard. *(Promoted from previous engineering-risks list.)*
9. **No traceability bundle endpoint.** `GET /v1/runs/{runId}/traceability-bundle` (audit + decision trace + manifest + comparison delta in one ZIP) not yet wired — useful for both forensics and customer audit hand-off. *(Promoted from §1.17 recommendation.)*
10. **No governance dry-run / what-if mode for policy threshold changes.** Operators tightening a severity threshold today must commit, observe, and roll back if the impact is wrong. *(Promoted from §1.22 recommendation. Now addressed by **Improvement 10**, added 2026-04-23 §0.3.)*

**Removed 2026-04-23 (now V1.1 commitments or V1-shipped, not V1 weaknesses):**

- ~~No published reference customer~~ — deferred to V1.1 (§0.2); CI guard correctly stays in warn-mode for V1.
- ~~No Microsoft Teams connector~~ — shipped in V1 (six production triggers, Logic Apps Standard fan-out).
- ~~Marketplace listing not live~~ — deferred to V1.1 (§0.3); wiring stays in V1 and the `BillingProductionSafetyRules` startup gate makes the V1.1 un-hold safe.

### 2.2 Top 5 monetization blockers

> **Re-ranked twice on 2026-04-23.** Re-rank #1 (§0.2) removed the reference-customer entry. Re-rank #2 (§0.3) removes three commerce-un-hold entries (Marketplace listing not published, Stripe live keys not flipped, no transactable price page) — **all are the same V1.1-deferred milestone under §0.3**. The list below is the **V1-only** blocker ranking under sales-led adoption. Three runner-ups promote to maintain a list of 5.

1. **No third-party pen test summary published** — every regulated buyer requires this (or a SOC 2 attestation, which is owner-resolved to the ~$1M ARR band per item 6, so pen test is the V1 path). Addressed by **Improvement 6**.
2. **Trial signup funnel not live on staging in TEST mode** — without a sales-engineer-led product evaluation path, sales calls turn into demo requests instead of guided trials. Addressed by **Improvement 2** (the V1 obligation; the live-keys flip is V1.1-deferred per §0.3).
3. **No quarterly board-pack PDF for sponsor-driven expansion conversations** — sponsors at existing customers cannot take a single artefact into a budget review; expansion velocity suffers. Addressed by **Improvement 9** (added 2026-04-23 §0.2).
4. **No PGP key for `security@archlucid.dev`** — security teams notice this on first procurement contact; corrodes initial trust signal. *(Promoted; addressed by Improvement 6.)*
5. **No aggregate ROI bulletin published** — V1 ships the template (`AGGREGATE_ROI_BULLETIN_TEMPLATE.md`) and the privacy guards (min N=5 per item 27 resolution), but the first publication waits on the V1.1 reference customer; the absence of any external ROI signal weakens prospect-facing collateral. *(Promoted from §1.4 recommendation; the publication itself is gated on the V1.1 reference customer per item 27.)*

**Removed 2026-04-23 (now V1.1 commitments, not V1 monetization blockers):**

- ~~No `Published` reference customer row~~ — deferred to V1.1 (§0.2); the `−15%` reference discount stays notional for V1 by design.
- ~~Marketplace listing not published~~ — deferred to V1.1 (§0.3); wiring stays V1, publication is V1.1.
- ~~Stripe live keys not flipped~~ — deferred to V1.1 (§0.3); TEST-mode staging stays V1.
- ~~No public price page transition from "displayed" to "transactable"~~ — same milestone as the live-keys flip; deferred to V1.1 (§0.3). V1's commercial motion is sales-led.

### 2.3 Top 5 enterprise adoption blockers

1. **No published pen test redacted summary** — security teams will not sign without it (or a SOC 2 equivalent).
2. **No PGP key for `security@archlucid.dev`** — security.txt advertises the channel but a vulnerability reporter cannot encrypt to it.
3. **Two persistence families still exist** (coordinator + authority); architects evaluating the codebase see two `IGoldenManifestRepository` interfaces and conclude "this is mid-refactor."
4. **No Microsoft Teams connector** — the Microsoft-shop default workflow surface; competitors have it.
5. **No formal VPAT for accessibility** — large public-sector buyers ask for it; `ACCESSIBILITY.md` self-attestation is not the same artefact.

### 2.4 Top 5 engineering risks

1. **LLM finding quality is not measured.** Golden cohort has placeholder SHAs; we cannot tell if a model swap, prompt change, or upstream regression silently changes findings until a pilot complains.
2. **Coordinator/authority dual interface families.** A future contributor wires the wrong `IGoldenManifestRepository` and a hard-to-detect drift between two sources of truth begins. The `DualPipelineRegistrationDisciplineTests` guard helps but only at registration time.
3. **No regression CI for the strangler progress** — the coordinator interface family count can silently grow back if no one watches.
4. **Real-LLM cost ceiling unowned.** The `ARCHLUCID_GOLDEN_COHORT_REAL_LLM` switch + the existing `LlmCompletionAccountingClient` exist but no monthly $/tenant ceiling is enforced in CI.
5. **Schema-version stamp dependency on bootstrap order.** Greenfield catalogs replay 001–050 then stamp `SchemaVersions` and continue at 051. A future operator who runs the master DDL out-of-band can leave the stamp inconsistent — the runbook covers this but no automated assertion does.

### 2.5 Most Important Truth

> **Updated twice on 2026-04-23.** The original 2026-04-21 truth named **three** owner-controlled events. §0.2's update collapsed it to **two**. §0.3's update collapses it to **one**. Updated truth below.

**ArchLucid has built almost every piece of evidence a V1 buyer needs to commit, and after the 2026-04-23 scope decisions only one external V1 signal remains unpublished.** The product, the trust posture, the engineering quality, the documentation, and the V1 commercial motion (sales-led with `/pricing` displaying numbers and `ORDER_FORM_TEMPLATE.md` driving quote-to-cash) are all materially complete on V1's contract. **One** owner-controlled event — the awarded pen test executes and the redacted summary publishes (item 20 / Improvement 6) — independently moves the weighted V1 score by **3–5 points** and is the only **owner-action-required** V1 lever remaining. Beyond that, the V1 score is rate-limited by **engineering work the assistant can land today**: Improvement 2 (trial funnel TEST-mode staging), Improvement 3 (ROI bulletin template + soft-required baseline), Improvement 5 (downloadable competitive PDF on `/why`), Improvement 6 (pen test scaffold + PGP recipe), Improvement 7 (Microsoft Teams connector), Improvement 8 (golden-cohort baseline lock), Improvement 9 (board-pack PDF endpoint), and Improvement 10 (governance dry-run). **The V1 score is no longer rate-limited by what we publish, attest, or transact externally — it is rate-limited by one owner action and a small set of in-band engineering improvements.**

**Separately, on the V1.1 horizon:** two milestones now define V1.1 readiness — **(1)** the first named, public reference customer (deferred 2026-04-23 §0.2) and **(2)** the commerce un-hold (Stripe live keys flipped + Marketplace listing published; deferred 2026-04-23 §0.3). The first triggers the `−15%` reference-discount re-rate in `PRICING_PHILOSOPHY.md` § 5.3, flips the reference-customer CI guard from warn-mode to merge-blocking, and gates the aggregate ROI bulletin (item 27). The second flips the V1 commercial motion from sales-led to self-serve transactable. **Both are V1.1 conversations, not V1 readiness conversations.**

---

## 3. The eight largest improvements

The eight biggest improvement-impact items are listed in priority order. Each one notes: **what I can do today**, **what is owner-only**, and a **DEFERRED** marker in the title when I cannot complete at least part of it. Companion paste-ready Cursor prompts live in [`CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_68_60.md`](CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_68_60.md). Where my recommendation matches an in-flight prompt from the previous (67.61%) cycle, I refine rather than restart.

---

### Improvement 1 — DEFERRED — V1.1 — Graduate the first reference customer (PLG row)

> **Status (Resolved 2026-04-23).** **Deferred to V1.1** by owner decision — see [`docs/library/V1_DEFERRED.md`](library/V1_DEFERRED.md) §6b and [`docs/PENDING_QUESTIONS.md`](PENDING_QUESTIONS.md) **Resolved 2026-04-23 (Reference-customer publication scope)**. **No Cursor prompt is generated for this item** (per the operating rule for DEFERRED improvements). The qualities this improvement would have moved are no longer charged against V1 readiness — they have been re-scored upward in §0.2 to reflect V1's real contract. The actionable improvement count is preserved at 8 by the addition of **Improvement 9** below.

**Why no Cursor prompt now.**

- The owner has named V1.1 as the release window for this milestone — there is no V1 work remaining for the assistant to do.
- The CI guard `scripts/ci/check_reference_customer_status.py` correctly stays in `continue-on-error: true` warn-mode for the entire V1 window. **Do not flip it merge-blocking before V1.1.**
- The publication-runbook scaffolding (placeholder audit, evidence-pack scaffold, state-transition CHANGELOG convention) was already in place from the prior cycle; no further V1 hardening is required.

**What needs the owner — *at V1.1 planning time*, not now.**

- Naming the customer (item 19 ownership is already resolved to "Owner solo" — that resolution stands).
- Setting `Status: Published` on the chosen row.
- Granting copy approval and signing the discount re-rate trigger from `PRICING_PHILOSOPHY.md` § 5.3.
- Pinning a calendar date inside the V1.1 window (currently unpinned).

---

### Improvement 2 — Live trial signup funnel end-to-end (Stripe TEST mode) — partial; owner-only for live keys

**Quality moved.** Adoption Friction (+12), Time-to-Value (+5), Decision Velocity (+8).

**What I can do today.** Trace the existing happy path end-to-end and document it as a runbook (`docs/runbooks/TRIAL_FUNNEL_END_TO_END.md`); add a Playwright spec running the funnel against the deterministic mocks; ship an `archlucid trial smoke` CLI command that runs the funnel in dev and prints PASS/FAIL per step; surface the `baselineReviewCycleHours` capture on the operator dashboard once one run has committed.

**What is owner-only — *and now V1.1-deferred per §0.3*.** Switching from Stripe TEST to live keys; turning off the trial signup feature flag in production; DNS cutover for `signup.archlucid.net`. **All three are explicitly V1.1-deferred** by the **Resolved 2026-04-23 (Commerce un-hold scope)** decision — they are no longer V1 owner-action items. The trial funnel TEST-mode end-to-end work in this improvement **stays a live V1 obligation** and is what the prompt below ships; only the final "flip TEST → live" gate is V1.1.

**Pending question.** Items 9, 22 in `PENDING_QUESTIONS.md` — both **still open** but now release-window-pinned to V1.1, not V1.

---

### Improvement 3 — Proof-of-ROI: aggregate ROI bulletin + soft-required baseline at signup

**Quality moved.** Proof-of-ROI Readiness (+12), Marketability (+5), Executive Value Visibility (+5).

**What I can do today.** Flip `baselineReviewCycleHours` from optional to **soft-required** at signup (skippable but defaulted to model — UI + API contract work the assistant owns); add a `BeforeAfterDeltaPanel` component to the operator dashboard, reading from `PilotRunDeltaComputer`; ship a quarterly aggregate ROI bulletin **template** under `docs/go-to-market/AGGREGATE_ROI_BULLETIN_TEMPLATE.md` with explicit minimum-N privacy guards; add a CLI command `archlucid roi-bulletin --quarter Q3-2026 --min-tenants 5` that emits a draft bulletin from production data when permissions allow (Admin authority).

**What is owner-only.** Approving the publication cadence; signing each issue; deciding privacy-notice update for the soft-required baseline.

**Pending question.** Items 27, 28 in `PENDING_QUESTIONS.md`.

---

### Improvement 4 — DEFERRED — V1.1 — Marketplace + Stripe live readiness (commerce un-hold)

> **Status (Resolved 2026-04-23 §0.3).** **Deferred to V1.1** by owner decision — see [`docs/library/V1_DEFERRED.md`](library/V1_DEFERRED.md) §6b (commerce-un-hold row), [`docs/library/V1_SCOPE.md`](library/V1_SCOPE.md) §3, and [`docs/PENDING_QUESTIONS.md`](PENDING_QUESTIONS.md) **Resolved 2026-04-23 (Commerce un-hold scope)**. **No Cursor prompt is generated for this item** (per the operating rule for DEFERRED improvements). The qualities this improvement would have moved are no longer charged against V1 readiness — they have been re-scored upward in §0.3 to reflect V1's sales-led contract. The actionable improvement count is preserved at 8 by the addition of **Improvement 10** below.

**What's already shipped in V1 (and stays in V1 — do not remove).**

- `BillingProductionSafetyRules` startup gate — fails `ASPNETCORE_ENVIRONMENT=Production` when (a) Stripe live key prefix `sk_live_` is configured without a webhook secret, or (b) Marketplace landing page URL is empty/localhost. **Its purpose is to make the V1.1 un-hold safe; it is V1 hardening, not V1 commerce.**
- `archlucid marketplace preflight` CLI — prints PASS/FAIL per Partner Center checklist (read-only; the V1 deliverable was the *checklist*, not the *publication*).
- `scripts/ci/assert_marketplace_pricing_alignment.py` — ensures `PRICING_PHILOSOPHY` tier numbers match `MARKETPLACE_PUBLICATION.md` SKU numbers (CI alignment, V1).
- Stripe TEST staging path documented end-to-end in `docs/go-to-market/STRIPE_CHECKOUT.md` (V1).

**Why no Cursor prompt now.**

- The owner has named V1.1 as the release window for this milestone — there is no further V1 work for the assistant to do here.
- All three V1.1 actions are owner-only: setting any live Stripe key, setting a Marketplace publisher ID + production webhook secret, pressing "Go live" in Partner Center, tax profile + payout account + seller verification.
- The trial funnel TEST-mode work is **not** here — it is in **Improvement 2** (which stays actionable for V1).

**What needs the owner — *at V1.1 planning time*, not now.**

- Tax profile, payout account, and seller verification in Partner Center (cannot be filed by the assistant).
- Setting any `sk_live_` Stripe key + production webhook secret rotation.
- Pressing "Go live" on the Marketplace SaaS offer.
- DNS cutover for `signup.archlucid.net` to the production Front Door custom domain.
- Pinning a calendar date inside the V1.1 window (currently unpinned per the **Resolved 2026-04-23 (Commerce un-hold scope)** decision).

---

### Improvement 5 — Differentiability: side-by-side downloadable artefact pack on `/why`

**Quality moved.** Differentiability (+10), Marketability (+5).

**What I can do today.** Extend `archlucid-ui/src/app/(marketing)/why/page.tsx` with a downloadable PDF that bundles (a) one full ArchLucid run package (manifest + decision trace + comparison delta + citations) sourced from the cached anonymous `/demo/preview` data; (b) a public-data scaffold of what an incumbent (LeanIX / Ardoq / MEGA HOPEX) would produce for the same input — every claim backed by a `COMPETITIVE_LANDSCAPE.md` citation; broaden the existing citation seam test to fail when any row in the comparison loses its citation footnote; add the page to the existing axe Playwright a11y gate.

**What is owner-only.** Approving any direct competitive claim that does not already appear in `COMPETITIVE_LANDSCAPE.md` with a public-source citation.

---

### Improvement 6 — Trustworthiness: pen test summary publication + PGP key — partial; owner-only for assessor delivery and key generation

**Quality moved.** Trustworthiness (+10), Security (+5), Procurement Readiness (+5).

**What I can do today.** Build a redacted-summary skeleton in `docs/security/pen-test-summaries/2026-Q2-REDACTED-SUMMARY.md` that matches `PEN_TEST_REDACTED_SUMMARY_TEMPLATE.md` exactly, with `TODO` markers for assessor narrative; wire the Trust Center page so the `SecurityAssessmentPublished` badge renders automatically when `POST /v1/admin/security-trust/publications` is called; ship an `archlucid security-trust publish` CLI command that calls the endpoint; add `scripts/ci/assert_pgp_key_present.py` (advisory: `continue-on-error: true` today) that fails if `archlucid-ui/public/.well-known/pgp-key.txt` is missing while Trust Center references PGP; remove the PGP TODO from `SECURITY.md` once the file is in place.

**What is owner-only.** Marking the redacted summary as published (requires assessor delivery — Aeronova engagement window not yet scheduled per item 20); generating the PGP key pair (must be done by the security custodian per item 21).

**Pending question.** Items 2, 10, 20, 21.

---

### Improvement 7 — Microsoft Teams notification connector — partial; owner-only for Teams app manifest

**Quality moved.** Workflow Embeddedness (+12), Stickiness (+3).

**What I can do today.** Add a Logic Apps Standard workflow template under `infra/terraform-logicapps/workflows/teams-notifications/` subscribing to Service Bus topics for `run.committed`, `governance.approval.requested`, `alert.raised`; render to a Teams adaptive card via Incoming Webhook; add per-tenant config surface at `archlucid-ui/src/app/(operator)/integrations/teams/page.tsx` and `POST /v1/integrations/teams/connections` storing the webhook URL via Key Vault references (no raw URLs in SQL); shape the page in Operate (governance and trust) tier (`ExecuteAuthority` for write, `ReadAuthority` for view); add to `nav-config.ts` and the Vitest seam tests; add Schemathesis contract test for the new endpoints; document under `docs/integrations/MICROSOFT_TEAMS_NOTIFICATIONS.md`.

**What is owner-only.** Choosing notification-only vs two-way (approve governance from Teams) — two-way needs a registered Teams app manifest in M365 admin (item 23).

**Pending question.** Items 11, 23.

---

### Improvement 8 — Golden-cohort drift report with locked baseline SHAs — partial; real-LLM gated

**Quality moved.** Correctness (+10), AI/Agent Readiness (+8).

**What I can do today.** Add a one-shot `archlucid golden-cohort lock-baseline` CLI that runs the 20 cohort items through the **simulator** path, captures committed-manifest canonical SHA-256s, and writes them back into `tests/golden-cohort/cohort.json`; extend `.github/workflows/golden-cohort-nightly.yml` from "contract test only" to "manifest drift report" — diff against the locked SHAs and the expected finding categories; publish `docs/quality/golden-cohort-drift-latest.md` overwriting per run with previous reports archived under `docs/quality/archive/<date>.md`; add a "Explain this finding" panel in the operator UI (data is already present via `FindingEvidenceChainService`).

**What is owner-only.** Provisioning the dedicated Azure OpenAI deployment used by the optional real-LLM run (item 15/25 — budget approval); publishing per-tenant feedback aggregates externally (privacy review).

**Pending question.** Items 15, 25.

---

### Improvement 9 — Quarterly board-pack PDF endpoint + monthly digest preset (added 2026-04-23 to replace deferred Improvement 1)

> **Why this improvement was added.** Improvement 1 (graduate the first reference customer) was deferred to V1.1 on 2026-04-23 (§0.2), removing one slot from the eight-actionable-improvement list. Improvement 9 fills that slot with a **fully actionable** item that does **not** depend on owner-only events: it stitches existing exec-digest + value-report machinery into the single deliverable a sponsor takes into a quarterly budget review. Sourced from §1.10's standing recommendation.

**Quality moved.** Executive Value Visibility (+8), Stickiness (+3), Marketability (+2).

**What I can do today.**

- Add a new endpoint `POST /v1/pilots/board-pack.pdf?quarter=Q3-2026` that produces a single PDF binding (a) the four most recent weekly exec-digest snapshots within the quarter, (b) the value-report DOCX rendering for the highest-impact committed manifest in the quarter (rendered to PDF in the same flow), (c) the per-tenant ROI delta panel (`PilotRunDeltaComputer` output) summarised across the quarter, and (d) a one-page sponsor cover narrative driven by `EXECUTIVE_SPONSOR_BRIEF.md` placeholders. Gate behind `ExecuteAuthority` — same authority floor as the existing exec-digest unsubscribe surface.
- Add a corresponding CLI command `archlucid board-pack --tenant <id> --quarter Q3-2026 --out board-pack-Q3-2026.pdf` so support can produce the artefact out-of-UI.
- Add a **monthly digest preset** option to `ExecDigestComposer` (`Cadence: Weekly | Monthly`, persisted on `dbo.TenantExecDigestPreferences`); migration **104**; respect the same IANA-tz preference; default stays `Weekly` for existing rows.
- Add Vitest coverage for a `BoardPackTrigger` UI affordance on `/runs` (visible to ExecuteAuthority); do not promote it to a primary nav slot — it lives next to the existing exec-digest preference link in `/settings/exec-digest`.
- Document under a new `docs/library/BOARD_PACK.md` plus a one-line pointer in `EXECUTIVE_SPONSOR_BRIEF.md` and `OPERATOR_ATLAS.md`.
- Add Schemathesis contract coverage for the new endpoint.

**What is owner-only.**

- Final approval on the cover-page brand voice (the placeholder narrative ships as `<<sponsor cover narrative — owner approval before external use>>`).
- Decision on whether the monthly preset is opt-in or opt-out by default for new tenants (assistant ships **opt-in** as the safe default).

**Pending question.** New item below in §4.

---

### Improvement 10 — Governance dry-run (what-if) mode for policy threshold changes (added 2026-04-23 to replace deferred Improvement 4)

> **Why this improvement was added.** Improvement 4 (Marketplace + Stripe live readiness) was deferred to V1.1 on 2026-04-23 (§0.3), removing one slot from the eight-actionable-improvement list. Improvement 10 fills that slot with a **fully actionable** enterprise-governance feature that does **not** depend on owner-only events. Sourced from §1.22's standing recommendation. Its absence is also weakness #10 in the updated §2.1.

**Quality moved.** Policy and Governance Alignment (+5), Usability (+3), Stickiness (+3).

**What I can do today.**

- Add a new endpoint `POST /v1/governance/dry-run` (gated `ExecuteAuthority`) that accepts a candidate manifest (or a pointer to an existing committed manifest) plus an optional set of *proposed* policy threshold overrides (e.g., "raise findings.severity.criticalThreshold from 70 to 80") and returns the policy-evaluation result **without** writing the audit trail, **without** blocking commit, and **without** mutating any persisted policy. Internally reuses `GovernanceWorkflowService` and the existing policy-pack evaluation pipeline behind a `DryRunPolicyOverrideContext` that the persistence layer rejects (RLS + new `[NotForCommit]` marker on the dry-run audit row source).
- Add a CLI command `archlucid governance dry-run --manifest <id> --overrides overrides.json --out report.md` that calls the endpoint and renders a one-page Markdown report (sections: current evaluation, proposed evaluation, delta of would-be-blocked findings, delta of would-be-warned findings, sponsor-summary one-liner).
- Add an operator UI affordance under `/governance/policy-packs/<packId>` — a "Dry-run threshold change" modal with a live preview of the affected committed manifests in the current tenant (limit to most recent 20 by default; show "load more" pagination); shape the page in Operate (governance and trust) tier (`ExecuteAuthority` for write, `ReadAuthority` for view); add to `nav-config.ts` and the existing Vitest seam tests.
- Add a new audit constant `AuditEventTypes.GovernanceDryRunRequested` (read-only event — captures *that* a dry-run was requested and by whom, never the proposed-override payload itself, to avoid leaking proposed policy intent into the audit log); bump the `audit-core-const-count` snapshot.
- Add Application unit tests for the `DryRunPolicyOverrideContext` (must reject any attempt to persist), governance workflow + dry-run path coverage, and the threshold-delta calculator.
- Add an Api integration test (Suite=Core, GreenfieldSqlApiFactory) calling `POST /v1/governance/dry-run` and asserting (a) `ExecuteAuthority` gate, (b) no rows inserted into `dbo.AuditEvents` for *the policy evaluation itself* (only the request marker), (c) no rows inserted into the policy-pack mutation table.
- Add Schemathesis contract coverage for the new endpoint.
- Add a Vitest spec for the `/governance/policy-packs/<packId>` modal that mocks the endpoint.
- Document under a new `docs/library/GOVERNANCE_DRY_RUN.md` (audience: operators with ExecuteAuthority, plus governance/audit reviewers who need to know the dry-run audit shape) plus a one-line pointer in `OPERATOR_ATLAS.md`, `library/PRODUCT_PACKAGING.md`, and the sponsor brief.
- Add a CHANGELOG entry under 2026-04-23 with the standard format.

**What is owner-only.**

- Decision on whether the dry-run audit marker (`GovernanceDryRunRequested`) should also capture the *count* of proposed-override entries (numeric metadata only — no payload) for forensic purposes. Assistant ships **count yes, payload no** as the safe default.
- Decision on the "load more" pagination cap on the policy-pack page (assistant ships 20-default, 100-max as the safe default).

**Pending question.** New items 37 and 38 below in §4.

---

## 4. Pending owner-only questions for later (additive)

The companion file [`PENDING_QUESTIONS.md`](PENDING_QUESTIONS.md) is the canonical list. This assessment **adds** the following items so that when you next ask "what is still open?", the answer is complete:

- ~~**29.** Sponsor approval on the `BeforeAfterDeltaPanel` placement in the operator dashboard (Improvement 3) — top of `/runs` list, sidebar widget, or `/runs/[runId]`?~~ **Resolved 2026-04-23 — all three placements** (single component instance gated by route context). See `PENDING_QUESTIONS.md` § *Resolved 2026-04-23 (assessment §4 items 29, 31–38 + two cross-cutting — 11 decisions)*.
- **30.** Marketplace publisher legal entity name on customer statements (Improvement 4) — "ArchLucid, Inc." or DBA variant? **Open — deferred to V1.1 commerce un-hold; revisit at V1.1 planning time.** (Improvement 4 itself is `DEFERRED — V1.1`; this question waits with it.)
- ~~**31.** Approval on the **side-by-side downloadable PDF** in Improvement 5~~ **Resolved 2026-04-23 — both surfaces** (inline page section visible without download AND a "Download PDF" button rendering the same artefact).
- ~~**32.** Preferred Teams connector trigger set in Improvement 7~~ **Resolved 2026-04-23 — all five triggers** (`run.committed`, `governance.approval.requested`, `alert.raised`, `compliance.drift.escalated`, `seat.reservation.released`).
- ~~**33.** Golden-cohort baseline-lock approval (Improvement 8)~~ **Resolved 2026-04-23 — lock SHAs today** from a single approved simulator run via `archlucid golden-cohort lock-baseline --write`.
- ~~**34.** Ownership of removing the stale `IMPROVEMENTS_COMPLETE.md` at repo root (§1.23)~~ **Resolved 2026-04-23 — delete** (git history preserves it).
- ~~**35.** *(Added 2026-04-23 with Improvement 9.)* **Board-pack PDF cover-page narrative**~~ **Resolved 2026-04-23 — assistant-drafted placeholder** `<<sponsor cover narrative — owner approval before external use>>`; owner approves before any external use.
- ~~**36.** *(Added 2026-04-23 with Improvement 9.)* **Monthly exec-digest cadence default**~~ **Resolved 2026-04-23 — opt-out for NEW tenants** (existing tenants stay 'Weekly' via the three-step migration shape — see `CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_68_60.md` Prompt 1 (replacement) step 1). **Safe-default override flagged:** the V1 migration MUST use the three-step backfill shape so SQL Server's `ADD … NOT NULL DEFAULT` behaviour does not silently flip every existing tenant to Monthly.
- ~~**37.** *(Added 2026-04-23 with Improvement 10.)* **Governance dry-run audit metadata**~~ **Resolved 2026-04-23 — capture count AND payload** with the existing `LlmPromptRedaction`-style PII redaction pipeline mandatory before serialisation. **Safe-default override flagged:** payload capture is **conditional on the redaction pipeline being applied**; if redaction is bypassed in a future change to the audit write path, payload capture must be turned off until redaction is restored. Anyone with `ReadAuditAuthority` in the same tenant can see proposed policy override values.
- ~~**38.** *(Added 2026-04-23 with Improvement 10.)* **Governance dry-run "load more" pagination cap**~~ **Resolved 2026-04-23 — 20-default / 100-max** (matches assistant default).
- **39.** *(Added 2026-04-23 from cross-cutting q11.)* **"AI Architecture Review Board" rebrand workstream — schedule.** Owner opened the door 2026-04-23 to repositioning "AI Architecture Intelligence" toward "AI Architecture Review Board" (more buyer-recognisable). The rebrand is multi-doc + multi-route (marketing site `/why`, `/pricing`, `/get-started`; sponsor brief; competitive landscape; per-vertical briefs; Trust Center; in-product copy on operator-shell governance pages). **Owner-only:** schedule (V1, V1.1, or post-V1.1?) and rebrand owner. Assistant safe default until scheduled: do not change current product copy.

**Cross-cutting items (resolved 2026-04-23 in the same Q&A — not numbered above):**
- Trust Center "Recent assurance activity" update timing when Aeronova pen test redacted summary lands → **update immediately on assessor delivery** (no comms draft gate).
- "AI Architecture Intelligence" category-name fixed vs repositionable → **open to repositioning** toward "AI Architecture Review Board" (surfaces as new pending question 39 above).

When you ask later "what pending questions do you have?" the answer is **items 30 (V1.1-deferred) and 39 (rebrand workstream schedule) from this assessment plus items 17–28 from the previous assessment that are still unresolved in `PENDING_QUESTIONS.md`**. **Items 29, 31–38** are **all resolved 2026-04-23** — see the **Resolved 2026-04-23 (assessment §4 items 29, 31–38 + two cross-cutting — 11 decisions)** section in `PENDING_QUESTIONS.md`. **Item 19** (first PLG row owner) is **closed** as of 2026-04-23 because the underlying milestone is now V1.1 and the ownership question was already resolved to "Owner solo" — see **Resolved 2026-04-23 (Reference-customer publication scope)** in `PENDING_QUESTIONS.md`. **Items 8, 9, 22** (Marketplace listing live, Stripe live keys flipped, DNS cutover) are **release-window-pinned to V1.1** as of 2026-04-23 — see **Resolved 2026-04-23 (Commerce un-hold scope)** in `PENDING_QUESTIONS.md`. They remain owner-only at V1.1 planning time but are no longer V1 owner-action items.

---

## 5. Items I could not assess from the repo

- **Field correctness on novel architecture inputs.** Until the golden cohort has locked baseline SHAs (Improvement 8), I can only score structural correctness.
- **Real customer onboarding friction.** Until the trial funnel runs end-to-end against staging in TEST mode (Improvement 2), and at least one pilot completes the funnel, I am inferring from code paths.
- **Production reliability.** Simmy chaos workflow exists; I have no production incident postmortems to read.
- **Real ROI delta.** Every number I see is from the model; no field-validated curve is published.

These are the same gaps the previous assessment flagged — they remain because they are **inherent to a pre-first-paying-customer state**, not because of an engineering deficit.

---

## 6. Related documents

| Doc | Use |
|-----|-----|
| [`CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_68_60.md`](CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_68_60.md) | Eight paste-ready Cursor prompts for the improvements above |
| [`PENDING_QUESTIONS.md`](PENDING_QUESTIONS.md) | Owner-only decisions and open items (canonical list) |
| [`V1_SCOPE.md`](library/V1_SCOPE.md) | What is in / out of V1 — the assessment respects this scope |
| [`V1_DEFERRED.md`](library/V1_DEFERRED.md) | Doc-sourced V1.1+ candidates (not held against the score) |
| [`V1_READINESS_SUMMARY.md`](library/V1_READINESS_SUMMARY.md) | One-paragraph honest snapshot of where the repo stands |
| [`PRICING_PHILOSOPHY.md`](go-to-market/PRICING_PHILOSOPHY.md) | Pricing source of truth; § 5.3/5.4 trigger gates |
| [`AUDIT_COVERAGE_MATRIX.md`](library/AUDIT_COVERAGE_MATRIX.md) | 101 typed audit events; known gaps tracking |
| [`ARCHITECTURE_COMPONENTS.md`](library/ARCHITECTURE_COMPONENTS.md) | Bounded context map for the assessment's architectural-integrity score |

**Change control.** When the next assessment lands, link it from `PENDING_QUESTIONS.md` § Related so the chain is navigable.
