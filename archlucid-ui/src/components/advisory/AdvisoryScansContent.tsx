"use client";

import { useState } from "react";
import { ContextualHelp } from "@/components/ContextualHelp";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
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
    <main style={{ maxWidth: 900 }}>
      <div className="m-0 mb-1 flex flex-wrap items-center gap-2">
        <h2 className="m-0">Improvement Advisor</h2>
        <ContextualHelp helpKey="advisory-hub" />
      </div>
      <p style={{ color: "#444", fontSize: 14 }}>
        Ranked recommendations from manifest gaps, issues, cost risks, and optional comparison to a prior run. Generated
        plans are persisted; accept, reject, defer, or mark implemented to feed the governance workflow.
      </p>

      <div style={{ display: "grid", gap: 12, marginBottom: 24 }}>
        <input
          value={runId}
          onChange={(e) => setRunId(e.target.value)}
          placeholder="Run ID (target / current run)"
          style={{ padding: 8, fontFamily: "monospace" }}
        />
        <input
          value={compareToRunId}
          onChange={(e) => setCompareToRunId(e.target.value)}
          placeholder="Optional compare-to run ID (base run for delta signals)"
          style={{ padding: 8, fontFamily: "monospace" }}
        />
        <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
          <button type="button" onClick={() => void loadAdvice()} disabled={loading || !runId.trim()}>
            {loading ? "Working…" : "Generate recommendations"}
          </button>
          <button type="button" onClick={() => void refreshPersistedOnly()} disabled={loading || !runId.trim()}>
            Refresh saved list
          </button>
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
          style={{ border: "1px dashed #94a3b8", padding: 12, marginBottom: 16, borderRadius: 8 }}
        >
          <h3 style={{ marginTop: 0 }}>Experimental</h3>
          <p style={{ fontSize: 13, color: "#475569", marginBottom: 0 }}>
            Optional panels for in-development advisory UX. Enable with{" "}
            <code style={{ fontSize: 12 }}>NEXT_PUBLIC_EXPERIMENTAL_ADVISORY_PANELS=true</code> at build time.
          </p>
        </section>
      ) : null}

      {planSummary ? (
        <>
          <h3>Summary</h3>
          <ul>
            {planSummary.summaryNotes.map((note, index) => (
              <li key={index}>{note}</li>
            ))}
          </ul>
        </>
      ) : null}

      {recommendations.length > 0 ? (
        <>
          <h3>Persisted recommendations</h3>
          <p style={{ color: "#555", fontSize: 13 }}>
            Status and reviewer fields are loaded from storage. Use actions below (requires operator access on the API).
          </p>
          <div style={{ display: "grid", gap: 16 }}>
            {recommendations.map((rec) => (
              <div
                key={rec.recommendationId}
                style={{
                  border: "1px solid #ddd",
                  borderRadius: 8,
                  padding: 16,
                  background: "#fff",
                }}
              >
                <h4 style={{ marginTop: 0 }}>{rec.title}</h4>
                <p>
                  <strong>Status:</strong> {rec.status}
                </p>
                <p>
                  <strong>Category:</strong> {rec.category}
                </p>
                <p>
                  <strong>Urgency:</strong> {rec.urgency}
                </p>
                <p>
                  <strong>Priority score:</strong> {rec.priorityScore}
                </p>
                <p>
                  <strong>Rationale:</strong> {rec.rationale}
                </p>
                <p>
                  <strong>Suggested action:</strong> {rec.suggestedAction}
                </p>
                <p>
                  <strong>Expected impact:</strong> {rec.expectedImpact}
                </p>
                {rec.reviewedByUserName ? (
                  <p>
                    <strong>Last reviewed by:</strong> {rec.reviewedByUserName}
                  </p>
                ) : null}
                {rec.reviewComment ? (
                  <p>
                    <strong>Review comment:</strong> {rec.reviewComment}
                  </p>
                ) : null}
                {rec.resolutionRationale ? (
                  <p>
                    <strong>Resolution rationale:</strong> {rec.resolutionRationale}
                  </p>
                ) : null}
                <div style={{ display: "flex", gap: 8, flexWrap: "wrap", marginTop: 12 }}>
                  <button type="button" onClick={() => void takeAction(rec.recommendationId, "Accept")}>
                    Accept
                  </button>
                  <button type="button" onClick={() => void takeAction(rec.recommendationId, "Reject")}>
                    Reject
                  </button>
                  <button type="button" onClick={() => void takeAction(rec.recommendationId, "Defer")}>
                    Defer
                  </button>
                  <button type="button" onClick={() => void takeAction(rec.recommendationId, "MarkImplemented")}>
                    Implemented
                  </button>
                </div>
              </div>
            ))}
          </div>
        </>
      ) : planSummary && recommendations.length === 0 ? (
        <p style={{ color: "#666" }}>No persisted recommendations returned for this run.</p>
      ) : null}
    </main>
  );
}
