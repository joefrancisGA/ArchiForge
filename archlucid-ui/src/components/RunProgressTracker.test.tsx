import { act, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { RunProgressTracker } from "@/components/RunProgressTracker";
import type { RunSummary } from "@/types/authority";

vi.mock("@/lib/api", () => ({
  getRunSummary: vi.fn(),
}));

import { getRunSummary } from "@/lib/api";

const mockGetRunSummary = vi.mocked(getRunSummary);

const baseSummary: RunSummary = {
  runId: "run-progress-1",
  projectId: "default",
  createdUtc: "2026-01-01T00:00:00.000Z",
};

function committedSummary(runId: string): RunSummary {
  return {
    ...baseSummary,
    runId,
    hasContextSnapshot: true,
    hasGraphSnapshot: true,
    hasFindingsSnapshot: true,
    hasGoldenManifest: true,
  };
}

describe("RunProgressTracker", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("does not poll when run is already committed", async () => {
    render(
      <RunProgressTracker runId="committed-1" initialSummary={committedSummary("committed-1")} />,
    );

    await act(async () => {
      await Promise.resolve();
    });

    expect(mockGetRunSummary).not.toHaveBeenCalled();
    expect(screen.queryByRole("heading", { name: "Pipeline progress" })).not.toBeInTheDocument();
  });

  it("polls and updates badges as stages complete", async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true });

    mockGetRunSummary
      .mockResolvedValueOnce({
        ...baseSummary,
        runId: "live-1",
        hasContextSnapshot: true,
      })
      .mockResolvedValue({
        ...baseSummary,
        runId: "live-1",
        hasContextSnapshot: true,
        hasGraphSnapshot: true,
        hasFindingsSnapshot: true,
        hasGoldenManifest: true,
      });

    render(
      <RunProgressTracker
        runId="live-1"
        initialSummary={{
          ...baseSummary,
          runId: "live-1",
        }}
      />,
    );

    await act(async () => {
      await vi.runOnlyPendingTimersAsync();
    });

    expect(mockGetRunSummary).toHaveBeenCalled();
    expect(screen.getAllByText("Ready").length).toBe(1);

    await act(async () => {
      await vi.advanceTimersByTimeAsync(3000);
    });

    await act(async () => {
      await vi.runOnlyPendingTimersAsync();
    });

    expect(screen.getAllByText("Ready").length).toBe(4);
  });

  it("stops polling when all stages are ready", async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true });

    mockGetRunSummary.mockResolvedValue({
      ...baseSummary,
      runId: "stop-1",
      hasContextSnapshot: true,
      hasGraphSnapshot: true,
      hasFindingsSnapshot: true,
      hasGoldenManifest: true,
    });

    render(
      <RunProgressTracker
        runId="stop-1"
        initialSummary={{
          ...baseSummary,
          runId: "stop-1",
          hasContextSnapshot: true,
        }}
      />,
    );

    await act(async () => {
      await vi.runOnlyPendingTimersAsync();
    });

    expect(mockGetRunSummary.mock.calls.length).toBeGreaterThanOrEqual(1);
    expect(screen.getByText("Pipeline complete — refresh for full detail.")).toBeInTheDocument();

    const callsAfterComplete = mockGetRunSummary.mock.calls.length;

    await act(async () => {
      await vi.advanceTimersByTimeAsync(60_000);
    });

    expect(mockGetRunSummary.mock.calls.length).toBe(callsAfterComplete);
  });

  it("shows timeout message after max duration", async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true });

    mockGetRunSummary.mockResolvedValue({
      ...baseSummary,
      runId: "slow-1",
      hasContextSnapshot: true,
    });

    render(
      <RunProgressTracker
        runId="slow-1"
        initialSummary={{
          ...baseSummary,
          runId: "slow-1",
        }}
      />,
    );

    await act(async () => {
      await vi.runOnlyPendingTimersAsync();
    });

    expect(mockGetRunSummary).toHaveBeenCalled();

    await act(async () => {
      await vi.advanceTimersByTimeAsync(180_000);
    });

    await act(async () => {
      await vi.runOnlyPendingTimersAsync();
    });

    expect(
      screen.getByText("Pipeline still in progress. Refresh manually to check."),
    ).toBeInTheDocument();
  });
});
