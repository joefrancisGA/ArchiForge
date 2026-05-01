"use client";

import Link from "next/link";

import type { CitationReference } from "@/types/explanation";

export type CitationChipsProps = {
  citations: CitationReference[] | undefined;
  runId: string;
};

function citationHref(c: CitationReference, runId: string): string {
  switch (c.kind) {
    case "Manifest":
      return `/manifests/${encodeURIComponent(c.id)}`;
    case "Finding":
      return `/reviews/${encodeURIComponent(runId)}#finding-${encodeURIComponent(c.id)}`;
    case "DecisionTrace":
    case "GraphSnapshot":
    case "ContextSnapshot":
      return `/reviews/${encodeURIComponent(runId)}/provenance`;
    case "EvidenceBundle":
      return `/reviews/${encodeURIComponent(runId)}`;
    default:
      return `/reviews/${encodeURIComponent(runId)}`;
  }
}

/** Renders persisted artifact links backing aggregate explanation narratives. */
export function CitationChips({ citations, runId }: CitationChipsProps) {
  if (!citations || citations.length === 0) {
    return null;
  }

  return (
    <div className="mb-3">
      <h4 className="mb-1.5 text-[13px] font-semibold text-neutral-700 dark:text-neutral-300">Citations</h4>
      <ul className="m-0 flex list-none flex-wrap gap-2 p-0">
        {citations.map((c) => {
          const href = citationHref(c, runId);
          return (
            <li key={`${c.kind}-${c.id}`}>
              <Link
                href={href}
                className="inline-block rounded-md border border-neutral-200 bg-neutral-50 px-2 py-1 text-xs font-medium text-neutral-800 hover:bg-neutral-100 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-100 dark:hover:bg-neutral-800"
                aria-label={`Citation ${c.kind}: ${c.label}`}
              >
                <span className="text-neutral-500 dark:text-neutral-400">{c.kind}</span> · {c.label}
              </Link>
            </li>
          );
        })}
      </ul>
    </div>
  );
}
