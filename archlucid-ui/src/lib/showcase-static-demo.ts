import type { DemoCommitPagePreviewResponse } from "@/types/demo-preview";

/** Public-marketing slug for demos and screenshots — no fixture-style token. */
export const SHOWCASE_STATIC_DEMO_RUN_ID = "claims-intake-modernization";

/** Canonical manifest UUID for the static showcase payload (matches operator mock + marketing body). */
export const SHOWCASE_STATIC_DEMO_MANIFEST_ID = "a1c2e3f4-a5b6-7890-abcd-ef1234567890";

/**
 * Deterministic finding id for deep links from manifest “Related findings” (aligned with demo narrative;
 * resolves when the API seeds this finding for the showcase run).
 */
export const SHOWCASE_STATIC_DEMO_PRIMARY_FINDING_ID = "phi-minimization-risk";

/**
 * Canonical counts for the static Claims Intake demo spine — Run detail, manifest summary, and showcase should
 * reflect the same numbers when serving this payload (see {@link getShowcaseStaticDemoPayload}).
 */
export const SHOWCASE_STATIC_DEMO_SPINE_COUNTS = {
  findingCount: 9,
  warningCount: 1,
  decisionCount: 12,
} as const;

/**
 * Curated synopses for the static Claims Intake manifest detail page (no list API on summary).
 * Keep length aligned with `manifest.decisionCount` / `warningCount` in this payload.
 */
export const SHOWCASE_STATIC_DEMO_DECISION_SYNOPSES: readonly string[] = [
  "Intake API remains system-of-record; channel adapters are stateless facades.",
  "PHI is classified at ingress; audit lineage follows the member correlation ID.",
  "Peak-load buffering uses bounded queues with explicit back-pressure to adjudication.",
  "Manual rework queues are capped; overflow routes to a supervised exception path.",
  "Third-party OCR is optional; human confirm gates apply before downstream commit.",
  "Adjudication handoff uses signed event envelopes with idempotent consumers.",
  "Retention aligns to enterprise policy; cold paths avoid negotiable shorter windows.",
  "Observability spans intake latency, queue depth, and exception-rate SLOs.",
  "Disaster recovery favors replay-from-journal over dual-active intake writers.",
  "Feature flags scope rollout by cohort; kill switches are tested each release.",
  "Data residency constraints are enforced at the storage account boundary.",
  "Sponsor KPI pack ties modernization outcomes to defensible operational metrics.",
];

/** Single curated warning matching `manifest.warningCount` for the static showcase. */
export const SHOWCASE_STATIC_DEMO_WARNING_SYNOPSES: readonly string[] = [
  "Unstructured attachments may still bypass the OCR path during peak; monitor exception volume weekly.",
];

const GENERATED_UTC = "2026-04-23T09:15:00.000Z";

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
      projectId: "claims-intake-sample-workspace",
      description: "Claims Intake Modernization Review",
      createdUtc: "2026-01-10T09:15:22.000Z",
    },
    manifest: {
      manifestId: SHOWCASE_STATIC_DEMO_MANIFEST_ID,
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
        "Finalized Architecture Manifest for Claims Intake Modernization — integration boundaries, PHI handling posture, " +
        "and sponsor-facing KPIs consolidated for sign-off.",
    },
    authorityChain: {
      contextSnapshotId: "ctx-snapshot-01",
      graphSnapshotId: "graph-snapshot-01",
      findingsSnapshotId: "find-snapshot-01",
      goldenManifestId: SHOWCASE_STATIC_DEMO_MANIFEST_ID,
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
        eventId: "evt-pipeline-request-created",
        occurredUtc: "2026-01-10T09:15:22.000Z",
        eventType: "RunStarted",
        actorUserName: "Jordan Lee",
        correlationId: "corr-intake-demo-request",
      },
      {
        eventId: "evt-pipeline-context",
        occurredUtc: "2026-01-14T21:42:10.000Z",
        eventType: "context.snapshot.created",
        actorUserName: "ArchLucid pipeline",
        correlationId: "corr-intake-demo-ctx",
      },
      {
        eventId: "evt-pipeline-graph",
        occurredUtc: "2026-01-14T21:51:33.000Z",
        eventType: "graph.snapshot.created",
        actorUserName: "ArchLucid pipeline",
        correlationId: "corr-intake-demo-graph",
      },
      {
        eventId: "evt-pipeline-findings",
        occurredUtc: "2026-01-14T22:03:18.000Z",
        eventType: "findings.snapshot.created",
        actorUserName: "ArchLucid pipeline",
        correlationId: "corr-intake-demo-findings",
      },
      {
        eventId: "evt-pipeline-manifest-finalized",
        occurredUtc: "2026-01-14T22:07:58.000Z",
        eventType: "finalize.run",
        actorUserName: "Taylor Morgan",
        correlationId: "corr-intake-demo-manifest",
      },
      {
        eventId: "evt-pipeline-bundle",
        occurredUtc: "2026-01-14T22:09:44.000Z",
        eventType: "artifact.bundle.created",
        actorUserName: "ArchLucid pipeline",
        correlationId: "corr-intake-demo-bundle",
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
      citations: [
        { kind: "Manifest", id: SHOWCASE_STATIC_DEMO_MANIFEST_ID, label: "Committed architecture manifest", runId },
        {
          kind: "GraphSnapshot",
          id: "graph-snapshot-01",
          label: "Review-trail graph — PHI minimization and control coverage",
          runId,
        },
        { kind: "ContextSnapshot", id: "ctx-snapshot-01", label: "Context snapshot — intake boundaries", runId },
      ],
    },
  };
}
