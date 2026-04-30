import type { ManifestSummary } from "@/types/authority";

/**
 * Pilot ROI / scorecard package CTA is shown only when the run has a golden manifest **and** we have loaded a manifest
 * summary whose API status is committed (finalized). Avoids advertising sponsor exports while the manifest is still a
 * draft or while summary fetch failed (API remains authoritative for commit truth).
 */
export function isManifestCommittedForPilotScorecardPackage(manifestSummary: ManifestSummary | null): boolean {
  if (manifestSummary === null) return false;

  return /^committed$/i.test((manifestSummary.status ?? "").trim());
}
