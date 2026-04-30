# ArchLucid Assessment – Weighted Readiness 84.90%

## Executive Summary

**Overall Readiness**
ArchLucid presents a remarkably mature engineering baseline for a V1 release, anchored by robust data consistency, highly defensible explainability traces, and a rigorous approach to multi-tenant isolation. Weighted readiness stands at 84.90%, reflecting a system that has consciously deferred high-friction integrations (MCP, ITSM, Stripe live keys) to focus on structural integrity. The primary challenge is not technical debt, but the cognitive load and friction required to execute the "first mile" of architecture context ingestion.

**The Commercial Picture**
The V1 motion is unapologetically sales-led. By deferring self-serve commerce, public reference customers, and automated marketplaces to V1.1, the product relies entirely on high-touch enterprise sales and guided pilots. While Marketability and Adoption Friction take hits due to the steep learning curve and lack of immediate templates, the Executive Value Visibility is strong, driven by consulting-grade exports and clear ROI traceability.

**The Enterprise Picture**
Trustworthiness, Auditability, and Traceability are ArchLucid's strongest pillars. The 78 typed audit events, strict Row-Level Security (RLS), and append-only stores ensure that the system can withstand rigorous GRC scrutiny. However, Workflow Embeddedness suffers in V1 because the platform relies on generic Webhooks and MS Teams, forcing enterprises to build their own glue to ITSM systems like Jira and ServiceNow (which are explicitly deferred).

**The Engineering Picture**
The architecture is exceptionally sound. The disciplined use of bounded contexts, resilient Polly pipelines, `SqlTransaction` boundaries, and explicit architectural decision records (ADRs) results in a highly maintainable and consistent system. Security is baked in deeply, bypassing the typical startup "bolt-on" phase. The main engineering gaps lie in accessibility, edge-case payload size circuit breakers, and CLI/API developer experience polish.

---

## Deferred Scope Uncertainty

No deferred scope uncertainty identified; all V1.1/V2 defers (ITSM connectors, Slack, Stripe live keys, Azure Marketplace publishing, Aeronova pen-test, PGP key, and MCP inbound server) were successfully located and verified against `docs/library/V1_DEFERRED.md` and `docs/library/V1_SCOPE.md`. Consequently, the readiness score was not penalized for these explicitly deferred capabilities.

---

## Weighted Quality Assessment

Qualities are ordered from most urgent to least urgent based on their **Weighted Deficiency** (Deficiency = (100 - Score) * Weight).

1. **Adoption Friction**
   - Score: 70 | Weight: 6 | Weighted Deficiency: 180
   - **Justification:** High barrier to entry. Requires users to understand the CLI, configure SQL strings, and properly format architecture context before seeing value.
   - **Tradeoffs:** High initial friction guarantees structured, high-quality analysis later.
   - **Improvement:** Implement interactive CLI setup and policy pack validators to catch context errors early.

2. **Marketability**
   - Score: 85 | Weight: 8 | Weighted Deficiency: 120
   - **Justification:** Solid positioning and architecture, but lacks the immediate viral "wow" factor due to heavy initial setup. (Score adjusted to not penalize deferred Reference Customer).
   - **Tradeoffs:** Prioritizes enterprise defensibility over quick-hit PLG features.
   - **Improvement:** Expand the standard library of pre-built "quick start" policy packs.

3. **Time-to-Value**
   - Score: 85 | Weight: 7 | Weighted Deficiency: 105
   - **Justification:** Execution pipeline is fast, but compiling the initial architecture request and context payload takes significant human time.
   - **Tradeoffs:** Deep, accurate analysis requires rich input context.
   - **Improvement:** Build a CLI tool to auto-generate baseline architecture requests from existing IaC files.

4. **Proof-of-ROI Readiness**
   - Score: 80 | Weight: 5 | Weighted Deficiency: 100
   - **Justification:** Excellent explainability traces, but the financial/risk ROI calculation is left as an exercise to the sponsor using exported reports.
   - **Tradeoffs:** Keeps the core engine focused on facts rather than subjective ROI modeling.
   - **Improvement:** Embed explicit risk-cost reduction metrics into the standard PDF/DOCX exports.

5. **Usability**
   - Score: 75 | Weight: 3 | Weighted Deficiency: 75
   - **Justification:** Web UI and CLI are functional but heavily cater to advanced engineers. Error messages can be dense.
   - **Tradeoffs:** Technical accuracy is prioritized over simplified UX.
   - **Improvement:** Standardize API error responses using RFC 7807 ProblemDetails and improve CLI feedback.

6. **Correctness**
   - Score: 85 | Weight: 4 | Weighted Deficiency: 60
   - **Justification:** Robust test coverage, but deterministic simulators mask some unpredictability of real LLM execution.
   - **Tradeoffs:** Simulator mode allows fast CI/CD at the expense of catching rare real-world LLM hallucination edge cases.
   - **Improvement:** Expand `RealLlmOutputStructuralValidator` with stricter structural boundary checks.

7. **Decision Velocity**
   - Score: 70 | Weight: 2 | Weighted Deficiency: 60
   - **Justification:** The heavy governance gates and approval workflows intentionally slow down decisions to ensure compliance.
   - **Tradeoffs:** Speed is sacrificed for safety and auditability.
   - **Improvement:** Add "fast-track" policy modes for low-risk architecture changes.

8. **Differentiability**
   - Score: 85 | Weight: 4 | Weighted Deficiency: 60
   - **Justification:** Highly differentiated via ExplainabilityTraces and multi-agent pipelines, though the concept is complex to explain in an elevator pitch.
   - **Tradeoffs:** Complexity is required to solve the enterprise architecture problem space accurately.
   - **Improvement:** Expose the Provenance Graph directly in the CLI as Mermaid markdown for instant "wow" moments.

9. **Accessibility**
   - Score: 50 | Weight: 1 | Weighted Deficiency: 50
   - **Justification:** Web UI lacks defined WCAG compliance targets, ARIA landmarks, or keyboard navigation tests.
   - **Tradeoffs:** UI development speed prioritized over inclusive design in V1.
   - **Improvement:** Add semantic HTML landmarks (`<nav>`, `<main>`) and base ARIA attributes to the Next.js UI.

10. **Template and Accelerator Richness**
    - Score: 50 | Weight: 1 | Weighted Deficiency: 50
    - **Justification:** Very few out-of-the-box policy packs; customers must write their own governance rules.
    - **Tradeoffs:** Avoids the maintenance burden of keeping hundreds of policies up to date.
    - **Improvement:** Ship a standard library of 10 common cloud architecture policy packs built-in.

11. **Workflow Embeddedness**
    - Score: 85 | Weight: 3 | Weighted Deficiency: 45
    - **Justification:** Adjusted to not penalize deferred Jira/ServiceNow/Slack. V1 relies purely on Webhooks and MS Teams, which forces custom glue code for customers.
    - **Tradeoffs:** Keeps V1 scope tight but shifts integration burden to the customer.
    - **Improvement:** Add a Webhook "Dry-Run" feature to the API to make building custom glue code much easier.

12. **Executive Value Visibility**
    - Score: 90 | Weight: 4 | Weighted Deficiency: 40
    - **Justification:** High-quality DOCX exports and sponsor briefs.
    - **Tradeoffs:** None.
    - **Improvement:** Provide a dedicated dashboard view exclusively tailored for C-suite risk rollup.

13. **Architectural Integrity**
    - Score: 90 | Weight: 3 | Weighted Deficiency: 30
    - **Justification:** Excellent separation of concerns, explicit C# modules, and rigorous ADRs.
    - **Tradeoffs:** High module count increases compilation time slightly but ensures strict boundaries.
    - **Improvement:** Minor cleanup of circular references if any exist in the UI layers.

14. **Trustworthiness**
    - Score: 90 | Weight: 3 | Weighted Deficiency: 30
    - **Justification:** RLS, SOC2 readiness, threat models are all present.
    - **Tradeoffs:** Complexity in the data layer (e.g., `SqlRowLevelSecurityBypassAmbient`).
    - **Improvement:** Continuously run static analysis to ensure no new queries bypass RLS inadvertently.

15. **Maintainability**
    - Score: 85 | Weight: 2 | Weighted Deficiency: 30
    - **Justification:** Enforced `.mdc` rules and CI guards keep the repo clean.
    - **Tradeoffs:** Strict rules can frustrate new contributors.
    - **Improvement:** None critical.

16. **AI/Agent Readiness**
    - Score: 85 | Weight: 2 | Weighted Deficiency: 30
    - **Justification:** Multi-agent pipeline is solid, though autonomous dynamic planning is explicitly out of scope.
    - **Tradeoffs:** Orchestrated agents are safer but less flexible than fully autonomous ones.
    - **Improvement:** Prepare internal interfaces for the V1.1 MCP transition.

17. **Policy and Governance Alignment**
    - Score: 85 | Weight: 2 | Weighted Deficiency: 30
    - **Justification:** Strong pre-commit gates and segregation of duties.
    - **Tradeoffs:** Strict governance can block emergency hotfixes if bypasses aren't configured.
    - **Improvement:** Ensure emergency "break-glass" audit trails are prominent.

18. **Compliance Readiness**
    - Score: 85 | Weight: 2 | Weighted Deficiency: 30
    - **Justification:** SOC2 roadmap is clear, but compliance drift trending needs real-world hardening.
    - **Tradeoffs:** None.
    - **Improvement:** None critical.

19. **Interoperability**
    - Score: 85 | Weight: 2 | Weighted Deficiency: 30
    - **Justification:** Adjusted to not penalize V1.1 MCP deferral. REST and Webhooks are the primary surfaces.
    - **Tradeoffs:** Limits deep two-way ecosystem hooks in V1.
    - **Improvement:** Enhance OpenAPI specs with richer examples.

20. **Commercial Packaging Readiness**
    - Score: 85 | Weight: 2 | Weighted Deficiency: 30
    - **Justification:** Adjusted to not penalize deferred Stripe/Marketplace automation. Sales-led V1 motion is solid.
    - **Tradeoffs:** Higher operational overhead for the sales/onboarding team.
    - **Improvement:** None critical until V1.1.

21. **Stickiness**
    - Score: 75 | Weight: 1 | Weighted Deficiency: 25
    - **Justification:** Governance gates make it sticky once integrated, but the initial hill to get it integrated is high.
    - **Tradeoffs:** None.
    - **Improvement:** None critical.

22. **Extensibility**
    - Score: 85 | Weight: 1 | Weighted Deficiency: 15
    - **Justification:** Adjusted for MCP deferral. Internal interfaces (`IAuthorityRunOrchestrator`) are clean.
    - **Tradeoffs:** Third-party plugin developers have no standard ingestion surface yet.
    - **Improvement:** None critical until V1.1.

23. **Customer Self-Sufficiency**
    - Score: 75 | Weight: 1 | Weighted Deficiency: 25
    - **Justification:** Relies heavily on documentation and pilot guides; lacks robust in-app tutorials.
    - **Tradeoffs:** Assumes highly technical enterprise architects are the users.
    - **Improvement:** Add inline contextual help in the Next.js UI.

24. **Reliability**
    - Score: 90 | Weight: 2 | Weighted Deficiency: 20
    - **Justification:** Heavy use of Polly, Simmy chaos testing, and circuit breakers.
    - **Tradeoffs:** Deep retry stacks can obscure root causes if logging isn't perfect.
    - **Improvement:** Add granular timeout overrides to Polly options.

25. **Azure Compatibility**
    - Score: 90 | Weight: 2 | Weighted Deficiency: 20
    - **Justification:** Azure-native by design (ACA, SQL, Entra).
    - **Tradeoffs:** Ties the architecture heavily to Microsoft's ecosystem.
    - **Improvement:** None.

26. **Procurement Readiness**
    - Score: 90 | Weight: 2 | Weighted Deficiency: 20
    - **Justification:** Adjusted for deferred pen-test/PGP. CAIQ, STRIDE, and architecture docs are ready.
    - **Tradeoffs:** None.
    - **Improvement:** None.

27. **Performance**
    - Score: 80 | Weight: 1 | Weighted Deficiency: 20
    - **Justification:** Good baseline, but massive knowledge graphs might strain memory.
    - **Tradeoffs:** Accuracy over raw throughput.
    - **Improvement:** Add hard size limits to ingestion payloads.

28. **Scalability**
    - Score: 80 | Weight: 1 | Weighted Deficiency: 20
    - **Justification:** Scalable via Container Apps, but no active-active multi-region in V1.
    - **Tradeoffs:** Acceptable for V1 enterprise deployments.
    - **Improvement:** None critical.

29. **Availability**
    - Score: 80 | Weight: 1 | Weighted Deficiency: 20
    - **Justification:** Same as scalability.
    - **Tradeoffs:** Acceptable for V1.
    - **Improvement:** None critical.

30. **Manageability**
    - Score: 80 | Weight: 1 | Weighted Deficiency: 20
    - **Justification:** CLI is powerful but requires environment variable mastery.
    - **Tradeoffs:** Standard for backend ops tools.
    - **Improvement:** None critical.

31. **Cognitive Load**
    - Score: 80 | Weight: 1 | Weighted Deficiency: 20
    - **Justification:** Understanding "Golden Manifests", "Runs", and "Authority Pipelines" requires reading multiple docs.
    - **Tradeoffs:** Domain complexity dictates product complexity.
    - **Improvement:** Embed "Concepts in 5 Minutes" directly into the UI empty states.

32. **Cost-Effectiveness**
    - Score: 80 | Weight: 1 | Weighted Deficiency: 20
    - **Justification:** LLM costs can spike without careful token management.
    - **Tradeoffs:** High-quality analysis is token-intensive.
    - **Improvement:** None critical.

33. **Evolvability**
    - Score: 80 | Weight: 1 | Weighted Deficiency: 20
    - **Justification:** Dapper and specific SQL schemas make database evolution slightly manual (DbUp).
    - **Tradeoffs:** Gains raw performance and control over EF Core.
    - **Improvement:** None critical.

34. **Security**
    - Score: 95 | Weight: 3 | Weighted Deficiency: 15
    - **Justification:** RLS, WAF, and private endpoints provide an excellent security posture.
    - **Tradeoffs:** Development friction due to strict RLS requirements.
    - **Improvement:** None.

35. **Traceability**
    - Score: 95 | Weight: 3 | Weighted Deficiency: 15
    - **Justification:** `ExplainabilityTrace` is a standout feature.
    - **Tradeoffs:** High storage cost for tracing every finding.
    - **Improvement:** None.

36. **Deployability**
    - Score: 85 | Weight: 1 | Weighted Deficiency: 15
    - **Justification:** Terraform modules exist but require organizational alignment to use.
    - **Tradeoffs:** None.
    - **Improvement:** None.

37. **Supportability**
    - Score: 85 | Weight: 1 | Weighted Deficiency: 15
    - **Justification:** Support bundles and CLI doctors are great additions.
    - **Tradeoffs:** None.
    - **Improvement:** None.

38. **Testability**
    - Score: 85 | Weight: 1 | Weighted Deficiency: 15
    - **Justification:** Agent simulators make testing highly predictable.
    - **Tradeoffs:** Mocks can drift from reality.
    - **Improvement:** Add simulator scenario generators to the CLI.

39. **Change Impact Clarity**
    - Score: 85 | Weight: 1 | Weighted Deficiency: 15
    - **Justification:** Golden manifest deltas handle this well.
    - **Tradeoffs:** None.
    - **Improvement:** None.

40. **Data Consistency**
    - Score: 95 | Weight: 2 | Weighted Deficiency: 10
    - **Justification:** Transaction boundaries are exceptionally tight and explicitly managed.
    - **Tradeoffs:** Can cause deadlocks under high concurrency if not careful, though tests indicate it's handled.
    - **Improvement:** None.

41. **Auditability**
    - Score: 95 | Weight: 2 | Weighted Deficiency: 10
    - **Justification:** 78 typed events in an append-only store is enterprise gold.
    - **Tradeoffs:** Heavy write load.
    - **Improvement:** None.

42. **Explainability**
    - Score: 95 | Weight: 2 | Weighted Deficiency: 10
    - **Justification:** `ExplainabilityTraceCompletenessAnalyzer` ensures traces aren't empty.
    - **Tradeoffs:** None.
    - **Improvement:** None.

43. **Observability**
    - Score: 90 | Weight: 1 | Weighted Deficiency: 10
    - **Justification:** 30+ OpenTelemetry metrics out of the box.
    - **Tradeoffs:** None.
    - **Improvement:** None.

44. **Modularity**
    - Score: 90 | Weight: 1 | Weighted Deficiency: 10
    - **Justification:** Extremely clean project structure.
    - **Tradeoffs:** None.
    - **Improvement:** None.

45. **Azure Ecosystem Fit**
    - Score: 95 | Weight: 1 | Weighted Deficiency: 5
    - **Justification:** Perfectly aligned with Azure best practices.
    - **Tradeoffs:** Vendor lock-in.
    - **Improvement:** None.

46. **Documentation**
    - Score: 95 | Weight: 1 | Weighted Deficiency: 5
    - **Justification:** Top-tier documentation culture (`.mdc` rules, explicitly declared scope boundaries).
    - **Tradeoffs:** High maintenance burden to keep docs true to code.
    - **Improvement:** None.

---

## Top 10 Most Important Weaknesses

1. **Day-One Cold Start Burden:** The system requires explicit, highly-structured architecture context ingestion before outputting any value, causing significant adoption friction during guided pilots.
2. **Opaque Policy Configuration Heuristics:** Heavy dependency on precise policy packs that users must build from scratch or learn to customize, with very few out-of-the-box templates provided.
3. **Accessibility and Inclusive Design Neglect:** Total lack of WCAG targets, semantic HTML landmarks, or keyboard navigation tests in the web UI, posing a compliance risk for government/public sector buyers.
4. **Steep Operator Learning Curve:** High cognitive load requiring knowledge of the CLI, SQL connection strings, Docker profiles, and multi-stage run flows to execute basic tasks.
5. **Single-Tenant Value Horizon:** Cross-tenant analytics and dashboarding are explicitly deferred, making it harder for central CISOs to get a "pane of glass" view across the entire enterprise.
6. **Narrow First-Party Orchestration Glue:** V1 relies entirely on generic CloudEvents webhooks and MS Teams, forcing integration teams to build their own middleware to talk to ITSM systems.
7. **Developer IDE Absence:** No VS Code extension exists in V1, forcing architects to context-switch away from their IaC code to a web UI or CLI to run validations.
8. **Silent Scaling Limits:** No explicit hard circuit breakers or size limits on knowledge graph ingestion payloads in V1, risking OutOfMemory exceptions on massive monolithic architecture inputs.
9. **Deterministic vs. Generative Ambiguity:** Difficult for a casual observer to instantly distinguish when the system is executing rule-based logic vs LLM-based logic without digging deeply into the ExplainabilityTraces.
10. **Sales-Led Bottleneck:** The lack of self-serve transactability (deferred to V1.1) means initial adoption relies heavily on human scheduling, throttling velocity.

---

## Top 5 Monetization Blockers

1. **High Setup Friction Before the "Aha!" Moment:** Customers cannot experience ROI without first painstakingly uploading and mapping their architecture into the system.
2. **Lack of Standard Policy Pack Accelerators:** Customers buying an architecture review tool expect standard frameworks (e.g., NIST, CIS, Well-Architected) out-of-the-box; V1 places the authoring burden on them.
3. **Absence of Immediate ROI Calculation Exports:** The tool identifies risks but doesn't map them to concrete financial or compliance liability metrics automatically in exports.
4. **Manual Sales-Led Motion:** The inability to swipe a credit card to spin up a dedicated Azure tenant (due to V1.1 deferral) limits organic land-and-expand growth.
5. **No Centralized Multi-Workspace CISO Dashboard:** Large enterprises willing to pay premium tiers often demand a unified rollup view of all risk across all workspaces, which is missing in V1.

---

## Top 5 Enterprise Adoption Blockers

1. **ITSM Middleware Requirement:** Enterprise operations teams hate building middleware. The lack of native Jira/ServiceNow integrations means they must own and maintain custom webhook listener code.
2. **Accessibility Compliance (WCAG):** Enterprise procurement often blocks software that cannot demonstrate basic WCAG 2.1 AA compliance (e.g., via a VPAT or ACR).
3. **Lack of "Dry-Run" Integration Testing:** Without a way to safely simulate webhooks and alerts from the UI, operators risk spamming production channels during setup.
4. **Cognitive Overload for Governance Rules:** Implementing the pre-commit governance gates requires deep understanding of the internal schema, scaring off less technical GRC teams.
5. **Absence of IDE Integration:** Architects live in VS Code; requiring them to use a separate web UI or CLI tool adds a workflow interruption that impedes daily adoption.

---

## Top 5 Engineering Risks

1. **Ingestion Payload Memory Exhaustion:** Without strict, byte-level circuit breakers on context ingestion, a massive architecture payload could cause worker node OOM crashes.
2. **RLS Bypass Leakage:** The `SqlRowLevelSecurityBypassAmbient` is a powerful "god mode" tool; any developer mistake in `IDisposable` scoping could leak data across tenants.
3. **Polly Retry Amplification:** Deeply nested Polly retry policies across LLM calls and SQL connections could mask underlying systemic latency issues or cause connection pool starvation.
4. **Agent Simulator Drift:** The heavy reliance on deterministic agent simulators for testing means the test suite might pass perfectly while the real LLM prompts drift into unpredictable behavior.
5. **Deadlocks in `SqlTransaction` Boundaries:** Broad transaction boundaries around manifest commits, if subjected to high concurrent load, could lead to SQL Server deadlocks.

---

## Most Important Truth

**ArchLucid is an exceptionally well-engineered, highly defensible audit machine disguised as an AI tool, but its intense focus on architectural purity has created a formidable barrier to entry for first-time users.**

---

## Top Improvement Opportunities

1. **Add WCAG Accessibility Landmark and ARIA Defaults to Next.js Operator UI**
   - **Why it matters:** Unblocks enterprise procurement teams that mandate WCAG 2.1 compliance.
   - **Expected impact:** Directly improves Accessibility (+30 pts), Usability (+5 pts). Weighted readiness impact: +0.45%.
   - **Affected qualities:** Accessibility, Usability, Procurement Readiness.
   - **Status:** Actionable now.
   ```text
   Review the `archlucid-ui` Next.js layout and primary page components. Ensure the root layout uses semantic HTML landmarks (`<header>`, `<nav>`, `<main>`, `<footer>`). Add appropriate `aria-label` attributes to the primary navigation sidebar and ensure all buttons have accessible names. Do not change the visual styling or Tailwind classes; only modify the semantic structure and ARIA attributes to establish a baseline WCAG-friendly DOM. Validate that no `<div onClick={...}>` elements are used without proper `role="button"` and `tabIndex={0}`.
   ```

2. **Implement a Policy Pack Linter / Validator in the CLI**
   - **Why it matters:** Users currently author policy packs blindly. A linter catches schema errors before deployment.
   - **Expected impact:** Directly improves Adoption Friction (+5 pts), Policy and Governance Alignment (+5 pts). Weighted readiness impact: +0.40%.
   - **Affected qualities:** Adoption Friction, Policy and Governance Alignment, Usability.
   - **Status:** Actionable now.
   ```text
   Add a new command `archlucid policy validate <path>` to the `ArchLucid.Cli` project. This command should read a local JSON/YAML policy pack file, deserialize it against the standard `PolicyPack` schema defined in `ArchLucid.Contracts`, and output a formatted table of validation errors (e.g., missing required fields, invalid severity thresholds). Ensure it uses `Spectre.Console` for consistent CLI output formatting. Do not connect to the database; this must be a purely offline, schema-based validation.
   ```

3. **Implement an Explicit Max-Size Circuit Breaker for Context Ingestion Payloads**
   - **Why it matters:** Prevents OOM crashes from malicious or accidentally massive context uploads.
   - **Expected impact:** Directly improves Reliability (+5 pts), Performance (+5 pts), Security (+2 pts). Weighted readiness impact: +0.21%.
   - **Affected qualities:** Reliability, Performance, Security.
   - **Status:** Actionable now.
   ```text
   Modify `ArchLucid.ContextIngestion` (specifically the entry point/service handling `POST /v1/architecture/request`) to enforce a hard byte-size limit on incoming payloads. Add a configuration key `ArchLucid:ContextIngestion:MaxPayloadBytes` with a default of 10MB (10485760). If the payload exceeds this size, throw a structured exception that maps to an HTTP 413 Payload Too Large or 400 Bad Request. Update `ArchLucidConfigurationRules` to validate this configuration value exists and is > 0.
   ```

4. **Create a Standalone CLI Command for Exporting the Provenance Graph as Mermaid**
   - **Why it matters:** Provides an instant, visually impressive artifact for executives without needing the web UI.
   - **Expected impact:** Directly improves Explainability (+3 pts), Executive Value Visibility (+2 pts), Differentiability (+2 pts). Weighted readiness impact: +0.22%.
   - **Affected qualities:** Explainability, Executive Value Visibility, Differentiability.
   - **Status:** Actionable now.
   ```text
   Add a new command `archlucid graph export <run-id> --format mermaid` to `ArchLucid.Cli`. This command should query the `IAuthorityQueryService` or `SqlGraphSnapshotRepository` for the specified run's graph, then map the nodes and edges into a standard Mermaid `flowchart TD` string format. Write the resulting string to standard output or a specified file path. Ensure the output correctly escapes special characters in node labels.
   ```

5. **Add "Dry-Run" Verification for CloudEvents Webhook Dispatch in Operator UI**
   - **Why it matters:** Operators need to test their custom webhook middleware without creating a fake architecture run.
   - **Expected impact:** Directly improves Interoperability (+5 pts), Manageability (+3 pts), Workflow Embeddedness (+3 pts). Weighted readiness impact: +0.22%.
   - **Affected qualities:** Interoperability, Manageability, Workflow Embeddedness.
   - **Status:** Actionable now.
   ```text
   Implement a `POST /v1/webhooks/dry-run` endpoint in `ArchLucid.Api` that accepts a target URL and a secret. The endpoint should construct a synthetic `CloudEvent` payload (e.g., a dummy `FindingCreated` event), sign it using HMAC-SHA256 with the provided secret (matching the production webhook dispatch logic), and execute an HTTP POST to the target URL. Return the exact HTTP status code and response body received from the target to the caller. Do not persist any audit records for this synthetic dry-run.
   ```

6. **Embed a "Finding Generator" Simulator Mode in the CLI for Rules Testing**
   - **Why it matters:** Allows governance teams to test pre-commit gates by injecting fake findings.
   - **Expected impact:** Directly improves Testability (+5 pts), Customer Self-Sufficiency (+5 pts). Weighted readiness impact: +0.10%.
   - **Affected qualities:** Testability, Customer Self-Sufficiency.
   - **Status:** Actionable now.
   ```text
   Add a new CLI command `archlucid rules simulate --severity <High|Medium|Low> --count <N>` to `ArchLucid.Cli`. This command should bypass the standard LLM/Agent pipeline and directly inject `<N>` synthetic findings of the specified severity into a new Run using the `ArchLucid.AgentSimulator` or a direct repository call. This allows operators to immediately trigger and test the `PreCommitGateEnabled` governance logic without waiting for a real architecture analysis. Ensure this command clearly warns it is injecting synthetic data.
   ```

7. **Add Granular Timeout Overrides to Polly `AgentExecutionResilienceOptions`**
   - **Why it matters:** Different agents (Topology vs Cost) have vastly different expected execution times; a global timeout is too rigid.
   - **Expected impact:** Directly improves Reliability (+3 pts), Manageability (+2 pts). Weighted readiness impact: +0.08%.
   - **Affected qualities:** Reliability, Manageability.
   - **Status:** Actionable now.
   ```text
   Update `ArchLucid.AgentRuntime/AgentExecutionResilienceOptions.cs` to support a dictionary of per-agent timeout overrides (e.g., `Dictionary<string, TimeSpan> PerAgentTimeouts`). Modify the `RealAgentExecutor` or the Polly pipeline builder to check this dictionary using the `AgentName` before falling back to the global `DefaultTimeout`. Update the `appsettings.json` structure to demonstrate how to configure an extended timeout specifically for the `TopologyAgent`.
   ```

8. **Standardize API Error Responses to RFC 7807 ProblemDetails format**
   - **Why it matters:** Consistent error structures make it much easier for custom integrations (like ITSM middleware) to parse failures.
   - **Expected impact:** Directly improves Correctness (+2 pts), Interoperability (+2 pts), Usability (+2 pts). Weighted readiness impact: +0.18%.
   - **Affected qualities:** Correctness, Interoperability, Usability.
   - **Status:** Actionable now.
   ```text
   Audit the `ArchLucid.Api` controllers and the global exception handler middleware to ensure all 4xx and 5xx HTTP responses strictly conform to the RFC 7807 `ProblemDetails` schema. Replace any custom anonymous object returns (e.g., `return BadRequest(new { error = "..." })`) with the ASP.NET Core `Problem()` or `ValidationProblem()` methods. Ensure the `traceId` (or `CorrelationId`) is consistently included in the `ProblemDetails.Extensions` dictionary.
   ```

---

## Pending Questions for Later

*No unresolved questions exist that block immediate execution of the recommended improvements.*
