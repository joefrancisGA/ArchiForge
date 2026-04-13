import Link from "next/link";

/**
 * Lightweight orientation page (Session A): links into the main operator flows.
 * Deeper checklists live under /onboarding and docs/START_HERE.md.
 */
export default function GettingStartedPage() {
  return (
    <main id="main-content" className="mx-auto max-w-3xl px-1 sm:px-0">
      <h2 className="mt-0 text-2xl font-semibold tracking-tight">Getting started</h2>
      <p className="text-sm text-neutral-600 dark:text-neutral-400">
        Use this page as a fast path into the operator shell. Each step links to the UI; see the repository{" "}
        <code className="rounded bg-neutral-100 px-1 text-xs dark:bg-neutral-800">docs/START_HERE.md</code> for
        architecture context.
      </p>
      <ol className="mt-6 list-decimal space-y-4 pl-5 text-sm">
        <li>
          <strong>Scope and auth</strong> — confirm API base URL and credentials in the header{" "}
          <span className="text-neutral-500">(Auth panel)</span>.
        </li>
        <li>
          <Link className="font-medium text-blue-700 underline dark:text-blue-400" href="/runs/new">
            Create a run
          </Link>{" "}
          and track the authority pipeline to completion.
        </li>
        <li>
          <Link className="font-medium text-blue-700 underline dark:text-blue-400" href="/governance/dashboard">
            Open the governance dashboard
          </Link>{" "}
          for pending approvals, lineage, and inline approve/reject when you are not the requester.
        </li>
        <li>
          <Link className="font-medium text-blue-700 underline dark:text-blue-400" href="/compare">
            Compare runs
          </Link>{" "}
          or{" "}
          <Link className="font-medium text-blue-700 underline dark:text-blue-400" href="/replay">
            replay
          </Link>{" "}
          for drift and validation.
        </li>
        <li>
          <Link className="font-medium text-blue-700 underline dark:text-blue-400" href="/onboarding">
            Full onboarding checklist
          </Link>{" "}
          for week-one tasks.
        </li>
      </ol>
    </main>
  );
}
