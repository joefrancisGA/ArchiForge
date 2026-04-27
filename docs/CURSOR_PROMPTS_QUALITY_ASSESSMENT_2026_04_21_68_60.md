> **Scope:** Eight paste-ready Cursor prompts for the largest-impact improvements in [`QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md`](QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md). Each prompt is self-contained — it assumes the assistant starts from a clean session with no memory of the assessment.

> **Spine doc:** [Five-document onboarding spine](FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.

>
> **Follow-on prompts (same assessment, §1.9–1.18):** [`CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_68_60_ADDITIONAL.md`](archive/quality/2026-04-23-doc-depth-reorg/CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_68_60_ADDITIONAL.md) — eight more paste-ready blocks (Prompts **A–H**).

# Cursor prompts — top-8 improvements after the 68.60% assessment (2026-04-21)

**How to use.** One prompt per session. Paste the whole block (between the triple backticks) into a fresh Cursor agent. Each prompt names its **stop-and-ask** boundaries — the assistant should not cross those without owner input. After each prompt completes, update [`docs/PENDING_QUESTIONS.md`](PENDING_QUESTIONS.md) accordingly.

**DEFERRED markers.** A prompt is marked `[DEFERRED]` in the heading when the assistant cannot complete **at least part** of it without owner input that has not yet been received. None of the eight below are fully `DEFERRED` — every prompt has substantive work the assistant can land today.

> **Update 2026-04-23.** **Prompt 1** ("Reference customer publication scaffolding (PLG row)") was **removed** because the underlying improvement was deferred to V1.1 by owner decision (see [`QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md`](QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md) §0.2). Its slot is now filled by **Prompt 1 (replacement) — Quarterly board-pack PDF endpoint + monthly digest preset**, sourced from §1.10's standing recommendation.

> **Update 2026-04-23 (second deferral, same day).** **Prompt 4** ("Marketplace + Stripe live readiness — production-safety guards") was **removed** because the underlying improvement was deferred to V1.1 by owner decision (see [`QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md`](QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md) §0.3 — commerce un-hold). Its slot is now filled by **Prompt 4 (replacement) — Governance dry-run / what-if mode for policy threshold changes**, sourced from §1.22's standing recommendation. **Prompt 2** (trial signup funnel TEST-mode) is unchanged and stays a live V1 obligation — only its "flip TEST → live" gate is V1.1-deferred. Prompts 3, 5, 6, 7, 8 are unchanged. The actionable count remains 8.

> **SaaS audience guard (read before running any prompt below).** ArchLucid is a **SaaS** product. **Customers, evaluators, and sponsors never install Docker, SQL, .NET, Node, or Terraform.** They only ever interact with the public website (`archlucid.net`), the in-product operator UI (after sign-in), and the Azure portal for their own subscription identity / billing. When any prompt below produces customer-facing copy (signup flow, marketing routes, pricing, trust center, sponsor brief, value report, reference case study, evidence pack, ROI bulletin, board pack, operator UI text), it **must not** assume the customer runs Docker, opens a terminal, runs `archlucid try`, or applies Terraform. Tooling like `apply-saas.ps1`, `archlucid try`, `dev up`, `docker compose`, the `.devcontainer/`, and `INSTALL_ORDER.md` is **internal ArchLucid contributor / operator** tooling — fine to reference in **engineer-facing** docs, never on a buyer surface. If a prompt seems to require a customer-side install step, **stop and ask the user** rather than inventing one. See `docs/QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md` §0.1 for the full SaaS-framing addendum.

---

## Prompt 1 (replacement, added 2026-04-23) — Quarterly board-pack PDF endpoint + monthly exec-digest cadence

**Why this replaced the original Prompt 1.** The original Prompt 1 (Reference customer publication scaffolding) was deferred to V1.1 on 2026-04-23. This replacement delivers the same single-slot exec-visibility leverage without depending on owner-only events. Sources: assessment §1.10 standing recommendation; assessment §3 Improvement 9.

**Owner-decided 2026-04-23.** Cover narrative ships as the literal placeholder `<<sponsor cover narrative — owner approval before external use>>` (owner approves before any external use). Monthly cadence is **opt-out** for NEW tenants — every newly provisioned tenant defaults to Monthly; existing tenants stay 'Weekly' via the three-step migration shape in step 1 (no retroactive cadence change without owner consent).

```
Goal: stitch the existing exec-digest, value-report, and
PilotRunDeltaComputer machinery into ONE PDF a sponsor can take
into a quarterly budget review. Add a parallel monthly cadence
preset for the existing exec-digest. Do NOT invent customer names,
do NOT publish anything externally, and do NOT retroactively change
the cadence of any EXISTING tenant (they all stay 'Weekly' until
they actively change it). NEW tenants default to 'Monthly' per
owner decision 2026-04-23 q36.

Read first:
- docs/QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md  (sections 1.10 and 3 Improvement 9)
- docs/EXECUTIVE_SPONSOR_BRIEF.md  (placeholder source for cover narrative)
- docs/library/PILOT_ROI_MODEL.md
- docs/go-to-market/ROI_MODEL.md
- ArchLucid.Application/Notifications/ExecDigestComposer.cs
- ArchLucid.Application/Notifications/ExecDigestWeeklyHostedService.cs
- ArchLucid.Application/Pilots/PilotRunDeltaComputer.cs
- ArchLucid.Persistence/Migrations/103_TenantExecDigestPreferences.sql  (look at the schema, then build migration 104 alongside it)
- ArchLucid.Api/Controllers/PilotsController.cs  (host the new POST endpoint here, alongside the existing first-value-report endpoint)
- archlucid-ui/src/app/(operator)/settings/exec-digest/page.tsx
- docs/library/AUDIT_COVERAGE_MATRIX.md  (add the new audit constant if state-changing)
- docs/library/OPERATOR_ATLAS.md  (add the new endpoint to the atlas)

Do this:
1. Add SQL migration 104 alongside 103. Per owner decision 2026-04-23
   the NEW-tenant default is 'Monthly' (opt-out), but existing tenants
   MUST stay 'Weekly' — they did not consent to a cadence change. Use
   this three-step shape so SQL Server does NOT silently flip existing
   rows to 'Monthly' via the ADD-NOT-NULL-DEFAULT backfill behaviour:
     -- Step A: add column with backfill default 'Weekly'
     ALTER TABLE dbo.TenantExecDigestPreferences
       ADD CadenceCode NVARCHAR(16) NOT NULL
         CONSTRAINT DF_TenantExecDigestPreferences_CadenceCode_Backfill DEFAULT N'Weekly';
     -- Step B: drop the backfill constraint
     ALTER TABLE dbo.TenantExecDigestPreferences
       DROP CONSTRAINT DF_TenantExecDigestPreferences_CadenceCode_Backfill;
     -- Step C: add the forward-looking new-row default 'Monthly'
     ALTER TABLE dbo.TenantExecDigestPreferences
       ADD CONSTRAINT DF_TenantExecDigestPreferences_CadenceCode DEFAULT N'Monthly' FOR CadenceCode;
   Plus the matching Rollback/R104_*.sql script (drops the new
   constraint, then drops the column). Add a unit test that:
     (a) inserts a synthetic existing tenant row PRIOR to migration
         and asserts CadenceCode == 'Weekly' AFTER migration,
     (b) inserts a NEW tenant AFTER migration with no CadenceCode
         specified and asserts CadenceCode == 'Monthly'.
2. Extend ExecDigestComposer to honour CadenceCode in {Weekly, Monthly}.
   Idempotency key for monthly: 'exec-digest:{tenantId}:{iso-year}-{iso-month}'.
   Add a second hosted service ExecDigestMonthlyHostedService that
   wakes once per day at 06:05 UTC and emits for tenants whose
   tenant-local date is the 1st of the month. Reuse the existing
   IANA-tz preference resolution helper.
3. Add a new endpoint:
     POST /v1/pilots/board-pack.pdf?quarter=Q3-2026
   gated [Authorize(Policy = "ExecuteAuthority")]. Body produces a
   single PDF binding (a) the four most recent weekly exec-digest
   snapshots within the quarter, (b) the highest-impact committed
   manifest's value-report rendered to PDF, (c) the per-tenant
   PilotRunDeltaComputer summary across the quarter, (d) a one-page
   sponsor cover narrative driven by EXECUTIVE_SPONSOR_BRIEF.md
   placeholders. Cover narrative ships as the literal text:
     "<<sponsor cover narrative — owner approval before external use>>"
   Do NOT invent customer-shareable cover prose.
4. Add CLI command: `archlucid board-pack --tenant <id> --quarter Q3-2026 --out board-pack-Q3-2026.pdf`
   that calls the new endpoint with an API key and writes the PDF to disk.
5. Add a "Generate board pack" button to /settings/exec-digest that
   opens a quarter-picker modal, calls the endpoint, and downloads
   the result. Do NOT promote board-pack to a primary nav slot.
6. Add a new audit constant AuditEventTypes.BoardPackGenerated (if
   adding state-changing audit) plus the AUDIT_COVERAGE_MATRIX row;
   bump the audit-core-const-count snapshot accordingly.
7. Tests:
   - Application unit tests for ExecDigestComposer monthly cadence and
     CadenceCode resolution.
   - Application unit tests for the board-pack composer (synthetic
     committed manifest + delta + cover placeholder).
   - Api integration test (Suite=Core, GreenfieldSqlApiFactory) calling
     POST /v1/pilots/board-pack.pdf and asserting (a) ExecuteAuthority
     gate, (b) PDF MIME type, (c) cover placeholder string is present
     in the PDF text layer.
   - Vitest spec for the /settings/exec-digest button that mocks the
     endpoint and asserts the download trigger.
   - Schemathesis contract test for the new endpoint.
8. Docs:
   - New docs/library/BOARD_PACK.md (audience: operators with ExecuteAuthority).
   - One-line pointer in EXECUTIVE_SPONSOR_BRIEF.md and
     OPERATOR_ATLAS.md.
   - New CHANGELOG entry under 2026-04-23 with the standard format.
   - Update docs/library/V1_RELEASE_CHECKLIST.md with a single new row
     for the board-pack endpoint smoke.
9. Pending-question items 35 and 36 in docs/PENDING_QUESTIONS.md were
   resolved 2026-04-23: cover narrative is the literal placeholder
   string (owner approves before external use); monthly cadence is
   the opt-out default for NEW tenants only. Reference the resolution
   in the new docs/library/BOARD_PACK.md page so future readers see
   why the migration uses the three-step backfill shape.

Stop and ask the user before:
- Replacing the cover narrative placeholder with any non-placeholder copy.
- Skipping the three-step migration shape (Steps A/B/C above) — using
  a single ADD … NOT NULL DEFAULT 'Monthly' would silently flip every
  existing tenant to Monthly, which violates the owner-confirmed
  "existing tenants stay Weekly" boundary.
- Sending the board-pack to any external email address (out of scope for
  this prompt — the endpoint produces the PDF and returns it; delivery
  is a separate workflow).

Exit criteria: PR opens with migration 104 + rollback, monthly hosted
service wired and unit-tested, POST /v1/pilots/board-pack.pdf returning
a valid PDF behind ExecuteAuthority, CLI command working in dev,
operator-UI button visible only to ExecuteAuthority and downloading
the PDF on click, all tests green, new audit constant if appropriate,
docs landed including CHANGELOG entry. Default cadence for NEW tenants
is 'Monthly' (opt-out, owner decision 2026-04-23 q36); existing
tenants stay 'Weekly' via the three-step migration above. Cover
narrative is the literal placeholder string per owner decision q35.
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
- Setting any DNS record on archlucid.net or staging.archlucid.net

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

## Prompt 4 (replacement, added 2026-04-23) — Governance dry-run (what-if) mode for policy threshold changes

**Why this replaced the original Prompt 4.** The original Prompt 4 (Marketplace + Stripe live readiness) was deferred to V1.1 on 2026-04-23 (see assessment §0.3). This replacement delivers the same single-slot enterprise-governance leverage without depending on owner-only events (Partner Center seller verification, Stripe live keys, etc.). Sources: assessment §1.22 standing recommendation; assessment §3 Improvement 10. The `BillingProductionSafetyRules` startup gate, `archlucid marketplace preflight` CLI, and `assert_marketplace_pricing_alignment.py` from the original Prompt 4 are **already shipped in V1** and stay in V1 — they do not need re-shipping.

**Owner-decided 2026-04-23.** Audit marker captures override count **and** the proposed-override payload (full forensic visibility — owner accepted the trade-off that anyone with audit-log read access in the same tenant can see proposed policy intent). Pagination cap stays at the assistant default of 20-default / 100-max.

```
Goal: ship a governance dry-run / what-if mode so operators can
score a candidate manifest against a proposed policy-threshold
change WITHOUT writing the audit trail, blocking commit, or
mutating any persisted policy. This is the missing "what would
happen if I tightened this threshold?" workflow.

Read first:
- docs/QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md  (sections 1.22 and 3 Improvement 10)
- docs/library/AUDIT_COVERAGE_MATRIX.md
- docs/library/PRODUCT_PACKAGING.md  (governance / Operate tier)
- docs/library/OPERATOR_ATLAS.md
- ArchLucid.Application-Governance/  (find GovernanceWorkflowService and the policy-pack evaluation path)
- ArchLucid.Application-Governance/Policies/  (look for the pack-evaluation entry point)
- ArchLucid.Domain-Governance/Policies/  (look for the threshold abstraction)
- ArchLucid.Persistence-Coordination/Governance/  (look for the policy-pack repository to confirm the dry-run path does NOT need a write)
- ArchLucid.Api/Controllers/Governance/  (host the new POST endpoint here)
- archlucid-ui/src/app/(operator)/governance/policy-packs/[packId]/page.tsx
- archlucid-ui/src/lib/nav/nav-config.ts
- ArchLucid.Tests.Authorization/  (mirror existing seam tests for the new endpoint)
- scripts/ci/audit_core_const_count_snapshot.txt  (will need a +1 bump)

Do this:
1. Add a domain marker DryRunPolicyOverrideContext in
   ArchLucid.Domain-Governance/Policies/. Carries an immutable
   IReadOnlyDictionary<string, decimal> of proposed threshold
   overrides (e.g., "findings.severity.criticalThreshold" -> 80m)
   plus a strongly-typed [NotForCommit] marker attribute. The
   persistence layer policy-pack repository MUST reject any
   write call where the context is in scope (assert this in a
   unit test).
2. Extend the policy-pack evaluation path to honour the dry-run
   context: when present, the threshold dictionary takes precedence
   over the persisted policy values for the duration of the call.
   Do NOT mutate the persisted policy. Add an Application-layer
   service GovernanceDryRunService that takes a manifest reference
   plus an override dictionary and returns:
     - current evaluation (findings allowed / warned / blocked)
     - proposed evaluation
     - delta of would-be-blocked findings
     - delta of would-be-warned findings
     - one-line sponsor summary string
3. Add a new endpoint:
     POST /v1/governance/dry-run
   gated [Authorize(Policy = "ExecuteAuthority")]. Body:
     { manifestRef: { runId, manifestSha256 } | { committedManifestId },
       proposedOverrides: { "findings.severity.criticalThreshold": 80, ... } }
   Returns the GovernanceDryRunService result. Do NOT write
   anything to dbo.AuditEvents for the *evaluation itself* - only
   write a single GovernanceDryRunRequested marker capturing
   { tenantId, requesterId, requestedUtc, manifestRef, overrideCount,
     overridePayloadJson }.
   Per owner decision 2026-04-23 the payload IS persisted (full
   forensic visibility). Acknowledge this in the marker's audit-matrix
   row: anyone with ReadAuditAuthority in the same tenant can see the
   proposed policy values. Apply the standard PII redaction pipeline
   (LlmPromptRedaction-style) before serialising overridePayloadJson
   so customer secrets accidentally pasted into a threshold field
   never land in the audit log.
4. Add a new audit constant
   AuditEventTypes.GovernanceDryRunRequested in
   ArchLucid.Domain/Auditing/AuditEventTypes.cs and a row in
   docs/library/AUDIT_COVERAGE_MATRIX.md. Bump
   scripts/ci/audit_core_const_count_snapshot.txt by 1.
5. Add the CLI command:
     archlucid governance dry-run --manifest <id> --overrides overrides.json --out report.md
   in ArchLucid.Cli/Commands/GovernanceDryRunCommand.cs. The
   Markdown report must include all five sections from step 2.
6. Operator UI: add a "Dry-run threshold change" button to
   /governance/policy-packs/<packId> that opens a modal:
     - left pane: editable threshold list seeded from the live pack
     - right pane: live preview of the affected committed manifests
       (most recent 20 by default; "Load more" up to 100)
     - footer: "Run dry-run" button calling the endpoint and
       rendering the result inline; no commit affordance in this modal
   Shape on Operate (governance and trust) tier: ExecuteAuthority
   for write (the dry-run call), ReadAuthority for view. Update
   nav-config.ts and add the seam test alongside existing
   authority-* regression specs.
7. Tests:
   - Application unit tests for GovernanceDryRunService coverage of
     the evaluation delta calculator (single-threshold change,
     multi-threshold change, no-op change, no-affected-findings).
   - Unit test asserting the persistence-layer rejection of any
     write attempted while DryRunPolicyOverrideContext is in scope.
   - Api integration test (Suite=Core, GreenfieldSqlApiFactory)
     calling POST /v1/governance/dry-run and asserting:
     (a) ExecuteAuthority gate
     (b) zero new rows in dbo.AuditEvents for the evaluation itself
     (c) exactly one new row of type GovernanceDryRunRequested
         carrying both overrideCount AND overridePayloadJson, with
         overridePayloadJson confirmed to have passed through the
         redaction pipeline (assert via the existing redaction-marker
         test pattern)
     (d) zero new rows in the policy-pack mutation tables.
   - Schemathesis contract test for the new endpoint.
   - Vitest spec for the modal mocking the endpoint (assert
     ExecuteAuthority gating, asserts default pagination = 20).
8. Docs:
   - New docs/library/GOVERNANCE_DRY_RUN.md (audience: operators
     with ExecuteAuthority, governance/audit reviewers).
   - One-line pointer in OPERATOR_ATLAS.md, library/PRODUCT_PACKAGING.md,
     and the EXECUTIVE_SPONSOR_BRIEF.md.
   - New CHANGELOG entry under 2026-04-23 with the standard format.
9. Pending-question items 37 and 38 in docs/PENDING_QUESTIONS.md were
   resolved 2026-04-23: capture count + redacted payload, pagination
   20-default / 100-max. Reference the resolution in the new
   docs/library/GOVERNANCE_DRY_RUN.md page so future readers see why.

Stop and ask the user before:
- Skipping the redaction pipeline on overridePayloadJson (the payload-
  capture decision is conditional on redaction being in place).
- Raising the pagination cap above 100 (assistant ships 20-default /
  100-max as the owner-confirmed default).
- Adding a "Commit this threshold change" affordance inside the
  dry-run modal (out of scope for this prompt - the modal is
  read-only for policy state).

Exit criteria: PR opens with the new domain marker + Application
service, POST /v1/governance/dry-run behind ExecuteAuthority writing
exactly one GovernanceDryRunRequested marker per call (count + redacted
payload, zero evaluation audit rows), CLI command working in dev,
operator-UI modal visible only to ExecuteAuthority and rendering the
dry-run result inline, all tests green, audit constant + matrix row +
count-snapshot bumped, docs landed including CHANGELOG entry.
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
   (security@archlucid.dev vs security@archlucid.net — the .dev TLD
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
   Operate (governance and trust) tier; gate behind ExecuteAuthority for write,
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
- **More prompts:** eight assessment-aligned follow-ons (strangler CI, board-pack PDF, task telemetry, pricing quote, compliance journey page, procurement ZIP, traceability ZIP, game day) live in [`CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_68_60_ADDITIONAL.md`](archive/quality/2026-04-23-doc-depth-reorg/CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_68_60_ADDITIONAL.md).
