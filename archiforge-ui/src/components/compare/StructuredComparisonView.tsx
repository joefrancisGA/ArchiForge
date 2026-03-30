import type { CSSProperties } from "react";

import { OperatorEmptyState } from "@/components/OperatorShellMessage";
import { getArchitecturePackageDocxUrl } from "@/lib/api";
import type { GoldenManifestComparison } from "@/types/comparison";

const tableStyle: CSSProperties = {
  borderCollapse: "collapse",
  width: "100%",
  fontSize: 14,
  marginTop: 8,
};

const cellStyle: React.CSSProperties = {
  border: "1px solid #e2e8f0",
  padding: "8px 10px",
  textAlign: "left",
  verticalAlign: "top",
};

const sectionBox: CSSProperties = {
  marginTop: 20,
  padding: 16,
  border: "1px solid #e2e8f0",
  borderRadius: 8,
  background: "#fff",
};

/** Inline empty-state note for a comparison section with zero deltas. */
function EmptySectionNote({ label }: { label: string }) {
  return (
    <OperatorEmptyState title={label}>
      <p style={{ margin: 0, fontSize: 14 }}>No rows in this section for this pair of runs.</p>
    </OperatorEmptyState>
  );
}

/**
 * Golden-manifest structured comparison: tables and stable column order for operator review.
 */
export function StructuredComparisonView(props: { golden: GoldenManifestComparison }) {
  const { golden } = props;
  const total =
    golden.totalDeltaCount !== undefined
      ? golden.totalDeltaCount
      : golden.decisionChanges.length +
        golden.requirementChanges.length +
        golden.securityChanges.length +
        golden.topologyChanges.length +
        golden.costChanges.length;

  return (
    <section style={{ marginTop: 28 }}>
      <h3 style={{ marginBottom: 8 }}>Structured manifest comparison</h3>
      <div
        style={{
          display: "flex",
          flexWrap: "wrap",
          gap: 12,
          alignItems: "baseline",
          fontSize: 14,
          color: "#334155",
          marginBottom: 12,
        }}
      >
        <span>
          <strong>Base run:</strong>{" "}
          <code style={{ fontSize: 13 }}>{golden.baseRunId}</code>
        </span>
        <span aria-hidden="true" style={{ color: "#cbd5e1" }}>
          →
        </span>
        <span>
          <strong>Target run:</strong>{" "}
          <code style={{ fontSize: 13 }}>{golden.targetRunId}</code>
        </span>
        <span style={{ color: "#64748b" }}>
          · <strong>Total deltas (reported):</strong> {total}
        </span>
      </div>
      <p style={{ margin: "0 0 16px", fontSize: 14 }}>
        <a
          href={getArchitecturePackageDocxUrl(golden.baseRunId, golden.targetRunId, {
            includeComparisonExplanation: true,
          })}
          rel="noreferrer"
        >
          Download architecture package DOCX (includes comparison; AI narrative when configured)
        </a>
      </p>

      <div style={sectionBox}>
        <h4 style={{ marginTop: 0, marginBottom: 8, fontSize: 15 }}>Summary highlights</h4>
        {golden.summaryHighlights.length === 0 ? (
          <EmptySectionNote label="No summary highlights" />
        ) : (
          <ul style={{ margin: 0, paddingLeft: 20, lineHeight: 1.5 }}>
            {golden.summaryHighlights.map((h, i) => (
              <li key={i}>{h}</li>
            ))}
          </ul>
        )}
      </div>

      <div style={sectionBox}>
        <h4 style={{ marginTop: 0, marginBottom: 8, fontSize: 15 }}>Decision changes</h4>
        {golden.decisionChanges.length === 0 ? (
          <EmptySectionNote label="No decision changes" />
        ) : (
          <table style={tableStyle}>
            <thead>
              <tr style={{ background: "#f8fafc" }}>
                <th style={cellStyle}>Decision</th>
                <th style={cellStyle}>Base</th>
                <th style={cellStyle}>Target</th>
                <th style={cellStyle}>Change</th>
              </tr>
            </thead>
            <tbody>
              {golden.decisionChanges.map((d, i) => (
                <tr key={i}>
                  <td style={cellStyle}>{d.decisionKey}</td>
                  <td style={cellStyle}>{d.baseValue ?? "—"}</td>
                  <td style={cellStyle}>{d.targetValue ?? "—"}</td>
                  <td style={cellStyle}>{d.changeType}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      <div style={sectionBox}>
        <h4 style={{ marginTop: 0, marginBottom: 8, fontSize: 15 }}>Requirement changes</h4>
        {golden.requirementChanges.length === 0 ? (
          <EmptySectionNote label="No requirement changes" />
        ) : (
          <table style={tableStyle}>
            <thead>
              <tr style={{ background: "#f8fafc" }}>
                <th style={cellStyle}>Requirement</th>
                <th style={cellStyle}>Change</th>
              </tr>
            </thead>
            <tbody>
              {golden.requirementChanges.map((r, i) => (
                <tr key={i}>
                  <td style={cellStyle}>{r.requirementName}</td>
                  <td style={cellStyle}>{r.changeType}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      <div style={sectionBox}>
        <h4 style={{ marginTop: 0, marginBottom: 8, fontSize: 15 }}>Security posture delta</h4>
        {golden.securityChanges.length === 0 ? (
          <EmptySectionNote label="No security control changes" />
        ) : (
          <table style={tableStyle}>
            <thead>
              <tr style={{ background: "#f8fafc" }}>
                <th style={cellStyle}>Control</th>
                <th style={cellStyle}>Base</th>
                <th style={cellStyle}>Target</th>
              </tr>
            </thead>
            <tbody>
              {golden.securityChanges.map((s, i) => (
                <tr key={i}>
                  <td style={cellStyle}>{s.controlName}</td>
                  <td style={cellStyle}>{s.baseStatus ?? "—"}</td>
                  <td style={cellStyle}>{s.targetStatus ?? "—"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      <div style={sectionBox}>
        <h4 style={{ marginTop: 0, marginBottom: 8, fontSize: 15 }}>Topology changes</h4>
        {golden.topologyChanges.length === 0 ? (
          <EmptySectionNote label="No topology changes" />
        ) : (
          <table style={tableStyle}>
            <thead>
              <tr style={{ background: "#f8fafc" }}>
                <th style={cellStyle}>Resource</th>
                <th style={cellStyle}>Change</th>
              </tr>
            </thead>
            <tbody>
              {golden.topologyChanges.map((t, i) => (
                <tr key={i}>
                  <td style={cellStyle}>{t.resource}</td>
                  <td style={cellStyle}>{t.changeType}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      <div style={sectionBox}>
        <h4 style={{ marginTop: 0, marginBottom: 8, fontSize: 15 }}>Cost delta</h4>
        {golden.costChanges.length === 0 ? (
          <OperatorEmptyState title="No cost line items">
            <p style={{ margin: 0, fontSize: 14 }}>Max monthly cost unchanged or not modeled as a delta row.</p>
          </OperatorEmptyState>
        ) : (
          <table style={tableStyle}>
            <thead>
              <tr style={{ background: "#f8fafc" }}>
                <th style={cellStyle}>Base (max monthly)</th>
                <th style={cellStyle}>Target (max monthly)</th>
              </tr>
            </thead>
            <tbody>
              {golden.costChanges.map((c, i) => (
                <tr key={i}>
                  <td style={cellStyle}>{c.baseCost ?? "—"}</td>
                  <td style={cellStyle}>{c.targetCost ?? "—"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </section>
  );
}
