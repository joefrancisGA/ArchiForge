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

/** API string enum for coarse evaluation-backed confidence (JSON via JsonStringEnumConverter). */
export type FindingConfidenceLevel = "High" | "Medium" | "Low";

/** Per-finding explainability trace completeness from the API. */
export type FindingTraceConfidenceDto = {
  findingId: string;
  traceCompletenessRatio: number;
  traceConfidenceLabel: string;
  /** Logical rule id(s) from the explainability trace (`;`-joined when multiple), or `unspecified` when present. */
  ruleId?: string | null;
  /** Count of deterministic evidence references backing the finding. */
  evidenceRefCount?: number | null;
  /** Plain-language title; may be long — truncate in dense tables. */
  findingTitle?: string | null;
  /** Trace dimensions that were empty when completeness was scored. */
  missingTraceFields?: string[] | null;
  /** Actionable next steps from the finding / trace pipeline when present. */
  recommendedActions?: string[] | null;
  /** Deterministic score from harness / reference-case / trace completeness when persisted. */
  evaluationConfidenceScore?: number | null;
  /** Mapped bucket for {@link evaluationConfidenceScore}. */
  confidenceLevel?: FindingConfidenceLevel | null;
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

/** Persisted artifact reference emitted with aggregate explanations (API: PascalCase → camelCase in JSON). */
export type CitationReference = {
  kind:
    | "Manifest"
    | "Finding"
    | "DecisionTrace"
    | "EvidenceBundle"
    | "GraphSnapshot"
    | "ContextSnapshot";
  id: string;
  label: string;
  runId?: string | null;
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
  /** Persisted artifacts backing the narrative; absent on older APIs. */
  citations?: CitationReference[] | null;
};

/** Deterministic factual explainability for one finding (never LLM-derived). */
export type FindingExplainabilityEvidence = {
  evidenceRefs: string[];
  conclusion: string;
  alternativePathsConsidered: string[];
  ruleId: string;
};

/** Pointers linking one finding to persisted run artifacts (`GET /v1/architecture/run/.../findings/.../evidence-chain`). */
export type FindingEvidenceChain = {
  runId: string;
  findingId: string;
  manifestVersion?: string | null;
  findingsSnapshotId?: string | null;
  contextSnapshotId?: string | null;
  graphSnapshotId?: string | null;
  decisionTraceId?: string | null;
  goldenManifestId?: string | null;
  relatedGraphNodeIds: string[];
  agentExecutionTraceIds: string[];
};

/** Redacted LLM audit slice for one finding (`GET /v1/explain/runs/.../findings/.../llm-audit`). */
export type FindingLlmAudit = {
  traceId: string;
  agentType: string;
  systemPromptRedacted: string;
  userPromptRedacted: string;
  rawResponseRedacted: string;
  modelDeploymentName?: string | null;
  modelVersion?: string | null;
  redactionCountsByCategory: Record<string, number>;
};

/** Deterministic explainability payload for one finding (`GET /v1/explain/runs/.../findings/.../explainability`). */
export type FindingExplainability = {
  findingId: string;
  title: string;
  engineType: string;
  severity: string;
  traceCompletenessRatio: number;
  /** Trace dimensions that were empty when completeness was scored. */
  missingTraceFields?: string[] | null;
  graphNodeIdsExamined: string[];
  rulesApplied: string[];
  decisionsTaken: string[];
  alternativePathsConsidered: string[];
  notes: string[];
  /** Structured factual explainability; absent on older API responses. */
  evidence?: FindingExplainabilityEvidence | null;
  narrativeText: string;
  /** Evaluation-derived confidence when persisted (absent on older responses). */
  evaluationConfidenceScore?: number | null;
  confidenceLevel?: FindingConfidenceLevel | null;
};

/** AI-generated narrative explaining the differences between two runs. */
export type ComparisonExplanation = {
  highLevelSummary: string;
  majorChanges: string[];
  keyTradeoffs: string[];
  narrative: string;
};
