import type { LearningPlanListItemResponse, LearningThemeResponse } from "@/types/learning";

/**
 * Surfaces the strongest signal themes first (evidence volume, then run spread, then recency).
 * Server list order is newest-first; the planning view labels these as "top" for operators.
 */
export function sortThemesForPlanningDisplay(themes: LearningThemeResponse[]): LearningThemeResponse[] {
  return [...themes].sort((a, b) => {
    if (b.evidenceSignalCount !== a.evidenceSignalCount) {
      return b.evidenceSignalCount - a.evidenceSignalCount;
    }

    if (b.distinctRunCount !== a.distinctRunCount) {
      return b.distinctRunCount - a.distinctRunCount;
    }

    return b.createdUtc.localeCompare(a.createdUtc);
  });
}

/** Highest priority score first; stable tie-break on title for scan-friendly tables. */
export function sortPlansForPlanningDisplay(plans: LearningPlanListItemResponse[]): LearningPlanListItemResponse[] {
  return [...plans].sort((a, b) => {
    if (b.priorityScore !== a.priorityScore) {
      return b.priorityScore - a.priorityScore;
    }

    return a.title.localeCompare(b.title);
  });
}
