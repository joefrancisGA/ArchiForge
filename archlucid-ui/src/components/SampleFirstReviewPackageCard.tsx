"use client";

import Link from "next/link";

import { Button } from "@/components/ui/button";
import { recordCorePilotRailChecklistStep } from "@/lib/core-pilot-rail-telemetry";
import {
  SHOWCASE_STATIC_DEMO_RUN_ID,
  SHOWCASE_STATIC_DEMO_SPINE_COUNTS,
} from "@/lib/showcase-static-demo";

const sampleReviewHref = `/reviews/${encodeURIComponent(SHOWCASE_STATIC_DEMO_RUN_ID)}`;

/** First-session shortcut: opens the curated sample review package before the real-input wizard. */
export function SampleFirstReviewPackageCard() {
  function recordSampleOpened(): void {
    recordCorePilotRailChecklistStep(3);
  }

  return (
    <section
      aria-labelledby="sample-first-review-heading"
      className="rounded-xl border border-teal-200 bg-white p-4 shadow-sm dark:border-teal-900 dark:bg-neutral-950"
    >
      <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
        <div className="min-w-0">
          <p className="m-0 text-xs font-semibold uppercase tracking-wide text-teal-700 dark:text-teal-300">
            Zero-config sample
          </p>
          <h2 id="sample-first-review-heading" className="m-0 mt-1 text-lg font-semibold text-neutral-900 dark:text-neutral-50">
            Start with a completed architecture review package
          </h2>
          <p className="m-0 mt-2 max-w-2xl text-sm leading-relaxed text-neutral-600 dark:text-neutral-400">
            Open the Claims Intake sample to see the reviewed manifest, evidence trail, findings, and artifacts before
            filling out the real-input wizard.
          </p>
          <p className="m-0 mt-2 text-xs text-amber-800 dark:text-amber-300">
            Illustrative sample review — use it to understand output shape, not as customer ROI evidence.
          </p>
        </div>

        <div className="shrink-0 space-y-3 lg:min-w-64">
          <dl className="grid grid-cols-3 gap-2 text-center">
            <div className="rounded-lg border border-neutral-200 px-2 py-2 dark:border-neutral-800">
              <dt className="text-[11px] text-neutral-500 dark:text-neutral-400">Findings</dt>
              <dd className="m-0 text-lg font-semibold text-neutral-900 dark:text-neutral-50">
                {SHOWCASE_STATIC_DEMO_SPINE_COUNTS.findingCount}
              </dd>
            </div>
            <div className="rounded-lg border border-neutral-200 px-2 py-2 dark:border-neutral-800">
              <dt className="text-[11px] text-neutral-500 dark:text-neutral-400">Decisions</dt>
              <dd className="m-0 text-lg font-semibold text-neutral-900 dark:text-neutral-50">
                {SHOWCASE_STATIC_DEMO_SPINE_COUNTS.decisionCount}
              </dd>
            </div>
            <div className="rounded-lg border border-neutral-200 px-2 py-2 dark:border-neutral-800">
              <dt className="text-[11px] text-neutral-500 dark:text-neutral-400">Warnings</dt>
              <dd className="m-0 text-lg font-semibold text-neutral-900 dark:text-neutral-50">
                {SHOWCASE_STATIC_DEMO_SPINE_COUNTS.warningCount}
              </dd>
            </div>
          </dl>

          <div className="flex flex-wrap gap-2">
            <Button asChild variant="primary" className="h-9">
              <Link href={sampleReviewHref} onClick={recordSampleOpened}>
                Start with sample review
              </Link>
            </Button>
            <Button asChild variant="outline" className="h-9">
              <Link href="/reviews/new">Use my own input</Link>
            </Button>
          </div>
        </div>
      </div>
    </section>
  );
}
