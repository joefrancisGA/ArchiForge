import type { RuleSimulationResult } from "@/types/alert-simulation";

/** A candidate threshold value being evaluated for alert tuning. */
export type ThresholdCandidate = {
  thresholdValue: number;
  label: string;
};

/** Noise scoring breakdown for a threshold candidate (coverage, noise, suppression, density). */
export type NoiseScoreBreakdown = {
  coverageScore: number;
  noisePenalty: number;
  suppressionPenalty: number;
  densityPenalty: number;
  finalScore: number;
  notes: string[];
};

/** Full evaluation of a threshold candidate: the candidate itself, simulation results, and noise score. */
export type ThresholdCandidateEvaluation = {
  candidate: ThresholdCandidate;
  simulationResult: RuleSimulationResult;
  scoreBreakdown: NoiseScoreBreakdown;
};

/** Result of threshold tuning: all evaluated candidates with the recommended winner. */
export type ThresholdRecommendationResult = {
  evaluatedUtc: string;
  ruleKind: string;
  tunedMetricType: string;
  recommendedCandidate?: ThresholdCandidateEvaluation | null;
  summaryNotes: string[];
  candidates: ThresholdCandidateEvaluation[];
};
