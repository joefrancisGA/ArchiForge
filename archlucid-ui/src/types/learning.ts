/**
 * 59R learning / improvement planning API (`GET /v1/learning/*`). Shapes match ArchiForge.Api.Models.Learning (camelCase JSON).
 */

export type LearningThemeResponse = {
  themeId: string;
  themeKey: string;
  sourceAggregateKey?: string | null;
  patternKey?: string | null;
  title: string;
  summary: string;
  affectedArtifactTypeOrWorkflowArea: string;
  severityBand: string;
  evidenceSignalCount: number;
  distinctRunCount: number;
  averageTrustScore?: number | null;
  derivationRuleVersion: string;
  status: string;
  createdUtc: string;
  createdByUserId?: string | null;
};

export type LearningThemesListResponse = {
  generatedUtc: string;
  themes: LearningThemeResponse[];
};

export type LearningPlanListItemResponse = {
  planId: string;
  themeId: string;
  title: string;
  summary: string;
  priorityScore: number;
  priorityExplanation?: string | null;
  status: string;
  createdUtc: string;
  themeEvidenceSignalCount?: number | null;
};

export type LearningPlansListResponse = {
  generatedUtc: string;
  plans: LearningPlanListItemResponse[];
};

export type LearningSummaryResponse = {
  generatedUtc: string;
  themeCount: number;
  planCount: number;
  totalThemeEvidenceSignals: number;
  maxPlanPriorityScore?: number | null;
  totalLinkedSignalsAcrossPlans: number;
};

export type LearningPlanStepResponse = {
  ordinal: number;
  actionType: string;
  description: string;
  acceptanceCriteria?: string | null;
};

export type LearningPlanEvidenceCountsResponse = {
  linkedSignalCount: number;
  linkedArtifactCount: number;
  linkedArchitectureRunCount: number;
};

export type LearningPlanDetailResponse = {
  planId: string;
  themeId: string;
  title: string;
  summary: string;
  priorityScore: number;
  priorityExplanation?: string | null;
  status: string;
  createdUtc: string;
  createdByUserId?: string | null;
  actionSteps: LearningPlanStepResponse[];
  evidenceCounts: LearningPlanEvidenceCountsResponse;
  theme?: LearningThemeResponse | null;
};
