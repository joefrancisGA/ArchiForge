> **Scope:** ArchLucid Independent Quality Assessment — Weighted 80.72% - full detail, tables, and links in the sections below.

# ArchLucid Independent Quality Assessment — Weighted 80.72%

**Author:** Independent assessor (no prior assessment reviewed)
**Date:** 2026-04-20
**Scope:** Whole-product (`ArchLucid.*` C# services, `archlucid-ui` Next.js shell, `infra/terraform-*` roots, `docs/`, billing + identity surfaces, CI/CD)
**Method:** Sampling of source, infra, and docs in the live tree. Scores are 1–100. Each weighted contribution = `score × weight`. Order in the body is **most-improvement-needed first**, computed as **leverage = (100 − score) × weight**.

**Headline weighted score:** **80.72%** (sum of weighted contributions ÷ sum of weights, where weights total **150**).

---

## 1. Weighted result table (sorted by improvement leverage)

| # | Quality | Score | Weight | Contribution | Leverage `(100−score)×w` |
|---|---------|------:|-------:|-------------:|-------------------------:|
| 1 | Marketability | 78 | 16 | 1,248 | **352** |
| 2 | Usability | 75 | 8 | 600 | **200** |
| 3 | Cognitive Load | 58 | 4 | 232 | **168** |
| 4 | Correctness | 80 | 8 | 640 | 160 |
| 5 | Supportability | 82 | 8 | 656 | 144 |
| 6 | Architectural Integrity | 80 | 7 | 560 | 140 |
| 7 | Evolvability | 78 | 6 | 468 | 132 |
| 8 | Maintainability | 72 | 4 | 288 | 112 |
| 9 | Reliability | 78 | 5 | 390 | 110 |
| 10 | Data Consistency | 78 | 5 | 390 | 110 |
| 11 | Security | 82 | 6 | 492 | 108 |
| 12 | Azure Compatibility & SaaS Deployment | 88 | 8 | 704 | 96 |
| 13 | Azure Developer Technology Optimized | 88 | 8 | 704 | 96 |
| 14 | Explainability | 85 | 6 | 510 | 90 |
| 15 | Policy and Governance Alignment | 83 | 5 | 415 | 85 |
| 16 | Traceability | 88 | 7 | 616 | 84 |
| 17 | Scalability | 78 | 3 | 234 | 66 |
| 18 | Performance | 70 | 2 | 140 | 60 |
| 19 | Cost-Effectiveness | 70 | 2 | 140 | 60 |
| 20 | Auditability | 88 | 5 | 440 | 60 |
| 21 | Observability | 88 | 5 | 440 | 60 |
| 22 | AI / Agent Readiness | 85 | 4 | 340 | 60 |
| 23 | Interoperability | 80 | 3 | 240 | 60 |
| 24 | Modularity | 85 | 3 | 255 | 45 |
| 25 | Testability | 85 | 3 | 255 | 45 |
| 26 | Availability | 78 | 2 | 156 | 44 |
| 27 | Manageability | 78 | 2 | 156 | 44 |
| 28 | Deployability | 82 | 2 | 164 | 36 |
| 29 | Accessibility | 65 | 1 | 65 | 35 |
| 30 | Extensibility | 80 | 1 | 80 | 20 |
| 31 | Documentation | 90 | 1 | 90 | 10 |
| | **Totals** | | **150** | **12,108** | |

**Weighted percent:** `12,108 ÷ 150 = 80.72%`

---

## 2. Areas needing the most improvement first

Each area carries the same six-section structure: **Score · Weighted Score · Justification · Trade-offs · Improvement recommendations**.

---

### 2.1 Marketability — **78** (weighted 1,248) · leverage 352

**Justification.** The repo carries an unusually complete go-to-market kit for an early-stage product: `EXECUTIVE_SPONSOR_BRIEF`, `PILOT_ROI_MODEL`, `PRODUCT_PACKAGING` with three named tiers (Core Pilot / Advanced Analysis / Enterprise Controls), `BUYER_PERSONAS`, `COMPETITIVE_LANDSCAPE`, `PRICING_PHILOSOPHY` with locked 2026 prices, `PILOT_SUCCESS_SCORECARD`, an Azure Marketplace SaaS offer (fulfillment v2) and Stripe alternative behind a single `IBillingProvider` abstraction, plus a `TRUST_CENTER`, `SUBPROCESSORS`, `DPA_TEMPLATE`, and `SLA_SUMMARY`. That density is itself a marketability asset because procurement and security reviewers can self-serve.

**The drag** is honest and visible inside the docs: the locked-price derivation in `docs/go-to-market/PRICING_PHILOSOPHY.md` § 5.1 explicitly applies a **−25 % trust discount** (no SOC 2 Type II, no published pen test), **−15 % reference discount** (no named logo), and **−10 % self-serve discount** (trial/billing loop not fully in production). Marketplace `ChangePlan` / `ChangeQuantity` are still acknowledged as `AcknowledgedNoOp` while `Billing:AzureMarketplace:GaEnabled=false`. The category is also new — the docs concede that ArchLucid invents an "AI Architecture Intelligence" sub-category, which means the buyer must be educated before they compare.

**Trade-offs.** Investing first in proof (a named pilot logo, a SOC 2 Type II attestation, a published pen test) raises ASP by removing the discount stack — but it slows time-to-revenue. Investing first in self-serve trial conversion compounds funnel volume but does little for the enterprise deal motion that the persona docs (Enterprise Architect, Platform Lead) describe. Pushing AI-native differentiators is high reward but requires sustained explainability and audit evidence, which competing EAM tools do not yet have.

**Improvement recommendations.**
- Treat the **−50 % discount stack** in `PRICING_PHILOSOPHY.md` § 5.1 as a **work-down backlog**, not a posture: each line (SOC 2, reference logo, trial GA) is a measurable initiative with a target date and a re-rate trigger.
- Land **one** named pilot reference customer with a published case study, then change `REFERENCE_NARRATIVE_TEMPLATE.md` from a template into a real artifact. This unlocks the `−15 %` reference discount.
- Move **Marketplace `ChangePlan` / `ChangeQuantity`** out of `AcknowledgedNoOp` (`Billing:AzureMarketplace:GaEnabled=true`) once seat/plan mapping is validated, so transactability claims are real, not aspirational.
- Replace abstract sub-category framing with a **one-screen "before/after"** comparison vs. LeanIX + GitHub Copilot ad-hoc usage in `COMPETITIVE_LANDSCAPE.md` — the current matrix is correct but heavy.

---

### 2.2 Usability — **75** (weighted 600) · leverage 200

**Justification.** Operator UX is mature on the surface: persona Day-1 docs (`day-one-developer.md`, `day-one-sre.md`, `day-one-security.md`), an `OPERATOR_QUICKSTART`, a Next.js operator shell with **progressive disclosure** of three product layers, an `archlucid` CLI with `dev up`, `doctor`, `support-bundle --zip`, and a `release-smoke` end-to-end command. The CLI redirects to the API via HTTP and the API publishes Swagger + an OpenAPI snapshot. That is genuinely friendly to evaluators.

**The drag.** The flip side is that the operator surface area is large and the disclosure layer requires the user to internalize four distinct concepts at once: the **three product layers** (Core Pilot vs Advanced Analysis vs Enterprise Controls), the **four UI shaping surfaces** (`nav-config` vs `useEnterpriseMutationCapability` vs `LayerHeader` vs `EnterpriseControlsContextHints`), the **dual run pipelines** (Coordinator vs Authority), and the **three auth modes** (`ApiKey` / `DevelopmentBypass` / `JwtBearer`). The `README.md` itself spans **300+ lines** and the architecture index lists ~50 entry-point docs. A first-pilot operator who only wants "request → commit → reviewable artifact" still has to ignore a lot.

**Trade-offs.** Aggressive simplification of the shell would erode operational accountability (especially for Enterprise Controls) and would fight existing Vitest seams (`authority-shaped-ui-regression.test.ts`, `authority-execute-floor-regression.test.ts`). On the other hand, leaving disclosure logic this rich means the *first* operator session is dominated by orientation, which is exactly what the `Core Pilot first-value rule` is trying to avoid.

**Improvement recommendations.**
- Add a **"first session" wizard route** (e.g. `/onboard`) that walks `request → seed → commit → manifest review` in a single linear flow, hides every Advanced/Enterprise hint until the first commit succeeds, and emits a *single* metric `archlucid_first_session_completed_total` that pilot scorecards can consume.
- Collapse the operator landing experience to **one verb per layer** (Core Pilot = "Run", Advanced = "Investigate", Enterprise = "Govern") instead of multiple entry points — `LayerHeader` already supports this; the change is in `nav-config.ts` group ordering and the sidebar caption strings.
- Replace the README's auth/rate-limit/CORS/Marketplace narrative with a **"What you actually need on day one"** stub at the top and demote the rest to `docs/REFERENCE.md`. The current README is a reference manual, not a getting-started document.
- Provide a **single "Healthy / Investigate / Down"** roll-up on the operator home dashboard, computed from `/health/ready` + recent SLO burn — replacing the current expectation that operators read individual histograms.

---

### 2.3 Cognitive Load — **58** (weighted 232) · leverage 168

**Justification.** This is the lowest-scoring area and the one most worth fixing because the *evidence is in the docs themselves*. A single packaging paragraph in `docs/PRODUCT_PACKAGING.md` is one sentence that runs to **roughly 400 words**, listing every Vitest seam by filename. The `CURSOR_PROMPTS_*` family contains nine separate index files. The **`ArchLucid` rename from `ArchiForge`** is "complete" (per `ArchLucid-Rename.mdc` cursor rule) yet the working-directory tree still has both `ArchiForge.*` and `ArchLucid.*` project folders side by side, and the workspace path is still `c:\ArchiForge\ArchiForge`. New contributors have to hold the rename plus the dual-pipeline plus the four UI shaping surfaces in their head simultaneously.

**The drag.** When a system is right but hard to *carry* in your head, every change costs more, every onboarding costs more, every PR review costs more. Cognitive load is also how good architectural decisions silently rot — contributors choose the path that requires fewer concepts, which is rarely the canonical one.

**Trade-offs.** Aggressive consolidation can erase nuance that a senior reviewer relies on (e.g. why `ReadAuthority` vs `ExecuteAuthority` differ by a single numeric rank). Splitting nuance into "what you need to know to ship" vs "what you need to know to design" requires editorial discipline and someone has to own it.

**Improvement recommendations.**
- **Delete the legacy `ArchiForge.*` project folders** from the working tree. The rename initiative is officially closed (`ArchLucid-Rename.mdc`); side-by-side directories are pure cognitive tax. If history matters, capture the move in one commit and use `git log --follow`.
- Adopt a **"one paragraph, one idea"** rule for `docs/PRODUCT_PACKAGING.md` and the README. The 400-word paragraph in §3 is unreadable; break it into a numbered list with at most one Vitest filename per bullet.
- **Collapse the dual run pipeline** (Coordinator vs Authority) into a single named pipeline with two adapters (see §2.6 Architectural Integrity). The two-pipeline mental model is the largest cognitive burden the codebase imposes.
- Introduce a **single "Concept map" page** (`docs/CONCEPTS.md`) that names the canonical 12 concepts a developer must own, and forbid new docs from inventing parallel vocabulary without updating that page (CI-checkable: grep for unknown capitalized concept words).

---

### 2.4 Correctness — **80** (weighted 640) · leverage 160

**Justification.** Strong, evidence-backed correctness posture: schema-validated typed findings, replay **verify** mode that returns 422 + Problem+JSON when regenerated output drifts, NetArchTest constraints in `ArchLucid.Architecture.Tests`, OpenAPI contract snapshot tests, Schemathesis + ZAP in CI, an explicit `ConfirmationDialog` for destructive actions, mutation testing with Stryker against four core assemblies, and ~700 test files driving the merge gate at **≥79 % merged line / ≥63 % merged branch / ≥63 % per-package**.

**The drag.** `docs/CODE_COVERAGE.md` itself states `ArchLucid.Api` is "**~60 % per-package line**" — i.e. **below the strict-profile floor**. Until that lifts, the gate is aspirational on `Api`. Marketplace `ChangePlan` / `ChangeQuantity` are still no-op responses (correctness via "we don't pretend to mutate" is fine, but it leaves a gap in the integration contract). RLS is "covered tables" — not "all scoped tables", and `MULTI_TENANT_RLS.md` § 7 explicitly calls correctness drift a residual risk.

**Trade-offs.** Driving `ArchLucid.Api` coverage to 79 % typically requires controller- and middleware-level tests that overlap with integration tests; the marginal correctness gain after ~75 % flattens. Hardening Marketplace flag-flip to GA needs operator validation and rollback discipline.

**Improvement recommendations.**
- Lift `ArchLucid.Api` per-package line to **≥ 79 %** with controller + middleware unit tests covering pagination cursors, range result, scope debug, auth debug, demo seed, metering admin (the same surfaces already trickled in late-session per `CODE_COVERAGE.md`).
- Remove the `Billing:AzureMarketplace:GaEnabled=false` short-circuit by validating `planId → tier` substring map and seat counts in a sandbox tenant, then default the flag to `true` with an explicit rollback runbook.
- Expand RLS coverage from "covered tables" to **all `dbo.*` tables that hold tenant-scoped data**; convert `RLS_RISK_ACCEPTANCE.md` from a template into a populated, dated register.

---

### 2.5 Supportability — **82** (weighted 656) · leverage 144

**Justification.** Excellent for a V1 product: `support-bundle --zip`, `doctor` command, `X-Correlation-ID` end-to-end (echoed in Problem+JSON `correlationId`), `health/live` vs `health/ready` vs `health` split (with 503 from any unhealthy check), per-runbook `docs/runbooks/` library (replay drift, advisory scan failures, comparison replay rate limits, provenance indexing, API key rotation, geo failover), and structured Problem+JSON with stable `type` URIs.

**The drag.** Many runbooks are short and conceptually correct but lack worked examples (especially geo failover and replay drift). The support bundle composition isn't documented in a single index, so support engineers have to read the CLI source to know what it includes. There is no documented "support tier matrix" tying customer tier (Team / Pro / Enterprise) to response SLA except in `PRICING_PHILOSOPHY.md`.

**Trade-offs.** More runbook detail makes them harder to keep current; fewer runbooks make incidents slower.

**Improvement recommendations.**
- Add a **`docs/SUPPORT_BUNDLE_CONTENTS.md`** that lists every artifact the CLI collects (logs, env, version, recent runs, recent audit rows) so support engineers can predict bundle contents and customers can review before sending.
- Add **"first 5 minutes"** sections to each runbook (`docs/runbooks/*`): the first commands to run, the first three things to rule out.
- Convert the SLA targets in `PRICING_PHILOSOPHY.md` into a single **`docs/SUPPORT_TIER_MATRIX.md`** with response-time, weekend coverage, and CSM contact rules — referenced from every runbook header.

---

### 2.6 Architectural Integrity — **80** (weighted 560) · leverage 140

**Justification.** Strong: clean container boundaries enforced by `ArchLucid.Architecture.Tests` (NetArchTest), 20+ ADRs, an explicit `DI_REGISTRATION_MAP`, primary-constructor + early-return + LINQ pipeline conventions, `Contracts` / `Contracts.Abstractions` split, separate `Persistence.Data.*` sub-namespaces. The product clearly evolved from a layered .NET service into a multi-host architecture (`Api`, `Worker`, optional `Logic Apps`) with one composition root (`Host.Composition`).

**The drag.** Two real concerns:
1. **Dual run pipelines** (Coordinator orchestrators vs Authority pipeline) co-exist; `DUAL_PIPELINE_NAVIGATOR.md` is the index but the duplication shows up in audit (`CoordinatorRun*` event types vs authority `RunStarted` / `RunCompleted`), in metrics, and in the data model (two "run" tables). This is a textbook strangler residual that should be closed, not preserved as architecture.
2. **Legacy `ArchiForge.*` projects remain alongside `ArchLucid.*`** (visible at the workspace root). Either they are dead and should be removed, or they are alive and the rename is not actually complete.

**Trade-offs.** Collapsing the two pipelines requires a one-shot migration of audit event types and dashboards; leaving them split means every new feature picks one and doubles the test surface. Removing the legacy `ArchiForge.*` projects is a destructive operation that needs careful CI verification.

**Improvement recommendations.**
- Pick a **target pipeline** (Authority is the more recent, finer-grained one) and convert the Coordinator orchestrators into thin adapters that emit Authority pipeline stages. Migrate `CoordinatorRun*` audit types behind the curtain and document the deprecation in `CHANGELOG.md`.
- **Delete every `ArchiForge.*` directory at the working-tree root** in a single commit, fix the build, and update `ArchLucid.sln`. Keep the rename initiative truly "closed" per `ArchLucid-Rename.mdc`.
- Ratchet the `ArchLucid.Architecture.Tests` rules to **deny** any new dependency from `Application` onto `Persistence.*` data namespaces directly — orchestration must go through repository interfaces.

---

### 2.7 Evolvability — **78** (weighted 468) · leverage 132

**Justification.** API versioning via path segment (`/v1/...`) plus `api-version` reader, deprecation header policy, OpenAPI snapshot diffing in CI, `IBillingProvider` abstraction with two real implementations, `Contracts.Abstractions` split, modular Terraform roots (eight stacks each with its own `tfvars.example`), explicit `BREAKING_CHANGES.md`. Easy to imagine a `/v2/...` rolling out without breaking `v1`.

**The drag.** The dual pipeline (see § 2.6), the dual project layout (`ArchiForge.*` + `ArchLucid.*`), and the layered UI shaping all add inertia. Each future change requires a contributor to know which seam to update — and the `PRODUCT_PACKAGING.md` § 3 *Contributor drift guard* is itself evidence that drift is happening.

**Trade-offs.** Strong evolvability tends to come from fewer concepts each used in more places; ArchLucid currently has many concepts each used in one place.

**Improvement recommendations.**
- Adopt a **per-area "ownership card"** (`docs/OWNERS.md` or `CODEOWNERS`) so that each subsystem has one author of record and one maintainer; this is the cheapest way to harden the contributor drift guard.
- Document a **"new feature" walkthrough** that makes the four UI shaping surfaces visible up front (currently a contributor learns them by getting a Vitest fail), so the seam contract is teachable, not archaeological.

---

### 2.8 Maintainability — **72** (weighted 288) · leverage 112

**Justification.** Tight C# house style (primary ctors, expression-bodied members, `is null` patterns, single-line guards), `editorconfig`, `Directory.Build.props`, `Directory.Packages.props` (centralized package management), Stryker mutation testing, formatting docs, `METHOD_DOCUMENTATION.md`. The cs-rules folder is 16 distinct rules, and they are visibly enforced.

**The drag.** ~2,000 production C# files and ~700 test files in a single solution, plus `archlucid-ui` with its own Vitest and Playwright matrix, plus 89 Terraform files across 12 roots. The result is that "build everything" is slow, "test everything" is slower, and the test-script surface (`test-core`, `test-fast-core`, `test-integration`, `test-slow`, `test-full`, `test-sqlserver-integration`, `test-ui-smoke`, `test-ui-unit`, plus PowerShell + cmd variants) is itself a maintenance liability.

**Trade-offs.** Splitting the monorepo would break the architectural test gates and the merged-coverage gate. Collapsing the test scripts into one parameterized entry point shrinks the surface but increases the cost of getting the parameters right.

**Improvement recommendations.**
- Replace `test-*.cmd` + `test-*.ps1` with a **single `test.ps1 -Tier <core|fast-core|integration|sql|full|ui-smoke|ui-unit>`** entry point and delete the rest. The current matrix is duplicated tooling, not configuration.
- Introduce a **build profile metric** (clean build wall-clock, full-regression wall-clock) tracked in `docs/CHANGELOG.md` per release. What is measured is what improves.
- Convert `Directory.Build.props` / `Directory.Build.targets` into the **only** place to add new analyzers / warn-as-error rules; today new rules sometimes show up project-by-project.

---

### 2.9 Reliability — **78** (weighted 390) · leverage 110

**Justification.** Real reliability investments: circuit breaker on the LLM hot path with audit bridge, retry on durable audit log writes (`DurableAuditLogRetry`), idempotent trial lifecycle transitions, idempotency key on billing webhooks (`BillingWebhookEvents.EventId` PK), DLQ on Service Bus, `terraform-sql-failover` IaC for the read/write listener, geo-failover drill runbook, and explicit RTO/RPO targets per environment.

**The drag.** The reliability evidence is conceptually correct but operational maturity is unproven outside drills; there is no documented record of a real failover. The k6 soak workflow is informational only ("not an SLO gate") so degradation under sustained load is not actively detected. The chaos-testing doc exists but no evidence of a regular drill cadence.

**Trade-offs.** Promoting k6 soak to an SLO gate means flaky CI on bad days and either dropping the gate or paying for stable load infrastructure.

**Improvement recommendations.**
- Schedule a **quarterly geo-failover drill** with a written postmortem each time, and link the most recent one from `docs/runbooks/GEO_FAILOVER_DRILL.md` so the runbook is *evidence*, not just instruction.
- Promote one k6 soak metric (e.g. `p95 < 500ms` on the read mix) from informational to **merge-blocking on the nightly schedule** with a documented manual-override path.
- Adopt a **chaos cadence**: each release picks one chaos experiment from `CHAOS_TESTING.md` and records the result in the changelog.

---

### 2.10 Data Consistency — **78** (weighted 390) · leverage 110

**Justification.** Genuinely above the bar for a V1: a `DATA_CONSISTENCY_MATRIX`, `DataConsistencyOrphanProbeHostedService` with a counter `archlucid_data_consistency_orphans_detected_total` for `ComparisonRecords` / `GoldenManifests` / `FindingsSnapshots` orphans, RLS on tenant-scoped tables, `EXECUTE AS OWNER` stored procs for billing mutations to keep `ArchLucidApp` `DENY INSERT/UPDATE/DELETE`, append-only `dbo.AuditEvents` with database-level `DENY UPDATE/DELETE`, transactional outboxes for integration events.

**The drag.** Detection-only orphans (per the metric description) means consistency drift is *visible* but not *self-healing*; an operator still has to act. The two-runs schema (Coordinator vs Authority) is a structural inconsistency risk that will only get worse as features land on both sides.

**Improvement recommendations.**
- Convert the orphan probe from detection to **detection + alert + scheduled cleanup job** (with break-glass for review-mode) under a clear policy in `docs/AUDIT_RETENTION_POLICY.md`.
- Resolve the dual-run schema (see § 2.6) so consistency invariants are stated once.

---

### 2.11 Security — **82** (weighted 492) · leverage 108

**Justification.** Layered defense: Entra ID + JWT + optional API keys (with fail-closed default — `Enabled=false` rejects callers, not the opposite), three policy tiers (`ReadAuthority` / `ExecuteAuthority` / `AdminAuthority`), RLS using `SESSION_CONTEXT`, deny-by-default CORS in production, CSP / X-Frame-Options / X-Content-Type-Options baseline, gitleaks + Trivy + CodeQL + ZAP + Schemathesis in CI, threat models (System STRIDE + Ask/RAG STRIDE), explicit "never expose SMB / port 445" workspace rule, key rotation runbook, secret-validation startup checks (rejects placeholder API keys).

**The drag.** Self-stated gaps are **real**: no formal pen test (per the threat model "gaps to track"), incomplete RLS coverage, no systematic PII redaction in LLM prompts, SOC 2 Type II not yet attested. The discount stack in `PRICING_PHILOSOPHY.md` (`−25 %` trust discount) is the commercial cost of these gaps.

**Trade-offs.** A pen test is a one-time cost that immediately removes a list of "we don't actually know yet" answers; SOC 2 Type II is a sustained operational commitment.

**Improvement recommendations.**
- Commission a **named third-party pen test** of the production deployment posture and publish the redacted summary in `TRUST_CENTER.md`.
- Add a **prompt-redaction backlog item** to `ASK_RAG_THREAT_MODEL.md` with an owner and a date; redaction policy can begin as deny-list of tenant secrets and PII patterns before becoming a model-side concern.
- Expand RLS to **every tenant-scoped table**; close `RLS_RISK_ACCEPTANCE.md` items for the migration history.

---

### 2.12 Azure Compatibility & SaaS Deployment Readiness — **88** (weighted 704) · leverage 96

**Justification.** Eight Terraform roots cover Container Apps + Front Door/WAF + private endpoints + Entra ID + Service Bus + Storage + SQL failover group + Monitoring; a ninth (`terraform-orchestrator`) is a CI `terraform validate` anchor; APIM Consumption is optional via `infra/terraform`. The Marketplace SaaS offer (fulfillment v2, managed-identity bearer for `marketplaceapi.microsoft.com/.default`) is implemented, with `Subscribe` / `Suspend` / `Reinstate` / `Unsubscribe` already mutating SQL via `sp_Billing_*` and `ChangePlan` / `ChangeQuantity` behind a GA flag.

**The drag.** Marketplace `ChangePlan` / `ChangeQuantity` are no-op until `GaEnabled=true`. There is no documented "first Azure subscription in 60 minutes" success story to point at.

**Improvement recommendations.**
- Make the **`FIRST_AZURE_DEPLOYMENT.md`** path measured and time-boxed — publish "from `az login` to first committed manifest" wall-clock targets.
- Flip the Marketplace GA flag (see § 2.4) and capture proof in a screenshot gallery entry.

---

### 2.13 Azure Developer Technology Optimized — **88** (weighted 704) · leverage 96

**Justification.** .NET 10, C# 12 idioms enforced by cursor rules, ASP.NET Core minimal hosting + `Asp.Versioning`, Dapper (per house preference) over heavy ORMs, Entra ID, Azure Container Apps with HTTP scale rules, Service Bus topics + JSON schemas for integration events, Azure SQL with failover groups, Blob Storage with private endpoints, Key Vault, Application Insights / OpenTelemetry, Logic Apps (Standard) for Marketplace fulfillment subscription, Front Door + WAF.

**The drag.** No managed integration with Azure App Configuration yet (`AZURE_APP_CONFIGURATION_FUTURE_ADOPTION.md` is the placeholder). A few patterns (e.g. `IDiagramImageRenderer` for DOCX export) still default to a `Null*` implementation in production unless the operator opts in.

**Improvement recommendations.**
- Adopt **Azure App Configuration + feature flags** for `Billing:AzureMarketplace:GaEnabled` and similar lever-flags so flips no longer require redeploys.
- Default the diagram renderer to **`MermaidCliDiagramImageRenderer`** in container images that ship with `mmdc`, so the customer-visible DOCX is the embedded-PNG version, not the source-block fallback.

---

### 2.14 Explainability — **85** (weighted 510) · leverage 90

**Justification.** First-class: `ExplanationResult` envelope, `RunExplanationSummaryService` with a faithfulness checker (token-overlap heuristic against finding traces), `archlucid_explanation_faithfulness_ratio` and `archlucid_explanation_aggregate_faithfulness_fallback_total` metrics, `EXPLANATION_SCHEMA.md`, `EXPLAINABILITY_TRACE_COVERAGE.md`, structured `ExplainabilityTrace` per finding, `/v1/explain` endpoints with `aggregate` and `compare` views, optional advisory scans.

**The drag.** Faithfulness is a token-overlap heuristic — it will accept paraphrased nonsense and reject correctly summarized findings that use synonyms. There is no end-user-visible "this explanation was downgraded" affordance in the UI, only a metric.

**Improvement recommendations.**
- Surface a small **"this summary was rewritten from manifest text because the LLM narrative was below faithfulness threshold"** banner in the run detail UI when `archlucid_explanation_aggregate_faithfulness_fallback_total` was hit for that run.
- Replace the token-overlap heuristic with a **rubric-grounded LLM-judge** on a sampled subset (offline), and treat the heuristic as the cheap online gate.

---

### 2.15 Policy & Governance Alignment — **83** (weighted 415) · leverage 85

**Justification.** Governance approvals + policy packs + `governance/approval-requests/batch-review` + audit retention tiers (hot / warm / cold) + segregation-of-duties on Enterprise tier + `dbo.GovernanceWorkflow` with dual-write audit (baseline + durable), `RequireAuditor` for CSV export, OperationalTransparency doc, SOC 2 roadmap doc, DPA template, subprocessors list.

**Improvement recommendations.**
- Convert `SOC2_ROADMAP.md` into a **dated initiative** with quarterly milestones and an ownership row.
- Wire an end-to-end **policy-pack drift report** that compares applied packs across environments — the data is already there in `PolicyPacks` and `BaselineMutationAudit`.

---

### 2.16 Traceability — **88** (weighted 616) · leverage 84

**Justification.** Excellent: `RunRecord`, `GoldenManifest`, `FindingsSnapshot`, `ComparisonRecord`, `ExportRecord`, `dbo.AuditEvents` with three indexes (scope-time, correlation, run-time), `X-Correlation-ID` echo on Problem+JSON, agent execution traces (with blob + inline fallback metrics), replay-as-new-comparison-record. Worth showing buyers as-is.

**Improvement recommendations.**
- Surface a **"trace for this run"** download in the operator UI that bundles the `AuditEvents` rows + finding traces + agent prompt blobs as a single ZIP for governance review.

---

### 2.17–2.31 Remaining areas (compact)

These score well or are low-weight; recommendations are tighter.

- **Scalability (78, w=3)** — Container Apps min/max + HTTP scale rules + Redis cache + SQL failover; **add documented load-tier targets** ("Pro tier sustains X runs/min") to the pricing tiers so scalability is buyer-visible.
- **Performance (70, w=2)** — Hot-path read cache + Dapper + k6 soak (informational); **publish a single perf scorecard** (P50/P95 for `commit`, `manifest`, `replay`) per release.
- **Cost-Effectiveness (70, w=2)** — APIM Consumption + Container Apps min replicas; **add per-run LLM token cost histogram** and a CSM-facing weekly cost summary.
- **Auditability (88, w=5)** — `dbo.AuditEvents` with `DENY UPDATE/DELETE` + dual-write + 93-constant CI count guard; recommendations covered in § 2.10 + § 2.15.
- **Observability (88, w=5)** — `ArchLucid` meter with dozens of stable instruments + ActivitySource + Prometheus rules + Grafana dashboards + SLO recording rules; **fold business KPIs into one operator dashboard** so non-SREs can read it.
- **AI / Agent Readiness (85, w=4)** — multi-agent (Topology / Cost / Compliance / Critic) + simulator mode + agent trace forensics + output evaluation + quality gate; **add a per-agent quality regression dashboard** so prompt drift is caught release-over-release.
- **Interoperability (80, w=3)** — OpenAPI snapshot + AsyncAPI 2.6 + CloudEvents + Bruno collection + integration catalog; **publish a Postman collection** alongside Bruno for buyer parity.
- **Modularity (85, w=3)** — many `ArchLucid.*` projects with clear bounded contexts; recommendations in § 2.6 (collapse dual pipeline) and § 2.8 (delete legacy folders).
- **Testability (85, w=3)** — `ArchLucidApiFactory` + Stryker + Vitest + Playwright + axe + k6 + Schemathesis; **add Stryker to `ArchLucid.Api`** (currently four assemblies are gated; the Api is the customer-facing one).
- **Availability (78, w=2)** — SQL failover group + Container Apps replicas + 99.5 % SLO + geo drill; recommendations in § 2.9.
- **Manageability (78, w=2)** — `archive-by-ids`, `archive-batch`, demo seed, support bundle; **add a single `/v1/admin/health-roll-up`** that returns one of `Healthy / Investigate / Down` for the operator UI.
- **Deployability (82, w=2)** — Docker compose profiles + Terraform per-stack + RC drill + landing-zone provisioning; **publish a verified one-liner for "Azure greenfield → first committed manifest"** based on `FIRST_AZURE_DEPLOYMENT.md`.
- **Accessibility (65, w=1)** — WCAG 2.1 AA target with axe-core on top 5 pages and `eslint-plugin-jsx-a11y`; **expand axe coverage to all operator routes** (currently a known gap) and publish the axe history per release.
- **Extensibility (80, w=1)** — `HOWTO_ADD_COMPARISON_TYPE` + billing provider abstraction + finding engine catalog; **publish a `HOWTO_ADD_FINDING_ENGINE`** to make the finding engine surface as extensible as comparison types.
- **Documentation (90, w=1)** — 187 docs files, persona Day-1, runbooks, ADRs, glossary, C4; the ceiling here is editorial discipline, not coverage. See § 2.3 (Cognitive Load).

---

## 3. Six best improvements (highest leverage)

These are the six initiatives that move the weighted score the most, ordered by leverage. Each is owned by a single area but most help several.

| # | Initiative | Primary area(s) helped | Why it has the most leverage |
|---|------------|------------------------|------------------------------|
| 1 | **Land one named pilot reference + close the `−50 %` discount stack in `PRICING_PHILOSOPHY.md`** (start with the reference logo, then SOC 2 Type II attestation, then trial-billing GA). | Marketability, Security (perception), Policy & Governance | Removes 25 + 15 + 10 = 50 percentage points of buyer-visible discount; converts marketability from "claims" to "evidence". |
| 2 | **Ship a Core-Pilot-only "first session wizard"** at `/onboard` that drives `request → seed → commit → manifest review` linearly, hides every Advanced/Enterprise hint until first commit, and emits `archlucid_first_session_completed_total`. | Usability, Cognitive Load, Marketability | Directly attacks the two highest-leverage non-marketability gaps. The wizard is also a marketing demo asset. |
| 3 | **Collapse dual pipelines + delete legacy `ArchiForge.*` folders** in one explicit refactor, with audit-event-type migration and a deprecation note in `CHANGELOG.md`. | Architectural Integrity, Cognitive Load, Maintainability, Evolvability, Data Consistency | Removes the largest single conceptual debt; helps five qualities at once. |
| 4 | **Lift `ArchLucid.Api` per-package coverage to ≥ 79 %, add Stryker to `ArchLucid.Api`, flip Marketplace `GaEnabled=true`** in one correctness sprint. | Correctness, Marketability (transactability), Testability | Closes the visible coverage shortfall and makes a marketability claim real. |
| 5 | **Commission a third-party pen test + expand RLS to every tenant-scoped table + add LLM prompt PII redaction** in one security sprint; publish summaries in `TRUST_CENTER.md`. | Security, Auditability, Marketability | Three known security gaps removed; supports the trust-discount work-down. |
| 6 | **Replace the `test-*.cmd/.ps1` matrix with one `test.ps1 -Tier <…>` entry point and consolidate runbook / docs landing pages** (single Concept Map, single Day-1 per persona), with CI guard on capitalized concept words. | Cognitive Load, Maintainability, Documentation, Supportability | Editorial-discipline initiative; small code change, large carrying-cost reduction for every contributor and operator from now on.

---

## 4. Cursor prompts for the first two improvements

See the companion file:

- **[`docs/CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_20.md`](CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_20.md)** — paste-ready Agent prompts for **Improvement 1 (Reference logo + discount-stack work-down)** and **Improvement 2 (Core-Pilot first-session wizard)**.
