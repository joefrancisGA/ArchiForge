import { describe, expect, it } from "vitest";

import {
  coerceArtifactDescriptor,
  coerceArtifactDescriptorList,
  coerceGoldenManifestComparison,
  coerceGraphViewModel,
  coerceManifestSummary,
  coerceReplayResponse,
  coerceRunComparison,
  coerceRunDetail,
  coerceRunSummaryList,
} from "./operator-response-guards";

describe("coerceRunSummaryList", () => {
  it("accepts empty array", () => {
    const result = coerceRunSummaryList([]);

    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.items).toEqual([]);
    }
  });

  it("rejects non-array", () => {
    const result = coerceRunSummaryList({});

    expect(result.ok).toBe(false);
  });

  it("rejects row without runId", () => {
    const result = coerceRunSummaryList([{ projectId: "p" }]);

    expect(result.ok).toBe(false);
  });
});

describe("coerceGraphViewModel", () => {
  it("accepts empty nodes and edges", () => {
    const result = coerceGraphViewModel({ nodes: [], edges: [] });

    expect(result.ok).toBe(true);
  });

  it("rejects missing nodes", () => {
    const result = coerceGraphViewModel({ edges: [] });

    expect(result.ok).toBe(false);
  });
});

describe("coerceRunComparison", () => {
  it("accepts minimal legacy compare payload with empty diffs", () => {
    const result = coerceRunComparison({
      leftRunId: "L",
      rightRunId: "R",
      runLevelDiffs: [],
    });

    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.value.runLevelDiffs).toEqual([]);
    }
  });

  it("rejects missing runLevelDiffs array", () => {
    expect(
      coerceRunComparison({
        leftRunId: "L",
        rightRunId: "R",
      }).ok,
    ).toBe(false);
  });
});

describe("coerceGoldenManifestComparison", () => {
  it("accepts minimal valid shape", () => {
    const result = coerceGoldenManifestComparison({
      baseRunId: "a",
      targetRunId: "b",
      decisionChanges: [],
      requirementChanges: [],
      securityChanges: [],
      topologyChanges: [],
      costChanges: [],
      summaryHighlights: [],
    });

    expect(result.ok).toBe(true);
  });

  it("rejects missing array section", () => {
    const result = coerceGoldenManifestComparison({
      baseRunId: "a",
      targetRunId: "b",
      decisionChanges: [],
    });

    expect(result.ok).toBe(false);
  });
});

describe("coerceReplayResponse", () => {
  it("accepts validation with notes", () => {
    const result = coerceReplayResponse({
      runId: "r",
      mode: "ReconstructOnly",
      replayedUtc: "2020-01-01T00:00:00Z",
      validation: {
        notes: [],
        manifestHashMatches: true,
        artifactBundlePresentAfterReplay: false,
      },
    });

    expect(result.ok).toBe(true);
  });

  it("rejects missing validation.notes", () => {
    const result = coerceReplayResponse({
      runId: "r",
      mode: "m",
      replayedUtc: "2020-01-01T00:00:00Z",
      validation: {},
    });

    expect(result.ok).toBe(false);
  });
});

describe("coerceRunDetail", () => {
  it("accepts minimal run envelope", () => {
    const result = coerceRunDetail({
      run: {
        runId: "a",
        projectId: "p",
        createdUtc: "2020-01-01T00:00:00Z",
      },
    });

    expect(result.ok).toBe(true);
  });

  it("rejects missing run", () => {
    expect(coerceRunDetail({}).ok).toBe(false);
  });
});

describe("coerceManifestSummary", () => {
  it("accepts valid summary", () => {
    const result = coerceManifestSummary({
      manifestId: "m",
      runId: "r",
      createdUtc: "2020-01-01T00:00:00Z",
      manifestHash: "h",
      ruleSetId: "rs",
      ruleSetVersion: "1",
      decisionCount: 0,
      warningCount: 0,
      unresolvedIssueCount: 0,
      status: "Committed",
    });

    expect(result.ok).toBe(true);
  });

  it("rejects bad status type", () => {
    const result = coerceManifestSummary({
      manifestId: "m",
      runId: "r",
      createdUtc: "2020-01-01T00:00:00Z",
      manifestHash: "h",
      ruleSetId: "rs",
      ruleSetVersion: "1",
      decisionCount: 0,
      warningCount: 0,
      unresolvedIssueCount: 0,
      status: 1,
    });

    expect(result.ok).toBe(false);
  });
});

describe("coerceArtifactDescriptor", () => {
  it("accepts valid descriptor", () => {
    const result = coerceArtifactDescriptor({
      artifactId: "a",
      artifactType: "Inventory",
      name: "n",
      format: "json",
      createdUtc: "2020-01-01T00:00:00Z",
      contentHash: "h",
    });

    expect(result.ok).toBe(true);
  });

  it("rejects missing format", () => {
    expect(
      coerceArtifactDescriptor({
        artifactId: "a",
        artifactType: "Inventory",
        name: "n",
        createdUtc: "2020-01-01T00:00:00Z",
        contentHash: "h",
      }).ok,
    ).toBe(false);
  });
});

describe("coerceArtifactDescriptorList", () => {
  it("accepts empty list", () => {
    expect(coerceArtifactDescriptorList([]).ok).toBe(true);
  });

  it("rejects row without artifactId", () => {
    expect(coerceArtifactDescriptorList([{ name: "n" }]).ok).toBe(false);
  });
});
