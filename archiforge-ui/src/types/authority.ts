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
};

export type ArtifactDescriptor = {
  artifactId: string;
  artifactType: string;
  name: string;
  format: string;
  createdUtc: string;
  contentHash: string;
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
};

export type ReplayResponse = {
  runId: string;
  mode: string;
  replayedUtc: string;
  rebuiltManifestId?: string | null;
  rebuiltManifestHash?: string | null;
  rebuiltArtifactBundleId?: string | null;
  validation: ReplayValidation;
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
