import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

vi.mock("@/lib/api", () => ({
  dryRunPolicyPack: vi.fn(),
}));

import { dryRunPolicyPack } from "@/lib/api";
import {
  POLICY_PACK_DRY_RUN_DEFAULT_PAGE_SIZE,
  POLICY_PACK_DRY_RUN_MAX_PAGE_SIZE,
  type PolicyPackDryRunResponse,
} from "@/types/policy-pack-dry-run";

import { GovernanceDryRunModal } from "./GovernanceDryRunModal";

const mockDryRun = vi.mocked(dryRunPolicyPack);

describe("GovernanceDryRunModal", () => {
  const policyPackId = "policy-pack-1";

  it("default page size is 20 (owner Q38, 2026-04-23)", () => {
    expect(POLICY_PACK_DRY_RUN_DEFAULT_PAGE_SIZE).toBe(20);
  });

  it("server-side cap is 100 (owner Q38, 2026-04-23)", () => {
    expect(POLICY_PACK_DRY_RUN_MAX_PAGE_SIZE).toBe(100);
  });

  it("opens the modal and prefills the page-size input with 20", () => {
    render(<GovernanceDryRunModal policyPackId={policyPackId} />);

    fireEvent.click(screen.getByTestId("open-dry-run-modal"));

    const pageSize = screen.getByTestId("dry-run-page-size") as HTMLInputElement;
    expect(pageSize.value).toBe("20");
  });

  it("submits the dry-run payload and renders the redacted JSON marker", async () => {
    const response: PolicyPackDryRunResponse = {
      policyPackId,
      evaluatedUtc: "2026-04-24T00:00:00.000Z",
      page: 1,
      pageSize: POLICY_PACK_DRY_RUN_DEFAULT_PAGE_SIZE,
      totalRequestedRuns: 1,
      returnedRuns: 1,
      proposedThresholdsRedactedJson: '{"maxCriticalFindings":"0","note":"contact [REDACTED]"}',
      deltaCounts: { evaluated: 1, wouldBlock: 1, wouldAllow: 0, runMissing: 0 },
      items: [
        {
          runId: "run-001",
          runMissing: false,
          findingsBySeverity: [{ severity: "Critical", count: 2 }],
          thresholdOutcomes: [
            { key: "maxCriticalFindings", proposedValue: 0, actualValue: 2, wouldBreach: true },
          ],
          wouldBlock: true,
        },
      ],
    };

    mockDryRun.mockResolvedValueOnce(response);

    render(<GovernanceDryRunModal policyPackId={policyPackId} />);

    fireEvent.click(screen.getByTestId("open-dry-run-modal"));

    const thresholdsTextarea = screen.getByTestId("dry-run-thresholds-json") as HTMLTextAreaElement;
    fireEvent.change(thresholdsTextarea, {
      target: { value: '{"maxCriticalFindings":"0","note":"contact alice@example.com"}' },
    });

    const runIdsInput = screen.getByTestId("dry-run-run-ids") as HTMLInputElement;
    fireEvent.change(runIdsInput, { target: { value: "run-001" } });

    fireEvent.click(screen.getByTestId("dry-run-submit"));

    await waitFor(() => {
      expect(mockDryRun).toHaveBeenCalledTimes(1);
    });

    expect(mockDryRun).toHaveBeenCalledWith(
      policyPackId,
      {
        proposedThresholds: {
          maxCriticalFindings: "0",
          note: "contact alice@example.com",
        },
        evaluateAgainstRunIds: ["run-001"],
      },
      { pageSize: POLICY_PACK_DRY_RUN_DEFAULT_PAGE_SIZE, page: 1 },
    );

    const redactedBlock = await screen.findByTestId("dry-run-redacted-json");
    expect(redactedBlock.textContent).toContain("[REDACTED]");

    const resultPageSize = screen.getByTestId("dry-run-result-page-size");
    expect(resultPageSize.textContent).toBe(String(POLICY_PACK_DRY_RUN_DEFAULT_PAGE_SIZE));
  });

  it("shows a validation error when run-ids list is empty", async () => {
    render(<GovernanceDryRunModal policyPackId={policyPackId} />);

    fireEvent.click(screen.getByTestId("open-dry-run-modal"));
    fireEvent.click(screen.getByTestId("dry-run-submit"));

    const error = await screen.findByTestId("dry-run-error");
    expect(error.textContent).toMatch(/at least one run id/i);
    expect(mockDryRun).not.toHaveBeenCalled();
  });
});
