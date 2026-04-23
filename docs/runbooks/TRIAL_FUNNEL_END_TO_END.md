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

- Funnel is exercised **either** locally (`dotnet run --project ArchLucid.Api` + `npm run dev`) **or** against staging (`https://staging.archlucid.com`).
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
| **First-commit pin** | `dbo.Tenants.TrialFirstManifestCommittedUtc` set once on the **tenant's first** committed manifest |

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
| **DNS cutover for `archlucid.com` / `staging.archlucid.com`** | Front Door custom domain validation + cert provisioning + downstream MX / DKIM. | `docs/PENDING_QUESTIONS.md` Resolved row "DNS / TLS" |
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

> **Note on spec path:** the operator-shell Playwright suite lives under `archlucid-ui/e2e/` (not `archlucid-ui/playwright/tests/`). Both Playwright configs (`playwright.mock.config.ts` and `playwright.config.ts`) point `testDir` at `e2e/`. New mock spec for the funnel is `archlucid-ui/e2e/trial-funnel.spec.ts`.

---

## 9. Local quick-start (Stripe TEST mode against staging)

```bash
# 1. Point the CLI at staging.
export ARCHLUCID_API_URL=https://staging.archlucid.com

# 2. Run the smoke loop (pure HTTP — no docker, no SQL on your laptop).
dotnet run --project ArchLucid.Cli -- trial smoke --org "TrialSmoke-$(date +%s)" \
  --email "trial-smoke@example.invalid" --baseline-hours 16

# 3. Watch each step print PASS / FAIL with the audit hint to follow up on failure.

# 4. Mock-Playwright equivalent (no API):
cd archlucid-ui && npx playwright test -c playwright.mock.config.ts trial-funnel
```

**Live (real SQL) acceptance** stays in [`TRIAL_END_TO_END.md`](TRIAL_END_TO_END.md) — that runbook is the canonical merge gate; this one exists so anyone can reason about the funnel end-to-end **without** spinning up SQL.

---

## 10. Related

- [`TRIAL_AND_SIGNUP.md`](../go-to-market/TRIAL_AND_SIGNUP.md), [`TRIAL_END_TO_END.md`](TRIAL_END_TO_END.md), [`TRIAL_FUNNEL.md`](TRIAL_FUNNEL.md), [`TRIAL_LIFECYCLE.md`](TRIAL_LIFECYCLE.md)
- [`SPONSOR_BANNER_FIRST_COMMIT_BADGE.md`](../library/SPONSOR_BANNER_FIRST_COMMIT_BADGE.md)
- [`PILOT_ROI_MODEL.md`](../library/PILOT_ROI_MODEL.md) § 3.1 — provenance of the baseline review-cycle hours when the form was skipped
- [`adr/0021-coordinator-pipeline-strangler-plan.md`](../adr/0021-coordinator-pipeline-strangler-plan.md) — `Run.CommitCompleted` dual-write window
