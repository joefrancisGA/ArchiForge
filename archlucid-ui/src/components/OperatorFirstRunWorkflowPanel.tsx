"use client";

import Link from "next/link";

import { Button } from "@/components/ui/button";
import type { ReactNode } from "react";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";

import { listRunsByProjectPaged } from "@/lib/api";
import { corePilotStepDoneStorageKey, emitCorePilotChecklistChanged } from "@/lib/core-pilot-checklist-storage";
import { CORE_PILOT_STEPS } from "@/lib/core-pilot-steps";
import { readHasExistingRunsCache, writeHasExistingRunsCache } from "@/lib/operator-run-presence";
import { SHOWCASE_STATIC_DEMO_RUN_ID } from "@/lib/showcase-static-demo";
import { GlossaryTooltip } from "@/components/GlossaryTooltip";
import { cn } from "@/lib/utils";

const minimizedStorageKey = "archlucid_operator_workflow_guide_v1";
const graduatedStorageKey = "archlucid_checklist_graduated";

type WorkflowStep = {
  title: string;
  shortBody: string;
  detail?: string;
  primaryHref: string;
  primaryLabel: string;
  secondary?: ReactNode;
};

const corePilotSteps: WorkflowStep[] = CORE_PILOT_STEPS.map((s, index) =>
  index === 1
    ? {
        ...s,
        secondary: (
          <>
            From the final wizard step, use <strong>Open run detail</strong> for the new run ID.
          </>
        ),
      }
    : s,
);

/**
 * Collapsible first-manifest checklist. Persists "minimized" in localStorage. Compact for a side column; step actions are
 * outline buttons so they do not compete with the main home CTAs.
 */
export function OperatorFirstRunWorkflowPanel(props: { exploreCompletedOutput?: boolean } = {}) {
  const exploreCompletedOutput = props.exploreCompletedOutput === true;
  const autoGraduateBlockedRef = useRef(false);
  const [hydrated, setHydrated] = useState(false);
  const [minimized, setMinimized] = useState(false);
  const [graduated, setGraduated] = useState(false);
  const [doneByIndex, setDoneByIndex] = useState<boolean[]>(() => corePilotSteps.map(() => false));
  const [expandedIndex, setExpandedIndex] = useState<number | null>(null);
  const [hasAnyRun, setHasAnyRun] = useState(false);

  useEffect(() => {
    const nextDone: boolean[] = [];

    for (let i = 0; i < corePilotSteps.length; i++) {
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

    const allDoneFromStorage = nextDone.length === corePilotSteps.length && nextDone.every(Boolean);

    try {
      if (typeof window !== "undefined" && window.localStorage.getItem(minimizedStorageKey) === "1") {
        setMinimized(true);
      }
    } catch {
      /* private mode */
    }

    try {
      if (typeof window !== "undefined") {
        const rawGrad = window.localStorage.getItem(graduatedStorageKey);

        if (rawGrad === "1" && allDoneFromStorage) {
          setGraduated(true);
        }

        if (rawGrad === "1" && !allDoneFromStorage) {
          window.localStorage.removeItem(graduatedStorageKey);
        }
      }
    } catch {
      /* private mode */
    }

    setHydrated(true);
    emitCorePilotChecklistChanged();
  }, []);

  useEffect(() => {
    setHasAnyRun(readHasExistingRunsCache());
    let cancelled = false;

    void (async () => {
      try {
        const page = await listRunsByProjectPaged("default", 1, 1);
        const next = (page.items?.length ?? 0) > 0;

        if (cancelled) {
          return;
        }

        setHasAnyRun(next);
        writeHasExistingRunsCache(next);
      } catch {
        if (!cancelled) {
          setHasAnyRun(false);
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, []);

  const doneCount = useMemo(() => doneByIndex.filter(Boolean).length, [doneByIndex]);
  const allDone = doneCount === corePilotSteps.length;

  const firstUndoneIndex = useMemo(() => doneByIndex.findIndex((d) => !d), [doneByIndex]);

  useEffect(() => {
    if (!hydrated || !allDone) {
      return;
    }

    if (autoGraduateBlockedRef.current) {
      return;
    }

    try {
      window.localStorage.setItem(graduatedStorageKey, "1");
    } catch {
      /* private mode */
    }

    setGraduated(true);
  }, [hydrated, allDone]);

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
    autoGraduateBlockedRef.current = false;

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

  function revisitChecklist() {
    autoGraduateBlockedRef.current = true;

    try {
      window.localStorage.removeItem(graduatedStorageKey);
    } catch {
      /* private mode */
    }

    setGraduated(false);
    setMinimized(false);

    try {
      window.localStorage.removeItem(minimizedStorageKey);
    } catch {
      /* private mode */
    }
  }

  if (minimized) {
    return (
      <div data-onboarding="tour-core-pilot">
        <button
          type="button"
          onClick={expand}
          aria-expanded={false}
          aria-controls="first-run-workflow-panel"
          className="auth-panel-focus w-full cursor-pointer rounded-lg border border-neutral-300 bg-white px-3.5 py-2 text-left text-sm text-neutral-900 dark:border-neutral-600 dark:bg-neutral-900 dark:text-neutral-100"
        >
          Show First Manifest Guide
        </button>
      </div>
    );
  }

  if (graduated && allDone) {
    return (
      <section
        data-onboarding="tour-core-pilot"
        className="w-full rounded-lg border border-neutral-200 bg-neutral-50 px-3 py-3 dark:border-neutral-700 dark:bg-neutral-900/80"
        aria-labelledby="whats-next-heading"
      >
        <h2 id="whats-next-heading" className="m-0 text-base font-semibold text-neutral-900 dark:text-neutral-100">
          What&apos;s next
        </h2>
        <p className="m-0 mt-1 text-xs text-neutral-700 dark:text-neutral-300">
          Compare runs, replay pipeline steps, and explore the architecture graph.
        </p>
        <div className="mt-2 flex flex-wrap gap-1.5">
          <Link
            className="inline-flex rounded-full border border-neutral-200 bg-white px-2 py-0.5 text-xs font-medium text-teal-800 no-underline hover:bg-neutral-50 dark:border-neutral-600 dark:bg-neutral-900 dark:text-teal-300 dark:hover:bg-neutral-800"
            href="/compare"
          >
            Compare
          </Link>
          <Link
            className="inline-flex rounded-full border border-neutral-200 bg-white px-2 py-0.5 text-xs font-medium text-teal-800 no-underline hover:bg-neutral-50 dark:border-neutral-600 dark:bg-neutral-900 dark:text-teal-300 dark:hover:bg-neutral-800"
            href="/replay"
          >
            Replay
          </Link>
          <Link
            className="inline-flex rounded-full border border-neutral-200 bg-white px-2 py-0.5 text-xs font-medium text-teal-800 no-underline hover:bg-neutral-50 dark:border-neutral-600 dark:bg-neutral-900 dark:text-teal-300 dark:hover:bg-neutral-800"
            href="/graph"
          >
            Graph
          </Link>
        </div>
        <button
          type="button"
          onClick={revisitChecklist}
          className="auth-panel-focus mt-2 cursor-pointer text-xs font-semibold text-teal-800 underline dark:text-teal-300"
        >
          Revisit checklist
        </button>
      </section>
    );
  }

  return (
    <section
      id="first-run-workflow-panel"
      data-onboarding="tour-core-pilot"
      className="w-full rounded-lg border border-neutral-200 bg-white px-3 py-3 shadow-sm dark:border-neutral-700 dark:bg-neutral-900"
      aria-labelledby="first-run-workflow-heading"
    >
      {hasAnyRun ? (
        <div className="mb-3 rounded-md border border-teal-200 bg-teal-50/70 px-3 py-2.5 dark:border-teal-800 dark:bg-teal-950/40">
          <h2 id="first-run-workflow-heading" className="m-0 text-sm font-semibold text-teal-900 dark:text-teal-100">
            Explore completed output
          </h2>
          <p className="m-0 mt-0.5 text-xs text-teal-800 dark:text-teal-300">
            A run has completed. Jump into the outputs below.
          </p>
          <div className="mt-2 flex flex-wrap gap-1.5">
            <Link
              className="inline-flex rounded-full border border-teal-200 bg-white px-2 py-0.5 text-xs font-medium text-teal-800 no-underline hover:bg-teal-50 dark:border-teal-700 dark:bg-neutral-900 dark:text-teal-300 dark:hover:bg-teal-950/60"
              href="/runs?projectId=default"
            >
              Runs
            </Link>
            <Link
              className="inline-flex rounded-full border border-teal-200 bg-white px-2 py-0.5 text-xs font-medium text-teal-800 no-underline hover:bg-teal-50 dark:border-teal-700 dark:bg-neutral-900 dark:text-teal-300 dark:hover:bg-teal-950/60"
              href={`/runs/${encodeURIComponent(SHOWCASE_STATIC_DEMO_RUN_ID)}`}
            >
              Claims Intake run
            </Link>
            <Link
              className="inline-flex rounded-full border border-teal-200 bg-white px-2 py-0.5 text-xs font-medium text-teal-800 no-underline hover:bg-teal-50 dark:border-teal-700 dark:bg-neutral-900 dark:text-teal-300 dark:hover:bg-teal-950/60"
              href="/showcase/claims-intake-modernization"
            >
              Showcase
            </Link>
            <Link
              className="inline-flex rounded-full border border-teal-200 bg-white px-2 py-0.5 text-xs font-medium text-teal-800 no-underline hover:bg-teal-50 dark:border-teal-700 dark:bg-neutral-900 dark:text-teal-300 dark:hover:bg-teal-950/60"
              href="/governance/findings"
            >
              Findings
            </Link>
            <Link
              className="inline-flex rounded-full border border-teal-200 bg-white px-2 py-0.5 text-xs font-medium text-teal-800 no-underline hover:bg-teal-50 dark:border-teal-700 dark:bg-neutral-900 dark:text-teal-300 dark:hover:bg-teal-950/60"
              href="/ask"
            >
              Ask
            </Link>
            <Link
              className="inline-flex rounded-full border border-teal-200 bg-white px-2 py-0.5 text-xs font-medium text-teal-800 no-underline hover:bg-teal-50 dark:border-teal-700 dark:bg-neutral-900 dark:text-teal-300 dark:hover:bg-teal-950/60"
              href="/compare"
            >
              Compare
            </Link>
          </div>
        </div>
      ) : null}

      <div className="mb-2 flex flex-wrap items-start justify-between gap-2">
        <div className="min-w-0">
          {exploreCompletedOutput ? (
            <>
              <h2 className="m-0 text-base font-semibold text-neutral-900 dark:text-neutral-100">Explore completed output</h2>
              <p className="m-0 mt-1 text-xs text-neutral-600 dark:text-neutral-400">
                Claims Intake is the guided story — run detail, manifest, and showcase are the proof path. The checklist
                below is optional.
              </p>
              <p className="m-0 mt-2 text-xs font-medium text-neutral-700 dark:text-neutral-300">
                <Link
                  className="text-teal-800 underline decoration-teal-300/50 underline-offset-2 hover:text-teal-900 dark:text-teal-300 dark:hover:text-teal-200"
                  href={`/runs/${encodeURIComponent(SHOWCASE_STATIC_DEMO_RUN_ID)}`}
                >
                  Open the completed Claims Intake run
                </Link>{" "}
                ·{" "}
                <Link
                  className="text-teal-800 underline decoration-teal-300/50 underline-offset-2 hover:text-teal-900 dark:text-teal-300 dark:hover:text-teal-200"
                  href="/showcase/claims-intake-modernization"
                >
                  Public showcase
                </Link>
              </p>
            </>
          ) : !hasAnyRun ? (
            <h2 id="first-run-workflow-heading" className="m-0 text-base font-semibold text-neutral-900 dark:text-neutral-100">
              First Manifest Guide
            </h2>
          ) : (
            <h2 className="m-0 text-xs font-semibold uppercase tracking-wide text-neutral-500 dark:text-neutral-400">
              First Manifest Guide
            </h2>
          )}
          {!exploreCompletedOutput ? (
            <p className="m-0 mt-0.5 text-xs font-medium tracking-wide text-neutral-600 dark:text-neutral-400">
              Create → <GlossaryTooltip termKey="run">Run</GlossaryTooltip> → Finalize → Review
            </p>
          ) : null}
        </div>
        <button
          type="button"
          onClick={minimize}
          aria-expanded={true}
          aria-controls="first-run-workflow-panel"
          className="auth-panel-focus shrink-0 cursor-pointer rounded-md border border-neutral-300 bg-white px-2.5 py-1 text-xs text-neutral-800 dark:border-neutral-600 dark:bg-neutral-900 dark:text-neutral-200"
        >
          Hide
        </button>
      </div>
      <p className="m-0 mb-2 text-xs font-medium text-neutral-800 dark:text-neutral-200" aria-live="polite">
        {doneCount} of {corePilotSteps.length} steps complete
      </p>
      {allDone ? (
        <p className="m-0 mb-2 rounded border border-teal-200/80 bg-teal-50/80 px-2 py-1.5 text-xs text-teal-900 dark:border-teal-800 dark:bg-teal-950/50 dark:text-teal-100">
          First manifest complete. You can hide this panel or revisit any step.
        </p>
      ) : null}
      <p className="m-0 mb-2 text-xs leading-snug text-neutral-700 dark:text-neutral-300">
        Complete these four steps to get from an empty workspace to a reviewed, exportable{" "}
        <GlossaryTooltip termKey="run">architecture run</GlossaryTooltip>.
      </p>
      <ol className="m-0 list-none space-y-2 p-0">
        {corePilotSteps.map((step, index) => {
          const done = doneByIndex[index] === true;
          const expanded = expandedIndex === index;
          const isActiveUndone = index === firstUndoneIndex && firstUndoneIndex >= 0;
          const showBody = isActiveUndone || expanded;

          const highlightNext = isActiveUndone;

          return (
            <li
              key={step.title}
              className={cn(
                "border-b border-neutral-200/80 pb-2.5 last:border-b-0 dark:border-neutral-800/80",
                done ? "opacity-60" : "",
                highlightNext
                  ? "rounded-md border-l-2 border-l-teal-600 bg-teal-50/25 pl-2 dark:border-l-teal-400 dark:bg-teal-950/20"
                  : "",
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
                    aria-expanded={showBody}
                    onClick={() => {
                      setExpandedIndex((prev) => (prev === index ? null : index));
                    }}
                  >
                    Step {index + 1} — {step.title}
                    {highlightNext ? (
                      <span className="ml-1.5 inline-flex align-middle rounded-full bg-teal-100 px-1.5 py-0.5 text-[9px] font-bold uppercase tracking-wide text-teal-900 dark:bg-teal-900/70 dark:text-teal-100">
                        {exploreCompletedOutput ? "Next step" : "Start here"}
                      </span>
                    ) : null}
                    {done ? <span className="ml-1 text-[10px] font-normal text-teal-700 dark:text-teal-400">(done)</span> : null}
                  </button>
                  {showBody ? (
                    <div className="mt-1.5">
                      <p className="m-0 text-[11px] leading-snug text-neutral-600 dark:text-neutral-400">{step.shortBody}</p>
                      <div className="mt-1.5">
                        <Button asChild variant="outline" size="sm" className="h-7 text-xs font-medium">
                          <Link className="no-underline" href={step.primaryHref}>
                            {step.primaryLabel}
                          </Link>
                        </Button>
                      </div>
                      {index === 0 && hasAnyRun ? (
                        <div className="mt-1 text-[11px] leading-snug text-neutral-500 dark:text-neutral-500 [&_a]:text-teal-700 [&_a]:underline [&_a]:decoration-teal-300/50 dark:[&_a]:text-teal-400">
                          Or open the{" "}
                          <Link className="workflow-inline-link text-teal-700 dark:text-teal-400" href="/runs?projectId=default">
                            Runs list
                          </Link>
                          .
                        </div>
                      ) : null}
                      {step.secondary ? (
                        <div className="mt-1 text-[11px] leading-snug text-neutral-500 dark:text-neutral-500 [&_a]:text-teal-700 [&_a]:underline [&_a]:decoration-teal-300/50 dark:[&_a]:text-teal-400">
                          {step.secondary}
                        </div>
                      ) : null}
                    </div>
                  ) : null}
                </div>
              </div>
            </li>
          );
        })}
      </ol>

      <div className="mt-3 border-t border-neutral-200/80 pt-2.5 dark:border-neutral-800/80">
        <p className="m-0 text-xs font-semibold text-neutral-700 dark:text-neutral-300">
          After finalizing your first manifest
        </p>
        <div className="mt-2 flex flex-wrap gap-1.5">
          <Link
            className="inline-flex rounded-full border border-neutral-200 bg-white px-2 py-0.5 text-xs font-medium text-teal-800 no-underline hover:bg-neutral-50 dark:border-neutral-600 dark:bg-neutral-900 dark:text-teal-300 dark:hover:bg-neutral-800"
            href="/compare"
          >
            Compare
          </Link>
          <Link
            className="inline-flex rounded-full border border-neutral-200 bg-white px-2 py-0.5 text-xs font-medium text-teal-800 no-underline hover:bg-neutral-50 dark:border-neutral-600 dark:bg-neutral-900 dark:text-teal-300 dark:hover:bg-neutral-800"
            href="/replay"
          >
            Replay
          </Link>
          <Link
            className="inline-flex rounded-full border border-neutral-200 bg-white px-2 py-0.5 text-xs font-medium text-teal-800 no-underline hover:bg-neutral-50 dark:border-neutral-600 dark:bg-neutral-900 dark:text-teal-300 dark:hover:bg-neutral-800"
            href="/graph"
          >
            Graph
          </Link>
        </div>
      </div>
    </section>
  );
}
