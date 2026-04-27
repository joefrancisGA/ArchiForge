import { render } from "@testing-library/react";
import { axe, toHaveNoViolations } from "jest-axe";
import { describe, expect, it, vi } from "vitest";

vi.mock("next/navigation", () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), back: vi.fn() }),
  usePathname: () => "/alerts",
  useSearchParams: () => ({
    get: () => null,
    toString: () => "",
  }),
  redirect: vi.fn(),
}));

vi.mock("@/components/alerts/AlertsInboxContent", () => ({
  AlertsInboxContent: () => <div data-testid="stub-inbox">Inbox</div>,
}));

vi.mock("@/components/alerts/AlertRulesContent", () => ({
  AlertRulesContent: () => <div data-testid="stub-rules">Rules</div>,
}));

vi.mock("@/components/alerts/AlertRoutingContent", () => ({
  AlertRoutingContent: () => <div data-testid="stub-routing">Routing</div>,
}));

vi.mock("@/components/alerts/CompositeAlertRulesContent", () => ({
  CompositeAlertRulesContent: () => <div data-testid="stub-composite">Composite</div>,
}));

vi.mock("@/components/alerts/AlertSimulationTuningSection", () => ({
  AlertSimulationTuningSection: () => <div data-testid="stub-simulation">Simulation</div>,
}));

import AlertsPage from "@/app/(operator)/alerts/page";

expect.extend(toHaveNoViolations);

describe("operator alerts pages — axe (Vitest)", () => {
  it(
    "AlertsPage has no serious axe violations",
    async () => {
      const { container } = render(<AlertsPage />);

      expect(await axe(container)).toHaveNoViolations();
    },
    20_000,
  );
});
