> **Scope:** Independent cognitive-load solution quality assessment for V1 readiness; excludes explicitly deferred V1.1/V2 scope and does not assess general product quality, security, or procurement readiness except where they affect operator comprehension.

# Cognitive Load Solution Quality Assessment — 71.39%

## Objective

Assess the ArchLucid cognitive-load solution quality only: how well the current product, UI, and docs help a buyer, operator, or evaluator understand what to do next, avoid irrelevant depth, trust the output, and recover from confusion.

This score does not penalize intentionally deferred V1.1/V2 work. I found the deferred-scope markdown in `docs/library/V1_DEFERRED.md`; MCP, first-party Jira/ServiceNow/Slack, live commerce un-hold, PGP key drop, and third-party pen-test publication are out of this score.

## Weighted Score

Weighted result: **71.39 / 100**.

This is a useful but not yet commercially smooth cognitive-load solution. The product has the right primitives: Core Pilot boundary, progressive navigation, onboarding checklist, explainability panels, contextual help, empty states, route readiness tiers, and deferred-scope discipline. The commercial weakness is that those primitives are not consistently composed into one obvious first-session story. A determined evaluator can succeed. A distracted buyer can still get pulled into route naming conflicts, technical vocabulary, deep docs, or diagnostic detail before they understand the value.

Scoring model:

- **First-session path and task framing — 25% weight, 68/100, weighted 17.00.**
- **Output comprehension and explainability — 20% weight, 76/100, weighted 15.20.**
- **Navigation information architecture — 20% weight, 78/100, weighted 15.60.**
- **Failure recovery and confidence under friction — 12% weight, 67/100, weighted 8.04.**
- **Docs-to-product coherence — 10% weight, 62/100, weighted 6.20.**
- **Measurement and feedback loops — 8% weight, 70/100, weighted 5.60.**
- **Accessibility and help discoverability — 5% weight, 75/100, weighted 3.75.**

## 1. First-Session Path And Task Framing

**Weighted gap: 8.00 points. This is the biggest issue.**

The intended model is sound: a Core Pilot is four steps, and the first proof is a finalized architecture review package. That appears clearly in `docs/CORE_PILOT.md` and is reused in `CORE_PILOT_STEPS`. The operator checklist also helps by storing local progress and moving the user to the first undone step.

The problem is that the route and vocabulary system still leaks implementation history. ~~`docs/library/ONBOARDING_WIZARD.md` says `/getting-started` is canonical and `/onboarding` redirects to it, but the current UI implementation makes `/onboarding` canonical and redirects `/getting-started` to `/onboarding`. That is not a minor docs mismatch: it creates immediate doubt about which surface is current.~~ **Update (2026-05-02):** docs and `TRIAL_SIGNUP_UI.md` now align with code — canonical operator FTUE is `/onboarding`; `/getting-started` is a legacy redirect. Remaining risk is marketing `archlucid.net/get-started` vs operator `/onboarding` (separate hostname/persona story). The buyer-facing docs also say the product is a hosted SaaS, while several operator docs still assume local/API/CLI language close to the first-use path.

The vocabulary tradeoff is real. “Run” is the correct technical model; “architecture review package” is the buyer outcome. The current solution tries to bridge both, but the bridge is uneven. Several components still lead with “run” or “manifest” before the user has earned those terms.

**Recommendation:** make one canonical first-session route and one canonical first-session noun set. The default path should be “architecture review → finalized review package → findings and artifacts.” Keep “run”, “manifest”, and “authority chain” visible as technical backing, not as first-order buyer tasks.

## 2. Output Comprehension And Explainability

**Weighted gap: 4.80 points.**

The solution has serious strengths here. `RunDetailOutcomeCards` puts manifest status, findings, artifacts, and review trail at the top. `RunExplanationSection` surfaces risk posture, decision counts, finding counts, trace confidence, faithfulness, citations, and deterministic fallback warnings. `FindingExplainPanel` exposes persisted evidence-chain pointers and redacted LLM audit text with authority-aware access.

The weakness is cognitive sequencing. The UI can explain almost everything, but it does not always answer the operator’s first three questions in order:

- What changed or matters most?
- Why should I believe it?
- What should I do next?

The explainability panel is technically defensible but can become a diagnostic console: system prompt, user prompt, raw completion, trace IDs, graph node IDs, and manifest version are all useful, but not all are first-screen useful. The user needs an “executive explanation” before trace forensics.

**Tradeoff:** hiding trace detail would reduce trust for technical evaluators. Showing it too early increases perceived complexity for buyers. The right answer is not deletion; it is a layered explanation contract.

**Recommendation:** add an inspect-first summary layer for findings: recommendation, rationale, evidence count, confidence/trace completeness, affected component, and next action. Put raw/redacted LLM audit behind a “technical audit” disclosure.

## 3. Navigation Information Architecture

**Weighted gap: 4.40 points.**

The navigation architecture is one of the better parts of the solution. Tier then authority filtering is centralized in `nav-shell-visibility.ts`, the sidebar has progressive disclosure, demo mode hides unfinished or distracting surfaces, and persona presets exist for pilot operator, governance reviewer, and analytics investigator. This is the right pattern: cognitive load is reduced by shaping the shell, not by weakening server authorization.

The main issue is default posture. `full` is the default preset, then separate disclosure toggles, collapsed sidebar filtering, admin sections, route readiness, and demo-mode filtering all interact. That is too many mental switches for a first-session user. “Show all features”, “Navigation settings”, extended, advanced, persona preset, admin section, and “N more” are individually reasonable. Together they can feel like the product is asking the user to configure the map before completing the first journey.

**Tradeoff:** power users need a broad navigator, and hiding too much can create “where did the feature go?” support friction. But the first-session default should optimize for completion, not feature discovery.

**Recommendation:** default new tenants to the pilot operator preset until the first review package is finalized. After finalization, offer a clear “Explore analysis and governance” transition rather than asking users to manage disclosure flags manually.

## 4. Failure Recovery And Confidence Under Friction

**Weighted gap: 3.96 points.**

The solution has reusable error components with Problem Details copy, support hints, correlation ID display, and copy affordances. That is commercially useful: a buyer can give support something actionable instead of a screenshot.

The gap is that several important first-session failures still read like implementation failures rather than guided recovery. `OnboardingStartClient` says “Trial status request failed (status)” or “Could not load trial status.” That is accurate but not enough. The user needs to know whether to retry, sign in again, contact an admin, continue without the sample, or open a fallback route.

The same pattern appears in broader failure surfaces: correlation IDs are good for support, but the primary cognitive-load question is “Can I keep going?” The current answer is inconsistent.

**Recommendation:** add task-specific recovery copy for first-session surfaces. Every failure in onboarding, new-review creation, sample-run loading, review detail, and finalization should name the likely cause, the safe next action, and the fallback path.

## 5. Docs-To-Product Coherence

**Weighted gap: 3.80 points.**

The docs contain the right ideas: `START_HERE.md` routes by persona, `CORE_PILOT.md` explicitly says what to ignore, `V1_SCOPE.md` separates Pilot from Operate, and `V1_DEFERRED.md` prevents deferred work from being misread as readiness debt.

The weakness is drift. Some docs refer to old canonical paths, some in-app help points to paths that appear moved or inconsistent, and several docs still present technical deep links near buyer entry points. The docs are not bad; there are too many near-canonical truths.

**Tradeoff:** deep documentation is a strength for technical diligence. But depth must not be allowed to compete with the first-session spine.

**Recommendation:** create a route-and-term drift guard for cognitive-load surfaces: canonical route aliases, buyer-facing nouns, first-session links, and help-topic doc paths should be testable.

## 6. Measurement And Feedback Loops

**Weighted gap: 2.40 points.**

The product has useful signals: first-session completion, finalized-run counters, checklist telemetry, local checklist state, finding feedback, and first-finding-viewed events. This is enough to begin measuring whether the cognitive-load work is helping.

The weakness is that some signals are process-lifetime or local-only. That is fine for a V1 internal cockpit, but weak for a commercial claim. A buyer-facing or operator-facing “we know where evaluators get stuck” story needs durable funnel events by tenant/workspace, with privacy constraints and clear retention.

**Recommendation:** keep V1 modest: add durable, tenant-scoped first-session funnel events for only the Core Pilot path. Do not build a general analytics platform.

## 7. Accessibility And Help Discoverability

**Weighted gap: 1.25 points.**

The solution includes keyboard shortcuts help, role/status messaging, headings, aria labels, focusable controls, contextual help, and route-aware help topics. That is a solid baseline.

The remaining issue is prioritization, not existence. Help is searchable, but the first-session help path is not aggressive enough about answering “what should I do now?” Help topics can still send users into broad docs instead of small, task-specific guidance.

**Recommendation:** add a first-session help mode that pins only the current Core Pilot step, the next action, and a single fallback doc. Keep the broader help drawer available after that.

## Tradeoffs

The current design makes a defensible tradeoff toward transparency and power-user access. That is appropriate for architecture governance software, where buyers need evidence and operators need auditability.

The cost is that the first-session experience still feels like a capable platform being narrowed into a pilot path, not a pilot path that naturally expands into a platform. Commercially, that matters. The product must win the first 30 minutes before it asks the user to admire the system depth.

The right improvement direction is not visual polish. It is stricter sequencing:

1. One canonical route.
2. One first-session noun set.
3. One next action at a time.
4. Explain outcome before trace.
5. Defer technical surfaces until the user asks for them or completes the review package.

## Eight Best Improvements

### 1. Fix First-Session Route And Vocabulary Drift

Unify `/onboarding` and `/getting-started` behavior, docs, help topics, and test names. Pick one canonical route and make all other routes explicit redirects in code and docs.

```text
Cursor prompt:
Audit ArchLucid first-session onboarding route drift and fix it. Treat either `/onboarding` or `/getting-started` as the single canonical route based on current product intent, then update the Next.js routes, redirect comments, docs under `docs/`, help-topic links, route titles, and tests so there is one consistent story. Do not change deferred V1.1/V2 scope. Add or update regression tests proving legacy onboarding routes preserve query strings and land on the canonical page.
```

### 2. Make The First Review Package The Dominant Outcome

Change first-session UI copy so “architecture review package” is the dominant buyer-facing term. Keep “run” and “manifest” as supporting technical labels.

```text
Cursor prompt:
Review the Core Pilot and operator first-session surfaces for buyer-facing terminology. Update copy so the default user-facing outcome is “architecture review package” or “finalized review package,” while preserving “run” and “manifest” where IDs, APIs, or technical backing require them. Cover `CORE_PILOT_STEPS`, `OperatorFirstRunWorkflowPanel`, onboarding page copy, run detail outcome cards, help topics, and related tests. Keep changes scoped to cognitive-load copy and do not rename API routes or persisted models.
```

### 3. Add An Inspect-First Finding Summary Layer

Put the buyer/operator summary above trace forensics: finding, impact, evidence, confidence, affected area, recommended next action.

```text
Cursor prompt:
Add an inspect-first summary layer to the finding explainability UI. In `FindingExplainPanel` and adjacent finding detail surfaces, show a concise top block with: finding title/id, severity if available, affected component if available, plain-language rationale, evidence count, trace completeness/confidence if available, and one recommended next action. Move redacted prompt/completion audit behind a clearly labeled “Technical audit details” disclosure. Preserve existing authority checks, APIs, and feedback controls. Add focused tests for summary-first rendering and for the audit disclosure remaining accessible.
```

### 4. Default New Tenants To Pilot Navigation Until First Finalization

Use existing first-run/finalization signals to keep the shell in the pilot path until the user earns the broader platform.

```text
Cursor prompt:
Update operator navigation defaults so a tenant/user with no finalized review package starts in a pilot-focused shell. Use existing run/finalization presence helpers where possible; do not add a new analytics system. The default should expose Core Pilot links first and offer a clear “Explore analysis and governance” transition after the first finalized review package. Preserve server authorization boundaries and existing manual navigation settings. Add tests for no-finalized-review defaults, post-finalization expansion affordance, and manual override persistence.
```

### 5. Replace Generic First-Session Errors With Recovery Copy

Make onboarding, sample-run, review creation, and finalization failures answer “Can I continue?”

```text
Cursor prompt:
Improve first-session error recovery copy across onboarding and Core Pilot UI surfaces. Start with `OnboardingStartClient`, new review wizard submission/loading states, review detail loading/finalization failures, and sample-run handoff. For each failure, show likely cause, safe next action, retry/fallback option, and correlation ID when available. Reuse `OperatorApiProblem` where appropriate, but add task-specific wrappers instead of generic messages. Add tests for trial-status failure, sample missing, API 401/403, 429 retry-after, and generic 5xx fallback copy.
```

### 6. Add Cognitive-Load Drift Guards For Routes, Help Links, And Terms

Prevent future regression by checking the first-session spine mechanically.

```text
Cursor prompt:
Add lightweight cognitive-load drift guards for first-session routes, help links, and canonical terms. Create tests or scripts that verify onboarding route aliases, `HELP_TOPICS` doc paths, Core Pilot route links, and buyer-facing first-session copy stay aligned. The guard should fail when docs claim one canonical route while code redirects to another, or when help topics point to moved docs. Keep the implementation small and integrated with existing Vitest or Python CI patterns; do not build a general documentation crawler.
```

### 7. Persist Minimal Core Pilot Funnel Events

Make stuck points durable enough to improve the product without building a broad analytics platform.

```text
Cursor prompt:
Design and implement minimal durable Core Pilot funnel events for V1 cognitive-load measurement. Track tenant/workspace/project scoped events for onboarding viewed, new review started, request submitted, first review opened, first finding viewed, first finalization attempted, first finalization succeeded, and artifact/export opened. Reuse existing audit/diagnostic patterns where appropriate, avoid collecting prompt text or sensitive architecture content, and document retention/privacy behavior. Add unit tests for event recording and a small operator/admin read surface or diagnostic endpoint if an existing one fits.
```

### 8. Add Step-Aware Help Mode For The Core Pilot

Make help answer the current step before offering the full documentation library.

```text
Cursor prompt:
Add a Core Pilot step-aware mode to the operator Help panel. When the user is on onboarding, home, new review, reviews list, or review detail before first finalization, pin the current Core Pilot step, the next action, and one fallback link above the general guide/search tabs. Use `CORE_PILOT_STEPS` as the source of truth and avoid duplicating copy. Preserve existing searchable help, shortcuts, and troubleshooting tabs. Add tests for route-to-step mapping and for the general help drawer remaining available.
```

## Pending Owner Questions

No top-eight improvement is fully blocked by owner input. I am not asking anything now. The only later decisions likely worth owner input are which route name should be canonical for the hosted product and how aggressively “run” should be hidden from buyer-facing UI after V1.
