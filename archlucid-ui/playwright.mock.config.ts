import { defineConfig, devices } from "@playwright/test";

/**
 * Mock-backed operator UI Playwright suite (loopback mock API on 18765).
 * On-demand: `npx playwright test -c playwright.mock.config.ts` or `npm run test:e2e:mock`.
 * Merge-blocking live journeys use the default `playwright.config.ts` in CI (`ui-e2e-live`).
 *
 * If `MOCK_E2E_SKIP_NEXT_BUILD=1`, the webServer only runs `start-e2e-with-mock` (assumes `npm run build` already ran).
 * By default the UI port is **not** reused (avoids screenshot/E2E hitting the wrong process on 3000). Set
 * `MOCK_E2E_REUSE_SERVER=1` to reuse an existing listener when you intentionally run standalone yourself.
 * After a one-time `npm run build`, prefer `npm run screenshots:all:prebuilt` to avoid the webServer re-running
 * a full build (faster, clearer failures). PNGs for `capture-all` land under `public/screenshots/all-routes/`.
 */
const mockE2eSkipNextBuild = process.env.MOCK_E2E_SKIP_NEXT_BUILD === "1";
const mockWebServerCommand = mockE2eSkipNextBuild
  ? "npx tsx --tsconfig e2e/tsconfig.json e2e/start-e2e-with-mock.ts"
  : "npm run build && npx tsx --tsconfig e2e/tsconfig.json e2e/start-e2e-with-mock.ts";

/** When 3000 is taken (e.g. another dev server), set `MOCK_E2E_PORT=3001` and `PORT=3001`. */
const mockE2ePort = process.env.MOCK_E2E_PORT ?? process.env.PORT ?? "3000";
const mockBaseUrl = `http://127.0.0.1:${mockE2ePort}`;

/** Time until `webServer` URL responds. Large copies + cold Node + AV can exceed 10m even without `npm run build`. */
const mockWebServerStartupTimeoutMs = 30 * 60 * 1_000;

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
    reuseExistingServer: process.env.MOCK_E2E_REUSE_SERVER === "1",
    /** Build + standalone sync can be slow; with skip-build, only the mock + Next need to start. */
    timeout: mockWebServerStartupTimeoutMs,
    env: {
      ...process.env,
      NEXT_PUBLIC_SUPPRESS_ONBOARDING_TOUR: "1",
      /** Client bundle: hide dev-only chrome in mock E2E/screenshot runs when set at build time via local env. */
      NEXT_PUBLIC_DEMO_MODE: process.env.NEXT_PUBLIC_DEMO_MODE ?? "true",
      /** Operator `/runs` / `/manifests` static fallback for demo parity with showcase when API is down. */
      NEXT_PUBLIC_DEMO_STATIC_OPERATOR: process.env.NEXT_PUBLIC_DEMO_STATIC_OPERATOR ?? "",
    },
  },
});
