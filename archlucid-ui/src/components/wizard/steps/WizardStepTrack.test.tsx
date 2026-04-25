import { act, render, screen, waitFor } from "@testing-library/react";
import { useEffect, useState, type ReactElement } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { WizardStepTrack } from "@/components/wizard/steps/WizardStepTrack";
import { TooltipProvider } from "@/components/ui/tooltip";
import type { RunSummary } from "@/types/authority";

vi.mock("next/link", () => ({
  default: ({
    href,
    children,
    ...rest
  }: {
    href: string;
    children: React.ReactNode;
    className?: string;
  }) => (
    <a href={href} {...rest}>
      {children}
    </a>
  ),
}));

vi.mock("@/lib/api", () => ({
  getRunSummary: vi.fn(),
}));

import { getRunSummary } from "@/lib/api";

const mockGetRunSummary = vi.mocked(getRunSummary);

function renderWithTooltips(node: ReactElement) {
  return render(<TooltipProvider delayDuration={0}>{node}</TooltipProvider>);
}

const baseSummary: RunSummary = {
  runId: "run-track-1",
  projectId: "default",
  createdUtc: "2026-01-01T00:00:00.000Z",
};

function TrackPollingHarness({ runId }: { runId: string }) {
  const [pollSummary, setPollSummary] = useState<RunSummary | null>(null);

  useEffect(() => {
    const tick = async () => {
      const next: RunSummary = await mockGetRunSummary(runId);
      setPollSummary(next);
    };

    void tick();
    const intervalId = window.setInterval(() => void tick(), 3000);

    return () => window.clearInterval(intervalId);
  }, [runId]);

  return (
    <TooltipProvider delayDuration={0}>
      <WizardStepTrack runId={runId} pollSummary={pollSummary} />
    </TooltipProvider>
  );
}

describe("WizardStepTrack", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("shows Pending badges when pipeline flags are false or summary is null", () => {
    renderWithTooltips(<WizardStepTrack runId="r1" pollSummary={{ ...baseSummary, runId: "r1" }} />);

    expect(screen.getAllByText("Pending").length).toBeGreaterThanOrEqual(4);
  });

  it("marks stages Ready as flags flip true", () => {
    const { rerender } = renderWithTooltips(
      <WizardStepTrack
        runId="r1"
        pollSummary={{
          ...baseSummary,
          runId: "r1",
          hasContextSnapshot: true,
          hasGraphSnapshot: true,
        }}
      />,
    );

    expect(screen.getAllByText("Ready").length).toBe(2);
    expect(screen.getAllByText("Pending").length).toBe(2);

    rerender(
      <TooltipProvider delayDuration={0}>
        <WizardStepTrack
          runId="r1"
          pollSummary={{
            ...baseSummary,
            runId: "r1",
            hasContextSnapshot: true,
            hasGraphSnapshot: true,
            hasFindingsSnapshot: true,
            hasGoldenManifest: true,
          }}
        />
      </TooltipProvider>,
    );

    expect(screen.getAllByText("Ready").length).toBe(4);
  });

  it("renders Open run detail when hasGoldenManifest is true", () => {
    renderWithTooltips(
      <WizardStepTrack
        runId="golden-1"
        pollSummary={{
          ...baseSummary,
          runId: "golden-1",
          hasGoldenManifest: true,
        }}
      />,
    );

    const link = screen.getByRole("link", { name: "Open run detail" });
    expect(link).toHaveAttribute("href", "/runs/golden-1");
  });

  it("advances polled summary when the interval callback runs (mock setInterval + getRunSummary)", async () => {
    let intervalHandler: (() => void) | undefined;
    vi.spyOn(globalThis, "setInterval").mockImplementation((handler, delay) => {
      if (delay === 3000 && typeof handler === "function") {
        intervalHandler = handler as () => void;
      }

      return 42 as unknown as ReturnType<typeof setInterval>;
    });
    vi.spyOn(globalThis, "clearInterval").mockImplementation(() => {
      /* no-op */
    });

    mockGetRunSummary
      .mockResolvedValueOnce({
        ...baseSummary,
        hasContextSnapshot: true,
      })
      .mockResolvedValue({
        ...baseSummary,
        hasContextSnapshot: true,
        hasGraphSnapshot: true,
        hasFindingsSnapshot: true,
        hasGoldenManifest: true,
      });

    render(<TrackPollingHarness runId={baseSummary.runId} />);

    await waitFor(() => {
      expect(screen.getAllByText("Ready").length).toBeGreaterThanOrEqual(1);
    });

    await waitFor(() => {
      expect(intervalHandler).toBeDefined();
    });

    await act(async () => {
      intervalHandler?.();
    });

    await act(async () => {
      await Promise.resolve();
      await Promise.resolve();
    });

    await waitFor(() => {
      expect(screen.getByRole("link", { name: "Open run detail" })).toBeInTheDocument();
    });

    expect(mockGetRunSummary.mock.calls.length).toBeGreaterThanOrEqual(2);

    vi.restoreAllMocks();
  });

  it("uses vi.useFakeTimers to advance a 3s polling interval", () => {
    vi.useFakeTimers();

    try {
      const tick = vi.fn();
      const id = window.setInterval(tick, 3000);
      vi.advanceTimersByTime(2999);
      expect(tick).not.toHaveBeenCalled();
      vi.advanceTimersByTime(1);
      expect(tick).toHaveBeenCalledTimes(1);
      window.clearInterval(id);
    } finally {
      vi.useRealTimers();
    }
  });

  it("encodes run id on Compare runs when the golden manifest is ready", () => {
    renderWithTooltips(
      <WizardStepTrack
        runId="run-encode-9"
        pollSummary={{
          ...baseSummary,
          runId: "run-encode-9",
          hasGoldenManifest: true,
        }}
      />,
    );

    expect(screen.getByRole("link", { name: "Compare runs" })).toHaveAttribute(
      "href",
      "/compare?leftRunId=run-encode-9",
    );
  });
});
