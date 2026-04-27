import type { DemoCommitPagePreviewResponse } from "@/types/demo-preview";

/** Public-marketing slug for demos and screenshots — no fixture-style token. */
export const SHOWCASE_STATIC_DEMO_RUN_ID = "claims-intake-modernization";

const GENERATED_UTC = "2026-01-15T14:30:00.000Z";

/**
 * Read-only static payload for `/showcase/[runId]` when no preview API is configured,
 * or for mock API responses in E2E. `urlRunId` is echoed into `run.runId` so the URL and body stay aligned.
 */
export function getShowcaseStaticDemoPayload(urlRunId: string): DemoCommitPagePreviewResponse {
  const runId = urlRunId.trim().length > 0 ? urlRunId.trim() : SHOWCASE_STATIC_DEMO_RUN_ID;

  return {
    generatedUtc: GENERATED_UTC,
    isDemoData: true,
    demoStatusMessage: "Demonstration — sample healthcare intake scenario",
    run: {
      runId,
      projectId: "contoso-health-pilot",
      description:
        "Claims Intake Modernization — streamline intake workflows, adjudication handoffs, and audit-ready evidence.",
      createdUtc: "2026-01-10T09:15:22.000Z",
    },
    manifest: {
      manifestId: "a1c2e3f4-a5b6-7890-abcd-ef1234567890",
      runId,
      createdUtc: "2026-01-14T22:08:41.000Z",
      manifestHash: "sha256-demo-7f91c4aab3…",
      ruleSetId: "healthcare-claims-v3",
      ruleSetVersion: "3.4.1",
      decisionCount: 12,
      warningCount: 1,
      unresolvedIssueCount: 0,
      status: "Committed",
      operatorSummary:
        "Finalized reviewed manifest for Claims Intake Modernization — integration boundaries, PHI handling posture, " +
        "and sponsor-facing KPIs consolidated for sign-off.",
    },
    authorityChain: {
      contextSnapshotId: "ctx-snapshot-01",
      graphSnapshotId: "graph-snapshot-01",
      findingsSnapshotId: "find-snapshot-01",
      goldenManifestId: "a1c2e3f4-a5b6-7890-abcd-ef1234567890",
      decisionTraceId: "trace-claims-01",
      artifactBundleId: "bundle-intake-demo-01",
    },
    artifacts: [
      {
        artifactId: "b2d4e6f8-a1c3-5e79-abcd-ef9876543210",
        artifactType: "MarkdownReport",
        name: "Sponsor briefing — Claims Intake Modernization.md",
        format: "text/markdown",
        createdUtc: "2026-01-14T22:10:05.000Z",
        contentHash: "sha256-demo-art-md",
      },
      {
        artifactId: "c3e5f709-b2d4-6f81-bcde-f12345678901",
        artifactType: "JsonBundle",
        name: "Architecture decision record bundle.json",
        format: "application/json",
        createdUtc: "2026-01-14T22:10:12.000Z",
        contentHash: "sha256-demo-art-json",
      },
      {
        artifactId: "d4f6181b-c5e7-7932-cdef-a23456789012",
        artifactType: "Diagram",
        name: "Intake modernization context diagram.mmd",
        format: "text/plain",
        createdUtc: "2026-01-14T22:10:20.000Z",
        contentHash: "sha256-demo-art-diagram",
      },
    ],
    pipelineTimeline: [
      {
        eventId: "evt-pipeline-finalize-demo",
        occurredUtc: "2026-01-14T22:07:58.000Z",
        eventType: "Finalize",
        actorUserName: "Taylor Morgan",
        correlationId: "corr-intake-demo-01",
      },
    ],
    runExplanation: {
      explanation: {
        rawText: "",
        structured: null,
        confidence: null,
        provenance: null,
        summary: "Demonstration narrative for Claims Intake Modernization.",
        keyDrivers: [
          "Member experience parity across channels",
          "Auditability of intake-to-adjudication flow",
          "Latency under peak submission windows",
        ],
        riskImplications: ["PHI egress controls must stay consistent during rollout."],
        costImplications: ["Ops touch reduction on intake rework."],
        complianceImplications: ["HIPAA-aligned logging and segregation of duties."],
        detailedNarrative:
          "This demonstration summarizes a stable, sponsor-reviewable modernization path for intake with clear " +
          "decisions, bounded risks, and evidence-backed recommendations.",
      },
      themeSummaries: ["Intake experience", "Platform integration", "Compliance posture"],
      overallAssessment:
        "Modernization preserves core intake guarantees while improving throughput and downstream traceability.",
      riskPosture: "Controlled",
      findingCount: 9,
      decisionCount: 12,
      unresolvedIssueCount: 0,
      complianceGapCount: 1,
      faithfulnessSupportRatio: null,
      usedDeterministicFallback: false,
      faithfulnessWarning: null,
      findingTraceConfidences: null,
      citations: [],
    },
  };
}
