import { render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("@/lib/core-pilot-commit-context", () => ({
  fetchCorePilotCommitContext: vi.fn(),
}));

import { CorePilotNextStepsCard } from "@/components/CorePilotNextStepsCard";
import { fetchCorePilotCommitContext } from "@/lib/core-pilot-commit-context";

const mockedFetchCorePilotCommitContext = vi.mocked(fetchCorePilotCommitContext);

describe("CorePilotNextStepsCard", () => {
  afterEach(() => {
    vi.clearAllMocks();
  });

  beforeEach(() => {
    mockedFetchCorePilotCommitContext.mockResolvedValue({
      hasCommittedManifest: false,
      latestRunId: null,
      firstCommittedRunId: null,
    });
  });

  it("shows pilot-only steps without operate hrefs when no committed manifest", async () => {
    render(<CorePilotNextStepsCard />);

    await waitFor(() => {
      expect(screen.getByTestId("core-pilot-next-steps")).toBeInTheDocument();
    });

    expect(screen.getByRole("link", { name: /create architecture request/i })).toHaveAttribute("href", "/reviews/new");
    expect(screen.getByRole("link", { name: /open reviews/i })).toHaveAttribute(
      "href",
      "/reviews?projectId=default",
    );
    expect(screen.queryByRole("link", { name: /ask/i })).not.toBeInTheDocument();
  });

  it("collapses to optional Operate CTA when commit exists", async () => {
    mockedFetchCorePilotCommitContext.mockResolvedValueOnce({
      hasCommittedManifest: true,
      latestRunId: "r1",
      firstCommittedRunId: "r1",
    });

    render(<CorePilotNextStepsCard />);

    await waitFor(() => {
      expect(screen.getByTestId("core-pilot-next-steps-complete")).toBeInTheDocument();
    });

    expect(screen.getByRole("link", { name: /open ask \(operate\)/i })).toHaveAttribute("href", "/ask");
  });
});
