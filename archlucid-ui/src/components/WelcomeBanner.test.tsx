import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import type { RunSummary } from "@/types/authority";

const listRunsByProjectPaged = vi.fn();

vi.mock("@/lib/api", () => ({
  listRunsByProjectPaged: (...args: unknown[]) => listRunsByProjectPaged(...args),
}));

import { WelcomeBanner } from "./WelcomeBanner";

const SESSION_DISMISS_KEY = "archlucid_welcome_dismissed_session";

const emptyRunsPage = {
  items: [] as RunSummary[],
  totalCount: 0,
  page: 1,
  pageSize: 1,
  hasMore: false,
};

afterEach(() => {
  localStorage.clear();
  sessionStorage.clear();
  vi.clearAllMocks();
});

beforeEach(() => {
  listRunsByProjectPaged.mockResolvedValue(emptyRunsPage);
  globalThis.fetch = vi.fn(async (input: RequestInfo | URL) => {
    const url = typeof input === "string" ? input : input instanceof URL ? input.toString() : input.url;

    if (url.includes("/api/proxy/v1/tenant/trial-status")) {
      return new Response(JSON.stringify({ status: "Inactive" }), { status: 200 });
    }

    return new Response("not found", { status: 404 });
  }) as unknown as typeof fetch;
});

describe("WelcomeBanner — renders heading and CTAs", () => {
  it("shows welcome heading, primary CTA, value card, and example link when not dismissed", async () => {
    render(<WelcomeBanner />);

    await waitFor(() => {
      expect(screen.getByRole("banner", { name: "Welcome" })).toBeInTheDocument();
    });

    expect(screen.getByRole("heading", { name: "Turn architecture proposals into governed, evidence-backed review packages." })).toBeInTheDocument();
    expect(
      screen.getByText(
        "Turn architecture intent into a governed, reviewable manifest with supporting artifacts and findings.",
      ),
    ).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Create Request" })).toHaveAttribute("href", "/runs/new");
    expect(screen.getByText("Governed manifest")).toBeInTheDocument();
    expect(screen.getByText(/one request produces everything needed for review/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/What you will receive from a completed run/)).toBeInTheDocument();
    const exampleLinks = screen.getAllByRole("link", { name: /see completed example/i });
    expect(exampleLinks.length).toBeGreaterThanOrEqual(1);
    expect(exampleLinks[0]).toHaveAttribute("href", "/runs?projectId=default");
    expect(screen.getByTestId("opt-in-tour-launcher")).toBeInTheDocument();
  });

  it("shows returning-user copy when at least one run exists", async () => {
    const run: RunSummary = {
      runId: "00000000-0000-0000-0000-000000000099",
      projectId: "default",
      description: "Demo",
      createdUtc: "2026-01-15T12:00:00.000Z",
      hasFindingsSnapshot: false,
      hasGoldenManifest: false,
    };
    listRunsByProjectPaged.mockResolvedValue({
      items: [run],
      totalCount: 1,
      page: 1,
      pageSize: 1,
      hasMore: false,
    });

    render(<WelcomeBanner />);

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "Architecture manifest workspace" })).toBeInTheDocument();
    });

    expect(
      screen.getByText("Monitor active runs, finalize manifests, and review governance findings."),
    ).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "View runs" })).toHaveAttribute("href", "/runs?projectId=default");
    expect(screen.getByRole("link", { name: /see completed example/i })).toHaveAttribute(
      "href",
      "/runs?projectId=default",
    );
  });
});

describe("WelcomeBanner — dismiss hides banner", () => {
  it("hides after session dismiss click", async () => {
    render(<WelcomeBanner />);

    await waitFor(() => {
      expect(screen.getByRole("banner", { name: "Welcome" })).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole("button", { name: /dismiss welcome/i }));

    expect(screen.queryByRole("banner", { name: "Welcome" })).not.toBeInTheDocument();
    expect(sessionStorage.getItem(SESSION_DISMISS_KEY)).toBe("1");
  });
});

describe("WelcomeBanner — session flag respected on re-render", () => {
  it("stays hidden when session dismissed flag is set", async () => {
    sessionStorage.setItem(SESSION_DISMISS_KEY, "1");
    render(<WelcomeBanner />);

    await waitFor(() => {
      expect(screen.queryByRole("banner", { name: "Welcome" })).not.toBeInTheDocument();
    });
  });
});
