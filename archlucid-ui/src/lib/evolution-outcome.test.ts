import { describe, expect, it } from "vitest";
import { parseEvolutionOutcomeJson } from "./evolution-outcome";

describe("parseEvolutionOutcomeJson", () => {
  it("returns empty for blank input", () => {
    expect(parseEvolutionOutcomeJson("")).toEqual({ kind: "empty" });
    expect(parseEvolutionOutcomeJson("   ")).toEqual({ kind: "empty" });
  });

  it("parses 60R-v2 envelope shadow", () => {
    const json = JSON.stringify({
      schemaVersion: "60R-v2",
      shadow: {
        error: null,
        architectureRunId: "run-a",
        evaluationMode: "ReadOnlyArchitectureAnalysis",
        runStatus: "Succeeded",
        manifestVersion: "1.0.0",
        hasManifest: true,
        summaryLength: 120,
        warningCount: 2,
      },
      evaluation: { improvementDelta: 0.5, regressionSignals: [] },
    });

    expect(parseEvolutionOutcomeJson(json)).toEqual({
      kind: "v2",
      shadow: {
        error: null,
        architectureRunId: "run-a",
        evaluationMode: "ReadOnlyArchitectureAnalysis",
        runStatus: "Succeeded",
        manifestVersion: "1.0.0",
        hasManifest: true,
        summaryLength: 120,
        warningCount: 2,
      },
    });
  });

  it("parses legacy flat shadow DTO", () => {
    const json = JSON.stringify({
      error: null,
      architectureRunId: "run-b",
      evaluationMode: "ReadOnlyArchitectureAnalysis",
      runStatus: null,
      manifestVersion: null,
      hasManifest: false,
      summaryLength: 0,
      warningCount: 0,
    });

    expect(parseEvolutionOutcomeJson(json)).toEqual({
      kind: "legacy",
      shadow: {
        error: null,
        architectureRunId: "run-b",
        evaluationMode: "ReadOnlyArchitectureAnalysis",
        runStatus: null,
        manifestVersion: null,
        hasManifest: false,
        summaryLength: 0,
        warningCount: 0,
      },
    });
  });

  it("returns invalid for malformed JSON", () => {
    expect(parseEvolutionOutcomeJson("{")).toEqual({ kind: "invalid" });
  });
});
