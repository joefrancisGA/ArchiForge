import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { CollapsibleSection } from "@/components/CollapsibleSection";
import { getRunAgentEvaluation, getRunTraces } from "@/lib/api";
import { formatInstantForLocale } from "@/lib/locale-datetime";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import type {
  AgentExecutionTraceListPayload,
  AgentOutputEvaluationScoreRow,
  AgentOutputEvaluationSummaryPayload,
} from "@/types/agent-forensics";

function agentTypeLabel(agentType: number): string {
  switch (agentType) {
    case 1:
      return "Topology";
    case 2:
      return "Cost";
    case 3:
      return "Compliance";
    case 4:
      return "Critic";
    default:
      return `AgentType(${agentType})`;
  }
}

function scoreForTrace(
  scores: AgentOutputEvaluationScoreRow[] | undefined,
  traceId: string,
): AgentOutputEvaluationScoreRow | undefined {
  return scores?.find((s) => s.traceId === traceId);
}

/** Server fragment: architecture-run LLM traces, blob-upload warnings, and on-demand structural evaluation scores. */
export async function RunAgentForensicsSection(props: { runId: string }) {
  const { runId } = props;
  let tracesPayload: AgentExecutionTraceListPayload | null = null;
  let tracesFailure: ApiLoadFailureState | null = null;
  let evaluationPayload: AgentOutputEvaluationSummaryPayload | null = null;
  let evaluationFailure: ApiLoadFailureState | null = null;

  try {
    tracesPayload = (await getRunTraces(runId, 1, 100)).data;
  } catch (e) {
    tracesFailure = toApiLoadFailure(e);
  }

  try {
    evaluationPayload = (await getRunAgentEvaluation(runId)).data;
  } catch (e) {
    evaluationFailure = toApiLoadFailure(e);
  }

  const traces = tracesPayload?.traces ?? [];
  const blobPersistFailed = traces.some((t) => t.blobUploadFailed === true);


  return (
    <section id="agent-forensics" className="scroll-mt-24 mb-6" aria-label="Diagnostics — agent traces">
      <CollapsibleSection title="Diagnostics — agent traces and output structure" defaultOpen={false}>
      <p className="mt-0 max-w-3xl text-sm text-neutral-500 dark:text-neutral-400">
        Prompt/response audit rows and a structural JSON completeness pass over persisted agent outputs (no LLM). Requires
        architecture API access; empty results are normal when tracing is disabled or the run has no agent steps yet.
      </p>

      {blobPersistFailed ? (
        <div
          role="status"
          aria-live="polite"
          className="mb-3 rounded-lg border border-amber-400 bg-amber-50 px-3 py-2.5 text-sm text-amber-800 dark:border-amber-600 dark:bg-amber-950/40 dark:text-amber-300"
        >
          <strong>Blob persistence warning:</strong> at least one trace row has{" "}
          <code>blobUploadFailed=true</code> (full prompt/response blobs may be missing). See{" "}
          <code>docs/AGENT_TRACE_FORENSICS.md</code> and durable audit{" "}
          <code>AgentTraceBlobPersistenceFailed</code> when inline persistence exhausts retries or times out.
        </div>
      ) : null}

      {tracesFailure ? (
        <>
          <p className="mb-2 text-sm font-semibold">Traces could not be loaded.</p>
          <OperatorApiProblem
            problem={tracesFailure.problem}
            fallbackMessage={tracesFailure.message}
            correlationId={tracesFailure.correlationId}
            variant="warning"
          />
        </>
      ) : null}

      {evaluationFailure ? (
        <>
          <p className="mb-2 mt-3 text-sm font-semibold">
            On-demand evaluation could not be loaded.
          </p>
          <OperatorApiProblem
            problem={evaluationFailure.problem}
            fallbackMessage={evaluationFailure.message}
            correlationId={evaluationFailure.correlationId}
            variant="warning"
          />
        </>
      ) : null}

      {!tracesFailure && traces.length === 0 ? (
        <p className="text-sm text-neutral-500 dark:text-neutral-400">
          No execution traces in the first page of results — expand when troubleshooting ingestion or agent steps.
        </p>
      ) : null}

      {!tracesFailure && traces.length > 0 ? (
        <div className="overflow-x-auto">
          <table className="w-full border-collapse text-sm">
            <thead>
              <tr className="border-b border-neutral-200 text-left dark:border-neutral-700">
                <th className="px-1.5 py-2">Agent</th>
                <th className="px-1.5 py-2">Trace ID</th>
                <th className="px-1.5 py-2">Parse OK</th>
                <th className="px-1.5 py-2">Blob upload</th>
                <th className="px-1.5 py-2">Structural ratio</th>
              </tr>
            </thead>
            <tbody>
              {traces.map((t) => {
                const sc = scoreForTrace(evaluationPayload?.scores, t.traceId);

                return (
                  <tr key={t.traceId} className="border-b border-neutral-100 dark:border-neutral-800">
                    <td className="whitespace-nowrap px-1.5 py-2">{agentTypeLabel(t.agentType)}</td>
                    <td className="px-1.5 py-2 font-mono text-xs">{t.traceId}</td>
                    <td className="px-1.5 py-2">{t.parseSucceeded ? "yes" : "no"}</td>
                    <td className="px-1.5 py-2">
                      {t.blobUploadFailed === true ? "failed" : t.blobUploadFailed === false ? "ok" : "—"}
                    </td>
                    <td className="px-1.5 py-2">
                      {sc
                        ? sc.isJsonParseFailure
                          ? "parse failure"
                          : sc.structuralCompletenessRatio.toFixed(2)
                        : evaluationFailure
                          ? "—"
                          : "n/a"}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      ) : null}

      {evaluationPayload && !evaluationFailure ? (
        <p className="mt-3 text-[13px] text-neutral-500 dark:text-neutral-400">
          Evaluated at {formatInstantForLocale(evaluationPayload.evaluatedAtUtc)} · skipped traces:{" "}
          {evaluationPayload.tracesSkippedCount}
          {evaluationPayload.averageStructuralCompletenessRatio !== null &&
          evaluationPayload.averageStructuralCompletenessRatio !== undefined
            ? ` · avg structural: ${evaluationPayload.averageStructuralCompletenessRatio.toFixed(2)}`
            : ""}
        </p>
      ) : null}
      </CollapsibleSection>
    </section>
  );
}
