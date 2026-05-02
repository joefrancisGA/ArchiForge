"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";

import { useDeltaQuery } from "@/components/BeforeAfterDelta/useDeltaQuery";
import { formatFindings, formatHours } from "@/components/BeforeAfterDelta/formatDelta";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { RunStatusBadge } from "@/components/RunStatusBadge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { listRunsByProjectPaged } from "@/lib/api";
import {
  OPERATOR_HOME_EXAMPLE_DESCRIPTION,
  OPERATOR_HOME_EXAMPLE_QUERY_VALUE,
  OPERATOR_HOME_EXAMPLE_RUN_DESCRIPTION_TOKEN,
} from "@/lib/operator-home-example-request";
import { tryStaticDemoRunSummariesPaged, isStaticDemoPayloadFallbackEnabled } from "@/lib/operator-static-demo";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure, uiFailureFromMessage } from "@/lib/api-load-failure";
import { coerceRunSummaryPaged } from "@/lib/operator-response-guards";
import { SHOWCASE_STATIC_DEMO_MANIFEST_ID, SHOWCASE_STATIC_DEMO_RUN_ID, SHOWCASE_STATIC_DEMO_PRIMARY_FINDING_ID } from "@/lib/showcase-static-demo";
import { cn } from "@/lib/utils";
import type { RunSummary } from "@/types/authority";

const DEFAULT_PROJECT_ID = "default";
const PREVIEW_MAX = 5;

type TabId = "recent" | "attention" | "outcomes";

function runListPrimaryTitle(run: RunSummary): string {
  const d = run.description?.trim() ?? "";

  if (d.length > 0) {
    return d;
  }

  return "Untitled architecture review";
}

function isRunNeedingAttention(run: RunSummary): boolean {
  return run.hasFindingsSnapshot === true && run.hasGoldenManifest !== true;
}

const TAB_LABEL: Record<TabId, string> = {
  recent: "Recent",
  attention: "Needs attention",
  outcomes: "Outcomes",
};

function runIsShowcaseHomeExampleStory(run: RunSummary): boolean {
  const id = run.runId.trim();

  if (id === SHOWCASE_STATIC_DEMO_RUN_ID) {
    return true;
  }

  return (run.description ?? "")
    .toLowerCase()
    .includes(OPERATOR_HOME_EXAMPLE_RUN_DESCRIPTION_TOKEN.toLowerCase());
}

/**
 * Single home-column runs snapshot: recent list, attention runs, and outcome medians in one card with tab segments
 * and one `listRunsByProjectPaged` request.
 */
export function RunsDashboardPanel() {
  const [tab, setTab] = useState<TabId>("recent");
  const [phase, setPhase] = useState<"loading" | "ready" | "error">("loading");
  const [failure, setFailure] = useState<ApiLoadFailureState | null>(null);
  const [runsListAuthorityUnusable, setRunsListAuthorityUnusable] = useState(false);
  const [items, setItems] = useState<RunSummary[]>([]);
  const { status: deltaStatus, data: deltaData } = useDeltaQuery({ count: 5 });

  useEffect(() => {
    let cancelled = false;

    async function load() {
      setPhase("loading");
      setFailure(null);
      setRunsListAuthorityUnusable(false);

      try {
        const raw: unknown = await listRunsByProjectPaged(DEFAULT_PROJECT_ID, 1, PREVIEW_MAX);
        const coerced = coerceRunSummaryPaged(raw);

        if (cancelled) {
          return;
        }

        if (!coerced.ok) {
          setRunsListAuthorityUnusable(true);
          setFailure(uiFailureFromMessage(coerced.message));
          setPhase("error");

          return;
        }

        setItems(coerced.value.items);
        setPhase("ready");
      } catch (e) {
        if (cancelled) {
          return;
        }

        setRunsListAuthorityUnusable(true);
        setFailure(toApiLoadFailure(e));
        setPhase("error");
      }
    }

    void load();

    return () => {
      cancelled = true;
    };
  }, []);

  const effectiveItems = useMemo(() => {
    if (items.length > 0) {
      return items;
    }

    if (phase !== "ready" && phase !== "error") {
      return items;
    }

    const fallback = tryStaticDemoRunSummariesPaged(DEFAULT_PROJECT_ID, {
      afterAuthorityListFailure: runsListAuthorityUnusable,
    });

    if (fallback !== null && fallback.items.length > 0) {
      return fallback.items;
    }

    if (
      isStaticDemoPayloadFallbackEnabled() &&
      phase === "ready" &&
      items.length === 0 &&
      !runsListAuthorityUnusable
    ) {
      const emptyWorkspaceFallback = tryStaticDemoRunSummariesPaged(DEFAULT_PROJECT_ID);

      if (emptyWorkspaceFallback !== null && emptyWorkspaceFallback.items.length > 0) {
        return emptyWorkspaceFallback.items;
      }
    }

    return items;
  }, [items, phase, runsListAuthorityUnusable]);

  const showcaseDemoRun = useMemo(
    () => effectiveItems.find((r) => runIsShowcaseHomeExampleStory(r)),
    [effectiveItems],
  );

  const attentionRuns = useMemo(() => effectiveItems.filter(isRunNeedingAttention), [effectiveItems]);
  const attentionPreview = useMemo(() => attentionRuns.slice(0, 3), [attentionRuns]);

  const runListError = phase === "error" && failure !== null && effectiveItems.length === 0;

  return (
    <section aria-labelledby="runs-dashboard-heading" data-onboarding="tour-runs-dashboard">
      <h3
        id="runs-dashboard-heading"
        className="mb-3 text-sm font-bold uppercase tracking-wide text-neutral-600 dark:text-neutral-300"
      >
        Architecture reviews
      </h3>
      <Card
        className="border border-neutral-200 bg-white shadow-sm dark:border-neutral-800 dark:bg-neutral-900"
        data-testid="runs-dashboard-panel"
      >
        <CardHeader className="space-y-2 px-3 pb-2 pt-3">
          <div className="flex flex-wrap gap-2" role="tablist" aria-label="Run views">
            {(["recent", "attention", "outcomes"] as const).map((id) => (
              <button
                key={id}
                type="button"
                role="tab"
                aria-selected={tab === id}
                data-testid={`runs-dashboard-tab-${id}`}
                className={cn(
                  "border-b-2 border-transparent bg-transparent px-0 py-0.5 text-xs font-semibold",
                  tab === id
                    ? "border-teal-700 text-teal-900 dark:border-teal-300 dark:text-teal-200"
                    : "text-neutral-500 hover:text-neutral-800 dark:text-neutral-400 dark:hover:text-neutral-100",
                )}
                onClick={() => {
                  setTab(id);
                }}
              >
                {TAB_LABEL[id]}
              </button>
            ))}
          </div>
          <CardTitle className="text-sm font-semibold text-neutral-900 dark:text-neutral-100">
            {tab === "recent" ? "Latest in workspace" : null}
            {tab === "attention" ? "Runs needing attention" : null}
            {tab === "outcomes" ? "Run outcomes" : null}
          </CardTitle>
          <p className="m-0 text-xs text-neutral-600 dark:text-neutral-400">
            {tab === "recent" ? "Showing the latest runs for this workspace." : null}
            {tab === "attention" ? "Runs with findings awaiting a finalized manifest." : null}
            {tab === "outcomes"
              ? "Manifests finalized, findings surfaced, and average time to finalization."
              : null}
          </p>
        </CardHeader>
        <CardContent className="space-y-3 px-3 pb-3 text-sm">
          {tab === "recent" ? (
            <div data-testid="runs-dashboard-tab-recent">
              {phase === "loading" ? (
                <p className="m-0 text-xs text-neutral-500 dark:text-neutral-400">Loading runs…</p>
              ) : null}

              {runListError ? (
                <div className="text-xs [&_strong]:text-sm" data-testid="runs-dashboard-recent-error">
                  <OperatorApiProblem
                    problem={failure.problem}
                    fallbackMessage={failure.message}
                    correlationId={failure.correlationId}
                  />
                </div>
              ) : null}

              {(phase === "ready" || phase === "error") && showcaseDemoRun ? (
                <div
                  className="space-y-3 rounded-lg border border-emerald-200 bg-emerald-50/60 px-3 py-3 dark:border-emerald-900 dark:bg-emerald-950/25"
                  data-testid="operator-home-showcase-demo-banner"
                >
                  <p className="m-0 text-sm font-semibold text-neutral-900 dark:text-neutral-100">
                    Claims Intake — completed example run
                  </p>
                  <p className="m-0 text-xs text-neutral-600 dark:text-neutral-400">
                    Open the proof path: run detail, finalized manifest, primary finding, or the read-only marketing
                    showcase.
                  </p>
                  <div className="flex flex-wrap items-center gap-2">
                    <Button asChild variant="primary" size="sm" className="h-8">
                      <Link href={`/reviews/${encodeURIComponent(showcaseDemoRun.runId)}`}>Open review</Link>
                    </Button>
                    <Button asChild variant="outline" size="sm" className="h-8">
                      <Link
                        href={`/manifests/${encodeURIComponent(showcaseDemoRun.goldenManifestId ?? SHOWCASE_STATIC_DEMO_MANIFEST_ID)}`}
                      >
                        Finalized manifest
                      </Link>
                    </Button>
                    <Button asChild variant="outline" size="sm" className="h-8">
                      <Link
                        href={`/reviews/${encodeURIComponent(showcaseDemoRun.runId)}/findings/${encodeURIComponent(SHOWCASE_STATIC_DEMO_PRIMARY_FINDING_ID)}`}
                      >
                        Primary finding
                      </Link>
                    </Button>
                    <Button asChild variant="outline" size="sm" className="h-8">
                      <Link href="/showcase/claims-intake-modernization">Showcase (read-only)</Link>
                    </Button>
                  </div>
                </div>
              ) : null}

              {(phase === "ready" || phase === "error") && effectiveItems.length === 0 && !runListError ? (
                <div
                  className="space-y-3 rounded-lg border border-teal-200 bg-teal-50/60 px-3 py-3 dark:border-teal-900 dark:bg-teal-950/30"
                  data-testid="operator-home-getting-started"
                >
                  <p className="m-0 text-sm font-semibold text-neutral-900 dark:text-neutral-100">Getting started</p>
                  <p className="m-0 text-xs text-neutral-600 dark:text-neutral-400">
                    You have no architecture reviews yet. Create a request to produce a manifest,
                    findings, and exportable artifacts — or walk the pilot checklist first.
                  </p>
                  <div className="flex flex-wrap items-center gap-2">
                    <Button asChild variant="primary" size="sm" className="h-8">
                      <Link href="/reviews/new">Create your first request</Link>
                    </Button>
          <Button asChild variant="outline" size="sm" className="h-8">
            <Link href="/onboarding">First-review checklist</Link>
          </Button>
                    <Button asChild variant="outline" size="sm" className="h-8">
                      <Link href="/help">How this works</Link>
                    </Button>
                  </div>
                </div>
              ) : null}

              {(phase === "ready" || phase === "error") && effectiveItems.length > 0 ? (
                <ul className="m-0 list-none space-y-2 p-0" data-testid="recent-runs-home-panel">
                  {effectiveItems.map((run) => (
                    <li
                      key={run.runId}
                      className="flex flex-wrap items-start justify-between gap-2 border-b border-neutral-100 pb-2 last:border-b-0 last:pb-0 dark:border-neutral-800"
                    >
                      <Link
                        href={`/reviews/${encodeURIComponent(run.runId)}`}
                        className="min-w-0 flex-1 text-xs font-medium text-teal-800 underline decoration-teal-300/80 hover:text-teal-900 dark:text-teal-200 dark:hover:text-teal-100"
                      >
                        {runListPrimaryTitle(run)}
                      </Link>
                      <RunStatusBadge run={run} className="text-[0.6rem]" />
                    </li>
                  ))}
                </ul>
              ) : null}
            </div>
          ) : null}

          {tab === "attention" ? (
            <div data-testid="runs-dashboard-tab-attention">
              {phase === "loading" ? (
                <p className="m-0 text-xs text-neutral-500 dark:text-neutral-400">Loading runs…</p>
              ) : null}

              {runListError ? (
                <div className="text-xs [&_strong]:text-sm">
                  <OperatorApiProblem
                    problem={failure.problem}
                    fallbackMessage={failure.message}
                    correlationId={failure.correlationId}
                  />
                </div>
              ) : null}

              {(phase === "ready" || (phase === "error" && effectiveItems.length > 0)) ? (
                <>
                  {attentionRuns.length === 0 ? (
                    <p className="m-0 text-xs leading-relaxed text-neutral-600 dark:text-neutral-400">
                      No reviews currently need attention.
                    </p>
                  ) : (
                    <>
                      <p className="m-0 text-xs font-medium text-neutral-700 dark:text-neutral-300">
                        {attentionRuns.length === 1
                          ? "1 run needs attention."
                          : `${attentionRuns.length} runs need attention.`}
                      </p>
                      <ul className="m-0 list-none space-y-2 p-0" data-testid="command-center-runs-card">
                        {attentionPreview.map((run) => (
                          <li
                            key={run.runId}
                            className="flex flex-wrap items-start gap-2 border-b border-neutral-100 pb-2 last:border-b-0 last:pb-0 dark:border-neutral-800"
                          >
                            <span className="min-w-0 flex-1 text-xs font-medium text-neutral-900 dark:text-neutral-100">
                              {runListPrimaryTitle(run)}
                            </span>
                            <RunStatusBadge run={run} className="text-[0.6rem]" />
                          </li>
                        ))}
                      </ul>
                    </>
                  )}
                </>
              ) : null}
            </div>
          ) : null}

          {tab === "outcomes" ? (
            <div data-testid="command-center-activity-card">
              {deltaStatus === "loading" ? (
                <p className="m-0 text-xs text-neutral-500 dark:text-neutral-400">Loading run outcomes…</p>
              ) : null}

              {deltaStatus === "error" ? (
                <p className="m-0 text-xs text-neutral-600 dark:text-neutral-400">
                  Run outcomes are unavailable right now. Try again later or open the runs list.
                </p>
              ) : null}

              {deltaStatus === "ready" && deltaData !== null && deltaData.returnedCount > 0 ? (
                <dl className="m-0 grid grid-cols-2 gap-2 text-xs">
                  <div>
                    <dt className="text-[10px] uppercase text-neutral-500 dark:text-neutral-400">Findings</dt>
                    <dd className="m-0 text-sm font-semibold text-neutral-900 dark:text-neutral-100">
                      {formatFindings(deltaData.medianTotalFindings)}
                    </dd>
                  </div>
                  <div>
                    <dt className="text-[10px] uppercase text-neutral-500 dark:text-neutral-400">Time to finalize</dt>
                    <dd className="m-0 text-sm font-semibold text-neutral-900 dark:text-neutral-100">
                      {formatHours(deltaData.medianTimeToCommittedManifestTotalSeconds)}
                    </dd>
                  </div>
                </dl>
              ) : null}

              {deltaStatus === "ready" && deltaData !== null && deltaData.returnedCount === 0 ? (
                <p className="m-0 text-xs text-neutral-600 dark:text-neutral-400">
                  After your first finalized run, this panel will show manifests finalized, findings surfaced, and average
                  time to finalization.
                </p>
              ) : null}
            </div>
          ) : null}

          <Link
            href={`/reviews?projectId=${encodeURIComponent(DEFAULT_PROJECT_ID)}`}
            className="inline-block text-xs font-semibold text-teal-800 underline dark:text-teal-300"
          >
            Open full runs list
          </Link>
        </CardContent>
      </Card>

      {(phase === "ready" || phase === "error") && effectiveItems.length === 0 && !runListError ? (
        <Card
          className="mt-3 border border-neutral-200 bg-white shadow-sm dark:border-neutral-800 dark:bg-neutral-900"
          data-testid="example-request-panel"
        >
          <CardHeader className="space-y-1 px-3 pb-2 pt-3">
            <CardTitle className="text-sm font-semibold text-neutral-900 dark:text-neutral-100">Example request</CardTitle>
            <p className="m-0 text-xs text-neutral-600 dark:text-neutral-400">
              {OPERATOR_HOME_EXAMPLE_DESCRIPTION}
            </p>
          </CardHeader>
          <CardContent className="flex flex-wrap gap-2 px-3 pb-3">
            <Button asChild variant="outline" size="sm" className="h-8">
              <Link
                href={`/reviews/new?example=${encodeURIComponent(OPERATOR_HOME_EXAMPLE_QUERY_VALUE)}`}
              >
                Use this example
              </Link>
            </Button>
            <Button asChild variant="primary" size="sm" className="h-8">
              <Link href={`/reviews?projectId=${encodeURIComponent(DEFAULT_PROJECT_ID)}`}>
                See completed output
              </Link>
            </Button>
          </CardContent>
        </Card>
      ) : null}
    </section>
  );
}
