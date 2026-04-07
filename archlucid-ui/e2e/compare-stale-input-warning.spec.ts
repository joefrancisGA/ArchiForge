import { expect, test } from "@playwright/test";

import { FIXTURE_LEFT_RUN_ID, FIXTURE_RIGHT_RUN_ID } from "./fixtures";
import {
  expectComparisonRequestOutcomeVisible,
  gotoComparePageWithFixturePair,
} from "./helpers/operator-journey";
import { registerDefaultPairLegacyStructuredCompare } from "./helpers/register-operator-api-routes";

test.describe("operator journey — compare stale input warning", () => {
  test("shows when run IDs change after a successful compare, clears when values match last request again", async ({
    page,
  }) => {
    await registerDefaultPairLegacyStructuredCompare(page);
    await gotoComparePageWithFixturePair(page);

    await page.getByRole("button", { name: "Compare" }).click();
    await expectComparisonRequestOutcomeVisible(page);

    const leftInput = page.getByPlaceholder("Base run ID (left)");
    await leftInput.fill(`${FIXTURE_LEFT_RUN_ID}-edited`);

    const staleCallout = page.getByRole("status").filter({
      has: page.getByText("Run IDs no longer match the results below.", { exact: true }),
    });
    await expect(staleCallout).toBeVisible();
    await expect(staleCallout.getByText(/Content below still reflects/)).toBeVisible();
    await expect(staleCallout.getByRole("code").filter({ hasText: FIXTURE_LEFT_RUN_ID })).toBeVisible();
    await expect(staleCallout.getByRole("code").filter({ hasText: FIXTURE_RIGHT_RUN_ID })).toBeVisible();
    await expect(staleCallout.getByText(/restore the previous values/)).toBeVisible();

    await leftInput.fill(FIXTURE_LEFT_RUN_ID);
    await expect(
      page.getByText("Run IDs no longer match the results below.", { exact: true }),
    ).not.toBeVisible();
  });
});
