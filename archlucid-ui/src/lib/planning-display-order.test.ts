import { describe, expect, it } from "vitest";
import type { LearningPlanListItemResponse, LearningThemeResponse } from "@/types/learning";
import { sortPlansForPlanningDisplay, sortThemesForPlanningDisplay } from "./planning-display-order";

function theme(partial: Partial<LearningThemeResponse> & Pick<LearningThemeResponse, "themeId">): LearningThemeResponse {
  return {
    themeKey: "k",
    title: "t",
    summary: "s",
    affectedArtifactTypeOrWorkflowArea: "a",
    severityBand: "low",
    evidenceSignalCount: 0,
    distinctRunCount: 0,
    derivationRuleVersion: "1",
    status: "open",
    createdUtc: "2024-01-01T00:00:00Z",
    ...partial,
  };
}

function plan(partial: Partial<LearningPlanListItemResponse> & Pick<LearningPlanListItemResponse, "planId">): LearningPlanListItemResponse {
  return {
    themeId: "th",
    title: "p",
    summary: "s",
    priorityScore: 0,
    status: "open",
    createdUtc: "2024-01-01T00:00:00Z",
    ...partial,
  };
}

describe("sortThemesForPlanningDisplay", () => {
  it("orders by evidence signal count descending", () => {
    const a = theme({ themeId: "a", evidenceSignalCount: 1 });
    const b = theme({ themeId: "b", evidenceSignalCount: 9 });
    expect(sortThemesForPlanningDisplay([a, b]).map((t) => t.themeId)).toEqual(["b", "a"]);
  });

  it("breaks ties with distinct run count then createdUtc", () => {
    const older = theme({
      themeId: "o",
      evidenceSignalCount: 2,
      distinctRunCount: 1,
      createdUtc: "2024-01-01T00:00:00Z",
    });
    const newer = theme({
      themeId: "n",
      evidenceSignalCount: 2,
      distinctRunCount: 1,
      createdUtc: "2024-06-01T00:00:00Z",
    });
    expect(sortThemesForPlanningDisplay([older, newer]).map((t) => t.themeId)).toEqual(["n", "o"]);
  });
});

describe("sortPlansForPlanningDisplay", () => {
  it("orders by priority score descending", () => {
    const low = plan({ planId: "l", priorityScore: 1, title: "a" });
    const high = plan({ planId: "h", priorityScore: 10, title: "b" });
    expect(sortPlansForPlanningDisplay([low, high]).map((p) => p.planId)).toEqual(["h", "l"]);
  });

  it("breaks ties by title", () => {
    const b = plan({ planId: "2", priorityScore: 5, title: "b" });
    const a = plan({ planId: "1", priorityScore: 5, title: "a" });
    expect(sortPlansForPlanningDisplay([b, a]).map((p) => p.planId)).toEqual(["1", "2"]);
  });
});
