"use client";

import { useEffect, useMemo, useState } from "react";

import { Badge } from "@/components/ui/badge";
import { Progress } from "@/components/ui/progress";
import { Separator } from "@/components/ui/separator";
import { getRunSummary } from "@/lib/api";
import type { RunSummary } from "@/types/authority";

export type RunProgressTrackerProps = {
  runId: string;
  initialSummary: RunSummary | null;
};

function stageDone(flag: boolean | undefined): boolean {
  return flag === true;
}

function allStagesReady(s: RunSummary | null): boolean {
  if (s === null) {
    return false;
  }

  return (
    stageDone(s.hasContextSnapshot) &&
    stageDone(s.hasGraphSnapshot) &&
    stageDone(s.hasFindingsSnapshot) &&
    stageDone(s.hasGoldenManifest)
  );
}

const POLL_INTERVAL_MS = 3000;
const POLL_MAX_MS = 180_000;

export function RunProgressTracker({ runId, initialSummary }: RunProgressTrackerProps) {
  const pollEnabled = !allStagesReady(initialSummary);

  const [summary, setSummary] = useState<RunSummary | null>(initialSummary);
  const [phase, setPhase] = useState<"polling" | "complete" | "timeout">(() =>
    allStagesReady(initialSummary) ? "complete" : "polling",
  );

  useEffect(() => {
    setSummary(initialSummary);
    setPhase(allStagesReady(initialSummary) ? "complete" : "polling");
  }, [initialSummary]);

  useEffect(() => {
    if (!pollEnabled) {
      return;
    }

    let cancelled = false;
    const started = Date.now();
    let intervalId: ReturnType<typeof window.setInterval> | undefined;

    const stopInterval = () => {
      if (intervalId !== undefined) {
        window.clearInterval(intervalId);
        intervalId = undefined;
      }
    };

    const tick = async () => {
      if (cancelled) {
        return;
      }

      if (Date.now() - started > POLL_MAX_MS) {
        stopInterval();

        if (!cancelled) {
          setPhase("timeout");
        }

        return;
      }

      try {
        const next = await getRunSummary(runId);

        if (cancelled) {
          return;
        }

        setSummary(next);

        if (allStagesReady(next)) {
          stopInterval();
          setPhase("complete");
        }
      } catch {
        /* keep polling until timeout */
      }
    };

    void tick();
    intervalId = window.setInterval(() => void tick(), POLL_INTERVAL_MS);

    return () => {
      cancelled = true;
      stopInterval();
    };
  }, [runId, pollEnabled]);

  const ctx = stageDone(summary?.hasContextSnapshot);
  const graph = stageDone(summary?.hasGraphSnapshot);
  const findings = stageDone(summary?.hasFindingsSnapshot);
  const manifest = stageDone(summary?.hasGoldenManifest);

  const completedStages = [ctx, graph, findings, manifest].filter(Boolean).length;
  const progressValue = (completedStages / 4) * 100;

  const liveStatus = useMemo(() => {
    if (phase === "complete") {
      return "Pipeline complete — refresh for full detail.";
    }

    if (phase === "timeout") {
      return "Pipeline still in progress. Refresh manually to check.";
    }

    return `${completedStages} of 4 authority pipeline stages complete.`;
  }, [phase, completedStages]);

  if (!pollEnabled) {
    return null;
  }

  return (
    <section
      className="mb-6 rounded-lg border border-neutral-200 bg-neutral-50 p-4 dark:border-neutral-700 dark:bg-neutral-900/40"
      aria-labelledby="run-progress-tracker-title"
    >
      <h3 id="run-progress-tracker-title" className="mt-0 text-base font-semibold">
        Pipeline progress
      </h3>
      <p className="text-sm text-neutral-600 dark:text-neutral-400">
        <strong>Run id:</strong>{" "}
        <code className="rounded bg-neutral-100 px-1 py-0.5 text-xs dark:bg-neutral-800">{runId}</code>
      </p>

      <div aria-live="polite" aria-atomic="true" className="mt-3 text-sm text-neutral-800 dark:text-neutral-200">
        {liveStatus}
      </div>

      <div className="mt-4 space-y-2">
        <div className="flex justify-between text-xs text-neutral-500">
          <span>Pipeline progress</span>
          <span>{completedStages} / 4 stages</span>
        </div>
        <Progress value={progressValue} className="h-2" />
      </div>

      <Separator className="my-6" />

      <ul className="m-0 flex flex-col gap-3 p-0 list-none">
        <li className="flex flex-wrap items-center gap-2">
          <span className="w-28 text-sm font-medium">Context</span>
          <Badge variant={ctx ? "default" : "secondary"}>{ctx ? "Ready" : "Pending"}</Badge>
        </li>
        <li className="flex flex-wrap items-center gap-2">
          <span className="w-28 text-sm font-medium">Graph</span>
          <Badge variant={graph ? "default" : "secondary"}>{graph ? "Ready" : "Pending"}</Badge>
        </li>
        <li className="flex flex-wrap items-center gap-2">
          <span className="w-28 text-sm font-medium">Findings</span>
          <Badge variant={findings ? "default" : "secondary"}>{findings ? "Ready" : "Pending"}</Badge>
        </li>
        <li className="flex flex-wrap items-center gap-2">
          <span className="w-28 text-sm font-medium">Manifest</span>
          <Badge variant={manifest ? "default" : "secondary"}>{manifest ? "Ready" : "Pending"}</Badge>
        </li>
      </ul>

      {summary?.description ? (
        <p className="mt-4 text-sm text-neutral-700 dark:text-neutral-300">{summary.description}</p>
      ) : null}
    </section>
  );
}
