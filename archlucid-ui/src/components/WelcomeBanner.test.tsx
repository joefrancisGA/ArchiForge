import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import type { RunSummary } from "@/types/authority";

const listRunsByProjectPaged = vi.fn();

vi.mock("@/lib/api", () => ({
  listRunsByProjectPaged: (...args: unknown[]) => listRunsByProjectPaged(...args),
}));

import { WelcomeBanner } from "./WelcomeBanner";

const STORAGE_KEY = "archlucid_welcome_dismissed";

const emptyRunsPage = {
  items: [] as RunSummary[],
  totalCount: 0,
  page: 1,
  pageSize: 1,
  hasMore: false,
};

afterEach(() => {
  localStorage.clear();
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
  it("shows welcome heading, primary CTA, sample output preview, and example link when not dismissed", async () => {
    render(<WelcomeBanner />);

    await waitFor(() => {
      expect(screen.getByRole("banner", { name: "Welcome" })).toBeInTheDocument();
    });

    expect(screen.getByRole("heading", { name: "Generate your first architecture manifest" })).toBeInTheDocument();
    expect(
      screen.getByText(
        "Turn architecture intent into a governed, reviewable manifest with supporting artifacts and findings.",
      ),
    ).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Create Request" })).toHaveAttribute("href", "/runs/new");
    expect(screen.getByRole("link", { name: "See completed example" })).toHaveAttribute(
      "href",
      "/runs?projectId=default",
    );
    expect(screen.getByLabelText("Sample completed run output")).toBeInTheDocument();
    expect(screen.getByText("Sample output includes")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "See a completed example" })).toHaveAttribute(
      "href",
      "/runs?projectId=default",
    );
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
  });
});

describe("WelcomeBanner — dismiss hides banner", () => {
  it("hides after dismiss click", async () => {
    render(<WelcomeBanner />);

    await waitFor(() => {
      expect(screen.getByRole("banner", { name: "Welcome" })).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole("button", { name: "Dismiss welcome banner" }));

    expect(screen.queryByRole("banner", { name: "Welcome" })).not.toBeInTheDocument();
    expect(localStorage.getItem(STORAGE_KEY)).toBe("1");
  });
});

describe("WelcomeBanner — localStorage respected on re-render", () => {
  it("stays hidden when dismissed flag is set", async () => {
    localStorage.setItem(STORAGE_KEY, "1");
    render(<WelcomeBanner />);

    await waitFor(() => {
      expect(screen.queryByRole("banner", { name: "Welcome" })).not.toBeInTheDocument();
    });
  });
});
