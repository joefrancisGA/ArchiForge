import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

vi.mock("@/lib/api", () => ({
  downloadFirstValueReportPdf: vi.fn(),
}));

import { downloadFirstValueReportPdf } from "@/lib/api";

import { EmailRunToSponsorBanner } from "./EmailRunToSponsorBanner";

const mockDownload = vi.mocked(downloadFirstValueReportPdf);

describe("EmailRunToSponsorBanner", () => {
  it("renders the time-to-value heading and primary CTA copy", () => {
    render(<EmailRunToSponsorBanner runId="run-42" />);

    expect(screen.getByTestId("email-run-to-sponsor-banner")).toBeInTheDocument();
    expect(screen.getByText(/time to value/i)).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /email this run to your sponsor/i }),
    ).toBeInTheDocument();
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
});
