/**
 * Parses `planSnapshotJson` from evolution results (EvolutionPlanSnapshotDocument, camelCase).
 */

export type EvolutionPlanSnapshot = {
  planId: string;
  themeId: string;
  title: string;
  summary: string;
  priorityScore: number;
  priorityExplanation?: string | null;
  status: string;
  actionStepCount: number;
  linkedArchitectureRunIds: string[];
};

export function parseEvolutionPlanSnapshot(json: string): EvolutionPlanSnapshot | null {
  if (json === null || json === undefined) {
    return null;
  }

  const trimmed = json.trim();

  if (trimmed.length === 0) {
    return null;
  }

  try {
    const raw: unknown = JSON.parse(trimmed);

    if (raw === null || typeof raw !== "object") {
      return null;
    }

    const o = raw as Record<string, unknown>;
    const planId = o.planId;
    const themeId = o.themeId;
    const title = o.title;
    const summary = o.summary;
    const priorityScore = o.priorityScore;
    const status = o.status;
    const actionStepCount = o.actionStepCount;
    const linked = o.linkedArchitectureRunIds;

    if (typeof planId !== "string" || typeof themeId !== "string") {
      return null;
    }

    if (typeof title !== "string" || typeof summary !== "string" || typeof status !== "string") {
      return null;
    }

    if (typeof priorityScore !== "number" || typeof actionStepCount !== "number") {
      return null;
    }

    if (!Array.isArray(linked) || !linked.every((id) => typeof id === "string")) {
      return null;
    }

    const priorityExplanation =
      o.priorityExplanation === null || o.priorityExplanation === undefined
        ? null
        : typeof o.priorityExplanation === "string"
          ? o.priorityExplanation
          : null;

    return {
      planId,
      themeId,
      title,
      summary,
      priorityScore,
      priorityExplanation,
      status,
      actionStepCount,
      linkedArchitectureRunIds: linked,
    };
  } catch {
    return null;
  }
}
