import { expect, test } from "@playwright/test";

import { FIXTURE_RUN_ID } from "../../e2e/fixtures";
import {
  expectComparisonRequestOutcomeVisible,
  gotoComparePageWithFixturePair,
  gotoRunDetailForMockFixtureRun,
} from "../../e2e/helpers/operator-journey";
import { registerDefaultPairLegacyStructuredCompare } from "../../e2e/helpers/register-operator-api-routes";

/** Shared options for stable viewports (mock operator UI; golden files live next to this spec). */
const screenshotOptions = {
  animations: "disabled" as const,
  caret: "hide" as const,
  fullPage: true,
};

test.describe("visual regression — operator UI", () => {
  test("main dashboard matches golden baseline", async ({ page }) => {
    await page.goto("/runs?projectId=default");

    await expect(page.getByRole("heading", { name: /^Architecture runs$/i })).toBeVisible();
    await expect(page.locator('[data-testid^="runs-row-"]').first()).toBeVisible();

    await expect(page).toHaveScreenshot("main-dashboard.png", screenshotOptions);
  });

  test("run detail matches golden baseline", async ({ page }) => {
    await gotoRunDetailForMockFixtureRun(page);

    await expect(page.getByRole("heading", { name: "Run detail", level: 2 })).toBeVisible();
    await expect(page.getByText(/E2E fixture run \(no live API\)/)).toBeVisible();

    await expect(page).toHaveScreenshot("run-detail.png", screenshotOptions);
  });

  test("comparison view matches golden baseline", async ({ page }) => {
    await registerDefaultPairLegacyStructuredCompare(page);
    await gotoComparePageWithFixturePair(page);

    await expect(page.getByRole("heading", { name: "Compare reviews", level: 2 })).toBeVisible();

    await page.getByRole("button", { name: "Compare" }).click();
    await expect(page.locator("#compare-structured")).toBeVisible();
    await expect(page.locator("#compare-legacy")).toBeVisible();
    await expectComparisonRequestOutcomeVisible(page);

    await expect(page).toHaveScreenshot("comparison-view.png", screenshotOptions);
  });
});
