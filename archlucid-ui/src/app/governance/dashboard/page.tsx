"use client";

import { useCallback, useEffect, useMemo, useState } from "react";

import Link from "next/link";
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
import { ComplianceDriftChart } from "@/components/ComplianceDriftChart";
import { ConfirmationDialog } from "@/components/ConfirmationDialog";
import {
  approveRequest,
  getComplianceDriftTrend,
  getGovernanceDashboard,
  rejectRequest,
} from "@/lib/api";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import { formatIsoUtcForDisplay } from "@/lib/format-iso-utc";
import { showError, showSuccess } from "@/lib/toast";
import { cn } from "@/lib/utils";
import type {
  ComplianceDriftTrendPoint,
  GovernanceDashboardSummary,
} from "@/types/governance-dashboard";
import type { GovernanceApprovalRequest } from "@/types/governance-workflow";

import { governanceStatusBadgeClass } from "./governance-status-badge-class";

const EMPTY_PENDING_APPROVALS: GovernanceApprovalRequest[] = [];

function GovernanceStatusBadge({ status }: { status: string }) {
  return (
    <Badge className={cn("text-xs font-semibold", governanceStatusBadgeClass(status))} variant="outline">
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
  const [trendPoints, setTrendPoints] = useState<ComplianceDriftTrendPoint[]>([]);
  const [failure, setFailure] = useState<ApiLoadFailureState | null>(null);
  const [initialLoad, setInitialLoad] = useState(true);
  const [reviewDialog, setReviewDialog] = useState<
    null | { mode: "approve" | "reject"; approvalRequestId: string; runId: string }
  >(null);
  const [reviewBusy, setReviewBusy] = useState(false);
  const [selectedApprovalIds, setSelectedApprovalIds] = useState<Set<string>>(() => new Set());
  const [batchDialog, setBatchDialog] = useState<null | { mode: "approve" | "reject" }>(null);
  const [batchBusy, setBatchBusy] = useState(false);

  const loadDashboard = useCallback(async (isInitial: boolean) => {
    if (isInitial) {
      setInitialLoad(true);
    }

    try {
      const next = await getGovernanceDashboard();
      setSummary(next);

      try {
        const to = new Date();
        const from = new Date(to.getTime() - 30 * 24 * 60 * 60 * 1000);
        const trend = await getComplianceDriftTrend(from.toISOString(), to.toISOString(), 1440);
        setTrendPoints(trend);
      } catch {
        setTrendPoints([]);
      }

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

  const pending = useMemo(
    () => summary?.pendingApprovals ?? EMPTY_PENDING_APPROVALS,
    [summary?.pendingApprovals],
  );
  const decisions = summary?.recentDecisions ?? [];
  const changes = summary?.recentChanges ?? [];
  const pendingCount = summary?.pendingCount ?? 0;

  const selectablePending = pending.filter(
    (row) => row.status === "Submitted" || row.status === "Draft",
  );

  useEffect(() => {
    setSelectedApprovalIds((previous) => {
      const next = new Set<string>();

      for (const id of previous) {
        if (pending.some((row) => row.approvalRequestId === id)) {
          next.add(id);
        }
      }

      return next;
    });
  }, [pending]);

  const allSelectableSelected =
    selectablePending.length > 0 && selectablePending.every((row) => selectedApprovalIds.has(row.approvalRequestId));

  function toggleSelectAll(): void {
    if (allSelectableSelected) {
      setSelectedApprovalIds(new Set());

      return;
    }

    setSelectedApprovalIds(new Set(selectablePending.map((row) => row.approvalRequestId)));
  }

  function toggleOne(approvalRequestId: string): void {
    setSelectedApprovalIds((previous) => {
      const next = new Set(previous);

      if (next.has(approvalRequestId)) {
        next.delete(approvalRequestId);
      } else {
        next.add(approvalRequestId);
      }

      return next;
    });
  }

  async function onConfirmDashboardReview() {
    if (reviewDialog === null) {
      return;
    }

    setReviewBusy(true);

    try {
      if (reviewDialog.mode === "approve") {
        await approveRequest(reviewDialog.approvalRequestId, {
          reviewComment: "Approved from governance dashboard",
        });
        showSuccess("Approval recorded.");
      } else {
        await rejectRequest(reviewDialog.approvalRequestId, {
          reviewComment: "Rejected from governance dashboard",
        });
        showSuccess("Rejection recorded.");
      }

      setReviewDialog(null);
      await loadDashboard(false);
    } catch (e) {
      const loadFailure = toApiLoadFailure(e);
      showError("Governance action failed", loadFailure.message);
      setFailure(loadFailure);
    } finally {
      setReviewBusy(false);
    }
  }

  async function onConfirmBatchReview(): Promise<void> {
    if (batchDialog === null) {
      return;
    }

    const ids = Array.from(selectedApprovalIds);

    if (ids.length === 0) {
      setBatchDialog(null);

      return;
    }

    setBatchBusy(true);

    try {
      const commentBase =
        batchDialog.mode === "approve"
          ? "Batch approved from governance dashboard"
          : "Batch rejected from governance dashboard";
      const results = await Promise.allSettled(
        ids.map((approvalRequestId) =>
          batchDialog.mode === "approve"
            ? approveRequest(approvalRequestId, { reviewComment: commentBase })
            : rejectRequest(approvalRequestId, { reviewComment: commentBase }),
        ),
      );
      const failed = results.filter((result) => result.status === "rejected");
      const ok = results.length - failed.length;

      if (failed.length === 0) {
        showSuccess(
          batchDialog.mode === "approve"
            ? `Approved ${ok} request${ok === 1 ? "" : "s"}.`
            : `Rejected ${ok} request${ok === 1 ? "" : "s"}.`,
        );
      } else {
        const firstReason =
          failed[0].status === "rejected"
            ? String((failed[0].reason as Error)?.message ?? "Unknown error")
            : "";
        showError(
          `${batchDialog.mode === "approve" ? "Approve" : "Reject"} completed with failures`,
          `${ok} succeeded, ${failed.length} failed. ${firstReason}`,
        );
      }

      setBatchDialog(null);
      setSelectedApprovalIds(new Set());
      await loadDashboard(false);
    } catch (e) {
      const loadFailure = toApiLoadFailure(e);
      showError("Batch governance action failed", loadFailure.message);
      setFailure(loadFailure);
    } finally {
      setBatchBusy(false);
    }
  }

  return (
    <main className="mx-auto max-w-4xl px-1 sm:px-0">
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

      {reviewDialog !== null ? (
        <ConfirmationDialog
          open
          onOpenChange={(open) => {
            if (!open) {
              setReviewDialog(null);
            }
          }}
          title={reviewDialog.mode === "approve" ? "Approve this request?" : "Reject this request?"}
          description={
            reviewDialog.mode === "approve"
              ? `Approve promotion workflow for run ${reviewDialog.runId}. You cannot approve a request you submitted (segregation of duties).`
              : `Reject promotion workflow for run ${reviewDialog.runId}. This records a governance decision.`
          }
          confirmLabel={reviewDialog.mode === "approve" ? "Approve" : "Reject"}
          variant={reviewDialog.mode === "approve" ? "default" : "destructive"}
          onConfirm={() => void onConfirmDashboardReview()}
          busy={reviewBusy}
        />
      ) : null}

      {batchDialog !== null ? (
        <ConfirmationDialog
          open
          onOpenChange={(open) => {
            if (!open) {
              setBatchDialog(null);
            }
          }}
          title={
            batchDialog.mode === "approve"
              ? `Approve ${selectedApprovalIds.size} request${selectedApprovalIds.size === 1 ? "" : "s"}?`
              : `Reject ${selectedApprovalIds.size} request${selectedApprovalIds.size === 1 ? "" : "s"}?`
          }
          description={
            batchDialog.mode === "approve"
              ? "Each selected request is approved with a batch comment. Segregation of duties still applies per request — some may fail if you are the requester."
              : "Each selected request is rejected with a batch comment."
          }
          confirmLabel={batchDialog.mode === "approve" ? "Approve all" : "Reject all"}
          variant={batchDialog.mode === "approve" ? "default" : "destructive"}
          onConfirm={() => void onConfirmBatchReview()}
          busy={batchBusy}
        />
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
                {selectablePending.length > 0 ? (
                  <label className="flex cursor-pointer items-center gap-2 text-sm text-neutral-700 dark:text-neutral-300">
                    <input
                      type="checkbox"
                      className="h-4 w-4 rounded border-neutral-300 dark:border-neutral-600"
                      checked={allSelectableSelected}
                      onChange={() => {
                        toggleSelectAll();
                      }}
                      aria-label="Select all pending requests that can be approved or rejected"
                    />
                    Select all ({selectablePending.length})
                  </label>
                ) : null}
              </div>
            </div>

            {selectedApprovalIds.size > 0 ? (
              <div
                className="mb-4 flex flex-wrap items-center gap-2 rounded-md border border-amber-200 bg-amber-50 p-3 text-sm dark:border-amber-900 dark:bg-amber-950/40"
                role="region"
                aria-label="Batch approval actions"
              >
                <span className="font-medium text-neutral-800 dark:text-neutral-100">
                  {selectedApprovalIds.size} selected
                </span>
                <Button
                  type="button"
                  size="sm"
                  variant="secondary"
                  onClick={() => {
                    setBatchDialog({ mode: "approve" });
                  }}
                >
                  Approve selected
                </Button>
                <Button
                  type="button"
                  size="sm"
                  variant="destructive"
                  onClick={() => {
                    setBatchDialog({ mode: "reject" });
                  }}
                >
                  Reject selected
                </Button>
                <Button
                  type="button"
                  size="sm"
                  variant="outline"
                  onClick={() => {
                    setSelectedApprovalIds(new Set());
                  }}
                >
                  Clear selection
                </Button>
              </div>
            ) : null}

            {pending.length === 0 ? (
              <OperatorEmptyState title="No pending approvals — all clear.">
                <p className="text-sm">Nothing requires review in Draft or Submitted state.</p>
              </OperatorEmptyState>
            ) : (
              <div className="grid gap-4">
                {pending.map((row: GovernanceApprovalRequest) => (
                  <Card key={row.approvalRequestId} className="border-l-4 border-l-amber-500">
                    <CardHeader className="flex flex-row flex-wrap items-start justify-between gap-2 space-y-0">
                      <div className="flex min-w-0 flex-1 flex-wrap items-start gap-3">
                        {(row.status === "Submitted" || row.status === "Draft") ? (
                          <input
                            type="checkbox"
                            className="mt-1 h-4 w-4 shrink-0 rounded border-neutral-300 dark:border-neutral-600"
                            checked={selectedApprovalIds.has(row.approvalRequestId)}
                            onChange={() => {
                              toggleOne(row.approvalRequestId);
                            }}
                            aria-label={`Select approval request for run ${row.runId}`}
                          />
                        ) : null}
                        <div className="min-w-0">
                        <CardTitle className="text-base font-semibold">
                          <span className="font-mono text-sm">{row.runId}</span>
                          <span className="mx-1 text-neutral-400">·</span>
                          {row.sourceEnvironment} → {row.targetEnvironment}
                        </CardTitle>
                        <CardDescription>
                          Manifest <code className="text-xs">{row.manifestVersion}</code>
                        </CardDescription>
                        </div>
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
                    <CardFooter className="flex flex-wrap gap-2">
                      <Button type="button" size="sm" variant="outline" asChild>
                        <Link
                          href={`/governance/approval-requests/${encodeURIComponent(row.approvalRequestId)}/lineage`}
                        >
                          Lineage
                        </Link>
                      </Button>
                      {(row.status === "Submitted" || row.status === "Draft") && (
                        <>
                          <Button
                            type="button"
                            size="sm"
                            variant="secondary"
                            onClick={() =>
                              setReviewDialog({
                                mode: "approve",
                                approvalRequestId: row.approvalRequestId,
                                runId: row.runId,
                              })
                            }
                          >
                            Approve
                          </Button>
                          <Button
                            type="button"
                            size="sm"
                            variant="destructive"
                            onClick={() =>
                              setReviewDialog({
                                mode: "reject",
                                approvalRequestId: row.approvalRequestId,
                                runId: row.runId,
                              })
                            }
                          >
                            Reject
                          </Button>
                        </>
                      )}
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

          <section className="mb-10" aria-labelledby="gov-dash-drift-heading">
            <h3 id="gov-dash-drift-heading" className="mb-4 text-lg font-semibold">
              Compliance drift trend (last 30 days)
            </h3>
            <p className="mb-4 text-sm text-neutral-600 dark:text-neutral-400">
              Daily counts of policy pack mutations from the change log (same source as the list below).
            </p>
            <ComplianceDriftChart points={trendPoints} />
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
