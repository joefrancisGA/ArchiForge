import { expect, test } from "@playwright/test";

import {
  FIXTURE_LEFT_RUN_ID,
  FIXTURE_MANIFEST_ID,
  FIXTURE_RIGHT_RUN_ID,
  FIXTURE_RUN_ID,
  fixtureArtifactDescriptorsNonEmpty,
  fixtureComparisonExplanation,
  fixtureGoldenManifestComparison,
  fixtureLegacyRunComparison,
  fixtureManifestSummary,
  fixtureRunDetail,
} from "./fixtures";
import { comparePairSearchParams } from "./helpers/operator-journey";
import { FIXTURE_EMPTY_ZIP_BYTES, registerOperatorJourneyApiRoutes } from "./helpers/register-operator-api-routes";

const OUT = "public/screenshots/all-routes";

/** Per-test cap for visiting every route: default Playwright would time out; 30m for slow cold builds or CI. */
const ALL_ROUTES_SCREENSHOT_TEST_TIMEOUT_MS = 30 * 60 * 1_000;

const PLAN_ID = "e2e-plan-001";
const FINDING_ID = "e2e-finding-001";
const APPROVAL_ID = "e2e-approval-001";
const POLICY_PACK_ID = "e2e-policy-pack-001";

/** One href per `page.tsx` (63 routes); dynamics use stable E2E fixture ids. Legacy `/onboarding*` aliases omitted — they redirect to `/getting-started`. */
const HREFS: string[] = [
  "/",
  "/accessibility",
  "/advisory",
  "/advisory-scheduling",
  "/admin/health",
  "/admin/support",
  "/admin/users",
  "/alerts",
  "/ask",
  "/audit",
  "/auth/callback",
  "/auth/signin",
  `/compare?${comparePairSearchParams()}`,
  "/compliance-journey",
  "/demo/explain",
  "/demo/preview",
  "/digest-subscriptions",
  "/digests",
  "/evolution-review",
  "/example-roi-bulletin",
  "/getting-started",
  "/governance",
  "/governance/dashboard",
  "/governance/findings",
  `/governance/approval-requests/${encodeURIComponent(APPROVAL_ID)}/lineage`,
  `/governance/policy-packs/${encodeURIComponent(POLICY_PACK_ID)}`,
  "/governance-resolution",
  "/graph",
  "/help",
  "/integrations/teams",
  "/get-started",
  "/live-demo",
  `/manifests/${encodeURIComponent(FIXTURE_MANIFEST_ID)}`,
  "/planning",
  `/planning/plans/${encodeURIComponent(PLAN_ID)}`,
  "/policy-packs",
  "/pricing",
  "/privacy",
  "/product-learning",
  "/recommendation-learning",
  "/replay",
  "/runs?projectId=default",
  "/runs/new",
  `/runs/${encodeURIComponent(FIXTURE_RUN_ID)}`,
  `/runs/${encodeURIComponent(FIXTURE_RUN_ID)}/findings/${encodeURIComponent(FINDING_ID)}`,
  `/runs/${encodeURIComponent(FIXTURE_RUN_ID)}/findings/${encodeURIComponent(FINDING_ID)}/inspect`,
  `/runs/${encodeURIComponent(FIXTURE_RUN_ID)}/provenance`,
  "/search",
  "/security-trust",
  "/see-it",
  `/showcase/${encodeURIComponent(FIXTURE_RUN_ID)}`,
  "/settings/baseline",
  "/settings/exec-digest",
  "/settings/tenant",
  "/settings/tenant-cost",
  "/signup",
  "/signup/verify",
  "/trust",
  "/value-report",
  "/welcome",
  "/why",
  "/why-archlucid",
  "/workspace/security-trust",
];

function filePathForHref(href: string): string {
  const noLead = href.replace(/^\//, "");
  const slug = (noLead.length > 0 ? noLead : "index").replace(/[/?&=]+/g, "-").replace(/-+/g, "-");
  return `${OUT}/${slug}.png`;
}

test.describe("all routes screenshots (mock API)", () => {
  test.beforeEach(async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 });
    await registerOperatorJourneyApiRoutes(page, {
      runDetail: { runId: FIXTURE_RUN_ID, body: fixtureRunDetail() },
      manifestSummary: { manifestId: FIXTURE_MANIFEST_ID, body: fixtureManifestSummary() },
      artifactList: { manifestId: FIXTURE_MANIFEST_ID, body: fixtureArtifactDescriptorsNonEmpty() },
      artifactBundle: { manifestId: FIXTURE_MANIFEST_ID, body: FIXTURE_EMPTY_ZIP_BYTES, headOk: true },
      legacyCompare: {
        leftRunId: FIXTURE_LEFT_RUN_ID,
        rightRunId: FIXTURE_RIGHT_RUN_ID,
        body: fixtureLegacyRunComparison(),
      },
      structuredCompare: {
        baseRunId: FIXTURE_LEFT_RUN_ID,
        targetRunId: FIXTURE_RIGHT_RUN_ID,
        body: fixtureGoldenManifestComparison(),
      },
      compareExplanation: {
        baseRunId: FIXTURE_LEFT_RUN_ID,
        targetRunId: FIXTURE_RIGHT_RUN_ID,
        body: fixtureComparisonExplanation(),
      },
    });
  });

  test("writes PNGs for every app route (page.tsx)", async ({ page }) => {
    test.setTimeout(ALL_ROUTES_SCREENSHOT_TEST_TIMEOUT_MS);

    for (const href of HREFS) {
      await page.goto(href, { waitUntil: "domcontentloaded", timeout: 120_000 });
      await expect(page.locator("body")).toBeVisible({ timeout: 120_000 });
      await page.screenshot({ path: filePathForHref(href), fullPage: true });
    }
  });
});
