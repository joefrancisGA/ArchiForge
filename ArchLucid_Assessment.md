# ArchLucid Assessment – Weighted Readiness 82.55%

## Executive Summary

**Overall Readiness:** ArchLucid is technically sound, highly auditable, and architecturally coherent, but it suffers from a fragile, prototype-grade operator frontend. The system excels at data consistency, observability, and compliance tracking, making the backend heavily enterprise-ready. However, usability and maintainability bottlenecks in the UI layer significantly drag down the overall readiness score (82.55%), threatening long-term agility.

**Commercial Picture:** The product is highly differentiated and proves value quickly *if* inputs are perfectly structured. However, the adoption friction is high due to the lack of out-of-the-box native integrations (e.g., Slack, Jira) in V1, forcing users to rely on raw webhooks. The narrative packaging ("Pilot" vs "Operate") is brilliant, but actual stickiness will suffer until V1.1/V2 integrations ship.

**Enterprise Picture:** ArchLucid is a CISO's dream. With 78 typed audit events, strict SQL Row-Level Security, private endpoint postures, and thorough compliance mapping, it will sail through procurement. The main enterprise adoption blockers are operational—specifically, the lack of turnkey SSO federation for non-Microsoft identity providers and the out-of-band workflow that requires architects to leave their native IDEs.

**Engineering Picture:** The backend ASP.NET Core API and SQL Server architecture is robust, leveraging WAF, OTEL, and idempotent operations. Conversely, the Next.js `archlucid-ui` shell is severely under-engineered. The deliberate avoidance of a CSS framework, the reliance on hand-written JSON coercion functions over schemas, and the lack of client-side caching or React error boundaries present massive, immediate maintainability and reliability risks.

## Weighted Quality Assessment

**1. Adoption Friction**
- **Score:** 75
- **Weight:** 6
- **Weighted deficiency signal:** 150
- **Justification:** High barrier to entry. Requires provisioning SQL, Blob storage, and Entra ID for non-Docker deployments.
- **Tradeoffs:** High security and isolation vs. easy frictionless onboarding.
- **Improvement recommendations:** Provide more robust Terraform automated bootstrappers for rapid PoCs.
- **Status:** Better suited for V1.1/V2 (Integration expansions).

**2. Time-to-Value**
- **Score:** 80
- **Weight:** 7
- **Weighted deficiency signal:** 140
- **Justification:** Claimed 30-minute pilot requires perfectly pristine architecture inputs which enterprises rarely have.
- **Tradeoffs:** Structured determinism vs. tolerant messy ingestion.
- **Improvement recommendations:** Enhance the ingestion pipeline to parse unstructured/messy legacy docs better.
- **Status:** Fixable in V1.

**3. Marketability**
- **Score:** 85
- **Weight:** 8
- **Weighted deficiency signal:** 120
- **Justification:** Solid messaging and tiering, but deferred commerce un-hold and reference customers soften the immediate V1 launch impact.
- **Tradeoffs:** Launch speed vs. fully automated self-serve purchasing.
- **Improvement recommendations:** Clarify the manual sales-led process aggressively on the pricing page.
- **Status:** Blocked on user input (Commerce un-hold is V1.1).

**4. Usability**
- **Score:** 65
- **Weight:** 3
- **Weighted deficiency signal:** 105
- **Justification:** No CSS framework, inline styles, no error boundaries. UI is primitive.
- **Tradeoffs:** Minimized dependencies vs. standard modern UI practices.
- **Improvement recommendations:** Introduce Tailwind CSS to manage styles systematically.
- **Status:** Fixable in V1.

**5. Workflow Embeddedness**
- **Score:** 65
- **Weight:** 3
- **Weighted deficiency signal:** 105
- **Justification:** Operates entirely out-of-band via a separate web portal or CLI. No IDE extensions. ITSM deferred to V1.1.
- **Tradeoffs:** Focused governance portal vs. meeting developers where they code.
- **Improvement recommendations:** Develop a VS Code extension to surface findings directly in code.
- **Status:** Better suited for V1.1/V2.

**6. Correctness**
- **Score:** 80
- **Weight:** 4
- **Weighted deficiency signal:** 80
- **Justification:** Backend is precise, but UI uses fragile hand-written `coerce*` functions that fail silently on schema drift.
- **Tradeoffs:** Zero UI dependencies vs. strict schema safety.
- **Improvement recommendations:** Migrate UI coercion to Zod schemas.
- **Status:** Fixable in V1.

**7. Maintainability**
- **Score:** 60
- **Weight:** 2
- **Weighted deficiency signal:** 80
- **Justification:** The `archlucid-ui` shell's inline styling and lack of global state will become an unmaintainable legacy burden within months.
- **Tradeoffs:** Quick initial build vs. sustainable long-term frontend architecture.
- **Improvement recommendations:** Implement Tailwind CSS and Zustand.
- **Status:** Fixable in V1.

**8. Interoperability**
- **Score:** 70
- **Weight:** 2
- **Weighted deficiency signal:** 60
- **Justification:** Relies on raw webhooks. Native ITSM (Jira/ServiceNow) and ChatOps (Slack) are deferred.
- **Tradeoffs:** Core engine stability vs. broad ecosystem connectivity.
- **Improvement recommendations:** Finalize ITSM mappings and accelerate the Jira/ServiceNow webhook templates.
- **Status:** Better suited for V1.1.

**9. Differentiability**
- **Score:** 85
- **Weight:** 4
- **Weighted deficiency signal:** 60
- **Justification:** High differentiation via auditability and graph provenance, though UI lacks the polish of established enterprise tools.
- **Tradeoffs:** Deep governance capabilities vs. surface-level UI polish.
- **Improvement recommendations:** Polish the exported DOCX packages to look more premium.
- **Status:** Fixable in V1.

**10. Proof-of-ROI Readiness**
- **Score:** 90
- **Weight:** 5
- **Weighted deficiency signal:** 50
- **Justification:** Excellent PDF exports and first-value reports, though highly static.
- **Tradeoffs:** Immutable snapshot reports vs. live interactive dashboards.
- **Improvement recommendations:** Provide live ROI tracking widgets on the Home screen.
- **Status:** Fixable in V1.

**11. Architectural Integrity**
- **Score:** 85
- **Weight:** 3
- **Weighted deficiency signal:** 45
- **Justification:** Excellent API/SQL backend. Frontend architecture lacks standard robustness (no caching, inline CSS).
- **Tradeoffs:** Thin UI layer vs. robust, independent frontend app.
- **Improvement recommendations:** Add SWR caching and error boundaries.
- **Status:** Fixable in V1.

**12. Executive Value Visibility**
- **Score:** 90
- **Weight:** 4
- **Weighted deficiency signal:** 40
- **Justification:** Great sponsor briefs and value reports.
- **Tradeoffs:** Executive summaries vs. deep engineering diagnostics.
- **Improvement recommendations:** Ensure PDF exports clearly highlight cost-savings and compliance avoids.
- **Status:** Fixable in V1.

**13. Reliability**
- **Score:** 80
- **Weight:** 2
- **Weighted deficiency signal:** 40
- **Justification:** Server-side fetches on every Next.js render will overwhelm the API under load.
- **Tradeoffs:** Data freshness vs. infrastructure load.
- **Improvement recommendations:** Implement client-side caching for immutable runs.
- **Status:** Fixable in V1.

**14. Decision Velocity**
- **Score:** 80
- **Weight:** 2
- **Weighted deficiency signal:** 40
- **Justification:** Great governance gates, but manual UI interactions slow down bulk reviews.
- **Tradeoffs:** Careful manual review vs. automated mass approvals.
- **Improvement recommendations:** Improve bulk approval UI ergonomics.
- **Status:** Fixable in V1.

**15. Stickiness**
- **Score:** 65
- **Weight:** 1
- **Weighted deficiency signal:** 35
- **Justification:** Hard to retain daily active usage when the tool sits outside the main IDE/ChatOps flow.
- **Tradeoffs:** Specialized portal vs. integrated tooling.
- **Improvement recommendations:** Accelerate Slack integration to keep it top-of-mind.
- **Status:** Better suited for V2.

**16. Policy and Governance Alignment**
- **Score:** 85
- **Weight:** 2
- **Weighted deficiency signal:** 30
- **Justification:** Strong policy packs, but assignment resolution can be opaque without deep reading.
- **Tradeoffs:** Complex hierarchical policies vs. simple flat rules.
- **Improvement recommendations:** Visualize policy resolution hierarchy in the UI.
- **Status:** Fixable in V1.

**17. Compliance Readiness**
- **Score:** 85
- **Weight:** 2
- **Weighted deficiency signal:** 30
- **Justification:** SOC2 is self-assessment only for V1.
- **Tradeoffs:** Bootstrapping costs vs. immediate external attestation.
- **Improvement recommendations:** Prepare external readiness consultant engagements.
- **Status:** Blocked on user input (Budget/Timelines for Type I).

**18. Commercial Packaging Readiness**
- **Score:** 85
- **Weight:** 2
- **Weighted deficiency signal:** 30
- **Justification:** Soft API tier gating rather than hard billing enforcement limits immediate self-serve revenue.
- **Tradeoffs:** Frictionless early adoption vs. strict revenue protection.
- **Improvement recommendations:** Implement strict Stripe token mapping.
- **Status:** Better suited for V1.1.

**19. Security**
- **Score:** 90
- **Weight:** 3
- **Weighted deficiency signal:** 30
- **Justification:** ZAP, CodeQL, strict RLS. External pen-test deferred to V2.
- **Tradeoffs:** High internal security vs. expensive external validation.
- **Improvement recommendations:** Finalize V2 pen-test scope.
- **Status:** Blocked on user input.

**20. AI/Agent Readiness**
- **Score:** 85
- **Weight:** 2
- **Weighted deficiency signal:** 30
- **Justification:** Excellent redaction and multi-vendor setup. Lacks autonomous multi-step discovery agents.
- **Tradeoffs:** Deterministic safety vs. open-ended agentic exploration.
- **Improvement recommendations:** Deploy the MCP server membrane.
- **Status:** Better suited for V1.1.

**21. Traceability**
- **Score:** 90
- **Weight:** 3
- **Weighted deficiency signal:** 30
- **Justification:** Superb 78-event audit log and provenance graph.
- **Tradeoffs:** Heavy storage footprint vs. absolute auditability.
- **Improvement recommendations:** Enhance visual highlighting of traces in UI.
- **Status:** Fixable in V1.

**22. Trustworthiness**
- **Score:** 90
- **Weight:** 3
- **Weighted deficiency signal:** 30
- **Justification:** High trust due to transparent explanations, though lack of third-party attestation holds it back slightly.
- **Tradeoffs:** Internal rigor vs. external badges.
- **Improvement recommendations:** Publish the PGP key for security disclosures.
- **Status:** Better suited for V1.1.

**23. Performance**
- **Score:** 75
- **Weight:** 1
- **Weighted deficiency signal:** 25
- **Justification:** Lack of UI caching means high latency for users far from the Azure datacenter.
- **Tradeoffs:** Simple stateless frontend vs. optimized fast UX.
- **Improvement recommendations:** Implement SWR.
- **Status:** Fixable in V1.

**24. Testability**
- **Score:** 75
- **Weight:** 1
- **Weighted deficiency signal:** 25
- **Justification:** E2E Playwright tests exist but are not enforced in CI, risking UI regressions.
- **Tradeoffs:** Fast CI builds vs. guaranteed UI correctness.
- **Improvement recommendations:** Add Playwright to standard GitHub Actions.
- **Status:** Fixable in V1.

**25. Customer Self-Sufficiency**
- **Score:** 75
- **Weight:** 1
- **Weighted deficiency signal:** 25
- **Justification:** Relies on command-line tools for setup, which alienates non-technical stakeholders.
- **Tradeoffs:** Dev-first onboarding vs. generic SaaS onboarding.
- **Improvement recommendations:** Build a fully guided web-based onboarding wizard.
- **Status:** Better suited for V1.1.

**26. Extensibility**
- **Score:** 80
- **Weight:** 1
- **Weighted deficiency signal:** 20
- **Justification:** Webhooks exist, but MCP and native integrations are deferred.
- **Tradeoffs:** Core focus vs. ecosystem growth.
- **Improvement recommendations:** Deliver the MCP SDK membrane.
- **Status:** Better suited for V1.1.

**27. Evolvability**
- **Score:** 80
- **Weight:** 1
- **Weighted deficiency signal:** 20
- **Justification:** API is versioned, but the UI's manual coercion functions will break if the API evolves unexpectedly.
- **Tradeoffs:** Rapid iteration vs. strict schema contracts.
- **Improvement recommendations:** Introduce Zod for forward-compatible parsing.
- **Status:** Fixable in V1.

**28. Scalability**
- **Score:** 80
- **Weight:** 1
- **Weighted deficiency signal:** 20
- **Justification:** Stateless Node.js UI is good, but without caching or CDNs, scale costs will be high.
- **Tradeoffs:** Simple architecture vs. massive horizontal scale.
- **Improvement recommendations:** Configure Next.js caching directives properly.
- **Status:** Fixable in V1.

**29. Cost-Effectiveness**
- **Score:** 80
- **Weight:** 1
- **Weighted deficiency signal:** 20
- **Justification:** Constant API refetching increases compute costs unnecessarily.
- **Tradeoffs:** Developer velocity vs. optimized resource usage.
- **Improvement recommendations:** Cache immutable artifacts on the client.
- **Status:** Fixable in V1.

**30. Template and Accelerator Richness**
- **Score:** 80
- **Weight:** 1
- **Weighted deficiency signal:** 20
- **Justification:** Good Terraform and Power Automate templates, but lacking built-in industry-specific policy packs.
- **Tradeoffs:** Generic platform vs. verticalized solutions.
- **Improvement recommendations:** Provide out-of-the-box HIPAA/PCI policy packs.
- **Status:** Better suited for V1.1.

**31. Azure Compatibility and SaaS Deployment Readiness**
- **Score:** 90
- **Weight:** 2
- **Weighted deficiency signal:** 20
- **Justification:** Excellent Terraform modules for ACA, SQL, KeyVault.
- **Tradeoffs:** Azure lock-in vs. multi-cloud flexibility.
- **Improvement recommendations:** Document Azure cross-region failover limits clearly.
- **Status:** Fixable in V1.

**32. Availability**
- **Score:** 85
- **Weight:** 1
- **Weighted deficiency signal:** 15
- **Justification:** 99.9% target is good, but active-active multi-region is out of scope for V1.
- **Tradeoffs:** Cost vs. absolute uptime.
- **Improvement recommendations:** Refine disaster recovery RTO/RPO targets.
- **Status:** Better suited for V1.1.

**33. Supportability**
- **Score:** 85
- **Weight:** 1
- **Weighted deficiency signal:** 15
- **Justification:** Support bundles and `doctor` command are excellent. UI error states are slightly cryptic.
- **Tradeoffs:** Engineering-focused errors vs. user-friendly messages.
- **Improvement recommendations:** Add UI error boundaries.
- **Status:** Fixable in V1.

**34. Manageability**
- **Score:** 85
- **Weight:** 1
- **Weighted deficiency signal:** 15
- **Justification:** Great admin CLI, but lacking bulk-management UI for complex policies.
- **Tradeoffs:** CLI efficiency vs. UI completeness.
- **Improvement recommendations:** Add bulk editing tools to Policy Packs in UI.
- **Status:** Better suited for V1.1.

**35. Deployability**
- **Score:** 85
- **Weight:** 1
- **Weighted deficiency signal:** 15
- **Justification:** Docker compose provided, but strictly noted as not for production customers.
- **Tradeoffs:** Controlled SaaS vs. on-prem flexibility.
- **Improvement recommendations:** Create a Helm chart for enterprise customers demanding on-prem.
- **Status:** Better suited for V2.

**36. Cognitive Load**
- **Score:** 85
- **Weight:** 1
- **Weighted deficiency signal:** 15
- **Justification:** Progressive disclosure in UI helps, but domain concepts (Context -> Graph -> Authority) remain heavy.
- **Tradeoffs:** Domain accuracy vs. simplified terminology.
- **Improvement recommendations:** Add inline tooltips for domain terms in UI.
- **Status:** Fixable in V1.

**37. Change Impact Clarity**
- **Score:** 85
- **Weight:** 1
- **Weighted deficiency signal:** 15
- **Justification:** Excellent structured comparison, but diff UI is visually raw.
- **Tradeoffs:** Data accuracy vs. visual polish.
- **Improvement recommendations:** Upgrade the visual diff component in `LegacyRunComparisonView`.
- **Status:** Fixable in V1.

**38. Modularity**
- **Score:** 85
- **Weight:** 1
- **Weighted deficiency signal:** 15
- **Justification:** Backend cleanly separated. Frontend is monolithic Next.js.
- **Tradeoffs:** Simple deployment vs. micro-frontend flexibility.
- **Improvement recommendations:** Keep components small and abstract API logic further.
- **Status:** Fixable in V1.

**39. Accessibility**
- **Score:** 90
- **Weight:** 1
- **Weighted deficiency signal:** 10
- **Justification:** VPAT 2.5 AA completed. Inline styles might cause edge-case screen reader issues.
- **Tradeoffs:** Fast UI delivery vs. semantic HTML perfection.
- **Improvement recommendations:** Audit UI with Axe-core post-Tailwind migration.
- **Status:** Fixable in V1.

**40. Auditability**
- **Score:** 95
- **Weight:** 2
- **Weighted deficiency signal:** 10
- **Justification:** Exceptional SQL-backed append-only logs.
- **Tradeoffs:** High storage costs vs. perfect compliance.
- **Improvement recommendations:** None needed currently.
- **Status:** N/A

**41. Procurement Readiness**
- **Score:** 95
- **Weight:** 2
- **Weighted deficiency signal:** 10
- **Justification:** Automated CLI procurement pack generation is world-class.
- **Tradeoffs:** None.
- **Improvement recommendations:** Automatically inject signed NDA watermarks.
- **Status:** Better suited for V1.1.

**42. Data Consistency**
- **Score:** 95
- **Weight:** 2
- **Weighted deficiency signal:** 10
- **Justification:** Strong relational FKs, strict SQL DDL.
- **Tradeoffs:** Migration rigidity vs. flexible schema.
- **Improvement recommendations:** Maintain current rigor.
- **Status:** N/A

**43. Explainability**
- **Score:** 95
- **Weight:** 2
- **Weighted deficiency signal:** 10
- **Justification:** `ExplainabilityTrace` and confidence scoring are top-tier.
- **Tradeoffs:** High token cost vs. transparency.
- **Improvement recommendations:** None needed currently.
- **Status:** N/A

**44. Observability**
- **Score:** 95
- **Weight:** 1
- **Weighted deficiency signal:** 5
- **Justification:** Outstanding backend OpenTelemetry integration. UI lacks tracing.
- **Tradeoffs:** Backend focus vs. end-to-end visibility.
- **Improvement recommendations:** Add Otel to the Next.js client.
- **Status:** Fixable in V1.

**45. Documentation**
- **Score:** 95
- **Weight:** 1
- **Weighted deficiency signal:** 5
- **Justification:** Exhaustive, highly accurate Markdown library.
- **Tradeoffs:** High maintenance burden vs. transparency.
- **Improvement recommendations:** None needed currently.
- **Status:** N/A

**46. Azure Ecosystem Fit**
- **Score:** 95
- **Weight:** 1
- **Weighted deficiency signal:** 5
- **Justification:** Deeply integrated with ACA, Service Bus, SQL, Entra.
- **Tradeoffs:** Vendor lock-in.
- **Improvement recommendations:** None needed currently.
- **Status:** N/A

## Top 10 Most Important Weaknesses
1. **UI Maintainability Fragility:** Inline CSS, manual type coercion, and lack of global state ensure the frontend will become unmaintainable technical debt rapidly.
2. **Adoption Friction for Self-Hosted Evaluations:** Complex local setup (SQL, Entra ID, Azurite) alienates quick PoCs unless customers use the non-production Docker Compose.
3. **Workflow Isolation:** The lack of IDE integration and chat-ops keeps ArchLucid as an out-of-band portal, limiting developer engagement and daily active use.
4. **Client-Side Performance Risks:** Absolutely no UI caching leads to redundant API calls on every render, wasting compute and creating latency.
5. **Fragile UI Error States:** The absence of React Error Boundaries means whole component trees crash on minor data anomalies, harming user trust.
6. **Insufficient UI Test Automation:** E2E tests exist but are not enforced in CI, making frontend regressions highly likely during rapid iterations.
7. **Uninstrumented Client Latency:** OpenTelemetry stops at the API, blinding operators to the actual UI performance experienced by end-users.
8. **Rate Limit UX Degradation:** 429 errors are handled as generic failures rather than graceful backoffs with "Retry-After" indicators.
9. **Strict Ingestion Schema Rigidness:** Highly structured input requirements prevent easy onboarding of messy, legacy enterprise architecture documents.
10. **Delayed Native Interoperability:** Heavy reliance on raw webhooks over native Jira/ServiceNow integrations limits immediate workflow embeddedness.

## Top 5 Monetization Blockers
1. **High Time-To-Value for complex architectures:** Hitting the 30-minute pilot claim requires pristine inputs; messy enterprise data will stall PoCs and kill deals.
2. **Out-of-band workflow limits daily active usage:** Without Slack/Teams/IDE integrations, architects will forget to use it, preventing expansion and renewals.
3. **Soft Commercial Packaging:** V1 relies on soft UI constraints rather than hard API billing gates for some features, risking revenue leakage.
4. **Friction in proving ongoing ROI:** Pilot value is proven via static PDFs rather than live, interactive dashboards that executives can check daily.
5. **Lack of Turnkey SSO for non-Microsoft IdPs:** Enterprises running Okta or Ping will stall procurement trying to configure Entra ID federation.

## Top 5 Enterprise Adoption Blockers
1. **Heavy reliance on manual API/CLI calls for advanced features:** Operations like Alert Simulation and bulk archiving lack polished UI flows.
2. **Strict schema requirements for ingestion:** Legacy enterprise architecture docs will fail to process without significant manual massaging.
3. **Audit log search is paginated but lacks deep SIEM native connectors:** Enterprises want Splunk/Sentinel integration out-of-the-box, not just CSV exports.
4. **Lack of granular role-based UI scoping:** UI relies heavily on progressive disclosure rather than hard entitlement boundaries, confusing strict compliance teams.
5. **Absence of external third-party attestation:** While internal SOC2 is great, the deferred V2 external pen-test will block highly regulated buyers.

## Top 5 Engineering Risks
1. **Manual JSON coercion in UI:** Hand-written `coerce*` guards in `operator-response-guards.ts` are highly prone to silent failures as the API evolves.
2. **Inline CSS architecture:** `archlucid-ui` guarantees an unmaintainable UI codebase within 6 months, slowing feature delivery.
3. **Total lack of client-side caching:** Synchronous server-to-server fetches on every render will DDOS the C# API under heavy read load.
4. **Absence of React Error Boundaries:** Ensures full-page crashes for minor data anomalies, damaging perceived reliability.
5. **Missing UI OpenTelemetry tracing:** Blinds SREs to the actual operator experience, making frontend performance degradation invisible.

## Most Important Truth
ArchLucid possesses an exceptionally robust, enterprise-grade backend with stellar data consistency, security, and traceability, but its frontend operator shell is built like a disposable prototype, severely threatening long-term maintainability, scalability, and perceived product quality.

## Top Improvement Opportunities

1. **Migrate Operator UI Shape Validation to Zod**
   - **Why it matters:** Hand-written `coerce*` guards scale poorly, miss deep nested type mismatches, and increase bug risk.
   - **Expected impact:** Directly improves Correctness (+5-8 pts), Maintainability (+15-20 pts), and Reliability (+5 pts). Weighted readiness impact: +0.6-0.9%.
   - **Affected qualities:** Correctness, Maintainability, Reliability.
   - **Status:** Actionable Now.
   - **Prompt:** Install `zod`. In `archlucid-ui/src/lib/operator-response-guards.ts`, replace hand-written `coerce*` functions (e.g., `coerceRunSummaryList`, `coerceRunDetail`, `coerceManifestSummary`) with Zod schemas (`z.object({...})`). Update the return types to use `z.infer`. Ensure existing `ok: true/false` return shapes remain backward compatible for consumers. Do not change any backend API code.

2. **Implement Tailwind CSS for Operator UI Maintainability**
   - **Why it matters:** Inline styles are currently used, which severely limits responsive design, themeability, and code readability.
   - **Expected impact:** Directly improves Maintainability (+20-25 pts), Usability (+10-15 pts), and Extensibility (+5 pts). Weighted readiness impact: +0.8-1.2%.
   - **Affected qualities:** Maintainability, Usability, Extensibility.
   - **Status:** Actionable Now.
   - **Prompt:** Install and configure `tailwindcss`, `postcss`, and `autoprefixer` in `archlucid-ui`. Create `tailwind.config.ts` and `globals.css` with Tailwind directives. Refactor `OperatorErrorCallout`, `SectionCard`, and `ArtifactListTable` to use Tailwind utility classes instead of inline `style={{...}}`. Do not change the visual layout or colors, just replace the implementation.

3. **Add SWR Client-Side Caching for Immutable Artifacts**
   - **Why it matters:** The UI fetches data on every page load. Runs and Manifests are immutable once committed; caching them reduces API load.
   - **Expected impact:** Directly improves Performance (+15-20 pts), Reliability (+10-15 pts), and Cost-Effectiveness (+5-10 pts). Weighted readiness impact: +0.5-0.8%.
   - **Affected qualities:** Performance, Reliability, Cost-Effectiveness.
   - **Status:** Actionable Now.
   - **Prompt:** Install `swr` in `archlucid-ui`. Refactor the client components `GraphPage` and `ComparePage` to use `useSWR` for fetching run details and comparison data instead of manual `useEffect` + `apiGet` calls. Configure the SWR cache to have a high `dedupingInterval` for these immutable resources. Do not alter the server components.

4. **Implement Zustand for Global UI Notifications**
   - **Why it matters:** Background tasks and transient API errors need a global notification system, which is currently missing.
   - **Expected impact:** Directly improves Usability (+10-15 pts) and Manageability (+5-10 pts). Weighted readiness impact: +0.4-0.6%.
   - **Affected qualities:** Usability, Manageability.
   - **Status:** Actionable Now.
   - **Prompt:** Install `zustand` in `archlucid-ui`. Create a new store `src/lib/store/toast-store.ts` with `addToast`, `removeToast`, and `toasts` array. Create a `ToastContainer` component and mount it in the root layout. Update `resolveRequest` in `api.ts` to dispatch a global error toast on HTTP 500s or network failures. Do not refactor existing inline callouts (`OperatorErrorCallout`).

5. **DEFERRED Finalize Pricing Tiers and Feature Matrix Mapping**
   - **Why it matters:** V1 has soft UI gating, but hard commercial enforcement requires mapping specific endpoints to Stripe SKUs to prevent revenue leakage.
   - **Expected impact:** Crucial for Commercial Packaging Readiness and Marketability.
   - **Affected qualities:** Commercial Packaging Readiness, Stickiness.
   - **Status:** DEFERRED.
   - **Input needed:** I need you to provide the exact mapping of API endpoints to your specific Stripe SKUs (Standard vs Enterprise) so we can implement hard billing gates.

6. **Enable Automated Playwright E2E Tests in CI**
   - **Why it matters:** Playwright exists but is not required for builds, increasing the risk of merging broken UI code during refactors.
   - **Expected impact:** Directly improves Testability (+15-20 pts) and Correctness (+5-10 pts). Weighted readiness impact: +0.4-0.7%.
   - **Affected qualities:** Testability, Correctness.
   - **Status:** Actionable Now.
   - **Prompt:** Create a new GitHub Actions workflow `.github/workflows/playwright.yml`. Configure it to run `npm ci`, `npx playwright install --with-deps`, and `npx playwright test` inside the `archlucid-ui` directory on every pull request. Add a step to upload Playwright HTML reports as workflow artifacts on failure. Do not write new Playwright tests, just enable the runner for existing ones.

7. **Implement React Error Boundaries for Crash Recovery**
   - **Why it matters:** Client-side JS exceptions currently crash the React tree. Error boundaries isolate crashes and keep the navigation shell alive.
   - **Expected impact:** Directly improves Supportability (+10-15 pts) and Reliability (+5-10 pts). Weighted readiness impact: +0.3-0.5%.
   - **Affected qualities:** Supportability, Reliability.
   - **Status:** Actionable Now.
   - **Prompt:** Create an `ErrorBoundary` component in `archlucid-ui/src/components/ErrorBoundary.tsx` that catches rendering errors and displays a fallback UI using `OperatorErrorCallout`. Wrap the main interactive client components (`GraphViewer`, `CompareForm`, `ReplayPage`) with this `ErrorBoundary`. Log caught errors to `console.error` with a specific `[Crash]` prefix.

8. **Add OpenTelemetry Tracing to UI API Client**
   - **Why it matters:** The UI has no visibility into API latency from the client's perspective, blinding operators to real-world UX issues.
   - **Expected impact:** Directly improves Observability (+5-10 pts) and Performance (+5 pts). Weighted readiness impact: +0.2-0.4%.
   - **Affected qualities:** Observability, Performance.
   - **Status:** Actionable Now.
   - **Prompt:** Install `@opentelemetry/api` in `archlucid-ui`. In `src/lib/api.ts`, wrap the `apiGet` and `apiPostJson` fetch calls in an OpenTelemetry span. Extract the HTTP method, URL path, and status code as span attributes. Inject the active W3C `traceparent` header into the outgoing requests to the `/api/proxy` route so distributed traces link UI actions to backend SQL spans.

9. **DEFERRED Finalize Third-Party Pen-Test Scope (V2)**
   - **Why it matters:** Highly regulated enterprises require an external attestation, which is deferred to V2.
   - **Expected impact:** Unblocks enterprise procurement and boosts Trustworthiness.
   - **Affected qualities:** Trustworthiness, Procurement Readiness.
   - **Status:** DEFERRED.
   - **Input needed:** Please confirm the budget, timeline, and select the vendor so we can finalize the Statement of Work (SoW) and prepare the sandbox environment.

10. **Implement Graceful 429 Rate Limit Handling in UI**
    - **Why it matters:** API rate limits return 429s. The UI currently treats these as generic errors, confusing users during high-volume operations.
    - **Expected impact:** Directly improves Usability (+5-10 pts) and Reliability (+5-10 pts). Weighted readiness impact: +0.3-0.5%.
    - **Affected qualities:** Usability, Reliability.
    - **Status:** Actionable Now.
    - **Prompt:** Update `resolveRequest` in `archlucid-ui/src/lib/api.ts` to explicitly detect HTTP 429 responses. Extract the `Retry-After` header if present. Modify the error return shape to include an `isRateLimited` flag and `retryAfterSeconds`. Update `OperatorErrorCallout` to render a specific "Rate limit exceeded, please wait X seconds" message when this flag is true.

## Pending Questions for Later

- **DEFERRED Finalize Pricing Tiers and Feature Matrix Mapping:**
  - What is the exact mapping of API endpoints to your specific Stripe SKUs (Standard vs Enterprise)? 

- **DEFERRED Finalize Third-Party Pen-Test Scope (V2):**
  - What is the budget and timeline for the V2 pen-test?
  - Which vendor has been selected so the SoW can be finalized?