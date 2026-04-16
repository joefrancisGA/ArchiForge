import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { getRunAgentEvaluation, getRunTraces } from "@/lib/api";
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
    <section id="agent-forensics" style={{ marginBottom: 24 }} aria-labelledby="agent-forensics-title">
      <h3 id="agent-forensics-title">Agent traces & output structure</h3>
      <p style={{ fontSize: 14, color: "#64748b", marginTop: 0, maxWidth: 720 }}>
        Prompt/response audit rows and a structural JSON completeness pass over persisted agent outputs (no LLM). Requires
        architecture API access; empty results are normal when tracing is disabled or the run has no agent steps yet.
      </p>

      {blobPersistFailed ? (
        <div
          role="status"
          aria-live="polite"
          style={{
            marginBottom: 12,
            padding: "10px 12px",
            borderRadius: 8,
            border: "1px solid #f59e0b",
            background: "#fffbeb",
            fontSize: 14,
            color: "#92400e",
          }}
        >
          <strong>Blob persistence warning:</strong> at least one trace row has{" "}
          <code>blobUploadFailed=true</code> (full prompt/response blobs may be missing). See{" "}
          <code>docs/AGENT_TRACE_FORENSICS.md</code> and durable audit{" "}
          <code>AgentTraceBlobPersistenceFailed</code> when inline persistence exhausts retries or times out.
        </div>
      ) : null}

      {tracesFailure ? (
        <>
          <p style={{ margin: "0 0 8px", fontSize: 14, fontWeight: 600 }}>Traces could not be loaded.</p>
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
          <p style={{ margin: "12px 0 8px", fontSize: 14, fontWeight: 600 }}>
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
        <p style={{ fontSize: 14, color: "#64748b" }}>No execution traces returned for this run (first page).</p>
      ) : null}

      {!tracesFailure && traces.length > 0 ? (
        <div style={{ overflowX: "auto" }}>
          <table style={{ borderCollapse: "collapse", width: "100%", fontSize: 14 }}>
            <thead>
              <tr style={{ textAlign: "left", borderBottom: "1px solid #e2e8f0" }}>
                <th style={{ padding: "8px 6px" }}>Agent</th>
                <th style={{ padding: "8px 6px" }}>Trace ID</th>
                <th style={{ padding: "8px 6px" }}>Parse OK</th>
                <th style={{ padding: "8px 6px" }}>Blob upload</th>
                <th style={{ padding: "8px 6px" }}>Structural ratio</th>
              </tr>
            </thead>
            <tbody>
              {traces.map((t) => {
                const sc = scoreForTrace(evaluationPayload?.scores, t.traceId);

                return (
                  <tr key={t.traceId} style={{ borderBottom: "1px solid #f1f5f9" }}>
                    <td style={{ padding: "8px 6px", whiteSpace: "nowrap" }}>{agentTypeLabel(t.agentType)}</td>
                    <td style={{ padding: "8px 6px", fontFamily: "monospace", fontSize: 12 }}>{t.traceId}</td>
                    <td style={{ padding: "8px 6px" }}>{t.parseSucceeded ? "yes" : "no"}</td>
                    <td style={{ padding: "8px 6px" }}>
                      {t.blobUploadFailed === true ? "failed" : t.blobUploadFailed === false ? "ok" : "—"}
                    </td>
                    <td style={{ padding: "8px 6px" }}>
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
        <p style={{ marginTop: 12, fontSize: 13, color: "#64748b" }}>
          Evaluated at {new Date(evaluationPayload.evaluatedAtUtc).toLocaleString()} · skipped traces:{" "}
          {evaluationPayload.tracesSkippedCount}
          {evaluationPayload.averageStructuralCompletenessRatio !== null &&
          evaluationPayload.averageStructuralCompletenessRatio !== undefined
            ? ` · avg structural: ${evaluationPayload.averageStructuralCompletenessRatio.toFixed(2)}`
            : ""}
        </p>
      ) : null}
    </section>
  );
}
