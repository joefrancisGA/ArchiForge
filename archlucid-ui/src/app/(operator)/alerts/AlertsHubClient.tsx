"use client";

import { useCallback, useMemo } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";

import { GlossaryTooltip } from "@/components/GlossaryTooltip";
import { AlertRoutingContent } from "@/components/alerts/AlertRoutingContent";
import { AlertRulesContent } from "@/components/alerts/AlertRulesContent";
import { AlertSimulationTuningSection } from "@/components/alerts/AlertSimulationTuningSection";
import { AlertsInboxContent } from "@/components/alerts/AlertsInboxContent";
import { CompositeAlertRulesContent } from "@/components/alerts/CompositeAlertRulesContent";
import {
  ALERT_HUB_TAB_IDS,
  alertHubTabFromSearchParam,
  type AlertHubTabId,
} from "@/lib/alerts-hub-tab";
import { cn } from "@/lib/utils";

const TAB_PARAM = "tab";

const TAB_LABEL: Record<AlertHubTabId, string> = {
  inbox: "Inbox",
  rules: "Rules",
  routing: "Routing",
  composite: "Composite",
  simulation: "Simulation & Tuning",
};

/**
 * Single `/alerts` surface: inbox, rules, routing, composite, and merged simulation + tuning.
 * Tab state is in the query string for deep links and browser history.
 */
export function AlertsHubClient() {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const rawTab = searchParams.get(TAB_PARAM);

  const activeTab: AlertHubTabId = useMemo(
    () => alertHubTabFromSearchParam(rawTab),
    [rawTab],
  );

  const onSelectTab = useCallback(
    (id: AlertHubTabId) => {
      if (id === "inbox") {
        router.push(pathname);
        return;
      }

      router.push(`${pathname}?${TAB_PARAM}=${encodeURIComponent(id)}`);
    },
    [pathname, router],
  );

  return (
    <div className="px-0">
      <nav
        className="mb-6 border-b border-neutral-200 dark:border-neutral-800"
        aria-label="Alert hub sections"
      >
        <div className="-mb-px flex flex-wrap gap-1" role="tablist">
          {ALERT_HUB_TAB_IDS.map((id) => {
            const selected = activeTab === id;

            return (
              <button
                key={id}
                type="button"
                role="tab"
                aria-selected={selected}
                id={`alert-hub-tab-${id}`}
                data-testid={`alert-hub-tab-${id}`}
                onClick={() => onSelectTab(id)}
                className={cn(
                  "rounded-t-md border border-b-0 px-3 py-2 text-sm font-medium",
                  selected
                    ? "border-neutral-200 bg-white text-neutral-900 dark:border-neutral-700 dark:bg-neutral-950 dark:text-neutral-50"
                    : "border-transparent bg-transparent text-neutral-600 hover:bg-neutral-100 dark:text-neutral-400 dark:hover:bg-neutral-900",
                )}
              >
                {TAB_LABEL[id]}
              </button>
            );
          })}
        </div>
      </nav>

      <p className="mb-4 max-w-prose text-sm text-neutral-600 dark:text-neutral-400">
        Inbox and rules evaluate signals against{" "}
        <GlossaryTooltip termKey="effective_governance">effective governance</GlossaryTooltip> and persisted findings —
        escalation paths are tenant-specific.
      </p>

      <div
        className="min-w-0"
        role="tabpanel"
        aria-labelledby={`alert-hub-tab-${activeTab}`}
        data-testid="alert-hub-panel"
      >
        {activeTab === "inbox" ? <AlertsInboxContent /> : null}
        {activeTab === "rules" ? <AlertRulesContent /> : null}
        {activeTab === "routing" ? <AlertRoutingContent /> : null}
        {activeTab === "composite" ? <CompositeAlertRulesContent /> : null}
        {activeTab === "simulation" ? <AlertSimulationTuningSection /> : null}
      </div>
    </div>
  );
}
