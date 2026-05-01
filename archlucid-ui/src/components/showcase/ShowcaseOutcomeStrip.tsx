import Link from "next/link";
import type { ReactElement } from "react";

export type ShowcaseOutcomeStripProps = {
  runId: string;
  manifestId: string | null | undefined;
  /** When set (demo spine), adds a direct finding deep-link card */
  primaryFindingId?: string | null | undefined;
  /**
   * When false, omit authenticated `/reviews/...` deep links (use manifest-only CTAs). Public marketing surfaces pass
   * {@link import("@/lib/operator-static-demo").isStaticDemoPayloadFallbackEnabled} from a server parent.
   */
  readonly isRunDetailAvailable?: boolean;
};

/**
 * Primary CTAs for the public showcase — deep-links into operator routes used by the mock / pilot flows.
 */
export function ShowcaseOutcomeStrip(props: ShowcaseOutcomeStripProps): ReactElement {
  const { runId, manifestId, primaryFindingId, isRunDetailAvailable = true } = props;
  const encRun = encodeURIComponent(runId);
  const hasManifest = typeof manifestId === "string" && manifestId.trim().length > 0;
  const encFinding =
    typeof primaryFindingId === "string" && primaryFindingId.trim().length > 0
      ? encodeURIComponent(primaryFindingId.trim())
      : null;

  const cardClass =
    "flex flex-col gap-1 rounded-lg border border-neutral-200 bg-white p-4 no-underline shadow-sm transition hover:border-teal-600/40 hover:shadow dark:border-neutral-800 dark:bg-neutral-950 dark:hover:border-teal-400/40";

  const encManifest = hasManifest ? encodeURIComponent(manifestId.trim()) : "";

  return (
    <section aria-label="Open completed output" className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5">
      {isRunDetailAvailable ? (
        <Link className={cardClass} href={`/reviews/${encRun}`}>
          <span className="text-sm font-semibold text-neutral-900 dark:text-neutral-50">Open review</span>
          <span className="text-xs text-neutral-600 dark:text-neutral-400">Outcome summary, timeline, exports</span>
        </Link>
      ) : (
        <div className={`${cardClass} pointer-events-none cursor-not-allowed opacity-60`}>
          <span className="text-sm font-semibold text-neutral-900 dark:text-neutral-50">Open review</span>
          <span className="text-xs text-neutral-600 dark:text-neutral-400">
            Sign in with a connected workspace to open the full review detail screen.
          </span>
        </div>
      )}

      {hasManifest ? (
        <Link className={cardClass} href={`/manifests/${encManifest}`}>
          <span className="text-sm font-semibold text-neutral-900 dark:text-neutral-50">Manifest finalized</span>
          <span className="text-xs text-neutral-600 dark:text-neutral-400">
            Finalized Architecture Manifest — architecture record and artifact list
          </span>
        </Link>
      ) : (
        <div className={`${cardClass} pointer-events-none cursor-not-allowed opacity-60`}>
          <span className="text-sm font-semibold text-neutral-900 dark:text-neutral-50">Manifest finalized</span>
          <span className="text-xs text-neutral-600 dark:text-neutral-400">Unavailable for this preview</span>
        </div>
      )}

      {isRunDetailAvailable ? (
        <Link className={cardClass} href={`/reviews/${encRun}#run-explanation`}>
          <span className="text-sm font-semibold text-neutral-900 dark:text-neutral-50">Findings &amp; explanation</span>
          <span className="text-xs text-neutral-600 dark:text-neutral-400">Aggregate narrative on the review</span>
        </Link>
      ) : hasManifest ? (
        <Link className={cardClass} href={`/manifests/${encManifest}`}>
          <span className="text-sm font-semibold text-neutral-900 dark:text-neutral-50">Findings &amp; explanation</span>
          <span className="text-xs text-neutral-600 dark:text-neutral-400">See findings and narrative in the finalized manifest</span>
        </Link>
      ) : (
        <div className={`${cardClass} pointer-events-none cursor-not-allowed opacity-60`}>
          <span className="text-sm font-semibold text-neutral-900 dark:text-neutral-50">Findings &amp; explanation</span>
          <span className="text-xs text-neutral-600 dark:text-neutral-400">Unavailable for this preview</span>
        </div>
      )}

      {isRunDetailAvailable ? (
        <Link className={cardClass} href={`/reviews/${encRun}#artifacts-exports`}>
          <span className="text-sm font-semibold text-neutral-900 dark:text-neutral-50">Artifacts &amp; exports</span>
          <span className="text-xs text-neutral-600 dark:text-neutral-400">Downloads and descriptor table</span>
        </Link>
      ) : hasManifest ? (
        <Link className={cardClass} href={`/manifests/${encManifest}`}>
          <span className="text-sm font-semibold text-neutral-900 dark:text-neutral-50">Artifacts &amp; exports</span>
          <span className="text-xs text-neutral-600 dark:text-neutral-400">Artifact list and exports on the manifest</span>
        </Link>
      ) : (
        <div className={`${cardClass} pointer-events-none cursor-not-allowed opacity-60`}>
          <span className="text-sm font-semibold text-neutral-900 dark:text-neutral-50">Artifacts &amp; exports</span>
          <span className="text-xs text-neutral-600 dark:text-neutral-400">Unavailable for this preview</span>
        </div>
      )}

      {encFinding !== null ? (
        isRunDetailAvailable ? (
          <Link className={cardClass} href={`/reviews/${encRun}/findings/${encFinding}`}>
            <span className="text-sm font-semibold text-neutral-900 dark:text-neutral-50">Primary finding</span>
            <span className="text-xs text-neutral-600 dark:text-neutral-400">
              PHI Minimization Risk — human-readable detail
            </span>
          </Link>
        ) : hasManifest ? (
          <Link className={cardClass} href={`/manifests/${encManifest}`}>
            <span className="text-sm font-semibold text-neutral-900 dark:text-neutral-50">Primary finding</span>
            <span className="text-xs text-neutral-600 dark:text-neutral-400">
              PHI minimization posture — see related items in the manifest
            </span>
          </Link>
        ) : (
          <div className={`${cardClass} pointer-events-none cursor-not-allowed opacity-60`}>
            <span className="text-sm font-semibold text-neutral-900 dark:text-neutral-50">Primary finding</span>
            <span className="text-xs text-neutral-600 dark:text-neutral-400">Unavailable for this preview</span>
          </div>
        )
      ) : null}
    </section>
  );
}
