"use client";

import Link from "next/link";
import { useState } from "react";

import { FindingExplainabilityDialog } from "@/components/FindingExplainabilityDialog";
import { Button } from "@/components/ui/button";
import type { FindingTraceConfidenceDto } from "@/types/explanation";

export type RunFindingExplainabilityTableProps = {
  runId: string;
  rows: FindingTraceConfidenceDto[];
};

/**
 * Lists findings with trace completeness from the aggregate explanation payload; opens per-finding explainability.
 */
export function RunFindingExplainabilityTable({ runId, rows }: RunFindingExplainabilityTableProps) {
  const [open, setOpen] = useState(false);
  const [activeFindingId, setActiveFindingId] = useState<string | null>(null);

  if (rows.length === 0) {
    return null;
  }

  return (
    <div className="mt-4 rounded-lg border border-neutral-200 bg-neutral-50/80 p-4 dark:border-neutral-700 dark:bg-neutral-900/40">
      <h3 className="m-0 mb-2 text-sm font-semibold text-neutral-900 dark:text-neutral-100">
        Per-finding explainability
      </h3>
      <p className="mb-3 text-xs text-neutral-600 dark:text-neutral-400">
        Open the trace captured for each finding (structured evidence, rules, graph nodes, narrative). Uses{" "}
        <code className="rounded bg-neutral-200 px-1 text-[0.7rem] dark:bg-neutral-800">GET /v1/explain/runs/…/findings/…/explainability</code>.
      </p>
      <div className="overflow-x-auto">
        <table className="w-full min-w-[28rem] border-collapse text-left text-sm">
          <thead>
            <tr className="border-b border-neutral-200 dark:border-neutral-700">
              <th className="py-2 pr-3 font-semibold text-neutral-800 dark:text-neutral-200">Finding</th>
              <th className="py-2 pr-3 font-semibold text-neutral-800 dark:text-neutral-200">Rule id</th>
              <th className="py-2 pr-3 font-semibold text-neutral-800 dark:text-neutral-200">Evidence refs</th>
              <th className="py-2 pr-3 font-semibold text-neutral-800 dark:text-neutral-200">Trace label</th>
              <th className="py-2 pr-3 font-semibold text-neutral-800 dark:text-neutral-200">Completeness</th>
              <th className="py-2 font-semibold text-neutral-800 dark:text-neutral-200">Action</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => {
              const pct =
                row.traceCompletenessRatio <= 1
                  ? Math.round(row.traceCompletenessRatio * 100)
                  : Math.round(row.traceCompletenessRatio);

              return (
                <tr
                  key={row.findingId}
                  className="border-b border-neutral-100 last:border-0 dark:border-neutral-800"
                >
                  <td className="py-2 pr-3 font-mono text-xs text-neutral-800 dark:text-neutral-200">{row.findingId}</td>
                  <td className="py-2 pr-3 text-xs text-neutral-700 dark:text-neutral-300">
                    {row.ruleId && row.ruleId.trim().length > 0 ? row.ruleId : "—"}
                  </td>
                  <td className="py-2 pr-3 tabular-nums text-neutral-700 dark:text-neutral-300">
                    {typeof row.evidenceRefCount === "number" && Number.isFinite(row.evidenceRefCount)
                      ? row.evidenceRefCount
                      : "—"}
                  </td>
                  <td className="py-2 pr-3 text-neutral-700 dark:text-neutral-300">{row.traceConfidenceLabel}</td>
                  <td className="py-2 pr-3 tabular-nums text-neutral-700 dark:text-neutral-300">{pct}%</td>
                  <td className="py-2">
                    <div className="flex flex-wrap gap-2">
                      <Button
                        type="button"
                        size="sm"
                        variant="outline"
                        onClick={() => {
                          setActiveFindingId(row.findingId);
                          setOpen(true);
                        }}
                      >
                        View trace
                      </Button>
                      <Button type="button" size="sm" variant="ghost" asChild>
                        <Link href={`/runs/${runId}/findings/${encodeURIComponent(row.findingId)}`}>Explain page</Link>
                      </Button>
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
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
