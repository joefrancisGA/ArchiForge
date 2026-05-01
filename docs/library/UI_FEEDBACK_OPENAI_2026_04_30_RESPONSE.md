> **Scope:** Internal engineering response to the OpenAI UI feedback pass (2026-04-30, 100 items); captures agreement, disagreement, and a prioritized follow-up todo list. Not a design spec or external communication.

---

# ArchLucid UI Feedback — OpenAI 2026-04-30 Response

**Received:** 2026-04-30 8:24 PM EDT  
**Reviewed by:** Engineering  
**Items reviewed:** 100 (50 P0 demo blockers, 50 P1 commercial polish)

---

## Summary Verdict

The feedback is largely accurate and grounded in real product state. The
reviewer correctly identified that broken demo routes undercut the entire
buyer narrative, and that the gap between "completed" claims in the Reviews
page versus empty Audit/Alerts/Graph/Ask pages is commercially damaging.

We agree with **roughly 85 of the 100 items outright**. The remaining 15
or so are either already addressed in code, partially misstated, or
represent genuine design disagreements explained below.

---

## What We Already Have In Code

Before treating every item as net-new work, note what exists:

| Area | What exists |
|---|---|
| Demo ID aliasing | `demo-run-canonical.ts` — `claims-intake-modernization-run` already maps to canonical `claims-intake-modernization` |
| Static fallback | `operator-static-demo.ts` serves a curated Claims Intake row when the API fails; `tryStaticDemoRunSummariesPaged` prevents the "no runs" empty state on the Runs list |
| Nav hiding in demo mode | `nav-shell-visibility.ts` already omits `/admin/health`, `/admin/support`, `/admin/users`, `/policy-packs`, `/governance-resolution`, `/governance`, `/audit`, `/alerts`, `/planning`, `/product-learning`, `/recommendation-learning`, `/integrations/teams`, `/digests`, `/settings/tenant` from public demo nav |
| Terminology | The Runs page `<title>` and `<h1>` already say "Architecture reviews" |
| Workspace label | The project label already renders as "Sample workspace" in `NEXT_PUBLIC_DEMO_MODE` |

This means several items — particularly 45–48 (hide Admin from demos), 37
(hide Policy Packs), 79 nav labeling, and parts of 71–76 — are **already
coded and the question is verification/wiring, not net-new design**.

---

## Strong Agreement — Act Immediately

These are correct and unambiguous. The codebase evidence supports the
reviewer's diagnosis.

### P0 Group A — Broken Route / Data Mismatch (Items 1–4, 11, 16, 19, 25, 28)

**Agree fully.** The alias wiring in `demo-run-canonical.ts` exists but
needs to be verified end-to-end through the `[runId]` server page,
`FindingDetail`, and `Provenance` routes. The canonical ID (`claims-intake-modernization`)
may differ from what the static fallback payload uses in those three routes.
Items 11 (inconsistent IDs) and 16 (Home "no runs" contradiction) are both
symptoms of the same root cause: static demo fallback is applied on the
Runs list page but may not be applied to the Home page's `RunsDashboardPanel`.

**Action:** Trace the full route chain for `/runs/claims-intake-modernization`,
`/runs/{id}/findings/phi-minimization-risk`, and `/runs/{id}/provenance`.
Confirm static fallback fires for each.

### P0 Group B — "Do Not Link to Broken" (Items 5–10)

**Agree fully.** Linking to a known-broken route is strictly worse than no
link. Every "Open review" CTA that points to a route that renders
"architecture review could not be loaded" is a demo killer. The fix is
conditional: either fix the routes first (Group A), or guard the CTAs
behind a demo-mode check that either hides them or points to the static
Showcase page.

### P0 Group C — Fake Health Status (Item 12)

**Agree fully.** "Platform services: Healthy" on a page that fails to
load its core content is incoherent and erodes trust. A broken content
route should either suppress the health badge or show an honest degraded
state.

### P0 Group D — Home/Graph/Ask/Governance Empty While Reviews Shows Finalized (Items 16, 19–22, 25–30, 40–42)

**Agree fully.** These are all variants of the same architectural gap:
the static demo fallback is applied selectively (Runs list gets it;
Home, Graph, Ask, Governance selectors do not). The `tryStaticDemoRunSummariesPaged`
pattern should propagate to every page that renders a run selector.
The auto-select behavior (items 20, 26, 29) is the right UX answer once
the selector is populated.

### P0 Item 15 — Error Title

**Agree.** "This architecture review could not be loaded" is honest for
production. For demo mode it should be "Sample review temporarily
unavailable — [View sample walkthrough →]". One conditional, high value.

### P1 — Runs → Reviews Rename (Items 71–76)

**Agree.** The URL slug is still `/runs/*` even though the page heading
already says "Architecture reviews." The mismatch is confusing when URLs
appear in screenshots, link shares, or logs. This is a well-defined rename
with known scope: URL segments, breadcrumbs, copy strings. It is not a
trivial change (it touches every internal link), but it is a correct one.

### P1 — Artifact Business Labels (Items 67–68, 97)

**Agree.** `.md`, `.json`, `text/plain`, `application/json`, and raw
filenames are implementation vocabulary. Sponsors do not buy manifest files;
they buy "Decision record," "Sponsor brief," "Evidence bundle." The label
map should live in one place and render everywhere artifacts are listed.

### P1 — Timestamp and Raw ID Hygiene (Items 64–66, 90, 99)

**Agree.** Raw slugs and UTC timestamps on public demo pages read as developer
output. The "4 months ago" relative timestamp on the sample review is a
self-inflicted wound; a fixed business-friendly date ("January 2026") reads
far better. The sample workspace label `claims-intake-sample-workspace`
should not appear anywhere a sponsor sees it.

---

## Partial Agreement — Right Direction, Wrong Prescription

These items identify real problems but the proposed remedy is either
already done, too broad, or would trade one problem for another.

### Item 13 — "Suppress the full app shell on fatal content-load pages"

**Partially agree.** Showing the full nav chrome on a broken route does
create a jarring mismatch. However, completely suppressing the shell on
error routes risks disorienting users who landed on the broken page from
a bookmark or share link and need the nav to recover. The better fix is
(a) quality error copy with a working recovery CTA (item 14–15), and (b)
hiding the "Platform services: Healthy" badge (item 12). Full shell
suppression adds significant complexity for marginal benefit once the error
page is honest and recovery is one click away.

### Item 32 — "Remove 'coming soon' from authenticated demo routes"

**Partially agree on mechanism; disagree on the word 'remove.'** Removing
"coming soon" labels does not fix the underlying problem — incomplete pages
are still empty or thin. The correct fix is hiding those routes in demo mode
(already done in `nav-shell-visibility.ts` for most of them). If a route is
hidden, the "coming soon" label is never seen. Where routes are still
visible despite being incomplete, hiding them is preferable to scrubbing
labels from pages that will never be seen in the demo.

### Item 44 — "If alerts cannot show a concrete signal, hide the Alerts route"

**Agree on hiding; partially disagree on the binary.** Hiding is the right
short-term call given `nav-shell-visibility.ts` already has `/alerts` in the
demo omit set. The mid-term answer is one populated sample alert tied to the
PHI finding, which the reviewer correctly identifies. Mark this as a
populated-or-hidden gate.

### Item 79 — "Simplify the left navigation; '11 more analysis links' is not commercial-grade"

**Agree on copy; disagree on architecture.** The progressive disclosure
tiers (Core Pilot → Advanced → Enterprise) are intentional and architecturally
sound for the product's upsell story. Collapsing all tiers would eliminate
a key packaging mechanism. The issue is label copy: "11 more analysis links"
is a raw count, not a business label. Replace with "Advanced analysis" or
"Analysis suite" as the toggle label. The tier structure itself should stay.

### Item 82 — "Hide or restyle the 'Jump...' command palette"

**Disagree on hiding; agree on restyling.** A command palette is a
genuine enterprise productivity feature that technical buyers recognize
as a sign of product maturity. The problem is that the current visual
treatment looks like a developer search box. Restyle with a professional
label and keyboard hint that reads as intentional power-user tooling.
Do not hide it.

### Item 90 — "Reviews list should not show '4 months ago'"

**Partially agree on the symptom; the fix is different.** Relative timestamps
are standard UX. The real problem is that "4 months ago" reveals the sample
hasn't been refreshed. Fix: update the static payload's `generatedUtc`
to a recent date. In `showcase-static-demo.ts`, `GENERATED_UTC` is hardcoded
to `2026-01-15T14:30:00.000Z`; update it (or derive it relative to build
time) so it reads "last week" or similar. Do not remove relative timestamps
globally.

### Item 100 — "Create a strict demo-safe route allowlist"

**Directionally agree; execution note.** The nav already implements a
demo-mode omit set. The gap is that the omit set is missing some routes
(Search, Replay, Evolution Review, Digest Subscriptions, Settings Exec Digest,
Teams Integration, Approval Lineage, Policy Pack Detail) and the approach
is opt-out rather than opt-in. Flipping to an opt-in allowlist is cleaner
for demo safety and is the right long-term architecture. However, some
enterprise pilot buyers want to see governance and audit surfaces even in
early demos; the allowlist should be configurable per demo tier, not a
single hard list.

---

## Disagreement

These items are either already handled, overstated, or represent a design
choice we should not reverse.

### Items 45–48 (Hide Admin from demos)

**Already done.** `nav-shell-visibility.ts` already omits `/admin/health`,
`/admin/support`, and `/admin/users` from the demo mode nav. The review
confirms this is the right call; the implementation is already there.
Verification task: confirm these routes also 404 or redirect if hit directly.

### Items 37–39 (Policy Packs)

**Already hidden in nav.** `/policy-packs` is in the demo omit set.
The raw JSON content is not buyer-visible if the route is unreachable from
demo nav. If the reviewer reached it via direct URL, that's a nav-is-not-enough
gap — add a demo-mode guard at the page level as well.

### Item 69 — "'Offline snapshot' language weakens the real manifest claim"

**Conditional agreement.** The warning is honest and legally appropriate
for a page that is not pulling live data. The copy could be softened to
"Stable sample — finalized 15 January 2026" without lying about the data
source. Removing the disclaimer entirely is not recommended.

### Item 70 — "'Sample only' warning dominance"

**Disagree.** The warning should appear, but after the headline value
statement, not before it. This is a layout issue, not a content issue.
Move the disclaimer below the first section heading; do not remove it.

---

## Items Outside Our Control

- **Item 49–50 (Auth pages):** Auth page polish depends on whether the
  identity provider integration is live. If auth is partially configured,
  the UI reflects that correctly. This is an ops decision, not a UI fix.

- **Item 58 (Planning pages):** Planning is explicitly scoped out of V1
  per `V1_SCOPE.md`. The fix is adding it to the demo nav omit set.

---

## Todo List

See the companion todo section below, organized by priority tier and
owning area.

### Tier 1 — Fix Before Next Demo (P0 route integrity)

- [ ] **Trace `/runs/claims-intake-modernization` through `[runId]` server page**: confirm static fallback fires and renders run detail correctly
- [ ] **Trace `/runs/{id}/findings/phi-minimization-risk`**: confirm `FindingDetail` page uses static payload or redirects to a working summary
- [ ] **Trace `/runs/{id}/provenance`**: confirm `Provenance` page uses static fallback
- [ ] **Guard "Open review" CTAs in Reviews list, drawer, Demo Preview, and Live Demo** behind a working-route check; in demo mode redirect to Showcase if Run Detail fails
- [ ] **Guard "Review finding" CTA in Demo Preview** against broken finding route; point to static finding summary
- [ ] **Fix Home "no runs" contradiction**: wire `RunsDashboardPanel` to use the same static fallback as the Runs list page when in demo mode
- [ ] **Fix Graph selector "no runs"**: auto-populate with canonical sample run in demo mode
- [ ] **Fix Ask selector "no runs"**: auto-populate and auto-select canonical sample in demo mode; disable suggested prompts until context is loaded
- [ ] **Fix Governance selector "no runs"**: auto-populate; show sample approval workflow
- [ ] **Suppress "Platform services: Healthy" badge on any page where core content fails to load**
- [ ] **Update error copy on demo-mode broken routes**: "Sample review temporarily unavailable" + working fallback CTA

### Tier 2 — Demo Safety Sweep (P0 remaining)

- [ ] **Add to demo nav omit set**: `/search`, `/replay`, `/evolution-review`, `/digest-subscriptions`, `/settings/exec-digest`, `/integrations/teams`, `/governance/approval-lineage`
- [ ] **Add page-level demo guard** to `/policy-packs` (not just nav omit — block direct URL access)
- [ ] **Audit `Governance Findings` empty state**: connect to static sample findings in demo mode or hide the Findings tab
- [ ] **Populate Audit with sample events** (request submitted → context captured → graph created → findings generated → manifest finalized → artifacts bundled); use `demo-audit-sample-events.ts` (file already exists — verify it wires into the Audit page)
- [ ] **Create one sample Alert tied to PHI finding** in `operator-static-demo.ts`; wire into Alerts page in demo mode
- [ ] **Update `GENERATED_UTC` in `showcase-static-demo.ts`** from `2026-01-15` to a more recent value (or derive from build date) so relative timestamps read naturally
- [ ] **Fix Homepage CTA path** for "See completed example" — route only to working Showcase page, not Run Detail
- [ ] **Fix Graph page** to auto-load the Claims Intake sample graph in demo mode; remove "No graph on screen yet" and "No runs" language

### Tier 3 — Terminology and Copy (P1)

- [ ] **Complete `/runs` → `/reviews` URL rename**: update all internal `href` links, breadcrumbs, and any canonical URL references; confirm redirect from old `/runs/*` paths
- [ ] **Rename nav heading "RUNS" to "Architecture reviews"**
- [ ] **Rename all "filter by run id" / "run id or description" copy** → "filter by review name or description"
- [ ] **Rename "Compare two finalized runs"** → "Compare two finalized reviews"
- [ ] **Rename "Run required for a new conversation"** → "Select an architecture review"
- [ ] **Remove "Operate —" prefix** from buyer-visible labels ("Operate — governance", "Operate — analysis")
- [ ] **Rename "thread" language** in Ask/Graph/Governance to "conversation" or "review context"
- [ ] **Rename progressive disclosure toggle label** from "11 more analysis links" to "Advanced analysis" (or equivalent)
- [ ] **Restyle the "Jump..." command palette** to read as intentional power-user tooling, not a dev search box
- [ ] **Rename "Sample workspace"** to "Claims Intake Demo Workspace" or similar in demo mode public label
- [ ] **Rename "Rule set" in Manifest** → "Policy pack" (e.g., "Healthcare Claims Policy Pack v3.4.1")
- [ ] **Rename generated artifact filenames** to business labels: "Sponsor brief," "Decision record," "System diagram," "Evidence bundle" — centralize in one label map
- [ ] **Remove MIME types** (`application/json`, `text/plain`, `text/markdown`) from all sponsor-facing artifact tables
- [ ] **Move manifest technical identifiers** (run ID, manifest UUID) below the executive conclusion section
- [ ] **Fix "See It" / offline snapshot copy**: replace "stable offline snapshot" with "Stable sample — finalized January 2026"; move sample-data disclaimer below the first value heading

### Tier 4 — Structural Polish (P1, lower urgency)

- [ ] **Audit the homepage CTA set**: trim to New Review, See completed example, and Take tour; demote First Manifest Guide, Compare, Replay, and Graph to secondary context
- [ ] **Update homepage example request copy** from corporate PDFs / environmental standards → healthcare claims intake (align with flagship demo)
- [ ] **Fix Reviews side-panel title truncation** for "Claims Intake Modernization — sample c..."
- [ ] **Remove "Artifact bundle attached — see run detail"** from Reviews panel while Run Detail is broken
- [ ] **Update Compare page raw IDs** (`claims-intake-run-v1`, `claims-intake-run-v2`) to business labels; fix "100 to 120" cost delta to credible financial language
- [ ] **Remove "Navigation settings" and "open navigation settings" links** from normal buyer-facing nav
- [ ] **Remove raw project/workspace names** (`claims-intake-sample-workspace`) from all public demo pages
- [ ] **Planning: add `/planning` and `/evolution-review` to demo nav omit set** (V1 deferred scope)
- [ ] **Learning pages: add `/product-learning` and `/recommendation-learning` to demo nav omit set** (already present — verify)
- [ ] **Digest Subscriptions and Digests: verify both are in demo nav omit set and page-guarded**

---

## Assessment of Feedback Quality

The reviewer did a thorough walkthrough and caught genuine gaps. The
highest-value observation is the systemic one underlying items 16, 19, 25,
28, 40–42: the static demo fallback exists and works on the Runs list page
but has not propagated to every page that renders a run selector or a
run-scoped data view. That is the single root cause behind more than a
dozen distinct demo failure modes.

The weakest items in the feedback are:
- Items 45–48 (already coded)
- Item 82 (command palette should stay; it is a signal of product maturity)
- Items 69–70 (sample disclaimer is appropriate — layout fix, not removal)
- Item 13 (shell suppression on error pages is complex for low gain)

The feedback does not account for the V1 deferred scope items (planning,
learning, replay, evolution review), which are correctly marked as
out-of-scope in `V1_SCOPE.md` and should be addressed by hiding routes,
not by completing the features before V1.
