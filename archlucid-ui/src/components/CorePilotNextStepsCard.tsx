"use client";

import Link from "next/link";
import { useEffect, useState } from "react";

import { HelpLink } from "@/components/HelpLink";
import { fetchCorePilotCommitContext } from "@/lib/core-pilot-commit-context";

type Phase = "loading" | "ready";

/**
 * Core Pilot four-step rail for operator home. When no manifest is committed yet, shows pilot-only routes; after
 * commit, offers a single optional Operate entry (per product packaging — no deep Operate links in the pre-commit body).
 */
export function CorePilotNextStepsCard() {
  const [phase, setPhase] = useState<Phase>("loading");
  const [hasCommit, setHasCommit] = useState(false);

  useEffect(() => {
    let cancelled = false;

    async function load() {
      setPhase("loading");

      try {
        const ctx = await fetchCorePilotCommitContext();

        if (cancelled) {
          return;
        }

        setHasCommit(ctx.hasCommittedManifest);
        setPhase("ready");
      } catch {
        if (cancelled) {
          return;
        }

        setHasCommit(false);
        setPhase("ready");
      }
    }

    void load();

    return () => {
      cancelled = true;
    };
  }, []);

  if (phase === "loading") {
    return null;
  }

  if (hasCommit) {
    return (
      <section
        className="rounded-lg border border-neutral-200 bg-white p-4 shadow-sm dark:border-neutral-700 dark:bg-neutral-950"
        aria-labelledby="core-pilot-next-steps-complete"
        data-testid="core-pilot-next-steps-complete"
      >
        <h2
          id="core-pilot-next-steps-complete"
          className="mt-0 text-base font-semibold text-neutral-900 dark:text-neutral-100"
        >
          Core Pilot complete
        </h2>
        <p className="mb-3 mt-2 text-sm text-neutral-700 dark:text-neutral-300">
          First committed manifest is in place. Optional next: explore Operate tools when your team is ready.
        </p>
        <div className="flex flex-col gap-2 text-sm">
          <Link
            href="/governance/dashboard"
            className="text-neutral-600 underline decoration-neutral-400 underline-offset-2 hover:text-neutral-800 dark:text-neutral-400 dark:hover:text-neutral-200"
          >
            Workspace health (sponsor view)
          </Link>
          <Link href="/ask" className="font-medium text-blue-700 underline dark:text-blue-400">
            Open Ask (Operate)
          </Link>
        </div>
      </section>
    );
  }

  return (
    <section
      className="rounded-lg border border-neutral-200 bg-white p-4 shadow-sm dark:border-neutral-700 dark:bg-neutral-950"
      aria-labelledby="core-pilot-next-steps"
      data-testid="core-pilot-next-steps"
    >
      <div className="mb-2 flex flex-wrap items-center gap-2">
        <h2 id="core-pilot-next-steps" className="m-0 text-base font-semibold text-neutral-900 dark:text-neutral-100">
          Core Pilot — your first session
        </h2>
        <HelpLink docPath="/docs/CORE_PILOT.md" label="Open Core Pilot guide on GitHub (new tab)" />
      </div>
      <ol className="m-0 mt-3 list-decimal space-y-2 pl-5 text-sm text-neutral-800 dark:text-neutral-200">
        <li>
          <Link href="/reviews/new" className="font-medium text-blue-700 underline dark:text-blue-400">
            Create architecture request
          </Link>
        </li>
        <li>
          <Link href="/reviews?projectId=default" className="font-medium text-blue-700 underline dark:text-blue-400">
            Open Reviews — run the pipeline
          </Link>
        </li>
        <li>
          Commit a golden manifest from review detail when the pipeline completes (
          <HelpLink docPath="/docs/CORE_PILOT.md" label="Core Pilot — commit step (new tab)" />).
        </li>
        <li>
          Review the architecture package and findings from the same review (
          <Link href="/help" className="font-medium text-blue-700 underline dark:text-blue-400">
            Help
          </Link>
          ).
        </li>
      </ol>
    </section>
  );
}
