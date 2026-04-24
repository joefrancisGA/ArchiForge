import { render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { BRAND_CATEGORY } from "@/lib/brand-category";

import { WelcomeMarketingPage } from "./WelcomeMarketingPage";

describe("WelcomeMarketingPage", () => {
  beforeEach(() => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => {
        return new Response(
          JSON.stringify({
            schemaVersion: 1,
            currency: "USD",
            packages: [
              {
                id: "pilot",
                title: "Pilot",
                summary: "Pilot summary",
                workspaceMonthlyUsd: 100,
                seatMonthlyUsd: 10,
                annualFloorUsd: 1200,
              },
            ],
          }),
          { status: 200, headers: { "Content-Type": "application/json" } },
        );
      }),
    );
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it("renders hero, pillars, and pricing cards from fetched JSON", async () => {
    render(<WelcomeMarketingPage />);

    expect(screen.getByRole("heading", { level: 1, name: /Ship governed architecture decisions faster/i })).toBeInTheDocument();
    const escapedCategory = BRAND_CATEGORY.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
    expect(
      screen.getByText(new RegExp(`ArchLucid is an ${escapedCategory} platform\\. You describe a system`, "i")),
    ).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: /Three pillars/i })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: /AI-native architecture analysis/i })).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "Pilot" })).toBeInTheDocument();
    });
  });
});
