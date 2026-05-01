import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("@/lib/api", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/lib/api")>();

  return {
    ...actual,
    listRunsByProjectPaged: vi.fn(),
  };
});

import { listRunsByProjectPaged } from "@/lib/api";

import { RunsDashboardPanel } from "./RunsDashboardPanel";

import type { RunSummary } from "@/types/authority";

const listRuns = vi.mocked(listRunsByProjectPaged);

const originalFetch = globalThis.fetch;

function stubFetchForDashboard() {
  globalThis.fetch = vi.fn(async (input: RequestInfo | URL) => {
    const url = typeof input === "string" ? input : input instanceof URL ? input.toString() : input.url;

    if (url.includes("/api/proxy/v1/pilots/runs/recent-deltas")) {
      return new Response(
        JSON.stringify({
          items: [],
          requestedCount: 5,
          returnedCount: 0,
          medianTotalFindings: null,
          medianTimeToCommittedManifestTotalSeconds: null,
        }),
        { status: 200 },
      );
    }

    return new Response("not found", { status: 404 });
  }) as unknown as typeof fetch;
}

describe("RunsDashboardPanel", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
  });

  it("renders runs section and tabbed card", async () => {
    listRuns.mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 5,
      hasMore: false,
    });
    stubFetchForDashboard();

    render(<RunsDashboardPanel />);

    expect(screen.getByRole("heading", { name: /^runs$/i })).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByTestId("runs-dashboard-panel")).toBeInTheDocument();
    });
  });

  it("lists recent runs and links to run detail", async () => {
    const run: RunSummary = {
      runId: "11111111-1111-1111-1111-111111111111",
      projectId: "default",
      description: "Sample",
      createdUtc: "2026-01-15T12:00:00.000Z",
      hasFindingsSnapshot: false,
      hasGoldenManifest: false,
    };
    listRuns.mockResolvedValue({
      items: [run],
      totalCount: 1,
      page: 1,
      pageSize: 5,
      hasMore: false,
    });
    stubFetchForDashboard();

    render(<RunsDashboardPanel />);

    expect(await screen.findByTestId("recent-runs-home-panel")).toBeInTheDocument();
    const link = await screen.findByRole("link", { name: "Sample" });
    expect(link).toHaveAttribute("href", "/reviews/11111111-1111-1111-1111-111111111111");
  });

  it("shows empty state when there are no runs", async () => {
    listRuns.mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 5,
      hasMore: false,
    });
    stubFetchForDashboard();

    render(<RunsDashboardPanel />);

    await waitFor(() => {
      expect(screen.getByTestId("operator-home-getting-started")).toBeInTheDocument();
    });
    expect(
      screen.getByText(
        /You have no architecture reviews yet\. Create a request to produce a manifest/i,
      ),
    ).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Create your first request" })).toHaveAttribute("href", "/reviews/new");
    expect(screen.getByTestId("example-request-panel")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Use this example" })).toHaveAttribute(
      "href",
      "/reviews/new?example=healthcare-claims-intake",
    );
    expect(screen.getByRole("link", { name: "See completed output" })).toHaveAttribute(
      "href",
      "/reviews?projectId=default",
    );
  });

  it("handles runs list API errors in the recent tab", async () => {
    listRuns.mockRejectedValue(new Error("runs unavailable"));
    stubFetchForDashboard();

    render(<RunsDashboardPanel />);

    await waitFor(() => {
      expect(screen.getByText(/runs unavailable/i)).toBeInTheDocument();
    });
  });

  it("shows pipeline status for runs needing attention tab", async () => {
    const run: RunSummary = {
      runId: "00000000-0000-0000-0000-000000000099",
      projectId: "default",
      description: "Demo",
      createdUtc: "2026-01-15T12:00:00.000Z",
      hasFindingsSnapshot: true,
      hasGoldenManifest: false,
    };
    listRuns.mockResolvedValue({
      items: [run],
      totalCount: 1,
      page: 1,
      pageSize: 5,
      hasMore: false,
    });
    stubFetchForDashboard();

    render(<RunsDashboardPanel />);

    await waitFor(() => {
      expect(screen.getByRole("tab", { name: /needs attention/i })).toBeInTheDocument();
    });
    fireEvent.click(screen.getByRole("tab", { name: /needs attention/i }));

    expect(await screen.findByLabelText(/Run pipeline status: Ready to finalize/i)).toBeInTheDocument();
  });
});
