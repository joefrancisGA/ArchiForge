"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { OperatorPageHeader } from "@/components/OperatorPageHeader";
import { SimulationRunDiffCard } from "@/components/evolution/SimulationRunDiffCard";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { OperatorEmptyState, OperatorLoadingNotice } from "@/components/OperatorShellMessage";
import {
  fetchEvolutionCandidates,
  fetchEvolutionResults,
  postEvolutionSimulate,
} from "@/lib/api";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import { buildEvolutionSimulationReportFileUrl } from "@/lib/evolution-simulation-report-urls";
import { parseEvolutionPlanSnapshot } from "@/lib/evolution-plan-snapshot";
import type { EvolutionCandidateChangeSetResponse, EvolutionResultsResponse } from "@/types/evolution";

/**
 * 60R simulation review: browse candidate change sets, plan-derived expectations, and per-baseline before/after diffs.
 */
export default function EvolutionReviewPage() {
  const [candidates, setCandidates] = useState<EvolutionCandidateChangeSetResponse[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [detail, setDetail] = useState<EvolutionResultsResponse | null>(null);
  const [listLoading, setListLoading] = useState(false);
  const [detailLoading, setDetailLoading] = useState(false);
  const [simulateBusy, setSimulateBusy] = useState(false);
  const [listFailure, setListFailure] = useState<ApiLoadFailureState | null>(null);
  const [detailFailure, setDetailFailure] = useState<ApiLoadFailureState | null>(null);
  const [simulateFailure, setSimulateFailure] = useState<ApiLoadFailureState | null>(null);

  const loadList = useCallback(async () => {
    setListLoading(true);
    setListFailure(null);

    try {
      const body = await fetchEvolutionCandidates(100);
      const rows = body.candidates ?? [];
      setCandidates(rows);
      setSelectedId((prev) => {
        if (prev !== null && rows.some((c) => c.candidateChangeSetId === prev)) {
          return prev;
        }

        return rows.length > 0 ? rows[0].candidateChangeSetId : null;
      });
    } catch (e) {
      setListFailure(toApiLoadFailure(e));
      setCandidates([]);
      setSelectedId(null);
    } finally {
      setListLoading(false);
    }
  }, []);

  const loadDetail = useCallback(async (candidateId: string) => {
    setDetailLoading(true);
    setDetailFailure(null);

    try {
      const res = await fetchEvolutionResults(candidateId);
      setDetail(res);
    } catch (e) {
      setDetailFailure(toApiLoadFailure(e));
      setDetail(null);
    } finally {
      setDetailLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadList();
  }, [loadList]);

  useEffect(() => {
    if (selectedId === null || selectedId === "") {
      setDetail(null);

      return;
    }

    void loadDetail(selectedId);
  }, [selectedId, loadDetail]);

  const planSnapshot = useMemo(() => {
    if (detail === null) {
      return null;
    }

    return parseEvolutionPlanSnapshot(detail.planSnapshotJson);
  }, [detail]);

  const onSimulate = useCallback(async () => {
    if (selectedId === null || selectedId === "") {
      return;
    }

    setSimulateBusy(true);
    setSimulateFailure(null);

    try {
      await postEvolutionSimulate(selectedId);
      await loadDetail(selectedId);
      await loadList();
    } catch (e) {
      setSimulateFailure(toApiLoadFailure(e));
    } finally {
      setSimulateBusy(false);
    }
  }, [selectedId, loadDetail, loadList]);

  const emptyList = !listLoading && candidates.length === 0 && listFailure === null;

  return (
    <main className="max-w-5xl">
      <OperatorPageHeader title="Simulation review" />
      <p className="text-neutral-600 dark:text-neutral-400 text-sm leading-relaxed max-w-3xl">
        Read-only view of <strong>60R evolution candidates</strong>: plan snapshot (description and expected impact),
        persisted simulation runs, and a side-by-side <strong>before / after</strong> layout per baseline architecture
        run. Create candidates from{" "}
        <Link href="/planning" className="text-blue-700 dark:text-blue-400">
          Planning
        </Link>{" "}
        via the API; use <strong>Run simulation</strong> when your account has permission to refresh outcomes and
        scores.
      </p>

      <div className="flex flex-wrap gap-3 items-center mt-4 mb-5">
        <button type="button" onClick={() => void loadList()} disabled={listLoading}>
          Refresh list
        </button>
        <button
          type="button"
          onClick={() => void onSimulate()}
          disabled={simulateBusy || selectedId === null || detailLoading}
        >
          {simulateBusy ? "Running simulation…" : "Run simulation"}
        </button>
      </div>

      {listLoading && candidates.length === 0 ? (
        <OperatorLoadingNotice>
          <strong>Loading candidates.</strong>
          <p className="mt-2 text-sm">Fetching evolution candidate change sets…</p>
        </OperatorLoadingNotice>
      ) : null}

      {listFailure !== null ? (
        <div role="alert" className="mb-4">
          <OperatorApiProblem
            problem={listFailure.problem}
            fallbackMessage={listFailure.message}
            correlationId={listFailure.correlationId}
          />
        </div>
      ) : null}

      {simulateFailure !== null ? (
        <div role="alert" className="mb-4">
          <OperatorApiProblem
            problem={simulateFailure.problem}
            fallbackMessage={simulateFailure.message}
            correlationId={simulateFailure.correlationId}
          />
        </div>
      ) : null}

      {selectedId !== null && selectedId !== "" ? (
        <section className="mb-[22px]" aria-labelledby="evolution-export-heading">
          <h3 id="evolution-export-heading" className="text-[15px] mb-1.5 text-neutral-700 dark:text-neutral-300">
            Export simulation report
          </h3>
          <p className="m-0 text-[13px] text-neutral-500 dark:text-neutral-400 max-w-3xl">
            Markdown or JSON bundle for the selected candidate: change set description, plan snapshot / expected impact,
            each run&apos;s shadow outcome, evaluation scores, diff summary lines, and raw outcome JSON.
          </p>
          <p className="mt-2.5 text-sm">
            <a href={buildEvolutionSimulationReportFileUrl(selectedId, "markdown")} download>
              Download Markdown
            </a>
            {" · "}
            <a href={buildEvolutionSimulationReportFileUrl(selectedId, "json")} download>
              Download JSON
            </a>
            {" · "}
            <a
              href={buildEvolutionSimulationReportFileUrl(selectedId, "json")}
              target="_blank"
              rel="noopener noreferrer"
            >
              Open JSON in new tab
            </a>
          </p>
        </section>
      ) : null}

      {emptyList ? (
        <OperatorEmptyState title="No candidate change sets">
          <p className="m-0 text-sm">
            When candidates exist for this scope, they appear in the list. Create one with{" "}
            <code className="text-[13px]">POST /v1/evolution/candidates/from-plan/{"{planId}"}</code>.
          </p>
        </OperatorEmptyState>
      ) : null}

      {candidates.length > 0 ? (
        <section aria-labelledby="evolution-candidates-heading">
          <h3 id="evolution-candidates-heading" className="text-[17px] mb-2">
            Candidate change sets
          </h3>
          <div className="flex flex-col gap-2 mb-5">
            {candidates.map((c) => {
              const sel = c.candidateChangeSetId === selectedId;

              return (
                <button
                  key={c.candidateChangeSetId}
                  type="button"
                  className={sel ? "text-left px-3 py-2.5 rounded-lg border border-blue-600 bg-white cursor-pointer text-sm shadow-[0_0_0_1px_#2563eb] dark:border-blue-500 dark:bg-neutral-950" : "text-left px-3 py-2.5 rounded-lg border border-neutral-200 bg-white cursor-pointer text-sm dark:border-neutral-700 dark:bg-neutral-950"}
                  onClick={() => setSelectedId(c.candidateChangeSetId)}
                >
                  <div className="font-semibold">{c.title}</div>
                  <div className="text-xs text-neutral-500 dark:text-neutral-400 mt-1">
                    {c.status} · {new Date(c.createdUtc).toLocaleString()}
                  </div>
                </button>
              );
            })}
          </div>
        </section>
      ) : null}

      {selectedId !== null && detailLoading && detail === null ? (
        <OperatorLoadingNotice>
          <strong>Loading simulation results.</strong>
        </OperatorLoadingNotice>
      ) : null}

      {detailFailure !== null ? (
        <div role="alert" className="mb-4">
          <OperatorApiProblem
            problem={detailFailure.problem}
            fallbackMessage={detailFailure.message}
            correlationId={detailFailure.correlationId}
          />
        </div>
      ) : null}

      {detail !== null ? (
        <section aria-labelledby="evolution-detail-heading">
          <h3 id="evolution-detail-heading" className="text-[17px] mb-2">
            Description
          </h3>
          <p className="mb-1.5 text-sm leading-relaxed">
            <strong>{detail.candidate.title}</strong>
          </p>
          <p className="mb-4 text-sm leading-relaxed text-neutral-700 dark:text-neutral-300">{detail.candidate.summary}</p>
          <p className="mb-5 text-[13px] text-neutral-500 dark:text-neutral-400">
            Status: <strong>{detail.candidate.status}</strong> · Source plan{" "}
            <Link href={`/planning/plans/${encodeURIComponent(detail.candidate.sourcePlanId)}`} className="text-blue-700 dark:text-blue-400">
              {detail.candidate.sourcePlanId}
            </Link>
          </p>

          <h3 className="text-[17px] mb-2">Expected impact (plan snapshot)</h3>
          {planSnapshot !== null ? (
            <div className="px-3.5 py-3 border border-indigo-200 bg-indigo-50 rounded-lg mb-[18px] text-sm leading-relaxed dark:border-indigo-900 dark:bg-indigo-950/40">
              <p className="mb-2">
                <strong>Priority score:</strong> {planSnapshot.priorityScore}
              </p>
              {planSnapshot.priorityExplanation !== null &&
              planSnapshot.priorityExplanation !== undefined &&
              planSnapshot.priorityExplanation !== "" ? (
                <p className="mb-2">
                  <strong>Priority explanation:</strong> {planSnapshot.priorityExplanation}
                </p>
              ) : (
                <p className="mb-2 text-neutral-500 dark:text-neutral-400">No priority explanation on the snapshot.</p>
              )}
              <p className="mb-2">
                <strong>Action steps (count):</strong> {planSnapshot.actionStepCount}
              </p>
              <p className="m-0 text-[13px] text-indigo-700 dark:text-indigo-400">
                Snapshot summary: {planSnapshot.summary}
              </p>
            </div>
          ) : (
            <p className="text-amber-700 dark:text-amber-400 text-sm">Plan snapshot JSON could not be parsed.</p>
          )}

          <h3 className="text-[17px] mb-2">Simulation results</h3>
          <p className="text-[13px] text-neutral-500 dark:text-neutral-400 mb-3 max-w-3xl">
            Each row is a <strong>before / after</strong> diff: <span className="bg-amber-50 dark:bg-amber-950/40 px-1.5 py-px">before</span>{" "}
            is the plan-linked baseline context; <span className="bg-green-50 dark:bg-green-950/40 px-1.5 py-px">after</span> is the
            read-only shadow re-analysis and any parsed evaluation scores.
          </p>
          {detailLoading ? (
            <p className="text-neutral-500 dark:text-neutral-400 text-[13px]" role="status">
              Updating…
            </p>
          ) : null}
          {(detail.simulationRuns ?? []).length === 0 ? (
            <p className="text-sm text-neutral-500 dark:text-neutral-400">
              No persisted simulation rows. Run shadow evaluation or simulation from the API, or use <strong>Run simulation</strong>{" "}
              above.
            </p>
          ) : (
            (detail.simulationRuns ?? []).map((r) => (
              <SimulationRunDiffCard
                key={r.simulationRunId}
                run={r}
                planLinkedRunIds={planSnapshot?.linkedArchitectureRunIds ?? []}
              />
            ))
          )}
        </section>
      ) : null}
    </main>
  );
}
