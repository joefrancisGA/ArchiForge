"use client";

import Link from "next/link";
import type { ReactNode } from "react";
import { useCallback, useEffect, useMemo, useState } from "react";

import { corePilotStepDoneStorageKey, emitCorePilotChecklistChanged } from "@/lib/core-pilot-checklist-storage";
import { NAV_DISCLOSURE } from "@/lib/nav-disclosure-copy";

const minimizedStorageKey = "archlucid_operator_workflow_guide_v1";

type WorkflowStep = {
  title: string;
  body: string;
  primaryHref: string;
  primaryLabel: string;
  secondary?: ReactNode;
};

/**
 * Core Pilot path — the four steps every first pilot must complete.
 * Steps 5-6 (compare/replay/export) are listed separately below as optional.
 */
const corePilotSteps: WorkflowStep[] = [
  {
    title: "Create an architecture request",
    body: "The guided wizard walks you through system identity, requirements, constraints, and advanced inputs — then submits the run and tracks the pipeline in real time.",
    primaryHref: "/runs/new",
    primaryLabel: "Start new run wizard",
    secondary: (
      <>
        Or browse existing runs on the{" "}
        <Link className="workflow-inline-link text-teal-700 dark:text-teal-400" href="/runs?projectId=default">
          Runs list
        </Link>
        .
      </>
    ),
  },
  {
    title: "Let the pipeline run, then open the run",
    body: "After creation, the coordinator fills snapshots and pipeline validation steps. Watch progress on the wizard's last step or open run detail anytime.",
    primaryHref: "/runs?projectId=default",
    primaryLabel: "Open runs list",
    secondary: (
      <>
        Tip: from the wizard&apos;s final step, use <strong>Open run detail</strong> for the new ID.
      </>
    ),
  },
  {
    title: "Commit the golden manifest",
    body: "Until commit, there is no manifest link or artifact exports. On run detail, use Commit run when the run is ready, or commit through the API or CLI.",
    primaryHref: "/runs?projectId=default",
    primaryLabel: "Choose run → open detail",
    secondary: (
      <>
        CLI/API: <code>docs/OPERATOR_QUICKSTART.md</code> in the repo.
      </>
    ),
  },
  {
    title: "Inspect manifest & artifacts",
    body: "After commit, run detail shows manifest summary, the artifact table, and links into each artifact.",
    primaryHref: "/runs?projectId=default",
    primaryLabel: "Open a committed run",
    secondary: (
      <>
        Full manifest page: open the <strong>Golden manifest</strong> link from run detail.
      </>
    ),
  },
];

// Alias kept for localStorage key stability (step indices 0–3 map to corePilotSteps).
const steps = corePilotSteps;

/**
 * Collapsible Core Pilot checklist on Home. Persists "minimized" in localStorage so returning operators can hide it.
 * Advanced operations (compare, replay, graph, export) are surfaced as optional links below the core steps.
 */
export function OperatorFirstRunWorkflowPanel() {
  const [hydrated, setHydrated] = useState(false);
  const [minimized, setMinimized] = useState(false);
  const [doneByIndex, setDoneByIndex] = useState<boolean[]>(() => steps.map(() => false));

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
    return <div className="mb-7 min-h-[140px]" aria-hidden />;
  }

  if (minimized) {
    return (
      <div className="mb-5">
        <button
          type="button"
          onClick={expand}
          aria-expanded={false}
          aria-controls="first-run-workflow-panel"
          className="auth-panel-focus cursor-pointer rounded-lg border border-neutral-300 bg-white px-3.5 py-2 text-sm text-neutral-900 dark:border-neutral-600 dark:bg-neutral-900 dark:text-neutral-100"
        >
          Show Core Pilot checklist
        </button>
      </div>
    );
  }

  return (
    <section
      id="first-run-workflow-panel"
      className="mb-7 max-w-[820px] rounded-[10px] border border-sky-200 bg-sky-50 px-5 py-[18px] dark:border-sky-900 dark:bg-sky-950/40"
      aria-labelledby="first-run-workflow-heading"
    >
      <div className="mb-3.5 flex flex-wrap items-start justify-between gap-3">
        <div>
          <h2 id="first-run-workflow-heading" className="m-0 text-lg font-semibold text-sky-900 dark:text-sky-100">
            Core Pilot checklist
          </h2>
          <p className="m-0 mt-0.5 text-xs text-sky-700 dark:text-sky-300">
            4 steps — create · run · commit · review
          </p>
        </div>
        <button
          type="button"
          onClick={minimize}
          aria-expanded={true}
          aria-controls="first-run-workflow-panel"
          className="auth-panel-focus cursor-pointer rounded-md border border-sky-300 bg-white px-3 py-1.5 text-[13px] text-sky-800 dark:border-sky-700 dark:bg-neutral-900 dark:text-sky-200"
        >
          Hide checklist
        </button>
      </div>
      <p className="m-0 mb-2 text-sm font-medium text-sky-900 dark:text-sky-100" aria-live="polite">
        Progress: {doneCount} of {steps.length} steps marked done
      </p>
      {allDone ? (
        <p className="m-0 mb-4 rounded-md border border-teal-200 bg-teal-50 px-3 py-2 text-sm text-teal-900 dark:border-teal-800 dark:bg-teal-950/50 dark:text-teal-100">
          Core Pilot complete — hide the checklist when you no longer need it, or reset individual steps with the
          checkboxes.
        </p>
      ) : null}
      <p className="m-0 mb-4 max-w-[760px] text-sm leading-relaxed text-neutral-700 dark:text-neutral-300">
        Complete these four steps to go from an empty workspace to a reviewed, exportable architecture run.
      </p>
      <ol className="m-0 list-decimal pl-6 leading-normal text-neutral-800 dark:text-neutral-200">
        {steps.map((step, index) => (
          <li key={step.title} className="mb-[22px]">
            <div className="mb-1.5 flex flex-wrap items-start gap-2">
              <input
                id={`workflow-step-done-${index}`}
                type="checkbox"
                className="auth-panel-focus mt-1 h-4 w-4 shrink-0 rounded border-neutral-300 text-teal-700 focus:ring-teal-700 dark:border-neutral-600 dark:bg-neutral-900"
                checked={doneByIndex[index] === true}
                onChange={() => {
                  toggleStep(index);
                }}
                aria-label={`Mark step ${index + 1} done: ${step.title}`}
              />
              <strong className="block flex-1">
                {index + 1}. {step.title}
              </strong>
            </div>
            <span className="text-sm text-neutral-600 dark:text-neutral-400">{step.body}</span>
            <div>
              <Link
                className="workflow-primary-action mt-2.5 inline-block rounded-lg bg-teal-700 px-[18px] py-2.5 text-sm font-semibold text-white no-underline hover:bg-teal-800 dark:bg-teal-800 dark:text-white dark:hover:bg-teal-700"
                href={step.primaryHref}
              >
                {step.primaryLabel}
              </Link>
            </div>
            {step.secondary ? (
              <div className="mt-2 text-[13px] leading-normal text-neutral-600 dark:text-neutral-400">
                {step.secondary}
              </div>
            ) : null}
          </li>
        ))}
      </ol>

      {/* Optional next steps — not required for the Core Pilot but available once you have a committed run. */}
      <div className="mt-3 rounded-md border border-neutral-200 bg-white px-4 py-3 dark:border-neutral-700 dark:bg-neutral-900">
        <p className="m-0 mb-1.5 text-xs font-semibold uppercase tracking-wide text-neutral-500 dark:text-neutral-400">
          Later maturity — not first pilot
        </p>
        <p className="m-0 mb-2 text-[12px] leading-snug text-neutral-500 dark:text-neutral-400">
          Explore only after commit (or when you explicitly expand scope). Does not change what “done” means for Core
          Pilot.
        </p>
        <ul className="m-0 list-none space-y-1 pl-0 text-[13px] text-neutral-600 dark:text-neutral-400">
          <li>
            <Link className="workflow-inline-link text-teal-700 dark:text-teal-400" href="/compare">
              Compare two runs
            </Link>
            {" — "}structured manifest diff between a base run and a target run.
          </li>
          <li>
            <Link className="workflow-inline-link text-teal-700 dark:text-teal-400" href="/replay">
              Replay a run
            </Link>
            {" — "}re-validate the provenance chain and surface validation results.
          </li>
          <li>
            <Link className="workflow-inline-link text-teal-700 dark:text-teal-400" href="/graph">
              Graph (visual)
            </Link>
            {" — "}provenance or architecture graph for a run ID. Enable via{" "}
            <em>{NAV_DISCLOSURE.extended.show}</em> in the sidebar.
          </li>
          <li>
            <strong>Export a package</strong> — on run detail (committed), use{" "}
            <em>Download bundle (ZIP)</em> and <em>Download run export (ZIP)</em> under Artifacts.
          </li>
        </ul>
      </div>

      <p className="mt-[18px] text-[13px] text-neutral-700 dark:text-neutral-300">
        More orientation:{" "}
        <Link className="workflow-inline-link text-teal-700 dark:text-teal-400" href="/onboarding">
          Onboarding
        </Link>{" "}
        ·{" "}
        <Link className="workflow-inline-link text-teal-700 dark:text-teal-400" href="/">
          Home overview
        </Link>
      </p>
    </section>
  );
}
