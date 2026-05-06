import { describe, expect, it } from "vitest";

import { buyerFacingReviewTitleFromSummary } from "@/lib/buyer-facing-review-title";
import { SHOWCASE_STATIC_DEMO_RUN_ID } from "@/lib/showcase-static-demo";
import type { RunSummary } from "@/types/authority";

function summary(overrides: Partial<RunSummary>): RunSummary {
  return {
    runId: "rid",
    projectId: "default",
    description: "Test",
    createdUtc: "2026-01-01T00:00:00.000Z",
    hasFindingsSnapshot: false,
    hasGoldenManifest: false,
    ...overrides,
  };
}

describe("buyerFacingReviewTitleFromSummary", () => {
  it("uses stable Claims Intake title for the showcase review id", () => {
    const title = buyerFacingReviewTitleFromSummary(
      summary({ runId: SHOWCASE_STATIC_DEMO_RUN_ID, description: "Legacy description" }),
    );

    expect(title).toBe("Claims Intake Modernization Review");
  });

  it("falls back to description then untitled", () => {
    expect(buyerFacingReviewTitleFromSummary(summary({ runId: "other", description: "  My review  " }))).toBe("My review");

    expect(buyerFacingReviewTitleFromSummary(summary({ runId: "other", description: "" }))).toBe("Untitled review");
  });
});
