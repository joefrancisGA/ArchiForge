import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("next/navigation", () => ({
  useSearchParams: () => ({
    get: (key: string) => (key === "example" ? "claims-intake-modernization" : null),
  }),
}));

vi.mock("next/link", () => ({
  default: ({
    href,
    children,
    ...rest
  }: {
    href: string;
    children: import("react").ReactNode;
    className?: string;
  }) => (
    <a href={href} {...rest}>
      {children}
    </a>
  ),
}));

vi.mock("@/lib/api", () => ({
  createArchitectureRun: vi.fn(),
  getRunSummary: vi.fn(),
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

import {
  OPERATOR_HOME_EXAMPLE_DESCRIPTION,
  OPERATOR_HOME_EXAMPLE_SYSTEM_NAME,
} from "@/lib/operator-home-example-request";

const WIZARD_MODE_STORAGE_KEY = "archlucid_new_run_wizard_mode_v1";

describe("NewRunWizardClient (example query)", { timeout: 60_000 }, () => {
  beforeEach(() => {
    window.localStorage.setItem(WIZARD_MODE_STORAGE_KEY, "full");

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
              estimatedCostUsdLow: null,
              estimatedCostUsdHigh: null,
              estimatedCostBasis:
                "Starter run = 4 parallel agents (Topology, Cost, Compliance, Critic). Low = one completion at 8192 assumed input tokens.",
              pricingUsesIllustrativeUsdRates: true,
              deploymentName: null,
            }),
          };
        }

        return { ok: false, status: 404, json: async () => ({}) };
      }),
    );
  });

  it("prefills description and system name when example=claims-intake-modernization", async () => {
    render(<NewRunWizardClient />);

    await waitFor(() => {
      expect(screen.queryByText("Loading wizard…")).not.toBeInTheDocument();
    });

    await waitFor(
      () => {
        expect(screen.getByTestId("new-run-wizard-step-line")).toHaveTextContent(/Step 1: Choose starting point/);
      },
      { timeout: 15_000 },
    );

    const useGreenfield = screen.getByRole("button", { name: "Use greenfield web app" });
    const greenfieldCard = useGreenfield.closest('[class*="rounded-xl"]');
    expect(greenfieldCard).toBeTruthy();
    fireEvent.click(within(greenfieldCard as HTMLElement).getByRole("button", { name: "Use greenfield web app" }));

    expect((screen.getByLabelText("System name") as HTMLInputElement).value).toBe(OPERATOR_HOME_EXAMPLE_SYSTEM_NAME);

    expect((screen.getByLabelText("Description") as HTMLTextAreaElement).value).toBe(OPERATOR_HOME_EXAMPLE_DESCRIPTION);
  });

  afterEach(() => {
    window.localStorage.removeItem(WIZARD_MODE_STORAGE_KEY);
    vi.unstubAllGlobals();
  });
});
