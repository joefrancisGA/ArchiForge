> **Scope:** Self-serve trial funnel — end-to-end map (signup → tenant → sample run → first commit → sponsor banner) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Trial funnel — end-to-end (signup → first commit → sponsor banner)

**Audience:** engineers, product, and onboarding owners who need a single map of what happens between a prospect typing their email on `/signup` and the operator dashboard showing the **Day N since first commit** badge with a **before vs measured** review-cycle delta.

**Companion docs (do not duplicate them here):**

- [`docs/go-to-market/TRIAL_AND_SIGNUP.md`](../go-to-market/TRIAL_AND_SIGNUP.md) — product design of the trial.
- [`docs/runbooks/TRIAL_END_TO_END.md`](TRIAL_END_TO_END.md) — live Playwright + harness acceptance with **real SQL** and the Noop checkout activator.
- [`docs/runbooks/TRIAL_FUNNEL.md`](TRIAL_FUNNEL.md) — Prometheus + Grafana **observability** runbook.
- [`docs/runbooks/TRIAL_LIFECYCLE.md`](TRIAL_LIFECYCLE.md) — what happens **after** day 14.
- [`docs/security/TRIAL_AUTH.md`](../security/TRIAL_AUTH.md) and [`docs/security/TRIAL_LIMITS.md`](../security/TRIAL_LIMITS.md) — auth + write-gate boundaries.

This document is the **operational map** that ties those together for someone debugging the funnel locally or in staging.

---

## 1. Objective

Document the **single happy path** a prospect takes through the funnel, with **file paths**, **HTTP endpoints**, and **durable audit events** for each step. Anyone reading this should be able to stand up the funnel against staging in **Stripe TEST mode**, watch each step succeed, and know exactly which lines of code to look at when one step fails.

---

## 2. Assumptions

- Funnel is exercised **either** locally (`dotnet run --project ArchLucid.Api` + `npm run dev`) **or** against staging (`https://staging.archlucid.net`).
- `Billing:Provider=Noop` (local) **or** `Billing:Provider=Stripe` with **TEST** keys (staging). Live keys are an **owner-only** decision (see § 7).
- `Auth:Trial:Modes=LocalIdentity` (or `MsaExternalId` when External ID is configured).
- Operator UI has access to the API via the same-origin proxy (`/api/proxy/v1/...`).

---

## 3. Constraints

- **Do not** bypass feature flags that exist for safety (`Trial:SignupEnabled`, `Billing:LiveKeysEnabled`, etc.).
- **Do not** flip `Status: Published` on any reference customer row to "validate" the funnel — the publication runbook is owner-only ([`docs/go-to-market/reference-customers/REFERENCE_PUBLICATION_RUNBOOK.md`](../go-to-market/reference-customers/REFERENCE_PUBLICATION_RUNBOOK.md)).
- **Do not** disable trial write limits in production via configuration (`TrialLimitGate` is intentional).

---

## 4. Architecture overview

```mermaid
flowchart LR
  Browser[Browser - /signup] --> SignupForm[SignupForm.tsx]
  SignupForm -->|POST /api/proxy/v1/register| ApiProxy[Next.js API proxy]
  ApiProxy -->|forwards| RegistrationController[ArchLucid.Api/Controllers/RegistrationController.cs]
  RegistrationController --> Provisioning[ITenantProvisioningService.ProvisionAsync]
  Provisioning --> SqlTenant[(dbo.Tenants / Workspaces / Projects)]
  RegistrationController --> Bootstrap[ITrialTenantBootstrapService.TryBootstrapAfterSelfRegistrationAsync]
  Bootstrap --> SeedRun[Sample run seed - simulator agents]
  Bootstrap --> AuditTrialProvisioned[(dbo.AuditEvents - TrialProvisioned)]
  Browser --> VerifyPage[/signup/verify - email verification]
  VerifyPage --> LocalIdentity[/v1/auth/trial/local/* - if LocalIdentity mode]
  LocalIdentity --> JwtMint[ArchLucid-issued RSA JWT]
  Browser --> OperatorHome[/ - OperatorHomeGate + TrialWelcomeRunDeepLink]
  OperatorHome --> RunDetail[/runs/{trialWelcomeRunId}]
  RunDetail --> CommitButton[Commit run]
  CommitButton --> CoordinatorCommit[CoordinatorRunCommitCompleted audit]
  CoordinatorCommit --> TrialFirstCommit[(dbo.Tenants.TrialFirstManifestCommittedUtc)]
  RunDetail --> SponsorBanner[EmailRunToSponsorBanner]
  SponsorBanner -->|GET /v1/tenant/trial-status| TenantTrialController
  TenantTrialController -->|firstCommitUtc| BadgeRender[Day N since first commit badge]
```

---

## 5. Step-by-step happy path

Each step lists: **what happens**, **file path / HTTP endpoint**, **durable audit / metric**, and **how it fails**. **Stop and read the linked code** for any step you are debugging — do not infer from this table.

### Step 1 — Prospect lands on `/signup`

| Item | Value |
|------|-------|
| **UI page** | [`archlucid-ui/src/app/(marketing)/signup/page.tsx`](../../archlucid-ui/src/app/(marketing)/signup/page.tsx) |
| **Form component** | [`archlucid-ui/src/components/marketing/SignupForm.tsx`](../../archlucid-ui/src/components/marketing/SignupForm.tsx) |
| **Form schema** | [`archlucid-ui/src/lib/signup-schema.ts`](../../archlucid-ui/src/lib/signup-schema.ts) (`signupFormSchema`) |
| **Required fields** | `adminEmail`, `adminDisplayName`, `organizationName` |
| **Optional fields** | `companySize` (sessionStorage only), `baselineReviewCycleHours` + `baselineReviewCycleSource` (forwarded to API; gated behind `aria-expanded` "Add a baseline review-cycle estimate (optional)" disclosure — kept optional per `docs/PENDING_QUESTIONS.md` item 28) |
| **Audit / metric** | None at this step — the form is purely client-side |
| **Failure mode** | Zod validation error inline; no server traffic |

### Step 2 — `POST /api/proxy/v1/register` → `POST /v1/register`

| Item | Value |
|------|-------|
| **Proxy** | Next.js same-origin proxy (`/api/proxy/[[...path]]`) — adds `X-Tenant-Registration-Scope` headers when present |
| **Controller** | [`ArchLucid.Api/Controllers/RegistrationController.cs`](../../ArchLucid.Api/Controllers/RegistrationController.cs) |
| **Authorization** | `[AllowAnonymous]` + `[EnableRateLimiting("registration")]` |
| **Request model** | [`ArchLucid.Api/Models/Tenancy/TenantRegistrationRequest.cs`](../../ArchLucid.Api/Models/Tenancy/TenantRegistrationRequest.cs) |
| **Audit (always)** | `TrialSignupAttempted` (anonymous; `tenantId = Guid.Empty`) |
| **Audit (failure)** | `TrialSignupFailed` with `{ stage, reason }` (`provision/duplicate_slug`, `validation/<ExceptionType>`) |
| **Metric (failure)** | `archlucid_trial_signup_failures_total{stage, reason}` |
| **Failure modes** | 400 (validation), 409 (duplicate org slug), 429 (rate-limit policy `registration`) |

### Step 3 — Tenant + workspace + project provisioning

| Item | Value |
|------|-------|
| **Service** | `ITenantProvisioningService.ProvisionAsync` (registered in `ArchLucid.Application`) |
| **Persistence** | `dbo.Tenants`, `dbo.Workspaces`, `dbo.Projects` (initial role assignment to admin email) |
| **Audit** | `TenantSelfRegistered` (with `tenantId / workspaceId / projectId / organizationName / adminEmail`) |
| **Idempotency** | Returns `WasAlreadyProvisioned=true` for repeat slug → controller returns **409 Conflict** with stable problem type (`ProblemTypes.Conflict`) |

### Step 4 — Trial bootstrap (sample run + baseline capture)

| Item | Value |
|------|-------|
| **Service** | `ITrialTenantBootstrapService.TryBootstrapAfterSelfRegistrationAsync` (`ArchLucid.Application/Tenancy/TrialTenantBootstrapService.cs`) |
| **Email-verification gate** | `ITrialBootstrapEmailVerificationPolicy` — when a `dbo.IdentityUsers` row exists for `adminEmail`, bootstrap is **skipped** until `EmailVerifiedUtc` is set (see [`docs/security/TRIAL_AUTH.md`](../security/TRIAL_AUTH.md) § 3) |
| **Sample run** | Demo seed pattern (simulator agents) — same code path as `archlucid pilot up`, no Azure OpenAI keys required |
| **Tenant row updates** | `TrialStatus=Active`, `TrialStartUtc`, `TrialExpiresUtc`, `TrialRunsLimit`, `TrialSeatsLimit`, `TrialWelcomeRunId` (the seeded sample run) |
| **Baseline capture** | When the request includes `BaselineReviewCycleHours`, `TrialSignupBaselineReviewCycleCapture` is forwarded to the bootstrap service, persisted on the tenant row, and emits `TrialBaselineReviewCycleCaptured` audit (see CHANGELOG `2026-04-21 — Trial signup captures baseline review-cycle time`) |
| **Audit** | `TrialProvisioned` (durable email-intent signal — see [`TRIAL_END_TO_END.md`](TRIAL_END_TO_END.md) § "Email + integration events") |
| **Failure mode** | If bootstrap throws, the `RegistrationController` still returns **201** for the tenant — the operator UI surfaces a `TrialPendingEmailVerification` state on `/signup/verify` |

### Step 5 — Email verification (LocalIdentity mode only)

| Item | Value |
|------|-------|
| **UI page** | [`archlucid-ui/src/app/(marketing)/signup/verify/page.tsx`](../../archlucid-ui/src/app/(marketing)/signup/verify/page.tsx) |
| **API** | `TrialLocalIdentityAuthController` — `/v1/auth/trial/local/verify-email`, `/v1/auth/trial/local/token` |
| **Audit** | `TrialEmailVerified` |
| **JWT** | ArchLucid-issued RSA JWT with `tenant_id`, `roles=Reader|Operator|Admin` matching workforce JWT shape |
| **Failure mode** | Verification token expiry (`ITokenLifetime`) → user requests a new link |

### Step 6 — Operator UI first-run

| Item | Value |
|------|-------|
| **Landing** | [`archlucid-ui/src/app/(operator)/page.tsx`](../../archlucid-ui/src/app/(operator)/page.tsx) — wraps in `OperatorHomeGate` + `TrialWelcomeRunDeepLink` |
| **Deep-link source** | `GET /v1/tenant/trial-status` returns `trialWelcomeRunId` → UI navigates to `/runs/{id}` automatically on first visit |
| **First-run wizard** | `OperatorFirstRunWorkflowPanel` shows the four-step Core Pilot path (new run / runs / commit / artifacts) |
| **Telemetry** | `archlucid.ui.trial_welcome_deep_link.taken` (counter) |

### Step 7 — Commit the seeded run

| Item | Value |
|------|-------|
| **UI button** | "Commit run" on `/runs/{runId}` |
| **API** | `POST /v1/architecture/run/{runId}/commit` (façade `IRunCommitOrchestrator` → legacy `IArchitectureRunCommitOrchestrator` during ADR 0021 Phase 2) |
| **Audit (canonical + dual-write)** | `Run.CommitCompleted` + `CoordinatorRunCommitCompleted` |
| **Tenant trial counter** | `dbo.Tenants.TrialRunsUsed` increment is **atomic in the same transaction** as the run insert (`SqlRunRepository.SaveAsync`) — see [`docs/security/TRIAL_LIMITS.md`](../security/TRIAL_LIMITS.md) § "Data flow" |
| **First-commit pin** | `dbo.Tenants.TrialFirstManifestCommittedUtc` set once on the **tenant's first** committed golden manifest (**all** tiers; trial-only **`TrialFirstRunCompleted`** audit still gates on `TrialExpiresUtc`) |

### Step 8 — Sponsor banner + Day N badge + before-vs-measured panel

| Item | Value |
|------|-------|
| **Sponsor banner** | [`archlucid-ui/src/components/EmailRunToSponsorBanner.tsx`](../../archlucid-ui/src/components/EmailRunToSponsorBanner.tsx) — fetches `GET /v1/tenant/trial-status` and renders the **Day N since first commit** badge using `firstCommitUtc` |
| **Telemetry** | `archlucid.ui.sponsor_banner.first_commit_badge_rendered` + `POST /v1/diagnostics/sponsor-banner-first-commit-badge` |
| **PDF download** | `POST /v1/pilots/runs/{runId}/first-value-report.pdf` (ReadAuthority) |
| **Before-vs-measured panel** | [`archlucid-ui/src/components/BeforeAfterDeltaPanel.tsx`](../../archlucid-ui/src/components/BeforeAfterDeltaPanel.tsx) — renders the same shape as `ValueReportReviewCycleSectionFormatter`: `baselineReviewCycleHours` (from `/v1/tenant/trial-status`) vs measured time-to-commit (from `GET /v1/pilots/runs/{runId}/pilot-run-deltas` → `timeToCommittedManifestTotalSeconds`) |

---

## 6. End-to-end durable audit chain (for forensic replay)

| Order | Audit type | When | TenantId | Notes |
|-------|------------|------|----------|-------|
| 1 | `TrialSignupAttempted` | `RegistrationController` entry | `Guid.Empty` | Anonymous |
| 2 | `TrialSignupFailed` | Any failure path | `Guid.Empty` | With `{ stage, reason }` |
| 3 | `TenantSelfRegistered` | After provisioning | tenant | `{ organizationName, adminEmail }` |
| 4 | `TrialBaselineReviewCycleCaptured` | When form sent baseline | tenant | `{ baselineReviewCycleHours, baselineReviewCycleSource, capturedUtc }` |
| 5 | `TrialProvisioned` | Bootstrap commit | tenant | Durable email-intent signal |
| 6 | `TrialEmailVerified` | LocalIdentity verification | tenant | Only if `LocalIdentity` mode |
| 7 | `Run.CommitCompleted` (+ `CoordinatorRunCommitCompleted`) | First commit | tenant | Dual-write per ADR 0021 Phase 2 |

A cross-tenant audit query for a single funnel run **must** find at least rows 1, 3, 5, and 7 in chronological order. Missing row 5 with rows 3 + 7 present means email verification stalled.

---

## 7. Owner-only blockers (do not bypass)

| Blocker | Why owner-only | Tracking |
|---------|----------------|----------|
| **Stripe live keys + webhook secret** | Real money, real chargebacks. Live keys flip behaviour in `BillingWebhookTrialActivator.OnSubscriptionActivatedAsync` and the Marketplace publisher row in customer statements. | `docs/PENDING_QUESTIONS.md` item 9, item 22 |
| **Marketplace publisher legal entity name** | Appears on customer statements; must match the legal entity on Partner Center. | `docs/PENDING_QUESTIONS.md` item 22 |
| **DNS cutover for `archlucid.net` / `staging.archlucid.net`** | Front Door custom domain validation + cert provisioning + downstream MX / DKIM. | `docs/PENDING_QUESTIONS.md` Resolved row "DNS / TLS" |
| **Trial signup feature flag in production** | Throwing the funnel open to anonymous traffic without rate-limit pre-warming risks SQL pressure on shared catalogs. | `Trial:SignupEnabled` in `appsettings.SaaS.json` overlay |
| **Soft-required baseline review-cycle field** | Owner approval needed for the UX change + privacy notice. Today the field stays **optional**. | `docs/PENDING_QUESTIONS.md` item 28 |
| **Reference-customer row → `Published`** | First paying tenant; copy must be approved by the customer; discount re-rate review per `PRICING_PHILOSOPHY.md` § 5.3. | `docs/PENDING_QUESTIONS.md` item 19 |

---

## 8. Automated verification

| Layer | Spec / test | What it proves |
|-------|-------------|----------------|
| **Unit (controller)** | `ArchLucid.Api.Tests/RegistrationControllerTests.cs` | Validation rules, audit events emitted, baseline capture forwarded |
| **Unit (bootstrap)** | `ArchLucid.Application.Tests/Tenancy/TrialTenantBootstrapServiceTests.cs` | Email verification gate, baseline persistence |
| **Mock Playwright** | [`archlucid-ui/e2e/trial-funnel.spec.ts`](../../archlucid-ui/e2e/trial-funnel.spec.ts) | Form → mocked `/v1/register` 201 → mocked `/v1/tenant/trial-status` with `firstCommitUtc` → operator dashboard renders Day N badge + `BeforeAfterDeltaPanel` |
| **Live Playwright** | [`archlucid-ui/e2e/live-api-trial-end-to-end.spec.ts`](../../archlucid-ui/e2e/live-api-trial-end-to-end.spec.ts) | Real SQL, real RegistrationController, Noop checkout, harness-simulated subscription activation |
| **CLI smoke** | `archlucid trial smoke` ([`ArchLucid.Cli/Commands/TrialSmokeCommand.cs`](../../ArchLucid.Cli/Commands/TrialSmokeCommand.cs)) — pure HTTP loop. Prints **PASS / FAIL** per step against any local or staging API base URL. Tests in `ArchLucid.Cli.Tests/TrialSmokeCommandTests.cs`. |
| **CLI smoke (staging preset)** | `archlucid trial smoke --staging` — convenience: auto-targets `https://staging.archlucid.net` and emits a **single greppable line** `PASS|FAIL host=… correlation=… tenant=… welcomeRun=… failed=…`. Use this for sales-engineer pre-flight and for the nightly oncall paging payload (see § 9.1.b). Implementation: [`TrialSmokeOneLineSummaryFormatter`](../../ArchLucid.Cli/Commands/TrialSmokeOneLineSummaryFormatter.cs); correlation id is read from the `X-Correlation-ID` header on the first `POST /v1/register` response. |
| **Staging UI smoke (TEST-mode)** | [`archlucid-ui/e2e/trial-funnel-test-mode.spec.ts`](../../archlucid-ui/e2e/trial-funnel-test-mode.spec.ts) via [`playwright.trial-funnel-test-mode.config.ts`](../../archlucid-ui/playwright.trial-funnel-test-mode.config.ts). Drives a real browser at `signup.staging.archlucid.net` (override with `STAGING_BASE_URL`). Self-skips when `STRIPE_TEST_KEY` is unset so it is safe to run from a developer laptop without staging credentials. Wired into [`.github/workflows/trial-funnel-test-mode.yml`](../../.github/workflows/trial-funnel-test-mode.yml) for the nightly run. |
| **CI guard** | [`scripts/ci/assert_billing_safety_rules_shipped.py`](../../scripts/ci/assert_billing_safety_rules_shipped.py) — fails the merge if `BillingProductionSafetyRules` is removed or its `sk_live_` / Marketplace landing-page / GA offer-id checks are weakened. Self-test: [`scripts/ci/tests/test_assert_billing_safety_rules_shipped.py`](../../scripts/ci/tests/test_assert_billing_safety_rules_shipped.py). |

> **Note on spec path:** the operator-shell Playwright suite lives under `archlucid-ui/e2e/` (not `archlucid-ui/playwright/tests/` or `archlucid-ui/tests/e2e/`). Both default Playwright configs (`playwright.mock.config.ts` and `playwright.config.ts`) point `testDir` at `e2e/`. The trial-funnel mock spec is `archlucid-ui/e2e/trial-funnel.spec.ts`; the staging TEST-mode spec is `archlucid-ui/e2e/trial-funnel-test-mode.spec.ts` and ships its own dedicated config (no local Next.js webServer — drives staging directly).

---

## 9. Local quick-start (Stripe TEST mode against staging)

```bash
# 1. Point the CLI at staging (auto-set when you pass --staging).
dotnet run --project ArchLucid.Cli -- trial smoke --staging \
  --org "TrialSmoke-$(date +%s)" --email "trial-smoke@example.invalid" --baseline-hours 16
# One-line output: PASS host=https://staging.archlucid.net correlation=<guid> ...

# 2. Or, the per-step long form (any base URL):
export ARCHLUCID_API_URL=https://staging.archlucid.net
dotnet run --project ArchLucid.Cli -- trial smoke --org "TrialSmoke-$(date +%s)" \
  --email "trial-smoke@example.invalid" --baseline-hours 16

# 3. Mock-Playwright equivalent (no API):
cd archlucid-ui && npx playwright test -c playwright.mock.config.ts trial-funnel

# 4. Staging UI smoke (real browser → signup.staging.archlucid.net; self-skips
#    if STRIPE_TEST_KEY is unset):
STRIPE_TEST_KEY=<key> npx playwright test -c playwright.trial-funnel-test-mode.config.ts
```

**Live (real SQL) acceptance** stays in [`TRIAL_END_TO_END.md`](TRIAL_END_TO_END.md) — that runbook is the canonical merge gate; this one exists so anyone can reason about the funnel end-to-end **without** spinning up SQL.

---

## 9.1 Sales-engineer playbook (staging Stripe TEST mode — V1)

> **Audience:** the **sales engineer** running a live product evaluation for a prospect against the staging stack. The V1 commercial motion is **sales-led** — live keys are V1.1-deferred per owner Q17 (2026-04-23) — so the funnel is **always exercised in Stripe TEST mode** during evaluations. This section is the "how I drive the demo" cheat sheet. It is intentionally separate from § 9 (developer quick-start) so an SE never has to read the full runbook.

### 9.1.a What you have available on staging

| Surface | URL (default) | What you do here |
|---|---|---|
| **Public signup** | `https://signup.staging.archlucid.net/signup` | Drive the prospect through the form. |
| **Operator UI** | same host, post-verification | Show the wizard, commit, and the Day-N badge. |
| **API smoke (no UI)** | `archlucid trial smoke --staging` | Pre-flight check before joining the prospect call — confirms the funnel is alive on staging. One-line **PASS|FAIL** with a **correlation id** to hand to support if anything is off. |
| **UI smoke (Playwright)** | `npx playwright test -c playwright.trial-funnel-test-mode.config.ts` from `archlucid-ui/` | Same flow as the prospect, end-to-end. Use this if the prospect reports something you cannot reproduce in the API smoke. |
| **Nightly automation** | `.github/workflows/trial-funnel-test-mode.yml` | Runs both smokes nightly. If you start the day with a green run on the dashboard, you know the funnel is healthy *before* the prospect call. |

### 9.1.b Pre-call checklist (5 minutes, on your laptop)

```bash
# 1. Confirm staging is alive (under 10 s):
dotnet run --project ArchLucid.Cli -- trial smoke --staging \
  --org "PreCall-$(date +%s)" --email "se+precall@example.invalid"
# Expect: PASS host=https://staging.archlucid.net correlation=<guid> ...

# 2. (Optional) end-to-end UI run, if you want to see what the prospect will see:
cd archlucid-ui && STRIPE_TEST_KEY="$STRIPE_TEST_KEY" \
  npx playwright test -c playwright.trial-funnel-test-mode.config.ts
```

If either fails, **do not** start the demo — open the nightly workflow on the Actions tab, copy the `correlation=<guid>` from the failed step, and ping `#staging-oncall` with that id. The on-call already has the audit trail keyed to that id.

### 9.1.c What to tell the prospect (script)

Keep it consultative. Avoid trial-puffery — the SaaS-on-GitHub posture is the differentiator, not the trial form.

> "I'm going to take you through the same five steps an evaluator sees on `archlucid.net/get-started`. We're on the staging environment so the billing path is in **TEST mode** — no card is charged, no Marketplace listing is involved. After you commit your first run we'll look at the Day-N badge together; that's the smallest unit of value the product produces."

Then drive the form on `/signup`. Stay on the **model-default** baseline (the radio is preselected) unless the prospect asks for the custom hours field — that field is **optional** and gated behind an `aria-expanded` disclosure (see § 5 Step 1, owner Q28). Telling the prospect the hours field is optional is the right answer: ArchLucid still renders a measured-vs-modeled curve, just from the conservative model defaults.

After the wizard step 1 → step 7 → commit, point the prospect at the **before-vs-measured panel** (`BeforeAfterDeltaPanel`) and the **Day-N since first commit** badge. Those two surfaces are the demo's punchline.

### 9.1.d Resetting a trial tenant after the evaluation

The trial tenant persists in the staging SQL catalog after the call. Two reset shapes are supported:

1. **Soft reset (recommended).** Let the trial expire on its own (`TrialExpiresUtc` is set at provisioning per § 4 Step 4). The tenant stays in the catalog for forensic replay. Use this when the prospect may convert and you want the audit chain (`TrialSignupAttempted` → `TenantSelfRegistered` → `TrialProvisioned` → `Run.CommitCompleted`) to remain queryable.
2. **Hard reset.** When the prospect explicitly asked for their data to be removed: open a ticket against the staging operator on-call with the **correlation id** from the smoke run. Hard delete is a one-way operation against `dbo.Tenants` / `dbo.Workspaces` / `dbo.Projects` and is owner-only (no SE-self-serve).

**Do not** flip `dbo.Tenants.Status` to publish, do not promote a trial tenant to a reference customer row, and do not touch `Trial:SignupEnabled` to "speed up" repeat signups — those are owner-only per § 7.

### 9.1.e Stop-and-ask boundaries (mirror of § 7, restated for SEs)

- **Live Stripe keys (`sk_live_*`).** SEs **never** touch them. The `BillingProductionSafetyRules` boot guard refuses to start the API in Production with a live key and no webhook signing secret — we ship a CI guard (`scripts/ci/assert_billing_safety_rules_shipped.py`) that fails the merge if those checks are removed or weakened.
- **Marketplace listing publication.** The Partner Center listing stays at **Status: Draft** for V1 (per `docs/library/V1_DEFERRED.md` § 6b). Do not ask the prospect to "click the marketplace tile" during the demo — there isn't one yet.
- **DNS cutover.** `archlucid.net/signup` is not the demo URL — `signup.staging.archlucid.net` is. If a prospect already has a tab open on the production hostname, ask them to close it and re-open the staging hostname; the operator UI on production will refuse the trial flow because `Trial:SignupEnabled` is off there.

### 9.1.f When the smoke says PASS but the demo still feels wrong

Two failure shapes show **PASS** at the smoke layer:

1. **Email-verification stall.** If the prospect never receives the verification email and you are running against a tenant whose `Auth:Trial:Modes` is `LocalIdentity`, the dev-harness verify endpoint should auto-flip `EmailVerifiedUtc` (the new Playwright spec uses it). If that endpoint is missing, the spec self-skips with a clear message and the SE should fall back to driving the prospect through email manually.
2. **Telemetry gap.** The CLI smoke does not assert sponsor-banner telemetry. If the Day-N badge renders but `archlucid.ui.sponsor_banner.first_commit_badge_rendered` is empty in Grafana for that tenant, file a bug with the correlation id; the demo itself is fine, the telemetry hook needs investigation.

---

## 10. Related

- [`TRIAL_AND_SIGNUP.md`](../go-to-market/TRIAL_AND_SIGNUP.md), [`TRIAL_END_TO_END.md`](TRIAL_END_TO_END.md), [`TRIAL_FUNNEL.md`](TRIAL_FUNNEL.md), [`TRIAL_LIFECYCLE.md`](TRIAL_LIFECYCLE.md)
- [`SPONSOR_BANNER_FIRST_COMMIT_BADGE.md`](../library/SPONSOR_BANNER_FIRST_COMMIT_BADGE.md)
- [`PILOT_ROI_MODEL.md`](../library/PILOT_ROI_MODEL.md) § 3.1 — provenance of the baseline review-cycle hours when the form was skipped
- [`adr/0021-coordinator-pipeline-strangler-plan.md`](../adr/0021-coordinator-pipeline-strangler-plan.md) — `Run.CommitCompleted` dual-write window
