/**
 * Opt-in gate for Team Stripe Checkout on public marketing surfaces (e.g. `/pricing`).
 *
 * Only explicit `"1"` / `"true"` (case-insensitive, trimmed) enables the flow; absent or other values keep the quote-first posture.
 */
export function isPublicStripeTeamCheckoutEnabled(): boolean {
  const raw = process.env.NEXT_PUBLIC_STRIPE_TEAM_CHECKOUT_ENABLED;

  if (typeof raw !== "string") return false;

  const trimmed = raw.trim();

  if (trimmed.length === 0) return false;

  const lower = trimmed.toLowerCase();

  return lower === "1" || lower === "true";
}
