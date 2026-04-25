"use client";

import { formatFindings, formatHours } from "./formatDelta";
import { useDeltaQuery } from "./useDeltaQuery";

/**
 * "Top" placement of `BeforeAfterDeltaPanel` — rendered above the runs index list.
 *
 * Aggregates the most recent N committed runs (default 5, matches owner Q29) and shows
 * the **median delta on findings + median time-to-committed-manifest** as the headline,
 * with a thin per-run row strip below so the median is auditable at a glance.
 *
 * Uses median (not mean) so a single noisy outlier run cannot inflate the headline —
 * the same choice the server makes in `RecentPilotRunDeltasService.ComputeMedian`.
 *
 * Hidden when zero committed runs are in scope so the runs index does not start with
 * a sad-empty card; the runs-index empty state already covers that case.
 */
export type BeforeAfterDeltaTopPanelProps = {
  /** Hard upper bound — server still clamps to [1, 25]. Default 5 matches the prompt. */
  count?: number;
};

export function BeforeAfterDeltaTopPanel({ count = 5 }: BeforeAfterDeltaTopPanelProps) {
  const { status, data } = useDeltaQuery({ count });

  if (status !== "ready" || data === null) return null;
  if (data.returnedCount === 0) return null;

  return (
    <section
      data-testid="before-after-delta-panel-top"
      role="region"
      aria-label="Median proof-of-ROI deltas across recent committed runs"
      className="mb-6 max-w-4xl rounded-md border border-neutral-200 bg-white p-4 shadow-sm dark:border-neutral-700 dark:bg-neutral-900"
    >
      <h3 className="m-0 text-sm font-semibold uppercase tracking-wide text-neutral-700 dark:text-neutral-200">
        Recent committed runs — median delta
      </h3>
      <p className="mt-1 text-xs text-neutral-600 dark:text-neutral-400">
        Across the last <strong data-testid="delta-top-window">{data.returnedCount}</strong> committed run(s) in
        scope. Median (not mean) so one outlier does not skew the headline. Same numbers as the per-run value
        report.
      </p>

      <dl className="mt-3 grid grid-cols-1 gap-3 sm:grid-cols-2">
        <div className="rounded border border-neutral-200 p-3 dark:border-neutral-700">
          <dt className="text-xs font-medium uppercase text-neutral-500 dark:text-neutral-400">
            Median findings per committed run
          </dt>
          <dd
            data-testid="delta-top-median-findings"
            className="mt-1 text-2xl font-semibold text-neutral-900 dark:text-neutral-100"
          >
            {formatFindings(data.medianTotalFindings)}
          </dd>
        </div>
        <div className="rounded border border-neutral-200 p-3 dark:border-neutral-700">
          <dt className="text-xs font-medium uppercase text-neutral-500 dark:text-neutral-400">
            Median time-to-committed manifest
          </dt>
          <dd
            data-testid="delta-top-median-time"
            className="mt-1 text-2xl font-semibold text-neutral-900 dark:text-neutral-100"
          >
            {formatHours(data.medianTimeToCommittedManifestTotalSeconds)}
          </dd>
        </div>
      </dl>

      <ol
        data-testid="delta-top-rows"
        className="mt-3 space-y-1 text-xs text-neutral-600 dark:text-neutral-400"
      >
        {data.items.map((row) => (
          <li key={row.runId} className="flex flex-wrap gap-x-3">
            <span className="font-mono">{row.runId.slice(0, 8)}…</span>
            <span>{row.totalFindings} finding(s)</span>
            <span>{formatHours(row.timeToCommittedManifestTotalSeconds)}</span>
            {row.isDemoTenant ? (
              <span className="rounded bg-amber-100 px-1.5 py-0.5 text-amber-900 dark:bg-amber-900/30 dark:text-amber-200">
                demo
              </span>
            ) : null}
          </li>
        ))}
      </ol>
    </section>
  );
}
