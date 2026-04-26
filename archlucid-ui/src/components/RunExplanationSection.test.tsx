import { fireEvent, render, screen, within } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { riskPostureBadgeClass, riskPostureBadgeColors, RunExplanationSection } from "@/components/RunExplanationSection";
import type { RunExplanationSummary } from "@/types/explanation";

function mockSummary(overrides: Partial<RunExplanationSummary> = {}): RunExplanationSummary {
  const defaultExplanation: RunExplanationSummary["explanation"] = {
    rawText: "",
    structured: null,
    confidence: 0.72,
    provenance: {
      agentType: "unit-agent",
      modelId: "gpt-test",
      promptTemplateId: "tpl-1",
      promptTemplateVersion: "v2",
      promptContentHash: "abc",
    },
    summary: "Short summary.",
    keyDrivers: ["A: b → c"],
    riskImplications: ["r1"],
    costImplications: [],
    complianceImplications: [],
    detailedNarrative: "Long.",
  };

  const explanation: RunExplanationSummary["explanation"] = {
    ...defaultExplanation,
    ...(overrides.explanation ?? {}),
  };

  return {
    explanation,
    themeSummaries: overrides.themeSummaries ?? ["Theme one"],
    overallAssessment: overrides.overallAssessment ?? "Overall OK.",
    riskPosture: overrides.riskPosture ?? "Medium",
    findingCount: overrides.findingCount ?? 2,
    decisionCount: overrides.decisionCount ?? 3,
    unresolvedIssueCount: overrides.unresolvedIssueCount ?? 1,
    complianceGapCount: overrides.complianceGapCount ?? 0,
  };
}

describe("riskPostureBadgeColors", () => {
  it("maps severities to distinct palettes", () => {
    expect(riskPostureBadgeColors("Low").background).toBe("#dcfce7");
    expect(riskPostureBadgeColors("medium").background).toBe("#fef3c7");
    expect(riskPostureBadgeColors("HIGH").background).toBe("#ffedd5");
    expect(riskPostureBadgeColors("Critical").background).toBe("#fee2e2");
  });
});

describe("riskPostureBadgeClass", () => {
  it("maps severities to Tailwind utility groups", () => {
    expect(riskPostureBadgeClass("Low")).toContain("emerald");
    expect(riskPostureBadgeClass("medium")).toContain("amber");
    expect(riskPostureBadgeClass("HIGH")).toContain("orange");
    expect(riskPostureBadgeClass("Critical")).toContain("rose");
  });
});

describe("RunExplanationSection", () => {
  it("shows loading state", () => {
    render(<RunExplanationSection summary={null} loading={true} error={null} runId="r1" />);

    expect(screen.getByText("Loading explanation…")).toBeInTheDocument();
    expect(screen.getByRole("status")).toBeInTheDocument();
  });

  it("shows error alert", () => {
    render(<RunExplanationSection summary={null} loading={false} error="Boom" runId="r1" />);

    expect(screen.getByRole("alert")).toHaveTextContent("Boom");
  });

  it("renders risk badge, confidence progress, and themes", () => {
    render(<RunExplanationSection summary={mockSummary()} loading={false} error={null} runId="r1" />);

    expect(screen.getByRole("status", { name: /risk posture medium/i })).toBeInTheDocument();
    expect(screen.getByRole("progressbar")).toBeInTheDocument();
    expect(screen.getByText("72%")).toBeInTheDocument();
    expect(screen.getByText("Theme one")).toBeInTheDocument();
    expect(screen.getByText("Overall OK.")).toBeInTheDocument();
  });

  it("shows Not available when confidence is null", () => {
    const s = mockSummary({ explanation: { confidence: null } });

    render(<RunExplanationSection summary={s} loading={false} error={null} runId="r1" />);

    expect(screen.getByText("Not available")).toBeInTheDocument();
    expect(screen.queryByRole("progressbar")).not.toBeInTheDocument();
  });

  it("reveals provenance in details", () => {
    render(<RunExplanationSection summary={mockSummary()} loading={false} error={null} runId="r1" />);

    const provenanceDetails = document.getElementById("doc-explanation-provenance");
    expect(provenanceDetails).not.toBeNull();
    fireEvent.click(within(provenanceDetails as HTMLElement).getByText("Provenance metadata"));

    expect(screen.getByText("unit-agent")).toBeInTheDocument();
    expect(screen.getByText("gpt-test")).toBeInTheDocument();
    expect(screen.getByText("tpl-1")).toBeInTheDocument();
    expect(screen.getByText("v2")).toBeInTheDocument();
    expect(screen.getByText("abc")).toBeInTheDocument();
  });
});
