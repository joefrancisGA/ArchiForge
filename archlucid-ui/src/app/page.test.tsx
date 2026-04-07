import { render, screen } from "@testing-library/react";
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

import HomePage from "./page";

describe("HomePage (55R smoke — landing)", () => {
  it("renders start heading and workflow summary", () => {
    render(<HomePage />);

    expect(screen.getByRole("heading", { level: 2, name: "Start here" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { level: 3, name: "Main workflows" })).toBeInTheDocument();
    expect(screen.getByRole("main").textContent).toContain("review the manifest and artifacts");
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

    const compare = screen.getByRole("link", { name: "Compare runs" });
    expect(compare).toHaveAttribute("href", "/compare");

    const replay = screen.getByRole("link", { name: "Replay run" });
    expect(replay).toHaveAttribute("href", "/replay");
  });
});
