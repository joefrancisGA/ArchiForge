"use client";

import { OperatorLoadingNotice } from "@/components/OperatorShellMessage";
import { Progress } from "@/components/ui/progress";
import type { RunExplanationSummary } from "@/types/explanation";

export type RunExplanationSectionProps = {
  summary: RunExplanationSummary | null;
  loading: boolean;
  error: string | null;
};

/** Maps API `riskPosture` string to accessible badge colors (Low / Medium / High / Critical). */
export function riskPostureBadgeColors(posture: string): {
  background: string;
  color: string;
  borderColor: string;
} {
  const key = posture.trim().toLowerCase();

  if (key === "critical") {
    return { background: "#fee2e2", color: "#991b1b", borderColor: "#fecaca" };
  }

  if (key === "high") {
    return { background: "#ffedd5", color: "#c2410c", borderColor: "#fed7aa" };
  }

  if (key === "medium") {
    return { background: "#fef3c7", color: "#92400e", borderColor: "#fde68a" };
  }

  return { background: "#dcfce7", color: "#166534", borderColor: "#bbf7d0" };
}

function confidencePercent(confidence: number): number {
  if (!Number.isFinite(confidence)) {
    return 0;
  }

  const pct = confidence <= 1 ? Math.round(confidence * 100) : Math.round(confidence);

  return Math.min(100, Math.max(0, pct));
}

/**
 * Run-level aggregate explanation: assessment, posture, confidence, themes, drivers/risks, provenance.
 */
export function RunExplanationSection({ summary, loading, error }: RunExplanationSectionProps) {
  if (loading) {
    return (
      <div aria-busy="true">
        <OperatorLoadingNotice>Loading explanation…</OperatorLoadingNotice>
      </div>
    );
  }

  if (error) {
    return (
      <p role="alert" style={{ margin: 0, color: "#b91c1c", fontSize: 14 }}>
        {error}
      </p>
    );
  }

  if (!summary) {
    return null;
  }

  const badge = riskPostureBadgeColors(summary.riskPosture);
  const conf = summary.explanation.confidence;
  const pct = conf !== null && conf !== undefined ? confidencePercent(conf) : null;
  const prov = summary.explanation.provenance;

  return (
    <div style={{ maxWidth: 720 }}>
      <p
        style={{
          margin: "0 0 12px",
          fontSize: 16,
          fontWeight: 600,
          lineHeight: 1.4,
          color: "#0f172a",
        }}
      >
        {summary.overallAssessment}
      </p>

      <p style={{ margin: "0 0 12px", fontSize: 14, color: "#475569" }}>
        <span className="sr-only">Risk posture:</span>
        <span
          role="status"
          aria-label={`Risk posture ${summary.riskPosture}`}
          data-risk-posture={summary.riskPosture.trim().toLowerCase()}
          style={{
            display: "inline-block",
            padding: "4px 10px",
            borderRadius: 6,
            fontSize: 13,
            fontWeight: 600,
            border: `1px solid ${badge.borderColor}`,
            background: badge.background,
            color: badge.color,
          }}
        >
          {summary.riskPosture}
        </span>
        <span style={{ marginLeft: 12, fontSize: 13, color: "#64748b" }}>
          {summary.decisionCount} decisions · {summary.findingCount} findings · {summary.unresolvedIssueCount}{" "}
          unresolved · {summary.complianceGapCount} compliance gaps
        </span>
      </p>

      <div style={{ marginBottom: 16 }}>
        <p id="explanation-confidence-label" style={{ margin: "0 0 6px", fontSize: 14, fontWeight: 600 }}>
          Model confidence
        </p>
        {pct === null ? (
          <p role="status" style={{ margin: 0, fontSize: 14, color: "#64748b" }}>
            Not available
          </p>
        ) : (
          <>
            <Progress
              value={pct}
              aria-valuemin={0}
              aria-valuemax={100}
              aria-valuenow={pct}
              aria-labelledby="explanation-confidence-label"
            />
            <p style={{ margin: "6px 0 0", fontSize: 13, color: "#64748b" }}>{pct}%</p>
          </>
        )}
      </div>

      <div style={{ marginBottom: 16 }}>
        <h4 style={{ margin: "0 0 8px", fontSize: 15 }}>Themes</h4>
        <ul style={{ margin: 0, paddingLeft: 20, fontSize: 14, lineHeight: 1.5 }}>
          {summary.themeSummaries.map((t) => (
            <li key={t}>{t}</li>
          ))}
        </ul>
      </div>

      <div style={{ marginBottom: 16 }}>
        <h4 style={{ margin: "0 0 8px", fontSize: 15 }}>Key drivers</h4>
        <ul style={{ margin: 0, paddingLeft: 20, fontSize: 14, lineHeight: 1.5 }}>
          {summary.explanation.keyDrivers.map((d) => (
            <li key={d}>{d}</li>
          ))}
        </ul>
      </div>

      <div style={{ marginBottom: 16 }}>
        <h4 style={{ margin: "0 0 8px", fontSize: 15 }}>Risk implications</h4>
        <ul style={{ margin: 0, paddingLeft: 20, fontSize: 14, lineHeight: 1.5 }}>
          {summary.explanation.riskImplications.map((r) => (
            <li key={r}>{r}</li>
          ))}
        </ul>
      </div>

      {prov ? (
        <details style={{ fontSize: 14, color: "#334155" }}>
          <summary style={{ cursor: "pointer", fontWeight: 600 }}>Provenance metadata</summary>
          <dl
            style={{
              margin: "12px 0 0",
              display: "grid",
              gridTemplateColumns: "auto 1fr",
              gap: "6px 16px",
            }}
          >
            <dt>Agent type</dt>
            <dd style={{ margin: 0 }}>{prov.agentType}</dd>
            <dt>Model ID</dt>
            <dd style={{ margin: 0 }}>{prov.modelId}</dd>
            <dt>Prompt template</dt>
            <dd style={{ margin: 0 }}>{prov.promptTemplateId ?? "—"}</dd>
            <dt>Prompt version</dt>
            <dd style={{ margin: 0 }}>{prov.promptTemplateVersion ?? "—"}</dd>
            <dt>Content hash</dt>
            <dd style={{ margin: 0 }}>{prov.promptContentHash ?? "—"}</dd>
          </dl>
        </details>
      ) : null}
    </div>
  );
}
