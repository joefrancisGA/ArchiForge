import type { RunDetail } from "@/types/authority";

import { getShowcaseStaticDemoPayload } from "@/lib/showcase-static-demo";

import {
  FIXTURE_MANIFEST_ID,
  FIXTURE_PROJECT_ID,
  FIXTURE_RUN_ID,
} from "./ids";

/** Run detail aligned to marketing showcase data (mock API for `claims-intake-*` run URL segments). */
export function fixtureRunDetailAlignedToShowcase(urlRunId: string): RunDetail {
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
    contextSnapshot: { fixture: true },
    graphSnapshot: { fixture: true },
    findingsSnapshot: { fixture: true },
    decisionTrace: { fixture: true },
    goldenManifest: { fixture: true },
    artifactBundle: { fixture: true },
  };
}

/** Minimal run envelope that passes `coerceRunDetail` and matches operator run page expectations. */
export function fixtureRunDetail(): RunDetail {
  return {
    run: {
      runId: FIXTURE_RUN_ID,
      projectId: FIXTURE_PROJECT_ID,
      description:
        "Claims Intake Modernization — integration boundaries, PHI handling posture, and sponsor-facing KPIs.",
      createdUtc: "2025-06-01T12:00:00.000Z",
      contextSnapshotId: "ctx-snap-fixture",
      graphSnapshotId: "graph-snap-fixture",
      findingsSnapshotId: "findings-snap-fixture",
      goldenManifestId: FIXTURE_MANIFEST_ID,
      decisionTraceId: "trace-fixture",
      artifactBundleId: "bundle-fixture",
    },
    contextSnapshot: { fixture: true },
    graphSnapshot: { fixture: true },
    findingsSnapshot: { fixture: true },
    decisionTrace: { fixture: true },
    goldenManifest: { fixture: true },
    artifactBundle: { fixture: true },
  };
}
