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

import HomePage from "./page";

describe("HomePage (55R smoke — landing)", () => {
  it("renders dashboard tagline and quick links", async () => {
    render(<HomePage />);

    expect(screen.getByRole("heading", { level: 2, name: "Operator home" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { level: 3, name: "Quick links" })).toBeInTheDocument();
    expect(screen.getByTestId("first-run-panel-mock")).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByTestId("welcome-banner-mock")).toBeInTheDocument();
    });
    expect(
      screen.getByText("Create architecture runs, review manifests, and track governance from this dashboard."),
    ).toBeInTheDocument();
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
