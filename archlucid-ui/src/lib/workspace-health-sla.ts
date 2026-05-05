import type { GovernanceApprovalRequest } from "@/types/governance-workflow";

/** API returns SLA fields; keep optional for older rows. */
export type GovernanceApprovalWithSla = GovernanceApprovalRequest;

function isPendingStatus(status: string): boolean {
  const s = status.trim().toLowerCase();

  return s === "draft" || s === "submitted";
}

function parseUtc(iso: string | null | undefined): number | null {
  if (iso === null || iso === undefined || iso.trim().length === 0) {
    return null;
  }

  const t = Date.parse(iso);

  return Number.isNaN(t) ? null : t;
}

export type WorkspaceHealthSlaStats = {
  overduePendingCount: number;
  onTrackPendingWithSlaCount: number;
  /** Share of recent terminal decisions with SLA that were reviewed on or before deadline; null if none eligible. */
  onTimeDecisionRate: number | null;
  onTimeEligibleDecisions: number;
  onTimeMetDecisions: number;
};

/**
 * SLA posture from governance dashboard lists — UI-only aggregation; server remains authoritative.
 */
export function computeWorkspaceHealthSlaStats(
  pendingApprovals: GovernanceApprovalWithSla[],
  recentDecisions: GovernanceApprovalWithSla[],
  nowMs: number = Date.now(),
): WorkspaceHealthSlaStats {
  let overduePendingCount = 0;
  let onTrackPendingWithSlaCount = 0;

  for (const row of pendingApprovals) {
    if (!isPendingStatus(row.status)) {
      continue;
    }

    const deadline = parseUtc(row.slaDeadlineUtc);

    if (deadline === null) {
      continue;
    }

    if (nowMs > deadline) {
      overduePendingCount++;
    } else {
      onTrackPendingWithSlaCount++;
    }
  }

  let onTimeEligibleDecisions = 0;
  let onTimeMetDecisions = 0;

  for (const row of recentDecisions) {
    const reviewed = parseUtc(row.reviewedUtc);
    const deadline = parseUtc(row.slaDeadlineUtc);

    if (reviewed === null || deadline === null) {
      continue;
    }

    onTimeEligibleDecisions++;

    if (reviewed <= deadline) {
      onTimeMetDecisions++;
    }
  }

  const onTimeDecisionRate =
    onTimeEligibleDecisions > 0 ? onTimeMetDecisions / onTimeEligibleDecisions : null;

  return {
    overduePendingCount,
    onTrackPendingWithSlaCount,
    onTimeDecisionRate,
    onTimeEligibleDecisions,
    onTimeMetDecisions,
  };
}
