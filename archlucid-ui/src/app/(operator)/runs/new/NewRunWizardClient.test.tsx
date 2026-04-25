import { act, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

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

vi.mock("next/navigation", () => ({
  useSearchParams: () => new URLSearchParams(),
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
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;

        if (url.includes("/v1/agent-execution/cost-preview")) {
          return {
            ok: true,
            json: async () => ({
              mode: "Simulator",
              maxCompletionTokens: 4096,
              estimatedCostUsd: null,
              deploymentName: null,
            }),
          };
        }

        return { ok: false, status: 404, json: async () => ({}) };
      }),
    );
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

  it("blocks Next and shows an inline system name error when required field is empty", async () => {
    render(<NewRunWizardClient />);

    const greenfieldCard = screen.getByText("Greenfield web app").closest('[class*="rounded-xl"]');
    expect(greenfieldCard).toBeTruthy();
    fireEvent.click(within(greenfieldCard as HTMLElement).getByRole("button", { name: "Select" }));

    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: "Next" }));
    });
    expect(progressLine()).toHaveTextContent(/Step 2 of 7/);

    const systemName = screen.getByLabelText("System name");
    fireEvent.change(systemName, { target: { value: "" } });

    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: "Next" }));
    });

    expect(progressLine()).toHaveTextContent(/Step 2 of 7/);
    const alert = await screen.findByRole("alert");
    expect(alert).toHaveTextContent(/System name is required/i);
  });

  it("clears the system name error when the user types", async () => {
    render(<NewRunWizardClient />);

    const greenfieldCard = screen.getByText("Greenfield web app").closest('[class*="rounded-xl"]');
    fireEvent.click(within(greenfieldCard as HTMLElement).getByRole("button", { name: "Select" }));

    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: "Next" }));
    });

    const systemName = screen.getByLabelText("System name");
    fireEvent.change(systemName, { target: { value: "" } });

    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: "Next" }));
    });
    expect(await screen.findByRole("alert")).toBeInTheDocument();

    fireEvent.change(systemName, { target: { value: "X" } });
    await waitFor(() => {
      expect(screen.queryByText(/System name is required/i)).toBeNull();
    });
  });

  it("advances from identity when fields satisfy validation", async () => {
    render(<NewRunWizardClient />);

    const greenfieldCard = screen.getByText("Greenfield web app").closest('[class*="rounded-xl"]');
    fireEvent.click(within(greenfieldCard as HTMLElement).getByRole("button", { name: "Select" }));

    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: "Next" }));
    });
    expect(progressLine()).toHaveTextContent(/Step 2 of 7/);

    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: "Next" }));
    });
    expect(progressLine()).toHaveTextContent(/Step 3 of 7/);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });
});
