import { render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { RunWizardCostPreviewCard } from "./RunWizardCostPreviewCard";

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
        json: async () => ({
          mode: "Simulator",
          maxCompletionTokens: 4096,
          estimatedCostUsd: null,
          deploymentName: null,
        }),
      })),
    );

    const { container } = render(<RunWizardCostPreviewCard previewUrl="/api/proxy/v1/agent-execution/cost-preview" />);

    await waitFor(() => {
      expect(screen.queryByTestId("run-cost-preview-loading")).not.toBeInTheDocument();
    });

    expect(container.querySelector('[data-testid="run-cost-preview-card"]')).toBeNull();
  });

  it("shows dollar estimate and max tokens when mode is Real", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => ({
        ok: true,
        json: async () => ({
          mode: "Real",
          maxCompletionTokens: 1024,
          estimatedCostUsd: 0.42,
          deploymentName: "gpt-test",
        }),
      })),
    );

    render(<RunWizardCostPreviewCard previewUrl="/api/proxy/v1/agent-execution/cost-preview" />);

    await waitFor(() => {
      expect(screen.getByTestId("run-cost-preview-card")).toBeInTheDocument();
    });

    expect(screen.getByTestId("run-cost-preview-amount")).toHaveTextContent("$0.42");
    expect(screen.getByTestId("run-cost-preview-headline")).toHaveTextContent("MaxCompletionTokens");
    expect(screen.getByTestId("run-cost-preview-headline")).toHaveTextContent("=1024");
    expect(screen.getByTestId("run-cost-preview-headline")).toHaveTextContent("gpt-test");
    expect(screen.getByRole("link", { name: /PER_TENANT_COST_MODEL/i })).toHaveAttribute(
      "href",
      expect.stringContaining("PER_TENANT_COST_MODEL.md"),
    );
  });
});
