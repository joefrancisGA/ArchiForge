import type { ApiProblemDetails } from "@/lib/api-problem";

export type OperatorProblemCopy = {
  heading: string;
  body: string;
  hint?: string;
};

/** Optional HTTP context for operator copy (e.g. 429 + `Retry-After`). */
export type OperatorProblemCopyContext = {
  httpStatus?: number | null;
  retryAfterSeconds?: number | null;
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
  COMMIT_FAILED: "Finalization failed",
  INVALID_RUN_STATE: "Invalid run state",
  POLICY_PACK_VERSION_NOT_FOUND: "Policy pack version not found",
};

/**
 * Common, actionable remediation steps for stable `errorCode` values.
 * Used as a fallback if the API does not provide a specific `supportHint`.
 */
const ERROR_CODE_REMEDIATION: Record<string, string> = {
  DATABASE_TIMEOUT: "The database took too long to respond. Wait a minute and try again. If the issue persists, check the database health in the admin dashboard.",
  DATABASE_UNAVAILABLE: "The database is currently unreachable. Verify your connection strings and ensure the database server is running.",
  CIRCUIT_BREAKER_OPEN: "The AI service is currently overwhelmed or unavailable. Please wait a few minutes before retrying your request.",
  VALIDATION_FAILED: "Review the highlighted fields above and correct any invalid inputs before resubmitting.",
  CONFLICT: "Another user or process may have modified this resource. Please refresh the page to see the latest changes.",
  COMPARISON_VERIFICATION_FAILED: "The runs you selected cannot be compared. Ensure they belong to the same project and have compatible manifests.",
  INVALID_RUN_STATE: "This run is not in a valid state for this action. Refresh the page to check its current progress.",
  POLICY_PACK_VERSION_NOT_FOUND: "The requested policy pack version is missing. It may have been deleted or archived.",
  INTERNAL_ERROR: "An unexpected server error occurred. Try your action again in a few moments.",
};

function mergeRateLimitCopy(
  base: OperatorProblemCopy,
  context: OperatorProblemCopyContext,
  problem: ApiProblemDetails | null,
): OperatorProblemCopy {
  const effectiveStatus = context.httpStatus ?? problem?.status ?? null;

  if (effectiveStatus !== 429) {
    return base;
  }

  const retrySec = context.retryAfterSeconds;
  const retryLine =
    retrySec !== null && retrySec !== undefined && retrySec > 0
      ? `The service asked you to wait about ${retrySec} second${retrySec === 1 ? "" : "s"} before retrying.`
      : "Wait a short time, then try again.";

  const hintParts = [retryLine, base.hint?.trim()].filter((p) => p !== undefined && p.length > 0);
  const hint = hintParts.length > 0 ? hintParts.join(" ") : undefined;

  return { heading: "Too many requests", body: base.body, hint };
}

/**
 * Builds operator-facing copy: prefers API `supportHint`, then fallback remediation by `errorCode`, then ProblemDetails title/detail.
 * When status is **429** (from `context` or problem `status`), heading becomes rate-limit copy and `Retry-After` is surfaced when present.
 */
export function operatorCopyForProblem(
  problem: ApiProblemDetails | null,
  fallbackMessage: string,
  context: OperatorProblemCopyContext = {},
): OperatorProblemCopy {
  const trimmedFallback = fallbackMessage.trim() || "Request failed.";

  if (problem === null) {
    return mergeRateLimitCopy({ heading: "Request failed", body: trimmedFallback }, context, problem);
  }

  const code = problem.errorCode?.trim();
  const fromCode = code ? ERROR_CODE_HEADINGS[code] : undefined;
  const heading = fromCode ?? problem.title?.trim() ?? "Request failed";
  const body =
    problem.detail?.trim() ?? problem.title?.trim() ?? trimmedFallback;

  const apiHint = problem.supportHint?.trim();
  const fallbackHint = code ? ERROR_CODE_REMEDIATION[code] : undefined;
  const hint = apiHint || fallbackHint;

  const base: OperatorProblemCopy = hint ? { heading, body, hint } : { heading, body };

  return mergeRateLimitCopy(base, context, problem);
}
