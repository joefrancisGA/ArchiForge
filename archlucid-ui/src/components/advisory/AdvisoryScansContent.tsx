"use client";

import { useState } from "react";

import { ContextualHelp } from "@/components/ContextualHelp";
import { DocumentLayout } from "@/components/DocumentLayout";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { Button } from "@/components/ui/button";
import { getImprovementPlan } from "@/lib/api";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import { applyRecommendationAction, listRecommendations } from "@/lib/advisory-api";
import { isExperimentalAdvisoryPanelsEnabled } from "@/lib/feature-flags";
import type { ImprovementPlan, RecommendationRecord } from "@/types/advisory";

/**
 * Scans tab: improvement advisor (former standalone `/advisory` page body).
 */
export function AdvisoryScansContent() {
  const [runId, setRunId] = useState("");
  const [compareToRunId, setCompareToRunId] = useState("");
  const [planSummary, setPlanSummary] = useState<ImprovementPlan | null>(null);
  const [recommendations, setRecommendations] = useState<RecommendationRecord[]>([]);
  const [loading, setLoading] = useState(false);
  const [failure, setFailure] = useState<ApiLoadFailureState | null>(null);

  async function takeAction(recommendationId: string, action: string) {
    const comment = window.prompt(`Optional comment for ${action}:`) ?? "";
    const rationale = window.prompt(`Optional rationale for ${action}:`) ?? "";

    setFailure(null);
    try {
      await applyRecommendationAction(recommendationId, action, comment, rationale);
      const rid = runId.trim();

      if (rid) {
        const refreshed = await listRecommendations(rid);
        setRecommendations(refreshed);
      }
    } catch (e) {
      setFailure(toApiLoadFailure(e));
    }
  }

  async function loadAdvice() {
    const rid = runId.trim();

    if (!rid) {
      return;
    }

    setLoading(true);
    setFailure(null);
    try {
      const data = await getImprovementPlan(rid, compareToRunId.trim() || undefined);
      setPlanSummary(data);
      const persisted = await listRecommendations(rid);
      setRecommendations(persisted);
    } catch (e) {
      setFailure(toApiLoadFailure(e));
      setPlanSummary(null);
      setRecommendations([]);
    } finally {
      setLoading(false);
    }
  }

  async function refreshPersistedOnly() {
    const rid = runId.trim();

    if (!rid) {
      return;
    }

    setLoading(true);
    setFailure(null);
    try {
      const persisted = await listRecommendations(rid);
      setRecommendations(persisted);
    } catch (e) {
      setFailure(toApiLoadFailure(e));
    } finally {
      setLoading(false);
    }
  }

  return (
    <main className="mx-auto max-w-4xl px-4 py-6">
      <DocumentLayout>
        <div className="m-0 mb-1 flex flex-wrap items-center gap-2">
          <h2 className="m-0 text-xl font-bold text-neutral-900 dark:text-neutral-50">Improvement Advisor</h2>
          <ContextualHelp helpKey="advisory-hub" />
        </div>
        <p className="doc-meta m-0">
          Ranked recommendations from manifest gaps, issues, cost risks, and optional comparison to a prior architecture
          run. Generated plans are persisted; accept, reject, defer, or mark implemented to feed the governance workflow.
        </p>

        <div className="mb-6 grid gap-3">
          <input
            value={runId}
            onChange={(e) => setRunId(e.target.value)}
            placeholder="Architecture run ID (target / current run)"
            className="rounded-md border border-neutral-300 bg-white p-2 font-mono text-sm text-neutral-900 dark:border-neutral-600 dark:bg-neutral-950 dark:text-neutral-100"
          />
          <input
            value={compareToRunId}
            onChange={(e) => setCompareToRunId(e.target.value)}
            placeholder="Optional compare-to architecture run ID (base run for delta signals)"
            className="rounded-md border border-neutral-300 bg-white p-2 font-mono text-sm text-neutral-900 dark:border-neutral-600 dark:bg-neutral-950 dark:text-neutral-100"
          />
          <div className="flex flex-wrap gap-2">
            <Button type="button" onClick={() => void loadAdvice()} disabled={loading || !runId.trim()}>
              {loading ? "Working…" : "Generate recommendations"}
            </Button>
            <Button type="button" variant="outline" onClick={() => void refreshPersistedOnly()} disabled={loading || !runId.trim()}>
              Refresh saved list
            </Button>
          </div>
        </div>

        {failure !== null ? (
          <div role="alert">
            <OperatorApiProblem
              problem={failure.problem}
              fallbackMessage={failure.message}
              correlationId={failure.correlationId}
            />
          </div>
        ) : null}

        {isExperimentalAdvisoryPanelsEnabled() ? (
          <section
            aria-label="Experimental advisory panels"
            className="mb-4 rounded-lg border border-dashed border-neutral-400 p-3 dark:border-neutral-500"
          >
            <h3 className="m-0 text-lg font-semibold text-neutral-900 dark:text-neutral-100">Experimental</h3>
            <p className="m-0 text-sm text-neutral-600 dark:text-neutral-400">
              Optional panels for in-development advisory UX. Enable with{" "}
              <code className="rounded bg-neutral-200 px-1 text-xs dark:bg-neutral-800">NEXT_PUBLIC_EXPERIMENTAL_ADVISORY_PANELS=true</code>{" "}
              at build time.
            </p>
          </section>
        ) : null}

        {planSummary ? (
          <>
            <h3 className="m-0 text-lg font-semibold text-neutral-900 dark:text-neutral-100">Summary</h3>
            <ul className="m-0 list-disc space-y-1 pl-5 text-base leading-relaxed">
              {planSummary.summaryNotes.map((note, index) => (
                <li key={index}>{note}</li>
              ))}
            </ul>
          </>
        ) : null}

        {recommendations.length > 0 ? (
          <>
            <h3 className="m-0 text-lg font-semibold text-neutral-900 dark:text-neutral-100">Persisted recommendations</h3>
            <p className="doc-meta m-0 text-sm">
              Status and reviewer fields are loaded from storage. Use actions below (requires operator access on the API).
            </p>
            <div className="grid gap-4">
              {recommendations.map((rec) => (
                <div
                  key={rec.recommendationId}
                  className="rounded-lg border border-neutral-200 bg-white p-4 dark:border-neutral-700 dark:bg-neutral-950"
                >
                  <h4 className="m-0 text-base font-semibold text-neutral-900 dark:text-neutral-100">{rec.title}</h4>
                  <p className="m-0 mt-2 text-base leading-relaxed">
                    <strong>Status:</strong> {rec.status}
                  </p>
                  <p className="m-0 mt-2 text-base leading-relaxed">
                    <strong>Category:</strong> {rec.category}
                  </p>
                  <p className="m-0 mt-2 text-base leading-relaxed">
                    <strong>Urgency:</strong> {rec.urgency}
                  </p>
                  <p className="m-0 mt-2 text-base leading-relaxed">
                    <strong>Priority score:</strong> {rec.priorityScore}
                  </p>
                  <p className="m-0 mt-2 text-base leading-relaxed">
                    <strong>Rationale:</strong> {rec.rationale}
                  </p>
                  <p className="m-0 mt-2 text-base leading-relaxed">
                    <strong>Suggested action:</strong> {rec.suggestedAction}
                  </p>
                  <p className="m-0 mt-2 text-base leading-relaxed">
                    <strong>Expected impact:</strong> {rec.expectedImpact}
                  </p>
                  {rec.reviewedByUserName ? (
                    <p className="m-0 mt-2 text-base leading-relaxed">
                      <strong>Last reviewed by:</strong> {rec.reviewedByUserName}
                    </p>
                  ) : null}
                  {rec.reviewComment ? (
                    <p className="m-0 mt-2 text-base leading-relaxed">
                      <strong>Review comment:</strong> {rec.reviewComment}
                    </p>
                  ) : null}
                  {rec.resolutionRationale ? (
                    <p className="m-0 mt-2 text-base leading-relaxed">
                      <strong>Resolution rationale:</strong> {rec.resolutionRationale}
                    </p>
                  ) : null}
                  <div className="mt-3 flex flex-wrap gap-2">
                    <Button type="button" size="sm" variant="outline" onClick={() => void takeAction(rec.recommendationId, "Accept")}>
                      Accept
                    </Button>
                    <Button type="button" size="sm" variant="outline" onClick={() => void takeAction(rec.recommendationId, "Reject")}>
                      Reject
                    </Button>
                    <Button type="button" size="sm" variant="outline" onClick={() => void takeAction(rec.recommendationId, "Defer")}>
                      Defer
                    </Button>
                    <Button type="button" size="sm" variant="outline" onClick={() => void takeAction(rec.recommendationId, "MarkImplemented")}>
                      Implemented
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          </>
        ) : planSummary && recommendations.length === 0 ? (
          <p className="m-0 text-sm text-neutral-600 dark:text-neutral-400">No persisted recommendations returned for this architecture run.</p>
        ) : null}
      </DocumentLayout>
    </main>
  );
}
