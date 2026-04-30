import Link from "next/link";

import { OperatorEmptyState } from "@/components/OperatorShellMessage";

/**
 * Shared 404 body for invalid or stale deep links. With `notFound()` from an operator route, the nearest
 * `app/(operator)/not-found.tsx` wraps this in the normal operator shell.
 */
export function OperatorBrandedNotFound() {
  return (
    <OperatorEmptyState title="We could not find that in ArchLucid">
      <p className="m-0 text-sm leading-relaxed text-neutral-700 dark:text-neutral-300">
        The link may be mistyped, expired, or pointed at a resource that is not in this workspace. Use a fresh link
        from the product, or start from home.
      </p>
      <p className="m-0 mt-3 text-xs text-neutral-500 dark:text-neutral-400">
        If you pasted an id, confirm the full value copied — truncated identifiers are rejected.
      </p>
      <div className="mt-4 flex flex-wrap gap-4 text-sm font-medium">
        <Link className="text-teal-800 underline dark:text-teal-300" href="/" data-testid="not-found-home">
          Home
        </Link>
        <Link className="text-teal-800 underline dark:text-teal-300" href="/runs?projectId=default">
          Runs
        </Link>
        <Link className="text-teal-800 underline dark:text-teal-300" href="/governance/findings">
          Findings
        </Link>
      </div>
      <p className="m-0 mt-6 text-[11px] uppercase tracking-wide text-neutral-400 dark:text-neutral-500">
        ArchLucid · 404
      </p>
      <span data-testid="branded-not-found" className="sr-only">
        Page not found
      </span>
    </OperatorEmptyState>
  );
}
