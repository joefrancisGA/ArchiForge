import { render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

vi.mock("@/lib/proxy-fetch-registration-scope", () => ({
  mergeRegistrationScopeForProxy: (init?: RequestInit) => init ?? {},
}));

import { BeforeAfterDeltaSidebarPanel } from "../BeforeAfterDeltaSidebarPanel";
import {
  installFailingRecentDeltasFetch,
  installRecentDeltasFetch,
  makePayload,
  makeRow,
} from "./sharedRecentDeltasHandler";

describe("BeforeAfterDeltaSidebarPanel", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.clearAllMocks();
  });

  it("renders nothing when the recent-deltas request fails", async () => {
    installFailingRecentDeltasFetch();

    const { container } = render(<BeforeAfterDeltaSidebarPanel />);

    await waitFor(() => {
      expect(vi.mocked(fetch)).toHaveBeenCalled();
    });

    expect(container.querySelector('[data-testid="before-after-delta-panel-sidebar"]')).toBeNull();
  });

  it("renders nothing when the response has zero committed runs", async () => {
    installRecentDeltasFetch({
      items: [],
      requestedCount: 5,
      returnedCount: 0,
      medianTotalFindings: null,
      medianTimeToCommittedManifestTotalSeconds: null,
    });

    const { container } = render(<BeforeAfterDeltaSidebarPanel />);

    await waitFor(() => {
      expect(vi.mocked(fetch)).toHaveBeenCalled();
    });

    expect(container.querySelector('[data-testid="before-after-delta-panel-sidebar"]')).toBeNull();
  });

  it("renders the compact median card when committed runs exist", async () => {
    const payload = makePayload([
      makeRow({ runId: "row1", totalFindings: 3, timeToCommittedManifestTotalSeconds: 30 * 60 }),
      makeRow({ runId: "row2", totalFindings: 7, timeToCommittedManifestTotalSeconds: 90 * 60 }),
    ]);

    installRecentDeltasFetch(payload);

    render(<BeforeAfterDeltaSidebarPanel />);

    await waitFor(() => {
      expect(screen.getByTestId("before-after-delta-panel-sidebar")).toBeInTheDocument();
    });

    expect(screen.getByTestId("delta-sidebar-window")).toHaveTextContent("2");
    expect(screen.getByTestId("delta-sidebar-median-findings")).toHaveTextContent("5");
    expect(screen.getByTestId("delta-sidebar-median-time")).toHaveTextContent("1.00 h");
  });
});
