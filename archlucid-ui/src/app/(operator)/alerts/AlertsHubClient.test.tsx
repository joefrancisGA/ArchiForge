import { render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

const tabValue: { current: string | null } = { current: null };
const push = vi.fn();

vi.mock("next/navigation", () => ({
  useRouter: () => ({ push }),
  usePathname: () => "/alerts",
  useSearchParams: () => ({
    get: (k: string) => (k === "tab" ? tabValue.current : null),
  }),
}));

vi.mock("@/components/alerts/AlertsInboxContent", () => ({
  AlertsInboxContent: () => <div data-testid="stub-inbox" />,
}));
vi.mock("@/components/alerts/AlertRulesContent", () => ({
  AlertRulesContent: () => <div data-testid="stub-rules" />,
}));
vi.mock("@/components/alerts/AlertRoutingContent", () => ({
  AlertRoutingContent: () => <div data-testid="stub-routing" />,
}));
vi.mock("@/components/alerts/CompositeAlertRulesContent", () => ({
  CompositeAlertRulesContent: () => <div data-testid="stub-composite" />,
}));
vi.mock("@/components/alerts/AlertSimulationTuningSection", () => ({
  AlertSimulationTuningSection: () => <div data-testid="stub-simulation" />,
}));

import { AlertsHubClient } from "./AlertsHubClient";

describe("AlertsHubClient", () => {
  beforeEach(() => {
    push.mockReset();
    tabValue.current = null;
  });

  it("shows inbox by default (no ?tab=)", () => {
    render(<AlertsHubClient />);
    expect(screen.getByTestId("stub-inbox")).toBeInTheDocument();
  });

  it("shows rules when ?tab=rules", () => {
    tabValue.current = "rules";
    render(<AlertsHubClient />);
    expect(screen.getByTestId("stub-rules")).toBeInTheDocument();
  });

  it("falls back to inbox for unknown ?tab= values", () => {
    tabValue.current = "nope";
    render(<AlertsHubClient />);
    expect(screen.getByTestId("stub-inbox")).toBeInTheDocument();
  });
});
