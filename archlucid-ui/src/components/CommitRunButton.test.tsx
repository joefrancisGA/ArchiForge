import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

vi.mock("next/navigation", () => ({
  useRouter: (): { refresh: () => void } => ({
    refresh: vi.fn(),
  }),
}));

vi.mock("@/lib/api", () => ({
  commitArchitectureRun: vi.fn(),
}));

import { commitArchitectureRun } from "@/lib/api";

import { CommitRunButton } from "./CommitRunButton";

const mockCommit = vi.mocked(commitArchitectureRun);

describe("CommitRunButton", () => {
  it("renders disabled message when already finalized", () => {
    render(<CommitRunButton runId="abc" disabled />);

    expect(screen.getByText(/already finalized/i)).toBeInTheDocument();
  });

  it("opens confirm dialog and calls commit on confirm", async () => {
    mockCommit.mockResolvedValue({});

    render(<CommitRunButton runId="run-1" disabled={false} />);

    fireEvent.click(screen.getByRole("button", { name: /^finalize manifest$/i }));

    const dialog = await screen.findByRole("alertdialog");

    fireEvent.click(within(dialog).getByRole("button", { name: /^finalize manifest$/i }));

    await waitFor(() => {
      expect(mockCommit).toHaveBeenCalledWith("run-1", { notifySponsor: false });
    });
  });

  it("passes notifySponsor when the email checkbox is checked", async () => {
    mockCommit.mockResolvedValue({});

    render(<CommitRunButton runId="run-2" disabled={false} />);

    fireEvent.click(screen.getByRole("button", { name: /^finalize manifest$/i }));

    const dialog = await screen.findByRole("alertdialog");

    fireEvent.click(within(dialog).getByRole("checkbox", { name: /email tenant admin contact/i }));

    fireEvent.click(within(dialog).getByRole("button", { name: /^finalize manifest$/i }));

    await waitFor(() => {
      expect(mockCommit).toHaveBeenCalledWith("run-2", { notifySponsor: true });
    });
  });
});
