import { describe, expect, it } from "vitest";

import {
  assignRunWorkQueueGroup,
  partitionRunsIntoWorkQueueSections,
  workQueueSectionHeading,
} from "./run-work-queue-groups";

import type { RunSummary } from "@/types/authority";

function baseRun(overrides: Partial<RunSummary> = {}): RunSummary {
  return {
    runId: "00000000-0000-0000-0000-000000000001",
    projectId: "default",
    createdUtc: "2026-01-01T00:00:00.000Z",
    ...overrides,
  };
}

describe("assignRunWorkQueueGroup", () => {
  it("classifies committed runs by hasGoldenManifest", () => {
    expect(assignRunWorkQueueGroup(baseRun({ hasGoldenManifest: true }))).toBe("committed");
  });

  it("classifies needs-attention when findings exist but manifest is absent", () => {
    expect(
      assignRunWorkQueueGroup(
        baseRun({ hasFindingsSnapshot: true, hasGoldenManifest: false }),
      ),
    ).toBe("needs-attention");
  });

  it("treats missing manifest flag as not committed for needs-attention", () => {
    expect(
      assignRunWorkQueueGroup(
        baseRun({ hasFindingsSnapshot: true, hasGoldenManifest: undefined }),
      ),
    ).toBe("needs-attention");
  });

  it("classifies in-progress when no findings yet", () => {
    expect(
      assignRunWorkQueueGroup(
        baseRun({
          hasContextSnapshot: true,
          hasGraphSnapshot: true,
          hasFindingsSnapshot: false,
        }),
      ),
    ).toBe("in-progress");
  });
});

describe("partitionRunsIntoWorkQueueSections", () => {
  it("orders sections needs-attention, in-progress, committed and drops empties", () => {
    const sections = partitionRunsIntoWorkQueueSections([
      baseRun({ runId: "a", hasGoldenManifest: true }),
      baseRun({ runId: "b", hasFindingsSnapshot: true, hasGoldenManifest: false }),
      baseRun({ runId: "c", hasContextSnapshot: true }),
    ]);

    expect(sections.map((s) => s.groupId)).toEqual(["needs-attention", "in-progress", "committed"]);
    expect(sections[0]?.runs.map((r) => r.runId)).toEqual(["b"]);
    expect(sections[1]?.runs.map((r) => r.runId)).toEqual(["c"]);
    expect(sections[2]?.runs.map((r) => r.runId)).toEqual(["a"]);
  });
});

describe("workQueueSectionHeading", () => {
  it("returns stable labels", () => {
    expect(workQueueSectionHeading("needs-attention")).toBe("Needs attention");
    expect(workQueueSectionHeading("in-progress")).toBe("In progress");
    expect(workQueueSectionHeading("committed")).toBe("Finalized");
  });
});
