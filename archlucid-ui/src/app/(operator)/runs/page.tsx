import type { Metadata } from "next";
import Link from "next/link";
import { OperatorPageHeader } from "@/components/OperatorPageHeader";
import { GlossaryTooltip } from "@/components/GlossaryTooltip";
import { redirect } from "next/navigation";

import { RunsListClient } from "@/app/(operator)/runs/RunsListClient";
import { BeforeAfterDeltaPanel } from "@/components/BeforeAfterDeltaPanel";
import { RunsIndexBeforeAfterPanel } from "@/components/RunsIndexBeforeAfterPanel";
import { EmptyState } from "@/components/EmptyState";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { ShortcutHint } from "@/components/ShortcutHint";
import { OperatorMalformedCallout, OperatorTryNext } from "@/components/OperatorShellMessage";
import { Button } from "@/components/ui/button";
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
      <OperatorPageHeader
        title="Architecture runs"
        metadata={<span>Project {projectId}</span>}
      />
      <p className="max-w-3xl leading-relaxed text-neutral-700 dark:text-neutral-300">
        Open an <GlossaryTooltip termKey="run">architecture run</GlossaryTooltip> to review its manifest, artifacts,
        findings, and exports.
      </p>
      <div className="mt-3 flex flex-wrap items-center gap-2">
        <div className="inline-flex items-center gap-1.5">
          <Button variant="outline" size="sm" asChild>
            <Link href="/runs/new" className="no-underline">
              New request
            </Link>
          </Button>
          <ShortcutHint shortcut="Alt+N" className="text-[0.75rem] text-neutral-500 dark:text-neutral-400" />
        </div>
        <Button variant="outline" size="sm" asChild>
          <Link href="/compare" className="no-underline">
            Compare two runs
          </Link>
        </Button>
      </div>

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
            The run list could not be loaded. Check your connection and try reloading.
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
            The server response was unexpected. If this persists, contact support.
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
