import type { Metadata } from "next";
import Link from "next/link";
import { OperatorPageHeader } from "@/components/OperatorPageHeader";
import { GlossaryTooltip } from "@/components/GlossaryTooltip";
import { redirect } from "next/navigation";

import { RunsListAggregateErrorBoundary } from "@/components/RunsListAggregateErrorBoundary";
import { BeforeAfterDeltaPanel } from "@/components/BeforeAfterDeltaPanel";
import { RunsIndexBeforeAfterPanel } from "@/components/RunsIndexBeforeAfterPanel";
import { EmptyState } from "@/components/EmptyState";
import { OperatorDemoStaticBanner } from "@/components/OperatorDemoStaticBanner";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { ShortcutHint } from "@/components/ShortcutHint";
import { OperatorMalformedCallout, OperatorTryNext } from "@/components/OperatorShellMessage";
import { Button } from "@/components/ui/button";
import { normalizeRunSummaryForDemoPicker } from "@/lib/demo-run-canonical";
import { isPublicDemoModeEnv } from "@/lib/public-demo-mode";
import { toDocsBlobUrl } from "@/lib/contextual-help-content";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import { coerceRunSummaryPaged } from "@/lib/operator-response-guards";
import { RUNS_EMPTY } from "@/lib/empty-state-presets";
import { tryStaticDemoRunSummariesPaged } from "@/lib/operator-static-demo";
import { listRunsByProjectPaged } from "@/lib/api";
import type { RunSummary } from "@/types/authority";

export const metadata: Metadata = {
  title: "Architecture reviews",
};

/** Server-rendered run list page. Fetches a page of runs and validates via coerceRunSummaryPaged. */
export default async function RunsPage({
  searchParams,
}: {
  searchParams: Promise<{ projectId?: string; page?: string; pageSize?: string; take?: string; cursor?: string }>;
}) {
  const resolved = await searchParams;
  const projectId = resolved.projectId ?? "default";
  const page = Math.max(1, Number.parseInt(resolved.page ?? "1", 10) || 1);
  const sizeRaw = resolved.pageSize ?? resolved.take ?? "20";
  const pageSize = Math.min(200, Math.max(1, Number.parseInt(sizeRaw, 10) || 20));

  const cursorParam = resolved.cursor?.trim();

  let cursor: string | undefined;

  if (cursorParam) {
    cursor = cursorParam;
  }

  let nextCursorForClient: string | null = null;

  let runs: RunSummary[] = [];
  let totalCount = 0;
  let loadFailure: ApiLoadFailureState | null = null;
  let malformedMessage: string | null = null;

  let usedStaticRunsFallback = false;

  try {
    const raw: unknown = await listRunsByProjectPaged(projectId, page, pageSize, { cursor });
    const coerced = coerceRunSummaryPaged(raw, { page });

    if (!coerced.ok) {
      malformedMessage = coerced.message;
      runs = [];
      totalCount = 0;
    } else {
      runs = coerced.value.items;
      totalCount = coerced.value.totalCount;

      const maybeNext = coerced.value.nextCursor;

      if (typeof maybeNext === "string" && maybeNext.length > 0) {
        nextCursorForClient = maybeNext;
      }
    }
  } catch (e) {
    loadFailure = toApiLoadFailure(e);
  }

  const demoPaged =
    loadFailure !== null || malformedMessage !== null
      ? tryStaticDemoRunSummariesPaged(projectId, { afterAuthorityListFailure: true })
      : null;

  if (demoPaged !== null) {
    runs = demoPaged.items;
    totalCount = demoPaged.totalCount;
    loadFailure = null;
    malformedMessage = null;
    usedStaticRunsFallback = true;
  }

  runs = runs.map(normalizeRunSummaryForDemoPicker);

  const projectTitle =
    isPublicDemoModeEnv() && projectId === "default" ? "Claims Intake Demo Workspace" : `Project ${projectId}`;

  if (loadFailure === null && malformedMessage === null && totalCount > 0 && !usedStaticRunsFallback) {
    const pages = Math.max(1, Math.ceil(totalCount / pageSize));

    if (page > pages) {
      redirect(`/reviews?projectId=${encodeURIComponent(projectId)}&page=${pages}&pageSize=${pageSize}`);
    }
  }

  const firstCommittedRunId: string | null =
    runs.find(
      (r) =>
        (typeof r.goldenManifestId === "string" && r.goldenManifestId.length > 0) || r.hasGoldenManifest === true,
    )?.runId ?? null;

  return (
    <main aria-label="Architecture reviews">
      <OperatorPageHeader title="Architecture reviews" metadata={<span>{projectTitle}</span>} />
      <p className="max-w-3xl leading-relaxed text-neutral-700 dark:text-neutral-300">
        Open an <GlossaryTooltip termKey="run">architecture review</GlossaryTooltip> to inspect its manifest, artifacts,
        findings, and exports.
      </p>
      <div className="mt-3 flex flex-wrap items-center gap-2">
        <div className="inline-flex items-center gap-1.5">
          <Button variant="outline" size="sm" asChild>
            <Link href="/reviews/new" className="no-underline">
              New request
            </Link>
          </Button>
          <ShortcutHint shortcut="Alt+N" className="text-[0.75rem] text-neutral-500 dark:text-neutral-400" />
        </div>
        {totalCount > 0 ? (
          <Button variant="outline" size="sm" asChild>
            <Link href="/compare" className="no-underline">
              Compare two reviews
            </Link>
          </Button>
        ) : null}
      </div>

      {usedStaticRunsFallback ? (
        <div className="mt-4 max-w-3xl">
          <OperatorDemoStaticBanner />
        </div>
      ) : null}

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

      {loadFailure === null && !malformedMessage && totalCount === 0 ? (
        <>
          <div
            className="mt-4 max-w-prose rounded-md border border-amber-200 bg-amber-50/70 px-3 py-2 text-sm leading-snug text-neutral-800 dark:border-amber-900 dark:bg-amber-950/30 dark:text-neutral-200"
            data-testid="runs-empty-core-pilot-hint"
          >
            <strong className="font-semibold">Core Pilot first:</strong> use{" "}
            <strong className="font-semibold">New request</strong> here, then execute, commit, and review on run detail
            (see{" "}
            <a
              href={toDocsBlobUrl("/docs/CORE_PILOT.md")}
              target="_blank"
              rel="noopener noreferrer"
              className="font-medium text-teal-800 underline dark:text-teal-300"
            >
              Core Pilot path
            </a>
            ). You do not need Compare, Replay, or Governance until after your first committed manifest.
          </div>
          <EmptyState {...RUNS_EMPTY} />
        </>
      ) : null}

      {!loadFailure && !malformedMessage && totalCount > 0 ? (
        <RunsListAggregateErrorBoundary
          runs={runs}
          projectId={projectId}
          page={page}
          pageSize={pageSize}
          totalCount={totalCount}
          nextCursor={nextCursorForClient}
        />
      ) : null}
    </main>
  );
}
