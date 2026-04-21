> **Scope:** For product, engineering, and GTM leads: independent weighted quality assessment from shipped repository evidence (2026-04-20), scores, blockers, and improvement prompts; not external benchmarking or prior internal score sheets.

# ArchLucid Independent Quality Assessment — Weighted 64.60%

**Audience:** Founder / product owner, engineering lead, GTM lead.

**Method.** Each quality was scored 1–100 against what is actually shipped in the repo today (code, docs, scripts, tests, Terraform). The weighted percent is `Σ(score × weight) / Σ(weight)` where the weights given total exactly 100, so the result is a direct weighted percent.

**Headline weighted score: 64.60%.**

This document is ordered **by remaining weighted improvement headroom** (`(100 − score) × weight`) so the highest-leverage areas come first, rather than by category. The summary tables, blocker lists, and Cursor prompts for the six best improvements appear at the end. Pending questions that I cannot answer without you are listed in the final section so you can come back to them later.

---

## 1. Scoring summary by improvement priority (highest leverage first)

| # | Quality | Category | Weight | Score (1-100) | Weighted contribution | Improvement headroom (weighted) |
|---|---------|----------|--------|---------------|----------------------|--------------------------------|
| 1 | Marketability | Commercial | 8 | 62 | 4.96 | **3.04** |
| 2 | Adoption Friction | Commercial | 6 | 55 | 3.30 | **2.70** |
| 3 | Time-to-Value | Commercial | 7 | 65 | 4.55 | 2.45 |
| 4 | Proof-of-ROI Readiness | Commercial | 5 | 55 | 2.75 | 2.25 |
| 5 | Differentiability | Commercial | 4 | 55 | 2.20 | 1.80 |
| 6 | Workflow Embeddedness | Enterprise | 3 | 45 | 1.35 | 1.65 |
| 7 | Usability | Enterprise | 3 | 58 | 1.74 | 1.26 |
| 8 | Architectural Integrity | Engineering | 3 | 60 | 1.80 | 1.20 |
| 9 | Executive Value Visibility | Commercial | 4 | 70 | 2.80 | 1.20 |
| 10 | Correctness | Engineering | 4 | 72 | 2.88 | 1.12 |
| 11 | Trustworthiness | Enterprise | 3 | 65 | 1.95 | 1.05 |
| 12 | Compliance Readiness | Enterprise | 2 | 50 | 1.00 | 1.00 |
| 13 | Interoperability | Enterprise | 2 | 55 | 1.10 | 0.90 |
| 14 | Maintainability | Engineering | 2 | 58 | 1.16 | 0.84 |
| 15 | Traceability | Enterprise | 3 | 72 | 2.16 | 0.84 |
| 16 | Procurement Readiness | Enterprise | 2 | 60 | 1.20 | 0.80 |
| 17 | Decision Velocity | Commercial | 2 | 62 | 1.24 | 0.76 |
| 18 | Security | Engineering | 3 | 75 | 2.25 | 0.75 |
| 19 | Commercial Packaging Readiness | Commercial | 2 | 65 | 1.30 | 0.70 |
| 20 | Reliability | Engineering | 2 | 68 | 1.36 | 0.64 |
| 21 | Cognitive Load | Engineering | 1 | 38 | 0.38 | **0.62** |
| 22 | Policy and Governance Alignment | Enterprise | 2 | 70 | 1.40 | 0.60 |
| 23 | Data Consistency | Engineering | 2 | 70 | 1.40 | 0.60 |
| 24 | Explainability | Engineering | 2 | 70 | 1.40 | 0.60 |
| 25 | AI/Agent Readiness | Engineering | 2 | 70 | 1.40 | 0.60 |
| 26 | Auditability | Enterprise | 2 | 75 | 1.50 | 0.50 |
| 27 | Stickiness | Commercial | 1 | 50 | 0.50 | 0.50 |
| 28 | Azure Compatibility (SaaS Deploy) | Engineering | 2 | 75 | 1.50 | 0.50 |
| 29 | Cost-Effectiveness | Engineering | 1 | 62 | 0.62 | 0.38 |
| 30 | Customer Self-Sufficiency | Enterprise | 1 | 65 | 0.65 | 0.35 |
| 31 | Manageability | Engineering | 1 | 65 | 0.65 | 0.35 |
| 32 | Extensibility | Engineering | 1 | 65 | 0.65 | 0.35 |
| 33 | Evolvability | Engineering | 1 | 65 | 0.65 | 0.35 |
| 34 | Template and Accelerator Richness | Commercial | 1 | 55 | 0.55 | 0.45 |
| 35 | Availability | Engineering | 1 | 55 | 0.55 | 0.45 |
| 36 | Performance | Engineering | 1 | 55 | 0.55 | 0.45 |
| 37 | Scalability | Engineering | 1 | 55 | 0.55 | 0.45 |
| 38 | Accessibility | Enterprise | 1 | 70 | 0.70 | 0.30 |
| 39 | Change Impact Clarity | Enterprise | 1 | 70 | 0.70 | 0.30 |
| 40 | Deployability | Engineering | 1 | 70 | 0.70 | 0.30 |
| 41 | Testability | Engineering | 1 | 72 | 0.72 | 0.28 |
| 42 | Supportability | Engineering | 1 | 75 | 0.75 | 0.25 |
| 43 | Modularity | Engineering | 1 | 75 | 0.75 | 0.25 |
| 44 | Azure Ecosystem Fit | Engineering | 1 | 75 | 0.75 | 0.25 |
| 45 | Observability | Engineering | 1 | 78 | 0.78 | 0.22 |
| 46 | Documentation | Engineering | 1 | 80 | 0.80 | 0.20 |
| **Σ** | | | **100** | | **64.60** | — |

> The **weighted percent in the title** (`64.60`) is `Σ(score × weight) / 100` rounded to two decimal places.

---

## 2. Detailed scoring, justification, trade-offs, and recommendations

Each quality is presented in the same priority order as Section 1 (largest weighted improvement opportunity first). For brevity, lower-weight, high-score qualities are grouped at the end.

### 2.1 Marketability (Commercial, weight 8) — **62**

- **Why this score.** Strong narrative scaffolding exists: `docs/EXECUTIVE_SPONSOR_BRIEF.md`, `docs/go-to-market/POSITIONING.md`, `COMPETITIVE_LANDSCAPE.md`, `IDEAL_CUSTOMER_PROFILE.md`, `BUYER_PERSONAS.md`, `PRODUCT_DATASHEET.md`, an honest three-layer packaging story (`PRODUCT_PACKAGING.md`), and a Marketplace SaaS offer doc. Pricing is published in a single source of truth. Narrative is honest (limits-of-AI section, "what not to over-claim").
- **Why not higher.** Zero **published** reference customers (the table in `docs/go-to-market/reference-customers/README.md` has only an `EXAMPLE_DESIGN_PARTNER` placeholder), no public website assets, no analyst coverage, no third-party social proof in repo, no customer logo wall, and the category framing ("AI Architecture Intelligence") is self-defined and not yet validated by analysts or buyers.
- **Trade-offs.** The team has rightly invested in narrative discipline before chasing PR. That has produced a defensible story but defers the moment of external validation. Marketing pace is a function of design-partner deal closure speed.
- **Recommendation.** (1) Convert at least one design partner into a `Published` row in the reference table and flip the CI guard in `scripts/ci/check_reference_customer_status.py` from warning to merge-blocking. (2) Ship a one-page public landing/datasheet derived from `PRODUCT_DATASHEET.md`. (3) Record a 2-minute Loom of the Core Pilot first-30-minutes path. (4) Publish an outcome quote tied to the sponsor brief language.

### 2.2 Adoption Friction (Commercial, weight 6) — **55**

- **Why this score.** Genuine reductions exist: `docs/FIRST_30_MINUTES.md` is Docker-only; `scripts/demo-start.ps1` provides a Contoso seed; the operator UI uses progressive disclosure; the agent simulator removes any LLM cost dependency for evaluation; `docs/go-to-market/TRIAL_AND_SIGNUP.md` describes a sub-5-minute self-serve trial with the live merge-blocking spec `archlucid-ui/e2e/live-api-trial-end-to-end.spec.ts`.
- **Why not higher.** The hosted self-serve trial path itself is **not yet a running URL** any prospect can reach; it is a designed flow with a passing spec. A real pilot still requires SQL, Entra/JWT or API key configuration, optional Azure OpenAI, and operator decisions across `ArchLucidAuth` modes. The UI shaping model (four surfaces — `nav-config.ts`, `nav-shell-visibility.ts`, `useEnterpriseMutationCapability`, `LayerHeader`) is sophisticated for contributors but adds adoption-time concept burden if buyers ever read about it. Phase 7.5–7.8 rename items (Terraform `state mv`, Entra app rename, repo rename) are deferred and bleed into customer-side cleanup.
- **Trade-offs.** Optionality is currently winning over opinionation — there are 3 auth modes, 3 product layers, 2 run pipelines (coordinator + authority), Sql vs other storage providers. Each option is documented but each option is a decision a pilot operator must make.
- **Recommendation.** (1) Stand up an actual hosted trial URL and bind the trial spec to it in CI. (2) Ship a single `archlucid pilot up` command that runs SQL+API+UI+seed in one step (compose profile already exists, but wrap it). (3) Bias defaults: ship with `DevelopmentBypass` only on Development, but include a **minimal-friction JWT preset** for pilots with one-paragraph instructions instead of three modes shown side-by-side in the README. (4) Hide the four UI shaping surfaces from buyer-facing docs entirely — they are contributor concerns.

### 2.3 Time-to-Value (Commercial, weight 7) — **65**

- **Why this score.** The core path "request → execute (simulator) → commit → manifest" can be completed in one sitting with the demo seed. The CLI `--quick` flag short-circuits the agent submission step. `release-smoke.ps1` and `v1-rc-drill.ps1` mechanically prove the path. `archlucid_first_session_completed_total` is a real metric — first-value is being measured.
- **Why not higher.** A *committed manifest* is the technical first value, but the *business* first value is "a reviewer signed off using ArchLucid evidence." Nothing in the repo automates the bridge from committed manifest to a reviewer's actual sign-off action; that depends on integrating with the team's existing review tool (which is not yet built — see Workflow Embeddedness). Real (non-simulator) agents require Azure OpenAI configuration and `LlmPromptRedaction` review.
- **Trade-offs.** The simulator path is fast but a sponsor will discount value perceived from synthetic data. Adding "real LLM with redaction on sample brief" as a default trial flow would lengthen first-run but raise perceived value.
- **Recommendation.** Add a `archlucid pilot first-value-report <runId>` command that emits a one-page Markdown summarizing what was committed, time-to-commit vs baseline (from `PILOT_ROI_MODEL.md`), and links to manifest + audit + decision trace. Make it the natural artifact a sponsor sees after the first session.

### 2.4 Proof-of-ROI Readiness (Commercial, weight 5) — **55**

- **Why this score.** Strong measurement scaffolding: `docs/PILOT_ROI_MODEL.md`, `docs/go-to-market/PILOT_SUCCESS_SCORECARD.md`, `docs/go-to-market/ROI_MODEL.md`, `archlucid_first_session_completed_total`, `archlucid_runs_created_total`, `archlucid_findings_produced_total`, plus an explicit baseline-capture protocol.
- **Why not higher.** All ROI claims are based on **internal modeling** (`~$294K annual savings for a 6-architect team`, `break-even at ~180 architect-hours/year`). There is no published case study with real numbers, no pilot scorecard filled in for a real customer, and no automated "pilot summary report" that turns the metrics into a sponsor PDF.
- **Recommendation.** Wire the existing metrics into a `GET /v1/pilots/{tenant}/scorecard` endpoint that emits the `PILOT_SUCCESS_SCORECARD.md` shape with real values populated. Then publish one redacted real-pilot version.

### 2.5 Differentiability (Commercial, weight 4) — **55**

- **Why this score.** The combination "AI-orchestrated architecture review **with** committed manifest, decision trace, append-only audit, and explainability faithfulness measurement" is genuinely unusual — `COMPETITIVE_LANDSCAPE.md` correctly notes that no incumbent fully occupies this space. The faithfulness metric (`archlucid_explanation_faithfulness_ratio`) and citation chips on the explanation aggregate are unusual technical commitments.
- **Why not higher.** Buyers do not yet have a one-sentence wedge. "Reviewable, defensible architecture package" is honest but undifferentiated against generic copilots in a buyer's first 30 seconds. Competitors (LeanIX, Ardoq) own the EAM seat that buyers already pay for; the wedge for *displacing budget* is not yet sharp.
- **Recommendation.** Anchor the wedge on **what only ArchLucid produces**: the *committed manifest + decision trace + faithfulness measurement* as a single "evidence package" object. Make the package exportable as a single signed bundle and put the bundle on the landing page.

### 2.6 Workflow Embeddedness (Enterprise, weight 3) — **45**

- **Why this score.** Optional integration paths exist: `docs/INTEGRATION_EVENTS_AND_WEBHOOKS.md`, an AsyncAPI spec, a CloudEvents envelope, `docs/integrations/`, the global `dotnet tool` CLI, OpenAPI surfaces, and a Logic Apps Terraform stack with workflow scaffolding for `governance-approval-routing`, `incident-chatops`, `marketplace-fulfillment-handoff`, `promotion-customer-notifications`, `trial-lifecycle-email`.
- **Why not higher.** No first-party connector ships **today** for Confluence, ServiceNow, Jira, GitHub PR comments, Azure DevOps, or VS Code. The Logic Apps workflow folders are mostly README scaffolds. `V1_SCOPE.md` §3 explicitly calls VS Code/IDE shell integration **out of scope**. Reviewers still leave their tool to use ArchLucid.
- **Recommendation.** Pick **one** native integration (a GitHub Action that posts the manifest delta as a PR check, or a Confluence "publish manifest" action) and ship it end-to-end with Terraform. This is the single highest-leverage Enterprise improvement.

### 2.7 Usability (Enterprise, weight 3) — **58**

- **Why this score.** Operator UI is real, has progressive disclosure, role-aware shaping, accessible alert dialogs, live regions, axe gates. CLI surface is documented. Wizards exist (`FIRST_RUN_WIZARD.md`, `ONBOARDING_WIZARD.md`).
- **Why not higher.** The conceptual surface is heavy: three layers, four UI shaping surfaces, a "dual pipeline navigator" (coordinator + authority), Read/Execute/Admin authority, and many "*Show more links*" disclosures. A reviewer trying ArchLucid for the first time still has to learn the difference between **manifest, golden manifest, decision trace, finding, advisory scan, alert, comparison, replay, authority chain**. The sheer count of operator concepts (visible from `docs/CONCEPTS.md`, `GLOSSARY.md`) is large for a V1 product.
- **Recommendation.** Add a single "what am I looking at?" inline glossary popover in the operator UI and aggressively collapse Enterprise-Controls vocabulary out of Core Pilot copy unless the user has already crossed into that layer.

### 2.8 Architectural Integrity (Engineering, weight 3) — **60**

- **Why this score.** Bounded contexts are real (Coordinator, Decisioning, Persistence sub-namespaces, Application, ArtifactSynthesis, ContextIngestion, Provenance, KnowledgeGraph, AgentRuntime, Host.Composition, Contracts/Contracts.Abstractions). Each is its own project, its own test project. House style (terse C# rules) is consistent.
- **Why not higher.** ~50 projects (`ArchLucid.*`) is heavy for a V1 product and reflects past splits more than active boundaries — `Persistence`, `Persistence.Advisory`, `Persistence.Alerts`, `Persistence.Coordination`, `Persistence.Integration`, `Persistence.Runtime` is a lot for one team to keep coherent. Two convergent run pipelines (string-coordinator and authority) still exist (`DUAL_PIPELINE_NAVIGATOR.md`). Naming is mid-rename: configuration is `ArchLucid*` but Terraform addresses, some RLS object names, and historical SQL filenames still contain `archlucid` (Phase 7.5–7.8 deferred). This is documented honestly but it is real architectural debt.
- **Recommendation.** Schedule a "Persistence consolidation" cut that collapses the `Persistence.*` family back to two projects (`Persistence.Read`, `Persistence.Write`) when a quiet release window opens. Do **not** retire the dual-pipeline navigator until one path is provably dead.

### 2.9 Executive Value Visibility (Commercial, weight 4) — **70**

- **Why this score.** `EXECUTIVE_SPONSOR_BRIEF.md` is excellent: short, honest, names what *not* to over-claim. The `PILOT_ROI_MODEL.md` companion is concrete. `START_HERE.md` and the persona table in the README route an exec to the right entrypoint quickly. SLA summary, trust center, SOC2 roadmap exist as separate docs.
- **Why not higher.** No outward-facing one-pager (PDF / website hero), and no "executive view" inside the operator UI that a sponsor can be shown live. An executive cannot see ArchLucid's own metrics about itself in a single dashboard route today.
- **Recommendation.** Add a `/executive` route in the operator UI that renders a fixed scorecard from the Pilot ROI Model populated from real metrics for the current tenant, and a screenshot in `docs/go-to-market/SCREENSHOT_GALLERY.md`.

### 2.10 Correctness (Engineering, weight 4) — **72**

- **Why this score.** 711 C# test files, 360 UI test files, mutation testing config (`stryker-config.*.json`), contract tests (`ArchLucid.Contracts.Tests`, `ArchLucid.Architecture.Tests`), explicit `CORRECTNESS_QUALITY_ASSESSMENT_2026_04_15.md` discipline, a coverage gap analysis, comparison-replay verify mode that returns 422 on drift instead of silently passing.
- **Why not higher.** Heavy reliance on the simulator path means "correctness on real LLM outputs" is bounded by the agent quality gate (`archlucid_agent_output_quality_gate_total`) and the faithfulness fallback, both of which are heuristic. No published differential-test suite against past committed manifests to detect regressions in deterministic outputs across release cuts.
- **Recommendation.** Add a small "golden manifest snapshot" suite: commit a handful of canonical request → committed-manifest pairs and assert byte-stable outputs across releases (with a controlled allow-drift channel for explanations).

### 2.11 Trustworthiness (Enterprise, weight 3) — **65**

- **Why this score.** Honest scoping is the strongest trust signal here — `EXECUTIVE_SPONSOR_BRIEF.md §10` ("Limits of AI explanations") explicitly says LLM paragraphs are **decision support**, not legal attestations. Append-only audit, RLS, prompt redaction, and citation chips back this up technically.
- **Why not higher.** No SOC 2 (`SOC2_ROADMAP.md` is a roadmap), no published pen-test summary (template exists in `docs/security/PEN_TEST_REDACTED_SUMMARY_TEMPLATE.md`), no third-party security audit attached, no PGP key for `security@archlucid.dev`. For governance/audit/security buyers, those absences are the trust gap.
- **Recommendation.** Commission and publish one external pen-test redacted summary using the template, and complete a SOC 2 Type 1 readiness assessment. These are weeks-of-work, not months.

### 2.12 Compliance Readiness (Enterprise, weight 2) — **50**

- **Why this score.** DPA template, subprocessors register, tenant-isolation doc, RLS, audit retention policy, append-only DENY DDL on `dbo.AuditEvents` to `ArchLucidApp` role.
- **Why not higher.** No certifications. RLS object names still contain legacy `archlucid` strings until a follow-up migration. SOC 2 is a roadmap. No HIPAA / PCI / FedRAMP statements (unsurprising for V1).
- **Recommendation.** Same as 2.11 — pair the SOC 2 push with a public Trust Center page that lists what is in place vs in flight, mirroring the structure already in `docs/go-to-market/TRUST_CENTER.md`.

### 2.13 Interoperability (Enterprise, weight 2) — **55**

- **Why this score.** OpenAPI, AsyncAPI 2.6, CloudEvents envelope, JSON public contracts (`docs/JSON_PUBLIC_CONTRACTS.md`), event catalog, integration event schemas, contract drift CI gate (`OPENAPI_CONTRACT_DRIFT.md`).
- **Why not higher.** Spec-rich but connector-poor (see Workflow Embeddedness). No published SDK in any language other than the in-repo `ArchLucid.Api.Client`. No published Postman collection.
- **Recommendation.** Generate a TypeScript SDK from the OpenAPI spec in CI and publish it as a private GitHub package; add a Postman collection to `docs/contracts/`.

### 2.14 Maintainability (Engineering, weight 2) — **58**

- **Why this score.** House style is enforced (terse rules, primary constructors, expression-bodied members, `is null` patterns), `Directory.Packages.props` provides CPM, ADRs exist, repo hygiene doc, formatting doc.
- **Why not higher.** 50 projects + 185 docs + 4 UI shaping surfaces + dual pipelines is a lot of cognitive surface for a small team to keep aligned. Several `QUALITY_ASSESSMENT*` and `CURSOR_PROMPTS_*` files overlap and risk drift; that itself is maintenance overhead.
- **Recommendation.** Archive prior quality-assessment and cursor-prompt files under `docs/archive/` once superseded; keep only the active document and its index.

### 2.15 Traceability (Enterprise, weight 3) — **72**

- **Why this score.** Correlation IDs end-to-end, decision traces persisted, manifest hashing, authority chain replay, `IX_AuditEvents_CorrelationId` and `IX_AuditEvents_RunId_OccurredUtc` filtered indexes, `archlucid_data_consistency_orphans_detected_total`.
- **Why not higher.** Some baseline mutation paths still log-only (do not write `dbo.AuditEvents`) per `AUDIT_COVERAGE_MATRIX.md`. Audit search keyset uses `OccurredUtc` only — tie-breaks on identical timestamps are a known limitation.
- **Recommendation.** Add `EventId` as a tie-break in audit keyset cursors and dual-write the remaining baseline mutations into `dbo.AuditEvents`.

### 2.16 Procurement Readiness (Enterprise, weight 2) — **60**

- **Why this score.** Order form template, DPA template, single-source pricing with CI guard, marketplace SaaS offer, subprocessors register, SLA summary, incident communications policy.
- **Why not higher.** No actual Marketplace listing live, no published certifications, the `−15%` reference discount is still a placeholder until first published reference customer.
- **Recommendation.** List on Azure Marketplace as a private offer with one design partner first.

### 2.17 Decision Velocity (Commercial, weight 2) — **62**

- **Why this score.** A buyer can be quoted from `PRICING_PHILOSOPHY.md` § 3 in one call. Order form template is one file.
- **Why not higher.** No self-serve "credit-card to live tenant" path is wired (see Adoption Friction). Enterprise contracts will still take legal review.
- **Recommendation.** Pilot a true credit-card self-serve Team-tier flow even if it caps at one workspace and 20 runs; that converts decision velocity to days, not weeks.

### 2.18 Security (Engineering, weight 3) — **75**

- **Why this score.** Entra ID + JWT, optional API key, RLS, prompt redaction, gitleaks in CI, CodeQL, Trivy, SBOM (CycloneDX), CSP/HSTS/X-Frame headers, fixed-time API key compare, no SMB/445 public exposure, private endpoints in Terraform.
- **Why not higher.** No external pen-test summary published, PGP key for security mailbox not provisioned, mid-rename RLS object names still contain `archlucid`, the placeholder check for API keys is startup-only (no runtime rotation guidance documented in one place).
- **Recommendation.** Commission an external pen-test on a staging deploy and publish a redacted summary; provision the PGP key.

### 2.19 Commercial Packaging Readiness (Commercial, weight 2) — **65**

- **Why this score.** Three-layer model named, priced, mapped to UI surfaces, mapped to controller policies, with a contributor drift guard.
- **Why not higher.** Layers are still **operational** boundaries today, not **entitlement** boundaries — the README makes that explicit. A buyer who pays Team cannot be technically prevented from clicking into Enterprise UI surfaces; they would just get 401/403 from API. That is fine as honesty but limits a clean upsell motion.
- **Recommendation.** Add a feature-flag/entitlement gate keyed by tenant tier in `appsettings` so demos of "what Enterprise unlocks" are clean.

### 2.20 Reliability (Engineering, weight 2) — **68**

- **Why this score.** Circuit breaker (`archlucid_circuit_breaker_*`), LLM retry, degraded mode doc, RTO/RPO targets, k6 baseline, resilience configuration, SLO Prometheus rules.
- **Why not higher.** SLOs are documented but not externally committed; no chaos drill record beyond the `CHAOS_TESTING.md` design.
- **Recommendation.** Run one scheduled chaos drill per quarter and publish the runbook output to `docs/runbooks/`.

### 2.21 Cognitive Load (Engineering, weight 1) — **38** *(very low score, but only weight 1)*

- **Why this score.** As noted in 2.7 and 2.14: 50 projects, 185 docs, 4 UI shaping surfaces, dual run pipelines, 3 auth modes, 3 product layers, ~78 audit event constants, ~93 audit event constants in Core (CI-anchored). Even contributor onboarding requires reading several maintenance maps.
- **Why not higher (i.e. why score this low?).** A new contributor cannot make a safe change touching governance UI without learning `nav-config.ts` + `nav-shell-visibility.ts` + `current-principal.ts` + `layer-guidance.ts` + `enterprise-mutation-capability.ts` + the matching API policies, plus four Vitest regression suites. That is genuinely high cognitive cost for a small product.
- **Recommendation.** Treat this as a real V1.1 burndown: collapse `Persistence.*` projects, archive overlapping quality docs, retire the dual-pipeline navigator once a path is dead, and produce a single contributor "if you change X, edit these N files" quick-reference (one page, not the current paragraph in `PRODUCT_PACKAGING.md`).

### 2.22 Policy and Governance Alignment (Enterprise, weight 2) — **70**

- Pre-commit gate, approval workflow with segregation of duties, SLA tracking with webhook escalation, policy packs with scope assignments, governance dashboard. **Recommendation:** add policy-pack signing so a buyer can prove which pack version evaluated their commit.

### 2.23 Data Consistency (Engineering, weight 2) — **70**

- Orphan probe, data consistency matrix, append-only audit, single DDL discipline, schema migrations. **Recommendation:** add a nightly job that emits a data-consistency report artifact per environment.

### 2.24 Explainability (Engineering, weight 2) — **70**

- Citation chips, faithfulness ratio, decision traces, agent trace forensics, deterministic fallback when faithfulness too low. **Recommendation:** surface the faithfulness number visibly in the UI alongside the explanation, not only as a backend metric.

### 2.25 AI/Agent Readiness (Engineering, weight 2) — **70**

- Authority pipeline, agent runtime, simulator vs real, prompt redaction, output evaluation recorder, quality gate. **Recommendation:** add a regression harness that pins agent prompts and asserts structural-completeness ratio above threshold across releases.

### 2.26 Auditability (Enterprise, weight 2) — **75**

- 78+ typed events, append-only DENY DDL, CSV export, indexed search. **Recommendation:** publish a sample audit export in `docs/security/` for procurement reviews.

### 2.27 Stickiness (Commercial, weight 1) — **50**

- Manifest history and audit trail create some lock-in, but no deep integration moat or proprietary data network effect yet. **Recommendation:** ship the GitHub Action / Confluence integration first (see Workflow Embeddedness) — once the team's PR flow depends on it, leaving is expensive.

### 2.28 Azure Compatibility / SaaS Deployment Readiness (Engineering, weight 2) — **75**

- Container Apps, Front Door, Entra, OpenAI, Key Vault, Service Bus, SQL failover, OTel collector, Logic Apps, monitoring, storage — all in Terraform stacks. Marketplace offer doc. **Recommendation:** publish one stamped reference deployment of `infra/terraform-container-apps` to a public lab subscription for buyer-facing demos.

### 2.29–2.46 Lower-headroom qualities (grouped)

| Quality | Score | Why | One-line rec |
|---|---|---|---|
| Cost-Effectiveness (1) | 62 | Per-tenant cost model, capacity playbook, consumption_budget.tf, pilot profile | Wire `consumption_budget.tf` budgets into Slack alerts. |
| Customer Self-Sufficiency (1) | 65 | Many docs, troubleshooting, support bundle, doctor | Add an in-product `?` that opens the relevant runbook. |
| Manageability (1) | 65 | Many config keys, governance config-driven | Generate a `docs/CONFIG_REFERENCE.md` from `appsettings.json`. |
| Extensibility (1) | 65 | Finding engine plugin doc, comparison-type how-to | Ship one external plugin sample. |
| Evolvability (1) | 65 | DbUp migrations, BREAKING_CHANGES.md, ADRs, rename phases | Re-baseline ADR index after Phase 7 closes. |
| Template/Accelerator Richness (1) | 55 | DPA, order form, demo, narrative templates; light on architecture starter packs | Publish 3 canonical architecture briefs (greenfield web, brownfield migration, regulated workload). |
| Availability (1) | 55 | SQL failover module exists; not promised at V1 | Document one customer-side runbook for failover. |
| Performance (1) | 55 | k6 baseline, cold-start trimming doc | Publish a perf budget per endpoint in `docs/API_SLOS.md`. |
| Scalability (1) | 55 | Container Apps + jobs + secondary region | Publish one scale test result against the secondary region module. |
| Accessibility (1) | 70 | WCAG 2.1 AA target, axe gates, jsx-a11y, live regions | Publish the axe pass/fail summary in the release notes. |
| Change Impact Clarity (1) | 70 | Compare runs, replay, manifest deltas | Surface the structured manifest delta count on the run-detail page. |
| Deployability (1) | 70 | Compose profiles, Terraform stacks, demo-quickstart | Add a one-click Bicep alternative for buyers who do not use Terraform. |
| Testability (1) | 72 | TestSupport, ApiFactory, mutation testing | Lock the mutation score floor in CI. |
| Supportability (1) | 75 | doctor, support-bundle, correlation IDs | Encrypt support-bundle by default and document key handling. |
| Modularity (1) | 75 | ~50 projects, contracts split | Start the consolidation noted in 2.8. |
| Azure Ecosystem Fit (1) | 75 | Service Bus, Entra, OpenAI, App Config future | Adopt App Configuration once Phase 7 settles. |
| Observability (1) | 78 | OTel, Grafana dashboards, Prometheus alerts, business KPIs | Publish the dashboards as a Grafana Cloud public preview. |
| Documentation (1) | 80 | 185 docs, persona-routed entrypoints, change control | Archive overlapping QA / Cursor prompt files under `docs/archive/`. |

---

## 3. Top 10 most important weaknesses (ordered by weighted impact)

1. **No published reference customer** — caps Marketability, Differentiability, Stickiness, and the discount stack.
2. **Self-serve trial is designed but not hosted** — caps Adoption Friction and Decision Velocity despite a passing CI spec.
3. **No first-party workflow integration** (Confluence / ServiceNow / Jira / GitHub PR check) — caps Workflow Embeddedness, the single biggest Enterprise headroom.
4. **Time-to-value tops out at "committed manifest"** rather than "reviewer signed off" — caps Time-to-Value and Stickiness.
5. **Cognitive surface is large** (50 projects, 4 UI shaping surfaces, dual run pipelines, 3 auth modes, 3 product layers) — caps Usability, Maintainability, Cognitive Load, Architectural Integrity.
6. **No external pen-test or SOC 2 attestation published** — caps Trustworthiness, Compliance Readiness, Procurement Readiness.
7. **Phase 7.5–7.8 rename leaks** (Terraform addresses, RLS object names, repo / Entra rename) — adds adoption-time friction and minor architectural-integrity drag.
8. **ROI claims are modeled, not measured on a real pilot** — caps Proof-of-ROI Readiness.
9. **No native entitlement / packaging gate** — Enterprise UI is reachable from Team tier and only stopped by API 401/403; clean upsell story is harder.
10. **Spec-rich but connector-poor** (OpenAPI, AsyncAPI exist; SDKs and connectors do not) — caps Interoperability and indirectly Workflow Embeddedness.

---

## 4. Top 5 monetization blockers

1. **No published reference customer** (blocks the `−15%` reference re-rate trigger and removes social proof).
2. **No live self-serve trial URL** (no PLG funnel; only seller-led demo).
3. **No first-party integration** the buyer's existing review process already touches (PR check / Confluence publish).
4. **No SOC 2 / pen-test publication** — blocks regulated buyers entirely and slows mid-market procurement.
5. **No technical entitlement enforcement** between Team / Professional / Enterprise tiers — makes upsell rely on social contract, not capability gating.

---

## 5. Top 5 enterprise adoption blockers

1. **Workflow embeddedness gap** — reviewers must leave their existing tool to use ArchLucid.
2. **Compliance attestation gap** — no SOC 2 / ISO / pen-test publication.
3. **Operational complexity for first install** — three auth modes, multiple Terraform stacks, optional Azure OpenAI, Phase 7 rename leaks.
4. **Concept count overhead** — operators must learn manifest vs golden manifest vs decision trace vs finding vs advisory vs alert vs comparison vs replay vs authority chain before they are productive.
5. **No native role-mapping helper** for the customer's IdP — JWT works, but no guided mapping doc per IdP.

---

## 6. Top 5 engineering risks

1. **Persistence and pipeline fan-out** — six `Persistence.*` projects and two run pipelines are a refactor liability if either path needs deep change.
2. **Heuristic explainability fallback** — `archlucid_explanation_aggregate_faithfulness_fallback_total` is a real gate, but a regression in faithfulness scoring can silently degrade the explanation product.
3. **Audit channel duality** — baseline mutations log-only vs durable `dbo.AuditEvents`; remaining gaps mean audit completeness is environment-dependent.
4. **Phase 7 rename half-state** — historical `archiforge` strings in Terraform addresses, RLS objects, and repo metadata are fine today but expensive if any are exposed in customer-visible names later.
5. **Operator-UI shaping seam complexity** — four shaping surfaces with five Vitest regression suites means a single careless change can break Enterprise UI shaping in subtle, hard-to-test-by-hand ways.

---

## 7. Most Important Truth

> **ArchLucid is engineered ahead of where it is sold.** The product can already commit a manifest, prove the path with scripted drills, and back the result with append-only audit, decision traces, and faithfulness measurement — that is rare for a V1. What is missing is **external proof** (one published reference customer, one pen-test, one workflow integration the buyer already uses) and **first-mile friction reduction** (one hosted trial URL, one `archlucid pilot up`, one entitlement gate). Until those land, the score will sit in the mid-60s no matter how much more engineering ships, because every remaining headroom point is a commercial / adoption point, not a technical one.

---

## 8. Six best improvements (Cursor prompts)

These are the six highest weighted-headroom improvements that can be implemented inside Cursor. Each prompt is self-contained, references existing files in the repo, and respects the always-applied workspace rules (Azure-native, IaC, terse C#, no public SMB, etc.).

### 8.1 Prompt — Marketability: convert one design partner to a published reference

```
Goal: move the reference-customer table in
docs/go-to-market/reference-customers/README.md from a single
"EXAMPLE_DESIGN_PARTNER" Placeholder row to one Published row, and
flip scripts/ci/check_reference_customer_status.py from warn-only to
merge-blocking when at least one Published row exists.

Steps:
1. Add a new Markdown file
   docs/go-to-market/reference-customers/<customer-slug>_CASE_STUDY.md
   based on EXAMPLE_DESIGN_PARTNER_CASE_STUDY.md, with placeholders
   left as <<...>> for me to fill in.
2. Add a corresponding row in the table in
   docs/go-to-market/reference-customers/README.md with
   Status: Customer review.
3. Update scripts/ci/check_reference_customer_status.py so that when
   at least one row has Status: Published the script exits non-zero
   with a clear message; warn-only behaviour stays for Customer review
   and Drafting rows.
4. Add a CI workflow change in .github/workflows/ that removes
   continue-on-error for the reference-customer guard step once
   Published is detected, gated behind a new boolean input
   `enforce_reference_guard` defaulting to false so we can flip it on
   in one PR when the customer publishes.
5. Update docs/CHANGELOG.md with a one-line entry under the current
   release header.
6. Do not invent a real customer name; leave <<CUSTOMER_NAME>> for me.
Constraints: do not modify pricing files; do not change the discount
stack figures; do not delete the Placeholder row.
```

### 8.2 Prompt — Adoption Friction: ship a single `archlucid pilot up` command

```
Goal: add an `archlucid pilot up` CLI verb that wraps docker compose
profiles plus seed plus a printed "open this URL" message, so a
prospect can go from `git clone` to a running pilot stack in one
command.

Steps:
1. Add a new command class PilotUpCommand under ArchLucid.Cli that
   executes the same logic as scripts/demo-start.ps1 but in C#:
   - docker compose --profile full-stack up -d --build
   - wait for /health/ready on the API (poll up to 90s)
   - call POST /v1.0/demo/seed when Demo:Enabled is true
   - print http URLs for the API, the operator UI, and the Swagger.
2. Reuse existing CLI infrastructure (System.CommandLine setup in
   Program.cs); keep the class in its own file per house style; use
   primary constructor and is null guards.
3. Add a unit test PilotUpCommandTests under ArchLucid.Cli.Tests using
   the existing test infrastructure; mock the docker invocation.
4. Update docs/CLI_USAGE.md and docs/FIRST_30_MINUTES.md to point at
   the new verb; keep the existing scripts as fallbacks.
5. Add a row to README.md "Pick your persona" table for the new
   one-liner.
Constraints: must not require Azure OpenAI keys (simulator only);
must not expose SMB / port 445; no new dependencies beyond the docker
CLI; reuse ArchLucid.Cli HTTP client wiring.
```

### 8.3 Prompt — Time-to-Value: emit a sponsor-ready first-value report

```
Goal: add a CLI verb `archlucid first-value-report <runId>` and a
matching API endpoint GET /v1/pilots/runs/{runId}/first-value-report
that emit a one-page Markdown summary of the first committed run for
a sponsor to read.

Output should include:
- run id, environment, commit timestamp, manifest version
- count of findings by severity
- decision trace summary (top 5 decisions with citation links)
- elapsed wall time from request creation to commit
- baseline-comparison rows from docs/PILOT_ROI_MODEL.md (left blank
  for the sponsor to fill in)
- a footer linking to docs/EXECUTIVE_SPONSOR_BRIEF.md.

Steps:
1. Add an Application service `FirstValueReportBuilder` in
   ArchLucid.Application with one method per data row, each in its
   own file per the project rule.
2. Add a controller endpoint in ArchLucid.Api under existing
   architecture controller area; require ReadAuthority policy.
3. Add a CLI verb that POSTs nothing, just GETs and writes Markdown to
   stdout or to outputs/first-value-{runId}.md when --save is set.
4. Add unit tests in ArchLucid.Application.Tests and a smoke test in
   ArchLucid.Api.Tests.
5. Wire OpenAPI examples; update docs/API_CONTRACTS.md and
   docs/CLI_USAGE.md.
Constraints: no new persistence; this is a read-only projection over
existing manifests / decision traces / audit / metrics; respect RLS
through existing repository pattern; use Dapper, not EF.
```

### 8.4 Prompt — Workflow Embeddedness: GitHub Action for manifest delta PR check

```
Goal: ship one first-party workflow integration so reviewers do not
have to leave their existing tool. Build a GitHub Action that, given
two run ids, posts the structured manifest delta as a PR check
summary and a comment.

Steps:
1. Add a new directory integrations/github-action-manifest-delta/
   with action.yml (composite action), README.md, and a small Node
   script that calls the existing comparison endpoints
   (POST /v1/architecture/run/{runId}/compare equivalents — confirm
   exact route via docs/API_CONTRACTS.md and docs/COMPARISON_REPLAY.md
   before coding).
2. Inputs: api-base-url, api-token, left-run-id, right-run-id.
3. Output: a PR check named "ArchLucid manifest delta" with summary
   markdown showing added/removed/changed rows from the structured
   golden-manifest delta, plus a link back to the operator UI compare
   route.
4. Add docs/integrations/GITHUB_ACTION_MANIFEST_DELTA.md describing
   setup, secret handling, and required ReadAuthority scope.
5. Add a sample workflow .github/workflows/example-manifest-delta.yml
   (runs only on workflow_dispatch) so a buyer can copy-paste it.
Constraints: do not commit secrets; the action must work against a
public API endpoint; honour rate-limiting (use existing /v1
endpoints); reuse the existing OpenAPI client where possible.
```

### 8.5 Prompt — Cognitive Load / Architectural Integrity: collapse Persistence.* fan-out

```
Goal: produce a refactor plan (no code yet) that consolidates the
six Persistence projects (ArchLucid.Persistence,
ArchLucid.Persistence.Advisory, ArchLucid.Persistence.Alerts,
ArchLucid.Persistence.Coordination, ArchLucid.Persistence.Integration,
ArchLucid.Persistence.Runtime) into two: ArchLucid.Persistence.Read
and ArchLucid.Persistence.Write, with stable namespaces for
backwards-compat re-exports.

Deliverables in a new file
docs/PROJECT_CONSOLIDATION_PROPOSAL_PERSISTENCE.md:
1. Inventory of every public type currently exported by the six
   projects (use the .csproj and namespace structure to enumerate).
2. Proposed mapping: which type goes to Read vs Write.
3. Risk analysis: which downstream projects (Application, Api,
   Coordinator, Decisioning, Worker) need reference updates and what
   breaking changes are exposed.
4. Migration sequence: a step-by-step plan that keeps CI green on
   every step (start with a re-export shim project, then move
   types one folder at a time).
5. Test impact on ArchLucid.Persistence.Tests and any downstream
   test projects.
6. Explicit non-goals: do not change DDL, do not rename DbUp scripts,
   do not change RLS object names (those are owned by the rename
   checklist).
Constraints: do not write code in this prompt; produce only the
proposal doc; reference the existing PROJECT_CONSOLIDATION_PROPOSAL.md
and PERSISTENCE_SPLIT.md so the new doc reads as the next chapter.
```

### 8.6 Prompt — Trustworthiness / Compliance: publish a redacted pen-test summary

```
Goal: stand up the publication path for a real external pen-test
redacted summary, so once the test runs we can publish in one PR.

Steps:
1. Create docs/security/pen-test-summaries/ directory with a
   README.md describing the publication discipline (cadence, scope,
   redaction rules) that mirrors
   docs/security/PEN_TEST_REDACTED_SUMMARY_TEMPLATE.md.
2. Copy PEN_TEST_REDACTED_SUMMARY_TEMPLATE.md into
   docs/security/pen-test-summaries/2026-Q2-DRAFT.md with all real
   values left as <<...>> placeholders.
3. Update docs/go-to-market/TRUST_CENTER.md to link to the new
   directory and clearly mark the current state as "redacted summary
   forthcoming, see <<vendor>> SoW".
4. Update SECURITY.md to add the PGP key provisioning note as a TODO
   (do not invent a key).
5. Update docs/CHANGELOG.md with one line under the current release
   header.
Constraints: do not invent a vendor name; do not invent finding
counts; placeholders only; do not move existing security docs.
```

---

## 9. Pending questions for the user

**Consolidated list (living document):** [`docs/PENDING_QUESTIONS.md`](PENDING_QUESTIONS.md) — owner decisions, execution status for the six §8 prompts, and remaining open items. The numbered list below is the **2026-04-20 assessment snapshot**; prefer `PENDING_QUESTIONS.md` when answering *what is still open?*

These are decisions or facts I cannot make on my own. When you next ask me what is pending, I will be ready with this exact list.

1. **Reference customer name and tier.** Which design partner are you authorized to publish first, and which pricing tier do they sit in? Required to fill in `<<CUSTOMER_NAME>>`, `<<TIER>>`, and the design-partner term in the case study under §8.1.
2. **Hosted trial URL ownership.** Who owns the public DNS / Front Door target for `trial.archlucid.dev` (or equivalent), and which subscription should it deploy into? Required before binding the merge-blocking trial spec to a real URL.
3. **Pen-test vendor selection.** Which vendor and SoW are you using for the external pen-test the redacted summary in §8.6 will publish? Required to retire the `<<vendor>>` placeholder.
4. **SOC 2 path.** Are we pursuing a SOC 2 Type 1 readiness assessment first or going direct to Type 2, and with which auditor? Affects how the Trust Center and `SOC2_ROADMAP.md` are sequenced.
5. **First first-party integration target.** GitHub PR check (proposed in §8.4) vs Confluence publish vs ServiceNow change record — which buyer's workflow do we anchor on?
6. **Entitlement gate authority.** Are we ready to add a tier-keyed feature flag (Team / Professional / Enterprise) so Enterprise UI is hidden for Team tenants, or do we keep the V1 stance that the layer model is operational only?
7. **Phase 7.5–7.8 timing.** Is there a deploy window opening for Terraform `state mv`, repo rename, and Entra app rename? Some adoption-friction recommendations soften considerably once that closes.
8. **Marketplace listing scope.** Public listing vs private offer for the first published reference customer? Affects pricing-display sequencing.
9. **Persistence consolidation priority.** Should the consolidation proposal in §8.5 go on the V1.1 roadmap, or is it a "next quiet release" item with no committed date?
10. **Founder time on landing page.** The landing page / one-page datasheet (Marketability rec) is the kind of asset that benefits from your voice. Do you want to draft it and let me convert to Markdown + screenshots, or the other way around?

---

## 10. Method notes

- Weights as given total exactly 100, so `Σ(score × weight) / Σ(weight)` is reported as a percent rounded to two decimal places.
- Scores are anchored to what is in the repo today. Where the repo says "deferred" or "intentionally not promised" (e.g. multi-region active/active, IDE integration, full audit parity for some baseline paths, Phase 7.5–7.8 rename items), the score reflects that the gap exists today, not that the intention is missing.
- The order of presentation is **weighted improvement headroom**, not score. A high-weight quality with a mid score (Marketability 8 × 62) appears before a low-weight quality with a very low score (Cognitive Load 1 × 38) because it moves the headline number more.
- Independent of any prior `QUALITY_ASSESSMENT_*.md` file in the repo.

