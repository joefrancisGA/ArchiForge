import { render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("@/lib/proxy-fetch-registration-scope", () => ({
  mergeRegistrationScopeForProxy: (init?: RequestInit) => init ?? {},
}));

import { BeforeAfterDeltaTopPanel } from "../BeforeAfterDeltaTopPanel";
import {
  installFailingRecentDeltasFetch,
  installRecentDeltasFetch,
  makePayload,
  makeRow,
} from "./sharedRecentDeltasHandler";

describe("BeforeAfterDeltaTopPanel", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.clearAllMocks();
  });

  it("renders nothing while loading and during HTTP failure", async () => {
    installFailingRecentDeltasFetch();

    const { container } = render(<BeforeAfterDeltaTopPanel />);

    await waitFor(() => {
      expect(vi.mocked(fetch)).toHaveBeenCalled();
    });

    expect(container.querySelector('[data-testid="before-after-delta-panel-top"]')).toBeNull();
  });

  it("renders nothing when zero committed runs are in scope", async () => {
    installRecentDeltasFetch({
      items: [],
      requestedCount: 5,
      returnedCount: 0,
      medianTotalFindings: null,
      medianTimeToCommittedManifestTotalSeconds: null,
    });

    const { container } = render(<BeforeAfterDeltaTopPanel />);

    await waitFor(() => {
      expect(vi.mocked(fetch)).toHaveBeenCalled();
    });

    expect(container.querySelector('[data-testid="before-after-delta-panel-top"]')).toBeNull();
  });

  it("renders median findings, median time, and per-run row strip when committed runs exist", async () => {
    const payload = makePayload([
      makeRow({ runId: "aaaaaaaa11111111", totalFindings: 4, timeToCommittedManifestTotalSeconds: 30 * 60 }),
      makeRow({ runId: "bbbbbbbb22222222", totalFindings: 2, timeToCommittedManifestTotalSeconds: 60 * 60 }),
      makeRow({ runId: "cccccccc33333333", totalFindings: 6, timeToCommittedManifestTotalSeconds: 45 * 60 }),
    ]);

    installRecentDeltasFetch(payload);

    render(<BeforeAfterDeltaTopPanel />);

    await waitFor(() => {
      expect(screen.getByTestId("before-after-delta-panel-top")).toBeInTheDocument();
    });

    expect(screen.getByTestId("delta-top-window")).toHaveTextContent("3");
    expect(screen.getByTestId("delta-top-median-findings")).toHaveTextContent("4");
    expect(screen.getByTestId("delta-top-median-time")).toHaveTextContent("0.75 h");
    expect(screen.getByTestId("delta-top-rows").querySelectorAll("li")).toHaveLength(3);
  });

  it("renders the demo badge when a row is flagged as a demo tenant", async () => {
    const payload = makePayload([
      makeRow({ runId: "demoseed00000001", isDemoTenant: true, totalFindings: 1 }),
    ]);

    installRecentDeltasFetch(payload);

    render(<BeforeAfterDeltaTopPanel />);

    await waitFor(() => {
      expect(screen.getByTestId("delta-top-rows")).toBeInTheDocument();
    });

    expect(screen.getByText("demo")).toBeInTheDocument();
  });

  it("respects the count prop in the request URL (server clamps further)", async () => {
    const handler = installRecentDeltasFetch(
      makePayload([makeRow({ runId: "row1" }), makeRow({ runId: "row2" })]),
    );

    render(<BeforeAfterDeltaTopPanel count={2} />);

    await waitFor(() => {
      expect(handler).toHaveBeenCalled();
    });

    const calls = handler.mock.calls.map((args) => String(args[0]));

    expect(calls.some((u) => u.includes("count=2"))).toBe(true);
  });
});
