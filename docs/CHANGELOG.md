> **Scope:** ArchLucid changelog - full detail, tables, and links in the sections below.

# ArchLucid changelog

Release entries newest-first. Each section condenses the detailed prompt logs preserved in `docs/archive/`.

## 2026-04-21 — ADR 0021 Phase 3 blocked (exit gates)

**Architectural integrity — strangler Phase 3 not executed:** [ADR 0022](adr/0022-coordinator-phase3-deferred.md) records **failed** ADR 0021 § Phase 3 exit gates: **(iv)** `docs/runbooks/COORDINATOR_TO_AUTHORITY_PARITY.md` still has `*(TBD)*` rows only (no 14-day zero-write window); **Phase 2 catalog** — no `AuditEventTypes.Run` nested class in `AuditEventTypes.cs`. Mechanical verification: [`artifacts/phase3/gate-verification.md`](../artifacts/phase3/gate-verification.md). **No** coordinator concrete/interface deletion, **no** `IRunCommitOrchestrator` façade — fail-closed per strangler governance. **Follow-up:** unblock PR A after parity + Phase 2 evidence; **PR B** (audit constants + interface deletion) remains **≥ 2026-07-21** (30-day gate (i) after PR A + **Sunset 2026-07-20**).

---

## 2026-04-21 — Production Azure subscription recorded

**Operations — production subscription mapping captured:** Owner provided the dedicated production Azure subscription ID (`aab65184-5005-4b0d-a884-9e28328630b1`). New canonical doc **[`docs/AZURE_SUBSCRIPTIONS.md`](AZURE_SUBSCRIPTIONS.md)** is now the **single source of truth** for ArchLucid Azure subscription mapping (staging, production, quarterly greenfield CI), the GitHub Environment secret each one maps to (`AZURE_SUBSCRIPTION_ID` per environment in [`cd.yml`](../.github/workflows/cd.yml)), the default region (`centralus`), and the OIDC-only login pattern. Subscription IDs are intentionally **not** hard-coded in `infra/**/*.tf` or example tfvars — `azure/login@v2` exports `ARM_SUBSCRIPTION_ID` for every Terraform step. Item 1 in [`docs/PENDING_QUESTIONS.md`](PENDING_QUESTIONS.md) moved from **Still open** to **Resolved**; cross-links added from [`REFERENCE_SAAS_STACK_ORDER.md`](REFERENCE_SAAS_STACK_ORDER.md) and [`FIRST_AZURE_DEPLOYMENT.md`](FIRST_AZURE_DEPLOYMENT.md). Operator action: set the `AZURE_SUBSCRIPTION_ID` secret on the **`production`** GitHub Environment to the recorded value (with required reviewers enabled).

---

## 2026-04-21 — Seven-improvements batch (Azure DevOps, Stryker scopes, SaaS region, security.txt)

**Workflow embeddedness — Azure DevOps PR decoration (opt-in):** New assembly **`ArchLucid.Integrations.AzureDevOps`** with **`IAzureDevOpsPullRequestDecorator`** / **`AzureDevOpsPullRequestDecorator`** (REST 7.1 PR **statuses** + **threads**) and **`AuthorityRunCompletedAzureDevOpsIntegrationEventHandler`** consuming **`com.archlucid.authority.run.completed`**. Worker DI registers typed **`HttpClient`** + configuration section **`AzureDevOps`** (`appsettings.json`). Tests: **`ArchLucid.Integrations.AzureDevOps.Tests`**. Docs: **[`docs/integrations/AZURE_DEVOPS_PR_DECORATION.md`](integrations/AZURE_DEVOPS_PR_DECORATION.md)**; catalog row in **[`docs/go-to-market/INTEGRATION_CATALOG.md`](go-to-market/INTEGRATION_CATALOG.md)**.

**Engineering — Stryker scoped namespaces (CI only):** New **`stryker-config.decisioning-merge.json`** (label **DecisioningMerge**) and **`stryker-config.application-governance.json`** (label **ApplicationGovernance**); **`stryker-baselines.json`** starts both at **55.0**; weekly matrix + **`stryker_pr_plan.py`** FULL_MATRIX and path rules updated; **`docs/MUTATION_TESTING_STRYKER.md`** + **`docs/TEST_STRUCTURE.md`** tables extended. Local Stryker runs not required — ratchet via **`refresh_stryker_baselines.py`** after green scheduled jobs.

**Deployability — default Azure region:** **`infra/terraform-container-apps/variables.tf`** `location` default **`centralus`**; **[`docs/REFERENCE_SAAS_STACK_ORDER.md`](REFERENCE_SAAS_STACK_ORDER.md)** documents primary region; **[`docs/go-to-market/MARKETPLACE_PUBLICATION.md`](go-to-market/MARKETPLACE_PUBLICATION.md)** publication checklist; **[`docs/PENDING_QUESTIONS.md`](PENDING_QUESTIONS.md)** item 1 notes Central US default.

**Architectural integrity — ADR 0021 Phase 3 not executed here:** Tracking placeholder **[`docs/adr/0022-coordinator-phase3-deferred.md`](adr/0022-coordinator-phase3-deferred.md)** (no coordinator interface deletion in this change set).

**Trust — security.txt:** Marketing static **[`archlucid-ui/public/.well-known/security.txt`](../archlucid-ui/public/.well-known/security.txt)**; **[`SECURITY.md`](../SECURITY.md)** links canonical URL + future **`pgp-key.txt`** path; **[`docs/go-to-market/TRUST_CENTER.md`](go-to-market/TRUST_CENTER.md)** notes internal CISO ownership of the SOC 2 self-assessment.

**Cursor prompts:** Consolidated execution log **[`docs/CURSOR_PROMPTS_SEVEN_IMPROVEMENTS_2026_04_21.md`](CURSOR_PROMPTS_SEVEN_IMPROVEMENTS_2026_04_21.md)** (what shipped vs owner-blocked).

---

## 2026-04-21 — Pending questions + PLG reference + owner security assessment draft

**Architectural integrity — ADR 0021 Phase 1 retirement gate + Phase 2 deprecation signal:** Drove ADR 0021 Phase 1 to its retirement gate by **enforcing** what was previously only documented: every internal read of `GoldenManifest` now goes through `IUnifiedGoldenManifestReader`, and a new build-blocking assertion **[`DualPipelineRegistrationDisciplineTests.Production_types_outside_allow_list_do_not_reference_ICoordinatorGoldenManifestRepository`](../ArchLucid.Api.Tests/Startup/DualPipelineRegistrationDisciplineTests.cs)** sweeps **every loaded `ArchLucid.*` production assembly** (constructor parameters, fields, properties — public and non-public) and fails the build when any type outside an explicit four-entry allow-list type-references the coordinator manifest repository. The allow-list contains the **single permitted reader** ([`UnifiedGoldenManifestReader`](../ArchLucid.Persistence/Reads/UnifiedGoldenManifestReader.cs)) plus the three documented write-path orchestrators ([`ArchitectureRunCommitOrchestrator`](../ArchLucid.Application/Runs/Orchestration/ArchitectureRunCommitOrchestrator.cs), [`ReplayRunService`](../ArchLucid.Application/ReplayRunService.cs), [`DemoSeedService`](../ArchLucid.Application/Bootstrap/DemoSeedService.cs)) that ADR 0021 Phase 3 will retire when the write-side façade lands; new consumers must justify their addition to the allow-list (and amend the ADR) before merging. Then started **ADR 0021 Phase 2** by mounting the standards-track deprecation signal on the mutating coordinator routes (`POST /v1/architecture/request`, `…/run/{id}/execute`, `…/replay`, `…/determinism-check`, `…/commit`, `…/result`, `…/seed-fake-results` — every action on `RunsController`). Implemented as a thin attribute + filter pair following the established `[FeatureGate]` shape: **[`[CoordinatorPipelineDeprecated]`](../ArchLucid.Api/Attributes/CoordinatorPipelineDeprecatedAttribute.cs)** ([`TypeFilterAttribute`](../ArchLucid.Api/Attributes/CoordinatorPipelineDeprecatedAttribute.cs)) → **[`CoordinatorPipelineDeprecationFilter`](../ArchLucid.Api/Filters/CoordinatorPipelineDeprecationFilter.cs)** ([`IAsyncActionFilter`](../ArchLucid.Api/Filters/CoordinatorPipelineDeprecationFilter.cs)) registers an `OnStarting` callback that emits the canonical triplet on every applicable response (success, problem-details, exception-mapped): **`Deprecation: true`** (RFC 9745 §3 — emit on every applicable response, not just successes), **`Sunset: Mon, 20 Jul 2026 00:00:00 GMT`** (RFC 8594 — earliest possible removal date, set to one full quarter beyond the Phase 2 ship date so consumers have a real migration window), and **`Link: <github.com/.../docs/adr/0021-coordinator-pipeline-strangler-plan.md>; rel="deprecation"; type="text/markdown"`** (RFC 8288 + RFC 9745 §4 — points at the canonical migration target). Header writes use indexer-assignment (not `Append`) so an enabled global `ApiDeprecationHeadersMiddleware` cannot duplicate the signal. The route-scoped attribute deliberately stays on the controllers being retired — the read-only siblings ([`RunQueryController`](../ArchLucid.Api/Controllers/Authority/RunQueryController.cs), [`ManifestsController`](../ArchLucid.Api/Controllers/Governance/ManifestsController.cs)) which already read through `IUnifiedGoldenManifestReader` stay clean. Tests: filter unit tests **[`CoordinatorPipelineDeprecationFilterTests`](../ArchLucid.Api.Tests/CoordinatorPipelineDeprecationFilterTests.cs)** (delegate forwarding, header emission via a capturing `IHttpResponseFeature`, idempotency on double-fire, RFC 1123 / RFC 8288 shape of the constants, null-arg guards); integration tests **[`CoordinatorRoutesDeprecationHeaderTests`](../ArchLucid.Api.Tests/CoordinatorRoutesDeprecationHeaderTests.cs)** (real `ArchLucidApiFactory` HTTP — coordinator route emits triplet on a 201 create, coordinator route emits triplet on a 404 commit / non-existent run, adjacent read-only `RunQueryController` route does **not** carry the signal). Docs: ADR 0021 Status note records both 2026-04-21 events (Phase 1 retirement gate + Phase 2 sunset clock start), Phase 1 inventory cross-references the new assembly-wide assertion, Related section links the [`COORDINATOR_TO_AUTHORITY_PARITY.md`](runbooks/COORDINATOR_TO_AUTHORITY_PARITY.md) parity report.

**Workflow embeddedness — PR-commenter GitHub Action:** New composite GitHub Action **[`integrations/github-action-manifest-delta-pr-comment/`](../integrations/github-action-manifest-delta-pr-comment/)** posts the structured `GET /v1/compare` manifest delta as a **single sticky pull-request comment** that is rewritten on every workflow run instead of stacking duplicates. Two-step composite: step 1 reuses the existing **[`fetch-manifest-delta.mjs`](../integrations/github-action-manifest-delta/fetch-manifest-delta.mjs)** from the sibling job-summary action (single source of truth for the Markdown shape — both actions render identical bodies) and writes the rendered Markdown to `${RUNNER_TEMP}/archlucid-manifest-delta.md`; step 2 invokes the new **[`post-pr-comment.mjs`](../integrations/github-action-manifest-delta-pr-comment/post-pr-comment.mjs)** which lists the PR's comments via `gh api repos/$OWNER/$REPO/issues/$PR/comments --paginate`, finds the one whose body contains the marker `<!-- archlucid:manifest-delta -->` (HTML-comment, renders as nothing in the PR view but survives the GitHub REST round-trip), and either **PATCH**es it in place or **POST**s a new one. The pure `upsertStickyComment(...)` helper is exported behind a pluggable `gh` client so the create-vs-update branching is unit-testable without invoking the `gh` binary or hitting the GitHub API. Reuses the existing **`ARCHLUCID_READONLY_API_KEY`** secret (`X-Api-Key` / `ReadAuthority`); requires `permissions: pull-requests: write` on the job so the default `secrets.GITHUB_TOKEN` can create / patch comments. Per-tenant marker override (e.g. `<!-- archlucid:manifest-delta:tenant-acme -->`) lets one PR carry multiple independent stickies. Tests: [`post-pr-comment.test.mjs`](../integrations/github-action-manifest-delta-pr-comment/post-pr-comment.test.mjs) (9 cases — empty / non-string-body lists, new-PR POST path, existing-sticky PATCH path, custom-marker isolation, full required-arg validation; `node --test`, zero external deps). Example workflow: **[`.github/workflows/example-manifest-delta-pr-comment.yml`](../.github/workflows/example-manifest-delta-pr-comment.yml)** wired for both `pull_request` (auto-refresh on every push) and `workflow_dispatch` (manual back-fill). Docs: new contract + sticky-marker guide at **[`docs/integrations/GITHUB_ACTION_MANIFEST_DELTA_PR_COMMENT.md`](integrations/GITHUB_ACTION_MANIFEST_DELTA_PR_COMMENT.md)**; sibling [`GITHUB_ACTION_MANIFEST_DELTA.md`](integrations/GITHUB_ACTION_MANIFEST_DELTA.md) Related section cross-links to the new action.

**Differentiability — public provenance + citations demo route:** New unauthenticated, read-only operator route **`/demo/explain`** ([`archlucid-ui/src/app/(operator)/demo/explain/page.tsx`](../archlucid-ui/src/app/(operator)/demo/explain/page.tsx)) renders the **`ProvenanceGraph`** for the latest committed demo-seed run side-by-side with the **citations-bound aggregate explanation** for the same run — so a sponsor or pilot evaluator can see, in one glance, "evidence → decision → citation" without signing in. Backed by new endpoint **`GET /v1/demo/explain`** ([`DemoExplainController`](../ArchLucid.Api/Controllers/Demo/DemoExplainController.cs)) which delegates to a new server-side **`DemoReadModelClient`** ([`ArchLucid.Host.Core/Demo/DemoReadModelClient.cs`](../ArchLucid.Host.Core/Demo/DemoReadModelClient.cs), interface [`IDemoReadModelClient`](../ArchLucid.Host.Core/Demo/IDemoReadModelClient.cs)) that composes the same application services as `/v1/explain` and `/v1/provenance` (`IRunRepository`, `IRunExplanationSummaryService`, `IProvenanceQueryService`) but **hard-pins the `ScopeContext` to the demo tenant** — the underlying authenticated routes' policies are unchanged. Demo run resolution: canonical `ContosoRetailDemoIdentifiers.AuthorityRunBaselineId` first, then a bounded scan over the 100 most-recent runs filtered on `ContosoRetailDemoIdentifiers.IsDemoRequestId(...)` + a non-empty `RunRecord.GoldenManifestId` (covers per-tenant `req-contoso-demo-{suffix}` shapes). Hard-blocked from non-`Demo:Enabled=true` deployments by a new generic gate filter — **`[FeatureGate(FeatureGateKey.DemoEnabled)]`** ([`FeatureGateAttribute`](../ArchLucid.Api/Attributes/FeatureGateAttribute.cs) → [`FeatureGateFilter`](../ArchLucid.Api/Filters/FeatureGateFilter.cs)) — which returns **`404 Not Found`** Problem Details (not 403) so production hosts cannot leak the existence of the demo surface; the same `FeatureGateKey` enum is reusable for future per-deployment toggles. Response type **`DemoExplainResponse`** ([`ArchLucid.Host.Core/Demo/DemoExplainResponse.cs`](../ArchLucid.Host.Core/Demo/DemoExplainResponse.cs)) always carries `IsDemoData=true` and the `DemoStatusMessage` "demo tenant — replace before publishing" so sponsor screenshots cannot be quoted as production telemetry. DI: registered scoped in [`ServiceCollectionExtensions.ApplicationPipeline.cs`](../ArchLucid.Host.Composition/Startup/ServiceCollectionExtensions.ApplicationPipeline.cs). Tests: API integration ([`DemoExplainEndpointTests`](../ArchLucid.Api.Tests/DemoExplainEndpointTests.cs)) covers **404 when `Demo:Enabled=false`** (filter short-circuits), **404 when `Demo:Enabled=true` but no committed demo run** (read-model returns null), and **200 with payload when read-model resolves** (test-host stubs `IDemoReadModelClient`); filter unit tests ([`FeatureGateFilterTests`](../ArchLucid.Api.Tests/FeatureGateFilterTests.cs)) cover open / closed / unmapped-key (closed-by-default); read-model unit tests ([`DemoReadModelClientTests`](../ArchLucid.Host.Composition.Tests/Demo/DemoReadModelClientTests.cs)) cover canonical-baseline path, recent-scan fallback ordering, no-run / no-summary degrade-to-null, and null-graph → empty-graph substitution. UI: Vitest snapshot + 404 fallback + API-problem callouts ([`page.test.tsx`](../archlucid-ui/src/app/(operator)/demo/explain/page.test.tsx)); new typed helper `getDemoExplain()` in [`archlucid-ui/src/lib/api.ts`](../archlucid-ui/src/lib/api.ts) (returns `null` on 404 so the page can render the "no committed demo-seed run is available" notice without a thrown error). OpenAPI v1 snapshot refreshed (`ARCHLUCID_UPDATE_OPENAPI_SNAPSHOT=1`); `ArchLucid.Api.Client` regenerated via NSwag. Docs: [`go-to-market/POSITIONING.md`](go-to-market/POSITIONING.md) §2 (Pillar 2 — Auditable decision trail) gains a **Live deep link in the staging funnel** subsection that points sponsors at the staging `/demo/explain` URL and explains the `Demo:Enabled` gate.

**Proof-of-ROI readiness — deltas computed from demo seed:** `FirstValueReportBuilder` (Markdown) and `SponsorOnePagerPdfBuilder` (PDF) no longer ship baseline placeholder cells for the metrics ArchLucid can derive on its own. A new application service, **`PilotRunDeltaComputer`** ([`ArchLucid.Application/Pilots/PilotRunDeltaComputer.cs`](../ArchLucid.Application/Pilots/PilotRunDeltaComputer.cs), interface [`IPilotRunDeltaComputer`](../ArchLucid.Application/Pilots/IPilotRunDeltaComputer.cs)), produces a single immutable [`PilotRunDeltas`](../ArchLucid.Application/Pilots/PilotRunDeltas.cs) record per run that is consumed by **both** builders so the Markdown sibling and the sponsor PDF wrapper render identical numbers (no drift). Computed lines: **time from `RunRecord.CreatedUtc` to `GoldenManifest.CommittedUtc`**, **findings total + by severity** (aggregated from `ArchitectureRunDetail.Results[*].Findings`), **LLM calls for the run** (counted from `IAgentExecutionTraceRepository.GetByRunIdAsync` — sibling of the cardinality-safe `archlucid_llm_calls_per_run` histogram), **audit row count for the run** (`IAuditRepository.GetFilteredAsync` filtered on `RunId`, capped to 500 with a "lower bound" marker when truncated), and a **decision-trace excerpt** for the top-severity finding via `IFindingEvidenceChainService`. Every computed line is stamped **"demo tenant — replace before publishing"** when the run matches `ContosoRetailDemoIdentifiers.IsDemoRunId(...)` *or* the `RequestId` starts with the multi-tenant `req-contoso-demo-` prefix that `ContosoRetailDemoIds.ForTenant(...)` mints — see new helper methods on [`ContosoRetailDemoIdentifiers`](../ArchLucid.Application/Bootstrap/ContosoRetailDemoIdentifiers.cs). Failures in the audit / trace / evidence-chain queries are warning-logged and gracefully degrade (the row still renders so the report shape is stable across runs). DI: `IPilotRunDeltaComputer` registered scoped in [`ServiceCollectionExtensions.ApplicationPipeline.cs`](../ArchLucid.Host.Composition/Startup/ServiceCollectionExtensions.ApplicationPipeline.cs). Tests: new [`PilotRunDeltaComputerTests`](../ArchLucid.Application.Tests/Pilots/PilotRunDeltaComputerTests.cs) (committed-and-scoped happy path, demo-RunId match, multi-tenant request-prefix match, audit cap → truncation flag, audit-throw → 0 + warning, no-findings → null evidence chain, non-GUID RunId skips audit query, evidence-chain throw → null chain pointers, manifest-missing → null wall-clock, null-arg guard) and matcher tests in [`ContosoRetailDemoIdentifiersMatcherTests`](../ArchLucid.Application.Tests/Bootstrap/ContosoRetailDemoIdentifiersMatcherTests.cs); existing [`FirstValueReportBuilderTests`](../ArchLucid.Application.Tests/Pilots/FirstValueReportBuilderTests.cs), [`FirstValueReportPdfBuilderTests`](../ArchLucid.Application.Tests/Pilots/FirstValueReportPdfBuilderTests.cs), and [`SponsorOnePagerPdfBuilderTests`](../ArchLucid.Application.Tests/Pilots/SponsorOnePagerPdfBuilderTests.cs) updated to mock the computer and assert the dual-banner placement on demo runs + PDF magic-byte rendering. OpenAPI snapshot unchanged (no endpoint shape moved). Docs: [`PILOT_ROI_MODEL.md`](PILOT_ROI_MODEL.md) §4.1 marks each metric **Computed by ArchLucid? Yes/No** and §4.1.1 adds a non-negotiable **"How to read the demo numbers"** redaction callout.

**Time-to-value — in-product CTA + sponsor PDF one-shot:** New non-modal post-commit banner **`EmailRunToSponsorBanner`** ([`archlucid-ui/src/components/EmailRunToSponsorBanner.tsx`](../archlucid-ui/src/components/EmailRunToSponsorBanner.tsx)) on the operator-shell run-detail page (`archlucid-ui/src/app/(operator)/runs/[runId]/page.tsx`) renders only when the run has a golden manifest and exposes a single primary action — **"Email this run to your sponsor"** — that downloads a sponsor-shareable PDF projection of the canonical first-value-report Markdown for that run. New endpoint **`POST /v1/pilots/runs/{runId}/first-value-report.pdf`** ([`PilotsController.PostFirstValueReportPdf`](../ArchLucid.Api/Controllers/Pilots/PilotsController.cs)) returns `application/pdf`; auth is `ReadAuthority` (mirrors the Markdown sibling — no Standard-tier gate at the click site so the CTA stays one-shot). Backed by new **`FirstValueReportPdfBuilder`** + thin Markdown→PDF renderer ([`MarkdownPdfRenderer`](../ArchLucid.Application/Pilots/MarkdownPdfRenderer.cs)) under `ArchLucid.Application/Pilots/` — the builder calls the existing `FirstValueReportBuilder` so PDF output cannot drift from the Markdown response (single source of truth). DI: registered as scoped in [`ServiceCollectionExtensions.ApplicationPipeline.cs`](../ArchLucid.Host.Composition/Startup/ServiceCollectionExtensions.ApplicationPipeline.cs). Tests: [`ArchLucid.Application.Tests/Pilots/FirstValueReportPdfBuilderTests.cs`](../ArchLucid.Application.Tests/Pilots/FirstValueReportPdfBuilderTests.cs) (null-on-missing, PDF magic bytes on committed run, argument validation), [`ArchLucid.Api.Tests/FirstValueReportPdfEndpointTests.cs`](../ArchLucid.Api.Tests/FirstValueReportPdfEndpointTests.cs) (404 on unknown run + asserts no Standard-tier 402 silently appears), Vitest [`EmailRunToSponsorBanner.test.tsx`](../archlucid-ui/src/components/EmailRunToSponsorBanner.test.tsx) (CTA copy, click-to-download, error rendering, busy state), and live Playwright [`live-api-email-run-to-sponsor.spec.ts`](../archlucid-ui/e2e/live-api-email-run-to-sponsor.spec.ts) (full create→execute→commit cycle, click banner, verify `%PDF` magic bytes on the downloaded blob). New API helper **`downloadFirstValueReportPdf(runId)`** in `archlucid-ui/src/lib/api.ts`. OpenAPI v1 snapshot refreshed (`ARCHLUCID_UPDATE_OPENAPI_SNAPSHOT=1`). Docs: [`API_CONTRACTS.md`](API_CONTRACTS.md) Pilots table, [`EXECUTIVE_SPONSOR_BRIEF.md`](EXECUTIVE_SPONSOR_BRIEF.md) cross-link in the "Related" preface.

**Adoption friction — `archlucid try` + dev-container:** New CLI command **`archlucid try`** ([`ArchLucid.Cli/Commands/TryCommand.cs`](../ArchLucid.Cli/Commands/TryCommand.cs)) takes a brand-new evaluator from `git clone` to a committed manifest + saved sponsor Markdown report in a single command. Composes existing primitives (no rewrites): **`PilotUpCommand`** for the Docker stack + readiness probe, **`POST /v1/demo/seed`** for idempotent demo data, **`ArchLucidApiClient.CreateRunAsync` / `ExecuteAsync` / `GetRunAsync` / `CommitRunAsync` / `SeedFakeResultsAsync`** for the sample-run lifecycle, and **`GET /v1/pilots/runs/{runId}/first-value-report`** for the Markdown. Polls `GET /v1/architecture/run/{runId}` until `ReadyForCommit` (or falls back to `seed-fake-results` after `--commit-deadline`); opens the saved Markdown and the operator-UI `/runs/{runId}` URL in the default handlers (suppressed by **`--no-open`** for containers / SSH / CI). New devcontainer (**`.devcontainer/devcontainer.json`** + **`.devcontainer/docker-compose.devcontainer.yml`**) layers .NET 10 SDK + Node 22 on the host docker socket (Docker-outside-of-Docker) and runs **`archlucid try --no-open`** on `postCreateCommand`. Tests: [`ArchLucid.Cli.Tests/TryCommandTests.cs`](../ArchLucid.Cli.Tests/TryCommandTests.cs) covers argument parsing (defaults, `--no-open`, custom URLs, invalid flags / values), missing-Docker handling (no `docker-compose.yml` in any cwd ancestor → `CliExitCode.UsageError`), and the readiness-poll timeout (returns the last observed status when the deadline elapses). **`completions`** word lists and the no-arg usage banner updated. Docs: [`README.md`](../README.md) "First-time evaluator" row, [`docs/FIRST_30_MINUTES.md`](FIRST_30_MINUTES.md) skip-ahead callout, [`docs/CLI_USAGE.md`](CLI_USAGE.md) new **`archlucid try`** section.

**Marketability — proof page:** New operator-shell route **`/why-archlucid`** (Core Pilot tier, no `requiredAuthority`) renders a read-only "Why ArchLucid" proof page for sponsor demos. Wires three live read endpoints against the seeded **Contoso Retail Modernization** demo tenant: **`GET /v1/pilots/why-archlucid-snapshot`** (new — process-wide `ArchLucidInstrumentation` counters + canonical demo run id + scoped audit row count), **`GET /v1/pilots/runs/{runId}/first-value-report`** (sponsor Markdown), and **`GET /v1/explain/runs/{runId}/aggregate`** (executive aggregate explanation + citations). Backed by a new **`MeterListenerCounterSnapshotProvider`** singleton (`System.Diagnostics.Metrics.MeterListener` over `archlucid_runs_created_total` and `archlucid_findings_produced_total`) and **`WhyArchLucidSnapshotService`** application service. Vitest snapshot test (`archlucid-ui/src/app/(operator)/why-archlucid/page.test.tsx`) and live Playwright spec (`archlucid-ui/e2e/live-api-why-archlucid.spec.ts`) exercise the route end-to-end after a best-effort `POST /v1/demo/seed`. Cross-links added to [`go-to-market/POSITIONING.md`](go-to-market/POSITIONING.md) §4 and [`go-to-market/PRODUCT_DATASHEET.md`](go-to-market/PRODUCT_DATASHEET.md) Get-started step 4. **`EXECUTIVE_SPONSOR_BRIEF.md`** intentionally unchanged (sponsor brief stays canonical).

**Quality / docs hygiene:** Moved superseded **2026-04-20** quality assessments and the improvement-decision log to [`archive/quality/`](archive/quality/) with inbound link rewrites (`CHANGELOG`, ADR 0021, Cursor prompt companions, `PENDING_QUESTIONS`, CI comments). See [`archive/quality/README.md`](archive/quality/README.md).

**Explainability / pilots:** `GET /v1/architecture/run/{runId}/findings/{findingId}/evidence-chain` (read-only pointers) + `FindingEvidenceChainService` tests. **`POST /v1/pilots/runs/{runId}/sponsor-one-pager`** (Standard tier) + `SponsorOnePagerPdfBuilder` + CLI `archlucid sponsor-one-pager <runId> [--save]`. ADR 0021 Phase 1 internal inventory + `DualPipelineInternalReadPathTests`. Stryker PR planner includes **Api** in the full matrix; refresh script help text aligned to all targets.

**GTM / reference customers:** New placeholder case study [`go-to-market/reference-customers/TRIAL_FIRST_REFERENCE_CASE_STUDY.md`](go-to-market/reference-customers/TRIAL_FIRST_REFERENCE_CASE_STUDY.md) and table row **First paying tenant (PLG)** in [`go-to-market/reference-customers/README.md`](go-to-market/reference-customers/README.md) (ship-trial-first path). PLG note in same README.

**Trust / security:** [`go-to-market/TRUST_CENTER.md`](go-to-market/TRUST_CENTER.md) — SOC 2 row set to **Deferred**; penetration section distinguishes **owner self-assessment** vs third-party; links [`security/OWNER_SECURITY_ASSESSMENT_2026_Q2-DRAFT.md`](security/OWNER_SECURITY_ASSESSMENT_2026_Q2-DRAFT.md). [`security/pen-test-summaries/README.md`](security/pen-test-summaries/README.md) indexes the owner-assessment draft.

**Operations:** New [`PENDING_QUESTIONS.md`](PENDING_QUESTIONS.md) (resolved vs open decisions + six-prompt execution status). [`archive/quality/QUALITY_ASSESSMENT_2026_04_20_INDEPENDENT_64_60.md`](archive/quality/QUALITY_ASSESSMENT_2026_04_20_INDEPENDENT_64_60.md) §9 points to it.

---

## 2026-04-20 — Quality prompts execution (ADR 0021 Phase 1, tier 402 gates, pilot scorecard, SaaS profile)

**Architecture / ADR:** [ADR 0021](adr/0021-coordinator-pipeline-strangler-plan.md) moved to **Accepted**; added `IUnifiedGoldenManifestReader` + `UnifiedGoldenManifestReader`; `ManifestsController` now consumes the unified reader. Navigator + scope updates: [`DUAL_PIPELINE_NAVIGATOR.md`](DUAL_PIPELINE_NAVIGATOR.md), [`V1_SCOPE.md`](V1_SCOPE.md), new runbook [`runbooks/COORDINATOR_TO_AUTHORITY_PARITY.md`](runbooks/COORDINATOR_TO_AUTHORITY_PARITY.md).

**API / commercial:** `[RequiresCommercialTenantTier]` + `CommercialTenantTierFilter` return **402** when `dbo.Tenants.Tier` is below **Standard** (governance / policy packs / manifest advanced reads) or **Enterprise** (audit CSV export). New problem type `ProblemTypes.PackagingTierInsufficient`. **`POST /v1/pilots/scorecard`** + `PilotScorecardBuilder`.

**Hosting:** `ArchLucid.Api/appsettings.SaaS.json` (optional chained in `Program.cs` — API keys **off** in-repo until Key Vault/env supplies keys); `infra/apply-saas.ps1`; docs: [`FIRST_30_MINUTES.md`](FIRST_30_MINUTES.md), [`REFERENCE_SAAS_STACK_ORDER.md`](REFERENCE_SAAS_STACK_ORDER.md), [`PRODUCT_PACKAGING.md`](PRODUCT_PACKAGING.md).

**Operator UI:** Marketing pricing section supports optional `teamStripeCheckoutUrl` from `pricing.json` (see [`go-to-market/STRIPE_CHECKOUT.md`](go-to-market/STRIPE_CHECKOUT.md)); new-run wizard shows a **3-phase macro stepper** over the existing seven steps.

**CLI:** `archlucid doctor` prints a **SaaS checklist** table (`DoctorCommand`).

**Security / trust (self-assessment):** [`security/SOC2_SELF_ASSESSMENT_2026.md`](security/SOC2_SELF_ASSESSMENT_2026.md), [`security/COMPLIANCE_MATRIX.md`](security/COMPLIANCE_MATRIX.md), [`security/pen-test-summaries/2026-Q2-SOW.md`](security/pen-test-summaries/2026-Q2-SOW.md), [`security/pen-test-summaries/2026-Q2-REDACTED-SUMMARY.md`](security/pen-test-summaries/2026-Q2-REDACTED-SUMMARY.md) (placeholder); [`TRUST_CENTER.md`](go-to-market/TRUST_CENTER.md) + [`SOC2_ROADMAP.md`](go-to-market/SOC2_ROADMAP.md) cross-links.

**CI / scripts:** Clearer stderr copy in [`scripts/ci/check_reference_customer_status.py`](../scripts/ci/check_reference_customer_status.py); [`PRICING_PHILOSOPHY.md`](go-to-market/PRICING_PHILOSOPHY.md) §5.4 documents **auto-flip** (no manual YAML edit). OpenAPI snapshot refreshed.

**Tests:** `DualPipelineRegistrationDisciplineTests` asserts unified reader registration (scoped `IUnifiedGoldenManifestReader` resolved via `CreateScope()`).

---

## 2026-04-20 — Six quality improvements (pilot CLI, first-value report, GitHub compare action, persistence proposal, pen-test folder, reference row)

**CLI:** Added `archlucid pilot up` (`ArchLucid.Cli/Commands/PilotUpCommand.cs`) — Docker Compose **full-stack** + **`docker-compose.demo.yml`** (simulator, demo seed on startup) with readiness polling on `http://127.0.0.1:5000/health/ready`. Added `archlucid first-value-report <runId> [--save]` calling **`GET /v1/pilots/runs/{runId}/first-value-report`**. **`CompletionsCommand`** word lists updated.

**API:** New **`PilotsController`** + **`FirstValueReportBuilder`** (Markdown sponsor summary). DI registration in **`ServiceCollectionExtensions.ApplicationPipeline.cs`**. OpenAPI snapshot refreshed (`ArchLucid.Api.Tests/Contracts/openapi-v1.contract.snapshot.json`).

**Docs / GTM:** Second reference-customer row (`DESIGN_PARTNER_NEXT`) + case study placeholder; reference README notes **CI auto-flip** for Published rows. **`docs/integrations/GITHUB_ACTION_MANIFEST_DELTA.md`**, **`docs/PROJECT_CONSOLIDATION_PROPOSAL_PERSISTENCE.md`**, **`docs/security/pen-test-summaries/*`**, **`docs/API_CONTRACTS.md`** (Pilots section), **`docs/go-to-market/TRUST_CENTER.md`**, **`SECURITY.md`** (PGP TODO). **`README.md`**, **`docs/CLI_USAGE.md`**, **`docs/FIRST_30_MINUTES.md`**.

**Integrations:** Composite GitHub Action under **`integrations/github-action-manifest-delta/`** plus **`.github/workflows/example-manifest-delta.yml`** (`workflow_dispatch`).

**Tests:** `ArchLucid.Application.Tests/Pilots/FirstValueReportBuilderTests.cs`, `ArchLucid.Cli.Tests/PilotUpCommandTests.cs`.

---

## 2026-04-20 — Doc scope header enforcement (Quality Assessment Improvement 2d)

**Docs / CI:** Prepended a machine-generated `> **Scope:** ...` line to **300** active Markdown files under `docs/` (excluding `docs/archive/`), using the first ATX heading in each file when available. Added [`scripts/ci/backfill_doc_scope_headers.py`](../scripts/ci/backfill_doc_scope_headers.py) (idempotent one-shot back-fill). [`scripts/ci/check_doc_scope_header.py`](../scripts/ci/check_doc_scope_header.py) is now **merge-blocking** in [`.github/workflows/ci.yml`](../.github/workflows/ci.yml) (runs after `check_doc_links.py`). Added [`scripts/ci/test_backfill_doc_scope_headers.py`](../scripts/ci/test_backfill_doc_scope_headers.py) and extended the existing Doc scope header unit-test step to run it.

> **Design-session logs:** The full incremental prompt records live in
> `docs/archive/CHANGE_SET_55R_SUMMARY.md` through `CHANGE_SET_59R.md`.
> Read those when you need exact delivery scope or deferred-backlog decisions.

---

## 2026-04-20 — Tenant-only RLS expansion, first-session metric, LLM prompt redaction, `/onboard` wizard (Quality Assessment follow-up)

**Database (DbUp):** **`096_RlsTenantIdOnlyTables.sql`** introduces **`rls.archiforge_tenant_predicate(@TenantId)`** and adds **FILTER + BLOCK** predicates on **`dbo.SentEmails`**, **`dbo.TenantLifecycleTransitions`**, and **`dbo.TenantTrialSeatOccupants`** under existing **`rls.ArchiforgeTenantScope`**. **`097_TenantOnboardingState.sql`** adds **`dbo.TenantOnboardingState`** (`FirstSessionCompletedUtc`) with the same tenant-only predicate when objects exist. Rollbacks: **`Rollback/R096_RlsTenantIdOnlyTables.sql`**, **`Rollback/R097_TenantOnboardingState.sql`**. Consolidated parity: **`ArchLucid.Persistence/Scripts/ArchLucid.sql`**.

**Application:** **`IFirstSessionLifecycleHook`** / **`SqlFirstSessionLifecycleHook`** records the first successful golden-manifest commit per tenant via **`ITenantOnboardingStateRepository`**; emits **`archlucid_first_session_completed_total`**. **`LlmPromptRedactionOptions`** + **`IPromptRedactor`** redact prompts on **`LlmCompletionAccountingClient`** and trace/blob paths in **`AgentExecutionTraceRecorder`**; counters **`archlucid_llm_prompt_redactions_total`**, **`archlucid_llm_prompt_redaction_skipped_total`**. Production-like hosts log a warning when redaction is disabled (**`LlmPromptRedactionProductionWarningPostConfigure`**).

**UI:** Operator route **`/onboard`** (Core Pilot nav) — four-step first-session wizard using existing architecture API helpers.

**Tests:** **`ArchLucid.Core.Tests/Llm/Redaction/PromptRedactorTests.cs`**. **`archlucid-ui`** unit test **`OnboardWizardClient.test.tsx`**. **`InMemoryStorageProviderRegistrar`** now registers **`NoOpFirstSessionLifecycleHook`** instead of SQL onboarding types (fixes **`ISqlConnectionFactory`** validation failures in **`ArchLucid.Api.Tests`** / **`WebApplicationFactory`** hosts). **`StorageProviderRegistrationParityTests`** allowlists **`ITenantOnboardingStateRepository`** as SQL-only.

**Docs / trust:** Updated **`docs/SQL_SCRIPTS.md`**, **`docs/security/MULTI_TENANT_RLS.md`**, **`docs/security/RLS_RISK_ACCEPTANCE.md`**, **`docs/runbooks/LLM_PROMPT_REDACTION.md`**, **`docs/runbooks/README.md`**, **`docs/OBSERVABILITY.md`**, **`docs/ONBOARDING_WIZARD.md`**, **`docs/go-to-market/TRUST_CENTER.md`**, **`docs/security/SYSTEM_THREAT_MODEL.md`**, **`docs/AGENT_TRACE_FORENSICS.md`**, **`docs/ARCHITECTURE_INDEX.md`**, **`SECURITY.md`**, pen-test templates **`docs/security/PEN_TEST_SOW_TEMPLATE.md`** and **`docs/security/PEN_TEST_REDACTED_SUMMARY_TEMPLATE.md`**. **`docs/CODE_COVERAGE.md`** — local merged Cobertura snapshot (merged line **72.95%**, **`ArchLucid.Api`** **60.79%**) with CI caveats.

---

## 2026-04-20 — Marketplace `ChangePlan` / `ChangeQuantity` GA + Stryker target for `ArchLucid.Api` (Quality Assessment 2026-04-20 § Improvement 4)

**Changed (default behavior):** [`ArchLucid.Api/appsettings.json`](../ArchLucid.Api/appsettings.json) and [`ArchLucid.Api/appsettings.Production.json`](../ArchLucid.Api/appsettings.Production.json) now ship with `Billing:AzureMarketplace:GaEnabled=true`. Marketplace `ChangePlan` and `ChangeQuantity` webhooks are mutating in production by default; both reach the `Processed` terminal state and call the existing `sp_Billing_ChangePlan` / `sp_Billing_ChangeQuantity` stored procedures. The previous `AcknowledgedNoOp` short-circuit is **not** removed — it is intentionally preserved as the supported zero-deploy rollback path.

**Added (rollback runbook):** [`docs/runbooks/MARKETPLACE_CHANGEPLAN_QUANTITY_ROLLBACK.md`](runbooks/MARKETPLACE_CHANGEPLAN_QUANTITY_ROLLBACK.md) — operator runbook for flipping `Billing:AzureMarketplace:GaEnabled` back to `false` via Azure App Configuration or environment variables (no redeploy). Includes "First 5 minutes" copy-paste commands (Kusto, PromQL), reconciliation steps for tier and seat drift, and explicit guidance on when re-enabling GA is safe. Registered in [`docs/runbooks/README.md`](runbooks/README.md) at `P2` and cross-linked from [`docs/ARCHITECTURE_INDEX.md`](ARCHITECTURE_INDEX.md).

**Added (mutation testing target):** [`stryker-config.api.json`](../stryker-config.api.json) extends Stryker.NET coverage to the `ArchLucid.Api` assembly, with HTML / JSON / progress reporters and `thresholds = { high: 70, low: 55, break: 55 }`. Baseline `Api: 55.0` written to [`scripts/ci/stryker-baselines.json`](../scripts/ci/stryker-baselines.json); the `Api` target is added to [`scripts/ci/refresh_stryker_baselines.py`](../scripts/ci/refresh_stryker_baselines.py) and to the weekly matrix in [`.github/workflows/stryker-scheduled.yml`](../.github/workflows/stryker-scheduled.yml). Initial thresholds are intentionally lower than the **70** used by older modules because HTTP wiring code (controllers, middleware, problem-details mapping) has higher mutant density than assertion-rich domain code; the ratchet sequence to bring it to **70 / 70** is documented in [`docs/MUTATION_TESTING_STRYKER.md`](MUTATION_TESTING_STRYKER.md) under "API target (advisory ratchet)". Also fixed a latent inconsistency: `PersistenceCoordination` was already in `stryker-baselines.json` but missing from `refresh_stryker_baselines.py`'s `STRYKER_TARGETS` list — both files now agree.

**Documentation:** [`docs/BILLING.md`](BILLING.md), [`docs/AZURE_MARKETPLACE_SAAS_OFFER.md`](AZURE_MARKETPLACE_SAAS_OFFER.md), [`docs/MUTATION_TESTING_STRYKER.md`](MUTATION_TESTING_STRYKER.md), [`docs/CODE_COVERAGE.md`](CODE_COVERAGE.md), and [`docs/ARCHITECTURE_INDEX.md`](ARCHITECTURE_INDEX.md) all updated in the same commit. The Improvement 4 prompt's per-package line-coverage uplift target (≥ 79 % on `ArchLucid.Api`) remains **open**; a later session added a **local** merged Cobertura snapshot to `docs/CODE_COVERAGE.md` (authoritative numbers still come from the green **`.NET: full regression (SQL)`** CI artifact). Auditability artifacts (`docs/AUDIT_COVERAGE_MATRIX.md`, `audit-core-const-count` anchor) are intentionally **not** edited; no audit constants changed.

**Operational impact:** Marketplace customers on the GA path now see real plan / quantity mutations within seconds. Operators with prior deferred-mode test scaffolding (`BillingMarketplaceWebhookDeferredApiFactory`) keep working because the in-memory factory explicitly sets `GaEnabled=false` per test; the production default flip does not affect those tests.

---

## 2026-04-20 — Reference-customers scaffolding + discount-stack work-down (Quality Assessment 2026-04-20 § Improvement 1)

**Added:** [`docs/go-to-market/reference-customers/README.md`](go-to-market/reference-customers/README.md) — single source of truth for **real, publishable** reference-customer assets, distinct from the existing fictional [`REFERENCE_NARRATIVE_TEMPLATE.md`](go-to-market/REFERENCE_NARRATIVE_TEMPLATE.md). Documents the `Placeholder → Drafting → Customer review → Published` lifecycle. Seeded with a single `EXAMPLE_DESIGN_PARTNER` placeholder row to keep the table renderable.

**Added:** [`docs/go-to-market/reference-customers/EXAMPLE_DESIGN_PARTNER_CASE_STUDY.md`](go-to-market/reference-customers/EXAMPLE_DESIGN_PARTNER_CASE_STUDY.md) — case-study scaffold built from the existing [`REFERENCE_NARRATIVE_TEMPLATE.md`](go-to-market/REFERENCE_NARRATIVE_TEMPLATE.md) structure, with explicit `<<...>>` placeholders so a sales engineer can one-shot the substitution from a single deal-close email. Includes a "publish-cleanup" checklist that strips the internal-only sections.

**Added:** [`docs/go-to-market/PRICING_PHILOSOPHY.md`](go-to-market/PRICING_PHILOSOPHY.md) **§ 5.4 — Discount-stack work-down** (inserted as 5.4 because § 5.3 already exists as the *Re-rate plan*). § 5.4 is an operational tracker — owner / target close date / evidence link / re-rate trigger — for each of the three discount lines from § 5.1 (`−25%` trust, `−15%` reference, `−10%` self-serve). The locked-prices fenced block in § 5.2 is **unchanged**; this section is project-management overlay only.

**Added:** [`scripts/ci/check_reference_customer_status.py`](../scripts/ci/check_reference_customer_status.py) — Python CI guard that parses the reference-customer table and exits non-zero when zero rows have `Status: Published`. Wired into `.github/workflows/ci.yml` with `continue-on-error: true` (non-blocking warning) until the first real customer publishes. Removing that line is the single switch that makes the guard merge-blocking and triggers the pricing review described in § 5.3 / § 5.4. Companion unit tests in `scripts/ci/test_check_reference_customer_status.py` cover 21 cases including header parsing, status-token normalization, and main-function exit codes.

**Updated:** [`README.md`](../README.md) "Key documentation" table — added a row for the new reference-customers index so the asset is discoverable from the repo root.

**Background.** Improvement 1 in [`archive/quality/QUALITY_ASSESSMENT_2026_04_20_WEIGHTED_80_72.md`](archive/quality/QUALITY_ASSESSMENT_2026_04_20_WEIGHTED_80_72.md) called for "land the first reference-customer asset". Without a real customer in hand we cannot legitimately publish a case study, so this PR ships the **scaffolding** (index, placeholder, lifecycle, CI guard, work-down tracker) that makes the day-of-publication a small, mechanical change rather than a doc-and-pricing scramble. The CI guard is intentionally non-blocking today so that build green is preserved; flipping it blocking is the explicit signal that the `−15%` reference discount is now eligible for re-rate.

---

## 2026-04-20 — Test-script consolidation + concept vocabulary CI guard (Quality Assessment 2026-04-20 § Improvement 6)

**Added:** Single canonical test driver — [`test.ps1`](../test.ps1) (PowerShell) and [`test.cmd`](../test.cmd) (cmd trampoline) with a `-Tier <name>` parameter accepting `Core`, `FastCore`, `Integration`, `SqlServerIntegration`, `Full`, `UiUnit`, `UiSmoke`, `Slow`. `.\test.ps1 -ListTiers` enumerates all tiers and the underlying command. Replaces 8 separate per-tier script pairs (16 files) that drifted independently.

**Deprecated (kept as shims):** `test-core.{cmd,ps1}`, `test-fast-core.{cmd,ps1}`, `test-integration.{cmd,ps1}`, `test-sqlserver-integration.{cmd,ps1}`, `test-full.{cmd,ps1}`, `test-slow.{cmd,ps1}`, `test-ui-unit.{cmd,ps1}`, `test-ui-smoke.{cmd,ps1}` are all now thin shims that delegate to the consolidated driver. They are scheduled for removal **after 2026-Q3**; new docs and runbooks should call `.\test.ps1 -Tier <name>` directly.

**Added:** [`docs/CONCEPTS.md`](CONCEPTS.md) — canonical concept vocabulary with explicit canonical-vs-rejected mappings, rationale, and a documented promotion gate. Distinct from [`docs/GLOSSARY.md`](GLOSSARY.md) (which defines terms) by focusing on adjudication between competing forms and the rules for when reviewers should push back.

**Added:** [`scripts/ci/check_concept_vocabulary.py`](../scripts/ci/check_concept_vocabulary.py) — minimal, conservative CI guard implementing the rules from [`docs/CONCEPTS.md`](CONCEPTS.md) § 1.1. The initial enforced rule is the Microsoft Entra ID rename (see CONCEPTS.md row 1 for the full canonical-vs-rejected mapping). Companion unit tests in `scripts/ci/test_check_concept_vocabulary.py` (12 cases, including word-boundary correctness so unrelated tokens such as `Azure ADX` are not flagged) wired into the same CI workflow as the legacy-directory guard. Adding new rules requires the documented promotion gate in `docs/CONCEPTS.md` § 3.

**Updated:** [`docs/AZURE_MARKETPLACE_SAAS_OFFER.md`](AZURE_MARKETPLACE_SAAS_OFFER.md) — fixed one stale legacy-tenant reference in the publishing checklist to use the canonical Microsoft Entra ID form so the new CI guard passes against live `docs/`.

**Updated:** [`docs/TEST_EXECUTION_MODEL.md`](TEST_EXECUTION_MODEL.md) — added a "Canonical entry point" callout at the top documenting `.\test.ps1 -Tier <name>` and `test.cmd <name>`, and rewrote the optional pre-PR sequence to use the consolidated driver.

**Background.** Improvement 6 in [`archive/quality/QUALITY_ASSESSMENT_2026_04_20_WEIGHTED_80_72.md`](archive/quality/QUALITY_ASSESSMENT_2026_04_20_WEIGHTED_80_72.md) called for "collapse `test-*.cmd/.ps1` proliferation into a parameterized driver and add a CI vocabulary guard". The two changes ship together because both are documentation-discipline mechanisms that keep the project legible as it grows: one for runnable entry points, one for terminology. The shim layer preserves backward compatibility with every existing runbook reference, runbook screenshot, and external link while pushing all *new* writing toward the consolidated form.

---

## 2026-04-20 — Workspace-root cleanup + dual-pipeline strangler hardening (Quality Assessment 2026-04-20 § Improvement 3)

**Removed:** Empty legacy `ArchiForge.*` workspace-root directories (28 of them, build-artifact-only, never tracked by git) deleted as workspace-cleanup follow-up to the [ArchLucid rename initiative](ARCHLUCID_RENAME_CHECKLIST.md) (Phase 8). The new blocking CI guard [`scripts/ci/check_no_legacy_archiforge_dirs.py`](../scripts/ci/check_no_legacy_archiforge_dirs.py) (with companion unit tests in `scripts/ci/test_check_no_legacy_archiforge_dirs.py`) prevents reintroduction. Background guidance: [`.cursor/rules/ArchLucid-Rename.mdc`](../.cursor/rules/ArchLucid-Rename.mdc).

**Added:** Audit-event-type collision regression suite — `ArchLucid.Core.Tests/Audit/AuditEventTypes_DoNotCollideAcrossPipelinesTests.cs` pins the invariant from [`docs/AUDIT_COVERAGE_MATRIX.md`](AUDIT_COVERAGE_MATRIX.md) that `CoordinatorRun*` and authority `RunStarted` / `RunCompleted` constants stay distinct as the catalog grows. Four reflection-driven tests cover (1) coordinator-vs-authority value disjointness, (2) baseline-vs-top-level value disjointness, (3) catalog-wide value uniqueness, and (4) every `CoordinatorRun*` constant has either an authority counterpart or an explicit "coordinator-only" allow-list entry.

**Added:** DI-discipline regression — `ArchLucid.Api.Tests/Startup/DualPipelineRegistrationDisciplineTests.cs` turns the [ADR 0010](adr/0010-dual-manifest-trace-repository-contracts.md) "fully qualified at registration time" rule into a build-breaking guarantee (the duplicate-named `IGoldenManifestRepository` and `IDecisionTraceRepository` interface pairs across `ArchLucid.Persistence.Data.Repositories` and `ArchLucid.Decisioning.Interfaces` must not silently cross-wire).

**Added:** "Which path do I use?" decision tree at the top of [`docs/DUAL_PIPELINE_NAVIGATOR.md`](DUAL_PIPELINE_NAVIGATOR.md), plus a "Why we have not collapsed these" section linking the two governing ADRs.

**Added:** [ADR 0021 — Coordinator pipeline strangler plan](adr/0021-coordinator-pipeline-strangler-plan.md) (`Status: Proposed`). Implementation requires a separate PR after architecture review; see the ADR's own status note. ADR 0010 stays `Accepted` until ADR 0021 is `Accepted` *and* the strangler implementation has shipped.

**Background.** Improvement 3 in [`archive/quality/QUALITY_ASSESSMENT_2026_04_20_WEIGHTED_80_72.md`](archive/quality/QUALITY_ASSESSMENT_2026_04_20_WEIGHTED_80_72.md) § 3 originally called for "collapse dual pipelines + delete legacy `ArchiForge.*` folders in one explicit refactor". A repo scan showed (a) the folders were truly empty and trivially safe to delete, and (b) the dual-pipeline interface families are governed by an Accepted ADR (0010) that cannot be overruled in a single refactor PR without a superseding ADR. The work was therefore split into Phase A (folder cleanup + CI guard, this entry), Phase B (strangler hardening tests + sharpened navigator, also this entry), and Phase C (the actual interface collapse — gated on ADR 0021 acceptance, deliberately deferred). See the rationale in [`docs/CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_20_PART3.md`](CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_20_PART3.md).

---

## 2026-04-17 — Order form + ROI model alignment (pricing freeze follow-on)

**Updated:** [docs/go-to-market/ORDER_FORM_TEMPLATE.md](go-to-market/ORDER_FORM_TEMPLATE.md) to replace placeholder pricing with links to [PRICING_PHILOSOPHY.md §5](go-to-market/PRICING_PHILOSOPHY.md). Added:
- Three concrete worked pricing examples: Team 3-seat, Professional 8-seat, Enterprise 50-seat / 3-workspace.
- Run overage line item section with a 150%-of-allowance worked example for Professional tier ($8/run overage; 50 overage runs = $400).
- Annual prepay addendum (Addendum A).
- Design partner agreement addendum (Addendum B): 50% off Professional list for 12 months, capped at 3 customers, in exchange for published case study + quarterly reference calls.

**Updated:** [docs/go-to-market/ROI_MODEL.md](go-to-market/ROI_MODEL.md). Added:
- §8: Subscription cost and payback analysis at locked Professional list price for 6-architect baseline. Full-list payback ≈ 2 months; design partner payback ≈ 1.5 months (6 weeks).
- §9: Three-year TCO comparison vs. LeanIX and Ardoq using publicly observed price ranges from COMPETITIVE_LANDSCAPE.md. Includes sensitivity analysis at 50% of benchmark savings (payback still < 4 months).
- Updated §10 (was §8) "How to present" to reference new sections.

---

## 2026-04-17 — Pricing freeze (locked list prices 2026)

**Added:** Locked list prices in [docs/go-to-market/PRICING_PHILOSOPHY.md](go-to-market/PRICING_PHILOSOPHY.md) §5. Prices are effective 2026-04-17 and valid for 12 months unless a re-rate gate triggers an explicit product leadership review.

**Team:** $199 / workspace / month platform fee + $79 / architect / month (up to 5 seats), 20 runs/month included, $10/run overage, 2 months free on annual prepay.

**Professional:** $899 / workspace / month platform fee + $179 / architect / month (up to 20 seats), 100 runs/month included, $8/run overage, 2 months free on annual prepay.

**Enterprise:** $60,000–$250,000 / year; unlimited runs (2,000 run/mo fair-use soft cap); unlimited seats and workspaces; custom policy packs, retention, SLA, and dedicated CSM.

**Pilot / design partner:** Self-serve trial free (14 days, 10 runs, 3 seats, sample seeded); guided pilot $15,000 flat (credited on conversion); design partner 50% off Pro list for 12 months (first 3 customers only, in exchange for published case study + quarterly reference call).

**Re-rate gates documented:** SOC 2 Type II (+25%), two named reference customers (+15%), self-serve billing loop in production (+10%). Existing customers price-locked for remainder of term + one renewal before any increase applies.

**CI guard added:** `scripts/ci/check_pricing_single_source.py` — fails the build if any price figure appears outside the allowed source files (PRICING_PHILOSOPHY.md, ORDER_FORM_TEMPLATE.md, TRIAL_AND_SIGNUP.md, CHANGELOG.md). Wired into `doc-markdown-links` CI job.

**Cross-links:** POSITIONING.md, TRIAL_AND_SIGNUP.md, ROI_MODEL.md, ORDER_FORM_TEMPLATE.md, CUSTOMER_ONBOARDING_PLAYBOOK.md updated to link to the single price source rather than restating numbers.

---

## 2026-04-14 — Configurable severity thresholds + approval SLA with escalation

**Added:** Configurable **`BlockCommitMinimumSeverity`** on `PolicyPackAssignment` (SQL **`057`**) — allows blocking commits at any `FindingSeverity` level, not just Critical. When null with `BlockCommitOnCritical=true`, behavior is unchanged.

**Added:** **Warning-only mode** via `ArchLucid:Governance:WarnOnlySeverities` — severities in this list trigger `GovernancePreCommitWarned` audit event but allow commit to proceed. Enables phased enforcement rollout.

**Added:** **Approval SLA** via `ArchLucid:Governance:ApprovalSlaHours` — new approval requests receive `SlaDeadlineUtc`. **`ApprovalSlaMonitor`** detects breaches, emits `GovernanceApprovalSlaBreached` audit events, and sends HMAC-signed webhook escalation notifications. SQL **`058`** adds `SlaDeadlineUtc` and `SlaBreachNotifiedUtc` to `GovernanceApprovalRequests`.

**Tests:** `PreCommitGovernanceGateTests` — configurable severity threshold (block on Error, allow Warning-only, legacy Critical-only fallback, warn-only mode). `ApprovalSlaMonitorTests` — SLA breach audit, before-deadline skip, already-notified skip, no-webhook audit-only, SLA-not-configured skip.

**Docs:** Updated `PRE_COMMIT_GOVERNANCE_GATE.md` (severity thresholds, warning mode, approval SLA sections). Updated `AUDIT_COVERAGE_MATRIX.md` (`GovernancePreCommitWarned`, `GovernanceApprovalSlaBreached` rows; count 73→75).

---

## 2026-04-13 — Stryker enforcement tightening + pre-commit gate tests

**Tests:** **`ArchitectureRunServiceExecuteCommitTests`** — commit path throws **`PreCommitGovernanceBlockedException`** when the gate blocks; happy path when allowed; gate skipped when disabled. **`ArchitectureRunCommitPipelineIntegrationTests`** — real **`PreCommitGovernanceGate`** blocks commit without persisting manifest and emits **`GovernancePreCommitBlocked`** audit; allows commit when findings are non-critical. **`PreCommitGovernanceGateTests`** — edge cases (unparseable run id, missing snapshot id, non-enforcing assignment, disabled assignment, missing snapshot row, multiple critical ids, assignment tie-break).

**Stryker:** Raised committed baselines **`62.0` → `65.0`** in **`scripts/ci/stryker-baselines.json`** for all five matrix labels. Tightened scheduled workflow assert tolerance **`0.15` → `0.10`** pp. Documented baseline ratchet policy in **`MUTATION_TESTING_STRYKER.md`**; noted baselines in **`TEST_STRUCTURE.md`**; added Tier **4c** row in **`TEST_EXECUTION_MODEL.md`**.

---

## 2026-04-12 — Quality prompts batch (live E2E docs, k6, trace blobs, audit UI, pre-commit gate, Terraform runbook)

**Added:** Optional **pre-commit governance gate** (`ArchLucid:Governance:PreCommitGateEnabled`, `PolicyPackAssignment.BlockCommitOnCritical`, SQL **`054`**), **`#governance-pre-commit-blocked`** problem type, durable audit **`GovernancePreCommitBlocked`**.

**Added:** **Agent execution trace** full-text blob persistence behind **`AgentExecution:TraceStorage:PersistFullPrompts`** (async blob writes + **`PatchBlobStorageFieldsAsync`**), SQL **`053`**, contract fields on **`AgentExecutionTrace`**.

**Added:** CI job **Performance: k6 smoke (API baseline)** (`tests/load/smoke.js`, non-blocking) and docs **`PERFORMANCE_TESTING.md`**.

**Changed:** Operator **Audit** page — **Clear filters** re-queries, **Export CSV**, summary line, helpers + Vitest; **`ComparisonSummaryPersisted`** audit matrix row; **`ExportsControllerCompareSummaryAuditTests`** usings fix.

**Docs:** **`AGENT_TRACE_FORENSICS.md`**, **`PRE_COMMIT_GOVERNANCE_GATE.md`**, **`TEST_STRUCTURE`** live E2E row, **`TEST_EXECUTION_MODEL`** k6/live rows, **`operator-shell`** audit section, Phase **7.5** Terraform runbook **`TERRAFORM_STATE_MV_PHASE_7_5.md`**, **`NEXT_REFACTORINGS`** backlog summary table.

---

## 2026-04-13 — Governance drift trend, promotion ordering, pipeline timeout, RunId, docs, Schemathesis PR

**Added:** **`GET /v1/governance/compliance-drift-trend`** and **`ComplianceDriftTrendService`** (time-bucketed policy pack change log aggregates). Operator UI **`ComplianceDriftChart`** on the governance dashboard (last 30 days, daily buckets).

**Changed:** Governance **promotions** and **approval requests** must follow **dev → test → prod** single steps (**`GovernanceEnvironmentOrder`**).

**Added:** **`AuthorityPipelineOptions`** (`AuthorityPipeline:PipelineTimeout`, default 5 minutes; **`TimeSpan.Zero`** disables). Authority orchestrator uses a linked cancellation source; timeouts roll back, log, and increment **`archlucid_authority_pipeline_timeouts_total`**.

**Added:** Strongly typed **`RunId`** (**`ArchLucid.Core.Identity`**) with **`System.Text.Json`** converter (incremental adoption; **`Guid`** remains the primary wire/storage shape until migrated).

**Docs:** **`DEGRADED_MODE.md`**; **`START_HERE.md`** reading order + documentation tiers + degraded-mode link; **`DATA_CONSISTENCY_MATRIX.md`** read-replica lag section; **`docs/archive/README.md`** and **`ARCHITECTURE_INDEX.md`** archive pointers; **`API_FUZZ_TESTING.md`** PR vs scheduled Schemathesis; **`UI_COMPONENTS.md`** **`ComplianceDriftChart`**.

**CI:** **`api-schemathesis-light`** job in **`ci.yml`** (Schemathesis **examples** phase only).

---

## 2026-04-12 — LogSanitizer (CWE-117)

**Added:** **`LogSanitizer`** utility for CWE-117 log injection prevention. Applied to string-typed HTTP input in the global exception handler, **`RunsController`** (**`CreateRun`** **`RequestId`**), and **`GovernanceController`** (**`Promote`** **`RunId`**).

---

## 2026-04-12 — Governance confirmations and run progress UI

**Added:** Confirmation dialogs for governance promote and activate actions via reusable **`ConfirmationDialog`** component.

**Added:** Real-time run progress tracker on run detail page — polls pipeline stages (context, graph, findings, manifest) with progress bar and badges for in-progress runs. See **`docs/UI_COMPONENTS.md`**.

---

## 2026-04-12 — Business KPI metrics and aggregate explanation caching

**Added:** Aggregate explanation caching via **`CachingRunExplanationSummaryService`** — eliminates redundant LLM calls on repeated run-detail aggregate explanation views when **`HotPathCache`** is enabled (keyed by run id + **`ROWVERSION`**; TTL from **`HotPathCacheOptions`**).

**Added:** Business-level OpenTelemetry metrics — **`archlucid_runs_created_total`**, **`archlucid_findings_produced_total`** (label **`severity`**), **`archlucid_llm_calls_per_run`** (histogram per agent batch), **`archlucid_explanation_cache_hits_total`** / **`archlucid_explanation_cache_misses_total`** (cache effectiveness; derive hit ratio in Prometheus/Grafana). See **`docs/OBSERVABILITY.md`** and recording rule **`archlucid:explanation_cache_hit_ratio`** in **`infra/prometheus/archlucid-slo-rules.yml`**.

---

## 2026-04-12 — IFeatureFlags and LLM fallback client

Introduced **`IFeatureFlags`** abstraction for testable feature flag evaluation. Added **`FallbackAgentCompletionClient`** for automatic LLM model failover on **429** / **5xx**.

---

## 2026-04-12 — Persisted run trace ID and CLI trace command

Persisted OpenTelemetry trace ID in **`dbo.Runs`** (Migration **052**). Added **`archlucid trace <runId>`** CLI command for post-hoc distributed trace lookup. Surfaced creation-time trace link in run detail UI.

---

## 2026-04-12 — Stryker mutation baselines

Raised Stryker mutation score baselines from 62% to 70% across all five modules (Persistence, Application, AgentRuntime, Coordinator, Decisioning).

---

## 2026-04-12 — Audit export and retention policy

Added audit export endpoint (`GET /v1/audit/export`) with CSV/JSON support and 90-day range limit. Created audit retention policy document (`docs/AUDIT_RETENTION_POLICY.md`). Database-enforced append-only on `dbo.AuditEvents` (Migration **051**).

---

## 2026-04-12 — CI hardening

CI hardening: Simmy chaos tests now block PRs (burn-in complete). Per-package line coverage gate raised from 50% to 60%.

Added Schemathesis API fuzz testing as a scheduled CI workflow against the OpenAPI spec. Operator docs: `docs/API_FUZZ_TESTING.md`; execution model and test matrix updated for Tier 4 (ZAP + Schemathesis).

---

## 2026-04-12 — Aggregate run explanation

Added aggregate run explanation endpoint (`/v1/explain/runs/{runId}/aggregate`) with theme summaries, risk posture, confidence score, and explanation provenance. Surfaced in run detail UI.

---

## Phase 7 — ArchLucid rename (code-level)

**Area:** Rename / operator breaking changes  
**Summary:** Removed legacy **`ArchiForge*`** configuration keys, **`ARCHIFORGE_*`** / UI OIDC storage bridges, and renamed CLI manifest (`archlucid.json`), global tool command (`archlucid`), SQL DDL file (`ArchLucid.sql`), and dev Docker/compose defaults. **`com.archiforge.*` integration event type strings are no longer emitted or aliased** — only canonical **`com.archlucid.*`** types apply. See **`BREAKING_CHANGES.md`** for migration steps. Terraform resource **addresses** using the historical **`archiforge`** token remain until a planned `state mv` (checklist 7.5); the APIM backend URL **variable** is now **`archlucid_api_backend_url`**.

---

## 59R — Learning-to-planning bridge

**Area:** Product learning / planning  
**Key deliverables:**

- `032_ProductLearningPlanningBridge.sql` (DbUp) + `ArchLucid.sql` parity — SQL tables for improvement themes, plans, and junction links to runs/signals/artifacts.
- Contracts under `ArchLucid.Contracts/ProductLearning/Planning/`.
- `IProductLearningPlanningRepository`, Dapper + in-memory implementations, DI registration.
- Unit tests: `ProductLearningPlanningRepositoryTests`.
- Docs: `SQL_SCRIPTS.md`, `DATA_MODEL.md`, this file.

**Intentionally deferred:** deterministic theme-derivation service, plan-draft builder with priority score.

---

## 58R — Product learning dashboard and improvement triage

**Area:** Operator tooling / product feedback  
**Key deliverables:**

- `ProductLearningPilotSignals` SQL table + Dapper and in-memory repositories.
- Aggregation services: `IProductLearningFeedbackAggregationService`, `IProductLearningImprovementOpportunityService`, `IProductLearningDashboardService`.
- HTTP API: `GET /v1/product-learning/summary`, `/improvement-opportunities`, `/artifact-outcome-trends`, `/triage-queue`, `/report` (Markdown/JSON).
- Operator UI: **Pilot feedback** page (`/product-learning`), export links.
- Tests: aggregation, ranking, parser, API, report-builder (`ChangeSet=58R` / `ProductLearning` filter tags).
- Docs: `PRODUCT_LEARNING.md`; updated `PILOT_GUIDE.md`, `OPERATOR_QUICKSTART.md`, `README.md`.

**Constraints:** No autonomous adaptation; human-entered signals only; scoped to tenant/workspace/project.

---

## 57R — Operator-journey E2E (Playwright)

**Area:** UI test harness  
**Key deliverables:**

- `e2e/fixtures/` — typed JSON payloads aligned with all UI coercion helpers.
- `e2e/helpers/route-match.ts`, `register-operator-api-routes.ts`, `operator-journey.ts` — centralised route dispatch and journey navigation.
- Specs: `smoke`, `compare-proxy-mock`, `run-manifest-journey`, `compare-journey`, `compare-stale-input-warning`, `manifest-empty-artifacts`.
- `e2e/mock-archlucid-api-server.ts` + `e2e/start-e2e-with-mock.ts` — loopback HTTP mock on port 18765 for RSC pages; `playwright.config.ts` `webServer` updated.
- `tsx` devDependency for TS mock runner; `e2e/tsconfig.json` + `npm run typecheck:e2e`.
- `-RunPlaywright` flag added to `release-smoke.ps1` / `.cmd`.
- Docs: `archlucid-ui/docs/TESTING_AND_TROUBLESHOOTING.md` (section 8 rewritten).

---

## 56R — Release-candidate hardening and pilot readiness

**Area:** Configuration, observability, packaging, operator docs  
**Key deliverables:**

- Fail-fast config validation (`ArchLucidConfigurationRules`) before DbUp; SQL connection required only when `StorageProvider=Sql`.
- `/health/live` (minimal) + `/health/ready` (DB, schema files, compliance pack, writable temp) + `/health` (all) with `DetailedHealthCheckResponseWriter` (enriched JSON including `version`, `commitSha`, `totalDurationMs`).
- Startup non-secret configuration snapshot log (toggle `Hosting:LogStartupConfigurationSummary`).
- `GET /version` endpoint (`VersionController`, `[AllowAnonymous]`): `application`, `informationalVersion`, `assemblyVersion`, `fileVersion`, `commitSha`, `runtimeFramework`, `environment`.
- `BuildProvenance` + `BuildInfoResponse` (Core): parses `CommitSha` from `+{sha}` suffix of informational version; CI stamps `SourceRevisionId=$(git rev-parse HEAD)`.
- API `ProblemSupportHints` (`extensions.supportHint`); CLI `CliOperatorHints` (`Next:` lines); UI proxy `502/503 supportHint`.
- `archlucid support-bundle` CLI command (folder + optional `--zip`): `README.txt`, `manifest.json` (v1.1 + `triageReadOrder`), `build.json`, `health.json`, `api-contract.json` (bounded OpenAPI probe), `config-summary.json`, `environment.json`, `workspace.json`, `references.json`, `logs.json`.
- Local scripts: `build-release`, `package-release`, `run-readiness-check`, `release-smoke` (`.cmd` + `.ps1`); `scripts/OperatorDiagnostics.ps1` (structured triage output).
- Release handoff artifacts in `artifacts/release/`: `metadata.json` (schema 1.1), `release-manifest.json`, `checksums-sha256.txt`, `PACKAGE-HANDOFF.txt`.
- Docs added: `PILOT_GUIDE.md`, `OPERATOR_QUICKSTART.md`, `TROUBLESHOOTING.md`, `RELEASE_LOCAL.md`, `RELEASE_SMOKE.md`, `CLI_USAGE.md`.

---

## 55R — Operator shell coherence

**Area:** UI shell  
**Key deliverables:**

- Shared navigation, breadcrumbs, and operator messaging patterns across home, runs, run/manifest detail, graph, compare, replay, and artifact review.
- Canonical manifest-scoped artifact URLs; `GET /runs/{runId}/artifacts/{artifactId}` resolves manifest then redirects.
- Compare page: sequential legacy-then-structured fetches; UI explains fetch order vs. on-page review order; optional AI explanation; stale-input warning when run IDs drift.
- Coercion/guard helpers for operator-facing JSON.
- Vitest smoke coverage: API wiring (list/descriptor/compare/explain), shell nav, key review components.

---

## How to add a changelog entry

1. Add a new `## <version> — <title>` section **above** the previous one.
2. Use the subsections: **Area**, **Key deliverables**, and (optionally) **Intentionally deferred**.
3. Keep entries to a navigable summary; put fine-grained prompt records in a new `docs/archive/CHANGE_SET_<id>.md` file and link from here.
