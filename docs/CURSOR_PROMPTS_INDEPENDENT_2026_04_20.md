> **Scope:** Copy-paste Cursor Agent prompts for the six 2026-04-20 independent quality improvements (paths, acceptance criteria, verification); not the scored quality assessment itself (see companion link below).

> **Companion file to:** [QUALITY_ASSESSMENT_2026_04_20_INDEPENDENT_69_33.md](archive/quality/QUALITY_ASSESSMENT_2026_04_20_INDEPENDENT_69_33.md)

# Cursor Prompts — Top 6 Improvements (2026-04-20, Independent)

These six prompts are designed to be pasted into Cursor in Agent mode, in order. Each prompt is self-contained, names the files involved, lists the acceptance criteria, and ends with verification commands.

**Repo facts already filled in below:** canonical ADR path, CI reference-customer **auto-flip** (no manual script change for merge-blocking), real controller and test paths, OpenAPI contract snapshot location, existing billing/Stripe hooks, wizard and CLI paths, Terraform pilot root, and pen-test summary folder layout.

Where a placeholder still requires your input it remains as `<<…>>`.

---

## Prompt 1 — Publish the first real reference customer

```text
Goal: Move the first real ArchLucid reference customer from placeholder toward Published so the −15% reference discount line in docs/go-to-market/PRICING_PHILOSOPHY.md can eventually be re-rated (see § 5.3 / § 5.4).

Context files (verified paths):
- docs/go-to-market/reference-customers/README.md — single GFM table; **do not** split the table. Columns (exact order): Customer | Tier | Pilot start | Case-study link | Reference-call cadence | Status
- docs/go-to-market/REFERENCE_NARRATIVE_TEMPLATE.md — fictional templates only; real assets live under reference-customers/
- docs/go-to-market/reference-customers/EXAMPLE_DESIGN_PARTNER_CASE_STUDY.md — copy pattern for new case study
- scripts/ci/check_reference_customer_status.py — exits 0 iff ≥1 row has Status token `Published` (see ALLOWED_STATUS_TOKENS)
- scripts/ci/test_check_reference_customer_status.py — unit tests (run as `python -m unittest test_check_reference_customer_status` from scripts/ci)
- .github/workflows/ci.yml — lines ~100–117: **two-step guard**. Step `refcust-warn` has continue-on-error: true. Step "Guard — reference-customer status (auto-flip: strict once any Published row exists)" runs the **same** script **without** continue-on-error **only when** `steps.refcust-warn.outcome == 'success'` (i.e. the warn step exited 0 because a Published row exists). **You do not edit the Python script to "flip" merge-blocking** — the workflow already auto-flips.

Tasks:
1. Per README § "How to add a real reference": copy EXAMPLE_DESIGN_PARTNER_CASE_STUDY.md to docs/go-to-market/reference-customers/<<CUSTOMER_SLUG>>_CASE_STUDY.md (slug: lowercase, hyphens/underscores, no spaces — README requires this naming).
2. Update the **Reference-customer table** in README.md: either replace the DESIGN_PARTNER_NEXT row or add a new bottom row (README: "add new rows to the bottom"). Set Status to Drafting when internal copy is ready. Allowed Status tokens (parsed before first em dash): Placeholder, Drafting, Customer review, Published.
   - Customer name: <<ENTER CUSTOMER NAME>>
   - Tier: <<Team | Professional | Enterprise (or design-partner note)>>
   - Pilot start: <<YYYY-MM-DD>>
   - Case-study link: README table column must point at the sibling case-study file using standard GFM link syntax (text + relative path); substitute the real slug for the placeholder token used in the filename.
   - Reference-call cadence: <<e.g. quarterly, on-request>>
3. Add an entry to CHANGELOG.md under Commercial: "Added first real reference customer <<name>> (Drafting)."
4. Optional hardening (only if product wants stricter parsing): extend scripts/ci/check_reference_customer_status.py with clearer stderr when zero Published rows — **do not** change exit-code semantics without updating test_check_reference_customer_status.py.

Acceptance criteria:
- README table shows the real row with a valid Status token.
- New <<CUSTOMER_SLUG>>_CASE_STUDY.md exists beside README.
- CHANGELOG entry under Commercial.
- CI guard locally:
  - From repo root: python scripts/ci/check_reference_customer_status.py docs/go-to-market/reference-customers/README.md
  - From scripts/ci: python -m unittest test_check_reference_customer_status
- docs/go-to-market/PRICING_PHILOSOPHY.md: add a short TODO note (§ 5.4) that the −15% reference discount removal is triggered when the first row hits Published **and** the strict CI step starts running (same day as first successful Published parse).

Out of scope:
- Removing the −15% discount text (do in a follow-up commit when Status is Published).
```

---

## Prompt 2 — Commission and publish a redacted external pen test (+ SOC 2 self-assessment)

```text
Goal: Stand up the artifacts needed to start an external penetration test and publish its redacted summary, AND publish an owner-led SOC 2 self-assessment (no external CPA — self-assessed only).

Context files (verified paths):
- docs/security/PEN_TEST_SOW_TEMPLATE.md
- docs/security/PEN_TEST_REDACTED_SUMMARY_TEMPLATE.md
- docs/security/pen-test-summaries/README.md — index for summaries (already exists)
- docs/security/pen-test-summaries/2026-Q2-DRAFT.md — replace or supersede with final SoW/summary split as needed
- docs/go-to-market/SOC2_ROADMAP.md
- docs/go-to-market/TRUST_CENTER.md

Evidence anchors for COMPLIANCE_MATRIX.md (concrete code/config — extend as needed):
- ArchLucid.Host.Core/Startup/AuthSafetyGuard.cs — DevelopmentBypass / production guards (invoked from ArchLucid.Api/Program.cs)
- ArchLucid.Api/Program.cs — GuardAllDevelopmentBypasses
- .github/workflows/ci.yml — jobs api-schemathesis-light (~1657+); weekly .github/workflows/schemathesis-scheduled.yml
- docs/runbooks/ — pick key rotation / RLS / LLM runbooks by grep for "rotation", "RLS", "LLM"

Tasks:
1. Create docs/security/pen-test-summaries/2026-Q2-SOW.md from PEN_TEST_SOW_TEMPLATE.md. Pre-fill **scope** (non-placeholder) with at least: public HTTP API surface (v1), operator UI (archlucid-ui), multi-tenant isolation + RLS break-glass paths, JWT + API key auth, billing webhooks (ArchLucid.Api/Controllers/Billing/BillingStripeWebhookController.cs), LLM prompt boundary + redaction + content safety. Leave assessor-specific blanks as:
   - Assessor vendor: <<TBD>>
   - Target delivery: <<YYYY-MM-DD>>
2. **Default hosting decision (repo-aligned, change if product disagrees):** publish the redacted summary under docs/security/pen-test-summaries/ (e.g. 2026-Q2-REDACTED-SUMMARY.md from PEN_TEST_REDACTED_SUMMARY_TEMPLATE.md) **and** add a one-line link from docs/go-to-market/TRUST_CENTER.md. A future customer-facing trust portal can deep-link the same file path until a web property exists.
3. Create docs/security/SOC2_SELF_ASSESSMENT_2026.md — front matter must state **SELF-ASSESSMENT ONLY / NOT CPA ATTESTATION**. Map controls to SOC 2 TSC categories; include a gap register with Owner column (names <<TBD>> where unknown).
4. Create docs/security/COMPLIANCE_MATRIX.md — join controls to evidence paths (file paths + workflow names above).
5. Update docs/go-to-market/SOC2_ROADMAP.md: pen test = one-shot funded item; external SOC 2 Type I/II unfunded; self-assessment published as interim artifact.
6. Update docs/go-to-market/TRUST_CENTER.md with links to SOC2_SELF_ASSESSMENT_2026.md, COMPLIANCE_MATRIX.md, pen-test-summaries/2026-Q2-SOW.md, and the redacted summary file name you chose.
7. CHANGELOG under Security.

Acceptance criteria:
- New/edited docs as above; TRUST_CENTER.md links resolve (repo-relative).
- markdownlint clean on touched docs.

Verification:
- python scripts/ci/check_doc_links.py (repo: scripts/ci/check_doc_links.py — same family as CI doc-link guard), or manually open each new link from TRUST_CENTER.md
```

---

## Prompt 3 — Execute ADR 0021 phase 1 (Coordinator strangler; break in v1 where needed)

```text
Goal: Promote ADR 0021 from Proposed to Accepted and ship **Phase 1** per the ADR (single read-side adapter — additive). Owner has approved breaking deprecated Coordinator-only **public HTTP** surface in v1 (no /v2 URL bump) **when** a concrete endpoint is chosen for deprecation — align any HTTP deprecation with ADR Phase 2+ notes and API_CONTRACTS.md.

Canonical ADR (verified):
- docs/adr/0021-coordinator-pipeline-strangler-plan.md — Phase 0 shipped; Phase 1 = introduce IUnifiedGoldenManifestReader in ArchLucid.Decisioning.Interfaces; **no deletion of ICoordinator* yet**
- docs/DUAL_PIPELINE_NAVIGATOR.md — update "Why we have not collapsed these" once ADR is Accepted
- docs/adr/0010-dual-manifest-trace-repository-contracts.md — remains Accepted until superseding ADR after Phase 3

Existing regression tests (extend these; do not duplicate with vague "NetArchTest coordinator namespace" unless you add a **new** rule in ArchLucid.Architecture.Tests):
- ArchLucid.Core.Tests/Audit/AuditEventTypes_DoNotCollideAcrossPipelinesTests.cs
- ArchLucid.Api.Tests/Startup/DualPipelineRegistrationDisciplineTests.cs

Known Coordinator seam on HTTP (read path still uses coordinator repository):
- ArchLucid.Api/Controllers/Governance/ManifestsController.cs — constructor takes ICoordinatorGoldenManifestRepository (alongside IArchitectureApplicationService). Phase 1 work likely introduces IUnifiedGoldenManifestReader and migrates **internal** reads; public routes may stay unchanged until a later phase.

Other context:
- docs/V1_SCOPE.md — add explicit note: no net-new public endpoints that exist only to extend the Coordinator repository family without ADR sign-off
- docs/BREAKING_CHANGES.md — if you deprecate any public route, log Sunset / Obsolete here per docs/API_CONTRACTS.md
- ArchLucid.Architecture.Tests/DependencyConstraintTests.cs — only if a **new** cross-assembly rule is justified; prefer extending DualPipelineRegistrationDisciplineTests first

Tasks:
1. Update docs/adr/0021-coordinator-pipeline-strangler-plan.md header: Status: Accepted; add architecture-review note (reviewers + date) in the PR body or a short subsection per ADR § Decision review gate.
2. Implement Phase 1: IUnifiedGoldenManifestReader + façade + migrate internal read call sites per ADR § Phase 1. The ADR cites docs/runbooks/COORDINATOR_TO_AUTHORITY_PARITY.md for parity evidence — that file is **not** in repo yet; add it as a new runbook (template tables for p95/p99 latency, audit-row counts, replay parity) and fill what you can from staging metrics.
3. If product also wants an HTTP-visible deprecation in this PR: pick the **smallest** Coordinator-only or duplicate surface, mark [Obsolete] on controller action **and** document Authority equivalent in BREAKING_CHANGES.md. **Do not** delete implementations in this PR (ADR Phase 3).

Acceptance criteria:
- ADR shows Accepted; DUAL_PIPELINE_NAVIGATOR.md reflects Authority as supported long-term path.
- dotnet test — at minimum ArchLucid.Core.Tests and ArchLucid.Api.Tests filters touching changed code; full suite before merge.
- markdownlint on touched docs.

Verification:
- dotnet test --filter "FullyQualifiedName~DualPipelineRegistrationDisciplineTests|FullyQualifiedName~AuditEventTypes_DoNotCollideAcrossPipelinesTests"
- dotnet test ArchLucid.Architecture.Tests/ArchLucid.Architecture.Tests.csproj
```

---

## Prompt 4 — Enforce tier entitlements server-side (402 Payment Required)

```text
Goal: Return 402 Payment Required when tenant packaging tier is below the capability tier for Pro/Enterprise-only surfaces, so PRODUCT_PACKAGING.md reflects enforced entitlements. Owner approved 402 over 403.

Context files (verified controller paths — annotate all routes that map to Enterprise Controls / Advanced Analysis per PRODUCT_PACKAGING.md):
- docs/PRODUCT_PACKAGING.md — layer-to-capability map; update § 4.4 when enforcement lands
- ArchLucid.Api/Controllers/Governance/GovernanceController.cs
- ArchLucid.Api/Controllers/Governance/GovernanceResolutionController.cs
- ArchLucid.Api/Controllers/Governance/GovernancePreviewController.cs
- ArchLucid.Api/Controllers/Governance/PolicyPacksController.cs
- ArchLucid.Api/Controllers/Governance/ManifestsController.cs — manifest read/compare/export family (confirm tier: Professional vs Enterprise per packaging doc)
- ArchLucid.Api/Controllers/Admin/AuditController.cs — route prefix v{version:apiVersion}/audit — CSV export and related read APIs
- ArchLucid.Api/Routing/ApiV1Routes.cs — stable string constants for v1 governance segments used in tests
- ArchLucid.Core.Authorization / existing policies — extend with tier policy (discover ArchLucidPolicies and JWT claim types via grep)
- docs/go-to-market/PRICING_PHILOSOPHY.md — Team / Professional / Enterprise naming

OpenAPI / contract tests (verified):
- Runtime spec: GET /openapi/v1.json from a running API (or generation pipeline your branch uses)
- Merge artifact: ArchLucid.Api.Tests/Contracts/openapi-v1.contract.snapshot.json — update snapshot when 402 responses are added
- CI: .github/workflows/ci.yml job api-schemathesis-light — ensure new 402 responses are declared so Schemathesis does not treat them as failures (document expected status codes per route in OpenAPI / operation filters if needed)

Tasks:
1. Introduce ITenantTierResolver (or equivalent) reading tier from tenant claims and/or SESSION_CONTEXT / scope — follow existing ArchLucid.Core.Scoping patterns (IScopeContextProvider).
2. Add authorization requirement attributes/filters mapping Professional vs Enterprise to route groups above.
3. On tier mismatch return ProblemDetails with HTTP 402 using existing ArchLucid.Api/ProblemDetails helpers (grep StatusCodes.Status402PaymentRequired usage; add if none).
4. Document 402 in OpenAPI + refresh openapi-v1.contract.snapshot.json.
5. Unit tests: tier sufficient → 200; tier insufficient → 402; unauthenticated → 401 (not 402).
6. Integration test: Team JWT hits a Professional-only governance route → assert 402 body shape.

Notes:
- 402 is uncommon in some enterprise HTTP clients; document in OpenAPI description per owner decision.

Verification:
- dotnet test ArchLucid.Api.Tests/ArchLucid.Api.Tests.csproj --filter "<<your new test class name>>"
- dotnet test (full API tests) before PR
```

---

## Prompt 5 — In-product pilot scorecard + Stripe Checkout for Team tier

```text
Goal: Sponsor-ready pilot scorecard from real run data, plus Stripe Checkout for Team tier. Billing plumbing already exists — extend rather than reinvent.

Existing code (verified — inspect before adding duplicates):
- ArchLucid.Api/Controllers/Pilots/PilotsController.cs — route v{version:apiVersion}/pilots; today GET runs/{runId}/first-value-report (Markdown). **Add** POST pilot/scorecard (or sub-route you choose) returning JSON + optional DOCX export flag — keep policy consistent (today: ReadAuthority).
- ArchLucid.Api/Controllers/Authority/DocxExportController.cs — patterns for DOCX responses
- ArchLucid.Api/Controllers/Billing/BillingCheckoutController.cs — POST v1/tenant/billing/checkout (AdminAuthority); uses IBillingProviderRegistry / Billing:Provider
- ArchLucid.Api/Controllers/Billing/BillingStripeWebhookController.cs — webhook endpoint already present for Stripe events
- docs/PILOT_ROI_MODEL.md, docs/go-to-market/PILOT_SUCCESS_SCORECARD.md, docs/deployment/PER_TENANT_COST_MODEL.md

UI / static pricing:
- archlucid-ui/public/pricing.json — **JSON does not allow comments**; for TODO placeholders add optional string fields such as "teamCheckoutUrl": null and "teamCheckoutMode": null or use a parallel archlucid-ui/public/pricing.overrides.example.json documented in STRIPE_CHECKOUT.md
- Locate pricing page component via grep for pricing.json or /pricing in archlucid-ui/

Docs / trial flow:
- docs/go-to-market/TRIAL_AND_SIGNUP.md — trial expiry CTA → checkout URL field
- Add docs/go-to-market/STRIPE_CHECKOUT.md — flow, webhook URL shape, manual provisioning until Marketplace GA

Tasks (scorecard):
1. Define PilotScorecard DTO: tenantId, periodStart, periodEnd, runs, manifests, findings by severity, governanceApprovals, exports, hoursSavedEstimate (from PILOT_ROI_MODEL), simulatorRunsRatio.
2. Implement service in ArchLucid.Application (or existing Pilots namespace) and wire PilotsController.
3. DOCX: reuse reporting stack used by DocxExportController / consulting export patterns — add template asset under source-controlled templates directory used by the reporting project (grep ConsultDocx / .docx in repo).
4. Tests: unit for aggregation; integration with demo seed endpoint (grep demo/seed in ArchLucid.Api for exact route).

Tasks (Stripe):
5. Prefer wiring **Buy Team** through existing BillingCheckoutController when TargetTier=Team matches Billing:Provider=Stripe — only add a static marketing link if product wants a hosted Payment Link separate from API-driven checkout.
6. Surface CTA on pricing page; store URL or mode in pricing.json extra keys **or** environment-specific archlucid-ui config — document in STRIPE_CHECKOUT.md.

Still <<TBD>> from owner before production flip:
- Stripe account / price IDs / Payment Link URL
- Subscription vs one-shot
- Public webhook base URL for BillingStripeWebhookController

Verification:
- dotnet test (new tests)
- npm test / npx playwright test — only the suites you touched under archlucid-ui
```

---

## Prompt 6 — Halve adoption friction (one SaaS profile + 3-step wizard + doctor red/green)

```text
Goal: One opinionated SaaS configuration profile, collapse the new-run wizard to three visible steps, extend doctor with config red/green.

Verified paths:
- ArchLucid.Api/appsettings.json, ArchLucid.Api/appsettings.Development.json — baseline; add ArchLucid.Api/appsettings.SaaS.json and wire via ASPNETCORE_ENVIRONMENT=SaaS or additional hosting startup (follow existing Program.cs configuration chains)
- docs/REFERENCE_SAAS_STACK_ORDER.md — already exists; add SaaS profile section pointing at appsettings.SaaS.json + ordered Terraform roots
- infra/terraform-pilot/ — **already the canonical pilot stack**; do not duplicate as a second folder unless product renames. Add infra/apply-saas.ps1 at repo root that invokes terraform init/apply in the order REFERENCE_SAAS_STACK_ORDER.md documents (parameters <<TBD>> for state backends)
- Other infra/terraform-* directories — prepend README "Advanced — only if ..." only where missing

Wizard (verified — App Router):
- archlucid-ui/src/app/(operator)/runs/new/page.tsx
- archlucid-ui/src/app/(operator)/runs/new/NewRunWizardClient.tsx
- Tests: NewRunWizardClient.test.tsx, NewRunWizardClient.sample-run.test.tsx

Doctor CLI (verified):
- ArchLucid.Cli/Commands/DoctorCommand.cs — currently probes CLI build, local layout, GET /version, /health/live, /health/ready, /health. Extend with **configuration red/green** rows for SaaS profile keys (mirror AuthSafetyGuard / RLS / ApiKey / Jwt / LlmPromptRedaction / ContentSafety). Add doc links to docs/runbooks/*.md per key.
- ArchLucid.Cli/Program.cs — command registration for doctor

Tasks:
1. appsettings.SaaS.json — pin: ApiKey enabled for service accounts, Jwt enabled, Development bypass false, RLS ApplySessionContext true, LlmPromptRedaction enabled, ContentSafety on, rate limits per PRICING/SECURITY docs.
2. infra/apply-saas.ps1 — ordered apply; -WhatIf or plan mode flag optional.
3. Wizard UX: three steps visible; move advanced fields behind disclosure; keep validation identical.
4. Doctor: tabular OK/MISSING/INVALID; non-zero exit on any MISSING/INVALID for SaaS profile when --strict (default for CI operator image optional).

CLI doctor integration test (if none):
- Prefer a small unit test on a new pure helper class in ArchLucid.Cli or ArchLucid.Cli.Tests if the test project exists — grep ArchLucid.Cli.Tests

Verification:
- dotnet run --project ArchLucid.Cli -- doctor (against local docker stack per docs/FIRST_30_MINUTES.md)
- npm test in archlucid-ui for wizard tests
- Update docs/FIRST_30_MINUTES.md to mention appsettings.SaaS.json + apply-saas.ps1 when landed
```

---

## Pending input still required from the owner

These still block **content** completion (not prompt scaffolding):

1. First reference customer: legal name, tier, pilot start, cadence, slug (Prompt 1).
2. Pen test: assessor vendor, delivery date; adjust default hosting if not `docs/security/pen-test-summaries/` + TRUST_CENTER link (Prompt 2).
3. Stripe: account/price IDs, subscription vs one-shot, public webhook URL (Prompt 5).
