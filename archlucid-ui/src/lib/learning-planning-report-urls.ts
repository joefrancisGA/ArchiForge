/**
 * Pure URL builders for 59R planning exports (same-origin `/api/proxy`, scoped like other v1 learning calls).
 */

export function buildLearningPlanningReportFileUrl(format: "markdown" | "json"): string {
  const params = new URLSearchParams();
  params.set("format", format);

  return `/api/proxy/v1/learning/report/file?${params.toString()}`;
}

export function buildLearningPlanningReportJsonUrl(): string {
  const params = new URLSearchParams();
  params.set("format", "json");

  return `/api/proxy/v1/learning/report?${params.toString()}`;
}
