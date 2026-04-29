import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import {
  DemoPreviewFriendlyUnavailable,
  DemoPreviewMarketingBody,
  DemoPreviewNotAvailable,
} from "./DemoPreviewMarketingBody";
import type { DemoCommitPagePreviewResponse } from "@/types/demo-preview";

const fixture: DemoCommitPagePreviewResponse = {
  generatedUtc: "2026-04-01T12:00:00.000Z",
  isDemoData: true,
  demoStatusMessage: "demo tenant — replace before publishing",
  run: {
    runId: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
    projectId: "default",
    description: "Fixture",
    createdUtc: "2026-03-15T08:00:00Z",
  },
  authorityChain: {
    contextSnapshotId: "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
    graphSnapshotId: null,
    findingsSnapshotId: null,
    goldenManifestId: "cccccccccccccccccccccccccccccccc",
    decisionTraceId: null,
    artifactBundleId: null,
  },
  manifest: {
    manifestId: "cccccccccccccccccccccccccccccccc",
    runId: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
    createdUtc: "2026-03-15T08:05:00Z",
    manifestHash: "mh",
    ruleSetId: "rs",
    ruleSetVersion: "v1",
    decisionCount: 2,
    warningCount: 1,
    unresolvedIssueCount: 0,
    status: "Committed",
    operatorSummary: "2 decisions, 1 warnings, 0 unresolved issues, status Committed",
  },
  artifacts: [
    {
      artifactId: "dddddddddddddddddddddddddddddddd",
      artifactType: "docx",
      name: "Architecture brief",
      format: "binary",
      createdUtc: "2026-03-15T08:06:00Z",
      contentHash: "abc123",
    },
  ],
  pipelineTimeline: [
    {
      eventId: "eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee",
      occurredUtc: "2026-03-15T08:04:00Z",
      eventType: "Commit",
      actorUserName: "demo",
      correlationId: "corr-1",
    },
  ],
  runExplanation: {
    explanation: {
      rawText: "",
      structured: null,
      confidence: null,
      provenance: null,
      summary: "Summary",
      keyDrivers: [],
      riskImplications: [],
      costImplications: [],
      complianceImplications: [],
      detailedNarrative: "Narrative",
    },
    themeSummaries: ["Theme A", "Theme B"],
    overallAssessment: "Healthy",
    riskPosture: "Moderate",
    findingCount: 3,
    decisionCount: 2,
    unresolvedIssueCount: 0,
    complianceGapCount: 0,
    citations: [
      { kind: "Manifest", id: "m-1", label: "manifest" },
      { kind: "Finding", id: "f-1", label: "finding" },
    ],
  },
};

describe("Demo preview marketing body", () => {
  it("renders all sections from a fixture payload", () => {
    render(<DemoPreviewMarketingBody payload={fixture} />);

    expect(screen.getByTestId("demo-preview-status-banner")).toHaveTextContent("demo tenant — replace before publishing");
    expect(screen.getByTestId("demo-preview-run")).toHaveTextContent("Fixture");
    expect(screen.getByTestId("demo-preview-authority-chain")).toHaveTextContent("Context snapshot captured");
    expect(screen.getByTestId("demo-preview-manifest-summary")).toHaveTextContent("Finalized");
    expect(screen.getByTestId("demo-preview-aggregate-explanation")).toHaveTextContent("Healthy");
    expect(screen.getByTestId("demo-preview-pipeline-timeline")).toHaveTextContent("Changes committed");
    expect(screen.getByTestId("demo-preview-artifacts")).toHaveTextContent("Architecture brief");
    expect(screen.getByTestId("demo-preview-footer")).toHaveTextContent("Powered by ArchLucid");
  });

  it("renders the not-available notice", () => {
    render(<DemoPreviewNotAvailable />);
    expect(screen.getByTestId("demo-preview-not-available")).toBeInTheDocument();
  });

  it("renders customer-safe friendly unavailable with example links", () => {
    render(<DemoPreviewFriendlyUnavailable />);
    expect(screen.getByTestId("demo-preview-friendly-unavailable")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /view example output/i })).toHaveAttribute(
      "href",
      "/showcase/claims-intake-modernization",
    );
  });

  it("does not render sponsor email banner or finalize controls", () => {
    render(<DemoPreviewMarketingBody payload={fixture} />);

    expect(screen.queryByTestId("email-run-to-sponsor-banner")).toBeNull();
    expect(screen.queryByRole("button", { name: /finalize manifest/i })).toBeNull();
  });
});
