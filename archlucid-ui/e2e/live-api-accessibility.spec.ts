import { expect, test } from "@playwright/test";

import { formatViolations, runAxe } from "./helpers/axe-helper";
import { FIXTURE_MANIFEST_ID, FIXTURE_RUN_ID } from "./fixtures/ids";

/**
 * Live API + SQL axe sweep (merge-blocking via `ui-e2e-live` / default `playwright.config.ts`).
 */
const PAGES = [
  { name: "Home", path: "/" },
  { name: "Welcome marketing", path: "/welcome" },
  { name: "Why ArchLucid marketing", path: "/why" },
  { name: "Compliance journey marketing", path: "/compliance-journey" },
  { name: "Pricing marketing", path: "/pricing" },
  { name: "Trial signup", path: "/signup" },
  { name: "Trial onboarding start", path: "/onboarding/start" },
  { name: "Getting started", path: "/getting-started" },
  { name: "Onboarding", path: "/onboarding" },
  { name: "Core Pilot onboard", path: "/onboard" },
  { name: "New run wizard", path: "/runs/new" },
  { name: "Runs", path: "/runs?projectId=default" },
  { name: "Run detail", path: `/runs/${FIXTURE_RUN_ID}` },
  { name: "Run provenance", path: `/runs/${FIXTURE_RUN_ID}/provenance` },
  { name: "Manifest detail", path: `/manifests/${FIXTURE_MANIFEST_ID}` },
  { name: "Compare", path: "/compare" },
  { name: "Replay", path: "/replay" },
  { name: "Ask", path: "/ask" },
  { name: "Search", path: "/search" },
  { name: "Advisory", path: "/advisory" },
  { name: "Graph", path: "/graph" },
  { name: "Audit", path: "/audit" },
  { name: "Policy packs", path: "/policy-packs" },
  { name: "Alerts inbox (hub)", path: "/alerts" },
  { name: "Alerts rules tab", path: "/alerts?tab=rules" },
  { name: "Alerts routing tab", path: "/alerts?tab=routing" },
  { name: "Alerts simulation and tuning tab", path: "/alerts?tab=simulation" },
  { name: "Alerts composite tab", path: "/alerts?tab=composite" },
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
