"use client";

import Link from "next/link";
import type { ReactNode } from "react";
import { useEffect, useState } from "react";

const storageKey = "archlucid_operator_workflow_guide_v1";

type WorkflowStep = {
  title: string;
  body: string;
  primaryHref: string;
  primaryLabel: string;
  secondary?: ReactNode;
};

const steps: WorkflowStep[] = [
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
    body: "After creation, the coordinator fills snapshots and authority steps. Watch progress on the wizard’s last step or open run detail anytime.",
    primaryHref: "/runs?projectId=default",
    primaryLabel: "Open runs list",
    secondary: (
      <>
        Tip: from the wizard’s final step, use <strong>Open run detail</strong> for the new ID.
      </>
    ),
  },
  {
    title: "Commit the golden manifest",
    body: "Until commit, there is no manifest link or artifact exports. Commit through the API or CLI when the run is ready.",
    primaryHref: "/runs?projectId=default",
    primaryLabel: "Choose run → open detail",
    secondary: (
      <>
        See <code>docs/OPERATOR_QUICKSTART.md</code> in the repo for commit examples.
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
  {
    title: "Compare or replay",
    body: "Diff two runs structurally, or replay authority validation for one run. Run detail has shortcuts prefilled for this run.",
    primaryHref: "/compare",
    primaryLabel: "Compare two runs",
    secondary: (
      <>
        <Link className="workflow-inline-link text-teal-700 dark:text-teal-400" href="/replay">
          Replay a run
        </Link>{" "}
        ·{" "}
        <Link className="workflow-inline-link text-teal-700 dark:text-teal-400" href="/graph">
          Graph (visual)
        </Link>
      </>
    ),
  },
  {
    title: "Export a package",
    body: "On run detail (with a manifest), use Download bundle (ZIP) and Download run export (ZIP) under Artifacts.",
    primaryHref: "/runs?projectId=default",
    primaryLabel: "Runs: open run → Artifacts",
  },
];

/**
 * Collapsible first-run checklist on Home. Persists “minimized” in localStorage so returning operators can hide it.
 */
export function OperatorFirstRunWorkflowPanel() {
  const [hydrated, setHydrated] = useState(false);
  const [minimized, setMinimized] = useState(false);

  useEffect(() => {
    try {
      if (typeof window !== "undefined" && window.localStorage.getItem(storageKey) === "1") {
        setMinimized(true);
      }
    } catch {
      /* private mode / SSR guard */
    }

    setHydrated(true);
  }, []);

  function minimize() {
    setMinimized(true);

    try {
      window.localStorage.setItem(storageKey, "1");
    } catch {
      /* ignore */
    }
  }

  function expand() {
    setMinimized(false);

    try {
      window.localStorage.removeItem(storageKey);
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
          Show V1 workflow checklist
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
        <h2 id="first-run-workflow-heading" className="m-0 text-lg font-semibold text-sky-900 dark:text-sky-100">
          First-run workflow (V1 checklist)
        </h2>
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
      <p className="m-0 mb-4 max-w-[760px] text-sm leading-relaxed text-neutral-700 dark:text-neutral-300">
        Follow these steps once to go from an empty tenant to a reviewed, exportable architecture run. Your next
        action is highlighted on each step.
      </p>
      <ol className="m-0 list-decimal pl-6 leading-normal text-neutral-800 dark:text-neutral-200">
        {steps.map((step, index) => (
          <li key={step.title} className="mb-[22px]">
            <strong className="mb-1.5 block">
              {index + 1}. {step.title}
            </strong>
            <span className="text-sm text-neutral-600 dark:text-neutral-400">{step.body}</span>
            <div>
              <Link
                className="workflow-primary-action mt-2.5 inline-block rounded-lg bg-teal-700 px-[18px] py-2.5 text-sm font-semibold text-white no-underline hover:bg-teal-800 dark:bg-teal-600 dark:hover:bg-teal-500"
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
