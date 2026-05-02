"use client";

import Link from "next/link";
import { useState } from "react";

import { CopyIdButton } from "@/components/CopyIdButton";
import { RunStatusBadge } from "@/components/RunStatusBadge";
import { Button } from "@/components/ui/button";
import { isNextPublicDemoMode } from "@/lib/demo-ui-env";
import { formatOperatorProjectIdDisplay } from "@/lib/operator-project-display";
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
  const [moreOpen, setMoreOpen] = useState(false);
  const [technicalOpen, setTechnicalOpen] = useState(false);
  const demo = isNextPublicDemoMode();
  const showcaseStory = run.runId.trim() === SHOWCASE_STATIC_DEMO_RUN_ID;
  const headline = run.description?.trim() ?? "Architecture review";
  const createdLabel = showcaseStory
    ? demo
      ? "Sample review (illustrative)"
      : new Date(run.createdUtc).toLocaleDateString(undefined, {
          year: "numeric",
          month: "short",
          day: "numeric",
        })
    : new Date(run.createdUtc).toLocaleString();
  const compareHref = `/compare?leftRunId=${encodeURIComponent(run.runId)}`;
  const replayHref = `/replay?runId=${encodeURIComponent(run.runId)}`;
  const manifestId = run.goldenManifestId ?? SHOWCASE_STATIC_DEMO_MANIFEST_ID;
  const findingHref = `/reviews/${encodeURIComponent(run.runId)}/findings/${encodeURIComponent(SHOWCASE_STATIC_DEMO_PRIMARY_FINDING_ID)}`;
  const artifactNote =
    showcaseStory && demo
      ? `${SHOWCASE_STATIC_DEMO_SPINE_COUNTS.decisionCount} decisions · ${SHOWCASE_STATIC_DEMO_SPINE_COUNTS.findingCount} findings · ${SHOWCASE_STATIC_DEMO_SPINE_COUNTS.warningCount} warnings (demo totals)`
      : run.hasArtifactBundle
        ? "Artifact bundle attached — use the Artifacts quick link on this review."
        : "Artifact bundle not reported in list payload";

  const hasFindingsLink = run.hasFindingsSnapshot === true || showcaseStory;
  const hasArtifactsLink = run.hasArtifactBundle === true || showcaseStory;

  return (
    <div className="space-y-4 text-sm text-neutral-800 dark:text-neutral-200" data-testid="run-inspector-preview">
      <div>
        <p className="m-0 text-sm font-semibold text-neutral-900 dark:text-neutral-100">{headline}</p>
        <button
          type="button"
          className="mt-1 text-xs font-medium text-teal-800 underline dark:text-teal-300"
          onClick={() => setTechnicalOpen((v) => !v)}
          aria-expanded={technicalOpen}
        >
          {technicalOpen ? "Hide technical details" : "Technical details (IDs)"}
        </button>
        {technicalOpen ? (
          <dl className="m-0 mt-2 grid gap-2 sm:grid-cols-[minmax(5rem,auto)_1fr] sm:gap-x-3">
            <dt className="text-xs font-medium uppercase tracking-wide text-neutral-500 dark:text-neutral-400">
              Review ID
            </dt>
            <dd className="m-0 flex min-w-0 items-center gap-1">
              <code className="truncate font-mono text-[11px] text-neutral-900 dark:text-neutral-100">{run.runId}</code>
              <CopyIdButton value={run.runId} aria-label="Copy review ID" />
            </dd>
            <dt className="text-xs font-medium uppercase tracking-wide text-neutral-500 dark:text-neutral-400">Workspace</dt>
            <dd className="m-0 text-xs text-neutral-800 dark:text-neutral-200">
              {formatOperatorProjectIdDisplay(run.projectId)}
            </dd>
            <dt className="text-xs font-medium uppercase tracking-wide text-neutral-500 dark:text-neutral-400">Created</dt>
            <dd className="m-0">{createdLabel}</dd>
          </dl>
        ) : null}
      </div>

      <div className="flex flex-wrap items-center gap-2">
        <RunStatusBadge run={run} />
      </div>

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

      {/* Primary action */}
      <div className="border-t border-neutral-200 pt-3 dark:border-neutral-700">
        <Button variant="primary" size="sm" className="w-full" asChild>
          <Link href={`/reviews/${encodeURIComponent(run.runId)}`}>Open review</Link>
        </Button>
      </div>

      {/* 4 primary quick links: Manifest, Findings, Artifacts, Timeline */}
      <div>
        <p className="m-0 text-xs font-semibold uppercase tracking-wide text-neutral-500 dark:text-neutral-400">
          Quick links
        </p>
        <div className="mt-2 flex flex-wrap gap-2">
          <Button variant="outline" size="sm" className="h-8" asChild>
            <Link href={`/manifests/${encodeURIComponent(manifestId)}`}>Manifest</Link>
          </Button>
          {hasFindingsLink ? (
            <Button variant="outline" size="sm" className="h-8" asChild>
              <Link href={`/reviews/${encodeURIComponent(run.runId)}#run-explanation`}>Findings</Link>
            </Button>
          ) : null}
          {hasArtifactsLink ? (
            <Button variant="outline" size="sm" className="h-8" asChild>
              <Link href={`/reviews/${encodeURIComponent(run.runId)}#artifacts-exports`}>Artifacts</Link>
            </Button>
          ) : null}
          <Button variant="outline" size="sm" className="h-8" asChild>
            <Link href={`/reviews/${encodeURIComponent(run.runId)}#pipeline-timeline`}>Timeline</Link>
          </Button>
        </div>
      </div>

      {/* Secondary actions collapsed behind "More actions" */}
      <div>
        <button
          type="button"
          className="text-xs text-neutral-500 hover:text-neutral-700 dark:text-neutral-400 dark:hover:text-neutral-200"
          onClick={() => setMoreOpen((v) => !v)}
          aria-expanded={moreOpen}
        >
          {moreOpen ? "▾ Less" : "▸ More actions"}
        </button>
        {moreOpen ? (
          <div className="mt-2 flex flex-wrap gap-2">
          {showcaseStory ? (
            <Button variant="outline" size="sm" className="h-8" asChild>
              <Link href={findingHref}>Primary finding</Link>
            </Button>
          ) : null}
          {run.hasGraphSnapshot === true || showcaseStory ? (
            <Button variant="outline" size="sm" className="h-8" asChild>
              <Link href={`/reviews/${encodeURIComponent(run.runId)}/provenance`}>Trail graph</Link>
            </Button>
          ) : null}
            <Button variant="outline" size="sm" className="h-8" asChild>
              <Link href={compareHref}>Compare</Link>
            </Button>
            <Button variant="outline" size="sm" className="h-8" asChild>
              <Link href={replayHref}>Replay</Link>
            </Button>
          </div>
        ) : null}
      </div>
    </div>
  );
}
