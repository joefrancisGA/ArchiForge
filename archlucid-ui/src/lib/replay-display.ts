/** Human-readable labels for authority replay modes (matches API enum names). */
const REPLAY_MODE_LABELS: Record<string, string> = {
  ReconstructOnly:
    "Reconstruct only — replay authority steps without rebuilding the golden manifest or artifacts.",
  RebuildManifest:
    "Rebuild manifest — replay and regenerate the golden manifest from stored inputs.",
  RebuildArtifacts:
    "Rebuild artifacts — replay through manifest regeneration and artifact synthesis where applicable.",
};

/** Returns a short operator-facing sentence for the replay mode, or the raw mode string if unknown. */
export function replayModeLabel(mode: string): string {
  const label = REPLAY_MODE_LABELS[mode];

  if (label !== undefined) {
    return label;
  }

  return mode;
}

/** Sorts validation note lines for a stable bullet list across reloads. */
export function sortReplayNotes(notes: string[]): string[] {
  return [...notes].sort((a, b) => a.localeCompare(b, "en"));
}
