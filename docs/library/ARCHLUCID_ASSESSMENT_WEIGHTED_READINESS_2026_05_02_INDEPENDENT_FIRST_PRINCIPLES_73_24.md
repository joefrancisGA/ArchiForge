> **Scope:** Independent first-principles weighted readiness assessment of the current ArchLucid solution using the user-provided quality model; not a roadmap commitment, prior-assessment summary, or release approval.

# ArchLucid Assessment – Weighted Readiness 73.24%

## 2. Executive Summary

### Overall Readiness

ArchLucid is above the threshold for a serious V1 pilot, but below the threshold for low-friction enterprise scale-out. The strongest evidence is the coherent V1 scope boundary, SQL-backed authority state, audit and observability discipline, Azure-native deployment posture, and a clear Core Pilot path. The main weakness is not raw engineering immaturity; it is the gap between a powerful architecture workflow platform and a buyer who needs fast, obvious, low-effort proof.

Weighted score math: total weighted score is **74.70** out of **102.00** possible weighted points, producing **73.24%** readiness.

### Commercial Picture

The commercial story is credible but still heavy. The product can explain what a pilot should prove, how ROI should be measured, and how pricing maps to value. What remains weak is conversion immediacy: the buyer must understand a new category, trust the evidence shape, supply or accept baselines, and tolerate a sales-led purchasing path. Explicitly deferred items such as published reference customers and live self-serve commerce were treated as out of scope for current scoring.

### Enterprise Picture

Enterprise posture is materially stronger than the commercial posture. The solution has trust-center material, RBAC, tenant isolation, audit trails, procurement pack mechanics, accessibility gates, compliance mappings, and operational runbooks. The enterprise risk is that reviewers will still have to distinguish between self-attested evidence, owner-conducted assurance, and formal third-party attestations. Explicitly deferred third-party pen testing, SOC 2 attestation timing, PGP publication, and V1.1/V2 connectors were not scored as current defects.

### Engineering Picture

Engineering readiness is broad and serious: modular .NET projects, Dapper/SQL Server persistence, DbUp migrations, OpenTelemetry, k6, ZAP, Schemathesis, live UI E2E, accessibility scanning, mutation testing, and coverage gates are all present. The risks are complexity, dual vocabulary, real-agent quality enforcement still leaning toward warning and measurement, and operational paths that require careful configuration. The system is supportable, but not yet simple.

## 3. Weighted Quality Assessment

Qualities are ordered by **weighted deficiency signal**, calculated as `Weight × (100 - Score) / 100`. Weighted impact is `Weight × Score / 100`.

### 1. Marketability

- **Score:** 74
- **Weight:** 8
- **Weighted impact on readiness:** 5.92
- **Weighted deficiency signal:** 2.08
- **Justification:** The product has a coherent sponsor brief, pricing philosophy, buyer journey, trust center, public demo positioning, and a sharp claim: shorten the path from architecture request to reviewable architecture package. The issue is category load. Buyers must understand "architecture review package", "golden manifest", "runs", "Operate", governance evidence, AI agents, and traceability before the value becomes obvious.
- **Tradeoffs:** The richer the product, the harder it is to market simply. The V1 boundary correctly narrows the pitch to Pilot, but the repository still exposes a large Operate surface.
- **Improvement recommendations:** Make the public and in-product first impression relentlessly focused on one outcome: "get a defensible architecture review package from a sample in minutes." Move secondary terms behind progressive disclosure. Add a single comparison narrative against manual architecture review.
- **Disposition:** Fixable in V1.

### 2. Adoption Friction

- **Score:** 66
- **Weight:** 6
- **Weighted impact on readiness:** 3.96
- **Weighted deficiency signal:** 2.04
- **Justification:** The Core Pilot path is well defined, but adoption still asks users to absorb new vocabulary, choose a vertical, understand a run lifecycle, complete or bypass a seven-step wizard, review outputs, and decide when to use Operate. REST, CLI, CloudEvents, webhooks, Service Bus, and recipes are adequate for V1, but not frictionless for teams living in Jira, Confluence, and ServiceNow.
- **Tradeoffs:** Avoiding first-party enterprise connectors in V1 keeps the product shippable and preserves scope discipline, but customer-owned bridge work becomes an adoption tax.
- **Improvement recommendations:** Add a zero-config sample review path and an opinionated "second run with your data" path that avoids the full doc stack. Validate webhook bridge recipes with contract tests so customer-owned integration is less risky.
- **Disposition:** Partly fixable in V1; native ITSM connectors are explicitly V1.1+ and not scored as V1 defects.

### 3. Time-to-Value

- **Score:** 76
- **Weight:** 7
- **Weighted impact on readiness:** 5.32
- **Weighted deficiency signal:** 1.68
- **Justification:** The buyer path promises no local install, a vertical picker, a sample run, and a first finding within a short session. The Core Pilot path is four steps and intentionally excludes advanced features. The remaining issue is proof, not design: the repository documents the path, but the product must make first value feel automatic and unmissable.
- **Tradeoffs:** Simulator mode and seeded demos accelerate first value but must be clearly labeled so buyers do not mistake demo numbers for customer outcomes.
- **Improvement recommendations:** Add a measurable first-value rail that records sample start, first committed manifest, first artifact opened, and sponsor report generated. Show a one-screen "you got value" summary at the end.
- **Disposition:** Fixable in V1.

### 4. Proof-of-ROI Readiness

- **Score:** 74
- **Weight:** 5
- **Weighted impact on readiness:** 3.70
- **Weighted deficiency signal:** 1.30
- **Justification:** The ROI model is unusually explicit: baseline review cycle hours, time to committed manifest, findings, audit rows, LLM calls, evidence chain, first-value reports, pilot scorecard, and sponsor packaging are all defined. The weakness is that several ROI inputs remain buyer- or operator-supplied, and demo data must be carefully separated from real proof.
- **Tradeoffs:** A conservative ROI model is more credible than automated magic math, but it slows proof unless the UI captures baselines and confidence automatically.
- **Improvement recommendations:** Make ROI evidence completeness a hard send/no-send indicator for sponsor artifacts. Normalize metric names and API paths in scorecard material so field teams do not chase stale terms.
- **Disposition:** Fixable in V1.

### 5. Differentiability

- **Score:** 70
- **Weight:** 4
- **Weighted impact on readiness:** 2.80
- **Weighted deficiency signal:** 1.20
- **Justification:** ArchLucid is not just another diagramming or documentation tool; it combines architecture workflow, manifests, findings, evidence, governance, audit, and replay. That is differentiated. The issue is that the category still needs proof in a buyer's language, not internal architecture terms.
- **Tradeoffs:** The same breadth that differentiates the product can make it feel like an "architecture OS" pitch unless the Pilot wedge stays narrow.
- **Improvement recommendations:** Publish a concise "manual review vs ArchLucid review package" proof page using current V1 evidence surfaces and no speculative connector claims.
- **Disposition:** Fixable in V1.

### 6. Workflow Embeddedness

- **Score:** 62
- **Weight:** 3
- **Weighted impact on readiness:** 1.86
- **Weighted deficiency signal:** 1.14
- **Justification:** The system can publish events, expose APIs, support Service Bus, use webhooks, and document Power Automate style bridges. SCIM is a strong enterprise point. But the day-to-day architecture workflow still largely happens inside ArchLucid plus customer-owned bridges.
- **Tradeoffs:** V1 avoids bespoke connector scope and keeps the integration contract clean. The tradeoff is weaker embeddedness in the tools where architecture decisions are already discussed.
- **Improvement recommendations:** Treat V1 bridge reliability as a product feature: schema validation, replay examples, HMAC verification helpers, and recipe test harnesses.
- **Disposition:** Partly fixable in V1; first-party Jira, Confluence, and ServiceNow connectors are V1.1+ and not scored as V1 defects.

### 7. Correctness

- **Score:** 74
- **Weight:** 4
- **Weighted impact on readiness:** 2.96
- **Weighted deficiency signal:** 1.04
- **Justification:** Correctness is supported by schema validation, deterministic simulator mode, golden manifests, decisioning tests, OpenAPI snapshots, API integration tests, real SQL tests, and agent-output structural/semantic evaluation. The main weakness is real LLM behavior: measurement exists, but default rejection floors appear conservative/warn-oriented unless explicitly configured.
- **Tradeoffs:** Warn-first quality gates reduce false negatives during pilot learning, but they allow questionable outputs to proceed if operators do not review quality signals.
- **Improvement recommendations:** Add pilot-safe reject floors for malformed or low-evidence real-agent outputs, with deterministic fallbacks and clear operator diagnostics.
- **Disposition:** Fixable in V1.

### 8. Usability

- **Score:** 68
- **Weight:** 3
- **Weighted impact on readiness:** 2.04
- **Weighted deficiency signal:** 0.96
- **Justification:** The default Core Pilot path, progressive disclosure, accessibility scans, and operator docs show real attention to usability. The product still has a dense interface vocabulary and many surfaces. The seven-step wizard is reasonable for real input, but too much for a first impression if a sample path exists.
- **Tradeoffs:** Enterprise architecture input is inherently complex; oversimplifying risks garbage outputs. The right move is staged disclosure rather than removing necessary fields.
- **Improvement recommendations:** Add an "example first, real input second" experience and standardize architecture-review copy across the default Pilot route.
- **Disposition:** Fixable in V1.

### 9. Executive Value Visibility

- **Score:** 78
- **Weight:** 4
- **Weighted impact on readiness:** 3.12
- **Weighted deficiency signal:** 0.88
- **Justification:** The executive sponsor brief, sponsor one-pager, first-value report, value report DOCX, ROI model, and buyer-safe proof package contract create a strong executive bridge. The gap is confidence: sponsors need to know whether a report is based on live tenant evidence, tenant baselines, or demo defaults.
- **Tradeoffs:** Honest evidence-confidence labeling may reduce short-term excitement, but it increases buyer trust.
- **Improvement recommendations:** Make evidence confidence impossible to miss in sponsor artifacts and UI banners.
- **Disposition:** Fixable in V1.

### 10. Trustworthiness

- **Score:** 72
- **Weight:** 3
- **Weighted impact on readiness:** 2.16
- **Weighted deficiency signal:** 0.84
- **Justification:** Trustworthiness is supported by citations, evidence chains, audit logs, RLS, security docs, content safety, prompt redaction, and assurance posture. The main constraint is that AI narratives remain decision support, not proof; the system must keep forcing users back to persisted evidence.
- **Tradeoffs:** Strong caveats can feel less marketable, but they are necessary for enterprise realism.
- **Improvement recommendations:** Bias every explainability surface toward "claim + evidence pointer + confidence" rather than general prose.
- **Disposition:** Fixable in V1.

### 11. Architectural Integrity

- **Score:** 73
- **Weight:** 3
- **Weighted impact on readiness:** 2.19
- **Weighted deficiency signal:** 0.81
- **Justification:** The C4 maps, project separation, ADRs, authority convergence, SQL boundary, and modular projects show structural maturity. Complexity remains high: API, worker, CLI, UI, persistence families, decisioning, retrieval, graph, agent runtime, artifact synthesis, and governance all interact.
- **Tradeoffs:** A modular platform is necessary for enterprise architecture workflows, but a broad module graph increases contributor and operator load.
- **Improvement recommendations:** Keep architecture maps current and add contributor-facing "change one route/capability" checklists that prevent cross-surface drift.
- **Disposition:** Fixable in V1.

### 12. Interoperability

- **Score:** 63
- **Weight:** 2
- **Weighted impact on readiness:** 1.26
- **Weighted deficiency signal:** 0.74
- **Justification:** REST API, CLI, webhooks, CloudEvents, Service Bus, AsyncAPI, SCIM, and SIEM export give V1 a respectable integration base. The missing piece is first-party workflow integration into the systems customers already use daily; those connectors are explicitly outside V1.
- **Tradeoffs:** Protocol-first interoperability is reusable and supportable; connector-first interoperability is more immediately valuable to buyers.
- **Improvement recommendations:** Strengthen V1 bridge recipes and consumer validation instead of widening connector scope.
- **Disposition:** Partly fixable in V1; first-party ITSM/documentation connectors are V1.1+.

### 13. Decision Velocity

- **Score:** 66
- **Weight:** 2
- **Weighted impact on readiness:** 1.32
- **Weighted deficiency signal:** 0.68
- **Justification:** The buyer has a sponsor brief, pricing, order-form path, quote request path, procurement pack, and trust center. The path still requires multiple stakeholders and likely human sales follow-up. Live self-serve commerce is explicitly V1.1 and was not scored as a V1 defect.
- **Tradeoffs:** Sales-led V1 is realistic for enterprise architecture buyers. It is slower than product-led checkout but better aligned with trust and procurement needs.
- **Improvement recommendations:** Make the quote request response package automatic: sponsor summary, procurement pack link, first-run plan, and decision checklist.
- **Disposition:** Fixable in V1 for sales-led motion; self-serve commerce un-hold is V1.1.

### 14. Commercial Packaging Readiness

- **Score:** 70
- **Weight:** 2
- **Weighted impact on readiness:** 1.40
- **Weighted deficiency signal:** 0.60
- **Justification:** Tiers, pricing, feature gates, order form, trial, quote path, tenant tier filters, and trial limits are documented and partly enforced. The model is credible. The remaining risk is buyer confusion around Team list pricing versus interim Stripe bundled pricing and the fact that V1 is sales-led despite self-serve engineering rails.
- **Tradeoffs:** Keeping temporary self-serve pricing explicit avoids accidental contractual confusion but adds explanation load.
- **Improvement recommendations:** Add a commercial-readiness validator that flags contradictory pricing, placeholder checkout URLs, and tier-gate drift.
- **Disposition:** Fixable in V1.

### 15. Compliance Readiness

- **Score:** 70
- **Weight:** 2
- **Weighted impact on readiness:** 1.40
- **Weighted deficiency signal:** 0.60
- **Justification:** Compliance readiness is supported by SOC 2 self-assessment, compliance matrix, CAIQ/SIG pre-fills, DPA template, subprocessors, DSAR process, tenant isolation, and trust-center honesty. Formal attestations are not V1 commitments and were not penalized as current defects.
- **Tradeoffs:** Honest self-assessment is appropriate before revenue scale, but some enterprise buyers will still require formal attestations before production use.
- **Improvement recommendations:** Make the procurement pack distinguish "implemented control", "self-assessed", "template", and "formal attestation not claimed" at a glance.
- **Disposition:** Fixable in V1 for clarity; formal attestations are later-stage.

### 16. Procurement Readiness

- **Score:** 70
- **Weight:** 2
- **Weighted impact on readiness:** 1.40
- **Weighted deficiency signal:** 0.60
- **Justification:** The procurement pack build, manifest hashing, redaction report, trust docs, order form, DPA template, incident policy, SLA summary, and objection playbook are strong. Legal and buyer-specific artifacts still require human completion.
- **Tradeoffs:** Automation can package evidence, but it should not pretend to complete legal review or customer-specific cover letters.
- **Improvement recommendations:** Add a procurement-pack completeness report that labels missing human-only fields separately from missing product evidence.
- **Disposition:** Fixable in V1.

### 17. Maintainability

- **Score:** 70
- **Weight:** 2
- **Weighted impact on readiness:** 1.40
- **Weighted deficiency signal:** 0.60
- **Justification:** Modularity, ADRs, docs, test maps, style rules, and CI guards improve maintainability. The challenge is volume: many projects, docs, rules, and cross-surface invariants create maintenance overhead.
- **Tradeoffs:** Extensive documentation reduces institutional memory risk but can become a second system to maintain.
- **Improvement recommendations:** Add narrow contributor checklists for the most common changes: route, policy, nav item, SQL migration, finding engine, integration event.
- **Disposition:** Fixable in V1.

### 18. AI/Agent Readiness

- **Score:** 70
- **Weight:** 2
- **Weighted impact on readiness:** 1.40
- **Weighted deficiency signal:** 0.60
- **Justification:** The system has simulator mode, real executor paths, content safety, prompt redaction, LLM accounting, circuit breakers, agent traces, structural and semantic evaluation, and reference-case scoring hooks. The issue is operational posture: high-confidence agent gating must be explicit for real pilots.
- **Tradeoffs:** Strict gates improve trust but can block pilots due to model variability.
- **Improvement recommendations:** Add tiered agent quality policies: demo/simulator, pilot warn+block floor, production strict floor.
- **Disposition:** Fixable in V1.

### 19. Security

- **Score:** 80
- **Weight:** 3
- **Weighted impact on readiness:** 2.40
- **Weighted deficiency signal:** 0.60
- **Justification:** Security posture is strong for V1: fail-closed auth defaults, DevelopmentBypass production guard, RBAC, rate limiting, RLS, private endpoint posture, ZAP, Schemathesis, content safety, prompt redaction, structured logging guidance, and trust-center material. External pen testing and PGP publication are deferred and not scored as V1 defects.
- **Tradeoffs:** Allowing API key production pilots is practical, but regulated SaaS should prefer Entra/JWT with stricter startup validation.
- **Improvement recommendations:** Add a production-readiness preflight that makes unsafe auth, missing content safety, weak observability export, and public network assumptions impossible to miss.
- **Disposition:** Fixable in V1.

### 20. Reliability

- **Score:** 72
- **Weight:** 2
- **Weighted impact on readiness:** 1.44
- **Weighted deficiency signal:** 0.56
- **Justification:** Health checks, readiness, outbox patterns, circuit breakers, retry-aware trace persistence, runbooks, hosted probes, k6 smoke, and RTO/RPO docs support reliability. Production-grade evidence is still more staging/probe/runbook oriented than demonstrated under sustained real load.
- **Tradeoffs:** V1 does not need active/active guarantees, but enterprise pilots still need predictable recovery and clear failure states.
- **Improvement recommendations:** Create a release-readiness preflight report that combines health, schema, queues, auth mode, observability export, and recent smoke results.
- **Disposition:** Fixable in V1.

### 21. Data Consistency

- **Score:** 72
- **Weight:** 2
- **Weighted impact on readiness:** 1.44
- **Weighted deficiency signal:** 0.56
- **Justification:** SQL transactions, rowversion, outbox patterns, consistency matrix, archival cascades, orphan probes, quarantine, and health checks show real discipline. Residual risk remains where referential integrity is application-enforced, RLS scope columns do not cover every table, read replicas are eventually consistent, or ad-hoc SQL bypasses repositories.
- **Tradeoffs:** Application-enforced consistency is sometimes necessary during migration and modularization, but operators need clear repair and review workflows.
- **Improvement recommendations:** Add a single operator-facing data consistency dashboard/report that summarizes orphan counts, quarantine rows, replica lag assumptions, and remediation links.
- **Disposition:** Fixable in V1.

### 22. Azure Compatibility and SaaS Deployment Readiness

- **Score:** 76
- **Weight:** 2
- **Weighted impact on readiness:** 1.52
- **Weighted deficiency signal:** 0.48
- **Justification:** The solution is intentionally Azure-native: Entra, Azure SQL, Front Door/WAF, Container Apps, Key Vault, Service Bus, Application Insights/OpenTelemetry, Terraform roots, private endpoints, and no public SMB. The gap is deployment certainty: some production hostname, subscription, and organizational choices remain operator-owned.
- **Tradeoffs:** Terraform modularity supports enterprise deployment variance, but it creates more preflight burden.
- **Improvement recommendations:** Add an Azure SaaS readiness command/report that validates the chosen Terraform roots, required variables, auth mode, health endpoints, and private endpoint posture.
- **Disposition:** Fixable in V1.

### 23. Traceability

- **Score:** 84
- **Weight:** 3
- **Weighted impact on readiness:** 2.52
- **Weighted deficiency signal:** 0.48
- **Justification:** Runs, manifests, findings snapshots, persisted trace IDs, audit events, evidence chains, comparison/replay records, and artifact bundles create a strong traceability spine. The buyer-facing risk is not missing trace data; it is making trace data understandable without forcing users into internal implementation concepts.
- **Tradeoffs:** Deep traceability increases confidence and supportability, but can overwhelm first-time operators if exposed too early.
- **Improvement recommendations:** Keep traceability visible through plain-language evidence chains and review-package summaries rather than raw implementation terminology.
- **Disposition:** Healthy for V1, with usability improvements.

### 24. Explainability

- **Score:** 76
- **Weight:** 2
- **Weighted impact on readiness:** 1.52
- **Weighted deficiency signal:** 0.48
- **Justification:** Explainability has citations, trace completeness metrics, faithfulness fallback, evidence chains, persisted trace IDs, and UI links. The weakness is that explainability spans many surfaces and can still be perceived as LLM prose unless evidence pointers dominate the experience.
- **Tradeoffs:** Rich explanations help humans reason; strict evidence anchoring keeps the product from over-claiming.
- **Improvement recommendations:** Standardize every finding and aggregate explanation around claim, evidence, confidence, and next action.
- **Disposition:** Fixable in V1.

### 25. Policy and Governance Alignment

- **Score:** 78
- **Weight:** 2
- **Weighted impact on readiness:** 1.56
- **Weighted deficiency signal:** 0.44
- **Justification:** Governance approvals, policy packs, segregation of duties, pre-commit gates, SLA tracking, audit events, and governance dashboards are implemented and documented. The main concern is adoption sequencing: governance should not overwhelm the first Pilot path.
- **Tradeoffs:** Governance depth is a differentiator, but only after first value is proven.
- **Improvement recommendations:** Keep governance as an explicit Operate expansion path with one "turn this on after first review" checklist.
- **Disposition:** Fixable in V1.

### 26. Auditability

- **Score:** 82
- **Weight:** 2
- **Weighted impact on readiness:** 1.64
- **Weighted deficiency signal:** 0.36
- **Justification:** Durable append-only audit events, typed event catalog, audit UI/search/export, correlation IDs, RLS, and audit coverage matrix are strong. Some catalogued-only or endpoint-specific gaps are handled explicitly rather than hidden.
- **Tradeoffs:** Audit breadth adds storage and privacy obligations.
- **Improvement recommendations:** Keep audit coverage matrix as a required update when adding routes/events.
- **Disposition:** Healthy for V1, with ongoing maintenance.

### 27. Cognitive Load

- **Score:** 64
- **Weight:** 1
- **Weighted impact on readiness:** 0.64
- **Weighted deficiency signal:** 0.36
- **Justification:** The product has deliberately narrowed Pilot versus Operate, but the surrounding vocabulary and documentation remain dense. New users must distinguish architecture review, run, golden manifest, authority, coordinator, finding engines, policy packs, replay, and graph.
- **Tradeoffs:** Enterprise architecture is complex, and hiding every term would make support harder. The goal should be staged vocabulary.
- **Improvement recommendations:** Add a "plain-language mode" for first-session copy and help text.
- **Disposition:** Fixable in V1.

### 28. Scalability

- **Score:** 68
- **Weight:** 1
- **Weighted impact on readiness:** 0.68
- **Weighted deficiency signal:** 0.32
- **Justification:** The design includes SQL, optional read replicas, caching, worker/outbox paths, Service Bus, rate limits, and cost budgets. The missing evidence is sustained production-scale behavior across many tenants and real LLM workloads.
- **Tradeoffs:** V1 pilots do not require massive scale, but SaaS claims should remain conservative.
- **Improvement recommendations:** Add a named pilot-scale envelope: tenants, runs/day, p95, queue depth, LLM budget, and expected Azure SKU.
- **Disposition:** Better suited for V1 hardening and V1.1 scale proof.

### 29. Availability

- **Score:** 68
- **Weight:** 1
- **Weighted impact on readiness:** 0.68
- **Weighted deficiency signal:** 0.32
- **Justification:** The repo defines health checks, 99.9% target language, backup/DR docs, probes, and failover runbooks. V1 explicitly does not promise active/active multi-region product guarantees.
- **Tradeoffs:** A 99.9% target is commercially useful, but should remain pre-contractual until production evidence exists.
- **Improvement recommendations:** Tie availability claims to concrete probe history and incident response artifacts.
- **Disposition:** Partly fixable in V1; active/active is out of V1 scope.

### 30. Stickiness

- **Score:** 68
- **Weight:** 1
- **Weighted impact on readiness:** 0.68
- **Weighted deficiency signal:** 0.32
- **Justification:** Stickiness is plausible through manifests, audit history, governance workflows, policy packs, comparisons, replay, and institutional evidence. It is not yet guaranteed because V1 workflow embeddedness is limited and the first habit must be built through repeated architecture reviews.
- **Tradeoffs:** Stickiness should come from evidence history and governance adoption, not lock-in.
- **Improvement recommendations:** Add "review history compounding value" UI moments: what this tenant has learned, repeated findings, closed risks, and time saved.
- **Disposition:** Fixable in V1 and stronger in V1.1+.

### 31. Performance

- **Score:** 70
- **Weight:** 1
- **Weighted impact on readiness:** 0.70
- **Weighted deficiency signal:** 0.30
- **Justification:** k6 merge-blocking smoke, performance baselines, query latency instrumentation, hot-path cache, and LLM call metrics provide a foundation. The evidence is more smoke/baseline than production load proof.
- **Tradeoffs:** Overbuilding performance before market proof is wasteful; measuring p95 on the Core Pilot path is the right V1 target.
- **Improvement recommendations:** Publish a Core Pilot performance envelope and fail CI when the simulator path regresses outside it.
- **Disposition:** Fixable in V1.

### 32. Template and Accelerator Richness

- **Score:** 72
- **Weight:** 1
- **Weighted impact on readiness:** 0.72
- **Weighted deficiency signal:** 0.28
- **Justification:** The repository has templates, policy packs, vertical briefs, integration recipes, procurement templates, demo seeds, and operator guides. The weakness is findability and proof that these accelerators map to a first buyer outcome.
- **Tradeoffs:** More templates help field teams but can confuse users if surfaced too early.
- **Improvement recommendations:** Create a curated V1 accelerator index: first sample, second run, webhook bridge, procurement pack, policy pack.
- **Disposition:** Fixable in V1.

### 33. Customer Self-Sufficiency

- **Score:** 72
- **Weight:** 1
- **Weighted impact on readiness:** 0.72
- **Weighted deficiency signal:** 0.28
- **Justification:** Buyer-first docs, operator quickstart, troubleshooting, support bundles, runbooks, and in-product guidance support self-sufficiency. The product still likely needs a guided pilot for meaningful enterprise proof.
- **Tradeoffs:** Guided pilots are appropriate for enterprise V1, but self-service evaluation should still be smooth enough to create qualified demand.
- **Improvement recommendations:** Add a single "I am stuck" path that gathers version, correlation ID, support bundle checklist, and likely next step.
- **Disposition:** Fixable in V1.

### 34. Manageability

- **Score:** 72
- **Weight:** 1
- **Weighted impact on readiness:** 0.72
- **Weighted deficiency signal:** 0.28
- **Justification:** Configuration, RBAC, startup validation, health checks, Key Vault guidance, feature-like settings, and runbooks are strong. The weakness is the number of settings and deployment choices an operator must understand.
- **Tradeoffs:** Enterprise configurability is necessary, but V1 needs an opinionated profile.
- **Improvement recommendations:** Define and validate "Pilot", "Staging SaaS", and "Production SaaS" configuration profiles.
- **Disposition:** Fixable in V1.

### 35. Extensibility

- **Score:** 73
- **Weight:** 1
- **Weighted impact on readiness:** 0.73
- **Weighted deficiency signal:** 0.27
- **Justification:** Services, policy packs, finding engines, artifact generators, event schemas, and modular projects make extension plausible. MCP and first-party connector membranes are explicitly future scope.
- **Tradeoffs:** Extension points are safer when they map to existing application services rather than bypassing authority state.
- **Improvement recommendations:** Document one complete extension example from DTO/schema through service, tests, API, UI, and docs.
- **Disposition:** Fixable in V1 for current extension paths; MCP is V1.1.

### 36. Deployability

- **Score:** 74
- **Weight:** 1
- **Weighted impact on readiness:** 0.74
- **Weighted deficiency signal:** 0.26
- **Justification:** Docker, compose, Terraform roots, release smoke, readiness scripts, migrations, health endpoints, and Azure apply docs support deployability. Some production choices remain organization-owned.
- **Tradeoffs:** Keeping infra modular supports different enterprise subscriptions but reduces one-command certainty.
- **Improvement recommendations:** Add an opinionated "reference SaaS preflight" that checks chosen roots and reports blockers.
- **Disposition:** Fixable in V1.

### 37. Cost-Effectiveness

- **Score:** 74
- **Weight:** 1
- **Weighted impact on readiness:** 0.74
- **Weighted deficiency signal:** 0.26
- **Justification:** Simulator mode, LLM call counts, tenant monthly budget warnings/hard stops, cost models, and Azure SKU guidance reduce cost risk. Actual cost-effectiveness still depends on real tenant workload and model pricing.
- **Tradeoffs:** Conservative budgets protect margins but can cap output quality if real-mode runs need more model work.
- **Improvement recommendations:** Show per-run estimated LLM cost and budget status directly in first-value reports.
- **Disposition:** Fixable in V1.

### 38. Evolvability

- **Score:** 76
- **Weight:** 1
- **Weighted impact on readiness:** 0.76
- **Weighted deficiency signal:** 0.24
- **Justification:** ADRs, scope contracts, deferral docs, modular projects, test gates, and backlog boundaries make evolution manageable. The risk is that many future-facing documents can look like current commitments unless kept disciplined.
- **Tradeoffs:** Explicit deferral prevents scope creep but requires continuous messaging hygiene.
- **Improvement recommendations:** Keep V1 scope, deferred scope, and product packaging in lockstep whenever a capability changes.
- **Disposition:** Healthy for V1.

### 39. Change Impact Clarity

- **Score:** 78
- **Weight:** 1
- **Weighted impact on readiness:** 0.78
- **Weighted deficiency signal:** 0.22
- **Justification:** Comparison, replay, manifest deltas, architecture graph, provenance, and traceability artifacts make change impact more visible than manual review packages. The buyer must still learn how to read those surfaces.
- **Tradeoffs:** Deep change analysis belongs after first pilot proof, not in the first-session path.
- **Improvement recommendations:** Add one example before/after change story using a sample architecture review.
- **Disposition:** Fixable in V1.

### 40. Testability

- **Score:** 79
- **Weight:** 1
- **Weighted impact on readiness:** 0.79
- **Weighted deficiency signal:** 0.21
- **Justification:** The test system is broad: xUnit tiers, SQL integration, API factories, OpenAPI snapshots, live API Playwright, Vitest, axe, k6, ZAP, Schemathesis, mutation testing, and coverage gates. The weakness is operational cost and occasional local/CI parity complexity.
- **Tradeoffs:** Full fidelity tests are expensive but necessary for this product class.
- **Improvement recommendations:** Keep a fast, trustworthy "changed area" map so contributors know which focused tests prove their edit.
- **Disposition:** Healthy for V1.

### 41. Supportability

- **Score:** 80
- **Weight:** 1
- **Weighted impact on readiness:** 0.80
- **Weighted deficiency signal:** 0.20
- **Justification:** Correlation IDs, trace IDs, support bundles, doctor command, health endpoints, troubleshooting docs, and runbooks are strong. The remaining risk is training support teams to interpret a complex architecture workflow.
- **Tradeoffs:** Rich diagnostics are useful only if the operator can collect and interpret them quickly.
- **Improvement recommendations:** Add a Tier 1 "collect these five facts" in-product support panel.
- **Disposition:** Healthy for V1, with training.

### 42. Accessibility

- **Score:** 80
- **Weight:** 1
- **Weighted impact on readiness:** 0.80
- **Weighted deficiency signal:** 0.20
- **Justification:** WCAG 2.1 AA target, live API axe scans across many routes, component axe tests, jsx-a11y, skip links, labels, focus styling, and annual review cadence show a strong baseline. Manual screen-reader and keyboard validation remains necessary because automated tools catch only part of WCAG.
- **Tradeoffs:** Gating critical/serious violations is practical; full manual accessibility review is more expensive.
- **Improvement recommendations:** Add a lightweight manual keyboard/screen-reader checklist to release candidate drills.
- **Disposition:** Healthy for V1.

### 43. Modularity

- **Score:** 82
- **Weight:** 1
- **Weighted impact on readiness:** 0.82
- **Weighted deficiency signal:** 0.18
- **Justification:** API, application, persistence, decisioning, graph, retrieval, artifact synthesis, agent runtime, simulator, contracts, worker, UI, and CLI are cleanly named and documented as separate containers/libraries.
- **Tradeoffs:** Many modules improve boundaries but require strong DI, tests, and contributor maps.
- **Improvement recommendations:** Keep enforcing architecture tests and update project maps when dependencies shift.
- **Disposition:** Healthy for V1.

### 44. Observability

- **Score:** 83
- **Weight:** 1
- **Weighted impact on readiness:** 0.83
- **Weighted deficiency signal:** 0.17
- **Justification:** The solution has custom metrics, traces, trace IDs on runs, dashboards, Prometheus alerts, Application Insights/OTLP/Prometheus export paths, and runbooks. The known limitation is source-specific always-sampling in-process.
- **Tradeoffs:** Head-based sampling controls cost but may drop interesting traces without collector tail sampling.
- **Improvement recommendations:** Make the collector/tail-sampling recommendation part of production preflight.
- **Disposition:** Healthy for V1.

### 45. Azure Ecosystem Fit

- **Score:** 84
- **Weight:** 1
- **Weighted impact on readiness:** 0.84
- **Weighted deficiency signal:** 0.16
- **Justification:** The solution aligns strongly with Azure-native identity, storage, networking, SaaS deployment, Application Insights/OpenTelemetry, Azure SQL, Key Vault, Service Bus, Front Door/WAF, Container Apps, and Terraform.
- **Tradeoffs:** Azure-first posture is a strength for Microsoft-centric enterprises but may reduce fit for buyers standardized elsewhere.
- **Improvement recommendations:** Keep Azure as the reference platform and avoid diluting the story with unimplemented multi-cloud claims.
- **Disposition:** Healthy for V1.

### 46. Documentation

- **Score:** 88
- **Weight:** 1
- **Weighted impact on readiness:** 0.88
- **Weighted deficiency signal:** 0.12
- **Justification:** Documentation is one of the solution's strongest assets: architecture, security, deployment, tests, runbooks, buyer material, procurement, V1 scope, and deferred scope are extensive. The risk is not absence; it is volume and navigation.
- **Tradeoffs:** Deep docs are valuable for enterprise review but can overwhelm first-time readers.
- **Improvement recommendations:** Keep the five-document spine strict and treat long-form docs as reference, not onboarding.
- **Disposition:** Healthy for V1.

## 4. Top 10 Most Important Weaknesses

1. **The first value moment is still too easy to miss.** The path is documented, but the product should make the first review package feel automatic and unmistakable.
2. **The buyer story still carries too much internal vocabulary.** "Run", "golden manifest", "authority", and related terms are support metadata, not the buyer's reason to care.
3. **ROI proof depends on baseline confidence.** The model is good, but sponsor artifacts must clearly separate tenant-supplied baselines, conservative defaults, and demo data.
4. **Workflow integration is protocol-first, not tool-native.** REST, CLI, CloudEvents, webhooks, and Service Bus are viable, but customer-owned bridges add implementation friction.
5. **Agent quality enforcement is not yet as strong as agent quality measurement.** Structural and semantic metrics exist; real pilot policy should define when low-quality output blocks.
6. **Enterprise trust evidence is honest but not formal.** Self-assessment, owner-conducted testing, and templates are useful, but they are not SOC 2 Type II or external pen-test evidence.
7. **The product surface is broad enough to confuse first-time operators.** Progressive disclosure helps, but the docs and UI still expose a large conceptual model.
8. **Production readiness is spread across many artifacts.** Health, auth, content safety, observability, Terraform, billing, and private networking need a single preflight view.
9. **Data consistency repair is more operator-runbook than operator-product.** Detection and quarantine exist, but remediation needs a clear productized flow.
10. **Commercial buying still requires human orchestration.** That is acceptable for V1, but quote-to-close can still stall without automatic follow-up evidence packages.

## 5. Top 5 Monetization Blockers

1. **Value proof is not yet self-evident from the first session.** Buyers pay when the first artifact makes the manual alternative look obviously worse.
2. **Baseline-dependent ROI can slow urgency.** If the buyer does not have current review-cycle data, the business case becomes lower-confidence.
3. **The sales-led path must compensate for deferred self-serve commerce.** Live checkout and marketplace publication are out of V1 scope, so quote follow-up must be excellent.
4. **The trust discount is still commercially real.** Formal attestations are not current V1 commitments, but procurement-heavy buyers will price that risk.
5. **The category needs sharper competitive contrast.** The product must clearly explain why it is not just documentation, diagramming, governance workflow, or generic AI analysis.

## 6. Top 5 Enterprise Adoption Blockers

1. **Native workflow embeddedness is limited in V1.** Enterprise users live in ITSM, documentation, and portfolio tools; V1 relies on protocols and recipes.
2. **Security/procurement evidence is self-attested in key areas.** This is acceptable for pilots but may block production rollout in stricter organizations.
3. **Configuration and deployment choices are numerous.** Enterprise operators need a validated profile rather than a pile of options.
4. **AI output trust requires disciplined review.** The product must keep persisted evidence above model prose in every workflow.
5. **Support teams must understand a complex domain model.** Incidents involve runs, manifests, artifacts, traces, findings, governance, and tenant scope.

## 7. Top 5 Engineering Risks

1. **Real-agent output quality could fall below reviewer trust thresholds without blocking.**
2. **Application-enforced consistency and partial RLS coverage require disciplined repository usage and operational probes.**
3. **Configuration drift across auth, tier gates, UI shaping, and API policies could expose inconsistent buyer experiences.**
4. **Observability may miss high-value traces in production if head-based sampling drops the wrong requests.**
5. **The broad module and documentation surface can slow safe changes unless contributor checklists stay current.**

## 8. Most Important Truth

ArchLucid is strong enough for a serious V1 pilot, but it will win or lose commercially on how quickly a buyer sees one defensible architecture review package and believes it is better than their manual process.

## 9. Top Improvement Opportunities

No top improvement below is deferred; each can be executed without new user input. Items explicitly deferred to V1.1 or V2 were not used as current-readiness penalties.

### 1. Zero-Config First Review Package

- **Why it matters:** This directly attacks the highest-weight weaknesses: marketability, time-to-value, adoption friction, and proof.
- **Expected impact:** A buyer should reach a committed sample architecture review, findings, artifacts, and sponsor-ready summary without understanding the full workflow first.
- **Affected qualities:** Marketability, Time-to-Value, Adoption Friction, Usability, Cognitive Load, Executive Value Visibility.
- **Status:** Fully actionable now.
- **Impact of running the prompt:** Directly improves Time-to-Value (+6-9 pts), Adoption Friction (+5-8 pts), Marketability (+3-5 pts), Usability (+3-5 pts). Weighted readiness impact: **+0.9-1.5%**.

```text
Implement a zero-config "First Review Package" path for ArchLucid V1.

Goal:
Let a buyer/operator start from the default hosted/operator entry, run or open a seeded sample architecture review, commit/finalize it if needed, and land on a concise review package summary with findings, artifacts, evidence confidence, and a sponsor-report CTA.

Start by inspecting:
- docs/CORE_PILOT.md
- docs/BUYER_FIRST_30_MINUTES.md
- docs/library/PRODUCT_PACKAGING.md
- archlucid-ui/src/app routes for Home, Onboarding, New run, Runs, and run detail
- ArchLucid.Api demo/preview, pilot report, architecture request, execute, commit, and artifact endpoints
- existing demo seed and Contoso demo identifiers

Implement the smallest safe V1 slice:
- Add or refine a prominent "Start with sample review" CTA on the default Pilot surface.
- Reuse existing demo/sample seed infrastructure if present; do not invent a second sample data model.
- If a committed sample already exists, route directly to the review package summary.
- If a sample must be created/executed/committed, call existing APIs in the same sequence as the Core Pilot path.
- Add a final first-value summary panel: review identity, top findings count by severity, artifact count, evidence confidence, demo-data warning, and sponsor report CTA where available.
- Track a server-side or client-side first-value milestone using existing diagnostics/metrics patterns if there is already a suitable endpoint; otherwise add a minimal, rate-limited diagnostics event following the Core Pilot rail pattern.

Acceptance criteria:
- A new user can reach the sample review package from the default Pilot path without filling the seven-step wizard.
- Demo/sample output is clearly labeled as demo data and cannot be mistaken for customer ROI evidence.
- Existing Core Pilot APIs remain authoritative; no duplicate run lifecycle logic is introduced.
- Existing route authorization and commercial tier filters are not weakened.
- Add focused unit/component tests for the CTA and summary panel.
- Add or update one live/mock e2e path that verifies the sample first-value path reaches a review package surface.
- Update docs/CORE_PILOT.md and docs/BUYER_FIRST_30_MINUTES.md only where needed to describe the new path.

Constraints:
- Do not change REST route names, database schema, or pricing/commercial entitlement rules unless a failing test proves it is necessary.
- Do not remove the seven-step real-input wizard; this is an example-first path, not a replacement.
- Do not claim customer ROI from demo data.
```

### 2. Evidence-Confidence Gate for Sponsor Artifacts

- **Why it matters:** Sponsor artifacts drive purchase decisions. They must clearly show whether proof is based on live tenant data, supplied baselines, defaults, or demo records.
- **Expected impact:** Prevents over-claiming and increases trust with executives, security, and procurement.
- **Affected qualities:** Proof-of-ROI Readiness, Executive Value Visibility, Trustworthiness, Procurement Readiness, Marketability.
- **Status:** Fully actionable now.
- **Impact of running the prompt:** Directly improves Proof-of-ROI Readiness (+5-8 pts), Executive Value Visibility (+4-6 pts), Trustworthiness (+3-5 pts), Procurement Readiness (+2-4 pts). Weighted readiness impact: **+0.5-0.9%**.

```text
Add an evidence-confidence gate to ArchLucid sponsor-facing artifacts and CTAs.

Goal:
Every sponsor-facing first-value report, PDF/DOCX value report, sponsor banner, and buyer-safe proof package should clearly state whether its claims use live tenant evidence, tenant-supplied baselines, conservative defaults, or demo data.

Start by inspecting:
- docs/library/PILOT_ROI_MODEL.md
- docs/EXECUTIVE_SPONSOR_BRIEF.md
- docs/go-to-market/PILOT_SUCCESS_SCORECARD.md
- ArchLucid.Application pilot/value report formatters and renderers
- ArchLucid.Api pilot report endpoints
- archlucid-ui run detail sponsor banner/value report surfaces

Implement:
- Reuse existing ROI evidence completeness concepts if present.
- Add a single evidence-confidence model if one does not already exist, with values equivalent to Strong, Partial, Low, and DemoOnly.
- Render the evidence-confidence state consistently in Markdown, PDF/DOCX, and UI sponsor CTAs.
- Disable or warn on "send/share sponsor artifact" UI actions when evidence is DemoOnly or materially incomplete, while still allowing operators to download clearly watermarked demo artifacts.
- Include missing evidence reasons: no tenant baseline, demo tenant, no committed manifest, no findings, no artifact package, no evidence chain.

Acceptance criteria:
- Sponsor artifacts visibly include evidence confidence.
- Demo records render a non-negotiable demo warning.
- Tests cover Strong, Partial, Low, and DemoOnly states.
- No existing pilot report endpoint loses backward-compatible response fields unless tests and docs are updated.
- Documentation explains the gate in buyer-safe language.

Constraints:
- Do not invent ROI numbers.
- Do not require a published customer reference.
- Do not change pricing or order-form terms.
```

### 3. Pilot Agent Quality Enforcement Profile

- **Why it matters:** Agent output correctness is central to product trust. Measurement alone is insufficient if low-quality outputs can still become review packages.
- **Expected impact:** Converts agent quality from passive telemetry into an explicit pilot safety control.
- **Affected qualities:** Correctness, AI/Agent Readiness, Trustworthiness, Explainability, Reliability.
- **Status:** Fully actionable now.
- **Impact of running the prompt:** Directly improves Correctness (+4-7 pts), AI/Agent Readiness (+6-8 pts), Trustworthiness (+3-5 pts), Explainability (+2-4 pts). Weighted readiness impact: **+0.5-0.9%**.

```text
Implement a V1 pilot agent-output quality enforcement profile.

Goal:
Keep simulator/demo paths deterministic, but require real-agent pilot outputs to meet configurable structural and semantic quality floors before they are treated as accepted review evidence.

Start by inspecting:
- docs/library/AGENT_OUTPUT_EVALUATION.md
- docs/library/OBSERVABILITY.md
- ArchLucid.AgentRuntime and ArchLucid.Application agent output evaluation/recording code
- configuration defaults in appsettings*.json
- tests around AgentOutputEvaluationRecorder, AgentResult schema validation, and architecture execute orchestration

Implement:
- Add a named configuration profile such as ArchLucid:AgentOutput:QualityGate:Profile with values Demo, PilotWarn, PilotEnforce, ProductionStrict.
- Keep existing defaults safe and backwards compatible for simulator/demo.
- For PilotEnforce, reject or hold real-agent outputs below configured structural/semantic floors and surface a clear operator error with correlation/run IDs.
- Emit existing quality gate metrics with accepted/warned/rejected outcomes.
- Ensure low-quality rejection does not corrupt persisted run state or produce a misleading committed manifest.

Acceptance criteria:
- Unit tests cover each profile and threshold behavior.
- Integration or application tests prove low-quality real-agent output cannot silently commit as normal review evidence under PilotEnforce.
- Existing simulator tests remain deterministic.
- Docs explain recommended profiles for local demo, guided pilot, and production-like environments.

Constraints:
- Do not add a new LLM provider.
- Do not remove warn-only support; it remains useful for development.
- Do not persist full prompts unless existing trace-storage settings permit it.
```

### 4. V1 Bridge Recipe Contract Harness

- **Why it matters:** V1 intentionally uses customer-owned bridges for ITSM/documentation workflows. Those bridges must be easy to validate or adoption friction remains high.
- **Expected impact:** Makes protocol-first interoperability feel safer without pulling deferred first-party connectors into V1.
- **Affected qualities:** Adoption Friction, Workflow Embeddedness, Interoperability, Enterprise Adoption, Supportability.
- **Status:** Fully actionable now.
- **Impact of running the prompt:** Directly improves Workflow Embeddedness (+5-7 pts), Interoperability (+5-8 pts), Adoption Friction (+3-5 pts), Supportability (+2-3 pts). Weighted readiness impact: **+0.4-0.8%**.

```text
Build a V1 bridge recipe contract harness for ArchLucid integration events and webhook recipes.

Goal:
Let customers and field engineers validate ServiceNow/Jira-style Power Automate or webhook bridges against the same CloudEvents/HMAC/schema contract that ArchLucid emits, without building first-party connectors in V1.

Start by inspecting:
- docs/library/INTEGRATION_EVENTS_AND_WEBHOOKS.md
- docs/library/ITSM_BRIDGE_V1_RECIPES.md
- docs/integrations/recipes/README.md
- schemas/integration-events/
- docs/contracts/archlucid-asyncapi-2.6.yaml
- existing integration event outbox/publisher tests

Implement:
- Add a small CLI or script command that validates a captured webhook payload against the integration event schema catalog.
- Validate CloudEvents fields, event type, schema version, HMAC/signature expectations, tenant/workspace/project scope fields, and correlation IDs.
- Include sample payloads for finding-created or governance/escalation events using existing schemas.
- Add tests for valid payload, invalid signature, unknown event type, schema mismatch, and missing correlation/scope fields.
- Update the V1 bridge recipe docs to instruct customers how to run the validator before connecting to ServiceNow/Jira/Confluence automations.

Acceptance criteria:
- The harness runs locally without external ServiceNow/Jira credentials.
- It validates current schemas rather than duplicating event contracts.
- CI covers the validator logic.
- Docs clearly state this is V1 customer-owned bridge validation, not a first-party connector.

Constraints:
- Do not implement first-party Jira, Confluence, or ServiceNow connectors.
- Do not add new event schemas unless an existing recipe cannot be represented.
- Do not weaken HMAC/security guidance.
```

### 5. Production Readiness Preflight Command

- **Why it matters:** Production-like readiness is distributed across docs and settings. A single preflight reduces deployment mistakes and enterprise reviewer anxiety.
- **Expected impact:** Improves deployability, reliability, security, manageability, and Azure SaaS readiness.
- **Affected qualities:** Security, Reliability, Deployability, Manageability, Azure Compatibility and SaaS Deployment Readiness, Supportability.
- **Status:** Fully actionable now.
- **Impact of running the prompt:** Directly improves Deployability (+5-7 pts), Manageability (+4-6 pts), Security (+2-4 pts), Reliability (+3-5 pts), Azure SaaS Readiness (+3-5 pts). Weighted readiness impact: **+0.4-0.8%**.

```text
Add an ArchLucid production-readiness preflight command and report.

Goal:
Provide one operator command that checks whether a deployment is safe enough for a production-like pilot or SaaS environment.

Start by inspecting:
- ArchLucid.Cli doctor/support-bundle commands
- ArchLucid.Host.Core startup/configuration validation
- docs/library/SECURITY.md
- docs/library/OBSERVABILITY.md
- docs/library/REFERENCE_SAAS_STACK_ORDER.md
- docs/library/V1_RELEASE_CHECKLIST.md
- docs/library/PRODUCT_PACKAGING.md

Implement:
- Add a CLI command such as archlucid readiness preflight or extend doctor with a production-readiness mode.
- Check API base URL, /health/live, /health/ready, /version, auth mode, DevelopmentBypass safety, API key/JWT posture, content safety requirements, SQL storage provider, migration readiness, observability exporter configuration, trace viewer URL template, CORS origins, rate limiting, and commercial/billing placeholder status where detectable.
- Emit a human-readable Markdown report and a JSON report for CI.
- Categorize findings as Blocker, Warning, Info, and DeferredScope.
- Include explicit checks for no public SMB/445 assumptions where infrastructure metadata is available; otherwise emit a manual verification item.

Acceptance criteria:
- Command succeeds against a healthy local dev deployment with appropriate dev warnings.
- Command returns non-zero when production-like hosts use DevelopmentBypass or lack required content safety configuration.
- Tests cover classification and JSON output.
- Docs show how to attach the report to a pilot handoff.

Constraints:
- Do not require Azure credentials for the base command.
- Do not mutate infrastructure.
- Do not hide warnings for unknown configuration; unknowns should be explicit.
```

### 6. Data Consistency Operator Report

- **Why it matters:** The system has data consistency probes and quarantine, but operators need one productized view of current state and remediation next steps.
- **Expected impact:** Reduces integrity risk and support time during pilots.
- **Affected qualities:** Data Consistency, Reliability, Supportability, Trustworthiness, Auditability.
- **Status:** Fully actionable now.
- **Impact of running the prompt:** Directly improves Data Consistency (+6-9 pts), Reliability (+2-4 pts), Supportability (+3-5 pts), Trustworthiness (+2-3 pts). Weighted readiness impact: **+0.3-0.6%**.

```text
Create a data consistency operator report for ArchLucid.

Goal:
Expose the existing consistency guarantees, orphan probe results, quarantine counts, and remediation guidance in one read-only operator/reporting surface.

Start by inspecting:
- docs/library/DATA_CONSISTENCY_MATRIX.md
- docs/security/MULTI_TENANT_RLS.md
- DataConsistencyOrphanProbeHostedService
- DataConsistencyQuarantine SQL migration/table/repository code
- existing health checks and diagnostics controllers
- operator UI diagnostics/admin surfaces

Implement:
- Add a read-only API endpoint or CLI report that summarizes current data consistency state for SQL-backed deployments.
- Include orphan counts by table/column, quarantine counts, last probe time if available, health check state, read-replica enabled/disabled state, and configuration mode.
- Link each finding to remediation documentation, not automatic destructive cleanup.
- Add an operator UI card only if there is an existing diagnostics/admin pattern; otherwise keep the first slice CLI/API + docs.

Acceptance criteria:
- No destructive repair is performed by this report.
- Tests cover empty/healthy state, orphan counts, quarantine counts, and InMemory/not-applicable state.
- Report output includes tenant/workspace/project scope where safe and available.
- Documentation explains when to escalate and what not to delete manually.

Constraints:
- Do not bypass RLS except through existing approved diagnostic patterns.
- Do not expose tenant data across scopes.
- Do not add broad admin mutation endpoints.
```

### 7. Plain-Language Pilot Copy Pass

- **Why it matters:** The product is too easy to explain in internal terms. First-session copy should talk about architecture reviews, evidence, findings, and packages.
- **Expected impact:** Lowers cognitive load and improves marketability without architectural risk.
- **Affected qualities:** Cognitive Load, Usability, Marketability, Adoption Friction, Executive Value Visibility.
- **Status:** Fully actionable now.
- **Impact of running the prompt:** Directly improves Cognitive Load (+6-10 pts), Usability (+3-5 pts), Marketability (+2-4 pts), Adoption Friction (+2-4 pts). Weighted readiness impact: **+0.3-0.7%**.

```text
Run a plain-language Pilot copy pass across ArchLucid's default buyer/operator path.

Goal:
Make the first-session product language emphasize "architecture review", "findings", "evidence", and "review package", while keeping "run" visible as technical support metadata.

Start by inspecting:
- docs/library/PRODUCT_PACKAGING.md buyer vocabulary decision
- docs/CORE_PILOT.md
- docs/BUYER_FIRST_30_MINUTES.md
- archlucid-ui default Pilot routes: Home, Onboarding, New run, Runs, Run detail, artifact/review package surfaces
- layer-guidance.ts and nav-config.ts

Implement:
- Update visible headings, helper text, empty states, CTAs, and first-session guidance on the default Pilot path.
- Keep API names, DTOs, database entities, route paths, and support metadata unchanged.
- Add one bridge sentence where needed: each architecture review is tracked as a run for support, logs, and API calls.
- Keep Operate features positioned as optional follow-on paths after the first review package.
- Add or update component tests where copy is intentionally locked.

Acceptance criteria:
- A first-time buyer can understand the default flow without knowing "golden manifest" or "authority pipeline" up front.
- Run ID remains visible where support needs it.
- No REST paths, database names, or generated clients are renamed.
- Docs and UI copy remain aligned.

Constraints:
- Do not perform a broad product rename.
- Do not change authorization, tiering, or navigation logic except copy labels where tests support it.
- Do not remove technical terms from developer docs where precision is required.
```

### 8. Pilot Scorecard/API Metric Alignment

- **Why it matters:** Several field-facing metrics need to map exactly to current endpoints, OTel names, and report fields. Stale metric names reduce trust.
- **Expected impact:** Makes ROI and pilot success measurement easier to run without engineering interpretation.
- **Affected qualities:** Proof-of-ROI Readiness, Documentation, Customer Self-Sufficiency, Decision Velocity, Supportability.
- **Status:** Fully actionable now.
- **Impact of running the prompt:** Directly improves Proof-of-ROI Readiness (+3-5 pts), Customer Self-Sufficiency (+3-5 pts), Decision Velocity (+2-3 pts), Supportability (+2-3 pts). Weighted readiness impact: **+0.2-0.5%**.

```text
Align ArchLucid pilot scorecard metrics with current API routes, OTel instruments, and report fields.

Goal:
Ensure docs used by sales engineers and pilot champions reference current V1 route names, metric names, data locations, and report outputs.

Start by inspecting:
- docs/go-to-market/PILOT_SUCCESS_SCORECARD.md
- docs/library/PILOT_ROI_MODEL.md
- docs/library/OBSERVABILITY.md
- docs/library/API_CONTRACTS.md
- current OpenAPI snapshot if present
- ArchLucid.Api pilot/report endpoints

Implement:
- Replace stale /v1.0 route references with current /v1 paths where applicable.
- Replace stale metric names with the actual instruments documented in OBSERVABILITY.md or implemented in ArchLucidInstrumentation.
- Add a "Metric source of truth" table mapping each scorecard item to API endpoint, SQL concept, report field, or OTel instrument.
- Mark operator-filled qualitative fields separately from system-computed fields.
- Add a short "do not invent missing pilot data" warning aligned with the ROI model.

Acceptance criteria:
- Every quantitative metric in the scorecard has a current source or is explicitly labeled operator-filled.
- No scorecard metric references a nonexistent route or metric.
- Links to API contracts and observability docs resolve.
- Documentation remains buyer-safe and does not expose internal-only implementation detail unnecessarily.

Constraints:
- Do not change production API routes just to match docs.
- Do not add fake metrics.
- Do not alter pricing assumptions.
```

## 10. Pending Questions for Later

No pending question blocks the eight actionable improvements above. The following questions are decision-shaping for later work, but they should not interrupt the current V1 improvement path.

### Zero-Config First Review Package

- Should the hosted buyer path always create a fresh sample per tenant, or reuse a canonical read-only sample until the user starts a real second run?

### Evidence-Confidence Gate for Sponsor Artifacts

- What level of evidence confidence should be allowed for external sponsor emails by default: Partial or Strong only?

### Pilot Agent Quality Enforcement Profile

- What default semantic and structural floors should apply to paid pilots using real LLM execution?

### Production Readiness Preflight Command

- Which production-like profile should be considered the minimum for an enterprise pilot: API key allowed, or Entra/JWT required?

### Data Consistency Operator Report

- Should remediation remain runbook-only in V1, or should V1 include narrow admin repair actions after report-only mode stabilizes?
