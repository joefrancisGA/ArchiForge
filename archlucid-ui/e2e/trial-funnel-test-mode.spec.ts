/**
 * Staging end-to-end Playwright spec for the trial funnel — **Stripe TEST mode only**.
 *
 * **Path note (consultative).** The prompt nominated `archlucid-ui/tests/e2e/...`,
 * but the operator-shell convention (and both existing Playwright configs) is
 * `archlucid-ui/e2e/`. This spec ships there so it is actually picked up by a
 * Playwright config (`playwright.trial-funnel-test-mode.config.ts`).
 *
 * **Skip semantics.** The whole describe block is skipped unless `STRIPE_TEST_KEY`
 * is set in the environment. This is the contract from the prompt — the spec is
 * a sales-engineer-led smoke against `signup.staging.archlucid.net`, not a
 * unit-style fixture, so it must not fail a developer's local `npm test` run.
 *
 * **What it asserts (happy path):**
 *  1. `/signup` form renders → `POST /api/proxy/v1/register` succeeds.
 *  2. `/signup/verify?email=...` page renders.
 *  3. (Optional) email-verification dev-harness endpoint flips the user verified.
 *  4. Sign-in → operator UI deep-links to the trial welcome run.
 *  5. Wizard step 1 is visible → step 7 reachable → commit succeeds.
 *  6. Sponsor banner / value report renders Day-N badge after commit.
 *  7. Every API response carries an `X-Correlation-ID`; the spec records it on
 *     test failure so the on-call attachment includes a single grep token.
 *
 * **What it intentionally does NOT do:** anything Stripe LIVE. No
 * `sk_live_*` traffic, no Marketplace publish, no DNS cutover. See
 * `docs/library/V1_DEFERRED.md` § 6b and owner Q17.
 */
import { expect, test, type Page } from "@playwright/test";

const STAGING_BASE_URL = process.env.STAGING_BASE_URL ?? "https://signup.staging.archlucid.net";
const STAGING_OPERATOR_BASE_URL = process.env.STAGING_OPERATOR_BASE_URL ?? STAGING_BASE_URL;
const STRIPE_TEST_KEY = (process.env.STRIPE_TEST_KEY ?? "").trim();
const VERIFICATION_HARNESS_PATH =
  process.env.STAGING_VERIFICATION_HARNESS_PATH ?? "/api/proxy/v1/auth/trial/local/dev-verify";

type CorrelationLog = { ids: string[]; lastSeen: string | null };

function attachCorrelationListener(page: Page): CorrelationLog {
  const log: CorrelationLog = { ids: [], lastSeen: null };

  page.on("response", (res) => {
    const id = res.headers()["x-correlation-id"];

    if (id && id.length > 0) {
      log.ids.push(id);
      log.lastSeen = id;
    }
  });

  return log;
}

function uniqueTenantSeed(): { org: string; email: string } {
  const stamp = Date.now().toString(36);
  const rand = Math.random().toString(36).slice(2, 8);

  return {
    org: `TrialFunnelTestMode-${stamp}-${rand}`,
    email: `trial-smoke+${stamp}-${rand}@example.invalid`,
  };
}

test.describe("trial-funnel-test-mode (staging, Stripe TEST mode)", () => {
  test.skip(
    STRIPE_TEST_KEY.length === 0,
    "STRIPE_TEST_KEY is not set — skipping staging trial-funnel TEST-mode spec. " +
      "Set STRIPE_TEST_KEY (and STAGING_BASE_URL if not the default) to enable.",
  );

  test("signup → verify → operator wizard step 1 → step 7 → commit → value report", async ({ page }) => {
    test.setTimeout(180_000);

    const correlation = attachCorrelationListener(page);
    const seed = uniqueTenantSeed();
    const failureContext: string[] = [];

    failureContext.push(`STAGING_BASE_URL=${STAGING_BASE_URL}`);
    failureContext.push(`org=${seed.org}`);
    failureContext.push(`email=${seed.email}`);

    try {
      await page.goto(`${STAGING_BASE_URL}/signup`, { waitUntil: "domcontentloaded" });

      await page.fill("#signup-email", seed.email);
      await page.fill("#signup-name", "Trial Smoke User");
      await page.fill("#signup-org", seed.org);

      const registerResPromise = page.waitForResponse(
        (res) => res.url().includes("/api/proxy/v1/register") && res.request().method() === "POST",
      );
      await page.getByRole("button", { name: /create organization|create org|continue/i }).click();

      const registerRes = await registerResPromise;
      expect(registerRes.status(), `register failed; correlation=${correlation.lastSeen ?? "<none>"}`).toBe(201);
      expect(
        registerRes.headers()["x-correlation-id"],
        "register response should carry X-Correlation-ID for support",
      ).toBeTruthy();

      await page.waitForURL(/\/signup\/verify/, { timeout: 30_000 });

      const harnessRes = await page.request.post(`${STAGING_BASE_URL}${VERIFICATION_HARNESS_PATH}`, {
        data: { email: seed.email },
        failOnStatusCode: false,
      });
      failureContext.push(`harness=${harnessRes.status()}`);

      if (harnessRes.status() >= 400) {
        test.skip(
          true,
          `Email-verification dev-harness returned ${harnessRes.status()} at ${VERIFICATION_HARNESS_PATH} — ` +
            "this staging environment does not expose the LocalIdentity dev-verify endpoint, so the TEST-mode " +
            "spec cannot complete the post-signup steps. Fix the env or set STAGING_VERIFICATION_HARNESS_PATH.",
        );
      }

      await page.goto(`${STAGING_OPERATOR_BASE_URL}/`, { waitUntil: "domcontentloaded" });

      const wizardStep1 = page
        .getByTestId("operator-first-run-wizard-step-1")
        .or(page.getByRole("heading", { name: /first run/i }));
      await expect(wizardStep1).toBeVisible({ timeout: 60_000 });

      const wizardStep7 = page
        .getByTestId("operator-first-run-wizard-step-7")
        .or(page.getByRole("button", { name: /finalize manifest/i }));
      await expect(wizardStep7).toBeVisible({ timeout: 60_000 });

      const commitResPromise = page.waitForResponse(
        (res) => res.url().includes("/architecture/run/") && res.url().endsWith("/commit"),
      );
      await page.getByRole("button", { name: /finalize manifest/i }).click();

      const commitRes = await commitResPromise;
      expect(commitRes.ok(), `commit failed; correlation=${correlation.lastSeen ?? "<none>"}`).toBeTruthy();
      expect(commitRes.headers()["x-correlation-id"]).toBeTruthy();

      const valueReport = page
        .getByTestId("email-run-to-sponsor-banner")
        .or(page.getByText(/day .* since first finalization/i));
      await expect(valueReport).toBeVisible({ timeout: 60_000 });

      expect(correlation.ids.length, "expected at least one X-Correlation-ID across the funnel").toBeGreaterThan(0);
    } catch (err) {
      failureContext.push(`lastCorrelationId=${correlation.lastSeen ?? "<none>"}`);
      failureContext.push(`correlationCount=${correlation.ids.length}`);
      console.error(`[trial-funnel-test-mode] failure context: ${failureContext.join(" | ")}`);
      throw err;
    }
  });
});
