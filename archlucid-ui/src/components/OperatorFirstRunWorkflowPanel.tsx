"use client";

import Link from "next/link";

import { Button } from "@/components/ui/button";
import type { ReactNode } from "react";
import { useCallback, useEffect, useMemo, useState } from "react";

import { corePilotStepDoneStorageKey, emitCorePilotChecklistChanged } from "@/lib/core-pilot-checklist-storage";
import { cn } from "@/lib/utils";

const minimizedStorageKey = "archlucid_operator_workflow_guide_v1";

type WorkflowStep = {
  title: string;
  shortBody: string;
  detail?: string;
  primaryHref: string;
  primaryLabel: string;
  secondary?: ReactNode;
};

/**
 * Core Pilot path — the four steps every first pilot must complete.
 * Long copy lives in expandables so the home column stays a cockpit, not a manual.
 */
const corePilotSteps: WorkflowStep[] = [
  {
    title: "Create an architecture request",
    shortBody: "Use the guided wizard to capture system identity, requirements, and constraints.",
    detail:
      "The wizard walks you through system identity, requirements, constraints, and advanced inputs — then submits the run and tracks the pipeline in real time.",
    primaryHref: "/runs/new",
    primaryLabel: "Start new run wizard",
    secondary: (
      <>
        Or open the{" "}
        <Link className="workflow-inline-link text-teal-700 dark:text-teal-400" href="/runs?projectId=default">
          Runs list
        </Link>
        .
      </>
    ),
  },
  {
    title: "Let the pipeline run, then open the run",
    shortBody: "Watch progress in the wizard or open the run from the runs list when ready.",
    detail:
      "The coordinator fills snapshots and pipeline steps. You can use the wizard’s last step or open run detail anytime.",
    primaryHref: "/runs?projectId=default",
    primaryLabel: "Open runs list",
    secondary: (
      <>
        From the final wizard step, use <strong>Open run detail</strong> for the new run ID.
      </>
    ),
  },
  {
    title: "Finalize the reviewed manifest",
    shortBody: "On run detail, finalize when the run is ready, or use the API/CLI for automation.",
    detail:
      "Until finalization, there is no manifest link or artifact exports. See docs/OPERATOR_QUICKSTART.md in the repo for CLI/API examples.",
    primaryHref: "/runs?projectId=default",
    primaryLabel: "Choose run → open detail",
  },
  {
    title: "Inspect manifest & artifacts",
    shortBody: "After finalization, review the manifest summary, artifact table, and export links on run detail.",
    detail:
      "Open the reviewed manifest link from run detail for the full page; use artifact actions for download and review.",
    primaryHref: "/runs?projectId=default",
    primaryLabel: "Open a finalized run",
  },
];

// Alias for localStorage key stability (step indices 0–3 map to corePilotSteps).
const steps = corePilotSteps;

/**
 * Collapsible first-manifest checklist. Persists "minimized" in localStorage. Compact for a side column; step actions are
 * outline buttons so they do not compete with the main home CTAs.
 */
export function OperatorFirstRunWorkflowPanel() {
  const [hydrated, setHydrated] = useState(false);
  const [minimized, setMinimized] = useState(false);
  const [doneByIndex, setDoneByIndex] = useState<boolean[]>(() => steps.map(() => false));
  const [expandedIndex, setExpandedIndex] = useState<number | null>(null);

  useEffect(() => {
    const nextDone: boolean[] = [];

    for (let i = 0; i < steps.length; i++) {
      try {
        if (typeof window !== "undefined" && window.localStorage.getItem(corePilotStepDoneStorageKey(i)) === "1") {
          nextDone.push(true);
        } else {
          nextDone.push(false);
        }
      } catch {
        nextDone.push(false);
      }
    }

    setDoneByIndex(nextDone);

    try {
      if (typeof window !== "undefined" && window.localStorage.getItem(minimizedStorageKey) === "1") {
        setMinimized(true);
      }
    } catch {
      /* private mode */
    }

    setHydrated(true);
    emitCorePilotChecklistChanged();
  }, []);

  const doneCount = useMemo(() => doneByIndex.filter(Boolean).length, [doneByIndex]);
  const allDone = doneCount === steps.length;

  const firstUndoneIndex = useMemo(() => doneByIndex.findIndex((d) => !d), [doneByIndex]);

  useEffect(() => {
    if (!hydrated) {
      return;
    }

    if (firstUndoneIndex < 0) {
      setExpandedIndex(null);

      return;
    }

    setExpandedIndex((prev) => {
      if (prev !== null && doneByIndex[prev]) {
        return firstUndoneIndex;
      }

      if (prev === null) {
        return firstUndoneIndex;
      }

      return prev;
    });
  }, [hydrated, doneByIndex, firstUndoneIndex]);

  const toggleStep = useCallback((index: number) => {
    setDoneByIndex((prev) => {
      const next = [...prev];
      next[index] = !next[index];

      try {
        if (next[index]) {
          window.localStorage.setItem(corePilotStepDoneStorageKey(index), "1");
        } else {
          window.localStorage.removeItem(corePilotStepDoneStorageKey(index));
        }
      } catch {
        /* ignore */
      }

      emitCorePilotChecklistChanged();

      return next;
    });
  }, []);

  function minimize() {
    setMinimized(true);

    try {
      window.localStorage.setItem(minimizedStorageKey, "1");
    } catch {
      /* ignore */
    }
  }

  function expand() {
    setMinimized(false);

    try {
      window.localStorage.removeItem(minimizedStorageKey);
    } catch {
      /* ignore */
    }
  }

  if (!hydrated) {
    return <div className="min-h-[100px] w-full" aria-hidden />;
  }

  if (minimized) {
    return (
      <div>
        <button
          type="button"
          onClick={expand}
          aria-expanded={false}
          aria-controls="first-run-workflow-panel"
          className="auth-panel-focus w-full cursor-pointer rounded-lg border border-neutral-300 bg-white px-3.5 py-2 text-left text-sm text-neutral-900 dark:border-neutral-600 dark:bg-neutral-900 dark:text-neutral-100"
        >
          Show First Manifest Checklist
        </button>
      </div>
    );
  }

  return (
    <section
      id="first-run-workflow-panel"
      className="w-full rounded-lg border border-sky-200/90 bg-sky-50/90 px-3 py-3 dark:border-sky-900 dark:bg-sky-950/40"
      aria-labelledby="first-run-workflow-heading"
    >
      <div className="mb-2 flex flex-wrap items-start justify-between gap-2">
        <div className="min-w-0">
          <h2 id="first-run-workflow-heading" className="m-0 text-base font-semibold text-sky-900 dark:text-sky-100">
            First Manifest Checklist
          </h2>
          <p className="m-0 mt-0.5 text-[11px] text-sky-800 dark:text-sky-300">Create · run · finalize · review</p>
        </div>
        <button
          type="button"
          onClick={minimize}
          aria-expanded={true}
          aria-controls="first-run-workflow-panel"
          className="auth-panel-focus shrink-0 cursor-pointer rounded-md border border-sky-300/80 bg-white px-2.5 py-1 text-xs text-sky-800 dark:border-sky-700 dark:bg-neutral-900 dark:text-sky-200"
        >
          Hide
        </button>
      </div>
      <p className="m-0 mb-2 text-xs font-medium text-sky-900 dark:text-sky-100" aria-live="polite">
        {doneCount} of {steps.length} steps complete
      </p>
      {allDone ? (
        <p className="m-0 mb-2 rounded border border-teal-200/80 bg-teal-50/80 px-2 py-1.5 text-xs text-teal-900 dark:border-teal-800 dark:bg-teal-950/50 dark:text-teal-100">
          First manifest complete. You can hide this panel or revisit any step.
        </p>
      ) : null}
      <p className="m-0 mb-2 text-xs leading-snug text-neutral-700 dark:text-neutral-300">
        Complete these four steps to get from an empty workspace to a reviewed, exportable run.
      </p>
      <ol className="m-0 list-none space-y-2 p-0">
        {steps.map((step, index) => {
          const done = doneByIndex[index] === true;
          const expanded = expandedIndex === index;

          return (
            <li
              key={step.title}
              className={cn(
                "border-b border-sky-200/40 pb-2.5 last:border-b-0 dark:border-sky-800/40",
                done ? "opacity-60" : "",
              )}
            >
              <div className="flex items-start gap-2">
                <input
                  id={`workflow-step-done-${index}`}
                  type="checkbox"
                  className="auth-panel-focus mt-0.5 h-3.5 w-3.5 shrink-0 rounded border-neutral-300 text-teal-700 focus:ring-teal-700 dark:border-neutral-600 dark:bg-neutral-900"
                  checked={done}
                  onChange={() => {
                    toggleStep(index);
                  }}
                  aria-label={`Mark step ${index + 1} done: ${step.title}`}
                />
                <div className="min-w-0 flex-1">
                  <button
                    type="button"
                    className="auth-panel-focus m-0 w-full cursor-pointer rounded-sm border-0 bg-transparent p-0 text-left text-xs font-semibold text-neutral-900 hover:text-teal-900 dark:text-neutral-100 dark:hover:text-teal-200"
                    aria-expanded={expanded}
                    onClick={() => {
                      setExpandedIndex((prev) => (prev === index ? null : index));
                    }}
                  >
                    Step {index + 1} — {step.title}
                    {done ? <span className="ml-1 text-[10px] font-normal text-teal-700 dark:text-teal-400">(done)</span> : null}
                  </button>
                  {expanded ? (
                    <div className="mt-1.5">
                      <p className="m-0 text-[11px] leading-snug text-neutral-600 dark:text-neutral-400">{step.shortBody}</p>
                      {step.detail ? (
                        <details className="mt-1 text-[11px] text-neutral-600 dark:text-neutral-400">
                          <summary className="cursor-pointer select-none text-teal-800 underline decoration-teal-300/50 dark:text-teal-300">
                            More detail
                          </summary>
                          <p className="mt-1.5 m-0 leading-relaxed text-neutral-600 dark:text-neutral-400">{step.detail}</p>
                        </details>
                      ) : null}
                      <div className="mt-1.5">
                        <Button asChild variant="outline" size="sm" className="h-7 text-xs font-medium">
                          <Link className="no-underline" href={step.primaryHref}>
                            {step.primaryLabel}
                          </Link>
                        </Button>
                      </div>
                      {step.secondary ? (
                        <div className="mt-1.5 text-[11px] leading-snug text-neutral-600 dark:text-neutral-500">{step.secondary}</div>
                      ) : null}
                    </div>
                  ) : null}
                </div>
              </div>
            </li>
          );
        })}
      </ol>

      <div className="mt-3 border-t border-sky-200/40 pt-2.5 dark:border-sky-800/40">
        <p className="m-0 mb-1.5 flex flex-wrap items-center gap-x-2 gap-y-1 text-[10px] leading-snug text-neutral-600 dark:text-neutral-400">
          <span className="font-semibold text-neutral-700 dark:text-neutral-300">After your first finalization</span>
          <Link
            className="inline-flex rounded-full border border-neutral-200 bg-white px-2 py-0.5 font-medium text-teal-800 no-underline hover:bg-neutral-50 dark:border-neutral-600 dark:bg-neutral-900 dark:text-teal-300 dark:hover:bg-neutral-800"
            href="/compare"
          >
            Compare
          </Link>
          <Link
            className="inline-flex rounded-full border border-neutral-200 bg-white px-2 py-0.5 font-medium text-teal-800 no-underline hover:bg-neutral-50 dark:border-neutral-600 dark:bg-neutral-900 dark:text-teal-300 dark:hover:bg-neutral-800"
            href="/replay"
          >
            Replay
          </Link>
          <Link
            className="inline-flex rounded-full border border-neutral-200 bg-white px-2 py-0.5 font-medium text-teal-800 no-underline hover:bg-neutral-50 dark:border-neutral-600 dark:bg-neutral-900 dark:text-teal-300 dark:hover:bg-neutral-800"
            href="/graph"
          >
            Graph
          </Link>
        </p>
      </div>
    </section>
  );
}
