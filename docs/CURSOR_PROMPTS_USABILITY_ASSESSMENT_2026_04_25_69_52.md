> **Scope:** Nine paste-ready Cursor prompts for the V1-actionable improvements in [`USABILITY_SOLUTION_QUALITY_ASSESSMENT_2026_04_25_69_52.md`](USABILITY_SOLUTION_QUALITY_ASSESSMENT_2026_04_25_69_52.md). Each prompt is self-contained — it assumes the assistant starts from a clean session with no memory of the assessment.

> **Spine doc:** [Five-document onboarding spine](FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.

# Cursor prompts — V1-actionable usability improvements after the 69.52% assessment (2026-04-25)

**How to use.** One prompt per session. Paste the whole block (between the triple backticks) into a fresh Cursor agent. Each prompt names its **stop-and-ask** boundaries — the assistant should not cross those without owner input. After each prompt completes, update [`docs/PENDING_QUESTIONS.md`](PENDING_QUESTIONS.md) accordingly.

**Extended bundle (same nine prompts + nine Linear-inspired UI prompts):** [`library/CURSOR_PROMPTS_UI_MODERNIZATION_2026_04_25.md`](library/CURSOR_PROMPTS_UI_MODERNIZATION_2026_04_25.md).

**Owner decisions recorded 2026-04-25.** All five pending questions from the assessment are resolved:
- Q1–Q3: Scope switching, non-obscure tenant settings UI, and role/authority management UI all **approved** for V1.
- Q4–Q5: In-app health/diagnostics panel **approved**, gated at **ReadAuthority** (visible to any authenticated user).

> **SaaS audience guard (read before running any prompt below).** ArchLucid is a **SaaS** product. Customers, evaluators, and sponsors never install Docker, SQL, .NET, Node, or Terraform. They only ever interact with the public website (`archlucid.net`), the in-product operator UI (after sign-in), and the Azure portal for their own subscription identity / billing. Any customer-facing copy must not assume the customer runs Docker, opens a terminal, or applies Terraform. Tooling like `apply-saas.ps1`, `archlucid try`, `dev up`, `docker compose`, the `.devcontainer/`, and `engineering/INSTALL_ORDER.md` is **internal ArchLucid contributor / operator** tooling. If a prompt seems to require a customer-side install step, **stop and ask the user** rather than inventing one.

---

## Prompt 1 — Consolidate onboarding to a single canonical route and wizard

**Why this matters.** Four overlapping onboarding routes (`/onboarding`, `/onboarding/start`, `/onboard`, `/getting-started`) with three distinct wizard implementations is the single largest cognitive load issue in the operator shell. Estimated weighted lift: **+2.36 points.**

```
Goal: consolidate operator onboarding into ONE canonical route and ONE
wizard implementation, eliminating cognitive load from overlapping
entry points.

Read first:
- archlucid-ui/src/app/(operator)/onboarding/page.tsx
- archlucid-ui/src/app/(operator)/onboarding/start/page.tsx
- archlucid-ui/src/app/(operator)/onboard/page.tsx
- archlucid-ui/src/app/(operator)/getting-started/page.tsx
- archlucid-ui/src/components/OnboardingWizardClient.tsx
- archlucid-ui/src/app/(operator)/onboard/OnboardWizardClient.tsx
- archlucid-ui/src/components/OperatorFirstRunWorkflowPanel.tsx
- archlucid-ui/src/components/HelpPanel.tsx  (references to onboarding/getting-started links)
- archlucid-ui/src/app/(operator)/page.tsx  (home page references)

Plan:
1. Keep /getting-started as the single canonical onboarding route (it
   already renders OperatorFirstRunWorkflowPanel, which is the
   checklist also on Home — reuse wins).
2. Add redirect pages at /onboarding, /onboarding/start, and /onboard
   that do `redirect("/getting-started")` (Next.js server redirect, not
   client). This preserves bookmarks and deep links without duplicating
   wizard implementations.
3. Preserve the post-registration flow: /onboarding/start currently
   loads trial-status and links to /runs/new with sampleRunId. Move
   that logic INTO /getting-started by detecting a registration-session
   cookie or query param (e.g. ?source=registration) and rendering the
   trial-status banner above the standard checklist.
4. Remove OnboardingWizardClient.tsx and OnboardWizardClient.tsx after
   the redirects are in place. Keep OperatorFirstRunWorkflowPanel as
   the single implementation.
5. Update HelpPanel.tsx — its footer links to both /onboarding and
   /getting-started; update to /getting-started only.
6. Update the OptInTour step 1 body if it references /onboarding.
7. Update any help-topics.ts route arrays that reference /onboarding.
8. Run existing tests. Add a redirect integration test for each old
   route.

Stop-and-ask boundaries:
- If the OnboardWizardClient (/onboard) has unique trial-flow logic
  that cannot cleanly merge, stop and list the differences before
  deleting it.
- If any external doc (docs/*.md) links to /onboarding or /onboard,
  list them for batch-update.

Do NOT create new onboarding routes. Do NOT add wizard steps. The goal
is consolidation, not feature addition.
```

---

## Prompt 2 — Upgrade Search and Ask pages to use design system components

**Why this matters.** `/search` and `/ask` are the only operator pages using raw HTML elements and inline `style={}` — no dark mode, no focus rings, no design system consistency. Estimated weighted lift: **+1.55 points.**

```
Goal: refactor the /search and /ask operator pages to use the same
design system components as the rest of the operator shell. No behavior
or API changes — pure visual consistency.

Read first:
- archlucid-ui/src/app/(operator)/search/page.tsx  (entire file)
- archlucid-ui/src/app/(operator)/ask/page.tsx  (entire file)
- archlucid-ui/src/components/ui/button.tsx  (Button variants)
- archlucid-ui/src/components/ui/input.tsx  (Input component)
- archlucid-ui/src/components/ui/card.tsx  (Card, CardContent, etc.)
- archlucid-ui/src/components/ui/label.tsx  (Label)
- archlucid-ui/src/components/OperatorApiProblem.tsx  (already used correctly)
- archlucid-ui/src/components/EmptyState.tsx  (for zero-results state)

For /search:
1. Replace raw <input> elements with <Input> from ui/input. Add <Label>
   wrappers.
2. Replace raw <button> with <Button> from ui/button.
3. Replace inline style objects with Tailwind classes matching the shell
   palette (neutral/teal).
4. Wrap search results in <Card> + <CardContent> instead of inline
   style divs.
5. Add a zero-results empty state using SEARCH_EMPTY (create a new
   preset in empty-state-presets.ts if needed, or inline text).
6. Add dark-mode classes matching the shell palette.

For /ask:
1. Same component replacements (Input, Button, Textarea, Card, Label).
2. The thread sidebar should use a Card with proper dark-mode borders.
3. Message bubbles: use Card with conditional bg- classes (user vs
   assistant) instead of inline background colors.
4. The grid layout is fine — keep it but replace inline styles with
   Tailwind grid classes.
5. The <details>/<summary> for compare inputs is fine semantically —
   add Tailwind classes for consistent styling.

For both:
- Ensure all inputs have visible focus-visible rings.
- Add ContextualHelp (?) icons next to the page headings (use
  helpKeys "semantic-search" and "ask-archlucid" — add entries in
  contextual-help-content.ts).
- Write or update component tests to verify rendering without errors.
- Run axe accessibility checks if the accessibility test suite includes
  a pattern for page-level components.

Do NOT change API call logic, state management, or URL structure.
Do NOT add new features. This is a styling and component consistency
refactor only.
```

---

## Prompt 3 — Add contextual help entries to high-traffic operator pages

**Why this matters.** `ContextualHelp` (?) popovers exist in only two places. Ten high-traffic pages have no inline help. Estimated weighted lift: **+1.20 points.**

```
Goal: expand the contextual-help-content.ts index and wire ContextualHelp
(?) icons to operator page headings where they are currently missing.

Read first:
- archlucid-ui/src/components/ContextualHelp.tsx
- archlucid-ui/src/lib/contextual-help-content.ts  (existing entries)
- archlucid-ui/src/lib/help-topics.ts  (HelpTopic summaries — reuse
  these for contextual help text where possible)

Add contextual-help entries (key → text → optional learnMoreUrl) for
at minimum:
1. alerts-inbox — "Alerts inbox shows deduplicated architecture-risk
   alerts. Ack, filter by severity, or configure rules via the Rules
   tab." → docs/ALERTS.md
2. governance-dashboard — "Governance dashboard tracks approval
   requests, promotions, and activations across runs." →
   docs/API_CONTRACTS.md
3. compare-runs — "Compare diffs two committed manifests. Enter base
   and target run IDs from the Runs list." →
   docs/COMPARISON_REPLAY.md
4. replay-run — "Replay re-validates a stored comparison. Verify mode
   detects drift since the original comparison." →
   docs/COMPARISON_REPLAY.md
5. architecture-graph — "Graph shows provenance or architecture view
   for a single run. Enter a run ID and choose a mode." →
   docs/KNOWLEDGE_GRAPH.md
6. audit-log — "Append-only audit trail. Use filters and keyset
   pagination to browse events. Export via API." →
   docs/AUDIT_COVERAGE_MATRIX.md
7. policy-packs — "Policy packs bundle rules and scope defaults. Assign
   them to workspaces to enforce governance." →
   docs/API_CONTRACTS.md
8. semantic-search — "Scoped to your workspace. Uses the same embedding
   index as Ask ArchLucid." → docs/operator-shell.md
9. ask-archlucid — "Multi-turn conversations about your architecture.
   First message needs a run ID; follow-ups reuse the thread." →
   docs/operator-shell.md
10. advisory-hub — "Advisory scans evaluate your architecture against
    configurable advisory rules." → docs/operator-shell.md

Then wire ContextualHelp into each page heading (same pattern as
/runs/new):
- /alerts → <ContextualHelp helpKey="alerts-inbox" />
- /governance/dashboard → <ContextualHelp helpKey="governance-dashboard" />
- /compare → <ContextualHelp helpKey="compare-runs" />
- /replay → <ContextualHelp helpKey="replay-run" />
- /graph → <ContextualHelp helpKey="architecture-graph" />
- /audit → <ContextualHelp helpKey="audit-log" />
- /policy-packs → <ContextualHelp helpKey="policy-packs" />
- /search → <ContextualHelp helpKey="semantic-search" />
  (can combine with Prompt 2 refactor)
- /ask → <ContextualHelp helpKey="ask-archlucid" />
  (can combine with Prompt 2 refactor)
- /advisory → <ContextualHelp helpKey="advisory-hub" />

Run existing ContextualHelp tests. Add a test in
contextual-help-content.test.ts that asserts every key used by a
ContextualHelp component exists in the index (prevent typo orphans).

Do NOT change ContextualHelp component behavior. Do NOT add help
entries for marketing pages.
```

---

## Prompt 4 — Add inline validation feedback to the new-run wizard steps

**Why this matters.** The 7-step wizard validates only on submit. Operators discover empty/invalid fields only after clicking Next or Submit. Estimated weighted lift: **+1.05 points.**

```
Goal: add inline validation to the new-run wizard so operators see
field-level feedback BEFORE advancing to the next step.

Read first:
- archlucid-ui/src/app/(operator)/runs/new/NewRunWizardClient.tsx
- archlucid-ui/src/app/(operator)/runs/new/NewRunWizardClient.test.tsx
- archlucid-ui/src/components/wizard/WizardStepPanel.tsx
- archlucid-ui/src/components/wizard/steps/ (all step files)
- archlucid-ui/src/components/ui/input.tsx
- archlucid-ui/src/components/ui/label.tsx

Plan:
1. For each wizard step, identify required fields. Add a per-step
   validate() function that returns an array of { field, message }
   errors (empty array = valid).
2. On "Next" click, run the step's validate(). If errors exist, render
   them inline below the relevant field using a consistent error
   message style (text-red-600, text-sm, role="alert"). Do NOT advance
   the step.
3. Clear field-level errors when the user modifies that field's value
   (onChange).
4. Step 1 (system identity): system name is required, must be non-empty.
5. UUID fields: validate format with a simple regex before submission.
6. Do NOT add async validation (no API calls during typing). This is
   purely client-side structural validation.
7. Add tests in NewRunWizardClient.test.tsx for:
   - "Next" blocked when required field is empty
   - Error message appears inline
   - Error clears on input change
   - Valid step advances normally

Do NOT change the API submission logic. Do NOT add new wizard steps.
Do NOT add server-side validation — that already exists.
```

---

## Prompt 5 — Expand glossary tooltip coverage to key operator pages

**Why this matters.** `GlossaryTooltip` exists but is applied to very few pages. Domain terms like "golden manifest" and "authority pipeline" appear as plain text on high-traffic pages. Estimated weighted lift: **+0.70 points.**

```
Goal: systematically apply GlossaryTooltip to domain terms on the
highest-traffic operator pages.

Read first:
- archlucid-ui/src/components/GlossaryTooltip.tsx
- archlucid-ui/src/lib/glossary-terms.ts  (existing term keys)
- archlucid-ui/src/app/(operator)/page.tsx  (home — uses plain text
  for "golden manifest", "artifacts", etc.)

Target pages (in priority order):
1. Home page (page.tsx) — wrap first occurrence of: "golden manifest",
   "artifacts", "run", "findings" with GlossaryTooltip using existing
   keys (golden_manifest, findings, run).
2. Run detail page (runs/[runId]/page.tsx or its client component) —
   wrap "golden manifest", "authority pipeline", "context snapshot",
   "decision trace".
3. /compare — wrap "manifest diff", "comparison record" (add new
   glossary entries if needed).
4. /governance/dashboard — wrap "approval request", "governance
   resolution".
5. /alerts — wrap "finding" on first use.

Rules:
- Only wrap the FIRST occurrence of each term per page (avoid visual
  clutter).
- If a glossary key does not exist in glossary-terms.ts, add it with
  a 1-2 sentence definition and an optional docLink to the relevant
  docs/library/*.md anchor.
- Do NOT wrap terms inside headings (h1-h3) — only body text.
- Do NOT add more than 5 GlossaryTooltips per page.
- Preserve pulseOnFirstSession={true} (default) for all new usages.
- Run GlossaryTooltip.test.tsx and GlossaryTerm.test.tsx after changes.

Do NOT change the GlossaryTooltip component. Do NOT add terms that are
self-explanatory to a two-year developer (e.g. "run ID", "API",
"JSON").
```

---

## Prompt 6 — Wire NEXT_PUBLIC_DOCS_BASE_URL default or fallback for HelpPanel doc links

**Why this matters.** `getDocHref()` returns `null` when the env var is unset, making every "Open documentation" link in the HelpPanel non-functional. This is the primary in-app help surface. Estimated weighted lift: **+0.60 points.**

```
Goal: make HelpPanel documentation links functional by default, even
when NEXT_PUBLIC_DOCS_BASE_URL is not set.

Read first:
- archlucid-ui/src/lib/help-topics.ts  (getDocHref function)
- archlucid-ui/src/components/HelpPanel.tsx  (how getDocHref is used)
- archlucid-ui/src/lib/contextual-help-content.ts  (toDocsBlobUrl — may
  already have a similar fallback)

Plan:
1. In getDocHref(), when the env var is unset or empty, fall back to
   the public GitHub blob URL:
   "https://github.com/ArchiForge/ArchiForge/blob/main/"
   This makes links functional for any operator with internet access.
2. The env var, when set, still takes priority (enterprise deployments
   may use private GitHub Enterprise or a docs site).
3. Update the HelpPanel — the current fallback text ("Set
   NEXT_PUBLIC_DOCS_BASE_URL…") should no longer appear when the
   default is in place. Keep it only if the link construction fails
   for another reason.
4. Apply the same fallback to toDocsBlobUrl in
   contextual-help-content.ts if it has the same pattern.
5. Update help-topics.test.ts (or create one) to test:
   - getDocHref returns a full URL when env var is set
   - getDocHref returns GitHub blob URL when env var is empty/unset
   - getDocHref handles trailing slashes correctly

Stop-and-ask: If the GitHub repo is private or the org/repo name
differs from "ArchiForge/ArchiForge", stop and ask for the correct
public URL before hardcoding a default.

Do NOT change the HelpPanel component layout. Do NOT change help topic
content.
```

---

## Prompt 7 — Add operator-visible tenant/workspace settings, scope switching, and role management UI

**Why this matters.** Operators cannot inspect or change their scope, view tenant settings, or manage user roles from the UI. Owner approved all three surfaces on 2026-04-25. Estimated weighted lift: **+0.80 points.**

```
Goal: add three operator-management surfaces — scope switching, tenant
settings, and role/authority management — based on owner decisions
recorded 2026-04-25.

Read first:
- archlucid-ui/src/components/AppShellClient.tsx  (header layout where
  scope switcher should live)
- archlucid-ui/src/app/(operator)/admin/support/page.tsx  (existing
  admin page pattern)
- archlucid-ui/src/lib/nav-config.ts  (where to add new nav entries)
- archlucid-ui/src/lib/nav-authority.ts  (authority rank model)
- archlucid-ui/src/components/OperatorNavAuthorityProvider.tsx
  (useNavCallerAuthorityRank)
- docs/library/API_CONTRACTS.md  (scope header contract)
- docs/library/GLOSSARY.md  (workspace, project definitions)

Surface 1 — Scope switcher (header bar element):
1. Add a compact dropdown or popover in the AppShellClient header bar
   (between breadcrumbs and the Help button) that displays the current
   workspace and project name.
2. Populate the dropdown from GET /v1/tenant/workspaces (or equivalent
   API — check for an existing endpoint). If no endpoint exists, create
   a stub page that reads from the proxy env vars and displays them
   read-only (with a note that the API endpoint is needed).
3. On selection, persist the chosen workspace/project IDs in a cookie
   or localStorage and pass them as proxy scope headers on subsequent
   API calls. Read archlucid-ui/src/app/api/proxy/ to understand how
   scope headers are currently forwarded.
4. Show the current scope in the header so operators always know their
   context (e.g. "Workspace: default / Project: my-pilot-project").
5. Gate scope switching behind ReadAuthority at minimum (any
   authenticated operator can switch their own view).

Surface 2 — Tenant settings page (/settings/tenant):
1. Create archlucid-ui/src/app/(operator)/settings/tenant/page.tsx.
2. Display non-obscure tenant settings that operators commonly need:
   - Tenant display name
   - Default workspace / project
   - Notification preferences (if applicable)
   - Trial status (read-only, from GET /v1/tenant/trial-status)
3. "Non-obscure" means: settings that affect the operator's daily
   experience. Do NOT expose database connection strings, internal
   feature flags, circuit breaker thresholds, or infrastructure config.
4. Each editable field should POST/PUT to the appropriate API endpoint.
   If no endpoint exists for a setting, render it read-only with a
   note "(API endpoint not yet available)".
5. Add a nav entry under the existing "Settings" group in nav-config.ts
   with requiredAuthority matching ExecuteAuthority or AdminAuthority
   (tenant settings are operator/admin scope).

Surface 3 — Role/authority management page (/admin/users):
1. Create archlucid-ui/src/app/(operator)/admin/users/page.tsx.
2. List current users/principals for the tenant (GET /v1/admin/users
   or equivalent). Show display name, email, authority rank.
3. Allow assigning authority rank (Reader / Operator / Admin) via a
   dropdown per user row (PUT /v1/admin/users/{userId}/authority or
   equivalent).
4. If the user-management API endpoints do not exist, build the page
   as a read-only list with a note "(User management API endpoints
   are required to enable editing)".
5. Gate this page behind AdminAuthority in nav-config.ts.
6. Add a nav entry under a new "Admin" group or the existing
   operate-governance group.

For all three surfaces:
- Use design system components (Card, Input, Button, Label, Select).
- Add dark-mode support.
- Add ContextualHelp (?) icons with appropriate help text.
- Write component tests for each new page.
- Follow the existing nav-config drift guard checklist when adding
  nav entries.

Stop-and-ask boundaries:
- If no API endpoints exist for workspace listing, tenant settings
  mutation, or user management, list what endpoints are needed and
  build the UI as read-only stubs. Do NOT invent API contracts.
- If the proxy scope-header forwarding model cannot support per-session
  scope switching (e.g. headers are baked into env vars at deploy
  time), stop and describe the constraint before building the switcher.

Do NOT change existing API endpoints. Do NOT add backend code. This
is a UI-only improvement that consumes existing or future APIs.
```

---

## Prompt 8 — Add in-app diagnostics/health dashboard for operators

**Why this matters.** Operators must use CLI or direct API calls to check system health. Owner approved an in-app panel visible to any authenticated user on 2026-04-25. Estimated weighted lift: **+0.74 points.**

```
Goal: add an in-app diagnostics/health dashboard page that gives any
authenticated operator a single-glance view of system health, without
requiring CLI access or external tooling.

Read first:
- docs/library/OBSERVABILITY.md  (custom metrics, health endpoints)
- docs/TROUBLESHOOTING.md  (health endpoint contract, circuit breakers)
- docs/library/OPERATOR_QUICKSTART.md  (doctor, support-bundle commands)
- archlucid-ui/src/app/(operator)/admin/support/page.tsx  (existing
  admin page — reference for layout pattern)
- archlucid-ui/src/lib/api.ts  (apiGet helper)
- archlucid-ui/src/components/OperatorApiProblem.tsx  (error rendering)
- archlucid-ui/src/components/EmptyState.tsx
- archlucid-ui/src/lib/nav-config.ts  (where to add the nav entry)

Create: archlucid-ui/src/app/(operator)/admin/health/page.tsx

Layout — three sections in a single scrollable page:

Section 1 — System health (top):
1. Call GET /api/proxy/health/ready on page load.
2. Render each health check entry as a row: check name, status
   (Healthy / Degraded / Unhealthy) with color-coded badge (green /
   amber / red), and duration.
3. Show the overall status as a large badge at the top: "Healthy",
   "Degraded", or "Unhealthy".
4. Show build identity (version, commitSha) from the same response or
   from GET /api/proxy/version.
5. Add a "Refresh" button that re-fetches.

Section 2 — Circuit breakers (middle):
1. Call GET /api/proxy/health (authenticated endpoint that includes
   circuit_breakers in the response).
2. Render each breaker gate as a row: name, state (Closed / Open /
   HalfOpen) with color-coded badge, and DurationOfBreakSeconds.
3. If the authenticated health endpoint returns 401/403, show a
   note: "Circuit breaker detail requires API authentication" and
   skip this section gracefully.

Section 3 — Operator task success rates (bottom):
1. Call GET /api/proxy/v1/diagnostics/operator-task-success-rates.
2. Render as a simple table: task name, count, last occurrence.
3. This gives operators visibility into onboarding funnel health
   without external tooling.

Additional:
- Add a ContextualHelp (?) icon next to the page heading with text:
  "System health shows API readiness checks, circuit breaker state,
  and onboarding milestone rates. For full metrics, connect
  Prometheus or Application Insights — see
  docs/library/OBSERVABILITY.md."
- Add the page to nav-config.ts under the operate-governance group
  (or a new "Admin" sub-section if one exists). Set tier to
  "essential" and do NOT set requiredAuthority (ReadAuthority floor
  per owner decision — any authenticated user can view).
- Add a loading.tsx skeleton.
- Use Card components for each section. Use the shell color palette.
- Add dark-mode support.
- Write component tests:
  - Renders health checks from mocked /health/ready response
  - Shows Unhealthy badge when any check is Unhealthy
  - Handles 401 on circuit breaker endpoint gracefully
  - Renders operator task success rates table
- Add a help-topics.ts entry for the health page.
- Add a contextual-help-content.ts entry with key "system-health".

Stop-and-ask boundaries:
- If GET /health/ready or GET /health do not return the expected JSON
  shape (entries[], circuit_breakers), list the actual shape and
  propose a mapping before building the UI.
- If GET /v1/diagnostics/operator-task-success-rates does not exist
  or returns an unexpected shape, build section 3 as a stub with
  "(Endpoint not yet available)".

Do NOT add new API endpoints. Do NOT modify health check registration.
This is a UI-only consumer of existing API surfaces.
```

---

## Prompt 9 — Add a "what's next" continuation funnel after Core Pilot checklist completion

**Why this matters.** After completing the 4-step Core Pilot checklist, operators see a static text block instead of guided next steps. Momentum is lost. Estimated weighted lift: **+0.58 points.**

```
Goal: replace the static AfterCorePilotChecklistHint with an
interactive "what's next" continuation panel that guides operators from
Core Pilot completion into Operate (analysis workloads) or Operate (governance and trust).

Read first:
- archlucid-ui/src/components/AfterCorePilotChecklistHint.tsx
- archlucid-ui/src/components/AfterCorePilotChecklistHint.test.tsx
- archlucid-ui/src/components/OperatorFirstRunWorkflowPanel.tsx
  (for the checklist UI pattern to reuse)
- archlucid-ui/src/lib/core-pilot-checklist-storage.ts

Plan:
1. Replace the static text in AfterCorePilotChecklistHint with a
   collapsible card (same Card/Collapsible pattern as the core
   checklist) titled "Ready for more?" or "Expand your pilot."
2. Include 3-4 optional next steps (NOT checkboxes — these are
   suggestions, not a required checklist):
   a. "Compare two runs" → /compare (link + 1-line description)
   b. "Explore the architecture graph" → /graph
   c. "Set up governance alerts" → /alerts?tab=rules
   d. "Review policy packs" → /policy-packs
3. Each suggestion should note which sidebar toggle to enable
   (e.g. "Requires Show more links in the sidebar").
4. Keep the existing hint text as an intro paragraph above the
   suggestions.
5. The card should be dismissible (persist dismissal in localStorage)
   so returning operators don't see it forever.
6. Update AfterCorePilotChecklistHint.test.tsx to cover:
   - Renders suggestions when all core steps are done
   - Dismiss persists to localStorage
   - Does not render when core steps are incomplete

Do NOT add new wizard steps. Do NOT change the Core Pilot checklist.
Do NOT auto-enable extended/advanced sidebar links — just tell the
operator what to toggle.
```
