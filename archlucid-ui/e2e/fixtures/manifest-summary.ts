import type { ManifestSummary } from "@/types/authority";

import { getShowcaseStaticDemoPayload, SHOWCASE_STATIC_DEMO_RUN_ID } from "@/lib/showcase-static-demo";

import {
  FIXTURE_MANIFEST_EMPTY_ARTIFACTS_ID,
  FIXTURE_MANIFEST_ID,
  FIXTURE_RUN_ID,
} from "./ids";

/** Manifest summary matching the static showcase UUID (same shape as marketing preview). */
export function fixtureManifestSummaryForShowcase(urlRunId: string = SHOWCASE_STATIC_DEMO_RUN_ID): ManifestSummary {
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

/** Manifest summary that passes `coerceManifestSummary`. */
export function fixtureManifestSummary(): ManifestSummary {
  return {
    manifestId: FIXTURE_MANIFEST_ID,
    runId: FIXTURE_RUN_ID,
    createdUtc: "2025-06-01T12:05:00.000Z",
    manifestHash: "sha256:claims_intake_manifest_demo_000000000000000001",
    ruleSetId: "healthcare-claims-v3",
    ruleSetVersion: "3.4.1",
    decisionCount: 3,
    warningCount: 0,
    unresolvedIssueCount: 0,
    status: "Committed",
    hasWarnings: false,
    hasUnresolvedIssues: false,
    operatorSummary:
      "Finalized reviewed manifest for Claims Intake Modernization — integration boundaries, PHI handling posture, " +
      "and sponsor-facing KPIs.",
  };
}

/** Summary for a manifest whose artifact-list endpoint returns `[]` (valid empty, not an HTTP failure). */
export function fixtureManifestSummaryEmptyArtifacts(): ManifestSummary {
  return {
    manifestId: FIXTURE_MANIFEST_EMPTY_ARTIFACTS_ID,
    runId: FIXTURE_RUN_ID,
    createdUtc: "2025-06-01T12:07:00.000Z",
    manifestHash: "sha256:e2e_fixture_manifest_empty_artifacts_hash",
    ruleSetId: "fixture-rules",
    ruleSetVersion: "1.0.0",
    decisionCount: 1,
    warningCount: 0,
    unresolvedIssueCount: 0,
    status: "Accepted",
    hasWarnings: false,
    hasUnresolvedIssues: false,
    operatorSummary:
      "E2E fixture: golden manifest summary is present; artifact descriptors are intentionally empty (valid).",
  };
}
