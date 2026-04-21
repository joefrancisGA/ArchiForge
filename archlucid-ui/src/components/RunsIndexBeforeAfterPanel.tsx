"use client";

import { BeforeAfterDeltaPanel } from "@/components/BeforeAfterDeltaPanel";

export type RunsIndexBeforeAfterPanelProps = {
  /** First run on the current page that already has a golden manifest (committed). */
  committedRunId: string | null;
};

/**
 * Shows the review-cycle delta card on the runs index when the tenant has at least one committed run on this page.
 * Uses the same `GET /v1/tenant/trial-status` + `GET /v1/pilots/runs/{runId}/pilot-run-deltas` path as the operator home
 * panel (server-side deltas are produced by `PilotRunDeltaComputer` via `PilotRunDeltasResponseMapper`).
 */
export function RunsIndexBeforeAfterPanel({ committedRunId }: RunsIndexBeforeAfterPanelProps) {
  if (committedRunId === null || committedRunId.length === 0) return null;

  return <BeforeAfterDeltaPanel runId={committedRunId} />;
}
