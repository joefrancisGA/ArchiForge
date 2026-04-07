import type { CSSProperties } from "react";

import { OperatorEmptyState } from "@/components/OperatorShellMessage";
import type { ComparisonExplanation } from "@/types/explanation";

const sectionBox: CSSProperties = {
  marginTop: 20,
  padding: 16,
  border: "1px solid #e2e8f0",
  borderRadius: 8,
  background: "#fff",
};

/**
 * LLM-generated comparison narrative (when the explain endpoint succeeds).
 */
export function AiComparisonExplanationView(props: { explanation: ComparisonExplanation }) {
  const { explanation } = props;

  return (
    <section id="compare-ai" style={{ marginTop: 28 }}>
      <h3 style={{ marginBottom: 8 }}>AI explanation</h3>
      <p style={{ fontSize: 13, color: "#64748b", marginTop: 0 }}>
        Generated from structured deltas. Treat as narrative assistance only—confirm every claim against the
        structured and legacy tables before sign-off.
      </p>

      <div style={sectionBox}>
        <h4 style={{ marginTop: 0, fontSize: 15 }}>Summary</h4>
        <p style={{ fontWeight: 600, margin: 0, lineHeight: 1.5 }}>{explanation.highLevelSummary}</p>
      </div>

      <div style={sectionBox}>
        <h4 style={{ marginTop: 0, fontSize: 15 }}>Major changes (from structured delta)</h4>
        {explanation.majorChanges.length === 0 ? (
          <OperatorEmptyState title="No major change lines">
            <p style={{ margin: 0, fontSize: 14 }}>The model returned an empty list for this section.</p>
          </OperatorEmptyState>
        ) : (
          <ul style={{ margin: 0, paddingLeft: 20, lineHeight: 1.55 }}>
            {explanation.majorChanges.map((line, i) => (
              <li key={i}>{line}</li>
            ))}
          </ul>
        )}
      </div>

      <div style={sectionBox}>
        <h4 style={{ marginTop: 0, fontSize: 15 }}>Key tradeoffs</h4>
        {explanation.keyTradeoffs.length === 0 ? (
          <OperatorEmptyState title="No tradeoff lines">
            <p style={{ margin: 0, fontSize: 14 }}>None reported for this comparison.</p>
          </OperatorEmptyState>
        ) : (
          <ul style={{ margin: 0, paddingLeft: 20, lineHeight: 1.55 }}>
            {explanation.keyTradeoffs.map((line, i) => (
              <li key={i}>{line}</li>
            ))}
          </ul>
        )}
      </div>

      <div style={sectionBox}>
        <h4 style={{ marginTop: 0, fontSize: 15 }}>Narrative</h4>
        <p style={{ whiteSpace: "pre-wrap", lineHeight: 1.55, margin: 0 }}>{explanation.narrative}</p>
      </div>
    </section>
  );
}
