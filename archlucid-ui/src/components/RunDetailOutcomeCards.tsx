import Link from "next/link";

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";

type RunDetailOutcomeCardsProps = {
  readonly runId: string;
  /** When finalized, links the manifest outcome card to manifest detail. */
  readonly manifestId?: string | null;
  readonly hasGoldenManifest: boolean;
  readonly findingCountDisplay: number | null;
  readonly warningCountDisplay: number | null;
  readonly artifactCount: number;
  readonly unresolvedIssueCountDisplay: number | null;
  /** From manifest status when summary is loaded; omit to hide the governance line on the manifest card. */
  readonly governanceGateLabel?: string | null;
};

/**
 * Top-of-run proof summary: reviewers see outcomes before scrolling to timeline and agent diagnostics.
 */
const samePageJumpClass =
  "block rounded-lg no-underline outline-none ring-offset-2 transition hover:ring-2 hover:ring-teal-500/40 focus-visible:ring-2 focus-visible:ring-teal-600 dark:ring-offset-neutral-950";

export function RunDetailOutcomeCards({
  runId,
  manifestId,
  hasGoldenManifest,
  findingCountDisplay,
  warningCountDisplay,
  artifactCount,
  unresolvedIssueCountDisplay,
  governanceGateLabel,
}: RunDetailOutcomeCardsProps) {

  const findingsCardEl = (
        <Card className="h-full border-neutral-200 dark:border-neutral-800">
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-semibold text-neutral-900 dark:text-neutral-100">
              Warnings &amp; findings
            </CardTitle>
            <CardDescription>
              {manifestId ? "From architecture review — click to jump" : "From architecture review"}
            </CardDescription>
          </CardHeader>
          <CardContent className="pt-0 space-y-1">
            <p className="m-0 text-sm tabular-nums text-neutral-900 dark:text-neutral-100">
              <span className="font-medium">Findings:</span>{" "}
              <span className="text-lg font-semibold">{findingCountDisplay === null ? "—" : findingCountDisplay}</span>
            </p>
            <p className="m-0 text-sm tabular-nums text-neutral-900 dark:text-neutral-100">
              <span className="font-medium">Warnings (manifest):</span>{" "}
              <span className="text-lg font-semibold">{warningCountDisplay === null ? "—" : warningCountDisplay}</span>
            </p>
            {unresolvedIssueCountDisplay !== null && unresolvedIssueCountDisplay > 0 ? (
              <p className="mt-1 text-xs text-neutral-600 dark:text-neutral-400">
                {unresolvedIssueCountDisplay} unresolved on manifest
              </p>
            ) : null}
          </CardContent>
        </Card>
      );

  const artifactsCardEl = (
        <Card className="h-full border-neutral-200 dark:border-neutral-800">
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-semibold text-neutral-900 dark:text-neutral-100">
              Artifacts
            </CardTitle>
            <CardDescription>
              {manifestId ? "Generated outputs — click to jump" : "Generated outputs"}
            </CardDescription>
          </CardHeader>
          <CardContent className="pt-0">
            <p className="m-0 text-lg font-semibold tabular-nums text-neutral-900 dark:text-neutral-100">{artifactCount}</p>
            <p className="mt-1 text-xs text-neutral-600 dark:text-neutral-400">
              Attached to manifest when finalized
            </p>
          </CardContent>
        </Card>
      );

  return (
    <section aria-label="Run outcomes" className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
      <Card className="border-neutral-200 dark:border-neutral-800">
        <CardHeader className="pb-2">
          <CardTitle className="text-sm font-semibold text-neutral-900 dark:text-neutral-100">
            Manifest
          </CardTitle>
          <CardDescription>Reviewed architecture record</CardDescription>
        </CardHeader>
        <CardContent className="pt-0">
          <p className={`m-0 text-base font-semibold ${hasGoldenManifest ? "text-emerald-700 dark:text-emerald-400" : "text-amber-800 dark:text-amber-200"}`}>
            {hasGoldenManifest ? "Finalized" : "Awaiting finalize"}
          </p>
          <p className="mt-1 text-xs text-neutral-600 dark:text-neutral-400">
            {hasGoldenManifest ? "Architecture manifest is pinned to this run." : "Finalize from the finalize control when ready."}
          </p>
          {governanceGateLabel !== null && governanceGateLabel !== undefined && governanceGateLabel.length > 0 ? (
            <p className="m-0 mt-2 text-xs text-neutral-700 dark:text-neutral-300">
              <span className="font-medium text-neutral-800 dark:text-neutral-200">Governance gate:</span>{" "}
              {governanceGateLabel}
            </p>
          ) : null}
          {hasGoldenManifest && manifestId !== null && manifestId !== undefined && manifestId.trim().length > 0 ? (
            <Link
              className="mt-2 inline-block text-sm font-medium text-teal-800 underline underline-offset-2 hover:text-teal-900 dark:text-teal-300 dark:hover:text-teal-200"
              href={`/manifests/${encodeURIComponent(manifestId.trim())}`}
            >
              Open manifest detail
            </Link>
          ) : null}
        </CardContent>
      </Card>

      {manifestId ? (
        <Link
          href="#run-explanation"
          className={samePageJumpClass}
          aria-label="Jump to architecture review summary and findings"
        >
          {findingsCardEl}
        </Link>
      ) : (
        findingsCardEl
      )}

      {manifestId ? (
        <Link href="#artifacts-exports" className={samePageJumpClass} aria-label="Jump to artifacts and exports">
          {artifactsCardEl}
        </Link>
      ) : (
        artifactsCardEl
      )}

      <Card className="border-neutral-200 dark:border-neutral-800">
        <CardHeader className="pb-2">
          <CardTitle className="text-sm font-semibold text-neutral-900 dark:text-neutral-100">
            Review trail
          </CardTitle>
          <CardDescription>Pipeline + traceability</CardDescription>
        </CardHeader>
        <CardContent className="pt-0">
          <Link
            className="text-sm font-medium text-teal-800 underline underline-offset-2 hover:text-teal-900 dark:text-teal-300 dark:hover:text-teal-200"
            href="#authority-chain"
          >
            Jump to review trail on this page
          </Link>
          <Link
            className="mt-2 block text-sm font-medium text-teal-800 underline underline-offset-2 hover:text-teal-900 dark:text-teal-300 dark:hover:text-teal-200"
            href={`/reviews/${encodeURIComponent(runId)}/provenance`}
          >
            Full provenance view
          </Link>
          <Link
            className="mt-2 block text-sm font-medium text-teal-800 underline underline-offset-2 hover:text-teal-900 dark:text-teal-300 dark:hover:text-teal-200"
            href={`/showcase/${encodeURIComponent(runId)}`}
          >
            Completed output (public showcase)
          </Link>
          <p className="mt-2 text-xs text-neutral-600 dark:text-neutral-400">
            Timeline and audit identifiers stay below — start here for the proof path.
          </p>
        </CardContent>
      </Card>
    </section>
  );
}
