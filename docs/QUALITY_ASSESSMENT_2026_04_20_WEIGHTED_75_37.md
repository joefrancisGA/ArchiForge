# ArchLucid Solution Quality Assessment — 75.37% Weighted (2026-04-20)

> **Independent assessment.** This document was scored from the codebase as it exists today, without reference to prior `QUALITY_ASSESSMENT_*` or `MARKETABILITY_ASSESSMENT_*` files in `docs/`. Where my conclusions overlap with earlier assessments, that is convergent evidence — not citation.

> **Method.** 31 quality attributes, each scored **1–100**, each weighted as the user supplied (sum of weights = **150**). Weighted score = `Σ(weight × score) / (150 × 100)`. Improvement order = `weight × (100 − score)` (largest gap first), so the highest-leverage work shows up at the top.

---

## 1. Headline numbers

| Metric | Value |
|---|---|
| **Weighted score** | **75.37 / 100** |
| Sum of `weight × score` | 11,306 |
| Sum of `weight × 100` | 15,000 |
| Unweighted average score | 76.0 / 100 |
| Number of attributes | 31 |
| Total weight | 150 |

---

## 2. Score table — sorted by weighted improvement gap (highest-leverage first)

| # | Quality | Weight | Score (1–100) | Weighted gap = w × (100 − s) |
|---|---|---:|---:|---:|
| 1 | **Marketability** | 16 | 52 | **768** |
| 2 | **Usability** | 8 | 72 | 224 |
| 3 | **Correctness** | 8 | 78 | 176 |
| 4 | **Architectural Integrity** | 7 | 75 | 175 |
| 5 | **Cognitive Load** | 4 | 58 | 168 |
| 6 | **Supportability** | 8 | 80 | 160 |
| 7 | **Evolvability** | 6 | 74 | 156 |
| 8 | **Azure Compatibility & SaaS Deployment Readiness** | 8 | 82 | 144 |
| 9 | **Azure Developer Technology Optimized** | 8 | 82 | 144 |
| 10 | **Security** | 6 | 78 | 132 |
| 11 | **Explainability** | 6 | 80 | 120 |
| 12 | **Policy & Governance Alignment** | 5 | 76 | 120 |
| 13 | **Traceability** | 7 | 84 | 112 |
| 14 | **Reliability** | 5 | 78 | 110 |
| 15 | **Maintainability** | 4 | 74 | 104 |
| 16 | **Data Consistency** | 5 | 82 | 90 |
| 17 | **Auditability** | 5 | 82 | 90 |
| 18 | **AI / Agent Readiness** | 4 | 80 | 80 |
| 19 | **Interoperability** | 3 | 76 | 72 |
| 20 | **Scalability** | 3 | 76 | 72 |
| 21 | **Observability** | 5 | 86 | 70 |
| 22 | **Testability** | 3 | 80 | 60 |
| 23 | **Modularity** | 3 | 82 | 54 |
| 24 | **Performance** | 2 | 74 | 52 |
| 25 | **Availability** | 2 | 76 | 48 |
| 26 | **Cost-Effectiveness** | 2 | 76 | 48 |
| 27 | **Manageability** | 2 | 78 | 44 |
| 28 | **Accessibility** | 1 | 65 | 35 |
| 29 | **Deployability** | 2 | 84 | 32 |
| 30 | **Extensibility** | 1 | 78 | 22 |
| 31 | **Documentation** | 1 | 88 | 12 |
| **Σ** | | **150** | | **11,306** *(sum of `w × s`)* |

---

## 3. Per-quality assessment (ordered most-improvement-needed first)

### 3.1 Marketability — 52 / 100 (weight 16, gap 768)

**Justification.** Strong rails: `archlucid-ui/src/app/(marketing)/{welcome,signup,signup/verify}/page.tsx` exist; `pricing.json` is a single-source-of-truth pricing surface (with a CI guard `scripts/ci/check_pricing_single_source.py`); `EXECUTIVE_SPONSOR_BRIEF.md`, `PILOT_ROI_MODEL.md`, `OPERATOR_DECISION_GUIDE.md`, `go-to-market/POSITIONING.md`, `ORDER_FORM_TEMPLATE.md`, an Azure Marketplace SaaS offer doc with fulfillment v2 and `GaEnabled=true` default, trial enforcement (HTTP 402 + `problem+json`), and a competitive landscape doc are all in-repo.

The product does **not show evidence of a live commercial motion**:

- No public marketing host wired in `infra/terraform-edge/frontdoor-marketing.tf` to a real domain in any tracked variable file (the routes file exists but I see no `archlucid.com`-class hostname binding).
- No Stripe (or equivalent) billing-bridge GA artifact — Marketplace is the only fulfillment path and the marketing page itself sits behind no CDN-fronted brand domain in tracked config.
- The reference-customer page in `docs/go-to-market/reference-customers/README.md` is acknowledged as "one placeholder seed" gated by a non-blocking warn.
- Repo history previously contained a leaked test token (per the `MARKETABILITY_ASSESSMENT_2026_04_18.md` honesty section) — that's a brand and procurement-due-diligence risk on its own.

**Tradeoffs.** Building the rails first is the correct sequence (you cannot sell what you cannot deliver), but Marketability is **weight 16** — by the user's own weighting, this is the single biggest lever. Until a sponsor can hit a live URL, talk to a reference, and complete a paid checkout, every other quality is un-monetised.

**Improvements.**
1. Stand up the `archlucid-ui` marketing surface on a real brand domain, fronted by `infra/terraform-edge/frontdoor-marketing.tf`, with WAF + custom domain + cert binding committed to `infra/`.
2. Flip a non-Marketplace fulfilment path (Stripe Checkout or equivalent) from "in progress" to **GA**, with a dedicated webhook handler equivalent to the Marketplace one and a runbook in `docs/runbooks/`.
3. Publish a single named reference customer (or named design partner with permission) to satisfy the existing `−15%` reference-discount gate in `PRICING_PHILOSOPHY.md §5.4`.
4. Run `gitleaks --redact` over **history** (BFG / `git filter-repo`) to evict the legacy test token; rotate any provider-side credential it touched.

---

### 3.2 Usability — 72 / 100 (weight 8, gap 224)

**Justification.** UI evidence is good — `(operator)/runs/new/NewRunWizardClient.tsx`, `OnboardWizardClient.tsx`, `CommandPalette.tsx`, `MobileNavDrawer.tsx`, `LayerHeader.tsx`, `OperatorFirstRunWorkflowPanel.tsx`, plus 4 Day-1 personas (`day-one-{developer,sre,security}.md`), `OPERATOR_QUICKSTART.md`, `START_HERE.md`. UI tests cover authority-shaped layout regressions and seam tests.

What hurts the score:

- **Three product layers** (Core Pilot / Advanced Analysis / Enterprise Controls) plus a layer-shaping seam (`nav-config.ts`, `nav-shell-visibility.ts`, `LayerHeader`, `enterprise-mutation-capability.ts`) is conceptually dense for a first-time operator. The README itself spends two screens explaining how the seam works before the user runs a command.
- The "first 30 minutes" experience requires choosing between `dev up`, `demo-start.ps1`, `release-smoke`, `docker compose --profile full-stack`, plus four Day-1 docs by persona. Decision fatigue.
- `archlucid-ui/docs/COMPONENT_REFERENCE.md` exists, but the **operator** has no equivalent "what does each page do, in one screen" reference — only per-page client components.

**Tradeoffs.** The layer model is real and commercially load-bearing (it's how packaging is sold). Hiding it would make the buyer story worse. The fix is **path collapse**, not layer collapse.

**Improvements.**
1. Add a single `docs/FIRST_30_MINUTES.md` that names the **one** command per persona to run, with one screenshot per outcome — link to it from the very top of the README.
2. Add a "what is on this page in one sentence" overlay component (`PageHelp.tsx`) that reads from a small `page-help.json`, so usability help is one click away, not a doc-search away.
3. In `NewRunWizardClient.tsx`, ship a default sample-run preset that always works (already partially present in `NewRunWizardClient.sample-run.test.tsx`) — remove the "what do I type here" friction.

---

### 3.3 Correctness — 78 / 100 (weight 8, gap 176)

**Justification.** Strong: 20 unit/integration test projects (`ArchLucid.*.Tests`), `ArchLucid.TestSupport`, Stryker mutation testing (PR + scheduled), Schemathesis OpenAPI fuzzing (PR-light + scheduled-full), CodeQL with custom log-sanitizer model pack, k6 smoke + per-tenant burst soak, Simmy chaos, ZAP baseline strict scheduled, JSON/check constraint migrations (`095_CheckConstraints_StatusDomains_Batch.sql`, `096_CheckJson_CorePayloadColumns.sql`), explicit `JSON_FALLBACK_AUDIT.md` discipline, RowVersion optimistic concurrency (`039_RowVersion_OptimisticConcurrency.sql`), data-consistency orphan probe (`099_DataConsistencyQuarantine.sql`).

What suppresses the score:

- Schemathesis **stateful** is **scheduled-only**, not merge-blocking — drift between OpenAPI and behaviour can slip through PR.
- `ArchLucid.Api.Client` exists (and is republished), but I see no PR-time **client-replay** suite that runs golden requests through the typed client against the API container. Schemathesis covers shape; replay would cover *intent*.
- The 100-script migration history with two files numbered `096_*` (`096_CheckJson_CorePayloadColumns.sql` and `096_RlsTenantIdOnlyTables.sql`) is a correctness-of-DDL hazard if DbUp ordering is not strictly lexical.
- `ContractTest_Coverage_Gap_Analysis.md` and `COVERAGE_GAP_ANALYSIS_RECENT.md` both exist — i.e. the team itself knows there are uncovered seams.

**Tradeoffs.** Stateful Schemathesis is expensive (minutes), so making it merge-blocking trades CI latency for correctness — but on weight 8 the trade is worth it.

**Improvements.**
1. Promote Schemathesis stateful from `schemathesis-scheduled.yml` into the PR pipeline with a smaller seed set; keep the full nightly run.
2. Renumber the duplicate `096_*` migrations to `096_*` and `100_*` (or similar) and add a CI guard `scripts/ci/check_migration_numbering.py`.
3. Add an `ArchLucid.Api.Client.ContractReplay.Tests` project that drives a fixed list of operator scenarios end-to-end via the typed client against a containerised API.

---

### 3.4 Architectural Integrity — 75 / 100 (weight 7, gap 175)

**Justification.** Bounded contexts are visible (`Contracts`, `Contracts.Abstractions`, `Application`, `Coordinator`, `AgentRuntime`, `ContextIngestion`, `KnowledgeGraph`, `Decisioning`, `Provenance`, `Retrieval`, `ArtifactSynthesis`, `Persistence` + four sub-projects `Persistence.{Alerts,Coordination,Integration,Runtime,Advisory}`, `Host.Core`, `Host.Composition`). 21 ADRs document architectural decisions including the convergence of two run tables (ADR 0012), dual-persistence (ADR 0002), URL-path API versioning (ADR 0006), and "Azure primary platform permanent" (ADR 0020).

What hurts:

- **No mechanical guard** that the layering documented in ADRs holds. There is `docs/ARCHITECTURE_CONSTRAINTS.md` but no `NetArchTest`/`ArchUnitNET` test project I can find that fails when, e.g., `ArchLucid.Application` references `ArchLucid.Persistence` directly, or when a controller bypasses `Application` to talk to a SQL repository.
- `PROJECT_CONSOLIDATION_PROPOSAL.md` is open — i.e. the team has noticed that the project graph has accumulated overlap that is not yet reconciled.
- `ArchLucid.Persistence.Alerts` carries methods named `AlertIntegrationEventPublishing` (taking raw `ILogger` rather than `ILogger<T>`) — small but tells me the boundary between "Persistence" and "Integration outbox" is fuzzy in places.

**Tradeoffs.** Mechanical architecture tests are noisy on day one and tend to grow exemption lists. They pay back over multi-year maintenance — exactly the timescale ArchLucid is designed for.

**Improvements.**
1. Add `ArchLucid.Architecture.Tests` boundary tests (NetArchTest) pinning: no project depends on `Persistence*` except `Host.*`, `Application`, and the `Persistence.*` siblings; controllers depend only on `Application`.
2. Land one round of the consolidation proposal: pick the two smallest sibling projects with the heaviest mutual coupling and merge them, with an ADR.

---

### 3.5 Cognitive Load — 58 / 100 (weight 4, gap 168)

**Justification.** 191 markdown files in `docs/`, 31 runbooks, 21 ADRs, 50+ csproj, three product layers, four onboarding personas, two parallel correctness assessments, three parallel marketability assessments, three sets of "Cursor prompts" docs (`CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_20*.md`, `CURSOR_PROMPTS_SAAS_IMPROVEMENTS_2_TO_6.md`, etc.). The codebase **rewards** a reader who already knows where to look and **punishes** a reader who doesn't.

What helps: `START_HERE.md`, `ARCHITECTURE_INDEX.md`, `CODE_MAP.md`, `PROJECT_MAP.md`, `Navigation.mdc` rule, day-one persona docs.

**Tradeoffs.** Aggressive culling will be politically expensive (each doc has an author and a use case). The cheaper path is **demoting** rather than deleting.

**Improvements.**
1. Move all `*_ASSESSMENT_*` files older than the current quarter into `docs/archive/quality/`.
2. Replace the README "Key documentation" table with a 5-row "If you are X, read Y" table that links into the existing index.
3. Add a one-paragraph "scope" header to every `docs/*.md` file, generated/checked by a script.

---

### 3.6 Supportability — 80 / 100 (weight 8, gap 160)

**Justification.** Excellent: 31 runbooks, `archlucid doctor`, `archlucid support-bundle --zip`, `release-smoke.{cmd,ps1}`, `run-readiness-check.{cmd,ps1}`, `X-Correlation-ID` propagation, persisted `dbo.Runs.OtelTraceId`, `archlucid trace <runId>` CLI, Application Insights wiring, OWASP ZAP gate, sanitized logger error extensions (added in this session per git status), CodeQL custom log-sanitizer model pack.

What hurts:

- The `support-bundle` is a CLI command — not exposed via an authenticated `/v1/support/bundle` API for SREs without shell access.
- 31 runbooks is a lot to keep current; I see no automated freshness check (e.g., a CI lint that fails when a runbook's referenced metric or env-var is renamed).

**Improvements.**
1. Add `GET /v1/support/bundle.zip` (Admin-only, audited) that wraps the same code path as the CLI.
2. Add `scripts/ci/check_runbook_freshness.py` that scans for env-var and metric names referenced in `docs/runbooks/*.md` and fails if any of them no longer appears in source.

---

### 3.7 Evolvability — 74 / 100 (weight 6, gap 156)

**Justification.** API versioning ADR (URL-path; ADR 0013/0006), `OPENAPI_CONTRACT_DRIFT.md`, JSON schema versioning, integration event schema registry, dual-pipeline navigator doc, strangler plan ADR for the Coordinator pipeline (ADR 0021), `COMMERCIAL_BOUNDARY_HARDENING_SEQUENCE.md`, `FUTURE_PACKAGING_ENFORCEMENT.md`, `V1_DEFERRED.md`, `RENAME_DEFERRED_RATIONALE.md` — the team is explicit about deferred work.

What hurts:

- No automated **breaking-change detector** for OpenAPI (e.g., `openapi-diff` in CI).
- No `/v1/...` deprecation header behaviour or `Sunset:` header conventions documented.

**Improvements.**
1. Add an `openapi-diff` step to CI that fails when a removal/rename is detected without a corresponding `BREAKING_CHANGES.md` entry.
2. Document and implement `Sunset:` and `Deprecation:` HTTP headers on any endpoint marked deprecated in the spec.

---

### 3.8 Azure Compatibility & SaaS Deployment Readiness — 82 / 100 (weight 8, gap 144)

**Justification.** Excellent. Terraform across 14 stacks (`terraform`, `terraform-edge`, `terraform-private`, `terraform-container-apps`, `terraform-storage`, `terraform-keyvault`, `terraform-monitoring`, `terraform-openai`, `terraform-servicebus`, `terraform-sql-failover`, `terraform-orchestrator`, `terraform-entra`, `terraform-pilot`, `terraform-logicapps`). Marketplace SaaS offer with fulfillment v2 GA (`GaEnabled=true` since 2026-04-20), MI for `marketplaceapi.microsoft.com/.default`, App Configuration ADR (0017 deferred), Container Apps Jobs (ADR 0018), Logic Apps Standard (ADR 0019), CMK + SQL TDE runbooks, geo-failover drill, secondary-region tf, RLS, Entra trial CIAM (ADR 0015), private endpoints, deny-public guard for SMB/445.

What hurts:

- No first-party landing zone deployment **workflow** (`cd-saas-pilot.yml`) that proves the Terraform actually plans cleanly against a fresh subscription end-to-end. `cd-staging-on-merge.yml` exists but I see no greenfield apply test.
- Front Door config exists for `frontdoor-marketing.tf` but the brand domain binding is missing.

**Improvements.**
1. Add `cd-saas-greenfield.yml` (manual trigger, monthly schedule) that creates a temporary RG, runs `terraform apply` end-to-end, executes `archlucid doctor` against the resulting API, then destroys.
2. Bind the marketing Front Door route to the chosen brand domain in Terraform.

---

### 3.9 Azure Developer Technology Optimized — 82 / 100 (weight 8, gap 144)

**Justification.** .NET 10, ASP.NET Core, Minimal hosting + controllers, OpenTelemetry .NET (with named meter and stable instruments), DbUp, Azure SDKs (`Azure.AI.ContentSafety`, Service Bus, OpenAI, KeyVault), MI everywhere. C# 12 primary constructors, collection expressions, switch expressions enforced by repo rules.

What hurts:

- No evidence of `.NET Aspire` orchestration — the docker-compose path is the dev-loop reality. Aspire would give you typed service discovery, OTEL wiring, and dashboard for free.
- Some integration paths (Logic Apps Standard for edge orchestration in ADR 0019) are not yet covered by `ArchLucid.*.Tests`.

**Improvements.**
1. Pilot `.NET Aspire` for the local dev stack (`ArchLucid.AppHost`) — keep docker-compose for CI/operator use.
2. Add a Logic Apps Standard contract test that hits the workflow endpoints with golden payloads.

---

### 3.10 Security — 78 / 100 (weight 6, gap 132)

**Justification.** Strong: shipped auth defaults are **fail-closed** (`appsettings.json` API-key disabled; only `Development` flips to `DevelopmentBypass`); `AuthSafetyGuard.GuardAllDevelopmentBypasses` throws on production-like hosts; role-aware rate limiting with role-multipliers; `RequireJwtBearerInProduction` opt-in; SQL RLS with break-glass bypass guarded by **two** env vars **and** a Prometheus alert (`ArchLucidRlsBypassEnabledInProduction`); content safety mandatory in production; prompt redactor with skipped-counter audit; ZAP baseline strict on PR + weekly; CodeQL custom log-sanitizer model pack; gitleaks Tier 0; Schemathesis with `--checks=all`; API key rotation runbook; CMK rotation runbook; deny-public for SMB/445.

What hurts:

- The legacy leaked test token is still in `git log` (per the marketability honesty boundary). Any procurement-driven security review will surface it.
- I see no SBOM **diff** gate (CycloneDX is generated but not compared PR-over-PR for new GPL or risky transitive deps).
- No documented response time for the `archlucid_rls_bypass_enabled_info` alert.

**Improvements.**
1. Rewrite history to evict the leaked test token and rotate any related provider-side credential.
2. Add a CI step that diffs CycloneDX SBOM against `main` and fails on new high-severity CVEs or licence changes.
3. Add an SLA row for `ArchLucidRlsBypassEnabledInProduction` to `runbooks/SLO_PROMETHEUS_GRAFANA.md`.

---

### 3.11 Explainability — 80 / 100 (weight 6, gap 120)

**Justification.** `RunExplanationSummaryService`, `ExplanationFaithfulnessChecker`, `archlucid_explanation_faithfulness_ratio` histogram, `archlucid_explanation_aggregate_faithfulness_fallback_total` (deterministic fallback when LLM narrative diverges from findings), `EXPLANATION_SCHEMA.md`, `EXPLAINABILITY_TRACE_COVERAGE.md`, `archlucid_explanation_citations_emitted_total` with `kind` label, `CitationChips.tsx`, `RunExplanationSection.tsx`. Faithfulness is measured *and* enforced by deterministic substitution.

What hurts: the "what did the agent do, in human words" surface is per-run; there is no cross-run *explainability summary* (e.g., "across the last 30 days, the Compliance agent fired finding X 14 times for reason Y").

**Improvements.**
1. Add a `GET /v1/explain/aggregations?from=&to=` endpoint that returns a per-agent, per-finding-type explainability rollup.

---

### 3.12 Policy & Governance Alignment — 76 / 100 (weight 5, gap 120)

**Justification.** Policy packs versioned with unique `(Pack,Version)` constraint (`080_PolicyPackVersions_UniquePackVersion.sql`), governance approvals with SLA columns (`058_GovernanceApprovalRequests_Sla.sql`), governance workflow tables (`017_GovernanceWorkflow.sql`), advisory scheduling, alerting, audit deny-update-delete (`051_AuditEvents_DenyUpdateDelete.sql`), `GOVERNANCE.md`, `GOVERNANCE_WORKFLOW_UI.md`, `POLICY_PACKS_ASSIGNMENTS_BlockCommitMinimumSeverity.sql`.

What hurts: no published evidence the policy-pack format is **importable from external sources** (e.g., OPA/Rego, Conftest). Today's pack model is internal.

**Improvements.**
1. Document and implement an external-pack import contract (`docs/POLICY_PACK_IMPORT.md` + `POST /v1/policy-packs/import`) accepting a small subset of OPA-style rules.

---

### 3.13 Traceability — 84 / 100 (weight 7, gap 112)

**Justification.** Excellent. `dbo.Runs.OtelTraceId` persisted at run creation (migration 052), `X-Trace-Id` + `traceparent` on **every** API response regardless of sampling, custom Activity sources (`ArchLucid.AuthorityRun`, `ArchLucid.AdvisoryScan`, `ArchLucid.Retrieval.Index`, `ArchLucid.Agent.Handler`, `ArchLucid.Agent.LlmCompletion`, `ArchLucid.RetrievalIndexing.Outbox`, `ArchLucid.IntegrationEvent.Outbox`, `ArchLucid.DataArchival`), `archlucid trace <runId>` CLI, agent execution trace blob storage with SQL inline fallback, integration-event outbox correlation tests, `V1_REQUIREMENTS_TEST_TRACEABILITY.md`, `BACKGROUND_JOB_CORRELATION.md`, `AGENT_TRACE_FORENSICS.md`.

What hurts: tail-sampling reliance on a collector (head-based SDK loses high-value error traces in production at SamplingRatio=0.1) is documented but not turn-key in `infra/`.

**Improvements.**
1. Ship an OTEL collector module (`infra/terraform-otel-collector/`) with a tail-sampling policy that always retains errors, slow requests, and `ArchLucid.AuthorityRun`.

---

### 3.14 Reliability — 78 / 100 (weight 5, gap 110)

**Justification.** Circuit breakers (`CircuitBreakingAgentCompletionClient`, `CircuitBreakingOpenAiEmbeddingClient`), `archlucid_circuit_breaker_*` counters, transactional outboxes for retrieval indexing (ADR 0004) and integration events (`041_IntegrationEventOutbox_RetryDeadLetter.sql`), RowVersion optimistic concurrency, RCSI (`091_ReadCommittedSnapshotIsolation.sql`), Simmy chaos (`simmy-chaos-scheduled.yml`), geo-failover drill runbook, SQL failover Terraform module, k6 soak.

What hurts: no documented MTBF / SLO targets for the alerting pipeline itself; degraded-mode doc exists but is not machine-checked.

**Improvements.**
1. Add per-component SLOs (alert evaluation, run commit, agent execution) to `docs/API_SLOS.md` and back them with Prometheus recording rules.

---

### 3.15 Maintainability — 74 / 100 (weight 4, gap 104)

**Justification.** `CSHARP_HOUSE_STYLE.md`, `.editorconfig`, multiple "CSharp-Terse-*.mdc" rules (primary constructors, switch expressions, target-typed `new()`, collection expressions, pattern matching, expression-bodied members, range/index, null-coalescing, LINQ over `foreach`), `TERSENESS_REWRITER_ASSEMBLY_CHECKLIST.md`, `NEXT_REFACTORINGS.md`, `Navigation.mdc`, dependency injection map (`DI_REGISTRATION_MAP.md`), code map docs.

What hurts: 50+ projects mean cold-builds and IntelliSense load are significant; `PROJECT_CONSOLIDATION_PROPOSAL.md` is open; one-class-per-file rule is enforced by convention but not by analyzer.

**Improvements.**
1. Add a Roslyn analyzer (or repo-local `.editorconfig` rule) that enforces one top-level type per file — surfaces drift in PR.

---

### 3.16 Data Consistency — 82 / 100 (weight 5, gap 90)

**Justification.** RCSI on, FK batches (`092_FK_Outbox_Alerts_Batch1.sql`, `093_FK_Audit_Recommendations_ConversationMessages_Batch2.sql`), check constraints batch (`095_CheckConstraints_StatusDomains_Batch.sql`), JSON-shape checks (`096_CheckJson_CorePayloadColumns.sql`), `099_DataConsistencyQuarantine.sql`, `DataConsistencyOrphanProbeHostedService` with `archlucid_data_consistency_orphans_detected_total` and `archlucid_data_consistency_alerts_total` (with `Alert`/`Quarantine` enforcement modes), `DATA_CONSISTENCY_MATRIX.md`, `JSON_FALLBACK_AUDIT.md` (relational-first reads).

What hurts: I see no scheduled "consistency report" exposed to operators (the probe runs and emits metrics, but operators have to know the metric name).

**Improvements.**
1. Add a `GET /v1/admin/consistency/report` endpoint (Admin/Auditor) that returns the current orphan slices in human form.

---

### 3.17 Auditability — 82 / 100 (weight 5, gap 90)

**Justification.** Append-only audit (`051_AuditEvents_DenyUpdateDelete.sql`), correlation/run indexes on audit events (`055_AuditEvents_CorrelationId_RunId_Indexes.sql`), `Auditor` role + `RequireAuditor` policy, `GET /v1/audit/export`, `AUDIT_COVERAGE_MATRIX.md`, integration event catalog persisted to `dbo.IntegrationEventOutbox` with retry/DLQ.

What hurts: no documented **immutability proof** path (e.g., periodic Merkle digest of audit rows posted to immutable storage).

**Improvements.**
1. Add a daily background job that hashes the previous day's `dbo.AuditEvents` rows into a Merkle root and pushes the root to an immutable blob (with `IsVersioningEnabled=true`, `BlobImmutabilityPolicy`).

---

### 3.18 AI / Agent Readiness — 80 / 100 (weight 4, gap 80)

**Justification.** `AgentRuntime` with circuit breaker, fallback completion client, caching completion client, LLM accounting client, prompt redactor, content safety guard with production-mandatory wiring, agent execution trace recorder, prompt-injection regression eval (`AI_AGENT_PROMPT_REGRESSION.md`), reference-case scoring (`AGENT_OUTPUT_EVALUATION.md`), critic agent, semantic + structural completeness metrics, `archlucid_agent_output_quality_gate_total` with `accepted/warned/rejected` outcomes.

What hurts: no documented retrieval-augmentation evaluation harness for the *Compliance* agent's claim grounding; no published model-card.

**Improvements.**
1. Add `docs/AGENT_MODEL_CARD.md` per agent (Topology, Cost, Compliance, Critic) covering inputs, outputs, known failure modes, and which redaction categories are enforced.

---

### 3.19 Interoperability — 76 / 100 (weight 3, gap 72)

**Justification.** OpenAPI v1 served at `/openapi/v1.json`, drift detection (`OPENAPI_CONTRACT_DRIFT.md`), AsyncAPI 2.6 spec (`docs/contracts/archlucid-asyncapi-2.6.yaml`), JSON-schema'd integration events (`schemas/integration-events/*.v1.schema.json`), republished `ArchLucid.Api.Client` package, `INTEGRATION_EVENTS_AND_WEBHOOKS.md`, OAuth2 / Entra SSO.

What hurts: no published Python or TypeScript client.

**Improvements.**
1. Auto-generate a TypeScript client from the OpenAPI snapshot in CI (`@hey-api/openapi-ts`) and publish to npm.

---

### 3.20 Scalability — 76 / 100 (weight 3, gap 72)

**Justification.** Container Apps with revision/canary controls, secondary-region Terraform, RCSI, page compression (`087_*`, `088_*`, `089_*`, `090_*`), per-tenant cost model, capacity & cost playbook, k6 per-tenant burst.

What hurts: no proven horizontal-scale story for the SQL writer side (single-writer per tenant is the implicit model and is not stress-tested in `LOAD_TEST_BASELINE.md`).

**Improvements.**
1. Add a per-tenant write-burst k6 scenario and document the writer-side ceiling in `SCALING_PATH.md`.

---

### 3.21 Observability — 86 / 100 (weight 5, gap 70)

**Justification.** Best-in-class for this project size. Stable `ArchLucid` meter, dozens of named instruments with low-cardinality labels, custom `ActivitySource` per subsystem, `archlucid_explanation_cache_hit_ratio` recording rule, Grafana dashboard JSON committed (`infra/grafana/dashboard-archlucid-trial-funnel.json`), Prometheus alert rules (`infra/prometheus/archlucid-alerts.yml`), business KPI metrics co-located with operational metrics, sampling strategy documented, persisted trace IDs on `dbo.Runs`.

What hurts: no built-in **anomaly detection** on the KPI side; SLO burn-rate alerts not in repo (only generic alerts).

**Improvements.**
1. Add multi-window multi-burn-rate SLO alerts per `API_SLOS.md` to `infra/prometheus/`.

---

### 3.22 Testability — 80 / 100 (weight 3, gap 60)

**Justification.** `ArchLucid.TestSupport` shared fixtures, 20+ test projects, in-memory + SQL providers, per-test SQL DBs with DbUp on the test host, Playwright with `playwright.mock.config.ts`, accessibility (`axe`) tests in marketing flow, Stryker mutation testing, contract test coverage gap doc.

What hurts: contract-test coverage doc explicitly notes gaps; mutation score is not surfaced as a PR comment with thresholds.

**Improvements.**
1. Surface Stryker mutation score per project in the PR sticky coverage comment; fail PR if score drops > 2 percentage points.

---

### 3.23 Modularity — 82 / 100 (weight 3, gap 54)

**Justification.** Persistence is split into 4 sub-projects, Contracts is split into Contracts.Abstractions, Host is split into Host.Core + Host.Composition, dedicated AgentSimulator project, dedicated Backfill.Cli, dedicated Jobs.Cli. Composition root pattern visible in `Startup/ServiceCollectionExtensions.*.cs`.

**Improvements.**
1. Apply NetArchTest from §3.4 to make the modularity testable, not just structural.

---

### 3.24 Performance — 74 / 100 (weight 2, gap 52)

**Justification.** `ArchLucid.Benchmarks` project (BenchmarkDotNet), page compression on hot tables, hot-path read cache (`IHotPathReadCache`), explanation cache, k6 baseline doc, sampling tuning.

What hurts: benchmark project exists but I see no scheduled benchmark run that posts deltas to a PR comment.

**Improvements.**
1. Add a nightly `bench.yml` that runs `ArchLucid.Benchmarks` and posts a delta vs `main` baseline.

---

### 3.25 Availability — 76 / 100 (weight 2, gap 48)

**Justification.** Geo-failover drill runbook, SQL failover Terraform module, secondary region tf, Container Apps multi-zone, canary deploy controls.

**Improvements.**
1. Schedule the `GEO_FAILOVER_DRILL.md` as a quarterly GHA workflow with a sign-off checklist artefact.

---

### 3.26 Cost-Effectiveness — 76 / 100 (weight 2, gap 48)

**Justification.** `consumption_budget.tf` modules, `PER_TENANT_COST_MODEL.md`, page compression, sampling reduction, `CAPACITY_AND_COST_PLAYBOOK.md`.

**Improvements.**
1. Add a monthly Azure Cost Mgmt export-to-blob pipeline + a `cost-summary` GHA that posts the per-feature spend to the team channel.

---

### 3.27 Manageability — 78 / 100 (weight 2, gap 44)

**Justification.** `archlucid doctor`, `archlucid support-bundle --zip`, 31 runbooks, App Configuration deferred ADR (0017) — the team consciously chose JSON config until App Config maturity is needed.

**Improvements.**
1. Implement the App Configuration adoption path described in ADR 0017 once the SaaS deployment is live, with a `docs/runbooks/APP_CONFIGURATION.md`.

---

### 3.28 Accessibility — 65 / 100 (weight 1, gap 35)

**Justification.** `archlucid-ui/src/accessibility/trial-marketing-axe.test.tsx` + npm `test:axe-components` script. Keyboard shortcuts doc (`archlucid-ui/docs/KEYBOARD_SHORTCUTS.md`). Operator UI uses semantic HTML in components I sampled.

What hurts: axe coverage looks limited to **trial marketing** flow today; operator pages don't all have axe gates.

**Improvements.**
1. Extend `axe` gates to every `(operator)` route at the Vitest layer.

---

### 3.29 Deployability — 84 / 100 (weight 2, gap 32)

**Justification.** Terraform across 14 stacks, `cd-staging-on-merge.yml`, `cd.yml`, container apps revision + canary, `release-smoke.{cmd,ps1}`, `run-readiness-check.{cmd,ps1}`, `RELEASE_LOCAL.md`, `BUILD.md`, DbUp migration on startup with fail-fast.

**Improvements.**
1. See §3.8 — wire a greenfield-apply test to close the only material gap.

---

### 3.30 Extensibility — 78 / 100 (weight 1, gap 22)

**Justification.** Provider abstractions (`Billing:Provider=AzureMarketplace|Stripe|...`), pluggable storage (in-memory vs SQL — ADR 0011), pluggable agent client (real / fallback / cached / accounting / circuit-breaking), `templates/archlucid-finding-engine` template project, integration events as the public extension point.

**Improvements.**
1. Document the `IFindingEngine` contract as a public SDK and ship a NuGet template (`dotnet new archlucid-finding-engine`).

---

### 3.31 Documentation — 88 / 100 (weight 1, gap 12)

**Justification.** 191 docs, 31 runbooks, 21 ADRs, multiple architecture indexes, four Day-1 personas, Cursor-prompts catalog, `CHANGELOG.md`, `BREAKING_CHANGES.md`, glossary terms in UI (`GlossaryTerm.tsx`).

**Tradeoffs.** Quantity is high (which trades against §3.5 Cognitive Load).

**Improvements.**
1. See §3.5 — demote stale assessments to keep the active doc set sized for human scan.

---

## 4. Six best improvements (highest leverage on the weighted score)

These are sequenced by the size of their *combined* effect across multiple high-weight dimensions, not just by single-dimension gap.

| # | Improvement | Primary dimensions impacted (weight) | Combined weight |
|---|---|---|---:|
| **1** | **Bring the commercial motion live** — bind `archlucid-ui` marketing pages to a real brand domain via `infra/terraform-edge/frontdoor-marketing.tf` (with WAF + cert), GA a non-Marketplace billing path (Stripe), publish one named reference customer, and rewrite history to evict the leaked test token. | Marketability (16) | **16** |
| **2** | **Single-page, persona-locked first-30-minute path + retire stale assessments** to compress cognitive load and friction. | Usability (8), Cognitive Load (4), Documentation (1), Supportability (8) | **21** |
| **3** | **Architecture boundary tests + first consolidation merge** — introduce `ArchLucid.Architecture.Tests` (NetArchTest) pinning ADR-mandated layering, then land the first project consolidation step from `PROJECT_CONSOLIDATION_PROPOSAL.md`. | Arch Integrity (7), Evolvability (6), Maintainability (4), Modularity (3) | **20** |
| **4** | **Greenfield SaaS deployment workflow + OTEL collector module** — ship `cd-saas-greenfield.yml` (manual + scheduled `terraform apply` end-to-end on a fresh subscription), and add `infra/terraform-otel-collector/` with tail sampling for errors and `ArchLucid.AuthorityRun`. | Azure Compatibility (8), Azure Dev Tech (8), Deployability (2), Availability (2), Traceability (7), Observability (5) | **32** |
| **5** | **Promote stateful Schemathesis + add typed-client replay tests + enforce migration numbering** to harden correctness on every PR. | Correctness (8), Data Consistency (5), Reliability (5), Interoperability (3) | **21** |
| **6** | **Day-1 evidence pack** — `GET /v1/support/evidence-pack.zip` (Admin/Auditor) bundling audit export, RLS posture, content-safety configuration, policy-pack hashes, last-30-day SLO numbers, and the daily Merkle root of `dbo.AuditEvents`. | Security (6), Auditability (5), Policy & Governance (5), Supportability (8), Explainability (6) | **30** |

---

## 5. Cursor prompts for the six improvements

> Paste each prompt into Cursor as the body of a new agent task. Each prompt is self-contained; do not require the agent to read this assessment.

### 5.1 Cursor prompt — Improvement 1: Bring the commercial motion live

```text
Goal: Make ArchLucid commercially reachable from a real, branded URL.

Do the following in this order. Stop and ask if any step requires a decision
beyond what the repository already documents.

1. Read these files first:
   - infra/terraform-edge/frontdoor-marketing.tf
   - infra/terraform-edge/frontdoor-marketing-routes.tf
   - infra/terraform-edge/main.tf
   - infra/terraform-edge/variables.tf
   - archlucid-ui/src/app/(marketing)/welcome/page.tsx
   - archlucid-ui/src/app/(marketing)/signup/page.tsx
   - archlucid-ui/public/pricing.json
   - docs/MARKETABILITY_ASSESSMENT_2026_04_18.md (honesty boundary section)
   - docs/adr/0016-billing-provider-abstraction.md
   - docs/AZURE_MARKETPLACE_SAAS_OFFER.md

2. In infra/terraform-edge, add:
   - A required Terraform variable `marketing_brand_hostname`
     (e.g. "archlucid.com", "www.archlucid.com").
   - An `azurerm_cdn_frontdoor_custom_domain` resource bound to that hostname
     with managed TLS, plus the routing rule that maps it to the existing
     marketing origin group. Do NOT hard-code a hostname.

3. Add a non-Marketplace billing provider path:
   - Following the abstraction in ADR 0016, add a `StripeBillingProvider`
     (or whatever provider best fits the existing abstraction in
     ArchLucid.Application/Billing/*).
   - Add `POST /v1/billing/webhooks/stripe` (or equivalent) in
     ArchLucid.Api with signature verification. Mirror the GA flag pattern
     from Marketplace (`Billing:Stripe:GaEnabled`, default false initially).
   - Update docs/BILLING.md to document the second provider and add a
     runbook docs/runbooks/STRIPE_WEBHOOK_INCIDENT.md.

4. In docs/go-to-market/reference-customers/README.md, replace the placeholder
   row with one named reference (or a clearly-marked design-partner row with
   a public-permission flag).

5. Run a leaked-secrets sweep:
   - Execute `gitleaks detect --redact --no-git -v` (do NOT modify history;
     just report). Print findings.
   - If findings exist, prepare a `git filter-repo --replace-text` plan and
     a credential-rotation checklist as a new file
     `docs/runbooks/SECRET_HISTORY_REWRITE.md`. Do not run the rewrite.

6. Wire CI: extend `scripts/ci/check_reference_customer_status.py` so the
   warn becomes an error once any row reaches `Status: Published`.

7. Update CHANGELOG.md, ARCHLUCID_RENAME_CHECKLIST guardrails do not apply
   here, and open one PR per top-level concern (Terraform, billing provider,
   docs/runbook, CI guard) to keep review surface tight.

Acceptance:
- `terraform validate` passes in infra/terraform-edge with the new variable
  exercised by a sample tfvars in `infra/terraform-edge/examples/`.
- `dotnet test` passes including new StripeBillingProvider unit tests.
- ZAP baseline still clean.
- New runbooks render and link from docs/runbooks/README.md.

Constraints:
- No ConfigureAwait(false) in tests.
- One class per file, primary constructors where applicable, is null /
  is not null, collection expressions, switch expressions per repo style.
- Always check nulls.
```

### 5.2 Cursor prompt — Improvement 2: First-30-minute path + retire stale assessments

```text
Goal: Cut cognitive load for first-time operators and developers without
deleting any historically valuable doc.

Do the following:

1. Read these files first:
   - README.md (Key documentation table)
   - docs/START_HERE.md
   - docs/onboarding/day-one-developer.md
   - docs/onboarding/day-one-sre.md
   - docs/onboarding/day-one-security.md
   - docs/OPERATOR_QUICKSTART.md
   - docs/CORE_PILOT.md

2. Create docs/FIRST_30_MINUTES.md. Structure:
   - One paragraph: what the reader will accomplish.
   - Pick ONE persona: "operator evaluating ArchLucid for the first time".
   - Exactly 10 numbered shell commands, copy-pasteable, that go from
     `git clone` to "I committed a manifest and saw a finding". Use the
     existing `scripts/demo-start.ps1` / `archlucid dev up` paths; do NOT
     introduce new tooling.
   - For each command, one sentence on what to expect and the next step.
   - One image placeholder per major outcome (run created, manifest
     committed, finding visible) — leave as `![alt](placeholder.png)` with
     an issue link to capture the screenshot.

3. Replace the "Key documentation" block in README.md with a 5-row table:
   "If you are an Operator/Developer/SRE/Security/Sponsor → start here".
   Each row links to exactly one Day-1 doc (or to FIRST_30_MINUTES.md
   for the operator row).

4. Demote stale assessments:
   - Move every file matching docs/MARKETABILITY_ASSESSMENT_*.md,
     docs/QUALITY_ASSESSMENT_*.md, docs/CORRECTNESS_QUALITY_ASSESSMENT_*.md
     EXCEPT the most recent of each family into docs/archive/quality/.
   - Update internal links via grep + replace; verify with
     `rg "docs/MARKETABILITY_ASSESSMENT" -n` showing only intentional
     archive references.
   - Add a one-line README in docs/archive/quality/ explaining the policy.

5. Add a scope header convention:
   - Add scripts/ci/check_doc_scope_header.py that requires every
     docs/*.md file to start with a markdown blockquote line beginning
     with `**Scope:**`. Wire it into a new (non-blocking) CI job so the
     team sees the gap surface PR-by-PR.
   - Add the scope header to FIRST_30_MINUTES.md and to README.md (as a
     comment-style note that the script tolerates).

Acceptance:
- README.md "Key documentation" is 5 rows, not the current giant table.
- docs/FIRST_30_MINUTES.md exists, has 10 commands, and links from the
  README operator row.
- `rg "QUALITY_ASSESSMENT_" docs/ -l` shows only the most recent file
  outside docs/archive/quality/.
- CI passes; the new scope-header job runs but is `continue-on-error: true`.

Style:
- Markdown only; no code style changes.
- Markdown-Generosity rule applies — err on the side of clear instructions.
```

### 5.3 Cursor prompt — Improvement 3: Architecture boundary tests + first consolidation merge

```text
Goal: Make the layering described in ADRs mechanically enforceable, then
land the first consolidation step from PROJECT_CONSOLIDATION_PROPOSAL.md.

Do the following:

1. Read these files first:
   - docs/adr/0001-hosting-roles-api-worker-combined.md
   - docs/adr/0002-dual-persistence-architecture-runs-and-runs.md
   - docs/adr/0012-runs-authority-convergence-write-freeze.md
   - docs/adr/0021-coordinator-pipeline-strangler-plan.md
   - docs/PROJECT_CONSOLIDATION_PROPOSAL.md
   - docs/ARCHITECTURE_CONSTRAINTS.md
   - ArchLucid.sln (project list only)
   - The .csproj files of ArchLucid.Application, ArchLucid.Persistence,
     ArchLucid.Persistence.Alerts, ArchLucid.Persistence.Coordination,
     ArchLucid.Persistence.Integration, ArchLucid.Persistence.Runtime,
     ArchLucid.Persistence.Advisory, ArchLucid.Coordinator,
     ArchLucid.AgentRuntime, ArchLucid.Host.Core, ArchLucid.Host.Composition.

2. Create a new test project ArchLucid.Architecture.Tests (xUnit + FluentAssertions
   + NetArchTest.Rules). Add it to the solution under the existing tests folder.

3. Add the following NetArchTest assertions, each as its own test class (one
   class per file):
   a. `ApplicationDoesNotDependOnPersistence` — types in ArchLucid.Application
      may not directly reference any ArchLucid.Persistence* assembly except
      through interfaces declared in ArchLucid.Contracts.Abstractions.
   b. `ApiDoesNotDependOnPersistence` — types in ArchLucid.Api may not directly
      reference any ArchLucid.Persistence* assembly.
   c. `ContractsHasNoFrameworkDependencies` — types in ArchLucid.Contracts may
      not reference Microsoft.AspNetCore.* or Microsoft.Data.SqlClient.
   d. `HostCompositionIsTheOnlyDIWiringPoint` — only ArchLucid.Host.Composition
      may contain `Add*` extension methods on `IServiceCollection` for
      Persistence* concrete types.
   e. `OneTopLevelTypePerFile` — best-effort check that every .cs file in
      ArchLucid.* contains at most one top-level public type
      (skip generated files under obj/).

4. For any failing assertion, do NOT loosen the rule. Open issues in a
   markdown list at the end of the PR description. If a violation is
   trivially fixable, fix it; otherwise add a `[Trait("Category","Quarantine")]`
   xUnit trait and exclude that one offender by name with a `// TODO(architecture)`
   comment that links to the issue.

5. Land the first consolidation step:
   - Pick the two smallest sibling projects in PROJECT_CONSOLIDATION_PROPOSAL.md
     with the heaviest mutual coupling (justify in the PR body using
     `dotnet list reference` output).
   - Merge them with namespace preservation; update the .sln; run the
     full test suite.
   - Add an ADR docs/adr/0022-project-consolidation-step-1.md documenting
     the merge and the next candidates.

Acceptance:
- ArchLucid.Architecture.Tests builds and runs in CI Tier 1 (fast core).
- Solution builds; all existing tests pass.
- New ADR is in place and linked from docs/adr/README.md.

Style:
- C# 12 primary constructors; one class per file; is null patterns; no
  ConfigureAwait(false) in tests; collection expressions; switch expressions.
- Always check nulls.
```

### 5.4 Cursor prompt — Improvement 4: Greenfield SaaS deployment workflow + OTEL collector

```text
Goal: Prove the Terraform end-to-end on a fresh Azure subscription, and ship
a tail-sampling OTEL collector so high-value traces survive production sampling.

Do the following:

1. Read these files first:
   - infra/terraform/main.tf, providers.tf, variables.tf, outputs.tf
   - infra/terraform-container-apps/{main,jobs,variables,providers}.tf
   - infra/terraform-storage/main.tf
   - infra/terraform-keyvault/main.tf
   - infra/terraform-monitoring/{application_insights,grafana_dashboards,prometheus_slo_rules}.tf
   - infra/terraform-sql-failover/main.tf
   - .github/workflows/cd-staging-on-merge.yml
   - .github/workflows/cd.yml
   - docs/DEPLOYMENT.md
   - docs/DEPLOYMENT_TERRAFORM.md
   - docs/OBSERVABILITY.md (Sampling strategy section — head-based caveat)

2. Create a new GitHub Actions workflow .github/workflows/cd-saas-greenfield.yml:
   - Trigger: workflow_dispatch + schedule (monthly cron, e.g. 1st of month 06:00 UTC).
   - Use OIDC federation to a dedicated greenfield-test Azure subscription
     (introduce `AZURE_GREENFIELD_TENANT_ID`, `AZURE_GREENFIELD_SUBSCRIPTION_ID`,
     `AZURE_GREENFIELD_CLIENT_ID` repository secrets — document in the workflow
     header).
   - Steps: create RG → terraform init/validate/plan/apply for the minimal
     stack (terraform, terraform-storage, terraform-keyvault, terraform-monitoring,
     terraform-container-apps, terraform-sql-failover) → wait for
     `GET /health/ready` 200 → run `archlucid doctor` against the API →
     terraform destroy in `if: always()` cleanup job.
   - Persist plan + apply logs as workflow artifacts (no secrets).

3. Add a new Terraform stack infra/terraform-otel-collector/:
   - providers.tf, versions.tf, variables.tf, main.tf, outputs.tf, checks.tf.
   - Deploy the OpenTelemetry collector as an Azure Container App with a
     tail-sampling processor configuration that retains:
       * any trace where `error.type` is set,
       * any trace whose root span duration > 2s,
       * any trace whose root span belongs to ActivitySource
         `ArchLucid.AuthorityRun` (always 100%).
     Configure OTLP receivers (gRPC + HTTP) and an OTLP/Azure Monitor exporter.
   - Document the sampling policy inline in main.tf as comments and update
     docs/OBSERVABILITY.md "Sampling strategy" section to point at the new
     stack as the recommended path.

4. Update docs/runbooks/CANARY_DEPLOYMENT.md with a "greenfield smoke" section
   that calls out the monthly workflow as the canonical Terraform regression.

Acceptance:
- `terraform validate` passes on infra/terraform-otel-collector/ in CI.
- The new workflow lints (`act -n` or `actionlint`) and is referenced from
  README.md → "Pilot onboarding" section.
- docs/OBSERVABILITY.md no longer says "not turn-key in infra/" — it points
  at the new stack.

Style:
- Terraform: no `archiforge` substring (CI guard); use `archlucid_*` names.
- Markdown: keep section headings stable; link bidirectionally.
- All infra representable in Terraform (do not add scripts that mutate Azure
  out-of-band).
```

### 5.5 Cursor prompt — Improvement 5: Stateful Schemathesis + typed-client replay + migration numbering guard

```text
Goal: Catch contract drift on PR (not just nightly) and prevent duplicate
migration numbers from sneaking in.

Do the following:

1. Read these files first:
   - .github/workflows/ci.yml (jobs: api-schemathesis-light)
   - .github/workflows/schemathesis-scheduled.yml
   - docs/API_FUZZ_TESTING.md
   - docs/CONTRACT_TEST_COVERAGE_GAP_ANALYSIS.md
   - ArchLucid.Api.Client/ArchLucid.Api.Client.csproj
   - ArchLucid.Api.Client.Tests/ArchLucid.Api.Client.Tests.csproj
   - The list under ArchLucid.Persistence/Migrations/*.sql (note duplicates
     numbered 096_*).

2. Promote stateful Schemathesis to PR:
   - In .github/workflows/ci.yml, add job `api-schemathesis-stateful` that
     runs after `api-schemathesis-light` succeeds. Use
     `--phases=stateful --max-examples=10 --hypothesis-seed=fixed`
     to keep PR latency bounded (target < 4 minutes).
   - Allow `continue-on-error: false` only after one green nightly run on
     main; ship initially with `continue-on-error: true` and a TODO to flip.

3. Create ArchLucid.Api.Client.ContractReplay.Tests project:
   - References ArchLucid.Api.Client.
   - Uses xUnit + WebApplicationFactory<Program> from ArchLucid.Api or, if
     that is not feasible, the API container started by the existing
     test-fast-core script.
   - Hard-code 6 golden operator scenarios derived from
     ArchLucid.Api/ArchLucid.Api.http and the existing fixtures under tests/
     (request → manifest commit → finding read → audit list → policy pack
     read → replay).
   - Each scenario calls the typed client end-to-end and asserts the
     response shape against an embedded golden JSON file.

4. Migration numbering guard:
   - Add scripts/ci/check_migration_numbering.py that scans
     ArchLucid.Persistence/Migrations/*.sql for duplicate `^\d{3}_` prefixes
     and unexpected gaps (gap > 3). Fail with a clear message.
   - Wire it into the .NET fast-core CI tier as a pre-build step.
   - In a separate PR scope (called out in the PR body), rename the duplicate
     096_* files to non-conflicting numbers, updating any DbUp manifest if
     ordering relies on lexical sort. Do NOT modify shipped migration
     content; only the filename/number prefix per ArchLucid-Rename rule
     ("never modify historical SQL migration files" applies to content,
     not to fixing a duplicate-number bug — document this in the PR).

Acceptance:
- CI green: api-schemathesis-stateful runs on PR; ContractReplay.Tests passes.
- check_migration_numbering.py fails locally before the rename, passes after.
- docs/API_FUZZ_TESTING.md updated to reflect the new PR job.

Style:
- No ConfigureAwait(false) in tests.
- LINQ over foreach in C#; one class per file; is null patterns.
- Always check nulls.
```

### 5.6 Cursor prompt — Improvement 6: Day-1 evidence pack + audit Merkle root

```text
Goal: Ship a single endpoint that produces the artefact governance reviewers
ask for, plus a tamper-evident audit chain.

Do the following:

1. Read these files first:
   - ArchLucid.Api/Controllers/* (find audit/export, run/export controllers)
   - docs/AUDIT_COVERAGE_MATRIX.md
   - docs/security/MULTI_TENANT_RLS.md
   - docs/SECURITY.md
   - docs/POLICY_PACKS_*.md (if present) and policy-pack assignments tables
   - ArchLucid.Persistence/Migrations/051_AuditEvents_DenyUpdateDelete.sql
   - ArchLucid.Persistence/Migrations/055_AuditEvents_CorrelationId_RunId_Indexes.sql
   - ArchLucid.Core/Authorization/ArchLucidPolicies.cs
   - infra/prometheus/archlucid-alerts.yml

2. Add `GET /v1/support/evidence-pack.zip` to ArchLucid.Api:
   - Authorize with policy `RequireAuditor` (and accept Admin too).
   - Build the ZIP in-process from these sources and stream to response:
       * audit-export.jsonl   — same content as the existing audit export
       * rls-posture.json     — current RLS session-context settings,
                                bypass guard env vars (true/false; never
                                values), break-glass alert state.
       * content-safety.json  — endpoint host (no key), threshold,
                                fail-closed flag.
       * policy-packs.json    — assigned packs + version + sha256 of the
                                serialised pack content.
       * slo-30day.json       — query Prometheus for the 9 metrics named
                                in API_SLOS.md and embed last-30d burn rates.
       * audit-merkle.json    — see step 3.
       * MANIFEST.txt         — file list + sha256 per file + run timestamp.
   - Add operation tag `support` and document in API_CONTRACTS.md.

3. Daily audit Merkle job:
   - In ArchLucid.Worker, add a hosted service `AuditMerkleDailyHostedService`
     that runs at 00:30 UTC, computes a Merkle root of the previous day's
     dbo.AuditEvents rows (canonical row hash → leaf SHA-256 → balanced tree
     → root), writes the root + leaf-count + day to a new SQL table
     dbo.AuditMerkleRoots (add migration 100_AuditMerkleRoots.sql; do NOT
     reuse a 09x number), and uploads the root JSON to a configured immutable
     blob container `audit-merkle` (blob versioning + immutability policy).
   - Surface the latest 30 roots from `audit-merkle.json` in the evidence pack.

4. Tests:
   - ArchLucid.Api.Tests: HTTP tests covering 401 (anon), 403 (Reader/Operator),
     200 (Auditor + Admin) on /v1/support/evidence-pack.zip.
   - ArchLucid.Persistence.Tests: integration test that inserts known audit
     rows and verifies the Merkle root is reproducible from the same input.
   - ArchLucid.Worker.Tests (create if missing): unit test for the leaf and
     tree construction.

5. Docs:
   - docs/security/EVIDENCE_PACK.md — what is in the pack, why each item is
     safe to share, and the daily-Merkle proof model.
   - docs/runbooks/EVIDENCE_PACK_OPS.md — how to verify a Merkle root from
     the immutable blob given a downloaded pack.

Acceptance:
- `dotnet test` green including new tests.
- New SQL migration runs on a greenfield empty catalog (DbUp).
- The endpoint produces a ZIP whose MANIFEST.txt validates against the
  contained sha256 sums.
- Evidence pack contains no secrets (verified by the sha256 + a unit test
  that fails if any pre-listed secret-name pattern appears in any file).

Style:
- Primary constructors; is null / is not null; collection expressions; switch
  expressions; LINQ over foreach.
- One class per file; no ConfigureAwait(false) in tests.
- Always check nulls. Single SQL DDL discipline (master `ArchLucid.sql`
  receives the new table addition in a separate PR per repo rule).
```

---

## 6. Notes on the score

- The product is **technically mature** (observability, persistence, multi-tenancy, agent runtime, CI rigour) but **commercially under-launched**. That asymmetry is why a single attribute — Marketability, weight 16 — accounts for **6.8% of the entire weighted gap** (768 / 11,306 of the score that *was earned*; expressed against the unearned part: 768 / 3,694 ≈ 21% of the remaining headroom).
- The next four improvements are all worth doing in parallel; they touch disjoint code paths (CI / Terraform / Architecture tests / API endpoint + worker + SQL).
- I did not score "Brand strength", "Pricing power", or other purely commercial sub-attributes separately because they were folded into Marketability per your weighting.
