import { render, screen, waitFor } from "@testing-library/react";
import { axe, toHaveNoViolations } from "jest-axe";
import { describe, expect, it, vi } from "vitest";

import { SignupForm } from "@/components/marketing/SignupForm";
import { WelcomeMarketingPage } from "@/components/marketing/WelcomeMarketingPage";
import { TrialBanner } from "@/components/TrialBanner";

expect.extend(toHaveNoViolations);

vi.mock("next/navigation", () => ({
  useRouter: () => ({ push: vi.fn() }),
  redirect: vi.fn(),
}));

vi.mock("@/lib/toast", () => ({
  showError: vi.fn(),
  showSuccess: vi.fn(),
}));

vi.mock("@/lib/auth-config", () => ({
  AUTH_MODE: "development-bypass",
}));

vi.mock("@/lib/oidc/config", () => ({
  isJwtAuthMode: () => false,
}));

vi.mock("@/lib/oidc/session", () => ({
  isLikelySignedIn: () => false,
}));

describe("trial + marketing — axe (Vitest)", () => {
  it("WelcomeMarketingPage has no serious axe violations", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => {
        return new Response(
          JSON.stringify({
            schemaVersion: 1,
            currency: "USD",
            packages: [{ id: "p", title: "Pkg", summary: "S", workspaceMonthlyUsd: 1, seatMonthlyUsd: 1, annualFloorUsd: 1 }],
          }),
          { status: 200, headers: { "Content-Type": "application/json" } },
        );
      }),
    );

    const { container } = render(
      <div>
        <WelcomeMarketingPage />
      </div>,
    );

    expect(await axe(container)).toHaveNoViolations();
    vi.unstubAllGlobals();
  });

  it("SignupForm has no serious axe violations", async () => {
    const { container } = render(
      <div>
        <SignupForm />
      </div>,
    );

    expect(await axe(container)).toHaveNoViolations();
  });

  it("TrialBanner (Active) has no serious axe violations", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => {
        return new Response(JSON.stringify({ status: "Active", daysRemaining: 3 }), {
          status: 200,
          headers: { "Content-Type": "application/json" },
        });
      }),
    );

    const { container } = render(<TrialBanner />);

    await waitFor(() => {
      expect(screen.getByRole("region", { name: /Trial subscription/i })).toBeInTheDocument();
    });

    expect(await axe(container)).toHaveNoViolations();
    vi.unstubAllGlobals();
  });
});
