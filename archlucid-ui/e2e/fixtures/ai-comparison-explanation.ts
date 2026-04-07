import type { ComparisonExplanation } from "@/types/explanation";

/** AI compare explanation that passes `coerceComparisonExplanation`. */
export function fixtureComparisonExplanation(): ComparisonExplanation {
  return {
    highLevelSummary: "E2E fixture: target run adds capacity versus the base run.",
    majorChanges: ["Fourth service introduced in target topology.", "Estimated cost rises modestly."],
    keyTradeoffs: ["Higher cost vs. improved isolation."],
    narrative:
      "This is deterministic fixture copy for Playwright. No model was called. " +
      "The UI should render these sections for operator review.",
  };
}
