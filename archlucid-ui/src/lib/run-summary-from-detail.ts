import type { RunDetail, RunSummary } from "@/types/authority";

/**
 * Builds a {@link RunSummary} from run detail so {@link deriveRunListPipelineLabel} / {@link RunStatusBadge}
 * can use the same snapshot-ID presence rules as the runs list when `getRunSummary` is unavailable.
 */
export function runFromDetailToRunSummary(run: RunDetail["run"]): RunSummary {
  return {
    runId: run.runId,
    projectId: run.projectId,
    description: run.description,
    createdUtc: run.createdUtc,
    contextSnapshotId: run.contextSnapshotId,
    graphSnapshotId: run.graphSnapshotId,
    findingsSnapshotId: run.findingsSnapshotId,
    goldenManifestId: run.goldenManifestId,
    decisionTraceId: run.decisionTraceId,
    artifactBundleId: run.artifactBundleId,
    hasContextSnapshot: Boolean(run.contextSnapshotId),
    hasGraphSnapshot: Boolean(run.graphSnapshotId),
    hasFindingsSnapshot: Boolean(run.findingsSnapshotId),
    hasGoldenManifest: Boolean(run.goldenManifestId),
    hasDecisionTrace: Boolean(run.decisionTraceId),
    hasArtifactBundle: Boolean(run.artifactBundleId),
  };
}

/**
 * Prefer `GET …/runs/{id}/summary` when it matches this run; otherwise fall back to the detail row.
 * OR-merge pipeline booleans so a malformed or empty summary (common in mock / screenshot stubs) cannot
 * contradict a finalized run that already lists snapshot IDs on the detail envelope.
 */
export function effectiveRunSummaryForPipeline(
  apiSummary: RunSummary | null,
  run: RunDetail["run"],
): RunSummary {
  const fromDetail = runFromDetailToRunSummary(run);

  if (apiSummary === null || typeof apiSummary.runId !== "string" || apiSummary.runId !== run.runId) {
    return fromDetail;
  }

  return {
    ...fromDetail,
    ...apiSummary,
    hasContextSnapshot: apiSummary.hasContextSnapshot === true || fromDetail.hasContextSnapshot === true,
    hasGraphSnapshot: apiSummary.hasGraphSnapshot === true || fromDetail.hasGraphSnapshot === true,
    hasFindingsSnapshot: apiSummary.hasFindingsSnapshot === true || fromDetail.hasFindingsSnapshot === true,
    hasGoldenManifest: apiSummary.hasGoldenManifest === true || fromDetail.hasGoldenManifest === true,
    hasDecisionTrace: apiSummary.hasDecisionTrace === true || fromDetail.hasDecisionTrace === true,
    hasArtifactBundle: apiSummary.hasArtifactBundle === true || fromDetail.hasArtifactBundle === true,
  };
}
