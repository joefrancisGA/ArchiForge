import type { ApiProblemDetails } from "@/lib/api-problem";
import { isApiRequestError } from "@/lib/api-request-error";

/** Serializable load failure for server and client components (Problem Details + correlation id). */
export type ApiLoadFailureState = {
  message: string;
  problem: ApiProblemDetails | null;
  correlationId: string | null;
};

export function toApiLoadFailure(error: unknown): ApiLoadFailureState {
  if (isApiRequestError(error)) {
    return {
      message: error.message,
      problem: error.problem,
      correlationId: error.correlationId,
    };
  }

  if (error instanceof Error) {
    return { message: error.message, problem: null, correlationId: null };
  }

  return { message: "An unexpected error occurred.", problem: null, correlationId: null };
}

/** Validation or UI messages that are not API Problem Details. */
export function uiFailureFromMessage(message: string): ApiLoadFailureState {
  const trimmed = message.trim();

  return {
    message: trimmed.length > 0 ? trimmed : "Something went wrong.",
    problem: null,
    correlationId: null,
  };
}
