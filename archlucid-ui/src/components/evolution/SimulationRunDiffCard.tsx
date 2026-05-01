import Link from "next/link";
import type { ReactElement } from "react";
import type { EvolutionSimulationRunWithEvaluationResponse } from "@/types/evolution";
import { parseEvolutionOutcomeJson } from "@/lib/evolution-outcome";

const cardCls = "mb-3.5 overflow-hidden rounded-lg border border-neutral-200 dark:border-neutral-700";
const headerCls = "flex flex-wrap items-baseline gap-2.5 border-b border-neutral-200 bg-neutral-50/90 px-3 py-2.5 text-[13px] dark:border-neutral-700 dark:bg-neutral-900/50";
const colCls = "p-3.5 text-[13px] leading-normal align-top";
const colBeforeCls = `${colCls} border-r-[3px] border-neutral-300 bg-amber-50 dark:border-neutral-600 dark:bg-amber-950/40`;
const colAfterCls = `${colCls} bg-green-50 dark:bg-green-950/40`;
const labelCls = "mb-1.5 text-[11px] font-bold uppercase tracking-wide text-neutral-500 dark:text-neutral-400";
const monoCls = "font-mono text-xs break-all";

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
    <article className={cardCls} aria-labelledby={`sim-run-${run.simulationRunId}`}>
      <div className={headerCls} id={`sim-run-${run.simulationRunId}`}>
        <span>
          <strong>Baseline run</strong>{" "}
          <Link href={`/reviews/${encodeURIComponent(baselineId)}`} className={monoCls}>
            {baselineId}
          </Link>
        </span>
        <span className="text-neutral-500 dark:text-neutral-400">
          Completed {new Date(run.completedUtc).toLocaleString()} · {run.evaluationMode}
          {run.isShadowOnly ? " · shadow" : ""}
          {run.outcomeSchemaVersion !== null && run.outcomeSchemaVersion !== undefined && run.outcomeSchemaVersion !== ""
            ? ` · ${run.outcomeSchemaVersion}`
            : ""}
        </span>
      </div>

      <div className="grid grid-cols-2">
        <div className={colBeforeCls}>
          <div className={labelCls}>Before (plan &amp; baseline)</div>
          <p className="mb-2 text-stone-700 dark:text-stone-300">
            This run re-reads the architecture for a baseline that the source plan associated with the candidate.
          </p>
          <ul className="m-0 pl-[18px] text-stone-600 dark:text-stone-400">
            <li>
              Listed on plan snapshot:{" "}
              <strong>{isLinkedOnPlan ? "yes" : "no"}</strong>
              {planLinkedRunIds.length > 0 ? (
                <span className="mt-1.5 block">
                  Plan-linked IDs:{" "}
                  {planLinkedRunIds.map((id, idx) => (
                    <span key={`${id}-${idx}`} className="block">
                      <Link href={`/reviews/${encodeURIComponent(id)}`} className={monoCls}>
                        {id}
                      </Link>
                      {id === baselineId ? " ← this row" : null}
                    </span>
                  ))}
                </span>
              ) : (
                <span className="text-amber-700 dark:text-amber-400"> No runs linked on the plan snapshot.</span>
              )}
            </li>
          </ul>
        </div>

        <div className={colAfterCls}>
          <div className={labelCls}>After (simulation read)</div>
          {parsed.kind === "empty" || parsed.kind === "invalid" ? (
            <p className="m-0 text-red-800 dark:text-red-400">
              {parsed.kind === "empty" ? "No outcome payload stored." : "Outcome JSON could not be parsed as shadow data."}
            </p>
          ) : (
            <>
              {parsed.shadow.error !== null && parsed.shadow.error !== undefined && parsed.shadow.error !== "" ? (
                <p className="mb-2 text-red-800 dark:text-red-400">
                  <strong>Error:</strong> {parsed.shadow.error}
                </p>
              ) : null}
              <dl className="m-0 grid grid-cols-[auto_1fr] items-baseline gap-x-3 gap-y-1">
                <dt className="text-neutral-500 dark:text-neutral-400">Run status</dt>
                <dd className="m-0">{parsed.shadow.runStatus ?? "—"}</dd>
                <dt className="text-neutral-500 dark:text-neutral-400">Manifest version</dt>
                <dd className={`m-0 ${monoCls}`}>{parsed.shadow.manifestVersion ?? "—"}</dd>
                <dt className="text-neutral-500 dark:text-neutral-400">Has manifest</dt>
                <dd className="m-0">{parsed.shadow.hasManifest ? "yes" : "no"}</dd>
                <dt className="text-neutral-500 dark:text-neutral-400">Summary length</dt>
                <dd className="m-0">{parsed.shadow.summaryLength}</dd>
                <dt className="text-neutral-500 dark:text-neutral-400">Analysis warnings</dt>
                <dd className="m-0">{parsed.shadow.warningCount}</dd>
              </dl>
            </>
          )}

          {ev !== null && ev !== undefined ? (
            <>
              <div className={`${labelCls} mt-3.5`}>Evaluation scores</div>
              <table className="mt-2 w-full border-collapse text-xs">
                <tbody>
                  <tr>
                    <td className="pr-2 py-0.5 text-neutral-500 dark:text-neutral-400">Simulation</td>
                    <td className="p-0.5">{formatScore(ev.simulationScore)}</td>
                  </tr>
                  <tr>
                    <td className="pr-2 py-0.5 text-neutral-500 dark:text-neutral-400">Determinism</td>
                    <td className="p-0.5">{formatScore(ev.determinismScore)}</td>
                  </tr>
                  <tr>
                    <td className="pr-2 py-0.5 text-neutral-500 dark:text-neutral-400">Regression risk</td>
                    <td className="p-0.5">{formatScore(ev.regressionRiskScore)}</td>
                  </tr>
                  <tr>
                    <td className="pr-2 py-0.5 text-neutral-500 dark:text-neutral-400">Improvement Δ</td>
                    <td className="p-0.5">{formatScore(ev.improvementDelta)}</td>
                  </tr>
                  <tr>
                    <td className="pr-2 py-0.5 text-neutral-500 dark:text-neutral-400">Confidence</td>
                    <td className="p-0.5">{formatScore(ev.confidenceScore)}</td>
                  </tr>
                </tbody>
              </table>
              {(ev.regressionSignals ?? []).length > 0 ? (
                <div className="mt-2">
                  <div className="mb-1 text-xs font-semibold">Regression signals</div>
                  <ul className="m-0 pl-[18px] text-xs">
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
            <p className="mt-2.5 text-xs text-neutral-700 dark:text-neutral-300">
              <strong>Summary:</strong> {run.evaluationExplanationSummary}
            </p>
          ) : null}
        </div>
      </div>
    </article>
  );
}
