/**
 * Client-side parse of persisted evolution `outcomeJson`: 60R-v2 envelope or legacy flat shadow DTO.
 */

export type EvolutionShadowOutcome = {
  error?: string | null;
  architectureRunId: string;
  evaluationMode: string;
  runStatus?: string | null;
  manifestVersion?: string | null;
  hasManifest: boolean;
  summaryLength: number;
  warningCount: number;
};

export type ParsedEvolutionOutcome =
  | { kind: "v2"; shadow: EvolutionShadowOutcome }
  | { kind: "legacy"; shadow: EvolutionShadowOutcome }
  | { kind: "empty" }
  | { kind: "invalid" };

function readShadow(raw: Record<string, unknown>): EvolutionShadowOutcome | null {
  const architectureRunId = raw.architectureRunId;
  const evaluationMode = raw.evaluationMode;
  const hasManifest = raw.hasManifest;
  const summaryLength = raw.summaryLength;
  const warningCount = raw.warningCount;

  if (typeof architectureRunId !== "string" || typeof evaluationMode !== "string") {
    return null;
  }

  if (typeof hasManifest !== "boolean" || typeof summaryLength !== "number" || typeof warningCount !== "number") {
    return null;
  }

  const error =
    raw.error === null || raw.error === undefined
      ? null
      : typeof raw.error === "string"
        ? raw.error
        : null;
  const runStatus =
    raw.runStatus === null || raw.runStatus === undefined
      ? null
      : typeof raw.runStatus === "string"
        ? raw.runStatus
        : null;
  const manifestVersion =
    raw.manifestVersion === null || raw.manifestVersion === undefined
      ? null
      : typeof raw.manifestVersion === "string"
        ? raw.manifestVersion
        : null;

  return {
    error,
    architectureRunId,
    evaluationMode,
    runStatus,
    manifestVersion,
    hasManifest,
    summaryLength,
    warningCount,
  };
}

export function parseEvolutionOutcomeJson(outcomeJson: string): ParsedEvolutionOutcome {
  if (outcomeJson === null || outcomeJson === undefined) {
    return { kind: "empty" };
  }

  const trimmed = outcomeJson.trim();

  if (trimmed.length === 0) {
    return { kind: "empty" };
  }

  try {
    const raw: unknown = JSON.parse(trimmed);

    if (raw === null || typeof raw !== "object") {
      return { kind: "invalid" };
    }

    const o = raw as Record<string, unknown>;

    if (o.schemaVersion === "60R-v2" && o.shadow !== null && typeof o.shadow === "object") {
      const shadow = readShadow(o.shadow as Record<string, unknown>);

      if (shadow === null) {
        return { kind: "invalid" };
      }

      return { kind: "v2", shadow };
    }

    const legacy = readShadow(o);

    if (legacy === null) {
      return { kind: "invalid" };
    }

    return { kind: "legacy", shadow: legacy };
  } catch {
    return { kind: "invalid" };
  }
}
