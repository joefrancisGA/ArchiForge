import Link from "next/link";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

/**
 * Static, immediately-rendered product help (no fetch). Developer doc index is secondary in HelpDocsClient.
 */
export function HelpProductGuide() {
  return (
    <div className="space-y-4" aria-labelledby="help-product-guide-heading">
      <h2
        id="help-product-guide-heading"
        className="m-0 text-lg font-semibold text-neutral-900 dark:text-neutral-100"
      >
        Using ArchLucid
      </h2>
      <p className="m-0 text-sm text-neutral-600 dark:text-neutral-400">
        ArchLucid turns a request into a run, then produces a finalized manifest, findings, artifacts, and a review
        trail.
      </p>

      <div className="grid gap-4 sm:grid-cols-1 md:grid-cols-2">
        <Card className="border border-teal-200/80 bg-white/90 shadow-sm dark:border-teal-900/50 dark:bg-neutral-950/80">
          <CardHeader className="pb-2">
            <CardTitle className="text-base">First request</CardTitle>
          </CardHeader>
          <CardContent className="pt-0 text-sm text-neutral-800 dark:text-neutral-200">
            Use{" "}
            <Link className="text-teal-700 underline dark:text-teal-300" href="/reviews/new">
              New request
            </Link>{" "}
            to describe your system and start the pipeline. You can use an industry starter or a blank brief.
          </CardContent>
        </Card>

        <Card className="border border-teal-200/80 bg-white/90 shadow-sm dark:border-teal-900/50 dark:bg-neutral-950/80">
          <CardHeader className="pb-2">
            <CardTitle className="text-base">Requests and runs</CardTitle>
          </CardHeader>
          <CardContent className="pt-0 text-sm text-neutral-800 dark:text-neutral-200">
            Each submission creates a <strong>run</strong>. Track progress on the run detail page until the manifest is
            ready to finalize.
          </CardContent>
        </Card>

        <Card className="border border-teal-200/80 bg-white/90 shadow-sm dark:border-teal-900/50 dark:bg-neutral-950/80">
          <CardHeader className="pb-2">
            <CardTitle className="text-base">Finalized manifests</CardTitle>
          </CardHeader>
          <CardContent className="pt-0 text-sm text-neutral-800 dark:text-neutral-200">
            After you <strong>finalize</strong>, the run has a versioned, reviewed <strong>manifest</strong>{" "}
            (architecture record). Open it from the run or the manifests list.
          </CardContent>
        </Card>

        <Card className="border border-teal-200/80 bg-white/90 shadow-sm dark:border-teal-900/50 dark:bg-neutral-950/80">
          <CardHeader className="pb-2">
            <CardTitle className="text-base">Findings</CardTitle>
          </CardHeader>
          <CardContent className="pt-0 text-sm text-neutral-800 dark:text-neutral-200">
            Issues and recommendations are surfaced with severity, rationale, and suggested actions. Resolve or accept
            them in context on the run or governance views.
          </CardContent>
        </Card>

        <Card className="border border-teal-200/80 bg-white/90 shadow-sm dark:border-teal-900/50 dark:bg-neutral-950/80">
          <CardHeader className="pb-2">
            <CardTitle className="text-base">Artifacts and review trail</CardTitle>
          </CardHeader>
          <CardContent className="pt-0 text-sm text-neutral-800 dark:text-neutral-200">
            Generated outputs and the <strong>review trail</strong> show what was produced and how decisions were
            recorded.
          </CardContent>
        </Card>

        <Card className="border border-teal-200/80 bg-white/90 shadow-sm dark:border-teal-900/50 dark:bg-neutral-950/80">
          <CardHeader className="pb-2">
            <CardTitle className="text-base">Troubleshooting</CardTitle>
          </CardHeader>
          <CardContent className="pt-0 text-sm text-neutral-800 dark:text-neutral-200">
            If a page fails to load, refresh once; for sign-in issues, return to{" "}
            <Link className="text-teal-700 underline dark:text-teal-300" href="/auth/signin">
              Sign in
            </Link>
            .
          </CardContent>
        </Card>
      </div>

      <p className="m-0 text-sm text-neutral-600 dark:text-neutral-400">
        <strong>What to do next:</strong> open{" "}
        <Link className="text-teal-700 underline dark:text-teal-300" href="/reviews/new">
          New request
        </Link>{" "}
        or your{" "}
        <Link className="text-teal-700 underline dark:text-teal-300" href="/reviews?projectId=default">
          Runs
        </Link>{" "}
        list to continue.
      </p>
    </div>
  );
}
