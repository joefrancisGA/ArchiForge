"use client";

/**
 * Lightweight stand-in when the graph canvas has not loaded — node-kind key and sample nodes
 * so the page does not read as an empty error state.
 */
export function GraphIdleLegend() {
  return (
    <div
      className="mb-6 max-w-4xl rounded-lg border border-dashed border-teal-300/70 bg-teal-50/40 p-4 dark:border-teal-800/60 dark:bg-teal-950/25"
      data-testid="graph-idle-legend"
    >
      <p className="m-0 text-sm font-semibold text-neutral-900 dark:text-neutral-100">Graph preview (sample)</p>
      <p className="m-0 mt-1 max-w-prose text-xs text-neutral-600 dark:text-neutral-400">
        Loaded graphs represent <strong>decisions</strong>, <strong>findings</strong>, <strong>artifacts</strong>,{" "}
        <strong>review trail events</strong>, and <strong>architecture components</strong> as nodes, with edges for
        provenance and flow. Use the review and mode controls to load or refresh the graph.
      </p>
      <ul className="m-0 mt-3 flex flex-wrap gap-2 p-0 list-none">
        {[
          { k: "Decision", c: "bg-blue-100 text-blue-900 dark:bg-blue-950/50 dark:text-blue-200" },
          { k: "Finding", c: "bg-amber-100 text-amber-950 dark:bg-amber-950/40 dark:text-amber-100" },
          { k: "Artifact", c: "bg-violet-100 text-violet-950 dark:bg-violet-950/40 dark:text-violet-100" },
          { k: "Review", c: "bg-teal-100 text-teal-950 dark:bg-teal-950/40 dark:text-teal-100" },
          { k: "Component", c: "bg-neutral-200 text-neutral-900 dark:bg-neutral-800 dark:text-neutral-100" },
        ].map((x) => (
          <li
            key={x.k}
            className={`inline-flex rounded-full px-2.5 py-0.5 text-[10px] font-semibold uppercase tracking-wide ${x.c}`}
          >
            {x.k}
          </li>
        ))}
      </ul>
      <div className="mt-3 flex flex-wrap items-center gap-3 text-[11px] text-neutral-500 dark:text-neutral-400">
        <span className="rounded border border-neutral-300 bg-white px-2 py-1 dark:border-neutral-600 dark:bg-neutral-900">Review</span>
        <span aria-hidden>→</span>
        <span className="rounded border border-neutral-300 bg-white px-2 py-1 dark:border-neutral-600 dark:bg-neutral-900">Context</span>
        <span aria-hidden>→</span>
        <span className="rounded border border-neutral-300 bg-white px-2 py-1 dark:border-neutral-600 dark:bg-neutral-900">…</span>
      </div>
    </div>
  );
}

export const GRAPH_MODE_NATIVE_TITLES: Record<string, string> = {
  "provenance-full": "Full review-trail graph: decisions, findings, artifacts, review events, and linkage to the review.",
  "decision-subgraph": "Neighborhood focused on one decision id and its connected evidence.",
  "node-neighborhood": "Expand a specific graph node by id up to the selected depth.",
  architecture:
    "Architecture-oriented graph: components and relationships (may paginate on reviews with very large evidence graphs).",
};
