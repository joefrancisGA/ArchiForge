import type { CSSProperties } from "react";

import { OperatorEmptyState } from "@/components/OperatorShellMessage";
import { sortDiffItems } from "@/lib/compare-display-sort";
import type { RunComparison } from "@/types/authority";

const tableStyle: CSSProperties = {
  borderCollapse: "collapse",
  width: "100%",
  fontSize: 14,
  marginTop: 8,
};

const cellStyle: CSSProperties = {
  border: "1px solid #e2e8f0",
  padding: "8px 10px",
  textAlign: "left",
  verticalAlign: "top",
};

const mono: CSSProperties = { fontFamily: "ui-monospace, monospace", fontSize: 13 };

/**
 * Legacy authority comparison: run-level and flat manifest diffs as stable tables.
 */
export function LegacyRunComparisonView(props: { result: RunComparison }) {
  const { result } = props;
  const runLevelDiffs = sortDiffItems(result.runLevelDiffs);
  const manifestDiffs =
    result.manifestComparison !== undefined && result.manifestComparison !== null
      ? sortDiffItems(result.manifestComparison.diffs)
      : [];

  return (
    <section id="compare-legacy" style={{ marginTop: 28 }}>
      <h3 style={{ marginBottom: 8 }}>Authority run / manifest diff (legacy)</h3>
      <p style={{ fontSize: 14, color: "#64748b", marginTop: 0 }}>
        <strong>Left (base):</strong> <code style={mono}>{result.leftRunId}</code> ·{" "}
        <strong>Right (target):</strong> <code style={mono}>{result.rightRunId}</code>
        {result.runLevelDiffCount !== undefined && (
          <>
            {" "}
            · <strong>Run-level diff count:</strong> {result.runLevelDiffCount}
          </>
        )}
      </p>

      <h4 style={{ fontSize: 15 }}>Run-level diffs</h4>
      {result.runLevelDiffs.length === 0 ? (
        <OperatorEmptyState title="No run-level diffs">
          <p style={{ margin: 0, fontSize: 14 }}>
            The legacy endpoint returned zero row-level differences (valid empty result).
          </p>
        </OperatorEmptyState>
      ) : (
        <table style={tableStyle}>
          <thead>
            <tr style={{ background: "#f8fafc" }}>
              <th style={cellStyle}>Kind</th>
              <th style={cellStyle}>Section</th>
              <th style={cellStyle}>Key</th>
              <th style={cellStyle}>Before</th>
              <th style={cellStyle}>After</th>
            </tr>
          </thead>
          <tbody>
            {runLevelDiffs.map((diff, index) => (
              <tr key={`${diff.section}-${diff.key}-${diff.diffKind}-${index}`}>
                <td style={cellStyle}>{diff.diffKind}</td>
                <td style={cellStyle}>{diff.section}</td>
                <td style={cellStyle}>{diff.key}</td>
                <td style={{ ...cellStyle, ...mono }}>{diff.beforeValue ?? "—"}</td>
                <td style={{ ...cellStyle, ...mono }}>{diff.afterValue ?? "—"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      <h4 style={{ fontSize: 15, marginTop: 24 }}>Manifest differences (flat)</h4>
      {!result.manifestComparison ? (
        <OperatorEmptyState title="No manifest comparison block">
          <p style={{ margin: 0, fontSize: 14 }}>
            The API did not include a manifest comparison object for this pair (distinct from “zero
            diffs inside a comparison”).
          </p>
        </OperatorEmptyState>
      ) : (
        <>
          <p style={{ fontSize: 14, marginBottom: 8 }}>
            <strong>Manifest IDs:</strong>{" "}
            <code style={mono}>{result.manifestComparison.leftManifestId}</code> vs{" "}
            <code style={mono}>{result.manifestComparison.rightManifestId}</code>
            <br />
            <strong>Hashes:</strong>{" "}
            <span style={mono}>{result.manifestComparison.leftManifestHash}</span> vs{" "}
            <span style={mono}>{result.manifestComparison.rightManifestHash}</span>
            <br />
            <strong>Counts:</strong> added {result.manifestComparison.addedCount}, removed{" "}
            {result.manifestComparison.removedCount}, changed {result.manifestComparison.changedCount}
          </p>
          {manifestDiffs.length === 0 ? (
            <OperatorEmptyState title="Manifest comparison has zero line items">
              <p style={{ margin: 0, fontSize: 14 }}>Comparison object present but diff list is empty.</p>
            </OperatorEmptyState>
          ) : (
            <table style={tableStyle}>
              <thead>
                <tr style={{ background: "#f8fafc" }}>
                  <th style={cellStyle}>Kind</th>
                  <th style={cellStyle}>Section</th>
                  <th style={cellStyle}>Key</th>
                  <th style={cellStyle}>Before</th>
                  <th style={cellStyle}>After</th>
                  <th style={cellStyle}>Notes</th>
                </tr>
              </thead>
              <tbody>
                {manifestDiffs.map((diff, index) => (
                  <tr key={`${diff.section}-${diff.key}-${diff.diffKind}-${index}`}>
                    <td style={cellStyle}>{diff.diffKind}</td>
                    <td style={cellStyle}>{diff.section}</td>
                    <td style={cellStyle}>{diff.key}</td>
                    <td style={{ ...cellStyle, ...mono }}>{diff.beforeValue ?? "—"}</td>
                    <td style={{ ...cellStyle, ...mono }}>{diff.afterValue ?? "—"}</td>
                    <td style={cellStyle}>{diff.notes ?? "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </>
      )}
    </section>
  );
}
