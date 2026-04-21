import { render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

vi.mock("@/lib/api", () => ({
  getWhyArchLucidSnapshot: vi.fn(),
  getFirstValueReportMarkdown: vi.fn(),
  getRunExplanationSummary: vi.fn(),
}));

vi.mock("@/components/OperatorApiProblem", () => ({
  OperatorApiProblem: ({ fallbackMessage }: { fallbackMessage: string }) => (
    <div data-testid="api-problem-mock">{fallbackMessage}</div>
  ),
}));

import {
  getFirstValueReportMarkdown,
  getRunExplanationSummary,
  getWhyArchLucidSnapshot,
} from "@/lib/api";

import WhyArchLucidPage from "./page";

const snapshotMock = vi.mocked(getWhyArchLucidSnapshot);
const reportMock = vi.mocked(getFirstValueReportMarkdown);
const explanationMock = vi.mocked(getRunExplanationSummary);

const fixedSnapshot = {
  generatedUtc: "2026-04-20T12:00:00.000Z",
  demoRunId: "6e8c4a102b1f4c9a9d3e10b2a4f0c501",
  runsCreatedTotal: 7,
  findingsProducedBySeverity: { Critical: 1, High: 2, Medium: 3 },
  auditRowCount: 12,
  auditRowCountTruncated: false,
};

const fixedReport = `# ArchLucid — first value report (pilot)\n\nDemo body.`;

const fixedExplanation = {
  explanation: {
    rawText: "raw",
    structured: null,
    confidence: null,
    provenance: null,
    summary: "Summary",
    keyDrivers: [],
    riskImplications: [],
    costImplications: [],
    complianceImplications: [],
    detailedNarrative: "Narrative.",
  },
  themeSummaries: ["Theme A", "Theme B"],
  overallAssessment: "Healthy baseline with two open mediums.",
  riskPosture: "Moderate",
  findingCount: 6,
  decisionCount: 4,
  unresolvedIssueCount: 1,
  complianceGapCount: 0,
  citations: [
    { kind: "Manifest" as const, id: "m-1", label: "contoso-baseline-v1" },
    { kind: "Finding" as const, id: "f-1", label: "Public storage" },
  ],
};

describe("WhyArchLucidPage (proof page snapshot)", () => {
  it("matches the rendered layout snapshot for the demo tenant", async () => {
    snapshotMock.mockResolvedValue(fixedSnapshot);
    reportMock.mockResolvedValue(fixedReport);
    explanationMock.mockResolvedValue(fixedExplanation);

    const { container } = render(<WhyArchLucidPage />);

    await waitFor(() => {
      expect(screen.getByTestId("why-archlucid-counters")).toBeInTheDocument();
      expect(screen.getByTestId("why-archlucid-first-value-report-body")).toHaveTextContent(
        "ArchLucid — first value report",
      );
      expect(screen.getByTestId("why-archlucid-citations")).toHaveTextContent("Manifest");
    });

    expect(container.firstChild).toMatchSnapshot();
  });

  it("shows API-problem callouts when downstream calls fail", async () => {
    snapshotMock.mockRejectedValue(new Error("snapshot failed"));
    reportMock.mockRejectedValue(new Error("report failed"));
    explanationMock.mockRejectedValue(new Error("explain failed"));

    render(<WhyArchLucidPage />);

    await waitFor(() => {
      const problems = screen.getAllByTestId("api-problem-mock");
      expect(problems.some((p) => p.textContent?.includes("snapshot failed"))).toBe(true);
    });
  });
});
