/** Lightweight summary of an architecture run (mirrors RunSummaryResponse in C#). */
export type RunSummary = {
  runId: string;
  projectId: string;
  description?: string | null;
  createdUtc: string;
  contextSnapshotId?: string | null;
  graphSnapshotId?: string | null;
  findingsSnapshotId?: string | null;
  goldenManifestId?: string | null;
  decisionTraceId?: string | null;
  artifactBundleId?: string | null;
  /** API 55R+: explicit flags (camelCase from JSON). */
  hasContextSnapshot?: boolean;
  hasGraphSnapshot?: boolean;
  hasFindingsSnapshot?: boolean;
  hasGoldenManifest?: boolean;
  hasDecisionTrace?: boolean;
  hasArtifactBundle?: boolean;
  /** Optional list enrichment (API may omit). */
  findingCount?: number | null;
  warningCount?: number | null;
  artifactCount?: number | null;
};

/** Golden manifest summary: decision/warning/issue counts and status (mirrors ManifestSummaryResponse). */
export type ManifestSummary = {
  manifestId: string;
  runId: string;
  createdUtc: string;
  manifestHash: string;
  ruleSetId: string;
  ruleSetVersion: string;
  decisionCount: number;
  warningCount: number;
  unresolvedIssueCount: number;
  status: string;
  /** API 55R+ */
  hasWarnings?: boolean;
  hasUnresolvedIssues?: boolean;
  /** API 55R+: deterministic one-line summary from API (counts + status). */
  operatorSummary?: string;
};

/** A single diff entry from a run or manifest comparison (section/key/before/after). */
export type DiffItem = {
  section: string;
  key: string;
  diffKind: string;
  beforeValue?: string | null;
  afterValue?: string | null;
  notes?: string | null;
};

/** Manifest-level comparison with added/removed/changed counts and flat diffs. */
export type ManifestComparison = {
  leftManifestId: string;
  rightManifestId: string;
  leftManifestHash: string;
  rightManifestHash: string;
  addedCount: number;
  removedCount: number;
  changedCount: number;
  diffs: DiffItem[];
};

/** Legacy flat-diff comparison between two runs (run-level diffs + optional manifest comparison). */
export type RunComparison = {
  leftRunId: string;
  rightRunId: string;
  runLevelDiffs: DiffItem[];
  manifestComparison?: ManifestComparison | null;
  runLevelDiffCount?: number;
  hasManifestComparison?: boolean;
};

/** Metadata for a synthesized artifact (file name, type, format, hash — no binary content). */
export type ArtifactDescriptor = {
  artifactId: string;
  artifactType: string;
  name: string;
  format: string;
  createdUtc: string;
  contentHash: string;
  /** API 55R+: set on list and descriptor responses. */
  manifestId?: string;
  /** API 55R+: set when returned from full artifact row (descriptor endpoint). */
  runId?: string;
};

/** Validation flags from an authority chain replay (presence checks + hash match). */
export type ReplayValidation = {
  contextPresent: boolean;
  graphPresent: boolean;
  findingsPresent: boolean;
  manifestPresent: boolean;
  tracePresent: boolean;
  artifactsPresent: boolean;
  manifestHashMatches: boolean;
  artifactBundlePresentAfterReplay: boolean;
  notes: string[];
  /** API 55R+ */
  hasValidationNotes?: boolean;
};

/** Full replay response including mode, rebuilt IDs, and validation results. */
export type ReplayResponse = {
  runId: string;
  mode: string;
  replayedUtc: string;
  rebuiltManifestId?: string | null;
  rebuiltManifestHash?: string | null;
  rebuiltArtifactBundleId?: string | null;
  validation: ReplayValidation;
  hasRebuildOutput?: boolean;
  validationNoteCount?: number;
};

/** Full run detail envelope containing the run summary and optional snapshot/manifest/trace/bundle data. */
export type RunDetail = {
  run: {
    runId: string;
    projectId: string;
    description?: string | null;
    createdUtc: string;
    contextSnapshotId?: string | null;
    graphSnapshotId?: string | null;
    findingsSnapshotId?: string | null;
    goldenManifestId?: string | null;
    decisionTraceId?: string | null;
    artifactBundleId?: string | null;
    /** Persisted W3C trace id from run creation (OpenTelemetry); distinct from the current-request trace header. */
    otelTraceId?: string;
    /** API mirrors `RunRecord.RealModeFellBackToSimulator` when present. */
    realModeFellBackToSimulator?: boolean;
    /** Optional deployment label captured when fallback was recorded (`PilotAoaiDeploymentSnapshot`). */
    pilotAoaiDeploymentSnapshot?: string | null;
  };
  contextSnapshot?: unknown;
  graphSnapshot?: unknown;
  findingsSnapshot?: unknown;
  decisionTrace?: unknown;
  goldenManifest?: unknown;
  artifactBundle?: unknown;
};

/** Node in the API decision provenance graph (camelCase JSON from GET …/provenance). */
export type ProvenanceNode = {
  id: string;
  type: number;
  referenceId: string;
  name: string;
  metadata?: Record<string, string>;
};

export type ProvenanceEdge = {
  id: string;
  fromNodeId: string;
  toNodeId: string;
  type: number;
};

export type DecisionProvenanceGraph = {
  id: string;
  runId: string;
  nodes: ProvenanceNode[];
  edges: ProvenanceEdge[];
};

/** Audit timeline row for operator pipeline view (GET …/pipeline-timeline). */
export type PipelineTimelineItem = {
  eventId: string;
  occurredUtc: string;
  eventType: string;
  actorUserName: string;
  correlationId?: string | null;
};
