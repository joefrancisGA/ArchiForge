/**
 * Query-string tab ids for the `/advisory` hub (`?tab=`). **scans** is the default when the param is absent or unknown.
 */
export const ADVISORY_HUB_TAB_IDS = ["scans", "schedules"] as const;
export type AdvisoryHubTabId = (typeof ADVISORY_HUB_TAB_IDS)[number];

const TAB_SET = new Set<string>(ADVISORY_HUB_TAB_IDS);

/**
 * Resolves the active advisory hub tab from `?tab=`; unknown values fall back to **scans**.
 */
export function advisoryHubTabFromSearchParam(param: string | null): AdvisoryHubTabId {
  if (param === null || param === "" || param === "scans") {
    return "scans";
  }

  if (TAB_SET.has(param)) {
    return param as AdvisoryHubTabId;
  }

  return "scans";
}
