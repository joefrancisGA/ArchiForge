"use client";

import Link from "next/link";

import { CopyIdButton } from "@/components/CopyIdButton";
import { RunStatusBadge } from "@/components/RunStatusBadge";
import { Button } from "@/components/ui/button";
import { isNextPublicDemoMode } from "@/lib/demo-ui-env";
import {
  SHOWCASE_STATIC_DEMO_MANIFEST_ID,
  SHOWCASE_STATIC_DEMO_PRIMARY_FINDING_ID,
  SHOWCASE_STATIC_DEMO_RUN_ID,
  SHOWCASE_STATIC_DEMO_SPINE_COUNTS,
} from "@/lib/showcase-static-demo";
import type { RunSummary } from "@/types/authority";

function snapshotLabel(ok: boolean | undefined): string {
  if (ok === true) {
    return "✓";
  }

  return "—";
}

export type RunInspectorPreviewProps = {
  run: RunSummary;
};

/**
 * Read-only run preview for list inspectors — uses only {@link RunSummary} fields from the list payload.
 */
export function RunInspectorPreview({ run }: RunInspectorPreviewProps) {
  const demo = isNextPublicDemoMode();
  const createdLabel = new Date(run.createdUtc).toLocaleString();
  const compareHref = `/compare?leftRunId=${encodeURIComponent(run.runId)}`;
  const replayHref = `/replay?runId=${encodeURIComponent(run.runId)}`;
  const manifestId = run.goldenManifestId ?? SHOWCASE_STATIC_DEMO_MANIFEST_ID;
  const showcaseStory = run.runId.trim() === SHOWCASE_STATIC_DEMO_RUN_ID;
  const findingHref = `/runs/${encodeURIComponent(run.runId)}/findings/${encodeURIComponent(SHOWCASE_STATIC_DEMO_PRIMARY_FINDING_ID)}`;
  const artifactNote =
    showcaseStory && demo
      ? `${SHOWCASE_STATIC_DEMO_SPINE_COUNTS.decisionCount} decisions · ${SHOWCASE_STATIC_DEMO_SPINE_COUNTS.findingCount} findings · ${SHOWCASE_STATIC_DEMO_SPINE_COUNTS.warningCount} warnings (demo totals)`
      : run.hasArtifactBundle
        ? "Artifact bundle attached — see run detail"
        : "Artifact bundle not reported in list payload";

  return (
    <div className="space-y-4 text-sm text-neutral-800 dark:text-neutral-200" data-testid="run-inspector-preview">
      <div className="flex flex-wrap items-center gap-2">
        <RunStatusBadge run={run} />
      </div>

      <dl className="m-0 grid gap-2 sm:grid-cols-[minmax(5rem,auto)_1fr] sm:gap-x-3">
        <dt className="text-xs font-medium uppercase tracking-wide text-neutral-500 dark:text-neutral-400">Run ID</dt>
        <dd className="m-0 flex min-w-0 items-center gap-1">
          <code className="truncate font-mono text-[11px] text-neutral-900 dark:text-neutral-100">{run.runId}</code>
          <CopyIdButton value={run.runId} aria-label="Copy run ID" />
        </dd>
        <dt className="text-xs font-medium uppercase tracking-wide text-neutral-500 dark:text-neutral-400">Project</dt>
        <dd className="m-0 font-mono text-xs">{run.projectId}</dd>
        <dt className="text-xs font-medium uppercase tracking-wide text-neutral-500 dark:text-neutral-400">Created</dt>
        <dd className="m-0">{createdLabel}</dd>
      </dl>

      <div>
        <p className="m-0 text-xs font-semibold uppercase tracking-wide text-neutral-500 dark:text-neutral-400">
          Pipeline output
        </p>
        <p className="m-0 mt-1 text-xs text-neutral-700 dark:text-neutral-200">{artifactNote}</p>
        <ul className="m-0 mt-2 list-none space-y-1 p-0 text-xs">
          <li className="flex justify-between gap-2">
            <span>Context captured</span>
            <span aria-label={run.hasContextSnapshot ? "Context snapshot present" : "Context snapshot missing"}>
              {snapshotLabel(run.hasContextSnapshot)}
            </span>
          </li>
          <li className="flex justify-between gap-2">
            <span>Graph generated</span>
            <span aria-label={run.hasGraphSnapshot ? "Graph snapshot present" : "Graph snapshot missing"}>
              {snapshotLabel(run.hasGraphSnapshot)}
            </span>
          </li>
          <li className="flex justify-between gap-2">
            <span>Findings reviewed</span>
            <span aria-label={run.hasFindingsSnapshot ? "Findings snapshot present" : "Findings snapshot missing"}>
              {snapshotLabel(run.hasFindingsSnapshot)}
            </span>
          </li>
          <li className="flex justify-between gap-2">
            <span>Manifest finalized</span>
            <span aria-label={run.hasGoldenManifest ? "Reviewed manifest present" : "Reviewed manifest missing"}>
              {snapshotLabel(run.hasGoldenManifest)}
            </span>
          </li>
        </ul>
      </div>

      <div>
        <p className="m-0 text-xs font-semibold uppercase tracking-wide text-neutral-500 dark:text-neutral-400">
          Quick navigation
        </p>
        <div className="mt-2 flex flex-wrap gap-2">
          <Button variant="outline" size="sm" className="h-8" asChild>
            <Link href={`/manifests/${encodeURIComponent(manifestId)}`}>Finalized manifest</Link>
          </Button>
          {showcaseStory ? (
            <Button variant="outline" size="sm" className="h-8" asChild>
              <Link href={findingHref}>Primary finding</Link>
            </Button>
          ) : null}
          <Button variant="outline" size="sm" className="h-8" asChild>
            <Link href={`/replay?runId=${encodeURIComponent(run.runId)}`}>Review trail (replay)</Link>
          </Button>
        </div>
      </div>

      <div className="flex flex-col gap-2 border-t border-neutral-200 pt-3 dark:border-neutral-700">
        <Button variant="primary" size="sm" className="w-full sm:w-auto" asChild>
          <Link href={`/runs/${run.runId}`}>Open run detail</Link>
        </Button>
        <div className="flex flex-wrap gap-2">
          <Button variant="outline" size="sm" asChild>
            <Link href={compareHref}>Compare</Link>
          </Button>
          <Button variant="outline" size="sm" asChild>
            <Link href={replayHref}>Replay</Link>
          </Button>
        </div>
      </div>
    </div>
  );
}
