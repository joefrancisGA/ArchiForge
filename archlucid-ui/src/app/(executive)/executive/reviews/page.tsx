import Link from "next/link";

import { listRunsByProjectPaged } from "@/lib/api";
import { isApiRequestError } from "@/lib/api-request-error";
import { tryStaticDemoRunSummariesPaged } from "@/lib/operator-static-demo";
import type { RunSummary } from "@/types/authority";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

function runHeadline(run: RunSummary): string {
  const d = (run.description ?? "").trim();

  if (d.length > 0) {
    return d;
  }

  return run.runId;
}

function isFinalizedReview(run: RunSummary): boolean {
  return run.hasGoldenManifest === true || (run.goldenManifestId?.trim().length ?? 0) > 0;
}

/**
 * Executive entry: finalized architecture reviews only (committed manifest).
 */
export default async function ExecutiveReviewsPage() {
  let runs: RunSummary[] = [];
  let loadError: string | null = null;

  try {
    const page = await listRunsByProjectPaged("default", 1, 40);
    runs = (page.items ?? []).filter(isFinalizedReview);
  } catch (e) {
    if (isApiRequestError(e) && e.httpStatus === 401) {
      loadError =
        "Sign in is required. Open Operator shell and sign in with your organization account, then return here.";
    } else if (isApiRequestError(e) && e.httpStatus === 403) {
      loadError = "You do not have access to list reviews for this workspace.";
    } else {
      loadError = e instanceof Error ? e.message : "Could not load reviews.";
    }
  }

  if (runs.length === 0 && loadError === null) {
    const demoFallback = tryStaticDemoRunSummariesPaged("default");

    if (demoFallback !== null) {
      runs = demoFallback.items.filter(isFinalizedReview);
    }
  }

  if (runs.length === 0 && loadError !== null) {
    const demoFallback = tryStaticDemoRunSummariesPaged("default", { afterAuthorityListFailure: true });

    if (demoFallback !== null) {
      runs = demoFallback.items.filter(isFinalizedReview);
      loadError = null;
    }
  }

  return (
    <div className="space-y-6">
      <header className="space-y-2">
        <p className="m-0 text-sm font-medium uppercase tracking-wide text-teal-800 dark:text-teal-300">
          Executive view
        </p>
        <h1 className="m-0 text-2xl font-semibold text-neutral-900 dark:text-neutral-100">
          Architecture risk reviews
        </h1>
        <p className="m-0 max-w-2xl text-sm leading-relaxed text-neutral-600 dark:text-neutral-400">
          Open a finalized review to see prioritized findings, evidence-linked detail, and export the architecture package.
        </p>
      </header>

      {loadError !== null ? (
        <Card className="border-amber-200 bg-amber-50/50 dark:border-amber-900 dark:bg-amber-950/30">
          <CardHeader className="pb-2">
            <CardTitle className="text-base text-neutral-900 dark:text-neutral-100">Could not load reviews</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            <p className="m-0 text-sm text-neutral-700 dark:text-neutral-300">{loadError}</p>
            <div className="flex flex-wrap gap-2">
              <Button asChild variant="default" size="sm">
                <Link href="/auth/signin">Sign in</Link>
              </Button>
              <Button asChild variant="outline" size="sm">
                <Link href="/">Open operator shell</Link>
              </Button>
            </div>
          </CardContent>
        </Card>
      ) : null}

      {loadError === null && runs.length === 0 ? (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">No finalized reviews yet</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            <p className="m-0 text-sm text-neutral-600 dark:text-neutral-400">
              Finalized reviews appear here after an operator completes the review and locks the architecture package.
            </p>
            <Button asChild variant="outline" size="sm">
              <Link href="/reviews/new">Start a review (operator shell)</Link>
            </Button>
          </CardContent>
        </Card>
      ) : null}

      {loadError === null && runs.length > 0 ? (
        <ul className="m-0 list-none space-y-3 p-0">
          {runs.map((run) => (
            <li key={run.runId}>
              <Card className="border border-neutral-200 shadow-sm dark:border-neutral-800">
                <CardHeader className="space-y-1 pb-2">
                  <CardTitle className="text-base font-semibold text-neutral-900 dark:text-neutral-100">
                    {runHeadline(run)}
                  </CardTitle>
                  <p className="m-0 text-xs text-neutral-500 dark:text-neutral-400">
                    Created {new Date(run.createdUtc).toLocaleString()} · {run.findingCount ?? "—"} findings
                  </p>
                </CardHeader>
                <CardContent className="pt-0">
                  <Button asChild variant="primary" size="sm">
                    <Link href={`/executive/reviews/${encodeURIComponent(run.runId)}`}>Open risk review</Link>
                  </Button>
                </CardContent>
              </Card>
            </li>
          ))}
        </ul>
      ) : null}
    </div>
  );
}
