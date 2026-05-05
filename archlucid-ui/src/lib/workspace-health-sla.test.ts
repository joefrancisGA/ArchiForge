import { describe, expect, it } from "vitest";

import { computeWorkspaceHealthSlaStats } from "@/lib/workspace-health-sla";

describe("computeWorkspaceHealthSlaStats", () => {
  const now = Date.parse("2026-05-05T12:00:00.000Z");

  it("counts overdue pending with SLA", () => {
    const stats = computeWorkspaceHealthSlaStats(
      [
        {
          approvalRequestId: "a",
          runId: "r",
          manifestVersion: "1",
          sourceEnvironment: "dev",
          targetEnvironment: "test",
          status: "Submitted",
          requestedBy: "u",
          reviewedBy: null,
          requestComment: null,
          reviewComment: null,
          requestedUtc: "2026-05-01T00:00:00.000Z",
          reviewedUtc: null,
          slaDeadlineUtc: "2026-05-04T00:00:00.000Z",
        },
      ],
      [],
      now,
    );

    expect(stats.overduePendingCount).toBe(1);
    expect(stats.onTrackPendingWithSlaCount).toBe(0);
  });

  it("computes on-time rate for decisions with SLA", () => {
    const stats = computeWorkspaceHealthSlaStats(
      [],
      [
        {
          approvalRequestId: "b",
          runId: "r",
          manifestVersion: "1",
          sourceEnvironment: "dev",
          targetEnvironment: "test",
          status: "Approved",
          requestedBy: "u",
          reviewedBy: "v",
          requestComment: null,
          reviewComment: null,
          requestedUtc: "2026-05-01T00:00:00.000Z",
          reviewedUtc: "2026-05-02T00:00:00.000Z",
          slaDeadlineUtc: "2026-05-03T00:00:00.000Z",
        },
        {
          approvalRequestId: "c",
          runId: "r2",
          manifestVersion: "1",
          sourceEnvironment: "dev",
          targetEnvironment: "test",
          status: "Rejected",
          requestedBy: "u",
          reviewedBy: "v",
          requestComment: null,
          reviewComment: null,
          requestedUtc: "2026-05-01T00:00:00.000Z",
          reviewedUtc: "2026-05-10T00:00:00.000Z",
          slaDeadlineUtc: "2026-05-03T00:00:00.000Z",
        },
      ],
      now,
    );

    expect(stats.onTimeEligibleDecisions).toBe(2);
    expect(stats.onTimeMetDecisions).toBe(1);
    expect(stats.onTimeDecisionRate).toBe(0.5);
  });

  it("returns null on-time rate when no eligible decisions", () => {
    const stats = computeWorkspaceHealthSlaStats([], [], now);

    expect(stats.onTimeDecisionRate).toBeNull();
  });
});
