> **Scope:** Engineers pasting Cursor prompts for cognitive-load improvements from the paired assessment — not deferred items 9–10 or out-of-band refactors.

# Cursor prompts — Cognitive Load 63.65% (2026-04-24)

Eight paste-ready Cursor prompts for the actionable improvements in `QUALITY_ASSESSMENT_2026_04_24_COGNITIVE_LOAD_63_65.md`. Improvements 9 and 10 are DEFERRED (owner decisions required) — no full prompts generated for those.

---

## Prompt 1 — Consolidate alert navigation into a tabbed hub page

```
## Goal

Collapse the six alert-related sidebar nav items (Alerts, Alert rules, Alert routing, Composite rules, Alert simulation, Alert tuning) into a single "Alerts" nav item that opens a tabbed hub page at `/alerts`.

## What to build

1. **Hub page at `/alerts`** — a single page with five tabs:
   - **Inbox** (current `/alerts` content — open and acknowledged alerts)
   - **Rules** (current `/alert-rules` content — metric thresholds)
   - **Routing** (current `/alert-routing` content — delivery subscriptions)
   - **Composite** (current `/composite-alert-rules` content — multi-metric conditions)
   - **Simulation & Tuning** (current `/alert-simulation` + `/alert-tuning` merged)

2. **Tab routing:** Use query-string tabs (`/alerts?tab=rules`) so deep links still work and browser back/forward is tab-aware. Default tab is "Inbox".

3. **Nav config update:** In `archlucid-ui/src/lib/nav-config.ts`, replace the six alert `NavLinkItem` entries in the `operate-governance` group with a single entry:
   ```ts
   {
     href: "/alerts",
     label: "Alerts",
     title: navTitleWithShortcut("Alerts — inbox, rules, routing, simulation, and tuning", "alt+l"),
     keyShortcut: "alt+l",
     icon: Bell,
     tier: "essential",
     requiredAuthority: "ReadAuthority",
   }
   ```

4. **Preserve existing page components.** Each tab renders the existing page component (e.g., `AlertRulesPage`, `AlertRoutingPage`) inside the hub layout. Do not rewrite the page internals — only lift them into the tab container.

5. **Redirect old routes.** Add Next.js redirects from `/alert-rules`, `/alert-routing`, `/composite-alert-rules`, `/alert-simulation`, `/alert-tuning` to `/alerts?tab=<matching-tab>` so bookmarks and docs do not break.

6. **Update docs:** Update `OPERATOR_ATLAS.md` and `operator-shell.md` to reflect the consolidated route. Update the troubleshooting quick matrix if any rows reference the old routes.

7. **Tests:** Update `authority-seam-regression.test.ts`, `authority-execute-floor-regression.test.ts`, and `authority-shaped-ui-regression.test.ts` to reflect the reduced nav link count. Add a Vitest for the tab routing (default tab, deep-link tab, unknown tab falls back to Inbox).

## Constraints

- Do not change API endpoints. The hub page calls the same APIs as the individual pages.
- Preserve the `requiredAuthority` semantics: if any tab's content requires `ExecuteAuthority`, the tab should show a read-only view for `ReadAuthority` callers (same pattern as existing mutation soft-disable).
- The keyboard shortcut `alt+l` stays on the consolidated "Alerts" item.
```

---

## Prompt 2 — Add contextual glossary tooltips in the operator UI

```
## Goal

Surface domain-specific term definitions inline in the operator UI so users learn vocabulary in context rather than leaving the product to read `docs/library/GLOSSARY.md`.

## What to build

1. **Glossary data module** — `archlucid-ui/src/lib/glossary-terms.ts`:
   - Export a `Record<string, { term: string; definition: string; docLink?: string }>` with entries for the ~15 most-encountered terms: run, golden manifest, findings, authority pipeline, context snapshot, decision trace, effective governance, policy pack, knowledge graph, artifact bundle, scope, comparison replay, hosting role, outbox, finding engine.
   - Definitions should be 1–2 sentences max (same voice as `GLOSSARY.md` but shorter).
   - `docLink` is a relative path to the deeper doc (e.g., `"/docs/library/GLOSSARY.md#golden-manifest"`).

2. **`GlossaryTooltip` component** — `archlucid-ui/src/components/GlossaryTooltip.tsx`:
   - Accepts `termKey: string` (matches glossary-terms key).
   - Renders a dotted-underline span with a tooltip (use `@radix-ui/react-tooltip` or the existing tooltip primitive if one exists).
   - Tooltip shows the short definition + optional "Learn more →" link to the doc.
   - Accessible: tooltip content is available to screen readers via `aria-describedby`.

3. **Apply to key surfaces:**
   - Run detail page: wrap "golden manifest" text.
   - Pipeline tracker (wizard step 7): wrap "Context", "Graph", "Findings", "Manifest" badges.
   - Governance resolution page: wrap "effective governance" heading.
   - Audit log page: wrap "audit event" column header.
   - Policy packs page: wrap "policy pack" heading.
   - Alert rules page: wrap "finding engine" references.

4. **First-use variant (optional, if time permits):** On the first encounter of a glossary term in a session, show the tooltip with a brief pulse animation. Subsequent encounters show tooltips only on hover. Track seen terms in `sessionStorage`.

5. **Tests:** Vitest for `GlossaryTooltip` (renders term, shows tooltip on hover, links to doc). Axe check for the tooltip's accessibility.

## Constraints

- Do not add tooltips to every term on every page. Target the ~10 highest-frequency surfaces where a new user is most likely to encounter the term for the first time.
- Keep the glossary data module under 100 lines. If more terms are needed later, the module grows; do not pre-populate every possible term.
- Do not modify `GLOSSARY.md`. The UI module is a parallel, shorter-form data source.
```

---

## Prompt 3 — Reduce documentation entry-point proliferation

```
## Goal

Consolidate the multiple "start here" documentation files into a single unambiguous entry point so a new arrival has exactly one file to open.

## What to build

1. **Make `docs/READ_THIS_FIRST.md` the single canonical entry.** It already exists as a "single Y/N entry surface." Ensure it contains:
   - A two-column audience table (buyer vs contributor) with exactly one link per column.
   - Buyer column links to `docs/BUYER_FIRST_30_MINUTES.md`.
   - Contributor column links to `docs/START_HERE.md` (which holds the five-document spine).
   - Nothing else on the page beyond the table and a one-paragraph product description.
   - Estimated time: "< 1 minute to pick your lane."

2. **Convert competing entry files to redirect stubs:**
   - `docs/FIRST_5_DOCS.md` — already a redirect stub; confirm it points to `READ_THIS_FIRST.md`.
   - `docs/FIRST_FIVE_DOCS.md` — same.
   - `docs/NAVIGATOR.md` — keep as a reference doc but add a header note: "Entry point: [`READ_THIS_FIRST.md`](../READ_THIS_FIRST.md). This page is a task-oriented lookup for users who already chose their lane."
   - `README.md` (repo root) — ensure its first link is `docs/READ_THIS_FIRST.md` with the label "Start here" and remove any competing "start here" phrasing that links elsewhere.

3. **Consolidate operator docs cross-reference:**
   - In `docs/START_HERE.md`, add a "Contributor quick links" section that replaces the current scattered references to `OPERATOR_ATLAS.md`, `OPERATOR_QUICKSTART.md`, `OPERATOR_DECISION_GUIDE.md`, `PILOT_GUIDE.md`, etc. with a single table:
     | I need... | Read this one file |
     |---|---|
     | Route × API × CLI lookup | `OPERATOR_ATLAS.md` |
     | Copy-paste commands | `OPERATOR_QUICKSTART.md` |
     | When to use which product layer | `OPERATOR_DECISION_GUIDE.md` |
     | UI workflow detail | `operator-shell.md` |

4. **Update `PILOT_GUIDE.md` redirect:** Currently redirects to `OPERATOR_QUICKSTART.md`. Add a sentence: "If you arrived here from a bookmark, start at [`READ_THIS_FIRST.md`](../READ_THIS_FIRST.md) instead."

5. **CI guard (optional):** Add a check to `scripts/ci/assert_docs_root_size.py` that warns if any new file under `docs/` contains the phrase "start here" or "read this first" (case-insensitive) to prevent future proliferation.

## Constraints

- Do not delete any file. Convert to redirect stubs for bookmark stability.
- Do not move `START_HERE.md` or `BUYER_FIRST_30_MINUTES.md` — they stay at their current paths.
- Do not change the five-document contributor spine content in `START_HERE.md`; only add the cross-reference table.
```

---

## Prompt 4 — Collapse governance navigation into a tabbed hub

```
## Goal

Merge the three governance-related nav items (Governance dashboard, Governance resolution, Governance workflow) into a single "Governance" nav item that opens a tabbed hub page at `/governance`.

## What to build

1. **Hub page at `/governance`** — a single page with three tabs:
   - **Dashboard** (current `/governance/dashboard` content — cross-run approvals and policy signals)
   - **Policy** (current `/governance-resolution` content — effective governance for this scope)
   - **Workflow** (current `/governance` content — approvals, promotions, activation mutations)

2. **Tab routing:** Use query-string tabs (`/governance?tab=policy`) so deep links work. Default tab is "Dashboard" (read-first orientation per the inspect-before-configure principle).

3. **Authority-aware tab visibility:**
   - Dashboard and Policy tabs: visible to `ReadAuthority`.
   - Workflow tab: visible only to `ExecuteAuthority`. For `ReadAuthority` users, the tab label appears but is disabled with a tooltip ("Requires operator or admin role").
   - This matches the existing `useOperateCapability()` pattern.

4. **Nav config update:** In `archlucid-ui/src/lib/nav-config.ts`, replace the three governance `NavLinkItem` entries with:
   ```ts
   {
     href: "/governance",
     label: "Governance",
     title: navTitleWithShortcut("Governance — dashboard, effective policy, and approval workflow", "alt+g"),
     keyShortcut: "alt+g",
     icon: Scale,
     tier: "extended",
     requiredAuthority: "ReadAuthority",
   }
   ```

5. **Redirect old routes:** `/governance/dashboard` → `/governance?tab=dashboard`, `/governance-resolution` → `/governance?tab=policy`. The existing `/governance` path stays as-is (default tab = Dashboard; Workflow tab addressable via `?tab=workflow`).

6. **Update docs:** `OPERATOR_ATLAS.md`, `operator-shell.md`, `OPERATOR_DECISION_GUIDE.md`.

7. **Tests:** Update authority seam regression tests. Add Vitest for tab routing and authority-aware tab visibility (ReadAuthority sees Dashboard + Policy; ExecuteAuthority sees all three).

## Constraints

- Do not change API endpoints.
- The `alt+g` shortcut stays on the consolidated "Governance" item.
- Preserve the `authority-shaped-layout-regression.test.tsx` invariant that inspect-first layout is maintained (Dashboard tab first, not Workflow).
```

---

## Prompt 5 — Replace generic "Show more links" with descriptive disclosure labels

```
## Goal

Replace the generic "Show more links" and "Show advanced links" sidebar disclosure labels with descriptive text that tells the user what will appear before they click.

## What to change

1. **Locate the disclosure labels** in the sidebar component (likely `archlucid-ui/src/components/ShellNav.tsx` or `archlucid-ui/src/components/Sidebar.tsx` or wherever the "Show more links" / "Show advanced links" strings are rendered).

2. **Replace labels:**
   | Current | New |
   |---|---|
   | "Show more links" | "Show analysis & investigation tools" |
   | "Hide more links" | "Hide analysis & investigation tools" |
   | "Show advanced links" | "Show governance, audit & admin controls" |
   | "Hide advanced links" | "Hide governance, audit & admin controls" |

3. **Tooltip (optional):** Add a `title` attribute on the disclosure toggle with a one-line description of what the tier contains:
   - Extended: "Compare runs, replay authority chains, advisory scans, and similar investigation tools."
   - Advanced: "Alert configuration, audit log, governance workflow, planning, and admin-level controls."

4. **Update any Vitest or Playwright specs** that assert on the old label text.

5. **Update `operator-shell.md`** where it references "Show more links" / "Show advanced links" — use the new labels.

## Constraints

- Do not change the tier model (`essential` / `extended` / `advanced`). Only change the user-visible label text.
- Keep labels under 45 characters so they fit comfortably in the sidebar at narrow viewport widths.
- If the sidebar uses a shared component for both toggles, ensure the label is parameterized per tier, not hardcoded.
```

---

## Prompt 6 — Merge overlapping Operate · analysis nav items

```
## Goal

Reduce the Operate · analysis nav group from 12 items to 8–9 by merging closely related digest and advisory items into hub pages with tabs.

## What to build

### A. Digest hub at `/digests`

1. Merge three nav items into one:
   - **Digests** (`/digests`) — browse generated digests
   - **Subscriptions** (`/digest-subscriptions`) — email delivery config
   - **Exec digest** (`/settings/exec-digest`) — weekly sponsor email schedule

2. Hub page with three tabs:
   - **Browse** (current `/digests` content)
   - **Subscriptions** (current `/digest-subscriptions` content)
   - **Schedule** (current `/settings/exec-digest` content)

3. Tab routing: `/digests?tab=subscriptions`, `/digests?tab=schedule`. Default: Browse.

4. Redirect old routes: `/digest-subscriptions` → `/digests?tab=subscriptions`, `/settings/exec-digest` → `/digests?tab=schedule`.

### B. Advisory hub at `/advisory`

1. Merge two nav items into one:
   - **Advisory** (`/advisory`) — scans and digests
   - **Schedules** (`/advisory-scheduling`) — scan windows

2. Hub page with two tabs:
   - **Scans** (current `/advisory` content)
   - **Schedules** (current `/advisory-scheduling` content)

3. Tab routing: `/advisory?tab=schedules`. Default: Scans.

4. Redirect old routes: `/advisory-scheduling` → `/advisory?tab=schedules`.

### C. Nav config update

In `archlucid-ui/src/lib/nav-config.ts`, replace the five merged items with two:
```ts
{
  href: "/advisory",
  label: "Advisory",
  title: "Advisory — architecture scans and scan schedules",
  icon: Activity,
  tier: "extended",
  requiredAuthority: "ReadAuthority",
},
{
  href: "/digests",
  label: "Digests",
  title: "Digests — generated digests, subscriptions, and sponsor schedule",
  icon: FileSearch,
  tier: "advanced",
  requiredAuthority: "ReadAuthority",
},
```

Net effect: Operate · analysis goes from 12 items to 9.

### D. Tests and docs

Update authority seam regression tests, `OPERATOR_ATLAS.md`, `operator-shell.md`.

## Constraints

- Do not change API endpoints.
- Preserve `requiredAuthority` per tab: if Subscriptions requires `ExecuteAuthority`, the tab should soft-disable for Read users.
- Advisory Schedules tab requires `ExecuteAuthority` (CRUD on scan windows) — show read-only for Read users.
```

---

## Prompt 7 — Create unified configuration reference with `archlucid config check` CLI command

```
## Goal

Create a single configuration reference document and a CLI command that validates all configuration sources, reducing the cognitive overhead of "what config do I need and is it set correctly?"

## What to build

### A. `docs/library/CONFIGURATION_REFERENCE.md`

A single markdown file listing every configuration key the system recognizes, organized by section:

| Section | Key | Source(s) | Default | Required | Description |
|---|---|---|---|---|---|
| ConnectionStrings | `ArchLucid` | appsettings / env / Key Vault | (none) | Yes (when StorageProvider=Sql) | SQL Server connection string |
| AzureOpenAI | `Endpoint` | appsettings / env / Key Vault | (none) | Yes (when Mode=Real) | Azure OpenAI endpoint URL |
| ... | ... | ... | ... | ... | ... |

**How to populate:** Scan `ArchLucid.Api/appsettings.json`, `ArchLucid.Host.Core/Startup/Validation/ArchLucidConfigurationRules.cs`, and all `IOptions<T>` / `IConfigureOptions<T>` bindings in the DI registration extensions. List every key that the startup validation checks or that appears in `appsettings.json` with a default. Group by the top-level JSON section (e.g., `ArchLucid`, `AzureOpenAI`, `Authentication`, `Billing`, `RateLimiting`, `Hosting`).

Target: 100–150 rows. Do not list internal-only keys that operators never set.

### B. `archlucid config check` CLI command

1. **New command** in `ArchLucid.Cli/Commands/ConfigCheckCommand.cs`.
2. **What it does:**
   - Reads `archlucid.json` (if present), environment variables (`ARCHLUCID_*`, `AZURE_OPENAI_*`), and (if API is reachable) `GET /v1/admin/config-summary` (existing endpoint, `AdminAuthority`).
   - For each known key: prints **SET** (with source: env / json / appsettings / Key Vault ref) or **MISSING** (with whether it is required or optional for the current mode).
   - Prints a summary: N required keys set, M optional keys set, P required keys missing.
   - Exit code 0 if all required keys are set; exit code 4 if any required key is missing.
3. **Fallback mode (no API):** When the API is not reachable, check only local sources (env vars, `archlucid.json`). Print a note that API-side keys were not checked.
4. **`--json` flag:** Machine-readable output for CI.

### C. Tests

- Unit test for the key registry (all keys in the reference doc match the code scanner).
- CLI integration test: run `config check` with a minimal env and confirm the expected MISSING keys appear.

## Constraints

- The `config-summary` endpoint already exists; do not create a new one.
- Do not expose secret values in the CLI output. Print `(set)` / `(not set)` only — never the actual value.
- The reference doc should note which keys are relevant to which hosting role (Api / Worker / Combined).
```

---

## Prompt 8 — Add persistent layer-context indicator to all Operate routes

```
## Goal

Add a small, persistent layer indicator below the page header on all Operate routes so users always know which product layer they are in and what question that layer answers.

## What to build

1. **`LayerContextStrip` component** — `archlucid-ui/src/components/LayerContextStrip.tsx`:
   - Accepts `layerId: "pilot" | "operate-analysis" | "operate-governance"`.
   - Renders a thin colored strip (subtle background tint per layer — e.g., blue for Pilot, teal for Operate · analysis, amber for Operate · governance).
   - Content: layer label + one-line question:
     | Layer ID | Label | Question |
     |---|---|---|
     | `pilot` | Pilot | Can we go from request to committed manifest faster? |
     | `operate-analysis` | Operate · analysis | What changed, why, and what does the architecture look like? |
     | `operate-governance` | Operate · governance | How do we govern, audit, and operationalize architecture decisions? |
   - Optional "Back to Core Pilot" link (only on Operate pages).

2. **Derive layer from route.** Create a helper `getLayerForRoute(pathname: string): LayerId` that maps the current `pathname` to a layer using the `NAV_GROUPS` config (find which group contains a link whose `href` matches the current path).

3. **Apply in the operator layout.** In the operator layout component (`archlucid-ui/src/app/(operator)/layout.tsx` or equivalent), render `<LayerContextStrip layerId={getLayerForRoute(pathname)} />` between the header and the page content. The strip should not appear on marketing routes.

4. **Do not duplicate `LayerHeader`.** Where `LayerHeader` already exists on a page, the strip supplements it (strip = persistent layer context; header = page-specific guidance). If they feel redundant on a page, keep the strip and remove the header only if the header adds no page-specific content beyond the layer name.

5. **Tests:**
   - Vitest: `LayerContextStrip` renders the correct label and question for each layer ID.
   - Vitest: `getLayerForRoute` returns the correct layer for known routes and falls back to `"pilot"` for unknown routes.
   - Axe: strip passes contrast and landmark checks.

## Constraints

- The strip should be visually subtle — thin height (32–40px), muted background, regular-weight text. It is orientation, not decoration.
- Do not add the strip to marketing pages (`/(marketing)/` routes).
- Do not change `nav-config.ts` or `nav-shell-visibility.ts`. Read from them only.
```

---

## Prompt 9 — Disambiguate "authority" in user-facing UI copy (RBAC sense → "access level")

```
## Goal

Eliminate the overloaded use of "authority" in user-facing operator UI copy. The word currently means two unrelated things:
- **Pipeline sense:** "authority pipeline" = the analysis pipeline that produces the golden manifest.
- **RBAC sense:** "ReadAuthority" / "ExecuteAuthority" / "AdminAuthority" = authorization levels.

This prompt addresses the **RBAC sense only** — replacing user-visible occurrences of "authority" (when it means authorization) with "access level" or "permission level" so operators stop conflating the two concepts.

**Code identifiers stay unchanged.** `RequiredAuthority`, `AUTHORITY_RANK`, `filterNavLinksByAuthority`, `authorityRank`, `maxAuthority`, `callerAuthorityRank`, `ArchLucidPolicies` all keep their current names internally. Only **user-visible strings** (tooltips, labels, LayerHeader copy, error messages, accessibility labels) change.

## Scope of change

### A. `archlucid-ui/src/lib/layer-guidance.ts`

In the `LAYER_PAGE_GUIDANCE` entries, every `enterpriseFootnote` and `useWhen` string that references "authority" in the RBAC sense should be reviewed. Most do not use the word "authority" in user-visible copy (they say "API-gated writes" or "Execute + Standard tier on API"). No changes expected here — confirm and move on.

### B. `archlucid-ui/src/lib/nav-config.ts`

Review every `title` string on `NavLinkItem` entries. The following currently contain "authority":
- None of the `title` strings actually surface "authority" to the user — they say things like "Guided first-run wizard" or "Audit log". **Confirm no user-visible "authority" leaks and move on.**

### C. `archlucid-ui/src/components/OperatorNavAuthorityProvider.tsx` (and related)

This component name uses "Authority" in the code sense — leave it. But check:
- Any `aria-label` or visible text it renders. If it renders "Authority: Read" or similar, change to "Access level: Read" or "Permission: Read".

### D. `archlucid-ui/src/components/EnterpriseControlsContextHints.tsx`

Check every user-visible string this component renders. If any tooltip or paragraph says "requires ExecuteAuthority" or "your authority level", replace with:
- "Requires Operator or Admin access" (preferred — maps to actual role names)
- or "Requires Execute access level"

### E. `archlucid-ui/src/components/LayerHeader.tsx`

Check the Execute+ rank cue strip. If it renders text like "Authority: Execute" or "Your authority is Read", replace with:
- "Access: Operator" or "Your access level: Read"
- Prefer role names (Reader / Operator / Admin) over policy names in user-facing copy.

### F. Sidebar disclosure tooltips

If any sidebar tooltip says "requires ReadAuthority" or "requires ExecuteAuthority", replace with:
- "Visible to Readers and above" / "Visible to Operators and above" / "Visible to Admins"
- These are the user-facing role names that map to the code-internal policy names.

### G. Error surfaces

Check `OperatorApiProblem` and any 401/403 handling copy. If the UI renders "insufficient authority", change to "insufficient access" or "insufficient permissions."

### H. `archlucid-ui/src/lib/enterprise-controls-context-copy.ts`

Review all string constants. Replace any user-facing "authority" (RBAC sense) with "access level" or role names.

### I. Documentation cross-references (in the UI)

Any in-product link text or hover text that says "authority" when meaning RBAC should say "access" or "permission."

## What NOT to change

- **Code identifiers:** `RequiredAuthority`, `AUTHORITY_RANK`, `filterNavLinksByAuthority`, `authorityRank`, `callerAuthorityRank`, `maxAuthority`, `navLinkVisibleForCallerRank`, `maxAuthorityRankFromMeClaims`, `requiredAuthorityFromRank`, `getCurrentAuthority`, `getCurrentAuthorityRank` — all stay as-is.
- **TypeScript types:** `RequiredAuthority`, `NavLinkItem.requiredAuthority` — stay as-is.
- **Server-side policy names:** `ReadAuthority`, `ExecuteAuthority`, `AdminAuthority` in `ArchLucidPolicies` — stay as-is.
- **Test file names:** `authority-seam-regression.test.ts`, etc. — stay as-is.
- **JSDoc / code comments:** Leave internal documentation alone. Only change strings that render in the browser.
- **Any reference to "authority pipeline" or "authority chain"** — that is the pipeline sense, handled separately in Prompt 10.

## User-facing vocabulary mapping (use this for replacements)

| Code-internal | User-facing replacement |
|---|---|
| `ReadAuthority` | "Read access" or "Reader" |
| `ExecuteAuthority` | "Operator access" or "Operator" |
| `AdminAuthority` | "Admin access" or "Admin" |
| "your authority" | "your access level" |
| "requires [X]Authority" | "requires [Role] access" or "visible to [Role]s and above" |
| "authority rank" (in visible text) | "access level" |
| "insufficient authority" | "insufficient permissions" |

## Tests

- Update any test assertions that match on user-visible strings you changed (e.g., `getByText("Authority: Read")` → `getByText("Access: Read")`).
- Do NOT rename test files, describe blocks, or code-internal variable names.
- Run all existing authority regression tests to confirm no breakage:
  - `authority-seam-regression.test.ts`
  - `authority-execute-floor-regression.test.ts`
  - `authority-shaped-ui-regression.test.ts`
  - `OperatorNavAuthorityProvider.test.tsx`
  - `enterprise-authority-ui-shaping.test.tsx`
  - `EnterpriseControlsContextHints.authority.test.tsx`

## Constraints

- Zero server-side changes. This is a UI copy-only change.
- Zero code identifier renames. Internal code stays `authority` everywhere.
- The only files that change are `.tsx` and `.ts` files in `archlucid-ui/src/` that render user-visible strings, plus any Vitest assertions on those strings.
```

---

## Prompt 10 — Rename "authority pipeline" to "analysis pipeline" in user-facing copy (post-coordinator-strangler)

```
## Goal

After the coordinator strangler completes (target: 2026-05-15 per ADR 0029), rename "authority pipeline" to "analysis pipeline" in all user-facing documentation and UI copy. The coordinator pipeline will be gone; "authority" will have lost its differentiating purpose.

**Code identifiers stay unchanged.** `IAuthorityRunOrchestrator`, `AuthorityPipelineContext`, `AuthorityPipelineStagesExecutor`, `AuthorityCommitProjectionBuilder`, etc. all keep their current names.

## Pre-condition gate

**Do not execute this prompt until:**
1. ADR 0021 Phase 3 PR A has merged (coordinator concretes and interfaces deleted).
2. No `CoordinatorRun*` audit event types remain in `AuditEventTypes.cs`.
3. The `CoordinatorPipelineDeprecationFilter` is removed or inactive.

If these conditions are not met, stop and report which condition is still open.

## Scope of change

### A. Documentation (docs/)

Search all `.md` files under `docs/` for the phrases below and replace as indicated:

| Find (case-insensitive) | Replace with |
|---|---|
| "authority pipeline" | "analysis pipeline" |
| "authority chain" (when describing the pipeline sequence, not the committed chain data structure) | "analysis chain" or "analysis sequence" |
| "authority run orchestrator" (in user-facing narrative, not code references) | "run orchestrator" |
| "authority-layer" (in user-facing narrative) | keep as-is when referring to the DB layer name; replace when used as a synonym for "the main pipeline" |

**Preserve code references.** Any doc line that references an actual class name (e.g., "`IAuthorityRunOrchestrator`", "`AuthorityPipelineStagesExecutor`") keeps the backtick-quoted code name as-is but may add a parenthetical: "`IAuthorityRunOrchestrator` (the analysis pipeline orchestrator)".

**Key files to update (non-exhaustive):**
- `docs/library/GLOSSARY.md` — update the "Authority run orchestrator" entry title and definition
- `docs/library/CANONICAL_PIPELINE.md` — replace user-facing "authority pipeline" references
- `docs/library/ARCHITECTURE_FLOWS.md` — same
- `docs/library/ARCHITECTURE_COMPONENTS.md` — same
- `docs/ARCHITECTURE_ON_ONE_PAGE.md` — same
- `docs/library/operator-shell.md` — the "replay" row references "stored authority chain"
- `docs/library/OPERATOR_ATLAS.md` — any authority pipeline references in the action descriptions
- `docs/library/FIRST_RUN_WIZARD.md` — pipeline stage descriptions
- `docs/CORE_PILOT.md` — step 2 references "coordinator fills context snapshots and authority steps"
- `docs/library/V1_SCOPE.md` — any pipeline references
- `docs/library/layer-guidance.ts` references in `replay` entry: "does the stored authority chain still validate" → "does the stored analysis chain still validate"

### B. Operator UI copy

Search `archlucid-ui/src/` for user-visible strings containing "authority pipeline", "authority chain", or "authority run" (in the pipeline sense, not the RBAC sense):

1. **`layer-guidance.ts`** — `replay.headline`: "does the stored authority chain still validate for this run?" → "does the stored analysis chain still validate for this run?"

2. **Pipeline tracker labels** (wizard step 7 in `NewRunWizardClient` or `RunProgressTracker`) — if any visible label says "Authority" as a pipeline stage name, replace with "Analysis" or the specific stage name.

3. **Run detail page** — if the pipeline timeline heading references "authority pipeline", replace with "analysis pipeline."

4. **Tooltips and aria-labels** — search for `aria-label` values containing "authority" in the pipeline sense.

### C. CLI output

Search `ArchLucid.Cli/` for user-facing `Console.Write` or interpolated strings containing "authority pipeline" or "authority chain." Replace in output strings only — not in code identifiers or variable names.

### D. GLOSSARY.md specific updates

```markdown
## Run orchestrator (analysis pipeline)

**`IAuthorityRunOrchestrator`** — the in-process pipeline that executes the full
ingestion → graph → findings → decisioning → artifact synthesis sequence for a
single run. Runs inside a SQL unit of work. Previously called "authority run
orchestrator" when a legacy coordinator pipeline coexisted (removed in ADR 0021
Phase 3).
```

### E. API response bodies

**Do not change API JSON field names or enum values.** If any API response includes a human-readable `description` or `detail` string that says "authority pipeline," update the string. But `type` fields, enum names, and JSON keys stay unchanged.

## What NOT to change

- Code identifiers (classes, interfaces, methods, properties, namespaces)
- SQL table names, column names, or stored procedure names
- API JSON keys, query parameters, or enum serialization values
- Test file names or test describe/it blocks (unless they assert on user-visible strings that changed)
- Historical ADR content (ADRs are immutable decision records)
- Files under `docs/archive/` (historical receipts)
- The RBAC sense of "authority" (handled in Prompt 9)
- The term "authority chain" when it refers to the **committed data structure** (`dbo.AuthorityCommittedChains`) rather than the pipeline execution sequence — that data structure keeps its name

## Tests

- Run `rg "authority pipeline" docs/` after changes — expect zero matches outside `docs/archive/` and ADR files.
- Run `rg "authority pipeline" archlucid-ui/src/` — expect zero matches in user-visible strings (code identifiers may still match).
- Existing unit and integration tests should pass unchanged since no code identifiers moved.

## Constraints

- This is a copy-only change. No `.cs` file identifiers change. No `.ts` type or variable names change.
- Do not touch `docs/adr/` files — ADRs are historical.
- Do not touch `docs/archive/` files — archived content stays as written.
- Estimated effort: ~2 hours of search-and-replace + review.
```

---

## Prompt 11 — Finalize opt-in tour copy and remove pending-approval wrappers (owner-approved 2026-04-24)

```
## Goal

The owner approved the final five-step tour copy on 2026-04-24. Remove the
`TourStepPendingApproval` wrappers from every step and replace the draft copy
with the approved text. All five steps ship in one PR (owner decision: option B
— batch all five, no mixed state).

## Approved tour copy (verbatim — use exactly this text)

Step 1 title: "1. Operator home"
Step 1 body:  "Your starting point. The Core Pilot checklist at the top walks you through your first run — follow it in order. The analysis and governance sections below are optional until your first run is committed."

Step 2 title: "2. Start a run"
Step 2 body:  "Click New run (or press Alt+N) to open the wizard. It guides you through system identity, requirements, and constraints, then kicks off the analysis pipeline. You will see live progress on step 7."

Step 3 title: "3. Review and commit"
Step 3 body:  "When the pipeline finishes, open your run from the Runs list. Review the findings and evidence, then click Commit to produce the versioned manifest — the architecture package you can export and share."

Step 4 title: "4. Governance and alerts"
Step 4 body:  "After your first commit, dashboards and alerts can highlight policy gaps and approval queues. These are available when you are ready — they are not required for a successful first pilot."

Step 5 title: "5. Get help"
Step 5 body:  "If something is not working, go to Admin → Support to download a redacted diagnostics bundle for support tickets. Most pages also include a link to the relevant documentation."

## Files to change

### 1. `archlucid-ui/src/components/tour/OptInTour.tsx`

**A. Replace `DRAFT_TOUR_STEPS` array contents** with the approved copy above.
Note: Step 3 title changes from "3. Inspect a run" to "3. Review and commit".

**B. Remove the `TourStepPendingApproval` wrapper from every step's render.**
In the `.map()` inside the `OptInTour` component, replace:

```tsx
<TourStepPendingApproval>{step.body}</TourStepPendingApproval>
```

with:

```tsx
<p className="text-sm leading-relaxed text-neutral-800 dark:text-neutral-100">{step.body}</p>
```

**C. Remove the `TourStepPendingApproval` import** from the top of the file.

**D. In `TourStepListForTesting`, apply the same wrapper removal:**
Replace:

```tsx
<TourStepPendingApproval>{step.body}</TourStepPendingApproval>
```

with:

```tsx
<p className="text-sm leading-relaxed text-neutral-800 dark:text-neutral-100">{step.body}</p>
```

Remove the `TourStepPendingApproval` import if it is no longer used anywhere
in this file.

### 2. `archlucid-ui/src/components/tour/OptInTour.test.tsx`

**A. Remove the marker assertion test.**
Delete the entire `describe("TourStepListForTesting (owner Q8 marker per step)")` block —
the pending-approval marker no longer exists.

**B. Remove the launcher marker assertion.**
In `describe("OptInTourLauncher (owner Q9 — never auto-launch)")`, delete the
test `"first step inside the dialog renders the pending-approval marker"` — there
is no marker to assert.

**C. Remove the `TOUR_PENDING_APPROVAL_MARKER` import** (no longer used).

**D. Remove the `TourStepListForTesting` import** if the only test that used it
was the deleted marker block.

**E. Add a new test** confirming approved copy renders without markers:

```tsx
it("renders step body as plain text without pending-approval markers", () => {
  render(<OptInTour isOpen={true} onClose={() => {}} />);

  expect(screen.queryByTestId("tour-pending-approval-marker")).toBeNull();
  expect(screen.getByTestId("opt-in-tour-step-0").textContent).toContain(
    "Your starting point",
  );
});
```

**F. Existing tests that still apply (do not delete):**
- "contains exactly five steps"
- "every step body is non-empty draft copy" (rename to "every step body is non-empty")
- "renders nothing when isOpen=false"
- "renders step 0 when isOpen=true"
- "close button persists the dismissal LocalStorage flag"
- "Next advances through every step then shows Finish on the last step"
- "does NOT render the tour dialog on mount"
- "renders the tour dialog only after the 'Show me around' button is clicked"
- "re-opens the tour even after a previous dismissal flag is present"

### 3. `archlucid-ui/src/components/tour/TourStepPendingApproval.tsx`

**Delete this file entirely.** No other component should import it after the
changes above.

Verify with a project-wide search:
```
rg "TourStepPendingApproval" archlucid-ui/src/
```
Expect zero matches after all changes are applied.

### 4. Documentation updates

**A. `docs/PENDING_QUESTIONS.md`**

Under the Improvement 5 entry, add a resolution line:

> **Resolved 2026-04-24 (tour copy approved).** Owner approved all five step
> copies. `TourStepPendingApproval` wrappers removed in a single batch PR per
> option B. No mixed-state UI.

**B. `docs/CHANGELOG.md`**

Add an entry under the current date:

> **Opt-in tour copy finalized.** Five-step operator tour ("Show me around")
> now renders approved copy without pending-approval markers. Step 3 renamed
> from "Inspect a run" to "Review and commit" for clarity.

## What NOT to change

- `OptInTourLauncher.tsx` — no changes needed (it just renders `OptInTour`).
- Tour behavior (never auto-launches, dismissal flag, controlled open/close).
- The `TOUR_DISMISSED_LOCAL_STORAGE_KEY` constant or its behavior.
- Any file outside `archlucid-ui/src/components/tour/` and the two doc files above.

## Verification

After all changes:
1. `rg "TourStepPendingApproval" archlucid-ui/` — expect zero matches.
2. `rg "pending.*approval" archlucid-ui/src/components/tour/` — expect zero matches.
3. `cd archlucid-ui && npm test` — all tour tests pass.
4. Manual: click "Show me around" on the operator home page. All five steps
   render clean copy with no amber "pending owner approval" banner.
```

---

## Prompt 12 — Structured baseline intake form and ROI instrumentation (owner-approved 2026-04-24)

```
## Goal

Extend the self-service signup and tenant settings to capture a structured baseline
intake beyond the single "review-cycle hours" field that exists today, so the value-
report pipeline can compute customer-specific ROI instead of relying on model-constant
placeholders.

Owner decisions (2026-04-24):
- V1 includes all three Tier 1 fields (review-cycle hours already exists; add manual
  prep hours and people per review).
- Manual prep hours and people-per-review are deferrable: captured via a tenant
  settings page, not required at signup.
- Company size is persisted server-side (expanded range dropdown).
- Architecture team size (numeric) and industry vertical (curated dropdown + "Other"
  free-text) are captured at signup.
- Industry vertical options: Healthcare, Financial Services, Technology,
  Government / Public Sector, Manufacturing, Retail, Insurance,
  Energy / Utilities, Education, Telecommunications, Other.

## Layer 1 — SQL migration (new file: `ArchLucid.Persistence/Migrations/102_Tenants_StructuredBaseline.sql`)

Add columns to `dbo.Tenants`. Follow the idempotent pattern used in migration 101.

```sql
SET NOCOUNT ON;
GO

/* 102: Structured baseline intake — company profile + deferrable ROI fields. */

IF COL_LENGTH(N'dbo.Tenants', N'BaselineManualPrepHoursPerReview') IS NULL
BEGIN
    ALTER TABLE dbo.Tenants ADD
        BaselineManualPrepHoursPerReview     DECIMAL(9,2)     NULL,
        BaselinePeoplePerReview              INT              NULL,
        BaselineManualPrepCapturedUtc        DATETIMEOFFSET(7) NULL,
        CompanySize                          NVARCHAR(30)     NULL,
        ArchitectureTeamSize                 INT              NULL,
        IndustryVertical                     NVARCHAR(100)    NULL,
        IndustryVerticalOther                NVARCHAR(200)    NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = N'CK_Tenants_BaselineManualPrepHoursPerReview_Positive'
      AND parent_object_id = OBJECT_ID(N'dbo.Tenants', N'U'))
BEGIN
    ALTER TABLE dbo.Tenants ADD CONSTRAINT CK_Tenants_BaselineManualPrepHoursPerReview_Positive
        CHECK (BaselineManualPrepHoursPerReview IS NULL OR BaselineManualPrepHoursPerReview > 0);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = N'CK_Tenants_BaselinePeoplePerReview_Positive'
      AND parent_object_id = OBJECT_ID(N'dbo.Tenants', N'U'))
BEGIN
    ALTER TABLE dbo.Tenants ADD CONSTRAINT CK_Tenants_BaselinePeoplePerReview_Positive
        CHECK (BaselinePeoplePerReview IS NULL OR BaselinePeoplePerReview > 0);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = N'CK_Tenants_ArchitectureTeamSize_Positive'
      AND parent_object_id = OBJECT_ID(N'dbo.Tenants', N'U'))
BEGIN
    ALTER TABLE dbo.Tenants ADD CONSTRAINT CK_Tenants_ArchitectureTeamSize_Positive
        CHECK (ArchitectureTeamSize IS NULL OR ArchitectureTeamSize > 0);
END;
GO
```

Also add a rollback script at `ArchLucid.Persistence/Migrations/Rollback/R102_Tenants_StructuredBaseline.sql`
that drops the three constraints then the seven columns, same pattern as `R101`.

Also append the same idempotent DDL to the master script at
`ArchLucid.Persistence/Scripts/ArchLucid.sql` (after the migration 101 block).

## Layer 2 — Domain model (`ArchLucid.Core/Tenancy/TenantRecord.cs`)

Add seven new properties (all nullable) after the existing `BaselineReviewCycleCapturedUtc`
property, before `EnterpriseSeatsLimit`:

- `decimal? BaselineManualPrepHoursPerReview`
- `int? BaselinePeoplePerReview`
- `DateTimeOffset? BaselineManualPrepCapturedUtc`
- `string? CompanySize`
- `int? ArchitectureTeamSize`
- `string? IndustryVertical`
- `string? IndustryVerticalOther`

## Layer 3 — Persistence (`ArchLucid.Persistence/Tenancy/DapperTenantRepository.cs`)

3a. Update every `SELECT` that reads `dbo.Tenants` columns to include the seven new
    columns (four queries: GetByIdAsync, GetBySlugAsync, GetByEntraTenantIdAsync,
    GetAllAsync). Add them after `BaselineReviewCycleCapturedUtc`.

3b. Update the `TenantRow` mapping class (private, inside the same file) with seven
    matching properties and update the `ToRecord()` mapper.

3c. Update `ActivateTrialAsync` (or whichever method currently persists baseline data)
    to also write `CompanySize`, `ArchitectureTeamSize`, `IndustryVertical`,
    `IndustryVerticalOther` during trial activation.

3d. Add a new method `UpdateBaselineAsync` for the deferred baseline capture:
    ```csharp
    Task UpdateBaselineAsync(
        Guid tenantId,
        decimal? manualPrepHoursPerReview,
        int? peoplePerReview,
        DateTimeOffset? capturedUtc,
        CancellationToken ct);
    ```
    SQL: `UPDATE dbo.Tenants SET BaselineManualPrepHoursPerReview = @ManualPrepHours,
    BaselinePeoplePerReview = @PeoplePerReview, BaselineManualPrepCapturedUtc = @CapturedUtc
    WHERE Id = @TenantId;`

3e. Add `UpdateBaselineAsync` to `ITenantRepository` interface in
    `ArchLucid.Core/Tenancy/ITenantRepository.cs`.

3f. Update `InMemoryTenantRepository` with the same new method (update the in-memory
    dictionary entry).

## Layer 4 — API registration model (`ArchLucid.Api/Models/Tenancy/TenantRegistrationRequest.cs`)

Add four new optional properties:

- `[MaxLength(30)] string? CompanySize` — valid values: "1-10", "11-50", "51-200",
  "201-1000", "1001-5000", "5001-50000", "50001+".
- `int? ArchitectureTeamSize` — must be > 0 and <= 10000 if provided.
- `[MaxLength(100)] string? IndustryVertical` — must be one of the curated values
  or "Other".
- `[MaxLength(200)] string? IndustryVerticalOther` — required when
  IndustryVertical == "Other", ignored otherwise.

## Layer 5 — Registration controller (`ArchLucid.Api/Controllers/RegistrationController.cs`)

5a. After the existing baseline validation block, add validation for:
    - `CompanySize`: if provided, must be in the allowed set; return 400 if not.
    - `ArchitectureTeamSize`: if provided, must be > 0 and <= 10000; return 400 if not.
    - `IndustryVertical`: if provided, must be in the curated list; return 400 if not.
    - `IndustryVerticalOther`: if `IndustryVertical == "Other"` and
      `IndustryVerticalOther` is blank, return 400.

5b. Pass the four new values through to `_trialBootstrap.TryBootstrapAfterSelfRegistrationAsync`.
    This requires extending either the capture DTO or adding a new DTO alongside
    `TrialSignupBaselineReviewCycleCapture`.

5c. Audit-log the new fields in the `TrialBaselineReviewCycleCaptured` event data
    (extend the anonymous object in DataJson).

## Layer 6 — Deferred baseline API endpoint

Create `ArchLucid.Api/Controllers/Tenancy/TenantBaselineController.cs`:

- `[Authorize] [ApiVersion("1.0")] [Route("v{version:apiVersion}/tenant/baseline")]`
- `PUT` endpoint: accepts `{ manualPrepHoursPerReview?: decimal, peoplePerReview?: int }`.
- Validates ranges (same as registration: > 0, <= 10000).
- Calls `ITenantRepository.UpdateBaselineAsync`.
- Emits audit event `TrialBaselineManualPrepCaptured`.
- Returns 200 with the updated values.
- Emits instrumentation counter `ArchLucidInstrumentation.RecordBaselineManualPrepCaptured()`.

## Layer 7 — Value report pipeline integration

7a. Extend `ValueReportRawMetrics` (in `ArchLucid.Persistence/Value/ValueReportRawMetrics.cs`)
    with two new fields:
    - `decimal? TenantBaselineManualPrepHoursPerReview`
    - `int? TenantBaselinePeoplePerReview`

7b. Extend `IValueReportMetricsReader.ReadAsync` and its two implementations
    (`DapperValueReportMetricsReader`, `InMemoryValueReportMetricsReader`) to read
    the new columns from the tenant row.

7c. Extend `ValueReportSnapshot` with:
    - `decimal? TenantBaselineManualPrepHoursPerReview`
    - `int? TenantBaselinePeoplePerReview`

7d. In `ValueReportBuilder.BuildAsync`: when `TenantBaselineManualPrepHoursPerReview`
    is not null, use it instead of the model constant
    `BaselineArchitectHoursBeforeArchLucidPerCommittedManifest` for the manifest-hours
    computation. When `TenantBaselinePeoplePerReview` is not null, multiply the
    hourly rate by (peoplePerReview / averageTeamSize) as a team-cost scaling factor.
    When null, fall back to current model constants (no behavior change for existing
    tenants).

7e. In `ValueReportReviewCycleSectionFormatter`: extend `ReviewCycleBaselineProvenance`
    enum with `TenantSuppliedViaSettings` to distinguish signup-time capture from
    deferred-settings-page capture (the "captured at" line should reflect the source).

## Layer 8 — UI: signup form updates (`archlucid-ui/src/`)

8a. Update `archlucid-ui/src/lib/signup-schema.ts`:
    - Expand `companySizeOptions` to:
      `["1-10", "11-50", "51-200", "201-1000", "1001-5000", "5001-50000", "50001+"]`
    - Add `industryVerticalOptions`:
      `["Healthcare", "Financial Services", "Technology", "Government / Public Sector",
        "Manufacturing", "Retail", "Insurance", "Energy / Utilities", "Education",
        "Telecommunications", "Other"]`
    - Add schema fields:
      `architectureTeamSize: z.string().optional()` (numeric string, validated in
      superRefine like baseline hours)
      `industryVertical: z.enum(industryVerticalOptions).optional()`
      `industryVerticalOther: z.string().max(200).optional()` (required when
      industryVertical is "Other", enforced in superRefine)

8b. Update `archlucid-ui/src/components/marketing/SignupForm.tsx`:
    - Add "Architecture team size" numeric input (optional, after company size).
    - Add "Industry" select dropdown (optional, after architecture team size).
    - Add "Industry (specify)" text input, shown only when "Other" is selected.
    - Include `companySize`, `architectureTeamSize`, `industryVertical`,
      `industryVerticalOther` in the POST payload to `/api/proxy/v1/register`.
    - Remove the `sessionStorage.setItem("archlucid_signup_company_size", ...)`
      line — no longer needed since it's persisted server-side.

8c. Update `archlucid-ui/src/components/marketing/SignupForm.test.tsx`:
    - Add test for expanded company-size dropdown rendering all options.
    - Add test for industry vertical dropdown rendering all options.
    - Add test for "Other" industry showing the free-text input.
    - Add test for architecture team size validation (non-numeric, zero, negative).

## Layer 9 — UI: deferred baseline settings page

9a. Create `archlucid-ui/src/app/(operator)/settings/baseline/page.tsx`:
    - Authenticated page (operator layout).
    - Title: "Baseline settings — ROI measurement"
    - Intro copy: "These fields tighten the 'before' anchor for your value reports.
      If you skip them, we use conservative model defaults. You can update them
      at any time."
    - Two fields:
      - "Manual preparation hours per review" (numeric input, optional)
      - "People involved per review" (numeric input, optional)
    - "Save" button that PUTs to `/api/proxy/v1/tenant/baseline`.
    - On load, GET the current tenant record and pre-fill if values exist.
    - Toast on success / error.

9b. Add nav entry in `archlucid-ui/src/lib/nav-config.ts`:
    - Under the `settings` group (or create one if none exists), add:
      ```ts
      {
        href: "/settings/baseline",
        label: "Baseline settings",
        title: "Baseline settings — ROI measurement inputs",
        icon: BarChart3,
        tier: "extended",
        requiredAuthority: "ExecuteAuthority",
      }
      ```

9c. Add test `archlucid-ui/src/app/(operator)/settings/baseline/page.test.tsx`:
    - Renders the form with empty defaults.
    - Submits with valid values — mock PUT succeeds — success toast shown.
    - Validation: rejects zero and negative values.
    - Pre-fills when tenant record has existing values.

## Layer 10 — Audit events

Add to `ArchLucid.Core/Audit/AuditEventTypes.cs`:
- `TrialBaselineManualPrepCaptured` (used by the new PUT endpoint)
- `TrialBaselineManualPrepUpdated` (used when values are changed after initial set)

## Layer 11 — Tests (.NET)

11a. `ArchLucid.Api.Tests/RegistrationControllerStructuredBaselineTests.cs`:
    - Registration with valid company size, architecture team size, industry vertical.
    - Registration with invalid company size returns 400.
    - Registration with architecture team size <= 0 returns 400.
    - Registration with IndustryVertical = "Other" but blank IndustryVerticalOther
      returns 400.
    - Registration without any of the new fields still succeeds (backward compatible).

11b. `ArchLucid.Api.Tests/TenantBaselineControllerTests.cs`:
    - PUT with valid manual prep hours and people per review returns 200.
    - PUT with zero or negative values returns 400.
    - PUT updates the tenant record (verify via repository read-back).
    - PUT emits the correct audit event.

11c. `ArchLucid.Persistence.Tests/Tenancy/Migration102_StructuredBaselineColumnsTests.cs`:
    - Verify columns exist after migration.
    - Verify check constraints reject invalid values.
    - Verify rollback script removes columns cleanly.

11d. `ArchLucid.Application.Tests/Value/ValueReportBuilderStructuredBaselineTests.cs`:
    - When `TenantBaselineManualPrepHoursPerReview` is provided, the value report
      uses it instead of the model constant.
    - When null, falls back to model constant (existing behavior preserved).
    - When `TenantBaselinePeoplePerReview` is provided, the team-cost scaling
      factor is applied.

## Constraints

- Do not modify historical SQL migration files (001–101). Only add new migration 102.
- Do not break existing tenants: all new columns are NULL, all new API fields are
  optional, all value-report fallbacks use existing model constants when tenant data
  is absent.
- Do not remove `sessionStorage` for company size until the server-side persistence
  is confirmed working.
- The deferred baseline fields (manual prep hours, people per review) are NOT on the
  signup form — they are ONLY on the settings page. The signup form captures review-
  cycle hours (existing), company size, architecture team size, and industry vertical.
- Follow the existing audit-event pattern: structured JSON in `DataJson`, actor from
  the authenticated principal.

## Acceptance criteria

1. `dotnet build` succeeds with zero warnings in the new files.
2. All new .NET tests pass: `dotnet test --filter "StructuredBaseline|TenantBaseline|Migration102|ValueReportBuilderStructuredBaseline"`
3. `cd archlucid-ui && npm test` — all signup and baseline settings tests pass.
4. Existing registration flow works unchanged when new fields are omitted.
5. Value report with tenant-supplied manual-prep hours shows customer-specific ROI
   instead of model-constant placeholder.
6. Value report without tenant-supplied data shows identical output to today (no
   regression).
```
