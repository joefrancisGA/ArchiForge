"use client";

import Link from "next/link";
import { useEffect } from "react";

import { OperatorErrorUiReferenceLine } from "@/components/OperatorErrorUiReferenceLine";
import { OperatorErrorCallout } from "@/components/OperatorShellMessage";
import { CopyIdButton } from "@/components/CopyIdButton";
import { Button } from "@/components/ui/button";
import { reportClientError } from "@/lib/error-telemetry";

/**
 * Segment error boundary for `/runs` so list/split layout failures stay scoped and surface a recovery path
 * without swapping the entire operator shell chrome.
 */
export default function RunsSegmentError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    reportClientError(error, { source: "runs-segment-error-boundary", digest: error.digest ?? "" });
  }, [error]);

  const digest = error.digest?.trim() ?? "";
  const isDev = process.env.NODE_ENV === "development";

  return (
    <main className="mx-auto max-w-lg space-y-4 px-4 py-8">
      <OperatorErrorCallout>
        <strong className="text-base">Reviews could not load</strong>
        <p className="mt-2 text-sm">
          {isDev
            ? "Development build — technical details appear below."
            : "This reviews view hit an unexpected error. You can retry, return to reviews, go home, or open Help."}
        </p>
        {isDev ? (
          <pre
            className="mt-3 max-h-40 overflow-auto rounded border border-neutral-200 bg-neutral-50 p-2 text-xs text-neutral-800 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-200"
            style={{ whiteSpace: "pre-wrap" }}
          >
            {error.message}
          </pre>
        ) : null}
        <OperatorErrorUiReferenceLine />
        {digest.length > 0 ? (
          <div className="mt-2 flex flex-wrap items-center gap-2">
            <p className="m-0 flex min-w-0 flex-1 flex-wrap items-center gap-1 font-mono text-[11px] text-neutral-600 dark:text-neutral-400">
              <span className="shrink-0">Next.js digest (optional):</span>
              <code className="break-all rounded bg-neutral-100 px-1 py-0.5 font-mono dark:bg-neutral-800">{digest}</code>
            </p>
            <CopyIdButton value={digest} aria-label="Copy Next.js diagnostic digest" />
          </div>
        ) : null}
      </OperatorErrorCallout>
      <div className="flex flex-wrap items-center gap-2">
        <Button type="button" variant="primary" onClick={() => reset()}>
          Retry
        </Button>
        <Button type="button" variant="outline" asChild>
          <Link href="/reviews?projectId=default">Back to reviews</Link>
        </Button>
        <Button type="button" variant="outline" asChild>
          <Link href="/">Home</Link>
        </Button>
        <Button type="button" variant="outline" asChild>
          <Link href="/help">Help</Link>
        </Button>
      </div>
    </main>
  );
}
