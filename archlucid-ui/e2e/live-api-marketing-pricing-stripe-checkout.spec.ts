/**
 * Marketing `/pricing`: Team-tier Stripe subscribe link behavior from `public/pricing.json`.
 *
 * With **`teamStripeCheckoutUrl`** still a placeholder, **Subscribe with Stripe** is hidden (`team-stripe-checkout-url.ts`).
 * After operators paste a real Payment Link / Checkout URL per **`docs/runbooks/STRIPE_OPERATOR_CHECKLIST.md`** § E,
 * this spec asserts the link opens a Stripe-hosted tab.
 *
 * Run (live UI bundle): `npx playwright test live-api-marketing-pricing-stripe-checkout.spec.ts`
 */
import { expect, test, type APIRequestContext } from "@playwright/test";

import { liveApiBase } from "./helpers/live-api-client";

async function pricingJsonTeamStripeCheckoutUrl(request: APIRequestContext): Promise<string> {
  const res = await request.get("http://127.0.0.1:3000/pricing.json", { timeout: 30_000 });

  expect(res.ok(), `pricing.json GET failed: ${res.status()}`).toBeTruthy();

  const json = (await res.json()) as { teamStripeCheckoutUrl?: string | null };

  return json.teamStripeCheckoutUrl ?? "";
}

function isPlaceholderStripeCheckoutUrl(raw: string): boolean {
  const lower = raw.trim().toLowerCase();

  return lower.length === 0 || lower.includes("placeholder-replace-before-launch") || lower.includes("checkout-placeholder");
}

test.describe("live-api-marketing-pricing-stripe-checkout", () => {
  test.beforeAll(async ({ request }) => {
    const health = await request.get(`${liveApiBase}/health/ready`, { timeout: 60_000 });

    if (!health.ok()) {
      throw new Error(
        `Live API not ready at ${liveApiBase}/health/ready (status ${health.status()}). Start ArchLucid.Api with Sql + DevelopmentBypass.`,
      );
    }
  });

  test("pricing Team tier: Stripe subscribe hidden until real checkout URL configured", async ({ page, request }) => {
    test.setTimeout(180_000);

    const rawUrl = await pricingJsonTeamStripeCheckoutUrl(request);

    await page.goto("/pricing", { waitUntil: "load" });
    await page.locator("main").first().waitFor({ state: "visible", timeout: 60_000 });

    await expect(page.getByTestId("pricing-tier-team")).toBeVisible({ timeout: 60_000 });

    const stripeLink = page.getByTestId("pricing-team-subscribe-stripe");

    if (isPlaceholderStripeCheckoutUrl(rawUrl)) {
      await expect(stripeLink).toHaveCount(0);

      return;
    }

    await expect(stripeLink).toBeVisible();

    const popupPromise = page.waitForEvent("popup");

    await stripeLink.click();

    const popup = await popupPromise;

    await popup.waitForLoadState("domcontentloaded");

    expect(popup.url()).toMatch(/^https:\/\/(checkout\.stripe\.com|buy\.stripe\.com)\//);
  });
});
