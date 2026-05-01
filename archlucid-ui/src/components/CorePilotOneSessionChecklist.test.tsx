import { render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

vi.mock("@/lib/core-pilot-commit-context", () => ({
  fetchCorePilotCommitContext: vi.fn(),
}));

vi.mock("@/components/HelpLink", () => ({
  HelpLink: () => <span data-testid="help-link-mock" />,
}));

vi.mock("next/link", () => ({
  default: ({
    href,
    children,
    ...rest
  }: {
    href: string;
    children: import("react").ReactNode;
  } & Record<string, unknown>) => (
    <a href={href} {...rest}>
      {children}
    </a>
  ),
}));

import { fetchCorePilotCommitContext } from "@/lib/core-pilot-commit-context";

import { CorePilotOneSessionChecklist } from "./CorePilotOneSessionChecklist";

const fetchCtx = vi.mocked(fetchCorePilotCommitContext);

describe("CorePilotOneSessionChecklist", () => {
  it("renders four Core Pilot steps with deep links after context resolves", async () => {
    fetchCtx.mockResolvedValue({
      hasCommittedManifest: false,
      latestRunId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
      firstCommittedRunId: null,
    });

    render(<CorePilotOneSessionChecklist />);

    expect(screen.getByTestId("core-pilot-one-session-checklist")).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.queryByText("Resolving checklist links…")).not.toBeInTheDocument();
    });

    expect(screen.getByRole("heading", { name: "First architecture review in one session" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "New request" })).toHaveAttribute("href", "/reviews/new");
    expect(screen.getByRole("link", { name: "Reviews list" })).toHaveAttribute("href", "/reviews?projectId=default");
    expect(screen.getByRole("link", { name: "Open review detail to commit" })).toHaveAttribute(
      "href",
      "/reviews/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
    );
    expect(screen.getByRole("link", { name: "Open a review after commit" })).toHaveAttribute(
      "href",
      "/reviews?projectId=default",
    );
  });

  it("links review step to the first committed run when available", async () => {
    fetchCtx.mockResolvedValue({
      hasCommittedManifest: true,
      latestRunId: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
      firstCommittedRunId: "11111111-2222-3333-4444-555555555555",
    });

    render(<CorePilotOneSessionChecklist />);

    await waitFor(() => {
      expect(screen.queryByText("Resolving checklist links…")).not.toBeInTheDocument();
    });

    expect(screen.getByRole("link", { name: "Open committed review" })).toHaveAttribute(
      "href",
      "/reviews/11111111-2222-3333-4444-555555555555",
    );
  });
});
