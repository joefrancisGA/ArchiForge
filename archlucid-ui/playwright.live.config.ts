import { defineConfig, devices } from "@playwright/test";

/**
 * Live ArchLucid API + real SQL (CI only by default). Does not start the mock server.
 * Set LIVE_API_URL to the running API base (e.g. http://127.0.0.1:5128).
 *
 * CI: run `npm run build` before starting the API (see `.github/workflows/ci.yml` ui-e2e-live)
 * and set LIVE_E2E_SKIP_NEXT_BUILD=1 so webServer only starts Next standalone — avoids OOM
 * killing dotnet while Next build runs alongside ArchLucid.Api + SQL.
 */
const skipNextBuild = process.env.LIVE_E2E_SKIP_NEXT_BUILD === "1";
const liveWebServerCommand = skipNextBuild
  ? "npx tsx --tsconfig e2e/tsconfig.json e2e/start-e2e-live-api.ts"
  : "npm run build && npx tsx --tsconfig e2e/tsconfig.json e2e/start-e2e-live-api.ts";

export default defineConfig({
  testDir: "e2e",
  /** All `live-api-*.spec.ts` files share one worker and real SQL; keep naming convention when adding specs. */
  testMatch: ["live-api-*.spec.ts"],
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
    timeout: skipNextBuild ? 120_000 : 180_000,
    env: {
      LIVE_API_URL: process.env.LIVE_API_URL ?? "http://127.0.0.1:5128",
    },
  },
});
