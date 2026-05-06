# ArchLucid Assessment – Weighted Readiness 86.57%

## Executive Summary

**Overall Readiness**
ArchLucid is a highly documented, well-scoped V1 product that excels in clearly defining its boundaries and onboarding paths. It scores strongly in readiness (86.57%), primarily due to its meticulous attention to process, procurement readiness, and structured pilot ROI measurement. However, a significant reliance on mocked testing for UI and partially completed ITSM connectors represent underlying risks that must be addressed to ensure enterprise-grade stability.

**Commercial Picture**
The commercial story is very strong. The "Pilot vs Operate" layered approach, combined with automated ROI reports (DOCX/PDF) and the Executive Sponsor Brief, gives ArchLucid an exceptional go-to-market structure. While the final transacting mechanisms (Marketplace listings, live Stripe keys) are deferred, the product is entirely ready to facilitate sales-led pilots and convert them using solid, evidence-based ROI generation.

**Enterprise Picture**
Enterprise readiness is robust, highlighted by excellent procurement pack generation, RBAC, row-level security (RLS), and append-only audit logs. The lack of a CPA-issued SOC 2 report will cause friction during procurement (though correctly deferred in scope). Future expansion will require hardening the bi-directional sync capabilities of ITSM connectors (Jira/ServiceNow) to fully embed into enterprise workflows.

**Engineering Picture**
The system is built on a clean .NET and Azure-native architecture. The most glaring engineering risk is the reliance on deterministic mocks for Playwright UI smoke tests, which could allow API contract drift to reach production undetected. Additionally, observability requires modernization (e.g., OpenTelemetry) to provide operators with the necessary visibility during high-stakes enterprise pilots.

---

## Weighted Quality Assessment

*Qualities are ranked from most urgent (highest weighted deficiency) to least urgent.*

### 1. Marketability
- **Score:** 85/100
- **Weight:** 8
- **Weighted deficiency signal:** 120
- **Justification:** Exceptional messaging (Sponsor Brief, ROI model). Slight drag due to deferred self-serve transactability rails requiring sales-led motion.
- **Tradeoffs:** Prioritizing sales-led over pure PLG reduces immediate conversion speed but ensures higher-quality pilot engagements.
- **Improvement:** Expose more productized, automated demo states directly on the landing page to accelerate top-of-funnel interest without needing SE engagement.

### 2. Correctness
- **Score:** 75/100
- **Weight:** 4
- **Weighted deficiency signal:** 100
- **Justification:** The heavy use of mocks in Playwright UI smoke testing introduces a significant risk of masking API contract drift or edge-case regressions.
- **Tradeoffs:** Mocks speed up CI pipelines but sacrifice the end-to-end integration guarantees essential for a complex workflow tool.
- **Improvement:** Introduce a `--live` test suite for Playwright that runs directly against the API and SQL database.

### 3. Adoption Friction
- **Score:** 85/100
- **Weight:** 6
- **Weighted deficiency signal:** 90
- **Justification:** Docker-compose setup and Core Pilot path are incredibly clear, but requiring users to understand `archlucid.json` and local CLI commands for advanced scenarios adds friction.
- **Tradeoffs:** CLI-first approaches appeal to engineers but can alienate business-focused enterprise architects initially.
- **Improvement:** Expand the onboarding wizard in the operator UI to encompass the CLI's configuration capabilities.

### 4. Differentiability
- **Score:** 80/100
- **Weight:** 4
- **Weighted deficiency signal:** 80
- **Justification:** While AI-assisted architecture review is unique, the core of the tool can feel like an advanced workflow engine or ITSM add-on until the agent analysis is deeply embedded.
- **Tradeoffs:** Focusing on governance and auditability builds trust but temporarily dilutes the "magic" of the AI agent analysis.
- **Improvement:** Highlight the specific AI decisioning algorithms and provide deeper, interactive examples of the AI catching flaws that humans missed.

### 5. Time-to-Value
- **Score:** 90/100
- **Weight:** 7
- **Weighted deficiency signal:** 70
- **Justification:** The 60-second trial run and structured Pilot methodology deliver value extremely quickly.
- **Tradeoffs:** Fast initial value via default templates might result in users hitting a complexity wall when they need deep customization.
- **Improvement:** Create "one-click" integrations for Jira/ServiceNow to shorten the time to connect external systems.

### 6. Workflow Embeddedness
- **Score:** 80/100
- **Weight:** 3
- **Weighted deficiency signal:** 60
- **Justification:** Jira, Slack, and ServiceNow connectors are V1 commitments, but lack advanced bi-directional sync (like ServiceNow -> ArchLucid status sync).
- **Tradeoffs:** Limiting sync to one-way or basic webhooks reduces V1 scope risk but limits how deeply the tool is entrenched in existing enterprise workflows.
- **Improvement:** Implement robust bi-directional status syncing for Jira to ensure ArchLucid remains the source of truth without requiring dual-entry.

### 7. Usability
- **Score:** 85/100
- **Weight:** 3
- **Weighted deficiency signal:** 45
- **Justification:** Progressive disclosure ("Pilot" vs "Operate" layers) is a brilliant UI pattern. However, UI shaping based on role without actual entitlement boundaries can confuse administrators.
- **Tradeoffs:** UI shaping without hard entitlement logic speeds up development but requires careful communication to avoid misleading users about security.
- **Improvement:** Add visual indicators clarifying that UI shaping is for cognitive load reduction, not security authorization.

### 8. Trustworthiness
- **Score:** 85/100
- **Weight:** 3
- **Weighted deficiency signal:** 45
- **Justification:** Excellent Trust Center and self-assessment posture. Lack of CPA SOC 2 and external pen-testing (both intentionally deferred) slightly lowers the absolute score.
- **Tradeoffs:** Deferring SOC 2 saves capital but introduces friction with enterprise procurement.
- **Improvement:** Accelerate the third-party penetration test (V2) to provide an external seal of approval earlier in the sales cycle.

### 9. Executive Value Visibility
- **Score:** 90/100
- **Weight:** 4
- **Weighted deficiency signal:** 40
- **Justification:** The automated ROI DOCX report and Executive Sponsor Brief are top-tier assets.
- **Tradeoffs:** The model relies partially on operator-filled qualitative data, which skeptical executives might scrutinize.
- **Improvement:** Automate more metrics, such as calculating the exact delta in approval times via ITSM webhooks.

### 10. Reliability
- **Score:** 80/100
- **Weight:** 2
- **Weighted deficiency signal:** 40
- **Justification:** Standard ASP.NET Core reliability with health checks, but DbUp migration errors on startup cause hard crashes without graceful degradation.
- **Tradeoffs:** Failing fast on DB migration ensures data integrity but lowers perceived availability during deployments.
- **Improvement:** Introduce a degraded "read-only" mode if non-critical migrations fail.

### 11. Decision Velocity
- **Score:** 85/100
- **Weight:** 2
- **Weighted deficiency signal:** 30
- **Justification:** Rapid progression from request to Golden Manifest.
- **Tradeoffs:** The speed of decisioning may encourage rubber-stamping if the AI agent's reasoning is not sufficiently scrutinized.
- **Improvement:** Enforce mandatory review checkpoints for high-severity AI findings.

### 12. Traceability
- **Score:** 90/100
- **Weight:** 3
- **Weighted deficiency signal:** 30
- **Justification:** RLS, typed audit logs, and graph connections provide a massive enterprise advantage.
- **Tradeoffs:** The heavy audit trail requires aggressive storage management.
- **Improvement:** Implement automated retention and archival policies for the audit logs.

### 13. Compliance Readiness
- **Score:** 85/100
- **Weight:** 2
- **Weighted deficiency signal:** 30
- **Justification:** CAIQ Lite, SIG Core, and DPA templates are ready, but the system relies entirely on these self-attestations currently.
- **Tradeoffs:** Templates win early pilots, but formal compliance requires costly audits.
- **Improvement:** Implement a feature to export the system's exact compliance configuration mapped to SOC 2 controls.

### 14. Interoperability
- **Score:** 85/100
- **Weight:** 2
- **Weighted deficiency signal:** 30
- **Justification:** Solid webhook and Service Bus foundations, but lacking a broad, pre-built integration ecosystem.
- **Tradeoffs:** Focusing on generic webhooks sacrifices the polished UX of native integrations.
- **Improvement:** Expand the first-party connector catalog to include GitHub and GitLab native integrations.

### 15. Architectural Integrity
- **Score:** 90/100
- **Weight:** 3
- **Weighted deficiency signal:** 30
- **Justification:** Highly coherent C4 mapping and clean .NET boundaries.
- **Tradeoffs:** Strict boundary enforcement can slow down the development of cross-cutting features.
- **Improvement:** Regularly run architectural fitness functions (like ArchUnitNET) in CI.

### 16. Security
- **Score:** 90/100
- **Weight:** 3
- **Weighted deficiency signal:** 30
- **Justification:** Excellent use of RLS, Entra ID, and private endpoints.
- **Tradeoffs:** High security defaults (like disabled API keys) increase initial onboarding friction.
- **Improvement:** Create an automated security diagnostic CLI command to verify RLS and Entra settings post-deployment.

### 17. Data Consistency
- **Score:** 85/100
- **Weight:** 2
- **Weighted deficiency signal:** 30
- **Justification:** Dapper and DbUp provide solid guarantees, but there is no native cross-region active-active support in V1.
- **Tradeoffs:** Avoiding multi-region complexity in V1 guarantees delivery but risks isolation during severe regional outages.
- **Improvement:** Add automated data reconciliation scripts for disaster recovery scenarios.

### 18. Maintainability
- **Score:** 85/100
- **Weight:** 2
- **Weighted deficiency signal:** 30
- **Justification:** Code is modular and well-documented.
- **Tradeoffs:** The sheer volume of documentation requires significant maintenance effort to prevent drift.
- **Improvement:** Automate documentation drift detection by linking code annotations to markdown headers.

### 19. Explainability
- **Score:** 85/100
- **Weight:** 2
- **Weighted deficiency signal:** 30
- **Justification:** Run comparisons and LLM citations are excellent, but LLM hallucinations remain a risk.
- **Tradeoffs:** AI narratives speed up comprehension but require human verification.
- **Improvement:** Add a "confidence score" to AI-generated explanations based on citation proximity.

### 20. AI/Agent Readiness
- **Score:** 85/100
- **Weight:** 2
- **Weighted deficiency signal:** 30
- **Justification:** Azure OpenAI integration is clean, with LLM redaction pipelines in place.
- **Tradeoffs:** Tying heavily to Azure OpenAI limits multi-cloud portability for the agent execution layer.
- **Improvement:** Abstract the LLM interface to support alternate providers (e.g., Anthropic, local SLMs).

### 21. Proof-of-ROI Readiness
- **Score:** 95/100
- **Weight:** 5
- **Weighted deficiency signal:** 25
- **Justification:** The automated, data-driven DOCX value report is an incredible asset.
- **Tradeoffs:** None. This is a massive strength.
- **Improvement:** Expose a real-time ROI dashboard in the UI alongside the static DOCX export.

### 22. Observability
- **Score:** 75/100
- **Weight:** 1
- **Weighted deficiency signal:** 25
- **Justification:** Standard .NET logging is present, but it lacks out-of-the-box OpenTelemetry tracing for deep enterprise observability.
- **Tradeoffs:** Standard logging is easy to setup but fails to provide distributed tracing across agents and webhooks.
- **Improvement:** Fully implement OpenTelemetry (OTLP) exporting for traces and metrics.

### 23. Commercial Packaging Readiness
- **Score:** 90/100
- **Weight:** 2
- **Weighted deficiency signal:** 20
- **Justification:** Strong layer definitions (Pilot/Operate).
- **Improvement:** Formalize the entitlement boundaries in code, not just UI shaping.

### 24. Policy and Governance Alignment
- **Score:** 90/100
- **Weight:** 2
- **Weighted deficiency signal:** 20
- **Justification:** Excellent segregation of duties and pre-commit gates.
- **Improvement:** Allow custom scripting for policy pack rules.

### 25. Stickiness
- **Score:** 80/100
- **Weight:** 1
- **Weighted deficiency signal:** 20
- **Justification:** Comparison over time creates lock-in.
- **Improvement:** Enhance trend analysis visualizations over months of architecture changes.

### 26. Template and Accelerator Richness
- **Score:** 80/100
- **Weight:** 1
- **Weighted deficiency signal:** 20
- **Justification:** Good ITSM recipes, but needs more core architecture pattern templates.
- **Improvement:** Ship a catalog of pre-defined, Azure-validated architecture templates.

### 27. Performance
- **Score:** 80/100
- **Weight:** 1
- **Weighted deficiency signal:** 20
- **Justification:** Basic rate limiting exists, but missing deep performance metrics.
- **Improvement:** Implement response caching for non-mutating heavy reads (like complex graphs).

### 28. Accessibility
- **Score:** 85/100
- **Weight:** 1
- **Weighted deficiency signal:** 15
- **Justification:** WCAG 2.2 AA target is a great baseline.

### 29. Customer Self-Sufficiency
- **Score:** 85/100
- **Weight:** 1
- **Weighted deficiency signal:** 15
- **Justification:** Comprehensive pilot guides.

### 30. Availability
- **Score:** 85/100
- **Weight:** 1
- **Weighted deficiency signal:** 15
- **Justification:** 99.9% SLO is standard.

### 31. Scalability
- **Score:** 85/100
- **Weight:** 1
- **Weighted deficiency signal:** 15
- **Justification:** Azure SQL and Container Apps scale well.

### 32. Deployability
- **Score:** 85/100
- **Weight:** 1
- **Weighted deficiency signal:** 15
- **Justification:** Docker and Terraform simplify deployment.

### 33. Testability
- **Score:** 85/100
- **Weight:** 1
- **Weighted deficiency signal:** 15
- **Justification:** Integration and UI tests exist.

### 34. Extensibility
- **Score:** 85/100
- **Weight:** 1
- **Weighted deficiency signal:** 15
- **Justification:** Webhooks and Service Bus enable extension.

### 35. Evolvability
- **Score:** 85/100
- **Weight:** 1
- **Weighted deficiency signal:** 15
- **Justification:** DbUp and versioned APIs protect evolution.

### 36. Cognitive Load
- **Score:** 85/100
- **Weight:** 1
- **Weighted deficiency signal:** 15
- **Justification:** Progressive disclosure minimizes overload.

### 37. Cost-Effectiveness
- **Score:** 85/100
- **Weight:** 1
- **Weighted deficiency signal:** 15
- **Justification:** Clear pilot cost model limits LLM spend surprises.

### 38. Auditability
- **Score:** 95/100
- **Weight:** 2
- **Weighted deficiency signal:** 10
- **Justification:** 78 typed events with CSV export is excellent.

### 39. Procurement Readiness
- **Score:** 95/100
- **Weight:** 2
- **Weighted deficiency signal:** 10
- **Justification:** Automated procurement pack builder is best-in-class.

### 40. Change Impact Clarity
- **Score:** 90/100
- **Weight:** 1
- **Weighted deficiency signal:** 10
- **Justification:** Diffing and comparison replay are highly effective.

### 41. Azure Compatibility and SaaS Deployment
- **Score:** 95/100
- **Weight:** 2
- **Weighted deficiency signal:** 10
- **Justification:** Native integration with Key Vault, Entra, SQL, and Front Door.

### 42. Supportability
- **Score:** 90/100
- **Weight:** 1
- **Weighted deficiency signal:** 10
- **Justification:** `archlucid doctor` and diagnostics bundles are top-tier.

### 43. Manageability
- **Score:** 90/100
- **Weight:** 1
- **Weighted deficiency signal:** 10
- **Justification:** CLI tooling is robust.

### 44. Modularity
- **Score:** 90/100
- **Weight:** 1
- **Weighted deficiency signal:** 10
- **Justification:** Clean architecture boundaries.

### 45. Documentation
- **Score:** 95/100
- **Weight:** 1
- **Weighted deficiency signal:** 5
- **Justification:** Best-in-class documentation.

### 46. Azure Ecosystem Fit
- **Score:** 95/100
- **Weight:** 1
- **Weighted deficiency signal:** 5
- **Justification:** Perfect alignment with Azure best practices.

---

## Top 10 Most Important Weaknesses

1. **UI Mocking Masks Real API Integration Bugs:** Relying on deterministic mocks for Playwright E2E tests creates a false sense of security and risks contract drift in production.
2. **Shallow Default Observability:** Standard logging is insufficient for distributed enterprise SaaS; missing native OpenTelemetry (OTLP) integration limits operator troubleshooting.
3. **First-Party Connectors Are Thin:** While promised for V1, ITSM connectors (Jira/ServiceNow) lack robust bidirectional status sync, limiting true enterprise workflow embeddedness.
4. **Rate Limiting Lacks Granular Metrics:** Rate limiting drops traffic effectively but fails to provide transparent metrics or webhook alerts for operators when thresholds are breached.
5. **Tenant Data Pruning Strategy is Incomplete:** Unbounded growth in funnel/audit tables without automated, configurable pruning scripts poses a long-term cost and compliance risk.
6. **Cross-Tenant Separation Relies on Application Layering:** While SQL RLS is used, the lack of strict schema or physical isolation means a single SQL bug could leak data.
7. **Keyset Pagination Edge Cases:** Pagination logic utilizing `OccurredUtc` can fail deterministically if `EventId` is omitted, causing missed or duplicated audit logs.
8. **UI Shaping vs Entitlements:** Relying on UI progressive disclosure (`useOperateCapability()`) without hard entitlement backend blocks creates a confusing security posture for auditors.
9. **Pilot Metrics Heavily Qualitative:** The automated ROI model is excellent, but still heavily relies on operator-supplied qualitative estimates rather than system-derived time-savings.
10. **LLM Cost Control is Reactive:** LLM token usage is tracked, but there are no proactive, hard circuit breakers to shut down rogue agents consuming massive token budgets.

---

## Top 5 Monetization Blockers

1. **Deferred Commerce Un-hold:** Without live Stripe keys and a Published Marketplace offer, the product cannot transact self-serve, throttling PLG momentum.
2. **Lack of Deep ITSM Integration:** Enterprise buyers will resist paying full price if ArchLucid cannot seamlessly synchronize statuses with their existing Jira/ServiceNow instances.
3. **No Named Public Reference Customer:** The absence of a published, named reference customer (deferred to V1.1) creates significant friction when selling to risk-averse enterprise sponsors.
4. **Qualitative ROI Vulnerability:** If the automated ROI DOCX relies too heavily on estimated, self-reported baselines, skeptical CFOs will reject the business case.
5. **No Native Cloud Billing Integration:** Deferred Azure Marketplace transactability prevents buyers from using committed cloud spend (MACC) to purchase ArchLucid.

---

## Top 5 Enterprise Adoption Blockers

1. **Absence of CPA-issued SOC 2:** Even with a strong self-assessment, large enterprises mandate a formal SOC 2 Type II report before allowing SaaS vendors to process architecture data.
2. **Lack of Third-Party Penetration Test:** Owner-conducted pen-testing is insufficient for enterprise procurement, which demands an external, redacted assessor summary.
3. **Manual SCIM Provisioning:** Setting up Entra ID / SCIM provisioning requires significant hands-on operator configuration rather than a streamlined, one-click enterprise app.
4. **Vague Cross-Tenant Isolation Policies:** While RLS exists, enterprises often demand dedicated databases or single-tenant deployments, which ArchLucid currently handles via logical separation.
5. **Missing Advanced Archival/Retention Controls:** Enterprises require strict data retention policies (e.g., automatically deleting LLM prompts after 30 days), which are currently manual or undefined.

---

## Top 5 Engineering Risks

1. **Mock-Induced Contract Drift:** Playwright UI tests using mocked API responses could easily miss breaking changes introduced in the C# backend.
2. **DbUp Startup Failure Cascades:** If a database migration fails, the API refuses to start, potentially causing severe availability drops during deployments rather than degrading gracefully.
3. **Unbounded Audit Log Growth:** Without aggressive, automated partitioning or archival, the `dbo.AuditEvents` table will degrade performance and inflate Azure SQL costs over time.
4. **Keyset Pagination Inconsistencies:** Returning a list of events without strictly enforcing `EventId` alongside `OccurredUtc` guarantees pagination bugs under high load.
5. **Missing OpenTelemetry:** Diagnosing latency issues between the API, SQL, and Azure OpenAI is incredibly difficult without distributed OTLP traces.

---

## Most Important Truth

ArchLucid possesses a phenomenal documentation, process, and go-to-market layer that outshines its actual automated, verifiable integration depth. To win in enterprise SaaS, the focus must shift immediately from defining the product's scope to proving its integration points (API, UI, ITSM) work flawlessly under realistic, unmocked load.

---

## Top Improvement Opportunities

### 1. Introduce Live API E2E Testing for Playwright
**Why it matters:** Mocked UI tests hide API contract drift and integration bugs.
**Expected impact:** Directly improves Correctness (+10-15 pts), Testability (+5-10 pts), and Reliability (+3-5 pts). Weighted readiness impact: +0.6-0.9%.
**Actionable now:** Yes.
**Cursor Prompt:**
```text
Modify the Playwright configuration in `archlucid-ui` to support running tests against a live local API. 
1. In `archlucid-ui/package.json`, add a script `"test:e2e:live": "playwright test --project=chromium --config=playwright.live.config.ts"`.
2. Create `archlucid-ui/playwright.live.config.ts` that copies the existing config but points the `baseURL` to `http://localhost:5128` and disables the deterministic MSW/mocking layer.
3. Ensure no existing mock-based scripts (`test:e2e`) are modified or broken.
Acceptance Criteria: Running `npm run test:e2e:live` hits the local .NET API running on port 5128.
Constraints: Do not rewrite the actual test files; just configure the environment so the existing tests attempt to hit the real endpoints.
```

### 2. Implement OpenTelemetry (OTLP) Tracing
**Why it matters:** Troubleshooting distributed agent tasks and DB queries is currently blind without OTLP.
**Expected impact:** Directly improves Observability (+15-20 pts), Supportability (+5-10 pts). Weighted readiness impact: +0.4-0.6%.
**Actionable now:** Yes.
**Cursor Prompt:**
```text
Implement OpenTelemetry tracing in the `ArchLucid.Api` project.
1. Add NuGet packages `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Instrumentation.AspNetCore`, and `OpenTelemetry.Exporter.OpenTelemetryProtocol` to `ArchLucid.Api.csproj`.
2. In `Program.cs`, configure `builder.Services.AddOpenTelemetry().WithTracing(...)` to instrument ASP.NET Core requests.
3. Configure the OTLP exporter to send data to the endpoint specified by the `OTEL_EXPORTER_OTLP_ENDPOINT` environment variable.
Acceptance Criteria: When the environment variable is set, the API emits traces for incoming HTTP requests.
Constraints: Do not modify existing `ILogger` configurations or remove any existing Application Insights telemetry if present.
```

### 3. DEFERRED: Bi-Directional Jira Status Sync
**Reason deferred:** Requires explicit definition of the status mapping matrix (e.g., ArchLucid "Resolved" -> Jira "Done") and authentication preference (OAuth vs API Token) before implementation can begin.
**Needed from you:** Please provide the exact Jira to ArchLucid status mapping table and confirm whether we should target Basic Auth (API Token) or OAuth 2.0 for the V1 release.

### 4. Enforce Deterministic Audit Pagination
**Why it matters:** Pagination relying solely on timestamps fails when multiple events occur in the same millisecond.
**Expected impact:** Directly improves Correctness (+5-8 pts), Auditability (+2-5 pts). Weighted readiness impact: +0.2-0.4%.
**Actionable now:** Yes.
**Cursor Prompt:**
```text
Update the audit log search endpoint in `ArchLucid.Api/Controllers/Admin/AuditController.cs`.
1. Locate the `GET /v1/audit/search` method.
2. Add validation logic: if the `beforeUtc` query parameter is provided, the `beforeEventId` parameter MUST also be provided.
3. If `beforeUtc` is present but `beforeEventId` is null/empty, return a `400 Bad Request` with a clear error message stating deterministic pagination requires both fields.
Acceptance Criteria: API rejects invalid pagination requests, preventing data duplication or drops.
Constraints: Do not change the SQL query logic; only add validation at the controller level.
```

### 5. Implement Slack Webhook Delivery Channel
**Why it matters:** Slack chat-ops parity with Teams is a V1 commitment.
**Expected impact:** Directly improves Interoperability (+5-10 pts), Workflow Embeddedness (+5-8 pts). Weighted readiness impact: +0.3-0.5%.
**Actionable now:** Yes.
**Cursor Prompt:**
```text
Implement a Slack-compatible webhook delivery channel in `ArchLucid.Application`.
1. Create a class `SlackWebhookDeliveryChannel` that implements `IWebhookDeliveryChannel`.
2. The channel should accept an `EnabledTriggersJson` configuration and an Azure Key Vault secret reference for the Slack webhook URL.
3. Implement the delivery logic to format the `Authority` payload into a basic Slack Block Kit message (using standard JSON serialization) and POST it to the webhook URL.
Acceptance Criteria: The system can route an alert/digest to Slack using the standard webhook pipeline.
Constraints: Do not build an interactive Slack app (no OAuth or interactive buttons); just outbound webhook delivery.
```

### 6. ServiceNow Incident Creation Client
**Why it matters:** First-party ServiceNow integration is a core V1 GA commitment.
**Expected impact:** Directly improves Workflow Embeddedness (+8-12 pts), Interoperability (+5-10 pts). Weighted readiness impact: +0.4-0.7%.
**Actionable now:** Yes.
**Cursor Prompt:**
```text
Create a `ServiceNowIncidentClient` in `ArchLucid.Application`.
1. Build an HTTP client that POSTs an incident payload to the ServiceNow Table API (`/api/now/table/incident`).
2. The payload must map the ArchLucid finding's `SystemName` to `cmdb_ci`.
3. Support Basic Auth configured via `appsettings.json` (`ServiceNow:Username`, `ServiceNow:Password`, `ServiceNow:InstanceUrl`).
Acceptance Criteria: The client successfully formats a finding into a ServiceNow incident JSON payload and sends it.
Constraints: Do not implement bi-directional status syncing; only outbound incident creation.
```

### 7. DEFERRED: FirstTenantFunnel Purge Worker
**Reason deferred:** The retention window (e.g., 30, 60, or 90 days) and exact purge semantics (hard delete vs archive) have not been finalized by the owner.
**Needed from you:** Please confirm the exact retention window (in days) and whether the rows should be permanently deleted from the database or moved to cold storage.

### 8. Add Reject Metrics to Rate Limiting
**Why it matters:** Operators have no visibility when API clients are hitting the 429 rate limit.
**Expected impact:** Directly improves Observability (+5-10 pts), Manageability (+2-5 pts). Weighted readiness impact: +0.1-0.3%.
**Actionable now:** Yes.
**Cursor Prompt:**
```text
Enhance the existing RateLimiting configuration in `ArchLucid.Api/Program.cs`.
1. Locate the `AddRateLimiter` setup block.
2. Implement the `OnRejected` callback.
3. Inside the callback, use `ILogger` to log a Warning message containing the client's IP address, the endpoint path requested, and the `X-Correlation-ID`.
Acceptance Criteria: 429 Too Many Requests responses result in a clear warning log for operators to monitor abuse.
Constraints: Ensure the logging operation does not block the response or throw exceptions if the correlation ID is missing.
```

### 9. Add Confluence Cloud Markdown Publisher
**Why it matters:** Confluence is the required Atlassian V1 connector to publish architecture artifacts.
**Expected impact:** Directly improves Workflow Embeddedness (+5-8 pts), Interoperability (+5-8 pts). Weighted readiness impact: +0.3-0.5%.
**Actionable now:** Yes.
**Cursor Prompt:**
```text
Create a `ConfluenceBasicAuthPublisher` class in a new or existing integrations folder in `ArchLucid.Application`.
1. The publisher should accept a Markdown string and post it to the Confluence Cloud REST API (`/wiki/api/v2/pages`).
2. Authenticate using Basic Auth (Email + API Token) loaded from configuration (`Confluence:Email`, `Confluence:ApiToken`).
3. Publish to a single fixed space defined by `Confluence:DefaultSpaceKey`.
Acceptance Criteria: ArchLucid can publish a run summary to a specific Confluence space.
Constraints: Do not implement OAuth 2.0; stick to the MVP Basic Auth pattern specified for V1.
```

### 10. Degraded Mode for Non-Critical Migrations
**Why it matters:** Hard failing the API startup due to minor schema changes hurts availability.
**Expected impact:** Directly improves Reliability (+5-10 pts), Availability (+2-5 pts). Weighted readiness impact: +0.2-0.4%.
**Actionable now:** Yes.
**Cursor Prompt:**
```text
Update the DbUp migration execution logic in `ArchLucid.Persistence` (likely called from `Program.cs` or an extension method).
1. Wrap the migration execution in a try/catch block.
2. If a migration fails, log a Critical error but check a new configuration flag `DbUp:AllowDegradedStartup`.
3. If `AllowDegradedStartup` is true, allow the API to start but set a global health check flag `IsDegraded = true` so the `/health` endpoint reports the degraded state.
Acceptance Criteria: The application can start even if a database migration fails, provided the configuration allows it.
Constraints: The default behavior must remain `AllowDegradedStartup = false` to preserve the current fail-fast safety.
```

---

## Pending Questions for Later

**Bi-Directional Jira Status Sync**
- What is the exact mapping between ArchLucid finding statuses (e.g., Open, Resolved, Ignored) and Jira statuses?
- Should the V1 implementation target Basic Auth (API Token) or OAuth 2.0?

**FirstTenantFunnel Purge Worker**
- What is the exact retention window in days for funnel events?
- Should the events be hard-deleted from SQL, or moved to a cheaper storage tier?
