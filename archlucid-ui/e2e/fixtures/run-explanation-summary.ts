import type { RunExplanationSummary } from "@/types/explanation";

/** Minimal aggregate explanation for E2E mock API (run detail page). */
export function fixtureRunExplanationSummary(): RunExplanationSummary {
  return {
    explanation: {
      rawText: "{}",
      structured: {
        schemaVersion: 1,
        reasoning: "Fixture narrative.",
        evidenceRefs: [],
        confidence: 0.85,
      },
      confidence: 0.85,
      provenance: {
        agentType: "run-explanation",
        modelId: "e2e-mock",
        promptTemplateId: "explain-run-fixture",
        promptTemplateVersion: "v1",
        promptContentHash: null,
      },
      summary: "Fixture executive summary for the run.",
      keyDrivers: ["Cost: Fixture SKU → A"],
      riskImplications: ["No unresolved issues recorded."],
      costImplications: ["Max monthly cost not specified."],
      complianceImplications: ["No compliance gaps listed."],
      detailedNarrative: "Fixture narrative body.",
    },
    themeSummaries: ["Cost: 1 key driver(s) — Fixture SKU → A"],
    overallAssessment: "Overall assessment (Low risk posture): no unresolved issues or compliance gaps on the manifest; Fixture executive summary for the run.",
    riskPosture: "Low",
    findingCount: 0,
    decisionCount: 1,
    unresolvedIssueCount: 0,
    complianceGapCount: 0,
  };
}
