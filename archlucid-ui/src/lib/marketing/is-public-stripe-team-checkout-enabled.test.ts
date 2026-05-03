import { afterEach, describe, expect, it, vi } from "vitest";

import { isPublicStripeTeamCheckoutEnabled } from "./is-public-stripe-team-checkout-enabled";

describe("isPublicStripeTeamCheckoutEnabled", () => {
  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it("returns false when unset, empty, or not an affirmative token", () => {
    expect(isPublicStripeTeamCheckoutEnabled()).toBe(false);

    vi.stubEnv("NEXT_PUBLIC_STRIPE_TEAM_CHECKOUT_ENABLED", "");
    expect(isPublicStripeTeamCheckoutEnabled()).toBe(false);

    vi.stubEnv("NEXT_PUBLIC_STRIPE_TEAM_CHECKOUT_ENABLED", "  ");
    expect(isPublicStripeTeamCheckoutEnabled()).toBe(false);

    vi.stubEnv("NEXT_PUBLIC_STRIPE_TEAM_CHECKOUT_ENABLED", "no");
    expect(isPublicStripeTeamCheckoutEnabled()).toBe(false);
  });

  it("returns true for trimmed true or 1 (case-insensitive)", () => {
    vi.stubEnv("NEXT_PUBLIC_STRIPE_TEAM_CHECKOUT_ENABLED", "true");
    expect(isPublicStripeTeamCheckoutEnabled()).toBe(true);

    vi.stubEnv("NEXT_PUBLIC_STRIPE_TEAM_CHECKOUT_ENABLED", "TRUE");
    expect(isPublicStripeTeamCheckoutEnabled()).toBe(true);

    vi.stubEnv("NEXT_PUBLIC_STRIPE_TEAM_CHECKOUT_ENABLED", " 1 ");
    expect(isPublicStripeTeamCheckoutEnabled()).toBe(true);
  });
});
