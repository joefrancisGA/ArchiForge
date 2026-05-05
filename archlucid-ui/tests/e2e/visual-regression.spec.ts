import { expect, test } from "@playwright/test";

import { FIXTURE_LEFT_RUN_ID, FIXTURE_RIGHT_RUN_ID } from "../../e2e/fixtures";
import { gotoComparePageWithFixturePair, gotoRunDetailForMockFixtureRun } from "../../e2e/helpers/operator-journey";
import { registerDefaultPairLegacyStructuredCompare } from "../../e2e/helpers/register-operator-api-routes";

/** Shared options for stable viewports (mock operator UI; golden files live next to this spec). */
const screenshotOptions = {
  animations: "disabled" as const,
  caret: "hide" as const,
  fullPage: true,
};

test.describe("visual regression — operator UI", () => {
  test("main dashboard matches golden baseline", async ({ page }) => {
    await page.goto("/reviews?projectId=default");

    await expect(page.getByRole("heading", { name: /^Architecture reviews$/i })).toBeVisible();
    await expect(page.locator('[data-testid^="runs-row-"]').first()).toBeVisible();

    await expect(page).toHaveScreenshot("main-dashboard.png", screenshotOptions);
  });

  test("run detail matches golden baseline", async ({ page }) => {
    await gotoRunDetailForMockFixtureRun(page);

    await expect(
      page.getByRole("heading", {
        level: 1,
        name: /Claims Intake Modernization — integration boundaries/i,
      }),
    ).toBeVisible();
    await expect(page.getByTestId("app-shell-main").getByRole("navigation", { name: "Breadcrumb" })).toContainText(
      "Reviews",
    );

    await expect(page).toHaveScreenshot("run-detail.png", screenshotOptions);
  });

  test("comparison view matches golden baseline", async ({ page }) => {
    await registerDefaultPairLegacyStructuredCompare(page);
    await gotoComparePageWithFixturePair(page);

    await expect(page.getByRole("heading", { name: "Compare reviews", level: 2 })).toBeVisible();
    await expect(page.locator("#compare-left-run-id")).toHaveValue(FIXTURE_LEFT_RUN_ID);
    await expect(page.locator("#compare-right-run-id")).toHaveValue(FIXTURE_RIGHT_RUN_ID);

    await page.getByRole("button", { name: "Compare", exact: true }).click();
    await expect(page.locator("#compare-structured")).toBeVisible();

    await page.locator("#compare-technical").locator(":scope > summary").click();
    await expect(page.locator("#compare-legacy")).toBeVisible();
    await expect(page.getByRole("heading", { name: "Manifest comparison", level: 3 })).toBeVisible();
    await expect(page.getByRole("heading", { name: "Run-level diff", level: 3 })).toBeVisible();

    await expect(page).toHaveScreenshot("comparison-view.png", screenshotOptions);
  });
});
