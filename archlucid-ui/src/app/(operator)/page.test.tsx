import { render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

vi.mock("next/link", () => ({
  default: ({
    href,
    children,
  }: {
    href: string;
    children: import("react").ReactNode;
  }) => <a href={href}>{children}</a>,
}));

vi.mock("@/components/OperatorFirstRunWorkflowPanel", () => ({
  OperatorFirstRunWorkflowPanel: () => (
    <div data-testid="first-run-panel-mock" aria-hidden>
      First-run panel mock
    </div>
  ),
}));

vi.mock("@/components/WelcomeBanner", () => ({
  WelcomeBanner: () => <div data-testid="welcome-banner-mock">Welcome mock</div>,
}));

vi.mock("@/components/TrialWelcomeRunDeepLink", () => ({
  TrialWelcomeRunDeepLink: () => null,
}));

vi.mock("@/components/PilotOutcomeCard", () => ({
  PilotOutcomeCard: () => <div data-testid="pilot-outcome-mock" aria-hidden />,
}));

vi.mock("@/components/OperatorHomeGate", () => ({
  OperatorHomeGate: ({ children }: { children: import("react").ReactNode }) => <>{children}</>,
}));

import HomePage from "./page";

describe("HomePage (55R smoke — landing)", () => {
  it("renders action cards, maturity layer cards, and workflow panel", async () => {
    render(<HomePage />);

    expect(screen.getByText("Advanced Analysis")).toBeInTheDocument();
    expect(screen.getByText("Enterprise Controls")).toBeInTheDocument();
    expect(screen.getByText("Search & Insights")).toBeInTheDocument();
    expect(screen.getByTestId("first-run-panel-mock")).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByTestId("welcome-banner-mock")).toBeInTheDocument();
    });
  });

  it("exposes primary workflow destinations via action cards and layer cards", () => {
    render(<HomePage />);

    const runsLinks = screen
      .getAllByRole("link")
      .filter((el) => el.getAttribute("href") === "/runs?projectId=default");
    expect(runsLinks.length).toBeGreaterThan(0);

    expect(screen.getByText("Create Run")).toBeInTheDocument();
    expect(screen.getByText("View Runs")).toBeInTheDocument();
    expect(screen.getByText("Commit Run")).toBeInTheDocument();
    expect(screen.getByText("Review Artifacts")).toBeInTheDocument();
  });
});
