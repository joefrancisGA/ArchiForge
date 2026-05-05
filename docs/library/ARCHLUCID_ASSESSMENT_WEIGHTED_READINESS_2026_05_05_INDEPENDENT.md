> **Scope:** Independent first-principles assessment of ArchLucid V1.1 readiness based on the provided quality model and weights.

# ArchLucid Assessment – Weighted Readiness 75.32%

## Executive Summary

**Overall Readiness**
ArchLucid demonstrates a solid V1 foundation with a weighted readiness of 75.32%. The core architecture is sound, leveraging SQL Server, RLS, and Azure-native patterns effectively. However, the product currently leans heavily on operator technical proficiency, which introduces friction in time-to-value and broader enterprise adoption.

**Commercial Picture**
The commercial foundation is viable for technical buyers, but Executive Value Visibility and Proof-of-ROI Readiness are lagging. The product solves complex architectural and governance problems, but translating those technical wins into easily digestible executive dashboards or automated ROI metrics remains a challenge.

**Enterprise Picture**
Enterprise trust and auditability are strong points, supported by the durable audit log and RLS. However, Workflow Embeddedness and Customer Self-Sufficiency are weaker. **First-party** **Jira** and **ServiceNow** are **V1 commitments** ([`docs/library/V1_SCOPE.md`](library/V1_SCOPE.md) §2.13); until shipped or enabled for a tenant, teams rely on webhooks and customer-operated bridges — **Slack** chat-ops remains **V2**, which still raises bridge burden versus native connectors for chat-centric workflows.

**Engineering Picture**
Engineering fundamentals (Security, Architectural Integrity, Azure Compatibility) score highly. The system is built defensively. The primary engineering risks lie in Cognitive Load and Explainability—the agentic outputs and governance workflows are complex, and making them transparent and easy to troubleshoot for tier-1 support or average operators needs improvement.

## Weighted Quality Assessment

*(Ordered from most urgent/highest weighted deficiency to least urgent)*

1. **Marketability**
   - **Score:** 70
   - **Weight:** 8
   - **Weighted impact on readiness:** 5.60 / 8.00 (Deficiency: 2.40)
   - **Justification:** The product is highly technical. While the core value is strong, marketing it to non-architects requires translating complex agentic workflows into simple business outcomes.
   - **Tradeoffs:** Balancing technical accuracy with marketing simplicity.
   - **Improvement recommendations:** Develop outcome-focused landing pages and simplified "first-run" demo scripts that abstract away the configuration complexity.
   - **Status:** Fixable in v1.1.

2. **Proof-of-ROI Readiness**
   - **Score:** 65
   - **Weight:** 5
   - **Weighted impact on readiness:** 3.25 / 5.00 (Deficiency: 1.75)
   - **Justification:** It is difficult for a pilot user to automatically quantify the hours saved or risks mitigated by the agentic architecture reviews.
   - **Tradeoffs:** Building ROI calculators vs. building core features.
   - **Improvement recommendations:** Add a baseline ROI telemetry module that tracks "issues caught pre-commit" and estimates hours saved.
   - **Status:** Fixable in v1.1.

3. **Executive Value Visibility**
   - **Score:** 60
   - **Weight:** 4
   - **Weighted impact on readiness:** 2.40 / 4.00 (Deficiency: 1.60)
   - **Justification:** The UI is operator-heavy. There is no "single pane of glass" for a CISO or VP of Engineering to see the overall architectural health or compliance drift at a glance.
   - **Tradeoffs:** Executive dashboards often require aggregating data that is currently tenant-isolated or run-specific.
   - **Improvement recommendations:** Create a high-level "Workspace Health" dashboard summarizing governance gates passed/failed.
   - **Status:** Fixable in v1.1.

4. **Adoption Friction**
   - **Score:** 75
   - **Weight:** 6
   - **Weighted impact on readiness:** 4.50 / 6.00 (Deficiency: 1.50)
   - **Justification:** The initial setup (SQL, Entra ID, connection strings) is standard for enterprise but still poses a hurdle for quick PLG-style trials.
   - **Tradeoffs:** Security and isolation (RLS) vs. frictionless onboarding.
   - **Improvement recommendations:** Streamline the onboarding wizard and provide a "sandbox" mode with pre-configured mocks for instant exploration.
   - **Status:** Fixable in v1.

5. **Time-to-Value**
   - **Score:** 80
   - **Weight:** 7
   - **Weighted impact on readiness:** 5.60 / 7.00 (Deficiency: 1.40)
   - **Justification:** Once configured, the first pilot run delivers value quickly, but the prerequisite configuration delays that "aha" moment.
   - **Tradeoffs:** Comprehensive setup vs. quick wins.
   - **Improvement recommendations:** Provide out-of-the-box template architectures that users can run immediately without bringing their own complex inputs.
   - **Status:** Fixable in v1.

6. **Workflow Embeddedness**
   - **Score:** 60
   - **Weight:** 3
   - **Weighted impact on readiness:** 1.80 / 3.00 (Deficiency: 1.20)
   - **Justification:** With ITSM (Jira/ServiceNow) deferred, users must manually bridge webhooks to their ticketing systems.
   - **Tradeoffs:** Deferring first-party connectors saved V1 engineering time but pushes work to the customer.
   - **Improvement recommendations:** Provide robust, copy-paste Power Automate or Logic App templates for the webhook-to-ticket bridge.
   - **Status:** Deferred scope (V1.1).

7. **Correctness**
   - **Score:** 80
   - **Weight:** 4
   - **Weighted impact on readiness:** 3.20 / 4.00 (Deficiency: 0.80)
   - **Justification:** Agentic outputs can sometimes hallucinate or diverge from strict architectural constraints.
   - **Tradeoffs:** LLM creativity vs. deterministic correctness.
   - **Improvement recommendations:** Strengthen the pre-commit governance gates with stricter deterministic checks alongside the LLM evaluations.
   - **Status:** Fixable in v1.

8. **Usability**
   - **Score:** 75
   - **Weight:** 3
   - **Weighted impact on readiness:** 2.25 / 3.00 (Deficiency: 0.75)
   - **Justification:** The operator shell is functional but dense.
   - **Tradeoffs:** Exposing all data vs. guided workflows.
   - **Improvement recommendations:** Implement progressive disclosure in the UI, hiding advanced governance and audit links until explicitly needed.
   - **Status:** Fixable in v1.

9. **Explainability**
   - **Score:** 70
   - **Weight:** 2
   - **Weighted impact on readiness:** 1.40 / 2.00 (Deficiency: 0.60)
   - **Justification:** When the agent makes a complex architectural decision, tracing *why* it made that decision through the provenance graph is difficult for non-experts.
   - **Tradeoffs:** Deep provenance data vs. human-readable summaries.
   - **Improvement recommendations:** Add an "Explain this decision" LLM-driven summary attached to key nodes in the provenance graph.
   - **Status:** Fixable in v1.1.

10. **Interoperability**
    - **Score:** 70
    - **Weight:** 2
    - **Weighted impact on readiness:** 1.40 / 2.00 (Deficiency: 0.60)
    - **Justification:** Relies heavily on webhooks for outbound integration.
    - **Tradeoffs:** Generic webhooks vs. specific API clients.
    - **Improvement recommendations:** Expand the webhook payload documentation with concrete examples for common SIEMs.
    - **Status:** Fixable in v1.

11. **Customer Self-Sufficiency**
    - **Score:** 65
    - **Weight:** 1
    - **Weighted impact on readiness:** 0.65 / 1.00 (Deficiency: 0.35)
    - **Justification:** High reliance on support or SEs during pilot setup.
    - **Tradeoffs:** White-glove sales motion vs. PLG.
    - **Improvement recommendations:** Enhance in-app contextual help and troubleshooting guides.
    - **Status:** Fixable in v1.

12. **Cognitive Load**
    - **Score:** 65
    - **Weight:** 1
    - **Weighted impact on readiness:** 0.65 / 1.00 (Deficiency: 0.35)
    - **Justification:** Operators must understand manifests, runs, artifacts, and provenance simultaneously.
    - **Tradeoffs:** Power vs. simplicity.
    - **Improvement recommendations:** Simplify the default run view to show only the final golden manifest and critical alerts.
    - **Status:** Fixable in v1.

13. **Template and Accelerator Richness**
    - **Score:** 65
    - **Weight:** 1
    - **Weighted impact on readiness:** 0.65 / 1.00 (Deficiency: 0.35)
    - **Justification:** Few out-of-the-box templates.
    - **Tradeoffs:** Custom architecture vs. boilerplate.
    - **Improvement recommendations:** Ship 3-5 standard reference architectures (e.g., standard 3-tier web app, serverless API) as built-in templates.
    - **Status:** Fixable in v1.

14. **Architectural Integrity**
    - **Score:** 90
    - **Weight:** 3
    - **Weighted impact on readiness:** 2.70 / 3.00 (Deficiency: 0.30)
    - **Justification:** Strong, coherent design using SQL, RLS, and clean API boundaries.
    - **Tradeoffs:** Rigidity vs. flexibility.
    - **Improvement recommendations:** Maintain current discipline; ensure new endpoints follow the Coordinator Strangler plan.
    - **Status:** Strong.

15. **Security**
    - **Score:** 90
    - **Weight:** 3
    - **Weighted impact on readiness:** 2.70 / 3.00 (Deficiency: 0.30)
    - **Justification:** Excellent defense-in-depth (RLS, Key Vault, no SMB).
    - **Tradeoffs:** Development friction vs. security.
    - **Improvement recommendations:** Continue OWASP ZAP and schema validation.
    - **Status:** Strong.

*(Remaining qualities score between 80-90 with minimal weighted deficiency and are omitted for brevity in this summary, but contribute to the 75.32% total).*

## Top 10 Most Important Weaknesses

1. **Lack of Executive Visibility:** No high-level dashboards for decision-makers.
2. **Unquantified ROI:** Hard for champions to prove the tool's financial or time-saving value.
3. **High Onboarding Friction:** Technical setup requires significant operator expertise.
4. **Opaque Agent Reasoning:** Provenance graphs are too dense for quick comprehension.
5. **Integration Burden:** Deferring ITSM connectors forces customers to build their own webhook bridges.
6. **Steep Learning Curve:** High cognitive load for new operators navigating runs and manifests.
7. **Template Scarcity:** Lack of out-of-the-box starting points delays time-to-value.
8. **Hallucination Risks:** Agentic outputs need stronger deterministic guardrails.
9. **Self-Serve Limitations:** Customers struggle to troubleshoot configuration errors without SE help.
10. **Marketing Translation:** Highly technical features aren't easily mapped to business outcomes.

## Top 5 Monetization Blockers

1. **Missing ROI Telemetry:** Buyers cannot easily justify the purchase without clear metrics.
2. **Executive Dashboard Gap:** Economic buyers (CISOs, VPs) don't have a view tailored to them.
3. **Sales-Led Bottleneck:** The complexity of the pilot setup limits the volume of concurrent trials.
4. **Deferred Commerce Rails:** (Deferred to V1.1) Lack of live Stripe/Marketplace integration prevents self-serve conversion.
5. **Value Translation:** Marketing materials are too focused on architecture rather than business risk mitigation.

## Top 5 Enterprise Adoption Blockers

1. **Manual ITSM Bridging:** Enterprises expect native Jira/ServiceNow integration, not just webhooks.
2. **Setup Complexity:** Requiring Entra ID and SQL configuration upfront slows down departmental adoption.
3. **Audit Log Consumption:** While the audit log is durable, exporting and mapping it to specific SIEMs requires manual effort.
4. **Operator Training:** The system requires a trained operator, limiting casual adoption by average developers.
5. **Deferred Compliance Attestations:** (Deferred to post-V1.1) Lack of a CPA-issued SOC 2 report causes friction in procurement.

## Top 5 Engineering Risks

1. **Agent Nondeterminism:** LLM-driven architecture decisions may occasionally violate strict enterprise policies if not caught by the pre-commit gate.
2. **RLS Complexity:** Maintaining Row-Level Security across all new features requires strict developer discipline.
3. **Webhook Delivery Failures:** Without first-party ITSM connectors, webhook delivery failures could result in missed critical alerts.
4. **Performance at Scale:** The provenance graph and large manifests may cause UI latency for massive enterprise architectures.
5. **Coordinator Strangler Execution:** Migrating legacy coordinator endpoints to the new Authority semantics risks introducing regressions if not tested thoroughly.

## Most Important Truth

ArchLucid is a highly secure, architecturally sound platform built for experts, but its current lack of executive visibility, automated ROI tracking, and native ITSM integrations creates significant friction for commercial scaling and rapid enterprise adoption.

## Top Improvement Opportunities

1. **Executive Workspace Health dashboard (operator governance route)**
   - **Why it matters:** Economic buyers and sponsors need a single pane of glass for risk and governance posture without drilling every run.
   - **Expected impact:** Improves Executive Value Visibility (+12–18 pts), Marketability (+6–10 pts), Proof-of-ROI Readiness (+4–8 pts via pre-commit + findings proxy). Weighted readiness impact: **+1.2–2.0%** (approximate).
   - **Affected qualities:** Executive Value Visibility, Marketability, Proof-of-ROI Readiness, Usability.
   - **Scope (agreed):** **Current `SESSION_CONTEXT` only** (tenant / workspace / project per request). **No cross-tenant or cross-workspace rollup.** Show an explicit banner: data reflects the active scope from the operator shell (same boundaries as `GET /v1/governance/*` and `GET /v1/audit/search`).
   - **Headline KPIs (exactly five):**
     1. **Governance pre-commit outcomes (rolling 30 days, audit-backed):** counts of `GovernancePreCommitBlocked` and `GovernancePreCommitWarned` from `GET /v1/audit/search` with `fromUtc` / `toUtc` (document cap: if `take` is capped, show “≥” or “first page” honesty per `V1_DEFERRED.md` audit keyset notes).
     2. **High/Critical finding exposure (rolling 90 days, pilot-value proxy):** `critical` + `high` from `GET /v1/tenant/pilot-value-report` (same pattern as `archlucid-ui/src/app/(operator)/value-report/pilot/page.tsx`). **Label clearly** that this is severity exposure in the report window, not the same as an “open backlog aged” inventory unless extended later.
     3. **Compliance drift trend:** reuse `getComplianceDriftTrend` + existing `ComplianceDriftChart` (`GET /v1/governance/compliance-drift-trend`, daily buckets, last 30 days) — see `docs/library/UI_COMPONENTS.md`.
     4. **Approval SLA posture:** derived from `getGovernanceDashboard()` — pending count; among **pending** approvals with `slaDeadlineUtc`, count overdue vs on-track; among **recentDecisions** with `slaDeadlineUtc` set and `reviewedUtc` set, compute **% reviewed on or before** deadline (simple on-time rate).
     5. **Pre-commit blocks as value proxy:** same blocked count as (1), with a **secondary line**: “Estimated review-hours surfaced pre-commit” using **shared coefficients from `archlucid-ui/src/lib/roi-assumptions.ts`** (introduced by Improvement 2). If Improvement 2 has not shipped yet, ship Improvement 1 with a temporary local constant `ASSUMED_HOURS_PER_BLOCK = 2` and replace it with the shared module when Improvement 2 lands. In-UI tooltip explains it is an estimate, not a measurement.
   - **Actionable:** Yes.
   - **Cursor Prompt:**
     ```text
     Implement the “Executive Workspace Health” dashboard by replacing the placeholder at archlucid-ui/src/app/(operator)/governance/dashboard/page.tsx.

     Goals:
     - Single page at /governance/dashboard (existing route) that operators and sponsors can bookmark.
     - Must respect current API scope only: do not add cross-workspace aggregation. Reuse existing client helpers in archlucid-ui/src/lib/api.ts: getGovernanceDashboard, getComplianceDriftTrend, searchAuditEvents, and fetch /v1/tenant/pilot-value-report the same way as archlucid-ui/src/app/(operator)/value-report/pilot/page.tsx (or add a small typed helper in api.ts if cleaner).
     - Top of page: a prominent scope banner (reuse existing layer context UI if the shell already exposes tenant/workspace/project names; otherwise a short line stating that figures are limited to the authenticated scope).
     - Render exactly five KPI sections in this order:
       (1) Pre-commit outcomes (30d): counts for audit event types GovernancePreCommitBlocked and GovernancePreCommitWarned via searchAuditEvents with eventType + fromUtc/toUtc. If pagination truncates, display counts with honest “partial sample” wording per API take limits.
       (2) High/Critical findings (90d proxy): from pilot-value-report JSON findingsBySeverity (critical + high). Subtext link to /governance/findings for the operational queue.
       (3) Compliance drift: ComplianceDriftChart with points from getComplianceDriftTrend (last 30 days, bucketMinutes=1440).
       (4) SLA posture: from getGovernanceDashboard — pendingCount; overdue pending (now > slaDeadlineUtc); on-time % for recent terminal decisions where both slaDeadlineUtc and reviewedUtc exist.
       (5) Value proxy: show blocked count again plus “assumed hours surfaced” = blocked * constant from a new file e.g. archlucid-ui/src/lib/workspace-health-assumptions.ts — tooltip must say the coefficient is a placeholder until Improvement 2 (ROI module).

     Constraints:
     - Do not change RBAC policies or add new API endpoints unless you discover a hard blocker; prefer composing existing routes.
     - Keep demo-mode behavior: if isNextPublicDemoMode() || isStaticDemoPayloadFallbackEnabled(), keep a clear “not available in demo mode” panel (today’s pattern).
     - Add at least one Vitest test for any non-trivial pure helper (e.g. SLA percentage + overdue counts) in a new *.test.ts file.
     - Do not reference or edit prior assessment markdown files.

     Acceptance criteria:
     - /governance/dashboard renders all five KPI blocks with loading and error states.
     - No cross-scope claims; banner + labels match SESSION_CONTEXT honesty.
     - From `archlucid-ui/`, `npm test` (vitest) passes for new tests.
     ```

2. **ROI telemetry module — “hours surfaced pre-commit” (split RBAC)**
   - **Why it matters:** Champions need a defensible single-page ROI artifact. Leading with **hours surfaced pre-commit** is grounded in audit events and severity counts already in the repo, and it does not require a buyer to argue an internal $/hour rate before the conversation can start.
   - **Expected impact:** Improves Proof-of-ROI Readiness (+15–20 pts), Marketability (+4–6 pts), Executive Value Visibility (+4–6 pts). Weighted readiness impact: **+0.9–1.4%**.
   - **Affected qualities:** Proof-of-ROI Readiness, Marketability, Executive Value Visibility, Decision Velocity.
   - **Scope (agreed):** Current `SESSION_CONTEXT` only. Reuses existing data paths (`/v1/tenant/pilot-value-report`, `/v1/audit/search`); no new SQL.
   - **Formula (defaults, editable in one config file):**
     - **Hours surfaced** = `8 × Critical findings + 3 × High + 1 × Medium + 2 × pre-commit Blocked events` over the report window.
     - **Default coefficients** live in `archlucid-ui/src/lib/roi-assumptions.ts`: `HOURS_PER_CRITICAL = 8`, `HOURS_PER_HIGH = 3`, `HOURS_PER_MEDIUM = 1`, `HOURS_PER_PRECOMMIT_BLOCK = 2`.
     - **Severity counts** come from `findingsBySeverity` on `GET /v1/tenant/pilot-value-report` (existing typed response in `archlucid-ui/src/app/(operator)/value-report/pilot/page.tsx`).
     - **Pre-commit blocked count** comes from `searchAuditEvents({ eventType: "GovernancePreCommitBlocked", fromUtc, toUtc })` and is labeled honestly under API caps (`At least N (sampled)` when paging is hit).
   - **Windows (two tiles, side by side):**
     - **Rolling 30 days** — default headline.
     - **Since pilot start** — `fromUtc = tenant.CreatedUtc` (already used by `PilotValueReportService`).
   - **RBAC split:**
     - **Operator (`ReadAuthority`):** sees **counts and hours**.
     - **Admin (`AdminAuthority`):** additionally sees **`$/hour` input + total dollars** and can edit coefficients (stored in **localStorage only** for v1.1 — no new SQL). Admin overrides clearly labeled “local override (this browser).”
   - **Actionable:** Yes.
   - **Cursor Prompt:**
     ```text
     Build the v1.1 ROI Telemetry Module on top of the existing Workspace Health and pilot-value-report surfaces. Do not introduce new backend endpoints, new SQL tables, or new audit event types.

     Files to add or edit (paths relative to repo root):
       - NEW  archlucid-ui/src/lib/roi-assumptions.ts
              Export: HOURS_PER_CRITICAL=8, HOURS_PER_HIGH=3, HOURS_PER_MEDIUM=1, HOURS_PER_PRECOMMIT_BLOCK=2,
              DEFAULT_LOADED_HOURLY_USD=150 (admin-only display).
              Export pure helpers: hoursSurfaced({critical,high,medium,precommitBlocks}, coefficients?), formatHours(hours), formatUsd(amount).
       - NEW  archlucid-ui/src/lib/roi-assumptions.test.ts (vitest)
              Cover: zero counts -> 0; default coefficients math; coefficient override path.
       - NEW  archlucid-ui/src/components/RoiTelemetryCard.tsx
              Props: { window: "rolling30" | "pilotToDate"; severity: SeverityJson; precommitBlocks: number; precommitBlocksExact: boolean; isAdmin: boolean; }
              Renders: hours surfaced (always); when isAdmin, additional row with editable $/hour input (controlled, persisted to localStorage key "archlucid.roi.hourlyUsd") and computed total USD; "local override" tag next to any non-default value.
              When precommitBlocksExact === false, append " (sampled)" to the blocked count and tooltip explains audit paging caps.
       - NEW  archlucid-ui/src/components/RoiTelemetryCard.test.tsx (vitest)
              Cover: operator view hides USD, admin view shows USD, sampled label rendering.
       - NEW  archlucid-ui/src/app/(operator)/value-report/roi/page.tsx
              Two RoiTelemetryCard instances: rolling 30 days (default), pilot-to-date.
              Data sources:
                * Severity: GET /v1/tenant/pilot-value-report (reuse the JSON shape already declared on the existing pilot value report page; lift its type into archlucid-ui/src/types/pilot-value-report.ts so both pages share it).
                * Pre-commit blocked count: searchAuditEvents({ eventType: "GovernancePreCommitBlocked", fromUtc, toUtc }) (already in archlucid-ui/src/lib/api.ts). If the response indicates more pages exist (hasMore=true), set precommitBlocksExact=false.
                * "Pilot start" = tenant.CreatedUtc; obtain via the existing pilot-value-report response (its fromUtc already defaults to tenant CreatedUtc when no fromUtc is sent — call once with no fromUtc to get the tenant-start window).
              Honest scope banner at top: "Figures reflect your current tenant/workspace/project scope only."
              No cross-workspace aggregation.
       - EDIT archlucid-ui/src/app/(operator)/governance/dashboard/page.tsx
              On tile (5) "Pre-commit blocks as value proxy", replace its placeholder hours line with a call to hoursSurfaced(...) using the same coefficients file. Add a "See ROI report" link to /value-report/roi.
       - EDIT archlucid-ui/src/app/(operator)/value-report/pilot/page.tsx
              Add a single inline link "Open ROI summary" -> /value-report/roi. Do not change existing logic.
       - EDIT archlucid-ui/src/components/ShellNav.tsx (or whichever file owns sidebar links)
              Add "ROI report" under the existing value-report area, behind the same authorization the existing pilot value report uses (ReadAuthority).

     Admin gating:
       - Determine isAdmin in the page using the existing client-side role detection used elsewhere in archlucid-ui (search for "Admin" or "AdminAuthority" usage in headers/menus and reuse). Do not invent a new role check.

     Constraints:
       - Do not add new backend endpoints, new SQL, new audit event types, or new RBAC policies.
       - Do not change ArchLucid.Application or ArchLucid.Api code.
       - Coefficients live only in roi-assumptions.ts; admin overrides persist only in localStorage for v1.1.
       - Demo-mode behavior: if isNextPublicDemoMode() || isStaticDemoPayloadFallbackEnabled(), the ROI page renders a clear "not available in demo mode" panel matching the governance dashboard pattern.

     Acceptance criteria:
       - /value-report/roi renders both windows with correct math given mocked inputs.
       - Operator role sees hours but no USD; Admin role sees hours + USD + editable $/hour input that persists across reloads (localStorage).
       - Vitest tests pass from archlucid-ui via `npm test`.
       - Workspace Health dashboard tile (5) now uses the shared coefficients file (no duplicated constants).
       - No backend changes are introduced.
     ```

3. **Implement Progressive Disclosure in Operator UI**
   - **Why it matters:** Reduces cognitive load for new users by hiding advanced features until needed.
   - **Expected impact:** Improves Usability (+10 pts), Cognitive Load (+15 pts), Adoption Friction (+5 pts). Weighted readiness impact: +0.6-0.8%.
   - **Affected qualities:** Usability, Cognitive Load, Adoption Friction.
   - **Actionable:** Yes.
   - **Cursor Prompt:**
     ```text
     Update the ArchLucid Operator UI navigation sidebar to implement progressive disclosure. 
     1. Hide the "Governance", "Audit", and "Alerts" links by default for new users or users in the "Pilot" phase.
     2. Add a toggle button at the bottom of the sidebar labeled "Show Advanced Operations".
     3. When toggled on, reveal the hidden links. Persist this preference in local storage.
     4. Do not modify any backend routing or RBAC permissions; this is strictly a UI/UX change in the React/frontend components.
     5. Ensure the toggle is accessible (ARIA labels).
     ```

4. **Add Concrete SIEM Webhook Payload Examples**
   - **Why it matters:** Reduces the burden on enterprise teams trying to integrate the audit log with Splunk or Sentinel.
   - **Expected impact:** Improves Interoperability (+10 pts), Customer Self-Sufficiency (+10 pts). Weighted readiness impact: +0.3-0.5%.
   - **Affected qualities:** Interoperability, Customer Self-Sufficiency.
   - **Actionable:** Yes.
   - **Cursor Prompt:**
     ```text
     Update the `docs/library/SIEM_EXPORT.md` and `docs/library/INTEGRATION_EVENTS_AND_WEBHOOKS.md` documentation files.
     1. Add a concrete, copy-pasteable JSON payload example showing exactly how an ArchLucid audit event maps to a Splunk HTTP Event Collector (HEC) format.
     2. Add a second concrete JSON payload example mapping to Microsoft Sentinel (Log Analytics workspace custom log format).
     3. Do not change the actual webhook emission code in the backend. This is a documentation enhancement only.
     ```

5. **Create "Sandbox" Mock Configuration for UI**
   - **Why it matters:** Allows users to explore the UI without setting up SQL and Entra ID first.
   - **Expected impact:** Improves Adoption Friction (+15 pts), Time-to-Value (+10 pts). Weighted readiness impact: +1.0-1.3%.
   - **Affected qualities:** Adoption Friction, Time-to-Value.
   - **Actionable:** Yes.
   - **Cursor Prompt:**
     ```text
     Create a `sandbox-mock-data.json` file in the UI repository containing a static, realistic "Golden Manifest", a sample run history, and 5 sample audit events.
     Update the UI's API client layer to support a `VITE_USE_SANDBOX_MOCKS=true` environment variable.
     When this variable is true, intercept API calls to `/v1/architecture/runs` and `/v1/audit` and return the static mock data instead of making HTTP requests.
     Do not alter the production API client logic; ensure the mock interception is completely bypassed when the variable is false or undefined.
     ```

6. **Add "Explain this Decision" Stub to Provenance Graph**
   - **Why it matters:** Makes complex agentic decisions understandable to non-experts.
   - **Expected impact:** Improves Explainability (+15 pts), Usability (+5 pts). Weighted readiness impact: +0.4-0.6%.
   - **Affected qualities:** Explainability, Usability.
   - **Actionable:** Yes.
   - **Cursor Prompt:**
     ```text
     In the Operator UI Provenance Graph component (`KNOWLEDGE_GRAPH.md` reference), add an "Explain" button to the node detail panel.
     When clicked, display a placeholder modal that says "Explanation generation will be available in a future update."
     Add the corresponding empty API endpoint `GET /v1/architecture/run/{runId}/provenance/{nodeId}/explanation` in the backend (`ArchLucid.Api`) that returns a 501 Not Implemented status with a JSON body `{"message": "Explanation feature pending"}`.
     Ensure the endpoint is secured with the standard `[Authorize]` and RLS checks.
     ```

7. **Ship Standard Reference Architecture Templates**
   - **Why it matters:** Accelerates time-to-value by giving users a starting point.
   - **Expected impact:** Improves Template and Accelerator Richness (+30 pts), Time-to-Value (+10 pts). Weighted readiness impact: +0.8-1.0%.
   - **Affected qualities:** Template and Accelerator Richness, Time-to-Value.
   - **Actionable:** Yes.
   - **Cursor Prompt:**
     ```text
     Create a new directory `templates/reference-architectures/` in the repository root.
     Add two JSON files: `standard-3-tier-web.json` and `azure-serverless-api.json`.
     Populate these files with valid ArchLucid architecture request payloads representing these common patterns.
     Update `docs/library/PILOT_GUIDE.md` to reference these templates, instructing users to use them via the CLI (e.g., `archlucid request create --from-file templates/reference-architectures/standard-3-tier-web.json`).
     Do not modify the core API or CLI code.
     ```

8. **Enhance Pre-Commit Gate with Strict Schema Validation**
   - **Why it matters:** Reduces the risk of LLM hallucinations corrupting the golden manifest.
   - **Expected impact:** Improves Correctness (+10 pts), Reliability (+5 pts). Weighted readiness impact: +0.4-0.6%.
   - **Affected qualities:** Correctness, Reliability.
   - **Actionable:** Yes.
   - **Cursor Prompt:**
     ```text
     In the `ArchLucid.Decisioning` or `ArchLucid.Governance` module, locate the pre-commit governance gate logic (`PRE_COMMIT_GOVERNANCE_GATE.md` reference).
     Add a strict JSON Schema validation step that runs *before* any policy packs are evaluated.
     The validation should ensure the proposed manifest strictly adheres to the `ManifestSchema.json` (or equivalent DTO structure).
     If schema validation fails, immediately reject the commit with a `400 Bad Request` and a specific error message detailing the schema violation, bypassing further governance checks.
     Ensure this does not break existing unit tests; update tests if necessary.
     ```

9. **Deep-link Operator Home to Workspace Health**
   - **Why it matters:** The governance dashboard page is intentionally not in primary nav today; champions need a obvious path after Core Pilot.
   - **Expected impact:** Improves Time-to-Value (+4–6 pts), Executive Value Visibility (+3–5 pts). Weighted readiness impact: **+0.35–0.55%**.
   - **Affected qualities:** Time-to-Value, Executive Value Visibility, Adoption Friction.
   - **Actionable:** Yes.
   - **Cursor Prompt:**
     ```text
     After the Executive Workspace Health dashboard (Improvement 1) ships, add a single secondary action on the operator Home / Core Pilot completion area (find the component that renders Core Pilot next steps — e.g. CorePilotNextStepsCard or Home page in archlucid-ui/src/app/(operator)/page.tsx):
     - Link label: “Workspace health (sponsor view)”
     - href: /governance/dashboard
     - Do not add to marketing site. Do not change primary IA; one subtle text link or outline button is enough.
     - Add a brief Playwright or Vitest assertion only if an existing test pattern already covers Home CTAs; otherwise skip E2E to avoid scope creep.
     ```

## Pending Questions for Later

**Executive Workspace Health (resolved 2026-05-05)**
- KPI set and five-tile layout: agreed in-session (pre-commit audit counts, pilot-value severity proxy, compliance drift chart, SLA posture from governance dashboard, value proxy with placeholder coefficient).
- Scope: **current `SESSION_CONTEXT` only** (no cross-workspace rollup). Future cross-workspace rollups are a separate product decision and likely Admin-only.

**ROI telemetry module (resolved 2026-05-05)**
- **Formula:** lead with **hours surfaced pre-commit** = `8·Critical + 3·High + 1·Medium + 2·pre-commit blocks` over the window; coefficients live in `archlucid-ui/src/lib/roi-assumptions.ts`. **Default loaded $/hour** = `$150` for the admin USD line.
- **Windows:** rolling 30 days (default) **and** since pilot start (tenant `CreatedUtc`).
- **RBAC split:** **Operator** (`ReadAuthority`) sees counts + hours; **Admin** (`AdminAuthority`) additionally sees `$/hour` (editable, localStorage-persisted) + total USD.
- **Storage:** Admin overrides persist in **localStorage only** for v1.1 (no new SQL, no new audit events, no new endpoints).
- **Open follow-ups (v2 candidates, not blocking):** persisting tenant-level coefficients server-side; modeling cycle-time and risk-incident-avoided variants alongside hours.

**Deferred Scope Uncertainty**
- *None identified. The assessment strictly adhered to the V1/V1.1 boundaries defined in `V1_SCOPE.md` and `V1_DEFERRED.md`.*
