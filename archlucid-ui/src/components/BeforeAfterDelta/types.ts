/**
 * Shared transport shapes for the three new `BeforeAfterDeltaPanel` variants
 * (top, sidebar, inline). Mirror the C# response in
 * `ArchLucid.Contracts.Pilots.RecentPilotRunDeltasResponse`.
 *
 * Kept narrow: the per-row shape only carries fields the panels actually render.
 * Adding a field server-side does not require updating these types until the
 * panel needs to display it.
 */

export type RecentPilotRunDeltaRow = {
  runId: string;
  requestId: string;
  runCreatedUtc: string;
  manifestCommittedUtc: string | null;
  timeToCommittedManifestTotalSeconds: number | null;
  totalFindings: number;
  topFindingSeverity: string | null;
  isDemoTenant: boolean;
};

export type RecentPilotRunDeltasPayload = {
  items: RecentPilotRunDeltaRow[];
  requestedCount: number;
  returnedCount: number;
  medianTotalFindings: number | null;
  medianTimeToCommittedManifestTotalSeconds: number | null;
};

/** Single-run delta payload (`GET /v1/pilots/runs/{runId}/pilot-run-deltas`) — only the fields the inline variant needs. */
export type SinglePilotRunDeltaPayload = {
  timeToCommittedManifestTotalSeconds: number | null;
  manifestCommittedUtc: string | null;
  findingsBySeverity: Array<{ severity: string; count: number }>;
};
