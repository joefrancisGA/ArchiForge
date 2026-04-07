/**
 * Same-origin `/api/proxy` URLs for 60R simulation report downloads (read authority).
 */

export function buildEvolutionSimulationReportFileUrl(
  candidateId: string,
  format: "markdown" | "json",
): string {
  const id = candidateId.trim();
  const params = new URLSearchParams();
  params.set("format", format);

  return `/api/proxy/v1/evolution/results/${encodeURIComponent(id)}/export?${params.toString()}`;
}
