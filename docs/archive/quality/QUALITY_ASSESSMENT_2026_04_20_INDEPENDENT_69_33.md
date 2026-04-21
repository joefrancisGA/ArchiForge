> **Scope:** Independent quality assessment (2026-04-20) — weighted **69.33%**. Independent of any prior assessment file.

# Independent Quality Assessment — ArchLucid (weighted 69.33%)

**Date:** 2026-04-20
**Mode:** Independent (no reference to prior assessments).
**Scoring scale:** 1–100 per quality, weighted by the user-supplied weights (commercial Σ=40, enterprise Σ=25, engineering Σ=37, grand Σ=102).
**Weighted overall:** **`Σ(score × weight) ÷ Σ(weight) = 7,071.9 ÷ 102 = 69.33%`**

| Section | Weighted contribution | Section average |
|---|---|---|
| Commercial (Σw=40) | 2,660 | 66.50 |
| Enterprise (Σw=25) | 1,705 | 68.20 |
| Engineering (Σw=37) | 2,706.9 | 73.16 |
| **Grand total** | **7,071.9 / 102** | **69.33** |

The product is **engineering-heavy and commercial-light**: the engine and the docs are mature, but the parts that turn that engine into revenue (named customers, pen-test report, frictionless adoption, working-tool embedding) are still scaffolding rather than evidence.

---

## Reading order

Items are sorted by **`gap × weight = (100 − score) × weight`** so the biggest weighted improvement opportunities appear first. Score is followed by **Justification**, **Tradeoffs**, and **Improvement recommendations**.

---

## 1. Marketability — 65 / 100 (weight 8, gap·w = 280)

**Justification.** `docs/go-to-market/POSITIONING.md`, `COMPETITIVE_LANDSCAPE.md`, `EXECUTIVE_SPONSOR_BRIEF.md`, `PRICING_PHILOSOPHY.md`, and `MARKETABILITY_ASSESSMENT_*` are professional and self-aware. The category claim ("AI Architecture Intelligence") is plausible and explicitly carved against LeanIX, Ardoq, Azure Advisor, Copilot. **However:** zero published reference customers (`docs/go-to-market/reference-customers/README.md` still has only `EXAMPLE_*` and `DESIGN_PARTNER_NEXT` placeholders), no SOC 2 attestation, no public pen-test summary, no live trial loop confirmed in production. Pricing is "locked" with a stated **−50% discount** because the trust/reference/self-serve gates have not cleared. That is honest, but it is also marketability that has not yet been proven by the market.

**Tradeoffs.** Honesty about the discount stack protects the brand long-term but caps near-term ARR. Marketing without a logo wall is a perpetual disadvantage in enterprise sales.

**Improvement recommendations.**
- Convert `DESIGN_PARTNER_NEXT` into a real signed design partner before further pricing work; flip the CI guard `scripts/ci/check_reference_customer_status.py` to merge-blocking only after the row is `Published`.
- Replace `EXECUTIVE_SPONSOR_BRIEF.md` aspirational language with **two named, dollarized outcomes** the moment a pilot completes.
- Stand up a public `archlucid.com` landing page that links the trust center, status page (currently planned only — `OPERATIONAL_TRANSPARENCY.md`), and a one-paragraph differentiator. Today the buyer has to read the GitHub repo to evaluate.

---

## 2. Adoption Friction — 60 / 100 (weight 6, gap·w = 240)

**Justification.** `docs/FIRST_30_MINUTES.md` is a strong "Docker-only, no .NET/Node/cloud keys" path with `scripts/demo-start.ps1`. Auth has fail-closed defaults and a Development bypass guard (`AuthSafetyGuard.GuardAllDevelopmentBypasses`). **But:** the production evaluator surface is large — `appsettings.*` has many sections (`ArchLucidAuth`, `Authentication:ApiKey`, `RateLimiting:*`, `HotPathCache:*`, `AgentExecution:TraceStorage:*`, `LlmPromptRedaction`, `SqlServer:RowLevelSecurity:ApplySessionContext`, `Demo:*`, `ContentSafety:*`, `Billing:AzureMarketplace:GaEnabled`, etc.), the Terraform stack is **15+ separate roots** (`infra/terraform-*`), and the operator has to choose between Coordinator and Authority pipelines (`docs/DUAL_PIPELINE_NAVIGATOR.md`, ADR 0021 "Proposed"). Day-1 mental model is wide.

**Tradeoffs.** Configurability is required for regulated buyers, but it is the #1 reason a 14-day trial fails to convert.

**Improvement recommendations.**
- Ship a **single opinionated SaaS profile** (`appsettings.SaaS.json` + `infra/terraform-pilot` as the only supported "evaluator" stack); demote everything else to advanced.
- Collapse Coordinator → Authority by **executing** ADR 0021 instead of leaving it `Status: Proposed`. Two pipelines that "converge on manifests, artifacts, and review" is a pricing-safe statement and an onboarding nightmare.
- Make `archlucid doctor` print a **red/green list of every required configuration value** so the operator does not learn at first request that `Authentication:ApiKey:Enabled=false` rejects them silently.

---

## 3. Time-to-Value — 70 / 100 (weight 7, gap·w = 210)

**Justification.** Strong: `FIRST_30_MINUTES.md`, `dotnet run --project ArchLucid.Cli -- pilot up`, simulator agents (no AI keys), Contoso seed (`POST /v1/.../demo/seed`), `release-smoke.ps1`. Core Pilot path is 6 steps. **But:** "value" in V1 = "a committed manifest exists" — that is a system milestone, not a *business* outcome. The PILOT_ROI_MODEL is honest that "value is hypothetical until measured" and gives no aggregated pilot data.

**Tradeoffs.** Simulator-only first-value is fast but unconvincing to architects who want to see the real LLM behavior. Adding LLMs adds Azure OpenAI keys and content-safety setup — friction returns.

**Improvement recommendations.**
- Add a **scripted `time-to-first-value` measurement** in `release-smoke.ps1` that prints "request → committed manifest in *N* seconds" so each pilot has its own number.
- Ship a **second seeded scenario** ("brownfield" / "regulated cloud migration") so the trial doesn't always show the same Contoso output.
- Add a **"why this finding matters" one-liner** on the run-detail page; today the manifest review is a JSON-grade artifact, not a story.

---

## 4. Proof-of-ROI Readiness — 60 / 100 (weight 5, gap·w = 200)

**Justification.** `docs/PILOT_ROI_MODEL.md` and `docs/go-to-market/PILOT_SUCCESS_SCORECARD.md` are well structured (baseline, primary metrics, secondary metrics). Per-tenant cost model exists (`docs/deployment/PER_TENANT_COST_MODEL.md`). **But:** the ROI model is a **template** customers must fill in — there is no in-product **automatic baseline capture** or a generated "pilot scorecard" PDF the sponsor can take to a CFO. No published case study with real numbers.

**Tradeoffs.** Self-measured ROI is methodologically defensible but commercially weak — sponsors shop for proof, not method.

**Improvement recommendations.**
- Build a **`POST /v1/pilot/scorecard`** endpoint that aggregates time-to-commit, manifests/run, finding density, governance approvals, and exports — and renders the scorecard in the existing DOCX exporter.
- Capture *one* real pilot baseline with permission and publish it (with redaction templates already in `docs/security/PEN_TEST_REDACTED_SUMMARY_TEMPLATE.md`'s spirit).

---

## 5. Workflow Embeddedness — 55 / 100 (weight 3, gap·w = 135)

**Justification.** Integration touchpoints exist: webhooks (`docs/INTEGRATION_EVENTS_AND_WEBHOOKS.md`), Service Bus (`schemas/integration-events/`), GitHub action (`integrations/github-action-manifest-delta`), CLI, REST. **But:** `V1_SCOPE.md` explicitly excludes VS Code/IDE integration, and `INTEGRATION_CATALOG.md` lists Jira/ServiceNow/Confluence as roadmap. Architects live in PRs, tickets, and Confluence — ArchLucid asks them to come to a **separate web UI**. That is the single most common reason architecture tools die in pilot.

**Tradeoffs.** Building real connectors is slow and risks shallow ones; a deep VS Code surface forks cognitive UX.

**Improvement recommendations.**
- Ship one **deep PR check** (not just the manifest-delta GitHub Action — a check that posts findings as PR comments with `data-archlucid-citation` anchors).
- Ship a **Confluence/SharePoint export** in addition to DOCX; review packages live there.
- Treat the **`X-Correlation-ID`** as a first-class link key — every external system should carry it back so support has a single thread.

---

## 6. Trustworthiness — 60 / 100 (weight 3, gap·w = 120)

**Justification.** `EXECUTIVE_SPONSOR_BRIEF.md` §10 explicitly says "treat AI text as decision support; treat manifests, findings, traces, governance records as reviewable evidence" — that is the right framing. Engineering reality: RLS on covered tables, fail-closed auth, content safety guard, prompt redaction, append-only audit, ZAP+Schemathesis in CI. **But:** SOC 2 Type II is targeted Q3 2027, no published pen-test summary (`docs/security/pen-test-summaries/` only has a draft placeholder), `RLS_RISK_ACCEPTANCE.md` is a template not a populated risk register, and several SQL RLS object names still carry `Archiforge*` per the rename rule. A regulated buyer's security team will mark this *Acceptable with conditions*, not *Approved*.

**Tradeoffs.** Trust is earned over time; the only short-cut is paid attestation, which is expensive — and explicitly **out of budget** for 2026 (per product owner). The owner can self-assess SOC 2 readiness; that produces an internal artifact, not a third-party opinion. Buyers who require a CPA-issued report will still be blocked.

**Improvement recommendations.**
- Run the **owner-led SOC 2 self-assessment now** (no auditor cost) and publish the resulting **gap register** as `docs/security/SOC2_SELF_ASSESSMENT_2026.md`. Mark it explicitly as "self-assessment, not third-party attested".
- Commission a **single external pen test** (the SoW template already exists). This is one-shot spend, not recurring like SOC 2, and removes the −25% trust discount line on its own.
- Populate `RLS_RISK_ACCEPTANCE.md` with a **real, dated risk register** and a named owner per row; an empty template is worse than no template.
- Eliminate the residual `Archiforge*` SQL identifiers in a single migration with a documented decision; the rename rule's "waived for SQL" is a time bomb at audit.

---

## 7. Architectural Integrity — 65 / 100 (weight 3, gap·w = 105)

**Justification.** Strong: 47 projects, NetArchTest layering rules (`ArchLucid.Architecture.Tests`), bounded-context map, ADRs (11), explicit "Tier 1 / Tier 2 / Tier 3 / Tier 4" constraint tiers. **But:** the codebase still hosts **two parallel pipelines** (Coordinator string-run vs Authority ingestion) with `DUAL_PIPELINE_NAVIGATOR.md` explaining how to choose. ADR 0021 (the strangler plan to retire Coordinator) is **`Status: Proposed`** — i.e. an explicit unhealed split. `Persistence` has been split into 7 csproj files (`Persistence`, `Persistence.Advisory`, `Persistence.Alerts`, `Persistence.Coordination`, `Persistence.Integration`, `Persistence.Runtime`, `Persistence.Tests`) — that is a visible domain that is still moving. UI shaping has *four* parallel surfaces (`PRODUCT_PACKAGING.md` §3 "Four UI shaping surfaces (do not merge)") — also a sign that the model is incomplete.

**Tradeoffs.** Strangler patterns are correct; leaving them open-ended for >18 months is not. The product owner has confirmed willingness to **break the Coordinator-only public surface in `v1`** rather than version up to `v2` — that materially shortens the strangler timeline.

**Improvement recommendations.**
- Promote ADR 0021 from **Proposed → Accepted** and execute the first phase this quarter; otherwise downgrade `Coordinator` to "deprecated" in `V1_SCOPE.md` so buyers don't choose it.
- Consolidate `Persistence.*` to **at most three** assemblies (`Persistence`, `Persistence.Coordination`, `Persistence.Tests`) once the dual-pipeline retires.
- Collapse the four UI shaping surfaces into a **single `useOperatorAuthority()` hook** — today contributors must touch 5+ files to add a route, per the `Contributor drift guard` rule.

---

## 8. Correctness — 75 / 100 (weight 4, gap·w = 100)

**Justification.** 840 test files across 23 test assemblies, FsCheck property tests, NetArchTest, Stryker mutation testing (`stryker-pr.yml`), Schemathesis OpenAPI conformance, ZAP, k6 smoke, Simmy chaos, agent prompt-regression baseline, golden agent fixtures, **GreenfieldSqlBoot** integration tests, live API E2E (`live-api-*.spec.ts`). Coverage gates: 79% line / 63% branch merged, with per-package floors. **But:** the `CODE_COVERAGE.md` snapshot is openly **72.95% line / 58.71% branch** and 15 tests fail on a clean local environment — exactly the gap a typical first-pilot CI will see. Prompt regression baseline only covers **Topology** today; Cost/Compliance/Critic floors are 0.0.

**Tradeoffs.** A high coverage gate is real engineering culture; broken local tests sap the same culture.

**Improvement recommendations.**
- Get the **15 locally failing tests** to green or move them under an explicit `Category=RequiresSql` filter that local docs spell out.
- Raise prompt-regression floors for **Cost** and **Compliance** to non-zero (≥0.7 structural). Merge-blocking that is one model's behavior on one agent type is theater.
- Move `Stryker` from advisory to **merge-blocking on `ArchLucid.Decisioning`** (the highest-correctness-risk module).

---

## 9. Executive Value Visibility — 75 / 100 (weight 4, gap·w = 100)

**Justification.** `EXECUTIVE_SPONSOR_BRIEF.md` is excellent — short, sponsor-grade, explicit anti-overclaim section. Three-layer narrative is buyable (Core Pilot / Advanced Analysis / Enterprise Controls). **But:** there is no **executive dashboard** in the operator UI — a CIO who logs in sees the operator surface, not a "this team committed N manifests, M findings, K hours saved" panel.

**Improvement recommendations.**
- Add an **`/exec`** route gated on `AdminAuthority` that renders the same scorecard as the proposed `POST /v1/pilot/scorecard`.
- Make the **DOCX export include the executive summary first**, manifest detail second; today the analysis report opens with run metadata.

---

## 10. Differentiability — 75 / 100 (weight 4, gap·w = 100)

**Justification.** `COMPETITIVE_LANDSCAPE.md` is genuinely good. The combination *multi-agent + provenance + governance + audit + replay* is not in any single competitor today. **But:** the differentiators that show up in a 30-minute demo are the *least* differentiated parts (a manifest, a DOCX, a diagram). The unique parts (citation-bound aggregate explanations, `archlucid_explanation_faithfulness_ratio`, comparison replay verify mode, prompt-regression baselines) are buried in `docs/explainability/` and `docs/MUTATION_TESTING_STRYKER.md`.

**Improvement recommendations.**
- Build **one demo route** ("Why did the agent say this?") that opens the citation-bound trace UI for the seeded run — that is the differentiator no incumbent has.
- Put a **"Replay this comparison"** CTA on every comparison detail page; "verify mode → 422 with drift fields" is unique and invisible today.

---

## 11. Compliance Readiness — 55 / 100 (weight 2, gap·w = 90)

**Justification.** SOC 2 roadmap exists (Phase 4 Q2 2027), DPA template, subprocessors register, GDPR-compatible data flows, audit retention policy, PII classification doc. **But:** none of these are **attested**. ISO 27001 explicitly "not claimed". HIPAA, FedRAMP, PCI not mentioned. Procurement teams treat "in progress" as "not yet". The product owner has indicated **no budget for an external SOC 2 auditor** but the capability to self-assess.

**Improvement recommendations.**
- Run an **owner-led SOC 2 self-assessment** and publish it as `docs/security/SOC2_SELF_ASSESSMENT_2026.md`. Be explicit that it is self-assessed, not third-party attested. This is still useful in procurement: it shows control maturity even without the CPA letter.
- Add a **`docs/security/COMPLIANCE_MATRIX.md`** that maps every ArchLucid control to SOC 2 Trust Services Criteria; this is the artifact the GRC team actually wants in week 1, and it is reusable when a future auditor is funded.
- Pre-fill **CAIQ Lite v4** as a standalone artifact — it removes 2 weeks of procurement back-and-forth even without SOC 2.

---

## 12. Usability — 70 / 100 (weight 3, gap·w = 90)

**Justification.** Operator UI has progressive disclosure, role-aware shaping, accessibility scans, live region for run progress, focus management. **But:** `PRODUCT_PACKAGING.md` §3 has a 600-word paragraph about "four UI shaping surfaces" — that is a smell. The first-pilot wizard is "seven steps" (`README.md`); seven is too many for a first run. Compare/replay/graph/ask are gated behind "Show more links" — they are also the most differentiated capabilities.

**Improvement recommendations.**
- Compress the create-run wizard to **3 steps**: pick brief, pick environment, run. Everything else is preset overridable.
- Surface **one** Advanced Analysis link (Compare runs) on the run-detail page after first commit — never make a user discover it via a "Show more" toggle.
- Run a real **5-user usability test** with architects who have never seen the product; the operator-shell tests (`*.test.tsx`) prove correctness, not learnability.

---

## 13. Security — 70 / 100 (weight 3, gap·w = 90)

**Justification.** Defensible: ZAP baseline (no `-I`, fails on warnings), Schemathesis on every PR, fail-closed auth, RLS, content safety guard, log sanitization, prompt redaction, RLS bypass guard with Prometheus alert, CodeQL workflow, key-rotation runbook. **But:** no published pen test, secrets management documented but not centrally rotated, the `Authentication:ApiKey:DevelopmentBypassAll=true` foot-gun still exists (guarded but exists), `LlmPromptRedaction` is opt-in (`archlucid_llm_prompt_redaction_skipped_total` exists *because* this is bypassable).

**Improvement recommendations.**
- Make **`LlmPromptRedaction:Enabled=true`** the default in every non-Development host and require explicit opt-out with a recorded approval.
- Add a **secret-rotation health check** that fails `/health/ready` if any signing key is older than the configured TTL.
- Ship a **threat-model delta** with every breaking change in `BREAKING_CHANGES.md` (one line: "no new attack surface" or "new endpoint X is rate-limited and authorized by Y").

---

## 14. Decision Velocity — 60 / 100 (weight 2, gap·w = 80)

**Justification.** Order form template, locked pricing, free trial, design-partner discount, guided pilot $15k credit-on-conversion. **But:** procurement gates listed in `SOC2_ROADMAP.md` mean any regulated buyer takes 6+ months. No frictionless self-serve checkout path validated end-to-end (`Billing:AzureMarketplace:GaEnabled` exists with a rollback runbook, suggesting the loop is fresh). No "click to buy" path for SMB.

**Tradeoffs.** Product owner has approved a **Stripe Checkout link for Team tier now** rather than waiting for Azure Marketplace SaaS GA to settle. Stripe pays out faster and avoids the Marketplace cut, but it sits outside the Marketplace lead-generation funnel.

**Improvement recommendations.**
- Stand up a **Stripe Checkout link for the Team tier** in production (one-shot purchase or subscription, owner's choice). Pricing comes from `PRICING_PHILOSOPHY.md` §5.2; this is the only commercial loop that can ship without third-party attestation.
- Continue validating the **Azure Marketplace SaaS offer** end-to-end against a real listing in parallel.

---

## 15. Interoperability — 60 / 100 (weight 2, gap·w = 80)

**Justification.** OpenAPI v1, generated NSwag client (`ArchLucid.Api.Client`), AsyncAPI 2.6 spec, CloudEvents envelope, Service Bus integration events, SIEM export, GitHub action manifest-delta. **But:** the `INTEGRATION_CATALOG.md` admits Jira, ServiceNow, Confluence, SCIM, generic OIDC are roadmap. Service Bus is Azure-only — and per product owner the **AWS/GCP gap is a strategic acceptance** (single-cloud Azure-native posture, ADR 0020). That removes pressure to build cross-cloud parity but keeps the multi-cloud question off the buyer table.

**Improvement recommendations.**
- Ship a **generic outbound webhook** (signed HMAC, configurable endpoint) so customers integrate without Service Bus. This is the cheapest way to widen the interop surface without retreating from Azure-native posture.
- Add **SCIM v2.0 user provisioning** — it is the table-stakes integration for any IDP-driven enterprise, regardless of cloud.

---

## 16. Procurement Readiness — 65 / 100 (weight 2, gap·w = 70)

**Justification.** DPA template, order form template, subprocessors register, SLA summary (99.5% / 30 days), pricing single-source enforced by CI. **But:** templates need legal review before use, no MSA, no security questionnaire pre-fill (e.g. CAIQ Lite, SIG), no "vendor risk packet" zip.

**Improvement recommendations.**
- Pre-fill **CAIQ Lite v4** and post it next to the trust center; that single file removes 2 weeks of procurement back-and-forth.
- Publish an **MSA template** alongside DPA; the order form references both.

---

## 17. Maintainability — 70 / 100 (weight 2, gap·w = 60)

**Justification.** Strong house style enforced by 17 `.cursor/rules/CSharp-Terse-*.mdc` rules (collection expressions, target-typed `new()`, `is null`, primary constructors, expression-bodied members), one-class-per-file, NetArchTest layer enforcement. **But:** 47 csproj projects with overlapping persistence assemblies; some files (e.g. `PRODUCT_PACKAGING.md` §3) have **single paragraphs over 1,000 words** that contributors cannot refactor confidently; the README is 347 lines of dense prose.

**Improvement recommendations.**
- Apply a `markdownlint` rule: **`MD013` line length ≤ 200** with explicit exceptions per file. Today some Markdown lines pass 4,000 characters.
- Split README into **`README.md` (≤80 lines)** + persona docs already exist; the README is currently a kitchen-sink.

---

## 18. Traceability — 80 / 100 (weight 3, gap·w = 60)

**Justification.** Provenance graph (`ProvenanceBuilder`/`Node`/`Edge`), citation references on aggregate explanations, decision traces (`RuleAuditTrace`/`RunEventTrace`), 78 typed audit events, golden-manifest versioning, idempotency hashing, correlation IDs end-to-end. Industry-leading.

**Improvement recommendations.**
- Surface the **provenance graph in the DOCX export** as a real graph image, not just a link.

---

## 19. Reliability — 75 / 100 (weight 2, gap·w = 50)

**Justification.** Health checks (`/live`, `/ready`, `/health`), Polly resilience, circuit-breaker metrics, Simmy chaos, k6 smoke + soak + per-tenant burst, geo-failover drill runbook, RTO/RPO targets. **But:** no published incident report, "incident communications policy" exists but no real incidents = no operating evidence.

**Improvement recommendations.**
- Stand up the **status page** described in `OPERATIONAL_TRANSPARENCY.md` against the real production probe (`api-synthetic-probe.yml`) — make the SLO real.

---

## 20. Data Consistency — 75 / 100 (weight 2, gap·w = 50)

**Justification.** ROWVERSION optimistic concurrency on covered tables, RLS, idempotency hashing, transactional outbox enqueue, `DataConsistencyOrphanProbe`, hot-path cache invalidation tied to `ROWVERSION`, read-replica staleness explicitly documented. **But:** the matrix admits some flows are "best-effort under extreme duplicate-key races" and "TTL-bound staleness if data changes outside repository methods" — a careful reviewer will pick this up.

**Improvement recommendations.**
- Add a **DB-side trigger** that asserts no caller writes to authority tables outside `IArchLucidUnitOfWork` in dev/staging; production keeps it as an alert.

---

## 21. Commercial Packaging Readiness — 75 / 100 (weight 2, gap·w = 50)

**Justification.** Pricing single source enforced via `scripts/ci/check_pricing_single_source.py`, three named tiers, locked-prices JSON fence consumed by `archlucid-ui/public/pricing.json`, Azure Marketplace SaaS offer doc. Rare maturity. **But:** the layer model is *not yet an entitlement engine* (`PRODUCT_PACKAGING.md` §4.4) — a paid customer on the Team tier can today access Enterprise Controls if their JWT roles say so. UI shaping is operational, not commercial.

**Improvement recommendations.**
- Implement **one entitlement check** server-side in `ArchLucid.Api` for `governance/*`, `policy-packs/*`, `audit/export` (the Pro/Enterprise gates) returning **`402 Payment Required`** for tier mismatches (per product owner). Until then "Enterprise Controls" is sales messaging, not a SKU.

---

## 22. Cognitive Load — 50 / 100 (weight 1, gap·w = 50)

**Justification.** Three-layer narrative helps. The README is 347 lines, `PRODUCT_PACKAGING.md` has 800+ word paragraphs, there are 378 docs, and architectural seams have explicit "do not merge" signs. New contributors will be lost.

**Improvement recommendations.** See #17. Hard limit on Markdown line length and paragraph length; a `docs/INDEX.md` table-of-contents that fits on one screen.

---

## Remaining engineering qualities (briefer)

| Quality | Score | Justification | One recommendation |
|---|---:|---|---|
| AI/Agent Readiness (w 2) | 78 | Multi-agent (Topology/Cost/Compliance/Critic), `ILlmProvider` with fallback, simulator mode, prompt-regression baseline, content safety, prompt redaction, `archlucid_agent_*` metrics. | Raise the Cost/Compliance/Critic prompt regression floors above zero. |
| Azure Compatibility / SaaS Deployment Readiness (w 2) | 78 | 99 `.tf` files across `terraform-edge`, `-private`, `-keyvault`, `-openai`, `-monitoring`, `-container-apps`, `-servicebus`, `-sql-failover`, `-storage`, `-entra`. | Publish the **end-to-end apply order** as a single script (`infra/apply-saas.ps1`) instead of `REFERENCE_SAAS_STACK_ORDER.md` prose. |
| Explainability (w 2) | 80 | `ExplainabilityTrace` per finding, `archlucid_explanation_faithfulness_ratio`, deterministic fallback, citation-bound aggregate explanations. Industry-leading. | Surface faithfulness ratio in the UI, not just Prometheus. |
| Availability (w 1) | 70 | SLO 99.5%/30d, geo-failover drill, multi-region documented (not active/active in V1). | Run the geo-failover drill against staging and publish the result. |
| Performance (w 1) | 70 | k6 smoke (p95<2000ms merge gate), per-tenant burst, soak, hot-path cache. | Add a **first-paint** k6 scenario for the operator UI. |
| Scalability (w 1) | 65 | Container Apps, Service Bus, read replicas, Redis, hot-path cache. No active/active. | Document concrete tenant/run-rate ceilings per tier. |
| Supportability (w 1) | 80 | `support-bundle --zip`, doctor, correlation IDs, 65 runbooks. | Auto-generate the bundle on health-check failure. |
| Manageability (w 1) | 70 | Many config knobs; Key Vault; `AZURE_APP_CONFIGURATION_FUTURE_ADOPTION.md`. | Adopt Azure App Configuration in production now, not "future". |
| Deployability (w 1) | 75 | Container images, Compose, Terraform per-module, CI/CD workflows (`cd.yml`, `cd-saas-greenfield.yml`). | Single `archlucid deploy --env staging` command that wraps all `terraform apply` calls. |
| Observability (w 1) | 80 | `ArchLucid` meter, OTel exporters, ActivitySource, Grafana starter dashboard, OTel collector module. | Ship the Grafana dashboards as `infra/grafana/dashboards/` JSON ready to import (they exist; document the import). |
| Testability (w 1) | 80 | 23 test assemblies, ApiFactory, GreenfieldSqlBoot, FsCheck, mutation testing, Schemathesis. | Tag every flaky test with `Category=Flaky` and quarantine. |
| Modularity (w 1) | 80 | 47 projects with NetArchTest enforcement. | Reduce to ~35 by collapsing Persistence sub-modules. |
| Extensibility (w 1) | 75 | `templates/archlucid-finding-engine/` plugin template, `ILlmProvider`, agent handler model. | Publish the plugin template to a NuGet feed; today consumers must clone. |
| Evolvability (w 1) | 70 | API versioning (`v1`, v2 guidance), DbUp migrations (001–050+), 11 ADRs, `BREAKING_CHANGES.md`. | Adopt **API contract diff CI** on `v1` (Schemathesis already loads `/openapi/v1.json`). |
| Documentation (w 1) | 75 | 378 docs, indexes, runbooks, ADRs, persona front doors. | See cognitive load #22. |
| Azure Ecosystem Fit (w 1) | 80 | Native Entra, Service Bus, OpenAI, Front Door, APIM, Container Apps, SQL, Key Vault, Application Insights. | Add **Microsoft AppSource** listing in addition to Azure Marketplace SaaS. |
| Cost-Effectiveness (w 1) | 70 | Per-tenant cost model, pilot profile, simulator mode, LLM completion caching, cost agent. | Publish a per-tier $/month per-1000-runs ceiling so buyers can budget. |

## Remaining enterprise qualities (briefer)

| Quality | Score | One-line basis | One recommendation |
|---|---:|---|---|
| Auditability (w 2) | 85 | 78 typed events, append-only `DENY UPDATE/DELETE`, CSV export, SIEM export. | Publish a **sample 30-day audit CSV** in the trust portal. |
| Policy and Governance Alignment (w 2) | 80 | Policy packs versioned, governance approvals with SoD, pre-commit gate, alert composite rules. | Ship a **starter policy pack** (Azure CIS-aligned) bundled with the trial. |
| Accessibility (w 1) | 70 | WCAG 2.1 AA target, axe-core scans on top 5 pages, jsx-a11y eslint, focus management. | Expand axe scan to all routes; today only 5 are scanned. |
| Customer Self-Sufficiency (w 1) | 75 | Strong docs, `support-bundle`, `doctor`, 65 runbooks, troubleshooting. | Ship an **in-product help drawer** that links the relevant runbook based on the page. |
| Change Impact Clarity (w 1) | 75 | Comparison runs, replay verify mode (422 with drift fields), golden manifest version diff. | Surface the diff inline on commit, not only in `/compare`. |

## Remaining commercial qualities (briefer)

| Quality | Score | One-line basis | One recommendation |
|---|---:|---|---|
| Stickiness (w 1) | 65 | Audit history, golden manifests, provenance, governance approvals = data lock-in. | Publish a **data-export** path for full account closure (counter-intuitive but increases trust → reduces churn). |
| Templates and Accelerator Richness (w 1) | 55 | `templates/archlucid-finding-engine`, `archlucid-api-endpoint`, 7 trial-email cshtml templates, DOCX templates. Limited. | Build a **starter pack** of 5 finding engines for common pain (cost-runaway, public storage, missing-tags, drift-from-baseline, secret-in-config). |

---

## Top 10 most important weaknesses (weighted)

1. **No published reference customer** — caps Marketability and triggers the −15% reference discount in the locked price stack.
2. **No SOC 2 attestation or pen-test report** — caps Trustworthiness, Compliance, and Procurement; locks the −25% trust discount. SOC 2 will be self-assessed (no auditor budget); pen test should still be commissioned.
3. **Two parallel run pipelines (Coordinator vs Authority)** — Architectural Integrity gap; ADR 0021 is *Proposed*, not executed. Owner has approved breaking the Coordinator surface in `v1`.
4. **Adoption surface is huge** — 15+ Terraform roots, dozens of config keys, 7-step wizard, "four UI shaping surfaces". First-time operators bounce.
5. **Layer model is sales messaging, not enforced entitlement** — UI shaping ≠ pricing tier. A Team-tier JWT with the right role can hit Enterprise Controls. Owner approved 402 enforcement.
6. **Local CI is not green** — `CODE_COVERAGE.md` openly notes 15 failing tests and coverage 6 points below the strict gate.
7. **No in-product business-value scorecard** — the ROI model is a doc template, not a sponsor-ready PDF generated from real run data.
8. **No deep IDE/PR/Confluence embedding** — architects review where they work; the operator UI asks them to leave.
9. **Cognitive load of docs and code is high** — README 347 lines; `PRODUCT_PACKAGING.md` has paragraphs over 1,000 words; new contributors will struggle.
10. **AI agent quality gates are real for one agent type only** — Topology has prompt-regression floors; Cost/Compliance/Critic floors are 0.0.

## Top 5 monetization blockers

1. **No published reference customer or case study** (`reference-customers/` table has only placeholders).
2. **No frictionless self-serve checkout in production** — owner has approved standing up a Stripe Checkout link for the Team tier now, in parallel with Azure Marketplace GA.
3. **No enforced entitlement layer** — paid tiers are not yet API-enforced, only UI-shaped. 402 enforcement approved.
4. **No SOC 2 / pen-test artifacts** — regulated buyers stall. Pen test still recommended; SOC 2 self-assessment is the funded path.
5. **Simulator-first first-impression** — the demo cannot show real LLM behavior without operator-supplied Azure OpenAI keys.

## Top 5 enterprise adoption blockers

1. **SOC 2 / external attestation gap** — procurement disqualifier in financial services and healthcare. Self-assessment closes part of the gap, not all.
2. **Heavy Azure-only deployment posture** — accepted as strategic per ADR 0020; AWS/GCP gap will not be closed. This is a permanent qualifier on the addressable market.
3. **Workflow embedding is shallow** — no Jira, ServiceNow, Confluence, IDE integration; architects must context-switch to a separate UI.
4. **Two pipelines (Coordinator vs Authority) confuse customer SREs** — they will ask "which one do we operate?". Resolved once ADR 0021 phase 1 lands.
5. **No enforced multi-region active/active** — `V1_SCOPE.md` explicitly excludes it.

## Top 5 engineering risks

1. **Strangler ADR 0021 has been "Proposed" too long** — every month of delay adds Coordinator-only code that must later be migrated. Now scheduled for breaking-in-`v1` retirement.
2. **Coverage gate is set higher than CI demonstrates locally** — every contributor will hit the gap; either lower the gate honestly or fix the gap.
3. **RLS and rename debt in SQL** — `Archiforge*` identifiers persist in DB objects; the rename rule waives this until a separate migration. That migration risk is unscoped.
4. **AI behavior regression coverage is single-agent** — a model swap could pass Topology gates and silently degrade Cost/Compliance.
5. **Hot-path cache invalidation depends on writes going through repositories** — `DATA_CONSISTENCY_MATRIX.md` admits "TTL-bound staleness if data changes outside these methods". A direct SQL operator action would silently serve stale data.

## Most Important Truth

> **ArchLucid is an over-built product looking for under-built proof.** The engineering discipline (47 projects, 840 test files, 99 Terraform files, NetArchTest, mutation testing, prompt-regression, ZAP, Schemathesis, RLS, fail-closed auth, citation-bound explanations) is well above what most pre-revenue products carry. The commercial proof (named customers, attestations, frictionless trial-to-revenue, working-tool embedding) is well below. **Spending the next quarter on monetization-blocker fixes — one design partner published, one pen test published, one entitlement enforcement layer (402), one Stripe checkout — will move the weighted score more than any further engineering improvement.**

---

## Top 6 best improvements (priority order)

1. **Publish the first real reference customer** end-to-end (signed agreement → drafted case study → customer-approved → marketing-published; flip CI guard to merge-blocking).
2. **Commission and publish a redacted external pen test** (one-shot spend, not recurring) using `docs/security/PEN_TEST_SOW_TEMPLATE.md` and `PEN_TEST_REDACTED_SUMMARY_TEMPLATE.md` as scaffolding. Pair with an **owner-led SOC 2 self-assessment** since auditor budget is unavailable.
3. **Execute ADR 0021 phase 1** — promote to Accepted, retire one Coordinator-only code path this sprint, document a 1-quarter glide path for the rest. Owner has approved breaking the Coordinator surface in `v1` (no `v2` bump required).
4. **Enforce tier entitlements server-side** in `ArchLucid.Api` for `governance/*`, `policy-packs/*`, `audit/export`, returning **`402 Payment Required`** for tier mismatches.
5. **Generate an in-product pilot scorecard** (`POST /v1/pilot/scorecard` → DOCX) so each pilot leaves with sponsor-ready numbers. Pair with a **Stripe Checkout link for the Team tier** so the same scorecard converts to paid without sales contact.
6. **Halve adoption friction** — one opinionated SaaS profile, three-step wizard, `archlucid doctor` red/green required-config table.

---

## Cursor prompts

Six paste-ready prompts implementing the recommendations above are kept in **[CURSOR_PROMPTS_INDEPENDENT_2026_04_20.md](CURSOR_PROMPTS_INDEPENDENT_2026_04_20.md)**.

---

## Pending questions resolved (2026-04-20)

| # | Question | Resolution |
|---|----------|------------|
| 3 | 402 Payment Required vs 403 Forbidden for tier mismatch | **402** approved |
| 4 | Stripe Checkout for Team tier now vs wait for Azure Marketplace GA | **Stripe now** (parallel to Marketplace) |
| 5 | AWS/GCP gap — strategic acceptance or deferred ambition | **Strategic acceptance** (ADR 0020 stands) |
| 6 | SOC 2 funding | **No external auditor budget**; owner will self-assess |
| 7 | Coordinator retirement — break in `v1` or version up to `v2` | **Break in `v1`** (no `v2` bump required) |

## Pending questions still open

| # | Question | Why it matters |
|---|----------|----------------|
| 1 | First reference customer details (name, tier, pilot start, reference-call cadence) | Needed to move `DESIGN_PARTNER_NEXT` from `Customer review` → `Drafting`. |
| 2 | Pen-test SoW details (assessor vendor, scope, target delivery, hosting location for redacted summary) | Needed to populate `docs/security/pen-test-summaries/2026-Q2-SOW.md` and trigger the −25% trust discount review. |
| 8 | Stripe Checkout: subscription vs one-shot, Stripe account ID, webhook endpoint for billing events | Needed before wiring `archlucid-ui/public/pricing.json` to a real checkout URL. |
