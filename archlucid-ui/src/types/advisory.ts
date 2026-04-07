/** A single AI-generated improvement recommendation (part of an ImprovementPlan). */
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

/** AI-generated improvement plan for a run, with prioritized recommendations. */
export type ImprovementPlan = {
  runId: string;
  comparedToRunId?: string | null;
  generatedUtc: string;
  summaryNotes: string[];
  recommendations: ImprovementRecommendation[];
  /** Merged advisoryDefaults from effective policy packs (optional). */
  policyPackAdvisoryDefaults?: Record<string, string>;
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
