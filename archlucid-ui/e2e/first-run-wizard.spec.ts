import { expect, test } from "@playwright/test";

test.describe("first-run wizard", () => {
  test("new run page renders wizard shell", async ({ page }) => {
    await page.goto("/runs/new");

    await expect(page.getByRole("heading", { name: "New run", level: 2 })).toBeVisible();
    await expect(
      page.getByText("Guided end-to-end wizard — from system description to pipeline tracking."),
    ).toBeVisible();
  });
});
