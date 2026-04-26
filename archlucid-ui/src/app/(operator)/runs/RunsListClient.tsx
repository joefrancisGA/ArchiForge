"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";

import { InspectorPanel } from "@/components/InspectorPanel";
import { RunInspectorPreview } from "@/components/RunInspectorPreview";
import { RunStatusBadge } from "@/components/RunStatusBadge";
import { Label } from "@/components/ui/label";
import { useViewportNarrow } from "@/hooks/useViewportNarrow";
import { formatRelativeTime } from "@/lib/relative-time";
import { cn } from "@/lib/utils";
import type { RunSummary } from "@/types/authority";

export type RunsListClientProps = {
  runs: RunSummary[];
  projectId: string;
  page: number;
  pageSize: number;
  totalCount: number;
};

type SortOrder = "createdDesc" | "createdAsc";

function totalPages(totalCount: number, pageSize: number): number {
  return Math.max(1, Math.ceil(totalCount / pageSize));
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
}: RunsListClientProps) {
  const [filterText, setFilterText] = useState("");
  const [sortOrder, setSortOrder] = useState<SortOrder>("createdDesc");
  const [selectedRun, setSelectedRun] = useState<RunSummary | null>(null);
  const viewportNarrow = useViewportNarrow();

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
    let list = runs;

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
  }, [runs, filterText, sortOrder]);

  const pages = totalPages(totalCount, pageSize);
  const baseQuery = `projectId=${encodeURIComponent(projectId)}&pageSize=${pageSize}`;

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
          Showing {filteredSorted.length} of {runs.length} on this page
          {filterText.trim().length > 0 ? " (filtered)" : ""}
        </p>
      </div>

      <div className={cn(!viewportNarrow && "lg:flex lg:items-stretch lg:gap-4")}>
        <div className={cn("min-w-0 flex-1 space-y-4", !viewportNarrow && "lg:min-w-0")}>
          <div className="overflow-x-auto">
            <table className="w-full border-collapse text-sm">
              <thead>
                <tr className="border-b border-neutral-300 dark:border-neutral-600">
                  <th className="p-2 text-left font-semibold">Run ID</th>
                  <th className="p-2 text-left font-semibold">Status</th>
                  <th className="p-2 text-left font-semibold">Description</th>
                  <th className="p-2 text-left font-semibold">Created</th>
                  <th className="p-2 text-left font-semibold">Actions</th>
                </tr>
              </thead>
              <tbody>
                {filteredSorted.map((run) => {
                  const created = new Date(run.createdUtc);
                  const createdLabel = created.toLocaleString();
                  const isSelected = selectedRun?.runId === run.runId;

                  return (
                    <tr
                      key={run.runId}
                      data-testid={`runs-row-${run.runId}`}
                      className={cn(
                        "cursor-pointer border-b border-neutral-200 transition-colors dark:border-neutral-700",
                        isSelected
                          ? "bg-teal-50/80 dark:bg-teal-950/30"
                          : "hover:bg-neutral-50 dark:hover:bg-neutral-900/50",
                      )}
                      onClick={(e) => {
                        onRowActivate(run, e);
                      }}
                    >
                      <td className="max-w-[11rem] p-2 align-top">
                        <code className="block break-all font-mono text-[11px] leading-snug text-neutral-800 dark:text-neutral-200">
                          {run.runId}
                        </code>
                      </td>
                      <td className="p-2 align-middle">
                        <RunStatusBadge run={run} />
                      </td>
                      <td className="p-2 align-top text-neutral-800 dark:text-neutral-200">{run.description ?? ""}</td>
                      <td className="p-2 align-top text-sm text-neutral-700 dark:text-neutral-300" title={createdLabel}>
                        <span className="block">{formatRelativeTime(run.createdUtc)}</span>
                        <span className="mt-0.5 block text-xs text-neutral-500 dark:text-neutral-500">{createdLabel}</span>
                      </td>
                      <td className="p-2 align-top">
                        <Link
                          href={`/runs/${run.runId}`}
                          className="font-medium text-teal-800 underline dark:text-teal-300"
                          onClick={(e) => {
                            e.stopPropagation();
                          }}
                        >
                          Open run
                        </Link>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
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
                href={`/runs?${baseQuery}&page=${page - 1}`}
              >
                Previous
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
                href={`/runs?${baseQuery}&page=${page + 1}`}
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
