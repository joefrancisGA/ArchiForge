> **Scope:** Operator-shell WCAG 2.1 AA axe coverage — what is enforced in CI and how to run locally.

# Accessibility audit (operator shell)

## Objective

Document how **critical** and **serious** WCAG 2.1 AA violations are blocked on the **operator shell** (Next.js app under `archlucid-ui/`) using **@axe-core/playwright** in merge-blocking live E2E.

## What CI enforces

| Surface | Config | Spec | WCAG tags |
|---------|--------|------|-----------|
| Operator + marketing routes used in live E2E | `archlucid-ui/playwright.config.ts` | `archlucid-ui/e2e/live-api-accessibility.spec.ts` | `wcag2a`, `wcag2aa`, `wcag21a`, `wcag21aa`, `best-practice` (see `e2e/helpers/axe-helper.ts`) |

The live suite builds the UI, starts the **live API + SQL** harness (`e2e/start-e2e-live-api.ts`), then visits each path in the spec’s `PAGES` array, waits for `main`, and fails if any violation has **impact** `critical` or `serious`.

**Marketing-only public route** (`marketing-accessibility-public.spec.ts`) is included in the same default Playwright `testMatch` for static marketing pages that do not require the operator shell proxy.

## Pages covered (operator shell)

The authoritative list is **`PAGES` in `archlucid-ui/e2e/live-api-accessibility.spec.ts`** (runs list, run detail, manifests, compare, replay, ask, search, advisory, graph, audit, policy packs, alerts, governance, digests, onboarding, trial signup, etc.).

## Local commands

From `archlucid-ui/`:

```bash
npm run test:e2e:live
```

Mock-backed UI tests (no live API): `npm run test:e2e:mock`.

Component-level axe (Vitest): `npm run test:axe-components`.

## Related

- **`docs/library/OBSERVABILITY.md`** — product metrics (not accessibility-specific).
- **`archlucid-ui/e2e/helpers/axe-helper.ts`** — axe builder wiring.
