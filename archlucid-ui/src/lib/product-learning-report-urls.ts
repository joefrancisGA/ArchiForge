/**
 * Pure URL builders for 58R triage exports (same-origin `/api/proxy` + selected `since`).
 * Keeps query strings testable without mounting the dashboard page.
 */

export function buildProductLearningReportFileUrl(format: "markdown" | "json", since: string | null): string {
  const params = new URLSearchParams();
  params.set("format", format);

  if (since) {
    params.set("since", since);
  }

  return `/api/proxy/v1/product-learning/report/file?${params.toString()}`;
}

export function buildProductLearningReportJsonUrl(since: string | null): string {
  const params = new URLSearchParams();
  params.set("format", "json");

  if (since) {
    params.set("since", since);
  }

  return `/api/proxy/v1/product-learning/report?${params.toString()}`;
}
