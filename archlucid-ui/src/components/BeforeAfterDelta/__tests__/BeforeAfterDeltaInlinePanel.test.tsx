import { render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

vi.mock("@/lib/proxy-fetch-registration-scope", () => ({
  mergeRegistrationScopeForProxy: (init?: RequestInit) => init ?? {},
}));

import { BeforeAfterDeltaInlinePanel } from "../BeforeAfterDeltaInlinePanel";
import {
  installFailingRecentDeltasFetch,
  installRecentDeltasFetch,
  makePayload,
  makeRow,
} from "./sharedRecentDeltasHandler";

describe("BeforeAfterDeltaInlinePanel", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.clearAllMocks();
  });

  it("renders nothing when the recent-deltas request fails", async () => {
    installFailingRecentDeltasFetch();

    const { container } = render(<BeforeAfterDeltaInlinePanel runId="run-current" />);

    await waitFor(() => {
      expect(vi.mocked(fetch)).toHaveBeenCalled();
    });

    expect(container.querySelector('[data-testid="before-after-delta-panel-inline"]')).toBeNull();
  });

  it("renders nothing when the current run is not in the recent window", async () => {
    installRecentDeltasFetch(
      makePayload([
        makeRow({ runId: "row-other-1", requestId: "req-A" }),
        makeRow({ runId: "row-other-2", requestId: "req-B" }),
      ]),
    );

    const { container } = render(<BeforeAfterDeltaInlinePanel runId="run-not-present" />);

    await waitFor(() => {
      expect(vi.mocked(fetch)).toHaveBeenCalled();
    });

    expect(container.querySelector('[data-testid="before-after-delta-panel-inline"]')).toBeNull();
  });

  it("renders the no-prior hint when the current run is the only commit for its request", async () => {
    installRecentDeltasFetch(
      makePayload([
        makeRow({
          runId: "run-current",
          requestId: "req-only",
          manifestCommittedUtc: "2026-04-23T11:00:00Z",
        }),
        makeRow({
          runId: "row-other",
          requestId: "req-different",
          manifestCommittedUtc: "2026-04-22T11:00:00Z",
        }),
      ]),
    );

    render(<BeforeAfterDeltaInlinePanel runId="run-current" />);

    await waitFor(() => {
      expect(screen.getByTestId("before-after-delta-panel-inline")).toBeInTheDocument();
    });

    expect(screen.getByTestId("delta-inline-no-prior")).toHaveTextContent(/req-only/);
    expect(screen.queryByTestId("delta-inline-findings")).toBeNull();
  });

  it("compares against the most recent prior committed run for the same architecture request", async () => {
    installRecentDeltasFetch(
      makePayload([
        makeRow({
          runId: "run-current",
          requestId: "req-shared",
          manifestCommittedUtc: "2026-04-23T11:00:00Z",
          totalFindings: 3,
          timeToCommittedManifestTotalSeconds: 30 * 60,
        }),
        makeRow({
          runId: "run-prior-recent",
          requestId: "req-shared",
          manifestCommittedUtc: "2026-04-23T09:00:00Z",
          totalFindings: 6,
          timeToCommittedManifestTotalSeconds: 60 * 60,
        }),
        makeRow({
          runId: "run-prior-older",
          requestId: "req-shared",
          manifestCommittedUtc: "2026-04-20T09:00:00Z",
          totalFindings: 9,
          timeToCommittedManifestTotalSeconds: 90 * 60,
        }),
        makeRow({
          runId: "run-other-request",
          requestId: "req-other",
          manifestCommittedUtc: "2026-04-23T10:00:00Z",
          totalFindings: 1,
        }),
      ]),
    );

    render(<BeforeAfterDeltaInlinePanel runId="run-current" />);

    await waitFor(() => {
      expect(screen.getByTestId("delta-inline-findings")).toBeInTheDocument();
    });

    expect(screen.getByTestId("delta-inline-findings")).toHaveTextContent("3");
    expect(screen.getByTestId("delta-inline-findings")).toHaveTextContent(/prior: 6/);
    expect(screen.getByTestId("delta-inline-findings-percent")).toHaveTextContent(/50\.0% fewer findings/);

    expect(screen.getByTestId("delta-inline-time")).toHaveTextContent("0.50 h");
    expect(screen.getByTestId("delta-inline-time")).toHaveTextContent(/prior: 1\.00 h/);
    expect(screen.getByTestId("delta-inline-time-percent")).toHaveTextContent(/50\.0% faster/);
  });

  it("calls /v1/pilots/runs/recent-deltas with count=25 (matches inline lookback constant)", async () => {
    const handler = installRecentDeltasFetch(makePayload([makeRow({ runId: "row1" })]));

    render(<BeforeAfterDeltaInlinePanel runId="run-current" />);

    await waitFor(() => {
      expect(handler).toHaveBeenCalled();
    });

    const calls = handler.mock.calls.map((args) => String(args[0]));

    expect(calls.some((u) => u.includes("count=25"))).toBe(true);
  });
});
