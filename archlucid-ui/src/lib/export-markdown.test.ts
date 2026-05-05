import { describe, expect, it } from "vitest";

import {
  buildGoldenManifestMarkdownFilename,
  formatGoldenManifestMarkdown,
  isUsableGoldenManifestExportJson,
} from "./export-markdown";
import type { ManifestSummary } from "@/types/authority";

describe("isUsableGoldenManifestExportJson", () => {
  it("rejects placeholders and empty objects", () => {
    expect(isUsableGoldenManifestExportJson(null)).toBe(false);
    expect(isUsableGoldenManifestExportJson(undefined)).toBe(false);
    expect(isUsableGoldenManifestExportJson({})).toBe(false);
    expect(isUsableGoldenManifestExportJson({ demo: true })).toBe(false);
  });

  it("accepts manifest-like objects", () => {
    expect(isUsableGoldenManifestExportJson({ runId: "r1", manifestId: "m1" })).toBe(true);
  });
});

describe("formatGoldenManifestMarkdown", () => {
  it("renders ManifestDocument-shaped JSON with required sections", () => {
    const doc = {
      manifestId: "m1",
      runId: "r1",
      ruleSetId: "rules",
      ruleSetVersion: "1.0",
      manifestHash: "h1",
      metadata: { changeDescription: "Ship faster with safer defaults." },
      assumptions: ["Assume regional Azure footprint."],
      topology: {
        selectedPatterns: ["Hub-spoke"],
        services: [
          {
            serviceId: "s1",
            serviceName: "API",
            serviceType: 0,
            runtimePlatform: 0,
            purpose: "Ingress",
          },
        ],
      },
      security: {
        controls: [{ controlName: "TLS", status: "Required", impact: "In transit" }],
        gaps: [],
      },
      decisions: [
        {
          decisionId: "d1",
          title: "Private endpoints",
          category: "Security",
          rationale: "Reduce attack surface.",
        },
      ],
    };

    const md = formatGoldenManifestMarkdown(doc);

    expect(md).toContain("# Ship faster with safer defaults.");
    expect(md).toContain("## Objectives");
    expect(md).toContain("## Architecture overview");
    expect(md).toContain("## Component breakdown");
    expect(md).toContain("### Services");
    expect(md).toContain("**API**");
    expect(md).toContain("## Security model");
    expect(md).toContain("### Controls");
    expect(md).toContain("**TLS**");
    expect(md).toContain("### Architecture decisions");
    expect(md).toContain("Private endpoints");
  });

  it("renders sandbox golden-manifest v1 JSON (highlights + summary)", () => {
    const sandbox = {
      schemaVersion: "archlucid.golden-manifest.v1",
      systemName: "Claims API",
      environment: "Production",
      cloudProvider: "Azure",
      status: "Committed",
      summary: {
        decisionCount: 3,
        warningCount: 1,
        unresolvedIssueCount: 0,
        costPosture: "Within budget.",
      },
      highlights: [
        {
          decisionId: "x",
          title: "Use private link",
          category: "Security",
          disposition: "Accepted",
          rationale: "Operator-only access.",
        },
      ],
      warnings: [{ code: "W1", message: "Add probes." }],
    };

    const md = formatGoldenManifestMarkdown(sandbox);

    expect(md).toContain("# Claims API");
    expect(md).toContain("## Objectives");
    expect(md).toContain("**Decisions recorded:** 3");
    expect(md).toContain("## Component breakdown");
    expect(md).toContain("### Use private link");
    expect(md).toContain("## Security model");
    expect(md).toContain("**W1:** Add probes.");
  });

  it("falls back to manifest summary when JSON is missing", () => {
    const summary: ManifestSummary = {
      manifestId: "m9",
      runId: "r9",
      createdUtc: "2026-01-01T00:00:00Z",
      manifestHash: "hash",
      ruleSetId: "p1",
      ruleSetVersion: "2.0",
      status: "Committed",
      decisionCount: 4,
      warningCount: 1,
      unresolvedIssueCount: 0,
      operatorSummary: "One-line operator summary.",
    };

    const md = formatGoldenManifestMarkdown(null, {
      runId: "r9",
      manifestSummaryFallback: summary,
    });

    expect(md).toContain("# Architecture manifest summary");
    expect(md).toContain("**Decisions:** 4");
    expect(md).toContain("One-line operator summary.");
  });

  it("returns a clear message when nothing is available", () => {
    const md = formatGoldenManifestMarkdown({ demo: true });

    expect(md).toContain("Manifest JSON was not available");
  });
});

describe("buildGoldenManifestMarkdownFilename", () => {
  it("sanitizes run id for download name", () => {
    expect(buildGoldenManifestMarkdownFilename("run/a#b", "m1")).toBe("golden-manifest-run-a-b.md");
  });
});
