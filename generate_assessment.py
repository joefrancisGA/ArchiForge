import json

qualities = [
    {"name": "Marketability", "category": "COMMERCIAL", "weight": 8, "score": 65, "justification": "The product is highly technical and focuses heavily on architectural mechanics rather than business outcomes, making it harder to sell to non-technical economic buyers.", "tradeoffs": "Precision in technical messaging vs. approachability for business sponsors.", "recommendations": "Create a 'Business Value Cheat Sheet' mapping technical features (like RLS and Golden Manifests) directly to risk reduction and cost savings.", "status": "Fixable in v1"},
    {"name": "Time-to-Value", "category": "COMMERCIAL", "weight": 7, "score": 70, "justification": "While the first pilot run is valuable, the initial setup (SQL, Entra ID, Key Vault) creates a barrier to experiencing that value quickly.", "tradeoffs": "Enterprise-grade security defaults vs. frictionless trial experience.", "recommendations": "Implement a guided sandbox onboarding mode that uses local mocks to demonstrate value before requiring full Azure setup.", "status": "Fixable in v1"},
    {"name": "Adoption Friction", "category": "COMMERCIAL", "weight": 6, "score": 60, "justification": "High operator expertise is required to configure the system, write policy packs, and understand the provenance graph.", "tradeoffs": "Flexibility and power vs. ease of use.", "recommendations": "Build a visual 'Policy Pack Builder' in the UI to reduce the need for writing raw JSON/YAML policies.", "status": "Fixable in v1.1"},
    {"name": "Proof-of-ROI Readiness", "category": "COMMERCIAL", "weight": 5, "score": 60, "justification": "The system lacks built-in mechanisms to quantify the time saved or risks avoided by catching architectural flaws pre-commit.", "tradeoffs": "Focusing on core execution vs. building reporting and analytics.", "recommendations": "Add a baseline ROI telemetry module tracking issues caught pre-commit.", "status": "Fixable in v1.1"},
    {"name": "Executive Value Visibility", "category": "COMMERCIAL", "weight": 4, "score": 55, "justification": "The UI is designed for operators. There is no default, high-level dashboard that an executive can glance at to understand overall architectural health.", "tradeoffs": "Operator-centric workflows vs. executive reporting.", "recommendations": "Implement a Workspace Health dashboard with high-level KPIs.", "status": "Fixable in v1"},
    {"name": "Differentiability", "category": "COMMERCIAL", "weight": 4, "score": 80, "justification": "The combination of agentic architecture review with strict enterprise governance (RLS, durable audit) is a strong differentiator.", "tradeoffs": "Niche focus vs. broad appeal.", "recommendations": "Highlight the pre-commit governance gate as a unique differentiator in marketing.", "status": "Strong"},
    {"name": "Decision Velocity", "category": "COMMERCIAL", "weight": 2, "score": 75, "justification": "The sales-led motion is clear, but the lack of self-serve ROI and executive dashboards slows down the final purchase decision.", "tradeoffs": "Sales-led control vs. self-serve speed.", "recommendations": "Expose ROI telemetry directly to the buyer during the pilot.", "status": "Fixable in v1.1"},
    {"name": "Commercial Packaging Readiness", "category": "COMMERCIAL", "weight": 2, "score": 80, "justification": "Tiers are documented, but enforcement mechanisms in the codebase need to be strictly aligned with the documentation.", "tradeoffs": "Complex tiering vs. simple pricing.", "recommendations": "Audit and enforce all documented feature gates in the codebase.", "status": "Fixable in v1"},
    {"name": "Stickiness", "category": "COMMERCIAL", "weight": 1, "score": 85, "justification": "Once integrated into the CI/CD pipeline and governance workflows, the product becomes a system of record and is highly sticky.", "tradeoffs": "Deep integration vs. easy offboarding.", "recommendations": "Deepen ITSM integrations.", "status": "Strong"},
    {"name": "Template and Accelerator Richness", "category": "COMMERCIAL", "weight": 1, "score": 50, "justification": "Very few out-of-the-box templates exist, forcing users to build architecture requests from scratch.", "tradeoffs": "Custom architecture vs. boilerplate.", "recommendations": "Ship standard reference architectures as built-in templates.", "status": "Fixable in v1"},

    {"name": "Traceability", "category": "ENTERPRISE", "weight": 3, "score": 85, "justification": "Strong traceability from architecture requests to golden manifests and artifacts.", "tradeoffs": "Data volume vs. granular tracking.", "recommendations": "Ensure all agent decisions link back to specific policy pack rules.", "status": "Strong"},
    {"name": "Usability", "category": "ENTERPRISE", "weight": 3, "score": 70, "justification": "The operator shell is functional but dense, exposing all data and advanced features at once, which overwhelms new users.", "tradeoffs": "Exposing all data vs. guided workflows.", "recommendations": "Implement progressive disclosure in the UI.", "status": "Fixable in v1"},
    {"name": "Workflow Embeddedness", "category": "ENTERPRISE", "weight": 3, "score": 65, "justification": "Relies heavily on webhooks for integration. First-party Jira and ServiceNow are V1 commitments but not fully realized, adding friction.", "tradeoffs": "Native connectors vs. generic webhooks.", "recommendations": "Accelerate delivery of first-party ServiceNow and Jira connectors.", "status": "Fixable in v1"},
    {"name": "Trustworthiness", "category": "ENTERPRISE", "weight": 3, "score": 90, "justification": "High trust due to RLS, durable audit log, and transparent self-assessment.", "tradeoffs": "Transparency vs. exposing vulnerabilities.", "recommendations": "Maintain rigorous audit log discipline.", "status": "Strong"},
    {"name": "Auditability", "category": "ENTERPRISE", "weight": 2, "score": 90, "justification": "The append-only SQL audit store with typed events provides excellent auditability.", "tradeoffs": "Storage costs vs. comprehensive auditing.", "recommendations": "Provide out-of-the-box SIEM export templates.", "status": "Strong"},
    {"name": "Policy and Governance Alignment", "category": "ENTERPRISE", "weight": 2, "score": 85, "justification": "Policy packs and pre-commit governance gates align well with enterprise needs.", "tradeoffs": "Strict enforcement vs. developer velocity.", "recommendations": "Add a 'dry-run' mode for policy packs.", "status": "Fixable in v1.1"},
    {"name": "Compliance Readiness", "category": "ENTERPRISE", "weight": 2, "score": 85, "justification": "Strong foundation with SOC 2 self-assessment and CAIQ/SIG templates.", "tradeoffs": "Cost of certification vs. self-attestation.", "recommendations": "Continue maintaining the SOC 2 roadmap.", "status": "Strong"},
    {"name": "Procurement Readiness", "category": "ENTERPRISE", "weight": 2, "score": 85, "justification": "The procurement pack generation script streamlines the buying process.", "tradeoffs": "Standardized responses vs. custom RFP answers.", "recommendations": "Keep the procurement pack updated.", "status": "Strong"},
    {"name": "Interoperability", "category": "ENTERPRISE", "weight": 2, "score": 75, "justification": "Relies on webhooks, but testing and debugging webhook delivery is currently difficult for operators.", "tradeoffs": "Generic webhooks vs. specific API clients.", "recommendations": "Add a 'Test Webhook' button in the UI to improve manageability.", "status": "Fixable in v1"},
    {"name": "Accessibility", "category": "ENTERPRISE", "weight": 1, "score": 80, "justification": "Self-attestation review against WCAG 2.2 AA is documented.", "tradeoffs": "UI complexity vs. accessibility.", "recommendations": "Ensure all new UI components pass axe-core checks.", "status": "Strong"},
    {"name": "Customer Self-Sufficiency", "category": "ENTERPRISE", "weight": 1, "score": 60, "justification": "High reliance on support during setup. Troubleshooting configuration errors (like Key Vault access) is difficult.", "tradeoffs": "White-glove sales motion vs. PLG.", "recommendations": "Enhance the 'doctor' CLI command to verify Azure Key Vault connectivity.", "status": "Fixable in v1"},
    {"name": "Change Impact Clarity", "category": "ENTERPRISE", "weight": 1, "score": 75, "justification": "Run comparisons provide visibility, but understanding the exact impact of a policy change before applying it is hard.", "tradeoffs": "Detailed diffs vs. high-level summaries.", "recommendations": "Implement a Policy Pack Dry-Run API.", "status": "Fixable in v1.1"},

    {"name": "Correctness", "category": "ENGINEERING", "weight": 4, "score": 85, "justification": "Agentic outputs generally adhere to constraints, backed by strong governance gates.", "tradeoffs": "LLM creativity vs. deterministic correctness.", "recommendations": "Enhance pre-commit gate with strict JSON schema validation.", "status": "Strong"},
    {"name": "Architectural Integrity", "category": "ENGINEERING", "weight": 3, "score": 90, "justification": "Strong, coherent design using SQL, RLS, and clean API boundaries.", "tradeoffs": "Rigidity vs. flexibility.", "recommendations": "Maintain current discipline.", "status": "Strong"},
    {"name": "Security", "category": "ENGINEERING", "weight": 3, "score": 90, "justification": "Excellent defense-in-depth with RLS, Key Vault, and private endpoints.", "tradeoffs": "Development friction vs. security.", "recommendations": "Continue OWASP ZAP and schema validation.", "status": "Strong"},
    {"name": "Reliability", "category": "ENGINEERING", "weight": 2, "score": 85, "justification": "Solid foundation with SQL Server and documented RTO/RPO targets.", "tradeoffs": "Cost vs. multi-region active/active.", "recommendations": "Implement planned staging chaos exercises.", "status": "Strong"},
    {"name": "Data Consistency", "category": "ENGINEERING", "weight": 2, "score": 85, "justification": "Relational integrity and DbUp migrations ensure consistent state.", "tradeoffs": "Relational overhead vs. NoSQL speed.", "recommendations": "Ensure all new features use established transaction boundaries.", "status": "Strong"},
    {"name": "Maintainability", "category": "ENGINEERING", "weight": 2, "score": 80, "justification": "Clean architecture, but some legacy coordinator endpoints still need strangling.", "tradeoffs": "Upfront design vs. rapid prototyping.", "recommendations": "Execute the Coordinator Strangler plan.", "status": "Fixable in v1.1"},
    {"name": "Explainability", "category": "ENGINEERING", "weight": 2, "score": 65, "justification": "Tracing why an agent made a specific decision through the provenance graph is difficult for non-experts.", "tradeoffs": "Deep provenance data vs. human-readable summaries.", "recommendations": "Add an 'Explain this decision' stub to the provenance graph.", "status": "Fixable in v1.1"},
    {"name": "AI/Agent Readiness", "category": "ENGINEERING", "weight": 2, "score": 85, "justification": "Architecture is designed around agentic workflows, with MCP planned for v1.1.", "tradeoffs": "Deterministic control vs. autonomous agents.", "recommendations": "Prepare internal APIs for the v1.1 MCP membrane.", "status": "Strong"},
    {"name": "Azure Compatibility and SaaS Deployment Readiness", "category": "ENGINEERING", "weight": 2, "score": 90, "justification": "Deeply integrated with Azure-native services.", "tradeoffs": "Cloud lock-in vs. operational simplicity.", "recommendations": "Maintain Azure-first IaC templates.", "status": "Strong"},
    {"name": "Availability", "category": "ENGINEERING", "weight": 1, "score": 85, "justification": "Documented 99.9% SLA target.", "tradeoffs": "Cost of high availability vs. customer needs.", "recommendations": "Automate SLA measurement.", "status": "Strong"},
    {"name": "Performance", "category": "ENGINEERING", "weight": 1, "score": 80, "justification": "Generally performant, but large manifests may cause UI latency.", "tradeoffs": "Rich UI vs. rendering speed.", "recommendations": "Implement pagination for large provenance graphs.", "status": "Fixable in v1.1"},
    {"name": "Scalability", "category": "ENGINEERING", "weight": 1, "score": 85, "justification": "Stateless API and scalable SQL backend support enterprise loads.", "tradeoffs": "Complexity of scaling vs. current usage.", "recommendations": "Monitor SQL DTU usage.", "status": "Strong"},
    {"name": "Supportability", "category": "ENGINEERING", "weight": 1, "score": 75, "justification": "CLI diagnostics exist, but lacks offline validation tools for manifests.", "tradeoffs": "Building internal tools vs. customer features.", "recommendations": "Add a CLI command for offline manifest validation.", "status": "Fixable in v1"},
    {"name": "Manageability", "category": "ENGINEERING", "weight": 1, "score": 75, "justification": "Configuration is standard, but testing integrations (like webhooks) is manual.", "tradeoffs": "Configuration complexity vs. flexibility.", "recommendations": "Add webhook testing tools in the UI.", "status": "Fixable in v1"},
    {"name": "Deployability", "category": "ENGINEERING", "weight": 1, "score": 80, "justification": "Container images exist, but local evaluation without Azure is hard.", "tradeoffs": "Maintaining multiple IaC tools vs. standardizing.", "recommendations": "Add a Dockerfile.local for completely self-contained local evaluation.", "status": "Fixable in v1"},
    {"name": "Observability", "category": "ENGINEERING", "weight": 1, "score": 80, "justification": "Correlation IDs provide good observability.", "tradeoffs": "Log volume vs. troubleshooting context.", "recommendations": "Integrate with OpenTelemetry.", "status": "Fixable in v1.1"},
    {"name": "Testability", "category": "ENGINEERING", "weight": 1, "score": 80, "justification": "Clear test structure, but golden corpus could be expanded.", "tradeoffs": "Test maintenance overhead vs. confidence.", "recommendations": "Expand golden corpus.", "status": "Strong"},
    {"name": "Modularity", "category": "ENGINEERING", "weight": 1, "score": 85, "justification": "Clean separation between layers.", "tradeoffs": "Indirection vs. separation of concerns.", "recommendations": "Ensure MCP membrane remains removable.", "status": "Strong"},
    {"name": "Extensibility", "category": "ENGINEERING", "weight": 1, "score": 80, "justification": "Webhook patterns allow extension, but UI lacks export options.", "tradeoffs": "First-party vs. third-party integrations.", "recommendations": "Implement 'Export Golden Manifest to Markdown' in the UI.", "status": "Fixable in v1"},
    {"name": "Evolvability", "category": "ENGINEERING", "weight": 1, "score": 85, "justification": "DbUp migrations and versioned APIs support safe evolution.", "tradeoffs": "Backward compatibility vs. rapid iteration.", "recommendations": "Maintain strict breaking changes log.", "status": "Strong"},
    {"name": "Documentation", "category": "ENGINEERING", "weight": 1, "score": 85, "justification": "Extensive, high-quality documentation.", "tradeoffs": "Documentation maintenance vs. coding.", "recommendations": "Enforce doc scope header rules.", "status": "Strong"},
    {"name": "Azure Ecosystem Fit", "category": "ENGINEERING", "weight": 1, "score": 90, "justification": "Perfect alignment with Azure enterprise patterns.", "tradeoffs": "Multi-cloud vs. deep Azure integration.", "recommendations": "Leverage Azure Managed Identities.", "status": "Strong"},
    {"name": "Cognitive Load", "category": "ENGINEERING", "weight": 1, "score": 60, "justification": "Operators must understand manifests, runs, artifacts, and provenance simultaneously.", "tradeoffs": "Power vs. simplicity.", "recommendations": "Simplify the default run view.", "status": "Fixable in v1"},
    {"name": "Cost-Effectiveness", "category": "ENGINEERING", "weight": 1, "score": 80, "justification": "Efficient use of Azure resources, though LLM token costs need monitoring.", "tradeoffs": "Performance vs. cost.", "recommendations": "Implement token usage tracking.", "status": "Fixable in v1.1"}
]

for q in qualities:
    q['deficiency'] = q['weight'] * (100 - q['score'])
    q['weighted_score'] = q['weight'] * q['score']

qualities.sort(key=lambda x: x['deficiency'], reverse=True)

total_weight = sum(q['weight'] for q in qualities)
total_weighted_score = sum(q['weighted_score'] for q in qualities)
readiness_percentage = (total_weighted_score / (total_weight * 100)) * 100

report = f"""> **Scope:** Independent first-principles assessment of ArchLucid V1.1 readiness based on the provided quality model and weights.

# ArchLucid Assessment – Weighted Readiness {readiness_percentage:.2f}%

## Executive Summary

**Overall Readiness**
ArchLucid demonstrates a solid V1 foundation with a weighted readiness of {readiness_percentage:.2f}%. The core architecture is highly secure and leverages Azure-native patterns effectively. However, the product currently leans heavily on operator technical proficiency, which introduces significant friction in adoption, time-to-value, and marketability to non-technical buyers.

**Commercial Picture**
The commercial foundation is viable for technical buyers, but Marketability, Adoption Friction, and Executive Value Visibility are lagging. The product solves complex architectural and governance problems, but translating these technical wins into sponsor-facing views and easy-to-adopt workflows requires immediate attention. The lack of out-of-the-box templates and sandbox environments slows down the sales cycle.

**Enterprise Picture**
Enterprise trust and auditability are strong points, supported by the durable audit log and RLS. However, Customer Self-Sufficiency and Workflow Embeddedness are weaker. First-party Jira and ServiceNow are V1 commitments; until shipped, teams rely on webhooks. Furthermore, troubleshooting configuration issues (like Key Vault access) requires too much white-glove support.

**Engineering Picture**
Engineering fundamentals (Security, Architectural Integrity, Azure Compatibility) score highly. The system is built defensively. The primary engineering risks lie in Cognitive Load and Explainability. The agentic outputs and governance workflows are complex, and making them transparent, testable, and easy to troubleshoot for average operators needs improvement.

## Weighted Quality Assessment

"""

for q in qualities:
    report += f"### {q['name']}\n"
    report += f"- **Score:** {q['score']}\n"
    report += f"- **Weight:** {q['weight']}\n"
    report += f"- **Weighted deficiency signal:** {q['deficiency']:.2f}\n"
    report += f"- **Justification:** {q['justification']}\n"
    report += f"- **Tradeoffs:** {q['tradeoffs']}\n"
    report += f"- **Improvement recommendations:** {q['recommendations']}\n"
    report += f"- **Status:** {q['status']}\n\n"

report += """## Top 10 Most Important Weaknesses

1. **High Technical Barrier to Entry:** The product requires deep technical knowledge to configure and operate, limiting its marketability to business sponsors.
2. **Setup Complexity:** Requiring Entra ID, SQL, and Key Vault configuration upfront slows down initial trials and time-to-value.
3. **Policy Pack Authoring Friction:** Writing raw JSON/YAML for policy packs is error-prone and increases adoption friction.
4. **Opaque Agent Reasoning:** Provenance graphs are too dense for quick comprehension, hurting explainability.
5. **Lack of Executive Dashboards:** No default, high-level view exists for sponsors to understand overall architectural health.
6. **Unquantified ROI:** Hard for champions to prove the tool's financial or time-saving value automatically.
7. **Template Scarcity:** Lack of out-of-the-box starting points forces users to build architecture requests from scratch.
8. **Difficult Integration Testing:** Operators cannot easily test or debug webhook deliveries from within the UI.
9. **Troubleshooting Blind Spots:** Diagnosing infrastructure connectivity issues (e.g., Key Vault) relies heavily on support.
10. **Steep Learning Curve:** High cognitive load for new operators navigating runs, manifests, and policies simultaneously.

## Top 5 Monetization Blockers

1. **Value Translation:** Marketing materials and the product experience are too focused on architecture mechanics rather than business risk mitigation.
2. **Sales-Led Bottleneck:** The complexity of the pilot setup limits the volume of concurrent trials and self-serve adoption.
3. **Missing ROI Telemetry:** Buyers cannot easily justify the purchase without clear, quantified metrics.
4. **Lack of Executive Visibility:** Economic buyers lack a shipped dashboard experience to see the value of their investment.
5. **Template Scarcity:** The "blank canvas" problem delays the "aha" moment for new prospects.

## Top 5 Enterprise Adoption Blockers

1. **Setup Complexity:** Requiring full Azure infrastructure configuration upfront slows down departmental adoption.
2. **Policy Authoring Friction:** Security teams struggle to translate their written policies into raw JSON/YAML policy packs without a visual builder.
3. **Integration Testing Difficulty:** Setting up and verifying SIEM or ITSM webhooks is a manual, error-prone process.
4. **Customer Self-Sufficiency:** Customers struggle to troubleshoot configuration errors (like Key Vault access) without SE help.
5. **ITSM bridging before connectors:** Enterprises expect native Jira/ServiceNow; relying on webhooks adds integration friction.

## Top 5 Engineering Risks

1. **Agent Nondeterminism:** LLM-driven architecture decisions may occasionally violate strict enterprise policies if not caught by governance gates.
2. **RLS Complexity:** Maintaining Row-Level Security across all new features requires strict developer discipline.
3. **Integration delivery failures:** Relying on webhooks increases transport risk compared to first-party connectors.
4. **Coordinator Strangler Execution:** Migrating legacy endpoints risks introducing regressions if not tested thoroughly.
5. **Performance at Scale:** The provenance graph and large manifests may cause UI latency for massive architectures.

## Most Important Truth

ArchLucid is a highly secure, architecturally sound platform, but its steep technical learning curve and complex setup process severely throttle its marketability and adoption velocity; it must urgently bridge the gap between powerful engineering mechanics and approachable, self-serve business value.

## Top Improvement Opportunities

1. **Policy Pack Dry-Run API**
- **Why it matters:** Operators need to understand the impact of a policy change before enforcing it, reducing the fear of breaking existing workflows.
- **Expected impact:** Directly improves Change Impact Clarity (+15-20 pts), Policy and Governance Alignment (+10-15 pts), and Usability (+5-10 pts). Weighted readiness impact: +0.6-0.9%.
- **Affected qualities:** Change Impact Clarity, Policy and Governance Alignment, Usability.
- **Actionable:** Yes.
- **Prompt:**
```text
Implement a "Dry-Run" endpoint for Policy Packs in `ArchLucid.Api`.
- Create a new endpoint `POST /v1/governance/policy-packs/dry-run`.
- The endpoint should accept a proposed Policy Pack JSON payload and a target Run ID or Manifest ID.
- It should evaluate the policy pack against the target without saving the policy pack or altering the run's state.
- Return a JSON response detailing which rules passed, which failed, and any warnings, exactly as the real governance gate would.
- Ensure the endpoint is secured with `[Authorize]` and respects RLS for the target Run/Manifest.
- Add unit tests in `ArchLucid.Api.Tests` to verify the dry-run behavior.
```

2. **Visual Policy Pack Builder (React Component)**
- **Why it matters:** Writing raw JSON/YAML for policies is a massive adoption hurdle for security and compliance teams.
- **Expected impact:** Directly improves Adoption Friction (+15-20 pts), Usability (+10-15 pts), and Cognitive Load (+10-15 pts). Weighted readiness impact: +0.8-1.2%.
- **Affected qualities:** Adoption Friction, Usability, Cognitive Load.
- **Actionable:** Yes.
- **Prompt:**
```text
Create a visual "Policy Pack Builder" component in `archlucid-ui`.
- Create a new React component at `archlucid-ui/src/components/governance/PolicyPackBuilder.tsx`.
- The component should provide a form-based UI to construct policy rules (e.g., selecting resource types, conditions, and severity levels from dropdowns).
- It must generate valid ArchLucid Policy Pack JSON in real-time as the user interacts with the form.
- Include a "JSON Preview" toggle to see the generated output.
- Integrate this component into the existing Policy Pack creation page (`archlucid-ui/src/app/(operator)/governance/policy-packs/new/page.tsx`).
- Ensure all form fields are accessible (ARIA labels).
```

3. **Webhook Delivery Testing UI**
- **Why it matters:** Operators currently have to trigger real events to test if their webhook configurations (for SIEM or ITSM) are working, which is frustrating and slow.
- **Expected impact:** Directly improves Interoperability (+15-20 pts), Manageability (+10-15 pts), and Customer Self-Sufficiency (+10-15 pts). Weighted readiness impact: +0.5-0.8%.
- **Affected qualities:** Interoperability, Manageability, Customer Self-Sufficiency.
- **Actionable:** Yes.
- **Prompt:**
```text
Add a "Test Webhook" feature to the Operator UI and API.
- In `ArchLucid.Api`, add a `POST /v1/integrations/webhooks/{id}/test` endpoint that dispatches a synthetic `ping` event to the specified webhook URL.
- The endpoint should return the HTTP status code and response body received from the destination.
- In `archlucid-ui`, add a "Test Connection" button next to each configured webhook in the integration settings page.
- When clicked, call the new endpoint and display a success/failure toast notification with the detailed response.
- Ensure the API endpoint respects RLS and authorization.
```

4. **Local Manifest Validation CLI Command**
- **Why it matters:** Developers want to validate their architecture manifests locally in their CI pipelines before submitting them to the API.
- **Expected impact:** Directly improves Supportability (+15-20 pts), Correctness (+5-10 pts), and Workflow Embeddedness (+5-10 pts). Weighted readiness impact: +0.4-0.7%.
- **Affected qualities:** Supportability, Correctness, Workflow Embeddedness.
- **Actionable:** Yes.
- **Prompt:**
```text
Enhance the `archlucid` CLI to support offline manifest validation.
- Add a new command: `archlucid manifest validate --file <path-to-json>`.
- The command should load the JSON schema for the Golden Manifest (bundle it with the CLI or download it once and cache it).
- It should perform strict JSON schema validation on the provided file and output clear, actionable error messages with line numbers if validation fails.
- The command must return an exit code of `0` for success and `1` for failure, making it suitable for use in CI/CD pipelines.
- Do not require an active connection to the ArchLucid API for this command.
```

5. **Key Vault Connection Diagnostics in `doctor`**
- **Why it matters:** Misconfigured Azure Key Vault access is a common setup issue that currently requires SE support to diagnose.
- **Expected impact:** Directly improves Customer Self-Sufficiency (+15-20 pts), Supportability (+10-15 pts), and Time-to-Value (+5-10 pts). Weighted readiness impact: +0.4-0.6%.
- **Affected qualities:** Customer Self-Sufficiency, Supportability, Time-to-Value.
- **Actionable:** Yes.
- **Prompt:**
```text
Enhance the `archlucid doctor` CLI command to verify Azure Key Vault connectivity.
- Update the diagnostic logic to read the configured Key Vault URI from the environment or configuration files.
- Attempt to authenticate using the configured Managed Identity or Service Principal.
- Attempt to list secrets (or read a specific known test secret) to verify RBAC permissions.
- Output a clear diagnostic message: e.g., "Key Vault: Connected", "Key Vault: Authentication Failed (Check Managed Identity)", or "Key Vault: Permission Denied (Missing Key Vault Secrets User role)".
- Ensure this check handles timeouts gracefully and does not crash the entire `doctor` run if it fails.
```

6. **Export Golden Manifest to Markdown (UI)**
- **Why it matters:** Architecture decisions need to be shared with stakeholders who don't have access to the ArchLucid UI.
- **Expected impact:** Directly improves Extensibility (+15-20 pts), Interoperability (+10-15 pts), and Marketability (+5-10 pts). Weighted readiness impact: +0.4-0.7%.
- **Affected qualities:** Extensibility, Interoperability, Marketability.
- **Actionable:** Yes.
- **Prompt:**
```text
Implement an "Export to Markdown" feature for the Golden Manifest in the Operator UI.
- In `archlucid-ui`, add an "Export" dropdown to the Run Detail page.
- Add an option for "Markdown Summary".
- Create a utility function `src/lib/export-markdown.ts` that takes a Golden Manifest JSON object and formats it into a readable Markdown document (including sections for Objectives, Architecture Overview, Component Breakdown, and Security Model).
- Trigger a browser download of the generated `.md` file when the user clicks the option.
- Do not add new backend endpoints; perform the transformation entirely in the client.
```

7. **Business Value Cheat Sheet**
- **Why it matters:** Sales and marketing teams struggle to translate technical features (like RLS) into business outcomes (like compliance risk reduction).
- **Expected impact:** Directly improves Marketability (+15-20 pts), Differentiability (+5-10 pts). Weighted readiness impact: +0.3-0.5%.
- **Affected qualities:** Marketability, Differentiability.
- **Actionable:** Yes.
- **Prompt:**
```text
Create a new documentation file at `docs/go-to-market/BUSINESS_VALUE_CHEAT_SHEET.md`.
- Structure the document as a mapping table for sales and marketing teams.
- Column 1: Technical Feature (e.g., "Row-Level Security (RLS)", "Append-Only Audit Log", "Pre-Commit Governance Gate", "Golden Manifest").
- Column 2: Business Outcome (e.g., "Prevents cross-tenant data leaks", "Accelerates compliance audits", "Stops architectural flaws before they are built", "Provides a single source of truth").
- Column 3: Economic Impact (e.g., "Reduces compliance fines", "Saves 40 hours per audit", "Avoids costly rework", "Speeds up onboarding").
- Ensure the document follows the standard `> **Scope:**` header invariant required by CI.
```

8. **DEFERRED: Custom SIEM Mapping Configuration**
- **Why it matters:** Different enterprises have different SIEM schemas; hardcoding Splunk/Sentinel formats isn't enough for everyone.
- **Expected impact:** Directly improves Interoperability (+15-20 pts), Manageability (+10-15 pts). Weighted readiness impact: +0.4-0.7%.
- **Affected qualities:** Interoperability, Manageability.
- **Actionable:** DEFERRED
- **Reason deferred:** Requires user input on the desired configuration syntax (e.g., JQ expressions vs. Liquid templates) for transforming the internal audit event JSON into custom outbound webhook payloads.
- **Needed from user:** Please specify the preferred templating language or transformation engine (e.g., JQ, Liquid, or a custom mapping DSL) that operators should use to define custom SIEM payload shapes.

9. **DEFERRED: Guided Sandbox Onboarding Mode**
- **Why it matters:** The current setup requires full Azure infrastructure, killing PLG momentum. A sandbox mode using local mocks is needed.
- **Expected impact:** Directly improves Adoption Friction (+20-30 pts), Time-to-Value (+15-20 pts). Weighted readiness impact: +0.8-1.2%.
- **Affected qualities:** Adoption Friction, Time-to-Value.
- **Actionable:** DEFERRED
- **Reason deferred:** Requires user input on whether the sandbox should be a purely frontend-mocked experience (React only) or a local Docker Compose setup with a lightweight SQLite/In-Memory database replacing Azure SQL.
- **Needed from user:** Please clarify the architectural direction for the sandbox: should it be a purely client-side mock in the UI, or a local Docker Compose stack using an in-memory database?

## Pending Questions for Later

**DEFERRED: Custom SIEM Mapping Configuration**
- What is the preferred templating language or transformation engine (e.g., JQ, Liquid, or a custom mapping DSL) that operators should use to define custom SIEM payload shapes?

**DEFERRED: Guided Sandbox Onboarding Mode**
- Should the sandbox be a purely client-side mock in the UI, or a local Docker Compose stack using an in-memory database?

## Deferred Scope Uncertainty
*None identified. The assessment strictly adhered to the V1/V1.1 boundaries defined in `V1_SCOPE.md` and `V1_DEFERRED.md`.*
"""

with open('docs/library/ARCHLUCID_ASSESSMENT_WEIGHTED_READINESS_2026_05_05_INDEPENDENT.md', 'w', encoding='utf-8') as f:
    f.write(report)
