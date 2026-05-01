"use client";

import { ChevronDown } from "lucide-react";
import Link from "next/link";
import { useCallback, useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible";
import {
  AFTER_CORE_PILOT_WHATS_NEXT_DISMISSED_KEY,
  CORE_PILOT_CHECKLIST_CHANGED_EVENT,
  readAfterCorePilotWhatsNextDismissed,
  readCorePilotChecklistAllDone,
} from "@/lib/core-pilot-checklist-storage";
import { NAV_DISCLOSURE } from "@/lib/nav-disclosure-copy";

type Suggestion = {
  title: string;
  href: string;
  description: string;
  sidebarNote: string;
};

const suggestions: Suggestion[] = [
  {
    title: "Compare two runs",
    href: "/compare",
    description: "Structured manifest diff between a base run and a target run when you need to know what changed.",
    sidebarNote: `Requires “${NAV_DISCLOSURE.extended.show}” in the sidebar (extended analysis links).`,
  },
  {
    title: "Explore the architecture graph",
    href: "/graph",
    description: "Provenance or architecture graph for a review ID when a list view is not enough.",
    sidebarNote: `Requires “${NAV_DISCLOSURE.extended.show}” in the sidebar.`,
  },
  {
    title: "Set up governance alerts",
    href: "/alerts?tab=rules",
    description: "Inbox, routing, and rules on one hub—tune when architecture-risk signals need action.",
    sidebarNote:
      "Alerts is under Governance (sidebar). Open Alerts, then use the Rules tab for configuration.",
  },
  {
    title: "Review policy packs",
    href: "/policy-packs",
    description: "Versions, effective content, and how governance rules attach to your scope.",
    sidebarNote: `Use “${NAV_DISCLOSURE.extended.show}” and, for the full Enterprise slice, “${NAV_DISCLOSURE.advanced.show}”.`,
  },
];

/**
 * After the Core Pilot checklist is complete, optional “what’s next” suggestions (not a second checklist)
 * with dismissal persisted in localStorage. Does not change sidebar toggles—only explains them.
 */
export function AfterCorePilotChecklistHint() {
  const [allDone, setAllDone] = useState(false);
  const [dismissed, setDismissed] = useState(false);

  const refresh = useCallback(() => {
    setAllDone(readCorePilotChecklistAllDone());
    setDismissed(readAfterCorePilotWhatsNextDismissed());
  }, []);

  useEffect(() => {
    refresh();

    function onChanged() {
      refresh();
    }

    window.addEventListener(CORE_PILOT_CHECKLIST_CHANGED_EVENT, onChanged);

    return () => {
      window.removeEventListener(CORE_PILOT_CHECKLIST_CHANGED_EVENT, onChanged);
    };
  }, [refresh]);

  const onDismiss = useCallback(() => {
    try {
      window.localStorage.setItem(AFTER_CORE_PILOT_WHATS_NEXT_DISMISSED_KEY, "1");
    } catch {
      /* private mode */
    }
    setDismissed(true);
  }, []);

  if (!allDone || dismissed) {
    return null;
  }

  return (
    <section
      className="mb-5 max-w-3xl"
      aria-labelledby="after-core-pilot-card-title"
      data-testid="after-core-pilot-whats-next"
    >
      <Card className="border border-teal-200 bg-teal-50/60 dark:border-teal-900 dark:bg-teal-950/30">
        <CardHeader className="space-y-1 sm:flex sm:flex-row sm:items-start sm:justify-between sm:space-y-0">
          <div>
            <h3 id="after-core-pilot-card-title" className="m-0 text-base font-semibold tracking-tight text-teal-950 dark:text-teal-100">
              Ready for more?
            </h3>
            <p className="m-0 mt-0.5 text-xs text-teal-800/90 dark:text-teal-200/90">Expand your pilot — optional next steps</p>
          </div>
          <Button
            type="button"
            variant="outline"
            size="sm"
            className="shrink-0 border-teal-300 text-teal-900 hover:bg-teal-100 dark:border-teal-700 dark:text-teal-100 dark:hover:bg-teal-900/50"
            data-testid="after-core-pilot-whats-next-dismiss"
            onClick={onDismiss}
          >
            Dismiss
          </Button>
        </CardHeader>
        <CardContent className="space-y-4">
          <p className="m-0 text-sm text-neutral-800 dark:text-neutral-200" data-testid="after-core-pilot-intro">
            When you have a real question that run detail cannot answer—<strong>what changed between two runs</strong>,{" "}
            <strong>whether the provenance chain still validates</strong>, or a <strong>visual graph</strong>—the links
            below point to deeper analysis. <strong>Enterprise Controls</strong> (governance, audit, alerts) stay in
            the sidebar until sponsors or policy need them—not part of first-pilot success criteria.
          </p>

          <Collapsible defaultOpen className="rounded-md border border-teal-200/80 bg-white/70 dark:border-teal-900/60 dark:bg-teal-950/30">
            <CollapsibleTrigger
              className="auth-panel-focus flex w-full items-center justify-between gap-2 px-3 py-2.5 text-left text-sm font-semibold text-teal-950 dark:text-teal-100 [&[data-state=open]_svg]:rotate-180"
              data-testid="after-core-pilot-whats-next-collapsible-trigger"
            >
              Suggested next steps
              <ChevronDown className="size-4 shrink-0 transition-transform" aria-hidden />
            </CollapsibleTrigger>
            <CollapsibleContent>
              <ul className="m-0 list-none space-y-3 border-t border-teal-200/60 px-3 py-3 dark:border-teal-800/50">
                {suggestions.map((s, index) => {
                  return (
                    <li key={s.href} className="text-sm text-neutral-800 dark:text-neutral-200">
                      <div className="font-medium text-teal-900 dark:text-teal-200">
                        <Link href={s.href} className="underline decoration-teal-600/50 underline-offset-2 hover:decoration-teal-800 dark:decoration-teal-500/50 dark:hover:text-teal-100">
                          {s.title}
                        </Link>
                      </div>
                      <p className="m-0 mt-0.5 text-neutral-700 dark:text-neutral-300">{s.description}</p>
                      <p
                        className="m-0 mt-1.5 text-xs text-neutral-500 dark:text-neutral-400"
                        data-testid={`after-core-pilot-sidebar-note-${index}`}
                      >
                        <span className="font-medium text-neutral-600 dark:text-neutral-500">Sidebar: </span>
                        {s.sidebarNote}
                      </p>
                    </li>
                  );
                })}
              </ul>
            </CollapsibleContent>
          </Collapsible>
        </CardContent>
      </Card>
    </section>
  );
}
