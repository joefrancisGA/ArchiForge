import {
  getShowcaseStaticDemoPayload,
  SHOWCASE_STATIC_DEMO_MANIFEST_ID,
  SHOWCASE_STATIC_DEMO_RUN_ID,
} from "@/lib/showcase-static-demo";
import type { ArtifactDescriptor, ManifestSummary, PipelineTimelineItem, RunDetail } from "@/types/authority";

const DEMO_RUN_IDS_FOR_STATIC_FALLBACK = new Set<string>([
  SHOWCASE_STATIC_DEMO_RUN_ID,
  "claims-intake-modernization-run",
]);

/** When true, operator run/manifest pages use curated showcase data if the API fails (demo deploys only). */
export function isOperatorDemoStaticMode(): boolean {
  return process.env.NEXT_PUBLIC_DEMO_STATIC_OPERATOR === "true";
}

export function isDemoRunIdEligibleForStaticFallback(runId: string): boolean {
  return DEMO_RUN_IDS_FOR_STATIC_FALLBACK.has(runId.trim());
}

export function buildStaticDemoRunDetailFromShowcase(urlRunId: string): RunDetail {
  const d = getShowcaseStaticDemoPayload(urlRunId);
  const manifest = d.manifest;
  const chain = d.authorityChain;

  return {
    run: {
      runId: d.run.runId,
      projectId: d.run.projectId,
      description: d.run.description,
      createdUtc: d.run.createdUtc,
      contextSnapshotId: chain.contextSnapshotId ?? undefined,
      graphSnapshotId: chain.graphSnapshotId ?? undefined,
      findingsSnapshotId: chain.findingsSnapshotId ?? undefined,
      goldenManifestId: manifest.manifestId,
      decisionTraceId: chain.decisionTraceId ?? undefined,
      artifactBundleId: chain.artifactBundleId ?? undefined,
    },
    contextSnapshot: { demo: true },
    graphSnapshot: { demo: true },
    findingsSnapshot: { demo: true },
    decisionTrace: { demo: true },
    goldenManifest: { demo: true },
    artifactBundle: { demo: true },
  };
}

export function buildStaticDemoManifestSummaryFromShowcase(urlRunId: string): ManifestSummary {
  const d = getShowcaseStaticDemoPayload(urlRunId);
  const m = d.manifest;

  return {
    manifestId: m.manifestId,
    runId: m.runId,
    createdUtc: m.createdUtc,
    manifestHash: m.manifestHash,
    ruleSetId: m.ruleSetId,
    ruleSetVersion: m.ruleSetVersion,
    decisionCount: m.decisionCount,
    warningCount: m.warningCount,
    unresolvedIssueCount: m.unresolvedIssueCount,
    status: m.status,
    hasWarnings: m.warningCount > 0,
    hasUnresolvedIssues: m.unresolvedIssueCount > 0,
    operatorSummary: m.operatorSummary,
  };
}

export function buildStaticDemoPipelineTimelineFromShowcase(urlRunId: string): PipelineTimelineItem[] {
  const d = getShowcaseStaticDemoPayload(urlRunId);

  return d.pipelineTimeline.map((row) => ({
    eventId: row.eventId,
    occurredUtc: row.occurredUtc,
    eventType: row.eventType,
    actorUserName: row.actorUserName,
    correlationId: row.correlationId ?? undefined,
  }));
}

export function buildStaticDemoArtifactsFromShowcase(urlRunId: string): ArtifactDescriptor[] {
  const d = getShowcaseStaticDemoPayload(urlRunId);
  const manifestId = d.manifest.manifestId;
  const runId = d.run.runId;

  return d.artifacts.map((a) => ({
    artifactId: a.artifactId,
    artifactType: a.artifactType,
    name: a.name,
    format: a.format,
    createdUtc: a.createdUtc,
    contentHash: a.contentHash,
    manifestId,
    runId,
  }));
}

export function tryStaticDemoRunDetail(runId: string): RunDetail | null {
  if (!isOperatorDemoStaticMode()) {
    return null;
  }

  if (!isDemoRunIdEligibleForStaticFallback(runId)) {
    return null;
  }

  return buildStaticDemoRunDetailFromShowcase(runId);
}

export function tryStaticDemoManifestSummary(manifestId: string): ManifestSummary | null {
  if (!isOperatorDemoStaticMode()) {
    return null;
  }

  if (manifestId !== SHOWCASE_STATIC_DEMO_MANIFEST_ID) {
    return null;
  }

  return buildStaticDemoManifestSummaryFromShowcase(SHOWCASE_STATIC_DEMO_RUN_ID);
}

export function tryStaticDemoPipelineTimeline(runId: string): PipelineTimelineItem[] | null {
  if (!isOperatorDemoStaticMode()) {
    return null;
  }

  if (!isDemoRunIdEligibleForStaticFallback(runId)) {
    return null;
  }

  return buildStaticDemoPipelineTimelineFromShowcase(runId);
}

export function tryStaticDemoArtifacts(runIdForPayload: string, manifestId: string): ArtifactDescriptor[] | null {
  if (!isOperatorDemoStaticMode()) {
    return null;
  }

  if (manifestId !== SHOWCASE_STATIC_DEMO_MANIFEST_ID) {
    return null;
  }

  return buildStaticDemoArtifactsFromShowcase(runIdForPayload);
}
