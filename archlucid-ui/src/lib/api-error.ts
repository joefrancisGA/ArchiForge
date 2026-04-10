import type { ApiProblemDetails } from "@/lib/api-problem";
import { tryParseApiProblemDetails } from "@/lib/api-problem";
import { CORRELATION_ID_HEADER } from "@/lib/correlation";
import { ApiRequestError } from "@/lib/api-request-error";

/**
 * Single-line message for logs and backward-compatible `Error.message` (legacy callers).
 */
export function formatApiFailureMessage(
  problem: ApiProblemDetails | null,
  httpStatus: number,
  httpStatusText: string,
  bodyText: string,
): string {
  const statusLine = `${httpStatus} ${httpStatusText}`.trim();

  if (problem !== null) {
    const title = problem.title?.trim() ?? "";
    const detail = problem.detail?.trim() ?? "";

    if (title.length > 0 && detail.length > 0) {
      return `${title}: ${detail}`;
    }

    if (detail.length > 0) {
      return detail;
    }

    if (title.length > 0) {
      return title;
    }
  }

  const trimmedBody = bodyText.trim();

  if (trimmedBody.length > 0) {
    const contentLooksJson =
      trimmedBody.startsWith("{") &&
      (() => {
        try {
          const parsed: unknown = JSON.parse(trimmedBody) as unknown;

          if (parsed === null || typeof parsed !== "object" || Array.isArray(parsed)) {
            return false;
          }

          const record = parsed as Record<string, unknown>;
          const error = record.error;

          return typeof error === "string" && error.trim().length > 0;
        } catch {
          return false;
        }
      })();

    if (contentLooksJson) {
      try {
        const parsed: unknown = JSON.parse(trimmedBody) as unknown;

        if (parsed !== null && typeof parsed === "object" && !Array.isArray(parsed)) {
          const record = parsed as Record<string, unknown>;
          const error = record.error;

          if (typeof error === "string") {
            const e = error.trim();

            if (e.length > 0) {
              return e;
            }
          }
        }
      } catch {
        /* fall through */
      }
    }
  }

  return `Request failed (${statusLine})`;
}

function tryReadJsonCorrelationId(bodyText: string): string | null {
  const trimmed = bodyText.trim();

  if (!trimmed.startsWith("{")) {
    return null;
  }

  try {
    const parsed: unknown = JSON.parse(trimmed) as unknown;

    if (parsed === null || typeof parsed !== "object" || Array.isArray(parsed)) {
      return null;
    }

    const record = parsed as Record<string, unknown>;
    const raw = record.correlationId;

    if (typeof raw !== "string") {
      return null;
    }

    const id = raw.trim();

    return id.length > 0 ? id : null;
  } catch {
    return null;
  }
}

export function buildApiRequestErrorFromParts(
  response: Response,
  bodyText: string,
): ApiRequestError {
  const contentType = response.headers.get("content-type");
  const problem = tryParseApiProblemDetails(bodyText, contentType);
  const correlationId =
    response.headers.get(CORRELATION_ID_HEADER)?.trim() ||
    problem?.correlationId?.trim() ||
    tryReadJsonCorrelationId(bodyText) ||
    null;
  const message = formatApiFailureMessage(
    problem,
    response.status,
    response.statusText,
    bodyText,
  );

  return new ApiRequestError(message, {
    problem,
    correlationId,
    httpStatus: response.status,
  });
}

/**
 * Maps failed HTTP responses to a single operator-facing string.
 * Prefers ASP.NET Core ProblemDetails (`title` / `detail`) when JSON is returned.
 */
export async function readApiFailureMessage(response: Response): Promise<string> {
  let text: string;

  try {
    text = await response.text();
  } catch {
    return `Request failed (${response.status} ${response.statusText})`;
  }

  const err = buildApiRequestErrorFromParts(response, text);

  return err.message;
}
