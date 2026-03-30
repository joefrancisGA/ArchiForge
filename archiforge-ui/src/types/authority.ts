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
};

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

export type DiffItem = {
  section: string;
  key: string;
  diffKind: string;
  beforeValue?: string | null;
  afterValue?: string | null;
  notes?: string | null;
};

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

export type RunComparison = {
  leftRunId: string;
  rightRunId: string;
  runLevelDiffs: DiffItem[];
  manifestComparison?: ManifestComparison | null;
  runLevelDiffCount?: number;
  hasManifestComparison?: boolean;
};

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
  };
  contextSnapshot?: unknown;
  graphSnapshot?: unknown;
  findingsSnapshot?: unknown;
  decisionTrace?: unknown;
  goldenManifest?: unknown;
  artifactBundle?: unknown;
};
