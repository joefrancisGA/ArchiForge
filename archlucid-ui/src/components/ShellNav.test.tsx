import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { ShellNav } from "./ShellNav";

vi.mock("next/navigation", () => ({
  usePathname: (): string => "/",
}));

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
  beforeEach(() => {
    // Progressive disclosure persists `archlucid_nav_show_extended` in localStorage; clear so tests
    // do not inherit "Show more links" from a prior case in the same file.
    localStorage.clear();
  });

  it(
    "shows only Core Pilot essential links by default — Graph, Compare, and Replay require Show more links",
    () => {
      render(<ShellNav />);

      // nav group is now labelled with the product layer name "Core Pilot"
      const nav = screen.getByRole("navigation", { name: "Core Pilot" });
      expect(nav).toBeInTheDocument();

      const homeLink = screen.getByRole("link", { name: "Home" });
      expect(homeLink).toHaveAttribute("href", "/");
      expect(homeLink).toHaveAttribute("aria-current", "page");
      expect(screen.getByRole("link", { name: "New run" })).toHaveAttribute("href", "/runs/new");
      expect(screen.getByRole("link", { name: "New run" })).toHaveAttribute(
        "title",
        "Guided first-run wizard — system identity through pipeline tracking (Alt+N)",
      );
      expect(screen.getByRole("link", { name: "Runs" })).toHaveAttribute("href", "/runs?projectId=default");
      expect(screen.getByRole("link", { name: "Onboarding" })).toHaveAttribute("href", "/onboarding");

      // Graph is now tier="extended" — Core Pilot path does not require graph exploration.
      // It must NOT be visible in the default (no extended) sidebar.
      expect(screen.queryByRole("link", { name: "Graph" })).toBeNull();
      expect(screen.queryByRole("link", { name: "Compare two runs" })).toBeNull();
      expect(screen.queryByRole("link", { name: "Replay a run" })).toBeNull();

      // After enabling extended links, all three become visible.
      const showMore = screen.queryByRole("button", { name: "Show more links" });
      if (showMore) {
        fireEvent.click(showMore);
      }

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

  it("exposes Advanced Analysis and Enterprise Controls group navigations when sections are expanded", () => {
    render(<ShellNav />);

    // "Q&A & advisory" is now labelled "Advanced Analysis" (product layer name)
    expect(screen.getByRole("navigation", { name: "Advanced Analysis" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Ask" })).toHaveAttribute("href", "/ask");

    const showMore = screen.queryByRole("button", { name: "Show more links" });
    if (showMore) {
      fireEvent.click(showMore);
    }

    // "Alerts & governance" is now labelled "Enterprise Controls" — expand the group so the link is in the a11y tree.
    fireEvent.click(screen.getByRole("button", { name: "Enterprise Controls" }));

    expect(screen.getByRole("link", { name: "Dashboard" })).toHaveAttribute("href", "/governance/dashboard");

    // Enterprise Controls group: ensure <nav> is present now that the section is open.
    expect(screen.getByRole("navigation", { name: "Enterprise Controls" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Alerts" })).toHaveAttribute("href", "/alerts");
    expect(
      screen.getByText(
        /Operator\/admin layer—governance, audit, policy packs, and alert tooling\. Typically governance or platform operators; not required for Core Pilot\./i,
      ),
    ).toBeInTheDocument();
    expect(
      screen.getByText(/Governance and platform depth; optional vs Core Pilot\./i),
    ).toBeInTheDocument();

    expect(screen.queryByRole("link", { name: "Governance workflow" })).toBeNull();

    fireEvent.click(screen.getByRole("button", { name: "Navigation settings" }));
    fireEvent.click(screen.getByRole("checkbox", { name: "Show advanced links" }));
    fireEvent.click(screen.getByRole("button", { name: "Close" }));

    expect(screen.getByRole("link", { name: "Governance workflow" })).toHaveAttribute("href", "/governance");
  });

  it("shows a hint for opening help and keyboard shortcuts", () => {
    render(<ShellNav />);

    expect(screen.getByText("Press Shift+? for help and keyboard shortcuts")).toBeInTheDocument();
  });
});
