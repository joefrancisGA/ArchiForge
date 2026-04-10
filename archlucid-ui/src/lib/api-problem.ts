/**
 * Subset of RFC 7807 Problem Details plus ArchLucid API extensions (`errorCode`, `supportHint`).
 * ASP.NET Core typically serializes `ProblemDetails.Extensions` as extra root JSON properties (camelCase).
 */
export type ApiProblemDetails = {
  type?: string;
  title?: string;
  detail?: string;
  status?: number;
  instance?: string;
  errorCode?: string;
  supportHint?: string;
  /** Echoes API **X-Correlation-ID** / proxy **correlationId** when present in JSON (RFC 7807 extension promoted to root). */
  correlationId?: string;
};

function readTrimmedString(obj: Record<string, unknown>, key: string): string | undefined {
  const value = obj[key];

  if (typeof value !== "string") {
    return undefined;
  }

  const trimmed = value.trim();

  return trimmed.length > 0 ? trimmed : undefined;
}

function readExtensions(obj: Record<string, unknown>): {
  errorCode?: string;
  supportHint?: string;
  correlationId?: string;
} {
  const extensions = obj.extensions;

  if (extensions === null || extensions === undefined || typeof extensions !== "object") {
    return {};
  }

  if (Array.isArray(extensions)) {
    return {};
  }

  const ext = extensions as Record<string, unknown>;

  return {
    errorCode: readTrimmedString(ext, "errorCode"),
    supportHint: readTrimmedString(ext, "supportHint"),
    correlationId: readTrimmedString(ext, "correlationId"),
  };
}

function readOptionalNumber(obj: Record<string, unknown>, key: string): number | undefined {
  const value = obj[key];

  if (typeof value === "number" && Number.isFinite(value)) {
    return value;
  }

  return undefined;
}

/**
 * Parses a response body as Problem Details when JSON shape matches; otherwise returns null.
 */
export function tryParseApiProblemDetails(text: string, contentType: string | null): ApiProblemDetails | null {
  const trimmed = text.trim();

  if (trimmed.length === 0) {
    return null;
  }

  const ct = contentType ?? "";

  const looksJson =
    ct.includes("application/json") ||
    ct.includes("application/problem+json") ||
    (ct.length === 0 && (trimmed.startsWith("{") || trimmed.startsWith("[")));

  if (!looksJson) {
    return null;
  }

  let body: unknown;

  try {
    body = JSON.parse(trimmed) as unknown;
  } catch {
    return null;
  }

  if (body === null || typeof body !== "object" || Array.isArray(body)) {
    return null;
  }

  const record = body as Record<string, unknown>;
  const fromExt = readExtensions(record);

  const title = readTrimmedString(record, "title");
  const detail = readTrimmedString(record, "detail");
  const type = readTrimmedString(record, "type");
  const instance = readTrimmedString(record, "instance");
  const errorCode = readTrimmedString(record, "errorCode") ?? fromExt.errorCode;
  const supportHint = readTrimmedString(record, "supportHint") ?? fromExt.supportHint;
  const correlationId =
    readTrimmedString(record, "correlationId") ?? fromExt.correlationId;
  const status = readOptionalNumber(record, "status");

  if (!title && !detail && !type && !errorCode) {
    return null;
  }

  const problem: ApiProblemDetails = {};

  if (type) {
    problem.type = type;
  }

  if (title) {
    problem.title = title;
  }

  if (detail) {
    problem.detail = detail;
  }

  if (status !== undefined) {
    problem.status = status;
  }

  if (instance) {
    problem.instance = instance;
  }

  if (errorCode) {
    problem.errorCode = errorCode;
  }

  if (supportHint) {
    problem.supportHint = supportHint;
  }

  if (correlationId) {
    problem.correlationId = correlationId;
  }

  return problem;
}
