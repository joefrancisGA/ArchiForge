/**
 * Build-time flags for optional operator UI (must be NEXT_PUBLIC_* for client components).
 */
export function isExperimentalAdvisoryPanelsEnabled(): boolean {
  return process.env.NEXT_PUBLIC_EXPERIMENTAL_ADVISORY_PANELS === "true";
}
