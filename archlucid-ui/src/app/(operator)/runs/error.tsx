"use client";

import Link from "next/link";
import { useEffect } from "react";

import { OperatorErrorUiReferenceLine } from "@/components/OperatorErrorUiReferenceLine";
import { OperatorErrorCallout } from "@/components/OperatorShellMessage";
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
        <strong className="text-base">Runs list could not render</strong>
        <p className="mt-2 text-sm">
          {isDev
            ? "Development build — technical details appear below."
            : "This runs view hit an unexpected client error. You can retry, return home, or ask for help."}
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
          <p className="mt-2 font-mono text-[11px] text-neutral-600 dark:text-neutral-400">
            Next.js digest (optional):{" "}
            <code className="rounded bg-neutral-100 px-1 py-0.5 font-mono dark:bg-neutral-800">{digest}</code>
          </p>
        ) : null}
      </OperatorErrorCallout>
      <div className="flex flex-wrap items-center gap-2">
        <Button type="button" variant="primary" onClick={() => reset()}>
          Retry
        </Button>
        <Button type="button" variant="outline" asChild>
          <Link href="/runs">Reload runs</Link>
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
