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
    "shows compact Pilot by default; navigation settings can reveal extended Analysis links",
    () => {
      render(<ShellNav />);

      const nav = screen.getByRole("navigation", { name: "Pilot" });
      expect(nav).toBeInTheDocument();

      const homeLink = screen.getByRole("link", { name: "Home" });
      expect(homeLink).toHaveAttribute("href", "/");
      expect(homeLink).toHaveAttribute("aria-current", "page");
      expect(screen.getByRole("link", { name: "New request" })).toHaveAttribute("href", "/runs/new");
      expect(screen.getByRole("link", { name: "New request" })).toHaveAttribute(
        "title",
        "Guided new-request wizard — system identity through pipeline tracking (Alt+N)",
      );
      expect(screen.getByRole("link", { name: "Runs" })).toHaveAttribute("href", "/runs?projectId=default");

      expect(screen.queryByRole("link", { name: "Graph" })).toBeNull();
      expect(screen.queryByRole("link", { name: "Compare two runs" })).toBeNull();
      expect(screen.queryByRole("link", { name: "Replay a run" })).toBeNull();

      fireEvent.click(screen.getByRole("button", { name: "Navigation settings" }));
      fireEvent.click(screen.getByRole("checkbox", { name: NAV_DISCLOSURE.extended.show }));
      fireEvent.click(screen.getByRole("button", { name: "Close" }));

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

  it(
    "exposes Analysis and Governance group navigations when sections are expanded",
    () => {
      render(<ShellNav />);

      fireEvent.click(screen.getByRole("button", { name: "Navigation settings" }));
      fireEvent.click(screen.getByRole("checkbox", { name: NAV_DISCLOSURE.extended.show }));
      fireEvent.click(screen.getByRole("button", { name: "Close" }));

      expect(screen.getByRole("navigation", { name: "Analysis" })).toBeInTheDocument();
      expect(screen.getByRole("link", { name: "Ask" })).toHaveAttribute("href", "/ask");

      fireEvent.click(screen.getByRole("button", { name: "Governance" }));

      expect(screen.getByRole("link", { name: "Findings" })).toHaveAttribute("href", "/governance/findings");

      expect(screen.getByRole("navigation", { name: "Governance" })).toBeInTheDocument();
      expect(screen.getByRole("link", { name: "Alerts" })).toHaveAttribute("href", "/alerts");
      expect(screen.getByRole("button", { name: "Governance" })).toHaveAttribute(
        "title",
        "Policy, audit, alerts, and trust controls.",
      );
      expect(screen.getByText(enterpriseNavHintOperatorRank)).toBeInTheDocument();

      expect(screen.queryByRole("link", { name: "Governance workflow" })).toBeNull();

      fireEvent.click(screen.getByRole("button", { name: "Navigation settings" }));
      fireEvent.click(screen.getByRole("checkbox", { name: NAV_DISCLOSURE.advanced.show }));
      fireEvent.click(screen.getByRole("button", { name: "Close" }));

      expect(screen.getByRole("link", { name: "Governance workflow" })).toHaveAttribute("href", "/governance");
    },
    15_000,
  );

  it("does not show a footer keyboard-shortcut hint in the sidebar", () => {
    render(<ShellNav />);

    expect(screen.queryByText("Press Shift+? for help and keyboard shortcuts")).toBeNull();
  });
});
