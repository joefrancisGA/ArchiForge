"use client";

import Link from "next/link";
import { useEffect } from "react";

import "./globals.css";

import { Button } from "@/components/ui/button";

/**
 * Replaces the entire root layout when layout.tsx fails. Must define html/body.
 */
export default function GlobalError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    console.error("Operator shell global error:", error);
  }, [error]);

  const digest = error.digest?.trim() ?? "";
  const isDev = process.env.NODE_ENV === "development";

  return (
    <html lang="en">
      <body className="min-h-screen bg-neutral-50 p-8 text-neutral-900 dark:bg-neutral-950 dark:text-neutral-100">
        <h1 className="m-0 text-2xl font-semibold">ArchLucid</h1>
        <div className="mt-4 max-w-lg rounded-lg border border-red-200 bg-red-50 px-4 py-3 dark:border-red-900 dark:bg-red-950/40">
          <strong className="text-red-950 dark:text-red-100">The app shell could not load</strong>
          <p className="mt-2 text-sm text-red-900 dark:text-red-100/95">
            {isDev ? error.message : "A critical error occurred. Try reloading, open Help, or return home."}
          </p>
          {digest.length > 0 ? (
            <p className="mt-3 text-xs text-red-900/85 dark:text-red-100/80">
              Reference ID: <code className="rounded bg-red-100/80 px-1 py-0.5 font-mono dark:bg-red-900/60">{digest}</code>
            </p>
          ) : null}
        </div>
        <div className="mt-6 flex flex-wrap gap-2">
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
      </body>
    </html>
  );
}
