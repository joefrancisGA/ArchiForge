# ArchLucid Assessment – Weighted Readiness 73.25%

## Executive Summary

**Overall Readiness:** ArchLucid is a structurally sound, highly modular architecture decisioning platform with strong engineering fundamentals. The 73.25% readiness score reflects a system that is technically capable but requires commercial and enterprise polish to accelerate adoption and demonstrate executive value.

**Commercial Picture:** The product has a solid foundation for pilot execution, but struggles with Executive Value Visibility and Proof-of-ROI. The lack of built-in cross-tenant analytics and executive dashboards means the value is obvious to operators but harder to prove to buyers.

**Enterprise Picture:** The system has excellent auditability and traceability, with robust RBAC and RLS. However, Workflow Embeddedness and Interoperability are current weaknesses, as the system relies heavily on its own UI and CLI rather than meeting users where they already work (e.g., deeper ITSM integration beyond the V1 baseline).

**Engineering Picture:** Engineering is the strongest pillar. The architecture is highly modular, testability is exceptional, and observability is well-instrumented. Residual risks cluster around **hosted runtime economics and behaviour clarity** (LLM usage, explainability), **build/CI and release wall-clock** from a large solution graph and deep test matrix, and **operational surface area**—not day-to-day navigation across assemblies when development is consistently AI-assisted (for example with Cursor).

## Weighted Quality Assessment

### Marketability
- **Score:** 70
- **Weight:** 8
- **Weighted deficiency:** 2.40
- **Justification:** The system has strong technical merits but lacks clear, executive-facing value propositions and dashboards.
- **Tradeoffs:** Balancing deep technical features for operators vs. high-level summaries for buyers.
- **Improvement Recommendations:** Implement executive ROI dashboards to clearly demonstrate value.

### Adoption Friction
- **Score:** 70
- **Weight:** 6
- **Weighted deficiency:** 1.80
- **Justification:** Initial setup requires significant context and understanding of the architecture lifecycle (runs, manifests, governance).
- **Tradeoffs:** Comprehensive governance features inherently add friction to initial onboarding.
- **Improvement Recommendations:** Provide industry-specific accelerator templates to reduce the blank-page problem.

### Time-to-Value
- **Score:** 75
- **Weight:** 7
- **Weighted deficiency:** 1.75
- **Justification:** The pilot path is well-defined, but getting to the first meaningful architectural decision requires setup and configuration.
- **Tradeoffs:** A multi-tenant SaaS with governance, identity, and optional enterprise networking still has a longer time-to-first meaningful outcome than a lightweight, single-purpose tool.
- **Improvement Recommendations:** **Addressed for evaluation (2026-05-05):** `archlucid new <projectName> --quickstart` provisions local quickstart artifacts; optional further simplification remains (e.g. tighter host onboarding).

### Proof-of-ROI Readiness
- **Score:** 65
- **Weight:** 5
- **Weighted deficiency:** 1.75
- **Justification:** No automated way to show time or money saved by using the platform.
- **Tradeoffs:** ROI calculation is highly context-dependent and difficult to generalize.
- **Improvement Recommendations:** Add cost-tracking telemetry and ROI estimation to the dashboard.

### Executive Value Visibility
- **Score:** 60
- **Weight:** 4
- **Weighted deficiency:** 1.60
- **Justification:** The UI is heavily operator-focused, lacking views tailored for executives or non-technical stakeholders.
- **Tradeoffs:** Prioritizing operator workflows over executive reporting in V1.
- **Improvement Recommendations:** Create a dedicated executive summary view aggregating compliance and cost metrics.

### Differentiability
- **Score:** 70
- **Weight:** 4
- **Weighted deficiency:** 1.20
- **Justification:** Unique approach to architecture decisioning, but the value proposition can be hard to articulate against generic LLM tools.
- **Tradeoffs:** Building a specialized tool vs. a generic platform.
- **Improvement Recommendations:** Highlight the deterministic governance and auditability features in marketing materials.

### Workflow Embeddedness
- **Score:** 65
- **Weight:** 3
- **Weighted deficiency:** 1.05
- **Justification:** Relies heavily on its own UI/CLI. **ServiceNow** and **Jira** first-party connectors are in scope for V1, but **deep** workflow fit (CMDB-aligned signal, operational handoff) needs **ServiceNow-first** execution; IDE plugins and Slack remain deferred.
- **Tradeoffs:** Building a standalone platform vs. integrating into existing fragmented toolchains.
- **Improvement Recommendations:** Deepen **ServiceNow** integration first (incident + planned **CMDB** mapping); treat advanced **Jira** bi-directional polish as secondary until ServiceNow sequencing is complete.

### Usability
- **Score:** 70
- **Weight:** 3
- **Weighted deficiency:** 0.90
- **Justification:** The operator UI is functional but introduces many new concepts that require learning.
- **Tradeoffs:** Exposing complex governance features vs. keeping the UI simple.
- **Improvement Recommendations:** Improve error messages and provide contextual help within the UI.

### Correctness
- **Score:** 80
- **Weight:** 4
- **Weighted deficiency:** 0.80
- **Justification:** Strong test coverage, property-based testing, and deterministic simulator ensure high correctness.
- **Tradeoffs:** High maintenance cost for the extensive test suites.
- **Improvement Recommendations:** Continue expanding the golden corpus regression tests.

### Security
- **Score:** 75
- **Weight:** 3
- **Weighted deficiency:** 0.75
- **Justification:** Robust RBAC, RLS, and private endpoint support. However, third-party pen testing is deferred to V2.
- **Tradeoffs:** Relying on owner-conducted testing for V1 to accelerate release.
- **Improvement Recommendations:** Execute the planned V2 third-party pen test when funded.

### Trustworthiness
- **Score:** 75
- **Weight:** 3
- **Weighted deficiency:** 0.75
- **Justification:** High auditability and deterministic execution build trust, but AI explainability can be improved.
- **Tradeoffs:** Leveraging LLMs inherently introduces some opacity.
- **Improvement Recommendations:** Enhance agent explainability output in the UI.

### Interoperability
- **Score:** 65
- **Weight:** 2
- **Weighted deficiency:** 0.70
- **Justification:** Basic webhooks and V1 ITSM connectors exist, but the ecosystem is not yet mature (MCP deferred to V1.1).
- **Tradeoffs:** Focusing on core functionality before expanding the integration ecosystem.
- **Improvement Recommendations:** Accelerate the MCP integration planned for V1.1.

### Decision Velocity
- **Score:** 70
- **Weight:** 2
- **Weighted deficiency:** 0.60
- **Justification:** The platform speeds up architecture decisions, but the governance gates can introduce delays.
- **Tradeoffs:** Speed vs. control and compliance.
- **Improvement Recommendations:** Provide pre-approved policy packs for low-risk decisions.

### Explainability
- **Score:** 70
- **Weight:** 2
- **Weighted deficiency:** 0.60
- **Justification:** The system tracks provenance, but the internal reasoning of the AI agents is not always surfaced clearly to the user.
- **Tradeoffs:** Storing and presenting large amounts of LLM trace data.
- **Improvement Recommendations:** Surface LLM reasoning traces directly in the run detail view.

### Compliance Readiness
- **Score:** 70
- **Weight:** 2
- **Weighted deficiency:** 0.60
- **Justification:** Self-assessment is complete, but CPA SOC 2 is deferred.
- **Tradeoffs:** Cost and time of formal certification vs. time-to-market.
- **Improvement Recommendations:** Create an automated compliance mapping report generator.

### Procurement Readiness
- **Score:** 70
- **Weight:** 2
- **Weighted deficiency:** 0.60
- **Justification:** Trust center and security docs exist, but lack of formal certifications adds friction.
- **Tradeoffs:** Providing self-attested documentation vs. formal reports.
- **Improvement Recommendations:** Prepare the evidence room for the upcoming SOC 2 observation period.

### Traceability
- **Score:** 80
- **Weight:** 3
- **Weighted deficiency:** 0.60
- **Justification:** Excellent provenance tracking and knowledge graph capabilities.
- **Tradeoffs:** Storage overhead for maintaining full history.
- **Improvement Recommendations:** Optimize the storage of historical run data.

### Commercial Packaging Readiness
- **Score:** 75
- **Weight:** 2
- **Weighted deficiency:** 0.50
- **Justification:** Pricing and packaging are defined, but the self-serve commerce un-hold is deferred to V1.1.
- **Tradeoffs:** Sales-led motion for V1 vs. fully automated self-serve.
- **Improvement Recommendations:** Complete the Stripe live key flip for V1.1.

### Policy and Governance Alignment
- **Score:** 75
- **Weight:** 2
- **Weighted deficiency:** 0.50
- **Justification:** Strong policy packs and pre-commit gates.
- **Tradeoffs:** Complexity of configuring policies.
- **Improvement Recommendations:** Provide more out-of-the-box policy templates.

### Reliability
- **Score:** 75
- **Weight:** 2
- **Weighted deficiency:** 0.50
- **Justification:** Circuit breakers and resilience pipelines are in place, but multi-region active/active is not guaranteed for V1.
- **Tradeoffs:** Cost of multi-region deployment vs. availability requirements.
- **Improvement Recommendations:** Document clear disaster recovery procedures.

### Maintainability
- **Score:** 75
- **Weight:** 2
- **Weighted deficiency:** 0.50
- **Justification:** Highly modular codebase with many assemblies. **Day-to-day change** is less constrained by “finding the right file” when the team works primarily with **AI-assisted editing and search** (for example Cursor), provided naming and boundaries stay consistent. The **harder** cost is **solution build time**, **CI duration**, dependency churn, and onboarding someone who is *not* using the same tooling cadence.
- **Tradeoffs:** Granular modularity vs. build graph and pipeline complexity.
- **Improvement Recommendations:** Keep architecture tests and docs aligned so agents stay accurate; trim or merge projects only where builds or releases are clearly bottlenecked.

### Azure Compatibility and SaaS Deployment Readiness
- **Score:** 75
- **Weight:** 2
- **Weighted deficiency:** 0.50
- **Justification:** Strong Azure alignment (Container Apps, SQL, Entra), but ACR production image store is not fully automated.
- **Tradeoffs:** Relying on customer-owned infrastructure deployment for some components.
- **Improvement Recommendations:** Fully automate the ACR push pipeline.

### Architectural Integrity
- **Score:** 85
- **Weight:** 3
- **Weighted deficiency:** 0.45
- **Justification:** Clean architecture, clear boundaries, and NetArchTest enforcement.
- **Tradeoffs:** Strict layering can sometimes lead to boilerplate code.
- **Improvement Recommendations:** Continue enforcing architecture tests in CI.

### Template and Accelerator Richness
- **Score:** 60
- **Weight:** 1
- **Weighted deficiency:** 0.40
- **Justification:** Lacks industry-specific templates to accelerate onboarding.
- **Tradeoffs:** Focusing on the core engine before building content.
- **Improvement Recommendations:** Develop a library of common architectural patterns.

### Data Consistency
- **Score:** 80
- **Weight:** 2
- **Weighted deficiency:** 0.40
- **Justification:** Transactional outboxes and strong SQL constraints ensure consistency.
- **Tradeoffs:** Performance overhead of transactional guarantees.
- **Improvement Recommendations:** Monitor outbox processing latency.

### AI/Agent Readiness
- **Score:** 80
- **Weight:** 2
- **Weighted deficiency:** 0.40
- **Justification:** Robust agent runtime with circuit breakers and fallback mechanisms.
- **Tradeoffs:** Complexity of managing multiple LLM providers.
- **Improvement Recommendations:** Implement strict tenant-level token quotas.

### Stickiness
- **Score:** 65
- **Weight:** 1
- **Weighted deficiency:** 0.35
- **Justification:** Value is high once adopted, but the lack of deep workflow embeddedness reduces stickiness.
- **Tradeoffs:** Standalone platform vs. integrated tool.
- **Improvement Recommendations:** Prioritize **ServiceNow** connector depth (including **CMDB** / `cmdb_ci` planning per scope) before investing in **Jira** bi-directional edge cases.

### Customer Self-Sufficiency
- **Score:** 65
- **Weight:** 1
- **Weighted deficiency:** 0.35
- **Justification:** High reliance on documentation; error messages could be more actionable.
- **Tradeoffs:** Complex enterprise software often requires training.
- **Improvement Recommendations:** Improve in-app guidance and actionable error messages.

### Accessibility
- **Score:** 70
- **Weight:** 1
- **Weighted deficiency:** 0.30
- **Justification:** Basic accessibility testing (axe-core) is in place, but comprehensive audits are needed.
- **Tradeoffs:** Prioritizing functional features over deep accessibility compliance in V1.
- **Improvement Recommendations:** Conduct a full WCAG audit of the operator UI.

### Change Impact Clarity
- **Score:** 70
- **Weight:** 1
- **Weighted deficiency:** 0.30
- **Justification:** Comparison replays show what changed, but the business impact is not always clear.
- **Tradeoffs:** Technical diffs vs. business-level summaries.
- **Improvement Recommendations:** Add business-impact summaries to the comparison view.

### Availability
- **Score:** 70
- **Weight:** 1
- **Weighted deficiency:** 0.30
- **Justification:** Good baseline reliability, but lacks automated failover guarantees.
- **Tradeoffs:** Cost of high availability infrastructure.
- **Improvement Recommendations:** Implement automated health-check based failover.

### Deployability
- **Score:** 70
- **Weight:** 1
- **Weighted deficiency:** 0.30
- **Justification:** Docker compose and Terraform exist, but production deployment requires manual steps.
- **Tradeoffs:** Flexibility for different customer environments vs. turnkey deployment.
- **Improvement Recommendations:** Provide a one-click Azure deployment template.

### Cognitive Load
- **Score:** 70
- **Weight:** 1
- **Weighted deficiency:** 0.30
- **Justification:** The system introduces many domain-specific concepts.
- **Tradeoffs:** Domain richness vs. simplicity.
- **Improvement Recommendations:** Simplify the terminology in the UI where possible.

### Auditability
- **Score:** 85
- **Weight:** 2
- **Weighted deficiency:** 0.30
- **Justification:** 78 typed audit events in an append-only store. Excellent coverage.
- **Tradeoffs:** Storage and performance overhead of comprehensive auditing.
- **Improvement Recommendations:** Maintain the current high standard.

### Performance
- **Score:** 75
- **Weight:** 1
- **Weighted deficiency:** 0.25
- **Justification:** k6 load tests and caching are in place, but large manifests may slow down the UI.
- **Tradeoffs:** Processing large JSON structures in the browser.
- **Improvement Recommendations:** Implement virtualization for large manifest views in the UI.

### Scalability
- **Score:** 75
- **Weight:** 1
- **Weighted deficiency:** 0.25
- **Justification:** Worker processes and SQL read-replicas support scaling.
- **Tradeoffs:** Complexity of distributed systems.
- **Improvement Recommendations:** Load test the worker queues under high concurrency.

### Supportability
- **Score:** 75
- **Weight:** 1
- **Weighted deficiency:** 0.25
- **Justification:** CLI diagnostics and correlation IDs are present.
- **Tradeoffs:** Building support tools takes time away from feature development.
- **Improvement Recommendations:** Improve error messages for misconfigurations.

### Manageability
- **Score:** 75
- **Weight:** 1
- **Weighted deficiency:** 0.25
- **Justification:** Configuration is well-structured, but lacks a centralized admin UI for all settings.
- **Tradeoffs:** File-based config vs. database-backed settings.
- **Improvement Recommendations:** Build an admin settings dashboard in the UI.

### Extensibility
- **Score:** 75
- **Weight:** 1
- **Weighted deficiency:** 0.25
- **Justification:** Plugin architecture exists for finding engines.
- **Tradeoffs:** Maintaining stable extension points.
- **Improvement Recommendations:** Document the plugin API more comprehensively.

### Evolvability
- **Score:** 75
- **Weight:** 1
- **Weighted deficiency:** 0.25
- **Justification:** Modular design allows for easy updates.
- **Tradeoffs:** Refactoring across many projects can be tedious.
- **Improvement Recommendations:** Keep dependencies between modules strictly controlled.

### Cost-Effectiveness
- **Score:** 75
- **Weight:** 1
- **Weighted deficiency:** 0.25
- **Justification:** Efficient use of Azure resources, but LLM costs need careful monitoring.
- **Tradeoffs:** Using powerful LLMs vs. cheaper, faster models.
- **Improvement Recommendations:** Add cost-tracking telemetry.

### Observability
- **Score:** 80
- **Weight:** 1
- **Weighted deficiency:** 0.20
- **Justification:** Comprehensive OTel metrics and tracing.
- **Tradeoffs:** Instrumentation overhead.
- **Improvement Recommendations:** Provide pre-built Grafana dashboards in the repository.

### Testability
- **Score:** 80
- **Weight:** 1
- **Weighted deficiency:** 0.20
- **Justification:** Exceptional test infrastructure (mutation, chaos, property-based).
- **Tradeoffs:** High maintenance burden.
- **Improvement Recommendations:** Implement visual regression testing for the UI.

### Modularity
- **Score:** 80
- **Weight:** 1
- **Weighted deficiency:** 0.20
- **Justification:** Many projects with clear boundaries and enforcement (for example architecture tests).
- **Tradeoffs:** Larger build/CI graphs vs. strict separation of concerns; navigation overhead is **moderated** in practice when development is **AI-assisted** across the repo.
- **Improvement Recommendations:** Consolidate only where merge CI or release velocity proves it worthwhile.

### Azure Ecosystem Fit
- **Score:** 80
- **Weight:** 1
- **Weighted deficiency:** 0.20
- **Justification:** Deep integration with Azure services (Entra, Key Vault, SQL).
- **Tradeoffs:** Vendor lock-in.
- **Improvement Recommendations:** Maintain abstraction layers where possible.

### Documentation
- **Score:** 85
- **Weight:** 1
- **Weighted deficiency:** 0.15
- **Justification:** Extensive, well-organized documentation with clear scope boundaries.
- **Tradeoffs:** Time spent writing docs vs. code.
- **Improvement Recommendations:** Keep docs updated as V1.1 features are implemented.

## Top 10 Most Important Weaknesses

1. **Lack of Executive Dashboards:** Value is not visible to non-operators, hindering executive sponsorship.
2. **High Adoption Friction:** The system requires significant context and understanding of new concepts to start using effectively.
3. **Limited Workflow Embeddedness:** Forces users into a new UI rather than integrating deeply into existing tools (e.g., IDEs, Slack).
4. **ROI Proof is Manual:** No automated way to show time or money saved by using the platform.
5. **Opaque AI Decision-Making:** Agent reasoning can be difficult to understand for end-users, reducing trust.
6. **Template Richness:** Lack of out-of-the-box industry templates makes the "blank page" problem worse.
7. **Enterprise SaaS onboarding drag:** Even in a hosted product, first value is gated on IdP alignment, network and security sign-off (e.g. private endpoints), and connector or API credential setup—not merely “sign up and go.”
8. **Interoperability:** Limited out-of-the-box connectors for enterprise systems beyond the V1 baseline.
9. **Cognitive Load:** The system introduces many new concepts (runs, manifests, governance) that overwhelm casual users.
10. **Customer Self-Sufficiency:** High reliance on documentation and pilot guides due to complex configuration and sometimes cryptic error messages.

## Top 5 Monetization Blockers

1. Inability to clearly demonstrate ROI to executive buyers.
2. High perceived adoption friction for new teams evaluating the product.
3. Lack of compelling executive-level reporting and dashboards.
4. Missing industry-specific accelerator templates to speed up initial pilots.
5. Enterprise trial and pilot friction when security, identity, and integration prerequisites delay time-to-first successful run in the hosted product.

## Top 5 Enterprise Adoption Blockers

1. Limited deep integration with existing ITSM workflows (beyond basic ticket creation).
2. Lack of automated compliance mapping for specific frameworks (e.g., SOC 2, ISO 27001).
3. High cognitive load for casual users who just want to review an architecture.
4. Opaque AI decision-making processes that fail security/compliance reviews.
5. Need for extensive training to understand the architecture lifecycle.

## Top 5 Engineering Risks

1. **Build, CI, and release wall-clock:** A broad solution and deep test tiers still cap how fast every change can reach **merge-green** and production confidence, even when **individual implementation** is fast with Cursor-first workflows.
2. **Agent Cost:** Unbounded LLM usage without strict tenant-level quotas could lead to cost overruns.
3. **Hosted platform surface area:** API, worker, data plane, queues, and optional integrations multiply failure modes and incident scope for the SaaS operator—not a small, single-process footprint.
4. **Test Maintenance:** High burden of maintaining the extensive test suites (mutation, chaos, property-based).
5. **Data Migration:** Complex schema evolutions as the manifest model changes over time.

## Most Important Truth

ArchLucid is an engineering marvel that solves the operator's problem but currently fails to automatically translate that success into executive-visible ROI, creating friction in the sales and expansion motions.

## Top Improvement Opportunities

### 1. Implement Executive ROI Dashboard
- **Status:** **Completed (2026-05-05).** Shipped `ExecutiveRoiDashboard.tsx` under `archlucid-ui/src/components/dashboard/` (loads via `/api/proxy/v1/analytics/roi`), `RoiAnalyticsController` + `ExecutiveRoiAggregatesResponse` under `ArchLucid.Api` (mocked JSON until analytics persistence is defined), route constant `ApiV1Routes.AnalyticsRoi`, and `RoiAnalyticsEndpointTests` for `GET /v1/analytics/roi`. Persistence layers were not changed.
- **Why it matters:** Buyers need to see value without understanding the technical details.
- **Expected impact:** Directly improves Executive Value Visibility (+15 pts) and Proof-of-ROI Readiness (+10 pts). Weighted readiness impact: +1.1%. (Full buyer impact depends on wiring real aggregates and surfacing the component in executive flows.)
- **Affected qualities:** Executive Value Visibility, Proof-of-ROI Readiness.
- **Actionable:** No — baseline UI + API endpoint delivered; follow-on work is real metrics, product placement, and executive narrative.

```cursor
Create a new React component `ExecutiveRoiDashboard.tsx` in `archlucid-ui/src/components/dashboard/`. It should fetch aggregated metrics from a new API endpoint `/v1/analytics/roi` (which you should also create in `ArchLucid.Api/Controllers/`). The dashboard should display 'Time Saved', 'Decisions Automated', and 'Compliance Risks Mitigated'. Do not modify existing persistence layers, just mock the data in the controller for now until the data model is finalized.
```

### 2. Industry accelerator templates (healthcare, finance, manufacturing)
- **Status:** **Completed (2026-05-05).** Finance and manufacturing catalog entries and factories are shipped: `RetailBankingAndPaymentsPlatform` (`financial-services-pci-sox`) and `SmartManufacturingOtItReference` (`manufacturing-ot-it-convergence`) in `ArchLucid.Application/Templates/ArchitectureRequestTemplates.cs`; `Summaries` lists **seven** templates; `ArchitectureRequestTemplatesTests` and `ArchitectureControllerTests` assert the expanded catalog and POST coverage.
- **Why it matters:** Reduces adoption friction with recognizable vertical starting points; aligns pilots with buyer language.
- **Expected impact:** Directly improves Template and Accelerator Richness (+18–22 pts), Adoption Friction (+4–6 pts), Marketability (+3–5 pts). Weighted readiness impact: ~+0.35–0.45%.
- **Affected qualities:** Template and Accelerator Richness, Adoption Friction, Marketability.
- **Actionable:** No — fulfilled in code (healthcare remains `RegulatedHealthcareSystem` / `regulated-healthcare-hipaa`; finance and manufacturing added as above).

```cursor
Implement vertical accelerator templates for the three target industries: **healthcare**, **finance**, and **manufacturing**.

**Context:** `ArchLucid.Application/Templates/ArchitectureRequestTemplates.cs` already ships **healthcare** via `RegulatedHealthcareSystem` / `regulated-healthcare-hipaa`. Extend the catalog rather than duplicating that template.

**Do:**
1. Add two new static factory methods on `ArchitectureRequestTemplates`, following the same `Build(...)` pattern as existing templates:
   - **Finance:** e.g. `RetailBankingAndPaymentsPlatform` with template id `financial-services-pci-sox` — cover cardholder data / PCI scope boundaries, strong authentication, ledger posting integrity, settlement and reconciliation, AML/fraud analytics adjacency (high level), audit trails suitable for SOX-minded reviewers, and Azure-aligned controls (Key Vault, Encryption, private connectivity). Minimum **five** evidence markdown documents plus `ArchLucid.TemplateId` (same as other templates).
   - **Manufacturing:** e.g. `SmartManufacturingOtItReference` with template id `manufacturing-ot-it-convergence` — cover plant/MES integration, ERP handoff, historian/time-series data, shop-floor latency and availability, OT/IT network segmentation, safety-related systems and change control, and supply-chain integration touchpoints. Minimum **five** evidence documents plus template id.
2. Append both to `Summaries` with clear titles and short descriptions that name the industry (finance / manufacturing).
3. Update `ArchLucid.Application.Tests/Templates/ArchitectureRequestTemplatesTests.cs`: extend `TemplateFactories` theory data, change summary count from **5** to **7**, and assert the new template ids are present and unique across `Summaries`.
4. Update `ArchLucid.Api.Tests/ArchitectureControllerTests.cs` test `GetArchitectureRequestTemplates_ReturnsFiveSummaries` — rename and assert **7** summaries; ensure GET `/v1/architecture/templates` still returns 200 and IDs are unique.

**Constraints:** Do not change `POST /v1/architecture/request` validation. Match existing authorization/rate limits on `TemplatesController`. Preserve **one blank line max** between statements in `*.cs` per repo whitespace rules. Do not add new NuGet dependencies.

**Acceptance criteria:** `dotnet test` passes for projects touched; `GET /v1/architecture/templates` returns seven summaries; each new factory produces serializable `ArchitectureRequest` with ≥3 evidence docs after the template marker (same invariant as existing tests).
```

### 3. Simplify Initial Setup CLI Command
- **Status:** **Completed (2026-05-05).** `archlucid new <projectName> [--quickstart]` is parsed in `ArchLucid.Cli/Program.cs` (`TryParseNewCommandArgs`, `WriteNewUsage`, master command list); `ArchLucid.Cli/Commands/NewCommand.cs` sets `ScaffoldOptions.QuickStartEvaluation`; `ArchLucid.Cli/ArchLucidProjectScaffolder.cs` writes `local/archlucid.quickstart.appsettings.json` (`ArchLucid:StorageProvider` = `InMemory`) and `local/archlucid-evaluation.sqlite` via `ArchLucid.Cli/QuickStartSQLiteProjectRegistry.cs` (`Microsoft.Data.Sqlite`, `Pooling=false`). `docs/README.md` in the scaffold documents quickstart; tests in `ArchLucid.Cli.Tests` (`CreateProject_QuickStartEvaluation_*`, `New_with_quickstart_*`, `New_with_unknown_flag_*`).
- **Why it matters:** Reduces time-to-value for new evaluators.
- **Expected impact:** Directly improves Time-to-Value (+10 pts) and Adoption Friction (+5 pts). Weighted readiness impact: +1.0%.
- **Affected qualities:** Time-to-Value, Adoption Friction.
- **Actionable:** No — fulfilled in code.

```cursor
Update `ArchLucid.Cli/Commands/NewCommand.cs` to include a `--quickstart` flag that automatically provisions a local SQLite database (or in-memory store) and bypasses the need for a full SQL Server setup during initial evaluation. Ensure this is clearly documented in the CLI help text.
```

### 4. Enhance Agent Explainability Output
- **Why it matters:** Builds trust with users when they understand why a decision was made.
- **Expected impact:** Directly improves Explainability (+15 pts) and Trustworthiness (+10 pts). Weighted readiness impact: +0.6%.
- **Affected qualities:** Explainability, Trustworthiness.
- **Actionable:** Yes.

```cursor
Modify `ArchLucid.AgentRuntime/RealAgentExecutor.cs` to capture and return the intermediate reasoning steps from the LLM (e.g., chain-of-thought traces) in the `AgentResult`. Update the `AgentResult` DTO in `ArchLucid.Contracts` to include a `ReasoningTrace` property. Do not change the core execution logic, just append the trace to the output.
```

### 5. Lock ServiceNow-first sequencing for deep ITSM (CMDB before Jira depth)
- **Status:** **Completed (2026-05-05).** Owner decisions propagated in **`docs/go-to-market/INTEGRATION_CATALOG.md`** (**Sequencing and CMDB** + committed table), **`docs/library/ITSM_BRIDGE_V1_RECIPES.md`** (intro), **`templates/integrations/servicenow/servicenow-incident-recipe.md`** (V1 GA disclaimer + `cmdb_ci` note), and **`docs/library/V1_SCOPE.md`** §2.13 ServiceNow bullet. **`python scripts/ci/check_doc_scope_header.py`** passes. No runtime code in that pass.
- **Why it matters:** Buyers standardizing on **ServiceNow** need CMDB-aligned signal; spreading engineering across Jira and SNOW in parallel dilutes the highest-value path.
- **Expected impact:** Directly improves Workflow Embeddedness (+8–12 pts), Interoperability (+5–8 pts), Procurement Readiness (+3–5 pts for buyers on SNOW). Weighted readiness impact: ~+0.25–0.35%.
- **Affected qualities:** Workflow Embeddedness, Interoperability, Procurement Readiness (where SNOW is the incumbent ITSM).
- **Owner decisions (recorded):** (1) **ServiceNow is the priority** over advanced Jira depth. (2) **CMDB CI table: `cmdb_ci_appl`** — look up `SystemName` matching `name`, set `cmdb_ci` on the incident to the matched `sys_id`; `AutoCreateCmdbCi` defaults to `false`.
- **Actionable:** No — documentation and scope contract updated; connector implementation remains future work.

```cursor
Record and propagate two owner decisions into docs: (1) ServiceNow-first sequencing; (2) CMDB CI table is `cmdb_ci_appl`.

**Background on the CMDB CI table decision:** Every ArchLucid architecture request has a `SystemName` field identifying the software system under review. `cmdb_ci_appl` (Application CI) is the correct ServiceNow CMDB class for a named software application. Enterprise SNOW instances already catalogue their applications here, so a name-match lookup ties architecture findings directly to the customer's existing application records without requiring custom CMDB schema changes. The first-party connector should: (a) query `GET /api/now/table/cmdb_ci_appl?sysparm_query=name={SystemName}&sysparm_limit=1`; (b) if a match is found, set `cmdb_ci` on the new incident to that record's `sys_id`; (c) if no match is found, leave `cmdb_ci` empty; (d) expose a `ServiceNow:AutoCreateCmdbCi` boolean option (default `false`) that controls whether to create a new `cmdb_ci_appl` record when no match is found.

**Doc-only changes (no runtime code in this pass):**

1. **`docs/go-to-market/INTEGRATION_CATALOG.md`** — Below the **V1 committed — first-party ITSM connectors** table, add a **Sequencing and CMDB** subsection: (a) ServiceNow builds before Jira depth; (b) CMDB CI class is `cmdb_ci_appl`, matched by `SystemName → name`, `cmdb_ci` field set to matched `sys_id`; (c) `AutoCreateCmdbCi` defaults to `false`; (d) two-way ServiceNow → ArchLucid status sync is **not** in committed V1 scope.

2. **`docs/library/ITSM_BRIDGE_V1_RECIPES.md`** — In the intro, note: (a) ServiceNow before Jira for platform-led depth; (b) the first-party connector will populate `cmdb_ci` via a `cmdb_ci_appl` name lookup on `SystemName`.

3. **`templates/integrations/servicenow/servicenow-incident-recipe.md`** — Fix the V1.1 disclaimer: first-party ServiceNow is **V1 GA** per `V1_SCOPE.md` §2.13 (store listing may trail). Add a note: "The first-party connector will set `cmdb_ci` via a `cmdb_ci_appl` lookup on `SystemName`; this recipe does not attempt `cmdb_ci` population."

**Constraints:** Keep every touched `docs/**/*.md` file's first non-blank line as the required `> **Scope:**` blockquote. Run `python scripts/ci/check_doc_scope_header.py` on edited paths. No product runtime code changes in this pass.

**Acceptance criteria:** Docs read: ServiceNow CMDB uses `cmdb_ci_appl` matched by `SystemName`, `AutoCreateCmdbCi` defaults `false`, ServiceNow ships before Jira depth. No doc implies a different CI class or build order. Scope headers preserved, check script passes.```

### 6. Plan persistence consolidation (only if CI or release is the bottleneck)
- **Status:** **Completed (2026-05-05).** Plan-only doc added: **`docs/library/PERSISTENCE_CONSOLIDATION_PLAN.md`** (baselines to capture, merge outline, **`dotnet`** commands). No assembly merge executed — gated on measured build/CI pain.
- **Why it matters:** Many assemblies mainly hurt **merge and release wall-clock**, not necessarily day-to-day editing when tooling-assisted; consolidation is justified when builds or CI prove it.
- **Expected impact:** Directly improves Modularity (+5 pts) and Maintainability (+5 pts) **if** solution build or CI duration drops materially. Weighted readiness impact: +0.15% (nominal until measured).
- **Affected qualities:** Modularity, Maintainability.
- **Actionable:** No — plan is written; consolidation execution waits on metrics justification.

```cursor
Create a plan document `docs/library/PERSISTENCE_CONSOLIDATION_PLAN.md` outlining how to merge `ArchLucid.Persistence.Alerts` and `ArchLucid.Persistence.Advisory` into `ArchLucid.Persistence.Runtime` **only if** the team has measured that solution build time or core CI tier duration is a recurring bottleneck. Include: current build/CI baselines to capture before/after, merge steps, and the specific `dotnet` commands. Do not execute the code move in the same change—plan only unless metrics already justify execution.
```

### 7. Add Cost-Tracking Telemetry
- **Why it matters:** Enterprises need to know how much the LLM calls are costing per run.
- **Expected impact:** Directly improves Cost-Effectiveness (+15 pts) and Manageability (+10 pts). Weighted readiness impact: +0.25%.
- **Affected qualities:** Cost-Effectiveness, Manageability.
- **Actionable:** Yes.

```cursor
Update `ArchLucid.Core/Diagnostics/ArchLucidInstrumentation.cs` to include a new OpenTelemetry counter `archlucid.agent.estimated_cost_usd`. Update `RealAgentExecutor.cs` to calculate and record this cost based on token usage and a configured price-per-token. Do not hardcode prices; read them from `IOptions<AgentExecutionResilienceOptions>` or a new options class.
```

### 8. Improve Error Messages for Misconfigurations
- **Why it matters:** Reduces support burden and improves self-sufficiency.
- **Expected impact:** Directly improves Supportability (+15 pts) and Customer Self-Sufficiency (+10 pts). Weighted readiness impact: +0.25%.
- **Affected qualities:** Supportability, Customer Self-Sufficiency.
- **Actionable:** Yes.

```cursor
Review `ArchLucid.Host.Composition/Startup/` and add explicit startup validation checks for missing critical configuration values (e.g., missing SQL connection strings, missing LLM API keys). Throw a custom `ArchLucidConfigurationException` with a highly descriptive message explaining exactly which appsettings.json key is missing and providing a link to the relevant documentation.
```

### 9. Create Automated Compliance Mapping Report
- **Why it matters:** Accelerates procurement and security reviews.
- **Expected impact:** Directly improves Compliance Readiness (+15 pts) and Procurement Readiness (+10 pts). Weighted readiness impact: +0.5%.
- **Affected qualities:** Compliance Readiness, Procurement Readiness.
- **Actionable:** Yes.

```cursor
Create a new CLI command `archlucid compliance-report` in `ArchLucid.Cli/Commands/` that generates a Markdown report mapping the current system configuration and audit logs to SOC 2 and ISO 27001 controls. Use the existing `docs/security/SOC2_SELF_ASSESSMENT_2026.md` as the template source.
```

### 10. Implement Visual Regression Testing for UI
- **Why it matters:** Ensures UI changes don't break the operator experience.
- **Expected impact:** Directly improves Testability (+10 pts) and Reliability (+5 pts). Weighted readiness impact: +0.2%.
- **Affected qualities:** Testability, Reliability.
- **Actionable:** Yes.

```cursor
Add Playwright visual comparison tests to `archlucid-ui/tests/e2e/`. Create a new test file `visual-regression.spec.ts` that takes screenshots of the main dashboard, run detail page, and comparison view, and asserts them against golden baselines using `expect(page).toHaveScreenshot()`. Do not modify existing tests.
```

## Pending Questions for Later

### ITSM / CMDB specifics (only if blocking a signed pilot)
- **ServiceNow CI table:** ~~Resolved~~ — **`cmdb_ci_appl`** confirmed by owner. Lookup by `SystemName → name`, set `cmdb_ci` to matched `sys_id`, `AutoCreateCmdbCi` defaults to `false`. No further input needed unless a specific pilot customer uses a non-standard CI class.
- Are any **other** ITSM tools (e.g. Zendesk, Cherwell) **blocking** current deals in a way that would override the default **ServiceNow-first** sequencing?

