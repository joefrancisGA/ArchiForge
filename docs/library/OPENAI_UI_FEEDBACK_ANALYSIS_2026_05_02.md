> **Scope:** Internal triage of OpenAI UI feedback against `archlucid-ui` source for engineering prioritization; not a product roadmap, design spec, or customer-facing summary.

# OpenAI UI Feedback Analysis — 2026-05-02

**Feedback timestamp:** 2026-05-02 12:49 AM EDT  
**Analysis by:** ArchLucid engineering (Cursor agent)  
**Source read:** Live code — `archlucid-ui/src/` and `archlucid-ui/src/lib/`

---

## Overview

100 items were flagged, spanning P0 (demo blockers) and P1 (polish). After reading the
actual source code, roughly **37 items are real code bugs or copy issues we can fix**,
**28 are genuine product/UX design concerns worth discussing**, and **35 appear to be
artefacts of the screenshot capture running without a live API**.

The three-way split matters because applying the full list without that lens would waste
cycles fixing things that self-resolve when the API is connected.

---

## What I Agree With (code-confirmed)

### A. "run" → "review" migration is incomplete (items 38, 39, 40, 76–80)

**Confirmed in source.** `enterprise-controls-context-copy.ts` still contains:

```
"Approval requests for this run"          // lines 137, 139 — govWorkflowApprovalRequestsCard*
"Load a run to see approvals…"            // line 125 — govWorkflowQueryCardDescriptionOperator
"Inspect how a run moved through governance. Load a run below…"  // line 129 — reader
"Load a run to see its approval requests. Approving, promoting, and activating require elevated permissions."  // line 303
```

`GraphIdleLegend.tsx` line 50:
```
"may paginate on very large runs"
```

`layer-guidance.ts` lines 68–74:
```
"drift or integrity checks on a single run, not a visual diff."
"does stored pipeline output still validate for this run on replay?"
"how does provenance or architecture look for one run?"
```

`operate-analysis-nav-group-builder.ts` line 32:
```
"Review-trail or architecture graph for one run"
```

`help-topics.ts` line 58:
```
"Graph shows one run's provenance or architecture view for a single run ID."
```

All of these were **missed in the run→review rename sweep**. OpenAI is right on every
specific example they cite in items 38–40 and 76–80.

---

### B. "Defer until after Pilot proof" is visible to buyers (item 28)

**Confirmed in source.** `layer-guidance.ts` line 76:

```typescript
graph: {
  firstPilotNote: "Defer until after Pilot proof when a graph answers the question.",
```

This note is rendered in the `LayerHeader` component for every visitor to the Graph page.
It reads as internal product-roadmap guidance. The fix is to reword it to a
*when-you-need-it* customer benefit statement.

---

### C. "Operate" as a buyer-facing label is leaking internal taxonomy (item 29, item 81)

**Partially agree.** "Operate" is the intentional product layer name in
`PRODUCT_PACKAGING.md`. The `LayerHeader` badge shows "Operate" on compare, replay,
graph, governance, policy-packs, audit, alerts pages. This is a design decision, not an
accident. **However**, OpenAI is right that in a screenshot-captured state, a bare
"Operate" badge without any preceding "Pilot" context reads as jargon. A buyer who lands
on Graph for the first time sees "Operate · Graph" with no orientation. Softening the
badge label to "Analysis" for the analysis slice and "Governance" for the governance slice
would keep the internal structure while being more self-explanatory to buyers.

---

### D. "Elevated permissions" and "Execute access" in primary copy (items 43, 44)

**Confirmed in source.** `enterprise-controls-context-copy.ts`:

- Line 15: `"write actions require elevated permissions."` (shown in alert tooling)
- Line 303: `"Approving, promoting, and activating require elevated permissions."`  
- Governance page `page.tsx` line 679: `"Submitting for governance approval requires Execute access on your account."`

OpenAI's ask is right: secondary note is fine, but it should not be the **first** thing a
buyer-role user reads on a page.

---

### E. Raw review ID exposed in Governance main section (item 39)

**Confirmed in source.** Governance `page.tsx` line 963:

```tsx
Review <span className="font-mono">{activeRunId ?? "—"}</span> · promotions newest first
```

This renders `claims-intake-modernization` (or whichever run ID is loaded) in monospace
in the main section body — not inside an "Audit identifiers" collapsible. Should be
replaced with the display name or moved to a collapsed technical section.

---

### F. Promotion records show raw UUID in `CardDescription` (item 41)

**Confirmed in source.** Governance `page.tsx` line 988:

```tsx
<CardDescription className="font-mono text-xs">{p.promotionRecordId}</CardDescription>
```

The `promotionRecordId` UUID is in the default card subtitle position, not in a
collapsed technical details section. For the approval request card, the ID is in an
`sr-only` element (correct), but not for promotion records.

---

### G. Graph dynamic-import loading message is screenshot-unsafe (items 23–26)

**Confirmed in source.** `graph/page.tsx` lines 38–48:

```tsx
const GraphViewer = dynamic(
  () => import("@/components/GraphViewer").then((m) => m.GraphViewer),
  {
    ssr: false,
    loading: () => (
      <OperatorLoadingNotice>
        <strong>Loading graph viewer.</strong>
        <p className="mt-2 text-sm">Preparing the interactive canvas…</p>
      </OperatorLoadingNotice>
    ),
  },
);
```

When the static demo provenance graph loads (which happens on mount with
`SHOWCASE_STATIC_DEMO_RUN_ID`), it sets `graph` to a non-null value, which triggers
`GraphViewer` to render — but Next.js's `dynamic` shows the loading fallback while the
JS bundle resolves. A screenshot taken in that ~500 ms window will always show
"Loading graph viewer. Preparing the interactive canvas…" even though data is available.
This is a **real screenshot-safety bug**, independent of whether the API is running.

The fix is to either: (a) delay the screenshot past graph render, or (b) add a
`useEffect` that stores a `graphReady` flag and only renders `GraphViewer` after a first
paint, suppressing the dynamic-import flash.

---

### H. Ask — "No messages yet" when API is down is structural but real (items 32–33)

**Partially confirmed.** `ask/page.tsx` auto-selects the first thread only when
`listFailure === null`. Without the API, `listConversationThreads()` throws, sets
`listFailure`, and the auto-select never runs. `runId` is pre-seeded to
`SHOWCASE_STATIC_DEMO_RUN_ID` but there are no static demo messages — the messages list
stays empty. **Without the API this cannot show a sample answer.** The fix requires
either static demo messages seeded into `operator-static-demo.ts`, or an alternative
"demo conversation" fallback.

---

### I. Ask review picker shows stale/mismatched label (item 30)

**Confirmed logically.** When a thread is selected and its `runId` is set into state, the
`AskRunIdPicker` must resolve that ID to a display name. If the run list API is down and
the picker fetches runs dynamically, the display name doesn't resolve and the dropdown
shows the placeholder "Choose an architecture review" while the runId is actually set in
state. The **Ask button is not disabled** (because `runId` is non-empty), but the picker
looks empty. This creates the contradiction OpenAI observed.

---

### J. Home CTA overload (item 87)

**Agree, and this is code-confirmed.** `operator/page.tsx` composes:
`WelcomeBanner`, `OperatorNextActionsCard`, `RunsDashboardPanel`,
`OperatorCorePilotDiagnosticsChecklist`, `AfterCorePilotChecklistHint`,
`HomeFirstRunWorkflowGate`, plus conditional `PilotOutcomeCard`, `OperatorTaskSuccessTile`,
`BeforeAfterDeltaPanel`, `HomeMaturityLayerCards`. For a first-visit user these
simultaneously present 8–12 call-to-action paths. OpenAI is right that they compete.

---

## What I Disagree With (or Have a Nuanced Position On)

### 1. "Suppress the full app shell around fatal route failures" (item 10)

**Disagree.** Hiding the navigation shell when a sub-route fails is worse UX, not better.
The user has no way to navigate away. The correct fix is to make the route work (via the
static demo fallback or better error messaging), not to strip the shell. The shell staying
visible is not a bug — it is the escape hatch.

---

### 2. "Stop showing Platform services: Healthy on failed routes" (items 9, 16)

**Disagree with the framing, agree with the symptom.** "Platform services: Healthy" in
the footer reports infrastructure status (API health endpoint), not whether any particular
sample page rendered correctly. An API that is online and returning 4xx for a bad slug is
legitimately "healthy." The health indicator is not broken; the route is. Hiding the
indicator would be misleading in the other direction (a failing API would also look hidden).

If the concern is that buyers interpret it as "the whole product is fine," the fix is
better error copy on the route itself, not removing the indicator.

---

### 3. "Replace Retry as the primary action on deterministic sample failures" (item 11)

**Agree with the intent, disagree with the diagnosis.** In `reviews/[runId]/page.tsx`,
when the API fails and the static demo fallback also fails, the page shows `OperatorApiProblem`
plus a "← Back to reviews" link. There is **no Retry button** at the page level for this
failure path. The `OperatorSectionRetryButton` appears only for section-level failures
(timeline, artifacts). The complaint may be about a *specific* screenshot state that
OpenAI captured with a different code version. Based on current code, the primary action
on a fatal review-detail failure is already "Back to reviews."

---

### 4. "Disable Ask's Ask button until a review is selected" (item 34)

**Disagree.** The Ask button is already correctly disabled when `runMissing === true`
(no `runId` and no selected thread). And `runId` is pre-seeded to
`SHOWCASE_STATIC_DEMO_RUN_ID` so the button is not disabled by default. The *visual
mismatch* (picker shows "Choose a review" but button is enabled) is the real bug —
fixing the picker display label is the right solution, not further disabling the button.

---

### 5. "Create a strict demo-safe allowlist — hide everything else" (item 100)

**Disagree.** ArchLucid is a real product with real pages. Wholesale hiding of Audit,
Alerts, Governance sub-routes, and Policy Packs does not fix the underlying issues —
it creates a potemkin demo. The right approach is: fix the pages that are broken, populate
the ones that are thin, and accept that a sophisticated buyer evaluating enterprise
governance *should* see those pages. The allowlist approach would also break navigation for
real pilot customers.

---

### 6. "Remove 'one run' and 'large runs' from Graph" (items 27, 80) vs LayerHeader "Operate"

**Partially disagree.** "Operate" as a section badge is intentional product architecture
(not accidental jargon). The badge should remain but be relabeled more buyer-friendly
(see item C above). "one run" is a legitimate technical term in the help text context —
the concern is the buyer-facing heading/idle description, not the technical aria label.
Specific instances confirmed as copy bugs; not a wholesale removal of the concept.

---

### 7. "Make the fatal error more operationally specific" (item 12)

**Agree with intent, but the code already does this.** `OperatorApiProblem` renders the
HTTP problem detail (title, status, type, correlationId) when it is available. Without
the API running, there is no ProblemDetails payload — just a network error. The
specificity OpenAI asks for (which dependency failed: API, seed, route mismatch) would
require detecting those cases at the component level. This is a valid but lower-priority
enhancement, not a current bug.

---

### 8. "Hide Governance Dashboard, Policy Packs, Lineage unless fully populated" (items 47–53)

**Partially agree.** Policy Packs showing `Registered packs 0` and `Effective layers 0`
when the sample uses `Healthcare Claims Policy Pack v3.4.1` IS a real contradiction
(item 51–52). That is a static demo data gap. But hiding the pages entirely removes the
governance story from the demo entirely. The fix is to seed the static demo data, not
hide the pages.

---

### 9. "Development → Test" vs "Development → Staging" inconsistency (item 42)

**Nuanced.** In `governance/page.tsx`, `ENV_OPTIONS` maps `"test"` to label `"Staging"`.
The default submit path would show "Development → Staging." If OpenAI saw
"Development → Test" somewhere, it was either a static demo data fixture (the approval
record showing the raw `sourceEnvironment`/`targetEnvironment` API values without going
through `governanceEnvironmentPairDisplay`) or an older screenshot. The display mapping
is correct in current code; the static demo fixture needs to verify its environment values.

---

## Items That Are API-Not-Running Artefacts

These items describe symptoms that would self-resolve when the API is connected and the
sample data is seeded. They are **not code bugs** in the UI layer.

| # | Item | Why it is API-dependent |
|---|------|------------------------|
| 1 | Review detail cannot load | `tryStaticDemoRunDetail` covers known slugs; any other slug needs the API |
| 2 | PHI finding route fails | Finding page fetches finding detail from API; no static fallback for finding detail |
| 3 | Provenance route broken | `tryStaticDemoProvenanceGraph` exists but only for provenance-full mode |
| 4 | Inspect route fails | Inspect page calls the finding inspect API endpoint |
| 5 | "Open review" CTAs unsafe | Consequence of item 1 |
| 6 | "Review finding" CTAs unsafe | Consequence of item 2 |
| 13 | Home implies no reviews exist | `RunsDashboardPanel` calls the runs list API; empty without seeded data |
| 14 | Home "Latest in workspace" empty | Same as item 13 |
| 15 | Home "See completed example" route | Route target depends on which routes load |
| 30* | Ask review selector mismatch | Thread list from API; *also a UI display bug — see item I above* |
| 31 | Ask conversation doesn't populate review | Thread runId resolved only if thread API returns data |
| 32 | Ask shows "No messages yet" | No static demo messages in current code |
| 33 | Ask prompt chips no sample answer | Same as item 32 |
| 35 | Stale Ask timestamps | Static demo messages not seeded; real API would return real timestamps |
| 36 | UTC formatting on Ask cards | Would render live threads with locale-formatted dates |
| 46 | Governance Findings empty | Findings queue calls API; no static fallback |
| 57 | Audit Log zero events | Audit API not running; no static audit trail seeded |
| 58 | Audit trail not populated | Same as item 57 |
| 59 | Audit not defaulted to sample | Default filter still calls API |
| 63 | Alerts shows no matching alerts | Alerts inbox calls API; no static alert seeded |
| 64 | No sample alert tied to PHI finding | Would require API-side alert seeding |
| 65 | Empty Alerts tabs | Tab content calls API endpoints |
| 68 | Alerts "View reviews" broken routing | Consequence of item 1 |
| 69 | Admin Health operator-only | This IS a valid "should be hidden from demos" call even with the API |
| 71 | Admin Users empty | User list calls API |

> Items 30 and 35 are both API-dependent *and* have independent UI bugs documented above.

---

## Confirmed Disagreements Summary

| Item | Our position |
|------|-------------|
| 10 | Keep app shell on error pages |
| 9, 16 | Keep health indicator; fix the route |
| 11 | Current code already uses "Back to reviews" not "Retry" |
| 34 | Fix picker display; don't further disable the button |
| 100 | Fix pages; don't hide them |

---

## Todo List

Ordered by impact. Items marked **[quick]** are single-file copy changes. Items marked **[data]** require seeding static demo fixtures. Items marked **[design]** need a product decision before coding.

### P0 — Demo safety (fix before next screenshot session)

- [ ] **`enterprise-controls-context-copy.ts`** [quick]: Replace all "for this run" → "for this review"; "Load a run" → "Load a review"; "elevated permissions" → "approval rights" in governance copy (items 38–40, 43)
- [ ] **`layer-guidance.ts`** [quick]: Replace `firstPilotNote` for `graph` — change "Defer until after Pilot proof…" to a buyer-facing benefit statement like "Best used after you have a committed review — visual exploration complements the manifest and finding tables" (item 28)
- [ ] **`GraphIdleLegend.tsx`** [quick]: Replace "very large runs" → "very large reviews" (item 80)
- [ ] **`operate-analysis-nav-group-builder.ts`** [quick]: Replace "one run" → "one review" in shortcut title (item 80)
- [ ] **`help-topics.ts`** [quick]: Replace "one run's provenance" → "one review's provenance" (item 77)
- [ ] **`governance/page.tsx`**: Move raw `activeRunId` out of the visible section body — either use a display name or wrap in a collapsible "Audit identifiers" (item 39)
- [ ] **`governance/page.tsx`**: Move `promotionRecordId` from `CardDescription` to a collapsed "Audit details" section to match how `approvalRequestId` is already handled in `sr-only` (item 41)
- [ ] **`graph/page.tsx`**: Add a `graphReady` state flag so `GraphViewer` only mounts after the first data load resolves — prevents "Loading graph viewer." from being captured in screenshots (items 23–25)
- [ ] **`layer-guidance.ts`** [design]: Relabel `layerBadge: "Operate"` to `"Analysis"` on compare/replay/graph and `"Governance"` on governance/policy-packs/audit/alerts — keeps internal architecture but is self-explanatory to buyers (items 29, 81)

### P1 — Copy and terminology (high-signal buyer polish)

- [ ] **`layer-guidance.ts`** [quick]: Replace remaining "for this run" / "a single run ID" with "for this review" / "a review" in `replay.useWhen` and `graph.headline` (items 77, 79)
- [ ] **`enterprise-controls-context-copy.ts`** [quick]: Replace "elevated permissions" with "approval rights" or "approver role" in all visible copy; move any remaining permission-jargon to secondary `<p>` below the primary label (item 43)
- [ ] **`governance/page.tsx`** [quick]: Replace `"Execute access"` (line 679) with `"approver access"` and move to secondary note (item 43)
- [ ] **`ask/page.tsx`** [data]: Seed at least one static demo conversation with messages in `operator-static-demo.ts` so Ask can show a sample exchange without the API (items 32–33)
- [ ] **`ask/page.tsx`** [quick]: When `listFailure !== null`, show a static demo conversation thread fallback instead of a blank history panel (item 32)
- [ ] **`AskRunIdPicker`** component: When the resolved display name for a thread's pre-seeded runId cannot be loaded, show `"Claims Intake Modernization"` as the fallback label rather than the empty placeholder (item 30)
- [ ] **Static demo governance fixture**: Verify `sourceEnvironment`/`targetEnvironment` values use `"dev"`/`"test"` keys so `governanceEnvironmentPairDisplay()` renders "Development → Staging" consistently (item 42)
- [ ] **Policy Packs static demo data** [data]: Seed `Registered packs: 1` and `Effective layers: 1` for Healthcare Claims Policy Pack v3.4.1 so the registry is not empty when the sample manifest references it (items 51–52)
- [ ] **Home page** [design]: Reduce competing CTAs — prioritize one primary action per state (empty-state vs has-reviews vs has-committed-review) and demote secondary actions to a "more actions" card (item 87)
- [ ] **Governance `page.tsx`**: Clarify demo/read-only mode — add a single "Viewing sample — this approval record is read-only" banner when static demo data is active, so submit/promote button states are contextually explained (items 44–45)

### P2 — Nav and shell scaffolding

- [ ] **`layer-guidance.ts`** [quick]: Fix `replay.useWhen` "a single run" → "a single review" and `replay.headline` "for this run on replay" → "for this review on replay"
- [ ] **Navigation settings visibility** [design]: Evaluate gating "Navigation settings" behind an admin role or removing it from the default nav (item 83)
- [ ] **"W: Workspace / P: Default"** [design]: Replace workspace/project label in top bar with the display name from the workspace context (item 86)
- [ ] **Reviews drawer title** [quick]: Fix truncation of "Claims Intake Modernization — sample c…" — either extend the allowed length or truncate with a tooltip (item 90)
- [ ] **Manifest artifact labels** [quick]: Rename "Decision record JSON Bundle" → "Decision record", "System diagram Diagram" → "System diagram", "Sponsor brief Markdown report" → "Sponsor brief" — remove the MIME type echoes from visible labels (items 94–95)
- [ ] **Admin Health / Admin Support** [design]: Gate these pages behind `AdminAuthority` in `nav-config.ts` so they do not appear for standard demo users (items 69–70)

---

## Notes on "See It" and Public Demo Route Sprawl (items 96–99)

Items 96–99 about See It, Demo Preview, Live Demo, Showcase, Demo Explain overlap are
valid observations. These public marketing routes do have overlapping value propositions.
This is a **product/marketing decision** not a code change — they serve different
audiences (direct link, embedded iframe, showcase presentation). A consolidation decision
should come from product before engineering acts.

---

*Saved to `docs/library/OPENAI_UI_FEEDBACK_ANALYSIS_2026_05_02.md`*
