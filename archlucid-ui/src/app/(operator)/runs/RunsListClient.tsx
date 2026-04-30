"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";

import { InspectorPanel } from "@/components/InspectorPanel";
import { RunInspectorPreview } from "@/components/RunInspectorPreview";
import { RunProvenanceInline } from "@/components/RunProvenanceInline";
import { RunTableRowErrorBoundary } from "@/components/RunTableRowErrorBoundary";
import { RunStatusBadge } from "@/components/RunStatusBadge";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { Label } from "@/components/ui/label";
import { useViewportNarrow } from "@/hooks/useViewportNarrow";
import { partitionRunsIntoWorkQueueSections, workQueueSectionHeading } from "@/lib/run-work-queue-groups";
import { formatRelativeTime } from "@/lib/relative-time";
import { isNextPublicDemoMode } from "@/lib/demo-ui-env";
import { formatOperatorProjectIdDisplay } from "@/lib/operator-project-display";
import { SHOWCASE_STATIC_DEMO_RUN_ID, SHOWCASE_STATIC_DEMO_SPINE_COUNTS } from "@/lib/showcase-static-demo";
import { cn } from "@/lib/utils";
import type { RunSummary } from "@/types/authority";

export type RunsListClientProps = {
  runs: RunSummary[];
  projectId: string;
  page: number;
  pageSize: number;
  totalCount: number;
  /** From keyset `GET .../runs`; required on Next for page 2+ when the API uses cursor paging. */
  nextCursor?: string | null;
};

type SortOrder = "createdDesc" | "createdAsc";

function totalPages(totalCount: number, pageSize: number): number {
  return Math.max(1, Math.ceil(totalCount / pageSize));
}

function runRowNumericCountsLine(run: RunSummary): string | null {
  const fc = run.findingCount;
  const wc = run.warningCount;
  const ac = run.artifactCount;
  const hasFinding = typeof fc === "number" && Number.isFinite(fc);
  const hasWarning = typeof wc === "number" && Number.isFinite(wc);
  const hasArtifact = typeof ac === "number" && Number.isFinite(ac);

  if (!hasFinding && !hasWarning && !hasArtifact) {
    return null;
  }

  const tokens: string[] = [];

  if (hasFinding) {
    tokens.push(`${fc} findings`);
  }

  if (hasWarning) {
    tokens.push(`${wc} warnings`);
  }

  if (hasArtifact) {
    tokens.push(`${ac} artifacts`);
  }

  return tokens.join(" · ");
}

function runRowExplicitCountsLine(run: RunSummary): string | null {
  if (isNextPublicDemoMode() && run.runId.trim() === SHOWCASE_STATIC_DEMO_RUN_ID) {
    const c = SHOWCASE_STATIC_DEMO_SPINE_COUNTS;

    return `${c.findingCount} findings · ${c.warningCount} warnings · manifest ${run.hasGoldenManifest ? "finalized" : "pending"}`;
  }

  const numeric = runRowNumericCountsLine(run);

  if (numeric !== null) {
    return `${numeric} · manifest ${run.hasGoldenManifest ? "finalized" : "pending"}`;
  }

  return null;
}

function runRowOutputReadinessLine(run: RunSummary): string {
  const tokens: string[] = [];

  if (run.hasFindingsSnapshot) {
    tokens.push("Findings captured");
  }

  if (run.hasGoldenManifest) {
    tokens.push("Manifest finalized");
  }

  if (run.hasArtifactBundle) {
    tokens.push("Artifacts bundled");
  }

  const reviewTrailSummary =
    run.hasContextSnapshot === true &&
    run.hasGraphSnapshot === true &&
    run.hasFindingsSnapshot === true &&
    run.hasGoldenManifest === true
      ? "Review trail complete"
      : run.hasContextSnapshot === true ||
          run.hasGraphSnapshot === true ||
          run.hasFindingsSnapshot === true ||
          run.hasGoldenManifest === true
        ? "Review trail partial"
        : "Review trail: not started";

  if (tokens.length === 0) {
    return `Output: in progress · ${reviewTrailSummary}`;
  }

  return `${tokens.join(" · ")} · ${reviewTrailSummary}`;
}

function inspectorTitle(run: RunSummary | null): string {
  if (run === null) {
    return "Run preview";
  }

  const d = run.description?.trim() ?? "";

  if (d.length > 0) {
    return d;
  }

  return "Untitled run";
}

function runListPrimaryTitle(run: RunSummary): string {
  const d = run.description?.trim() ?? "";

  if (d.length > 0) {
    return d;
  }

  return "Untitled run";
}

function activateRowKeyboard(e: React.KeyboardEvent<HTMLTableRowElement>, run: RunSummary, select: (r: RunSummary) => void) {
  if (e.key !== "Enter" && e.key !== " ") {
    return;
  }

  if ((e.target as HTMLElement).closest("a")) {
    return;
  }

  e.preventDefault();
  select(run);
}

function displayRelativeCreated(run: RunSummary): string {
  if (isNextPublicDemoMode()) {
    return new Date(run.createdUtc).toLocaleDateString(undefined, {
      year: "numeric",
      month: "short",
      day: "numeric",
    });
  }

  return formatRelativeTime(run.createdUtc);
}

/**
 * Client-side filter and sort for the current server page of runs; pagination remains server URLs.
 * Large viewports show an inline inspector; smaller viewports use a slide-over sheet.
 */
export function RunsListClient({
  runs,
  projectId,
  page,
  pageSize,
  totalCount,
  nextCursor = null,
}: RunsListClientProps) {
  const safeRuns = useMemo(() => {
    return runs.filter((run) => {
      if (typeof run.runId !== "string" || run.runId.trim().length === 0) {
        return false;
      }

      if (typeof run.createdUtc !== "string" || run.createdUtc.trim().length === 0) {
        return false;
      }

      return true;
    });
  }, [runs]);

  const [filterText, setFilterText] = useState("");
  const [sortOrder, setSortOrder] = useState<SortOrder>("createdDesc");
  const [selectedRun, setSelectedRun] = useState<RunSummary | null>(() => (safeRuns.length > 0 ? safeRuns[0] : null));
  const viewportNarrow = useViewportNarrow();

  useEffect(() => {
    if (safeRuns.length === 0) {
      setSelectedRun(null);

      return;
    }

    setSelectedRun((current) => {
      if (current !== null && safeRuns.some((r) => r.runId === current.runId)) {
        return current;
      }

      return safeRuns[0] ?? null;
    });
  }, [safeRuns]);

  const closeInspector = useCallback(() => {
    setSelectedRun(null);
  }, []);

  useEffect(() => {
    if (selectedRun === null) {
      return;
    }

    function onKeyDown(e: KeyboardEvent) {
      if (e.key === "Escape") {
        closeInspector();
      }
    }

    window.addEventListener("keydown", onKeyDown);

    return () => {
      window.removeEventListener("keydown", onKeyDown);
    };
  }, [selectedRun, closeInspector]);

  const filteredSorted = useMemo(() => {
    const query = filterText.trim().toLowerCase();
    let list = safeRuns;

    if (query.length > 0) {
      list = list.filter((run) => {
        const idMatch = run.runId.toLowerCase().includes(query);
        const desc = (run.description ?? "").toLowerCase();

        return idMatch || desc.includes(query);
      });
    }

    return [...list].sort((left, right) => {
      const leftTime = new Date(left.createdUtc).getTime();
      const rightTime = new Date(right.createdUtc).getTime();

      return sortOrder === "createdDesc" ? rightTime - leftTime : leftTime - rightTime;
    });
  }, [safeRuns, filterText, sortOrder]);

  const workQueueSections = useMemo(
    () => partitionRunsIntoWorkQueueSections(filteredSorted),
    [filteredSorted],
  );

  const pages = totalPages(totalCount, pageSize);
  const baseQuery = `projectId=${encodeURIComponent(projectId)}&pageSize=${pageSize}`;
  const previousHref = `/runs?${baseQuery}&page=1`;
  const nextHref =
    nextCursor !== null && nextCursor !== undefined && nextCursor.length > 0
      ? `/runs?${baseQuery}&page=${page + 1}&cursor=${encodeURIComponent(nextCursor)}`
      : `/runs?${baseQuery}&page=${page + 1}`;

  const onRowActivate = useCallback((run: RunSummary, e: React.MouseEvent<HTMLTableRowElement>) => {
    if ((e.target as HTMLElement).closest("a")) {
      return;
    }

    setSelectedRun(run);
  }, []);

  const inspectorBody =
    selectedRun === null ? (
      <p className="m-0 text-sm text-neutral-600 dark:text-neutral-400" data-testid="run-inspector-empty">
        Select a run to preview details here.
      </p>
    ) : (
      <RunInspectorPreview run={selectedRun} />
    );

  return (
    <div className="mt-4 space-y-4">
      <div className="flex flex-col gap-3 sm:flex-row sm:flex-wrap sm:items-end">
        <div className="flex min-w-[12rem] max-w-md flex-1 flex-col gap-1">
          <Label htmlFor="runs-filter-input">Filter (run id or description)</Label>
          <input
            id="runs-filter-input"
            type="search"
            value={filterText}
            onChange={(event) => {
              setFilterText(event.target.value);
            }}
            className="rounded-md border border-neutral-300 bg-white px-3 py-2 text-sm text-neutral-900 shadow-sm focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-teal-700 dark:border-neutral-600 dark:bg-neutral-900 dark:text-neutral-100"
            autoComplete="off"
            aria-label="Filter runs by run id or description"
          />
        </div>
        <div className="flex flex-col gap-1">
          <Label htmlFor="runs-sort-select">Sort by created</Label>
          <select
            id="runs-sort-select"
            value={sortOrder}
            onChange={(event) => {
              setSortOrder(event.target.value as SortOrder);
            }}
            className="rounded-md border border-neutral-300 bg-white px-3 py-2 text-sm dark:border-neutral-600 dark:bg-neutral-900 dark:text-neutral-100"
            aria-label="Sort runs by created date"
          >
            <option value="createdDesc">Newest first</option>
            <option value="createdAsc">Oldest first</option>
          </select>
        </div>
        <p
          className="text-sm text-neutral-600 dark:text-neutral-400"
          aria-live="polite"
          aria-atomic="true"
        >
          Showing {filteredSorted.length} of {safeRuns.length} on this page
          {filterText.trim().length > 0 ? " (filtered)" : ""}
        </p>
      </div>

      <div className={cn(!viewportNarrow && "lg:flex lg:items-stretch lg:gap-4")}>
        <div className={cn("min-w-0 flex-1 space-y-4", !viewportNarrow && "lg:min-w-0")}>
          <div className="space-y-8">
            {filteredSorted.length === 0 ? (
              <div className="overflow-x-auto rounded-md border border-neutral-200 dark:border-neutral-800">
                <table className="w-full border-collapse text-sm">
                  <thead>
                    <tr className="border-b border-neutral-200 bg-neutral-50/80 dark:border-neutral-800 dark:bg-neutral-900/40">
                      <th className="px-3 py-2 text-left text-xs font-semibold uppercase tracking-wide text-neutral-600 dark:text-neutral-400">
                        Run
                      </th>
                      <th className="px-3 py-2 text-left text-xs font-semibold uppercase tracking-wide text-neutral-600 dark:text-neutral-400">
                        Created
                      </th>
                      <th className="px-3 py-2 text-left text-xs font-semibold uppercase tracking-wide text-neutral-600 dark:text-neutral-400">
                        Actions
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr>
                      <td className="px-3 py-2 text-neutral-600 dark:text-neutral-400" colSpan={3}>
                        No runs match this filter.
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            ) : null}

            {workQueueSections.map((section) => {
              const headingId = `runs-queue-${section.groupId}`;

              return (
                <section key={section.groupId} aria-labelledby={headingId} className="space-y-2">
                  <h3
                    id={headingId}
                    className="m-0 text-xs font-semibold uppercase tracking-wide text-neutral-600 dark:text-neutral-400"
                  >
                    {workQueueSectionHeading(section.groupId)}
                  </h3>
                  <div className="overflow-x-auto rounded-md border border-neutral-200 dark:border-neutral-800">
                    <table className="w-full border-collapse text-sm">
                      <thead>
                        <tr className="border-b border-neutral-200 bg-neutral-50/80 dark:border-neutral-800 dark:bg-neutral-900/40">
                          <th className="px-3 py-2 text-left text-xs font-semibold uppercase tracking-wide text-neutral-600 dark:text-neutral-400">
                            Run
                          </th>
                          <th className="px-3 py-2 text-left text-xs font-semibold uppercase tracking-wide text-neutral-600 dark:text-neutral-400">
                            Created
                          </th>
                          <th className="px-3 py-2 text-left text-xs font-semibold uppercase tracking-wide text-neutral-600 dark:text-neutral-400">
                            Actions
                          </th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-neutral-100 dark:divide-neutral-800">
                        {section.runs.map((run) => {
                          const createdLabel = new Date(run.createdUtc).toLocaleString();
                          const isSelected = selectedRun?.runId === run.runId;
                          const title = runListPrimaryTitle(run);
                          const countsLine = runRowExplicitCountsLine(run);

                          return (
                            <RunTableRowErrorBoundary key={run.runId} runId={run.runId}>
                              <tr
                                data-testid={`runs-row-${run.runId}`}
                                tabIndex={0}
                                className={cn(
                                  "cursor-pointer outline-none transition-colors focus-visible:ring-2 focus-visible:ring-teal-600 focus-visible:ring-offset-2 focus-visible:ring-offset-white dark:focus-visible:ring-offset-neutral-950",
                                  isSelected
                                    ? "bg-teal-50/80 dark:bg-teal-950/30"
                                    : "hover:bg-neutral-50 dark:hover:bg-neutral-800",
                                )}
                                onClick={(e) => {
                                  onRowActivate(run, e);
                                }}
                                onKeyDown={(e) => {
                                  activateRowKeyboard(e, run, setSelectedRun);
                                }}
                              >
                                <td className="max-w-[min(100vw,28rem)] px-3 py-2 align-top">
                                  <div className="flex min-w-0 flex-wrap items-center gap-x-2 gap-y-1">
                                    <span className="min-w-0 font-semibold text-sm text-neutral-900 dark:text-neutral-100">
                                      {title}
                                    </span>
                                    <Tooltip>
          <TooltipTrigger asChild>
            <span className="inline-flex shrink-0 cursor-help">
              <RunStatusBadge run={run} />
            </span>
          </TooltipTrigger>
          <TooltipContent side="top" className="max-w-xs">
            Pipeline phase derived from snapshots: finalized manifest, findings ready to finalize, still executing, or
            just starting.
          </TooltipContent>
        </Tooltip>
                                  </div>
                                  <code className="mt-1 block break-all font-mono text-xs text-neutral-500 dark:text-neutral-400">
                                    {run.runId}
                                  </code>
                                  {run.projectId !== projectId ? (
                                    <p className="m-0 mt-0.5 text-xs text-neutral-500 dark:text-neutral-400">
                                      Project{" "}
                                      <span className="font-mono">{formatOperatorProjectIdDisplay(run.projectId)}</span>
                                    </p>
                                  ) : null}
                                  <div className="mt-1.5">
                                    <RunProvenanceInline run={run} />
                                  </div>
                                  {countsLine !== null ? (
                                    <p
                                      className="m-0 mt-1 text-[11px] font-medium text-neutral-700 dark:text-neutral-300"
                                      data-testid={`runs-row-counts-${run.runId}`}
                                    >
                                      {countsLine}
                                    </p>
                                  ) : null}
                                  <p
                                    className="m-0 mt-1 text-[11px] text-neutral-600 dark:text-neutral-400"
                                    data-testid={`runs-row-readiness-${run.runId}`}
                                  >
                                    {runRowOutputReadinessLine(run)}
                                  </p>
                                </td>
                                <td
                                  className="whitespace-nowrap px-3 py-2 align-top text-xs text-neutral-600 dark:text-neutral-400"
                                  title={createdLabel}
                                >
                                  {displayRelativeCreated(run)}
                                </td>
                                <td className="whitespace-nowrap px-3 py-2 align-top">
                                  <Link
                                    href={`/runs/${run.runId}`}
                                    className="font-medium text-teal-800 underline dark:text-teal-300"
                                    onClick={(e) => {
                                      e.stopPropagation();
                                    }}
                                  >
                                    Open run detail
                                  </Link>
                                </td>
                              </tr>
                            </RunTableRowErrorBoundary>
                          );
                        })}
                      </tbody>
                    </table>
                  </div>
                </section>
              );
            })}
          </div>

          <nav
            className="mt-5 flex flex-wrap items-center gap-4 text-sm"
            aria-label="Runs pagination"
          >
            <span className="text-neutral-600 dark:text-neutral-400">
              Page {page} of {pages} · {totalCount} run{totalCount === 1 ? "" : "s"} total
            </span>
            {page > 1 ? (
              <Link
                className="font-semibold text-teal-800 underline dark:text-teal-300"
                href={previousHref}
                aria-label={page === 2 ? "Previous page" : "First page (keyset pagination)"}
              >
                {page === 2 ? "Previous" : "First page"}
              </Link>
            ) : (
              <button
                type="button"
                disabled
                className={cn(
                  "cursor-not-allowed font-semibold text-neutral-400 dark:text-neutral-500",
                )}
              >
                Previous
              </button>
            )}
            {page < pages ? (
              <Link
                className="font-semibold text-teal-800 underline dark:text-teal-300"
                href={nextHref}
              >
                Next
              </Link>
            ) : (
              <button
                type="button"
                disabled
                className={cn(
                  "cursor-not-allowed font-semibold text-neutral-400 dark:text-neutral-500",
                )}
              >
                Next
              </button>
            )}
          </nav>
        </div>

        {!viewportNarrow ? (
          <InspectorPanel
            title={inspectorTitle(selectedRun)}
            onClose={closeInspector}
            listenEscape={false}
            className="mt-4 hidden min-h-[16rem] shrink-0 lg:mt-0 lg:flex"
          >
            {inspectorBody}
          </InspectorPanel>
        ) : null}
      </div>

      {viewportNarrow && selectedRun !== null ? (
        <div className="fixed inset-0 z-40 flex justify-end" role="presentation">
          <button
            type="button"
            className="absolute inset-0 bg-black/40"
            aria-label="Dismiss inspector backdrop"
            onClick={closeInspector}
          />
          <div className="animate-in slide-in-from-right relative h-full w-full max-w-sm duration-200 ease-out">
            <InspectorPanel
              title={inspectorTitle(selectedRun)}
              onClose={closeInspector}
              listenEscape={false}
              className="h-full max-w-sm border-l-0 shadow-xl sm:border-l"
              widthClassName="w-full"
            >
              {inspectorBody}
            </InspectorPanel>
          </div>
        </div>
      ) : null}
    </div>
  );
}
