import { mergeRegistrationScopeForProxy } from "@/lib/proxy-fetch-registration-scope";
import type { PilotValueReportJson } from "@/types/pilot-value-report";

export function buildPilotValueReportQuery(fromIso: string | null, toIso: string): string {
  const params = new URLSearchParams();

  if (fromIso !== null && fromIso.length > 0) {
    params.set("fromUtc", fromIso);
  }

  params.set("toUtc", toIso);

  return params.toString();
}

export async function fetchPilotValueReportJson(fromIso: string | null, toIso: string): Promise<PilotValueReportJson> {
  const q = buildPilotValueReportQuery(fromIso, toIso);
  const res = await fetch(
    `/api/proxy/v1/tenant/pilot-value-report?${q}`,
    mergeRegistrationScopeForProxy({ headers: { Accept: "application/json" } }),
  );

  if (!res.ok) {
    throw new Error(`HTTP ${res.status}`);
  }

  return (await res.json()) as PilotValueReportJson;
}
