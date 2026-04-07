/**
 * JSON shapes for GET /v1/product-learning/* (camelCase from ASP.NET).
 * Change Set 58R — pilot feedback rollups.
 */

export type ProductLearningDashboardSummaryResponse = {
  generatedUtc: string;
  tenantId: string;
  workspaceId: string;
  projectId: string;
  totalSignalsInScope: number;
  distinctRunsTouched: number;
  topAggregateCount: number;
  artifactTrendCount: number;
  improvementOpportunityCount: number;
  triageQueueItemCount: number;
  summaryNotes: string[];
};

export type ArtifactOutcomeTrend = {
  trendKey: string;
  artifactTypeOrHint: string;
  windowLabel: string | null;
  acceptedOrTrustedCount: number;
  revisionCount: number;
  rejectionCount: number;
  needsFollowUpCount: number;
  distinctRunCount: number;
  averageTrustScore: number | null;
  averageUsefulnessScore: number | null;
  repeatedThemeIndicator: string | null;
  firstSeenUtc: string;
  lastSeenUtc: string;
};

export type ImprovementOpportunity = {
  opportunityId: string;
  sourceAggregateKey: string | null;
  patternKey: string | null;
  title: string;
  summary: string;
  affectedArtifactTypeOrWorkflowArea: string;
  severity: string;
  priorityRank: number;
  suggestedOwnerRole: string | null;
  evidenceSignalCount: number;
  distinctRunCount: number;
  averageTrustScore: number | null;
  repeatedThemeSnippet: string | null;
  firstSeenUtc: string;
  lastSeenUtc: string;
};

export type TriageQueueItem = {
  queueItemId: string;
  relatedSignalId: string | null;
  relatedOpportunityId: string | null;
  title: string;
  detailSummary: string;
  priorityRank: number;
  severity: string;
  affectedArtifactTypeOrWorkflowArea: string;
  triageStatus: string;
  firstSeenUtc: string;
  lastSeenUtc: string;
  suggestedNextAction: string | null;
};

export type ProductLearningImprovementOpportunitiesResponse = {
  generatedUtc: string;
  opportunities: ImprovementOpportunity[];
};

export type ProductLearningArtifactOutcomeTrendsResponse = {
  generatedUtc: string;
  trends: ArtifactOutcomeTrend[];
};

export type ProductLearningTriageQueueResponse = {
  generatedUtc: string;
  items: TriageQueueItem[];
};

/** Result of loading all four product-learning slices in parallel (same scope and optional `since`). */
export type ProductLearningDashboardBundle = {
  summary: ProductLearningDashboardSummaryResponse;
  opportunities: ProductLearningImprovementOpportunitiesResponse;
  trends: ProductLearningArtifactOutcomeTrendsResponse;
  triage: ProductLearningTriageQueueResponse;
};
