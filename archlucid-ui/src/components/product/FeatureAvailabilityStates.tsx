import Link from "next/link";

import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";

/**
 * Full-width empty state for routes that are not available in the current workspace (demo or tier).
 */
export function FeatureNotAvailable() {
  return (
    <Card
      className="border-teal-200/80 bg-teal-50/40 dark:border-teal-900/50 dark:bg-teal-950/20"
      data-testid="feature-not-available"
    >
      <CardContent className="px-6 py-8 text-center">
        <h2 className="m-0 text-lg font-semibold text-neutral-900 dark:text-neutral-100">
          This feature is not enabled in this workspace
        </h2>
        <p className="mx-auto mt-2 max-w-md text-sm text-neutral-600 dark:text-neutral-400">
          Contact your administrator to enable it, or explore what is available from the navigation.
        </p>
        <Button asChild className="mt-4 bg-teal-600 hover:bg-teal-700" type="button">
          <Link href="/">Return home</Link>
        </Button>
      </CardContent>
    </Card>
  );
}

/**
 * CTA for views that require a finalized run first.
 */
export function AvailableAfterFirstRun() {
  return (
    <Card
      className="border-teal-200/80 bg-teal-50/40 dark:border-teal-900/50 dark:bg-teal-950/20"
      data-testid="available-after-first-run"
    >
      <CardContent className="px-6 py-8 text-center">
        <h2 className="m-0 text-lg font-semibold text-neutral-900 dark:text-neutral-100">
          Available after your first finalized run
        </h2>
        <p className="mx-auto mt-2 max-w-md text-sm text-neutral-600 dark:text-neutral-400">
          Complete an architecture request and finalize the run to unlock this view.
        </p>
        <Button asChild className="mt-4 bg-teal-600 hover:bg-teal-700" type="button">
          <Link href="/reviews/new">Start a new request</Link>
        </Button>
      </CardContent>
    </Card>
  );
}
