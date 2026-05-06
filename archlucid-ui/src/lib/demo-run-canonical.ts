import type { RunSummary } from "@/types/authority";

import { SHOWCASE_STATIC_DEMO_RUN_ID } from "@/lib/showcase-static-demo";

/**
 * Maps legacy bookmark/demo URLs to the canonical showcase run id so list pickers, run detail, and static
 * payloads agree (see OpenAI UI review 2026-04-30 — route/data-key mismatch).
 */
const DEMO_RUN_ID_ALIASES: Readonly<Record<string, string>> = {
  "claims-intake-modernization-run": SHOWCASE_STATIC_DEMO_RUN_ID,
  /** Workspace/sample bookmark tokens that still appear in docs and screenshots — normalize to canonical showcase id. */
  "claims-intake-sample-workspace": SHOWCASE_STATIC_DEMO_RUN_ID,
  /** Mock Ask conversation fixtures historically used this token — align pickers with the showcase review id. */
  "run-claims-intake-demo": SHOWCASE_STATIC_DEMO_RUN_ID,
};

/** Returns the canonical run id when `runId` is a known demo alias; otherwise returns trimmed `runId`. */
export function canonicalizeDemoRunId(runId: string): string {
  const t = runId.trim();

  if (t.length === 0) {
    return t;
  }

  return DEMO_RUN_ID_ALIASES[t] ?? t;
}

/** True when visiting `/runs/{runId}` (or executive `/reviews/{runId}`) should 308 to the canonical id. */
export function demoRunUrlRequiresCanonicalRedirect(runId: string): boolean {
  const t = runId.trim();
  const canon = canonicalizeDemoRunId(t);

  return canon.length > 0 && canon !== t;
}

/** Normalize API rows that still use a legacy demo id so pickers match static list payloads. */
export function normalizeRunSummaryForDemoPicker(row: RunSummary): RunSummary {
  const canon = canonicalizeDemoRunId(row.runId);

  if (canon === row.runId) {
    return row;
  }

  return { ...row, runId: canon };
}
