# Change Set 57R — operator-journey E2E (Playwright)

## Prompt 1 — deterministic fixtures + proxy route interception

**Scope:** `archlucid-ui` only. No production behavior changes.

**Delivered:**

- `e2e/fixtures/` — typed JSON-shaped payloads aligned with `coerceRunDetail`, `coerceManifestSummary`, `coerceArtifactDescriptorList`, `coerceRunComparison`, `coerceGoldenManifestComparison`, `coerceComparisonExplanation`.
- `e2e/helpers/route-match.ts` — centralized pathname + query matching for `/api/proxy/...` → backend paths (avoids brittle full-URL string equality).
- `e2e/helpers/register-operator-api-routes.ts` — single `page.route('**/*')` dispatcher with `registerOperatorJourneyApiRoutes(page, config)`; presets `registerCompareAndExplainRoutes`, `registerDefaultRunManifestArtifactRoutes`; optional artifact bundle GET/HEAD.
- `e2e/compare-proxy-mock.spec.ts` — exercises **client** compare flow (browser → `/api/proxy`) with mocks.
- `e2e/smoke.spec.ts` — assertions updated to match the current home page (`ArchLucid` **h1** in layout, **Start here** **h2** on `/`).

**Note:** Run and manifest **RSC** pages call the API from the Next server (`getServerApiBaseUrl`); they are **not** covered by `page.route` interception. Prompt 2 adds a **loopback mock HTTP server** started alongside Next for Playwright so RSC receives the same fixture payloads.

---

## Prompt 2 — run detail → manifest → back (E2E)

**Delivered:**

- `e2e/mock-archlucid-api-server.ts` — serves `GET /health`, run detail, manifest summary, and artifact list for fixture IDs (imports `e2e/fixtures`).
- `e2e/start-e2e-with-mock.ts` — Playwright `webServer` entry: starts mock on **127.0.0.1:18765** (override with `E2E_MOCK_API_PORT`), sets **`ARCHIFORGE_API_BASE_URL`**, then `next start -p 3000`.
- `e2e/run-manifest-journey.spec.ts` — linear journey with role/text assertions (no snapshots).
- `playwright.config.ts` — `webServer` runs **build** then **start-e2e-with-mock** (not `npm run start` alone).
- **`tsx`** devDependency — runs the TypeScript mock + launcher.
- Root `tsconfig.json` **`exclude`: `e2e`** so Next build does not typecheck E2E-only files; **`e2e/tsconfig.json`** + **`npm run typecheck:e2e`** cover them.

**Caveat:** `reuseExistingServer: true` with a hand-started `npm run start` that does **not** point at the mock will fail this journey until you use the Playwright-managed stack or set **`ARCHIFORGE_API_BASE_URL=http://127.0.0.1:18765`** and run the mock separately.

---

## Prompt 3 — compare journey (query prefill + review order)

**Delivered:**

- `e2e/compare-journey.spec.ts` — opens `/compare?leftRunId&rightRunId` with fixture IDs; asserts placeholder inputs prefilled; **`registerOperatorJourneyApiRoutes`** with legacy + structured fixtures only (no AI); clicks **Compare**; asserts **Compare runs** heading, 55R-style guidance (**structured first** / **legacy flat diff**), **`#compare-structured`** and **`#compare-legacy`**, **Review order** nav (structured link before legacy), **Last compare request** region with both outcomes **OK**; uses fixture-backed rows (**topology** / **serviceCount**) for legacy visibility. Waits on visible content only (no fixed sleeps).

---

## Prompt 4 — compare stale input warning

**Delivered:**

- `e2e/compare-stale-input-warning.spec.ts` — self-contained flow: mock legacy + structured, compare, change base run ID, assert **`OperatorWarningCallout`** copy (**Run IDs no longer match the results below.**, **Content below still reflects**, prior pair in **`code`**, **restore the previous values**); then restore the original left ID and assert the warning copy is gone.

---

## Prompt 5 — manifest empty artifact list vs bundle affordance

**Delivered:**

- **`FIXTURE_MANIFEST_EMPTY_ARTIFACTS_ID`** + **`fixtureManifestSummaryEmptyArtifacts()`** — same coercion contract as other manifest summaries; artifact list stub returns **`[]`** for that id only.
- **`e2e/mock-archlucid-api-server.ts`** — routes summary + artifact list for the new manifest id (empty array).
- **`e2e/manifest-empty-artifacts.spec.ts`** — RSC load of `/manifests/...`; asserts **no** artifact-list **failure/malformed** callouts; **`OperatorEmptyState`** (**No artifacts listed for this manifest**) with **valid empty result** + **Bundle ZIP may return 404** copy; **Download bundle (ZIP)** link present with **`href`** containing manifest id and **`bundle`**; **no** artifact table headers. File-level comment documents distinction vs request failures and bundle semantics.

**Out of scope (per prompt):** no simulated bundle download / `page.route` click-through — keeps the spec stable; operator copy already separates empty list from ZIP availability.

---

## Prompt 6 — Playwright harness cleanup and readability pass

**Scope:** `archlucid-ui/e2e` only. Small helpers; specs stay explicit.

**Delivered:**

- **`registerDefaultPairLegacyStructuredCompare(page)`** in `e2e/helpers/register-operator-api-routes.ts` — single definition of legacy + structured mocks for the standard left/right fixture pair; **`registerCompareAndExplainRoutes`** reuses the same config and adds AI explain only.
- **`e2e/helpers/operator-journey.ts`** — operator-oriented navigation (`gotoComparePageWithFixturePair`, `gotoRunDetailForMockFixtureRun`, `gotoManifestDetail`, `gotoManifestEmptyArtifactsOperatorCase`), **`comparePairSearchParams`** for deterministic query strings, and **`expectComparisonRequestOutcomeVisible`** where it removed duplication.
- **`compare-journey.spec.ts`**, **`compare-stale-input-warning.spec.ts`**, **`run-manifest-journey.spec.ts`**, **`manifest-empty-artifacts.spec.ts`** — refactored to use the helpers above; **`compare-proxy-mock.spec.ts`** unchanged (still uses **`registerCompareAndExplainRoutes`**).

---

## Prompt 7 — optional Playwright from release-smoke

**Scope:** Root **`release-smoke.ps1`** / **`release-smoke.cmd`** and **`docs/RELEASE_SMOKE.md`**. Default behavior unchanged.

**Delivered:**

- **`-RunPlaywright`** — after the normal smoke steps (UI and, unless **`-SkipE2E`**, API+CLI+artifact checks), runs **`archlucid-ui`** **`npm run test:e2e`** with **`CI=1`**. Section header **`=== Playwright E2E (opt-in: -RunPlaywright) ===`**. Exits non-zero if Playwright fails; errors if Node is missing when the flag is set.
- **`-SkipE2E`** path still runs Playwright when **`-RunPlaywright`** is set (after UI); **`npm ci`** runs in **`archlucid-ui`** when **`-SkipUi`** or missing **`node_modules`** so E2E can run without the standard UI step.
- **`release-smoke.cmd`** passes **`%*`** unchanged (flags work from CMD). **`docs/RELEASE_SMOKE.md`** documents the switch, examples, and Playwright troubleshooting.

---

## Prompt 8 — documentation for 57R E2E contract

**Scope:** Docs only; wording aligned with **`e2e/*.spec.ts`**, **`playwright.config.ts`**, and **`release-smoke`** behavior.

**Delivered:**

- **`archlucid-ui/docs/TESTING_AND_TROUBLESHOOTING.md`** — section 8 rewritten: per-spec journey table, mock strategies (loopback server vs **`page.route`**), explicit **non-goals**, how to run **`npm run test:e2e`** / **`test-ui-smoke`** / **`-RunPlaywright`**, troubleshooting note when mocks pass but a real API fails.
- **`archlucid-ui/README.md`** — Tests + doc table updated for **57R** Playwright scope and links.
- **`docs/RELEASE_SMOKE.md`** — subsection **What `-RunPlaywright` actually exercises (57R)**; independence from C# API smoke; restored **`-RunPlaywright`** row in the parameters table.
- **`README.md`** — Key docs table, pilot handoff paragraph, Operator UI paragraph: concise **57R** / Playwright pointers without overstating coverage.

---

## Prompt 9 — focused validation pass

**Scope:** Run Vitest, Playwright, and repo UI smoke scripts; fix failures only where needed for a coherent slice.

**Validation run (green):**

- **`archlucid-ui`:** `npm test` (71 tests), `npm run typecheck:e2e`, `CI=1` / `npm run test:e2e` (6 Playwright tests).
- **Repo root:** `.\test-ui-smoke.ps1` after script fix.

**Fix delivered:**

- **`test-ui-smoke.ps1`**, **`test-ui-unit.ps1`**, **`release-smoke.ps1`** — on Windows, call **`npm.cmd`** (and **`npx.cmd`** in smoke) when available so **`Set-StrictMode -Version Latest`** does not execute Node’s **`npm.ps1`** shim (which can throw **`PropertyNotFoundStrict`** on **`$MyInvocation.Statement`**). Non-Windows unchanged (**`npm`** only).
