import { render, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

import { FindingInspectView } from "../FindingInspectView";

vi.mock("@/components/OperatorApiProblem", () => ({
  OperatorApiProblem: ({ fallbackMessage }: { fallbackMessage: string }) => (
    <div data-testid="api-problem-mock">{fallbackMessage}</div>
  ),
}));

vi.mock("@/components/OperatorEvidenceLimitsFooter", () => ({
  OperatorEvidenceLimitsFooter: () => <div data-testid="operator-evidence-limits-footer-stub" />,
}));

describe("FindingInspectView", () => {
  it("renders all four labeled sections when payload matches route run", () => {
    const { container } = render(
      <FindingInspectView
        runId="6e8c4a10-2b1f-4c9a-9d3e-10b2a4f0c501"
        decodedFindingId="f-1"
        failure={null}
        payload={{
          findingId: "f-1",
          typedPayload: { severity: "High" },
          decisionRuleId: "rule-a",
          decisionRuleName: "Rule A",
          evidence: [{ artifactId: null, lineRange: "12-20", excerpt: "node-x" }],
          auditRowId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
          runId: "6e8c4a10-2b1f-4c9a-9d3e-10b2a4f0c501",
          manifestVersion: "v1",
        }}
      />,
    );

    const view = within(container);

    expect(view.getByRole("heading", { name: "Technical inspection" })).toBeTruthy();
    expect(view.getByText("AI Audit Inspection")).toBeTruthy();
    expect(view.getByRole("heading", { name: "Why this matters" })).toBeTruthy();
    expect(view.getByRole("heading", { name: "Evidence" })).toBeTruthy();
    expect(view.getByRole("heading", { name: "Recommended action" })).toBeTruthy();
    expect(view.getByRole("heading", { name: "Audit" })).toBeTruthy();
    expect(view.getByText("rule-a")).toBeTruthy();
    expect(view.getByText("node-x")).toBeTruthy();
  });
});
