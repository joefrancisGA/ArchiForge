"use client";

import Link from "next/link";
import { useEffect, useState } from "react";

import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { RunStatusBadge } from "@/components/RunStatusBadge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { listRunsByProjectPaged } from "@/lib/api";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure, uiFailureFromMessage } from "@/lib/api-load-failure";
import { coerceRunSummaryPaged } from "@/lib/operator-response-guards";
import { cn } from "@/lib/utils";
import type { RunSummary } from "@/types/authority";

const DEFAULT_PROJECT_ID = "default";
const PREVIEW_MAX = 5;

function runListPrimaryTitle(run: RunSummary): string {
  const d = run.description?.trim() ?? "";

  if (d.length > 0) {
    return d;
  }

  return "Untitled run";
}

/**
 * Home-column snapshot of the most recent runs so returning operators see live state without opening the full list.
 */
export function RecentRunsHomePanel() {
  const [phase, setPhase] = useState<"loading" | "ready" | "error">("loading");
  const [failure, setFailure] = useState<ApiLoadFailureState | null>(null);
  const [items, setItems] = useState<RunSummary[]>([]);

  useEffect(() => {
    let cancelled = false;

    async function load() {
      setPhase("loading");
      setFailure(null);

      try {
        const raw: unknown = await listRunsByProjectPaged(DEFAULT_PROJECT_ID, 1, PREVIEW_MAX);
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

  return (
    <section aria-labelledby="recent-runs-home-heading">
      <h3
        id="recent-runs-home-heading"
        className="mb-3 text-sm font-bold uppercase tracking-wide text-neutral-600 dark:text-neutral-300"
      >
        Recent runs
      </h3>
      <Card
        className={cn(
          "border border-neutral-200 bg-white shadow-sm dark:border-neutral-800 dark:bg-neutral-900",
        )}
        data-testid="recent-runs-home-panel"
      >
        <CardHeader className="space-y-0.5 px-3 pb-2 pt-3">
          <CardTitle className="text-sm font-semibold text-neutral-900 dark:text-neutral-100">Latest in workspace</CardTitle>
          <p className="m-0 text-xs text-neutral-600 dark:text-neutral-400">Showing the latest runs for this workspace.</p>
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

          {phase === "ready" && items.length === 0 ? (
            <div className="space-y-2">
              <p className="m-0 text-sm font-semibold text-neutral-900 dark:text-neutral-100">No architecture runs yet</p>
              <p className="m-0 text-xs text-neutral-600 dark:text-neutral-400">
                Create a request to generate your first manifest, findings, and artifact bundle.
              </p>
              <div className="mt-2 flex flex-wrap items-center gap-2">
                <Button asChild variant="primary" size="sm" className="h-8">
                  <Link href="/runs/new">Create Request</Link>
                </Button>
                <Button
                  asChild
                  variant="outline"
                  size="sm"
                  className="h-8 border-teal-300 text-teal-800 hover:bg-teal-50 dark:border-teal-700 dark:text-teal-300 dark:hover:bg-teal-900/40"
                >
                  <Link href={`/runs?projectId=${encodeURIComponent(DEFAULT_PROJECT_ID)}`}>See completed example</Link>
                </Button>
              </div>
            </div>
          ) : null}

          {phase === "ready" && items.length > 0 ? (
            <ul className="m-0 list-none space-y-2 p-0">
              {items.map((run) => (
                <li
                  key={run.runId}
                  className="flex flex-wrap items-start justify-between gap-2 border-b border-neutral-100 pb-2 last:border-b-0 last:pb-0 dark:border-neutral-800"
                >
                  <Link
                    href={`/runs/${encodeURIComponent(run.runId)}`}
                    className="min-w-0 flex-1 text-xs font-medium text-teal-800 underline decoration-teal-300/80 hover:text-teal-900 dark:text-teal-200 dark:hover:text-teal-100"
                  >
                    {runListPrimaryTitle(run)}
                  </Link>
                  <RunStatusBadge run={run} className="text-[0.6rem]" />
                </li>
              ))}
            </ul>
          ) : null}

          {phase === "ready" ? (
            <Link
              href={`/runs?projectId=${encodeURIComponent(DEFAULT_PROJECT_ID)}`}
              className="inline-block text-xs font-semibold text-teal-800 underline dark:text-teal-300"
            >
              Open full runs list
            </Link>
          ) : null}
        </CardContent>
      </Card>
    </section>
  );
}
