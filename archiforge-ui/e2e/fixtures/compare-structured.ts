import type { GoldenManifestComparison } from "@/types/comparison";

import { FIXTURE_LEFT_RUN_ID, FIXTURE_RIGHT_RUN_ID } from "./ids";

/** Structured golden-manifest compare that passes `coerceGoldenManifestComparison`. */
export function fixtureGoldenManifestComparison(): GoldenManifestComparison {
  return {
    baseRunId: FIXTURE_LEFT_RUN_ID,
    targetRunId: FIXTURE_RIGHT_RUN_ID,
    decisionChanges: [
      {
        decisionKey: "fixture.decision.alpha",
        baseValue: "v1",
        targetValue: "v2",
        changeType: "Modified",
      },
    ],
    requirementChanges: [],
    securityChanges: [],
    topologyChanges: [],
    costChanges: [{ baseCost: 100, targetCost: 120 }],
    summaryHighlights: [
      "Fixture highlight alpha: cost increased from 100 to 120.",
      "Fixture highlight beta: one decision modified.",
    ],
    totalDeltaCount: 2,
  };
}
