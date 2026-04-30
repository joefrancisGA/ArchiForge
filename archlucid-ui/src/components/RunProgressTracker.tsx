"use client";

import { useEffect, useMemo, useState } from "react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Progress } from "@/components/ui/progress";
import { Separator } from "@/components/ui/separator";
import { useRunSummaryStream } from "@/hooks/useRunSummaryStream";
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

const POLL_MAX_MS = 180_000;

export function RunProgressTracker({ runId, initialSummary }: RunProgressTrackerProps) {
  const pollEnabled = !allStagesReady(initialSummary);

  const [pollSession, setPollSession] = useState(0);
  const [clientPhase, setClientPhase] = useState<"polling" | "complete" | "timeout">(() =>
    allStagesReady(initialSummary) ? "complete" : "polling",
  );

  const { summary, streamPhase, sseConnected } = useRunSummaryStream(runId, {
    enabled: pollEnabled && clientPhase === "polling",
    initialSummary,
    retryToken: pollSession,
  });

  useEffect(() => {
    if (!pollEnabled || clientPhase !== "polling") {
      return;
    }

    const timeoutId = window.setTimeout(() => {
      setClientPhase("timeout");
    }, POLL_MAX_MS);

    return () => {
      window.clearTimeout(timeoutId);
    };
  }, [pollEnabled, clientPhase, pollSession]);

  useEffect(() => {
    if (allStagesReady(summary) || streamPhase === "complete") {
      setClientPhase("complete");
    }
  }, [summary, streamPhase]);

  const ctx = stageDone(summary?.hasContextSnapshot);
  const graph = stageDone(summary?.hasGraphSnapshot);
  const findings = stageDone(summary?.hasFindingsSnapshot);
  const manifest = stageDone(summary?.hasGoldenManifest);

  const completedStages = [ctx, graph, findings, manifest].filter(Boolean).length;
  const progressValue = (completedStages / 4) * 100;

  const liveStatus = useMemo(() => {
    if (clientPhase === "complete") {
      return "Pipeline complete — refresh for full detail.";
    }

    if (clientPhase === "timeout") {
      return `Pipeline may still be running server-side (run ${runId}). Use Retry to watch for up to ~3 minutes, refresh this page, or check GET /health/ready on the API.`;
    }

    const transport = sseConnected ? "live stream" : "polling";

    return `${completedStages} of 4 run pipeline stages complete (${transport}).`;
  }, [clientPhase, completedStages, runId, sseConnected]);

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
        <strong>Run ID:</strong>{" "}
        <code className="rounded bg-neutral-100 px-1 py-0.5 text-xs dark:bg-neutral-800">{runId}</code>
      </p>

      <div aria-live="polite" aria-atomic="true" className="mt-3 text-sm text-neutral-800 dark:text-neutral-200">
        {liveStatus}
      </div>

      {clientPhase === "timeout" ? (
        <div className="mt-3">
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => {
              setClientPhase("polling");
              setPollSession((s) => s + 1);
            }}
          >
            Retry polling
          </Button>
        </div>
      ) : null}

      <div className="mt-4 space-y-2">
        <div className="flex justify-between text-xs text-neutral-500">
          <span>Pipeline progress</span>
          <span>{completedStages} / 4 stages</span>
        </div>
        <Progress
          value={progressValue}
          className="h-2"
          aria-label="Run pipeline stages completed"
        />
      </div>

      <Separator className="my-6" />

      <ul className="m-0 flex flex-col gap-3 p-0 list-none">
        <li className="flex flex-wrap items-center gap-2">
          <span className="w-36 text-sm font-medium">Context captured</span>
          <Badge variant={ctx ? "default" : "secondary"}>{ctx ? "Complete" : "Pending"}</Badge>
        </li>
        <li className="flex flex-wrap items-center gap-2">
          <span className="w-36 text-sm font-medium">Evidence graph ready</span>
          <Badge variant={graph ? "default" : "secondary"}>{graph ? "Complete" : "Pending"}</Badge>
        </li>
        <li className="flex flex-wrap items-center gap-2">
          <span className="w-36 text-sm font-medium">Findings complete</span>
          <Badge variant={findings ? "default" : "secondary"}>{findings ? "Complete" : "Pending"}</Badge>
        </li>
        <li className="flex flex-wrap items-center gap-2">
          <span className="w-36 text-sm font-medium">Manifest ready</span>
          <Badge variant={manifest ? "default" : "secondary"}>{manifest ? "Complete" : "Pending"}</Badge>
        </li>
      </ul>

      {summary?.description ? (
        <p className="mt-4 text-sm text-neutral-700 dark:text-neutral-300">{summary.description}</p>
      ) : null}
    </section>
  );
}
