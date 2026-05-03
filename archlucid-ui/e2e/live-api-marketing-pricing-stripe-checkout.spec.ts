/**
 * Marketing `/pricing`: Team-tier Stripe subscribe link behavior from `public/pricing.json` plus build-time opt-in.
 *
 * **`NEXT_PUBLIC_STRIPE_TEAM_CHECKOUT_ENABLED=true`** (or **`1`**) must be set when building the UI for the Checkout CTA to render.
 * With a placeholder **`teamStripeCheckoutUrl`**, **Subscribe with Stripe** stays hidden (`team-stripe-checkout-url.ts`).
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

function isPublicStripeTeamCheckoutEnabledFromEnv(): boolean {
  const token = process.env.NEXT_PUBLIC_STRIPE_TEAM_CHECKOUT_ENABLED?.trim().toLowerCase();

  return token === "true" || token === "1";
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

    if (isPlaceholderStripeCheckoutUrl(rawUrl) || !isPublicStripeTeamCheckoutEnabledFromEnv()) {
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
