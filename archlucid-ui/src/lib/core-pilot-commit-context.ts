import { listRunsByProjectPaged } from "@/lib/api";
import { coerceRunSummaryPaged } from "@/lib/operator-response-guards";
import type { RunSummary } from "@/types/authority";

const DEFAULT_PROJECT_ID = "default";

/** First page size when scanning for a committed run (newest runs appear first). */
const COMMIT_SCAN_PAGE_SIZE = 40;

export type CorePilotCommitContext = {
  /** True when tenant has at least one authority-committed manifest (trial anchor or run row). */
  hasCommittedManifest: boolean;
  /** Newest run id on the first page, if any — useful for “open run detail” deep links. */
  latestRunId: string | null;
  /** First run on the page that already has a golden manifest, if any. */
  firstCommittedRunId: string | null;
};

function isCommittedRunSummary(run: RunSummary): boolean {
  return (
    (typeof run.goldenManifestId === "string" && run.goldenManifestId.length > 0) ||
    run.hasGoldenManifest === true
  );
}

/**
 * Client-only: resolves Core Pilot “commit happened” signals without new API routes.
 * Prefer `GET /v1/tenant/trial-status.firstCommitUtc`; fall back to scanning run summaries.
 */
export async function fetchCorePilotCommitContext(): Promise<CorePilotCommitContext> {
  let trialAnchoredCommit = false;

  try {
    const res = await fetch("/api/proxy/v1/tenant/trial-status", { credentials: "include" });

    if (res.ok) {
      const json: unknown = await res.json();

      if (
        json !== null &&
        typeof json === "object" &&
        "firstCommitUtc" in json &&
        typeof (json as { firstCommitUtc?: unknown }).firstCommitUtc === "string" &&
        (json as { firstCommitUtc: string }).firstCommitUtc.length > 0
      ) {
        trialAnchoredCommit = true;
      }
    }
  } catch {
    /* defer to run scan */
  }

  try {
    const raw: unknown = await listRunsByProjectPaged(DEFAULT_PROJECT_ID, 1, COMMIT_SCAN_PAGE_SIZE);
    const coerced = coerceRunSummaryPaged(raw);

    if (!coerced.ok) {
      return {
        hasCommittedManifest: trialAnchoredCommit,
        latestRunId: null,
        firstCommittedRunId: null,
      };
    }

    const items = coerced.value.items;
    const latestRunId = items.length > 0 ? items[0].runId : null;
    const committed = items.find((r) => isCommittedRunSummary(r));

    const hasCommittedManifest = trialAnchoredCommit || committed !== undefined;

    return {
      hasCommittedManifest,
      latestRunId,
      firstCommittedRunId: committed?.runId ?? null,
    };
  } catch {
    return {
      hasCommittedManifest: trialAnchoredCommit,
      latestRunId: null,
      firstCommittedRunId: null,
    };
  }
}
