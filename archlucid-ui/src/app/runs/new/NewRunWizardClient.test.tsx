import { act, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

const { createArchitectureRunMock, getRunSummaryMock } = vi.hoisted(() => ({
  createArchitectureRunMock: vi.fn(),
  getRunSummaryMock: vi.fn(),
}));

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
  createArchitectureRun: (...args: unknown[]) => createArchitectureRunMock(...args),
  getRunSummary: (...args: unknown[]) => getRunSummaryMock(...args),
}));

import { NewRunWizardClient } from "./NewRunWizardClient";

function progressLine(): HTMLElement {
  return screen.getByTestId("new-run-wizard-step-line");
}

async function clickNextAndSettle() {
  await act(async () => {
    fireEvent.click(screen.getByRole("button", { name: "Next" }));
  });
}

describe("NewRunWizardClient", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    createArchitectureRunMock.mockResolvedValue({ run: { runId: "integration-run-1" } });
    getRunSummaryMock.mockResolvedValue({
      runId: "integration-run-1",
      projectId: "default",
      createdUtc: "2026-01-01T00:00:00.000Z",
      hasContextSnapshot: true,
      hasGraphSnapshot: true,
      hasFindingsSnapshot: true,
      hasGoldenManifest: true,
    });
  });

  it(
    "walks preset → review, creates a run, lands on pipeline tracking with polling",
    async () => {
    render(<NewRunWizardClient />);

    expect(progressLine()).toHaveTextContent(/Step 1 of 7/);

    const greenfieldCard = screen.getByText("Greenfield web app").closest('[class*="rounded-xl"]');
    expect(greenfieldCard).toBeTruthy();
    fireEvent.click(within(greenfieldCard as HTMLElement).getByRole("button", { name: "Select" }));

    await clickNextAndSettle();
    expect(progressLine()).toHaveTextContent(/Step 2 of 7/);

    for (let i = 0; i < 4; i += 1) {
      await clickNextAndSettle();
    }

    expect(progressLine()).toHaveTextContent(/Step 6 of 7/);
    expect(screen.getByRole("heading", { name: "Review & submit" })).toBeInTheDocument();

    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: "Create run" }));
    });

    await waitFor(() => {
      expect(createArchitectureRunMock).toHaveBeenCalled();
    });

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "Track pipeline" })).toBeInTheDocument();
    });

    await waitFor(() => {
      expect(getRunSummaryMock).toHaveBeenCalledWith("integration-run-1");
    });
    },
    30_000,
  );

  it("navigates backward when Back is pressed", async () => {
    render(<NewRunWizardClient />);

    await clickNextAndSettle();
    expect(progressLine()).toHaveTextContent(/Step 2 of 7/);

    await clickNextAndSettle();
    expect(progressLine()).toHaveTextContent(/Step 3 of 7/);

    fireEvent.click(screen.getByRole("button", { name: "Back" }));
    expect(progressLine()).toHaveTextContent(/Step 2 of 7/);
  });

  it("disables Next when the current step has blocking validation errors", async () => {
    render(<NewRunWizardClient />);

    fireEvent.click(screen.getByRole("button", { name: "Next" }));

    const systemName = screen.getByLabelText("System name");
    fireEvent.change(systemName, { target: { value: "" } });
    fireEvent.blur(systemName);

    await waitFor(() => {
      expect(screen.getByRole("button", { name: "Next" })).toBeDisabled();
    });
  });
});
