import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

import { LegacyRunComparisonView } from "@/components/compare/LegacyRunComparisonView";
import { StructuredComparisonView } from "@/components/compare/StructuredComparisonView";
import type { GoldenManifestComparison } from "@/types/comparison";
import type { RunComparison } from "@/types/authority";

vi.mock("@/lib/api", () => ({
  getArchitecturePackageDocxUrl: vi.fn(
    () => "/api/proxy/api/export/architecture-package?base=a&target=b",
  ),
}));

const emptyGolden: GoldenManifestComparison = {
  baseRunId: "run-base",
  targetRunId: "run-target",
  decisionChanges: [],
  requirementChanges: [],
  securityChanges: [],
  topologyChanges: [],
  costChanges: [],
  summaryHighlights: [],
  totalDeltaCount: 0,
};

describe("Compare / review views (55R smoke)", () => {
  it("StructuredComparisonView shows run IDs and empty-section notes when there is no delta data", () => {
    render(<StructuredComparisonView golden={emptyGolden} />);

    expect(screen.getByText("Manifest comparison")).toBeInTheDocument();
    expect(screen.getByText("run-base")).toBeInTheDocument();
    expect(screen.getByText("run-target")).toBeInTheDocument();
    expect(screen.getByText("No summary highlights")).toBeInTheDocument();
    expect(screen.getByText("No decision changes")).toBeInTheDocument();
  });

  it("StructuredComparisonView renders decision rows when comparison data exists", () => {
    const golden: GoldenManifestComparison = {
      ...emptyGolden,
      decisionChanges: [
        {
          decisionKey: "deploy-region",
          baseValue: "east",
          targetValue: "west",
          changeType: "Modified",
        },
      ],
      summaryHighlights: ["Region changed"],
    };

    render(<StructuredComparisonView golden={golden} />);

    // Decision column shows `decisionKeyDisplay(key)` and the raw key below (duplicate text when they match).
    const decisionKeyNodes = screen.getAllByText("deploy-region");
    expect(decisionKeyNodes).toHaveLength(2);
    expect(screen.getByText("Modified")).toBeInTheDocument();
    expect(screen.getByText("Region changed")).toBeInTheDocument();
  });

  it("LegacyRunComparisonView shows empty run-level diff message when there are no diffs", () => {
    const result: RunComparison = {
      leftRunId: "L",
      rightRunId: "R",
      runLevelDiffs: [],
    };

    render(<LegacyRunComparisonView result={result} />);

    expect(screen.getByRole("heading", { name: "Run-level diff", level: 3 })).toBeInTheDocument();
    expect(screen.getByText("No run-level diffs")).toBeInTheDocument();
  });

  it("LegacyRunComparisonView renders flat diffs when data exists", () => {
    const result: RunComparison = {
      leftRunId: "L",
      rightRunId: "R",
      runLevelDiffs: [
        {
          section: "Meta",
          key: "status",
          diffKind: "Changed",
          beforeValue: "a",
          afterValue: "b",
        },
      ],
    };

    render(<LegacyRunComparisonView result={result} />);

    expect(screen.getByText("Meta")).toBeInTheDocument();
    expect(screen.getByText("status")).toBeInTheDocument();
    expect(screen.getByText("Changed")).toBeInTheDocument();
  });
});
