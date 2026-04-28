import { expect, test } from "@playwright/test";

import {
  SCREENSHOT_RUN_ID,
  SHOWCASE_DEMO_RUN_ID,
  SHOWCASE_STATIC_DEMO_MANIFEST_ID,
} from "./fixtures";

/**
 * Validates the mock-backed “proof chain”: runs list → run detail → manifest detail.
 * Run in isolation: `npx playwright test -c playwright.mock.config.ts e2e/demo-readiness.spec.ts`
 * or `npx playwright test --grep @demo-readiness`.
 */
test.describe.parallel("demo-readiness — mock proof chain @demo-readiness", () => {
  test("runs list shows Claims Intake example and run detail avoids not-found shells", async ({ page }) => {
    await page.goto("/runs?projectId=default");
    await expect(page.getByRole("heading", { name: /architecture runs/i })).toBeVisible();
    await expect(page.getByText(/Claims Intake Modernization/i).first()).toBeVisible();

    await page.goto(`/runs/${encodeURIComponent(SHOWCASE_DEMO_RUN_ID)}`);
    const main = page.locator("main");
    await expect(main).not.toContainText(/run not found/i);
    await expect(main).not.toContainText(/request failed/i);

    await page.goto(`/runs/${encodeURIComponent(SCREENSHOT_RUN_ID)}`);
    await expect(page.locator("main")).not.toContainText(/run not found/i);
  });

  test("showcase-aligned manifest UUID loads without manifest error shell", async ({ page }) => {
    await page.goto(`/manifests/${encodeURIComponent(SHOWCASE_STATIC_DEMO_MANIFEST_ID)}`);
    const main = page.locator("main");
    await expect(main).not.toContainText(/manifest summary could not be loaded/i);
    await expect(main).not.toContainText(/request failed/i);
  });
});
