"use client";

import { useCallback, useMemo } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";

import { useEnterpriseMutationCapability } from "@/hooks/use-enterprise-mutation-capability";
import { DIGESTS_HUB_TAB_IDS, digestsHubTabFromSearchParam, type DigestsHubTabId } from "@/lib/digests-hub-tab";
import { cn } from "@/lib/utils";

import { DigestsBrowseContent } from "./DigestsBrowseContent";
import { DigestSubscriptionsContent } from "./DigestSubscriptionsContent";
import { ExecDigestScheduleContent } from "./ExecDigestScheduleContent";

const TAB_PARAM = "tab";

const TAB_LABEL: Record<DigestsHubTabId, string> = {
  browse: "Browse",
  subscriptions: "Subscriptions",
  schedule: "Schedule",
};

const SUBSCRIPTIONS_TAB_READER_TITLE =
  "List is readable at Read rank; creating or changing subscriptions requires operator (Execute) access.";
const SCHEDULE_TAB_READER_TITLE =
  "Preferences are readable; saving changes requires operator (Execute) access.";

/**
 * Single `/digests` surface: browse, subscriptions, and executive digest schedule. Tab state in `?tab=` for deep links.
 */
export function DigestsHubClient() {
  const router: ReturnType<typeof useRouter> = useRouter();
  const pathname: string = usePathname();
  const searchParams = useSearchParams();
  const canMutate: boolean = useEnterpriseMutationCapability();
  const rawTab: string | null = searchParams.get(TAB_PARAM);

  const activeTab: DigestsHubTabId = useMemo(
    () => digestsHubTabFromSearchParam(rawTab),
    [rawTab],
  );

  const onSelectTab = useCallback(
    (id: DigestsHubTabId) => {
      if (id === "browse") {
        router.push(pathname);
        return;
      }

      router.push(`${pathname}?${TAB_PARAM}=${encodeURIComponent(id)}`);
    },
    [pathname, router],
  );

  return (
    <div className="px-0" data-testid="digests-hub">
      <nav
        className="mb-6 border-b border-neutral-200 dark:border-neutral-800"
        aria-label="Digest hub sections"
      >
        <div className="-mb-px flex flex-wrap gap-1" role="tablist">
          {DIGESTS_HUB_TAB_IDS.map((id) => {
            const selected: boolean = activeTab === id;
            const softMuted: boolean =
              !canMutate && (id === "subscriptions" || id === "schedule");
            const tabTitle: string | undefined =
              !canMutate && id === "subscriptions"
                ? SUBSCRIPTIONS_TAB_READER_TITLE
                : !canMutate && id === "schedule"
                  ? SCHEDULE_TAB_READER_TITLE
                  : undefined;

            return (
              <button
                key={id}
                type="button"
                role="tab"
                id={`digests-hub-tab-${id}`}
                aria-selected={selected}
                data-testid={`digests-hub-tab-${id}`}
                title={tabTitle}
                onClick={() => onSelectTab(id)}
                className={cn(
                  "rounded-t-md border border-b-0 px-3 py-2 text-sm font-medium",
                  selected
                    ? "border-neutral-200 bg-white text-neutral-900 dark:border-neutral-700 dark:bg-neutral-950 dark:text-neutral-50"
                    : "border-transparent bg-transparent text-neutral-600 hover:bg-neutral-100 dark:text-neutral-400 dark:hover:bg-neutral-900",
                  softMuted && !selected && "opacity-70",
                )}
              >
                {TAB_LABEL[id]}
              </button>
            );
          })}
        </div>
      </nav>

      <div
        className="min-w-0"
        role="tabpanel"
        aria-labelledby={`digests-hub-tab-${activeTab}`}
        data-testid="digests-hub-panel"
      >
        {activeTab === "browse" ? <DigestsBrowseContent /> : null}
        {activeTab === "subscriptions" ? <DigestSubscriptionsContent /> : null}
        {activeTab === "schedule" ? <ExecDigestScheduleContent /> : null}
      </div>
    </div>
  );
}
