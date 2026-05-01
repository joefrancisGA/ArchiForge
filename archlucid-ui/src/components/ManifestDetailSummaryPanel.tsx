import Link from "next/link";

import { CollapsibleSection } from "@/components/CollapsibleSection";
import { CopyIdButton } from "@/components/CopyIdButton";
import { manifestStatusForDisplay } from "@/lib/manifest-status-display";
import { policyPackBuyerLabel } from "@/lib/policy-pack-buyer-label";
import {
  SHOWCASE_STATIC_DEMO_DECISION_SYNOPSES,
  SHOWCASE_STATIC_DEMO_MANIFEST_ID,
  SHOWCASE_STATIC_DEMO_PRIMARY_FINDING_ID,
  SHOWCASE_STATIC_DEMO_WARNING_SYNOPSES,
} from "@/lib/showcase-static-demo";
import type { ManifestSummary } from "@/types/authority";

type ManifestDetailSummaryPanelProps = {
  readonly summary: ManifestSummary;
};

/**
 * Manifest summary: metric tiles plus expandable decisions/warnings when we have curated demo copy or counts only.
 */
export function ManifestDetailSummaryPanel({
  summary,
}: ManifestDetailSummaryPanelProps) {
  const isCuratedDemo = summary.manifestId === SHOWCASE_STATIC_DEMO_MANIFEST_ID;
  const decisionLinesAll = isCuratedDemo ? [...SHOWCASE_STATIC_DEMO_DECISION_SYNOPSES] : [];
  const decisionLinesPreview = decisionLinesAll.slice(0, 3);
  const decisionRestCount = Math.max(0, decisionLinesAll.length - decisionLinesPreview.length);
  const warningLines = isCuratedDemo ? [...SHOWCASE_STATIC_DEMO_WARNING_SYNOPSES] : [];

  return (
    <>
      {summary.operatorSummary ? (
        <p className="m-0 text-sm leading-relaxed text-neutral-600 dark:text-neutral-400">
          {summary.operatorSummary}
        </p>
      ) : null}

      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
        <div className="rounded-lg border border-neutral-200 bg-neutral-50/80 p-3 dark:border-neutral-800 dark:bg-neutral-900/40">
          <p className="m-0 text-xs font-medium uppercase tracking-wide text-neutral-500 dark:text-neutral-400">
            Status
          </p>
          <p className="m-0 mt-2">
            <span className="inline-flex items-center rounded-full border border-emerald-200 bg-emerald-50 px-2.5 py-0.5 text-xs font-medium text-emerald-900 dark:border-emerald-800 dark:bg-emerald-950 dark:text-emerald-100">
              {manifestStatusForDisplay(summary.status)}
            </span>
          </p>
        </div>
        <div className="rounded-lg border border-neutral-200 bg-neutral-50/80 p-3 dark:border-neutral-800 dark:bg-neutral-900/40">
          <p className="m-0 text-xs font-medium uppercase tracking-wide text-neutral-500 dark:text-neutral-400">
            Decisions
          </p>
          <p className="m-0 mt-2 text-2xl font-semibold tabular-nums text-neutral-900 dark:text-neutral-100">
            {summary.decisionCount}
          </p>
        </div>
        <div className="rounded-lg border border-neutral-200 bg-neutral-50/80 p-3 dark:border-neutral-800 dark:bg-neutral-900/40">
          <p className="m-0 text-xs font-medium uppercase tracking-wide text-neutral-500 dark:text-neutral-400">
            Warnings
          </p>
          <p className="m-0 mt-2 text-2xl font-semibold tabular-nums text-neutral-900 dark:text-neutral-100">
            {summary.warningCount}
          </p>
        </div>
        <div className="rounded-lg border border-neutral-200 bg-neutral-50/80 p-3 dark:border-neutral-800 dark:bg-neutral-900/40">
          <p className="m-0 text-xs font-medium uppercase tracking-wide text-neutral-500 dark:text-neutral-400">
            Unresolved
          </p>
          <p className="m-0 mt-2 text-2xl font-semibold tabular-nums text-neutral-900 dark:text-neutral-100">
            {summary.unresolvedIssueCount}
          </p>
        </div>
      </div>

      <p className="m-0 text-sm text-neutral-700 dark:text-neutral-300">
        <span className="font-medium text-neutral-800 dark:text-neutral-200">Policy pack:</span>{" "}
        {policyPackBuyerLabel(summary.ruleSetId, summary.ruleSetVersion)}
      </p>

      <CollapsibleSection title="Technical identifiers" defaultOpen={false}>
        <dl className="m-0 grid gap-3 sm:grid-cols-[minmax(8rem,auto)_1fr] sm:gap-x-6">
          <dt className="text-sm font-medium text-neutral-700 dark:text-neutral-300">Manifest ID</dt>
          <dd className="m-0 flex min-w-0 flex-wrap items-center gap-2 text-sm text-neutral-900 dark:text-neutral-100">
            <code className="min-w-0 break-all font-mono text-xs">{summary.manifestId}</code>
            <CopyIdButton value={summary.manifestId} aria-label="Copy manifest ID" />
          </dd>
          {summary.manifestHash ? (
            <>
              <dt className="text-sm font-medium text-neutral-700 dark:text-neutral-300">Hash</dt>
              <dd className="m-0 text-xs text-neutral-600 dark:text-neutral-400">
                <span className="font-mono text-[12px]">{summary.manifestHash}</span>
              </dd>
            </>
          ) : null}
        </dl>
      </CollapsibleSection>

      <details className="rounded-lg border border-neutral-200 dark:border-neutral-800" open>
        <summary className="cursor-pointer select-none px-3 py-2 text-sm font-semibold text-neutral-900 dark:text-neutral-100">
          Decisions recorded ({summary.decisionCount})
        </summary>
        <div className="border-t border-neutral-200 px-3 py-3 dark:border-neutral-800">
          {decisionLinesPreview.length > 0 ? (
            <ol className="m-0 list-decimal space-y-2 pl-5 text-sm text-neutral-700 dark:text-neutral-300">
              {decisionLinesPreview.map((line, index) => (
                <li key={`decision-${index}`}>{line}</li>
              ))}
            </ol>
          ) : summary.decisionCount > 0 ? (
            <p className="m-0 text-sm text-neutral-600 dark:text-neutral-400">
              Full decision text is included in the{" "}
              <Link className="font-medium text-teal-800 underline dark:text-teal-300" href={`/reviews/${summary.runId}`}>
                governed review export
              </Link>{" "}
              and manifest bundle — use the download actions on this page when available.
            </p>
          ) : (
            <p className="m-0 text-sm text-neutral-600 dark:text-neutral-400">No decisions recorded for this manifest.</p>
          )}
          {decisionRestCount > 0 ? (
            <p className="m-0 mt-2 text-xs text-neutral-600 dark:text-neutral-400">
              … and {decisionRestCount} more decisions in the governed export — open review detail or download the manifest
              bundle for the full list.
            </p>
          ) : null}
        </div>
      </details>

      <details className="rounded-lg border border-neutral-200 dark:border-neutral-800" open>
        <summary className="cursor-pointer select-none px-3 py-2 text-sm font-semibold text-neutral-900 dark:text-neutral-100">
          Warnings ({summary.warningCount})
        </summary>
        <div className="border-t border-neutral-200 px-3 py-3 dark:border-neutral-800">
          {warningLines.length > 0 ? (
            <ul className="m-0 list-disc space-y-2 pl-5 text-sm text-neutral-700 dark:text-neutral-300">
              {warningLines.map((line, index) => (
                <li key={`warning-${index}`}>{line}</li>
              ))}
            </ul>
          ) : summary.warningCount > 0 ? (
            <p className="m-0 text-sm text-neutral-600 dark:text-neutral-400">
              Warning detail ships with the governed manifest export. Use{" "}
              <Link className="font-medium text-teal-800 underline dark:text-teal-300" href={`/reviews/${summary.runId}`}>
                review detail
              </Link>{" "}
              or download the bundle.
            </p>
          ) : (
            <p className="m-0 text-sm text-neutral-600 dark:text-neutral-400">No warnings recorded for this manifest.</p>
          )}
        </div>
      </details>

      {isCuratedDemo ? (
        <section
          aria-labelledby="manifest-related-finding-heading"
          className="rounded-lg border border-teal-200/80 bg-teal-50/50 p-4 dark:border-teal-900/50 dark:bg-teal-950/30"
        >
          <h3
            id="manifest-related-finding-heading"
            className="m-0 text-sm font-semibold text-neutral-900 dark:text-neutral-100"
          >
            Related finding
          </h3>
          <p className="m-0 mt-2 text-sm text-neutral-700 dark:text-neutral-300">
            <Link
              className="font-medium text-teal-800 underline dark:text-teal-300"
              href={`/reviews/${encodeURIComponent(summary.runId)}/findings/${encodeURIComponent(SHOWCASE_STATIC_DEMO_PRIMARY_FINDING_ID)}`}
            >
              PHI Minimization Risk
            </Link>
            <span className="text-neutral-600 dark:text-neutral-400"> — open the product-facing finding detail.</span>
          </p>
        </section>
      ) : null}
    </>
  );
}
