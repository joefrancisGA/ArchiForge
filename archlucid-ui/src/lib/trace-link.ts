/**
 * Builds a trace viewer URL from the configured template and a trace ID.
 * Returns null if no template is configured or traceId is empty.
 */
export function buildTraceViewerUrl(traceId: string | null | undefined): string | null {
  const template = process.env.NEXT_PUBLIC_TRACE_VIEWER_URL_TEMPLATE;

  if (!template || !traceId) {
    return null;
  }

  const encoded = encodeURIComponent(traceId);

  return template.split("{traceId}").join(encoded);
}
