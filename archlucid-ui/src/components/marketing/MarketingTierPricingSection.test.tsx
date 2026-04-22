import { fireEvent, render, screen, waitFor } from "@testing-library/react";
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

  it("renders team trial CTA, Stripe when configured, and talk-to-sales for pro/enterprise", async () => {
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

    const trialLinks = screen.getAllByRole("link", { name: /start free trial/i });
    expect(trialLinks.some((a) => a.getAttribute("href") === "/signup?utm=test")).toBe(true);

    expect(screen.getByRole("link", { name: /subscribe with stripe/i })).toHaveAttribute(
      "href",
      "https://pay.example.test/checkout",
    );

    const talkButtons = screen.getAllByRole("button", { name: /talk to sales/i });
    expect(talkButtons).toHaveLength(2);

    const scroll = vi.spyOn(quote, "scrollIntoView");
    fireEvent.click(talkButtons[0]!);
    expect(scroll).toHaveBeenCalled();

    quote.remove();
  });
});
