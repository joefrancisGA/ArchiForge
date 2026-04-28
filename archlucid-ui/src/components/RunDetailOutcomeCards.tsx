import Link from "next/link";

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";


type RunDetailOutcomeCardsProps = {
  readonly runId: string;
  readonly hasGoldenManifest: boolean;
  readonly findingCountDisplay: number | null;
  readonly artifactCount: number;
  readonly unresolvedIssueCountDisplay: number | null;
};

/**
 * Top-of-run proof summary: reviewers see outcomes before scrolling to timeline and agent diagnostics.
 */
export function RunDetailOutcomeCards({
  runId,
  hasGoldenManifest,
  findingCountDisplay,
  artifactCount,
  unresolvedIssueCountDisplay,
}: RunDetailOutcomeCardsProps) {
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
        </CardContent>
      </Card>

      <Card className="border-neutral-200 dark:border-neutral-800">
        <CardHeader className="pb-2">
          <CardTitle className="text-sm font-semibold text-neutral-900 dark:text-neutral-100">
            Findings
          </CardTitle>
          <CardDescription>Surfaced recommendations</CardDescription>
        </CardHeader>
        <CardContent className="pt-0">
          <p className="m-0 text-lg font-semibold tabular-nums text-neutral-900 dark:text-neutral-100">
            {findingCountDisplay === null ? "—" : findingCountDisplay}
          </p>
          {unresolvedIssueCountDisplay !== null && unresolvedIssueCountDisplay > 0 ? (
            <p className="mt-1 text-xs text-neutral-600 dark:text-neutral-400">
              {unresolvedIssueCountDisplay} unresolved on manifest
            </p>
          ) : (
            <p className="mt-1 text-xs text-neutral-600 dark:text-neutral-400">From aggregate explanation</p>
          )}
        </CardContent>
      </Card>

      <Card className="border-neutral-200 dark:border-neutral-800">
        <CardHeader className="pb-2">
          <CardTitle className="text-sm font-semibold text-neutral-900 dark:text-neutral-100">
            Artifacts
          </CardTitle>
          <CardDescription>Generated outputs</CardDescription>
        </CardHeader>
        <CardContent className="pt-0">
          <p className="m-0 text-lg font-semibold tabular-nums text-neutral-900 dark:text-neutral-100">{artifactCount}</p>
          <p className="mt-1 text-xs text-neutral-600 dark:text-neutral-400">
            Attached to manifest when finalized
          </p>
        </CardContent>
      </Card>

      <Card className="border-neutral-200 dark:border-neutral-800">
        <CardHeader className="pb-2">
          <CardTitle className="text-sm font-semibold text-neutral-900 dark:text-neutral-100">
            Review trail
          </CardTitle>
          <CardDescription>Pipeline + provenance</CardDescription>
        </CardHeader>
        <CardContent className="pt-0">
          <Link
            className="text-sm font-medium text-teal-800 underline underline-offset-2 hover:text-teal-900 dark:text-teal-300 dark:hover:text-teal-200"
            href={`/runs/${encodeURIComponent(runId)}/provenance`}
          >
            Open review trail
          </Link>
          <p className="mt-2 text-xs text-neutral-600 dark:text-neutral-400">
            Timeline and trace details stay below — start here for the proof path.
          </p>
        </CardContent>
      </Card>
    </section>
  );
}
