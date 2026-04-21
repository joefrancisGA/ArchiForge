import { expect, test, type Page, type Route } from "@playwright/test";

import { backendApiPath } from "./helpers/route-match";

/**
 * Mock-only Playwright spec for the trial signup funnel.
 *
 * Covers the deterministic happy path described in `docs/runbooks/TRIAL_FUNNEL_END_TO_END.md`:
 *
 *  1. Marketing `/signup` form (custom baseline path: tenant-supplied hours).
 *  2. `POST /api/proxy/v1/register` → 201 with stub tenant identifiers.
 *  3. Redirect to `/signup/verify?email=...` (verification UI rendered).
 *  4. Operator dashboard `/` shows `BeforeAfterDeltaPanel` with the captured baseline (16 h)
 *     and the measured `pilot-run-deltas` (4 h) → 75% improvement summary.
 *
 * Live SQL coverage stays in `live-api-trial-end-to-end.spec.ts` (excluded from this config).
 */

const TRIAL_TENANT_ID = "11111111-1111-1111-1111-111111111111";
const TRIAL_WORKSPACE_ID = "22222222-2222-2222-2222-222222222222";
const TRIAL_PROJECT_ID = "33333333-3333-3333-3333-333333333333";
const TRIAL_WELCOME_RUN_ID = "44444444-4444-4444-4444-444444444444";
const FIRST_COMMIT_UTC = "2026-04-15T12:00:00.000Z";

type RegisterRequestBody = {
  organizationName?: string;
  adminEmail?: string;
  adminDisplayName?: string;
  baselineReviewCycleHours?: number;
  baselineReviewCycleSource?: string;
};

async function fulfillJson(route: Route, status: number, body: unknown): Promise<void> {
  await route.fulfill({
    status,
    contentType: "application/json",
    body: JSON.stringify(body),
  });
}

type RegisterCapture = { body: RegisterRequestBody | null };

async function installFunnelMocks(page: Page, capture: RegisterCapture): Promise<void> {
  await page.route("**/*", async (route) => {
    const req = route.request();
    const url = new URL(req.url());

    if (backendApiPath(url) === null) {
      await route.continue();
      return;
    }

    const path = backendApiPath(url) ?? "";
    const method = req.method();

    if (method === "POST" && path === "/v1/register") {
      try {
        capture.body = JSON.parse(req.postData() ?? "{}") as RegisterRequestBody;
      } catch {
        capture.body = null;
      }

      await fulfillJson(route, 201, {
        tenantId: TRIAL_TENANT_ID,
        defaultWorkspaceId: TRIAL_WORKSPACE_ID,
        defaultProjectId: TRIAL_PROJECT_ID,
        wasAlreadyProvisioned: false,
      });
      return;
    }

    if (method === "GET" && path === "/v1/tenant/trial-status") {
      await fulfillJson(route, 200, {
        status: "Active",
        trialStartUtc: "2026-04-14T12:00:00.000Z",
        trialExpiresUtc: "2026-04-28T12:00:00.000Z",
        daysRemaining: 7,
        trialRunsUsed: 1,
        trialRunsLimit: 5,
        trialSeatsUsed: 1,
        trialSeatsLimit: 3,
        trialWelcomeRunId: TRIAL_WELCOME_RUN_ID,
        firstCommitUtc: FIRST_COMMIT_UTC,
        baselineReviewCycleHours: 16,
        baselineReviewCycleSource: "team estimate",
        baselineReviewCycleCapturedUtc: "2026-04-14T12:00:00.000Z",
      });
      return;
    }

    if (method === "GET" && path === `/v1/pilots/runs/${TRIAL_WELCOME_RUN_ID}/pilot-run-deltas`) {
      await fulfillJson(route, 200, {
        timeToCommittedManifestTotalSeconds: 4 * 3600,
        manifestCommittedUtc: FIRST_COMMIT_UTC,
        runCreatedUtc: "2026-04-15T08:00:00.000Z",
        findingsBySeverity: [],
        auditRowCount: 12,
        auditRowCountTruncated: false,
        llmCallCount: 0,
        topFindingSeverity: null,
        topFindingId: null,
        topFindingEvidenceChain: null,
        isDemoTenant: true,
      });
      return;
    }

    await fulfillJson(route, 200, {});
  });
}

test.describe("trial funnel — mocked end-to-end", () => {
  test("signup form forwards the optional baseline + dashboard renders the before-vs-measured delta", async ({
    page,
  }) => {
    const capture: RegisterCapture = { body: null };
    await installFunnelMocks(page, capture);

    await page.goto("/signup");

    await page.getByLabel(/Work email/i).fill("ops@example.com");
    await page.getByLabel(/Full name/i).fill("Ops User");
    await page.getByLabel(/Organization name/i).fill("Contoso Trial Org");

    await page.getByTestId("signup-baseline-choice-custom").click();

    await page.getByTestId("signup-baseline-hours").fill("16");
    await page.getByTestId("signup-baseline-source").fill("team estimate");

    await page.getByRole("button", { name: /Create trial workspace/i }).click();

    await expect(page).toHaveURL(/\/signup\/verify\?email=ops%40example\.com/);

    expect(capture.body).not.toBeNull();
    expect(capture.body?.adminEmail).toBe("ops@example.com");
    expect(capture.body?.organizationName).toBe("Contoso Trial Org");
    expect(capture.body?.baselineReviewCycleHours).toBe(16);
    expect(capture.body?.baselineReviewCycleSource).toBe("team estimate");

    await page.goto("/");

    await expect(page.getByTestId("before-after-delta-panel")).toBeVisible();
    await expect(page.getByTestId("before-after-delta-baseline-hours")).toHaveText("16.00 h");
    await expect(page.getByTestId("before-after-delta-measured-hours")).toHaveText("4.00 h");
    await expect(page.getByTestId("before-after-delta-summary")).toContainText("12.00 h saved per run");
    await expect(page.getByTestId("before-after-delta-summary")).toContainText("75.0% improvement");
  });
});
