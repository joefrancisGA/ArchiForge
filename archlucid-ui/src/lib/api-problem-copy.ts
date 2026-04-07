import type { ApiProblemDetails } from "@/lib/api-problem";

export type OperatorProblemCopy = {
  heading: string;
  body: string;
  hint?: string;
};

/** Short headings for stable `extensions.errorCode` values (API contract). */
const ERROR_CODE_HEADINGS: Record<string, string> = {
  RUN_NOT_FOUND: "Run not found",
  MANIFEST_NOT_FOUND: "Manifest not found",
  RESOURCE_NOT_FOUND: "Resource not found",
  DATABASE_TIMEOUT: "Database timeout",
  DATABASE_UNAVAILABLE: "Database unavailable",
  CIRCUIT_BREAKER_OPEN: "AI service temporarily unavailable",
  VALIDATION_FAILED: "Validation failed",
  BAD_REQUEST: "Bad request",
  CONFLICT: "Conflict",
  COMPARISON_VERIFICATION_FAILED: "Comparison verification failed",
  INTERNAL_ERROR: "Server error",
  COMMIT_FAILED: "Commit failed",
  INVALID_RUN_STATE: "Invalid run state",
  POLICY_PACK_VERSION_NOT_FOUND: "Policy pack version not found",
};

/**
 * Builds operator-facing copy: prefers API `supportHint`, then maps `errorCode`, then ProblemDetails title/detail.
 */
export function operatorCopyForProblem(
  problem: ApiProblemDetails | null,
  fallbackMessage: string,
): OperatorProblemCopy {
  const trimmedFallback = fallbackMessage.trim() || "Request failed.";

  if (problem === null) {
    return { heading: "Request failed", body: trimmedFallback };
  }

  const code = problem.errorCode?.trim();
  const fromCode = code ? ERROR_CODE_HEADINGS[code] : undefined;
  const heading = fromCode ?? problem.title?.trim() ?? "Request failed";
  const body =
    problem.detail?.trim() ?? problem.title?.trim() ?? trimmedFallback;

  const hint = problem.supportHint?.trim();

  if (hint) {
    return { heading, body, hint };
  }

  return { heading, body };
}
