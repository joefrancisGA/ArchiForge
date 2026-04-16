/** AI-generated explanation of a single run's decisions, risks, costs, and compliance. */
export type RunExplanation = {
  summary: string;
  keyDrivers: string[];
  riskImplications: string[];
  costImplications: string[];
  complianceImplications: string[];
  detailedNarrative: string;
};

/** Provenance for a run explanation (agent, model, optional prompt catalog fields). */
export type ExplanationProvenance = {
  agentType: string;
  modelId: string;
  promptTemplateId: string | null;
  promptTemplateVersion: string | null;
  promptContentHash: string | null;
};

/** Structured LLM envelope nested under `ExplanationResult` on the API. */
export type StructuredExplanation = {
  schemaVersion: number;
  reasoning: string;
  evidenceRefs: string[];
  confidence: number | null;
  alternativesConsidered?: string[] | null;
  caveats?: string[] | null;
};

/** Per-finding explainability trace completeness from the API. */
export type FindingTraceConfidenceDto = {
  findingId: string;
  traceCompletenessRatio: number;
  traceConfidenceLabel: string;
};

/** Full explanation payload returned inside `RunExplanationSummary`. */
export type ExplanationResult = {
  rawText: string;
  structured: StructuredExplanation | null;
  confidence: number | null;
  provenance: ExplanationProvenance | null;
  summary: string;
  keyDrivers: string[];
  riskImplications: string[];
  costImplications: string[];
  complianceImplications: string[];
  detailedNarrative: string;
  /** Present on `GET /v1/explain/runs/{runId}/explain` when a findings snapshot exists. */
  findingTraceConfidences?: FindingTraceConfidenceDto[] | null;
};

/** Aggregate executive view for a run (themes, posture, counts + nested explanation). */
export type RunExplanationSummary = {
  explanation: ExplanationResult;
  themeSummaries: string[];
  overallAssessment: string;
  riskPosture: string;
  findingCount: number;
  decisionCount: number;
  unresolvedIssueCount: number;
  complianceGapCount: number;
  /** Set when faithfulness checker had claims to evaluate (0–1 support ratio). */
  faithfulnessSupportRatio?: number | null;
  /** Absent on older API responses; treat as false. */
  usedDeterministicFallback?: boolean;
  faithfulnessWarning?: string | null;
  findingTraceConfidences?: FindingTraceConfidenceDto[] | null;
};

/** Deterministic explainability payload for one finding (`GET /v1/explain/runs/.../findings/.../explainability`). */
export type FindingExplainability = {
  findingId: string;
  title: string;
  engineType: string;
  severity: string;
  traceCompletenessRatio: number;
  graphNodeIdsExamined: string[];
  rulesApplied: string[];
  decisionsTaken: string[];
  alternativePathsConsidered: string[];
  notes: string[];
  narrativeText: string;
};

/** AI-generated narrative explaining the differences between two runs. */
export type ComparisonExplanation = {
  highLevelSummary: string;
  majorChanges: string[];
  keyTradeoffs: string[];
  narrative: string;
};
