import { render, screen, within } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import {
  approvalRequestPrimaryLabel,
  GovernanceApprovalInspectorPreview,
} from "./GovernanceApprovalInspectorPreview";

import type { GovernanceApprovalRequest } from "@/types/governance-workflow";

const sample: GovernanceApprovalRequest = {
  approvalRequestId: "ar-1",
  runId: "00000000-0000-0000-0000-000000000099",
  manifestVersion: "v1",
  sourceEnvironment: "dev",
  targetEnvironment: "prod",
  status: "Submitted",
  requestedBy: "alice",
  reviewedBy: null,
  requestComment: null,
  reviewComment: null,
  requestedUtc: "2026-01-15T12:00:00.000Z",
  reviewedUtc: null,
};

describe("approvalRequestPrimaryLabel", () => {
  it("uses environment route as title", () => {
    expect(approvalRequestPrimaryLabel(sample)).toBe("dev → prod");
  });
});

describe("GovernanceApprovalInspectorPreview", () => {
  it("renders governance StatusPill and run link", () => {
    render(<GovernanceApprovalInspectorPreview request={sample} />);
    const root = screen.getByTestId("governance-approval-inspector-preview");

    expect(within(root).getByLabelText("Governance status: Submitted")).toBeInTheDocument();
    expect(within(root).getByRole("link", { name: sample.runId })).toHaveAttribute(
      "href",
      `/reviews/${encodeURIComponent(sample.runId)}`,
    );
  });
});
