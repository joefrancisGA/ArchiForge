import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";

import { RunsListClient } from "@/app/(operator)/runs/RunsListClient";
import { BeforeAfterDeltaPanel } from "@/components/BeforeAfterDeltaPanel";
import { RunsIndexBeforeAfterPanel } from "@/components/RunsIndexBeforeAfterPanel";
import { EmptyState } from "@/components/EmptyState";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { ShortcutHint } from "@/components/ShortcutHint";
import { OperatorMalformedCallout, OperatorTryNext } from "@/components/OperatorShellMessage";
import { RUNS_EMPTY } from "@/lib/empty-state-presets";
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

  const firstCommittedRunId: string | null =
    runs.find(
      (r) =>
        (typeof r.goldenManifestId === "string" && r.goldenManifestId.length > 0) || r.hasGoldenManifest === true,
    )?.runId ?? null;

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

      {loadFailure === null && malformedMessage === null ? (
        <BeforeAfterDeltaPanel variant="top" />
      ) : null}

      {loadFailure === null && malformedMessage === null && firstCommittedRunId !== null ? (
        <RunsIndexBeforeAfterPanel committedRunId={firstCommittedRunId} />
      ) : null}

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

      {loadFailure === null && !malformedMessage && totalCount === 0 ? <EmptyState {...RUNS_EMPTY} /> : null}

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
