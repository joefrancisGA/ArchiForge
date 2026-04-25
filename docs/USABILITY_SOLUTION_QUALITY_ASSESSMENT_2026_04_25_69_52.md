> **Scope:** Independent usability-focused quality assessment of ArchLucid as it stands on **2026-04-25**. Scored from first principles against the repository's current state — UI components, shell architecture, onboarding flows, help surfaces, error handling, CLI, API ergonomics, docs, and tests — without reference to prior assessments.

> **Spine doc:** [Five-document onboarding spine](FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.

# ArchLucid Usability Solution Quality Assessment — 2026-04-25 (Weighted **69.52%**)

**Audience:** Owner / product / engineering. The percent in the title is the **weight-normalized** sum of every quality dimension below (each scored 1–100 against its weight, divided by total weight 100). Intentionally deferred scope (V1.1/V2 per [`V1_DEFERRED.md`](library/V1_DEFERRED.md)) is excluded from scoring.

**Method.**
1. Scores are **absolute** (1–100), not relative to prior assessments.
2. Sections are **ordered by weighted improvement headroom** — `weight × (100 − score)` — so the items at the top are the ones where deliberate investment moves the dial most.
3. Deferred items (ITSM connectors V1.1, Slack V2, MCP V1.1, commerce un-hold V1.1, reference customer V1.1, pen test / PGP V1.1, Phase 7 rename, playground) are **not penalized**.

---

## 1. Weighted score summary

| Dimension | Weight | Score | Contribution | Headroom |
|-----------|--------|-------|--------------|----------|
| Usability | 20 | 72 | 14.40 | 560 |
| Cognitive Load | 15 | 65 | 9.75 | 525 |
| Adoption Friction | 10 | 64 | 6.40 | 360 |
| Time-to-Value | 12 | 72 | 8.64 | 336 |
| Manageability | 8 | 62 | 4.96 | 304 |
| Explainability | 10 | 74 | 7.40 | 260 |
| Supportability | 8 | 72 | 5.76 | 224 |
| Trustworthiness | 5 | 68 | 3.40 | 160 |
| Observability | 5 | 70 | 3.50 | 150 |
| Correctness | 5 | 75 | 3.75 | 125 |
| Architectural Integrity | 2 | 78 | 1.56 | 44 |
| **Total** | **100** | — | **69.52** | — |

---

## 2. Quality scores — ordered by weighted improvement headroom

> Headroom = `weight × (100 − score)`. Higher headroom = bigger weighted lift if you fix it.

### 2.1 Usability — **72 / 100** · weight 20 · headroom 560

**What this measures.** Whether target users (operators, architects, platform engineers) can complete tasks effectively through the UI, CLI, and API surfaces.

**Evidence of strength.**
- Well-structured operator shell: sticky header, breadcrumbs, skip-to-main link, collapsible sidebar with group persistence, mobile drawer, command palette (Ctrl+K) with UUID quick-open.
- Three-tier progressive disclosure (essential / extended / advanced) consistently applied across sidebar, mobile drawer, and command palette via shared `listNavGroupsVisibleInOperatorShell`.
- Layer context strip (`LayerContextStrip`) with color-coded buyer-context cue and "Back to Core Pilot" escape hatch.
- Empty states (`EmptyState` + 7 preset configurations in `empty-state-presets.ts`) with actionable CTAs and help deep-links.
- Keyboard shortcuts with `aria-keyshortcuts`, shortcut hint badges, and Shift+? help overlay.
- Dark mode toggle. Breadcrumbs with `aria-current="page"`. Route announcer for screen readers. Focus management on navigation (`useRouteChangeFocus`).
- Glossary tooltips with first-visit pulse animation and "Learn more" deep-links to `docs/library/GLOSSARY.md`.
- Contextual help (?) popover with Escape dismiss and outside-click close.
- `ConfirmationDialog` on destructive actions (visible on commit).
- 21 `loading.tsx` files and 4 custom skeleton components for perceived performance.

**Gaps.**
- **Search** (`/search`) and **Ask** (`/ask`) pages use raw `<input>`, `<button>`, `<textarea>`, and inline `style={}` instead of the design system (`Input`, `Button`, `Textarea`, `Card`). This is a visual and accessibility regression against the rest of the shell — no focus rings, no dark-mode support, no consistent border radii. Together these are two of the most cognitively demanding pages.
- No visible inline form validation on text inputs. Validation feedback arrives only as post-submit API error callouts. The new-run wizard could provide step-level validation before "Next."
- No undo/redo affordance for any mutation. Commit has a confirmation dialog, but other mutations (alert acknowledge, governance approve/reject) do not visibly confirm consequences or offer rollback.
- Onboarding has **four overlapping routes** (`/onboarding`, `/onboarding/start`, `/onboard`, `/getting-started`) with three distinct wizard implementations (`OnboardingWizardClient`, `OnboardWizardClient`, `OperatorFirstRunWorkflowPanel`). A new operator landing on any of these may not realize the others exist and may repeat work or miss steps.
- `NEXT_PUBLIC_DOCS_BASE_URL` is commonly unset, making every help-topic "Open documentation" link non-functional (shows path-only with no clickable URL).

**Score justification.** The shell and nav infrastructure is above average for an enterprise SaaS V1. The Search/Ask visual regression, overlapping onboarding paths, and missing inline validation pull the score below 75.

---

### 2.2 Cognitive Load — **65 / 100** · weight 15 · headroom 525

**What this measures.** How much mental effort the system imposes while completing tasks.

**Evidence of strength.**
- Two-layer mental model (**Pilot** vs **Operate** — Operate spans analysis workloads and governance/trust) consistently labeled across home page, sidebar groups, layer context strip, help topics, and onboarding.
- Progressive disclosure defaults hide ~60% of nav surface until the operator explicitly opts in — essential-only is the default state.
- Home page structures information in clear priority: Core Pilot checklist (required) → Operate (analysis workloads) (optional maturity) → Operate (governance and trust) (optional maturity), each with inline "not required for first pilot" callouts.
- `AfterCorePilotChecklistHint` appears only after all 4 core steps are marked done.
- OptInTour is explicitly non-auto-launching (owner Q9 decision) — avoids unwanted interruption.
- Sidebar "Recent activity" card is collapsed by default for new operators with zero context.

**Gaps.**
- **Four onboarding entry points** (see §2.1) is the single largest cognitive load issue. A new operator encounters different step counts (5 in `OnboardingWizardClient`, 4 in `OperatorFirstRunWorkflowPanel`, 4 in `OnboardWizardClient`), different ordering, and different CTAs. The HelpPanel links to both `/onboarding` and `/getting-started`.
- The `nav-config.ts` module header is 100+ lines of dense docblock explaining tier/authority/drift-guard rules. This is developer cognitive load that leaks into anyone reading the source — fine for a contributing developer, but a signal that the nav model itself is complex.
- Domain terminology ("Authority pipeline," "golden manifest," "context snapshot," "decision trace") is explained by glossary tooltips but only where a `GlossaryTooltip` wrapper is applied — many pages reference these terms in plain text without the tooltip.
- 80+ operator pages is a large surface area. Progressive disclosure mitigates this, but the "Show more" toggle text doesn't quantify how many additional pages will appear.

**Score justification.** Strong progressive disclosure and layered framing, but four overlapping onboarding wizards and inconsistent glossary coverage impose unnecessary mental effort on first-week operators.

---

### 2.3 Adoption Friction — **64 / 100** · weight 10 · headroom 360

**What this measures.** How much effort, retraining, workflow disruption, and implementation burden are required before ArchLucid becomes useful.

**Evidence of strength.**
- Multiple entry paths: CLI (`archlucid try` one-shot), Docker Compose (`pilot up`), API-first, UI wizard.
- `DevelopmentBypass` auth mode eliminates Entra ID setup for local evaluation.
- Simulator mode (`AgentExecution:Mode=Simulator`) removes Azure OpenAI key requirements for pilots.
- Trial signup funnel with `archlucid trial smoke` CLI validation.
- `TrialWelcomeRunDeepLink` links new trial users directly to a seeded sample run.
- In-app opt-in tour (5 steps, never auto-launches).
- `OperatorFirstRunWorkflowPanel` with persistent checkbox "done" state.

**Gaps.**
- **Second-run friction**: After the demo, the next step is reading `SECOND_RUN.md` and constructing a TOML file. There is no in-UI template picker or guided "create from template" flow. The new-run wizard (`/runs/new`) accepts a starting-point paste but offers no discovery of available templates or briefs.
- Local development still requires Docker + .NET SDK + Node.js. There is no one-click hosted sandbox. (Hosted playground is not in V1 scope, so not penalized, but the local toolchain barrier is real.)
- The `/onboarding/start` route (post-registration) and the `/onboard` route (core pilot wizard) serve different audiences (trial vs. self-hosted) but neither visibly explains which one to use when.
- Navigation settings are discoverable only through a gear icon at the bottom of the sidebar. A new operator who doesn't find this may never realize extended/advanced pages exist.

**Score justification.** Strong CLI shortcuts and simulator mode reduce friction for technical evaluators. The second-run gap and overlapping entry points raise adoption cost for the buyer persona who matters most (the architect evaluating without eng support).

---

### 2.4 Time-to-Value — **72 / 100** · weight 12 · headroom 336

**What this measures.** How quickly meaningful customer value appears after adoption.

**Evidence of strength.**
- `archlucid try` delivers a committed manifest with a first-value report in a single command.
- `pilot up` composes the full stack (API + UI + SQL + demo seed) with Docker only.
- The 7-step new-run wizard covers the full lifecycle from description through real-time pipeline tracking.
- Home page checklist (4 steps) is concrete and completable in one session.
- `first_session_completed` metric confirms server-side that a tenant reached first value.
- Contoso demo quickstart documented.
- `OperatorTaskSuccessTile` provides visual confirmation of onboarding progress.

**Gaps.**
- After the first committed run, there is no guided "what to do next" funnel that leads to Operate (analysis workloads) or Operate (governance and trust). The `AfterCorePilotChecklistHint` exists but is a static text block, not a continuation of the interactive checklist model.
- The `BeforeAfterDeltaPanel` (sidebar variant) is powerful but collapsed by default and not explained to new operators — its value requires understanding "baseline hours" and "pilot run deltas" concepts first.
- No embedded walkthrough video or animated demo (the marketing `/see-it` route exists but is separate from operator onboarding).

**Score justification.** Time to first manifest is excellent via CLI. Time to second value (analysis, governance) is weaker because the funnel ends after the checklist.

---

### 2.5 Manageability — **62 / 100** · weight 8 · headroom 304

**What this measures.** How well the system can be configured, governed, and operated through its available surfaces.

**Evidence of strength.**
- Tenant/workspace/project scope model provides strong multi-tenancy boundaries.
- Policy packs, alert rules, alert routing, governance dashboard, governance resolution, and approval workflows are all surfaced as operator pages.
- Nav progressive disclosure settings dialog with two toggles (extended, advanced).
- Digest subscriptions and advisory scheduling for proactive governance.
- `BillingProductionSafetyRules` startup guard prevents misconfigured production deploys.

**Gaps.**
- **No operator-visible tenant/workspace settings UI.** Scope headers (`x-tenant-id`, `x-workspace-id`, `x-project-id`) are implicit — operators cannot inspect or switch their current scope from the UI. The only way to change scope is to modify the API proxy environment variables.
- **No admin user management surface.** User roles, API key rotation, and tenant configuration live entirely in config files or direct API calls. The `/admin/support` page exists but is narrow (support bundle only).
- Nav settings dialog is limited to two checkboxes. There is no preference for default landing page, notification delivery channel, or display density.
- No configuration validation feedback in the UI — startup config errors only appear in server console logs.

**Score justification.** Governance and policy management are strong feature surfaces, but the absence of tenant settings, user management, and scope switching in the UI means operators frequently drop to config files or API calls for basic admin tasks.

---

### 2.6 Explainability — **74 / 100** · weight 10 · headroom 260

**What this measures.** Whether the system can explain its reasoning and outputs.

**Evidence of strength.**
- `FindingExplainPanel` shows redacted LLM prompt/completion audit per finding with evidence chain.
- `FindingExplainabilityDialog` provides deterministic trace alongside LLM narrative.
- `RunFindingExplainabilityTable` for tabular explainability across findings.
- Faithfulness checking (`archlucid_explanation_faithfulness_ratio`) with automatic deterministic fallback when LLM narrative scores low.
- Citation references (`archlucid_explanation_citations_emitted_total`) attached to aggregate explanations.
- Thumbs up/down feedback on explanations (Execute authority).
- Agent output structural completeness and semantic quality scores exposed as metrics.

**Gaps.**
- Explainability is available only at the individual finding level. There is no run-level "why did this architecture get these recommendations?" narrative in the UI (the aggregate explanation API exists but its UI rendering is not visible in the page files reviewed).
- The deterministic fallback path is a silent substitution — the operator does not know when the explanation was generated deterministically vs. by LLM unless they check metrics.
- No explainability surface for governance decisions (why was this approval required? which policy triggered this finding?).

**Score justification.** Per-finding explainability is genuinely strong and the faithfulness guardrail is a differentiator. Run-level and governance-level explanation gaps prevent a higher score.

---

### 2.7 Supportability — **72 / 100** · weight 8 · headroom 224

**What this measures.** How well issues can be diagnosed and resolved.

**Evidence of strength.**
- CLI `doctor` command validates API reachability, build identity, and health endpoints in one invocation.
- CLI `support-bundle --zip` produces sanitized diagnostics with a `README.txt` triage guide.
- Admin → Support page in UI (gated on operator access).
- Troubleshooting guide (`TROUBLESHOOTING.md`) with structured symptom matrix, first-line steps, and API startup failure checklist.
- RFC 9457 ProblemDetails with `supportHint`, `correlationId`, and `errorCode` on API error responses.
- Client-side error telemetry (`reportClientError`) with rate limiting (5/min) and production-only behavior.
- `OperatorApiProblem` component renders structured error details with correlation IDs in the UI.
- Error boundary (`error.tsx`) with "Try again" and "Home" recovery.

**Gaps.**
- No in-app diagnostics view. Operators who want health check results, circuit breaker state, or recent error rates must use CLI or direct API calls — the UI does not surface `/health/ready` or `/v1/diagnostics/*` in any admin panel.
- `NEXT_PUBLIC_DOCS_BASE_URL` is frequently unset, which breaks every "Open documentation" link in the HelpPanel — this is the primary in-app doc surface and it is non-functional in typical deployments.
- Error telemetry is fire-and-forget with no operator-visible history. If an operator experiences a transient error, there is no "recent errors" view to retroactively inspect.

**Score justification.** CLI diagnostics and structured error responses are strong. The inability to access diagnostics or documentation from within the UI itself is the main gap.

---

### 2.8 Trustworthiness — **68 / 100** · weight 5 · headroom 160

**What this measures.** Whether a buyer or operator should rely on outputs in real enterprise use.

**Evidence of strength.**
- Faithfulness guardrails on explanations with deterministic fallback.
- Quality gates on agent outputs (structural completeness + semantic score).
- Data consistency orphan probes with detection/alert/quarantine modes.
- Durable audit trail with `IAuditService` and `GovernanceWorkflowService` dual-write.
- Trust Center page (`/trust`), Security/Trust workspace page, and `/security-trust` marketing page.
- Owner-conducted security self-assessment on file.

**Gaps.**
- No operator-visible indicator of explanation provenance (LLM vs. deterministic fallback).
- No confidence score or uncertainty indicator displayed alongside findings — operators see findings as equally certain.
- Pen test and PGP key are V1.1-deferred (not penalized, but stated for completeness).

**Score justification.** The guardrails are architecturally sound. The absence of visible provenance/confidence indicators means operators must trust outputs without signals that would help them calibrate trust.

---

### 2.9 Observability — **70 / 100** · weight 5 · headroom 150

**What this measures.** How visible internal behavior is through logs, metrics, traces, and diagnostics.

**Evidence of strength.**
- 30+ custom OpenTelemetry instruments (histograms, counters, observable gauges) covering pipeline stages, agent quality, LLM token usage, circuit breakers, data consistency, and business KPIs.
- Structured logging with `RunId`, `X-Correlation-ID`, and pipeline stage markers.
- Trace viewer URL integration in CLI (`archlucid trace <runId>`).
- Explanation cache hit/miss metrics.
- `OperatorTaskSuccessTile` surfaces server-side onboarding milestone metrics in the UI.

**Gaps.**
- No built-in observability dashboard. Operators must set up Grafana, Prometheus, or Application Insights externally. The `OBSERVABILITY.md` doc provides Prometheus recording rules and Grafana panel suggestions, but there is no out-of-box experience.
- The operator UI exposes a `BeforeAfterDeltaPanel` with limited run-delta data, but not pipeline health, error rates, or system metrics.
- `archlucid trace` requires `ARCHLUCID_TRACE_VIEWER_URL_TEMPLATE` to be configured — without it, the CLI prints only a raw trace ID with setup instructions.

**Score justification.** Instrumentation depth is excellent. The gap is entirely in consumption — operators with no Grafana/OTLP setup have no way to see their own system's behavior.

---

### 2.10 Correctness — **75 / 100** · weight 5 · headroom 125

**What this measures.** Whether the system produces the right outputs, evaluations, recommendations, checks, and behaviors.

**Evidence of strength.**
- 183+ test files in the UI project covering components, hooks, accessibility (axe), regression, integration, and snapshot tests.
- Authority-shaped regression tests (`authority-shaped-ui-regression.test.ts`, `authority-execute-floor-regression.test.ts`) ensuring nav visibility and authority rank consistency.
- Nav shell visibility tests locking progressive disclosure behavior.
- API problem parsing tested for RFC 9457 compliance.
- Breadcrumb map, toast, config, and api-error modules all have dedicated test files.
- `OnboardingWizardClient.test.tsx`, `OperatorFirstRunWorkflowPanel.test.tsx`, `OptInTour.test.tsx` cover onboarding correctness.

**Gaps.**
- Search page (`/search`) and Ask page (`/ask`) have no visible test files for their page-level client components (API integration, state management, error rendering).
- No end-to-end Playwright tests for the full onboarding flow (smoke tests use mocked proxy per `V1_DEFERRED.md` §4).
- `OnboardWizardClient` (the `/onboard` variant) has a test file, but it is unclear whether it covers the post-registration trial-status integration.

**Score justification.** Test coverage is strong in the shell and shared components. The Search/Ask pages and e2e onboarding flows are the main correctness gaps.

---

### 2.11 Architectural Integrity — **78 / 100** · weight 2 · headroom 44

**What this measures.** Whether the overall design is internally coherent, properly bounded, structurally sound, and non-contradictory.

**Evidence of strength.**
- Clean data→filter→render separation: `nav-config.ts` (static data) → `nav-shell-visibility.ts` (tier+authority filtering) → `SidebarNav`/`MobileNavDrawer`/`CommandPalette` (rendering). All three nav surfaces share the same filtering function.
- Layer model (`LayerId`) consistently applied: `getLayerForRoute` → `LayerContextFromRoute` → `LayerContextStrip`, with tests at every level including axe accessibility.
- Authority rank model (`useNavCallerAuthorityRank`) shared across nav, capability hints, and Operate capability hooks.
- Error handling is compositional: `api-error.ts` → `ApiRequestError` → `OperatorApiProblem` → page-level rendering.
- Toast system is thin (3 functions wrapping `sonner`) and not over-abstracted.
- Empty state presets are centralized with consistent shape.

**Gaps.**
- Three distinct onboarding wizard implementations (`OnboardingWizardClient`, `OnboardWizardClient`, `OperatorFirstRunWorkflowPanel`) with different state models and step counts. This is an architectural coherence issue — the same concept (first-run guidance) has three implementations with no shared abstraction.
- Search and Ask pages do not use the design system components used everywhere else. This is a bounded-context violation — they exist inside the operator shell but render outside its visual language.

**Score justification.** The nav/authority/layer architecture is genuinely well-designed and tested. The onboarding fragmentation and Search/Ask visual divergence are the only structural inconsistencies.

---

## 3. Eight best improvements

Improvements are ordered by estimated weighted score impact. Each includes a paste-ready Cursor prompt unless marked **DEFERRED** (requires owner input before any work can begin).

> **SaaS audience guard.** ArchLucid is a SaaS product. Customers never install Docker, SQL, .NET, Node, or Terraform. Tooling like `archlucid try`, `dev up`, `docker compose` is internal contributor/operator tooling. If a prompt output seems to require a customer-side install step, stop and ask the user.

---

### Improvement 1 — Consolidate onboarding to a single canonical route and wizard

**Impact:** Usability (+4), Cognitive Load (+6), Adoption Friction (+3), Architectural Integrity (+2). Estimated weighted lift: **+2.36 points.**

**Problem.** Four routes (`/onboarding`, `/onboarding/start`, `/onboard`, `/getting-started`) with three wizard implementations. Operators encounter different step counts, different ordering, and different CTAs depending on which entry point they find first.

**Tradeoff.** Removing routes risks breaking bookmarks and deep links from emails or docs. Mitigation: redirect old routes to the canonical one.

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

### Improvement 2 — Upgrade Search and Ask pages to use design system components

**Impact:** Usability (+3), Cognitive Load (+2), Correctness (+2), Architectural Integrity (+1). Estimated weighted lift: **+1.55 points.**

**Problem.** `/search` and `/ask` are the only operator pages that use raw HTML elements and inline `style={}` instead of the shared design system (`Input`, `Button`, `Textarea`, `Card`, `Label`). They have no dark-mode support, no focus rings, no consistent border radii, and visually break from the rest of the shell.

**Tradeoff.** This is a styling refactor only — no behavior changes. Low risk.

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

### Improvement 3 — Add contextual help entries to high-traffic operator pages

**Impact:** Explainability (+3), Cognitive Load (+2), Usability (+1). Estimated weighted lift: **+1.20 points.**

**Problem.** `ContextualHelp` (?) popovers are only wired to `/runs/new` (`new-run-wizard`) and `/runs/[runId]` (`commit-manifest`). High-traffic pages like `/alerts`, `/governance/dashboard`, `/compare`, `/graph`, and `/audit` have no inline contextual help. The `contextual-help-content.ts` index likely has few entries.

**Tradeoff.** Each entry requires a one-line text blurb and an optional doc link. Low effort, high leverage for first-week operators.

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
  (can combine with Improvement 2 refactor)
- /ask → <ContextualHelp helpKey="ask-archlucid" />
  (can combine with Improvement 2 refactor)
- /advisory → <ContextualHelp helpKey="advisory-hub" />

Run existing ContextualHelp tests. Add a test in
contextual-help-content.test.ts that asserts every key used by a
ContextualHelp component exists in the index (prevent typo orphans).

Do NOT change ContextualHelp component behavior. Do NOT add help
entries for marketing pages.
```

---

### Improvement 4 — Add inline validation feedback to the new-run wizard steps

**Impact:** Usability (+2), Cognitive Load (+2), Correctness (+1). Estimated weighted lift: **+1.05 points.**

**Problem.** The 7-step new-run wizard validates inputs only on submit (server-side API error). Operators who leave required fields empty or enter invalid UUIDs discover this only after clicking "Next" or "Submit," which breaks flow and increases cognitive load.

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

### Improvement 5 — Expand glossary tooltip coverage to key operator pages

**Impact:** Cognitive Load (+2), Explainability (+1). Estimated weighted lift: **+0.70 points.**

**Problem.** `GlossaryTooltip` is implemented and tested but applied to only a few component instances. Many operator pages reference domain terms ("golden manifest," "authority pipeline," "findings," "context snapshot") in plain text without the tooltip wrapper, leaving first-time operators to infer meaning from context.

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

### Improvement 6 — Wire NEXT_PUBLIC_DOCS_BASE_URL default or fallback for HelpPanel doc links

**Impact:** Supportability (+3), Usability (+1). Estimated weighted lift: **+0.60 points.**

**Problem.** `getDocHref()` returns `null` when `NEXT_PUBLIC_DOCS_BASE_URL` is unset, rendering every "Open documentation" link in the HelpPanel as a non-clickable path string. This is the primary in-app help surface and it is non-functional in most deployments.

**Tradeoff.** Hardcoding a GitHub blob URL couples the app to a specific repo hosting location. Mitigation: use the public GitHub repo URL as a fallback, with the env var as an override.

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

### Improvement 7 — Add operator-visible tenant/workspace settings, scope switching, and role management UI

**Impact:** Manageability (+5), Usability (+2). Estimated weighted lift: **+0.80 points.**

**Problem.** Scope headers (`x-tenant-id`, `x-workspace-id`, `x-project-id`) are the only way to control data isolation, but operators cannot inspect or change their current scope from the UI. There is no tenant settings page, no user management surface, and no scope switcher.

**Owner decisions (2026-04-25):**
- Workspace/project scope switching from the UI: **approved**.
- Non-obscure tenant-level settings should be editable from the UI.
- Role/authority management UI: **approved** for V1.

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

### Improvement 8 — Add in-app diagnostics/health dashboard for operators

**Impact:** Supportability (+3), Observability (+3). Estimated weighted lift: **+0.74 points.**

**Problem.** Operators who want to check system health, circuit breaker state, recent error rates, or pipeline throughput must use the CLI (`doctor`, `support-bundle`) or call API endpoints directly. There is no in-UI health or diagnostics panel.

**Owner decisions (2026-04-25):**
- Include an in-app health/diagnostics panel: **approved**.
- Authority gate: **none — show to any authenticated user (ReadAuthority floor).**

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

### Improvement 9 — Add a "what's next" continuation funnel after Core Pilot checklist completion

**Impact:** Time-to-Value (+2), Adoption Friction (+1). Estimated weighted lift: **+0.58 points.**

**Problem.** The `AfterCorePilotChecklistHint` is a static text block that appears after all 4 checklist steps are done. It suggests Operate (analysis workloads) and Operate (governance and trust) but does not continue the interactive checklist model. Operators who completed the pilot successfully have momentum — the system should channel it into the next value layer.

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

## 4. Pending questions for owner (saved for later)

**Resolved 2026-04-25:**
1. ~~Should operators be able to switch workspace/project scope from the UI?~~ **Yes — approved.** Scope switching from the UI is in scope.
2. ~~Which tenant-level settings should be editable from the UI?~~ **Non-obscure settings should be editable from the UI.** Obscure/infrastructure settings stay config-file-only.
3. ~~Should user/role management have a UI surface in V1?~~ **Yes — a role/authority management UI makes sense for V1.**

**Resolved 2026-04-25:**
4. ~~Should the operator UI include a health/diagnostics panel?~~ **Yes — include an in-app health/diagnostics panel.**
5. ~~What authority rank should gate access?~~ **No restriction — show it to any authenticated user (ReadAuthority floor).**

---

## 5. Methodology notes

- **Weights** sum to 100 and reflect relative importance to the usability solution quality umbrella. Usability (20) and Cognitive Load (15) are weighted highest because they are the core of the assessment mandate. Architectural Integrity (2) is weighted lowest because it is an enabler of usability, not a direct user-facing quality.
- **Deferred items** per [`V1_DEFERRED.md`](library/V1_DEFERRED.md) are explicitly excluded: ITSM connectors (V1.1), Slack (V2), MCP (V1.1), commerce un-hold (V1.1), reference customer publication (V1.1), pen test / PGP key (V1.1), Phase 7 rename, hosted playground, cross-tenant analytics, product learning deterministic theme-derivation.
- **Scores are not inflated.** A 65 in Cognitive Load means "above-average progressive disclosure, below-average first-session clarity due to overlapping onboarding." A 78 in Architectural Integrity means "well-designed shared abstractions with two structural inconsistencies."
- **Uncertainty.** I did not execute the app or verify runtime behavior. Scores are based on source code, test files, and documentation review. Runtime visual regressions, performance bottlenecks, or dark-mode rendering issues may exist that are not captured here.
