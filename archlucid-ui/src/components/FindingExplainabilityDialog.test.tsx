import { render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

import { FindingExplainabilityDialog } from "@/components/FindingExplainabilityDialog";
import * as api from "@/lib/api";
import type { FindingExplainability } from "@/types/explanation";

describe("FindingExplainabilityDialog", () => {
  it("loads explainability when opened with a finding id", async () => {
    const sample: FindingExplainability = {
      findingId: "f-1",
      title: "Test finding",
      engineType: "Policy",
      severity: "High",
      traceCompletenessRatio: 0.75,
      graphNodeIdsExamined: ["n1"],
      rulesApplied: ["r1"],
      decisionsTaken: ["d1"],
      alternativePathsConsidered: [],
      notes: [],
      narrativeText: "Narrative body.",
    };

    const spy = vi.spyOn(api, "getFindingExplainability").mockResolvedValue(sample);

    render(
      <FindingExplainabilityDialog open={true} onOpenChange={() => {}} runId="run-a" findingId="f-1" />,
    );

    await waitFor(() => {
      expect(screen.getByText("Test finding")).toBeInTheDocument();
    });

    expect(screen.getByText("Narrative body.")).toBeInTheDocument();
    expect(spy).toHaveBeenCalledWith("run-a", "f-1");
    spy.mockRestore();
  });

  it("shows API problem when fetch fails", async () => {
    vi.spyOn(api, "getFindingExplainability").mockRejectedValue(new Error("boom"));

    render(
      <FindingExplainabilityDialog open={true} onOpenChange={() => {}} runId="run-a" findingId="f-1" />,
    );

    await waitFor(() => {
      expect(screen.getByText(/boom/i)).toBeInTheDocument();
    });

    vi.restoreAllMocks();
  });

});
