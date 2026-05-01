> **Scope:** Internal engineering triage of external OpenAI-operator UI feedback (round 2) — assessment, agreed items, and task references; not procurement commitments or a substitute for staging/prod smoke verification.

# OpenAI UI Feedback Round 2 — Assessment & To-Do List
**Received:** 2026-05-01 ~5:00 AM EDT  
**Previous round:** 2026-05-01 4:12 AM EDT  
**Assessed:** 2026-05-01

---

## Important context: T1-1 already shipped

The T1-1 implementation from the previous session (`isStaticDemoPayloadFallbackActiveForRun`) removed the env-var requirement for all `tryStaticDemo*` helpers on known showcase run IDs. Items 1–4 in this round describe the same failures reported in round 1. **If the deployment was updated, those routes now serve static content automatically.** If OpenAI tested before the deployment propagated, these are the same issues at a different timestamp.

The following analysis assumes T1-1 **is not yet deployed** and treats items 1–4 as still needing verification.

---

## Where We Agree

### Confirmed accurate

| # | Finding | Evidence / status |
|---|---------|------------------|
| 1–4 | Core review, finding, provenance, inspect routes fail | Root cause fixed by T1-1; requires deployment |
| 11, 14 | Home/Reviews contradiction | T7-1/T7-2 pending |
| 12 | "Resolving checklist links…" | T7-3 pending |
| 19–23 | Graph idle state / "No reviews" / workaround link | Graph auto-loads on demo mode (`isNextPublicDemoMode`), but NOT when env var absent. T3-1 still needed |
| 24–26 | Ask picker empty / auto-select / disabled prompts | T3-2 pending |
| 27–28 | Ask terminology leaks ("run context", "another run") | T5-1 pending |
| 29–31 | Governance empty pickers and findings | T3-3/T3-4 partially fixed (loadLists injects on failure; findings always show static rows on failure); full auto-prefill still requires demo env |
| 39–40 | Policy Packs shows 0 packs, 0 effective layers | `staticDemoPolicyPacksFallbackBundle` still gated on `isStaticDemoPayloadFallbackEnabled()` — not updated in T1-1 |
| 41–43 | Raw JSON, repo paths, Create/Publish/Assign forms on Policy Packs | T9-4 pending |
| 44–46 | Audit shows zero events while review trail claims complete | Partially fixed: failure path now injects without demo env; but success-with-empty path still requires demo env via `auditDemoSampleInjectEnabled()` |
| 47 | "Run ID" filter label in Audit | Confirmed — line 392 in `audit/page.tsx` still reads "Run ID" |
| 48–49 | Internal Audit copy: "Execute+ writes (API-enforced)", "CSV reuses same From/To window" | Confirmed — `layerHeaderEnterpriseOperatorRankLine`, `auditSearchSectionLeadReaderLine` in `enterprise-controls-context-copy.ts` are exposed to buyer-visible Audit UI |
| 50–54 | Alerts empty / internal copy / data-grid message | `alertToolingConfigureSectionSubline` is buyer-visible; `alertsInboxRankReaderLine` says "Execute+"; no sample alert without demo env |
| 55–58 | Admin Health/Support/Users in demo | T9-1 pending |
| 59–67 | Auth, Tenant Settings, Digests, Teams in demo | T9-2/T10-1 pending |
| 68–70 | Planning, Evolution Review, Replay in demo | T9-3 pending |
| 71–76 | "run" → "review" terminology migration | T5-1/T5-2 pending |
| 77–81 | Nav labels, nav settings, thread language | T6-1/T6-2/T6-3 pending |
| 83–84 | Default project / workspace name inconsistency | T6-4 pending |
| 85–88 | Homepage CTA overload, internal release language | T7-1 pending |
| 89 | "4 months ago" in Reviews list | Confirmed — `displayRelativeCreated` only uses calendar format in `isNextPublicDemoMode()`; without env var it falls through to `formatRelativeTime` |
| 90–91 | Side-panel truncation, quick-action clutter | T8-2/T8-3 pending |
| 92–95 | Manifest: counts before conclusion, identifiers, MIME types, artifact names | T8-4/T8-5/T8-6 pending |
| 96–98 | See It: "falls back to a checked-in snapshot" / "Sample only" banner / raw filenames | Confirmed in `see-it/page.tsx` lines 30 and 33–38 |
| 99 | Compare raw IDs and weak cost delta | T8-7 / T6-4 pending |

---

## Where We Disagree

| # | Finding | Our position |
|---|---------|-------------|
| 9 | "Suppress the full app shell around fatal sample-load errors" | **Disagree.** Same as round 1 — removing nav makes recovery harder. Fix: T4-2 (better error state design with prominent escape CTAs). |
| 35/38 | "Hide Approval Lineage" and "Hide Policy Packs from buyer demos" | **Partially disagree.** Same as round 1 — populate the data and strip internals; don't suppress core differentiators. |
| 100 | Strict route allowlist hiding Compare, Ask, Graph | **Disagree.** Make them demo-safe, not invisible. T1-1 was step one; T3-1/T3-2 complete it. |
| 32 | "Fix Governance Dashboard or hide it" | **Redirect.** "Coming soon" is the real bug (T9-3). An empty but honest dashboard is better than hiding the governance story entirely. |

---

## New issues surfaced in Round 2 (not in Round 1 list)

The following are **net-new** and need to be added to the backlog:

- **N1** — Policy Packs 0-count (items 39–40): `staticDemoPolicyPacksFallbackBundle` still gated on env vars; needs same unconditional treatment as `tryStaticDemoRunDetail`
- **N2** — Audit "Run ID" filter label (item 47): one-line rename  
- **N3** — Audit internal copy: "Execute+ writes (API-enforced)" and "CSV reuses same From/To window" (items 48–49)  
- **N4** — Alerts internal copy: "Execute+ config (API)" line and "No rows. Change filter or refresh" (items 53–54)  
- **N5** — See It page: "falls back to a checked-in snapshot", dominant "Sample only" warning, raw artifact filenames (items 96–98)  
- **N6** — Reviews list relative date still falls back to `formatRelativeTime` when `isNextPublicDemoMode()` is false — needs the same `afterAuthorityListFailure`-style pattern (item 89 is wider than previously understood)  
- **N7** — Graph auto-load gated on `isNextPublicDemoMode()` — same gap as T1-1 but for Graph's `setRunId` initialization (items 19–23 partially)

---

## Updated To-Do List

### Already done (T1-1 / T1-3)
- Static fallback for review detail, finding, provenance, inspect, governance approval/promotion, audit failure path — unconditional for showcase run IDs
- Canonical redirect for `claims-intake-modernization-run` alias URL

### Tier 1 — Deploy T1-1 + Close Remaining Fallback Gaps

- [ ] **T1-4 · Extend unconditional static fallback to Policy Packs list and effective pack state**  
  `tryStaticDemoPolicyPacksList`, `tryStaticDemoEffectivePolicyPacks`, and `mergePolicyPacksStateWithStaticDemo` still require `isStaticDemoPayloadFallbackEnabled()`. Apply `isStaticDemoPayloadFallbackActiveForRun(SHOWCASE_STATIC_DEMO_RUN_ID)` (constant true for canonical ID) or add an `afterAuthorityListFailure`-style option.  
  *Resolves: items 39–40*

- [ ] **T1-5 · Extend Graph run initialization to activate on showcase slug without demo env**  
  In `graph/page.tsx`, `setRunId` initialization is gated on `isNextPublicDemoMode()`. Change to also initialize when the static fallback is active for the canonical run, and trigger `performGraphLoad` on mount when provenance mode is selected.  
  *Resolves: items 19–23*

- [ ] **T1-6 · Extend Audit "success-but-empty" path to inject sample without demo env**  
  `auditDemoSampleInjectEnabled()` gates both the load-more and clear-and-search success paths. The failure path was already widened in T1-1; align the success-with-zero-results path to match.  
  *Resolves: items 44–46*

- [ ] **T1-7 · Fix Reviews list date display without demo env**  
  `displayRelativeCreated` in `RunsListClient.tsx` uses calendar format only when `isNextPublicDemoMode()`. The canonical showcase row's `createdUtc` is `2026-01-10`; use a fixed polished label ("January 2026" or "Finalized Jan 14, 2026") for the known sample run regardless of env.  
  *Resolves: item 89*

### Tier 2 — CTA Guards on Broken Routes

- [ ] **T2-1 · Guard "Open review" CTAs against non-resolvable routes**  
  Reviews list, side drawer, Demo Preview, Live Demo, Showcase, See It should not route into review detail unless the route can load. After T1-1 deployment, these CTAs should become safe automatically — but add an explicit guard that checks `isStaticDemoPayloadFallbackActiveForRun(runId)` before rendering the link.  
  *Resolves: items 5–6, 15–17*

- [ ] **T2-2 · Replace "Retry" with context-aware recovery CTAs on deterministic demo failures**  
  On known showcase slug failures, surface "Open sample manifest," "Open public preview," "Back to reviews."  
  *Resolves: item 10*

- [ ] **T2-3 · Disable "Review trail graph" quick link when provenance is empty**  
  *Resolves: item 18*

### Tier 3 — Empty States

- [ ] **T3-1 · Graph: auto-load showcase graph on mount when run ID resolves to canonical sample (regardless of env)**  
  (Supersedes prior T3-1 which was correct but partial — T1-5 covers initialization; this ensures `performGraphLoad` fires automatically.)  
  *Resolves: items 19–23*

- [ ] **T3-2 · Ask: auto-select showcase review in picker; disable suggested prompts until context is active**  
  *Resolves: items 24–26*

- [ ] **T3-3 · Governance: auto-prefill with showcase run on failure regardless of env**  
  The `useEffect` demoPrefill is still gated by `isStaticDemoPayloadFallbackEnabled()`. In T1-1 we intentionally kept the demo-env gate for the auto-prefill to avoid flooding real tenants. Re-examine: if the picker finds no live runs and the static list fallback activates, pre-select the canonical run.  
  *Resolves: items 29–30*

### Tier 4 — Copy and Terminology

- [ ] **T4-1 · Rename "Run ID" → "Review ID" in Audit filter label**  
  `audit/page.tsx` line 392: `Run ID{" "}` → `Review ID{" "}`.  
  *Resolves: item 47*

- [ ] **T4-2 · Remove internal authority-rank copy from buyer-facing pages**  
  Remove or replace: `layerHeaderEnterpriseOperatorRankLine` ("Execute+ writes (API-enforced)"), `auditSearchSectionLeadReaderLine` ("CSV uses same From/To; Auditor/Admin on API"), `alertToolingConfigureSectionSubline` ("Inspect above — configure below (Execute+, API)"), and similar visible on Audit, Alerts, Governance, Policy Packs.  
  *Resolves: items 48–49, 53*

- [ ] **T4-3 · Replace "No rows. Change filter or refresh" in Alerts**  
  Rephrase to "No alerts match your current filters. Clear filters or wait for new activity."  
  *Resolves: item 54*

- [ ] **T4-4 · Complete run→review terminology migration**  
  Ask: "run context" → "review context," "another run" → "another review." Governance/Graph: remove "thread" language. All labels per items 71–77.  
  *Resolves: items 27–28, 71–77*

### Tier 5 — See It Page Polish

- [ ] **T5-1 · Remove "falls back to a checked-in snapshot" from See It page copy**  
  Replace with neutral language: "Real finalized manifest — live preview when available."  
  *Resolves: item 96*

- [ ] **T5-2 · Downgrade "Sample only" to a subtle disclosure on See It**  
  The amber warning box (`rounded-md border border-amber-200…`) is too dominant. Replace with a one-line inline note below the heading.  
  *Resolves: item 97*

- [ ] **T5-3 · Apply business labels to artifact names on See It**  
  `Sponsor briefing — Claims Intake Modernization.md` → "Sponsor brief," `architecture decision record bundle.json` → "Decision record," `diagram.mmd` → "System diagram." (Same fix as T8-6 for Manifest page, now also applied to See It.)  
  *Resolves: item 98*

### Tier 6 — Nav and Shell Polish

- [ ] **T6-1 · Remove "Operate — analysis" and "Operate — governance" nav labels**  
- [ ] **T6-2 · Remove "2 pilot tools" / "11 advanced analysis" / "8 governance controls" nav labels**  
- [ ] **T6-3 · Hide "Navigation settings" and "open navigation settings" from normal users**  
- [ ] **T6-4 · Replace "Default project" in workspace selector with polished display name; consolidate workspace name**  
  *Resolves: items 78–84*

### Tier 7 — Homepage

- [ ] **T7-1 · Reduce homepage CTAs to three; remove internal release sequencing language**  
- [ ] **T7-2 · Fix "See completed example" routing to a working page only**  
- [ ] **T7-3 · Fix "Resolving checklist links…" loading state**  
  *Resolves: items 11–13, 85–88*

### Tier 8 — Buyer-Facing Display Polish

- [ ] **T8-1 · Reviews list: fixed polished date for canonical showcase row**  *(covered by T1-7 above)*
- [ ] **T8-2 · Fix Reviews side-panel title truncation**  
- [ ] **T8-3 · Reduce Reviews side-panel quick-actions to 4 primary CTAs**  
- [ ] **T8-4 · Manifest: lead with operatorSummary narrative before counts grid**  
- [ ] **T8-5 · Move manifest technical identifiers into collapsed section at bottom**  
- [ ] **T8-6 · Apply business labels to artifact descriptors; hide MIME types**  
- [ ] **T8-7 · Strip raw slugs, UTC timestamps, workspace names, artifact filenames from all public pages**  
  *Resolves: items 90–95, 99*

### Tier 9 — Feature-Flag Operator-Only Routes in Demo

- [ ] **T9-1 · Feature-flag Admin Health, Admin Support, Admin Users out of demo**  
- [ ] **T9-2 · Feature-flag Tenant Settings, Digest Subscriptions, Digests, Teams Integration out of demo**  
- [ ] **T9-3 · Remove "coming soon" from authenticated routes; feature-flag incomplete modules off**  
  Governance Dashboard, Planning, Evolution Review, Product Learning, Recommendation Learning, Replay, Search  
- [ ] **T9-4 · Strip raw JSON config, repo paths, Create/Publish/Assign forms from Policy Pack detail**  
  *Resolves: items 32–33, 41–43, 55–70*

### Tier 10 — Auth Pages

- [ ] **T10-1 · Auth error states render outside full app shell**  
  *Resolves: items 59–60*

### Tier 11 — Platform Status Banner

- [ ] **T11-1 · Make "Platform services: Healthy" banner demo-context-aware**  
  When serving static fallback data, change label to "Sample data — live API not connected." Do not remove the signal; make it accurate.  
  *Resolves: item 8*

---

## Execution Order

```
T1-4 → T1-5 → T1-6 → T1-7  (closes remaining fallback gaps — all small, targeted)
T2-1                         (CTA guards — safe after T1-x lands)
T4-1 → T4-2 → T4-3 → T4-4  (copy/terminology — parallel track, no blockers)
T5-1 → T5-2 → T5-3          (See It polish — isolated page, no blockers)
T3-1 → T3-2 → T3-3          (empty state auto-selects — after T1-5)
T9-3                         (feature-flag "coming soon" routes — standalone)
T9-1 → T9-2 → T9-4          (feature-flag admin routes — standalone)
T6-x, T7-x, T8-x            (nav/homepage/display polish — parallel, no blockers)
T10-1, T11-1                 (auth and status banner — isolated)
```

Highest-leverage next step: **T1-4 through T1-7** — small changes in `operator-static-demo.ts`, `audit/page.tsx`, and `RunsListClient.tsx` that close the remaining env-var gaps without touching any route files.
