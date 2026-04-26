"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";

import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { RunStatusBadge } from "@/components/RunStatusBadge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { listRunsByProjectPaged } from "@/lib/api";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure, uiFailureFromMessage } from "@/lib/api-load-failure";
import { formatFindings, formatHours } from "@/components/BeforeAfterDelta/formatDelta";
import { useDeltaQuery } from "@/components/BeforeAfterDelta/useDeltaQuery";
import type { HealthReadyResponse } from "@/lib/health-dashboard-types";
import { mergeRegistrationScopeForProxy } from "@/lib/proxy-fetch-registration-scope";
import { coerceRunSummaryPaged } from "@/lib/operator-response-guards";
import { cn } from "@/lib/utils";
import type { RunSummary } from "@/types/authority";

const DEFAULT_PROJECT_ID = "default";

function runListPrimaryTitle(run: RunSummary): string {
  const d = run.description?.trim() ?? "";

  if (d.length > 0) {
    return d;
  }

  return "Untitled run";
}

function isRunNeedingAttention(run: RunSummary): boolean {
  return run.hasFindingsSnapshot === true && run.hasGoldenManifest !== true;
}

function RunsNeedingAttentionCard() {
  const [phase, setPhase] = useState<"loading" | "ready" | "error">("loading");
  const [failure, setFailure] = useState<ApiLoadFailureState | null>(null);
  const [items, setItems] = useState<RunSummary[]>([]);

  useEffect(() => {
    let cancelled = false;

    async function load() {
      setPhase("loading");
      setFailure(null);

      try {
        const raw: unknown = await listRunsByProjectPaged(DEFAULT_PROJECT_ID, 1, 5);
        const coerced = coerceRunSummaryPaged(raw);

        if (cancelled) {
          return;
        }

        if (!coerced.ok) {
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

        setFailure(toApiLoadFailure(e));
        setPhase("error");
      }
    }

    void load();

    return () => {
      cancelled = true;
    };
  }, []);

  const attention = useMemo(() => items.filter(isRunNeedingAttention), [items]);
  const preview = useMemo(() => attention.slice(0, 3), [attention]);

  return (
    <Card
      className={cn(
        "border border-neutral-200 bg-neutral-50/60 shadow-none dark:border-neutral-800 dark:bg-neutral-900/30",
      )}
      data-testid="command-center-runs-card"
    >
      <CardHeader className="space-y-1 px-3 pb-2 pt-3">
        <CardTitle className="text-sm font-semibold text-neutral-900 dark:text-neutral-100">
          Runs needing attention
        </CardTitle>
        <p className="m-0 text-xs text-neutral-600 dark:text-neutral-400">
          Runs with findings awaiting a finalized manifest.
        </p>
      </CardHeader>
      <CardContent className="space-y-3 px-3 pb-3 text-sm">
        {phase === "loading" ? (
          <p className="m-0 text-xs text-neutral-500 dark:text-neutral-400">Loading runs…</p>
        ) : null}

        {phase === "error" && failure !== null ? (
          <div className="text-xs [&_strong]:text-sm">
            <OperatorApiProblem
              problem={failure.problem}
              fallbackMessage={failure.message}
              correlationId={failure.correlationId}
            />
          </div>
        ) : null}

        {phase === "ready" ? (
          <>
            {attention.length === 0 ? (
              <div className="space-y-2">
                <p className="m-0 text-xs leading-relaxed text-neutral-600 dark:text-neutral-400">
                  {items.length === 0
                    ? "No runs currently need attention. Create a request to start your first run."
                    : "No runs currently need attention."}
                </p>
                {items.length === 0 ? (
                  <div className="flex flex-wrap items-center gap-2">
                    <Button asChild variant="primary" size="sm" className="h-8">
                      <Link href="/runs/new">Create Request</Link>
                    </Button>
                  </div>
                ) : null}
              </div>
            ) : (
              <>
                <p className="m-0 text-xs font-medium text-neutral-700 dark:text-neutral-300">
                  {attention.length === 1
                    ? "1 run needs attention."
                    : `${attention.length} runs need attention.`}
                </p>
                <ul className="m-0 list-none space-y-2 p-0">
                  {preview.map((run) => (
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
            <Link
              href={`/runs?projectId=${encodeURIComponent(DEFAULT_PROJECT_ID)}`}
              className="inline-block text-xs font-semibold text-teal-800 underline dark:text-teal-300"
            >
              Open runs
            </Link>
          </>
        ) : null}
      </CardContent>
    </Card>
  );
}

function RecentActivityCommandCard() {
  const { status, data } = useDeltaQuery({ count: 5 });

  return (
    <Card
      className="border border-neutral-200 bg-neutral-50/60 shadow-none dark:border-neutral-800 dark:bg-neutral-900/30"
      data-testid="command-center-activity-card"
    >
      <CardHeader className="space-y-1 px-3 pb-2 pt-3">
        <CardTitle className="text-sm font-semibold text-neutral-900 dark:text-neutral-100">Run outcomes</CardTitle>
        <p className="m-0 text-xs text-neutral-600 dark:text-neutral-400">
          Manifests finalized, findings surfaced, and average time to finalization.
        </p>
      </CardHeader>
      <CardContent className="space-y-3 px-3 pb-3 text-sm">
        {status === "loading" ? (
          <p className="m-0 text-xs text-neutral-500 dark:text-neutral-400">Loading run outcomes…</p>
        ) : null}

        {status === "error" ? (
          <p className="m-0 text-xs text-neutral-600 dark:text-neutral-400">
            Run outcomes are unavailable right now. Try again later or open the runs list.
          </p>
        ) : null}

        {status === "ready" && data !== null && data.returnedCount > 0 ? (
          <dl className="m-0 grid grid-cols-2 gap-2 text-xs">
            <div>
              <dt className="text-[10px] uppercase text-neutral-500 dark:text-neutral-400">Findings</dt>
              <dd className="m-0 text-sm font-semibold text-neutral-900 dark:text-neutral-100">
                {formatFindings(data.medianTotalFindings)}
              </dd>
            </div>
            <div>
              <dt className="text-[10px] uppercase text-neutral-500 dark:text-neutral-400">Time to finalize</dt>
              <dd className="m-0 text-sm font-semibold text-neutral-900 dark:text-neutral-100">
                {formatHours(data.medianTimeToCommittedManifestTotalSeconds)}
              </dd>
            </div>
          </dl>
        ) : null}

        {status === "ready" && data !== null && data.returnedCount === 0 ? (
          <p className="m-0 text-xs text-neutral-600 dark:text-neutral-400">
            After your first finalized run, this panel will show manifests finalized, findings surfaced, and average time to
            finalization.
          </p>
        ) : null}

        <Link
          href={`/runs?projectId=${encodeURIComponent(DEFAULT_PROJECT_ID)}`}
          className="inline-block text-xs font-semibold text-teal-800 underline dark:text-teal-300"
        >
          View runs
        </Link>
      </CardContent>
    </Card>
  );
}

function healthReadinessDotClass(status: string): string {
  const normalized = status.trim().toLowerCase();

  if (normalized.includes("unhealthy") || normalized.includes("down") || normalized.includes("fail")) {
    return "bg-red-500";
  }

  if (normalized.includes("degraded") || normalized.includes("warn")) {
    return "bg-amber-500";
  }

  if (normalized.includes("healthy") || normalized.includes("ok")) {
    return "bg-emerald-500";
  }

  return "bg-neutral-400";
}

/** Compact readiness strip above the workspace grid — full cards reserved for runs and outcomes. */
function SystemHealthStatusStrip() {
  const [phase, setPhase] = useState<"loading" | "ready" | "unavailable">("loading");
  const [ready, setReady] = useState<HealthReadyResponse | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function load() {
      setPhase("loading");

      try {
        const res = await fetch(
          "/api/proxy/health/ready",
          mergeRegistrationScopeForProxy({ headers: { Accept: "application/json" }, cache: "no-store" }),
        );

        if (cancelled) {
          return;
        }

        if (!res.ok) {
          setReady(null);
          setPhase("unavailable");

          return;
        }

        const body = (await res.json()) as HealthReadyResponse;
        setReady(body);
        setPhase("ready");
      } catch {
        if (cancelled) {
          return;
        }

        setReady(null);
        setPhase("unavailable");
      }
    }

    void load();

    return () => {
      cancelled = true;
    };
  }, []);

  const overall = ready?.status?.trim() ?? "";

  return (
    <div
      data-testid="command-center-health-card"
      className="mb-4 flex flex-wrap items-center gap-2 rounded-lg border border-neutral-200 bg-neutral-50/60 px-3 py-2 text-xs dark:border-neutral-800 dark:bg-neutral-900/30"
      aria-label="System health"
    >
      {phase === "loading" ? (
        <span className="text-neutral-500 dark:text-neutral-400">Checking readiness…</span>
      ) : null}

      {phase === "unavailable" ? (
        <>
          <span className="h-2 w-2 shrink-0 rounded-full bg-amber-500" aria-hidden />
          <span className="text-neutral-600 dark:text-neutral-400">Health dashboard not configured yet.</span>
        </>
      ) : null}

      {phase === "ready" && overall.length > 0 ? (
        <>
          <span
            className={cn("h-2 w-2 shrink-0 rounded-full", healthReadinessDotClass(overall))}
            aria-hidden
          />
          <span className="text-neutral-800 dark:text-neutral-200">
            Platform services: <span className="font-medium">{overall}</span>
          </span>
        </>
      ) : null}

      {phase === "ready" && overall.length === 0 ? (
        <span className="text-neutral-600 dark:text-neutral-400">Readiness payload had no overall status.</span>
      ) : null}

      <Link
        href="/admin/health"
        className="ml-auto inline-block text-xs font-semibold text-teal-800 underline dark:text-teal-300"
      >
        Details
      </Link>
    </div>
  );
}

/**
 * Workspace status: runs snapshot, run outcome medians, and API readiness — always on home so the page stays an
 * operator cockpit, not only a first-run checklist.
 */
export function CommandCenterSection() {
  return (
    <section className="mt-6" aria-labelledby="workspace-status-heading">
      <h3 id="workspace-status-heading" className="mb-3 text-base font-bold text-neutral-900 dark:text-neutral-100">
        Workspace status
      </h3>
      <SystemHealthStatusStrip />
      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <RunsNeedingAttentionCard />
        <RecentActivityCommandCard />
      </div>
    </section>
  );
}
