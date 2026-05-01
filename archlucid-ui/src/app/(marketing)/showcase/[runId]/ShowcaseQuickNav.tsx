import Link from "next/link";
import type { ReactElement } from "react";

import type { DemoCommitPagePreviewResponse } from "@/types/demo-preview";
import { SHOWCASE_STATIC_DEMO_PRIMARY_FINDING_ID } from "@/lib/showcase-static-demo";

function primaryFindingIdForShowcase(payload: DemoCommitPagePreviewResponse): string {
  const rows = payload.runExplanation?.findingTraceConfidences;

  if (Array.isArray(rows)) {
    const withId = rows.find((r) => r.findingId?.trim());

    if (withId?.findingId?.trim()) {
      return withId.findingId.trim();
    }
  }

  return SHOWCASE_STATIC_DEMO_PRIMARY_FINDING_ID;
}

const btnClass =
  "inline-flex items-center justify-center rounded-md border border-teal-700 bg-white px-3 py-2 text-sm font-medium text-teal-900 no-underline hover:bg-teal-50 dark:border-teal-500/70 dark:bg-neutral-900 dark:text-teal-100 dark:hover:bg-teal-950/60";

/** Deep-links into the operator workspace for the loaded showcase example (auth may be required). */
export function ShowcaseQuickNav({ payload }: { readonly payload: DemoCommitPagePreviewResponse }): ReactElement {
  const runId = payload.run.runId;
  const manifestId = payload.manifest.manifestId;
  const findingId = primaryFindingIdForShowcase(payload);
  const findingHref = `/reviews/${encodeURIComponent(runId)}/findings/${encodeURIComponent(findingId)}`;

  return (
    <section
      aria-labelledby="showcase-quick-nav-heading"
      className="mt-6 rounded-lg border border-neutral-200 bg-white/90 p-4 shadow-sm dark:border-neutral-800 dark:bg-neutral-950/60"
    >
      <h2
        id="showcase-quick-nav-heading"
        className="m-0 text-base font-semibold text-neutral-900 dark:text-neutral-50"
      >
        Explore in workspace
      </h2>
      <p className="m-0 mt-1 text-xs text-neutral-600 dark:text-neutral-400">
        Sign in may be required. Same scenario as this public preview.
      </p>
      <div className="mt-3 flex flex-wrap gap-2">
        <Link href={`/reviews/${encodeURIComponent(runId)}`} className={btnClass}>
          Open review
        </Link>
        <Link href={`/manifests/${encodeURIComponent(manifestId)}`} className={btnClass}>
          Open manifest
        </Link>
        <Link href={findingHref} className={btnClass}>
          Review finding
        </Link>
      </div>
    </section>
  );
}
