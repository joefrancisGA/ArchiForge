import Link from "next/link";
import type { CSSProperties } from "react";
import type { LearningPlanListItemResponse } from "@/types/learning";
import { planningNumericCell, planningTableStyle, planningThTd } from "./planning-table-styles";

type PlanningPlansTableProps = {
  plans: LearningPlanListItemResponse[];
  themeTitleById: Map<string, string>;
};

const mutedNote: CSSProperties = { color: "#64748b", fontSize: 13 };

/** Prioritized plans with theme context and links into read-only detail. */
export function PlanningPlansTable(props: PlanningPlansTableProps) {
  const { plans, themeTitleById } = props;

  if (plans.length === 0) {
    return (
      <p style={{ color: "#64748b", fontSize: 14 }} role="status">
        No plans in this scope.
      </p>
    );
  }

  return (
    <div style={{ overflowX: "auto" }}>
      <table style={planningTableStyle}>
        <thead>
          <tr style={{ background: "#f8fafc" }}>
            <th style={planningNumericCell}>Priority</th>
            <th style={planningThTd}>Plan</th>
            <th style={planningThTd}>Theme</th>
            <th style={planningNumericCell}>Theme evidence</th>
            <th style={planningThTd}>Status</th>
          </tr>
        </thead>
        <tbody>
          {plans.map((p) => (
            <tr key={p.planId}>
              <td style={planningNumericCell}>{p.priorityScore}</td>
              <td style={planningThTd}>
                <Link href={`/planning/plans/${encodeURIComponent(p.planId)}`} style={{ color: "#1d4ed8" }}>
                  {p.title}
                </Link>
                <div style={{ fontSize: 13, color: "#475569", marginTop: 6 }}>{p.summary}</div>
              </td>
              <td style={planningThTd}>
                <span style={mutedNote}>{themeTitleById.get(p.themeId) ?? p.themeId}</span>
              </td>
              <td style={planningNumericCell}>{p.themeEvidenceSignalCount ?? "—"}</td>
              <td style={planningThTd}>{p.status}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
