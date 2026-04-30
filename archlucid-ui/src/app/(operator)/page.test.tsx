import { render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const listRunsByProjectPaged = vi.fn();

vi.mock("@/lib/api", () => ({
  listRunsByProjectPaged: (...args: unknown[]) => listRunsByProjectPaged(...args),
}));

vi.mock("next/link", () => ({
  default: ({
    href,
    children,
    ...rest
  }: {
    href: string;
    children: import("react").ReactNode;
  } & Record<string, unknown>) => (
    <a href={href} {...rest}>
      {children}
    </a>
  ),
}));

vi.mock("@/components/HomeFirstRunWorkflowGate", () => ({
  HomeFirstRunWorkflowGate: () => (
    <div data-testid="first-run-panel-mock" aria-hidden>
      First-run panel mock
    </div>
  ),
}));

vi.mock("@/components/WelcomeBanner", () => ({
  WelcomeBanner: () => <div data-testid="welcome-banner-mock">Welcome mock</div>,
}));

vi.mock("@/components/CorePilotOneSessionChecklist", () => ({
  CorePilotOneSessionChecklist: () => <div data-testid="core-pilot-one-session-checklist-mock" />,
}));

vi.mock("@/components/TrialWelcomeRunDeepLink", () => ({
  TrialWelcomeRunDeepLink: () => null,
}));

vi.mock("@/components/PilotOutcomeCard", () => ({
  PilotOutcomeCard: () => <div data-testid="pilot-outcome-mock" aria-hidden />,
}));

vi.mock("@/components/operator-home/OperationalMetricsGate", () => ({
  OperationalMetricsGate: ({ children }: { children: import("react").ReactNode }) => <>{children}</>,
}));

vi.mock("@/components/OperatorHomeGate", () => ({
  OperatorHomeGate: ({ children }: { children: import("react").ReactNode }) => <>{children}</>,
}));

import HomePage from "./page";

afterEach(() => {
  vi.clearAllMocks();
});

beforeEach(() => {
  listRunsByProjectPaged.mockResolvedValue({
    items: [],
    totalCount: 0,
    page: 1,
    pageSize: 5,
    hasMore: false,
  });
});

describe("HomePage (55R smoke — landing)", () => {
  it("renders Runs panel, maturity layer cards, and workflow panel", async () => {
    render(<HomePage />);

    expect(screen.getByRole("heading", { name: "Runs" })).toBeInTheDocument();
    expect(screen.getByText("Advanced Analysis")).toBeInTheDocument();
    expect(screen.getByText("Enterprise Controls")).toBeInTheDocument();
    expect(screen.getByText("Search & Insights")).toBeInTheDocument();
    expect(screen.getByTestId("first-run-panel-mock")).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByTestId("welcome-banner-mock")).toBeInTheDocument();
    });
  });

  it("exposes create-first-request CTA from runs empty state and layer card links", async () => {
    render(<HomePage />);

    const runsLinks = screen
      .getAllByRole("link")
      .filter((el) => el.getAttribute("href") === "/runs?projectId=default");
    expect(runsLinks.length).toBeGreaterThan(0);

    await waitFor(() => {
      expect(screen.getByRole("link", { name: "Create your first request" })).toBeInTheDocument();
    });
  });

  it("exposes primary workflow destinations matching shell review paths", async () => {
    render(<HomePage />);

    await waitFor(() => {
      expect(screen.getByRole("link", { name: "Open full runs list" })).toHaveAttribute(
        "href",
        "/runs?projectId=default",
      );
    });
  });
});
