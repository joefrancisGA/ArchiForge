> **Scope:** Independent weighted quality assessment of ArchLucid as it stands in this repository on 2026-04-21. Weighted overall score: **68.60%**. Companion Cursor prompts: [`CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_68_60.md`](CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_68_60.md).

> **Spine doc:** [Five-document onboarding spine](FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# ArchLucid Quality Assessment — 2026-04-21 — Weighted 68.60%

**Audience:** Product leadership, sponsoring exec, engineering leads, GTM owners.

**Method.** Each quality is scored 1–100 from a fresh inspection of the repository (source projects, Terraform stacks, docs, CI gates, runbooks, ADRs, templates, GTM material) on 2026-04-21. Weights come from the request. Items the owner has formally **deferred to V1.1 / V2** (per [`V1_DEFERRED.md`](library/V1_DEFERRED.md), [`V1_SCOPE.md`](library/V1_SCOPE.md) §3, and the **Resolved** table in [`PENDING_QUESTIONS.md`](PENDING_QUESTIONS.md)) are **excluded** from the readiness score — they are not held against ArchLucid here.

**Independence.** This file does **not** consult earlier `QUALITY_ASSESSMENT_*` outputs. Where my judgement happens to align with a previous one, that is convergent evidence, not citation.

**Ordering rule.** Sections appear **most-improvement-needed first**. "Improvement need" is `(100 − score) × weight` so a 38-point gap on a weight-8 quality outranks the same gap on a weight-1 quality.

**Weight arithmetic.** The supplied weights total **102** (Commercial 40 + Enterprise 25 + Engineering 37). The weighted percent is `Σ(score × weight) ÷ (102 × 100)`. Bucket sub-totals also use their bucket weight as the denominator so they read as 0–100 percentages.

---

## 0. Headline

| Bucket | Weight share | Sub-total contribution | Effective bucket score |
|--------|--------------|------------------------|------------------------|
| **Commercial** | 40 / 102 | **2,633 / 4,000** | 65.83% |
| **Enterprise** | 25 / 102 | **1,706 / 2,500** | 68.24% |
| **Engineering** | 37 / 102 | **2,658 / 3,700** | 71.84% |
| **Total** | 102 / 102 | **6,997 / 10,200** | **68.60%** |

**Plain-English read.** Engineering is the strongest column and continues to outpace the other two. Enterprise/governance posture is solid for V1 but is held down by the same external-trust gaps that have dogged earlier scores (no published reference, no executed pen test, no PGP key). Commercial readiness is the dominant headwind: every named monetization rail is wired in code but **none is live in production today** — Marketplace listing not published, Stripe live keys not flipped, no `Published` reference customer row, no attested compliance certificate. The score moves materially the day any of those ship; they do not require additional engineering, only owner action plus a small amount of integration plumbing.

---

## 1. Quality scores — ordered by improvement impact

> Throughout, "the repo" means the source tree at `c:\ArchiForge\ArchiForge` on 2026-04-21.

For each quality I report the score, the weight, the **gap × weight** improvement-impact, justification grounded in repo evidence, the trade-off accepted by the current design, and a concrete recommendation. The eight largest improvements are also distilled into Cursor prompts in **§3** and the companion file.

---

### 1.1 Marketability — Score **62 / 100** · Weight **8** · Impact **304**

**Justification.** The full kit of a sellable narrative is now in-repo: executive sponsor brief, three-layer product packaging (Core Pilot / Advanced Analysis / Enterprise Controls), competitive landscape doc, vertical briefs for five industries, screenshot gallery, trust center, public marketing routes for `/why`, `/pricing`, `/signup`, `/demo/preview`, and `/welcome` (`archlucid-ui/src/app/(marketing)/`), and a citation seam test that fails if competitive comparison rows lose their proof footnote. What is **still missing** is **external proof on the page**: every row in [`docs/go-to-market/reference-customers/README.md`](go-to-market/reference-customers/README.md) is `Placeholder` or `Customer review`; **no row is `Published`**, so the merge-blocking CI guard is still in advisory mode and the −15% reference discount in `PRICING_PHILOSOPHY.md` § 5.4 is still notional. The marketing site cannot yet quote a real logo or measured customer ROI delta.

**Trade-off.** The team explicitly chose narrative honesty over inflation (sponsor brief refuses transformation claims, vertical briefs refuse uncited statistics). That protects long-term trust but caps short-term magnetism.

**Recommendation.** See **Improvement 1** in §3 — graduate the **First paying tenant (PLG)** row in `reference-customers/README.md` from `Placeholder` to `Customer review` (then later `Published`, owner-only). The single state change flips the CI guard merge-blocking and is the moment marketability moves measurably.

---

### 1.2 Adoption Friction — Score **60 / 100** · Weight **6** · Impact **240**

**Justification.** Evaluator friction is **low**: [`docs/FIRST_30_MINUTES.md`](FIRST_30_MINUTES.md) is Docker-only, `archlucid try` is a one-command first-value loop, the `.devcontainer/` boots in the same posture. Real **paid-adoption** friction remains high: the trial signup page exists at `archlucid-ui/src/app/(marketing)/signup/page.tsx` and `POST /v1/register` is wired, but the production funnel still needs DNS cutover, Front Door custom domain, Stripe live keys, and Marketplace certification — none of which are live (per the **Still open** list in [`PENDING_QUESTIONS.md`](PENDING_QUESTIONS.md), items 8, 9, and 22). Self-hosting is **out of scope** by owner decision (Resolved 2026-04-21 in `PENDING_QUESTIONS.md`), which is correct for a SaaS product but means a customer who insists on BYO-cluster is turned away by design.

**Trade-off.** No customer-shipped containers means a clean SaaS contract; some prospects will still ask.

**Recommendation.** See **Improvement 2** in §3 — wire the trial funnel end-to-end against Stripe **TEST** mode on `staging.archlucid.com` so a prospect can complete signup → first sample run without a sales call. Owner action then flips Stripe live mode behind a feature flag.

---

### 1.3 Time-to-Value — Score **75 / 100** · Weight **7** · Impact **175**

**Justification.** "Clone to committed manifest" is genuinely fast: `archlucid try` plus the simulator-agent demo emits a sponsor-shareable Markdown first-value report within ~10 minutes locally, and the operator-shell post-commit banner can email a sponsor PDF straight from the run page. The repo even shows a "Day N since first commit" badge on the sponsor banner sourced from `dbo.Tenants.TrialFirstManifestCommittedUtc`. The **measurable** ROI value (review-cycle hours saved) is computed by `ValueReportReviewCycleSectionFormatter` and surfaced in the value-report DOCX. What is missing is **field-validated** time-to-value: the model is accurate to the implementation, but no real customer's hours-saved curve has been published yet.

**Trade-off.** The repo invests heavily in *the artifact that proves value* (manifest + delta + provenance) over flashy first-touch UI. That is the right long-term investment but means the first-90-seconds wow factor depends on the sample preset.

**Recommendation.** Pre-seed **one vertical-aligned sample run per industry brief** during trial signup so the user sees industry-relevant findings within 90 seconds; emit a `time-to-first-committed-manifest` metric on the tenant row; quote it in the sponsor banner. Five vertical briefs already exist (`templates/briefs/{financial-services,healthcare,retail,saas,public-sector}/`) so the wiring is small.

---

### 1.4 Proof-of-ROI Readiness — Score **65 / 100** · Weight **5** · Impact **175**

**Justification.** The plumbing is here: [`PILOT_ROI_MODEL.md`](library/PILOT_ROI_MODEL.md) defines the six measurement axes; [`go-to-market/ROI_MODEL.md`](go-to-market/ROI_MODEL.md) carries the dollar baseline (~$294K savings for a 6-architect team) with three-year TCO sensitivity; the value-report DOCX renderer is shipped; `EVIDENCE_PACK.md` and `REFERENCE_EVIDENCE_PACK_TEMPLATE.md` give a single-page measured-delta format; `PilotRunDeltaComputer` (`ArchLucid.Application/Pilots/PilotRunDeltaComputer.cs`) computes per-run deltas the builders consume. The gap is empirical: **zero customer-supplied baselines** are populated; every quoted number is from the model.

**Trade-off.** Conservative model defaults avoid over-claim but make every buyer's headline number look identical.

**Recommendation.** Make `baselineReviewCycleHours` **soft-required** at trial signup (skippable but defaulted to "I don't know — use model"); surface a **before/after panel** on the operator dashboard once a tenant has one committed run. Publish a **sanitized aggregate ROI bulletin** quarterly. See **Improvement 3** in §3.

---

### 1.5 Differentiability — Score **65 / 100** · Weight **4** · Impact **140**

**Justification.** [`COMPETITIVE_LANDSCAPE.md`](go-to-market/COMPETITIVE_LANDSCAPE.md) makes a defensible claim: ArchLucid is the only candidate that combines AI agent orchestration with enterprise governance, auditability, and provenance for **design-time** architecture. The repo backs the claim: decision traces, golden manifests, replay, comparison drift, dual-pipeline navigator, an anonymous `/demo/preview` cached commit page (ADR 0027), `/demo/explain` provenance + citations route, and a public `/why` marketing page (`archlucid-ui/src/app/(marketing)/why/page.tsx`) with a citation-protected comparison table. What is **still missing** is an external-facing **side-by-side artifact pack** that a buyer can read in two minutes — "this is the package ArchLucid hands an architecture review board; here is what LeanIX or Ardoq would have handed them for the same input."

**Trade-off.** The team has wisely refused competitor takedowns from the seat of the pants — but that leaves the differentiation buried in product behaviour rather than visible in shareable PDF form.

**Recommendation.** Extend `/why` with a one-click **downloadable comparison artifact** (PDF) that bundles the ArchLucid run package side-by-side with a public-data scaffold of what an incumbent would produce for the same input. Already partially shipped — see **Improvement 5** in §3 for the small extension.

---

### 1.6 Trustworthiness — Score **58 / 100** · Weight **3** · Impact **126**

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

### 1.12 Decision Velocity — Score **55 / 100** · Weight **2** · Impact **90**

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

### 1.15 Commercial Packaging Readiness — Score **60 / 100** · Weight **2** · Impact **80**

**Justification.** Three named tiers, single source of truth on prices (`PRICING_PHILOSOPHY.md`), `ORDER_FORM_TEMPLATE.md`, DPA, SLA summary, subprocessors, Stripe abstraction (ADR 0016), Marketplace alignment doc, packaging layer enforcement plan, `[RequiresCommercialTenantTier]` filter returning **402 Payment Required** with `ProblemTypes.PackagingTierInsufficient`. **Listing not live, Stripe not in prod.**

**Recommendation.** Same as §1.12 — ship the Marketplace listing and flip Stripe live keys behind a feature flag. See **Improvement 4**.

---

### 1.16 Procurement Readiness — Score **62 / 100** · Weight **2** · Impact **76**

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

**Justification.** 200+ docs files, 50+ projects, dual coordinator/authority interface families, three-layer UI model, two persistence families. The repository **does** mitigate this with `FIRST_5_DOCS.md`, `ARCHITECTURE_ON_ONE_PAGE.md`, `OPERATOR_ATLAS.md`, `DUAL_PIPELINE_NAVIGATOR.md`, scope headers on every doc (CI-enforced), `CONCEPTS.md` vocabulary guard, `bounded-context-map.md`, but a new contributor still has to read several maps before being productive. The `IMPROVEMENTS_COMPLETE.md` file at the repo root is also a stale orphan from an earlier change set — its "Run `dotnet restore`" instructions and `ArchLucid.DecisionEngine.csproj` references no longer match the current solution layout (verified — there is no `ArchLucid.DecisionEngine` project).

**Recommendation.** (1) Remove or archive the stale `IMPROVEMENTS_COMPLETE.md` at repo root — it now contradicts the project layout. (2) Add an "I have 30 minutes — what do I read?" path to `FIRST_5_DOCS.md` that picks **three** docs maximum and links to one navigator each.

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

1. **No published reference customer.** Every row in `reference-customers/README.md` is `Placeholder` or `Customer review`; the −15% reference discount remains notional.
2. **Trial signup funnel not live in production.** Page exists, endpoint exists, Stripe TEST not yet wired through the staging hostname end-to-end.
3. **No third-party pen test summary published.** SoW awarded; redacted-summary skeleton waits on assessor delivery.
4. **No PGP key for `security@archlucid.dev`.** Trust Center references PGP; key file (`archlucid-ui/public/.well-known/pgp-key.txt`) is missing.
5. **Marketplace listing not live.** Wiring is complete; publication is owner-only.
6. **Golden cohort SHAs are placeholders** — nightly workflow asserts contract only, not actual manifest drift. Real signal is one approved baseline-lock run away.
7. **Coordinator strangler not finished.** ADR 0021 Phase 3 deferred per ADR 0022 exit gates; dual interface families remain a teaching tax on every new engineer.
8. **No board-pack PDF / monthly executive digest preset.** Weekly digest exists; quarterly board-grade roll-up does not.
9. **Stale `IMPROVEMENTS_COMPLETE.md` at repo root.** References a non-existent `ArchLucid.DecisionEngine` project — small but visible inconsistency.
10. **No Microsoft Teams connector.** Highest-traffic Microsoft surface ArchLucid does not yet land in.

### 2.2 Top 5 monetization blockers

1. **No `Published` reference customer row** — pricing discount stack stays at −50% notional; no logo on the deck.
2. **Marketplace listing not published** — buyers on MACC contracts cannot transact; the assistant cannot resolve this (Partner Center seller verification is owner-only).
3. **Stripe live keys not flipped** — self-serve payment loop stops at Stripe TEST mode in staging.
4. **No SOC 2 attestation or executed pen test** — every regulated buyer requires at least one of these to start procurement.
5. **No public price page transition from "displayed" to "transactable"** — `/pricing` shows numbers but the buyer cannot click through to a live checkout that mints a tenant.

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

**ArchLucid has built almost every piece of evidence a buyer needs to commit, but it has not yet collected the three external signals that turn evidence into permission to commit.** The product, the trust posture, the engineering quality, and the documentation are all materially ahead of where the commercial proof, the third-party assurance, and the Microsoft-ecosystem coverage are. Three owner-controlled events — **(1)** the first paying tenant approves a published case study, **(2)** the awarded pen test executes and a redacted summary publishes, **(3)** the Marketplace listing goes live — would each independently move the weighted score by **5–8 points**. None of them require additional engineering. They require the owner's calendar, signature, or single payment authorization. **The score is no longer rate-limited by what we build; it is rate-limited by what we publish, attest, and transact.**

---

## 3. The eight largest improvements

The eight biggest improvement-impact items are listed in priority order. Each one notes: **what I can do today**, **what is owner-only**, and a **DEFERRED** marker in the title when I cannot complete at least part of it. Companion paste-ready Cursor prompts live in [`CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_68_60.md`](CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_68_60.md). Where my recommendation matches an in-flight prompt from the previous (67.61%) cycle, I refine rather than restart.

---

### Improvement 1 — Graduate the first reference customer (PLG row) — partial; owner-only for `Published`

**Quality moved.** Marketability (+10–12), Decision Velocity (+5), Procurement Readiness (+5).

**What I can do today.** Build the publication-runbook scaffolding so that the day a real customer approves copy is a small mechanical change: enrich `TRIAL_FIRST_REFERENCE_CASE_STUDY.md` with a `<<...>>` placeholder audit (every value the CSE must substitute on the day of close); commit a sample evidence-pack scaffold tied to the demo seed (clearly marked `demo tenant — replace before publishing`); add a `CHANGELOG` entry recording a state-transition convention; verify `scripts/ci/check_reference_customer_status.py` passes locally and that `.github/workflows/ci.yml` will auto-flip merge-blocking the moment any row is `Published`.

**What is owner-only.** Filling `<<CUSTOMER_NAME>>` with a real value; setting `Status: Published`; granting copy approval; signing the discount re-rate trigger from `PRICING_PHILOSOPHY.md` § 5.3.

**Pending question.** Item 19 in `PENDING_QUESTIONS.md` — *who graduates the first row?*

---

### Improvement 2 — Live trial signup funnel end-to-end (Stripe TEST mode) — partial; owner-only for live keys

**Quality moved.** Adoption Friction (+12), Time-to-Value (+5), Decision Velocity (+8).

**What I can do today.** Trace the existing happy path end-to-end and document it as a runbook (`docs/runbooks/TRIAL_FUNNEL_END_TO_END.md`); add a Playwright spec running the funnel against the deterministic mocks; ship an `archlucid trial smoke` CLI command that runs the funnel in dev and prints PASS/FAIL per step; surface the `baselineReviewCycleHours` capture on the operator dashboard once one run has committed.

**What is owner-only.** Switching from Stripe TEST to live keys; turning off the trial signup feature flag in production; DNS cutover for `signup.archlucid.com` (or staging variant).

**Pending question.** Items 9, 22 in `PENDING_QUESTIONS.md`.

---

### Improvement 3 — Proof-of-ROI: aggregate ROI bulletin + soft-required baseline at signup

**Quality moved.** Proof-of-ROI Readiness (+12), Marketability (+5), Executive Value Visibility (+5).

**What I can do today.** Flip `baselineReviewCycleHours` from optional to **soft-required** at signup (skippable but defaulted to model — UI + API contract work the assistant owns); add a `BeforeAfterDeltaPanel` component to the operator dashboard, reading from `PilotRunDeltaComputer`; ship a quarterly aggregate ROI bulletin **template** under `docs/go-to-market/AGGREGATE_ROI_BULLETIN_TEMPLATE.md` with explicit minimum-N privacy guards; add a CLI command `archlucid roi-bulletin --quarter Q3-2026 --min-tenants 5` that emits a draft bulletin from production data when permissions allow (Admin authority).

**What is owner-only.** Approving the publication cadence; signing each issue; deciding privacy-notice update for the soft-required baseline.

**Pending question.** Items 27, 28 in `PENDING_QUESTIONS.md`.

---

### Improvement 4 — Marketplace + Stripe live readiness (production-safety guards) — partial; owner-only for "Go live"

**Quality moved.** Decision Velocity (+10), Commercial Packaging Readiness (+10), Adoption Friction (+5).

**What I can do today.** Add `BillingProductionSafetyRules` that fails `ASPNETCORE_ENVIRONMENT=Production` startup when (a) Stripe live key prefix `sk_live_` is configured without a webhook secret, or (b) Marketplace landing page URL is empty/localhost; add `archlucid marketplace preflight` CLI that prints PASS/FAIL per Partner Center checklist; add `scripts/ci/assert_marketplace_pricing_alignment.py` ensuring `PRICING_PHILOSOPHY` tier numbers match `MARKETPLACE_PUBLICATION.md` SKU numbers; document the Stripe TEST staging path end-to-end in `docs/go-to-market/STRIPE_CHECKOUT.md`.

**What is owner-only.** Setting any live Stripe key, Marketplace publisher ID, or production webhook secret; pressing "Go live" in Partner Center; tax profile, payout account, seller verification.

**Pending question.** Items 8, 9, 22.

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

**What I can do today.** Add a Logic Apps Standard workflow template under `infra/terraform-logicapps/workflows/teams-notifications/` subscribing to Service Bus topics for `run.committed`, `governance.approval.requested`, `alert.raised`; render to a Teams adaptive card via Incoming Webhook; add per-tenant config surface at `archlucid-ui/src/app/(operator)/integrations/teams/page.tsx` and `POST /v1/integrations/teams/connections` storing the webhook URL via Key Vault references (no raw URLs in SQL); shape the page in Enterprise Controls tier (`ExecuteAuthority` for write, `ReadAuthority` for view); add to `nav-config.ts` and the Vitest seam tests; add Schemathesis contract test for the new endpoints; document under `docs/integrations/MICROSOFT_TEAMS_NOTIFICATIONS.md`.

**What is owner-only.** Choosing notification-only vs two-way (approve governance from Teams) — two-way needs a registered Teams app manifest in M365 admin (item 23).

**Pending question.** Items 11, 23.

---

### Improvement 8 — Golden-cohort drift report with locked baseline SHAs — partial; real-LLM gated

**Quality moved.** Correctness (+10), AI/Agent Readiness (+8).

**What I can do today.** Add a one-shot `archlucid golden-cohort lock-baseline` CLI that runs the 20 cohort items through the **simulator** path, captures committed-manifest canonical SHA-256s, and writes them back into `tests/golden-cohort/cohort.json`; extend `.github/workflows/golden-cohort-nightly.yml` from "contract test only" to "manifest drift report" — diff against the locked SHAs and the expected finding categories; publish `docs/quality/golden-cohort-drift-latest.md` overwriting per run with previous reports archived under `docs/quality/archive/<date>.md`; add a "Explain this finding" panel in the operator UI (data is already present via `FindingEvidenceChainService`).

**What is owner-only.** Provisioning the dedicated Azure OpenAI deployment used by the optional real-LLM run (item 15/25 — budget approval); publishing per-tenant feedback aggregates externally (privacy review).

**Pending question.** Items 15, 25.

---

## 4. Pending owner-only questions for later (additive)

The companion file [`PENDING_QUESTIONS.md`](PENDING_QUESTIONS.md) is the canonical list. This assessment **adds** the following items so that when you next ask "what is still open?", the answer is complete:

- **29.** Sponsor approval on the `BeforeAfterDeltaPanel` placement in the operator dashboard (Improvement 3) — top of `/runs` list, sidebar widget, or `/runs/[runId]`?
- **30.** Marketplace publisher legal entity name on customer statements (Improvement 4) — "ArchLucid, Inc." or DBA variant?
- **31.** Approval on the **side-by-side downloadable PDF** in Improvement 5 — should the incumbent comparison ship as **PDF** (downloadable), as **inline page section** (visible without download), or **both**?
- **32.** Preferred Teams connector trigger set in Improvement 7 — exactly the three events listed (`run.committed`, `governance.approval.requested`, `alert.raised`) or also `compliance.drift.escalated` and `seat.reservation.released`?
- **33.** Golden-cohort baseline-lock approval (Improvement 8) — do you want me to commit baseline SHAs from a single approved simulator run today, or wait for a product reviewer to approve the cohort scenario list before locking?
- **34.** Ownership of removing the stale `IMPROVEMENTS_COMPLETE.md` at repo root (§1.23) — the file references a non-existent `ArchLucid.DecisionEngine` project; safe to delete, but the file's history may still be useful for someone. Confirm I can delete (vs move to `docs/archive/`).

When you ask later "what pending questions do you have?" the answer is **items 29–34 from this assessment plus items 17–28 from the previous assessment that are still unresolved in `PENDING_QUESTIONS.md`**.

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
