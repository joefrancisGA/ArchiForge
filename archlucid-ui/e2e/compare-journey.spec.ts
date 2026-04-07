import { expect, test } from "@playwright/test";

import { FIXTURE_LEFT_RUN_ID, FIXTURE_RIGHT_RUN_ID } from "./fixtures";
import {
  expectComparisonRequestOutcomeVisible,
  gotoComparePageWithFixturePair,
} from "./helpers/operator-journey";
import { registerDefaultPairLegacyStructuredCompare } from "./helpers/register-operator-api-routes";

test.describe("operator journey — compare query prefill and review order", () => {
  test("prefills from URL, runs legacy then structured mocks, shows review order and last request summary", async ({
    page,
  }) => {
    await registerDefaultPairLegacyStructuredCompare(page);
    await gotoComparePageWithFixturePair(page);

    await expect(page.getByPlaceholder("Base run ID (left)")).toHaveValue(FIXTURE_LEFT_RUN_ID);
    await expect(page.getByPlaceholder("Target run ID (right)")).toHaveValue(FIXTURE_RIGHT_RUN_ID);

    await expect(page.getByRole("heading", { name: "Compare runs", level: 2 })).toBeVisible();
    await expect(page.getByText(/read .*structured first/i)).toBeVisible();
    await expect(page.getByText(/legacy flat diff/i)).toBeVisible();

    await page.getByRole("button", { name: "Compare" }).click();
    await expectComparisonRequestOutcomeVisible(page);

    await expect(page.getByRole("heading", { name: "Structured manifest comparison", level: 3 })).toBeVisible();
    await expect(page.locator("#compare-structured")).toBeVisible();
    await expect(page.getByText(/Fixture highlight alpha/i)).toBeVisible();

    await expect(page.getByRole("heading", { name: /Authority run \/ manifest diff \(legacy\)/ })).toBeVisible();
    await expect(page.locator("#compare-legacy")).toBeVisible();
    await expect(page.getByRole("cell", { name: "topology", exact: true })).toBeVisible();
    await expect(page.getByRole("cell", { name: "serviceCount", exact: true })).toBeVisible();

    const reviewNav = page.getByRole("navigation", { name: "Comparison results outline" });
    await expect(reviewNav.getByText("Review order", { exact: true })).toBeVisible();
    await expect(reviewNav.getByRole("link", { name: "Structured manifest comparison" })).toBeVisible();
    await expect(reviewNav.getByRole("link", { name: "Legacy authority diff" })).toBeVisible();

    const outcome = page.getByRole("region", { name: "Comparison request outcome" });
    await expect(outcome.getByRole("heading", { name: "Last compare request", level: 3 })).toBeVisible();
    await expect(outcome).toContainText(FIXTURE_LEFT_RUN_ID);
    await expect(outcome).toContainText(FIXTURE_RIGHT_RUN_ID);
    await expect(outcome.getByText("Structured manifest")).toBeVisible();
    await expect(outcome.getByText("Legacy run / manifest diff")).toBeVisible();
    await expect(outcome.getByText("OK")).toHaveCount(2);
  });
});
