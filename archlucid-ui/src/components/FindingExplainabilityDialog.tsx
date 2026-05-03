"use client";

import { useCallback, useEffect, useState } from "react";

import { FindingExplainPanel } from "@/components/FindingExplainPanel";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { OperatorLoadingNotice } from "@/components/OperatorShellMessage";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Progress } from "@/components/ui/progress";
import { getFindingExplainability } from "@/lib/api";
import { truncateForList } from "@/lib/truncate-for-list";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import type { FindingExplainability } from "@/types/explanation";

export type FindingExplainabilityDialogProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  runId: string;
  findingId: string | null;
};

/**
 * Fetches and displays persisted explainability for one finding (trace fields + server narrative).
 */
export function FindingExplainabilityDialog({
  open,
  onOpenChange,
  runId,
  findingId,
}: FindingExplainabilityDialogProps) {
  const [data, setData] = useState<FindingExplainability | null>(null);
  const [failure, setFailure] = useState<ApiLoadFailureState | null>(null);
  const [loading, setLoading] = useState(false);

  const load = useCallback(async () => {
    if (findingId === null || findingId.trim().length === 0) {
      return;
    }

    setLoading(true);
    setFailure(null);
    setData(null);

    try {
      const body = await getFindingExplainability(runId, findingId.trim());
      setData(body);
    } catch (e) {
      setFailure(toApiLoadFailure(e));
    } finally {
      setLoading(false);
    }
  }, [runId, findingId]);

  useEffect(() => {
    if (!open || findingId === null || findingId.trim().length === 0) {
      return;
    }

    void load();
  }, [open, findingId, load]);

  const ratioPct =
    data !== null && Number.isFinite(data.traceCompletenessRatio)
      ? Math.round(Math.min(1, Math.max(0, data.traceCompletenessRatio)) * 100)
      : 0;

  const missingFields = data?.missingTraceFields?.filter((s) => s.trim().length > 0) ?? [];

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className="max-h-[90vh] max-w-2xl overflow-y-auto"
        aria-labelledby="finding-explainability-dialog-title"
        aria-describedby="finding-explainability-dialog-desc"
      >
        <DialogHeader>
          <DialogTitle id="finding-explainability-dialog-title">Finding explainability</DialogTitle>
          <DialogDescription id="finding-explainability-dialog-desc">
            Deterministic trace from the run pipeline (no live LLM call in this dialog).
          </DialogDescription>
        </DialogHeader>

        {loading ? (
          <OperatorLoadingNotice>
            <strong>Loading explainability…</strong>
          </OperatorLoadingNotice>
        ) : null}

        {failure !== null ? (
          <OperatorApiProblem
            problem={failure.problem}
            fallbackMessage={failure.message}
            correlationId={failure.correlationId}
          />
        ) : null}

        {!loading && failure === null && data !== null ? (
          <div className="space-y-4 text-sm text-neutral-800 dark:text-neutral-200">
            <div className="flex flex-wrap items-center gap-2">
              <Badge variant="outline" className="font-mono text-xs">
                {data.findingId}
              </Badge>
              <Badge variant="secondary">{data.severity}</Badge>
              <span className="text-neutral-500 dark:text-neutral-400">{data.engineType}</span>
            </div>
            <p className="m-0 text-base font-semibold text-neutral-900 dark:text-neutral-100" title={data.title}>
              {truncateForList(data.title, 280)}
            </p>
            {data.evidence ? (
              <section aria-labelledby="finding-evidence-heading" className="rounded-md border border-sky-200 bg-sky-50/80 p-3 dark:border-sky-900 dark:bg-sky-950/30">
                <h3 id="finding-evidence-heading" className="mb-2 text-sm font-semibold text-sky-950 dark:text-sky-100">
                  Structured evidence (deterministic)
                </h3>
                <dl className="m-0 space-y-2 text-xs text-sky-950 dark:text-sky-50">
                  <div>
                    <dt className="font-semibold text-sky-900 dark:text-sky-200">Rule id</dt>
                    <dd className="m-0 font-mono">{data.evidence.ruleId}</dd>
                  </div>
                  <div>
                    <dt className="font-semibold text-sky-900 dark:text-sky-200">Conclusion (from finding rationale)</dt>
                    <dd className="m-0 whitespace-pre-wrap leading-relaxed">{data.evidence.conclusion}</dd>
                  </div>
                  <div>
                    <dt className="font-semibold text-sky-900 dark:text-sky-200">Evidence refs</dt>
                    <dd className="m-0">
                      {data.evidence.evidenceRefs.length === 0 ? (
                        <span className="text-sky-800/80 dark:text-sky-200/80">None recorded</span>
                      ) : (
                        <ul className="m-0 list-disc space-y-0.5 pl-5">
                          {data.evidence.evidenceRefs.map((ref, i) => (
                            <li key={`${ref}-${i}`} className="font-mono">
                              {ref}
                            </li>
                          ))}
                        </ul>
                      )}
                    </dd>
                  </div>
                  {data.evidence.alternativePathsConsidered.length > 0 ? (
                    <div>
                      <dt className="font-semibold text-sky-900 dark:text-sky-200">Alternative paths (structured)</dt>
                      <dd className="m-0">
                        <ul className="m-0 list-disc space-y-0.5 pl-5">
                          {data.evidence.alternativePathsConsidered.map((a, i) => (
                            <li key={`${a}-${i}`}>{a}</li>
                          ))}
                        </ul>
                      </dd>
                    </div>
                  ) : null}
                </dl>
              </section>
            ) : null}
            <div className="space-y-1">
              <div className="flex items-center justify-between gap-2 text-xs text-neutral-600 dark:text-neutral-400">
                <span>Trace completeness</span>
                <span>{ratioPct}%</span>
              </div>
              <Progress
                value={ratioPct}
                className="h-2"
                aria-label={`Trace completeness ${ratioPct} percent`}
              />
            </div>
            {missingFields.length > 0 ? (
              <section
                aria-label="Missing trace fields"
                className="rounded-md border border-amber-200 bg-amber-50/90 p-3 text-xs text-amber-950 dark:border-amber-900 dark:bg-amber-950/40 dark:text-amber-50"
              >
                <p className="m-0 font-semibold">Not populated in trace</p>
                <ul className="m-0 mt-1 list-disc space-y-0.5 pl-5">
                  {missingFields.map((m) => (
                    <li key={m}>{m}</li>
                  ))}
                </ul>
              </section>
            ) : null}
            {data.narrativeText.trim().length > 0 ? (
              <section aria-labelledby="finding-narrative-heading">
                <h3 id="finding-narrative-heading" className="mb-1 text-sm font-semibold text-neutral-900 dark:text-neutral-100">
                  Narrative (presentation)
                </h3>
                <p className="m-0 whitespace-pre-wrap leading-relaxed text-neutral-700 dark:text-neutral-300">
                  {data.narrativeText}
                </p>
              </section>
            ) : null}
            {data.rulesApplied.length > 0 ? (
              <section>
                <h3 className="mb-1 text-sm font-semibold text-neutral-900 dark:text-neutral-100">Rules applied</h3>
                <ul className="m-0 list-disc space-y-0.5 pl-5">
                  {data.rulesApplied.map((r, i) => (
                    <li key={`${r}-${i}`}>{r}</li>
                  ))}
                </ul>
              </section>
            ) : null}
            {data.decisionsTaken.length > 0 ? (
              <section>
                <h3 className="mb-1 text-sm font-semibold text-neutral-900 dark:text-neutral-100">Decisions taken</h3>
                <ol className="m-0 list-decimal space-y-0.5 pl-5">
                  {data.decisionsTaken.map((d, i) => (
                    <li key={`${d}-${i}`}>{d}</li>
                  ))}
                </ol>
              </section>
            ) : null}
            {data.graphNodeIdsExamined.length > 0 ? (
              <section>
                <h3 className="mb-1 text-sm font-semibold text-neutral-900 dark:text-neutral-100">Graph nodes examined</h3>
                <div className="flex flex-wrap gap-1">
                  {data.graphNodeIdsExamined.map((id, i) => (
                    <Badge key={`${id}-${i}`} variant="outline" className="font-mono text-xs">
                      {id}
                    </Badge>
                  ))}
                </div>
              </section>
            ) : null}
            {data.alternativePathsConsidered.length > 0 ? (
              <section className="rounded-md border border-amber-200 bg-amber-50 p-3 dark:border-amber-900 dark:bg-amber-950/40">
                <h3 className="mb-1 text-sm font-semibold text-amber-900 dark:text-amber-100">Alternative paths considered</h3>
                <ul className="m-0 list-disc space-y-0.5 pl-5 text-amber-950 dark:text-amber-50">
                  {data.alternativePathsConsidered.map((a, i) => (
                    <li key={`${a}-${i}`}>{a}</li>
                  ))}
                </ul>
              </section>
            ) : null}
            {data.notes.length > 0 ? (
              <section>
                <h3 className="mb-1 text-sm font-semibold text-neutral-900 dark:text-neutral-100">Notes</h3>
                <ul className="m-0 list-disc space-y-0.5 pl-5">
                  {data.notes.map((n, i) => (
                    <li key={`${n}-${i}`}>{n}</li>
                  ))}
                </ul>
              </section>
            ) : null}
            {findingId !== null && findingId.trim().length > 0 ? (
              <FindingExplainPanel
                runId={runId}
                findingId={findingId.trim()}
                confidenceLevel={data.confidenceLevel ?? null}
              />
            ) : null}
            <div className="flex justify-end border-t border-neutral-200 pt-3 dark:border-neutral-700">
              <Button type="button" variant="secondary" onClick={() => onOpenChange(false)}>
                Close
              </Button>
            </div>
          </div>
        ) : null}
      </DialogContent>
    </Dialog>
  );
}
