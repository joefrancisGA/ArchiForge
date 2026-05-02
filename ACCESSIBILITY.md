# Accessibility

Last reviewed: 2026-04-25

## Target compliance level

**WCAG 2.1 Level AA** — the ArchLucid operator UI targets conformance with the [Web Content Accessibility Guidelines 2.1](https://www.w3.org/TR/WCAG21/) at Level AA.

## Current status

**Baseline** — merge-blocking **`@axe-core/playwright`** runs against the `PAGES` list in [`archlucid-ui/e2e/live-api-accessibility.spec.ts`](archlucid-ui/e2e/live-api-accessibility.spec.ts) (**62** URL patterns as of 2026-05-02, including the **15** high-traffic operator paths in the table below, plus marketing routes, legacy `/onboarding` redirects, run provenance, findings (showcase run), manifest variants, governance findings/policy packs, settings surfaces, product-learning, executive reviews, and admin/help routes). Deferred matrix-only routes are documented as `PAGES_DEFERRED` in the same spec. Critical and serious violations are gated in CI; minor/moderate violations are tracked for incremental resolution.

The **Vitest** axe job (`npm run test:axe-components`) is separate; see the **Tooling** table.

### Pages with automated checks

The following **15** routes are the **priority operator coverage** set (wizard, list/detail, compare, analysis, graph, governance, settings, and shared pilot surfaces). They are a **subset** of the full `PAGES` array in the Playwright file above; CI scans **all** `PAGES` entries.

| Page | Route | Status |
| ---- | ----- | ------ |
| Home | `/` | Scanned |
| New run (wizard) | `/runs/new` | Scanned |
| Runs | `/runs?projectId=default` | Scanned |
| Run detail (fixture) | `/runs/{runId}` (see `e2e/fixtures/ids.ts`) | Scanned |
| Compare | `/compare` | Scanned |
| Ask | `/ask` | Scanned |
| Graph | `/graph` | Scanned |
| Advisory | `/advisory` | Scanned |
| Governance dashboard | `/governance/dashboard` | Scanned |
| Governance workflow | `/governance` | Scanned |
| Tenant settings | `/settings/tenant` | Scanned |
| Value report | `/value-report` | Scanned |
| Audit | `/audit` | Scanned |
| Policy packs | `/policy-packs` | Scanned |
| Alerts inbox (hub) | `/alerts` | Scanned |

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

**Annually.** The next review window is **2027-04-25**. The public attestation surface is the marketing route **`/accessibility`** (source: `archlucid-ui/src/app/(marketing)/accessibility/page.tsx`; live site when published: **https://archlucid.net/accessibility**).

Place the **annual accessibility policy review** on the **same owner calendar** as the independent **quality-assessment** cadence reminder (dated assessment series under `docs/` and prompts such as [`docs/QUALITY_IMPROVEMENT_PROMPTS.md`](docs/library/QUALITY_IMPROVEMENT_PROMPTS.md)).

## Expanding coverage

To add accessibility checks for a new page:

1. Add an entry to the `PAGES` array in `archlucid-ui/e2e/live-api-accessibility.spec.ts` (and update this document’s table if the route is product-significant).
2. For **live** e2e: ensure the live API + SQL happy path in `e2e/start-e2e-live-api.ts` / fixture IDs (`e2e/fixtures/ids.ts`) includes data for dynamic routes when needed. For **mock** Playwright: use `npx playwright test -c playwright.mock.config.ts` (that config ignores `live-api-*.spec.ts`).
3. For route-level axe against a live API, run `npx playwright test` from **`archlucid-ui/`** with **`ArchLucid.Api`** up (see **`docs/LIVE_E2E_HAPPY_PATH.md`**). For component axe only: **`npm run test:axe-components`**.

## Manual testing guidance

Automated scanning catches roughly 30–40% of WCAG issues. Supplement with:

- **Keyboard navigation**: Tab, Shift+Tab, Enter, Escape through all interactive elements — verify visible focus indicators and logical tab order
- **Screen reader**: NVDA (Windows) or VoiceOver (macOS) — verify headings, landmarks, and dynamic content announce correctly
- **Zoom**: 200% browser zoom — verify no clipping or overlapping
- **Reduced motion**: `prefers-reduced-motion` — verify animations respect the preference (currently none are used)
