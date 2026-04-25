import { defineConfig, devices } from "@playwright/test";

/**
 * Dedicated Playwright config for the **staging trial-funnel TEST-mode** spec
 * (`e2e/trial-funnel-test-mode.spec.ts`). Drives a real browser against
 * `signup.staging.archlucid.com` (override with `STAGING_BASE_URL`) — there is
 * **no** local Next.js / API webServer because the spec hits staging directly.
 *
 * Skip behaviour: if `STRIPE_TEST_KEY` is unset the spec self-skips, so this
 * config is also safe to invoke from a developer's laptop without staging
 * credentials — it will run zero tests rather than fail.
 *
 * Wired into `.github/workflows/trial-funnel-test-mode.yml` for the nightly
 * staging run; the workflow injects `STRIPE_TEST_KEY` from the repo secret.
 */
export default defineConfig({
  testDir: "e2e",
  testMatch: ["trial-funnel-test-mode.spec.ts"],
  fullyParallel: false,
  forbidOnly: Boolean(process.env.CI),
  retries: process.env.CI ? 1 : 0,
  workers: 1,
  reporter: process.env.CI ? [["github"], ["list"]] : "list",
  use: {
    baseURL: process.env.STAGING_BASE_URL ?? "https://signup.staging.archlucid.com",
    trace: "retain-on-failure",
    video: "retain-on-failure",
    screenshot: "only-on-failure",
    actionTimeout: 30_000,
    navigationTimeout: 60_000,
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
});
