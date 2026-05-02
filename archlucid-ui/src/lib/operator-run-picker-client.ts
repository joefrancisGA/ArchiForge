import { listRunsByProjectPaged } from "@/lib/api";
import { normalizeRunSummaryForDemoPicker } from "@/lib/demo-run-canonical";
import { tryStaticDemoCompareRunSummaries, tryStaticDemoRunSummariesPaged } from "@/lib/operator-static-demo";
import type { RunSummary } from "@/types/authority";

export type LoadProjectRunsOptions = {
  /**
   * When the live list is empty (or the request failed), prefer the two-row Compare demo pair instead of the single
   * showcase run — keeps baseline/updated pickers populated in demo builds.
   */
  readonly forCompare?: boolean;
};

/**
 * Loads recent runs from the API, then merges curated demo rows when enabled and the live response is unusable.
 * Matches the server-side spine used on `/runs` so Ask, Compare, and Graph stay consistent in demo deploys.
 */
export async function loadProjectRunsMergedWithDemoFallback(
  projectId: string,
  options?: LoadProjectRunsOptions,
): Promise<{ items: RunSummary[]; loadError: boolean }> {
  let loadError = false;

  try {
    const page = await listRunsByProjectPaged(projectId, 1, 50);
    const items = page.items ?? [];

    if (items.length > 0) {
      return { items: items.map(normalizeRunSummaryForDemoPicker), loadError: false };
    }

    if (options?.forCompare ?? false) {
      const compareEmptyDemo = tryStaticDemoCompareRunSummaries(projectId);

      if (compareEmptyDemo !== null && compareEmptyDemo.items.length > 0) {
        return { items: compareEmptyDemo.items.map(normalizeRunSummaryForDemoPicker), loadError: false };
      }
    }

    const emptyListDemo = tryStaticDemoRunSummariesPaged(projectId, { afterEmptyLiveList: true });

    if (emptyListDemo !== null && emptyListDemo.items.length > 0) {
      return { items: emptyListDemo.items.map(normalizeRunSummaryForDemoPicker), loadError: false };
    }

    return { items: [], loadError: false };
  } catch {
    loadError = true;
  }

  if (options?.forCompare ?? false) {
    const compareDemo = tryStaticDemoCompareRunSummaries(projectId, { afterAuthorityListFailure: loadError });

    if (compareDemo !== null) {
      return { items: compareDemo.items.map(normalizeRunSummaryForDemoPicker), loadError: false };
    }
  }

  const fallback = tryStaticDemoRunSummariesPaged(projectId, { afterAuthorityListFailure: loadError });

  if (fallback !== null) {
    return { items: fallback.items.map(normalizeRunSummaryForDemoPicker), loadError: false };
  }

  return { items: [], loadError };
}
