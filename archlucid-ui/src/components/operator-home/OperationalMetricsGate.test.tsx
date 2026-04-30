import { render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

vi.mock("@/lib/core-pilot-commit-context", () => ({
  fetchCorePilotCommitContext: vi.fn(),
}));

import { fetchCorePilotCommitContext } from "@/lib/core-pilot-commit-context";

import { OperationalMetricsGate } from "./OperationalMetricsGate";

const fetchCtx = vi.mocked(fetchCorePilotCommitContext);

describe("OperationalMetricsGate", () => {
  it("renders nothing while loading", () => {
    fetchCtx.mockImplementation(() => new Promise(() => {}));

    render(
      <OperationalMetricsGate>
        <div data-testid="gated-child">Child</div>
      </OperationalMetricsGate>,
    );

    expect(screen.queryByTestId("gated-child")).not.toBeInTheDocument();
  });

  it("hides children when no committed manifest is detected", async () => {
    fetchCtx.mockResolvedValue({
      hasCommittedManifest: false,
      latestRunId: "00000000-0000-0000-0000-000000000099",
      firstCommittedRunId: null,
    });

    render(
      <OperationalMetricsGate>
        <div data-testid="gated-child">Child</div>
      </OperationalMetricsGate>,
    );

    await waitFor(() => {
      expect(fetchCtx).toHaveBeenCalled();
    });

    expect(screen.queryByTestId("gated-child")).not.toBeInTheDocument();
  });

  it("shows children when a committed manifest exists", async () => {
    fetchCtx.mockResolvedValue({
      hasCommittedManifest: true,
      latestRunId: "00000000-0000-0000-0000-000000000001",
      firstCommittedRunId: "00000000-0000-0000-0000-000000000001",
    });

    render(
      <OperationalMetricsGate>
        <div data-testid="gated-child">Child</div>
      </OperationalMetricsGate>,
    );

    expect(await screen.findByTestId("gated-child")).toBeInTheDocument();
  });

  it("fails open when commit context resolution throws", async () => {
    fetchCtx.mockRejectedValue(new Error("network"));

    render(
      <OperationalMetricsGate>
        <div data-testid="gated-child">Child</div>
      </OperationalMetricsGate>,
    );

    expect(await screen.findByTestId("gated-child")).toBeInTheDocument();
  });
});
