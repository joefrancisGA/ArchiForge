import type { CSSProperties } from "react";
import { formatIsoUtcForDisplay } from "@/lib/format-iso-utc";
import type { LearningSummaryResponse } from "@/types/learning";

const cardList: CSSProperties = {
  display: "flex",
  flexWrap: "wrap",
  gap: 10,
  listStyle: "none",
  padding: 0,
  margin: "12px 0 0",
};

const card: CSSProperties = {
  border: "1px solid #e2e8f0",
  borderRadius: 8,
  padding: "10px 14px",
  minWidth: 160,
};

type PlanningSummarySectionProps = {
  summary: LearningSummaryResponse;
  generatedUtc: string | null;
};

/** Roll-up KPIs: evidence-style counts and plan priority ceiling. */
export function PlanningSummarySection(props: PlanningSummarySectionProps) {
  const { summary, generatedUtc } = props;

  return (
    <section style={{ marginBottom: 28 }} aria-labelledby="planning-summary-heading">
      <h3 id="planning-summary-heading" style={{ fontSize: 17, marginBottom: 8 }}>
        Summary
      </h3>
      <p style={{ color: "#64748b", fontSize: 13, marginTop: 0 }}>
        Generated {generatedUtc ? formatIsoUtcForDisplay(generatedUtc) : "—"} · {summary.themeCount} theme(s) ·{" "}
        {summary.planCount} plan(s)
      </p>
      <ul style={cardList}>
        <li style={card}>
          <div style={{ fontSize: 12, color: "#64748b" }}>Theme evidence (signals)</div>
          <div style={{ fontSize: 20, fontWeight: 600 }}>{summary.totalThemeEvidenceSignals}</div>
        </li>
        <li style={card}>
          <div style={{ fontSize: 12, color: "#64748b" }}>Linked signals (plans)</div>
          <div style={{ fontSize: 20, fontWeight: 600 }}>{summary.totalLinkedSignalsAcrossPlans}</div>
        </li>
        <li style={card}>
          <div style={{ fontSize: 12, color: "#64748b" }}>Max plan priority</div>
          <div style={{ fontSize: 20, fontWeight: 600 }}>{summary.maxPlanPriorityScore ?? "—"}</div>
        </li>
      </ul>
    </section>
  );
}
