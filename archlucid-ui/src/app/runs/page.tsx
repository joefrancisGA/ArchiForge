import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";

import { RunsListClient } from "@/app/runs/RunsListClient";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { ShortcutHint } from "@/components/ShortcutHint";
import {
  OperatorEmptyState,
  OperatorMalformedCallout,
  OperatorTryNext,
} from "@/components/OperatorShellMessage";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import { coerceRunSummaryPaged } from "@/lib/operator-response-guards";
import { listRunsByProjectPaged } from "@/lib/api";
import type { RunSummary } from "@/types/authority";

export const metadata: Metadata = {
  title: "Runs list",
};

/** Server-rendered run list page. Fetches a page of runs and validates via coerceRunSummaryPaged. */
export default async function RunsPage({
  searchParams,
}: {
  searchParams: Promise<{ projectId?: string; page?: string; pageSize?: string; take?: string }>;
}) {
  const resolved = await searchParams;
  const projectId = resolved.projectId ?? "default";
  const page = Math.max(1, Number.parseInt(resolved.page ?? "1", 10) || 1);
  const sizeRaw = resolved.pageSize ?? resolved.take ?? "20";
  const pageSize = Math.min(200, Math.max(1, Number.parseInt(sizeRaw, 10) || 20));

  let runs: RunSummary[] = [];
  let totalCount = 0;
  let loadFailure: ApiLoadFailureState | null = null;
  let malformedMessage: string | null = null;

  try {
    const raw: unknown = await listRunsByProjectPaged(projectId, page, pageSize);
    const coerced = coerceRunSummaryPaged(raw);

    if (!coerced.ok) {
      malformedMessage = coerced.message;
      runs = [];
      totalCount = 0;
    } else {
      runs = coerced.value.items;
      totalCount = coerced.value.totalCount;
    }
  } catch (e) {
    loadFailure = toApiLoadFailure(e);
  }

  if (loadFailure === null && malformedMessage === null && totalCount > 0) {
    const pages = Math.max(1, Math.ceil(totalCount / pageSize));

    if (page > pages) {
      redirect(`/runs?projectId=${encodeURIComponent(projectId)}&page=${pages}&pageSize=${pageSize}`);
    }
  }

  return (
    <main aria-labelledby="runs-page-heading">
      <h2 id="runs-page-heading">
        Runs{" "}
        <span className="text-[0.92em] font-normal text-neutral-600 dark:text-neutral-400">
          — project {projectId}
        </span>
      </h2>
      <p className="max-w-3xl leading-relaxed text-neutral-700 dark:text-neutral-300">
        Open a run for manifest summary, artifact review, compare and replay links, and exports. Results are paged
        server-side (default 20 per page; use <code>?page=</code> and <code>?pageSize=</code>, or legacy{" "}
        <code>?take=</code> as page size).
      </p>
      <p className="mt-2">
        <Link href="/" className="text-teal-800 underline dark:text-teal-300">
          Home
        </Link>
        {" · "}
        <Link href="/runs/new" className="text-teal-800 underline dark:text-teal-300">
          New run (wizard)
        </Link>{" "}
        <ShortcutHint shortcut="Alt+N" className="ml-1 align-middle text-[0.75rem]" />
        {" · "}
        <Link href="/graph" className="text-teal-800 underline dark:text-teal-300">
          Graph
        </Link>
        {" · "}
        <Link href="/compare" className="text-teal-800 underline dark:text-teal-300">
          Compare two runs
        </Link>
      </p>

      {loadFailure && (
        <>
          <OperatorApiProblem
            problem={loadFailure.problem}
            fallbackMessage={loadFailure.message}
            correlationId={loadFailure.correlationId}
          />
          <OperatorTryNext>
            Confirm the API is up (<code>GET /health/live</code>), <code>.env.local</code> has{" "}
            <code>ARCHLUCID_API_BASE_URL</code> (and API key if required), then reload. If you use a non-default
            project, add <code>?projectId=…</code> to the URL. Use the correlation ID above in API logs if support
            asks.
          </OperatorTryNext>
        </>
      )}

      {!loadFailure && malformedMessage && (
        <>
          <OperatorMalformedCallout>
            <strong>Runs list response was not usable.</strong>
            <p className="mt-2">{malformedMessage}</p>
            <p className="mt-2 text-sm">
              The HTTP call may have succeeded, but the JSON did not match the expected paged run summary
              shape. This is distinct from an empty project (zero runs).
            </p>
          </OperatorMalformedCallout>
          <OperatorTryNext>
            Deployed UI and API versions may be out of sync—compare release tags. Open <code>GET /version</code> on
            the API and the operator shell build you are running.
          </OperatorTryNext>
        </>
      )}

      {loadFailure === null && !malformedMessage && totalCount === 0 && (
        <OperatorEmptyState title="No runs in this project yet">
          <p className="m-0">
            This is a valid empty list — start with a guided request, or create runs via API/CLI and refresh.
          </p>
          <p className="mt-3.5">
            <Link
              href="/runs/new"
              className="inline-block rounded-lg bg-teal-700 px-[18px] py-2.5 text-sm font-semibold text-white no-underline hover:bg-teal-800 dark:bg-teal-600 dark:hover:bg-teal-500"
            >
              Create your first run (wizard)
            </Link>
          </p>
          <p className="mt-3 text-sm text-neutral-500 dark:text-neutral-400">
            CLI/API: <code>docs/CLI_USAGE.md</code> ·{" "}
            <Link href="/" className="text-teal-800 underline dark:text-teal-300">
              Home workflow
            </Link>{" "}
            ·{" "}
            <Link href="/onboarding" className="text-teal-800 underline dark:text-teal-300">
              Onboarding
            </Link>
          </p>
        </OperatorEmptyState>
      )}

      {!loadFailure && !malformedMessage && totalCount > 0 ? (
        <RunsListClient
          runs={runs}
          projectId={projectId}
          page={page}
          pageSize={pageSize}
          totalCount={totalCount}
        />
      ) : null}
    </main>
  );
}
