"use client";

import Link from "next/link";
import { Component, type ErrorInfo, type ReactNode } from "react";

import { RunsListClient, type RunsListClientProps } from "@/app/(operator)/runs/RunsListClient";
import { OperatorDemoStaticBanner } from "@/components/OperatorDemoStaticBanner";
import { Button } from "@/components/ui/button";
import { isNextPublicDemoMode } from "@/lib/demo-ui-env";
import { tryStaticDemoRunSummariesPaged } from "@/lib/operator-static-demo";
import type { RunSummary } from "@/types/authority";

function runListPrimaryTitle(run: RunSummary): string {
  const d = run.description?.trim() ?? "";

  if (d.length > 0) {
    return d;
  }

  return "Untitled run";
}

function RunsListMinimalDemoTable({ runs }: { readonly runs: RunSummary[] }) {
  return (
    <div className="overflow-x-auto rounded-md border border-neutral-200 dark:border-neutral-800">
      <table className="w-full border-collapse text-sm">
        <thead>
          <tr className="border-b border-neutral-200 bg-neutral-50/80 dark:border-neutral-800 dark:bg-neutral-900/40">
            <th className="px-3 py-2 text-left text-xs font-semibold uppercase tracking-wide text-neutral-600 dark:text-neutral-400">
              Run
            </th>
            <th className="px-3 py-2 text-left text-xs font-semibold uppercase tracking-wide text-neutral-600 dark:text-neutral-400">
              Actions
            </th>
          </tr>
        </thead>
        <tbody className="divide-y divide-neutral-100 dark:divide-neutral-800">
          {runs.map((run) => (
            <tr key={run.runId}>
              <td className="max-w-[min(100vw,28rem)] px-3 py-2 align-top">
                <span className="font-semibold text-sm text-neutral-900 dark:text-neutral-100">
                  {runListPrimaryTitle(run)}
                </span>
                <code className="mt-1 block break-all font-mono text-xs text-neutral-500 dark:text-neutral-400">
                  {run.runId}
                </code>
              </td>
              <td className="whitespace-nowrap px-3 py-2 align-top">
                <Link
                  href={`/runs/${encodeURIComponent(run.runId)}`}
                  className="font-medium text-teal-800 underline dark:text-teal-300"
                >
                  Open run detail
                </Link>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

type RunsListAggregateErrorBoundaryProps = RunsListClientProps;

type RunsListAggregateErrorBoundaryState = {
  hasError: boolean;
  message: string | null;
};

/**
 * Wraps the runs grid so catastrophic client failures can recover with a minimal demo-friendly table instead of swapping the `/runs` route error segment.
 */
export class RunsListAggregateErrorBoundary extends Component<
  RunsListAggregateErrorBoundaryProps,
  RunsListAggregateErrorBoundaryState
> {
  public state: RunsListAggregateErrorBoundaryState = { hasError: false, message: null };

  public static getDerivedStateFromError(error: Error): RunsListAggregateErrorBoundaryState {
    return { hasError: true, message: error.message || "Runs list encountered an unexpected error." };
  }

  public componentDidCatch(error: Error, errorInfo: ErrorInfo): void {
    console.error("RunsListAggregateErrorBoundary", error, errorInfo.componentStack);
  }

  public override render(): ReactNode {
    if (!this.state.hasError) {
      return <RunsListClient {...this.props} />;
    }

    const demoPaged = tryStaticDemoRunSummariesPaged(this.props.projectId);

    if (isNextPublicDemoMode() && demoPaged !== null && demoPaged.items.length > 0) {
      return (
        <div className="mt-4 space-y-4" role="alert">
          <p className="m-0 max-w-prose rounded-md border border-amber-200 bg-amber-50/80 px-3 py-2 text-sm text-neutral-900 dark:border-amber-900 dark:bg-amber-950/40 dark:text-neutral-50">
            <strong className="font-semibold">Showing sample run data.</strong> The live grid hit a client rendering
            issue; demo mode substitutes the Claims Intake row so navigation stays usable.
          </p>
          <OperatorDemoStaticBanner />
          <RunsListMinimalDemoTable runs={demoPaged.items} />
          <Button
            type="button"
            variant="outline"
            onClick={() => {
              this.setState({ hasError: false, message: null });
            }}
          >
            Retry live grid
          </Button>
        </div>
      );
    }

    const isDev = process.env.NODE_ENV === "development";

    return (
      <div
        className="mt-6 max-w-xl space-y-3 rounded-lg border border-red-200 bg-red-50/90 p-4 text-sm text-red-950 dark:border-red-900 dark:bg-red-950/40 dark:text-red-100"
        role="alert"
      >
        <p className="m-0 font-semibold">Reviews could not render</p>
        {isDev && this.state.message !== null ? (
          <p className="m-0 font-mono text-xs opacity-95">{this.state.message}</p>
        ) : (
          <p className="m-0 text-sm opacity-95">
            This review list hit an unexpected error. You can retry or return to Home for a fresh start.
          </p>
        )}
        <div className="flex flex-wrap gap-2">
          <Button
            type="button"
            variant="primary"
            onClick={() => {
              this.setState({ hasError: false, message: null });
            }}
          >
            Retry
          </Button>
          <Button type="button" variant="outline" asChild>
            <Link href="/runs?projectId=default">Back to reviews</Link>
          </Button>
        </div>
      </div>
    );
  }
}
