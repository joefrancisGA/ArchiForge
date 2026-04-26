import Link from "next/link";

import { GettingStartedTrialSection } from "@/components/GettingStartedTrialSection";
import { OperatorFirstRunWorkflowPanel } from "@/components/OperatorFirstRunWorkflowPanel";

type GettingStartedPageProps = {
  searchParams: Promise<{ source?: string }>;
};

/**
 * Same checklist as Home — single FTUE entry for “getting started” without duplicating a separate static list.
 * Post-registration handoff: `/getting-started?source=registration` (or legacy redirects) shows the trial card above.
 */
export default async function GettingStartedPage({ searchParams }: GettingStartedPageProps) {
  const p = await searchParams;
  const fromRegistration = p.source === "registration";

  return (
    <main className="mx-auto max-w-3xl px-1 sm:px-0">
      <h2 className="mt-0 text-2xl font-semibold tracking-tight">Getting started</h2>
      <p className="text-sm text-neutral-600 dark:text-neutral-400">
        Use the checklist below (same as <Link href="/">Home</Link>
        ). For architecture background, see the repository{" "}
        <code className="rounded bg-neutral-100 px-1 text-xs dark:bg-neutral-800">docs/START_HERE.md</code>.
      </p>
      <p className="mt-3 text-sm">
        <Link
          className="font-medium text-teal-800 underline dark:text-teal-300"
          href="/"
          title="Home — Advanced Analysis and Enterprise sections when you expand past the first-manifest path"
        >
          Week-one orientation (home overview)
        </Link>{" "}
        — same three layers and links as the operator home page when you are ready for more than the V1 path.
      </p>
      <GettingStartedTrialSection fromRegistrationQuery={fromRegistration} />
      <div className="mt-8">
        <OperatorFirstRunWorkflowPanel />
      </div>
      <div id="creating-runs" className="sr-only" aria-hidden>
        Anchor for help deep links.
      </div>
      <div id="alerts" className="sr-only" aria-hidden>
        Anchor for help deep links.
      </div>
      <div id="governance" className="sr-only" aria-hidden>
        Anchor for help deep links.
      </div>
    </main>
  );
}
