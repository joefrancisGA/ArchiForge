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

async function clickPrimaryForward() {
  await act(async () => {
    fireEvent.click(screen.getByRole("button", { name: /^(Continue|Next)$/ }));
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

    expect(progressLine()).toHaveTextContent(/Step 1: Choose starting point/);
    expect(screen.getByTestId("new-run-wizard-stage-line")).toHaveTextContent(/Stage 1 of 3 — Request brief/);

    const greenfieldCard = screen.getByText("Greenfield web app").closest('[class*="rounded-xl"]');
    expect(greenfieldCard).toBeTruthy();
    fireEvent.click(within(greenfieldCard as HTMLElement).getByRole("button", { name: "Use greenfield web app" }));

    await clickPrimaryForward();
    expect(progressLine()).toHaveTextContent(/Step 2:/);

    for (let i = 0; i < 4; i += 1) {
      await clickPrimaryForward();
    }

    expect(progressLine()).toHaveTextContent(/Step 6:/);
    expect(screen.getByRole("heading", { name: "Review & submit" })).toBeInTheDocument();

    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: "Create request" }));
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

    await clickPrimaryForward();
    expect(progressLine()).toHaveTextContent(/Step 2:/);

    await clickPrimaryForward();
    expect(progressLine()).toHaveTextContent(/Step 3:/);

    fireEvent.click(screen.getByRole("button", { name: "Back" }));
    expect(progressLine()).toHaveTextContent(/Step 2:/);
  });

  it("blocks Next and shows an inline system name error when required field is empty", async () => {
    render(<NewRunWizardClient />);

    const greenfieldCard = screen.getByText("Greenfield web app").closest('[class*="rounded-xl"]');
    expect(greenfieldCard).toBeTruthy();
    fireEvent.click(within(greenfieldCard as HTMLElement).getByRole("button", { name: "Use greenfield web app" }));

    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /^(Continue|Next)$/ }));
    });
    expect(progressLine()).toHaveTextContent(/Step 2:/);

    const systemName = screen.getByLabelText("System name");
    fireEvent.change(systemName, { target: { value: "" } });
    fireEvent.blur(systemName);

    expect(progressLine()).toHaveTextContent(/Step 2:/);
    expect(screen.getByRole("button", { name: "Next" })).toBeDisabled();
    const alert = await screen.findByRole("alert");
    expect(alert).toHaveTextContent(/Required/i);
  });

  it("clears the system name error when the user types", async () => {
    render(<NewRunWizardClient />);

    const greenfieldCard = screen.getByText("Greenfield web app").closest('[class*="rounded-xl"]');
    fireEvent.click(within(greenfieldCard as HTMLElement).getByRole("button", { name: "Use greenfield web app" }));

    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /^(Continue|Next)$/ }));
    });

    const systemName = screen.getByLabelText("System name");
    fireEvent.change(systemName, { target: { value: "" } });
    fireEvent.blur(systemName);
    expect(await screen.findByRole("alert")).toBeInTheDocument();

    fireEvent.change(systemName, { target: { value: "Ab" } });
    await waitFor(() => {
      expect(screen.queryByText(/Required/i)).toBeNull();
    });
  });

  it("advances from identity when fields satisfy validation", async () => {
    render(<NewRunWizardClient />);

    const greenfieldCard = screen.getByText("Greenfield web app").closest('[class*="rounded-xl"]');
    fireEvent.click(within(greenfieldCard as HTMLElement).getByRole("button", { name: "Use greenfield web app" }));

    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /^(Continue|Next)$/ }));
    });
    expect(progressLine()).toHaveTextContent(/Step 2:/);

    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: "Next" }));
    });
    expect(progressLine()).toHaveTextContent(/Step 3:/);
  });

  it("blocks Next on identity when prior manifest version is not a valid UUID", async () => {
    render(<NewRunWizardClient />);

    const greenfieldCard = screen.getByText("Greenfield web app").closest('[class*="rounded-xl"]');
    fireEvent.click(within(greenfieldCard as HTMLElement).getByRole("button", { name: "Use greenfield web app" }));

    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /^(Continue|Next)$/ }));
    });
    expect(progressLine()).toHaveTextContent(/Step 2:/);

    const prior = screen.getByLabelText("Prior manifest version (optional)");
    fireEvent.change(prior, {
      target: { value: "not-a-uuid" },
    });
    fireEvent.blur(prior);

    expect(progressLine()).toHaveTextContent(/Step 2:/);
    expect(screen.getByRole("button", { name: "Next" })).toBeDisabled();
    expect(await screen.findByRole("alert")).toHaveTextContent(/valid uuid/i);
  });

  it("blocks Next on description when narrative is shorter than the minimum length", async () => {
    render(<NewRunWizardClient />);

    const greenfieldCard = screen.getByText("Greenfield web app").closest('[class*="rounded-xl"]');
    fireEvent.click(within(greenfieldCard as HTMLElement).getByRole("button", { name: "Use greenfield web app" }));

    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /^(Continue|Next)$/ }));
    });
    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: "Next" }));
    });
    expect(progressLine()).toHaveTextContent(/Step 3:/);

    const description = screen.getByLabelText("Description");
    fireEvent.change(description, { target: { value: "short" } });
    fireEvent.blur(description);

    expect(progressLine()).toHaveTextContent(/Step 3:/);
    expect(screen.getByRole("button", { name: "Next" })).toBeDisabled();
    expect(await screen.findByRole("alert")).toHaveTextContent(/at least 10 characters/i);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });
});
