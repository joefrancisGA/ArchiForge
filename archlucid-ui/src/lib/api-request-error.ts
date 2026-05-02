import type { ApiProblemDetails } from "@/lib/api-problem";

/**
 * Thrown when an ArchLucid API call returns a non-success HTTP status.
 * Carries structured Problem Details and correlation id when present.
 */
export class ApiRequestError extends Error {
  readonly problem: ApiProblemDetails | null;

  readonly correlationId: string | null;

  readonly httpStatus: number;

  /** From `Retry-After` when the upstream returned it (typically with 429). */
  readonly retryAfterSeconds: number | null;

  constructor(
    message: string,
    options: {
      problem: ApiProblemDetails | null;
      correlationId: string | null;
      httpStatus: number;
      retryAfterSeconds?: number | null;
    },
  ) {
    super(message);
    this.name = "ApiRequestError";
    this.problem = options.problem;
    this.correlationId = options.correlationId;
    this.httpStatus = options.httpStatus;
    this.retryAfterSeconds = options.retryAfterSeconds ?? null;
  }
}

export function isApiRequestError(value: unknown): value is ApiRequestError {
  return value instanceof ApiRequestError;
}
