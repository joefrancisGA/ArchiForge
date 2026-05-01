import Link from "next/link";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import {
  SHOWCASE_STATIC_DEMO_DECISION_SYNOPSES,
  SHOWCASE_STATIC_DEMO_MANIFEST_ID,
  SHOWCASE_STATIC_DEMO_RUN_ID,
} from "@/lib/showcase-static-demo";
import type { ManifestSummary } from "@/types/authority";

export type ManifestTopDecisionsCardProps = {
  readonly summary: ManifestSummary;
};

function isShowcaseManifest(summary: ManifestSummary): boolean {
  return (
    summary.manifestId === SHOWCASE_STATIC_DEMO_MANIFEST_ID || summary.runId.trim() === SHOWCASE_STATIC_DEMO_RUN_ID
  );
}

/**
 * Surfaces actionable decision excerpts for the curated Claims Intake showcase; for other manifests, links onward
 * when decisionCount is non-zero (API does not yet return individual decision bullets on ManifestSummary — see backlog).
 */
export function ManifestTopDecisionsCard(props: ManifestTopDecisionsCardProps) {
  const { summary } = props;

  if (!isShowcaseManifest(summary)) {
    if (summary.decisionCount <= 0) {
      return null;
    }

    return (
      <Card>
        <CardHeader>
          <CardTitle className="text-base font-semibold">Architectural decisions</CardTitle>
          <CardDescription>
            This manifest records <strong>{summary.decisionCount}</strong> decision
            {summary.decisionCount === 1 ? "" : "s"} — review the originating run for full evidence and narration.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Button variant="outline" size="sm" asChild>
            <Link href={`/reviews/${encodeURIComponent(summary.runId)}#run-explanation`}>Open decisions on run</Link>
          </Button>
        </CardContent>
      </Card>
    );
  }

  const topThree = SHOWCASE_STATIC_DEMO_DECISION_SYNOPSES.slice(0, 3);
  const remainder = SHOWCASE_STATIC_DEMO_DECISION_SYNOPSES.slice(3);

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base font-semibold">Top decisions</CardTitle>
        <CardDescription>Preview of key architecture choices captured in this manifest.</CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <ul className="m-0 list-none space-y-2 p-0">
          {topThree.map((line) => (
            <li
              key={line}
              className="rounded-md border border-neutral-200 bg-neutral-50/80 px-3 py-2 text-sm text-neutral-800 dark:border-neutral-700 dark:bg-neutral-900/40 dark:text-neutral-200"
            >
              {line}
            </li>
          ))}
        </ul>

        {remainder.length > 0 ? (
          <details className="rounded-md border border-neutral-200 dark:border-neutral-700">
            <summary className="cursor-pointer px-3 py-2 text-sm font-medium text-neutral-900 dark:text-neutral-100">
              Show all decisions ({SHOWCASE_STATIC_DEMO_DECISION_SYNOPSES.length} total)
            </summary>
            <ul className="m-0 list-none space-y-2 border-t border-neutral-200 p-3 dark:border-neutral-700">
              {remainder.map((line) => (
                <li key={line} className="text-sm text-neutral-700 dark:text-neutral-300">
                  {line}
                </li>
              ))}
            </ul>
          </details>
        ) : null}
      </CardContent>
    </Card>
  );
}
