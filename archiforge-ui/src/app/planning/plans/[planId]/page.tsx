"use client";

import type { CSSProperties } from "react";
import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { OperatorLoadingNotice } from "@/components/OperatorShellMessage";
import { fetchLearningPlanDetail } from "@/lib/api";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import { formatIsoUtcForDisplay } from "@/lib/format-iso-utc";
import type { LearningPlanDetailResponse } from "@/types/learning";

const dlRow: CSSProperties = {
  display: "grid",
  gridTemplateColumns: "160px 1fr",
  gap: "8px 16px",
  fontSize: 14,
  marginBottom: 8,
  alignItems: "baseline",
};

const muted: CSSProperties = { color: "#64748b" };

/**
 * Single improvement plan detail: steps, priority, and evidence link counts (59R).
 */
export default function PlanningPlanDetailPage() {
  const params = useParams();
  const planIdRaw = params.planId;
  const planId = typeof planIdRaw === "string" ? planIdRaw : Array.isArray(planIdRaw) ? planIdRaw[0] : "";

  const [plan, setPlan] = useState<LearningPlanDetailResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [failure, setFailure] = useState<ApiLoadFailureState | null>(null);

  const load = useCallback(async () => {
    if (!planId.trim()) {
      return;
    }

    setLoading(true);
    setFailure(null);

    try {
      const detail = await fetchLearningPlanDetail(planId);
      setPlan(detail);
    } catch (e) {
      setFailure(toApiLoadFailure(e));
      setPlan(null);
    } finally {
      setLoading(false);
    }
  }, [planId]);

  useEffect(() => {
    void load();
  }, [load]);

  return (
    <main style={{ maxWidth: 720 }}>
      <p style={{ marginTop: 0, marginBottom: 16 }}>
        <Link href="/planning" style={{ color: "#1d4ed8", fontSize: 14 }}>
          ← Back to planning
        </Link>
      </p>

      <h2 style={{ marginTop: 0 }}>Improvement plan</h2>

      {!planId.trim() ? (
        <p role="alert" style={{ color: "#b91c1c" }}>
          Missing plan id.
        </p>
      ) : null}

      {loading && plan === null ? (
        <OperatorLoadingNotice>
          <strong>Loading plan.</strong>
          <p style={{ margin: "8px 0 0", fontSize: 14 }}>Fetching plan detail from the API…</p>
        </OperatorLoadingNotice>
      ) : null}

      {failure !== null ? (
        <div role="alert" style={{ marginBottom: 16 }}>
          <OperatorApiProblem
            problem={failure.problem}
            fallbackMessage={failure.message}
            correlationId={failure.correlationId}
          />
        </div>
      ) : null}

      {plan !== null ? (
        <>
          <section style={{ marginBottom: 24 }} aria-labelledby="plan-detail-title">
            <h3 id="plan-detail-title" style={{ fontSize: 20, marginBottom: 8 }}>
              {plan.title}
            </h3>
            <p style={{ color: "#334155", lineHeight: 1.55, marginTop: 0 }}>{plan.summary}</p>

            <div style={{ marginTop: 16 }}>
              <div style={dlRow}>
                <span style={muted}>Priority score</span>
                <span>{plan.priorityScore}</span>
              </div>
              {plan.priorityExplanation ? (
                <div style={dlRow}>
                  <span style={muted}>Priority note</span>
                  <span>{plan.priorityExplanation}</span>
                </div>
              ) : null}
              <div style={dlRow}>
                <span style={muted}>Status</span>
                <span>{plan.status}</span>
              </div>
              <div style={dlRow}>
                <span style={muted}>Created</span>
                <span>{formatIsoUtcForDisplay(plan.createdUtc)}</span>
              </div>
              <div style={dlRow}>
                <span style={muted}>Theme id</span>
                <span style={{ fontFamily: "ui-monospace, monospace", fontSize: 13 }}>{plan.themeId}</span>
              </div>
            </div>
          </section>

          <section style={{ marginBottom: 24 }} aria-labelledby="plan-evidence-heading">
            <h4 id="plan-evidence-heading" style={{ fontSize: 16, marginBottom: 8 }}>
              Evidence counts (linked)
            </h4>
            <ul style={{ margin: 0, paddingLeft: 20, color: "#334155", lineHeight: 1.6 }}>
              <li>Pilot signals: {plan.evidenceCounts.linkedSignalCount}</li>
              <li>Artifacts: {plan.evidenceCounts.linkedArtifactCount}</li>
              <li>Architecture runs: {plan.evidenceCounts.linkedArchitectureRunCount}</li>
            </ul>
          </section>

          {plan.theme ? (
            <section style={{ marginBottom: 24 }} aria-labelledby="plan-theme-heading">
              <h4 id="plan-theme-heading" style={{ fontSize: 16, marginBottom: 8 }}>
                Parent theme
              </h4>
              <p style={{ margin: "0 0 8px", fontWeight: 600 }}>{plan.theme.title}</p>
              <p style={{ margin: "0 0 8px", fontSize: 14, color: "#475569" }}>{plan.theme.summary}</p>
              <p style={{ margin: 0, fontSize: 13, color: "#64748b" }}>
                Evidence signals: {plan.theme.evidenceSignalCount} · Runs: {plan.theme.distinctRunCount} · Severity:{" "}
                {plan.theme.severityBand}
              </p>
            </section>
          ) : null}

          <section style={{ marginBottom: 24 }} aria-labelledby="plan-steps-heading">
            <h4 id="plan-steps-heading" style={{ fontSize: 16, marginBottom: 8 }}>
              Action steps
            </h4>
            {plan.actionSteps.length === 0 ? (
              <p style={{ color: "#64748b", fontSize: 14 }}>No steps recorded.</p>
            ) : (
              <ol style={{ margin: 0, paddingLeft: 22, lineHeight: 1.55, color: "#334155" }}>
                {[...plan.actionSteps].sort((a, b) => a.ordinal - b.ordinal).map((s) => (
                  <li key={`${s.ordinal}-${s.actionType}`} style={{ marginBottom: 12 }}>
                    <strong>
                      {s.ordinal}. {s.actionType}
                    </strong>
                    <p style={{ margin: "6px 0 0", fontSize: 14 }}>{s.description}</p>
                    {s.acceptanceCriteria ? (
                      <p style={{ margin: "6px 0 0", fontSize: 13, color: "#475569" }}>
                        <em>Acceptance:</em> {s.acceptanceCriteria}
                      </p>
                    ) : null}
                  </li>
                ))}
              </ol>
            )}
          </section>
        </>
      ) : null}
    </main>
  );
}
