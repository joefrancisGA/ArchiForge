import { expect, test } from "@playwright/test";

import {
  fixtureArtifactDescriptorsScreenshot,
  fixtureComparisonExplanation,
  fixtureGoldenManifestComparisonScreenshot,
  fixtureLegacyRunComparisonScreenshot,
  fixtureManifestSummaryScreenshot,
  fixtureRunDetailScreenshot,
  SCREENSHOT_APPROVAL_ID,
  SCREENSHOT_FINDING_ID,
  SCREENSHOT_LEFT_RUN_ID,
  SCREENSHOT_MANIFEST_ID,
  SCREENSHOT_PLAN_ID,
  SCREENSHOT_POLICY_PACK_ID,
  SCREENSHOT_RIGHT_RUN_ID,
  SCREENSHOT_RUN_ID,
  SHOWCASE_DEMO_RUN_ID,
} from "./fixtures";
import {
  FIXTURE_EMPTY_ZIP_BYTES,
  registerOperatorJourneyApiRoutes,
  registerScreenshotSuiteProxyRoutes,
} from "./helpers/register-operator-api-routes";
import { publicDirUnderUi } from "./screenshot-output-helpers";

const OUT = publicDirUnderUi("screenshots", "all-routes");

/** Per-test cap for visiting every route: default Playwright would time out; 30m for slow cold builds or CI. */
const ALL_ROUTES_SCREENSHOT_TEST_TIMEOUT_MS = 30 * 60 * 1_000;

/** One href per `page.tsx` (63 routes); run/manifest/compare paths use {@link SCREENSHOT_*} for human-readable URLs. Legacy `/onboarding*` aliases omitted — they redirect to `/getting-started`. */
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
  `/compare?${new URLSearchParams({ leftRunId: SCREENSHOT_LEFT_RUN_ID, rightRunId: SCREENSHOT_RIGHT_RUN_ID }).toString()}`,
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
  `/governance/approval-requests/${encodeURIComponent(SCREENSHOT_APPROVAL_ID)}/lineage`,
  `/governance/policy-packs/${encodeURIComponent(SCREENSHOT_POLICY_PACK_ID)}`,
  "/governance-resolution",
  "/graph",
  "/help",
  "/integrations/teams",
  "/get-started",
  "/live-demo",
  `/manifests/${encodeURIComponent(SCREENSHOT_MANIFEST_ID)}`,
  "/planning",
  `/planning/plans/${encodeURIComponent(SCREENSHOT_PLAN_ID)}`,
  "/policy-packs",
  "/pricing",
  "/privacy",
  "/product-learning",
  "/recommendation-learning",
  "/replay",
  "/runs?projectId=default",
  "/runs/new",
  `/runs/${encodeURIComponent(SCREENSHOT_RUN_ID)}`,
  `/runs/${encodeURIComponent(SCREENSHOT_RUN_ID)}/findings/${encodeURIComponent(SCREENSHOT_FINDING_ID)}`,
  `/runs/${encodeURIComponent(SCREENSHOT_RUN_ID)}/findings/${encodeURIComponent(SCREENSHOT_FINDING_ID)}/inspect`,
  `/runs/${encodeURIComponent(SCREENSHOT_RUN_ID)}/provenance`,
  "/search",
  "/security-trust",
  "/see-it",
  `/showcase/${encodeURIComponent(SHOWCASE_DEMO_RUN_ID)}`,
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
      runDetail: { runId: SCREENSHOT_RUN_ID, body: fixtureRunDetailScreenshot() },
      manifestSummary: { manifestId: SCREENSHOT_MANIFEST_ID, body: fixtureManifestSummaryScreenshot() },
      artifactList: { manifestId: SCREENSHOT_MANIFEST_ID, body: fixtureArtifactDescriptorsScreenshot() },
      artifactBundle: { manifestId: SCREENSHOT_MANIFEST_ID, body: FIXTURE_EMPTY_ZIP_BYTES, headOk: true },
      legacyCompare: {
        leftRunId: SCREENSHOT_LEFT_RUN_ID,
        rightRunId: SCREENSHOT_RIGHT_RUN_ID,
        body: fixtureLegacyRunComparisonScreenshot(),
      },
      structuredCompare: {
        baseRunId: SCREENSHOT_LEFT_RUN_ID,
        targetRunId: SCREENSHOT_RIGHT_RUN_ID,
        body: fixtureGoldenManifestComparisonScreenshot(),
      },
      compareExplanation: {
        baseRunId: SCREENSHOT_LEFT_RUN_ID,
        targetRunId: SCREENSHOT_RIGHT_RUN_ID,
        body: fixtureComparisonExplanation(),
      },
    });
    await registerScreenshotSuiteProxyRoutes(page);
  });

  test("writes PNGs for every app route (page.tsx)", async ({ page }) => {
    test.setTimeout(ALL_ROUTES_SCREENSHOT_TEST_TIMEOUT_MS);

    for (const href of HREFS) {
      // `networkidle` rarely settles on Next.js (open connections); health route proxy GETs must still resolve — see registerScreenshotSuiteProxyRoutes.
      await page.goto(href, { waitUntil: "load", timeout: 120_000 });

      if (href === "/advisory-scheduling")
        await page.waitForURL(/\/advisory\?tab=schedules(?:&[^#]*)?(?:$|#)/, { timeout: 30_000 });
      else if (href === "/settings/exec-digest")
        await page.waitForURL(/\/digests\?tab=schedule(?:&[^#]*)?(?:$|#)/, { timeout: 30_000 });

      /** Wait for hydrated shell ({@link AppShellClient} / {@link ShellReadySurface}); `networkidle` is unreliable on Next.js. */
      try {
        await page.locator("[data-app-ready=\"true\"]").waitFor({ state: "attached", timeout: 60_000 });
      } catch (e) {
        throw new Error(
          `data-app-ready not found for href=${href} (url=${page.url()}). Use a free UI port, run mock webServer (see playwright.mock.config), ` +
            `and avoid MOCK_E2E_REUSE_SERVER unless the correct standalone app is already listening. ${(e as Error).message}`,
        );
      }

      await expect(page.locator("body")).toBeVisible({ timeout: 120_000 });
      await page.screenshot({ path: filePathForHref(href), fullPage: true });
    }
  });
});
