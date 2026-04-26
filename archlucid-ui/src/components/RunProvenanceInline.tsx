import type { RunSummary } from "@/types/authority";

type StageKey = "context" | "graph" | "findings" | "manifest";

type StageDef = {
  key: StageKey;
  label: string;
  present: boolean;
};

function stagesForRun(run: RunSummary): StageDef[] {
  return [
    { key: "context", label: "Context snapshot", present: run.hasContextSnapshot === true },
    { key: "graph", label: "Graph snapshot", present: run.hasGraphSnapshot === true },
    { key: "findings", label: "Findings snapshot", present: run.hasFindingsSnapshot === true },
    { key: "manifest", label: "Golden manifest", present: run.hasGoldenManifest === true },
  ];
}

export type RunProvenanceInlineProps = {
  run: RunSummary;
};

/**
 * Compact pipeline-stage strip for dense run rows (context → graph → findings → manifest).
 */
export function RunProvenanceInline({ run }: RunProvenanceInlineProps) {
  const stages = stagesForRun(run);

  return (
    <ul
      className="m-0 flex list-none items-center gap-1.5 p-0"
      aria-label="Pipeline artifact progress"
      data-testid="run-provenance-inline"
    >
      {stages.map((stage) => (
        <li key={stage.key} className="flex items-center" title={stage.label}>
          <span
            className={
              stage.present
                ? "block h-1.5 w-1.5 rounded-full bg-teal-600 dark:bg-teal-400"
                : "block h-1.5 w-1.5 rounded-full border border-neutral-300 bg-transparent dark:border-neutral-600"
            }
            aria-label={stage.present ? `${stage.label}: present` : `${stage.label}: not yet present`}
          />
        </li>
      ))}
    </ul>
  );
}
