import Link from "next/link";
import type { ReactElement } from "react";

export type ShowcaseOutcomeStripProps = {
  runId: string;
  manifestId: string | null | undefined;
};

/**
 * Primary CTAs for the public showcase — deep-links into operator routes used by the mock / pilot flows.
 */
export function ShowcaseOutcomeStrip(props: ShowcaseOutcomeStripProps): ReactElement {
  const { runId, manifestId } = props;
  const encRun = encodeURIComponent(runId);
  const hasManifest = typeof manifestId === "string" && manifestId.trim().length > 0;

  const cardClass =
    "flex flex-col gap-1 rounded-lg border border-neutral-200 bg-white p-4 no-underline shadow-sm transition hover:border-teal-600/40 hover:shadow dark:border-neutral-800 dark:bg-neutral-950 dark:hover:border-teal-400/40";

  return (
    <section aria-label="Open completed output" className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
      <Link className={cardClass} href={`/runs/${encRun}`}>
        <span className="text-sm font-semibold text-neutral-900 dark:text-neutral-50">Run detail</span>
        <span className="text-xs text-neutral-600 dark:text-neutral-400">Outcome summary, timeline, exports</span>
      </Link>

      {hasManifest ? (
        <Link
          className={cardClass}
          href={`/manifests/${encodeURIComponent(manifestId.trim())}`}
        >
          <span className="text-sm font-semibold text-neutral-900 dark:text-neutral-50">Finalized manifest</span>
          <span className="text-xs text-neutral-600 dark:text-neutral-400">Architecture record and artifact list</span>
        </Link>
      ) : (
        <div className={`${cardClass} pointer-events-none cursor-not-allowed opacity-60`}>
          <span className="text-sm font-semibold text-neutral-900 dark:text-neutral-50">Finalized manifest</span>
          <span className="text-xs text-neutral-600 dark:text-neutral-400">Unavailable for this preview</span>
        </div>
      )}

      <Link className={cardClass} href={`/runs/${encRun}#run-explanation`}>
        <span className="text-sm font-semibold text-neutral-900 dark:text-neutral-50">Findings &amp; explanation</span>
        <span className="text-xs text-neutral-600 dark:text-neutral-400">Aggregate narrative on the run</span>
      </Link>

      <Link className={cardClass} href={`/runs/${encRun}#artifacts-exports`}>
        <span className="text-sm font-semibold text-neutral-900 dark:text-neutral-50">Artifacts &amp; exports</span>
        <span className="text-xs text-neutral-600 dark:text-neutral-400">Downloads and descriptor table</span>
      </Link>
    </section>
  );
}
