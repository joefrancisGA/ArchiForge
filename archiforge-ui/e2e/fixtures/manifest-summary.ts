import type { ManifestSummary } from "@/types/authority";

import { FIXTURE_MANIFEST_ID, FIXTURE_RUN_ID } from "./ids";

/** Manifest summary that passes `coerceManifestSummary`. */
export function fixtureManifestSummary(): ManifestSummary {
  return {
    manifestId: FIXTURE_MANIFEST_ID,
    runId: FIXTURE_RUN_ID,
    createdUtc: "2025-06-01T12:05:00.000Z",
    manifestHash: "sha256:e2e_fixture_manifest_hash_0000000000000001",
    ruleSetId: "fixture-rules",
    ruleSetVersion: "1.0.0",
    decisionCount: 3,
    warningCount: 0,
    unresolvedIssueCount: 0,
    status: "Accepted",
    hasWarnings: false,
    hasUnresolvedIssues: false,
    operatorSummary: "E2E fixture manifest: 3 decisions, 0 warnings, status Accepted.",
  };
}
