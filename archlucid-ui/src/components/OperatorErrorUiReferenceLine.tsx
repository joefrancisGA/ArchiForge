"use client";

import type { ReactElement } from "react";
import { useMemo } from "react";

import { cn } from "@/lib/utils";

function makeUiReferenceSegment(): string {
  if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
    return crypto.randomUUID().slice(0, 8);
  }

  return `ref-${Math.random().toString(36).slice(2, 10)}`;
}

export type OperatorErrorUiReferenceLineProps = {
  /** Optional typography override (e.g. red panel in **`global-error.tsx`**). */
  paragraphClassName?: string;
};

/**
 * Stable per-mount client reference shown on error callouts when no server correlation id exists —
 * aligns with **`OperatorApiProblem`** wording for support triage screenshots.
 */
export function OperatorErrorUiReferenceLine({
  paragraphClassName,
}: OperatorErrorUiReferenceLineProps = {}): ReactElement {
  const segment = useMemo(makeUiReferenceSegment, []);

  return (
    <p
      className={cn(
        "mt-3 font-mono text-[11px] text-neutral-600 dark:text-neutral-400",
        paragraphClassName,
      )}
    >
      Reference: ERR-{segment}
    </p>
  );
}
