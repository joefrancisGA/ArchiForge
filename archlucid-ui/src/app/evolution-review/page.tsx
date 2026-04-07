"use client";

import type { CSSProperties } from "react";
import { useCallback, useEffect, useMemo, useState } from "react";
import Link from "next/link";
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

const listWrap: CSSProperties = {
  display: "flex",
  flexDirection: "column",
  gap: 8,
  marginBottom: 20,
};

const listButton: CSSProperties = {
  textAlign: "left",
  padding: "10px 12px",
  borderRadius: 8,
  border: "1px solid #e2e8f0",
  background: "#fff",
  cursor: "pointer",
  fontSize: 14,
};

const listButtonSelected: CSSProperties = {
  ...listButton,
  borderColor: "#2563eb",
  boxShadow: "0 0 0 1px #2563eb",
};

const impactBox: CSSProperties = {
  padding: "12px 14px",
  border: "1px solid #e0e7ff",
  background: "#eef2ff",
  borderRadius: 8,
  marginBottom: 18,
  fontSize: 14,
  lineHeight: 1.55,
};

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
    <main style={{ maxWidth: 980 }}>
      <h2 style={{ marginTop: 0 }}>Simulation review</h2>
      <p style={{ color: "#475569", fontSize: 14, lineHeight: 1.55, maxWidth: 760 }}>
        Read-only view of <strong>60R evolution candidates</strong>: plan snapshot (description and expected impact),
        persisted simulation runs, and a side-by-side <strong>before / after</strong> layout per baseline architecture
        run. Create candidates from{" "}
        <Link href="/planning" style={{ color: "#1d4ed8" }}>
          Planning
        </Link>{" "}
        via the API; use <strong>Run simulation</strong> when your token has execute authority to refresh outcomes and
        scores.
      </p>

      <div style={{ display: "flex", flexWrap: "wrap", gap: 12, alignItems: "center", margin: "16px 0 20px" }}>
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
          <p style={{ margin: "8px 0 0", fontSize: 14 }}>Fetching evolution candidate change sets…</p>
        </OperatorLoadingNotice>
      ) : null}

      {listFailure !== null ? (
        <div role="alert" style={{ marginBottom: 16 }}>
          <OperatorApiProblem
            problem={listFailure.problem}
            fallbackMessage={listFailure.message}
            correlationId={listFailure.correlationId}
          />
        </div>
      ) : null}

      {simulateFailure !== null ? (
        <div role="alert" style={{ marginBottom: 16 }}>
          <OperatorApiProblem
            problem={simulateFailure.problem}
            fallbackMessage={simulateFailure.message}
            correlationId={simulateFailure.correlationId}
          />
        </div>
      ) : null}

      {selectedId !== null && selectedId !== "" ? (
        <section style={{ marginBottom: 22 }} aria-labelledby="evolution-export-heading">
          <h3 id="evolution-export-heading" style={{ fontSize: 15, margin: "0 0 6px", color: "#334155" }}>
            Export simulation report
          </h3>
          <p style={{ margin: 0, fontSize: 13, color: "#64748b", maxWidth: 760 }}>
            Markdown or JSON bundle for the selected candidate: change set description, plan snapshot / expected impact,
            each run&apos;s shadow outcome, evaluation scores, diff summary lines, and raw outcome JSON.
          </p>
          <p style={{ margin: "10px 0 0", fontSize: 14 }}>
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
          <p style={{ margin: 0, fontSize: 14 }}>
            When candidates exist for this scope, they appear in the list. Create one with{" "}
            <code style={{ fontSize: 13 }}>POST /v1/evolution/candidates/from-plan/{"{planId}"}</code>.
          </p>
        </OperatorEmptyState>
      ) : null}

      {candidates.length > 0 ? (
        <section aria-labelledby="evolution-candidates-heading">
          <h3 id="evolution-candidates-heading" style={{ fontSize: 17, marginBottom: 8 }}>
            Candidate change sets
          </h3>
          <div style={listWrap}>
            {candidates.map((c) => {
              const sel = c.candidateChangeSetId === selectedId;

              return (
                <button
                  key={c.candidateChangeSetId}
                  type="button"
                  style={sel ? listButtonSelected : listButton}
                  onClick={() => setSelectedId(c.candidateChangeSetId)}
                >
                  <div style={{ fontWeight: 600 }}>{c.title}</div>
                  <div style={{ fontSize: 12, color: "#64748b", marginTop: 4 }}>
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
        <div role="alert" style={{ marginBottom: 16 }}>
          <OperatorApiProblem
            problem={detailFailure.problem}
            fallbackMessage={detailFailure.message}
            correlationId={detailFailure.correlationId}
          />
        </div>
      ) : null}

      {detail !== null ? (
        <section aria-labelledby="evolution-detail-heading">
          <h3 id="evolution-detail-heading" style={{ fontSize: 17, marginBottom: 8 }}>
            Description
          </h3>
          <p style={{ margin: "0 0 6px", fontSize: 14, lineHeight: 1.55 }}>
            <strong>{detail.candidate.title}</strong>
          </p>
          <p style={{ margin: "0 0 16px", fontSize: 14, lineHeight: 1.55, color: "#334155" }}>{detail.candidate.summary}</p>
          <p style={{ margin: "0 0 20px", fontSize: 13, color: "#64748b" }}>
            Status: <strong>{detail.candidate.status}</strong> · Source plan{" "}
            <Link href={`/planning/plans/${encodeURIComponent(detail.candidate.sourcePlanId)}`} style={{ color: "#1d4ed8" }}>
              {detail.candidate.sourcePlanId}
            </Link>
          </p>

          <h3 style={{ fontSize: 17, marginBottom: 8 }}>Expected impact (plan snapshot)</h3>
          {planSnapshot !== null ? (
            <div style={impactBox}>
              <p style={{ margin: "0 0 8px" }}>
                <strong>Priority score:</strong> {planSnapshot.priorityScore}
              </p>
              {planSnapshot.priorityExplanation !== null &&
              planSnapshot.priorityExplanation !== undefined &&
              planSnapshot.priorityExplanation !== "" ? (
                <p style={{ margin: "0 0 8px" }}>
                  <strong>Priority explanation:</strong> {planSnapshot.priorityExplanation}
                </p>
              ) : (
                <p style={{ margin: "0 0 8px", color: "#64748b" }}>No priority explanation on the snapshot.</p>
              )}
              <p style={{ margin: "0 0 8px" }}>
                <strong>Action steps (count):</strong> {planSnapshot.actionStepCount}
              </p>
              <p style={{ margin: 0, fontSize: 13, color: "#4338ca" }}>
                Snapshot summary: {planSnapshot.summary}
              </p>
            </div>
          ) : (
            <p style={{ color: "#b45309", fontSize: 14 }}>Plan snapshot JSON could not be parsed.</p>
          )}

          <h3 style={{ fontSize: 17, marginBottom: 8 }}>Simulation results</h3>
          <p style={{ fontSize: 13, color: "#64748b", margin: "0 0 12px", maxWidth: 720 }}>
            Each row is a <strong>before / after</strong> diff: <span style={{ background: "#fffbeb", padding: "1px 6px" }}>before</span>{" "}
            is the plan-linked baseline context; <span style={{ background: "#f0fdf4", padding: "1px 6px" }}>after</span> is the
            read-only shadow re-analysis and any parsed evaluation scores.
          </p>
          {detailLoading ? (
            <p style={{ color: "#64748b", fontSize: 13 }} role="status">
              Updating…
            </p>
          ) : null}
          {(detail.simulationRuns ?? []).length === 0 ? (
            <p style={{ fontSize: 14, color: "#64748b" }}>
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
