/** Outcome of simulating an alert rule against a single run (would it fire? be suppressed?). */
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

/** Aggregate simulation result: how many runs matched, would fire, would be suppressed. */
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

/** Side-by-side comparison of two alert rule candidates simulated against the same runs. */
export type RuleCandidateComparisonResult = {
  candidateA: RuleSimulationResult;
  candidateB: RuleSimulationResult;
  summaryNotes: string[];
};
