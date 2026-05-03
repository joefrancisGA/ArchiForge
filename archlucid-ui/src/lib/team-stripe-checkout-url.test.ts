import { afterEach, describe, expect, it, vi } from "vitest";

import { isUsableTeamStripeCheckoutUrl, resolveTeamStripeCheckoutHref } from "./team-stripe-checkout-url";

describe("isUsableTeamStripeCheckoutUrl", () => {
  it("returns false for nullish, whitespace, repo placeholder token, or generic checkout-placeholder", () => {
    expect(isUsableTeamStripeCheckoutUrl(null)).toBe(false);
    expect(isUsableTeamStripeCheckoutUrl(undefined)).toBe(false);
    expect(isUsableTeamStripeCheckoutUrl("   ")).toBe(false);
    expect(isUsableTeamStripeCheckoutUrl("https://checkout.stripe.com/placeholder-replace-before-launch")).toBe(false);
    expect(isUsableTeamStripeCheckoutUrl("https://pay.example/checkout-placeholder/session")).toBe(false);
  });

  it("returns true for trimmed real Stripe URLs (including test checkout)", () => {
    expect(isUsableTeamStripeCheckoutUrl("https://checkout.stripe.com/c/pay/cs_test_xyz")).toBe(true);
    expect(isUsableTeamStripeCheckoutUrl("https://pay.example.test/checkout")).toBe(true);
  });
});

describe("resolveTeamStripeCheckoutHref", () => {
  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it("returns null when checkout is explicitly disabled", () => {
    vi.stubEnv("NEXT_PUBLIC_STRIPE_TEAM_CHECKOUT_ENABLED", "0");
    expect(resolveTeamStripeCheckoutHref("https://checkout.stripe.com/c/pay/cs_test_xyz")).toBe(null);
  });

  it("resolves usable pricing JSON when suppression flag is unset", () => {
    expect(resolveTeamStripeCheckoutHref("https://checkout.stripe.com/c/pay/cs_test_xyz")).toBe(
      "https://checkout.stripe.com/c/pay/cs_test_xyz",
    );
  });

  it("returns null when the URL is unusable (placeholders) even if suppression flag is unset", () => {
    expect(resolveTeamStripeCheckoutHref("https://checkout.stripe.com/placeholder-replace-before-launch")).toBe(null);
  });

  it("prefers env URL over pricing JSON when NEXT_PUBLIC_STRIPE_TEAM_CHECKOUT_URL is set", () => {
    vi.stubEnv("NEXT_PUBLIC_STRIPE_TEAM_CHECKOUT_URL", "https://checkout.stripe.com/c/pay/cs_test_env");
    expect(resolveTeamStripeCheckoutHref("https://checkout.stripe.com/c/pay/cs_test_json")).toBe(
      "https://checkout.stripe.com/c/pay/cs_test_env",
    );
  });

  it("falls back to pricing JSON when env override is empty", () => {
    vi.stubEnv("NEXT_PUBLIC_STRIPE_TEAM_CHECKOUT_URL", "");
    expect(resolveTeamStripeCheckoutHref("https://checkout.stripe.com/c/pay/cs_test_json")).toBe(
      "https://checkout.stripe.com/c/pay/cs_test_json",
    );
  });
});
