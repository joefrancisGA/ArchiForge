import type { PipelineTimelineItem } from "@/types/authority";

type AuthorityPipelineTimelineProps = {
  items: PipelineTimelineItem[] | null;
  /** When set, show a short operator-facing message instead of the table. */
  loadErrorMessage?: string | null;
};

/** Read-only vertical timeline of audit events for one run (oldest first). */
export function AuthorityPipelineTimeline({
  items,
  loadErrorMessage,
}: AuthorityPipelineTimelineProps) {
  if (loadErrorMessage) {
    return (
      <p className="mt-0 text-sm text-amber-700 dark:text-amber-400">
        Pipeline timeline could not be loaded: {loadErrorMessage}
      </p>
    );
  }

  if (items === null) {
    return (
      <p className="mt-0 text-sm text-neutral-500 dark:text-neutral-400">
        Pipeline timeline not loaded.
      </p>
    );
  }

  if (items.length === 0) {
    return (
      <p className="mt-0 text-sm text-neutral-500 dark:text-neutral-400">
        No events recorded yet for this run.
      </p>
    );
  }

  return (
    <ol className="m-0 max-w-3xl list-none space-y-0 pl-0 text-sm leading-relaxed text-neutral-700 dark:text-neutral-300">
      {items.map((row) => (
        <li key={row.eventId} className="relative border-s-2 border-neutral-200 pb-6 ps-6 last:border-s-transparent last:pb-0 dark:border-neutral-700">
          <span
            className="absolute -start-[5px] top-1.5 size-2 rounded-full bg-teal-600 dark:bg-teal-400"
            aria-hidden
          />
          <div className="flex flex-col gap-1 pt-0.5">
            <span className="text-xs font-medium text-neutral-500 dark:text-neutral-400">
              {new Date(row.occurredUtc).toLocaleString()}
            </span>
            <span className="font-medium text-neutral-900 dark:text-neutral-100">
              {pipelineEventTypeFriendlyLabel(row.eventType)}
            </span>
            <span className="text-neutral-700 dark:text-neutral-300">{row.actorUserName}</span>
            <details className="mt-1 text-xs text-neutral-500 dark:text-neutral-400">
              <summary className="cursor-pointer select-none text-teal-800 underline dark:text-teal-300">
                Technical details
              </summary>
              <div className="mt-2 space-y-1 border-s border-neutral-200 ps-3 dark:border-neutral-700">
                <p className="m-0">
                  <span className="font-medium text-neutral-600 dark:text-neutral-400">Event type:</span>{" "}
                  <code className="text-[12px]">{row.eventType}</code>
                </p>
                {row.correlationId ? (
                  <p className="m-0">
                    <span className="font-medium text-neutral-600 dark:text-neutral-400">Correlation:</span>{" "}
                    {row.correlationId}
                  </p>
                ) : null}
              </div>
            </details>
          </div>
        </li>
      ))}
    </ol>
  );
}

/** Maps API timeline event codes to reviewer-facing labels (falls back to the raw code). */
export function pipelineEventTypeFriendlyLabel(eventType: string): string {
  const key = eventType.trim();

  const map: Record<string, string> = {
    RunStarted: "Run started",
    RunCompleted: "Run completed",
    "finalize.run": "Manifest finalized",
    "run.finalized": "Run finalized",
    "context.snapshot.created": "Context captured",
    "graph.snapshot.created": "Architecture graph created",
    "findings.snapshot.created": "Findings generated",
    "manifest.committed": "Governed manifest committed",
    "artifact.bundle.created": "Artifacts bundled",
    "audit.pipeline.step": "Pipeline step recorded",
    Commit: "Changes committed",
    context_snapshot: "Context captured",
    graph_snapshot: "Architecture graph created",
    findings_snapshot: "Findings generated",
  };

  return map[key] ?? key;
}
