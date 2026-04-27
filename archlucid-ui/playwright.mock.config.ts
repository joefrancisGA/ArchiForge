import { defineConfig, devices } from "@playwright/test";

/**
 * Mock-backed operator UI Playwright suite (loopback mock API on 18765).
 * On-demand: `npx playwright test -c playwright.mock.config.ts` or `npm run test:e2e:mock`.
 * Merge-blocking live journeys use the default `playwright.config.ts` in CI (`ui-e2e-live`).
 *
 * If `MOCK_E2E_SKIP_NEXT_BUILD=1`, the webServer only runs `start-e2e-with-mock` (assumes `npm run build` already ran).
 */
const mockE2eSkipNextBuild = process.env.MOCK_E2E_SKIP_NEXT_BUILD === "1";
const mockWebServerCommand = mockE2eSkipNextBuild
  ? "npx tsx --tsconfig e2e/tsconfig.json e2e/start-e2e-with-mock.ts"
  : "npm run build && npx tsx --tsconfig e2e/tsconfig.json e2e/start-e2e-with-mock.ts";

/** When 3000 is taken (e.g. another dev server), set `MOCK_E2E_PORT=3001` and `PORT=3001`. */
const mockE2ePort = process.env.MOCK_E2E_PORT ?? process.env.PORT ?? "3000";
const mockBaseUrl = `http://127.0.0.1:${mockE2ePort}`;

/**
 * Full build + start: allow 30m (slow CI / cold disk).
 * Skip-build path: copy `.next/static` + `public` into standalone + mock + node can exceed 2m on slow disks/AV.
 */
const mockWebServerStartupTimeoutMs = mockE2eSkipNextBuild ? 10 * 60 * 1_000 : 30 * 60 * 1_000;

export default defineConfig({
  testDir: "e2e",
  testIgnore: "**/live-api-*.spec.ts",
  fullyParallel: true,
  forbidOnly: Boolean(process.env.CI),
  retries: process.env.CI ? 1 : 0,
  workers: 1,
  use: {
    baseURL: mockBaseUrl,
    trace: "on-first-retry",
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
  webServer: {
    command: mockWebServerCommand,
    url: mockBaseUrl,
    reuseExistingServer: !process.env.CI,
    /** Build + standalone sync can be slow; with skip-build, only the mock + Next need to start. */
    timeout: mockWebServerStartupTimeoutMs,
  },
});
