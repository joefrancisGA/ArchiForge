/**
 * Query-string tab ids for the `/alerts` hub (`?tab=`). **inbox** is the default when the param is absent or unknown.
 */
export const ALERT_HUB_TAB_IDS = ["inbox", "rules", "routing", "composite", "simulation"] as const;
export type AlertHubTabId = (typeof ALERT_HUB_TAB_IDS)[number];

const TAB_SET = new Set<string>(ALERT_HUB_TAB_IDS);

/**
 * Resolves the active hub tab from `?tab=`; unknown values fall back to **inbox**.
 */
export function alertHubTabFromSearchParam(param: string | null): AlertHubTabId {
  if (param === null || param === "") {
    return "inbox";
  }

  if (TAB_SET.has(param)) {
    return param as AlertHubTabId;
  }

  return "inbox";
}
