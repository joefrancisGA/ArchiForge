"use client";

import Link from "next/link";

import { pipelineEventTypeFriendlyLabel } from "@/lib/pipeline-event-type-labels";
import type { PipelineTimelineItem } from "@/types/authority";

function safeLocaleTime(iso: string): string {
  if (iso.trim().length === 0) {
    return "—";
  }

  const d = new Date(iso);

  if (Number.isNaN(d.getTime())) {
    return "—";
  }

  return d.toLocaleString();
}

function manifestishEvent(eventType: string): boolean {
  return /manifest|commit|finalize|committed|golden|bundle/i.test(eventType);
}

function findingishEvent(eventType: string): boolean {
  return /finding|assessment|risk/i.test(eventType);
}

/**
 * Marketing showcase review-trail as stacked cards (friendlier than a bare list) with optional deep links into the
 * proof chain (run, manifest, primary finding) when identifiers are known.
 */
export function ShowcasePipelineReviewTrailCards(props: {
  readonly items: PipelineTimelineItem[];
  readonly runId: string;
  readonly goldenManifestId: string | null | undefined;
  readonly primaryFindingId?: string;
}) {
  const { items, runId, goldenManifestId, primaryFindingId } = props;
  const manifest =
    typeof goldenManifestId === "string" && goldenManifestId.trim().length > 0 ? goldenManifestId.trim() : null;

  if (items.length === 0) {
    return (
      <p className="m-0 text-sm text-neutral-500 dark:text-neutral-400" data-testid="showcase-pipeline-cards-empty">
        No review-trail events in this preview payload yet.
      </p>
    );
  }

  return (
    <ol
      className="m-0 flex list-none flex-col gap-3 p-0"
      aria-label="Review trail milestones"
      data-testid="showcase-pipeline-review-cards"
    >
      {items.map((row) => {
        const label = pipelineEventTypeFriendlyLabel(row.eventType);
        const showManifest = manifest !== null && manifestishEvent(row.eventType);
        const showFinding =
          primaryFindingId !== undefined &&
          primaryFindingId.trim().length > 0 &&
          findingishEvent(row.eventType);

        return (
          <li
            key={row.eventId}
            className="rounded-lg border border-neutral-200 bg-white/90 p-4 shadow-sm dark:border-neutral-800 dark:bg-neutral-950/50"
          >
            <div className="flex flex-wrap items-start justify-between gap-2">
              <div className="min-w-0">
                <p className="m-0 text-sm font-semibold text-neutral-900 dark:text-neutral-100">{label}</p>
                <time
                  className="mt-1 block text-xs font-medium text-neutral-500 dark:text-neutral-400"
                  dateTime={row.occurredUtc}
                >
                  {safeLocaleTime(row.occurredUtc)}
                </time>
                {row.actorUserName.trim().length > 0 ? (
                  <p className="m-0 mt-1 text-xs text-neutral-600 dark:text-neutral-400">
                    <span className="font-medium text-neutral-700 dark:text-neutral-300">Actor:</span>{" "}
                    {row.actorUserName}
                  </p>
                ) : null}
              </div>
              <div className="flex flex-wrap gap-2 text-xs">
                <Link
                  className="rounded-md border border-neutral-200 bg-neutral-50 px-2 py-1 font-medium text-teal-800 no-underline hover:bg-neutral-100 dark:border-neutral-700 dark:bg-neutral-900 dark:text-teal-300 dark:hover:bg-neutral-800"
                  href={`/reviews/${encodeURIComponent(runId)}`}
                >
                  Open review
                </Link>
                {showManifest ? (
                  <Link
                    className="rounded-md border border-teal-200 bg-teal-50/80 px-2 py-1 font-medium text-teal-900 no-underline hover:bg-teal-100 dark:border-teal-800 dark:bg-teal-950/50 dark:text-teal-200 dark:hover:bg-teal-950/80"
                    href={`/manifests/${encodeURIComponent(manifest)}`}
                  >
                    Manifest
                  </Link>
                ) : null}
                {showFinding ? (
                  <Link
                    className="rounded-md border border-amber-200 bg-amber-50/80 px-2 py-1 font-medium text-amber-950 no-underline hover:bg-amber-100 dark:border-amber-800 dark:bg-amber-950/40 dark:text-amber-100 dark:hover:bg-amber-950/70"
                    href={`/reviews/${encodeURIComponent(runId)}/findings/${encodeURIComponent(primaryFindingId.trim())}`}
                  >
                    Review finding
                  </Link>
                ) : null}
              </div>
            </div>
          </li>
        );
      })}
    </ol>
  );
}
