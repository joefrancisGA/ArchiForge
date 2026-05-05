import { expect, test } from "@playwright/test";

import { formatViolations, runAxe } from "./helpers/axe-helper";
import {
  FIXTURE_FINDING_ID,
  FIXTURE_LEFT_RUN_ID,
  FIXTURE_MANIFEST_EMPTY_ARTIFACTS_ID,
  FIXTURE_MANIFEST_ID,
  FIXTURE_RIGHT_RUN_ID,
  FIXTURE_RUN_ID,
  SCREENSHOT_FINDING_ID,
  SCREENSHOT_PLAN_ID,
  SCREENSHOT_POLICY_PACK_ID,
  SHOWCASE_DEMO_RUN_ID,
  SHOWCASE_STATIC_DEMO_MANIFEST_ID,
} from "./fixtures/ids";

/**
 * Live API + SQL axe sweep (merge-blocking via `ui-e2e-live` / default `playwright.config.ts`).
 *
 * {@link SHOWCASE_DEMO_RUN_ID} + {@link SCREENSHOT_FINDING_ID} match `e2e/smoke.spec.ts` core path — required for
 * finding routes against the real demo catalog.
 */
const PAGES = [
  { name: "Home", path: "/" },
  { name: "Welcome marketing", path: "/welcome" },
  { name: "Why ArchLucid marketing", path: "/why" },
  { name: "Compliance journey marketing", path: "/compliance-journey" },
  { name: "Pricing marketing", path: "/pricing" },
  { name: "Trial signup", path: "/signup" },
  { name: "Onboarding (canonical)", path: "/onboarding" },
  { name: "Legacy /getting-started → onboarding", path: "/getting-started" },
  { name: "Legacy /onboarding/start → onboarding", path: "/onboarding/start" },
  { name: "Legacy /onboarding → onboarding", path: "/onboarding" },
  { name: "Legacy /onboard → onboarding", path: "/onboard" },
  { name: "New request", path: "/runs/new" },
  { name: "Runs", path: "/runs?projectId=default" },
  { name: "Run detail", path: `/runs/${FIXTURE_RUN_ID}` },
  { name: "Run provenance", path: `/runs/${FIXTURE_RUN_ID}/provenance` },
  { name: "Finding detail (showcase run)", path: `/runs/${SHOWCASE_DEMO_RUN_ID}/findings/${SCREENSHOT_FINDING_ID}` },
  {
    name: "Finding inspect (showcase run)",
    path: `/runs/${SHOWCASE_DEMO_RUN_ID}/findings/${SCREENSHOT_FINDING_ID}/inspect`,
  },
  { name: "Manifest detail", path: `/manifests/${FIXTURE_MANIFEST_ID}` },
  { name: "Manifest detail (empty artifacts fixture)", path: `/manifests/${FIXTURE_MANIFEST_EMPTY_ARTIFACTS_ID}` },
  { name: "Compare", path: "/compare" },
  { name: "Replay", path: "/replay" },
  { name: "Ask", path: "/ask" },
  { name: "Search", path: "/search" },
  { name: "Advisory", path: "/advisory" },
  { name: "Graph", path: "/graph" },
  { name: "Audit", path: "/audit" },
  { name: "Policy packs (operator hub)", path: "/policy-packs" },
  { name: "Alerts inbox (hub)", path: "/alerts" },
  { name: "Alerts rules tab", path: "/alerts?tab=rules" },
  { name: "Alerts routing tab", path: "/alerts?tab=routing" },
  { name: "Alerts simulation and tuning tab", path: "/alerts?tab=simulation" },
  { name: "Alerts composite tab", path: "/alerts?tab=composite" },
  { name: "Workspace health", path: "/governance/dashboard" },
  { name: "Governance workflow", path: "/governance" },
  { name: "Governance resolution", path: "/governance-resolution" },
  { name: "Governance findings queue", path: "/governance/findings" },
  { name: "Governance policy packs", path: "/governance/policy-packs" },
  {
    name: "Governance policy pack detail (marketing slug)",
    path: `/governance/policy-packs/${encodeURIComponent(SCREENSHOT_POLICY_PACK_ID)}`,
  },
  { name: "Planning", path: "/planning" },
  { name: "Digests", path: "/digests" },
  { name: "Digest subscriptions", path: "/digest-subscriptions" },
  { name: "Tenant settings", path: "/settings/tenant" },
  { name: "Settings baseline", path: "/settings/baseline" },
  { name: "Settings exec digest", path: "/settings/exec-digest" },
  { name: "Settings tenant cost", path: "/settings/tenant-cost" },
  { name: "Product learning", path: "/product-learning" },
  { name: "Advisory scheduling", path: "/advisory-scheduling" },
  { name: "Recommendation learning", path: "/recommendation-learning" },
  { name: "Evolution review", path: "/evolution-review" },
  { name: "Scorecard", path: "/scorecard" },
  { name: "Value report", path: "/value-report" },
  { name: "Value report pilot", path: "/value-report/pilot" },
  { name: "ROI summary", path: "/value-report/roi" },
  { name: "Help", path: "/help" },
  { name: "Workspace security & trust", path: "/workspace/security-trust" },
  { name: "Why ArchLucid (operator)", path: "/why-archlucid" },
  { name: "Demo explain", path: "/demo/explain" },
  { name: "Microsoft Teams integration", path: "/integrations/teams" },
  { name: "Admin users", path: "/admin/users" },
  { name: "Admin support", path: "/admin/support" },
  { name: "Admin health", path: "/admin/health" },
  { name: "Executive reviews list", path: "/executive/reviews" },
  { name: "Executive run detail (showcase)", path: `/executive/reviews/${SHOWCASE_DEMO_RUN_ID}` },
  {
    name: "Executive finding detail (showcase)",
    path: `/executive/reviews/${SHOWCASE_DEMO_RUN_ID}/findings/${SCREENSHOT_FINDING_ID}`,
  },
  { name: "New review (canonical /reviews)", path: "/reviews/new" },
  { name: "Reviews list (canonical /reviews)", path: "/reviews?projectId=default" },
  { name: "Run detail (canonical /reviews)", path: `/reviews/${FIXTURE_RUN_ID}` },
  { name: "Run provenance (canonical /reviews)", path: `/reviews/${FIXTURE_RUN_ID}/provenance` },
  {
    name: "Finding detail showcase (canonical /reviews)",
    path: `/reviews/${SHOWCASE_DEMO_RUN_ID}/findings/${SCREENSHOT_FINDING_ID}`,
  },
  {
    name: "Finding inspect showcase (canonical /reviews)",
    path: `/reviews/${SHOWCASE_DEMO_RUN_ID}/findings/${SCREENSHOT_FINDING_ID}/inspect`,
  },
  { name: "Replay (pre-filled runId)", path: `/replay?runId=${encodeURIComponent(SHOWCASE_DEMO_RUN_ID)}` },
  {
    name: "Compare (fixture left/right)",
    path: `/compare?leftRunId=${encodeURIComponent(FIXTURE_LEFT_RUN_ID)}&rightRunId=${encodeURIComponent(FIXTURE_RIGHT_RUN_ID)}`,
  },
  { name: "Operator sign in", path: "/auth/signin" },
  { name: "Marketing accessibility statement", path: "/accessibility" },
  { name: "Marketing privacy", path: "/privacy" },
  { name: "Marketing get started", path: "/get-started" },
  {
    name: "Manifest detail (showcase static demo UUID)",
    path: `/manifests/${SHOWCASE_STATIC_DEMO_MANIFEST_ID}`,
  },
  {
    name: "Planning plan detail (demo slug)",
    path: `/planning/plans/${encodeURIComponent(SCREENSHOT_PLAN_ID)}`,
  },
] as const;

/**
 * Routes intentionally excluded from the axe matrix: require state or OAuth handshakes that are not stable in CI.
 */
export const PAGES_DEFERRED = [
  {
    name: "Governance approval request lineage",
    path: "/governance/approval-requests/{id}/lineage",
    reason:
      "Lineage view expects a persisted approval request (e.g. Contoso demo `apr-demo-001`). Catalogs without demo governance seed return empty/error surfaces — flakes the `main` visibility gate. Scope as a targeted journey test once seed is mandatory.",
  },
  {
    name: "OAuth sign-in callback",
    path: "/auth/callback",
    reason:
      "Entra/OIDC callback requires `code` / `state` query parameters from the identity provider; loading the bare route yields a non-representative error shell for accessibility scoring.",
  },
  {
    name: "Finding detail (fixture run + mock-only finding slug)",
    path: `/reviews/${FIXTURE_RUN_ID}/findings/${FIXTURE_FINDING_ID}`,
    reason:
      "`FIXTURE_FINDING_ID` aligns with mock/breadcrumb fixtures; live SQL demos use human slugs (e.g. showcase `phi-minimization-risk`). Scanning this pair can 404 or show empty chrome on catalogs without that row — showcase finding routes already cover the inspect UI.",
  },
] as const;

test.describe("accessibility baseline — WCAG 2.2 AA (axe wcag22aa + inherited 2.x tags)", () => {
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
