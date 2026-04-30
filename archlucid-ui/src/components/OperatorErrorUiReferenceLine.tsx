"use client";

import { useEffect, useMemo } from "react";

function makeUiReferenceSegment(): string {
  if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
    return crypto.randomUUID().slice(0, 8);
  }

  return `ref-${Math.random().toString(36).slice(2, 10)}`;
}

export type OperatorErrorUiReferenceLineProps = {
  /** @deprecated Kept for API compatibility; reference is no longer shown in the UI. */
  paragraphClassName?: string;
};

/**
 * Logs a per-mount support reference for triage. Does not render buyer-facing “ERR-…” text.
 */
export function OperatorErrorUiReferenceLine(_props: OperatorErrorUiReferenceLineProps = {}): null {
  const segment = useMemo(makeUiReferenceSegment, []);

  useEffect(() => {
    console.info("[ArchLucid support reference]", `ERR-${segment}`);
  }, [segment]);

  return null;
}
