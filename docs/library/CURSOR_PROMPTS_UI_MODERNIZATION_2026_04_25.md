> **Scope:** Twenty-three paste-ready Cursor prompts — the nine V1-actionable usability improvements from the 69.52% assessment, nine Linear-inspired UI modernization prompts that introduce new visual patterns without breaking existing infrastructure, and five follow-up prompts (19–23) that finish the inline-style migration, unify page headers, and add print support. Each prompt is self-contained.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.

> **Lineage:** Prompts 1–9 are the usability assessment prompts from [`CURSOR_PROMPTS_USABILITY_ASSESSMENT_2026_04_25_69_52.md`](../CURSOR_PROMPTS_USABILITY_ASSESSMENT_2026_04_25_69_52.md). Prompts 10–18 incorporate Linear-inspired workbench concepts — inspector panel, unified status pills, report readability, run detail flagship, dashboard evolution, visual density, and consistency — **calibrated to the actual codebase** (existing `AppShellClient`, `SidebarNav`, `EmptyState`, `RunStatusBadge`, `governance-status-badge-class`, shadcn/Radix/Tailwind stack, 48 operator pages). Prompts 19–23 are follow-up prompts generated from the Prompt 18 punch list — they finish the inline-style sweep, introduce `OperatorPageHeader`, bring parity to lagging surfaces, unify compare views, and add print support.

# Cursor prompts — V1 UI modernization (usability + Linear-inspired polish)

**How to use.** One prompt per session. Paste the whole block (between the triple backticks) into a fresh Cursor agent. Each prompt names its **stop-and-ask** boundaries — the assistant should not cross those without owner input. After each prompt completes, update [`docs/PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) accordingly.

**Recommended execution order.** Prompts 1–9 (usability fixes) first, in order. Then Prompts 10–18 (Linear-inspired polish) in order. Then Prompts 19–23 (cleanup and polish) in order. The usability prompts fix structural issues; the Linear prompts layer visual polish on top; the cleanup prompts finish migration and add infrastructure.

**Owner decisions recorded 2026-04-25.** All five pending questions from the assessment are resolved:
- Q1–Q3: Scope switching, non-obscure tenant settings UI, and role/authority management UI all **approved** for V1.
- Q4–Q5: In-app health/diagnostics panel **approved**, gated at **ReadAuthority** (visible to any authenticated user).

> **SaaS audience guard (read before running any prompt below).** ArchLucid is a **SaaS** product. Customers, evaluators, and sponsors never install Docker, SQL, .NET, Node, or Terraform. They only ever interact with the public website (`archlucid.com`), the in-product operator UI (after sign-in), and the Azure portal for their own subscription identity / billing. Any customer-facing copy must not assume the customer runs Docker, opens a terminal, or applies Terraform. Tooling like `apply-saas.ps1`, `archlucid try`, `dev up`, `docker compose`, the `.devcontainer/`, and `engineering/INSTALL_ORDER.md` is **internal ArchLucid contributor / operator** tooling. If a prompt seems to require a customer-side install step, **stop and ask the user** rather than inventing one.

---

# Part A — Usability fixes (Prompts 1–9)

These are the nine V1-actionable improvements from the 69.52% usability assessment, reproduced here for single-document convenience.

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

---

# Part B — Linear-inspired UI modernization (Prompts 10–18)

These prompts layer visual polish and new interaction patterns on top of the existing infrastructure. They are designed to move ArchLucid toward a Linear-like engineering workbench feel **without replacing** the existing app shell, sidebar, empty states, or routing.

**Design direction (applies to all Part B prompts):**
- **Primary inspiration:** Linear — dense, professional, keyboard-first, calm.
- **Restrained accent from:** Vercel (platform polish), GitBook (document readability).
- **What to avoid:** Consumer/retail colors, flashy gradients, dashboard widget clutter, generic admin template look.
- **Existing palette to preserve:** neutral/teal, dark mode via `.dark` class strategy, shadcn/Radix primitives.

---

## Prompt 10 — Create a unified StatusPill component

**Why this matters.** Status display is currently split across two separate implementations — `RunStatusBadge` (pipeline statuses using `Badge` variants) and `governance-status-badge-class.ts` (governance workflow statuses using raw Tailwind class strings applied to `Badge`). A unified `StatusPill` with a shared semantic color palette will make status communication consistent and scannable across all list and detail views.

```
Goal: create a unified StatusPill component that consolidates the two
existing status display patterns into a single reusable component with
a Linear-inspired visual style — restrained, professional, scannable.

Read first:
- archlucid-ui/src/components/RunStatusBadge.tsx  (existing pipeline
  status: Committed, Ready for commit, In pipeline, Starting)
- archlucid-ui/src/app/(operator)/governance/dashboard/governance-status-badge-class.ts
  (existing governance status: Submitted, Approved, Rejected,
  Promoted, Activated, Draft)
- archlucid-ui/src/app/(operator)/governance/dashboard/governance-status-badge-class.test.ts
- archlucid-ui/src/components/ui/badge.tsx  (underlying Badge with
  CVA variants — default, secondary, destructive, outline)

Create: archlucid-ui/src/components/StatusPill.tsx

Design:
1. Build StatusPill as a thin wrapper around Badge (do NOT duplicate
   Badge internals). Accept a `status` string prop and an optional
   `domain` prop ("pipeline" | "governance" | "health" | "general")
   to namespace status semantics.
2. Map each known status string to a visual treatment:

   Pipeline domain (existing RunStatusBadge values):
   - "Committed" → solid teal/green (success, done)
   - "Ready for commit" → amber outline (action needed)
   - "In pipeline" → blue outline (in progress)
   - "Starting" → neutral outline (pending)

   Governance domain (existing governance-status-badge-class values):
   - "Submitted" → blue solid (in review)
   - "Approved" → emerald solid (accepted)
   - "Rejected" → red solid (blocked)
   - "Promoted" → violet solid (elevated)
   - "Activated" → teal solid (live)
   - "Draft" → neutral muted (not yet submitted)

   Health domain (for the health dashboard in Prompt 8):
   - "Healthy" → emerald
   - "Degraded" → amber
   - "Unhealthy" → red
   - "Closed" → emerald (circuit breaker nominal)
   - "Open" → red (circuit breaker tripped)
   - "HalfOpen" → amber (circuit breaker recovering)

   General fallback:
   - Unknown strings → neutral outline with the raw text

3. Visual style should be pill-shaped (rounded-full), compact, with
   enough padding to be readable in dense lists. Text should be
   uppercase or small-caps for scannability. Colors should be
   professional — not loud. Good contrast in both light and dark mode.
4. Add aria-label="Status: {status}" for screen readers.

Integration:
5. Refactor RunStatusBadge to use StatusPill internally (keep
   RunStatusBadge as a convenience wrapper that derives the label
   from RunSummary flags, then passes it to StatusPill with
   domain="pipeline"). This preserves all existing imports.
6. Refactor governance-status-badge-class.ts consumers: find where
   governanceStatusBadgeClass() is called, replace with <StatusPill
   status={status} domain="governance" />. Keep the old function
   file for one release cycle with a deprecation comment.
7. Do NOT delete RunStatusBadge.tsx — keep it as a domain adapter.

Tests:
8. Create StatusPill.test.tsx:
   - Renders correct text for each pipeline status
   - Renders correct text for each governance status
   - Falls back gracefully for unknown status strings
   - Applies aria-label
   - Renders in dark mode without contrast issues (axe)
9. Update RunStatusBadge tests to verify delegation to StatusPill.
10. Run governance-status-badge-class.test.ts — existing tests
    should still pass until consumers are migrated.

Do NOT change backend behavior. Do NOT change stored status values.
Do NOT remove RunStatusBadge — refactor it to delegate.
```

---

## Prompt 11 — Redesign Run Detail as the flagship screen

**Why this matters.** The Run Detail page (`/runs/[runId]`) is the product's centerpiece — where pipeline results, provenance, explanations, artifacts, and actions converge. It currently uses a mix of inline styles, raw `<p>` tags, and unstyled `<ul>`/`<li>` for the provenance chain. Elevating this screen to a polished, Linear-like detail view establishes the reference pattern for all other detail screens.

```
Goal: redesign the Run Detail page as the flagship detail screen for
ArchLucid. This screen should become the visual reference pattern for
all other detail views.

Read first:
- archlucid-ui/src/app/(operator)/runs/[runId]/page.tsx  (entire file
  — currently ~505 lines, server-rendered with multiple sections)
- archlucid-ui/src/components/RunDetailSectionNav.tsx  (existing
  in-page section navigation — horizontal tab strip for Run, Timeline,
  Forensics, Chain, Manifest, Explanation, Artifacts, Actions)
- archlucid-ui/src/components/AuthorityPipelineTimeline.tsx
- archlucid-ui/src/components/RunProgressTracker.tsx
- archlucid-ui/src/components/CollapsibleSection.tsx
- archlucid-ui/src/components/RunExplanationSection.tsx
- archlucid-ui/src/components/ArtifactListTable.tsx
- archlucid-ui/src/components/CommitRunButton.tsx
- archlucid-ui/src/components/PostCommitAdvancedAnalysisHint.tsx
- archlucid-ui/src/components/StatusPill.tsx  (created in Prompt 10)
- archlucid-ui/src/components/ui/card.tsx
- archlucid-ui/src/components/ui/badge.tsx

Plan — Header area:
1. Replace the plain <h2>Run detail</h2> + breadcrumb links with a
   structured header block:
   - Run title or description (large, semibold)
   - StatusPill showing pipeline status (Committed / Ready for commit /
     In pipeline / Starting), derived from the same RunSummary flags
     RunStatusBadge uses
   - Metadata row: Run ID (monospace, copy-to-clipboard), Project,
     Created date — using text-sm text-neutral-600 dark:text-neutral-400
   - Primary actions (Commit, Compare, Replay) as Button group in the
     header, not buried at the bottom of the page
2. Keep the RunDetailSectionNav as a sticky sub-header for in-page
   navigation (the horizontal tab strip is already a good pattern).

Plan — Provenance chain section:
3. Replace the raw <ul>/<li> list with a Card-based vertical timeline
   or structured metadata grid. Each chain item (Context Snapshot,
   Graph Snapshot, Findings Snapshot, Golden Manifest, Decision Trace,
   Artifact Bundle) should show:
   - Label (with GlossaryTooltip where already present)
   - ID value (monospace, truncated with copy button) or "—" if null
   - Link to detail page where one exists (e.g. /manifests/{id})

Plan — Sections:
4. Wrap each existing section in a Card or a consistent
   section-with-heading pattern. Replace inline style={{ }} objects
   with Tailwind classes throughout. The page currently has ~15
   inline style objects; replace all of them.
5. The Manifest Summary section should display its data in a metadata
   grid (label-value pairs) rather than <p><strong>Label:</strong>
   value</p> blocks.
6. The Actions section should use Button components with clear primary
   vs secondary hierarchy. Move the primary action (Commit) into the
   header (step 1). Keep secondary actions (Compare, Replay, Download
   bundle, Download export, Download traceability bundle) in the
   Actions section with Button styling.

Plan — Visual hierarchy:
7. Use consistent section spacing (gap-6 or space-y-6 between major
   sections).
8. Use text-lg font-semibold for section headings (h3) with a bottom
   border or subtle divider.
9. Ensure the page reads well at max-w-4xl with comfortable margins.

Hard constraints:
- Preserve ALL existing data loading, API calls, error handling,
  and coercion guards. Do NOT change getRunDetail, getManifestSummary,
  listArtifacts, getRunExplanationSummary, or any API function.
- Preserve RunDetailSectionNav section IDs and scroll-mt anchors.
- Preserve ContextualHelp placements.
- Preserve GlossaryTooltip placements.
- Preserve RunProgressTracker, EmailRunToSponsorBanner,
  PostCommitAdvancedAnalysisHint, BeforeAfterDeltaPanel, and all
  conditional rendering logic.
- Do NOT change URLs or route structure.

Tests:
- Run existing tests for run detail components.
- Verify the page renders without errors in both light and dark mode.
- Verify accessibility (skip link, aria labels, heading hierarchy).

After implementation:
- Summarize changed files.
- Note which inline style objects were replaced.
- Describe this as the "flagship pattern" — future detail screens
  (findings, manifests, governance approval detail) should follow
  the same card/header/metadata grid structure.
```

---

## Prompt 12 — Add a right-side inspector panel pattern

**Why this matters.** The Scan → Select → Inspect → Act workflow is a core Linear pattern. Currently, clicking a run in the list navigates to a full page load. An inspector panel lets operators preview run details, governance approvals, or alert summaries without losing their place in a list — faster triage, less navigation friction.

```
Goal: introduce a reusable InspectorPanel component and wire it to the
Runs list as the first integration point. The panel shows a selected
item's key details in a right-side slide-out or inline panel without
navigating away from the list.

Read first:
- archlucid-ui/src/components/AppShellClient.tsx  (current layout:
  sidebar 15.5rem | main content flex-1 — the inspector must fit
  within the main content area, not alter the shell)
- archlucid-ui/src/app/(operator)/runs/page.tsx  (server page)
- archlucid-ui/src/app/(operator)/runs/RunsListClient.tsx  (client
  component that renders the run list table/cards)
- archlucid-ui/src/components/RunStatusBadge.tsx
- archlucid-ui/src/components/StatusPill.tsx  (if created in Prompt 10)
- archlucid-ui/src/components/ui/card.tsx
- archlucid-ui/src/components/HelpPanel.tsx  (existing right-side
  slide-out — reference for the panel pattern and animation)

Create: archlucid-ui/src/components/InspectorPanel.tsx

Component design:
1. Reusable shell: accepts children, a title, an onClose callback,
   and an optional width prop (default 24rem).
2. Renders as a right-side panel within the main content area — NOT
   a full-page overlay, NOT a dialog. It should split the content
   area: list on left, inspector on right. On screens narrower than
   lg (1024px), it should overlay as a slide-out sheet (similar to
   HelpPanel) or simply be hidden with a "View details" link to the
   full page.
3. Transition: slide in from right with a brief ease-out animation.
4. Close button (X) in the top-right corner.
5. Keyboard: Escape closes the panel. Focus trap is NOT needed (this
   is not a modal).
6. The panel should have a clean card-like appearance: border-left,
   subtle shadow, bg-white dark:bg-neutral-900.

Runs list integration:
7. In RunsListClient, make each run row clickable to open the
   inspector (in addition to the existing link to /runs/{runId}).
   Use a single-click to open inspector; keep the existing link
   behavior for users who want the full page (e.g. Cmd+Click or
   an explicit "Open" button).
8. When a run is selected, the InspectorPanel shows:
   - Run description or "Untitled run" as title
   - StatusPill with pipeline status
   - Metadata: Run ID (monospace, copyable), Project, Created date
   - Provenance chain summary (which snapshots exist: checkmarks or
     dashes for context/graph/findings/manifest)
   - Primary action: "Open run detail" link to /runs/{runId}
   - Secondary actions: "Compare" link, "Replay" link (same as run
     detail page links)
9. Empty state when nothing is selected: "Select a run to preview
   details here."

Hard constraints:
- Do NOT change the AppShellClient layout. The inspector lives INSIDE
  the main content area, not as a shell-level element.
- Do NOT remove the existing full-page navigation to /runs/{runId}.
  The inspector supplements it; it does not replace it.
- Do NOT introduce a global state management library. Use React
  state (useState) at the list page level to track the selected run.
- Do NOT fetch additional API data for the inspector — use only the
  RunSummary fields already available in the list response.
- Do NOT introduce large dependencies.

Tests:
- InspectorPanel.test.tsx: renders children, calls onClose on Escape,
  renders empty state when no children.
- RunsListClient integration: clicking a row opens the inspector,
  clicking X closes it, Escape closes it.

After implementation:
- Summarize changed files.
- Note that this is a reusable pattern — future integrations can add
  InspectorPanel to /governance/dashboard, /alerts, /advisory, and
  /policy-packs lists by following the same pattern.
```

---

## Prompt 13 — Polish the Runs list as a work queue

**Why this matters.** The Runs list currently displays database rows. A work-queue framing — "what needs attention?" — makes the list more actionable and aligns with Linear's task-oriented list design.

```
Goal: improve the Runs list visual presentation so it feels like an
architecture work queue rather than a generic CRUD table. No new data
sources — improve the presentation of data already in RunSummary.

Read first:
- archlucid-ui/src/app/(operator)/runs/page.tsx
- archlucid-ui/src/app/(operator)/runs/RunsListClient.tsx
- archlucid-ui/src/components/RunStatusBadge.tsx
- archlucid-ui/src/components/StatusPill.tsx  (if created in Prompt 10)
- archlucid-ui/src/components/InspectorPanel.tsx  (if created in
  Prompt 12)
- archlucid-ui/src/types/authority.ts  (RunSummary type — check which
  fields are actually available)
- archlucid-ui/src/components/ui/card.tsx

Changes to RunsListClient:
1. Each run row should show (using ONLY fields available on
   RunSummary — do not invent fields):
   - Description (or "Untitled run") as the primary text — semibold,
     text-sm
   - StatusPill for pipeline status — inline after description
   - Run ID — monospace, text-xs, muted
   - Project — text-xs, muted, if available
   - Created date — relative time (e.g. "2 hours ago") with full
     date on hover (title attribute). Use a simple relative-time
     utility — do NOT add date-fns or moment; write a small
     formatRelativeTime helper.
   - Provenance progress indicators: small dots or checkmarks for
     each pipeline stage (context → graph → findings → manifest)
     derived from has* boolean flags

2. Group or visually separate runs by status:
   - "Needs attention" group: runs where hasGoldenManifest is false
     but hasFindingsSnapshot is true (Ready for commit — action
     needed)
   - "In progress" group: runs still in pipeline
   - "Committed" group: completed runs
   Only if RunSummary has enough fields to support this grouping.
   If grouping would require additional API calls, skip it and use
   StatusPill sorting instead.

3. Visual style:
   - Dense row spacing (py-2 px-3) — Linear-like compact rows
   - Hover background (hover:bg-neutral-50 dark:hover:bg-neutral-800)
   - Active/selected row highlight (for inspector integration)
   - No heavy borders between rows — use subtle dividers
     (divide-y divide-neutral-100 dark:divide-neutral-800)
   - Keyboard focus visible on each row

4. Keep the existing pagination controls. Keep the existing
   BeforeAfterDeltaPanel and RunsIndexBeforeAfterPanel placements.

Hard constraints:
- Do NOT change API calls or add new endpoints.
- Do NOT remove existing functionality (pagination, project filter,
  links to run detail, links to new run wizard).
- Do NOT invent RunSummary fields that do not exist.
- Preserve existing empty state (RUNS_EMPTY).

Tests:
- Run existing RunsListClient tests.
- Verify row rendering with various status combinations.

After implementation:
- Summarize changed files.
- Note which RunSummary fields were used vs which were unavailable.
```

---

## Prompt 14 — Polish the governance dashboard as a work queue

**Why this matters.** The governance dashboard (`/governance/dashboard`) tracks approval requests, promotions, and activations across runs. Applying the same work-queue visual treatment as the Runs list creates consistency and makes governance triage faster.

```
Goal: improve the governance dashboard visual presentation to match the
work-queue style established in the Runs list (Prompt 13), using
StatusPill and consistent Card/list patterns.

Read first:
- archlucid-ui/src/app/(operator)/governance/dashboard/page.tsx
- archlucid-ui/src/app/(operator)/governance/dashboard/governance-status-badge-class.ts
- archlucid-ui/src/components/StatusPill.tsx  (if created in Prompt 10)
- archlucid-ui/src/components/InspectorPanel.tsx  (if created in
  Prompt 12)
- archlucid-ui/src/components/ui/card.tsx

Plan:
1. Replace governanceStatusBadgeClass() inline Tailwind strings with
   <StatusPill status={...} domain="governance" /> everywhere on this
   page.
2. Structure approval request rows consistently:
   - Request title or description as primary text
   - StatusPill (Submitted / Approved / Rejected / Promoted /
     Activated / Draft)
   - Run ID reference (link to /runs/{runId})
   - Timestamp (relative time)
   - Reviewer/owner if available in the data
3. If the page has table-based rendering, keep the table but apply
   consistent row spacing, hover states, and divider styles matching
   the Runs list pattern.
4. If the inspector panel exists, wire it to show approval request
   details on row click (same Scan → Select → Inspect → Act pattern).
5. Add StatusPill to any other governance sub-views on this page
   (promotions, activations) that currently use raw badge classes.

Hard constraints:
- Do NOT change governance API calls or workflow logic.
- Do NOT remove existing actions (approve, reject, promote, activate).
- Preserve existing governance-status-badge-class.test.ts coverage
  until the old function is fully replaced.

Tests:
- Run existing governance dashboard tests.
- Verify StatusPill renders for each governance status.

After implementation, summarize changed files.
```

---

## Prompt 15 — Evolve the operator home toward a command center

**Why this matters.** The current operator home is structured as a checklist + link sections. For operators past the initial Pilot, a command-center dashboard that surfaces actionable items from across the product — runs needing attention, governance items, alerts, recent advisory scans — provides more value. This builds on the existing home without replacing it.

```
Goal: add a "Command center" section to the operator home page that
surfaces actionable items for operators who have completed the Core
Pilot. This section appears BELOW the existing checklist and
AfterCorePilotChecklistHint — it does not replace them.

Read first:
- archlucid-ui/src/app/(operator)/page.tsx  (current home layout)
- archlucid-ui/src/components/OperatorFirstRunWorkflowPanel.tsx
- archlucid-ui/src/components/AfterCorePilotChecklistHint.tsx
- archlucid-ui/src/components/PilotOutcomeCard.tsx
- archlucid-ui/src/components/OperatorTaskSuccessTile.tsx
- archlucid-ui/src/components/StatusPill.tsx  (if created in Prompt 10)
- archlucid-ui/src/components/EmptyState.tsx
- archlucid-ui/src/lib/empty-state-presets.ts
- archlucid-ui/src/lib/core-pilot-checklist-storage.ts  (to check if
  pilot is complete)
- archlucid-ui/src/lib/api.ts  (check which list endpoints are
  available — e.g. listRunsByProjectPaged, any alerts or governance
  list endpoints)

Create: archlucid-ui/src/components/operator-home/CommandCenterSection.tsx

Design:
1. Show this section ONLY when the Core Pilot checklist is complete
   (all 4 steps done per core-pilot-checklist-storage). For new
   operators, the checklist and welcome banner are sufficient.
2. The section should be a grid of 2-3 summary cards (2 columns on
   lg, 1 column on smaller screens). Each card is a Card component:

   Card A — "Runs needing attention"
   - Fetch the first page of runs (GET /api/proxy/v1/runs?projectId=
     default&page=1&pageSize=5).
   - Filter client-side for runs where hasFindingsSnapshot === true
     AND hasGoldenManifest === false (Ready for commit).
   - Show count + list of up to 3 run descriptions with StatusPill.
   - Link to /runs?projectId=default.
   - Empty state: "All runs are committed. Create a new run or wait
     for pipeline results."

   Card B — "Recent activity"
   - Reuse BeforeAfterDeltaPanel variant="compact" (or create a new
     compact variant if it does not exist) showing the latest delta.
   - Link to the full activity view if one exists.

   Card C — "System health" (if Prompt 8 is complete)
   - Fetch GET /api/proxy/health/ready.
   - Show overall status as a StatusPill (Healthy / Degraded /
     Unhealthy).
   - Link to /admin/health.
   - If the health endpoint is not available, show a graceful empty
     state: "Health dashboard not configured yet."

3. If a fetch fails for any card, show a graceful error state
   inside the card (not a full-page error). Use OperatorApiProblem
   at a compact size.

Visual style:
4. Section heading: "Command center" with text-base font-semibold.
5. Cards should be visually lightweight — border, subtle bg,
   compact padding. Not heavy shadow or elevation.
6. Dense information — this is for operators who already know the
   product, not onboarding users.

Hard constraints:
- Do NOT remove or reorder existing home page components
  (WelcomeBanner, PilotOutcomeCard, OperatorTaskSuccessTile,
  BeforeAfterDeltaPanel, OperatorFirstRunWorkflowPanel,
  AfterCorePilotChecklistHint, Operate · analysis home section, Operate ·
  governance home section — see `archlucid-ui/src/app/(operator)/page.tsx`
  for the two optional-maturity h3 blocks).
- Insert the CommandCenterSection AFTER AfterCorePilotChecklistHint
  and BEFORE the Operate · analysis home section (first optional-maturity
  h3 in `(operator)/page.tsx`).
- Do NOT add API endpoints. Use ONLY existing endpoints.
- Do NOT make the command center the primary home experience — the
  checklist remains the primary for new operators.
- If runs list API returns an error, degrade gracefully (show error
  inside the card, not a page-level error).

Tests:
- CommandCenterSection.test.tsx: renders nothing when checklist
  is incomplete; renders cards when complete; handles API errors
  gracefully; renders StatusPill in runs card.

After implementation, summarize changed files and describe the
conditional rendering logic.
```

---

## Prompt 16 — Improve document/report view readability

**Why this matters.** Architecture reports, explanations, and generated outputs are the deliverables operators share with sponsors and auditors. GitBook-like readability — comfortable line length, clear headings, section spacing — makes these artifacts credible enterprise documents rather than raw API output.

```
Goal: improve the presentation of report-like and document-like views
so they read as credible enterprise artifacts. Target: GitBook-inspired
readability without introducing a documentation framework.

Read first:
- archlucid-ui/src/app/(operator)/value-report/page.tsx  (sponsor
  value report — check current rendering)
- archlucid-ui/src/components/RunExplanationSection.tsx  (aggregate
  explanation rendering)
- archlucid-ui/src/components/RunFindingExplainabilityTable.tsx
- archlucid-ui/src/app/(operator)/runs/[runId]/findings/[findingId]/page.tsx
  (finding detail with explanation)
- archlucid-ui/src/components/FindingExplainPanel.tsx
- archlucid-ui/src/app/(operator)/digests/page.tsx  (digest view)
- archlucid-ui/src/app/(operator)/advisory/page.tsx  (advisory output)
- archlucid-ui/src/components/ui/card.tsx

Create: archlucid-ui/src/components/DocumentLayout.tsx

Component design:
1. A reusable wrapper that applies document-friendly typography and
   spacing to its children:
   - max-w-3xl (comfortable reading width — ~65-75 characters)
   - prose-like typography: text-base leading-relaxed for body,
     text-lg font-semibold for h3, text-xl font-bold for h2
   - Generous spacing between sections (space-y-6)
   - Muted metadata text (text-sm text-neutral-500)
   - Code blocks with bg-neutral-100 dark:bg-neutral-800 rounded
     padding
   - Table styling: border-collapse, alternating row backgrounds,
     compact cell padding
2. Optional right-side table of contents (sticky, text-xs, visible
   only on xl+ screens) that lists section headings with anchor
   links. Only render TOC if the content has 3+ sections.
3. Print-friendly: @media print styles that hide nav and expand
   content to full width.

Apply DocumentLayout to:
4. RunExplanationSection — wrap the aggregate explanation content
   in DocumentLayout for readable long-form text.
5. FindingExplainPanel — the per-finding explanation with evidence
   chain. Wrap the narrative portion in DocumentLayout.
6. Value report page — if it renders generated report content, wrap
   it in DocumentLayout.
7. Digest content — if digests render long-form content, wrap in
   DocumentLayout.

Hard constraints:
- Do NOT change report generation logic or API contracts.
- Do NOT fake missing report content.
- Do NOT alter the data loading or error handling.
- Preserve all existing ContextualHelp and GlossaryTooltip placements.
- DocumentLayout should be a pure presentation wrapper — no data
  fetching, no state management.

Tests:
- DocumentLayout.test.tsx: renders children with correct max-width
  class; renders TOC when 3+ sections present; omits TOC with fewer
  sections; applies print styles.

After implementation, summarize changed files and list which views
now use DocumentLayout.
```

---

## Prompt 17 — Replace remaining inline styles with Tailwind

**Why this matters.** The Run Detail page and several other operator pages still contain `style={{ }}` objects — approximately 15 on Run Detail alone, plus scattered instances on other pages. These bypass the design system, break dark mode consistency, and prevent the Linear-like visual uniformity achieved in earlier prompts.

```
Goal: systematically replace inline style={{ }} objects across operator
pages with equivalent Tailwind classes. This is a mechanical cleanup —
no behavior changes.

Read first:
- archlucid-ui/src/app/(operator)/runs/[runId]/page.tsx  (highest
  concentration: ~15 inline style objects for margins, font sizes,
  colors, padding, flex layouts)
- archlucid-ui/src/components/OperatorShellMessage.tsx  (check for
  any inline styles in callout components)
- archlucid-ui/src/app/(operator)/runs/[runId]/provenance/page.tsx
- archlucid-ui/src/app/(operator)/runs/[runId]/findings/[findingId]/page.tsx
- archlucid-ui/src/app/(operator)/runs/[runId]/findings/[findingId]/inspect/page.tsx

Search strategy:
1. Search for `style={{` and `style={` across all files in
   archlucid-ui/src/app/(operator)/ and archlucid-ui/src/components/.
2. For each instance, replace with the Tailwind equivalent:
   - margin: "12px 0 0" → mt-3
   - fontSize: 14 → text-sm
   - fontSize: "0.875rem" → text-sm
   - color: "#64748b" → text-neutral-500 (check closest match)
   - color: "#475569" → text-neutral-600
   - display: "flex" → flex
   - gap: 16 → gap-4
   - flexWrap: "wrap" → flex-wrap
   - paddingLeft: 20 → pl-5
   - lineHeight: 1.6 → leading-relaxed
   - maxWidth: 720 → max-w-3xl
3. For hardcoded color values (e.g. color: "#64748b"), map to the
   nearest Tailwind neutral shade AND add the dark: variant:
   - #64748b (slate-500) → text-neutral-500 dark:text-neutral-400
   - #475569 (slate-600) → text-neutral-600 dark:text-neutral-400

Rules:
- Do NOT change any conditional rendering logic.
- Do NOT change component props or data flow.
- Do NOT change text content.
- Do NOT change element types (p stays p, ul stays ul).
- If an inline style contains a value with no obvious Tailwind
  equivalent (e.g. a custom percentage width), use an arbitrary
  value class (e.g. w-[72%]) or leave a TODO comment.
- Verify dark mode works for every replaced style.

After implementation:
- Run the build. Fix any Tailwind class typos.
- Run existing tests — no behavior should change.
- List every file modified and the count of inline styles replaced.
- Report any remaining inline styles that could not be cleanly
  replaced.
```

---

## Prompt 18 — UI consistency pass and final punch list

**Why this matters.** After the usability fixes (Prompts 1–9) and Linear-inspired polish (Prompts 10–17), a dedicated consistency sweep catches stragglers — pages that still use old patterns, inconsistent terminology, missed StatusPill opportunities, or lingering visual roughness.

```
Goal: perform a consistency sweep across the operator UI. Find and fix
low-risk visual inconsistencies. Then produce a final punch list of
remaining issues for future work.

Read first (scan each for consistency gaps, do not read in full):
- archlucid-ui/src/app/(operator)/ — scan all page.tsx files for:
  - Pages still outside the consistent heading pattern (plain <h2>
    instead of structured header with metadata)
  - Pages still using raw Badge instead of StatusPill for status
    display
  - Pages still using inline style={{ }} (any survivors from
    Prompt 17)
  - Empty states that still say generic text ("No records",
    "No data", "Nothing to show") instead of domain-specific
    EmptyState presets
  - Pages missing ContextualHelp (?) icons that should have them
  - Inconsistent button styles (raw <button> instead of <Button>)
  - Inconsistent link styles (missing underline or teal color)
- archlucid-ui/src/components/ — scan for:
  - Components that define their own status colors instead of
    using StatusPill
  - Components with hardcoded color values instead of Tailwind
    classes

Fix only issues where:
- The fix is mechanical (swap component, replace class, update text)
- The fix does not change behavior
- The fix does not require new API calls
- The fix affects fewer than ~10 lines per file

Product vocabulary to prefer (update labels where they appear in UI
text, NOT in API field names or route paths):
- "Architecture run" instead of just "run" where context is ambiguous
- "Golden manifest" instead of just "manifest" in user-facing text
- "Provenance chain" instead of "authority chain" in headings
  (keep "authority" in technical contexts where it is the API term)
- "Finding" instead of "result" or "issue" where applicable
- "Policy pack" instead of "rule set" in user-facing text (keep
  "ruleSetId" in technical/API contexts)

Do NOT:
- Change route paths or URLs
- Change API field names
- Change component prop names
- Rename files
- Perform broad rewrites
- Remove any functionality
- Change backend behavior

After the fixes, produce a punch list — a markdown section at the end
of your response with:

1. A score from 1 to 100 for current UI product credibility after all
   18 prompts.
2. The top 10 remaining UX issues (with file paths where applicable).
3. The top 10 highest-ROI next improvements (beyond what these 18
   prompts addressed).
4. Any screens that still feel weak.
5. Any technical debt created during the modernization.
6. Recommended follow-up prompts for the next iteration.

Be direct. Do not flatter the project.
```

---

# Part C — Remaining inline-style cleanup, page header unification, and polish (Prompts 19–23)

These five prompts finish the inline-style migration, introduce a reusable page header, bring advisory schedules to parity, unify the compare views, and add a print stylesheet. Run after Prompts 1–18.

---

## Prompt 19 — Replace remaining inline styles across operator pages and shared components

**Why this matters.** After Prompts 16–17, approximately 340 `style={{ }}` instances remain across 14 operator page files and 28 component files. These bypass the design system, break dark mode, and prevent visual consistency. This prompt is a continuation of Prompt 17 — same rules, broader scope.

```
Goal: systematically replace every remaining style={{ }} object across
operator pages and shared components with equivalent Tailwind classes.
Mechanical cleanup — no behavior changes.

Read first (sorted by inline-style count, highest first):
Pages:
- archlucid-ui/src/app/(operator)/replay/page.tsx          (~43 instances)
- archlucid-ui/src/app/(operator)/policy-packs/page.tsx     (~45 instances)
- archlucid-ui/src/app/(operator)/evolution-review/page.tsx  (~36 instances)
- archlucid-ui/src/app/(operator)/audit/page.tsx             (~33 instances)
- archlucid-ui/src/app/(operator)/governance-resolution/page.tsx (~28 instances)
- archlucid-ui/src/app/(operator)/graph/page.tsx             (~18 instances)
- archlucid-ui/src/app/(operator)/product-learning/page.tsx  (~61 instances)
- archlucid-ui/src/app/(operator)/planning/page.tsx          (~13 instances)
- archlucid-ui/src/app/(operator)/planning/plans/[planId]/page.tsx (~27 instances)
- archlucid-ui/src/app/(operator)/manifests/[manifestId]/page.tsx  (~15 instances)
- archlucid-ui/src/app/(operator)/recommendation-learning/page.tsx (~6 instances)

Shared components:
- archlucid-ui/src/components/alerts/AlertSimulationContent.tsx     (~83 instances)
- archlucid-ui/src/components/alerts/AlertTuningContent.tsx          (~38 instances)
- archlucid-ui/src/components/alerts/CompositeAlertRulesContent.tsx  (~29 instances)
- archlucid-ui/src/components/alerts/AlertRoutingContent.tsx         (~18 instances)
- archlucid-ui/src/components/alerts/AlertRulesContent.tsx           (~15 instances)
- archlucid-ui/src/components/ProvenanceGraphDiagram.tsx             (~19 instances)
- archlucid-ui/src/components/PolicyPackDiffView.tsx                 (~19 instances)
- archlucid-ui/src/components/ArtifactListTable.tsx                  (~18 instances)
- archlucid-ui/src/components/RunAgentForensicsSection.tsx           (~21 instances)
- archlucid-ui/src/components/evolution/SimulationRunDiffCard.tsx    (~34 instances)
- archlucid-ui/src/components/digests/DigestSubscriptionsContent.tsx (~17 instances)
- archlucid-ui/src/components/planning/PlanningSummarySection.tsx    (~9 instances)
- archlucid-ui/src/components/planning/PlanningPlansTable.tsx        (~5 instances)
- archlucid-ui/src/components/planning/PlanningThemesTable.tsx       (~6 instances)
- archlucid-ui/src/components/planning/PlanningExportReadinessNote.tsx (~3 instances)
- archlucid-ui/src/components/ArtifactReviewContent.tsx              (~7 instances)
- archlucid-ui/src/components/SectionCard.tsx                        (~2 instances)
- archlucid-ui/src/components/OperatorApiProblem.tsx                 (~2 instances + CSSProperties const)
- archlucid-ui/src/components/AuthorityPipelineTimeline.tsx          (~7 instances)
- archlucid-ui/src/components/ComplianceDriftChart.tsx               (~2 instances)
- archlucid-ui/src/components/explanation/CitationChips.tsx           (~3 instances)
- archlucid-ui/src/components/GraphViewer.tsx                         (~6 instances)
- archlucid-ui/src/components/AuthPanel.tsx                           (~2 instances)
- archlucid-ui/src/components/ColorModeToggle.tsx                     (~1 instance)
- archlucid-ui/src/components/compare/LegacyRunComparisonView.tsx    (~15 instances)
- archlucid-ui/src/components/compare/StructuredComparisonView.tsx   (~22 instances)
- archlucid-ui/src/components/compare/AiComparisonExplanationView.tsx (~13 instances)

For auth pages (auth/callback, auth/signin) — leave as-is; they
have minimal inline styles and are not operator workflow pages.

Translation map (reuse from Prompt 17, plus additions):
- margin: 0 → m-0
- margin: "8px 0 0" → mt-2
- margin: "10px 0 0" → mt-2.5
- margin: "12px 0 8px" → mt-3 mb-2
- marginTop: 0 → mt-0
- marginTop: 4 → mt-1
- marginTop: 8 → mt-2
- marginTop: 12 → mt-3
- marginTop: 16 → mt-4
- marginTop: 20 → mt-5
- marginTop: 24 → mt-6
- marginTop: 28 → mt-7
- marginBottom: 0 → mb-0
- marginBottom: 6 → mb-1.5
- marginBottom: 8 → mb-2
- marginBottom: 10 → mb-2.5
- marginBottom: 12 → mb-3
- marginBottom: 16 → mb-4
- marginBottom: 24 → mb-6
- marginBottom: 28 → mb-7
- marginBottom: 32 → mb-8
- padding: 8 → p-2
- padding: 12 → p-3
- padding: 16 → p-4
- padding: "10px 16px" → px-4 py-2.5
- paddingLeft: 18 → pl-[18px]  (or pl-5 if close enough)
- paddingLeft: 20 → pl-5
- fontSize: 11 → text-[11px]
- fontSize: 12 → text-xs
- fontSize: 13 → text-[13px]
- fontSize: 14 → text-sm
- fontSize: 15 → text-[15px]
- fontSize: 17 → text-[17px]
- fontSize: 20 → text-xl
- fontSize: "0.875rem" → text-sm
- fontSize: "1rem" → text-base
- fontWeight: 600 → font-semibold
- fontFamily: "monospace" → font-mono
- fontFamily: "ui-monospace, monospace" → font-mono
- lineHeight: 1.5 → leading-normal
- lineHeight: 1.55 → leading-relaxed
- lineHeight: 1.6 → leading-relaxed
- color: "#64748b" → text-neutral-500 dark:text-neutral-400
- color: "#475569" → text-neutral-600 dark:text-neutral-400
- color: "#444" → text-neutral-600 dark:text-neutral-400
- color: "#555" → text-neutral-600 dark:text-neutral-400
- color: "#666" → text-neutral-500 dark:text-neutral-400
- color: "#333" → text-neutral-700 dark:text-neutral-300
- color: "#334155" → text-neutral-700 dark:text-neutral-300
- color: "#b91c1c" → text-red-700 dark:text-red-400
- color: "#b45309" → text-amber-700 dark:text-amber-400
- color: "#4338ca" → text-indigo-700 dark:text-indigo-400
- color: "#1d4ed8" → text-blue-700 dark:text-blue-400  (or use
  the existing teal link pattern for nav links)
- background: "#fafafa" → bg-neutral-50 dark:bg-neutral-950
- background: "#f8fafc" → bg-neutral-50/90 dark:bg-neutral-900/50
- background: "#fff" → bg-white dark:bg-neutral-950
- background: "#fff8f8" → bg-red-50/60 dark:bg-red-950/20
- background: "#fffbeb" → bg-amber-50 dark:bg-amber-950/40
- background: "#f0fdf4" → bg-green-50 dark:bg-green-950/40
- border: "1px solid #ddd" → border border-neutral-200 dark:border-neutral-700
- border: "1px solid #ccc" → border border-neutral-300 dark:border-neutral-600
- border: "1px solid #e0c4c4" → border border-red-200 dark:border-red-900
- border: "1px solid #e2e8f0" → border border-neutral-200 dark:border-neutral-700
- border: "1px dashed #94a3b8" → border border-dashed border-neutral-400
  dark:border-neutral-500
- borderRadius: 6 → rounded-md
- borderRadius: 8 → rounded-lg
- display: "grid" → grid
- display: "flex" → flex
- display: "block" → block
- gap: 8 → gap-2
- gap: 10 → gap-2.5
- gap: 12 → gap-3
- gap: 14 → gap-3.5
- gap: 16 → gap-4
- flexWrap: "wrap" → flex-wrap
- alignItems: "center" → items-center
- alignItems: "flex-end" → items-end
- maxWidth: 720 → max-w-3xl
- maxWidth: 800 → max-w-3xl
- maxWidth: 900 → max-w-4xl
- maxWidth: 960 → max-w-5xl
- maxWidth: 1100 → max-w-6xl
- overflowX: "auto" → overflow-x-auto
- overflow: "auto" → overflow-auto
- maxHeight: 200 → max-h-[200px]
- maxHeight: 220 → max-h-[220px]
- wordBreak: "break-all" → break-all
- whiteSpace: "nowrap" → whitespace-nowrap
- cursor: "pointer" → cursor-pointer
- cursor: "wait" → cursor-wait
- textAlign: "left" → text-left
- columns: 2 → columns-2
- pointerEvents: "none" → pointer-events-none
- width: "100%" → w-full
- width: 80 → w-20
- width: 160 → w-40
- minWidth: 140 → min-w-[140px]
- minWidth: 220 → min-w-[220px]
- borderCollapse: "collapse" → border-collapse
- listStyle: "none" → list-none

Special cases:
- SectionCard.tsx: replace the style const entirely with Tailwind on
  the <section> and <h3>. This is a shared component — convert it
  to Tailwind classes so every consumer benefits.
- OperatorApiProblem.tsx: remove the CSSProperties import and the
  correlationStyle const; convert to Tailwind className.
- ProvenanceGraphDiagram.tsx SVG colors — keep inline style for SVG
  fill/stroke that Tailwind cannot replace (e.g. dynamic node
  colors). Add a TODO comment where inline SVG paint is necessary.
- progress.tsx — keep the transform inline style (Radix internal).
- ColorModeToggle.tsx — keep if it is a single toggle animation
  style.

Rules (same as Prompt 17):
- Do NOT change conditional rendering logic.
- Do NOT change component props or data flow.
- Do NOT change text content.
- Do NOT change element types.
- If an inline style has no obvious Tailwind equivalent, use an
  arbitrary value class (e.g. w-[72%]) or leave a TODO.
- Every replaced hardcoded color MUST include a dark: variant.
- Replace raw <button> with <Button> from shadcn only where safe
  (no custom event handlers beyond onClick).

After implementation:
- Run the build. Fix any Tailwind class typos.
- Run existing tests — no behavior should change.
- List every file modified and the count of inline styles replaced.
- Report any remaining inline styles that could not be cleanly
  replaced (expected: SVG paint, Radix transform, animation).

Stop-and-ask boundaries:
- If a file has more than 60 inline styles and the mapping is
  ambiguous for more than 5 of them, pause and list the ambiguous
  cases before proceeding.
- If a component uses CSSProperties for dynamic computed styles
  (not static objects), stop and describe them — those may need a
  different approach (cn() with conditional classes).
```

---

## Prompt 20 — Introduce OperatorPageHeader and migrate highest-traffic pages

**Why this matters.** Operator pages use at least four heading patterns: plain `<h2 style={{}}>`, `<h2 className="...">`, `<h2>` with adjacent `<p>` metadata, and `<h2>` paired with `<ContextualHelp>`. A shared `OperatorPageHeader` — title, optional subtitle, optional `ContextualHelp` key, optional metadata line — eliminates drift and makes every page feel like part of the same product.

```
Goal: create a reusable OperatorPageHeader component and migrate
the highest-traffic operator pages to use it, replacing ad hoc
heading patterns with a single consistent structure.

Read first:
- archlucid-ui/src/app/(operator)/runs/[runId]/page.tsx
  (flagship run detail — likely already has a structured header;
  use as the reference pattern)
- archlucid-ui/src/app/(operator)/runs/page.tsx
- archlucid-ui/src/app/(operator)/governance/page.tsx
- archlucid-ui/src/app/(operator)/governance/dashboard/page.tsx
- archlucid-ui/src/app/(operator)/policy-packs/page.tsx
- archlucid-ui/src/app/(operator)/audit/page.tsx
- archlucid-ui/src/app/(operator)/replay/page.tsx
- archlucid-ui/src/app/(operator)/graph/page.tsx
- archlucid-ui/src/app/(operator)/evolution-review/page.tsx
- archlucid-ui/src/app/(operator)/governance-resolution/page.tsx
- archlucid-ui/src/app/(operator)/planning/page.tsx
- archlucid-ui/src/app/(operator)/compare/page.tsx
- archlucid-ui/src/app/(operator)/search/page.tsx
- archlucid-ui/src/app/(operator)/ask/page.tsx
- archlucid-ui/src/components/ContextualHelp.tsx

Create: archlucid-ui/src/components/OperatorPageHeader.tsx

Component design:
1. Props:
   - title: string (required)
   - subtitle?: string (muted secondary line under the title)
   - helpKey?: string (renders <ContextualHelp helpKey={helpKey} />
     next to the title)
   - metadata?: React.ReactNode (small text/badges rendered below
     the title row — for timestamps, status pills, counts)
   - actions?: React.ReactNode (right-aligned action buttons)
   - children?: React.ReactNode (extra content below the header
     block; e.g. tab bars, search bars)
2. Layout:
   - Top row: flex, items-center, gap-2. Title (<h2> text-xl
     font-bold text-neutral-900 dark:text-neutral-50 m-0).
     ContextualHelp icon inline. Actions pushed right with ml-auto.
   - If subtitle present: <p> text-sm text-neutral-500
     dark:text-neutral-400 m-0 mt-1 max-w-2xl.
   - If metadata present: <div> text-sm text-neutral-600
     dark:text-neutral-400 mt-1 flex flex-wrap gap-x-4 gap-y-1.
   - children rendered below with mt-4.
   - Wrapper: <header> with mb-6 border-b border-neutral-200
     dark:border-neutral-800 pb-4.
3. Pure presentation — no data fetching, no hooks.

Apply OperatorPageHeader to these pages (highest-traffic first):
4. /runs — "Architecture runs" title, helpKey if one exists.
5. /governance — "Governance" title.
6. /governance/dashboard — "Governance dashboard" title.
7. /policy-packs — "Policy packs" title, helpKey "policy-packs".
8. /audit — title "Audit log", helpKey "audit".
9. /replay — title "Replay", helpKey "replay".
10. /graph — title "Architecture graph", helpKey "graph".
11. /evolution-review — title "Simulation review".
12. /governance-resolution — title "Governance resolution".
13. /planning — title "Planning".
14. /compare — title "Compare runs".
15. /search — title, if one exists.
16. /ask — title, if one exists.

For each page:
- Replace the existing <h2 ...> (plus any adjacent subtitle <p>)
  with <OperatorPageHeader title="..." ... />.
- Keep any existing ContextualHelp — move its helpKey into the
  OperatorPageHeader prop.
- Keep any existing actions/buttons — pass as actions prop.
- Do NOT change data loading, route structure, or conditional
  rendering.

Tests:
- OperatorPageHeader.test.tsx: renders title; renders subtitle when
  provided; renders ContextualHelp when helpKey provided; renders
  actions right-aligned; omits subtitle when not provided.

Hard constraints:
- Do NOT change routes or URLs.
- Do NOT rename files.
- Do NOT change API contracts.
- Do NOT change component prop types on existing components.
- Pages not listed above: leave their headings as-is for now.

Product vocabulary to apply during migration (UI text only, NOT
API fields or route paths):
- "Architecture runs" (not "Runs") for the runs list page heading
- "Architecture graph" (not "Graph") for the graph page heading
- "Policy packs" (not "Rule sets") in the heading
- "Simulation review" (not "Evolution review") for the heading
- "Provenance chain" (not "Authority chain") in headings

After implementation, list every file modified and summarize which
pages now use OperatorPageHeader vs. which still have ad hoc headers.
```

---

## Prompt 21 — Advisory schedules tab parity and inline-style cleanup

**Why this matters.** The advisory hub has two tabs. After Prompt 16/17, the scans tab uses `DocumentLayout`, `Button`, and Tailwind. The schedules tab (`AdvisorySchedulesContent`) still has ~17 inline styles, raw `<button>` elements, and no `DocumentLayout`. Side-by-side, the tabs look like they belong to different products.

```
Goal: bring AdvisorySchedulesContent to visual parity with the
refreshed AdvisoryScansContent. Replace inline styles with Tailwind,
swap raw <button> for <Button>, add DocumentLayout where appropriate,
and apply OperatorPageHeader if Prompt 20 has been run.

Read first:
- archlucid-ui/src/components/advisory/AdvisoryScansContent.tsx
  (reference — already refreshed with DocumentLayout + Button +
  Tailwind)
- archlucid-ui/src/components/advisory/AdvisorySchedulesContent.tsx
  (~17 inline styles, raw <button>, no DocumentLayout)
- archlucid-ui/src/components/advisory/AdvisoryHubClient.tsx
  (tab container — verify tab layout is consistent)
- archlucid-ui/src/components/ui/button.tsx  (import path)
- archlucid-ui/src/components/DocumentLayout.tsx

Changes:
1. Replace all style={{ }} in AdvisorySchedulesContent.tsx with
   Tailwind classes. Use the same translation map from Prompt 17/19.
2. Replace raw <button> elements with <Button> from shadcn. Keep
   onClick handlers unchanged. Use variant="outline" for secondary
   actions, default for primary.
3. Wrap the main content area in <DocumentLayout> (no TOC — the
   page is form-heavy, not long-form narrative).
4. Wrap <main> in className="mx-auto max-w-4xl px-4 py-6" to match
   the scans tab main container.
5. Apply consistent form input styling:
   - input/select: rounded-md border border-neutral-300 bg-white
     p-2 font-mono text-sm dark:border-neutral-600
     dark:bg-neutral-950 dark:text-neutral-100
6. Schedule list cards: rounded-lg border border-neutral-200
   bg-white p-4 dark:border-neutral-700 dark:bg-neutral-950
   (matching recommendation cards in the scans tab).
7. If OperatorPageHeader exists (from Prompt 20), use it for the
   section headings; otherwise use the same heading pattern as
   the scans tab (<h3 className="m-0 text-lg font-semibold ...">).
8. Ensure the enterprise-controls copy functions and conditional
   mutation guards are preserved — do NOT change any behavior.
9. Verify that dark mode looks correct for every replaced style.

Rules:
- Do NOT change the state management, API calls, or mutation logic.
- Do NOT change the enterprise-mutation-capability guards.
- Do NOT change the tab container (AdvisoryHubClient).
- Do NOT remove any ContextualHelp or GlossaryTooltip placements.
- Do NOT change route paths.

After implementation:
- Run the build.
- Run any existing advisory tests.
- List inline-style count before and after.
- Visually confirm both tabs feel like the same product.
```

---

## Prompt 22 — Unify compare views with Tailwind and dark mode

**Why this matters.** The three compare views (`LegacyRunComparisonView`, `StructuredComparisonView`, `AiComparisonExplanationView`) contain a combined ~50 inline styles with hardcoded hex colors, table backgrounds, and monospace font stacks. They are among the most visible power-user surfaces and currently have no dark mode support at all — dark mode renders invisible text on invisible backgrounds.

```
Goal: convert the three run comparison views from inline styles to
Tailwind classes with full dark mode support. Mechanical cleanup —
no behavior changes.

Read first:
- archlucid-ui/src/components/compare/LegacyRunComparisonView.tsx
  (~15 inline styles including cellStyle/mono const objects and
  per-element overrides)
- archlucid-ui/src/components/compare/StructuredComparisonView.tsx
  (~22 inline styles; table header rows, section headings, metadata
  spans)
- archlucid-ui/src/components/compare/AiComparisonExplanationView.tsx
  (~13 inline styles)
- archlucid-ui/src/app/(operator)/compare/page.tsx  (check how these
  components are composed)
- archlucid-ui/src/components/DocumentLayout.tsx  (reference table
  styling conventions: [&_thead_th], [&_tbody_tr:nth-child(odd)])

Changes — LegacyRunComparisonView:
1. Remove the cellStyle and mono CSSProperties const objects.
2. Define shared Tailwind class strings at the top:
   - const cellClass = "p-2.5 text-sm align-top border-b
     border-neutral-100 dark:border-neutral-800";
   - const monoClass = "font-mono text-xs";
3. Replace all inline style objects on <td>, <tr>, <th>, <p>, <h4>
   with the equivalent Tailwind classes.
4. Table header rows: bg-neutral-50/90 dark:bg-neutral-900/50.
5. Section headings (<h4>): text-[15px] font-semibold mt-6.
6. "No rows" text: text-sm text-neutral-600 dark:text-neutral-400.

Changes — StructuredComparisonView:
7. Same table styling conventions as above (cellClass, monoClass).
8. The metadata span with run IDs and status: use text-neutral-500
   dark:text-neutral-400 and code elements with font-mono text-xs.
9. The separator arrow (aria-hidden): text-neutral-300
   dark:text-neutral-600.
10. Section headings (<h4>): mt-0 mb-2 text-[15px] font-semibold
    text-neutral-900 dark:text-neutral-100.
11. Summary highlights <ul>: standard list styling (list-disc pl-5
    leading-relaxed).
12. Each comparison section (decisions, requirements, security,
    topology, cost): consistent heading + empty-state + table pattern.

Changes — AiComparisonExplanationView:
13. Replace all inline styles with Tailwind equivalents.
14. Ensure any code/pre blocks match the DocumentLayout convention
    (rounded-md border bg-neutral-100 dark:bg-neutral-800 p-3).

Shared pattern extraction (optional but encouraged):
15. If all three views share the same table header/cell pattern,
    extract shared class strings into a
    archlucid-ui/src/components/compare/compare-table-classes.ts
    file with named exports (compareHeaderRowClass, compareCellClass,
    compareMonoClass). Import in each view. This prevents drift.

Rules:
- Do NOT change the comparison logic, diff algorithms, or data flow.
- Do NOT change component prop interfaces.
- Do NOT change the section IDs (compare-legacy, compare-structured).
- Do NOT change text content.
- Keep the before/after background tints for diff highlighting
  (convert #fffbeb → bg-amber-50 dark:bg-amber-950/40 and
  #f0fdf4 → bg-green-50 dark:bg-green-950/40).

Tests:
- If existing snapshot tests exist for compare views, update them.
- Add a small test for compare-table-classes.ts if the shared file
  is created (just verify the exports exist and contain expected
  Tailwind fragments).

After implementation:
- Run the build.
- Toggle dark mode and verify every table, heading, diff badge, and
  metadata span is legible in both modes.
- List files modified and inline-style count before/after for each.
```

---

## Prompt 23 — Print stylesheet and global print chrome cleanup

**Why this matters.** `DocumentLayout` already has `print:max-w-none` and `print:hidden` on the TOC nav, but the surrounding operator shell (sidebar, top bar, command palette trigger) still renders in print. Anyone printing a value report, explanation, or provenance chain gets 4+ pages of navigation chrome before the content starts. A lightweight print stylesheet makes these document views actually printable — a real requirement for compliance teams.

```
Goal: add a global print stylesheet that hides operator navigation
chrome and expands content to full width when printing, so document
views (value report, explanation, provenance, finding audit) produce
clean, printable output.

Read first:
- archlucid-ui/src/components/AppShellClient.tsx  (main shell layout:
  sidebar + top bar + main content area)
- archlucid-ui/src/components/SidebarNav.tsx  (sidebar navigation)
- archlucid-ui/src/components/CommandPalette.tsx  (Cmd+K trigger)
- archlucid-ui/src/components/ColorModeToggle.tsx  (theme toggle)
- archlucid-ui/src/components/HelpPanel.tsx  (help overlay)
- archlucid-ui/src/components/DocumentLayout.tsx  (already has
  print:max-w-none on article and print:hidden on TOC nav)
- archlucid-ui/src/app/globals.css  (global stylesheet — check for
  existing @media print rules)
- archlucid-ui/tailwind.config.ts  (verify print: variant is enabled;
  Tailwind v4+ has it by default)

Plan:
1. In globals.css (or a new archlucid-ui/src/styles/print.css
   imported from globals.css), add a @media print block:

   @media print {
     /* Hide all navigation and non-content chrome */
     [data-testid="sidebar-nav"],
     [data-testid="app-shell-topbar"],
     [data-testid="command-palette-trigger"],
     [data-testid="color-mode-toggle"],
     [data-testid="help-panel"],
     [data-testid="help-panel-trigger"] {
       display: none !important;
     }

     /* Expand content area to full width */
     [data-testid="app-shell-main"] {
       margin-left: 0 !important;
       padding-left: 0 !important;
       max-width: 100% !important;
     }

     /* Suppress background colors and borders that waste ink */
     body {
       background: white !important;
       color: black !important;
     }

     /* Force page breaks before major sections */
     h2 { page-break-before: auto; }
     h2, h3, h4 { page-break-after: avoid; }
     table, pre, figure { page-break-inside: avoid; }
   }

2. Add data-testid attributes to the shell components IF they do not
   already have them:
   - SidebarNav wrapper → data-testid="sidebar-nav"
   - AppShellClient top bar → data-testid="app-shell-topbar"
   - AppShellClient main content → data-testid="app-shell-main"
   - CommandPalette trigger button → data-testid="command-palette-trigger"
   - ColorModeToggle → data-testid="color-mode-toggle"
   - HelpPanel overlay → data-testid="help-panel"
   These data-testid values also benefit test automation, so this
   is a two-for-one improvement.

3. Add print:hidden to any chrome elements that already use Tailwind
   but lack print suppression:
   - SidebarNav wrapper: add print:hidden
   - Top bar: add print:hidden
   - Any floating action buttons: add print:hidden

4. Verify DocumentLayout's existing print classes still work with
   the global sheet (no conflicts).

5. Add a print-friendly header that appears ONLY in print:
   - In AppShellClient (or a new PrintHeader component), render a
     <div className="hidden print:block print:mb-6"> containing:
     - The product name "ArchLucid" in text-lg font-semibold
     - The current page title (from document.title or a prop)
     - Printed timestamp: new Date().toLocaleDateString()
   This gives every printed page an enterprise header.

Rules:
- Do NOT change any runtime behavior (no visible changes in browser).
- Do NOT remove any components — only hide them in @media print.
- Do NOT change the shell layout or sidebar state management.
- Do NOT change the responsive breakpoint behavior.
- Keep all existing data-testid values — only ADD new ones.
- The print stylesheet should be additive — it should not break
  any existing styles in normal browser rendering.

Tests:
- No unit tests needed for CSS print rules (they require a real
  print rendering context).
- Verify the build succeeds with the new print stylesheet.
- Manually verify by opening a document view (value report or
  explanation) and using the browser's Print Preview (Ctrl+P) to
  confirm navigation is hidden and content fills the page.

After implementation:
- List every file modified.
- Describe how to verify the print output (which page to open,
  what to expect in Print Preview).
- Note any components that could not be print-hidden because they
  lack a stable selector.
```

---

# Appendix — Execution notes

## Prompt dependency graph

```
Prompts 1–9: Independent of each other; run in order for best results.

Prompt 10 (StatusPill): No dependencies. Run first in Part B.
Prompt 11 (Run Detail flagship): Benefits from Prompt 10 (StatusPill).
Prompt 12 (Inspector panel): Independent.
Prompt 13 (Runs list polish): Benefits from Prompts 10 + 12.
Prompt 14 (Governance dashboard polish): Benefits from Prompts 10 + 12.
Prompt 15 (Command center): Benefits from Prompts 10 + 8 (health page).
Prompt 16 (Document layout): Independent.
Prompt 17 (Inline style cleanup): Run after Prompts 11–16 (they may
          introduce new Tailwind or remove old styles).
Prompt 18 (Consistency pass): Run LAST in Part B — it sweeps everything.

Prompt 19 (Full inline-style sweep): Run after Prompt 17 (finishes what
          17 started). Independent of Prompts 20–23.
Prompt 20 (OperatorPageHeader): Independent. Run early in Part C.
Prompt 21 (Advisory schedules parity): Benefits from Prompt 20
          (OperatorPageHeader). Run after Prompts 16 + 19.
Prompt 22 (Compare views): Independent. Benefits from Prompt 19 (shared
          translation map). Run after Prompt 19.
Prompt 23 (Print stylesheet): Run after Prompts 16 + 20 (DocumentLayout
          and OperatorPageHeader provide print hooks).
```

## What these prompts preserve

- All existing routes and URLs.
- All API contracts and data loading.
- The existing `AppShellClient` shell layout.
- The existing `SidebarNav` with progressive disclosure and authority gating.
- The existing `EmptyState` + presets pattern.
- The existing `CommandPalette`, `HelpPanel`, `ColorModeToggle`.
- All existing tests (they may need updates for new component wrappers).
- The shadcn/Radix/Tailwind stack — no new UI frameworks.

## What these prompts add

- `StatusPill` — unified semantic status display.
- `InspectorPanel` — right-side quick preview without full navigation.
- `DocumentLayout` — readable long-form content wrapper.
- `CommandCenterSection` — post-Pilot actionable dashboard cards.
- `formatRelativeTime` — lightweight relative time utility.
- Consistent Tailwind styling replacing all inline `style={{ }}` objects.
- Work-queue visual treatment for runs and governance lists.
- Flagship Run Detail screen as the reference pattern.
- `OperatorPageHeader` — unified page header with title, subtitle, help, metadata, and actions.
- Shared compare-table class tokens for consistent diff/table styling.
- Global print stylesheet — hides navigation chrome, expands content, adds enterprise header.
- Print-friendly document views for compliance/auditor artifact export.
