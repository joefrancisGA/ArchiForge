/**
 * Query-string tab ids for the `/digests` hub (`?tab=`). **browse** is the default when the param is absent, empty, or unknown.
 */
export const DIGESTS_HUB_TAB_IDS = ["browse", "subscriptions", "schedule"] as const;
export type DigestsHubTabId = (typeof DIGESTS_HUB_TAB_IDS)[number];

const TAB_SET = new Set<string>(DIGESTS_HUB_TAB_IDS);

/**
 * Resolves the active digest hub tab from `?tab=`; unknown values fall back to **browse**.
 */
export function digestsHubTabFromSearchParam(param: string | null): DigestsHubTabId {
  if (param === null || param === "" || param === "browse") {
    return "browse";
  }

  if (TAB_SET.has(param)) {
    return param as DigestsHubTabId;
  }

  return "browse";
}
