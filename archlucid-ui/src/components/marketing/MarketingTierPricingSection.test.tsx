import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { MarketingTierPricingSection } from "./MarketingTierPricingSection";

describe("MarketingTierPricingSection", () => {
  beforeEach(() => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          schemaVersion: 1,
          currency: "USD",
          packages: [
            {
              id: "team",
              title: "Team",
              summary: "Team tier",
              workspaceMonthlyUsd: 199,
              seatMonthlyUsd: 79,
            },
            {
              id: "professional",
              title: "Professional",
              summary: "Pro tier",
              workspaceMonthlyUsd: 899,
              seatMonthlyUsd: 179,
            },
            {
              id: "enterprise",
              title: "Enterprise",
              summary: "Ent tier",
              annualFloorUsd: 60000,
            },
          ],
          teamStripeCheckoutUrl: "https://pay.example.test/checkout",
        }),
      }),
    );
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("renders Request quote as primary on Team, Stripe when configured, and talk-to-sales for pro/enterprise", async () => {
    const quote = document.createElement("div");
    quote.id = "pricing-quote-request";
    document.body.appendChild(quote);

    render(
      <MarketingTierPricingSection
        sectionHeadingId="pricing-heading"
        sectionTitle="Pricing"
        signupHref="/signup?utm=test"
        quoteSectionDomId="pricing-quote-request"
      />,
    );

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "Team" })).toBeInTheDocument();
    });

    const teamCard = screen.getByRole("heading", { name: "Team" }).closest("li");
    if (teamCard === null) {
      throw new Error("expected Team tier list item");
    }

    const teamScope = within(teamCard);
    teamScope.getByRole("button", { name: /request quote/i });
    teamScope.getByRole("link", { name: /start free trial/i });

    const stripeSubscribe = teamScope.getByTestId("pricing-team-subscribe-stripe");

    expect(stripeSubscribe).toHaveAttribute("href", "https://pay.example.test/checkout");
    expect(stripeSubscribe).toHaveTextContent(/subscribe with stripe/i);

    const talkButtons = screen.getAllByRole("button", { name: /talk to sales/i });
    expect(talkButtons).toHaveLength(2);

    const scroll = vi.spyOn(quote, "scrollIntoView");
    fireEvent.click(teamScope.getByRole("button", { name: /request quote/i }));
    expect(scroll).toHaveBeenCalled();

    quote.remove();
  });

  it("hides Subscribe with Stripe when the configured URL is a placeholder", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          schemaVersion: 1,
          currency: "USD",
          packages: [
            {
              id: "team",
              title: "Team",
              summary: "Team tier",
              workspaceMonthlyUsd: 199,
              seatMonthlyUsd: 79,
            },
            {
              id: "professional",
              title: "Professional",
              summary: "Pro tier",
              workspaceMonthlyUsd: 899,
              seatMonthlyUsd: 179,
            },
            {
              id: "enterprise",
              title: "Enterprise",
              summary: "Ent tier",
              annualFloorUsd: 60000,
            },
          ],
          teamStripeCheckoutUrl: "https://checkout.stripe.com/placeholder-replace-before-launch",
        }),
      }),
    );

    render(
      <MarketingTierPricingSection sectionHeadingId="pricing-heading" sectionTitle="Pricing" signupHref="/signup" />,
    );

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "Team" })).toBeInTheDocument();
    });

    expect(screen.queryByRole("link", { name: /subscribe with stripe/i })).not.toBeInTheDocument();
  });

  it("hides Subscribe with Stripe when NEXT_PUBLIC_STRIPE_TEAM_CHECKOUT_ENABLED is off even if pricing JSON has a URL", async () => {
    vi.stubEnv("NEXT_PUBLIC_STRIPE_TEAM_CHECKOUT_ENABLED", "0");

    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          schemaVersion: 1,
          currency: "USD",
          packages: [
            {
              id: "team",
              title: "Team",
              summary: "Team tier",
              workspaceMonthlyUsd: 199,
              seatMonthlyUsd: 79,
            },
            {
              id: "professional",
              title: "Professional",
              summary: "Pro tier",
              workspaceMonthlyUsd: 899,
              seatMonthlyUsd: 179,
            },
            {
              id: "enterprise",
              title: "Enterprise",
              summary: "Ent tier",
              annualFloorUsd: 60000,
            },
          ],
          teamStripeCheckoutUrl: "https://pay.example.test/checkout",
        }),
      }),
    );

    render(
      <MarketingTierPricingSection sectionHeadingId="pricing-heading" sectionTitle="Pricing" signupHref="/signup" />,
    );

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "Team" })).toBeInTheDocument();
    });

    expect(screen.queryByRole("link", { name: /subscribe with stripe/i })).not.toBeInTheDocument();
  });
});
