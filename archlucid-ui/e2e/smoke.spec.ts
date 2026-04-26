import { expect, test } from "@playwright/test";

test.describe("operator shell smoke", () => {
  test("home renders shell headings", async ({ page }) => {
    await page.goto("/");

    await expect(page.getByRole("heading", { name: "ArchLucid", level: 1 })).toBeVisible();
    await expect(page.getByText("Create Run")).toBeVisible();
  });
});
