"use client";

import Link from "next/link";

import { Badge } from "@/components/ui/badge";
import { Progress } from "@/components/ui/progress";
import { Separator } from "@/components/ui/separator";
import { WizardStepPanel } from "@/components/wizard/WizardStepPanel";
import type { RunSummary } from "@/types/authority";

export type WizardStepTrackProps = {
  runId: string;
  pollSummary: RunSummary | null;
};

function stageDone(flag: boolean | undefined): boolean {
  return flag === true;
}

/**
 * Step 7: poll run summary and visualize authority pipeline stages.
 */
export function WizardStepTrack({ runId, pollSummary }: WizardStepTrackProps) {
  const ctx = stageDone(pollSummary?.hasContextSnapshot);
  const graph = stageDone(pollSummary?.hasGraphSnapshot);
  const findings = stageDone(pollSummary?.hasFindingsSnapshot);
  const manifest = stageDone(pollSummary?.hasGoldenManifest);

  const completedStages = [ctx, graph, findings, manifest].filter(Boolean).length;
  const progressValue = (completedStages / 4) * 100;

  return (
    <WizardStepPanel
      title="Track pipeline"
      description="The authority chain runs asynchronously. This view polls run summary every few seconds."
    >
      <p className="text-sm text-neutral-600 dark:text-neutral-400">
        <strong>Run ID:</strong>{" "}
        <code className="rounded bg-neutral-100 px-1 py-0.5 text-xs dark:bg-neutral-800">{runId}</code>
      </p>

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

      {pollSummary?.description ? (
        <p className="mt-4 text-sm text-neutral-700 dark:text-neutral-300">{pollSummary.description}</p>
      ) : null}

      {manifest ? (
        <div className="mt-6 rounded-md border border-teal-200 bg-teal-50 p-4 dark:border-teal-900 dark:bg-teal-950/40">
          <p className="m-0 text-sm font-semibold text-teal-900 dark:text-teal-100">Golden manifest is available.</p>
          <nav className="mt-3 flex flex-wrap gap-x-3 gap-y-2 text-sm">
            <Link className="text-teal-800 underline dark:text-teal-200" href={`/runs/${runId}`}>
              Open run detail
            </Link>
            <Link
              className="text-teal-800 underline dark:text-teal-200"
              href={`/compare?leftRunId=${encodeURIComponent(runId)}`}
            >
              Compare runs
            </Link>
            <Link className="text-teal-800 underline dark:text-teal-200" href={`/runs/${runId}/provenance`}>
              View provenance
            </Link>
          </nav>
        </div>
      ) : (
        <p className="mt-4 text-xs text-neutral-500">Waiting for golden manifest… (poll continues up to 2 minutes.)</p>
      )}
    </WizardStepPanel>
  );
}
