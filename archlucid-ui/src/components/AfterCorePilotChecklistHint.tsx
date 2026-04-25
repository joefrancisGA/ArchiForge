"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";

import {
  CORE_PILOT_CHECKLIST_CHANGED_EVENT,
  readCorePilotChecklistAllDone,
} from "@/lib/core-pilot-checklist-storage";

/**
 * On Home, after the operator marks all Core Pilot checklist steps done, nudge
 * toward Advanced Analysis without showing this before the wedge is complete. Enterprise stays demoted—sidebar only,
 * not a next mandatory step.
 */
export function AfterCorePilotChecklistHint() {
  const [allDone, setAllDone] = useState(false);

  const refresh = useCallback(() => {
    setAllDone(readCorePilotChecklistAllDone());
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

  if (!allDone) {
    return null;
  }

  return (
    <section
      className="mb-5 max-w-3xl rounded-md border border-teal-200 bg-teal-50/60 px-3 py-2.5 dark:border-teal-900 dark:bg-teal-950/30"
      aria-labelledby="after-core-pilot-heading"
    >
      <h3 id="after-core-pilot-heading" className="m-0 text-sm font-semibold text-teal-950 dark:text-teal-100">
        Core Pilot checklist complete
      </h3>
      <p className="m-0 mt-1 text-sm text-neutral-800 dark:text-neutral-200">
        When you have a real question that run detail cannot answer—<strong>what changed between two runs</strong>,{" "}
        <strong>whether the provenance chain still validates</strong>, or a <strong>visual graph</strong>—open{" "}
        <Link href="/compare" className="font-medium text-teal-900 underline dark:text-teal-200">
          Compare
        </Link>
        ,{" "}
        <Link href="/replay" className="font-medium text-teal-900 underline dark:text-teal-200">
          Replay
        </Link>
        , or{" "}
        <Link href="/graph" className="font-medium text-teal-900 underline dark:text-teal-200">
          Graph
        </Link>
        . <strong>Enterprise Controls</strong> (governance, audit, alerts) stay in the sidebar until sponsors or policy
        need them—not part of first-pilot success criteria.
      </p>
    </section>
  );
}
