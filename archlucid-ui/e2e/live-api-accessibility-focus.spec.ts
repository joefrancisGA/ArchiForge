import { expect, test } from "@playwright/test";

import { runAxe } from "./helpers/axe-helper";

/** Matches `NAV_GROUPS` entry `id: "runs-review"` → `label` in `src/lib/nav-config.ts` (sidebar `<nav aria-label>`). */
const operatorCorePilotNavLabel = "Core Pilot";

/** Live API + SQL focus/announcer checks (merge-blocking via `ui-e2e-live`). */
test.describe("route focus and announcements", () => {
  test("skip link moves focus to main content", async ({ page }) => {
    await page.goto("/", { waitUntil: "load" });
    await page.locator("main").first().waitFor({ state: "visible", timeout: 60_000 });

    await page.keyboard.press("Tab");
    await page.getByRole("link", { name: "Skip to main content" }).press("Enter");

    await expect(page.locator("#main-content")).toBeFocused({ timeout: 10_000 });
  });

  test("client navigation moves focus to main content", async ({ page }) => {
    await page.goto("/", { waitUntil: "load" });
    await page.locator("main").first().waitFor({ state: "visible", timeout: 60_000 });

    await page
      .getByRole("navigation", { name: operatorCorePilotNavLabel })
      .getByRole("link", { name: "Runs" })
      .click();

    await page.waitForURL("**/runs**", { timeout: 60_000 });

    // `waitForURL` can resolve before React's `useLayoutEffect` (route-change focus) runs; poll until the landmark is focused.
    await expect(page.locator("#main-content")).toBeFocused({ timeout: 10_000 });
  });

  test("route announcer updates after navigation", async ({ page }) => {
    await page.goto("/", { waitUntil: "load" });
    await page.locator("main").first().waitFor({ state: "visible", timeout: 60_000 });

    await page
      .getByRole("navigation", { name: operatorCorePilotNavLabel })
      .getByRole("link", { name: "Runs" })
      .click();
    await page.waitForURL("**/runs**", { timeout: 60_000 });

    await expect(page.getByTestId("route-announcer")).toContainText("Navigated to Runs", { timeout: 10_000 });
  });

  test("axe baseline passes in dark mode", async ({ page }) => {
    await page.goto("/", { waitUntil: "load" });
    await page.locator("main").first().waitFor({ state: "visible", timeout: 60_000 });

    await page.evaluate(() => {
      try {
        localStorage.setItem("archlucid_color_mode", "dark");
      } catch {
        /* ignore */
      }
    });

    await page.reload({ waitUntil: "load" });
    await page.locator("main").first().waitFor({ state: "visible", timeout: 60_000 });

    const results = await runAxe(page);
    const critical = results.violations.filter((v) => v.impact === "critical" || v.impact === "serious");

    expect(critical, JSON.stringify(critical, null, 2)).toHaveLength(0);
  });
});
