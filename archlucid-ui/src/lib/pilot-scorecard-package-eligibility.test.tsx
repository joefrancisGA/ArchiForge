import { render, screen } from "@testing-library/react";
import type { ReactNode } from "react";
import { describe, expect, it } from "vitest";

import { isManifestCommittedForPilotScorecardPackage } from "@/lib/pilot-scorecard-package-eligibility";
import type { ManifestSummary } from "@/types/authority";

/** Mirrors run detail page gating for {@link EmailRunToSponsorBanner} — keep rules in sync with `runs/[runId]/page.tsx`. */
function PilotScorecardPackageCtaGate(props: {
  readonly manifestId: string | null | undefined;
  readonly manifestSummary: ManifestSummary | null;
}): ReactNode {
  if (!props.manifestId) {
    return null;
  }

  if (!isManifestCommittedForPilotScorecardPackage(props.manifestSummary)) {
    return null;
  }

  return <div data-testid="pilot-scorecard-package-cta-slot">Pilot scorecard package CTA</div>;
}

describe("isManifestCommittedForPilotScorecardPackage", () => {
  it("returns false when manifest summary is null", () => {
    expect(isManifestCommittedForPilotScorecardPackage(null)).toBe(false);
  });

  it("returns false when status is not committed", () => {
    expect(
      isManifestCommittedForPilotScorecardPackage({
        manifestId: "m1",
        runId: "r1",
        createdUtc: "",
        manifestHash: "",
        ruleSetId: "",
        ruleSetVersion: "",
        decisionCount: 0,
        warningCount: 0,
        unresolvedIssueCount: 0,
        status: "Draft",
      }),
    ).toBe(false);
  });

  it("returns true when status is committed (case insensitive)", () => {
    expect(
      isManifestCommittedForPilotScorecardPackage({
        manifestId: "m1",
        runId: "r1",
        createdUtc: "",
        manifestHash: "",
        ruleSetId: "",
        ruleSetVersion: "",
        decisionCount: 0,
        warningCount: 0,
        unresolvedIssueCount: 0,
        status: "Committed",
      }),
    ).toBe(true);

    expect(
      isManifestCommittedForPilotScorecardPackage({
        manifestId: "m1",
        runId: "r1",
        createdUtc: "",
        manifestHash: "",
        ruleSetId: "",
        ruleSetVersion: "",
        decisionCount: 0,
        warningCount: 0,
        unresolvedIssueCount: 0,
        status: "committed",
      }),
    ).toBe(true);
  });
});

describe("Pilot scorecard package CTA visibility (run detail mirror)", () => {
  const committed: ManifestSummary = {
    manifestId: "m1",
    runId: "r1",
    createdUtc: "",
    manifestHash: "",
    ruleSetId: "",
    ruleSetVersion: "",
    decisionCount: 0,
    warningCount: 0,
    unresolvedIssueCount: 0,
    status: "Committed",
  };

  it("hides when run has no golden manifest id", () => {
    render(<PilotScorecardPackageCtaGate manifestId={null} manifestSummary={committed} />);

    expect(screen.queryByTestId("pilot-scorecard-package-cta-slot")).toBeNull();
  });

  it("hides when manifest summary failed to load", () => {
    render(<PilotScorecardPackageCtaGate manifestId="manifest-x" manifestSummary={null} />);

    expect(screen.queryByTestId("pilot-scorecard-package-cta-slot")).toBeNull();
  });

  it("hides when manifest is not committed", () => {
    render(
      <PilotScorecardPackageCtaGate
        manifestId="manifest-x"
        manifestSummary={{ ...committed, status: "Pending" }}
      />,
    );

    expect(screen.queryByTestId("pilot-scorecard-package-cta-slot")).toBeNull();
  });

  it("shows after successful commit when summary is committed", () => {
    render(<PilotScorecardPackageCtaGate manifestId="manifest-x" manifestSummary={committed} />);

    expect(screen.getByTestId("pilot-scorecard-package-cta-slot")).toBeVisible();
  });
});
