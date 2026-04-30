import Link from "next/link";

import { CorePilotOneSessionChecklist } from "@/components/CorePilotOneSessionChecklist";
import { GettingStartedTrialSection } from "@/components/GettingStartedTrialSection";
import { OperatorFirstRunWorkflowPanel } from "@/components/OperatorFirstRunWorkflowPanel";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { TRIAL_ONBOARDING_SAMPLE_RUN_ID } from "@/lib/trial-sample-run";

type GettingStartedPageProps = {
  searchParams: Promise<{ source?: string }>;
};

/**
 * First-time orientation: product journey steps, then the detailed checklist. Post-registration: trial card when
 * `?source=registration` is set.
 */
export default async function GettingStartedPage({ searchParams }: GettingStartedPageProps) {
  const p = await searchParams;
  const fromRegistration = p.source === "registration";
  const sampleNewRunHref = `/runs/new?sampleRunId=${encodeURIComponent(TRIAL_ONBOARDING_SAMPLE_RUN_ID)}`;

  return (
    <main className="mx-auto max-w-3xl space-y-8 px-1 sm:px-0">
      <h1 className="m-0 text-2xl font-semibold tracking-tight text-neutral-900 dark:text-neutral-100">
        Getting started
      </h1>
      <p className="m-0 text-sm text-neutral-600 dark:text-neutral-400 max-w-prose">
        Follow the milestones below, then use the detailed checklist. For the full home overview, go to{" "}
        <Link className="font-medium text-teal-800 underline dark:text-teal-300" href="/">
          Home
        </Link>
        .
      </p>
      <p className="m-0 text-sm text-neutral-600 dark:text-neutral-400 max-w-prose">
        Prefer proof before wiring your own tenant?{" "}
        <Link className="font-medium text-teal-800 underline dark:text-teal-300" href="/demo/explain">
          See a live run — citations &amp; provenance
        </Link>
        .
      </p>
      <CorePilotOneSessionChecklist />
      <GettingStartedTrialSection fromRegistrationQuery={fromRegistration} />
      <section aria-labelledby="getting-started-journey" className="space-y-4">
        <h2
          id="getting-started-journey"
          className="m-0 text-lg font-semibold text-neutral-900 dark:text-neutral-100"
        >
          Your first architecture analysis (four milestones)
        </h2>
        <ol className="m-0 list-none space-y-3 p-0">
          <li>
            <Card className="border border-neutral-200 dark:border-neutral-800">
              <CardHeader>
                <CardTitle className="text-base">1. Create a request</CardTitle>
              </CardHeader>
              <CardContent className="space-y-2 text-sm text-neutral-600 dark:text-neutral-400">
                <p className="m-0">Describe your architecture scenario in the new-request wizard.</p>
                <p className="m-0 flex flex-wrap gap-x-3 gap-y-2">
                  <Link className="font-medium text-teal-800 underline dark:text-teal-300" href="/runs/new">
                    Open new request
                  </Link>
                  <Link
                    className="font-medium text-teal-800 underline dark:text-teal-300"
                    data-testid="getting-started-use-sample-preset"
                    href={sampleNewRunHref}
                  >
                    Use a sample
                  </Link>
                </p>
              </CardContent>
            </Card>
          </li>
          <li>
            <Card className="border border-neutral-200 dark:border-neutral-800">
              <CardHeader>
                <CardTitle className="text-base">2. Track the run</CardTitle>
              </CardHeader>
              <CardContent className="space-y-2 text-sm text-neutral-600 dark:text-neutral-400">
                <p className="m-0">
                  The platform runs context ingestion, graph, findings, and manifest steps. Watch progress on the run
                  detail page and from the runs list.
                </p>
                <p className="m-0">
                  <Link
                    className="font-medium text-teal-800 underline dark:text-teal-300"
                    href="/runs?projectId=default"
                  >
                    View runs
                  </Link>
                </p>
              </CardContent>
            </Card>
          </li>
          <li>
            <Card className="border border-neutral-200 dark:border-neutral-800">
              <CardHeader>
                <CardTitle className="text-base">3. Review manifest &amp; findings</CardTitle>
              </CardHeader>
              <CardContent className="space-y-2 text-sm text-neutral-600 dark:text-neutral-400">
                <p className="m-0">
                  After the run finalizes, open the architecture manifest for decisions and warnings, then review
                  severity-ranked findings with rationale on the same run.
                </p>
                <p className="m-0">Use run detail first; governance-area Findings when your workspace is ready.</p>
              </CardContent>
            </Card>
          </li>
          <li>
            <Card className="border border-neutral-200 dark:border-neutral-800">
              <CardHeader>
                <CardTitle className="text-base">4. Download artifacts</CardTitle>
              </CardHeader>
              <CardContent className="space-y-2 text-sm text-neutral-600 dark:text-neutral-400">
                <p className="m-0">
                  Export generated documents, the bundle ZIP, and the traceability bundle from run detail when the
                  manifest is available.
                </p>
              </CardContent>
            </Card>
          </li>
        </ol>
      </section>
      <div>
        <h2 className="m-0 mb-3 text-lg font-semibold text-neutral-900 dark:text-neutral-100">Detailed checklist</h2>
        <OperatorFirstRunWorkflowPanel />
      </div>
    </main>
  );
}
