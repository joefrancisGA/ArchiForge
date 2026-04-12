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
};

/** AI-generated narrative explaining the differences between two runs. */
export type ComparisonExplanation = {
  highLevelSummary: string;
  majorChanges: string[];
  keyTradeoffs: string[];
  narrative: string;
};
