"use client";

import Link from "next/link";
import { useEffect } from "react";

import { OperatorErrorCallout } from "@/components/OperatorShellMessage";
import { Button } from "@/components/ui/button";
import { reportClientError } from "@/lib/error-telemetry";

/**
 * Catches errors in route segments below the root layout (pages, nested layouts).
 * Does not catch errors in root layout.tsx — see global-error.tsx.
 */
export default function AppError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    console.error("Operator shell route error:", error);
    reportClientError(error, { source: "app-error-boundary", digest: error.digest ?? "" });
  }, [error]);

  const digest = error.digest?.trim() ?? "";
  const isDev = process.env.NODE_ENV === "development";

  return (
    <main className="mx-auto max-w-lg space-y-4 px-4 py-8">
      <OperatorErrorCallout>
        <strong className="text-base">Something went wrong</strong>
        <p className="mt-2 text-sm">
          {isDev
            ? "Development build — technical details appear below."
            : "This page hit an unexpected error. You can try again, go home, or open Help for guidance."}
        </p>
        {isDev ? (
          <pre
            className="mt-3 max-h-40 overflow-auto rounded border border-neutral-200 bg-neutral-50 p-2 text-xs text-neutral-800 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-200"
            style={{ whiteSpace: "pre-wrap" }}
          >
            {error.message}
          </pre>
        ) : null}
        {digest.length > 0 ? (
          <p className="mt-3 text-xs text-neutral-600 dark:text-neutral-400">
            Reference ID: <code className="rounded bg-neutral-100 px-1 py-0.5 font-mono dark:bg-neutral-800">{digest}</code>
          </p>
        ) : null}
      </OperatorErrorCallout>
      <div className="flex flex-wrap items-center gap-2">
        <Button type="button" variant="primary" onClick={() => reset()}>
          Retry
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
