export type ImprovementRecommendation = {
  recommendationId: string;
  title: string;
  category: string;
  rationale: string;
  suggestedAction: string;
  urgency: string;
  expectedImpact: string;
  priorityScore: number;
};

export type ImprovementPlan = {
  runId: string;
  comparedToRunId?: string | null;
  generatedUtc: string;
  summaryNotes: string[];
  recommendations: ImprovementRecommendation[];
};

/** Persisted recommendation with governance workflow state (Change 36). */
export type RecommendationRecord = {
  recommendationId: string;
  tenantId: string;
  workspaceId: string;
  projectId: string;
  runId: string;
  comparedToRunId?: string | null;
  title: string;
  category: string;
  rationale: string;
  suggestedAction: string;
  urgency: string;
  expectedImpact: string;
  priorityScore: number;
  status: string;
  createdUtc: string;
  lastUpdatedUtc: string;
  reviewedByUserId?: string | null;
  reviewedByUserName?: string | null;
  reviewComment?: string | null;
  resolutionRationale?: string | null;
};
