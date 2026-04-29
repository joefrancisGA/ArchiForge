import type { GoldenManifestComparison } from "@/types/comparison";
import type { GraphViewModel } from "@/types/graph";
import type { ComparisonExplanation } from "@/types/explanation";
import type { PagedResponse } from "@/types/pagination";
import type {
  ArtifactDescriptor,
  ManifestSummary,
  ReplayResponse,
  RunComparison,
  RunDetail,
  RunSummary,
} from "@/types/authority";

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

/**
 * Ensures the runs list endpoint returned an array of objects with runId (malformed vs empty list).
 */
export function coerceRunSummaryList(
  data: unknown,
): { ok: true; items: RunSummary[] } | { ok: false; message: string } {
  if (!Array.isArray(data)) {
    return { ok: false, message: 'Expected a JSON array of runs; the API returned a non-array body.' };
  }

  const items: RunSummary[] = [];

  for (const row of data) {
    if (!isRecord(row) || typeof row.runId !== "string") {
      return {
        ok: false,
        message: "One or more run rows are missing a string runId; response shape may be outdated.",
      };
    }

    items.push(row as RunSummary);
  }

  return { ok: true, items };
}

/** Narrowed envelope without `Record<string, unknown>` — an index signature would keep paging fields typed as unknown. */
type LegacyOffsetPagedRunEnvelope = {
  totalCount: number;
  page: number;
  pageSize: number;
  hasMore: boolean;
};

function isLegacyOffsetPagedRunEnvelope(
  data: Record<string, unknown>,
): data is LegacyOffsetPagedRunEnvelope {
  return (
    typeof data.totalCount === "number" &&
    Number.isFinite(data.totalCount) &&
    typeof data.page === "number" &&
    Number.isFinite(data.page) &&
    typeof data.pageSize === "number" &&
    Number.isFinite(data.pageSize) &&
    typeof data.hasMore === "boolean"
  );
}

type CursorPagedRunEnvelope = {
  requestedTake: number;
  hasMore: boolean;
  /** Optional keyset token — validated in `coerceRunSummaryPaged`. */
  nextCursor?: unknown;
};

function isCursorPagedRunEnvelope(data: Record<string, unknown>): data is CursorPagedRunEnvelope {
  return typeof data.requestedTake === "number" && Number.isFinite(data.requestedTake) && typeof data.hasMore === "boolean";
}

function normalizeRunSummaryItems(rawItems: unknown): { ok: true; items: RunSummary[] } | { ok: false; message: string } {
  if (!Array.isArray(rawItems)) {
    return { ok: false, message: 'Paged runs response is missing an "items" array.' };
  }

  const items: RunSummary[] = [];

  for (const row of rawItems) {
    if (!isRecord(row) || typeof row.runId !== "string") {
      return {
        ok: false,
        message: "One or more paged run rows are missing a string runId.",
      };
    }

    items.push(row as RunSummary);
  }

  return { ok: true, items };
}

/** Lower bound on total rows so offset-style pagination UI can enable Next when the API uses keyset paging only. */
function keysetTotalCountLowerBound(page: number, pageSize: number, itemCount: number, hasMore: boolean): number {
  const prefixCount = (page - 1) * pageSize + itemCount;

  if (!hasMore) {
    return prefixCount;
  }

  return Math.max(prefixCount + 1, page * pageSize + 1);
}

/**
 * Ensures a paged runs response has an items array of run summaries and paging metadata.
 *
 * Accepts legacy offset pages (`totalCount`, `page`, `pageSize`) or keyset pages (`requestedTake`, `nextCursor`).
 */
export function coerceRunSummaryPaged(
  data: unknown,
  context?: { readonly page?: number },
):
  | { ok: true; value: PagedResponse<RunSummary> }
  | { ok: false; message: string } {
  if (!isRecord(data)) {
    return { ok: false, message: "Expected a JSON object for paged runs." };
  }

  const normalizedItems = normalizeRunSummaryItems(data.items);

  if (!normalizedItems.ok) {
    return normalizedItems;
  }

  const items = normalizedItems.items;

  if (isLegacyOffsetPagedRunEnvelope(data)) {
    return {
      ok: true,
      value: {
        items,
        totalCount: data.totalCount,
        page: data.page,
        pageSize: data.pageSize,
        hasMore: data.hasMore,
      },
    };
  }

  if (isCursorPagedRunEnvelope(data)) {
    if (data.nextCursor !== undefined && data.nextCursor !== null && typeof data.nextCursor !== "string") {
      return { ok: false, message: "Paged runs response has invalid nextCursor." };
    }

    const page = context?.page ?? 1;

    if (!Number.isFinite(page) || page < 1) {
      return { ok: false, message: "Paged runs context has invalid page." };
    }

    const pageSize = data.requestedTake;

    return {
      ok: true,
      value: {
        items,
        totalCount: keysetTotalCountLowerBound(page, pageSize, items.length, data.hasMore),
        page,
        pageSize,
        hasMore: data.hasMore,
        nextCursor:
          typeof data.nextCursor === "string" && data.nextCursor.length > 0 ? data.nextCursor : null,
      },
    };
  }

  return {
    ok: false,
    message:
      "Paged runs response is missing offset fields (totalCount, page, pageSize) or keyset fields (requestedTake).",
  };
}

/**
 * Ensures graph JSON has nodes and edges arrays (malformed vs empty graph).
 */
export function coerceGraphViewModel(
  data: unknown,
): { ok: true; value: GraphViewModel } | { ok: false; message: string } {
  if (!isRecord(data)) {
    return { ok: false, message: "Graph response was not a JSON object." };
  }

  if (!Array.isArray(data.nodes) || !Array.isArray(data.edges)) {
    return {
      ok: false,
      message: 'Graph response is missing "nodes" or "edges" arrays.',
    };
  }

  return { ok: true, value: data as GraphViewModel };
}

/**
 * Ensures GET api/compare payload has expected section arrays (partial parse failure).
 */
export function coerceGoldenManifestComparison(
  data: unknown,
): { ok: true; value: GoldenManifestComparison } | { ok: false; message: string } {
  if (!isRecord(data)) {
    return { ok: false, message: "Comparison response was not a JSON object." };
  }

  const needArrays = [
    "decisionChanges",
    "requirementChanges",
    "securityChanges",
    "topologyChanges",
    "costChanges",
    "summaryHighlights",
  ] as const;

  for (const key of needArrays) {
    if (!Array.isArray(data[key])) {
      return {
        ok: false,
        message: `Comparison response is missing or invalid "${key}" array.`,
      };
    }
  }

  if (typeof data.baseRunId !== "string" || typeof data.targetRunId !== "string") {
    return { ok: false, message: "Comparison response is missing baseRunId or targetRunId." };
  }

  return { ok: true, value: data as GoldenManifestComparison };
}

/**
 * Ensures replay POST body has validation + notes array.
 */
export function coerceReplayResponse(
  data: unknown,
): { ok: true; value: ReplayResponse } | { ok: false; message: string } {
  if (!isRecord(data)) {
    return { ok: false, message: "Replay response was not a JSON object." };
  }

  if (typeof data.runId !== "string" || typeof data.mode !== "string" || typeof data.replayedUtc !== "string") {
    return {
      ok: false,
      message: "Replay response is missing runId, mode, or replayedUtc.",
    };
  }

  if (!isRecord(data.validation) || !Array.isArray(data.validation.notes)) {
    return {
      ok: false,
      message: 'Replay response is missing "validation.notes" array.',
    };
  }

  const validation = data.validation;

  const boolKeys = [
    "manifestHashMatches",
    "artifactBundlePresentAfterReplay",
  ] as const;

  for (const key of boolKeys) {
    if (typeof validation[key] !== "boolean") {
      return {
        ok: false,
        message: `Replay validation is missing or invalid "${key}".`,
      };
    }
  }

  if (validation.hasValidationNotes !== undefined && typeof validation.hasValidationNotes !== "boolean") {
    return {
      ok: false,
      message: 'Replay validation has invalid "hasValidationNotes".',
    };
  }

  return { ok: true, value: data as ReplayResponse };
}

/**
 * Ensures run detail envelope has a run object with identifiers.
 */
export function coerceRunDetail(
  data: unknown,
): { ok: true; value: RunDetail } | { ok: false; message: string } {
  if (!isRecord(data)) {
    return { ok: false, message: "Run detail response was not a JSON object." };
  }

  if (!isRecord(data.run)) {
    return { ok: false, message: 'Run detail response is missing a "run" object.' };
  }

  const run = data.run;

  if (typeof run.runId !== "string" || typeof run.projectId !== "string") {
    return { ok: false, message: "Run detail is missing string runId or projectId." };
  }

  if (typeof run.createdUtc !== "string") {
    return { ok: false, message: "Run detail is missing string createdUtc." };
  }

  return { ok: true, value: data as RunDetail };
}

/**
 * Ensures manifest summary has required scalar fields for the review header.
 */
export function coerceManifestSummary(
  data: unknown,
): { ok: true; value: ManifestSummary } | { ok: false; message: string } {
  if (!isRecord(data)) {
    return { ok: false, message: "Manifest summary was not a JSON object." };
  }

  const stringKeys = [
    "manifestId",
    "runId",
    "createdUtc",
    "manifestHash",
    "ruleSetId",
    "ruleSetVersion",
    "status",
  ] as const;

  for (const key of stringKeys) {
    if (typeof data[key] !== "string") {
      return { ok: false, message: `Manifest summary is missing or invalid "${key}".` };
    }
  }

  const numberKeys = ["decisionCount", "warningCount", "unresolvedIssueCount"] as const;

  for (const key of numberKeys) {
    if (typeof data[key] !== "number") {
      return { ok: false, message: `Manifest summary is missing or invalid "${key}".` };
    }
  }

  if (data.operatorSummary !== undefined && typeof data.operatorSummary !== "string") {
    return { ok: false, message: 'Manifest summary has invalid "operatorSummary" (expected string).' };
  }

  return { ok: true, value: data as ManifestSummary };
}

/**
 * Ensures artifact list is an array of descriptors with stable ids.
 */
export function coerceArtifactDescriptorList(
  data: unknown,
): { ok: true; items: ArtifactDescriptor[] } | { ok: false; message: string } {
  if (!Array.isArray(data)) {
    return { ok: false, message: "Artifact list was not a JSON array." };
  }

  const items: ArtifactDescriptor[] = [];

  for (const row of data) {
    if (!isRecord(row)) {
      return { ok: false, message: "One or more artifact rows are not objects." };
    }

    if (typeof row.artifactId !== "string" || typeof row.name !== "string") {
      return {
        ok: false,
        message: "One or more artifacts are missing artifactId or name.",
      };
    }

    items.push(row as ArtifactDescriptor);
  }

  return { ok: true, items };
}

/**
 * Ensures a single artifact descriptor response matches the UI contract.
 */
export function coerceArtifactDescriptor(
  data: unknown,
): { ok: true; value: ArtifactDescriptor } | { ok: false; message: string } {
  if (!isRecord(data)) {
    return { ok: false, message: "Artifact descriptor was not a JSON object." };
  }

  const stringKeys = [
    "artifactId",
    "artifactType",
    "name",
    "format",
    "createdUtc",
    "contentHash",
  ] as const;

  for (const key of stringKeys) {
    if (typeof data[key] !== "string") {
      return { ok: false, message: `Artifact descriptor is missing or invalid "${key}".` };
    }
  }

  if (data.manifestId !== undefined && typeof data.manifestId !== "string") {
    return { ok: false, message: 'Artifact descriptor has invalid "manifestId".' };
  }

  if (data.runId !== undefined && typeof data.runId !== "string") {
    return { ok: false, message: 'Artifact descriptor has invalid "runId".' };
  }

  return { ok: true, value: data as ArtifactDescriptor };
}

/**
 * Ensures legacy compare payload is safe to render.
 */
export function coerceRunComparison(
  data: unknown,
): { ok: true; value: RunComparison } | { ok: false; message: string } {
  if (!isRecord(data)) {
    return { ok: false, message: "Run comparison response was not a JSON object." };
  }

  if (typeof data.leftRunId !== "string" || typeof data.rightRunId !== "string") {
    return { ok: false, message: "Run comparison is missing leftRunId or rightRunId." };
  }

  if (!Array.isArray(data.runLevelDiffs)) {
    return { ok: false, message: 'Run comparison is missing "runLevelDiffs" array.' };
  }

  if (data.manifestComparison != null && data.manifestComparison !== undefined) {
    if (!isRecord(data.manifestComparison)) {
      return { ok: false, message: "manifestComparison is present but not an object." };
    }

    if (!Array.isArray(data.manifestComparison.diffs)) {
      return { ok: false, message: 'manifestComparison is missing "diffs" array.' };
    }
  }

  return { ok: true, value: data as RunComparison };
}

/**
 * Ensures AI explanation payload matches the UI sections.
 */
export function coerceComparisonExplanation(
  data: unknown,
): { ok: true; value: ComparisonExplanation } | { ok: false; message: string } {
  if (!isRecord(data)) {
    return { ok: false, message: "AI explanation was not a JSON object." };
  }

  if (typeof data.highLevelSummary !== "string" || typeof data.narrative !== "string") {
    return {
      ok: false,
      message: "AI explanation is missing highLevelSummary or narrative strings.",
    };
  }

  if (!Array.isArray(data.majorChanges) || !Array.isArray(data.keyTradeoffs)) {
    return {
      ok: false,
      message: 'AI explanation is missing "majorChanges" or "keyTradeoffs" arrays.',
    };
  }

  return { ok: true, value: data as ComparisonExplanation };
}
