# Change Set 57R — operator-journey E2E (Playwright)

## Prompt 1 — deterministic fixtures + proxy route interception

**Scope:** `archiforge-ui` only. No production behavior changes.

**Delivered:**

- `e2e/fixtures/` — typed JSON-shaped payloads aligned with `coerceRunDetail`, `coerceManifestSummary`, `coerceArtifactDescriptorList`, `coerceRunComparison`, `coerceGoldenManifestComparison`, `coerceComparisonExplanation`.
- `e2e/helpers/route-match.ts` — centralized pathname + query matching for `/api/proxy/...` → backend paths (avoids brittle full-URL string equality).
- `e2e/helpers/register-operator-api-routes.ts` — single `page.route('**/*')` dispatcher with `registerOperatorJourneyApiRoutes(page, config)`; presets `registerCompareAndExplainRoutes`, `registerDefaultRunManifestArtifactRoutes`; optional artifact bundle GET/HEAD.
- `e2e/compare-proxy-mock.spec.ts` — exercises **client** compare flow (browser → `/api/proxy`) with mocks.
- `e2e/smoke.spec.ts` — assertions updated to match the current home page (`ArchiForge` **h1** in layout, **Start here** **h2** on `/`).

**Note:** Run and manifest **RSC** pages call the API from the Next server (`getServerApiBaseUrl`); they are **not** covered by `page.route` interception. Use these fixtures with a server-side test strategy or env-pointed mock when adding SSR journey tests.
