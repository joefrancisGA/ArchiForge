/**
 * Team-tier Stripe checkout URL resolution for `pricing.json` / env overrides.
 *
 * Pricing tier cards additionally require `isPublicStripeTeamCheckoutEnabled` before surfacing Checkout as the Team primary CTA.
 */

/**
 * Returns true when `teamStripeCheckoutUrl` should surface the Subscribe-with-Stripe control (sales-led posture:
 * placeholders and empty URLs stay hidden).
 */
export function isUsableTeamStripeCheckoutUrl(raw: string | null | undefined): boolean {
  if (raw === null || raw === undefined) {
    return false;
  }

  const url = raw.trim();

  if (url.length === 0) {
    return false;
  }

  const lower = url.toLowerCase();

  if (lower.includes("placeholder-replace-before-launch")) {
    return false;
  }

  if (lower.includes("checkout-placeholder")) {
    return false;
  }

  return true;
}

/**
 * Resolves the effective Team Stripe checkout URL for marketing/pricing surfaces.
 *
 * - When `NEXT_PUBLIC_STRIPE_TEAM_CHECKOUT_ENABLED` is explicitly `"0"`/`"false"`, returns `null` (sales-led suppression).
 *   When **unset**, Stripe follows historic `pricing.json` gating only (`isUsableTeamStripeCheckoutUrl`).
 * - When `NEXT_PUBLIC_STRIPE_TEAM_CHECKOUT_URL` is set (and passes {@link isUsableTeamStripeCheckoutUrl}), it overrides `pricing.json`.
 * - Otherwise falls back to `pricing.teamStripeCheckoutUrl` when usable.
 */
export function resolveTeamStripeCheckoutHref(pricingTeamStripeCheckoutUrl: string | null | undefined): string | null {
  const enabledStrip = process.env.NEXT_PUBLIC_STRIPE_TEAM_CHECKOUT_ENABLED?.trim();
  const checkoutExplicitlyDisabled =
    enabledStrip === "0" || enabledStrip?.toLowerCase() === "false";

  if (checkoutExplicitlyDisabled)
    return null;

  const envRaw = process.env.NEXT_PUBLIC_STRIPE_TEAM_CHECKOUT_URL;
  const envCandidate = typeof envRaw === "string" ? envRaw.trim() : "";

  const fromEnv = envCandidate.length > 0 ? envCandidate : null;
  const fromDoc =
    pricingTeamStripeCheckoutUrl !== null && pricingTeamStripeCheckoutUrl !== undefined
      ? pricingTeamStripeCheckoutUrl.trim()
      : "";

  const candidate = fromEnv ?? (fromDoc.length > 0 ? fromDoc : null);

  if (candidate === null || !isUsableTeamStripeCheckoutUrl(candidate))
    return null;

  return candidate;
}
