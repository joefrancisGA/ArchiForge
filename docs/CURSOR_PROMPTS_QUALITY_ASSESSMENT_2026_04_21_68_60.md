> **Scope:** Eight paste-ready Cursor prompts for the largest-impact improvements in [`QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md`](QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md). Each prompt is self-contained — it assumes the assistant starts from a clean session with no memory of the assessment.

# Cursor prompts — top-8 improvements after the 68.60% assessment (2026-04-21)

**How to use.** One prompt per session. Paste the whole block (between the triple backticks) into a fresh Cursor agent. Each prompt names its **stop-and-ask** boundaries — the assistant should not cross those without owner input. After each prompt completes, update [`docs/PENDING_QUESTIONS.md`](PENDING_QUESTIONS.md) accordingly.

**DEFERRED markers.** A prompt is marked `[DEFERRED]` in the heading when the assistant cannot complete **at least part** of it without owner input that has not yet been received. None of the eight below are fully `DEFERRED` — every prompt has substantive work the assistant can land today.

---

## Prompt 1 — Reference customer publication scaffolding (PLG row)

**Owner gate.** Filling `<<CUSTOMER_NAME>>` and setting `Status: Published` are owner-only.

```
Goal: harden the publication runbook so the day a real PLG customer
approves copy is a small mechanical change, not a doc-and-pricing
scramble. Do NOT invent a customer name or flip Published.

Read first:
- docs/go-to-market/reference-customers/README.md
- docs/go-to-market/reference-customers/TRIAL_FIRST_REFERENCE_CASE_STUDY.md
- docs/go-to-market/reference-customers/REFERENCE_PUBLICATION_RUNBOOK.md
- docs/go-to-market/reference-customers/REFERENCE_EVIDENCE_PACK_TEMPLATE.md
- docs/go-to-market/PRICING_PHILOSOPHY.md  (sections 5.1, 5.3, 5.4)
- scripts/ci/check_reference_customer_status.py
- .github/workflows/ci.yml  (auto-flip block for the reference-customer guard)

Do this:
1. Audit every <<...>> placeholder in TRIAL_FIRST_REFERENCE_CASE_STUDY.md.
   Produce a single table at the top of the file titled
   "Owner substitution checklist — fill before customer review" listing
   each placeholder, what real value is needed, and which interview /
   contract / metric source it comes from. Do NOT invent values.
2. Build a one-page evidence-pack scaffold using
   REFERENCE_EVIDENCE_PACK_TEMPLATE.md tied to a real pilot-run-deltas.json
   sample committed by the existing demo-seed tenant. Mark every value
   with the literal text "demo tenant — replace before publishing".
3. Add CHANGELOG entry recording the row state-transition convention
   (Drafting -> Customer review is in-band assistant work; Customer
   review -> Published is owner-only). Use the existing 2026-04-21
   CHANGELOG style.
4. Verify scripts/ci/check_reference_customer_status.py still passes
   locally (no rows currently Published, advisory mode). Confirm the
   .github/workflows/ci.yml auto-flip block becomes merge-blocking
   the moment any row reaches Published with no further file edits.
5. Append a new pending-question item to docs/PENDING_QUESTIONS.md
   asking "who graduates the first PLG row from Customer review to
   Published?" if it isn't already there (item 19 covers this).

Stop and ask the user before:
- Filling any <<CUSTOMER_NAME>> with a real value
- Setting Status: Published
- Triggering the discount re-rate review per PRICING_PHILOSOPHY § 5.3

Exit criteria: PR opens with the substitution checklist populated,
the evidence-pack scaffold present, a CHANGELOG entry, and CI green.
No row is Published.
```

---

## Prompt 2 — Trial signup funnel end-to-end (Stripe TEST mode)

**Owner gate.** Switching to Stripe live keys, DNS cutover, and turning off the trial signup feature flag in production are owner-only.

```
Goal: take docs/go-to-market/TRIAL_AND_SIGNUP.md from "designed" to a
working funnel on staging in Stripe TEST mode, end-to-end, with a
mocked-Playwright spec and a CLI smoke command.

Read first:
- docs/go-to-market/TRIAL_AND_SIGNUP.md
- docs/runbooks/TRIAL_END_TO_END.md
- docs/runbooks/TRIAL_FUNNEL.md
- docs/runbooks/TRIAL_LIFECYCLE.md
- docs/security/TRIAL_AUTH.md
- docs/security/TRIAL_LIMITS.md
- ArchLucid.Api/Controllers/  (search for /v1/register, trial seat reservation, tenant provisioning)
- archlucid-ui/src/app/(marketing)/signup/page.tsx
- archlucid-ui/src/components/marketing/SignupForm.tsx
- ArchLucid.Application/  (search for TrialSeatReservationService and tenant provisioning)
- ArchLucid.Application/Pilots/PilotRunDeltaComputer.cs (used downstream for the dashboard panel)

Do this:
1. Trace the existing happy path step-by-step:
   form submit -> POST /v1/register -> trial seat reservation
   -> tenant + workspace provision -> sample-run seed
   -> first-run wizard -> first commit -> sponsor banner.
   Document each step with file paths, endpoints, and audit events
   in a NEW file docs/runbooks/TRIAL_FUNNEL_END_TO_END.md.
2. For each step that has a TODO / placeholder / feature flag, either
   implement the missing piece OR call it out as an owner-only blocker
   (Stripe live key, DNS, Front Door custom domain). Do not bypass
   feature flags that exist for safety.
3. Add a new Playwright spec
   archlucid-ui/playwright/tests/trial-funnel.spec.ts that runs the
   funnel against the deterministic mocks already used in the operator-
   journey smoke (see archlucid-ui/docs/TESTING_AND_TROUBLESHOOTING.md
   section 8). The spec must cover signup form -> tenant provisioned
   -> sample run visible -> first commit -> sponsor banner renders the
   "Day N since first commit" badge.
4. Wire baselineReviewCycleHours capture from the signup form into the
   tenant row (POST /v1/register already accepts it per the 2026-04-21
   CHANGELOG entry). Surface a "before vs measured" panel on the
   operator dashboard once one run has committed - reuse the
   ValueReportReviewCycleSectionFormatter pattern; new component name
   BeforeAfterDeltaPanel.tsx with Vitest tests.
5. Add an `archlucid trial smoke` CLI command that runs the funnel
   end-to-end in dev mode and prints PASS/FAIL per step
   (ArchLucid.Cli/Commands/TrialSmokeCommand.cs with paired
   ArchLucid.Cli.Tests/TrialSmokeCommandTests.cs).
6. Add a doc cross-link in docs/FIRST_30_MINUTES.md and
   docs/CLI_USAGE.md pointing at the new CLI command.

Stop and ask the user before:
- Switching the funnel from Stripe TEST mode to live mode
- Turning off the trial signup feature flag in production
- Setting any DNS record on archlucid.com or staging.archlucid.com

Exit criteria: end-to-end funnel works against staging in TEST mode;
Playwright spec green against deterministic mocks; CLI command shipped
+ unit tested; runbook reflects the real flow; BeforeAfterDeltaPanel
component lands with Vitest coverage.
```

---

## Prompt 3 — Proof-of-ROI: aggregate bulletin + soft-required baseline

**Owner gate.** Approving the bulletin publication cadence, signing each issue, and the privacy-notice update for the soft-required baseline are owner-only.

```
Goal: convert "ROI is modeled" into "ROI is measurable per-tenant and
publishable in aggregate without per-customer disclosure", so a buyer
sees real numbers before they sign.

Read first:
- docs/PILOT_ROI_MODEL.md
- docs/go-to-market/ROI_MODEL.md
- ArchLucid.Application/Pilots/PilotRunDeltaComputer.cs
- ArchLucid.Application/Pilots/PilotRunDeltas.cs
- ArchLucid.Application/Pilots/PilotRunDeltasResponseMapper.cs
- ArchLucid.Application/Pilots/FirstValueReportBuilder.cs
- ArchLucid.Persistence/Migrations/  (find the latest migration number;
  the next baseline-related migration becomes that + 1)
- archlucid-ui/src/components/marketing/SignupForm.tsx
- ArchLucid.Api/Controllers/  (find /v1/register controller)

Do this:
1. Flip baselineReviewCycleHours from optional to soft-required at
   trial signup:
   - Form: keep skippable but default the input value to "Use model
     default (modeled estimate)" with a tooltip explaining the
     trade-off; show an inline note that overriding produces a
     measured-vs-baseline curve.
   - API: change validation in /v1/register to log a counter
     archlucid_trial_signup_baseline_skipped_total when the field
     comes through empty. Do NOT enforce non-empty (UX harm > value).
   - Privacy notice: add a new Markdown stub
     docs/go-to-market/TRIAL_BASELINE_PRIVACY_NOTE.md describing how
     the baseline is used (delta computation, never published per-
     tenant). Cross-link from TRIAL_AND_SIGNUP.md.
2. Add a new operator-shell component
   archlucid-ui/src/components/BeforeAfterDeltaPanel.tsx that calls
   the existing PilotRunDeltaComputer surface and renders a small
   "before vs measured" card on /runs index for tenants with at least
   one committed run. Vitest coverage required.
3. Ship a quarterly aggregate ROI bulletin TEMPLATE under
   docs/go-to-market/AGGREGATE_ROI_BULLETIN_TEMPLATE.md with explicit
   minimum-N privacy guards: bulletin must aggregate >= 5 tenants
   with non-default baseline values; bulletin shows mean / p50 / p90
   only, never per-tenant rows. Include an explicit
   "owner-approval gate" section saying the bulletin cannot be
   published without owner sign-off per PENDING_QUESTIONS item 27.
4. Add a CLI command `archlucid roi-bulletin --quarter <Q-YYYY>
   --min-tenants 5 [--out <file>]` that emits a draft bulletin from
   production data when admin authority is present. Refuses to run
   when min-tenants threshold is not met (returns CliExitCode.UsageError).
   File: ArchLucid.Cli/Commands/RoiBulletinCommand.cs with paired
   ArchLucid.Cli.Tests/RoiBulletinCommandTests.cs.
5. Append PENDING_QUESTIONS items 27 and 28 with explicit "needed
   inputs" sub-bullets so the owner sees exactly what to approve.

Stop and ask the user before:
- Publishing the first aggregate ROI bulletin externally
- Changing the baseline field from soft-required to hard-required
- Reducing the minimum-N threshold below 5

Exit criteria: signup form ships the soft-required baseline UX;
BeforeAfterDeltaPanel renders on the operator dashboard; bulletin
template + CLI command land with tests; privacy note in place;
PENDING_QUESTIONS items 27/28 enriched with owner-needed inputs.
```

---

## Prompt 4 — Marketplace + Stripe live readiness (production-safety guards)

**Owner gate.** Setting any live Stripe key, Marketplace publisher ID, or production webhook secret. Pressing "Go live" in Partner Center.

```
Goal: get the commercial rails out of "designed" and into
"transactable", without the assistant ever holding a live key.

Read first:
- docs/go-to-market/MARKETPLACE_PUBLICATION.md
- docs/AZURE_MARKETPLACE_SAAS_OFFER.md
- docs/BILLING.md
- docs/go-to-market/STRIPE_CHECKOUT.md
- docs/go-to-market/PRICING_PHILOSOPHY.md
- docs/runbooks/MARKETING_STRIPE_GA.md
- docs/runbooks/MARKETPLACE_CHANGEPLAN_QUANTITY_ROLLBACK.md
- docs/PENDING_QUESTIONS.md  (items 8, 9, 22)
- ArchLucid.Api/Controllers/Billing/BillingStripeWebhookController.cs
- ArchLucid.Api/Controllers/Billing/BillingMarketplaceWebhookController.cs
- ArchLucid.Api/Controllers/Billing/BillingCheckoutController.cs
- ArchLucid.Host.Composition/Startup/  (find ArchLucidConfigurationRules
  for the existing CollectProductionSafetyErrors pattern to mirror)

Do this:
1. Verify alignment between PRICING_PHILOSOPHY tiers (Team /
   Professional / Enterprise) and the Marketplace plan SKUs called
   out in MARKETPLACE_PUBLICATION.md and AZURE_MARKETPLACE_SAAS_OFFER.md.
   If any drift, reconcile in PRICING_PHILOSOPHY.md (single source
   of truth) and add scripts/ci/assert_marketplace_pricing_alignment.py
   plus paired tests under scripts/ci/test_assert_marketplace_pricing_alignment.py.
2. Add BillingProductionSafetyRules in
   ArchLucid.Host.Composition/Startup/Validation/ that fails
   ASPNETCORE_ENVIRONMENT=Production startup when:
   - Stripe live key prefix `sk_live_` is configured WITHOUT a Stripe
     webhook secret, OR
   - Marketplace landing page URL is empty or contains a localhost
     host, OR
   - Billing:AzureMarketplace:GaEnabled=true but no MarketplaceOffer ID
     is configured.
   Pattern: same shape as ArchLucidConfigurationRules.CollectProductionSafetyErrors.
   Unit test under ArchLucid.Host.Composition.Tests/.
3. Add a Marketplace publication preflight CLI command
   `archlucid marketplace preflight` (ArchLucid.Cli/Commands/
   MarketplacePreflightCommand.cs) that runs the Partner Center
   checklist from MARKETPLACE_PUBLICATION.md and prints PASS/FAIL
   per item, with non-zero exit code on any FAIL. Tests under
   ArchLucid.Cli.Tests/.
4. Document the Stripe TEST staging path end-to-end in
   docs/go-to-market/STRIPE_CHECKOUT.md so staging.archlucid.com/signup
   can transact in Stripe TEST mode before live keys arrive. Include
   curl examples showing the test webhook event flow.
5. Update PENDING_QUESTIONS items 8, 9, 22 with explicit
   "needed inputs" sub-bullets enumerating exactly what you need
   from the owner (publisher ID, tax profile, payout account, live
   webhook secret) so the next session knows the unblocking shape.

Stop and ask the user before:
- Setting any live Stripe key, Marketplace publisher ID, or production
  webhook secret
- Pressing "Go live" in Partner Center
- Flipping Billing:AzureMarketplace:GaEnabled in any environment
  beyond what 2026-04-20 already shipped

Exit criteria: pricing alignment guard in CI; production-safety guard
in startup; preflight CLI implemented + unit-tested; staging Stripe
TEST flow documented end-to-end; PENDING_QUESTIONS enriched.
```

---

## Prompt 5 — Differentiability: side-by-side downloadable artifact pack on `/why`

```
Goal: turn the public /why marketing page from "comparison table" into
"downloadable proof artifact" so a buyer can self-qualify without a
sales call.

Read first:
- docs/go-to-market/COMPETITIVE_LANDSCAPE.md
- docs/go-to-market/POSITIONING.md
- docs/EXECUTIVE_SPONSOR_BRIEF.md
- docs/adr/0027-demo-preview-cached-anonymous-commit-page.md
- docs/DEMO_PREVIEW.md
- archlucid-ui/src/app/(marketing)/why/page.tsx
- archlucid-ui/src/marketing/why-archlucid-comparison.ts
- archlucid-ui/src/app/(marketing)/why/WhyArchlucidMarketingView.tsx
- ArchLucid.Application/Pilots/MarkdownPdfRenderer.cs
- ArchLucid.Host.Core/Demo/DemoReadModelClient.cs
- ArchLucid.Api/Controllers/Demo/  (look for DemoExplainController)

Do this:
1. Add a new endpoint GET /v1/marketing/why-archlucid-pack.pdf
   (anonymous, gated by FeatureGateKey.DemoEnabled — same
   404-not-403 pattern as DemoExplainController) that returns a
   single PDF bundling:
   - One full ArchLucid run package: manifest summary + decision-trace
     excerpt + comparison-delta sample + citations, all sourced from
     the cached anonymous /demo/preview data so it is deterministic
     and never leaks tenant data.
   - A side-by-side scaffold of what an incumbent (LeanIX / Ardoq /
     MEGA HOPEX) would produce for the same input, with every claim
     citing COMPETITIVE_LANDSCAPE.md by section anchor. NO uncited
     competitive claims.
   - Header text "demo tenant — replace before publishing" on every
     ArchLucid-side panel.
   Implementation reuses MarkdownPdfRenderer + a new
   WhyArchLucidPackBuilder under ArchLucid.Application/Pilots/.
2. Extend archlucid-ui/src/app/(marketing)/why/WhyArchlucidMarketingView.tsx
   with a "Download the side-by-side proof pack (PDF)" button calling
   the new endpoint. Vitest snapshot.
3. Broaden the existing citation seam test so it fails when ANY row in
   why-archlucid-comparison.ts loses its citation footnote, including
   any new rows added for the side-by-side artifact.
4. Add the /why route to the existing axe Playwright a11y gate.
5. Update docs/go-to-market/POSITIONING.md § 4 to point at the new
   downloadable pack URL.
6. Add new pending-question item: "Should the comparison ship as PDF
   download only, inline page section only, or both?" (new item 31).

Stop and ask the user before:
- Adding any direct competitive claim that does not appear in
  COMPETITIVE_LANDSCAPE.md with a public-source citation

Exit criteria: PDF endpoint shipped + integration test green; /why
page renders the download button; citation seam test broadened;
axe-clean; PENDING_QUESTIONS item 31 added.
```

---

## Prompt 6 — Trustworthiness: pen test summary publication scaffold + PGP key CI guard

**Owner gate.** Marking the redacted summary as `Published` (requires assessor delivery). Generating the PGP key pair (security custodian).

```
Goal: turn the awarded Aeronova pen test SoW into a ready-to-publish
scaffold and stand up a CI guard that goes green automatically the
moment the security@archlucid.dev PGP key arrives.

Read first:
- docs/security/pen-test-summaries/2026-Q2-SOW.md
- docs/security/pen-test-summaries/2026-Q2-REDACTED-SUMMARY.md
- docs/security/PEN_TEST_REDACTED_SUMMARY_TEMPLATE.md
- docs/go-to-market/TRUST_CENTER.md
- docs/PENDING_QUESTIONS.md  (items 2, 10, 20, 21)
- archlucid-ui/public/.well-known/  (security.txt exists; pgp-key.txt does not)
- ArchLucid.Api/Controllers/  (find /v1/admin/security-trust/publications)
- SECURITY.md  (still has the PGP TODO line)

Do this:
1. Build a redacted-summary skeleton inside
   docs/security/pen-test-summaries/2026-Q2-REDACTED-SUMMARY.md that
   matches PEN_TEST_REDACTED_SUMMARY_TEMPLATE.md exactly, with TODO
   markers for assessor narrative tables. Do NOT invent findings.
   Mark the document as Status: "Awaiting assessor delivery — DO NOT
   PUBLISH" at the top.
2. Wire the Trust Center page so the SecurityAssessmentPublished badge
   renders automatically once POST /v1/admin/security-trust/publications
   is called with the published date. If the endpoint already exists,
   verify the wire-up; if not, scaffold a minimal handler in
   ArchLucid.Api/Controllers/Admin/SecurityTrustPublicationsController.cs
   with AdminAuthority policy and Schemathesis coverage.
3. Add a CLI subcommand
   `archlucid security-trust publish --kind pen-test --date <YYYY-MM-DD>
   --summary-url <URL>` that calls the endpoint and prints the resulting
   badge URL (ArchLucid.Cli/Commands/SecurityTrustPublishCommand.cs;
   tests under ArchLucid.Cli.Tests/).
4. Add CI guard scripts/ci/assert_pgp_key_present.py that fails if
   archlucid-ui/public/.well-known/pgp-key.txt is missing AND the Trust
   Center references PGP. Mark continue-on-error: true today; add a
   note in the workflow file that the moment the key file is added,
   the operator can flip continue-on-error to false. Add paired
   scripts/ci/test_assert_pgp_key_present.py (cover present /
   missing-but-trust-center-mentions / missing-and-trust-center-silent
   cases).
5. Update SECURITY.md to remove the PGP TODO line and replace with
   a "PGP key available at /.well-known/pgp-key.txt (publication
   pending — see docs/PENDING_QUESTIONS.md item 21)" sentence.
6. Add a new pending-question sub-bullet asking the security custodian
   to confirm the canonical email address for the key
   (security@archlucid.dev vs security@archlucid.com — the .dev TLD
   appears in some docs but not all).

Stop and ask the user before:
- Marking the redacted summary as published (requires assessor delivery)
- Generating the PGP key pair (must be done by the security custodian)
- Flipping the assert_pgp_key_present.py guard from continue-on-error
  to merge-blocking

Exit criteria: redacted-summary skeleton in place; CLI command shipped
+ unit-tested; CI guard added (advisory); Trust Center page reads
cleanly even before publication; PENDING_QUESTIONS items 2/10/20/21
enriched with custodian sub-question.
```

---

## Prompt 7 — Microsoft Teams notification connector

**Owner gate.** Choosing notification-only vs two-way (approve governance from Teams). Two-way needs a registered Teams app manifest in M365 admin.

```
Goal: ship the next workflow-embeddedness anchor after the GitHub
Action and Azure DevOps task. Target: Teams notification on run
commit, governance approval requested, and alert raised.

Read first:
- docs/INTEGRATION_EVENTS_AND_WEBHOOKS.md
- schemas/integration-events/catalog.json
- docs/contracts/archlucid-asyncapi-2.6.yaml
- docs/adr/0019-logic-apps-standard-edge-orchestration.md
- infra/terraform-logicapps/  (existing five workflow templates - mirror layout)
- integrations/github-action-manifest-delta/  (mirror reusable script layout)
- integrations/azure-devops-task-manifest-delta/
- docs/PENDING_QUESTIONS.md  (items 11, 23)

Do this:
1. Add a new Logic Apps Standard workflow template under
   infra/terraform-logicapps/workflows/teams-notifications/ that
   subscribes to Service Bus topics for run.committed,
   governance.approval.requested, and alert.raised. Render to a Teams
   adaptive card via Incoming Webhook. Module follows the same
   variables.tf / main.tf pattern as the existing five.
2. Add a per-tenant configuration surface
   archlucid-ui/src/app/(operator)/integrations/teams/page.tsx and a
   POST /v1/integrations/teams/connections endpoint that stores the
   webhook URL via Key Vault references (not raw URLs in SQL).
   Encryption posture: same as existing webhook delivery secrets.
3. Add operator UI shaping: the integrations/teams page is in
   Enterprise Controls tier; gate behind ExecuteAuthority for write,
   ReadAuthority for view. Add to nav-config.ts and the cross-module
   Vitest seam tests (authority-seam-regression.test.ts,
   authority-execute-floor-regression.test.ts).
4. Add a Schemathesis contract test for the new endpoints
   (POST/GET/DELETE /v1/integrations/teams/connections). Refresh
   the OpenAPI v1 snapshot.
5. Document the connector at
   docs/integrations/MICROSOFT_TEAMS_NOTIFICATIONS.md with curl
   examples + a screenshot stub. Add a row to
   docs/go-to-market/INTEGRATION_CATALOG.md.
6. Add a new pending-question (item 32) asking which trigger set the
   owner wants beyond the three above (e.g. compliance.drift.escalated,
   seat.reservation.released).

Stop and ask the user before:
- Choosing notification-only vs two-way (approve governance from Teams)
- Adding any per-channel configuration that requires a Teams app
  manifest registered in Microsoft 365 admin
- Storing any real Teams webhook URL outside Key Vault references

Exit criteria: Logic Apps workflow Terraform module passes
`terraform validate` in the pilot stack; operator UI page shaped
and tested; endpoints contract-tested; integration catalog updated;
PENDING_QUESTIONS items 11/23/32 enriched.
```

---

## Prompt 8 — Golden cohort drift report with locked baseline SHAs

**Owner gate.** Provisioning the dedicated Azure OpenAI deployment for the optional real-LLM cohort run (item 15/25 — budget approval). Publishing per-tenant feedback aggregates externally.

```
Goal: convert structural correctness into engine-quality correctness
by locking baseline manifest SHAs from a single approved simulator
run, then flipping the nightly cohort workflow from "contract test
only" to "publish a real drift report".

Read first:
- tests/golden-cohort/cohort.json
- tests/golden-cohort/README.md
- .github/workflows/golden-cohort-nightly.yml
- docs/MUTATION_TESTING_STRYKER.md
- docs/RUNBOOK_REPLAY_DRIFT.md
- docs/runbooks/LLM_PROMPT_REDACTION.md
- ArchLucid.AgentSimulator/  (deterministic simulator path)
- ArchLucid.Coordinator/  (find the canonical-SHA helper used for golden manifests)
- ArchLucid.Application/  (find the application service that drives a single run end-to-end)
- ArchLucid.Decisioning/Validation/SchemaValidationService.cs
- ArchLucid.Application/  (find FindingEvidenceChainService for the explain panel)
- archlucid-ui/src/components/RunAgentForensicsSection.tsx
- archlucid-ui/src/components/RunTraceViewerLink.tsx
- docs/PENDING_QUESTIONS.md  (item 15 — owner-only LLM budget)

Do this:
1. Add a one-shot CLI command
   `archlucid golden-cohort lock-baseline [--cohort tests/golden-cohort/cohort.json]
   [--write]` (ArchLucid.Cli/Commands/GoldenCohortLockBaselineCommand.cs)
   that:
   - Iterates every cohort item.
   - Runs each through the SIMULATOR path end-to-end (request -> execute
     -> commit), capturing the committed manifest's canonical SHA-256.
   - When --write is set, writes the SHAs back into cohort.json's
     expectedCommittedManifestSha256 fields.
   - Refuses to run when a real-LLM execution mode is configured
     (returns CliExitCode.UsageError with a clear message).
   Tests under ArchLucid.Cli.Tests/.
2. Extend the GoldenCohortContractTests in
   ArchLucid.Application.Tests/ from "JSON contract test" to
   "manifest drift report":
   - When SHAs are zero (current placeholder state), assert contract
     only and emit a clear "baseline not yet locked - run `archlucid
     golden-cohort lock-baseline --write`" message.
   - When SHAs are non-zero, run each cohort item through the simulator
     and assert SHA equality + finding-category set equality. Diff is
     reported as Markdown.
3. Extend .github/workflows/golden-cohort-nightly.yml to:
   - Run the contract test first (existing behavior).
   - When ARCHLUCID_GOLDEN_COHORT_BASELINE_LOCKED=true is set on the
     workflow run, run the drift assertion.
   - Publish a Markdown drift report to
     docs/quality/golden-cohort-drift-latest.md (overwriting per run);
     archive previous report under docs/quality/archive/<YYYY-MM-DD>.md.
   - Real-LLM run remains gated on
     ARCHLUCID_GOLDEN_COHORT_REAL_LLM=true (item 15 — budget approval).
4. Add a per-finding "Explain this" panel in the operator UI:
   archlucid-ui/src/components/FindingExplainPanel.tsx that shows
   - the agent prompt (already redacted via LlmPromptRedaction)
   - the LLM completion
   - the supporting evidence pieces from the decision trace via
     FindingEvidenceChainService
   Vitest tests required; gated behind ReadAuthority. Wire into
   /runs/[runId]/findings/[findingId] route.
5. Add new pending-question item 33: "Approve baseline SHA lock from
   single simulator run today, OR wait for product reviewer to approve
   the cohort scenario list before locking?"

Stop and ask the user before:
- Provisioning the dedicated Azure OpenAI deployment used by the
  optional nightly real-LLM run (budget approval — pending question 15)
- Publishing per-tenant feedback aggregates externally
- Locking baseline SHAs in cohort.json without explicit owner approval
  via PENDING_QUESTIONS item 33

Exit criteria: lock-baseline CLI command shipped + tested; drift
contract test extended; nightly workflow extended; explain panel
component lands with Vitest coverage; PENDING_QUESTIONS items 15/25/33
enriched.
```

---

## How to use these prompts

- **One prompt per session.** Paste the whole block into a fresh Cursor agent so it starts from a clean context.
- **Honor stop-and-ask boundaries.** The assistant should not cross those without owner input.
- **Track resolution in `PENDING_QUESTIONS.md`.** Each prompt enriches the relevant items so the next session sees the open shape.
- **Order matters only loosely.** Prompts 1, 2, and 6 unblock owner-only work; the rest are independent.
- **Re-run-safe.** Each prompt's exit criteria are idempotent; running the same prompt twice in different sessions should converge on the same end state.
