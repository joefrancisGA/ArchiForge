import { expect, test } from "@playwright/test";

import { formatViolations, runAxe } from "./helpers/axe-helper";

const PAGES = [
  { name: "Home", path: "/" },
  { name: "Runs", path: "/runs?projectId=default" },
  { name: "Audit", path: "/audit" },
  { name: "Policy packs", path: "/policy-packs" },
  { name: "Alerts", path: "/alerts" },
  { name: "Governance dashboard", path: "/governance/dashboard" },
];

test.describe("accessibility baseline — WCAG 2.1 AA", () => {
  for (const { name, path } of PAGES) {
    test(`${name} (${path}) has no critical or serious axe violations`, async ({ page }) => {
      await page.goto(path, { waitUntil: "load" });
      await page.locator("main").first().waitFor({ state: "visible", timeout: 60_000 });

      const results = await runAxe(page);
      const critical = results.violations.filter((v) => v.impact === "critical" || v.impact === "serious");

      expect(critical, formatViolations(critical)).toHaveLength(0);
    });
  }
});
