export type SimulatedAlertOutcome = {
  runId?: string | null;
  comparedToRunId?: string | null;
  ruleMatched: boolean;
  wouldCreateAlert: boolean;
  wouldBeSuppressed: boolean;
  title: string;
  severity: string;
  description: string;
  deduplicationKey: string;
  suppressionReason: string;
  evaluationMode: string;
  notes: string[];
};

export type RuleSimulationResult = {
  ruleKind: string;
  simulatedUtc: string;
  evaluatedRunCount: number;
  matchedCount: number;
  wouldCreateCount: number;
  wouldSuppressCount: number;
  summaryNotes: string[];
  outcomes: SimulatedAlertOutcome[];
};

export type RuleCandidateComparisonResult = {
  candidateA: RuleSimulationResult;
  candidateB: RuleSimulationResult;
  summaryNotes: string[];
};
