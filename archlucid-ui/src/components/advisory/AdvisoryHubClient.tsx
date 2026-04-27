"use client";

import { useCallback, useEffect, useState } from "react";
import { usePathname, useRouter } from "next/navigation";

import { useEnterpriseMutationCapability } from "@/hooks/use-enterprise-mutation-capability";
import { ADVISORY_HUB_TAB_IDS, advisoryHubTabFromSearchParam, type AdvisoryHubTabId } from "@/lib/advisory-hub-tab";
import { cn } from "@/lib/utils";

import { AdvisoryScansContent } from "./AdvisoryScansContent";
import { AdvisorySchedulesContent } from "./AdvisorySchedulesContent";

const TAB_PARAM = "tab";

const TAB_LABEL: Record<AdvisoryHubTabId, string> = {
  scans: "Scans",
  schedules: "Schedules",
};

const SCHEDULES_TAB_READER_TITLE =
  "View schedules and executions; creating schedules and run-now require operator (Execute) access on the API.";

export type AdvisoryHubClientProps = {
  readonly initialTab: AdvisoryHubTabId;
};

/**
 * Single `/advisory` surface: improvement scans and advisory scan schedules. Tab state in `?tab=` for deep links.
 * `initialTab` comes from the server so this tree does not depend on `useSearchParams` (avoids long Suspense fallbacks).
 */
export function AdvisoryHubClient({ initialTab }: AdvisoryHubClientProps) {
  const router: ReturnType<typeof useRouter> = useRouter();
  const pathname: string = usePathname();
  const canMutate: boolean = useEnterpriseMutationCapability();
  const [activeTab, setActiveTab] = useState<AdvisoryHubTabId>(initialTab);

  useEffect(() => {
    setActiveTab(initialTab);
  }, [initialTab]);

  useEffect(() => {
    const onPop = () => {
      const sp = new URLSearchParams(window.location.search);
      setActiveTab(advisoryHubTabFromSearchParam(sp.get(TAB_PARAM)));
    };

    window.addEventListener("popstate", onPop);

    return () => {
      window.removeEventListener("popstate", onPop);
    };
  }, []);

  const onSelectTab = useCallback(
    (id: AdvisoryHubTabId) => {
      setActiveTab(id);

      if (id === "scans") {
        router.push(pathname);

        return;
      }

      router.push(`${pathname}?${TAB_PARAM}=${encodeURIComponent(id)}`);
    },
    [pathname, router],
  );

  return (
    <div className="px-0" data-testid="advisory-hub">
      <nav
        className="mb-6 border-b border-neutral-200 dark:border-neutral-800"
        aria-label="Advisory hub sections"
      >
        <div className="-mb-px flex flex-wrap gap-1" role="tablist">
          {ADVISORY_HUB_TAB_IDS.map((id) => {
            const selected: boolean = activeTab === id;
            const softMuted: boolean = !canMutate && id === "schedules";
            const tabTitle: string | undefined =
              !canMutate && id === "schedules" ? SCHEDULES_TAB_READER_TITLE : undefined;

            return (
              <button
                key={id}
                type="button"
                role="tab"
                id={`advisory-hub-tab-${id}`}
                aria-selected={selected}
                data-testid={`advisory-hub-tab-${id}`}
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
        aria-labelledby={`advisory-hub-tab-${activeTab}`}
        data-testid="advisory-hub-panel"
      >
        {activeTab === "scans" ? <AdvisoryScansContent /> : null}
        {activeTab === "schedules" ? <AdvisorySchedulesContent /> : null}
      </div>
    </div>
  );
}
