# OpenAI UI Feedback — Assessment & To-Do List
**Received:** 2026-05-01 4:12 AM EDT  
**Assessed:** 2026-05-01  
**Scope:** 100 items across P0 (demo blockers) and P1 (commercial polish)

---

## Executive Summary

The vast majority of P0 failures share **one root cause**: the static demo fallback
infrastructure is fully built but is gated behind `NEXT_PUBLIC_DEMO_MODE=true` or
`NEXT_PUBLIC_DEMO_STATIC_OPERATOR=true`. If neither env var is set in the deployed
environment OpenAI is reviewing, every route that requires live data for the canonical
demo run (`claims-intake-modernization`) falls through to its error state — and the
static fallback never fires.

This means items 1–4, 11, 22, 25, 30, 42–44 and roughly a dozen more are the same
defect wearing different clothes. Fix the env-var gate for the canonical demo run ID
and most of the "broken" surfaces become functional with a single targeted change.

The P1 items are genuine polish issues: incomplete terminology migration (`run` → `review`),
raw technical identifiers surfacing on buyer-facing pages, and homepage CTA overload. These
need their own focused pass.

---

## What We Agree With

### Fully agreed — confirmed by code

| # | Finding | Why we agree |
|---|---------|-------------|
| 1–4 | Core review, finding, provenance, and inspect routes fail | Static fallback requires env var that is not set in deployment |
| 7 | Identifier mismatch across demo slugs | Four IDs in `DEMO_RUN_IDS_FOR_STATIC_FALLBACK`, two in Compare static payloads, one alias mapping, one in breadcrumb map — six total |
| 11, 15 | Home says "no reviews yet" while Reviews shows a finalized sample | Pickers hit live API; no static fallback without env var |
| 22, 25, 30 | Graph, Ask, Governance selectors all say "no reviews" | Same — they don't uniformly call static fallback functions |
| 28, 71–76 | "run" / "run context" / "Run ID" / "run detail" still appear in authenticated UI | Terminology migration is incomplete — confirmed in `RunDetailSectionNav` section IDs, `run-metadata` section heading, page copy |
| 42–44 | Audit log empty while review trail claims completion | `getDemoSampleAuditTrailEvents` exists and is wired, but `auditDemoSampleInjectEnabled()` is gated by the same env var |
| 5, 6, 16–19 | "Open review" / "Review finding" CTAs route into broken pages | Correct — these CTAs should guard against linking to unresolvable routes |
| 34 | "Coming soon" visible in authenticated routes | Should be feature-flagged off, not displayed |
| 45–46 | Alerts shows empty tabs including rules/routing/simulation/tuning | Valid — empty module tabs signal an unfinished product |
| 47–50 | Admin Health, Support, Users visible in demo | Operator tooling should not be discoverable during buyer demos |
| 85 | Homepage CTA overload | Multiple competing paths (New review, See completed example, Take tour, First Manifest Guide, etc.) |
| 93 | Manifest leads with counts, not business conclusion | Manifest section shows Status → Policy pack → Decisions before any narrative conclusion |
| 96–97 | Artifact filenames (`.md`, `.json`, `.mmd`) and MIME types are buyer-visible | Confirmed in `ArtifactListTable` — no business label transformation applied |
| 78 | "Operate — analysis" / "Operate — governance" nav labels | Internal product taxonomy, not buyer language |
| 10 | "Retry" as primary CTA on deterministic sample failures | Correct — retry misleads users on non-transient failures |
| 99 | Compare shows raw `claims-intake-run-v1` / `claims-intake-run-v2` slugs and weak cost delta | `compareRunBuyerDisplayLabel` exists but raw IDs still leak into display |
| 90 | Reviews list shows "4 months ago" | Relative dates erode demo credibility over time; a fixed polished date is better |
| 67–70 | Raw slugs, UTC timestamps, workspace names, artifact filenames on public pages | Confirmed pattern in codebase |

---

## What We Disagree With (or Redirect)

| # | Finding | Our position |
|---|---------|-------------|
| 9 | "Do not show the full app shell around fatal route failures" | **Disagree.** Removing the nav makes error recovery *harder*. Fix: better error state design with prominent escape routes, not nav removal. |
| 8, 14 | "Stop displaying Platform services: Healthy on broken pages" | **Partially disagree.** The backend services may genuinely be healthy; the *sample data* is broken due to missing env config. These are different failure modes. Fix: make the status widget demo-context-aware (show "Sample data unavailable"), not remove truthful health signals. |
| 36 | "Hide Approval Lineage entirely until complete" | **Disagree.** Lineage is a core differentiator. Partial lineage with honest data is better than hiding the route. Fix: populate the static demo lineage payload. |
| 39 | "Hide Policy Packs from buyer demos" | **Partially disagree.** Policy packs are a key governance selling point. Fix: strip raw JSON config, repository paths, and lifecycle controls (#40, #41). The concept should be shown; the implementation internals should not. |
| 100 | "Hide Compare, Replay, Search" in strict allowlist | **Disagree with scope.** Compare, Ask, and Graph are three of the product's most distinctive capabilities. Fix: make them demo-safe by extending the static fallback pattern — not by hiding them. |
| 82 | "Hide Jump control — looks like developer palette" | **Context-dependent.** A polished command palette can be a *positive* enterprise signal. The issue is styling and labeling, not existence. Evaluate after polish pass. |

---

## To-Do List

### Tier 1 — Single Root-Cause Fix (unblocks ~25 items)

These all resolve if the static demo fallback activates unconditionally for the canonical run.

- [ ] **T1-1 · Make static fallback unconditional for the canonical demo run ID**  
  In `operator-static-demo.ts`, change `isStaticDemoPayloadFallbackEnabled()` (or its call sites in
  `RunDetailPage`, `FindingPage`, `ProvenancePage`, `InspectPage`) so that when the request run ID
  matches `SHOWCASE_STATIC_DEMO_RUN_ID` (or its known aliases), static data is served regardless of
  env vars. Apply the same pattern to Ask, Graph, and Governance selectors.  
  *Resolves: P0 #1–4, 11, 15, 20–26, 30–31, 42–44*

- [ ] **T1-2 · Apply `canonicalizeDemoRunId` before any API call for demo slugs**  
  `RunDetailPage` and related pages receive `claims-intake-modernization-run` as a `runId` param but
  call APIs with it directly. Apply `canonicalizeDemoRunId(runId)` at the top of every server page
  that accepts a runId segment so all alias slugs redirect or resolve to the canonical ID before
  the API call is attempted.  
  *Resolves: P0 #7 (partial), route consistency*

- [ ] **T1-3 · Add 308 redirect from alias slugs to canonical slug**  
  In the `[runId]` layout (or middleware), detect non-canonical demo slugs via
  `demoRunUrlRequiresCanonicalRedirect` and issue a `redirect()` to the canonical URL. This
  eliminates the mismatch between bookmarks, CTAs, and static payloads in one place.  
  *Resolves: P0 #7 fully*

### Tier 2 — Broken CTAs and Route Guards

- [ ] **T2-1 · Guard "Open review" CTAs against known-broken routes**  
  In Reviews list, side drawer, Demo Preview, Live Demo, Showcase, and public sample pages: check
  whether the target `runId` is resolvable (static demo present OR API reachable) before rendering
  the CTA. If not resolvable, show "Open sample preview" or "See public summary" instead.  
  *Resolves: P0 #5, 6, 16–19*

- [ ] **T2-2 · Replace "Retry" with context-aware recovery actions on deterministic demo failures**  
  Detect when the failure is on a known demo slug (not a transient network error). In that case,
  replace or supplement "Retry" with "Open sample manifest," "Open public preview," and
  "Back to reviews."  
  *Resolves: P0 #10*

- [ ] **T2-3 · Disable or redirect "Review trail graph" quick link when provenance is empty**  
  If provenance page cannot load, the quick link in Reviews side drawer should be disabled or point
  to a populated static provenance view.  
  *Resolves: P0 #19*

### Tier 3 — Empty States That Contradict Finalized Sample Status

- [ ] **T3-1 · Auto-load sample graph in Graph page when static demo mode is active**  
  Graph should call `tryStaticDemoProvenanceGraph(SHOWCASE_STATIC_DEMO_RUN_ID)` on mount when no
  live graph is available. Remove "No graph on screen yet" as the default state.  
  *Resolves: P0 #20–24*

- [ ] **T3-2 · Auto-select sample review in Ask when static demo mode is active**  
  Ask's `AskRunIdPicker` should pre-select `SHOWCASE_STATIC_DEMO_RUN_ID` when no live reviews exist.
  Disable suggested prompts until a review context is active.  
  *Resolves: P0 #25–27*

- [ ] **T3-3 · Auto-load sample governance workflow when static demo mode is active**  
  Governance should render the sample approval request, promotion, and activation rows from static
  payload rather than opening to empty tables with disabled submission.  
  *Resolves: P0 #30–32*

- [ ] **T3-4 · Fix Governance Findings empty state**  
  Findings should show sample findings from the static payload (`SHOWCASE_STATIC_DEMO_PRIMARY_FINDING_ID`
  and associated findings) rather than "No findings in queue yet."  
  *Resolves: P0 #32*

### Tier 4 — Platform Status & Shell Behavior

- [ ] **T4-1 · Make "Platform services: Healthy" banner demo-context-aware**  
  When static demo mode is active and the sample data is being served from fallback, change the
  banner label to "Sample data — live API not connected" (or similar). Do NOT remove the health
  signal; make it accurate.  
  *Resolves: P0 #8, #14 (with our redirect from disagreement above)*

- [ ] **T4-2 · Improve error state design for fatal route failures**  
  Keep the app shell. Add a distinct visual treatment (e.g. tinted background, clear "Demo sample
  unavailable" header) that disambiguates a broken sample route from a normal product state. Make
  "Back to reviews," "Open public preview," and "Open sample manifest" the primary CTAs.  
  *Resolves: P0 #9 (our re-framing)*

### Tier 5 — Terminology Migration (`run` → `review`)

These are a focused find-and-replace pass across the authenticated operator shell.

- [ ] **T5-1 · Complete `run` → `review` terminology across all authenticated routes**  
  Rename: "Architecture runs" → "Architecture reviews," "Run ID" → "Review ID,"
  "Open run detail" → "Open review," "run context" → "review context,"
  "Compare two finalized runs" → "Compare two finalized reviews,"
  "Run required for a new conversation" → "Select an architecture review."  
  Also remove "thread" language from Ask, Graph, and Governance body copy.  
  *Resolves: P1 #71–77*

- [ ] **T5-2 · Rename `run-metadata` section ID and heading in `RunDetailPage`**  
  Section heading reads "Review" but section ID is `run-metadata` and `RunDetailSectionNav` label
  is "Run." Align the section ID, nav label, and heading.  
  *Resolves: P0 #28 (partial)*

### Tier 6 — Navigation & Shell Polish

- [ ] **T6-1 · Remove "Operate — analysis" / "Operate — governance" nav labels**  
  Replace with plain section headings or remove the category labels entirely.  
  *Resolves: P1 #78*

- [ ] **T6-2 · Simplify left navigation collapsed-menu labels**  
  Remove count-based labels like "2 pilot tools" / "11 advanced analysis" / "8 governance controls."
  These read like internal release notes.  
  *Resolves: P1 #79*

- [ ] **T6-3 · Hide "Navigation settings" and "open navigation settings" affordances from normal users**  
  These are shell configuration controls, not product features. Gate behind an admin flag.  
  *Resolves: P1 #80–81*

- [ ] **T6-4 · Rename "Default project" in workspace selector to a polished sample workspace label**  
  Use the demo workspace display name consistently ("Claims Intake Demo Workspace" or similar) in
  place of "Default project."  
  *Resolves: P1 #83–84*

### Tier 7 — Homepage

- [ ] **T7-1 · Reduce homepage CTA count to three clear actions**  
  Primary: "Start a new review." Secondary: "See the completed Claims Intake example." Tertiary:
  "How it works." Remove competing cards (First Manifest Guide, Compare, Replay, Graph shortcuts)
  from the hero area.  
  *Resolves: P1 #85–89*

- [ ] **T7-2 · Fix "See completed example" to route only to a working page**  
  Must not route to the broken review detail route. Route to the working public showcase or the
  manifest page after T1-1 is complete.  
  *Resolves: P0 #13*

- [ ] **T7-3 · Fix "Resolving checklist links..." loading state on Home**  
  This still reads like a debug/unfinished state. Either resolve it synchronously from static data
  or remove the checklist component from the demo build.  
  *Resolves: P0 #12*

### Tier 8 — Buyer-Facing Display Polish

- [ ] **T8-1 · Replace relative date "4 months ago" in Reviews list with a polished fixed label**  
  Use "Sample review — April 2026" or format as a full calendar date. Relative timestamps erode
  credibility over time.  
  *Resolves: P1 #90*

- [ ] **T8-2 · Fix Reviews side-panel title truncation**  
  "Claims Intake Modernization — sample c..." should display the full title or truncate with a
  proper `title` attribute.  
  *Resolves: P1 #91*

- [ ] **T8-3 · Reduce Reviews side-panel quick-action count**  
  Nine CTAs in the drawer is too many. Prioritize: "Open review," "Manifest," "Primary finding,"
  "Artifacts." Collapse the rest behind a "More" disclosure.  
  *Resolves: P1 #92*

- [ ] **T8-4 · Manifest: lead with business conclusion, move counts below**  
  Add an `operatorSummary` narrative paragraph above the `Status / Policy pack / Decisions` grid,
  or restructure the `ManifestSummarySection` to show the governance gate outcome first.  
  *Resolves: P1 #93–94*

- [ ] **T8-5 · Move manifest technical identifiers into a collapsed "Audit identifiers" section**  
  Even in collapsed form, identifiers appear too early for a sponsor-facing artifact. Move them to
  the bottom of the manifest detail behind a disclosure.  
  *Resolves: P1 #95*

- [ ] **T8-6 · Apply business labels to artifact descriptors**  
  Use a lookup table (similar to `compareRunBuyerDisplayLabel`) to map artifact filenames to
  "Sponsor brief," "Decision record," "System diagram," "Evidence bundle." Hide raw MIME types;
  move them into a `<details>` or tooltip.  
  *Resolves: P1 #96–97*

- [ ] **T8-7 · Strip raw slugs, UTC timestamps, workspace names, and artifact filenames from public pages**  
  Public marketing and showcase pages should not expose `claims-intake-modernization`,
  `claims-intake-sample-workspace`, `default`, `.md`, `.json`, or raw UTC strings. Use the
  `DEMO_PATH_SEGMENT_TITLES` pattern already in `breadcrumb-map.ts` as the reference.  
  *Resolves: P0 #67–70*

### Tier 9 — Operator-Only Routes Hidden in Demo

- [ ] **T9-1 · Feature-flag Admin Health, Admin Support, Admin Users out of demo mode**  
  These are operator tooling surfaces. In demo/public mode, either hide these nav items entirely
  or show a "For operators only" placeholder.  
  *Resolves: P0 #47–50*

- [ ] **T9-2 · Feature-flag Tenant Settings, Digest Subscriptions, Digests, Teams Integration out of demo**  
  None of these are buyer-journey pages in their current state.  
  *Resolves: P0 #53–59*

- [ ] **T9-3 · Remove "coming soon" from all authenticated operator routes**  
  Feature-flag incomplete modules off. Governance Dashboard, Planning, Evolution Review, Product
  Learning, Recommendation Learning, Replay (until T1-1 lands), Search.  
  *Resolves: P0 #33–34, 60–65*

- [ ] **T9-4 · Strip raw JSON policy configuration from Policy Pack detail page**  
  `complianceRuleIds`, `alertRuleIds`, metadata JSON, lifecycle controls, repository paths, and
  vertical template references are internal. Replace with buyer-facing governance narrative.  
  *Resolves: P0 #40–41*

### Tier 10 — Auth & Sign-In

- [ ] **T10-1 · Fix auth error states to render outside the full app shell**  
  Auth failures should be minimal, public pages — not wrapped in the left nav / workspace selector.
  (Note: this is the auth error page specifically, not the normal authenticated shell.)  
  *Resolves: P0 #51–52*

---

## Items Not in the To-Do List and Why

| # | Reason not actioned |
|---|---------------------|
| 9 | Reframed as T4-2 (improve error state, not remove shell) |
| 8, 14 | Reframed as T4-1 (context-aware banner, not removal) |
| 36 | Reframed as part of T1-1/T3-3 (populate data, don't hide route) |
| 39 | Reframed as T9-4 (strip internals, don't hide concept) |
| 100 | Reframed — extend demo safety to all surfaces, not strict allowlist |
| 82 | Deferred — evaluate after polish pass; not confirmed as net-negative |

---

## Dependency Order

```
T1-1 (unconditional fallback) → T2-1 (CTA guards)
T1-1 + T1-3 (canonical redirect) → T3-x (empty state fixes)
T5-1 (terminology) → independent, parallel track
T6-x (nav polish) → independent, parallel track
T8-x (display polish) → independent, parallel track
T9-x (hide operator routes) → can start immediately, no blocking dependencies
T10-1 (auth pages) → independent
```

The highest-leverage move is **T1-1**. It is a small targeted change to `operator-static-demo.ts`
and the four or five server pages that call `tryStaticDemo*` functions. It unblocks more of the
OpenAI P0 list than any other single change.
