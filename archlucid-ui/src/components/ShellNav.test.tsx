import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

import { ShellNav } from "./ShellNav";

vi.mock("next/link", () => ({
  default: ({
    href,
    children,
    title,
    className,
    ...rest
  }: {
    href: string;
    children: import("react").ReactNode;
    title?: string;
    className?: string;
  } & Record<string, unknown>) => (
    <a href={href} title={title} className={className} {...rest}>
      {children}
    </a>
  ),
}));

describe("ShellNav (sidebar re-export — primary navigation)", () => {
  it(
    "exposes runs-and-review workflow links with expected routes",
    () => {
    render(<ShellNav />);

    const nav = screen.getByRole("navigation", { name: "Runs & review" });
    expect(nav).toBeInTheDocument();

    expect(screen.getByRole("link", { name: "Home" })).toHaveAttribute("href", "/");
    expect(screen.getByRole("link", { name: "New run" })).toHaveAttribute("href", "/runs/new");
    expect(screen.getByRole("link", { name: "New run" })).toHaveAttribute(
      "title",
      "Guided first-run wizard — system identity through pipeline tracking (Alt+N)",
    );
    expect(screen.getByRole("link", { name: "Runs" })).toHaveAttribute("href", "/runs?projectId=default");
    expect(screen.getByRole("link", { name: "Graph" })).toHaveAttribute("href", "/graph");
    expect(screen.getByRole("link", { name: "Graph" })).toHaveAttribute(
      "title",
      "Provenance or architecture graph for one run ID (Alt+Y)",
    );
    expect(screen.getByRole("link", { name: "Compare two runs" })).toHaveAttribute("href", "/compare");
    expect(screen.getByRole("link", { name: "Replay a run" })).toHaveAttribute("href", "/replay");

    const linksWithKeyShortcuts = screen
      .getAllByRole("link")
      .filter((link) => {
        const value = link.getAttribute("aria-keyshortcuts");

        return value !== null && value !== "";
      });
    expect(linksWithKeyShortcuts.length).toBeGreaterThanOrEqual(2);
    },
    15_000,
  );

  it("exposes Q&A and alerts group navigations", () => {
    render(<ShellNav />);

    expect(screen.getByRole("navigation", { name: "Q&A & advisory" })).toBeInTheDocument();
    expect(screen.getByRole("navigation", { name: "Alerts & governance" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Ask" })).toHaveAttribute("href", "/ask");
    expect(screen.getByRole("link", { name: "Alerts" })).toHaveAttribute("href", "/alerts");
    expect(screen.getByRole("link", { name: "Dashboard" })).toHaveAttribute("href", "/governance/dashboard");
    expect(screen.getByRole("link", { name: "Governance workflow" })).toHaveAttribute("href", "/governance");
  });

  it("shows a hint for opening keyboard shortcuts help", () => {
    render(<ShellNav />);

    expect(screen.getByText("Press Shift+? for keyboard shortcuts")).toBeInTheDocument();
  });
});
