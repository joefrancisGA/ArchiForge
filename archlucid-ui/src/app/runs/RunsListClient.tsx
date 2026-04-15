"use client";

import Link from "next/link";
import { useMemo, useState } from "react";

import { Label } from "@/components/ui/label";
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

/**
 * Client-side filter and sort for the current server page of runs; pagination remains server URLs.
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

      <div className="overflow-x-auto">
        <table className="w-full border-collapse text-sm">
          <thead>
            <tr className="border-b border-neutral-300 dark:border-neutral-600">
              <th className="p-2 text-left font-semibold">Run ID</th>
              <th className="p-2 text-left font-semibold">Description</th>
              <th className="p-2 text-left font-semibold">Created</th>
              <th className="p-2 text-left font-semibold">Actions</th>
            </tr>
          </thead>
          <tbody>
            {filteredSorted.map((run) => (
              <tr key={run.runId} className="border-b border-neutral-200 dark:border-neutral-700">
                <td className="p-2 font-mono text-xs">{run.runId}</td>
                <td className="p-2">{run.description ?? ""}</td>
                <td className="p-2">{new Date(run.createdUtc).toLocaleString()}</td>
                <td className="p-2">
                  <Link href={`/runs/${run.runId}`} className="font-medium text-teal-800 underline dark:text-teal-300">
                    Open run
                  </Link>
                </td>
              </tr>
            ))}
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
  );
}
