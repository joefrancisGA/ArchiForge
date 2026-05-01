# Operator shell navigation contract (`nav-config`)

Canonical implementation: `src/lib/nav-config.ts`, built by **`NavGroupBuilder`** classes in `src/lib/pilot-nav-group-builder.ts`, `operate-analysis-nav-group-builder.ts`, `operate-governance-nav-group-builder.ts`, and `operator-admin-nav-group-builder.ts`.

This document replaces the historical mega-comment on `nav-config.ts`. **API authorization stays on the server**; this file describes **UI shaping only**.

## API vs UI

- **`tier`** and **`requiredAuthority`** describe how the shell **should** present routes.
- **Computed visibility** is **`filterNavLinksForOperatorShell`** in **`nav-shell-visibility.ts`** (**tier → authority**; empty groups dropped).
- **`[Authorize(Policy = …)]`** on **ArchLucid.Api** is **authoritative** (`401`/`403`); nav omission or soft-disabled controls never imply a safe POST or deep link.

## Two shaping surfaces

This stack owns **Visibility metadata only**:

1. **Visibility** — `tier` + `requiredAuthority` in nav config + **`nav-shell-visibility.ts`** + **`useNavSurface().layerGuidance`** / **`LayerHeader`**.
2. **Capability** — **`useOperateCapability()`** + **`OperateCapabilityHints`** (Execute+ floor).

Enumeration: **`docs/library/PRODUCT_PACKAGING.md`** §3.

## Nav groups → buyer layers

| Group `id`           | `surface`           | Layer    | Notes |
|----------------------|---------------------|----------|--------|
| `pilot`              | `review-workflow`   | Pilot    | request · run · finalize · review |
| `operate-analysis`   | `review-workflow`   | Operate  | analysis slice — compare, replay, graph, Q&A, advisory, … |
| `operate-governance` | `review-workflow`   | Operate  | governance slice — policy, audit, alerts, trust — Execute+ for writes where noted |
| `operator-admin`     | `platform-admin`    | Admin    | system health, tenant cost, settings, support, users |

**Shell filter:** `listNavGroupsVisibleInOperatorShell(..., surfaceFilter)` can target **`review-workflow`** vs **`platform-admin`** so buyer-first chrome (sidebar, palette) can separate review work from administration without duplicating hrefs.

## Drift guard (contributors)

When adding or moving a route, follow the **ordered checklist** in **`docs/library/PRODUCT_PACKAGING.md`** §3 *Contributor drift guard* (API policy → nav config → `layer-guidance` / `LayerHeader` → **`useOperateCapability`** → packaging doc). Verify **C#** `[Authorize(Policy = …)]` still matches each link’s **`requiredAuthority`** string.

### Cross-module Vitest anchors

- **`authority-seam-regression.test.ts`** — e.g. **`/governance`** must stay **`ExecuteAuthority`** so Reader-ranked callers do not see it under Operate nav (deep-link still hits API policy); every **`ExecuteAuthority`** row under **`operate-analysis`** and **`operate-governance`** stays absent from Read-tier filtered nav; Pilot essential hrefs stay visible for Reader with default tier toggles; **caller rank `0`** stays stricter than Read for **`ReadAuthority`** links; **`/alerts`** stays **`essential`** tier; filtered link order and **`listNavGroupsVisibleInOperatorShell`** group order stay aligned with config; Operate governance href sets grow **monotonically** Read→Execute→Admin under **`filterNavLinksByAuthority`** alone; default Reader shell keeps **Operate analysis** to **`/ask`** only (tier before authority); **`/governance`** appears only when **extended and advanced** are on for **Execute** rank (**`filterNavLinksForOperatorShell`**).
- **`OperatorNavAuthorityProvider.test.tsx`** — **`useNavCallerAuthorityRank`** stays Read during JWT **`/me`** refetch so stale Execute rank does not flash in nav or hooks.
- **`OperateCapabilityHints.authority.test.tsx`** — rank-gated Operate sidebar/page cues share the same **`ExecuteAuthority`** numeric floor as **`useOperateCapability`** (governance resolution, audit log, **Alerts inbox**, **governance dashboard** reader cue, alert tooling).
- **`authority-execute-floor-regression.test.ts`** — same **boolean** for a synthetic **`ExecuteAuthority`** row vs **`enterpriseMutationCapabilityFromRank`**.
- **`authority-shaped-ui-regression.test.ts`** — every catalog **`ExecuteAuthority`** link hidden at Read / visible at Execute (new rows cannot drift untested); **`operate-governance`** monotonicity Reader→Admin.
- **`nav-shell-visibility.test.ts`** — Analysis extended **Execute** links (e.g. **`/replay`**) behind **Show more** — tier before rank.
- **`current-principal.test.ts`** — **`maxAuthority`** vs **`requiredAuthorityFromRank`** and **`hasEnterpriseOperatorSurfaces`** vs mutation capability.
- **`nav-config.structure.test.ts`** — duplicate **`href`**s; **Pilot** essentials omit **`requiredAuthority`**; **Operate** **`ExecuteAuthority`** links must not use **`essential`** tier (progressive disclosure + rank story).
- **`authority-shaped-layout-regression.test.tsx`** — **inspect-first** DOM when mutation hook is false (parallel to tier→authority story; still **UI only**).

## `layer-guidance.ts` / `LayerHeader`

**Operate · governance** route families use **`LAYER_PAGE_GUIDANCE`** rows with **`enterpriseFootnote`** (see **`authority-seam-regression.test.ts`** — Operate analysis vs governance footnote contract). That strip is **cognitive packaging only**; it does not replace **`requiredAuthority`** in nav config or **`[Authorize]`** on the API.

## `requiredAuthority` vs Operate POSTs

This field shapes **nav / palette visibility** after tier filtering only (higher **caller rank** does **not** bypass **`tier`** — e.g. Operate **extended** hrefs stay hidden until “Show more”; **`nav-shell-visibility.test.ts`**). In-page **POST / toggle** soft-enable on Operate-heavy routes uses **`useOperateCapability()`** (or deprecated **`useEnterpriseMutationCapability()`**) — same **`AUTHORITY_RANK.ExecuteAuthority`** floor as **`ExecuteAuthority`** links here; keep both aligned with C# policies. **Audit CSV export** is a documented exception: gated on **`/me`** roles (**Auditor** or **Admin**) on the audit page, not this nav field alone.

## Authority (`requiredAuthority`) — first-pass map

UI hint only; API still 401/403.

- **Omit** on Pilot *essentials* (home, getting-started, new run, runs) so Reader-signed-in pilots keep the default path.
- **Analysis · extended:** inspection/diff surfaces that are `ReadAuthority` on the API (`GraphController`, `AuthorityCompareController`) use **`ReadAuthority`**. **Replay** stays **`ExecuteAuthority`** (`AuthorityReplayController`).
- **Operate · analysis (`operate-analysis`):** every link sets **`requiredAuthority`**. Read/analytics pages → **`ReadAuthority`** unless the API primary workflow is Execute-class (planning, evolution candidates; advisory **schedules** and digest **subscriptions** are hub tabs under **`/advisory`** and **`/digests`** with in-page Execute gating). Link `title` strings use **“Label — short description”** for tooltips (same convention as governance slice).
- **Operate · governance (`operate-governance`):** **inbox / dashboards / audit / policy pack browsing / alert tooling** whose controllers are class-scoped **`ReadAuthority`** → **`ReadAuthority`**. **Governance workflow** (mutations) → **`ExecuteAuthority`**.
- **Operator admin (`operator-admin`, `platform-admin` surface):** **`/admin/health`** and **`/admin/users`** use **`AdminAuthority`**; **`advanced`** tier on system health keeps diagnostics off the default review shell. Other admin destinations use **`ReadAuthority`** / **`ExecuteAuthority`** as appropriate. Elsewhere under Operate, do not label list/browse pages **`AdminAuthority`** when the API is Read-class — POST-only admin actions stay on server policy.

## UI shaping vs API authorization (boundary)

**`[Authorize(Policy = …)]`** on **ArchLucid.Api** is authoritative (**401/403**) for every route and POST — always. `requiredAuthority` drives **shell visibility** after **`nav-shell-visibility`** tier filtering — not whether HTTP writes succeed. Keep policy **names** aligned with C# when moving routes.

**Vitest:** `nav-config.structure.test.ts` (graph invariants); **`authority-execute-floor-regression.test.ts`** (Execute-class nav row vs mutation capability; Operate **`operate-governance`** Reader vs Execute href sets); **`src/app/(operator)/operate-authority-ui-shaping.test.tsx`** (representative Operate pages: **`useOperateCapability`** → **`disabled`** on primary actions).

Omitting `requiredAuthority` is used only for **Pilot essentials** (default path for any authenticated rank). Every **Operate** nav link sets `requiredAuthority`. Composed with tiers in **`nav-shell-visibility`**.

Group IDs are intentionally stable (used as localStorage keys); only labels are user-visible.
