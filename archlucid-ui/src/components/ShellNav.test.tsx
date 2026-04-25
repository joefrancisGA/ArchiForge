import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { enterpriseNavHintOperatorRank } from "@/lib/enterprise-controls-context-copy";
import { NAV_DISCLOSURE } from "@/lib/nav-disclosure-copy";

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
    // do not inherit extended disclosure state from a prior case in the same file.
    localStorage.clear();
  });

  it(
    "shows only Pilot essential links by default — Graph, Compare, and Replay require extended disclosure",
    () => {
      render(<ShellNav />);

      const nav = screen.getByRole("navigation", { name: "Pilot" });
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
      const showMore = screen.queryByRole("button", { name: NAV_DISCLOSURE.extended.show });
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

  it("exposes Operate · analysis and Operate · governance group navigations when sections are expanded", () => {
    render(<ShellNav />);

    expect(screen.getByRole("navigation", { name: "Operate · analysis" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Ask" })).toHaveAttribute("href", "/ask");

    const showMore = screen.queryByRole("button", { name: NAV_DISCLOSURE.extended.show });
    if (showMore) {
      fireEvent.click(showMore);
    }

    fireEvent.click(screen.getByRole("button", { name: "Operate · governance" }));

    expect(screen.getByRole("link", { name: "Dashboard" })).toHaveAttribute("href", "/governance/dashboard");

    expect(screen.getByRole("navigation", { name: "Operate · governance" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Alerts" })).toHaveAttribute("href", "/alerts");
    expect(
      screen.getByText(
        "Governance, audit, policy packs, alerts, and trust. Operator-heavy; Execute+ for writes where the API requires it — not required for first Pilot proof.",
      ),
    ).toBeInTheDocument();
    expect(screen.getByText(enterpriseNavHintOperatorRank)).toBeInTheDocument();

    expect(screen.queryByRole("link", { name: "Governance workflow" })).toBeNull();

    fireEvent.click(screen.getByRole("button", { name: "Navigation settings" }));
    fireEvent.click(screen.getByRole("checkbox", { name: NAV_DISCLOSURE.advanced.show }));
    fireEvent.click(screen.getByRole("button", { name: "Close" }));

    expect(screen.getByRole("link", { name: "Governance workflow" })).toHaveAttribute("href", "/governance");
  });

  it("shows a hint for opening help and keyboard shortcuts", () => {
    render(<ShellNav />);

    expect(screen.getByText("Press Shift+? for help and keyboard shortcuts")).toBeInTheDocument();
  });
});
