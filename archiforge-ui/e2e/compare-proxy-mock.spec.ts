import { expect, test } from "@playwright/test";

import { FIXTURE_LEFT_RUN_ID, FIXTURE_RIGHT_RUN_ID } from "./fixtures";
import { registerCompareAndExplainRoutes } from "./helpers/register-operator-api-routes";

test.describe("operator journey — compare proxy mocks", () => {
  test("client compare + explain calls are fulfilled without a live API", async ({ page }) => {
    await registerCompareAndExplainRoutes(page);

    const q = new URLSearchParams({
      leftRunId: FIXTURE_LEFT_RUN_ID,
      rightRunId: FIXTURE_RIGHT_RUN_ID,
    });
    await page.goto(`/compare?${q.toString()}`);

    await page.getByRole("button", { name: "Compare" }).click();
    await expect(page.getByText("Fixture highlight alpha:", { exact: false })).toBeVisible();

    await page.getByRole("button", { name: "Explain changes (AI)" }).click();
    await expect(page.getByText("E2E fixture: target run adds capacity", { exact: false })).toBeVisible();
  });
});
