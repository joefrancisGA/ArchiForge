"use client";

import { useCallback, useEffect, useMemo, useState } from "react";

import Link from "next/link";
import { useRouter } from "next/navigation";

import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { OperatorEmptyState } from "@/components/OperatorShellMessage";
import { StatusPill } from "@/components/StatusPill";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
} from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import { GovernanceDashboardReaderActionCue } from "@/components/EnterpriseControlsContextHints";
import { GlossaryTooltip } from "@/components/GlossaryTooltip";
import { OperatorPageHeader } from "@/components/OperatorPageHeader";
import { LayerHeader } from "@/components/LayerHeader";
import { ComplianceDriftChart } from "@/components/ComplianceDriftChart";
import { ConfirmationDialog } from "@/components/ConfirmationDialog";
import {
  approvalRequestPrimaryLabel,
  GovernanceApprovalInspectorPreview,
} from "@/components/GovernanceApprovalInspectorPreview";
import { InspectorPanel } from "@/components/InspectorPanel";
import {
  approveRequest,
  getComplianceDriftTrend,
  getGovernanceDashboard,
  rejectRequest,
} from "@/lib/api";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import {
  enterpriseMutationControlDisabledTitle,
  governanceDashboardApproveSelectedButtonLabelReaderRank,
  governanceDashboardChangeLogHeadingOperator,
  governanceDashboardChangeLogHeadingReader,
  governanceDashboardComplianceDriftHeadingOperator,
  governanceDashboardComplianceDriftHeadingReader,
  governanceDashboardLineageLinkTitle,
  governanceDashboardOpenWorkflowReviewTitleOperator,
  governanceDashboardOpenWorkflowReviewTitleReader,
  governanceDashboardPendingApprovalsHeadingOperator,
  governanceDashboardPendingApprovalsHeadingReader,
  governanceDashboardPendingClearReaderSupplement,
  governanceDashboardRecentDecisionsHeadingOperator,
  governanceDashboardRecentDecisionsHeadingReader,
  governanceDashboardRejectSelectedButtonLabelReaderRank,
  governanceWorkflowApproveButtonLabelReaderRank,
  governanceWorkflowRejectButtonLabelReaderRank,
} from "@/lib/enterprise-controls-context-copy";
import { formatIsoUtcForDisplay } from "@/lib/format-iso-utc";
import { formatRelativeTime } from "@/lib/relative-time";
import { showError, showSuccess } from "@/lib/toast";
import { cn } from "@/lib/utils";
import type {
  ComplianceDriftTrendPoint,
  GovernanceDashboardSummary,
} from "@/types/governance-dashboard";
import type { GovernanceApprovalRequest } from "@/types/governance-workflow";

import { useNavSurface } from "@/lib/use-nav-surface";
import { useViewportNarrow } from "@/hooks/useViewportNarrow";

const EMPTY_PENDING_APPROVALS: GovernanceApprovalRequest[] = [];

function DashboardSkeleton() {
  return (
    <div className="grid gap-6" role="status" aria-busy="true" aria-label="Loading governance dashboard">
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
  const canMutateGovernance = useNavSurface("governance-dashboard").mutationCapability;
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
  const [selectedApproval, setSelectedApproval] = useState<GovernanceApprovalRequest | null>(null);
  const viewportNarrow = useViewportNarrow();

  const closeApprovalInspector = useCallback(() => {
    setSelectedApproval(null);
  }, []);

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

  useEffect(() => {
    if (canMutateGovernance) {
      return;
    }

    setSelectedApprovalIds(new Set());
    setReviewDialog(null);
    setBatchDialog(null);
  }, [canMutateGovernance]);

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

  useEffect(() => {
    if (selectedApproval === null) {
      return;
    }

    const exists = pending.some((r) => r.approvalRequestId === selectedApproval.approvalRequestId);

    if (!exists) {
      setSelectedApproval(null);
    }
  }, [pending, selectedApproval]);

  useEffect(() => {
    if (selectedApproval === null) {
      return;
    }

    function onKeyDown(e: KeyboardEvent) {
      if (e.key === "Escape") {
        closeApprovalInspector();
      }
    }

    window.addEventListener("keydown", onKeyDown);

    return () => {
      window.removeEventListener("keydown", onKeyDown);
    };
  }, [selectedApproval, closeApprovalInspector]);

  const activatePendingRow = useCallback((row: GovernanceApprovalRequest, e: React.MouseEvent<HTMLTableRowElement>) => {
    if ((e.target as HTMLElement).closest("a,button,input,label")) {
      return;
    }

    setSelectedApproval(row);
  }, []);

  const activatePendingRowFromKeyboard = useCallback(
    (e: React.KeyboardEvent<HTMLTableRowElement>, row: GovernanceApprovalRequest) => {
      if (e.key !== "Enter" && e.key !== " ") {
        return;
      }

      if ((e.target as HTMLElement).closest("a,button,input,label")) {
        return;
      }

      e.preventDefault();
      setSelectedApproval(row);
    },
    [],
  );

  async function onConfirmDashboardReview() {
    if (reviewDialog === null) {
      return;
    }

    if (!canMutateGovernance) {
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

    if (!canMutateGovernance) {
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

  const approvalInspectorTitle =
    selectedApproval === null ? "Request preview" : approvalRequestPrimaryLabel(selectedApproval);

  const approvalInspectorBody =
    selectedApproval === null ? (
      <p className="m-0 text-sm text-neutral-600 dark:text-neutral-400" data-testid="gov-approval-inspector-empty">
        Select a pending approval to preview details here.
      </p>
    ) : (
      <GovernanceApprovalInspectorPreview request={selectedApproval} />
    );

  return (
    <main className="mx-auto max-w-6xl px-1 sm:px-0">
      <LayerHeader pageKey="governance-dashboard" />
      <OperatorPageHeader title="Governance dashboard" helpKey="governance-dashboard" />
      <GovernanceDashboardReaderActionCue />

      <p className="mb-4 max-w-3xl text-sm leading-relaxed text-neutral-600 dark:text-neutral-400">
        Track <GlossaryTooltip termKey="approval_request">approval requests</GlossaryTooltip> and route work through{" "}
        <GlossaryTooltip termKey="governance_resolution">governance resolution</GlossaryTooltip> when policy requires a
        decision before promotion.
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
          <div className={cn("flex flex-col", !canMutateGovernance && "flex-col-reverse")}>
          <section
            className={cn("mb-10", !canMutateGovernance && "mt-10")}
            aria-labelledby="gov-dash-pending-heading"
          >
            <div className="mb-4 flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
              <div className="flex flex-wrap items-center gap-2">
                <h3 id="gov-dash-pending-heading" className="text-lg font-semibold">
                  {canMutateGovernance
                    ? governanceDashboardPendingApprovalsHeadingOperator
                    : governanceDashboardPendingApprovalsHeadingReader}
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
                  <label
                    className={`flex items-center gap-2 text-sm text-neutral-700 dark:text-neutral-300 ${
                      canMutateGovernance ? "cursor-pointer" : "cursor-not-allowed opacity-70"
                    }`}
                    title={canMutateGovernance ? undefined : enterpriseMutationControlDisabledTitle}
                  >
                    <input
                      type="checkbox"
                      className="h-4 w-4 rounded border-neutral-300 dark:border-neutral-600"
                      checked={allSelectableSelected}
                      onChange={() => {
                        toggleSelectAll();
                      }}
                      disabled={!canMutateGovernance}
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
                  variant={canMutateGovernance ? "secondary" : "outline"}
                  disabled={!canMutateGovernance}
                  title={canMutateGovernance ? undefined : enterpriseMutationControlDisabledTitle}
                  onClick={() => {
                    setBatchDialog({ mode: "approve" });
                  }}
                >
                  {canMutateGovernance ? "Approve selected" : governanceDashboardApproveSelectedButtonLabelReaderRank}
                </Button>
                <Button
                  type="button"
                  size="sm"
                  variant="destructive"
                  disabled={!canMutateGovernance}
                  title={canMutateGovernance ? undefined : enterpriseMutationControlDisabledTitle}
                  onClick={() => {
                    setBatchDialog({ mode: "reject" });
                  }}
                >
                  {canMutateGovernance ? "Reject selected" : governanceDashboardRejectSelectedButtonLabelReaderRank}
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
                {!canMutateGovernance ? (
                  <p className="mt-2 text-xs text-neutral-600 dark:text-neutral-400">
                    {governanceDashboardPendingClearReaderSupplement}
                  </p>
                ) : null}
              </OperatorEmptyState>
            ) : (
              <div className={cn(!viewportNarrow && "lg:flex lg:items-stretch lg:gap-4")}>
                <div className={cn("min-w-0 flex-1", !viewportNarrow && "lg:min-w-0")}>
                  <div className="overflow-x-auto rounded-md border border-neutral-200 dark:border-neutral-800">
                    <table className="w-full border-collapse text-sm">
                      <thead>
                        <tr className="border-b border-neutral-200 bg-neutral-50/80 dark:border-neutral-800 dark:bg-neutral-900/40">
                          <th
                            className="w-10 px-3 py-2 text-left text-xs font-semibold uppercase tracking-wide text-neutral-600 dark:text-neutral-400"
                            aria-label="Selection"
                          />
                          <th className="px-3 py-2 text-left text-xs font-semibold uppercase tracking-wide text-neutral-600 dark:text-neutral-400">
                            Request
                          </th>
                          <th className="px-3 py-2 text-left text-xs font-semibold uppercase tracking-wide text-neutral-600 dark:text-neutral-400">
                            Status
                          </th>
                          <th className="px-3 py-2 text-left text-xs font-semibold uppercase tracking-wide text-neutral-600 dark:text-neutral-400">
                            Requested
                          </th>
                          <th className="px-3 py-2 text-left text-xs font-semibold uppercase tracking-wide text-neutral-600 dark:text-neutral-400">
                            Actions
                          </th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-neutral-100 dark:divide-neutral-800">
                        {pending.map((row: GovernanceApprovalRequest) => {
                          const requestedFull = formatIsoUtcForDisplay(row.requestedUtc);
                          const isSelected = selectedApproval?.approvalRequestId === row.approvalRequestId;

                          return (
                            <tr
                              key={row.approvalRequestId}
                              data-testid={`gov-pending-row-${row.approvalRequestId}`}
                              tabIndex={0}
                              className={cn(
                                "cursor-pointer outline-none transition-colors focus-visible:ring-2 focus-visible:ring-teal-600 focus-visible:ring-offset-2 focus-visible:ring-offset-white dark:focus-visible:ring-offset-neutral-950",
                                isSelected
                                  ? "bg-teal-50/80 dark:bg-teal-950/30"
                                  : "hover:bg-neutral-50 dark:hover:bg-neutral-800",
                              )}
                              onClick={(e) => {
                                activatePendingRow(row, e);
                              }}
                              onKeyDown={(e) => {
                                activatePendingRowFromKeyboard(e, row);
                              }}
                            >
                              <td className="px-3 py-2 align-top">
                                {row.status === "Submitted" || row.status === "Draft" ? (
                                  <input
                                    type="checkbox"
                                    className="mt-1 h-4 w-4 rounded border-neutral-300 dark:border-neutral-600"
                                    checked={selectedApprovalIds.has(row.approvalRequestId)}
                                    onChange={() => {
                                      toggleOne(row.approvalRequestId);
                                    }}
                                    disabled={!canMutateGovernance}
                                    title={canMutateGovernance ? undefined : enterpriseMutationControlDisabledTitle}
                                    aria-label={`Select approval request for run ${row.runId}`}
                                  />
                                ) : null}
                              </td>
                              <td className="max-w-[min(100vw,22rem)] px-3 py-2 align-top">
                                <div className="flex min-w-0 flex-wrap items-center gap-x-2 gap-y-1">
                                  <span className="min-w-0 font-semibold text-sm text-neutral-900 dark:text-neutral-100">
                                    {approvalRequestPrimaryLabel(row)}
                                  </span>
                                </div>
                                <p className="m-0 mt-0.5 text-xs text-neutral-500 dark:text-neutral-400">
                                  Manifest <code className="text-xs">{row.manifestVersion}</code>
                                </p>
                                <Link
                                  href={`/runs/${encodeURIComponent(row.runId)}`}
                                  className="mt-1 inline-block break-all font-mono text-xs text-teal-800 underline dark:text-teal-300"
                                  onClick={(e) => {
                                    e.stopPropagation();
                                  }}
                                >
                                  {row.runId}
                                </Link>
                                <p className="m-0 mt-0.5 text-xs text-neutral-500 dark:text-neutral-400">
                                  Requested by <span className="text-neutral-700 dark:text-neutral-200">{row.requestedBy}</span>
                                </p>
                              </td>
                              <td className="whitespace-nowrap px-3 py-2 align-top">
                                <StatusPill status={row.status} domain="governance" className="text-xs" />
                              </td>
                              <td
                                className="whitespace-nowrap px-3 py-2 align-top text-xs text-neutral-600 dark:text-neutral-400"
                                title={requestedFull}
                              >
                                {formatRelativeTime(row.requestedUtc)}
                              </td>
                              <td className="px-3 py-2 align-top">
                                <div className="flex max-w-[14rem] flex-wrap gap-2">
                                  <Button type="button" size="sm" variant="outline" asChild>
                                    <Link
                                      href={`/governance/approval-requests/${encodeURIComponent(row.approvalRequestId)}/lineage`}
                                      title={governanceDashboardLineageLinkTitle}
                                      onClick={(e) => {
                                        e.stopPropagation();
                                      }}
                                    >
                                      Lineage
                                    </Link>
                                  </Button>
                                  {row.status === "Submitted" || row.status === "Draft" ? (
                                    <>
                                      <Button
                                        type="button"
                                        size="sm"
                                        variant={canMutateGovernance ? "secondary" : "outline"}
                                        disabled={!canMutateGovernance}
                                        title={canMutateGovernance ? undefined : enterpriseMutationControlDisabledTitle}
                                        onClick={(e) => {
                                          e.stopPropagation();
                                          setReviewDialog({
                                            mode: "approve",
                                            approvalRequestId: row.approvalRequestId,
                                            runId: row.runId,
                                          });
                                        }}
                                      >
                                        {canMutateGovernance ? "Approve" : governanceWorkflowApproveButtonLabelReaderRank}
                                      </Button>
                                      <Button
                                        type="button"
                                        size="sm"
                                        variant="destructive"
                                        disabled={!canMutateGovernance}
                                        title={canMutateGovernance ? undefined : enterpriseMutationControlDisabledTitle}
                                        onClick={(e) => {
                                          e.stopPropagation();
                                          setReviewDialog({
                                            mode: "reject",
                                            approvalRequestId: row.approvalRequestId,
                                            runId: row.runId,
                                          });
                                        }}
                                      >
                                        {canMutateGovernance ? "Reject" : governanceWorkflowRejectButtonLabelReaderRank}
                                      </Button>
                                    </>
                                  ) : null}
                                  <Button
                                    type="button"
                                    size="sm"
                                    title={
                                      canMutateGovernance
                                        ? governanceDashboardOpenWorkflowReviewTitleOperator
                                        : governanceDashboardOpenWorkflowReviewTitleReader
                                    }
                                    onClick={(e) => {
                                      e.stopPropagation();
                                      navigateToWorkflowReview(router, row.runId);
                                    }}
                                  >
                                    Review
                                  </Button>
                                </div>
                              </td>
                            </tr>
                          );
                        })}
                      </tbody>
                    </table>
                  </div>
                </div>

                {!viewportNarrow ? (
                  <InspectorPanel
                    title={approvalInspectorTitle}
                    onClose={closeApprovalInspector}
                    listenEscape={false}
                    className="mt-4 hidden min-h-[14rem] shrink-0 lg:mt-0 lg:flex"
                  >
                    {approvalInspectorBody}
                  </InspectorPanel>
                ) : null}
              </div>
            )}

            {viewportNarrow && selectedApproval !== null ? (
              <div className="fixed inset-0 z-40 flex justify-end" role="presentation">
                <button
                  type="button"
                  className="absolute inset-0 bg-black/40"
                  aria-label="Dismiss request inspector backdrop"
                  onClick={closeApprovalInspector}
                />
                <div className="animate-in slide-in-from-right relative h-full w-full max-w-sm duration-200 ease-out">
                  <InspectorPanel
                    title={approvalInspectorTitle}
                    onClose={closeApprovalInspector}
                    listenEscape={false}
                    className="h-full max-w-sm border-l-0 shadow-xl sm:border-l"
                    widthClassName="w-full"
                  >
                    {approvalInspectorBody}
                  </InspectorPanel>
                </div>
              </div>
            ) : null}
          </section>

          <div className="flex flex-col">
          <Separator className="mb-10" />

          <section className="mb-10" aria-labelledby="gov-dash-decisions-heading">
            <h3 id="gov-dash-decisions-heading" className="mb-4 text-lg font-semibold">
              {canMutateGovernance
                ? governanceDashboardRecentDecisionsHeadingOperator
                : governanceDashboardRecentDecisionsHeadingReader}
            </h3>
            {decisions.length === 0 ? (
              <OperatorEmptyState title="No recent decisions.">
                <p className="text-sm">Approved, rejected, and promoted rows will appear here.</p>
              </OperatorEmptyState>
            ) : (
              <div className="overflow-x-auto rounded-md border border-neutral-200 dark:border-neutral-800">
                <table className="w-full border-collapse text-sm">
                  <thead>
                    <tr className="border-b border-neutral-200 bg-neutral-50/80 dark:border-neutral-800 dark:bg-neutral-900/40">
                      <th className="px-3 py-2 text-left text-xs font-semibold uppercase tracking-wide text-neutral-600 dark:text-neutral-400">
                        Request
                      </th>
                      <th className="px-3 py-2 text-left text-xs font-semibold uppercase tracking-wide text-neutral-600 dark:text-neutral-400">
                        Status
                      </th>
                      <th className="px-3 py-2 text-left text-xs font-semibold uppercase tracking-wide text-neutral-600 dark:text-neutral-400">
                        Reviewed
                      </th>
                      <th className="px-3 py-2 text-left text-xs font-semibold uppercase tracking-wide text-neutral-600 dark:text-neutral-400">
                        Reviewer
                      </th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-neutral-100 dark:divide-neutral-800">
                    {decisions.map((row: GovernanceApprovalRequest) => {
                      const reviewedFull =
                        row.reviewedUtc !== null && row.reviewedUtc.length > 0
                          ? formatIsoUtcForDisplay(row.reviewedUtc)
                          : null;

                      return (
                        <tr
                          key={row.approvalRequestId}
                          data-testid={`gov-decision-row-${row.approvalRequestId}`}
                          className="hover:bg-neutral-50 dark:hover:bg-neutral-800"
                        >
                          <td className="max-w-[min(100vw,22rem)] px-3 py-2 align-top">
                            <div className="font-semibold text-sm text-neutral-900 dark:text-neutral-100">
                              {approvalRequestPrimaryLabel(row)}
                            </div>
                            <p className="m-0 mt-0.5 text-xs text-neutral-500 dark:text-neutral-400">
                              Manifest <code className="text-xs">{row.manifestVersion}</code>
                            </p>
                            <Link
                              href={`/runs/${encodeURIComponent(row.runId)}`}
                              className="mt-1 inline-block break-all font-mono text-xs text-teal-800 underline dark:text-teal-300"
                            >
                              {row.runId}
                            </Link>
                            {row.reviewComment !== null && row.reviewComment.trim().length > 0 ? (
                              <p className="m-0 mt-1 line-clamp-2 text-xs text-neutral-600 dark:text-neutral-400">
                                {row.reviewComment}
                              </p>
                            ) : null}
                          </td>
                          <td className="whitespace-nowrap px-3 py-2 align-top">
                            <StatusPill status={row.status} domain="governance" className="text-xs" />
                          </td>
                          <td
                            className="whitespace-nowrap px-3 py-2 align-top text-xs text-neutral-600 dark:text-neutral-400"
                            title={reviewedFull ?? undefined}
                          >
                            {row.reviewedUtc !== null && row.reviewedUtc.length > 0
                              ? formatRelativeTime(row.reviewedUtc)
                              : "—"}
                          </td>
                          <td className="px-3 py-2 align-top text-xs text-neutral-700 dark:text-neutral-300">
                            {row.reviewedBy ?? "—"}
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            )}
          </section>

          <Separator className="mb-10" />

          <section className="mb-10" aria-labelledby="gov-dash-drift-heading">
            <h3 id="gov-dash-drift-heading" className="mb-4 text-lg font-semibold">
              {canMutateGovernance
                ? governanceDashboardComplianceDriftHeadingOperator
                : governanceDashboardComplianceDriftHeadingReader}
            </h3>
            <p className="mb-4 text-sm text-neutral-600 dark:text-neutral-400">
              Daily counts of policy pack mutations from the change log (same source as the list below).
            </p>
            <ComplianceDriftChart points={trendPoints} />
          </section>

          <Separator className="mb-10" />

          <section aria-labelledby="gov-dash-changes-heading">
            <h3 id="gov-dash-changes-heading" className="mb-4 text-lg font-semibold">
              {canMutateGovernance
                ? governanceDashboardChangeLogHeadingOperator
                : governanceDashboardChangeLogHeadingReader}
            </h3>
            {changes.length === 0 ? (
              <OperatorEmptyState title="No policy pack changes recorded.">
                <p className="text-sm">Publish or assign packs to generate audit rows for this tenant.</p>
              </OperatorEmptyState>
            ) : (
              <div className="grid gap-3">
                {changes.map((c) => {
                  const changedFull = formatIsoUtcForDisplay(c.changedUtc);

                  return (
                    <Card key={c.changeLogId} className="border-neutral-200 dark:border-neutral-800">
                      <CardHeader className="pb-2">
                        <div className="flex flex-wrap items-center justify-between gap-2">
                          <StatusPill
                            status={c.changeType}
                            domain="general"
                            uppercase={false}
                            className="max-w-full text-xs"
                            ariaLabel={`Change type: ${c.changeType}`}
                          />
                          <span className="font-mono text-xs text-neutral-600 dark:text-neutral-400">
                            {c.policyPackId}
                          </span>
                        </div>
                        <CardDescription className="mt-1">
                          <span title={changedFull}>{formatRelativeTime(c.changedUtc)}</span>
                          <span className="text-neutral-400"> · </span>
                          {c.changedBy}
                        </CardDescription>
                      </CardHeader>
                      {c.summaryText ? (
                        <CardContent className="pt-0 text-sm text-neutral-700 dark:text-neutral-300">
                          {c.summaryText}
                        </CardContent>
                      ) : null}
                    </Card>
                  );
                })}
              </div>
            )}
          </section>
          </div>
          </div>
        </>
      ) : null}
    </main>
  );
}
