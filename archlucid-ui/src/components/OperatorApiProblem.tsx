"use client";

import type { CSSProperties } from "react";

import type { ApiProblemDetails } from "@/lib/api-problem";
import { operatorCopyForProblem } from "@/lib/api-problem-copy";
import { OperatorErrorCallout, OperatorWarningCallout } from "@/components/OperatorShellMessage";

const correlationStyle: CSSProperties = {
  margin: "10px 0 0",
  fontSize: 12,
  fontFamily: "ui-monospace, monospace",
  color: "#475569",
};

type OperatorApiProblemProps = {
  problem: ApiProblemDetails | null;
  fallbackMessage: string;
  correlationId?: string | null;
  /** Use warning styling for non-fatal secondary failures (e.g. manifest summary on run detail). */
  variant?: "error" | "warning";
};

/**
 * Renders API failures using Problem Details (`errorCode`, `supportHint`) when available,
 * plus optional **X-Correlation-ID** for support (WAF RE:05 / OE:07).
 */
export function OperatorApiProblem({
  problem,
  fallbackMessage,
  correlationId,
  variant = "error",
}: OperatorApiProblemProps) {
  const { heading, body, hint } = operatorCopyForProblem(problem, fallbackMessage);
  const Callout = variant === "warning" ? OperatorWarningCallout : OperatorErrorCallout;
  const trimmedCorrelation = correlationId?.trim();

  return (
    <Callout>
      <strong>{heading}</strong>
      <p style={{ margin: "8px 0 0" }}>{body}</p>
      {hint ? (
        <p style={{ margin: "10px 0 0", fontSize: 14, lineHeight: 1.5 }}>{hint}</p>
      ) : null}
      {trimmedCorrelation && trimmedCorrelation.length > 0 ? (
        <p style={correlationStyle}>
          Reference (X-Correlation-ID): {trimmedCorrelation}
        </p>
      ) : null}
    </Callout>
  );
}
