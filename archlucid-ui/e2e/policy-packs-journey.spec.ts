import { expect, test } from "@playwright/test";

test.describe("policy packs — mock API journey", () => {
  test("create, publish, assign surfaces effective merged content", async ({ page }) => {
    test.setTimeout(120_000);

    await page.goto("/policy-packs");

    await expect(page.getByRole("heading", { name: "Policy packs", level: 2 })).toBeVisible();

    const createButton = page.getByRole("button", { name: "Create pack" });
    const publishButton = page.getByRole("button", { name: "Publish" });
    const assignButton = page.getByRole("button", { name: "Assign" });

    // Initial useEffect runs load(); Create pack / Refresh are disabled until listPolicyPacks + effective calls finish.
    await expect(createButton).toBeEnabled({ timeout: 60_000 });
    await createButton.click();
    // onCreate runs load() and selects the new pack; Publish enables when loading clears and selection is set.
    await expect(publishButton).toBeEnabled({ timeout: 60_000 });

    await publishButton.click();
    // Publish runs load() again; wait before clicking Assign so the button is stable and enabled.
    await expect(assignButton).toBeEnabled({ timeout: 60_000 });
    await assignButton.click();

    await expect(page.getByText("e2e-mock-rule", { exact: false })).toBeVisible({ timeout: 60_000 });
  });
});
