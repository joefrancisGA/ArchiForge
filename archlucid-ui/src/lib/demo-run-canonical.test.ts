import { describe, expect, it } from "vitest";

import {
  canonicalizeDemoRunId,
  demoRunUrlRequiresCanonicalRedirect,
  normalizeRunSummaryForDemoPicker,
} from "@/lib/demo-run-canonical";
import { SHOWCASE_STATIC_DEMO_RUN_ID } from "@/lib/showcase-static-demo";
import type { RunSummary } from "@/types/authority";

describe("demo-run-canonical", () => {
  it("maps legacy demo run URL aliases to the showcase id", () => {
    expect(canonicalizeDemoRunId("claims-intake-modernization-run")).toBe(SHOWCASE_STATIC_DEMO_RUN_ID);
    expect(canonicalizeDemoRunId(` ${SHOWCASE_STATIC_DEMO_RUN_ID} `)).toBe(SHOWCASE_STATIC_DEMO_RUN_ID);
  });

  it("maps legacy workspace slug aliases to the showcase id", () => {
    expect(canonicalizeDemoRunId("claims-intake-sample-workspace")).toBe(SHOWCASE_STATIC_DEMO_RUN_ID);
  });

  it("demoRunUrlRequiresCanonicalRedirect is true only for known aliases", () => {
    expect(demoRunUrlRequiresCanonicalRedirect("claims-intake-modernization-run")).toBe(true);
    expect(demoRunUrlRequiresCanonicalRedirect(SHOWCASE_STATIC_DEMO_RUN_ID)).toBe(false);
    expect(demoRunUrlRequiresCanonicalRedirect("claims-intake-run-v1")).toBe(false);
  });

  it("normalizeRunSummaryForDemoPicker updates runId when aliased", () => {
    const row: RunSummary = {
      runId: "claims-intake-modernization-run",
      projectId: "default",
      description: "x",
      createdUtc: "2026-01-01T00:00:00.000Z",
      hasContextSnapshot: true,
      hasGraphSnapshot: true,
      hasFindingsSnapshot: true,
      hasGoldenManifest: true,
    };

    const norm = normalizeRunSummaryForDemoPicker(row);

    expect(norm.runId).toBe(SHOWCASE_STATIC_DEMO_RUN_ID);
    expect(norm.projectId).toBe("default");
  });
});
