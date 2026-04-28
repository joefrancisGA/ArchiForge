import { describe, expect, it } from "vitest";

import { deriveRunListPipelineLabel } from "@/components/RunStatusBadge";
import { effectiveRunSummaryForPipeline, runFromDetailToRunSummary } from "@/lib/run-summary-from-detail";
import type { RunSummary } from "@/types/authority";

const baseRun = {
  runId: "claims-intake-modernization",
  projectId: "p1",
  createdUtc: "2026-01-01T00:00:00Z",
  contextSnapshotId: "c1",
  graphSnapshotId: "g1",
  findingsSnapshotId: "f1",
  goldenManifestId: "m1",
};

describe("runFromDetailToRunSummary", () => {
  it("maps snapshot IDs to boolean flags for pipeline label derivation", () => {
    const run = runFromDetailToRunSummary({
      runId: "r1",
      projectId: "p1",
      createdUtc: "2026-01-01T00:00:00Z",
      contextSnapshotId: "c1",
      graphSnapshotId: "g1",
      findingsSnapshotId: null,
      goldenManifestId: null,
    });

    expect(run.hasContextSnapshot).toBe(true);
    expect(run.hasGraphSnapshot).toBe(true);
    expect(run.hasFindingsSnapshot).toBe(false);
    expect(deriveRunListPipelineLabel(run)).toBe("In pipeline");
  });
});

describe("effectiveRunSummaryForPipeline", () => {
  it("falls back to detail when API summary is null", () => {
    const effective = effectiveRunSummaryForPipeline(null, baseRun);

    expect(effective.hasGoldenManifest).toBe(true);
    expect(effective.hasContextSnapshot).toBe(true);
  });

  it("falls back to detail when API body does not match run id", () => {
    const junk: RunSummary = {
      ...runFromDetailToRunSummary(baseRun),
      runId: "other-run",
      hasContextSnapshot: false,
      hasGraphSnapshot: false,
      hasFindingsSnapshot: false,
      hasGoldenManifest: false,
    };

    const effective = effectiveRunSummaryForPipeline(junk, baseRun);

    expect(effective.runId).toBe("claims-intake-modernization");
    expect(effective.hasGoldenManifest).toBe(true);
  });

  it("OR-merges pipeline flags when API summary matches run id", () => {
    const partialApi: RunSummary = {
      ...runFromDetailToRunSummary(baseRun),
      hasContextSnapshot: false,
      hasGraphSnapshot: false,
      hasFindingsSnapshot: false,
      hasGoldenManifest: false,
    };

    const effective = effectiveRunSummaryForPipeline(partialApi, baseRun);

    expect(effective.hasGoldenManifest).toBe(true);
    expect(effective.hasFindingsSnapshot).toBe(true);
  });
});
