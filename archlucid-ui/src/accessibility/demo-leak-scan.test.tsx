/**
 * Demo-leak scan: rendered HTML must not contain strings that expose internal implementation
 * language, fixture identifiers, or operator-only API copy to end users.
 *
 * This file tests the specific components that were identified as sources of leakage. Add new
 * cases here whenever a page or component is updated to remove a forbidden string.
 */
import { render } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { LegacyRunComparisonView } from "@/components/compare/LegacyRunComparisonView";
import { RunProvenanceInline } from "@/components/RunProvenanceInline";
import type { RunComparison } from "@/types/authority";
import type { RunSummary } from "@/types/authority";

/**
 * Asserts that the rendered HTML of a container does not contain any of the forbidden strings.
 * Uses case-insensitive matching for each pattern.
 */
function expectNoLeaks(container: HTMLElement, forbidden: string[]): void {
  const html = container.innerHTML;

  for (const token of forbidden) {
    expect(html).not.toContain(token);
  }
}

const GLOBALLY_FORBIDDEN = [
  "manifest-left-fixture",
  "manifest-right-fixture",
  "sha256:left",
  "sha256:right",
  "AP-gated",
  "execute+",
  "golden manifest",
  "provenance-full",
  "decision-subgraph",
  "node-neighborhood",
  "operator access",
  "api-gated",
];

describe("demo-leak scan — LegacyRunComparisonView", () => {
  const fixtureResult: RunComparison = {
    leftRunId: "run-a",
    rightRunId: "run-b",
    runLevelDiffCount: 2,
    runLevelDiffs: [
      {
        diffKind: "Changed",
        section: "metadata",
        key: "status",
        beforeValue: "Draft",
        afterValue: "Finalized",
        notes: null,
      },
    ],
    manifestComparison: {
      leftManifestId: "manifest-left-fixture",
      rightManifestId: "manifest-right-fixture",
      leftManifestHash: "sha256:left",
      rightManifestHash: "sha256:right",
      addedCount: 1,
      removedCount: 0,
      changedCount: 3,
      diffs: [],
    },
  };

  it("does not render fixture manifest IDs as visible text", () => {
    const { container } = render(<LegacyRunComparisonView result={fixtureResult} />);

    expectNoLeaks(container, [
      "manifest-left-fixture",
      "manifest-right-fixture",
    ]);
  });

  it("does not render fixture hash placeholders as visible text", () => {
    const { container } = render(<LegacyRunComparisonView result={fixtureResult} />);

    expectNoLeaks(container, ["sha256:left", "sha256:right"]);
  });

  it("does not expose 'legacy' in primary section headings", () => {
    const { container } = render(<LegacyRunComparisonView result={fixtureResult} />);
    const h3 = container.querySelector("h3");

    expect(h3?.textContent?.toLowerCase()).not.toContain("legacy");
  });
});

describe("demo-leak scan — RunProvenanceInline", () => {
  const minimalRun: RunSummary = {
    runId: "test-run",
    projectId: "default",
    createdUtc: new Date().toISOString(),
    hasContextSnapshot: true,
    hasGraphSnapshot: false,
    hasFindingsSnapshot: true,
    hasGoldenManifest: true,
    hasArtifactBundle: false,
    description: "Test run",
  };

  it("uses product-facing aria-label (Review trail status)", () => {
    const { container } = render(<RunProvenanceInline run={minimalRun} />);
    const ul = container.querySelector("ul");

    expect(ul?.getAttribute("aria-label")).toBe("Review trail status");
  });

  it("does not expose 'provenance snapshot' as visible text", () => {
    const { container } = render(<RunProvenanceInline run={minimalRun} />);

    expectNoLeaks(container, ["Provenance snapshot", "provenance snapshot"]);
  });
});

describe("demo-leak scan — global forbidden strings", () => {
  it("LegacyRunComparisonView does not contain any globally forbidden token", () => {
    const result: RunComparison = {
      leftRunId: "run-a",
      rightRunId: "run-b",
      runLevelDiffs: [],
      manifestComparison: {
        leftManifestId: "manifest-left-fixture",
        rightManifestId: "manifest-right-fixture",
        leftManifestHash: "sha256:left",
        rightManifestHash: "sha256:right",
        addedCount: 0,
        removedCount: 0,
        changedCount: 0,
        diffs: [],
      },
    };

    const { container } = render(<LegacyRunComparisonView result={result} />);

    expectNoLeaks(container, GLOBALLY_FORBIDDEN);
  });
});
