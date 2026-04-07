import type { ApiProblemDetails } from "@/lib/api-problem";

/**
 * Thrown when an ArchiForge API call returns a non-success HTTP status.
 * Carries structured Problem Details and correlation id when present.
 */
export class ApiRequestError extends Error {
  readonly problem: ApiProblemDetails | null;

  readonly correlationId: string | null;

  readonly httpStatus: number;

  constructor(
    message: string,
    options: {
      problem: ApiProblemDetails | null;
      correlationId: string | null;
      httpStatus: number;
    },
  ) {
    super(message);
    this.name = "ApiRequestError";
    this.problem = options.problem;
    this.correlationId = options.correlationId;
    this.httpStatus = options.httpStatus;
  }
}

export function isApiRequestError(value: unknown): value is ApiRequestError {
  return value instanceof ApiRequestError;
}
