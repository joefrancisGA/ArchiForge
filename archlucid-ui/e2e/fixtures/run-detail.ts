import type { RunDetail } from "@/types/authority";

import {
  FIXTURE_MANIFEST_ID,
  FIXTURE_PROJECT_ID,
  FIXTURE_RUN_ID,
} from "./ids";

/** Minimal run envelope that passes `coerceRunDetail` and matches operator run page expectations. */
export function fixtureRunDetail(): RunDetail {
  return {
    run: {
      runId: FIXTURE_RUN_ID,
      projectId: FIXTURE_PROJECT_ID,
      description: "Claims Intake Modernization — sample completed run (demo).",
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
