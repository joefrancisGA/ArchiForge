> **Scope:** Quick operator-shell audit — what a **buyer / pilot** sees in the first minute vs **platform admin** surfaces.

**Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md).

# Admin vs review workflow — first-60s UX notes

## Intent

- **Review workflow** (`NavShellSurface.review-workflow`): runs, findings, analysis, governance **inbox** — default collapsed sidebar emphasizes pilot path; command palette shows admin groups only after the user types a **non-empty** search.
- **Platform admin** (`platform-admin`): system health, tenant cost, support bundle, users — rendered in a separate sidebar cluster and mobile **Administration** block; deep links under `/admin/*` and `/settings/*` map here (`platform-admin-path.ts`).

## First 60 seconds (signed-in operator, default toggles)

| Moment | Buyer-safe expectation | Admin leakage check |
|--------|------------------------|---------------------|
| Land on home | Pilot essentials + **Governance** Alerts strip; no system health in primary nav | **Pass** — `/admin/health` only in **Admin** group |
| Sidebar collapsed | ≤8 links from review groups; admin links not folded into pilot row count | **Pass** — `countSidebarLinksHiddenByCollapsedPilot` skips `platform-admin` |
| “Show all features” | Extended analysis + governance + pilot findings; admin still segmented | **Pass** — surface discriminant on `NAV_GROUPS` |
| Command palette (empty query) | Jump lists for review groups; not a wall of admin URLs | **Pass** — admin groups require non-empty query |
| Demo mode (`NEXT_PUBLIC_DEMO_MODE`) | Trust center remains; alerts, audit, admin health hidden | **Pass** — demo omit set + operator-admin filter tests |

## Doc / test anchors

- **`archlucid-ui/docs/NAV_CONFIG_CONTRACT.md`** — surfaces + authority map.
- **`nav-config.structure.test.ts`** — `AdminAuthority` only on `operator-admin`.
- **`nav-shell-visibility.test.ts`** — system health via admin group; `surfaceFilter`; demo mode on admin group.

---

*Last reviewed: 2026-05-01 — aligns with nav surface split + palette gating.*
