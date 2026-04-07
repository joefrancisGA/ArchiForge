import { expect, test } from "@playwright/test";

test.describe("policy packs — mock API journey", () => {
  test("create, publish, assign surfaces effective merged content", async ({ page }) => {
    await page.goto("/policy-packs");

    await expect(page.getByRole("heading", { name: "Policy packs", level: 2 })).toBeVisible();

    await page.getByRole("button", { name: "Create pack" }).click();
    await page.getByRole("button", { name: "Publish" }).click();
    await page.getByRole("button", { name: "Assign" }).click();

    await expect(page.getByText("e2e-mock-rule", { exact: false })).toBeVisible({ timeout: 60_000 });
  });
});
