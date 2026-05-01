import type { RecentPilotRunDeltaRow } from "./types";

/**
 * Picks the most recent prior committed run for the same architecture request as **`current`**,
 * using **`manifestCommittedUtc`** ordering (same rules as the inline delta panel).
 */
export function pickPriorForSameRequest(
  current: RecentPilotRunDeltaRow,
  rows: RecentPilotRunDeltaRow[],
): RecentPilotRunDeltaRow | null {
  if (current.manifestCommittedUtc === null) {
    return null;
  }

  const currentCommittedAt = Date.parse(current.manifestCommittedUtc);

  if (Number.isNaN(currentCommittedAt)) {
    return null;
  }

  const candidates = rows.filter((r) => {
    if (r.runId === current.runId) {
      return false;
    }

    if (r.requestId === "" || r.requestId !== current.requestId) {
      return false;
    }

    if (r.manifestCommittedUtc === null) {
      return false;
    }

    const t = Date.parse(r.manifestCommittedUtc);

    if (Number.isNaN(t)) {
      return false;
    }

    return t < currentCommittedAt;
  });

  if (candidates.length === 0) {
    return null;
  }

  return candidates.reduce((latest, row) => {
    const a = Date.parse(latest.manifestCommittedUtc ?? "");
    const b = Date.parse(row.manifestCommittedUtc ?? "");

    return b > a ? row : latest;
  });
}
