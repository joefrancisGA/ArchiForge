import Link from "next/link";
import type { CSSProperties } from "react";

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

/**
 * Sets expectations: this surface is browse-only; formal roll-up export lives on pilot feedback until a
 * dedicated learning report route exists on the API.
 */
export function PlanningExportReadinessNote() {
  return (
    <aside style={box} aria-label="Reporting and export readiness">
      <strong>Reporting and export</strong>
      <p style={{ margin: "8px 0 0" }}>
        This view is optimized for in-browser review (tables, plan detail, and print-to-PDF from the browser). There is
        no dedicated 59R planning file export on the API yet; use{" "}
        <Link href="/product-learning" style={{ color: "#1d4ed8" }}>
          Pilot feedback
        </Link>{" "}
        for Markdown/JSON roll-up exports. The underlying JSON remains available to automation via{" "}
        <code style={{ fontSize: 13 }}>GET /v1/learning/summary</code>, <code style={{ fontSize: 13 }}>…/themes</code>,{" "}
        and <code style={{ fontSize: 13 }}>…/plans</code> (same scope as the operator shell).
      </p>
    </aside>
  );
}
