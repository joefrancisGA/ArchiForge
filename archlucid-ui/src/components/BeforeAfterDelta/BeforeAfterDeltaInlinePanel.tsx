"use client";

import { formatFindings, formatHours, percentDelta } from "./formatDelta";
import type { RecentPilotRunDeltaRow } from "./types";
import { useDeltaQuery } from "./useDeltaQuery";

/**
 * "Inline" placement of `BeforeAfterDeltaPanel` — rendered above the artifacts
 * table on `/runs/{runId}`. Shows the **single-run delta vs the prior committed
 * run for the same architecture request** so an operator can see whether this
 * commit improved on the previous one (fewer findings, shorter time, etc.).
 *
 * Approach:
 *  1. Fetch `/v1/pilots/runs/recent-deltas?count=25` (the same shared hook the
 *     top/sidebar variants use — one less HTTP surface to mock and rate-limit).
 *  2. Locate the **current** run inside `items` so the variant does not need a
 *     separate per-run lookup.
 *  3. Pick the **most recent prior committed run** with a matching `requestId`
 *     and an earlier `manifestCommittedUtc`.
 *  4. If no prior is found, render a small "no prior commit for this request" hint
 *     instead of nothing — it is information for the operator that this is the
 *     first commit on this request, not a broken panel.
 *
 * The 25-row window matches the server's hard ceiling so any prior commit that
 * is still in the recent window will be visible; older priors are invisible to
 * this variant by design (use `/compare` for the full history).
 */
export type BeforeAfterDeltaInlinePanelProps = {
  runId: string;
};

const INLINE_LOOKBACK_COUNT = 25;

export function BeforeAfterDeltaInlinePanel({ runId }: BeforeAfterDeltaInlinePanelProps) {
  const { status, data } = useDeltaQuery({ count: INLINE_LOOKBACK_COUNT });

  if (status !== "ready" || data === null) return null;

  const current = data.items.find((row) => row.runId === runId);

  if (current === undefined) return null;

  const prior = pickPriorForSameRequest(current, data.items);

  return (
    <section
      data-testid="before-after-delta-panel-inline"
      role="region"
      aria-label="Delta vs prior committed run for the same architecture request"
      className="mb-4 max-w-3xl rounded-md border border-neutral-200 bg-white p-4 shadow-sm dark:border-neutral-700 dark:bg-neutral-900"
    >
      <h3 className="m-0 text-sm font-semibold uppercase tracking-wide text-neutral-700 dark:text-neutral-200">
        Delta vs prior commit (same request)
      </h3>
      {prior === null ? (
        <p
          data-testid="delta-inline-no-prior"
          className="mt-2 text-xs text-neutral-600 dark:text-neutral-400"
        >
          No prior committed run found for request{" "}
          <code className="rounded bg-neutral-100 px-1 py-0.5 text-[11px] dark:bg-neutral-800">
            {current.requestId === "" ? "(unknown)" : current.requestId}
          </code>{" "}
          in the recent window. This is the first commit for this request — future commits will compare here.
        </p>
      ) : (
        <BeforeAfterDeltaInlineComparisonRow current={current} prior={prior} />
      )}
    </section>
  );
}

function pickPriorForSameRequest(
  current: RecentPilotRunDeltaRow,
  rows: RecentPilotRunDeltaRow[],
): RecentPilotRunDeltaRow | null {
  if (current.manifestCommittedUtc === null) return null;

  const currentCommittedAt = Date.parse(current.manifestCommittedUtc);

  if (Number.isNaN(currentCommittedAt)) return null;

  const candidates = rows.filter((r) => {
    if (r.runId === current.runId) return false;
    if (r.requestId === "" || r.requestId !== current.requestId) return false;
    if (r.manifestCommittedUtc === null) return false;

    const t = Date.parse(r.manifestCommittedUtc);

    if (Number.isNaN(t)) return false;

    return t < currentCommittedAt;
  });

  if (candidates.length === 0) return null;

  return candidates.reduce((latest, row) => {
    const a = Date.parse(latest.manifestCommittedUtc ?? "");
    const b = Date.parse(row.manifestCommittedUtc ?? "");

    return b > a ? row : latest;
  });
}

function BeforeAfterDeltaInlineComparisonRow({
  current,
  prior,
}: {
  current: RecentPilotRunDeltaRow;
  prior: RecentPilotRunDeltaRow;
}) {
  const findingsDelta = percentDelta(prior.totalFindings, current.totalFindings);
  const timeDelta = percentDelta(
    prior.timeToCommittedManifestTotalSeconds,
    current.timeToCommittedManifestTotalSeconds,
  );

  return (
    <dl className="mt-3 grid grid-cols-1 gap-3 sm:grid-cols-2">
      <div className="rounded border border-neutral-200 p-3 dark:border-neutral-700">
        <dt className="text-xs font-medium uppercase text-neutral-500 dark:text-neutral-400">Findings</dt>
        <dd
          data-testid="delta-inline-findings"
          className="mt-1 text-2xl font-semibold text-neutral-900 dark:text-neutral-100"
        >
          {formatFindings(current.totalFindings)}{" "}
          <span className="text-sm font-normal text-neutral-600 dark:text-neutral-400">
            (prior: {formatFindings(prior.totalFindings)})
          </span>
        </dd>
        {findingsDelta !== null ? (
          <dd
            data-testid="delta-inline-findings-percent"
            className="mt-1 text-xs text-neutral-600 dark:text-neutral-400"
          >
            {findingsDelta >= 0
              ? `${findingsDelta.toFixed(1)}% fewer findings vs prior commit`
              : `${Math.abs(findingsDelta).toFixed(1)}% more findings vs prior commit`}
          </dd>
        ) : null}
      </div>
      <div className="rounded border border-neutral-200 p-3 dark:border-neutral-700">
        <dt className="text-xs font-medium uppercase text-neutral-500 dark:text-neutral-400">
          Time-to-committed manifest
        </dt>
        <dd
          data-testid="delta-inline-time"
          className="mt-1 text-2xl font-semibold text-neutral-900 dark:text-neutral-100"
        >
          {formatHours(current.timeToCommittedManifestTotalSeconds)}{" "}
          <span className="text-sm font-normal text-neutral-600 dark:text-neutral-400">
            (prior: {formatHours(prior.timeToCommittedManifestTotalSeconds)})
          </span>
        </dd>
        {timeDelta !== null ? (
          <dd
            data-testid="delta-inline-time-percent"
            className="mt-1 text-xs text-neutral-600 dark:text-neutral-400"
          >
            {timeDelta >= 0
              ? `${timeDelta.toFixed(1)}% faster vs prior commit`
              : `${Math.abs(timeDelta).toFixed(1)}% slower vs prior commit`}
          </dd>
        ) : null}
      </div>
    </dl>
  );
}
