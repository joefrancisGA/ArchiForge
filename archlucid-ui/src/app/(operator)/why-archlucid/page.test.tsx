import { render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

vi.mock("@/lib/api", () => ({
  getTenantMeasuredRoi: vi.fn(),
  getSponsorEvidencePack: vi.fn(),
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
  getSponsorEvidencePack,
  getTenantMeasuredRoi,
  type SponsorEvidencePackPayload,
} from "@/lib/api";

import WhyArchLucidPage from "./page";

const measuredRoiMock = vi.mocked(getTenantMeasuredRoi);
const sponsorPackMock = vi.mocked(getSponsorEvidencePack);
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

const fixedMeasuredRoi = {
  snapshot: fixedSnapshot,
  monthlyCostEstimate: {
    currency: "USD",
    tier: "Standard",
    estimatedMonthlyUsdLow: 10,
    estimatedMonthlyUsdHigh: 50,
    factors: ["tier"],
    methodologyNote: "method",
  },
  disclaimer: "Process counters are cumulative since this API replica started.",
};

const fixedSponsorEvidencePack: SponsorEvidencePackPayload = {
  generatedUtc: "2026-04-28T01:02:03.456Z",
  demoRunId: fixedSnapshot.demoRunId,
  processInstrumentation: fixedSnapshot,
  explainabilityTrace: {
    totalFindings: 2,
    overallCompletenessRatio: 0.42,
    byEngine: [
      {
        engineType: "ArchitecturalDebt",
        findingCount: 2,
        completenessRatio: 0.42,
        graphNodeIdsPopulatedCount: 1,
        rulesAppliedPopulatedCount: 0,
        decisionsTakenPopulatedCount: 0,
        alternativePathsPopulatedCount: 0,
        notesPopulatedCount: 2,
      },
    ],
  },
  demoRunValueReportDelta: {
    runCreatedUtc: "2026-04-01T10:11:12.000Z",
    timeToCommittedManifestTotalSeconds: 812.25,
    manifestCommittedUtc: "2026-04-01T10:24:44.250Z",
    findingsBySeverity: [
      { severity: "High", count: 1 },
      { severity: "Medium", count: 2 },
    ],
    auditRowCount: 5,
    auditRowCountTruncated: false,
    llmCallCount: 3,
    topFindingSeverity: "High",
    topFindingId: "f-1",
    topFindingEvidenceChain: null,
    isDemoTenant: true,
  },
  governanceOutcomes: {
    pendingApprovalCount: 0,
    recentTerminalDecisionCount: 1,
    recentPolicyPackChangeCount: 0,
  },
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
    measuredRoiMock.mockResolvedValue(fixedMeasuredRoi);
    sponsorPackMock.mockResolvedValue(fixedSponsorEvidencePack);
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
    measuredRoiMock.mockRejectedValue(new Error("snapshot failed"));
    sponsorPackMock.mockRejectedValue(new Error("pack failed"));
    reportMock.mockRejectedValue(new Error("report failed"));
    explanationMock.mockRejectedValue(new Error("explain failed"));

    render(<WhyArchLucidPage />);

    await waitFor(() => {
      const problems = screen.getAllByTestId("api-problem-mock");
      expect(problems.some((p) => p.textContent?.includes("snapshot failed"))).toBe(true);
    });
  });
});
