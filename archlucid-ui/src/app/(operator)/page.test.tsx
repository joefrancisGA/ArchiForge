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
  it("renders dashboard tagline and product-layer sections", async () => {
    render(<HomePage />);

    expect(screen.getByRole("heading", { level: 2, name: "Operator home" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { level: 3, name: "Core Pilot path" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { level: 3, name: "Advanced Analysis" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { level: 3, name: "Enterprise Controls" })).toBeInTheDocument();
    expect(screen.getByTestId("first-run-panel-mock")).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByTestId("welcome-banner-mock")).toBeInTheDocument();
    });
    expect(screen.getByText(/three product layers/i)).toBeInTheDocument();
  });

  it("exposes primary workflow destinations matching shell review paths", () => {
    render(<HomePage />);

    const runsLinks = screen
      .getAllByRole("link", { name: "Runs" })
      .filter((el) => el.getAttribute("href") === "/runs?projectId=default");
    expect(runsLinks.length).toBeGreaterThan(0);

    const graphLinks = screen
      .getAllByRole("link", { name: "Graph" })
      .filter((el) => el.getAttribute("href") === "/graph");
    expect(graphLinks.length).toBeGreaterThan(0);

    const compareLinks = screen.getAllByRole("link", { name: "Compare two runs" });
    expect(compareLinks.some((el) => el.getAttribute("href") === "/compare")).toBe(true);

    const replayLinks = screen.getAllByRole("link", { name: "Replay a run" });
    expect(replayLinks.some((el) => el.getAttribute("href") === "/replay")).toBe(true);
  });
});
