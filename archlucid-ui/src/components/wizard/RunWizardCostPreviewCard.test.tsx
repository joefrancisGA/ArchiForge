import { render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { RunWizardCostPreviewCard } from "./RunWizardCostPreviewCard";

const simulatorPayload = {
  mode: "Simulator",
  maxCompletionTokens: 4096,
  estimatedCostUsd: null,
  estimatedCostUsdLow: null,
  estimatedCostUsdHigh: null,
  estimatedCostBasis:
    "Starter run = 4 parallel agents (Topology, Cost, Compliance, Critic). Low = one completion at 8192 assumed input tokens.",
  pricingUsesIllustrativeUsdRates: true,
  deploymentName: null,
};

describe("RunWizardCostPreviewCard", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders nothing when host mode is Simulator", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => ({
        ok: true,
        json: async () => simulatorPayload,
      })),
    );

    const { container } = render(<RunWizardCostPreviewCard previewUrl="/api/proxy/v1/agent-execution/cost-preview" />);

    await waitFor(() => {
      expect(screen.queryByTestId("run-cost-preview-loading")).not.toBeInTheDocument();
    });

    expect(container.querySelector('[data-testid="run-cost-preview-card"]')).toBeNull();
  });

  it("shows USD band and max tokens when mode is Real", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => ({
        ok: true,
        json: async () => ({
          mode: "Real",
          maxCompletionTokens: 1024,
          estimatedCostUsd: 0.137216,
          estimatedCostUsdLow: 0.005632,
          estimatedCostUsdHigh: 0.137216,
          estimatedCostBasis: "Starter run = 4 parallel agents.",
          pricingUsesIllustrativeUsdRates: true,
          deploymentName: "gpt-test",
        }),
      })),
    );

    render(<RunWizardCostPreviewCard previewUrl="/api/proxy/v1/agent-execution/cost-preview" />);

    await waitFor(() => {
      expect(screen.getByTestId("run-cost-preview-card")).toBeInTheDocument();
    });

    expect(screen.getByTestId("run-cost-preview-amount")).toHaveTextContent("$0.01");
    expect(screen.getByTestId("run-cost-preview-amount")).toHaveTextContent("$0.14");
    expect(screen.getByTestId("run-cost-preview-headline")).toHaveTextContent("MaxCompletionTokens");
    expect(screen.getByTestId("run-cost-preview-headline")).toHaveTextContent("=1024");
    expect(screen.getByTestId("run-cost-preview-headline")).toHaveTextContent("gpt-test");
    expect(screen.getByText(/Illustrative USD rates are still set from defaults/i)).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /PER_TENANT_COST_MODEL/i })).toHaveAttribute(
      "href",
      expect.stringContaining("PER_TENANT_COST_MODEL.md"),
    );
  });
});
