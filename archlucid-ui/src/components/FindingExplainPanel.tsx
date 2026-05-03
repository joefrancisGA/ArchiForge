"use client";

import { useCallback, useEffect, useState } from "react";

import { FindingConfidenceBadge } from "@/components/FindingConfidenceBadge";
import { MutationErrorBoundary } from "@/components/MutationErrorBoundary";
import { DocumentLayout } from "@/components/DocumentLayout";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { OperatorLoadingNotice } from "@/components/OperatorShellMessage";
import { useNavCallerAuthorityRank } from "@/components/OperatorNavAuthorityProvider";
import { Button } from "@/components/ui/button";
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible";
import { getFindingEvidenceChain, getFindingLlmAudit, postFindingFeedback } from "@/lib/api";
import { recordFirstTenantFunnelEvent } from "@/lib/first-tenant-funnel-telemetry";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import { AUTHORITY_RANK } from "@/lib/nav-authority";
import type { FindingConfidenceLevel, FindingEvidenceChain, FindingLlmAudit } from "@/types/explanation";

export type FindingExplainPanelProps = {
  runId: string;
  findingId: string;
  /** Evaluation coarse bucket when supplied by inspect/explainability callers; omit when unknown. */
  confidenceLevel?: FindingConfidenceLevel | null;
};

/**
 * Redacted LLM prompt/completion audit for one finding, plus thumbs feedback (Execute). Deterministic trace lives in
 * `FindingExplainabilityDialog` / `GET …/explainability`.
 */
export function FindingExplainPanel({ runId, findingId, confidenceLevel }: FindingExplainPanelProps) {
  const rank = useNavCallerAuthorityRank();
  const [audit, setAudit] = useState<FindingLlmAudit | null>(null);
  const [evidenceChain, setEvidenceChain] = useState<FindingEvidenceChain | null>(null);
  const [failure, setFailure] = useState<ApiLoadFailureState | null>(null);
  const [loading, setLoading] = useState(false);
  const [feedbackBusy, setFeedbackBusy] = useState(false);
  const [feedbackNote, setFeedbackNote] = useState<string | null>(null);

  const load = useCallback(async () => {
    if (findingId.trim().length === 0) {
      return;
    }

    setLoading(true);
    setFailure(null);
    setFeedbackNote(null);
    setEvidenceChain(null);

    try {
      const a = await getFindingLlmAudit(runId, findingId.trim());
      setAudit(a);
      recordFirstTenantFunnelEvent("first_finding_viewed");

      try {
        const chain = await getFindingEvidenceChain(runId, findingId.trim());
        setEvidenceChain(chain);
      } catch {
        setEvidenceChain(null);
      }
    } catch (err) {
      setFailure(toApiLoadFailure(err));
    } finally {
      setLoading(false);
    }
  }, [findingId, runId]);

  useEffect(() => {
    if (rank < AUTHORITY_RANK.ReadAuthority) {
      return;
    }

    void load();
  }, [load, rank]);

  if (rank < AUTHORITY_RANK.ReadAuthority) {
    return (
      <p className="text-sm text-neutral-600 dark:text-neutral-400">
        Sign in with Read access or higher to view redacted LLM audit text for this finding.
      </p>
    );
  }

  const canVote = rank >= AUTHORITY_RANK.ExecuteAuthority;

  return (
    <MutationErrorBoundary title="Finding explain panel failed to render">
    <div className="space-y-4 border-t border-neutral-200 pt-4 dark:border-neutral-700">
      <h4 className="m-0 text-sm font-semibold text-neutral-900 dark:text-neutral-100">Explain this finding</h4>
      {(confidenceLevel === "High" || confidenceLevel === "Medium" || confidenceLevel === "Low") ? (
        <div className="mt-1">
          <FindingConfidenceBadge level={confidenceLevel} />
        </div>
      ) : null}
      <p className="m-0 text-xs text-neutral-600 dark:text-neutral-400">
        Redacted prompts and model output are under <strong className="font-medium">Technical audit details</strong> below.
        Pair with deterministic explainability (&quot;View trace&quot;) on the review explanation table when available.
      </p>

      {!loading && failure === null && evidenceChain !== null ? (
        <section aria-labelledby="finding-evidence-chain-heading" className="space-y-2 rounded-md border border-violet-200 bg-violet-50/80 p-3 dark:border-violet-900 dark:bg-violet-950/30">
          <h4
            id="finding-evidence-chain-heading"
            className="m-0 text-xs font-semibold uppercase tracking-wide text-violet-900 dark:text-violet-100"
          >
            Evidence chain (persisted pointers)
          </h4>
          <p className="m-0 text-xs text-violet-900/90 dark:text-violet-100/90">
            From{" "}
            <code className="rounded bg-violet-200/80 px-1 text-[0.65rem] dark:bg-violet-900/80">
              GET /v1/architecture/run/…/findings/…/evidence-chain
            </code>
            .
          </p>
          <dl className="m-0 grid gap-2 text-xs text-violet-950 dark:text-violet-50 sm:grid-cols-2">
            <div>
              <dt className="font-semibold">Manifest version</dt>
              <dd className="m-0 font-mono">{evidenceChain.manifestVersion?.trim() ? evidenceChain.manifestVersion : "—"}</dd>
            </div>
            <div>
              <dt className="font-semibold">Findings snapshot</dt>
              <dd className="m-0 font-mono">{evidenceChain.findingsSnapshotId ?? "—"}</dd>
            </div>
            <div>
              <dt className="font-semibold">Decision trace</dt>
              <dd className="m-0 font-mono">{evidenceChain.decisionTraceId ?? "—"}</dd>
            </div>
            <div>
              <dt className="font-semibold">Reviewed manifest id</dt>
              <dd className="m-0 font-mono">{evidenceChain.goldenManifestId ?? "—"}</dd>
            </div>
          </dl>
          {evidenceChain.relatedGraphNodeIds.length > 0 ? (
            <div>
              <p className="m-0 mb-1 text-xs font-semibold text-violet-900 dark:text-violet-100">Related graph nodes</p>
              <ul className="m-0 list-disc space-y-0.5 pl-5 font-mono text-[0.7rem]">
                {evidenceChain.relatedGraphNodeIds.map((id) => (
                  <li key={id}>{id}</li>
                ))}
              </ul>
            </div>
          ) : null}
          {evidenceChain.agentExecutionTraceIds.length > 0 ? (
            <div>
              <p className="m-0 mb-1 text-xs font-semibold text-violet-900 dark:text-violet-100">Agent execution traces</p>
              <ul className="m-0 list-disc space-y-0.5 pl-5 font-mono text-[0.7rem]">
                {evidenceChain.agentExecutionTraceIds.map((id) => (
                  <li key={id}>{id}</li>
                ))}
              </ul>
            </div>
          ) : null}
        </section>
      ) : null}

      {loading ? (
        <OperatorLoadingNotice>
          <strong>Loading LLM audit…</strong>
        </OperatorLoadingNotice>
      ) : null}

      {failure !== null ? (
        <OperatorApiProblem
          problem={failure.problem}
          fallbackMessage={failure.message}
          correlationId={failure.correlationId}
        />
      ) : null}

      {!loading && failure === null && audit !== null ? (
        <Collapsible defaultOpen={false} className="rounded-md border border-neutral-200 dark:border-neutral-600">
          <CollapsibleTrigger
            type="button"
            className="flex w-full items-center justify-between gap-2 rounded-md px-3 py-2 text-left text-sm font-semibold text-neutral-900 hover:bg-neutral-50 dark:text-neutral-100 dark:hover:bg-neutral-800"
            aria-label="Expand technical audit details (redacted prompts and completion)"
          >
            <span>Technical audit details (redacted)</span>
            <span className="text-xs font-normal text-neutral-500 dark:text-neutral-400">Show</span>
          </CollapsibleTrigger>
          <CollapsibleContent className="border-t border-neutral-200 px-3 pb-3 pt-2 dark:border-neutral-600">
            <DocumentLayout
              tocItems={[
                { id: "finding-audit-system", label: "System prompt" },
                { id: "finding-audit-user", label: "User prompt" },
                { id: "finding-audit-completion", label: "Completion" },
              ]}
            >
              <div className="space-y-2">
                <p
                  id="finding-audit-system"
                  className="m-0 text-xs font-semibold uppercase tracking-wide text-neutral-500 dark:text-neutral-400"
                >
                  System prompt (redacted) · trace {audit.traceId}
                </p>
                <pre className="max-h-48 overflow-auto whitespace-pre-wrap rounded-md bg-neutral-100 p-2 text-xs dark:bg-neutral-900">
                  {audit.systemPromptRedacted.trim().length > 0 ? audit.systemPromptRedacted : "(empty)"}
                </pre>
                <p
                  id="finding-audit-user"
                  className="m-0 text-xs font-semibold uppercase tracking-wide text-neutral-500 dark:text-neutral-400"
                >
                  User prompt (redacted)
                </p>
                <pre className="max-h-48 overflow-auto whitespace-pre-wrap rounded-md bg-neutral-100 p-2 text-xs dark:bg-neutral-900">
                  {audit.userPromptRedacted.trim().length > 0 ? audit.userPromptRedacted : "(empty)"}
                </pre>
                <p
                  id="finding-audit-completion"
                  className="m-0 text-xs font-semibold uppercase tracking-wide text-neutral-500 dark:text-neutral-400"
                >
                  LLM completion (redacted)
                </p>
                <pre className="max-h-48 overflow-auto whitespace-pre-wrap rounded-md bg-neutral-100 p-2 text-xs dark:bg-neutral-900">
                  {audit.rawResponseRedacted.trim().length > 0 ? audit.rawResponseRedacted : "(empty)"}
                </pre>
                <p className="m-0 text-xs text-neutral-500 dark:text-neutral-400">
                  Model: {audit.modelDeploymentName ?? "—"} · Agent: {audit.agentType}
                </p>
              </div>
            </DocumentLayout>
          </CollapsibleContent>
        </Collapsible>
      ) : null}

      {canVote ? (
        <div className="flex flex-wrap items-center gap-2">
          <span className="text-xs text-neutral-600 dark:text-neutral-400">Was this finding helpful?</span>
          <Button
            type="button"
            size="sm"
            variant="outline"
            disabled={feedbackBusy}
            onClick={() => {
              void (async () => {
                setFeedbackBusy(true);
                setFeedbackNote(null);

                try {
                  await postFindingFeedback(runId, findingId.trim(), 1);
                  setFeedbackNote("Thanks — feedback recorded.");
                } catch (e) {
                  setFeedbackNote(toApiLoadFailure(e).message);
                } finally {
                  setFeedbackBusy(false);
                }
              })();
            }}
          >
            Thumbs up
          </Button>
          <Button
            type="button"
            size="sm"
            variant="outline"
            disabled={feedbackBusy}
            onClick={() => {
              void (async () => {
                setFeedbackBusy(true);
                setFeedbackNote(null);

                try {
                  await postFindingFeedback(runId, findingId.trim(), -1);
                  setFeedbackNote("Thanks — feedback recorded.");
                } catch (e) {
                  setFeedbackNote(toApiLoadFailure(e).message);
                } finally {
                  setFeedbackBusy(false);
                }
              })();
            }}
          >
            Thumbs down
          </Button>
          {feedbackNote !== null ? <span className="text-xs text-neutral-600 dark:text-neutral-400">{feedbackNote}</span> : null}
        </div>
      ) : (
        <p className="m-0 text-xs text-neutral-500 dark:text-neutral-400">
          Thumbs feedback requires Operator access or higher (API-enforced).
        </p>
      )}
    </div>
    </MutationErrorBoundary>
  );
}
