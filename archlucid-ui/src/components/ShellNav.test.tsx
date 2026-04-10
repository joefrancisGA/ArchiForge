import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

import { ShellNav } from "./ShellNav";

vi.mock("next/link", () => ({
  default: ({
    href,
    children,
    title,
    className,
  }: {
    href: string;
    children: import("react").ReactNode;
    title?: string;
    className?: string;
  }) => (
    <a href={href} title={title} className={className}>
      {children}
    </a>
  ),
}));

describe("ShellNav (55R smoke — primary navigation)", () => {
  it("exposes start-and-review workflow links with expected routes", () => {
    render(<ShellNav />);

    const nav = screen.getByRole("navigation", { name: "Primary operator workflows" });
    expect(nav).toBeInTheDocument();

    expect(screen.getByRole("link", { name: "Home" })).toHaveAttribute("href", "/");
    expect(screen.getByRole("link", { name: "New run" })).toHaveAttribute("href", "/runs/new");
    expect(screen.getByRole("link", { name: "Runs" })).toHaveAttribute(
      "href",
      "/runs?projectId=default",
    );
    expect(screen.getByRole("link", { name: "Graph" })).toHaveAttribute("href", "/graph");
    expect(screen.getByRole("link", { name: "Compare two runs" })).toHaveAttribute("href", "/compare");
    expect(screen.getByRole("link", { name: "Replay a run" })).toHaveAttribute("href", "/replay");
  });

  it("exposes Q&A and alerts group navigations", () => {
    render(<ShellNav />);

    expect(screen.getByRole("navigation", { name: "Question answering and advisory" })).toBeInTheDocument();
    expect(screen.getByRole("navigation", { name: "Alerts and governance" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Ask" })).toHaveAttribute("href", "/ask");
    expect(screen.getByRole("link", { name: "Alerts" })).toHaveAttribute("href", "/alerts");
  });
});
