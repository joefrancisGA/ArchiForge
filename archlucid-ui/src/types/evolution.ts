/**
 * 60R evolution API (`/v1/evolution/*`). Shapes match ArchiForge.Api.Models.Evolution (camelCase JSON).
 */

export type EvolutionCandidateChangeSetResponse = {
  candidateChangeSetId: string;
  sourcePlanId: string;
  status: string;
  title: string;
  summary: string;
  derivationRuleVersion: string;
  createdUtc: string;
  createdByUserId?: string | null;
};

export type EvolutionCandidateChangeSetListResponse = {
  candidates: EvolutionCandidateChangeSetResponse[];
};

export type EvaluationScoreResponse = {
  simulationScore?: number | null;
  determinismScore?: number | null;
  regressionRiskScore?: number | null;
  improvementDelta?: number | null;
  regressionSignals: string[];
  confidenceScore?: number | null;
};

export type EvolutionSimulationRunWithEvaluationResponse = {
  simulationRunId: string;
  baselineArchitectureRunId: string;
  evaluationMode: string;
  outcomeJson: string;
  warningsJson?: string | null;
  completedUtc: string;
  isShadowOnly: boolean;
  evaluationScore?: EvaluationScoreResponse | null;
  evaluationExplanationSummary?: string | null;
  outcomeSchemaVersion?: string | null;
};

export type EvolutionResultsResponse = {
  candidate: EvolutionCandidateChangeSetResponse;
  planSnapshotJson: string;
  simulationRuns: EvolutionSimulationRunWithEvaluationResponse[];
};

export type EvolutionSimulateResponse = {
  candidate: EvolutionCandidateChangeSetResponse;
  simulationRuns: EvolutionSimulationRunWithEvaluationResponse[];
};
