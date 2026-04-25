import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("@/lib/api", () => ({
  downloadFirstValueReportPdf: vi.fn(),
}));

vi.mock("@/lib/sponsor-banner-telemetry", () => ({
  recordSponsorBannerFirstCommitBadge: vi.fn(),
}));

import { downloadFirstValueReportPdf } from "@/lib/api";
import { recordSponsorBannerFirstCommitBadge } from "@/lib/sponsor-banner-telemetry";

import { EmailRunToSponsorBanner } from "./EmailRunToSponsorBanner";

const mockDownload = vi.mocked(downloadFirstValueReportPdf);
const mockTelemetry = vi.mocked(recordSponsorBannerFirstCommitBadge);

describe("EmailRunToSponsorBanner", () => {
  beforeEach(() => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({ firstCommitUtc: null }),
      } as Response),
    );
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.unstubAllGlobals();
    vi.clearAllMocks();
  });

  it("renders the time-to-value heading and primary CTA copy", async () => {
    render(<EmailRunToSponsorBanner runId="run-42" />);

    expect(screen.getByTestId("email-run-to-sponsor-banner")).toBeInTheDocument();
    expect(screen.getByText(/time to value/i)).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /email this run to your sponsor/i }),
    ).toBeInTheDocument();

    await waitFor(() => {
      expect(vi.mocked(fetch)).toHaveBeenCalled();
    });
  });

  it("invokes downloadFirstValueReportPdf with the run id when the primary action is clicked", async () => {
    mockDownload.mockResolvedValue(undefined);

    render(<EmailRunToSponsorBanner runId="run-42" />);

    fireEvent.click(screen.getByTestId("email-run-to-sponsor-primary-action"));

    await waitFor(() => {
      expect(mockDownload).toHaveBeenCalledWith("run-42");
    });
  });

  it("renders the API problem callout when the download throws a generic error", async () => {
    mockDownload.mockRejectedValueOnce(new Error("boom — server unavailable"));

    render(<EmailRunToSponsorBanner runId="run-42" />);

    fireEvent.click(screen.getByTestId("email-run-to-sponsor-primary-action"));

    await waitFor(() => {
      expect(screen.getByText(/boom — server unavailable/i)).toBeInTheDocument();
    });
  });

  it("disables the button while busy", async () => {
    let resolve: (() => void) | null = null;
    mockDownload.mockReturnValueOnce(
      new Promise<void>((r) => {
        resolve = r;
      }),
    );

    render(<EmailRunToSponsorBanner runId="run-42" />);

    fireEvent.click(screen.getByTestId("email-run-to-sponsor-primary-action"));

    const button = screen.getByTestId("email-run-to-sponsor-primary-action") as HTMLButtonElement;

    expect(button).toBeDisabled();
    expect(button).toHaveTextContent(/preparing pdf/i);

    resolve?.();
    await waitFor(() => {
      expect(screen.getByTestId("email-run-to-sponsor-primary-action")).not.toBeDisabled();
    });
  });

  it("renders Day 0 when first commit is within the first UTC day (fake timers)", async () => {
    vi.useFakeTimers({ toFake: ["Date"] });
    vi.setSystemTime(new Date("2026-03-10T14:00:00.000Z"));
    const anchorIso = new Date("2026-03-10T12:00:00.000Z").toISOString();
    vi.mocked(fetch).mockResolvedValueOnce({
      ok: true,
      json: async () => ({ firstCommitUtc: anchorIso }),
    } as Response);

    render(<EmailRunToSponsorBanner runId="run-42" />);

    await waitFor(() => {
      expect(screen.getByTestId("email-run-to-sponsor-first-commit-badge")).toHaveTextContent(
        "Day 0 since first commit",
      );
    });

    expect(mockTelemetry).toHaveBeenCalledWith(0);
  });

  it("renders Day 1 once 24h elapsed since first commit (fake timers)", async () => {
    vi.useFakeTimers({ toFake: ["Date"] });
    vi.setSystemTime(new Date("2026-03-11T12:00:01.000Z"));
    const anchorIso = new Date("2026-03-10T12:00:00.000Z").toISOString();
    vi.mocked(fetch).mockResolvedValueOnce({
      ok: true,
      json: async () => ({ firstCommitUtc: anchorIso }),
    } as Response);

    render(<EmailRunToSponsorBanner runId="run-42" />);

    await waitFor(() => {
      expect(screen.getByTestId("email-run-to-sponsor-first-commit-badge")).toHaveTextContent(
        "Day 1 since first commit",
      );
    });

    expect(mockTelemetry).toHaveBeenCalledWith(1);
  });

  it("renders Day 4 badge when firstCommitUtc is four and a half UTC-day periods earlier", async () => {
    const nowMs = Date.now();
    const anchorIso = new Date(nowMs - 4.5 * 24 * 60 * 60 * 1000).toISOString();
    vi.mocked(fetch).mockResolvedValueOnce({
      ok: true,
      json: async () => ({ firstCommitUtc: anchorIso }),
    } as Response);

    render(<EmailRunToSponsorBanner runId="run-42" />);

    await waitFor(() => {
      expect(screen.getByTestId("email-run-to-sponsor-first-commit-badge")).toHaveTextContent(
        "Day 4 since first commit",
      );
    });

    expect(mockTelemetry).toHaveBeenCalledWith(4);
  });

  it("hides the badge when firstCommitUtc is null", async () => {
    vi.mocked(fetch).mockResolvedValueOnce({
      ok: true,
      json: async () => ({ firstCommitUtc: null }),
    } as Response);

    render(<EmailRunToSponsorBanner runId="run-42" />);

    await waitFor(() => {
      expect(vi.mocked(fetch)).toHaveBeenCalled();
    });

    expect(screen.queryByTestId("email-run-to-sponsor-first-commit-badge")).toBeNull();
    expect(mockTelemetry).not.toHaveBeenCalled();
  });

  it("hides the badge when trial-status returns 5xx", async () => {
    vi.mocked(fetch).mockResolvedValueOnce({
      ok: false,
      status: 503,
      json: async () => ({}),
    } as Response);

    render(<EmailRunToSponsorBanner runId="run-42" />);

    await waitFor(() => {
      expect(vi.mocked(fetch)).toHaveBeenCalled();
    });

    expect(screen.queryByTestId("email-run-to-sponsor-first-commit-badge")).toBeNull();
    expect(mockTelemetry).not.toHaveBeenCalled();
  });
});
