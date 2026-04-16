import { CORRELATION_ID_HEADER, generateCorrelationId } from "@/lib/correlation";
import { isApiRequestError } from "@/lib/api-request-error";

const maxReportsPerMinute = 5;

let reportWindowStartMs = 0;

let reportCountInWindow = 0;

function resetWindowIfNeeded(nowMs: number): void {
  if (nowMs - reportWindowStartMs > 60_000)
  {
    reportWindowStartMs = nowMs;
    reportCountInWindow = 0;
  }
}

function shouldSkipTelemetry(): boolean {
  return process.env.NODE_ENV === "development";
}

/**
 * Fire-and-forget POST of a client-side error to the API diagnostics sink.
 * No-ops in development and when rate-limited; never throws.
 */
export function reportClientError(error: Error, context?: Record<string, string>): void {
  if (shouldSkipTelemetry())
  {
    return;
  }

  const now = Date.now();
  resetWindowIfNeeded(now);

  if (reportCountInWindow >= maxReportsPerMinute)
  {
    return;
  }

  reportCountInWindow++;

  const stack = error.stack ?? "";
  const truncatedStack = stack.length > 2000 ? stack.slice(0, 2000) : stack;
  const pathname = typeof window !== "undefined" ? window.location.pathname : "";
  const userAgent = typeof navigator !== "undefined" ? navigator.userAgent : "";

  const body = {
    message: error.message.slice(0, 500),
    stack: truncatedStack,
    pathname: pathname.slice(0, 200),
    userAgent: userAgent.slice(0, 500),
    timestampUtc: new Date().toISOString(),
    context: context ?? undefined,
  };

  void fetch("/api/proxy/v1/diagnostics/client-error", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Accept: "application/json",
      [CORRELATION_ID_HEADER]: generateCorrelationId(),
    },
    body: JSON.stringify(body),
    keepalive: true,
  }).catch(() => {
    /* intentional: telemetry must not surface secondary failures */
  });
}

/** When an API call failed with 5xx, optionally notify the diagnostics sink (production only). */
export function maybeReportApiServerErrorFromUnknown(error: unknown): void {
  if (shouldSkipTelemetry())
  {
    return;
  }

  if (!isApiRequestError(error))
  {
    return;
  }

  if (error.httpStatus < 500)
  {
    return;
  }

  const wrapped = new Error(`API ${error.httpStatus}: ${error.message}`);
  wrapped.stack = error.stack ?? undefined;
  reportClientError(wrapped, { source: "api-load", correlationId: error.correlationId ?? "" });
}
