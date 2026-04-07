import type { CSSProperties } from "react";
import type { LearningPlanListItemResponse, LearningThemeResponse } from "@/types/learning";
import { planningNumericCell, planningTableStyle, planningThTd } from "./planning-table-styles";

type PlanningThemesTableProps = {
  themes: LearningThemeResponse[];
  plans: LearningPlanListItemResponse[];
  selectedThemeId: string | null;
  onSelectThemeForPlans: (themeId: string) => void;
};

const browseBtn: CSSProperties = {
  fontSize: 13,
  padding: "4px 10px",
  cursor: "pointer",
};

function countPlansForTheme(plans: LearningPlanListItemResponse[], themeId: string): number {
  return plans.filter((p) => p.themeId === themeId).length;
}

/** Read-only theme list with evidence counts and a one-click path into filtered plans. */
export function PlanningThemesTable(props: PlanningThemesTableProps) {
  const { themes, plans, selectedThemeId, onSelectThemeForPlans } = props;

  if (themes.length === 0) {
    return (
      <p style={{ color: "#64748b", fontSize: 14 }} role="status">
        No themes in this scope.
      </p>
    );
  }

  return (
    <div style={{ overflowX: "auto" }}>
      <table style={planningTableStyle}>
        <thead>
          <tr style={{ background: "#f8fafc" }}>
            <th style={planningThTd}>Title</th>
            <th style={planningThTd}>Severity</th>
            <th style={planningNumericCell}>Evidence signals</th>
            <th style={planningNumericCell}>Runs</th>
            <th style={planningThTd}>Area</th>
            <th style={planningThTd}>Plans</th>
            <th style={planningThTd}>Summary</th>
          </tr>
        </thead>
        <tbody>
          {themes.map((t) => {
            const planCount = countPlansForTheme(plans, t.themeId);
            const isActive = selectedThemeId === t.themeId;

            return (
              <tr key={t.themeId} style={isActive ? { background: "#eff6ff" } : undefined}>
                <td style={planningThTd}>
                  <strong>{t.title}</strong>
                  <div style={{ fontSize: 12, color: "#94a3b8", marginTop: 4 }}>{t.themeKey}</div>
                </td>
                <td style={planningThTd}>{t.severityBand}</td>
                <td style={planningNumericCell}>{t.evidenceSignalCount}</td>
                <td style={planningNumericCell}>{t.distinctRunCount}</td>
                <td style={planningThTd}>{t.affectedArtifactTypeOrWorkflowArea || "—"}</td>
                <td style={planningThTd}>
                  {planCount === 0 ? (
                    <span style={{ color: "#94a3b8", fontSize: 13 }}>—</span>
                  ) : (
                    <button
                      type="button"
                      style={browseBtn}
                      onClick={() => onSelectThemeForPlans(t.themeId)}
                      aria-pressed={isActive}
                      aria-label={`Show ${planCount} plan(s) for theme ${t.title}`}
                    >
                      {planCount} plan{planCount === 1 ? "" : "s"}
                    </button>
                  )}
                </td>
                <td style={{ ...planningThTd, fontSize: 13, maxWidth: 280 }}>{t.summary}</td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
