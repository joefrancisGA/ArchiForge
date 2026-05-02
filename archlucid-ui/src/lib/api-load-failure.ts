import type { ApiProblemDetails } from "@/lib/api-problem";
import { isApiRequestError } from "@/lib/api-request-error";
import { maybeReportApiServerErrorFromUnknown } from "@/lib/error-telemetry";

/** Serializable load failure for server and client components (Problem Details + correlation id). */
export type ApiLoadFailureState = {
  message: string;
  problem: ApiProblemDetails | null;
  correlationId: string | null;
  /** Set when the source was {@link ApiRequestError}; used for stale-resource → branded Not Found. */
  httpStatus: number | null;
  /** Seconds suggested by `Retry-After` when the source was {@link ApiRequestError}. */
  retryAfterSeconds: number | null;
};

/** True when the API reported the target resource is missing (404). */
export function isApiNotFoundFailure(f: ApiLoadFailureState | null | undefined): boolean {
  if (f === null || f === undefined) {
    return false;
  }

  if (f.httpStatus === 404) {
    return true;
  }

  const ps = f.problem?.status;

  return ps === 404;
}

export function toApiLoadFailure(error: unknown): ApiLoadFailureState {
  if (isApiRequestError(error)) {
    maybeReportApiServerErrorFromUnknown(error);

    return {
      message: error.message,
      problem: error.problem,
      correlationId: error.correlationId,
      httpStatus: error.httpStatus,
      retryAfterSeconds: error.retryAfterSeconds,
    };
  }

  if (error instanceof Error) {
    return {
      message: error.message,
      problem: null,
      correlationId: null,
      httpStatus: null,
      retryAfterSeconds: null,
    };
  }

  return {
    message: "An unexpected error occurred.",
    problem: null,
    correlationId: null,
    httpStatus: null,
    retryAfterSeconds: null,
  };
}

/** Validation or UI messages that are not API Problem Details. */
export function uiFailureFromMessage(message: string): ApiLoadFailureState {
  const trimmed = message.trim();

  return {
    message: trimmed.length > 0 ? trimmed : "Something went wrong.",
    problem: null,
    correlationId: null,
    httpStatus: null,
    retryAfterSeconds: null,
  };
}
