import type { RunSummary } from "@/types/authority";

import { cn } from "@/lib/utils";

type StageKey = "context" | "graph" | "findings" | "manifest";

type StageDef = {
  key: StageKey;
  label: string;
  present: boolean;
};

function stagesForRun(run: RunSummary): StageDef[] {
  return [
    { key: "context", label: "Context", present: run.hasContextSnapshot === true },
    { key: "graph", label: "Graph", present: run.hasGraphSnapshot === true },
    { key: "findings", label: "Findings", present: run.hasFindingsSnapshot === true },
    { key: "manifest", label: "Manifest", present: run.hasGoldenManifest === true },
  ];
}

export type RunProvenanceInlineProps = {
  run: RunSummary;
};

/**
 * Compact pipeline-stage strip for dense run rows (context → graph → findings → manifest) as readable pill chips.
 */
export function RunProvenanceInline({ run }: RunProvenanceInlineProps) {
  const stages = stagesForRun(run);
  const presentCount = stages.filter((s) => s.present).length;

  return (
    <div className="flex flex-wrap items-center gap-x-2 gap-y-1">
      <ul
        className="m-0 flex list-none flex-wrap gap-1 p-0"
        aria-label="Review trail status"
        data-testid="run-provenance-inline"
      >
        {stages.map((stage) => (
          <li key={stage.key}>
            <span
              title={`${stage.label}: ${stage.present ? "present" : "not yet present"}`}
              className={cn(
                "inline-flex items-center rounded-full border px-2 py-px text-[10px] font-semibold uppercase tracking-wide",
                stage.present
                  ? "border-teal-600 bg-teal-50 text-teal-900 dark:border-teal-500 dark:bg-teal-950/40 dark:text-teal-50"
                  : "border-neutral-300 bg-white text-neutral-500 dark:border-neutral-600 dark:bg-neutral-950 dark:text-neutral-400",
              )}
            >
              {stage.label}
              {stage.present ? " · ok" : " · …"}
            </span>
          </li>
        ))}
      </ul>
      <span
        className="text-[11px] text-neutral-600 dark:text-neutral-400"
        data-testid="run-provenance-inline-summary"
      >
        Review trail {presentCount}/{stages.length} complete
      </span>
    </div>
  );
}
