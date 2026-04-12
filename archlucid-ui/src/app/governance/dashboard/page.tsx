"use client";

import { useCallback, useEffect, useState } from "react";

import { useRouter } from "next/navigation";

import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { OperatorEmptyState } from "@/components/OperatorShellMessage";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import { getGovernanceDashboard } from "@/lib/api";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import { formatIsoUtcForDisplay } from "@/lib/format-iso-utc";
import { cn } from "@/lib/utils";
import type { GovernanceDashboardSummary } from "@/types/governance-dashboard";
import type { GovernanceApprovalRequest } from "@/types/governance-workflow";

function statusBadgeClass(status: string): string {
  switch (status) {
    case "Submitted":
      return "border-transparent bg-blue-600 text-white hover:bg-blue-600/90 dark:bg-blue-600 dark:hover:bg-blue-600/90";
    case "Approved":
      return "border-transparent bg-emerald-600 text-white hover:bg-emerald-600/90 dark:bg-emerald-600 dark:hover:bg-emerald-600/90";
    case "Rejected":
      return "border-transparent bg-red-600 text-white hover:bg-red-600/90 dark:bg-red-600 dark:hover:bg-red-600/90";
    case "Promoted":
      return "border-transparent bg-violet-600 text-white hover:bg-violet-600/90 dark:bg-violet-600 dark:hover:bg-violet-600/90";
    case "Activated":
      return "border-transparent bg-teal-600 text-white hover:bg-teal-600/90 dark:bg-teal-600 dark:hover:bg-teal-600/90";
    case "Draft":
    default:
      return "border-oklch(0.922 0 0) bg-oklch(0.97 0 0) text-oklch(0.205 0 0) dark:border-oklch(1 0 0 / 10%) dark:bg-oklch(0.269 0 0) dark:text-oklch(0.985 0 0)";
  }
}

function GovernanceStatusBadge({ status }: { status: string }) {
  return (
    <Badge className={cn("text-xs font-semibold", statusBadgeClass(status))} variant="outline">
      {status}
    </Badge>
  );
}

function DashboardSkeleton() {
  return (
    <div className="grid gap-6" aria-busy="true" aria-label="Loading governance dashboard">
      {[1, 2, 3].map((i) => (
        <Card key={i}>
          <CardHeader>
            <div className="h-5 w-48 animate-pulse rounded-md bg-neutral-200 dark:bg-neutral-700" />
            <div className="mt-2 h-4 w-full max-w-md animate-pulse rounded-md bg-neutral-200 dark:bg-neutral-700" />
          </CardHeader>
          <CardContent className="grid gap-3">
            <div className="h-24 animate-pulse rounded-md bg-neutral-200 dark:bg-neutral-700" />
            <div className="h-24 animate-pulse rounded-md bg-neutral-200 dark:bg-neutral-700" />
          </CardContent>
        </Card>
      ))}
    </div>
  );
}

function navigateToWorkflowReview(router: ReturnType<typeof useRouter>, runId: string) {
  router.push(`/governance?runId=${encodeURIComponent(runId)}`);
}

export default function GovernanceDashboardPage() {
  const router = useRouter();
  const [summary, setSummary] = useState<GovernanceDashboardSummary | null>(null);
  const [failure, setFailure] = useState<ApiLoadFailureState | null>(null);
  const [initialLoad, setInitialLoad] = useState(true);

  const loadDashboard = useCallback(async (isInitial: boolean) => {
    if (isInitial) {
      setInitialLoad(true);
    }

    try {
      const next = await getGovernanceDashboard();
      setSummary(next);
      setFailure(null);
    } catch (e) {
      setFailure(toApiLoadFailure(e));

      if (isInitial) {
        setSummary(null);
      }
    } finally {
      if (isInitial) {
        setInitialLoad(false);
      }
    }
  }, []);

  useEffect(() => {
    void loadDashboard(true);
    const intervalId = window.setInterval(() => void loadDashboard(false), 30_000);

    return () => window.clearInterval(intervalId);
  }, [loadDashboard]);

  const pending = summary?.pendingApprovals ?? [];
  const decisions = summary?.recentDecisions ?? [];
  const changes = summary?.recentChanges ?? [];
  const pendingCount = summary?.pendingCount ?? 0;

  return (
    <main id="main-content" className="mx-auto max-w-4xl px-1 sm:px-0">
      <h2 className="mt-0 text-2xl font-semibold tracking-tight">Governance dashboard</h2>
      <p className="text-sm text-neutral-600 dark:text-neutral-400">
        Cross-run view of pending approvals, recent decisions, and policy pack changes for the current tenant scope.
        Refreshes every 30 seconds. Use <strong>Review</strong> to open the run-scoped workflow.
      </p>

      {failure !== null ? (
        <div className="mb-6" role="alert">
          <OperatorApiProblem
            problem={failure.problem}
            fallbackMessage={failure.message}
            correlationId={failure.correlationId}
          />
        </div>
      ) : null}

      {initialLoad ? <DashboardSkeleton /> : null}

      {!initialLoad && summary !== null ? (
        <>
          <section
            className="mb-10"
            aria-labelledby="gov-dash-pending-heading"
          >
            <div className="mb-4 flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
              <div className="flex flex-wrap items-center gap-2">
                <h3 id="gov-dash-pending-heading" className="text-lg font-semibold">
                  Pending approvals
                </h3>
                {pendingCount > 0 ? (
                  <Badge
                    data-testid="governance-dashboard-pending-count-badge"
                    className="border-transparent bg-amber-600 text-white hover:bg-amber-600/90 dark:bg-amber-600"
                    variant="outline"
                  >
                    {pendingCount} open
                  </Badge>
                ) : null}
              </div>
            </div>

            {pending.length === 0 ? (
              <OperatorEmptyState title="No pending approvals — all clear.">
                <p className="text-sm">Nothing requires review in Draft or Submitted state.</p>
              </OperatorEmptyState>
            ) : (
              <div className="grid gap-4">
                {pending.map((row: GovernanceApprovalRequest) => (
                  <Card key={row.approvalRequestId} className="border-l-4 border-l-amber-500">
                    <CardHeader className="flex flex-row flex-wrap items-start justify-between gap-2 space-y-0">
                      <div>
                        <CardTitle className="text-base font-semibold">
                          <span className="font-mono text-sm">{row.runId}</span>
                          <span className="mx-1 text-neutral-400">·</span>
                          {row.sourceEnvironment} → {row.targetEnvironment}
                        </CardTitle>
                        <CardDescription>
                          Manifest <code className="text-xs">{row.manifestVersion}</code>
                        </CardDescription>
                      </div>
                      <GovernanceStatusBadge status={row.status} />
                    </CardHeader>
                    <CardContent className="grid gap-2 text-sm">
                      <div>
                        <span className="text-neutral-500 dark:text-neutral-400">Requested by</span>{" "}
                        {row.requestedBy}
                      </div>
                      <div>
                        <span className="text-neutral-500 dark:text-neutral-400">Requested</span>{" "}
                        {formatIsoUtcForDisplay(row.requestedUtc)}
                      </div>
                    </CardContent>
                    <CardFooter>
                      <Button
                        type="button"
                        size="sm"
                        onClick={() => navigateToWorkflowReview(router, row.runId)}
                      >
                        Review
                      </Button>
                    </CardFooter>
                  </Card>
                ))}
              </div>
            )}
          </section>

          <Separator className="mb-10" />

          <section className="mb-10" aria-labelledby="gov-dash-decisions-heading">
            <h3 id="gov-dash-decisions-heading" className="mb-4 text-lg font-semibold">
              Recent decisions
            </h3>
            {decisions.length === 0 ? (
              <OperatorEmptyState title="No recent decisions.">
                <p className="text-sm">Approved, rejected, and promoted rows will appear here.</p>
              </OperatorEmptyState>
            ) : (
              <div className="relative grid gap-4 border-l-2 border-neutral-200 pl-4 dark:border-neutral-700">
                {decisions.map((row: GovernanceApprovalRequest) => (
                  <Card key={row.approvalRequestId} className="relative">
                    <div
                      className="absolute -left-[calc(0.5rem+2px)] top-6 h-3 w-3 -translate-x-1/2 rounded-full border-2 border-neutral-200 bg-background dark:border-neutral-600"
                      aria-hidden
                    />
                    <CardHeader className="flex flex-row flex-wrap items-start justify-between gap-2 space-y-0 pb-2">
                      <div>
                        <CardTitle className="text-base font-semibold">
                          <span className="font-mono text-sm">{row.runId}</span>
                        </CardTitle>
                        <CardDescription>
                          {row.reviewedUtc ? formatIsoUtcForDisplay(row.reviewedUtc) : "—"}
                        </CardDescription>
                      </div>
                      <GovernanceStatusBadge status={row.status} />
                    </CardHeader>
                    <CardContent className="grid gap-2 text-sm">
                      <div>
                        <span className="text-neutral-500 dark:text-neutral-400">Reviewed by</span>{" "}
                        {row.reviewedBy ?? "—"}
                      </div>
                      {row.reviewComment ? (
                        <div>
                          <span className="text-neutral-500 dark:text-neutral-400">Comment</span> {row.reviewComment}
                        </div>
                      ) : null}
                    </CardContent>
                  </Card>
                ))}
              </div>
            )}
          </section>

          <Separator className="mb-10" />

          <section aria-labelledby="gov-dash-changes-heading">
            <h3 id="gov-dash-changes-heading" className="mb-4 text-lg font-semibold">
              Policy pack change log
            </h3>
            {changes.length === 0 ? (
              <OperatorEmptyState title="No policy pack changes recorded.">
                <p className="text-sm">Publish or assign packs to generate audit rows for this tenant.</p>
              </OperatorEmptyState>
            ) : (
              <div className="grid gap-3">
                {changes.map((c) => (
                  <Card key={c.changeLogId}>
                    <CardHeader className="pb-2">
                      <div className="flex flex-wrap items-center justify-between gap-2">
                        <CardTitle className="text-base font-medium">{c.changeType}</CardTitle>
                        <span className="font-mono text-xs text-neutral-600 dark:text-neutral-400">
                          {c.policyPackId}
                        </span>
                      </div>
                      <CardDescription>
                        {c.changedBy} · {formatIsoUtcForDisplay(c.changedUtc)}
                      </CardDescription>
                    </CardHeader>
                    {c.summaryText ? (
                      <CardContent className="pt-0 text-sm text-neutral-700 dark:text-neutral-300">
                        {c.summaryText}
                      </CardContent>
                    ) : null}
                  </Card>
                ))}
              </div>
            )}
          </section>
        </>
      ) : null}
    </main>
  );
}
