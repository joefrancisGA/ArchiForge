import { defineConfig, devices } from "@playwright/test";

/**
 * Default Playwright config: **live ArchLucid API + SQL** (operator journeys + live axe routes).
 * CI (`ui-e2e-live`) sets `LIVE_E2E_SKIP_NEXT_BUILD=1` after a separate `npm run build`.
 *
 * Mock-backed specs: `npx playwright test -c playwright.mock.config.ts`.
 */
const skipNextBuild = process.env.LIVE_E2E_SKIP_NEXT_BUILD === "1";
const liveWebServerCommand = skipNextBuild
  ? "npx tsx --tsconfig e2e/tsconfig.json e2e/start-e2e-live-api.ts"
  : "npm run build && npx tsx --tsconfig e2e/tsconfig.json e2e/start-e2e-live-api.ts";

export default defineConfig({
  testDir: "e2e",
  /**
   * `live-api-*.spec.ts` — journeys against real SQL + API.
   * `marketing-accessibility-public.spec.ts` — static marketing route (no API); still uses the live webServer bundle.
   */
  testMatch: ["live-api-*.spec.ts", "marketing-accessibility-public.spec.ts", "marketing-demo-preview.spec.ts"],
  fullyParallel: false,
  forbidOnly: Boolean(process.env.CI),
  retries: process.env.CI ? 1 : 0,
  workers: 1,
  use: {
    baseURL: "http://127.0.0.1:3000",
    trace: "on-first-retry",
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
  webServer: {
    command: liveWebServerCommand,
    url: "http://127.0.0.1:3000",
    reuseExistingServer: !process.env.CI,
    // Local `npm run build` + standalone asset sync routinely exceeds 3 minutes on Windows HDDs /
    // cold caches; CI uses LIVE_E2E_SKIP_NEXT_BUILD=1 and only waits for standalone boot (120s).
    timeout: skipNextBuild ? 120_000 : 600_000,
    env: {
      LIVE_API_URL: process.env.LIVE_API_URL ?? "http://127.0.0.1:5128",
      ARCHLUCID_PROXY_BEARER_TOKEN: process.env.ARCHLUCID_PROXY_BEARER_TOKEN ?? "",
      NEXT_PUBLIC_SUPPRESS_ONBOARDING_TOUR: "1",
    },
  },
});
