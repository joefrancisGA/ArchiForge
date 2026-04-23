# Accessibility

Last reviewed: 2026-04-22

## Target compliance level

**WCAG 2.1 Level AA** — the ArchLucid operator UI targets conformance with the [Web Content Accessibility Guidelines 2.1](https://www.w3.org/TR/WCAG21/) at Level AA.

## Current status

**Baseline** — automated axe-core scanning covers the top 5 operator pages. Critical and serious violations are gated in CI; minor/moderate violations are tracked for incremental resolution.

### Pages with automated checks

| Page         | Route                       | Status  |
| ------------ | --------------------------- | ------- |
| Home         | `/`                         | Scanned |
| Runs         | `/runs?projectId=default`   | Scanned |
| Audit        | `/audit`                    | Scanned |
| Policy packs | `/policy-packs`             | Scanned |
| Alerts       | `/alerts`                   | Scanned |

## Tooling

| Tool                                      | Purpose              | Scope                 |
| ----------------------------------------- | -------------------- | --------------------- |
| **axe-core** via `@axe-core/playwright`   | Automated WCAG scan  | Playwright e2e suite  |
| **eslint-plugin-jsx-a11y**                | Static JSX linting   | ESLint (via Next.js)  |

CI enforcement: merge-blocking **`ui-e2e-live`** runs **`live-api-accessibility*.spec.ts`** (Playwright + **`@axe-core/playwright`**) against **live API + SQL**; critical/serious violations fail the job. Fast component-level checks run in **`ui-axe-components`** via Vitest + **jest-axe** on **`archlucid-ui/src/accessibility/**`**.

## Existing accessibility controls

- **Skip-to-content link**: first focusable element in `layout.tsx`, jumps to `#main-content` (visible on focus)
- **Language attribute**: `<html lang="en">`
- **Landmark navigation**: `<main>` on page components, `<nav>` with `aria-label` in `ShellNav`, `<header>` in layout
- **Form labels**: `<label>` wrapping on audit, policy packs, and alerts controls
- **Focus management**: custom `focus-visible` styles for nav links, workflow actions, and auth controls (`globals.css`)
- **Error regions**: `role="alert"` on API error messages

## Known exemptions

None at this time. Document any intentional deviations here with:

- The axe rule ID being exempted
- The affected page(s)
- The justification
- The planned resolution date (if temporary)

## Review cadence

**Annually.** The next review window is **2027-04-22**. The public attestation surface is the marketing route **`/accessibility`** (source: `archlucid-ui/src/app/(marketing)/accessibility/page.tsx`; live site when published: **https://archlucid.com/accessibility**).

Place the **annual accessibility policy review** on the **same owner calendar** as the independent **quality-assessment** cadence reminder (dated assessment series under `docs/` and prompts such as [`docs/QUALITY_IMPROVEMENT_PROMPTS.md`](docs/library/QUALITY_IMPROVEMENT_PROMPTS.md)).

## Expanding coverage

To add accessibility checks for a new page:

1. Add an entry to the `PAGES` array in `archlucid-ui/e2e/accessibility.spec.ts`.
2. Ensure the mock API server (`e2e/mock-archlucid-api-server.ts`) returns fixture data for that route when needed.
3. For route-level axe against a live API, run `npx playwright test` from **`archlucid-ui/`** with **`ArchLucid.Api`** up (see **`docs/LIVE_E2E_HAPPY_PATH.md`**). For component axe only: **`npm run test:axe-components`**.

## Manual testing guidance

Automated scanning catches roughly 30–40% of WCAG issues. Supplement with:

- **Keyboard navigation**: Tab, Shift+Tab, Enter, Escape through all interactive elements — verify visible focus indicators and logical tab order
- **Screen reader**: NVDA (Windows) or VoiceOver (macOS) — verify headings, landmarks, and dynamic content announce correctly
- **Zoom**: 200% browser zoom — verify no clipping or overlapping
- **Reduced motion**: `prefers-reduced-motion` — verify animations respect the preference (currently none are used)
