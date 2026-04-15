import { expect, test } from "@playwright/test";

import { formatViolations, runAxe } from "./helpers/axe-helper";

/** Stable run id from `e2e/fixtures/ids.ts` (mock API serves run detail + aggregate explanation). */
const FIXTURE_RUN_ID = "e2e-fixture-run-001";

const PAGES = [
  { name: "Home", path: "/" },
  { name: "Getting started", path: "/getting-started" },
  { name: "Onboarding", path: "/onboarding" },
  { name: "New run wizard", path: "/runs/new" },
  { name: "Runs", path: "/runs?projectId=default" },
  { name: "Run detail", path: `/runs/${FIXTURE_RUN_ID}` },
  { name: "Compare", path: "/compare" },
  { name: "Replay", path: "/replay" },
  { name: "Ask", path: "/ask" },
  { name: "Search", path: "/search" },
  { name: "Advisory", path: "/advisory" },
  { name: "Graph", path: "/graph" },
  { name: "Audit", path: "/audit" },
  { name: "Policy packs", path: "/policy-packs" },
  { name: "Alerts", path: "/alerts" },
  { name: "Alert rules", path: "/alert-rules" },
  { name: "Alert routing", path: "/alert-routing" },
  { name: "Alert simulation", path: "/alert-simulation" },
  { name: "Alert tuning", path: "/alert-tuning" },
  { name: "Composite alert rules", path: "/composite-alert-rules" },
  { name: "Governance dashboard", path: "/governance/dashboard" },
  { name: "Governance workflow", path: "/governance" },
  { name: "Governance resolution", path: "/governance-resolution" },
  { name: "Planning", path: "/planning" },
  { name: "Digests", path: "/digests" },
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
