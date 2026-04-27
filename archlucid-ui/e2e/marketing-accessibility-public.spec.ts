/**
 * Public marketing `/accessibility` — static route (no live API calls). Included in both Playwright configs:
 * default (live stack) and `playwright.mock.config.ts` (mock API) so CI and local `npm run test:e2e` exercise the page.
 */
import fs from "node:fs";
import path from "node:path";

import { expect, test } from "@playwright/test";

function escapeRegex(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

test.describe("marketing-accessibility-public", () => {
  test("/accessibility shows Last reviewed from ACCESSIBILITY.md and stays within annual cadence", async ({ page }) => {
    const mdPath = path.join(process.cwd(), "..", "ACCESSIBILITY.md");
    const md = fs.readFileSync(mdPath, "utf8");
    const m = md.match(/^Last reviewed:\s*(.+)$/m);
    expect(m, "ACCESSIBILITY.md must contain a Last reviewed line").not.toBeNull();
    if (m === null) {
      return;
    }

    const reviewedRaw = m[1]?.trim();
    expect(reviewedRaw, "Last reviewed capture group").toBeTruthy();
    if (reviewedRaw === undefined || reviewedRaw.length === 0) {
      return;
    }

    const reviewedMs = Date.parse(reviewedRaw);
    expect(Number.isNaN(reviewedMs), "Last reviewed must parse as a date").toBe(false);

    const st = fs.statSync(mdPath);
    expect(reviewedMs).toBeLessThanOrEqual(st.mtimeMs + 48 * 3600 * 1000);
    expect(Date.now() - reviewedMs).toBeLessThan(370 * 86400 * 1000);

    await page.goto("/accessibility", { waitUntil: "load" });
    await page.locator("main#main-content").waitFor({ state: "visible", timeout: 60_000 });

    await expect(page.getByRole("heading", { name: "Accessibility", level: 1 })).toBeVisible();
    await expect(page.getByRole("heading", { name: "Target compliance level", level: 2 })).toBeVisible();
    await expect(page.getByText(new RegExp(`Last reviewed:\\s*${escapeRegex(reviewedRaw)}`))).toBeVisible();
    await expect(page.getByRole("link", { name: "accessibility@archlucid.net" })).toBeVisible();
  });
});
