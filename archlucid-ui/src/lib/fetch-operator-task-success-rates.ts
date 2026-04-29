import { mergeRegistrationScopeForProxy } from "@/lib/proxy-fetch-registration-scope";

/** Process-lifetime snapshot from GET /v1/diagnostics/operator-task-success-rates (resets on API host restart). */
export type OperatorTaskSuccessRates = {
  windowNote: string;
  firstRunCommittedTotal: number;
  firstSessionCompletedTotal: number;
  firstRunCommittedPerSessionRatio: number;
};

/** Fetches onboarding funnel counters for operator UI tiles and Core Pilot diagnostics checklist. */
export async function fetchOperatorTaskSuccessRates(): Promise<OperatorTaskSuccessRates> {
  const res = await fetch(
    "/api/proxy/v1/diagnostics/operator-task-success-rates",
    mergeRegistrationScopeForProxy({ headers: { Accept: "application/json" } }),
  );

  if (!res.ok) {
    throw new Error(`operator-task-success-rates: ${String(res.status)}`);
  }

  return (await res.json()) as OperatorTaskSuccessRates;
}
