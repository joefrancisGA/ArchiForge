"use client";

import { useVirtualizer } from "@tanstack/react-virtual";
import Link from "next/link";
import { useMemo, useRef, useState } from "react";

import { CopyTraceRowWorkItemButton } from "@/components/CopyFindingAsWorkItemButton";
import { FindingConfidenceBadge } from "@/components/FindingConfidenceBadge";
import { FindingExplainabilityDialog } from "@/components/FindingExplainabilityDialog";
import { ProductLearningFeedbackControls } from "@/components/ProductLearningFeedbackControls";
import { Button } from "@/components/ui/button";
import { truncateForList } from "@/lib/truncate-for-list";
import type { FindingTraceConfidenceDto } from "@/types/explanation";

export type RunFindingExplainabilityTableProps = {
  runId: string;
  rows: FindingTraceConfidenceDto[];
};

function gapsSummary(row: FindingTraceConfidenceDto): string {
  const m = row.missingTraceFields?.filter((s) => s.trim().length > 0) ?? [];

  if (m.length === 0) {
    return "—";
  }

  if (m.length <= 2) {
    return m.join(", ");
  }

  return `${m[0]}, ${m[1]} +${m.length - 2}`;
}

function confidenceRank(level: FindingTraceConfidenceDto["confidenceLevel"]): number {
  if (level === "High") return 0;

  if (level === "Medium") return 1;

  if (level === "Low") return 2;

  return 3;
}

function compareFindingConfidenceRows(
  a: FindingTraceConfidenceDto,
  b: FindingTraceConfidenceDto,
  reversed: boolean,
): number {
  const ra = confidenceRank(a.confidenceLevel);
  const rb = confidenceRank(b.confidenceLevel);

  if (ra === 3 && rb === 3) return a.findingId.localeCompare(b.findingId);

  if (ra === 3) return 1;

  if (rb === 3) return -1;

  const primary = reversed ? rb - ra : ra - rb;

  if (primary !== 0) return primary;

  return a.findingId.localeCompare(b.findingId);
}

const rowGridClass =
  "grid w-full min-w-[46rem] grid-cols-[minmax(10rem,1.4fr)_minmax(6rem,1fr)_4.5rem_minmax(5rem,0.9fr)_4.5rem_minmax(9rem,1fr)_minmax(7rem,1fr)_minmax(11rem,auto)] gap-x-2 border-b border-neutral-100 px-1 py-2 text-sm last:border-b-0 dark:border-neutral-800";

/**
 * Lists findings with trace completeness from the aggregate explanation payload; opens per-finding explainability.
 */
export function RunFindingExplainabilityTable({ runId, rows }: RunFindingExplainabilityTableProps) {
  const [open, setOpen] = useState(false);
  const [activeFindingId, setActiveFindingId] = useState<string | null>(null);
  const [confidenceSortReversed, setConfidenceSortReversed] = useState(false);
  const parentRef = useRef<HTMLDivElement>(null);

  const sortedRows = useMemo(() => {
    const copy = [...rows];
    copy.sort((a, b) => compareFindingConfidenceRows(a, b, confidenceSortReversed));

    return copy;
  }, [rows, confidenceSortReversed]);

  const rowVirtualizer = useVirtualizer({
    count: sortedRows.length,
    getScrollElement: () => parentRef.current,
    estimateSize: () => 190,
    overscan: 10,
  });

  if (rows.length === 0) {
    return null;
  }

  return (
    <div className="mt-4 rounded-lg border border-neutral-200 bg-neutral-50/80 p-4 dark:border-neutral-700 dark:bg-neutral-900/40">
      <h3 className="m-0 mb-2 text-sm font-semibold text-neutral-900 dark:text-neutral-100">
        Per-finding explainability
      </h3>
      <p className="mb-3 text-xs text-neutral-600 dark:text-neutral-400">
        Open the trace captured for each finding (structured evidence, rules, graph nodes, narrative). Long lists are
        virtualized for smoother scrolling.
      </p>
      <div
        ref={parentRef}
        className="max-h-[min(28rem,70vh)] overflow-auto rounded-lg border border-neutral-200 dark:border-neutral-700"
      >
        <div className={`${rowGridClass} sticky top-0 z-[1] bg-neutral-100 font-semibold text-neutral-800 dark:bg-neutral-900/95 dark:text-neutral-200`}>
          <div>Finding</div>
          <div>Rule id</div>
          <div>Refs</div>
          <div>Trace label</div>
          <div>%</div>
          <div className="min-w-0">
            <button
              type="button"
              className="rounded px-1 text-left hover:underline"
              aria-label={
                confidenceSortReversed
                  ? "Sort evaluation confidence: Low to High, absent last"
                  : "Sort evaluation confidence: High to Low, absent last"
              }
              onClick={() => setConfidenceSortReversed((v) => !v)}
            >
              Confidence
              <span aria-hidden className="ml-0.5 text-[0.65rem] font-normal text-neutral-500 dark:text-neutral-400">
                {confidenceSortReversed ? "↓" : "↑"}
              </span>
            </button>
          </div>
          <div>Trace gaps</div>
          <div>Action</div>
        </div>
        <div
          className="relative w-full"
          style={{
            height: `${rowVirtualizer.getTotalSize()}px`,
          }}
        >
          {rowVirtualizer.getVirtualItems().map((vi) => {
            const row = sortedRows[vi.index]!;
            const pct =
              row.traceCompletenessRatio <= 1
                ? Math.round(row.traceCompletenessRatio * 100)
                : Math.round(row.traceCompletenessRatio);
            const titleFull =
              row.findingTitle !== null &&
              row.findingTitle !== undefined &&
              row.findingTitle.trim().length > 0
                ? row.findingTitle.trim()
                : "(no title)";

            const confidenceSlot =
              row.confidenceLevel === "High" ||
              row.confidenceLevel === "Medium" ||
              row.confidenceLevel === "Low" ? (
                <FindingConfidenceBadge level={row.confidenceLevel} />
              ) : (
                <span className="text-neutral-400 dark:text-neutral-500">—</span>
              );

            return (
              <div
                key={row.findingId}
                className={`${rowGridClass} absolute left-0 top-0 items-start bg-neutral-50/80 dark:bg-neutral-900/30`}
                style={{
                  transform: `translateY(${vi.start}px)`,
                  height: `${vi.size}px`,
                }}
              >
                <div className="min-w-0">
                  <div className="break-all font-mono text-[0.65rem] text-neutral-500 dark:text-neutral-400">
                    {row.findingId}
                  </div>
                  <div
                    className="mt-0.5 text-xs leading-snug text-neutral-800 dark:text-neutral-200"
                    title={titleFull}
                  >
                    {truncateForList(titleFull, 120)}
                  </div>
                </div>
                <div className="min-w-0 break-words text-xs text-neutral-700 dark:text-neutral-300">
                  {row.ruleId && row.ruleId.trim().length > 0 ? row.ruleId : "—"}
                </div>
                <div className="tabular-nums text-xs text-neutral-700 dark:text-neutral-300">
                  {typeof row.evidenceRefCount === "number" && Number.isFinite(row.evidenceRefCount)
                    ? row.evidenceRefCount
                    : "—"}
                </div>
                <div className="min-w-0 text-xs text-neutral-700 dark:text-neutral-300">{row.traceConfidenceLabel}</div>
                <div className="tabular-nums text-xs text-neutral-700 dark:text-neutral-300">{pct}</div>
                <div className="min-w-0 text-xs text-neutral-700 dark:text-neutral-300">{confidenceSlot}</div>
                <div
                  className="min-w-0 text-xs text-neutral-600 dark:text-neutral-400"
                  title={
                    row.missingTraceFields && row.missingTraceFields.length > 0
                      ? row.missingTraceFields.join(", ")
                      : undefined
                  }
                >
                  {gapsSummary(row)}
                </div>
                <div className="flex min-w-0 flex-col gap-1.5">
                  <div className="flex min-w-0 flex-wrap gap-1">
                    <Button
                      type="button"
                      size="sm"
                      variant="outline"
                      className="h-7 px-2 text-xs"
                      onClick={() => {
                        setActiveFindingId(row.findingId);
                        setOpen(true);
                      }}
                    >
                      View trace
                    </Button>
                    <Button type="button" size="sm" variant="ghost" className="h-7 px-2 text-xs" asChild>
                      <Link href={`/reviews/${runId}/findings/${encodeURIComponent(row.findingId)}/inspect`}>Why?</Link>
                    </Button>
                    <Button type="button" size="sm" variant="ghost" className="h-7 px-2 text-xs" asChild>
                      <Link href={`/reviews/${runId}/findings/${encodeURIComponent(row.findingId)}`}>Explain</Link>
                    </Button>
                  </div>
                  <CopyTraceRowWorkItemButton row={row} runId={runId} />
                  <ProductLearningFeedbackControls
                    runId={runId}
                    subjectType="Finding"
                    artifactHint={`finding:${row.findingId}`}
                    patternKey={row.ruleId ? `finding-rule:${row.ruleId}` : "finding"}
                    detail={{
                      findingId: row.findingId,
                      title: row.findingTitle,
                      traceCompletenessRatio: row.traceCompletenessRatio,
                    }}
                    compact
                    title="Useful?"
                  />
                </div>
              </div>
            );
          })}
        </div>
      </div>

      <FindingExplainabilityDialog
        open={open}
        onOpenChange={(next) => {
          setOpen(next);

          if (!next) {
            setActiveFindingId(null);
          }
        }}
        runId={runId}
        findingId={activeFindingId}
      />
    </div>
  );
}
