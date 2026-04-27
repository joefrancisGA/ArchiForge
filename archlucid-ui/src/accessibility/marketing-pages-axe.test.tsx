import { render } from "@testing-library/react";
import { axe, toHaveNoViolations } from "jest-axe";
import { describe, expect, it, vi } from "vitest";

vi.mock("next/navigation", () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), back: vi.fn() }),
  usePathname: () => "/",
  useSearchParams: () => ({
    get: () => null,
    toString: () => "",
  }),
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

import GetStartedPage from "@/app/(marketing)/get-started/page";
import PricingPage from "@/app/(marketing)/pricing/page";
import SeeItPage from "@/app/(marketing)/see-it/page";
import ComplianceJourneyPage from "@/app/(marketing)/compliance-journey/page";
import ExampleRoiBulletinPage from "@/app/(marketing)/example-roi-bulletin/page";
import PrivacyPage from "@/app/(marketing)/privacy/page";

expect.extend(toHaveNoViolations);

describe("marketing pages — axe (Vitest)", () => {
  it(
    "GetStartedPage has no serious axe violations",
    async () => {
      const { container } = render(<GetStartedPage />);

      expect(await axe(container)).toHaveNoViolations();
    },
    20_000,
  );

  it(
    "PricingPage has no serious axe violations",
    async () => {
      vi.stubGlobal(
        "fetch",
        vi.fn(async () => {
          return new Response(
            JSON.stringify({
              schemaVersion: 1,
              currency: "USD",
              packages: [
                { id: "pilot", title: "Pilot", summary: "Try it", workspaceMonthlyUsd: 0, seatMonthlyUsd: 0, annualFloorUsd: 0 },
                { id: "operate", title: "Operate", summary: "Run it", workspaceMonthlyUsd: 500, seatMonthlyUsd: 50, annualFloorUsd: 6000 },
              ],
            }),
            { status: 200, headers: { "Content-Type": "application/json" } },
          );
        }),
      );

      const { container } = render(<PricingPage />);

      expect(await axe(container)).toHaveNoViolations();
      vi.unstubAllGlobals();
    },
    20_000,
  );

  it(
    "SeeItPage has no serious axe violations",
    async () => {
      const { container } = render(<SeeItPage />);

      expect(await axe(container)).toHaveNoViolations();
    },
    20_000,
  );

  it(
    "ComplianceJourneyPage has no serious axe violations",
    async () => {
      const { container } = render(<ComplianceJourneyPage />);

      expect(await axe(container)).toHaveNoViolations();
    },
    20_000,
  );

  it(
    "ExampleRoiBulletinPage has no serious axe violations",
    async () => {
      const { container } = render(<ExampleRoiBulletinPage />);

      expect(await axe(container)).toHaveNoViolations();
    },
    20_000,
  );

  it(
    "PrivacyPage has no serious axe violations",
    async () => {
      const { container } = render(<PrivacyPage />);

      expect(await axe(container)).toHaveNoViolations();
    },
    20_000,
  );
});
