import type { ReactNode } from "react";

import { cn } from "@/lib/utils";

const calloutBase =
  "mb-4 max-w-3xl rounded-lg px-4 py-3 text-[15px] leading-snug";

/**
 * API / configuration failures on review pages (server-rendered).
 */
export function OperatorErrorCallout({ children }: { children: ReactNode }) {
  return (
    <div
      role="alert"
      className={cn(
        calloutBase,
        "border border-red-800 bg-red-50 text-red-900 dark:border-red-900 dark:bg-red-950/80 dark:text-red-100",
      )}
    >
      {children}
    </div>
  );
}

/**
 * Empty collections or blocked review steps (e.g. run not committed).
 * Use `children` for rich markup, or `description` for plain text (`children` wins when both are set).
 */
export function OperatorEmptyState({
  title,
  children,
  description,
}: {
  title: string;
  children?: ReactNode;
  description?: string;
}) {
  const detail: ReactNode = children ?? description;

  return (
    <div
      role="status"
      className={cn(
        calloutBase,
        "border border-neutral-300 bg-neutral-50 text-neutral-800 dark:border-neutral-600 dark:bg-neutral-900/60 dark:text-neutral-200",
      )}
    >
      <strong>{title}</strong>
      <div className="mt-2">{detail}</div>
    </div>
  );
}

/**
 * In-progress work (explicit copy, no animation).
 */
export function OperatorLoadingNotice({ children }: { children: ReactNode }) {
  return (
    <div
      role="status"
      aria-live="polite"
      className={cn(
        calloutBase,
        "border border-neutral-300 bg-slate-50 text-slate-800 dark:border-neutral-600 dark:bg-neutral-900/40 dark:text-neutral-200",
      )}
    >
      {children}
    </div>
  );
}

/**
 * Consistent “what to do next” line after HTTP or contract failures (place below OperatorApiProblem or callouts).
 */
export function OperatorTryNext({ children }: { children: ReactNode }) {
  return (
    <div className="mt-3 max-w-3xl text-sm leading-relaxed text-neutral-600 dark:text-neutral-400">
      <strong className="text-neutral-800 dark:text-neutral-200">Try next:</strong> {children}
    </div>
  );
}

/**
 * Unexpected JSON shape or contract drift (distinct from HTTP error).
 */
export function OperatorMalformedCallout({ children }: { children: ReactNode }) {
  return (
    <div
      role="alert"
      className={cn(
        calloutBase,
        "border border-violet-600 bg-violet-50 text-violet-950 dark:border-violet-800 dark:bg-violet-950/50 dark:text-violet-100",
      )}
    >
      {children}
    </div>
  );
}

/**
 * Non-fatal secondary fetch issues (manifest summary, artifact list).
 */
export function OperatorWarningCallout({ children }: { children: ReactNode }) {
  return (
    <div
      role="status"
      className={cn(
        calloutBase,
        "border border-amber-500 bg-amber-50 text-amber-950 dark:border-amber-800 dark:bg-amber-950/40 dark:text-amber-50",
      )}
    >
      {children}
    </div>
  );
}
