> **Scope:** Paste-ready Cursor prompts for the **six best improvements** identified in [`QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_64_14.md`](QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_64_14.md) § 8.

# Cursor prompts — six best improvements (2026-04-21)

Each prompt is **self-contained**: open a fresh Cursor agent thread, paste the full block, run. The prompts assume the agent will follow the workspace rules (no subagents, do-the-work-yourself, terse C# style, IaC-first, single SQL DDL per database, Markdown generosity).

Prompts marked **(owner-blocked)** require an answer in [`docs/PENDING_QUESTIONS.md`](PENDING_QUESTIONS.md) before the agent can complete them. The agent should still pre-build everything it can and **stop at the first owner gate**, surfacing the question.

---

## Prompt 1 — Publish first reference customer + computed ROI

**Title:** Publish first reference customer with computed-ROI evidence

**Why:** Marketability, Trustworthiness, Proof-of-ROI, Decision Velocity all lift together when one real tenant is publishable. Today every "proof" is the Contoso demo seed.

**Prompt to paste into Cursor:**

```text
You are extending ArchLucid's go-to-market evidence pack. Read these files first and do not skip them:
- docs/go-to-market/reference-customers/README.md
- docs/go-to-market/reference-customers/TRIAL_FIRST_REFERENCE_CASE_STUDY.md
- docs/go-to-market/reference-customers/DESIGN_PARTNER_NEXT_CASE_STUDY.md
- docs/go-to-market/PRICING_PHILOSOPHY.md  (§ 5.4 reference-customer CI guard)
- docs/PILOT_ROI_MODEL.md
- ArchLucid.Application/Pilots/PilotRunDeltaComputer.cs
- ArchLucid.Application/Pilots/PilotRunDeltas.cs
- ArchLucid.Application/Pilots/FirstValueReportBuilder.cs
- ArchLucid.Application/Pilots/SponsorOnePagerPdfBuilder.cs
- scripts/ci/check_reference_customer_status.py

Goal: produce two artifacts and one tenant-export tool.

(A) New file docs/go-to-market/reference-customers/REFERENCE_PUBLICATION_RUNBOOK.md
   Step-by-step for moving a tenant from "Drafting" -> "Customer review" -> "Published":
     1. Confirm signed reference agreement (link template; flag "owner-blocked: legal sign-off").
     2. Run new CLI command (see C below) to extract per-tenant computed deltas.
     3. Fill REFERENCE_NARRATIVE_TEMPLATE.md with measured numbers from the CLI output.
     4. Customer review checklist (quote redaction rules, screenshot tenant-data redaction).
     5. Replace placeholder row in reference-customers/README.md and let the CI guard auto-flip the discount status.
   Cite check_reference_customer_status.py for the CI flip mechanism.

(B) New file docs/go-to-market/reference-customers/REFERENCE_EVIDENCE_PACK_TEMPLATE.md
   A single-page template that pulls together for ONE tenant: logo, problem statement,
   measured deltas (time-to-commit, finding counts, audit-row counts, LLM call counts,
   review-cycle hours saved per ROI_MODEL formula), one quote, one screenshot. Mark every
   computed field with a code-fence pointing at the exact PilotRunDeltas property the
   value comes from, so the case study cannot drift from product reality.

(C) New CLI command (shipped 2026-04-21):
      archlucid reference-evidence --run <runId> [--out <dir>] [--include-demo]
      archlucid reference-evidence --tenant <tenantId> [--out <dir>] [--include-demo]   # AdminAuthority + ZIP
    File: ArchLucid.Cli/Commands/ReferenceEvidenceCommand.cs + ArchLucid.Cli.Tests/ReferenceEvidenceCommandTests.cs.
    Behaviour:
      - **--run:** GET pilot-run-deltas + first-value MD/PDF + sponsor PDF; refuses demo unless --include-demo.
      - **--tenant:** GET /v1/admin/tenants/{id}/reference-evidence ZIP; picks latest committed non-demo run unless --include-demo.
    Wire it into completions and the no-arg usage banner.

Stop and surface the OWNER question if any of these decisions are needed:
  - Discount-for-reference offer percent (suggest 15% per existing PRICING_PHILOSOPHY § 5.4).
  - Whether the first publishable reference will be the trial-first PLG row or the named
    design partner row (drives which placeholder file to graduate).

Tests must include: CLI argument parsing, demo-tenant refusal, --include-demo override,
missing-tenant 404, output-file shape (Markdown + PDF magic bytes + JSON schema).
Update docs/CHANGELOG.md and the cursor-prompts execution log.
```

---

## Prompt 2 — Collapse dual pipeline (ADR 0021 Phase 3)

**Title:** Unblock ADR 0021 Phase 3 — single pipeline with parity evidence and write-side façade

**Why:** Architectural Integrity is the largest engineering headroom item. ADR 0022 explicitly records Phase 3 blocked because the parity runbook is empty and `AuditEventTypes.Run` does not exist.

**Prompt to paste into Cursor:**

```text
You are executing the unblocking work for ADR 0021 Phase 3. Read these first:
- docs/adr/0021-coordinator-pipeline-strangler-plan.md
- docs/adr/0022-coordinator-phase3-deferred.md
- docs/adr/0010-dual-manifest-trace-repository-contracts.md
- docs/adr/0002-dual-persistence-architecture-runs-and-runs.md
- docs/runbooks/COORDINATOR_TO_AUTHORITY_PARITY.md
- ArchLucid.Api.Tests/Startup/DualPipelineRegistrationDisciplineTests.cs
- ArchLucid.Core/Audit/AuditEventTypes.cs
- ArchLucid.Persistence/Reads/UnifiedGoldenManifestReader.cs

Phase 3 needs three exit gates. Do parts A, B, C in order; STOP at the first OWNER gate
(see end of prompt) instead of forcing a deletion PR.

(A) Add the AuditEventTypes.Run nested catalog (Phase 2 catalog gate).
   - In ArchLucid.Core/Audit/AuditEventTypes.cs, add a nested `public static class Run`
     that mirrors today's CoordinatorRun* constants under one canonical namespace
     (RunStarted, RunExecuted, RunCommitted, ResultSubmitted, etc.).
   - Update the audit-core-const-count anchor in docs/AUDIT_COVERAGE_MATRIX.md and the
     CI grep guard in .github/workflows/ci.yml.
   - Update every emit site to write the new constant in addition to the legacy one
     (dual-emit window) -- DO NOT delete the legacy constants in this change set.
   - Add tests in ArchLucid.Core.Tests/Audit/ that assert legacy + canonical strings
     are emitted together for every coordinator orchestrator path.

(B) Build the write-side façade IRunCommitOrchestrator.
   - New interface ArchLucid.Application/Runs/IRunCommitOrchestrator.cs (its own file).
   - Implementation routes to the existing ArchitectureRunCommitOrchestrator today; the
     façade is what the four allow-list types in DualPipelineRegistrationDisciplineTests
     will eventually depend on after Phase 3 ships.
   - Add the façade as the only allow-listed dependency in the discipline test, leaving
     today's four-entry list intact for now (additive, not replacing).
   - Document the façade contract in docs/ARCHITECTURE_COMPONENTS.md.

(C) Write the parity-runbook automation harness.
   - New tool: scripts/ci/coordinator_parity_probe.py that queries
     `dbo.AuditEvents` for the last 24h and emits one Markdown row per day with
     coordinator-pipeline write counts vs authority-pipeline write counts.
     The script is a no-op when AuditEventTypes.Run does not exist yet (so it can
     ship before A is fully rolled out everywhere).
   - GitHub Actions workflow .github/workflows/coordinator-parity-daily.yml
     runs the probe nightly and appends to docs/runbooks/COORDINATOR_TO_AUTHORITY_PARITY.md
     via a sticky-comment-style edit (re-uses the same upsert pattern as the
     manifest-delta PR-comment action).
   - 14 contiguous green daily rows = ready to lift gate (iv).

OWNER GATE -- stop here and add a question to docs/PENDING_QUESTIONS.md if any of:
  - Whether the legacy CoordinatorRun* constants can be sunset on a fixed date
    (suggest 2026-07-20 per Phase 2 sunset clock already started).
  - Whether the parity probe is allowed to write to docs/ from CI (or must open a PR).
  - Whether ADR 0022 should be marked Superseded once 14 rows are green, or wait for
    actual Phase 3 deletion.

Tests required: AuditEventTypes.Run mirror parity (every legacy constant has a Run-side
twin), façade interface contract test, parity probe unit test (no SQL -- mock IAuditRepository).

Update docs/CHANGELOG.md and the cursor-prompts execution log.
```

### Execution log — Prompt 2 (ADR 0021 Phase 2 / 3 prep, 2026-04-21)

Shipped: **`AuditEventTypes.Run.*`** canonical strings + **dual-write** from coordinator create/execute/commit + failed durable audit; **`CoordinatorRunCatalogDurableDualWrite`**; **`IRunCommitOrchestrator`** / **`RunCommitOrchestratorFacade`** + DI; **`scripts/ci/coordinator_parity_probe.py`**, **`scripts/ci/test_coordinator_parity_probe.py`**, **`.github/workflows/coordinator-parity-daily.yml`**; runbook marker block; **`AUDIT_COVERAGE_MATRIX`** count **101**; **`DualPipelineRegistrationDisciplineTests`** façade resolution test; Core/Application tests. **OWNER gate:** three ADR 0021 / Phase 3 policy questions appended to **`docs/PENDING_QUESTIONS.md`** (legacy constant sunset date, whether nightly probe may commit to `docs/` on `main`, ADR 0022 supersede timing).

---

## Prompt 3 — Live Stripe + Azure Marketplace SaaS

**Title (owner-blocked):** Promote Stripe checkout to production and ship the Azure Marketplace SaaS listing

**Why:** Adoption Friction, Decision Velocity, Commercial Packaging. Until both rails are live, every deal is hand-rolled.

**Prompt to paste into Cursor:**

```text
You are taking the Stripe checkout path live in production and shipping the Azure
Marketplace SaaS listing. Read these first:
- docs/go-to-market/STRIPE_CHECKOUT.md
- docs/go-to-market/MARKETPLACE_PUBLICATION.md
- docs/go-to-market/PRICING_PHILOSOPHY.md
- docs/AZURE_MARKETPLACE_SAAS_OFFER.md
- docs/adr/0016-billing-provider-abstraction.md
- ArchLucid.Persistence/Migrations/078_BillingSubscriptions.sql
- ArchLucid.Persistence/Migrations/086_Billing_MarketplaceChangePlanQuantity.sql
- ArchLucid.Application.Tests/Billing/MarketplaceChangePlanWebhookMutationHandlerTests.cs
- ArchLucid.Application.Tests/Billing/MarketplaceChangeQuantityWebhookMutationHandlerTests.cs
- infra/terraform-storage/, infra/terraform-container-apps/, infra/terraform-private/

Do everything that does NOT require a real Stripe live key, real Marketplace credentials,
or a chargeback policy decision. STOP at the first OWNER gate.

(A) Production-readiness audit of the existing Stripe code path.
   - List every Stripe webhook handler today and its idempotency story.
   - Run through StartupConfigurationFactsReader / production-safety guards: ensure
     missing Stripe live keys fail-closed at startup (mirror the existing webhook HMAC
     guard in ArchLucidConfigurationRules.CollectProductionSafetyErrors).
   - Add a new production-safety rule: refuses to start if BillingProvider is Stripe
     and Stripe:Mode is "live" without Stripe:WebhookSecret AND Stripe:ApiKey present
     in environment / Key Vault.
   - Tests in ArchLucid.Api.Tests/StartupConfigurationFactsReaderTests.cs.

(B) Marketplace SaaS plan-SKU canonical doc.
   - New file docs/go-to-market/MARKETPLACE_PLAN_SKUS.md with three plan SKUs aligned
     to the three-tier model in PRICING_PHILOSOPHY.md. Mark every price as
     "owner-confirmed: <date>" or "owner-pending".
   - Add a CI check scripts/ci/check_marketplace_sku_alignment.py that compares the
     plan SKUs in this doc to PRICING_PHILOSOPHY.md and fails when they drift.

(C) Marketplace landing-page diff.
   - In docs/go-to-market/MARKETPLACE_PUBLICATION.md add a "publication checklist"
     table: every required field (logo, hero, screenshots, lead-form webhook URL,
     terms, privacy, support contact). For each row, link to the file in the repo
     that already contains the asset, or mark "owner-blocked: <what>".
   - Pre-fill the screenshots column from docs/go-to-market/SCREENSHOT_GALLERY.md.

OWNER GATES -- stop and surface to docs/PENDING_QUESTIONS.md when you hit any of:
  - Stripe live API key + webhook secret (rotate-once, store in Key Vault).
  - Chargeback / refund / dunning policy text for the order-form template.
  - Legal entity name to appear on Marketplace listing and on Stripe statements.
  - Marketplace lead-form webhook URL (one of the existing Logic Apps workflows
    under infra/terraform-logicapps/workflows/marketplace-fulfillment-handoff).
  - Final pricing per SKU (today PRICING_PHILOSOPHY.md is internal; Marketplace
    publication makes it public).

No code change goes to production-effective config; PR opens with environment-secret
references only. Update docs/CHANGELOG.md and the cursor-prompts execution log.
```

---

## Prompt 4 — External pen-test + SOC 2 Type I readiness

**Title (owner-blocked):** Commission external pen-test and start SOC 2 Type I readiness

**Why:** Trustworthiness, Procurement, Compliance, Security. Self-assessment cannot replace third-party signal.

**Prompt to paste into Cursor:**

```text
You are preparing the external-evidence stack for trust-tier procurement. Read these first:
- docs/security/pen-test-summaries/2026-Q2-SOW.md
- docs/security/pen-test-summaries/2026-Q2-REDACTED-SUMMARY.md
- docs/security/OWNER_SECURITY_ASSESSMENT_2026_Q2-DRAFT.md
- docs/security/COMPLIANCE_MATRIX.md
- docs/go-to-market/SOC2_ROADMAP.md
- docs/go-to-market/TRUST_CENTER.md
- SECURITY.md
- docs/security/RLS_RISK_ACCEPTANCE.md

Do everything you can without picking a vendor or paying an invoice. STOP at the first
OWNER gate.

(A) Pen-test SoW finalization.
   - In docs/security/pen-test-summaries/2026-Q2-SOW.md, complete every section that does
     NOT require a vendor name: scope (web app + API + LLM agent surface; explicit
     OUT-of-scope: marketing site, Mermaid CLI render path, demo seed routes), test
     window (suggest a 2-week window after the next minor release), credentials handed
     over (suggest two test tenants -- Reader-tier + Operator-tier -- and a dedicated
     pen-test API key prefix that audit logs can filter on), reporting format (CVSS v3.1,
     exec summary + technical findings + retest), retest clause.
   - Add an evidence-handling section: encrypted-at-rest only, NDA, redaction process
     for the public summary in 2026-Q2-REDACTED-SUMMARY.md.

(B) SOC 2 Type I readiness gap report scaffold.
   - New file docs/security/SOC2_TYPE1_READINESS_GAP.md that maps every Trust Services
     Criterion (CC1-CC9, A1, C1, PI1, P1) to existing controls in COMPLIANCE_MATRIX.md
     and marks gaps as Open / Partial / Met. Cite the existing control file for every
     "Met" row.
   - Add a CI guard scripts/ci/check_soc2_gap_evidence_links.py that asserts every
     "Met" row links to a real file in the repo (no broken evidence pointers).
   - Update docs/go-to-market/TRUST_CENTER.md with a one-paragraph status block: "SOC 2
     Type I readiness gap report on file; available under NDA via security@archlucid.dev."

(C) PGP key scaffolding for security@archlucid.dev (without owning the private key).
   - Update SECURITY.md to reference the future archlucid-ui/public/.well-known/pgp-key.txt
     URL and add a one-paragraph "how to verify" block.
   - Add a CI guard scripts/ci/check_security_contact_pgp.py that fails when SECURITY.md
     references the pgp-key.txt URL but the file is missing -- so the moment the owner
     drops a public key in the right path, the guard goes green automatically.

OWNER GATES -- stop and surface to docs/PENDING_QUESTIONS.md when you hit any of:
  - Pen-test vendor selection + budget + window confirmation.
  - SOC 2 Type I assessor selection + audit period start date.
  - PGP private-key custodianship and the public key (PEM-armored) to drop into
    archlucid-ui/public/.well-known/pgp-key.txt.

Update docs/CHANGELOG.md and the cursor-prompts execution log.
```

---

## Prompt 5 — Two enterprise workflow integrations (ServiceNow CR + Confluence page)

**Title:** Ship ServiceNow change-request creation and Confluence page publishing on commit

**Why:** Workflow Embeddedness, Stickiness, Adoption Friction. Architecture work needs to enter the existing CR/ticket flow without operator copy-paste.

**Prompt to paste into Cursor:**

```text
You are extending ArchLucid's first-party workflow integration catalog. Read these first:
- docs/INTEGRATION_EVENTS_AND_WEBHOOKS.md
- docs/contracts/archlucid-asyncapi-2.6.yaml
- schemas/integration-events/catalog.json
- ArchLucid.Integrations.AzureDevOps/   (entire assembly -- this is the reference shape)
- ArchLucid.Integrations.AzureDevOps.Tests/AzureDevOpsRequestBodyParityWithPipelineTaskTests.cs
- docs/integrations/AZURE_DEVOPS_PR_DECORATION_SERVER_SIDE.md
- docs/go-to-market/INTEGRATION_CATALOG.md

Goal: two new opt-in handlers that subscribe to com.archlucid.authority.run.completed,
modeled exactly on the AzureDevOps integration shape (typed HttpClient, configuration
section, integration-event handler, parity tests, opt-in via config).

(A) New assembly ArchLucid.Integrations.ServiceNow + tests:
   - IServiceNowChangeRequestCreator + ServiceNowChangeRequestCreator (REST table API
     for `change_request`, basic auth or OAuth -- pick OAuth, document why).
   - AuthorityRunCompletedServiceNowIntegrationEventHandler that creates one
     change_request per committed manifest with: short_description (run name + manifest
     version), description (Markdown rendering of the canonical first-value-report
     summary), category "Architecture", impact / risk pulled from the manifest's
     top-severity finding, link to the operator-shell run-detail URL, and a tag
     "archlucid.run_id=<runId>" so reverse-lookup is cheap.
   - Idempotency: dedupe key = run_id + manifest_version (skip if a change_request
     with that tag already exists).
   - Configuration section ServiceNow with Endpoint, ClientId/ClientSecret references
     to environment / Key Vault.
   - Unit tests: request body shape, idempotency dedupe, missing-config fail-closed,
     transient HTTP retry (re-use the existing resilience pipeline).

(B) New assembly ArchLucid.Integrations.Confluence + tests:
   - IConfluencePagePublisher + ConfluencePagePublisher (REST v2 -- create or update
     a page in a configured space; idempotency via title = "ArchLucid run <runId>"
     plus stable label "archlucid.run_id=<runId>").
   - AuthorityRunCompletedConfluenceIntegrationEventHandler that publishes the
     canonical first-value-report Markdown rendered to ADF (Atlassian Document Format)
     -- use a thin Markdown -> ADF converter implemented as one file per top-level node
     type (heading, paragraph, list, code-block, table); reuse FirstValueReportBuilder
     for the source Markdown so output cannot drift from the API endpoint.
   - Same configuration / fail-closed / dedupe / resilience pattern as ServiceNow.

(C) Worker DI registration.
   - In ArchLucid.Worker/Program.cs (and matching composition extensions), register
     both integrations behind a per-integration "Enabled" boolean that defaults to
     false; production-safety guard refuses to start when an integration is enabled
     without its required secrets present.

(D) Docs.
   - docs/integrations/SERVICENOW_CHANGE_REQUEST.md
   - docs/integrations/CONFLUENCE_PAGE_PUBLISHING.md
   - Add both rows to docs/go-to-market/INTEGRATION_CATALOG.md.

OWNER GATES -- stop and surface to docs/PENDING_QUESTIONS.md when you hit any of:
  - ServiceNow OAuth client registration vs basic-auth tradeoff (default OAuth; need
    target instance to register the OAuth app).
  - Confluence space key + parent page id for the published pages (per-tenant config
    or platform-wide default?).

Tests required: parity tests for both REST request bodies; idempotency dedupe; missing-
config fail-closed; transient-HTTP retry; integration-event handler subscribes to the
correct topic. Update docs/CHANGELOG.md and the cursor-prompts execution log.
```

---

## Prompt 6 — WCAG 2.2 AA + axe-core CI gate + continuously-running golden LLM cohort

**Title:** Enforce accessibility in CI and stand up the LLM golden-cohort drift detector

**Why:** Accessibility (38 -> bring to 70+), Usability, Correctness. Two thin investments with big credibility return.

**Prompt to paste into Cursor:**

```text
You are landing two parallel improvements: (A) accessibility CI gating + WCAG 2.2 AA
conformance statement, and (B) a continuously-running golden cohort that detects LLM
output drift between provider releases.

Read these first:
- archlucid-ui/e2e/helpers/axe-helper.ts
- archlucid-ui/e2e/                      (entire Playwright tree)
- archlucid-ui/package.json
- archlucid-ui/docs/TESTING_AND_TROUBLESHOOTING.md
- ArchLucid.AgentRuntime.Tests/Evaluation/AgentOutputEvaluationHarnessTests.cs
- ArchLucid.AgentRuntime.Tests/Evaluation/AgentOutputEvaluationHarnessGoldenFixtureTests.cs
- ArchLucid.AgentRuntime.Tests/Evaluation/PromptRegressionBaselineContractTests.cs
- .github/workflows/agent-eval-datasets-nightly.yml
- .github/workflows/live-e2e-nightly.yml
- .github/workflows/ci.yml

Part A -- WCAG 2.2 AA enforcement:
   1. Promote axe-helper.ts to a required step on every Playwright operator-shell
      route smoke (runs list, run detail, manifest, artifacts, governance, audit,
      compare, replay, /why-archlucid, /demo/preview).
   2. Configure axe with a "WCAG 2.2 AA" tag set; allow a documented exception list
      in archlucid-ui/e2e/axe-exceptions.json (one row per exception with rationale
      + owner + sunset date). CI fails on any violation NOT in that file.
   3. New file docs/accessibility/WCAG_22_AA_CONFORMANCE.md -- a public conformance
      statement listing scope (operator shell), method (axe-core 4.x + manual
      keyboard testing), known exceptions (read from axe-exceptions.json), and a
      contact (security@archlucid.dev or a new accessibility@ alias).
   4. Add a `?` keyboard-shortcut help overlay in the operator shell that lists
      every keyboard shortcut and links to the conformance statement.
   5. Update docs/go-to-market/TRUST_CENTER.md with a one-paragraph
      "Accessibility -- WCAG 2.2 AA with documented exceptions" block.

Part B -- Golden LLM cohort drift detector:
   1. New directory ArchLucid.AgentRuntime.Tests/GoldenCohort/ with 10-20 representative
      ArchitectureRequest fixtures (mix of cloud providers, scales, compliance regimes;
      keep each fixture small enough to stay under a sensible per-run LLM budget).
   2. New CLI tool: scripts/ci/run_golden_cohort.py that, given a configured LLM
      provider profile (Azure OpenAI default), runs each fixture end-to-end against a
      live API container (already started by docker-compose for live-e2e), captures
      the committed manifest hash + finding counts + ExplainabilityTrace shape, and
      writes a JSON snapshot to artifacts/golden-cohort/<date>/<fixture>.json.
   3. New GitHub Actions workflow .github/workflows/golden-cohort-nightly.yml that
      runs nightly, posts a sticky issue comment summarizing drift vs the previous
      green run (same upsert-sticky-comment pattern as the manifest-delta PR action),
      and opens a NEW issue (not a PR) when manifest hash drifts beyond a threshold
      (suggest: any new finding-category appearing or disappearing, OR > 20% change
      in finding count, OR ExplainabilityTrace completeness drop > 10%).
   4. Doc: docs/runbooks/GOLDEN_COHORT_DRIFT.md -- how to triage a drift issue
      (provider regression vs prompt change vs golden-fixture stale).

OWNER GATES -- stop and surface to docs/PENDING_QUESTIONS.md when you hit any of:
  - Azure OpenAI deployment dedicated to nightly golden-cohort runs (estimate
    monthly token cost so the owner can approve).
  - accessibility@ alias creation (or confirm reuse of security@).
  - WCAG conformance statement publication channel (Trust Center page only,
    or also marketing site /accessibility).

Tests required: axe-exception schema validation; golden-cohort snapshot diff unit
tests with synthetic fixtures (no live LLM in unit-test path).

Update docs/CHANGELOG.md and the cursor-prompts execution log.
```

---

---

# Ten next-best improvements (after the first six)

Ranked by remaining weighted headroom from the 2026-04-21 assessment, after subtracting whatever the first six already moved. Same paste-ready shape; same workspace rules apply.

---

## Prompt 7 — Two-minute `/demo/explain` video + comparison screenshot pair

**Title:** Ship a two-minute differentiation video and a "before/after" screenshot pair

**Why:** Differentiability is real on paper (multi-agent + provenance + governance) but takes too long to demo cold. A 30-second screenshot pair and a 2-minute video shrink the "what is this?" moment.

**Prompt:**

```text
You are producing differentiation collateral grounded in shipped V1 capabilities. Read first:
- docs/go-to-market/POSITIONING.md (§ 2 Pillar 2 -- Auditable decision trail; Live deep link section)
- docs/go-to-market/SCREENSHOT_GALLERY.md
- archlucid-ui/src/app/(operator)/demo/explain/page.tsx
- ArchLucid.Api/Controllers/Demo/DemoExplainController.cs
- ArchLucid.Host.Core/Demo/DemoReadModelClient.cs
- docs/EXECUTIVE_SPONSOR_BRIEF.md

Goal: two artifacts -- a deterministic recording script and a screenshot-pair generator.

(A) docs/go-to-market/recordings/DEMO_EXPLAIN_2_MINUTE_SCRIPT.md
   - Scene-by-scene script (timecodes, on-screen text overlays, narration).
   - 0:00-0:15 problem framing, 0:15-1:30 walk through provenance graph + citations
     side-by-side from /demo/explain (use the seeded Contoso run), 1:30-2:00 commit
     page + sponsor PDF download.
   - Every claim has a footnote citing the file or controller that backs it.
   - Recording prerequisites: docker-compose pilot up + Demo:Enabled=true + canonical
     ContosoRetailDemoIdentifiers.AuthorityRunBaselineId committed.
   - Add a "redaction checklist" section so any tenant-shaped data on screen is the
     canonical demo data only.

(B) New CLI command: archlucid screenshot-pair [--out <dir>]
   - File: ArchLucid.Cli/Commands/ScreenshotPairCommand.cs (its own file).
   - Drives a headless Playwright (re-use the existing archlucid-ui Playwright config
     via a small Node entrypoint under archlucid-ui/scripts/screenshot-pair.mjs that
     the CLI shells out to).
   - Captures two images at 1600x1000:
       1. archlucid-before.png  -- a placeholder Markdown rendered to PNG that
          represents "manual architecture review week 1" (use a small Markdown ->
          PNG renderer such as the existing ArchLucid.Application/Pilots/MarkdownPdfRenderer
          adapted; if PDF-only, render to PDF and convert to PNG via the same
          NullDiagramImageRenderer fallback chain).
       2. archlucid-after.png   -- screenshot of the operator-shell run-detail page
          for the seeded Contoso run with the sponsor banner visible.
   - Refuses to run unless the API responds 200 to /v1/demo/explain (so the demo
     seed is verified before capture).

(C) docs/go-to-market/SCREENSHOT_GALLERY.md gains a "Hero pair" section that points
    at the two captured images with alt-text suitable for an accessibility-conformant
    marketing page.

Tests: CLI argument parsing; refusal when /v1/demo/explain returns 404; output-file
existence + PNG magic bytes. Update docs/CHANGELOG.md and the cursor-prompts execution log.
```

---

## Prompt 8 — Public click-around `/demo/preview` sandbox

**Title:** Promote `/demo/preview` to a Docker-free public sandbox with a brief tour overlay

**Why:** Time-to-Value is already strong for evaluators with Docker. Procurement-channel buyers and analysts will not run Docker. A read-only public click-around closes that gap without weakening security.

**Prompt:**

```text
You are extending the cached anonymous demo preview surface so a buyer can click through
a few read-only pages without signing in. Read first:
- docs/adr/0027-demo-preview-cached-anonymous-commit-page.md
- docs/DEMO_PREVIEW.md
- ArchLucid.Api/Controllers/Demo/   (DemoController + DemoExplainController + DemoPreviewController)
- ArchLucid.Host.Core/Demo/         (DemoReadModelClient + IDemoCommitPagePreviewClient + IDemoSeedRunResolver)
- archlucid-ui/src/app/(operator)/demo/   (existing demo routes)
- ArchLucid.Api/Filters/FeatureGateFilter.cs + ArchLucid.Api/Attributes/FeatureGateAttribute.cs

Goal: a four-route public sandbox under archlucid-ui/src/app/sandbox/ that mirrors the
operator-shell run-detail experience for read-only browsing of the canonical demo run.

(A) Server-side surface (re-use feature-gate pattern):
   - GET /v1/sandbox/run        -- summary, manifest version, committed timestamp
   - GET /v1/sandbox/manifest   -- redacted golden manifest (drop ExternalRefs)
   - GET /v1/sandbox/explain    -- aggregate explanation + citations (already exists)
   - GET /v1/sandbox/audit      -- last 50 audit rows, scope-pinned to demo tenant
   Each new endpoint:
     * [FeatureGate(FeatureGateKey.DemoEnabled)] -- 404 in production hosts.
     * Cache-Control: public, max-age=300; In-process 5-min TTL identical to
       Demo:PreviewCacheSeconds (re-use the same options binding).
     * Always returns IsDemoData=true and the "demo tenant -- replace before publishing"
       banner in the payload.
     * Anonymous -- no auth scheme requirement, but rate-limited under the existing
       'fixed' policy plus a new 'sandbox' policy (60/min per IP, half of fixed).

(B) UI surface:
   - archlucid-ui/src/app/sandbox/layout.tsx -- minimal shell with a fixed top-bar
     "DEMO -- read-only sandbox -- not your data" + a "Try with your own brief" CTA
     that links to /signup (or staging-funnel sign-in if /signup not live).
   - archlucid-ui/src/app/sandbox/run/page.tsx          -- run detail
   - archlucid-ui/src/app/sandbox/manifest/page.tsx     -- manifest summary
   - archlucid-ui/src/app/sandbox/explain/page.tsx      -- provenance + citations
   - archlucid-ui/src/app/sandbox/audit/page.tsx        -- last 50 audit rows
   - First-visit one-time tour overlay (4 steps, dismissible to localStorage).
   - axe-core green per WCAG 2.2 AA (chain into the same Playwright suite added
     by Prompt 6).

(C) Hardening:
   - New ArchLucidConfigurationRules production-safety check: refuses to start when
     Demo:Enabled=true AND ASPNETCORE_ENVIRONMENT=Production AND no edge-rate-limit
     header (X-Forwarded-RateLimited) is present in the configured ingress -- so
     production cannot accidentally serve sandbox traffic without edge throttling.
   - Sandbox routes never call /v1/explain or /v1/provenance directly; they go
     through DemoReadModelClient so tenant-scope is hard-pinned.

OWNER GATES -- stop and surface to docs/PENDING_QUESTIONS.md if any:
  - Whether sandbox should ship in staging only (no production exposure ever) or in
    a dedicated demo-only deployment behind sandbox.archlucid.com.
  - Marketing CTA target ("Try with your own brief" -> /signup vs hosted staging).

Tests: API integration (404 when Demo:Enabled=false; payload always carries IsDemoData=true;
rate-limited); Vitest snapshots; live Playwright tour-overlay test.
Update docs/CHANGELOG.md and the cursor-prompts execution log.
```

---

## Prompt 9 — Operator command palette (cmd-K) + task-mode home templates

**Title:** Add a cmd-K command palette and task-mode templates on the operator home page

**Why:** Usability headroom is large because three layers + tier × authority composition hides power-user flows. A palette + task templates collapse the cognitive cost without changing the disclosure model.

**Prompt:**

```text
You are extending archlucid-ui to make every action discoverable in one keystroke and
to add task-mode home templates. Read first:
- archlucid-ui/src/lib/nav-config.ts
- archlucid-ui/src/lib/nav-shell-visibility.ts
- archlucid-ui/src/lib/current-principal.ts
- archlucid-ui/src/lib/authority-seam-regression.test.ts
- archlucid-ui/src/lib/authority-execute-floor-regression.test.ts
- archlucid-ui/src/components/EmailRunToSponsorBanner.tsx
- docs/operator-shell.md

(A) Command palette (cmd-K / ctrl-K on the operator shell):
   - New file archlucid-ui/src/components/CommandPalette.tsx + CommandPalette.test.tsx.
   - Source of truth for command rows = nav-config.ts (so a single seam stays canonical).
   - Each row carries: label, target route, requiredAuthority, layer tier.
   - Composition rule = tier ∩ rank (re-use authority-seam composition exactly so the
     palette never reveals a row the nav would hide).
   - Keyboard-only: open with cmd-K / ctrl-K, fuzzy filter, arrow-key navigation,
     enter to navigate, escape to close. axe-core green per WCAG 2.2 AA.
   - "Recently used" section persisted to localStorage (top 5).

(B) Task-mode home templates:
   - On archlucid-ui/src/app/(operator)/page.tsx (or wherever the operator home renders),
     add a "Start a task" grid above the existing runs list:
       * "Run a compliance review"   -> /runs/new?template=compliance
       * "Compare two runs"          -> /compare
       * "Replay last commit"        -> /comparisons (with the latest replayable comparison preselected)
       * "Generate sponsor report"   -> /value-report (already exists)
     Each card respects authority -- soft-disable when the user lacks the required
     authority, with the existing useEnterpriseMutationCapability() hook pattern.
   - Templates carry a small "what this does" tooltip and an estimated time-to-value
     pulled from the same FirstValueReportBuilder copy.

(C) Tests:
   - Cross-module Vitest archlucid-ui/src/lib/command-palette-seam.test.ts that asserts:
       * Every nav row appears in the palette (tier+authority preserved).
       * No palette row exists without a matching nav row (palette cannot leak).
   - Vitest archlucid-ui/src/components/CommandPalette.test.tsx for keyboard, fuzzy,
     authority-aware filtering, recently-used persistence.
   - Playwright archlucid-ui/e2e/operator-command-palette.spec.ts that opens the
     palette, navigates to one Core Pilot row and one Enterprise row (Reader principal
     should not see the Enterprise row).

Update docs/operator-shell.md, docs/PRODUCT_PACKAGING.md § 3 (cross-surface lock),
docs/CHANGELOG.md and the cursor-prompts execution log.
```

---

## Prompt 10 — Weekly exec digest email

**Title:** Schedule a weekly exec digest email per tenant

**Why:** Sponsor visibility today depends on the operator clicking the share button. A scheduled digest closes the loop and earns the next renewal conversation.

**Prompt:**

```text
You are adding a scheduled weekly exec digest email per tenant. Read first:
- ArchLucid.Application/Pilots/FirstValueReportBuilder.cs
- ArchLucid.Application/Pilots/PilotRunDeltaComputer.cs
- ArchLucid.Application/Common/   (DocxValueReportRenderer, ValueReportReviewCycleSectionFormatter)
- ArchLucid.Persistence/Migrations/076_SentEmails.sql
- ArchLucid.Persistence/Migrations/082_TenantNotificationChannelPreferences.sql
- ArchLucid.Persistence/Migrations/011_DigestDelivery.sql
- ArchLucid.Application.Tests/   (any DigestDelivery / SentEmails tests)
- infra/terraform-logicapps/workflows/trial-lifecycle-email/

(A) New application service IExecDigestComposer in ArchLucid.Application/ExecDigest/
   (its own file) + ExecDigestComposer.cs that, given a tenantId and a UTC week range,
   returns an immutable ExecDigest record containing:
     * Manifests committed (count + top 3 by significance via existing comparison engine).
     * Governance approvals processed (count, breakdown approved / rejected / pending,
       approvals breaching SLA).
     * Compliance drift trend (re-use the existing ComplianceDriftTrendService chart values
       for the operator UI -- write to a Markdown table for email).
     * Findings introduced + resolved (delta vs previous week).
     * Two CTAs: open the dashboard, generate a sponsor report.
   - All numbers come from existing services (no new SQL aggregates) -- if a number
     is unavailable, omit the row gracefully.

(B) New hosted background job:
   - ArchLucid.Worker/HostedServices/ExecDigestWeeklyHostedService.cs (its own file)
     scheduled via the existing background-jobs CLI scheduler -- runs every Monday
     08:00 in the tenant's configured timezone (default UTC).
   - For each tenant where TenantNotificationChannelPreferences indicates an exec
     digest channel is enabled, compose the digest, render to HTML + plaintext,
     and enqueue an outbound email through the existing SentEmails outbox path.
   - Idempotency key = tenantId + ISO week. Re-runs in the same week are no-ops.

(C) Operator UI surface:
   - New page archlucid-ui/src/app/(operator)/settings/exec-digest/page.tsx
     to enable / disable the digest, set recipient email(s) (default = sponsor
     captured during archlucid try), choose timezone, choose day-of-week.
   - Backed by GET / POST /v1/tenant/exec-digest-preferences with Reader+ to read,
     Operator+ to write.

(D) Email templates:
   - templates/email/exec-digest.html + .txt (Markdown source rendered to both),
     with explicit unsubscribe link that flips the channel preference off in one
     click (signed token, no auth required to unsubscribe).

OWNER GATES -- stop and surface to docs/PENDING_QUESTIONS.md if any:
  - Outbound email transport (existing SentEmails outbox uses what underneath today?
    -- read 076_SentEmails.sql + the outbox processor, then ask only if the answer is
    "owner has not picked yet"; otherwise re-use).
  - From-address branding (no-reply@archlucid.com vs success@archlucid.com).

Tests: composer unit tests with stub services (every section graceful-degrades),
hosted-service idempotency, controller integration tests for read/write of the
preferences, unsubscribe-token signing/verification tests.
Update docs/CHANGELOG.md and the cursor-prompts execution log.
```

---

## Prompt 11 — Five vertical starter briefs + matching policy packs

**Title:** Add five vertical starter briefs + matching policy packs under `templates/`

**Why:** Templates/Accelerator richness scored 30/100 -- the lowest commercial score. Five small content templates disproportionately raise perceived breadth.

**Prompt:**

```text
You are expanding the templates/ directory with five vertical starter briefs and
five matching policy packs. Read first:
- templates/archlucid-finding-engine/README.md
- templates/archlucid-api-endpoint/README.md
- ArchLucid.Contracts/Architecture/   (ArchitectureRequest schema)
- ArchLucid.Decisioning/Policy/       (PolicyPack* types)
- docs/HOWTO_FINDING_ENGINE_PLUGINS.md
- docs/CONCEPTS.md
- docs/go-to-market/IDEAL_CUSTOMER_PROFILE.md

Goal: ten new content artifacts -- five paired (brief + policy pack) per vertical:
  1. financial-services
  2. healthcare
  3. retail
  4. saas
  5. public-sector

For each vertical create:

(A) templates/briefs/<vertical>/brief.md
   - A complete inputs/brief.md ready to paste into a project's archlucid.json scaffold.
   - Realistic system name, environment, cloud provider (Azure default), regulatory
     constraints (e.g. PCI-DSS for retail, HIPAA for healthcare, GDPR-anchor for
     public-sector EU, SOC 2 for saas, GLBA + SOX for financial-services).
   - 200-400 words of prose that exercises the Topology, Cost, Compliance, and Critic
     agents non-trivially.
   - Header comment block: "Vertical: <x> -- Time to first commit (estimated): <minutes>".

(B) templates/policy-packs/<vertical>/policy-pack.json
   - A real PolicyPack JSON valid against the existing schema in
     ArchLucid.Decisioning/Policy/.
   - 8-15 rules per pack, each with severity and rationale, citing the regulatory
     anchor inline.
   - Loadable via the existing policy-pack ingestion path; add an integration test
     in ArchLucid.Decisioning.Tests/Policy/ that loads each pack and confirms it
     materializes a non-empty rule list.

(C) templates/README.md gains a top-level table listing all five verticals + links.

(D) Operator UI surface:
   - On the first-run wizard (or wherever templates can be picked), expose a
     "Start from a vertical template" picker that lists the five briefs.
   - On the policy-pack admin page, expose "Import a vertical policy pack" with the
     same five options.

OWNER GATES -- stop and surface to docs/PENDING_QUESTIONS.md if any:
  - Whether public-sector should default to US (FedRAMP / StateRAMP) or EU (GDPR)
    framing.
  - Whether any of the five verticals should be kept private to a paid tier
    (default: ship all five with Core Pilot).

Tests: schema-validation tests per pack; brief markdown-link checker;
operator-shell template-picker Vitest snapshot.
Update docs/CHANGELOG.md and the cursor-prompts execution log.
```

---

## Prompt 12 — Architecture-on-one-page + operator atlas

**Title:** Replace ten scattered entry docs with one architecture poster and one operator atlas

**Why:** Cognitive load and documentation volume cost more than the content earns. Two consolidating documents shrink the surface without losing depth (the deep docs become reference-only, linked from the poster).

**Prompt:**

```text
You are consolidating the ArchLucid documentation entry surface. Read first:
- README.md
- docs/ARCHITECTURE_INDEX.md
- docs/ARCHITECTURE_CONTEXT.md
- docs/ARCHITECTURE_CONTAINERS.md
- docs/ARCHITECTURE_COMPONENTS.md
- docs/ARCHITECTURE_FLOWS.md
- docs/V1_SCOPE.md
- docs/CORE_PILOT.md
- docs/OPERATOR_DECISION_GUIDE.md
- docs/SYSTEM_MAP.md
- docs/FIRST_FIVE_DOCS.md

Goal: two new canonical documents that each entry doc above must link to or fold into.

(A) docs/ARCHITECTURE_ON_ONE_PAGE.md
   - One Mermaid C4 system-context diagram + one Mermaid container diagram, both
     rendered inline.
   - Below each diagram, a six-row table mapping every node to (a) the project under
     ArchLucid.* that owns it, (b) the docs/ deep-dive that explains it, (c) the test
     project that exercises it.
   - One happy-path arrow trace ("create run -> execute -> commit -> sponsor PDF")
     with cross-links to the three existing flows in ARCHITECTURE_FLOWS.md.
   - Strict size budget: under 400 lines including Mermaid source.
   - Add a CI assertion (scripts/ci/check_doc_size_budget.py -- see Prompt 16) that
     keeps it under 500 lines.

(B) docs/OPERATOR_ATLAS.md
   - Single map of every operator action grouped by Core Pilot / Advanced / Enterprise.
   - Each row: action name, CLI command, API endpoint, operator-shell route, required
     authority, primary runbook link.
   - Replaces the role-by-role onboarding files as the canonical operator reference;
     the day-one-* files remain but link "for the canonical action map see OPERATOR_ATLAS.md".

(C) Update README.md "Architecture docs (internal)" section so the architecture poster
   is the first link and the older index becomes "for deeper details".
(D) Update docs/ARCHITECTURE_INDEX.md to point at the poster as the canonical entry.

OWNER GATES: none expected. If any owner-only naming decision arises (e.g. whether
the operator atlas should live under docs/ or under archlucid-ui/docs/), surface it
to docs/PENDING_QUESTIONS.md.

Tests: doc-size budget guard (Prompt 16); broken-link checker over the new docs;
mermaid-syntax validator (re-use the existing diagram check if present, else add
scripts/ci/check_mermaid_syntax.py that compiles every fenced mermaid block via
mermaid-cli when available, gracefully skips when mmdc missing).
Update docs/CHANGELOG.md and the cursor-prompts execution log.
```

---

## Prompt 13 — Pre-filled CAIQ Lite + SIG Lite + standard MSA bundle

**Title:** Ship a procurement bundle: pre-filled CAIQ Lite + SIG Lite + standard MSA

**Why:** Procurement Readiness scored 50/100 because the starter pack exists but the actual questionnaire answers do not. Pre-filling them once removes 60+ days from each enterprise deal.

**Prompt:**

```text
You are pre-filling the standard procurement questionnaires and shipping a standard MSA
template grounded in real shipped controls. Read first:
- docs/security/COMPLIANCE_MATRIX.md
- docs/security/MULTI_TENANT_RLS.md
- docs/security/MANAGED_IDENTITY_SQL_BLOB.md
- docs/security/PII_RETENTION_CONVERSATIONS.md
- docs/security/RLS_RISK_ACCEPTANCE.md
- docs/go-to-market/SUBPROCESSORS.md
- docs/go-to-market/DPA_TEMPLATE.md
- docs/go-to-market/TENANT_ISOLATION.md
- docs/go-to-market/TRUST_CENTER.md
- docs/go-to-market/SLA_SUMMARY.md
- docs/go-to-market/INCIDENT_COMMUNICATIONS_POLICY.md
- docs/AUDIT_COVERAGE_MATRIX.md
- docs/AUDIT_RETENTION_POLICY.md
- SECURITY.md

Goal: four new files under docs/security/procurement/ + a CI guard that keeps the
answers in sync with the actual control set.

(A) docs/security/procurement/CAIQ_V4_LITE_PREFILLED.md
   - The CAIQ v4 Lite question set (ask docs/PENDING_QUESTIONS.md if the assistant
     cannot find the canonical question list -- it ships from CSA, not the repo;
     the assistant should reproduce the public Lite subset only).
   - Every answer cites the file in the repo that backs it (Yes / No / N/A only;
     no narrative answers without a source).
   - Open questions explicitly marked "owner-blocked" instead of guessed.

(B) docs/security/procurement/SIG_LITE_PREFILLED.md
   - Same approach for SIG Lite. Explicitly skip non-Lite SIG question sets.

(C) docs/security/procurement/STANDARD_MSA.md
   - Standard Master Service Agreement template (skeleton only -- legal owner fills
     definitive language). Sections: definitions, services, fees + payment, term +
     termination, confidentiality, IP, warranties + disclaimers, liability cap,
     indemnification, data protection (point at DPA_TEMPLATE.md), governing law
     (placeholder).
   - Mark every "owner-blocked: legal" placeholder so this never ships as final.

(D) docs/security/procurement/README.md
   - Index of the bundle + how to use it during a deal cycle (which doc goes
     first, what NDA gating, who can fill the owner-blocked rows).

(E) CI guard scripts/ci/check_procurement_evidence_links.py
   - Asserts every "Yes" / "Met" cell in CAIQ + SIG cites a path that exists in
     the repo (no broken evidence pointers).

OWNER GATES -- stop and surface to docs/PENDING_QUESTIONS.md if any:
  - Legal entity name + governing-law jurisdiction for the standard MSA.
  - Whether to expose the pre-filled CAIQ + SIG publicly or behind an NDA-gated
    Trust Center download.
  - Liability-cap default (typical: 12 months of fees).

Tests: CI guard unit tests; markdown-link checker over the new bundle.
Update docs/go-to-market/TRUST_CENTER.md to link the bundle (NDA-gated by default).
Update docs/CHANGELOG.md and the cursor-prompts execution log.
```

---

## Prompt 14 — Public status page

**Title:** Publish `status.archlucid.com` backed by the existing synthetic-probe workflow

**Why:** Reliability + Trustworthiness both lift when buyers can see real uptime instead of taking it on trust. The synthetic probe already runs.

**Prompt:**

```text
You are publishing a public status page backed by the synthetic-probe workflow.
Read first:
- .github/workflows/api-synthetic-probe.yml
- ArchLucid.Api/Controllers/HealthController.cs (or whichever controller serves /health/*)
- docs/RELEASE_SMOKE.md
- docs/runbooks/ (look for any incident-comms runbook)
- docs/go-to-market/INCIDENT_COMMUNICATIONS_POLICY.md
- docs/go-to-market/SLA_SUMMARY.md
- infra/terraform-storage/  (for static-site hosting pattern)

Goal: a static status site fed by JSON snapshots written by the existing probe.

(A) Probe writes a JSON snapshot per run.
   - Extend api-synthetic-probe.yml to write a snapshot to a public storage account
     container (path: status/<UTC date>/<UTC time>.json) containing:
       * probe_started_utc, probe_finished_utc
       * /health/live, /health/ready, /health observed status + latency_ms
       * regional probe per configured region (start with primary centralus)
   - Idempotent overwrite of status/latest.json after each run.

(B) Static status site.
   - New directory marketing-site/status/ (or archlucid-ui/public/status/) holding
     index.html + status.js that fetches status/latest.json + the last 30 days of
     hourly snapshots and renders:
       * Current status banner (Operational / Degraded / Major / Maintenance).
       * 30-day uptime per check (live / ready / full).
       * Last 10 incidents (read from a manually-maintained incidents.json that
         the on-call updates -- ship with an empty file).
   - axe-core green per WCAG 2.2 AA.

(C) Terraform.
   - infra/terraform-status-page/ -- a small module: storage account + static-site
     hosting + Front Door route status.archlucid.com -> the static site +
     CDN cache 60s on JSON, 300s on HTML.
   - Storage account refuses public network access except from Front Door (private
     endpoint pattern); SMB / port 445 stays closed (per workspace rule).

(D) Docs.
   - docs/runbooks/STATUS_PAGE.md -- how to mark an incident, how to update
     incidents.json, how to roll back.
   - docs/go-to-market/SLA_SUMMARY.md gains a "Live status: status.archlucid.com" line.

OWNER GATES -- stop and surface to docs/PENDING_QUESTIONS.md if any:
  - DNS / TLS cutover for status.archlucid.com (mirrors the production-DNS owner item).
  - Storage-account subscription placement (production sub vs separate marketing sub).
  - Whether to expose per-region probe data publicly or only the aggregate status.

Tests: probe-snapshot JSON schema + integration test against a stubbed health controller;
static-site snapshot test (Vitest) confirming the UI degrades gracefully when status/
latest.json is missing.
Update docs/CHANGELOG.md and the cursor-prompts execution log.
```

---

## Prompt 15 — Agent-output evaluation dashboard in the operator UI

**Title:** Surface the existing AgentOutputEvaluator scores as a per-run quality dashboard

**Why:** AI/Agent Readiness is high (75) precisely because the harness already exists -- but the scores are not visible in product. Surfacing them turns "trust me, we score" into "look at this run's score."

**Prompt:**

```text
You are surfacing existing agent-output evaluation scores as an operator-shell page.
Read first:
- ArchLucid.AgentRuntime/Evaluation/AgentOutputEvaluator.cs
- ArchLucid.AgentRuntime/Evaluation/AgentOutputSemanticEvaluator.cs
- ArchLucid.AgentRuntime/Evaluation/AgentOutputEvaluationRecorder.cs
- ArchLucid.AgentRuntime.Tests/Evaluation/AgentOutputEvaluationHarnessTests.cs
- ArchLucid.Persistence/Migrations/063_AgentOutputEvaluationResults.sql
- ArchLucid.Persistence (find the AgentOutputEvaluationResult repository)
- ArchLucid.Application (find any service exposing evaluation results)

Goal: read API + operator-shell page that reads, never writes, the existing evaluation
results table.

(A) Read API: GET /v1/runs/{runId}/agent-evaluation
   - File: ArchLucid.Api/Controllers/Authority/RunAgentEvaluationController.cs (its own file).
   - Returns one row per agent task in the run: agent kind (Topology / Cost / Compliance
     / Critic), structural completeness score (0-100), semantic quality score (0-100),
     pass/fail vs configured quality gate, evaluator version, evaluated_utc.
   - ReadAuthority. Rate-limit: fixed.
   - 404 when runId is unknown; 200 with empty array when the run has no evaluations
     yet (run still in progress).

(B) Application service IRunAgentEvaluationQueryService -- composes the existing
   AgentOutputEvaluationResult repository with the run's task list so missing
   evaluations render as "not yet scored" rather than absent rows.

(C) Operator shell page: archlucid-ui/src/app/(operator)/runs/[runId]/agent-evaluation/page.tsx
   - Card per agent kind: score gauges (structural + semantic), pass/fail badge,
     "what this measures" tooltip linking to docs/AGENT_OUTPUT_EVALUATION.md.
   - Row per finding contributed by the agent with the same scores at finding
     granularity (when AgentOutputEvaluator records per-finding scores; otherwise
     show aggregate only).
   - axe-core green per WCAG 2.2 AA.
   - Cross-link from the run-detail page sidebar (visible to ReadAuthority).

(D) New doc docs/AGENT_OUTPUT_EVALUATION.md
   - Plain-language explanation of structural completeness vs semantic quality,
     how the gate is configured, what failing looks like, and how to investigate.
   - Cross-linked from /demo/explain so sponsors can read it without signing in.

OWNER GATES -- stop and surface to docs/PENDING_QUESTIONS.md if any:
  - Whether the dashboard should be Core Pilot (visible to every Reader) or
    Advanced Analysis (gated to architects). Suggest Core Pilot.

Tests: API integration (404 on unknown run; empty-array on in-progress run; populated
on completed run); query-service unit tests; Vitest snapshot for the page; Playwright
e2e that runs through commit + opens the dashboard.
Update docs/CHANGELOG.md and the cursor-prompts execution log.
```

---

## Prompt 16 — Persistence consolidation execution + doc-size CI budget

**Title:** Execute the persistence-consolidation proposal and enforce a doc-size budget

**Why:** Maintainability + Cognitive Load both lift when the codebase shrinks (the consolidation proposal already exists) and when docs cannot grow unbounded.

**Prompt:**

```text
You are executing the persistence-consolidation proposal and adding a doc-size CI budget.
Read first:
- docs/PROJECT_CONSOLIDATION_PROPOSAL_PERSISTENCE.md
- docs/PROJECT_CONSOLIDATION_PROPOSAL.md
- docs/PERSISTENCE_SPLIT.md
- docs/CONFIG_BRIDGE_SUNSET.md
- docs/REPO_HYGIENE.md
- ArchLucid.Persistence/, ArchLucid.Persistence.Data/ (if separate),
  ArchLucid.Persistence.Coordination/, ArchLucid.Persistence.Integration/,
  ArchLucid.Persistence.Runtime/ -- whatever the proposal says to consolidate

Part A -- Persistence consolidation:
   - Walk PROJECT_CONSOLIDATION_PROPOSAL_PERSISTENCE.md decision-by-decision.
   - For each "merge" decision in the proposal: move the affected files into the
     target project, update csproj references, run dotnet build + dotnet test.
   - For each "split" decision: same in reverse.
   - DO NOT touch historical SQL migration files (workspace rule -- 001-028 are
     immutable).
   - DO NOT delete coordinator interfaces (Phase 3 retirement is Prompt 2's job).
   - Update DI registration map docs/DI_REGISTRATION_MAP.md.
   - Update docs/ARCHITECTURE_CONTAINERS.md to reflect the consolidated container set.

Part B -- Doc-size CI budget:
   - New file scripts/ci/check_doc_size_budget.py:
       * Walks docs/**/*.md.
       * Warns (CI annotation, no fail) at > 500 lines.
       * Fails CI at > 1500 lines.
       * Allow-list file scripts/ci/doc-size-allowlist.json with a per-doc budget
         override + rationale (e.g. AUDIT_COVERAGE_MATRIX.md, CHANGELOG.md, large
         living docs).
   - Wire into .github/workflows/ci.yml as a non-blocking-warn job first; flip to
     blocking-fail in a follow-up PR after the offenders are split.
   - Inventory pass: list every doc currently > 1500 lines and add each to the
     allow-list with rationale + a TODO line + an issue link (use a single tracking
     issue archlucid/issues/<n> -- if the assistant cannot create one, leave a
     TODO with a clear marker).

Part C -- Tests + docs:
   - Unit tests for check_doc_size_budget.py covering: under-budget, warn-zone,
     fail-zone, allow-list override, missing-allow-list-file (treat as empty).
   - Update docs/REPO_HYGIENE.md with a "Doc size budget" section explaining the
     limits and the allow-list workflow.

OWNER GATES -- stop and surface to docs/PENDING_QUESTIONS.md if any:
  - Whether the consolidation proposal's "split" decisions (if any) should ship in
    the same PR as the "merge" decisions, or land in two PRs (prefer two for
    reviewer sanity unless the proposal says otherwise).
  - Final blocking-vs-warn flip date for the doc-size CI guard.

Tests: full dotnet test pass after consolidation; new doc-size guard unit tests;
broken-link checker over moved docs.
Update docs/CHANGELOG.md and the cursor-prompts execution log.
```

---

## How to use these prompts

- **Run them one at a time.** Each one assumes the previous prompts may not have shipped yet; they do not depend on each other.
- **Owner-blocked prompts (#3 + #4) will stop early** with a question logged to `docs/PENDING_QUESTIONS.md`. That is intentional — they pre-build everything mechanical first.
- **Every prompt updates `docs/CHANGELOG.md`** and ends by re-running the relevant CI guards locally so a green PR is the default outcome.
- **No subagents.** Per the workspace rule `Do-The-Work-Yourself.mdc`, every prompt asks the assistant to do the work directly with the standard tools.
