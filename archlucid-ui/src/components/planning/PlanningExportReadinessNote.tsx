import Link from "next/link";
import type { CSSProperties } from "react";
import {
  buildLearningPlanningReportFileUrl,
  buildLearningPlanningReportJsonUrl,
} from "@/lib/learning-planning-report-urls";

const box: CSSProperties = {
  border: "1px solid #e2e8f0",
  borderRadius: 8,
  padding: "12px 14px",
  marginTop: 20,
  background: "#f8fafc",
  fontSize: 14,
  color: "#334155",
  lineHeight: 1.55,
  maxWidth: 720,
};

/** Download / API links for 59R planning reports plus cross-link to 58R pilot feedback exports. */
export function PlanningExportReadinessNote() {
  return (
    <aside style={box} aria-label="Reporting and export readiness">
      <strong>Reporting and export</strong>
      <p style={{ margin: "8px 0 0" }}>
        <strong>59R planning report</strong> —{" "}
        <a
          href={buildLearningPlanningReportFileUrl("markdown")}
          className="workflow-inline-link font-medium text-blue-900 dark:text-blue-300"
        >
          Download Markdown
        </a>
        {" · "}
        <a
          href={buildLearningPlanningReportFileUrl("json")}
          className="workflow-inline-link font-medium text-blue-900 dark:text-blue-300"
        >
          Download JSON
        </a>
        {" · "}
        <a
          href={buildLearningPlanningReportJsonUrl()}
          className="workflow-inline-link font-medium text-blue-900 dark:text-blue-300"
          target="_blank"
          rel="noreferrer"
        >
          Open JSON in browser
        </a>
        . Same scope as the operator shell (<code style={{ fontSize: 13 }}>GET /v1/learning/report</code>,{" "}
        <code style={{ fontSize: 13 }}>…/report/file</code>). For 58R pilot roll-ups, use{" "}
        <Link href="/product-learning" className="workflow-inline-link font-medium text-blue-900 dark:text-blue-300">
          Pilot feedback
        </Link>
        .
      </p>
    </aside>
  );
}
