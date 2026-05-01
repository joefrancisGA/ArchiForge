"use client";

import Link from "next/link";
import { useEffect, useState } from "react";

import { HelpLink } from "@/components/HelpLink";
import { fetchCorePilotCommitContext } from "@/lib/core-pilot-commit-context";

type Phase = "loading" | "ready";

const STEPS: readonly {
  readonly title: string;
  readonly body: string;
}[] = [
  {
    title: "Create request",
    body: "Start the new-request wizard with your healthcare-claims modernization scenario (or your own system brief).",
  },
  {
    title: "Run review pipeline",
    body: "The pipeline runs automatically; watch progress on review detail or from the reviews list.",
  },
  {
    title: "Commit manifest",
    body: "When the review is ready, commit on review detail to produce the golden manifest.",
  },
  {
    title: "Review manifest and artifacts",
    body: "Review the manifest summary, PHI findings, and downloads on review detail.",
  },
];

/**
 * Above-the-fold checklist aligned to docs/CORE_PILOT.md §1 (four steps). Deep-links use run IDs when the
 * lightweight commit context can resolve them from existing APIs.
 */
export function CorePilotOneSessionChecklist() {
  const [phase, setPhase] = useState<Phase>("loading");
  const [latestRunId, setLatestRunId] = useState<string | null>(null);
  const [firstCommittedRunId, setFirstCommittedRunId] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    const safetyTimer =
      typeof window !== "undefined"
        ? window.setTimeout(() => {
            if (!cancelled) {
              setPhase("ready");
            }
          }, 4000)
        : null;

    void (async () => {
      try {
        const ctx = await fetchCorePilotCommitContext();

        if (!cancelled) {
          setLatestRunId(ctx.latestRunId);
          setFirstCommittedRunId(ctx.firstCommittedRunId);
          setPhase("ready");
        }
      } catch {
        if (!cancelled) {
          setLatestRunId(null);
          setFirstCommittedRunId(null);
          setPhase("ready");
        }
      }
    })();

    return () => {
      cancelled = true;

      if (safetyTimer !== null) {
        window.clearTimeout(safetyTimer);
      }
    };
  }, []);

  function hrefForStep(index: number): string {
    if (index === 0) {
      return "/reviews/new";
    }

    if (index === 1) {
      return "/reviews?projectId=default";
    }

    if (index === 2) {
      if (latestRunId !== null) {
        return `/reviews/${encodeURIComponent(latestRunId)}`;
      }

      return "/reviews?projectId=default";
    }

    if (firstCommittedRunId !== null) {
      return `/reviews/${encodeURIComponent(firstCommittedRunId)}`;
    }

    return "/reviews?projectId=default";
  }

  function labelForStep(index: number): string {
    if (index === 0) {
      return "New request";
    }

    if (index === 1) {
      return "Reviews list";
    }

    if (index === 2) {
      if (latestRunId !== null) {
        return "Open review detail to commit";
      }

      return "Choose a review to commit";
    }

    if (firstCommittedRunId !== null) {
      return "Open committed review";
    }

    return "Open a review after commit";
  }

  return (
    <section
      aria-labelledby="core-pilot-one-session-heading"
      data-testid="core-pilot-one-session-checklist"
      className="rounded-lg border border-teal-200 bg-teal-50/50 px-4 py-4 shadow-sm dark:border-teal-900 dark:bg-teal-950/25"
    >
      <div className="flex flex-wrap items-start justify-between gap-2">
        <h2
          id="core-pilot-one-session-heading"
          className="m-0 text-base font-semibold tracking-tight text-neutral-900 dark:text-neutral-100"
        >
          First architecture review in one session
        </h2>
        <HelpLink docPath="/docs/CORE_PILOT.md" label="Open Core Pilot path on GitHub (new tab)" />
      </div>
      <p className="m-0 mt-2 max-w-prose text-xs leading-snug text-neutral-600 dark:text-neutral-400">
        Four steps from the Core Pilot guide. Finish these before spending time on Compare, Replay, Governance, or
        other Operate surfaces — they stay available in the shell when you are ready.
      </p>

      {phase === "loading" ? (
        <p className="m-0 mt-3 text-xs text-neutral-500 dark:text-neutral-400" role="status">
          Resolving checklist links…
        </p>
      ) : (
        <ol className="m-0 mt-3 list-none space-y-2.5 p-0">
          {STEPS.map((step, index) => (
            <li key={step.title}>
              <div className="flex flex-wrap items-baseline gap-x-2 gap-y-1">
                <span className="text-xs font-semibold tabular-nums text-teal-800 dark:text-teal-200">
                  {index + 1}.
                </span>
                <span className="text-xs font-semibold text-neutral-900 dark:text-neutral-100">{step.title}</span>
              </div>
              <p className="m-0 mt-0.5 pl-5 text-[11px] leading-snug text-neutral-600 dark:text-neutral-400">
                {step.body}{" "}
                <Link
                  href={hrefForStep(index)}
                  className="font-medium text-teal-800 underline decoration-teal-400/60 underline-offset-2 hover:text-teal-950 dark:text-teal-300 dark:hover:text-teal-200"
                >
                  {labelForStep(index)}
                </Link>
              </p>
            </li>
          ))}
        </ol>
      )}
    </section>
  );
}
