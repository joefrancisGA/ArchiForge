import Link from "next/link";
import type { CSSProperties, ReactElement } from "react";
import type { EvolutionSimulationRunWithEvaluationResponse } from "@/types/evolution";
import { parseEvolutionOutcomeJson } from "@/lib/evolution-outcome";

const card: CSSProperties = {
  border: "1px solid #e2e8f0",
  borderRadius: 8,
  marginBottom: 14,
  overflow: "hidden",
};

const header: CSSProperties = {
  display: "flex",
  flexWrap: "wrap",
  gap: 10,
  alignItems: "baseline",
  padding: "10px 12px",
  background: "#f8fafc",
  borderBottom: "1px solid #e2e8f0",
  fontSize: 13,
};

const grid: CSSProperties = {
  display: "grid",
  gridTemplateColumns: "minmax(0, 1fr) minmax(0, 1fr)",
  gap: 0,
};

const col: CSSProperties = {
  padding: "12px 14px",
  fontSize: 13,
  lineHeight: 1.5,
  verticalAlign: "top",
};

const colBefore: CSSProperties = {
  ...col,
  borderRight: "3px solid #cbd5e1",
  background: "#fffbeb",
};

const colAfter: CSSProperties = {
  ...col,
  background: "#f0fdf4",
};

const label: CSSProperties = {
  fontSize: 11,
  fontWeight: 700,
  textTransform: "uppercase",
  letterSpacing: "0.04em",
  color: "#64748b",
  marginBottom: 6,
};

const mono: CSSProperties = {
  fontFamily: "ui-monospace, monospace",
  fontSize: 12,
  wordBreak: "break-all",
};

const scoreTable: CSSProperties = {
  width: "100%",
  borderCollapse: "collapse",
  fontSize: 12,
  marginTop: 8,
};

function formatScore(n: number | null | undefined): string {
  if (n === null || n === undefined) {
    return "—";
  }

  return Number.isFinite(n) ? n.toFixed(4) : "—";
}

export type SimulationRunDiffCardProps = {
  run: EvolutionSimulationRunWithEvaluationResponse;
  /** Baseline run IDs from the plan snapshot (before context). */
  planLinkedRunIds: string[];
};

/**
 * One simulation row: plan-linked baseline context vs shadow re-analysis outcome (read-only diff layout).
 */
export function SimulationRunDiffCard(props: SimulationRunDiffCardProps): ReactElement {
  const { run, planLinkedRunIds } = props;
  const parsed = parseEvolutionOutcomeJson(run.outcomeJson);
  const baselineId = run.baselineArchitectureRunId.trim();
  const isLinkedOnPlan = planLinkedRunIds.some((id) => id === baselineId);
  const ev = run.evaluationScore;

  return (
    <article style={card} aria-labelledby={`sim-run-${run.simulationRunId}`}>
      <div style={header} id={`sim-run-${run.simulationRunId}`}>
        <span>
          <strong>Baseline run</strong>{" "}
          <Link href={`/runs/${encodeURIComponent(baselineId)}`} style={mono}>
            {baselineId}
          </Link>
        </span>
        <span style={{ color: "#64748b" }}>
          Completed {new Date(run.completedUtc).toLocaleString()} · {run.evaluationMode}
          {run.isShadowOnly ? " · shadow" : ""}
          {run.outcomeSchemaVersion !== null && run.outcomeSchemaVersion !== undefined && run.outcomeSchemaVersion !== ""
            ? ` · ${run.outcomeSchemaVersion}`
            : ""}
        </span>
      </div>

      <div style={grid}>
        <div style={colBefore}>
          <div style={label}>Before (plan &amp; baseline)</div>
          <p style={{ margin: "0 0 8px", color: "#44403c" }}>
            This run re-reads the architecture for a baseline that the source plan associated with the candidate.
          </p>
          <ul style={{ margin: 0, paddingLeft: 18, color: "#57534e" }}>
            <li>
              Listed on plan snapshot:{" "}
              <strong>{isLinkedOnPlan ? "yes" : "no"}</strong>
              {planLinkedRunIds.length > 0 ? (
                <span style={{ display: "block", marginTop: 6 }}>
                  Plan-linked IDs:{" "}
                  {planLinkedRunIds.map((id, idx) => (
                    <span key={`${id}-${idx}`} style={{ display: "block" }}>
                      <Link href={`/runs/${encodeURIComponent(id)}`} style={mono}>
                        {id}
                      </Link>
                      {id === baselineId ? " ← this row" : null}
                    </span>
                  ))}
                </span>
              ) : (
                <span style={{ color: "#b45309" }}> No runs linked on the plan snapshot.</span>
              )}
            </li>
          </ul>
        </div>

        <div style={colAfter}>
          <div style={label}>After (simulation read)</div>
          {parsed.kind === "empty" || parsed.kind === "invalid" ? (
            <p style={{ margin: 0, color: "#991b1b" }}>
              {parsed.kind === "empty" ? "No outcome payload stored." : "Outcome JSON could not be parsed as shadow data."}
            </p>
          ) : (
            <>
              {parsed.shadow.error !== null && parsed.shadow.error !== undefined && parsed.shadow.error !== "" ? (
                <p style={{ margin: "0 0 8px", color: "#991b1b" }}>
                  <strong>Error:</strong> {parsed.shadow.error}
                </p>
              ) : null}
              <dl
                style={{
                  margin: 0,
                  display: "grid",
                  gridTemplateColumns: "auto 1fr",
                  gap: "4px 12px",
                  alignItems: "baseline",
                }}
              >
                <dt style={{ color: "#64748b" }}>Run status</dt>
                <dd style={{ margin: 0 }}>{parsed.shadow.runStatus ?? "—"}</dd>
                <dt style={{ color: "#64748b" }}>Manifest version</dt>
                <dd style={{ margin: 0, ...mono }}>{parsed.shadow.manifestVersion ?? "—"}</dd>
                <dt style={{ color: "#64748b" }}>Has manifest</dt>
                <dd style={{ margin: 0 }}>{parsed.shadow.hasManifest ? "yes" : "no"}</dd>
                <dt style={{ color: "#64748b" }}>Summary length</dt>
                <dd style={{ margin: 0 }}>{parsed.shadow.summaryLength}</dd>
                <dt style={{ color: "#64748b" }}>Analysis warnings</dt>
                <dd style={{ margin: 0 }}>{parsed.shadow.warningCount}</dd>
              </dl>
            </>
          )}

          {ev !== null && ev !== undefined ? (
            <>
              <div style={{ ...label, marginTop: 14 }}>Evaluation scores</div>
              <table style={scoreTable}>
                <tbody>
                  <tr>
                    <td style={{ padding: "2px 8px 2px 0", color: "#64748b" }}>Simulation</td>
                    <td style={{ padding: 2 }}>{formatScore(ev.simulationScore)}</td>
                  </tr>
                  <tr>
                    <td style={{ padding: "2px 8px 2px 0", color: "#64748b" }}>Determinism</td>
                    <td style={{ padding: 2 }}>{formatScore(ev.determinismScore)}</td>
                  </tr>
                  <tr>
                    <td style={{ padding: "2px 8px 2px 0", color: "#64748b" }}>Regression risk</td>
                    <td style={{ padding: 2 }}>{formatScore(ev.regressionRiskScore)}</td>
                  </tr>
                  <tr>
                    <td style={{ padding: "2px 8px 2px 0", color: "#64748b" }}>Improvement Δ</td>
                    <td style={{ padding: 2 }}>{formatScore(ev.improvementDelta)}</td>
                  </tr>
                  <tr>
                    <td style={{ padding: "2px 8px 2px 0", color: "#64748b" }}>Confidence</td>
                    <td style={{ padding: 2 }}>{formatScore(ev.confidenceScore)}</td>
                  </tr>
                </tbody>
              </table>
              {(ev.regressionSignals ?? []).length > 0 ? (
                <div style={{ marginTop: 8 }}>
                  <div style={{ fontSize: 12, fontWeight: 600, marginBottom: 4 }}>Regression signals</div>
                  <ul style={{ margin: 0, paddingLeft: 18, fontSize: 12 }}>
                    {(ev.regressionSignals ?? []).map((s, i) => (
                      <li key={`${i}-${s}`}>{s}</li>
                    ))}
                  </ul>
                </div>
              ) : null}
            </>
          ) : null}

          {run.evaluationExplanationSummary !== null &&
          run.evaluationExplanationSummary !== undefined &&
          run.evaluationExplanationSummary !== "" ? (
            <p style={{ margin: "10px 0 0", fontSize: 12, color: "#334155" }}>
              <strong>Summary:</strong> {run.evaluationExplanationSummary}
            </p>
          ) : null}
        </div>
      </div>
    </article>
  );
}
