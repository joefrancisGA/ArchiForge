> **Scope:** Ten paste-ready Cursor prompts for the V1-actionable improvements in [`QUALITY_ASSESSMENT_2026_04_23_INDEPENDENT_73_20.md`](QUALITY_ASSESSMENT_2026_04_23_INDEPENDENT_73_20.md). Each prompt is self-contained — it assumes the assistant starts from a clean session with no memory of the assessment.

> **Spine doc:** [Five-document onboarding spine](FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.

# Cursor prompts — V1-actionable improvements after the 73.20% assessment (2026-04-23)

**How to use.** One prompt per session. Paste the whole block (between the triple backticks) into a fresh Cursor agent. Each prompt names its **stop-and-ask** boundaries — the assistant should not cross those without owner input. After each prompt completes, update [`docs/PENDING_QUESTIONS.md`](PENDING_QUESTIONS.md) accordingly.

**DEFERRED markers.** Original Improvements 9 (pen-test publication) and 10 (PGP key drop) were deferred to V1.1 by the sixth-pass owner Q&A on 2026-04-23. No prompts are generated for them. They are replaced here by Improvements 11 and 12.

> **SaaS audience guard (read before running any prompt below).** ArchLucid is a **SaaS** product. Customers, evaluators, and sponsors never install Docker, SQL, .NET, Node, or Terraform. They only ever interact with the public website (`archlucid.net`), the in-product operator UI (after sign-in), and the Azure portal for their own subscription identity / billing. Any customer-facing copy must not assume the customer runs Docker, opens a terminal, or applies Terraform. Tooling like `apply-saas.ps1`, `archlucid try`, `dev up`, `docker compose`, the `.devcontainer/`, and `engineering/INSTALL_ORDER.md` is **internal ArchLucid contributor / operator** tooling. If a prompt seems to require a customer-side install step, **stop and ask the user** rather than inventing one.

---

## Prompt 1 — Buyer-facing first-30-minutes path (repo stub + marketing route)

**Why this matters.** Self-direct SaaS evaluation; no installs of any kind. Currently a non-engineer evaluator either signs a sales call or downloads contributor toolchain. Owner-decided 2026-04-23 sixth-pass Q&A: voice = **consultative**, vertical-picker labels = **existing `templates/briefs/*` folder slugs**, screenshots = **real anonymized tenant** (placeholder slots OK in this PR), placeholder copy in repo stub = **yes (q35-style markers)**, "talk to a human" CTA = **V1.1 (defer)**.

```
Goal: ship a buyer-facing first-30-minutes path in TWO surfaces — a
short repo stub at docs/BUYER_FIRST_30_MINUTES.md (for evaluators
arriving via GitHub) and the full copy on the marketing
archlucid-ui/src/app/(marketing)/get-started/page.tsx route. The
journey is: archlucid.net landing page → signed in → vertical picker
→ first sample run → first finding, with NO local install of any
kind. Voice is consultative / pragmatic per owner Q1 (2026-04-23
sixth pass). Vertical-picker labels come from the existing
templates/briefs/*/ folder slugs per Q2 (no new owner-supplied label
set). Screenshots ship as placeholder slots per Q3 (real anonymized
tenant capture is a follow-on owner task). Placeholder copy in the
repo stub uses the q35-style marker "<<placeholder copy — replace
before external use>>" per Q4. Do NOT add a "talk to a human" CTA —
that is V1.1 per Q5. Do NOT invent customer names or testimonials.

Read first:
- docs/QUALITY_ASSESSMENT_2026_04_23_INDEPENDENT_73_20.md  (sections 1.1, 1.2, 1.3, 1.25 and §3 Improvement 1)
- docs/PENDING_QUESTIONS.md  (Resolved 2026-04-23 sixth-pass table — Q1–Q5)
- docs/CORE_PILOT.md  (the four-step Pilot path — buyer copy mirrors but does not duplicate this)
- docs/library/PRODUCT_PACKAGING.md  (two-layer Pilot/Operate framing)
- docs/EXECUTIVE_SPONSOR_BRIEF.md  (sponsor-facing voice reference)
- archlucid-ui/src/app/(marketing)/get-started/  (existing route stub if any)
- templates/briefs/  (folder slugs are the picker labels — list them; do NOT rename)
- docs/QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md  §0.1  (SaaS-framing addendum — read carefully)

Do this:
1. Create docs/BUYER_FIRST_30_MINUTES.md (≤ 80 lines). Audience banner
   at the top: "Audience: prospective buyers and evaluators arriving
   via GitHub. For internal contributor onboarding see
   docs/engineering/FIRST_30_MINUTES.md." Sections (consultative
   voice, no marketing puffery):
     - "Where you are now" (you're on GitHub; the product is SaaS)
     - "What 30 minutes looks like" (sign in → pick vertical → first
       run → first finding) — five numbered steps with the q35
       placeholder marker on every owner-blocked sentence
     - "Where to go next" (link to marketing /get-started for screenshots,
       link to docs/CORE_PILOT.md for the operator path)
     - Explicit "no local install" note pointing at engineering/
       paths only for ArchLucid contributors.
2. Build the marketing route at archlucid-ui/src/app/(marketing)/get-started/page.tsx
   (or extend the existing stub). Same five-step shape as the repo
   stub, with placeholder image slots (`<Image src=
   "/get-started/step-{n}-placeholder.png" />`). The vertical picker
   reads from a static list mirroring templates/briefs/ folder slugs
   (financial-services, healthcare, manufacturing, public-sector,
   public-sector-us). NO talk-to-a-human CTA. NO email capture in
   this PR (V1 motion is sales-led; the sales CTA is the existing
   "Request a quote" button on /pricing).
3. Add a CI guard at scripts/ci/assert_buyer_first_30_minutes_in_sync.py
   that fails if the marketing route's vertical-picker labels diverge
   from the templates/briefs/ folder slugs OR if any non-placeholder
   sentence on either surface is missing an owner-approval marker
   when the file is touched (heuristic: any sentence outside the
   q35 placeholder pattern in the buyer-facing files needs to be
   in a small allow-list this script ships). Wire into ci.yml.
4. Tests:
   - Vitest spec for the marketing route asserting all five steps
     render and the picker shows exactly five labels matching the
     allow-list.
   - Markdown link-check for docs/BUYER_FIRST_30_MINUTES.md.
   - CI script self-test (fixture .md and fixture page.tsx — assert
     the script catches both divergence cases).
5. Update docs/START_HERE.md "Audience split" section to add the new
   buyer-facing path as the canonical evaluator entry.
6. Update docs/CHANGELOG.md with a 2026-04-23 entry naming Q1–Q5 as
   the source decisions.

Stop-and-ask boundaries:
- Do NOT invent customer prose for any sentence outside the
  q35-placeholder pattern beyond what's already in the consultative
  scaffold. If you find yourself writing more than ~3 paragraphs of
  net-new buyer-voice prose, stop and ask the user.
- Do NOT capture or display any real screenshots in this PR — slots
  only. Owner names tenantId/runId for the real-tenant capture in a
  follow-on task per Q3.
- Do NOT add the "talk to a human" CTA — that is V1.1 per Q5.

Land everything in one PR. After merge, update PENDING_QUESTIONS.md
item 36 to mark it Resolved (the wiring is done; copy is owner-blocked
on Q3 screenshots only, and that's a separate follow-on).
```

---

## Prompt 2 — Trial funnel TEST-mode end-to-end (staging)

**Why this matters.** Sales-engineer-led product evaluation without live commerce. The trial signup funnel exists in components but is not wired end-to-end on staging in TEST mode. Live keys are V1.1-deferred per owner Q17; the V1 scope is TEST-mode.

```
Goal: ship the trial signup funnel end-to-end on staging in Stripe
TEST mode. The V1 commercial motion is SALES-LED — live keys are
V1.1-deferred per owner Q17 (2026-04-23). This work makes the funnel
runnable as a sales-engineer-led product evaluation: prospect signs
up at signup.staging.archlucid.net, lands in the operator UI on a
trial tenant, sees the Pilot path on the home page, runs the seven-
step wizard, gets a first committed manifest, reads the value report.

Read first:
- docs/QUALITY_ASSESSMENT_2026_04_23_INDEPENDENT_73_20.md  (sections 1.1, 1.3, 1.6 and §3 Improvement 2)
- docs/runbooks/TRIAL_FUNNEL_END_TO_END.md  (existing developer-facing smoke — extend)
- ArchLucid.Api/Controllers/Billing*Controller.cs  (existing wiring)
- ArchLucid.Api/Filters/RequiresCommercialTenantTierAttribute.cs  (402 filter)
- ArchLucid.Api/Bootstrap/BillingProductionSafetyRules.cs  (must STAY shipped per V1_DEFERRED §6b rules)
- archlucid-ui/src/app/(marketing)/signup/  (existing stub)
- docs/library/V1_DEFERRED.md  §6b  (commerce-un-hold V1.1 boundaries)

Do this:
1. End-to-end Playwright spec at archlucid-ui/tests/e2e/trial-funnel-test-mode.spec.ts
   that exercises: signup form → email confirmation (deterministic
   mock) → trial tenant created → first sign-in → wizard step 1 →
   wizard step 7 → run committed → value report rendered. Asserts
   correlation IDs propagate. Skips if STRIPE_TEST_KEY env not set.
2. CLI smoke command `archlucid trial smoke --staging` that runs the
   API-side equivalent (no UI) against a staging API host with a
   TEST-mode Stripe configuration. Outputs a one-line PASS/FAIL +
   correlation ID for support.
3. Extend docs/runbooks/TRIAL_FUNNEL_END_TO_END.md with a "Sales-
   engineer playbook" section: how a sales engineer uses the funnel
   to drive a product evaluation, what to tell the prospect, how to
   reset a trial tenant after the evaluation.
4. Wire the e2e spec into a new GitHub Actions workflow at
   .github/workflows/trial-funnel-test-mode.yml that runs nightly
   against the staging environment. Failure pages an oncall channel
   (placeholder webhook variable; owner sets the real URL later).
5. Add a CI guard at scripts/ci/assert_billing_safety_rules_shipped.py
   that fails if BillingProductionSafetyRules is removed or its
   sk_live_/marketplace-published/landing-page checks are weakened.
6. Tests:
   - Unit tests for the new CLI command (mock the trial provisioning
     API).
   - Integration test for the BillingProductionSafetyRules guard.

Stop-and-ask boundaries:
- Do NOT touch any Stripe LIVE configuration. Anything with the
  sk_live_ prefix is owner-only and V1.1-gated.
- Do NOT publish the Marketplace listing. The wiring stays at
  Status: Draft per V1_DEFERRED §6b.
- Do NOT cut over signup.archlucid.net DNS. Staging only — that's
  signup.staging.archlucid.net.

Update docs/CHANGELOG.md with a 2026-04-23 entry. Update
PENDING_QUESTIONS.md item 22 to point at this work as the V1
deliverable that makes V1.1 commerce un-hold safe.
```

---

## Prompt 3 — `BeforeAfterDeltaPanel` (top of /runs + sidebar widget + inline)

**Why this matters.** Make value visible at every operator touch point. Owner q29 (prior batch, 2026-04-23) confirmed all three placements.

```
Goal: ship a single BeforeAfterDeltaPanel React component and wire
it into THREE routes per owner q29 (2026-04-23): top of the /runs
list page, as a sidebar widget, AND inline on each /runs/[runId]
detail page. Same component instance, route-context-gated.

Read first:
- docs/QUALITY_ASSESSMENT_2026_04_23_INDEPENDENT_73_20.md  (sections 1.2, 1.9, 1.22 and §3 Improvement 3)
- ArchLucid.Application/Pilots/PilotRunDeltaComputer.cs  (data source — already exists)
- archlucid-ui/src/app/(operator)/runs/page.tsx  (top placement)
- archlucid-ui/src/app/(operator)/runs/[runId]/page.tsx  (inline placement)
- archlucid-ui/src/components/operator-shell/Sidebar.tsx  (sidebar widget)

Do this:
1. New component: archlucid-ui/src/components/value/BeforeAfterDeltaPanel.tsx
   - Props: { variant: 'top' | 'sidebar' | 'inline', runId?: string }
   - Top variant: aggregates the most recent N runs (default 5) and
     shows the median delta on findings + time-to-first-finding.
   - Sidebar variant: same data, compact rendering.
   - Inline variant: single-run delta vs the prior committed run for
     the same architecture request (uses runId from props).
   - All three variants share a useDeltaQuery() hook that calls the
     existing /v1/pilots/runs/{id}/first-value-report endpoint or
     the new aggregated /v1/pilots/runs/recent-deltas endpoint
     (build the latter — gated on ReadAuthority, returns the last
     N committed runs' delta summaries).
2. New API endpoint: GET /v1/pilots/runs/recent-deltas?count=5 on
   PilotsController, gated [Authorize(Policy = "ReadAuthority")],
   returns Decisioning.Models.RunDeltaSummary[].
3. Wire all three placements:
   - top: insert at the top of /runs page above the existing list.
   - sidebar: add to Sidebar.tsx as a collapsible card under "Recent
     activity".
   - inline: insert on /runs/[runId] above the artifacts table.
4. Tests:
   - Vitest specs for all three variants (use a shared MSW handler
     so the test surface mirrors prod data shape).
   - API integration test for the new /recent-deltas endpoint.
5. Update docs/library/OPERATOR_ATLAS.md with the new endpoint.

Stop-and-ask boundaries: none — this is a self-contained component +
endpoint. Land in one PR. Update CHANGELOG.md.
```

---

## Prompt 4 — Brand-neutral content seam + V1 rebrand workstream ("AI Architecture Review Board")

**Why this matters.** Owner Q6 confirmed repositioning toward "AI Architecture Review Board"; Q7 confirmed V1 schedule. The content seam ships first; the workstream then flips it across all surfaces in follow-on PRs.

```
Goal: ship the brand-neutral content seam for product-category
positioning and schedule the V1 rebrand workstream that flips the
default value from "AI Architecture Intelligence" to "AI
Architecture Review Board" per owner Q6 / Q7 (2026-04-23 sixth pass).

This PR ships ONLY the seam and ONE flagship surface (/why) flipped.
Follow-on PRs cover the remaining surfaces in the workstream.

Read first:
- docs/QUALITY_ASSESSMENT_2026_04_23_INDEPENDENT_73_20.md  (sections 1.2, 1.9, 1.11 and §3 Improvement 4)
- docs/PENDING_QUESTIONS.md  (Resolved 2026-04-23 sixth-pass — Q6 / Q7 + pending question 39)
- archlucid-ui/src/lib/  (find a good home for brand-category.ts)
- archlucid-ui/src/app/(marketing)/why/  (flagship surface for this PR)
- docs/EXECUTIVE_SPONSOR_BRIEF.md  (one of the surfaces to flip in follow-on PRs)
- docs/go-to-market/COMPETITIVE_LANDSCAPE.md  (another follow-on surface)
- docs/trust-center.md  (another follow-on surface)
- templates/briefs/  (per-vertical brief docs — follow-on surfaces)

Do this:
1. New file: archlucid-ui/src/lib/brand-category.ts exporting:
     export const BRAND_CATEGORY = "AI Architecture Review Board";
     export const BRAND_CATEGORY_LEGACY = "AI Architecture Intelligence";
   With a JSDoc header explaining: "This is the single point of truth
   for the buyer-facing product category. To rebrand: change
   BRAND_CATEGORY here and run `npm run rebrand-check` to find any
   surface that hardcodes the legacy string."
2. New CI guard at scripts/ci/assert_brand_category_seam.py that
   fails if "AI Architecture Intelligence" appears in any file under
   archlucid-ui/src/app/, docs/EXECUTIVE_SPONSOR_BRIEF.md,
   docs/go-to-market/COMPETITIVE_LANDSCAPE.md, docs/trust-center.md,
   templates/briefs/**/brief.md, or docs/library/PRODUCT_PACKAGING.md
   — UNLESS the same file imports BRAND_CATEGORY_LEGACY (small allow-
   list for the seam file itself). The script ships in WARN mode for
   THIS PR; flip to fail mode in the follow-on PR that completes the
   workstream.
3. Flip the /why marketing surface in this PR: archlucid-ui/src/app/(marketing)/why/page.tsx
   imports BRAND_CATEGORY and uses it everywhere the legacy string
   appeared. Keep the legacy string in a single hidden meta tag so
   SEO redirects work for ~30 days.
4. Schedule the rebrand workstream in docs/CHANGELOG.md AND in a new
   docs/architecture/REBRAND_WORKSTREAM_2026_04_23.md tracker:
     - PR-1 (this PR): seam + /why
     - PR-2: /pricing + /get-started
     - PR-3: docs/EXECUTIVE_SPONSOR_BRIEF.md + docs/go-to-market/COMPETITIVE_LANDSCAPE.md
     - PR-4: per-vertical brief docs (templates/briefs/**/brief.md)
     - PR-5: docs/trust-center.md + docs/library/PRODUCT_PACKAGING.md
     - PR-6: in-product copy (operator-shell governance pages, navigation labels)
     - PR-7 (closing): flip the CI guard from warn to fail.
5. Tests:
   - Vitest spec asserting /why renders BRAND_CATEGORY (not the
     legacy string).
   - CI script self-test (fixture file with the legacy string,
     fixture file with the seam import).

Stop-and-ask boundaries:
- Do NOT touch the in-product operator-shell copy in this PR. That
  is PR-6 in the workstream — separate session.
- Do NOT remove the legacy string from the seam file. Keep
  BRAND_CATEGORY_LEGACY exported for SEO redirect handlers.

Update docs/CHANGELOG.md and PENDING_QUESTIONS.md (mark question 39
schedule sub-decision as scheduled — workstream is in flight).
```

---

## Prompt 5 — Governance dry-run / what-if mode for policy threshold changes

**Why this matters.** Threshold tuning today requires real commits. Owner q37 / q38 (prior batch) confirmed payload-capture-with-redaction audit + 20/100 pagination.

```
Goal: ship a governance dry-run / what-if mode for policy threshold
changes, with payload-capture-with-redaction audit per owner q37 and
20-default / 100-max pagination per owner q38 (both 2026-04-23).

Read first:
- docs/QUALITY_ASSESSMENT_2026_04_23_INDEPENDENT_73_20.md  (sections 1.17, 1.18, 1.21 and §3 Improvement 5)
- ArchLucid.Decisioning/Governance/  (existing governance engine)
- ArchLucid.Application/Audit/AuditEventTypes.cs  (add the new event constant)
- docs/library/AUDIT_COVERAGE_MATRIX.md  (add the new event row)
- ArchLucid.Application/LlmPromptRedaction/  (the existing redaction pipeline — REUSE)
- ArchLucid.Api/Controllers/GovernanceController.cs  (host the new endpoint)
- archlucid-ui/src/app/(operator)/governance/  (UI for the modal)

Do this:
1. New endpoint: POST /v1/governance/policy-packs/{id}/dry-run gated
   [Authorize(Policy = "ReadAuthority")] (read auth is enough — no
   real commit happens). Body: { proposedThresholds: {...},
   evaluateAgainstRunIds: [...] }. Response: per-run delta showing
   what the new threshold WOULD have done.
2. Capture audit event AuditEventTypes.GovernanceDryRunRequested
   with payload schema:
     { proposedThresholdsRedacted: <redacted-json>,
       evaluatedRunIds: [...], deltaCounts: {...} }
   The proposedThresholds payload MUST pass through the existing
   LlmPromptRedaction-style pipeline before serialisation per owner
   q37. Add an integration test that asserts the redaction marker
   pattern appears in the persisted audit row when the input
   contains a known PII pattern.
3. UI: governance/dry-run modal on /governance/policy-packs/[id].
   Default page size 20, server-side cap 100 per owner q38. Vitest
   spec asserts default = 20.
4. Add AuditEventTypes.GovernanceDryRunRequested to the typed
   constant list AND to the audit-core-const-count snapshot test.
5. Update docs/library/AUDIT_COVERAGE_MATRIX.md with the new event row.

Stop-and-ask boundaries:
- The redaction pipeline is mandatory. Do NOT add a code path that
  bypasses it — the per-rule rule in PENDING_QUESTIONS.md sixth-pass
  table notes this explicitly.

Land in one PR. Update CHANGELOG.md and PENDING_QUESTIONS.md.
```

---

## Prompt 6 — Trust Center evidence-pack ZIP endpoint

**Why this matters.** Procurement teams need a single artefact. The content already exists; the lever is consolidation.

```
Goal: ship a downloadable Trust Center evidence-pack ZIP endpoint
that bundles the existing procurement content into one file.

Read first:
- docs/QUALITY_ASSESSMENT_2026_04_23_INDEPENDENT_73_20.md  (sections 1.7, 1.19 and §3 Improvement 6)
- docs/trust-center.md  (top-level Trust Center)
- docs/security/  (DPA, subprocessors, owner sec assessment)
- docs/security/pen-test-summaries/  (SoW template — ships even though execution is V1.1)
- docs/library/AUDIT_COVERAGE_MATRIX.md  (audit matrix is part of the pack)
- docs/library/TRUST_CENTER_FAQ.md  (CAIQ Lite + SIG Core if present)
- ArchLucid.Api/Controllers/MarketingController.cs  (host the new endpoint here)

Do this:
1. New endpoint: GET /v1/marketing/trust-center/evidence-pack.zip
   ANONYMOUS access (no auth — public Trust Center artefact).
   Builds the ZIP at request time from the in-repo files; cache the
   ZIP for 1 hour with an ETag based on the SHA-256 of the included
   files' content.
2. ZIP contents (mirror docs/trust-center.md links plus the canonical
   security artefacts):
     - README.md (auto-generated index)
     - DPA-template.md
     - SUBPROCESSORS.md
     - SLA-summary.md
     - security.txt (a copy)
     - CAIQ-Lite.xlsx (if present in repo) or CAIQ-Lite.md
     - SIG-Core.md
     - OWNER_SECURITY_ASSESSMENT_2026_Q2-DRAFT.md
     - PEN_TEST_SOW_2026_Q2.md (the SoW, NOT the redacted summary —
       summary is V1.1)
     - AUDIT_COVERAGE_MATRIX.md
3. Add a "Download evidence pack" button to docs/trust-center.md AND
   to the marketing /trust-center page (if it exists; if not, scope
   to docs/trust-center.md only).
4. Tests:
   - Integration test: GET the endpoint and assert the ZIP contains
     all expected entries.
   - Unit test for the ETag derivation.
5. Update docs/library/OPERATOR_ATLAS.md with the new endpoint.

Stop-and-ask boundaries:
- Do NOT include the pen-test REDACTED SUMMARY (that's V1.1-gated
  per Q10). Include only the SoW.
- Do NOT include the PGP key (also V1.1).

Land in one PR. Update CHANGELOG.md.
```

---

## Prompt 7 — In-product opt-in tour + `/admin/support` support-bundle download UI

**Why this matters.** First-tenant guided path + SaaS self-sufficiency. Owner Q8 / Q9 confirmed: assistant drafts tour copy with "pending owner approval" markers; tour is opt-in via "Show me around" button (never auto-launches). Owner decisions F / G (prior batch) confirmed: support-bundle UI on `/admin/support`, gated `ExecuteAuthority`.

```
Goal: ship two things in one PR — (a) an in-product opt-in tour
launched only via a "Show me around" button on the operator-shell
home page (NEVER auto-launches per owner Q9), with five tour steps
whose copy is the assistant's first cut clearly marked "pending owner
approval" per owner Q8; and (b) a new /admin/support page with a
support-bundle download button gated on ExecuteAuthority per owner
decision F (prior batch).

Read first:
- docs/QUALITY_ASSESSMENT_2026_04_23_INDEPENDENT_73_20.md  (sections 1.14, 1.16, 1.1 and §3 Improvement 7)
- archlucid-ui/src/app/(operator)/  (operator shell layout)
- archlucid-ui/src/app/(operator)/admin/  (existing admin pages — model new page on /admin/api-keys)
- ArchLucid.Cli/Commands/SupportBundleCommand.cs  (existing CLI implementation — REUSE the bundle assembly logic)
- ArchLucid.Api/Controllers/AdminController.cs  (host the new endpoint here, near /admin/api-keys)

Do this:
1. New endpoint: POST /v1/admin/support-bundle gated
   [Authorize(Policy = "ExecuteAuthority")] per owner decision F.
   Reuses the existing CLI bundle-assembly logic via a shared
   ISupportBundleAssembler service. Returns a streaming ZIP.
2. New UI page: archlucid-ui/src/app/(operator)/admin/support/page.tsx
   - "Download support bundle" button — calls the new endpoint and
     streams the ZIP to the browser.
   - Link from /admin/api-keys page ("Need to file a support
     ticket? Download a support bundle here.").
3. Tour component: archlucid-ui/src/components/tour/OptInTour.tsx
   - Five steps. Copy is the assistant's first cut, each step
     wrapped in a `<TourStepPendingApproval>` component that renders
     a visible "<<tour copy — pending owner approval>>" marker
     below the displayed text per owner Q8. Marker is NOT visible
     to end-tenants once owner approves and removes the wrapper.
   - "Show me around" button on the operator-shell home page only —
     no auto-launch on first sign-in, no interception of any other
     route per owner Q9.
   - LocalStorage flag remembers if the user dismissed the tour
     (persistent dismissal).
4. Tests:
   - Integration test for the new /admin/support-bundle endpoint
     (assert ExecuteAuthority is required; unauthorised returns 403).
   - Vitest spec for the tour: asserts (a) all five steps render
     the placeholder marker, (b) tour does NOT auto-launch on app
     mount, (c) tour DOES launch when the button is clicked.
5. Update docs/library/OPERATOR_ATLAS.md and docs/CHANGELOG.md.

Stop-and-ask boundaries:
- Do NOT auto-launch the tour. Owner Q9 was explicit.
- Do NOT hide the "<<tour copy — pending owner approval>>" markers
  before owner approves real copy. They MUST be visible until then.

Land in one PR. Update PENDING_QUESTIONS.md item 37 (support-bundle
parts a + b are now Resolved; only the redaction policy sub-question
remains open).
```

---

## Prompt 8 — Coordinator → Authority unification PR A3 + PR A4

**Why this matters.** Owner Decision A (2026-04-23) unblocked the FK-chain rewrite of `DemoSeedService` + `ReplayRunService`. PR A3 deletes coordinator repos from composition + deletes legacy orchestrators; PR A4 drops `dbo.GoldenManifestVersions`.

```
Goal: ship PR A3 and PR A4 of ADR 0030 (Coordinator → Authority
pipeline unification). Owner Decision A on 2026-04-23 unblocked
this work by approving the full Authority FK chain build-out in
DemoSeedService and ReplayRunService.

Read first:
- docs/QUALITY_ASSESSMENT_2026_04_23_INDEPENDENT_73_20.md  (sections 1.5, 1.8, 1.20 and §3 Improvement 8)
- docs/adr/0030-coordinator-authority-pipeline-unification.md  (the canonical plan)
- docs/PENDING_QUESTIONS.md  (Resolved 2026-04-23 SaaS-framing follow-on Q&A — Decision A and Decision B)
- ArchLucid.Decisioning/  (Authority engine)
- ArchLucid.Persistence/Migrations/111_DropGoldenManifestVersions_Legacy.sql  (PR A4 reference)
- ArchLucid.Application/Demo/DemoSeedService.cs  (rewrite target #1)
- ArchLucid.Application/Replay/ReplayRunService.cs  (rewrite target #2)
- ArchLucid.Persistence.Coordination/  (deletion target)

Do this — sequence PR A3 first, then PR A4 in a separate PR (two PRs):

PR A3 (this session):
1. Rewrite DemoSeedService to emit the full Authority FK chain
   (snapshots, decision traces, evidence rows). Config flag
   Demo:SeedDepth = quickstart | vertical per owner Decision B —
   quickstart writes one-of-each minimum; vertical writes the
   production-realistic depth.
2. Rewrite ReplayRunService to emit the full Authority FK chain.
3. Delete coordinator-shape repository registrations from composition
   (ArchLucid.Host.Composition).
4. Delete the legacy CoordinatorRunCommitOrchestrator concrete (keep
   the interface for the deprecation header window if needed; verify
   in ADR 0029).
5. Sweep DI, regenerate OpenAPI snapshot, shrink
   DualPipelineRegistrationDisciplineTests allow-list.
6. Update ADR 0030 § Component breakdown PR A3 row to show DONE.
7. Tests: existing parity-probe daily workflow stays warn-mode; new
   integration test asserts DemoSeedService quickstart and vertical
   modes both produce a committed manifest with non-empty Services
   + Datastores + Relationships.

PR A4 (next session — DO NOT bundle into PR A3):
1. Run migration 111_DropGoldenManifestVersions_Legacy.sql.
2. Update ADR 0030 § Component breakdown PR A4 row to show DONE.
3. Verify the parity-probe workflow still has nothing to compare
   against (the legacy table is gone — probe should self-disable
   gracefully).

Stop-and-ask boundaries:
- Do NOT bundle PR A3 and PR A4 into one PR. The DDL drop in PR A4
  is the rollback boundary; keep them separate.
- Do NOT skip the OpenAPI snapshot regen — downstream client SDK
  generation depends on it.

Land PR A3 in this session; queue PR A4 for next session. Update
CHANGELOG.md for PR A3.
```

---

## Prompt 11 (replaces deferred Prompt 9) — Azure OpenAI cost-and-latency dashboard for the golden-cohort real-LLM gate

**Why this matters.** Owner Q15 (2026-04-23 sixth pass) approved the $50/month budget for the golden-cohort real-LLM gate. The dashboard + nightly kill-switch are the safety + visibility layer that lets the gate flip from disabled to required.

```
Goal: ship the Azure OpenAI cost-and-latency dashboard for the
golden-cohort real-LLM gate, with a nightly kill-switch that disables
the gate when month-to-date spend approaches the $50/month cap (per
owner Q15, 2026-04-23 sixth pass). When the dedicated Azure OpenAI
deployment exists in production (owner-only operational task),
flipping cohort-real-llm-gate from disabled to required is a one-line
change in golden-cohort-nightly.yml.

Read first:
- docs/QUALITY_ASSESSMENT_2026_04_23_INDEPENDENT_73_20.md  (sections 1.4, 1.10, 1.13 and §3 Improvement 11)
- docs/PENDING_QUESTIONS.md  (Resolved 2026-04-23 sixth-pass table — Q15)
- .github/workflows/golden-cohort-nightly.yml  (existing workflow with the placeholder cohort-real-llm-gate job)
- ArchLucid.Cli/Commands/GoldenCohortLockBaselineCommand.cs  (existing CLI)
- infra/  (Terraform — find the App Insights / Log Analytics workspace stacks)

Do this:
1. Terraform module at infra/modules/golden-cohort-cost-dashboard/
   that provisions an Azure Monitor Workbook in the existing App
   Insights workspace. Workbook shows:
     - Month-to-date Azure OpenAI spend on the cohort deployment
     - p50 / p95 / p99 latency per cohort scenario
     - Daily token-count trend
     - Kill-switch status (enabled / disabled / tripped)
   Workbook is read-only-by-default; only the cohort-ops role can edit.
2. New workflow step in .github/workflows/golden-cohort-nightly.yml:
   - Pre-run: query Azure Cost Management for month-to-date spend on
     the cohort Azure OpenAI deployment (use OIDC; service principal
     must have Cost Management Reader on the subscription).
   - If spend >= 80% of the cap ($40 of $50): post a warning to the
     workflow log and an issue on the repo, do NOT abort.
   - If spend >= 95% ($47.50): SKIP the cohort run for the rest of
     the month, post an issue, do NOT count as workflow failure.
   - On every run (when not skipped): write the per-scenario p50 /
     p95 / p99 latency into a JSON artefact persisted to App Insights
     custom metrics.
3. Add a CI guard at scripts/ci/assert_golden_cohort_kill_switch_present.py
   that fails if the kill-switch step is missing from the workflow
   OR if its threshold ratios are weakened (must stay at 0.80 / 0.95
   per Q15-conditional rule in PENDING_QUESTIONS.md).
4. Tests:
   - Unit test for the threshold-ratio parsing.
   - Workflow self-test (act-style local run with mocked Cost
     Management API).
5. Update docs/runbooks/GOLDEN_COHORT_REAL_LLM_GATE.md (new file)
   with: how to flip the gate from disabled to required, how to
   respond to a kill-switch trip, how to read the Workbook.

Stop-and-ask boundaries:
- Do NOT provision the dedicated Azure OpenAI deployment itself —
  that's owner-only operational task per Q15. The Terraform here
  provisions only the Workbook.
- Do NOT inject the Azure OpenAI secret into the workflow — that's
  owner-only via the protected GitHub Environment.
- Do NOT flip cohort-real-llm-gate from disabled to required in this
  PR. That's a separate one-line PR after the deployment exists.

Land in one PR. Update CHANGELOG.md and PENDING_QUESTIONS.md (mark
items 15 and 25 as fully Resolved on the budget portion; deployment
provisioning + secret injection remain owner-only operational tasks).
```

---

## Prompt 12 (replaces deferred Prompt 10) — First-tenant onboarding telemetry funnel

**Why this matters.** Without measurable evidence that real tenants hit "first finding" inside 30 minutes, the marketing claim is unsubstantiated. Instruments the opt-in tour from Q9 (Improvement 7).

```
Goal: ship a first-tenant onboarding telemetry funnel that measures
the 30-minute first-finding success rate. Default emission is
AGGREGATED-ONLY (no per-tenant correlation in the funnel store) per
the proposed default in pending question 40; per-tenant emission is
behind a feature flag the owner can flip after a privacy review.

Read first:
- docs/QUALITY_ASSESSMENT_2026_04_23_INDEPENDENT_73_20.md  (sections 1.6, 1.2, 1.16 and §3 Improvement 12)
- archlucid-ui/src/components/tour/OptInTour.tsx  (instrument this from Improvement 7)
- ArchLucid.Application/Telemetry/  (existing telemetry surface)
- docs/security/PRIVACY_NOTE.md  (GDPR Art. 6(1)(f) reference)

Do this:
1. New telemetry events (typed):
     - first_tenant.signup
     - first_tenant.tour_opt_in           (when "Show me around" clicked)
     - first_tenant.first_run_started
     - first_tenant.first_run_committed
     - first_tenant.first_finding_viewed
     - first_tenant.thirty_minute_milestone (boolean, fired when the
       above five all happen within 30 minutes of signup)
2. New emission service: ArchLucid.Application/Telemetry/FirstTenantFunnelEmitter.cs
   - Reads feature flag Telemetry:FirstTenantFunnel:PerTenantEmission
     (default FALSE per pending question 40 default).
   - When false: emits aggregated counters only (no tenantId,
     no userId, no IP).
   - When true: emits per-tenant rows with tenantId only (no userId
     / IP); subject to GDPR Art. 6(1)(f) legitimate-interest
     analysis already documented in docs/security/PRIVACY_NOTE.md.
3. UI instrumentation: wire OptInTour.tsx + the existing wizard +
   the existing run-detail page to call the emitter at the right
   moments.
4. Storage: aggregated counters land in App Insights custom metrics;
   per-tenant rows (when the flag is on) land in a new
   dbo.FirstTenantFunnelEvents table (new migration).
5. Dashboard: extend the Workbook from Improvement 11 (or create a
   sibling) showing the funnel conversion rates.
6. Tests:
   - Unit tests for the emitter (assert per-tenant flag toggles
     between aggregated and per-tenant emission).
   - Integration test asserting tenantId is absent from emitted
     payloads when the flag is false.
7. Update docs/security/PRIVACY_NOTE.md to add the funnel as a named
   processing activity. Stop-and-ask if the existing privacy notice
   text doesn't already cover the activity shape.
8. Add pending question 40 to docs/PENDING_QUESTIONS.md as the new
   open item (per-tenant emission consent — owner-only).

Stop-and-ask boundaries:
- Do NOT default the per-tenant flag to TRUE. Default is FALSE per
  pending question 40 default.
- Do NOT capture userId or IP in either mode.
- If the existing PRIVACY_NOTE.md does not cover the activity shape,
  STOP and ask the user before adding new processing-activity text.

Land in one PR. Update CHANGELOG.md.
```

---

## Operating notes

- Run prompts in the order listed; the only hard dependency is **Prompt 7 → Prompt 12** (Improvement 12 instruments the tour from Improvement 7).
- For each prompt, after merge, cross-check that the assistant updated `docs/CHANGELOG.md` and `docs/PENDING_QUESTIONS.md` per the prompt's instructions.
- The two **DEFERRED** improvements (original 9 and 10) have no prompts here. Their work happens at V1.1 per the [`V1_DEFERRED.md` §6c](library/V1_DEFERRED.md) commitment.
- The **rebrand workstream** kicked off by Prompt 4 spans seven PRs (PR-1 through PR-7); only PR-1 is covered by Prompt 4 itself. Each subsequent PR is a separate session.
