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
  listRunsByProjectPaged: vi.fn().mockResolvedValue({
    items: [
      {
        runId: "prior-run",
        projectId: "default",
        createdUtc: "2026-01-01T00:00:00.000Z",
        hasGoldenManifest: true,
      },
    ],
    totalCount: 1,
    page: 1,
    pageSize: 50,
    hasMore: false,
  }),
}));

import { NewRunWizardClient } from "./NewRunWizardClient";

const WIZARD_MODE_STORAGE_KEY = "archlucid_new_run_wizard_mode_v1";
const WIZARD_DRAFT_STORAGE_KEY = "archlucid_new_run_wizard_draft_v1";

function greenfieldPresetCard(): HTMLElement {
  const useGreenfield = screen.getByRole("button", { name: "Use greenfield web app" });

  return useGreenfield.closest('[class*="rounded-xl"]') as HTMLElement;
}

function progressLine(): HTMLElement {
  return screen.getByTestId("new-run-wizard-step-line");
}

async function renderNewRunWizard() {
  render(<NewRunWizardClient />);

  await waitFor(
    () => {
      expect(screen.queryByText("Loading wizard…")).not.toBeInTheDocument();
    },
    { timeout: 15_000 },
  );

  await act(async () => {
    fireEvent.click(screen.getByRole("button", { name: "Full Wizard (7 steps)" }));
  });

  await waitFor(
    () => {
      expect(screen.getByTestId("new-run-wizard-progress")).toBeInTheDocument();
    },
    { timeout: 15_000 },
  );

  await waitFor(
    () => {
      expect(screen.getByTestId("new-run-wizard-step-line")).toHaveTextContent(/Step 1: Choose starting point/);
    },
    { timeout: 15_000 },
  );
}

async function clickPrimaryForward() {
  await act(async () => {
    fireEvent.click(screen.getByRole("button", { name: /^(Continue|Next)$/ }));
  });
}

describe("NewRunWizardClient", { timeout: 60_000 }, () => {
  beforeEach(() => {
    vi.clearAllMocks();
    window.localStorage.removeItem(WIZARD_MODE_STORAGE_KEY);
    window.localStorage.removeItem(WIZARD_DRAFT_STORAGE_KEY);
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
    await renderNewRunWizard();

    expect(progressLine()).toHaveTextContent(/Step 1: Choose starting point/);
    expect(screen.getByTestId("new-run-wizard-stage-line")).toHaveTextContent(/Stage 1 of 3 — Request brief/);

    const greenfieldCard = greenfieldPresetCard();
    fireEvent.click(within(greenfieldCard).getByRole("button", { name: "Use greenfield web app" }));

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
    await renderNewRunWizard();

    await clickPrimaryForward();
    expect(progressLine()).toHaveTextContent(/Step 2:/);

    await clickPrimaryForward();
    expect(progressLine()).toHaveTextContent(/Step 3:/);

    fireEvent.click(screen.getByRole("button", { name: "Back" }));
    expect(progressLine()).toHaveTextContent(/Step 2:/);
  });

  it("blocks Next and shows an inline system name error when required field is empty", async () => {
    await renderNewRunWizard();

    const greenfieldCard = greenfieldPresetCard();
    fireEvent.click(within(greenfieldCard).getByRole("button", { name: "Use greenfield web app" }));

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
    await renderNewRunWizard();

    const greenfieldCard = greenfieldPresetCard();
    fireEvent.click(within(greenfieldCard).getByRole("button", { name: "Use greenfield web app" }));

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
    await renderNewRunWizard();

    const greenfieldCard = greenfieldPresetCard();
    fireEvent.click(within(greenfieldCard).getByRole("button", { name: "Use greenfield web app" }));

    expect(progressLine()).toHaveTextContent(/Step 2:/);

    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: "Next" }));
    });
    expect(progressLine()).toHaveTextContent(/Step 3:/);
  });

  it("blocks Next on identity when prior manifest version is not a valid UUID", async () => {
    await renderNewRunWizard();

    const greenfieldCard = greenfieldPresetCard();
    fireEvent.click(within(greenfieldCard).getByRole("button", { name: "Use greenfield web app" }));

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
    await renderNewRunWizard();

    const greenfieldCard = greenfieldPresetCard();
    fireEvent.click(within(greenfieldCard).getByRole("button", { name: "Use greenfield web app" }));

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
