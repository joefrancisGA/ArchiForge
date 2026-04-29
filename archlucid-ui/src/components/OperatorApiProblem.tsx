"use client";

import type { ApiProblemDetails } from "@/lib/api-problem";
import { operatorCopyForProblem } from "@/lib/api-problem-copy";
import { OperatorErrorCallout, OperatorWarningCallout } from "@/components/OperatorShellMessage";
import { OperatorErrorUiReferenceLine } from "@/components/OperatorErrorUiReferenceLine";

type OperatorApiProblemProps = {
  problem: ApiProblemDetails | null;
  fallbackMessage: string;
  correlationId?: string | null;
  /** Use warning styling for non-fatal secondary failures (e.g. manifest summary on run detail). */
  variant?: "error" | "warning";
};

/**
 * Renders API failures using Problem Details (`errorCode`, `supportHint`) when available,
 * plus optional correlation id (response header and/or problem JSON) for support triage.
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
      <p className="mt-2">{body}</p>
      {hint ? (
        <p className="mt-2.5 text-sm leading-normal">{hint}</p>
      ) : null}
      <OperatorErrorUiReferenceLine />
      {trimmedCorrelation && trimmedCorrelation.length > 0 ? (
        <p className="mt-2.5 font-mono text-xs text-neutral-600 dark:text-neutral-400">
          Reference (correlation ID — use with API logs and support bundle): {trimmedCorrelation}
        </p>
      ) : null}
    </Callout>
  );
}
