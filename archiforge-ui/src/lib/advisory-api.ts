import { apiGet, apiPostJson } from "@/lib/api";
import type { RecommendationRecord } from "@/types/advisory";

export async function listRecommendations(runId: string): Promise<RecommendationRecord[]> {
  return apiGet<RecommendationRecord[]>(
    `/api/advisory/runs/${encodeURIComponent(runId)}/recommendations`,
  );
}

export async function applyRecommendationAction(
  recommendationId: string,
  action: string,
  comment?: string,
  rationale?: string,
): Promise<RecommendationRecord> {
  return apiPostJson<RecommendationRecord>(
    `/api/advisory/recommendations/${encodeURIComponent(recommendationId)}/action`,
    {
      action,
      comment: comment ?? null,
      rationale: rationale ?? null,
    },
  );
}
