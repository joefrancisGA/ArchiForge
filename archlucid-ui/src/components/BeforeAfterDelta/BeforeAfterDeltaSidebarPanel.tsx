"use client";

import { formatFindings, formatHours } from "./formatDelta";
import { useDeltaQuery } from "./useDeltaQuery";

/**
 * "Sidebar" placement of `BeforeAfterDeltaPanel` — compact single-card rendering of
 * the same medians the top variant shows. Designed to live as a collapsible card
 * under the sidebar's "Recent activity" group.
 *
 * Reuses `useDeltaQuery` so the network shape and loading semantics are identical
 * to the top variant; the only difference is the rendered chrome (smaller heading,
 * no per-run row strip, label-and-value pairs stacked vertically).
 *
 * Hidden when zero committed runs are in scope (same rule as the top variant) so
 * the sidebar does not start with a sad-empty card on a fresh tenant.
 */
export type BeforeAfterDeltaSidebarPanelProps = {
  count?: number;
};

export function BeforeAfterDeltaSidebarPanel({ count = 5 }: BeforeAfterDeltaSidebarPanelProps) {
  const { status, data } = useDeltaQuery({ count });

  if (status !== "ready" || data === null) return null;
  if (data.returnedCount === 0) return null;

  return (
    <aside
      data-testid="before-after-delta-panel-sidebar"
      role="complementary"
      aria-label="Median proof-of-ROI deltas (sidebar)"
      className="rounded-md border border-neutral-200 bg-neutral-50 p-3 text-xs text-neutral-700 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-300"
    >
      <p className="m-0 mb-2 text-[10px] font-semibold uppercase tracking-wide text-neutral-500 dark:text-neutral-400">
        Median delta · last <span data-testid="delta-sidebar-window">{data.returnedCount}</span> finalized run(s)
      </p>
      <dl className="m-0 grid grid-cols-2 gap-2">
        <div>
          <dt className="text-[10px] uppercase text-neutral-500 dark:text-neutral-400">Findings</dt>
          <dd
            data-testid="delta-sidebar-median-findings"
            className="m-0 text-sm font-semibold text-neutral-900 dark:text-neutral-100"
          >
            {formatFindings(data.medianTotalFindings)}
          </dd>
        </div>
        <div>
          <dt className="text-[10px] uppercase text-neutral-500 dark:text-neutral-400">Time</dt>
          <dd
            data-testid="delta-sidebar-median-time"
            className="m-0 text-sm font-semibold text-neutral-900 dark:text-neutral-100"
          >
            {formatHours(data.medianTimeToCommittedManifestTotalSeconds)}
          </dd>
        </div>
      </dl>
    </aside>
  );
}
