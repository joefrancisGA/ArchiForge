"use client";

import { Suspense, useCallback, useEffect, useRef, useState } from "react";

import { useSearchParams } from "next/navigation";

import { ConfirmationDialog } from "@/components/ConfirmationDialog";
import { EmptyState } from "@/components/EmptyState";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { OperatorEmptyState, OperatorLoadingNotice } from "@/components/OperatorShellMessage";
import { GOVERNANCE_WORKFLOW_IDLE, GOVERNANCE_WORKFLOW_IDLE_READER } from "@/lib/empty-state-presets";
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
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Separator } from "@/components/ui/separator";
import { Textarea } from "@/components/ui/textarea";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip";
import { EnterpriseControlsExecutePageHint } from "@/components/EnterpriseControlsContextHints";
import { LayerHeader } from "@/components/LayerHeader";
import {
  activateEnvironment,
  approveRequest,
  listActivations,
  listApprovalRequests,
  listPromotions,
  promoteManifest,
  rejectRequest,
  submitApprovalRequest,
} from "@/lib/api";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import { formatIsoUtcForDisplay } from "@/lib/format-iso-utc";
import {
  enterpriseMutationControlDisabledTitle,
  governanceWorkflowPageLeadOperator,
  governanceWorkflowPageLeadReader,
  governanceWorkflowActivationsEmptyOperatorHint,
  governanceWorkflowActivationsEmptyReaderHint,
  governanceWorkflowNoApprovalsOperatorHint,
  governanceWorkflowNoApprovalsReaderHint,
  governanceWorkflowPromotionsEmptyOperatorHint,
  governanceWorkflowPromotionsEmptyReaderHint,
  governanceWorkflowQueryCardDescriptionOperator,
  governanceWorkflowQueryCardDescriptionReader,
  governanceWorkflowPendingReviewReaderNote,
  governanceWorkflowSubmitCardDescriptionReader,
} from "@/lib/enterprise-controls-context-copy";
import { useEnterpriseMutationCapability } from "@/hooks/use-enterprise-mutation-capability";
import { cn } from "@/lib/utils";
import type {
  GovernanceApprovalRequest,
  GovernanceEnvironmentActivation,
  GovernancePromotionRecord,
} from "@/types/governance-workflow";

/** API values (ArchLucid.Contracts.Governance.GovernanceEnvironment). */
const ENV_OPTIONS = [
  { value: "dev", label: "Development" },
  { value: "test", label: "Staging" },
  { value: "prod", label: "Production" },
] as const;

type ToastState = { kind: "ok" | "err"; message: string } | null;

type PendingReview = { approvalRequestId: string; mode: "approve" | "reject" };

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

function GovernanceWorkflowPageInner() {
  const searchParams = useSearchParams();
  const canMutateWorkflow = useEnterpriseMutationCapability();
  const [toast, setToast] = useState<ToastState>(null);

  const [submitRunId, setSubmitRunId] = useState("");
  const [submitManifestVersion, setSubmitManifestVersion] = useState("");
  const [submitSource, setSubmitSource] = useState<string>("dev");
  const [submitTarget, setSubmitTarget] = useState<string>("test");
  const [submitComment, setSubmitComment] = useState("");
  const [submitBusy, setSubmitBusy] = useState(false);

  const [queryRunId, setQueryRunId] = useState("");
  const [activeRunId, setActiveRunId] = useState<string | null>(null);
  const [workflowActor, setWorkflowActor] = useState("");

  const [approvals, setApprovals] = useState<GovernanceApprovalRequest[]>([]);
  const [promotions, setPromotions] = useState<GovernancePromotionRecord[]>([]);
  const [activations, setActivations] = useState<GovernanceEnvironmentActivation[]>([]);
  const [listsLoading, setListsLoading] = useState(false);
  const [listFailure, setListFailure] = useState<ApiLoadFailureState | null>(null);

  const [pendingReview, setPendingReview] = useState<PendingReview | null>(null);
  const [reviewedBy, setReviewedBy] = useState("");
  const [reviewComment, setReviewComment] = useState("");
  const [reviewBusy, setReviewBusy] = useState(false);

  const [promoteBusy, setPromoteBusy] = useState(false);

  const [pendingPromote, setPendingPromote] = useState<{
    manifestId: string;
    targetEnv: string;
  } | null>(null);
  const pendingPromoteRequestRef = useRef<GovernanceApprovalRequest | null>(null);

  const [pendingActivate, setPendingActivate] = useState<{
    activationId: string;
    env: string;
  } | null>(null);
  const pendingActivatePromotionRef = useRef<GovernancePromotionRecord | null>(null);

  const [activateBusyId, setActivateBusyId] = useState<string | null>(null);

  useEffect(() => {
    if (toast === null) {
      return;
    }

    const handle = window.setTimeout(() => setToast(null), 5000);

    return () => window.clearTimeout(handle);
  }, [toast]);

  useEffect(() => {
    if (canMutateWorkflow) {
      return;
    }

    setPendingReview(null);
    setPendingPromote(null);
    pendingPromoteRequestRef.current = null;
    setPendingActivate(null);
    pendingActivatePromotionRef.current = null;
  }, [canMutateWorkflow]);

  useEffect(() => {
    const fromQuery = searchParams.get("runId");

    if (fromQuery?.trim()) {
      setQueryRunId(fromQuery.trim());
    }
  }, [searchParams]);

  const loadLists = useCallback(async (runId: string) => {
    setListsLoading(true);
    setListFailure(null);

    try {
      setApprovals([]);
      setPromotions([]);
      setActivations([]);

      const [a, p, act] = await Promise.all([
        listApprovalRequests(runId),
        listPromotions(runId),
        listActivations(runId),
      ]);
      setApprovals(a);
      setPromotions(
        [...p].sort((x, y) => (x.promotedUtc < y.promotedUtc ? 1 : x.promotedUtc > y.promotedUtc ? -1 : 0)),
      );
      setActivations(
        [...act].sort((x, y) => (x.activatedUtc < y.activatedUtc ? 1 : x.activatedUtc > y.activatedUtc ? -1 : 0)),
      );
    } catch (e) {
      setListFailure(toApiLoadFailure(e));
      setApprovals([]);
      setPromotions([]);
      setActivations([]);
    } finally {
      setListsLoading(false);
    }
  }, []);

  const onLoadRun = useCallback(() => {
    const id = queryRunId.trim();

    if (!id) {
      setToast({ kind: "err", message: "Enter a run ID to load approval data." });

      return;
    }

    setActiveRunId(id);
    void loadLists(id);
  }, [queryRunId, loadLists]);

  const refreshIfActive = useCallback(async () => {
    if (activeRunId !== null) {
      await loadLists(activeRunId);
    }
  }, [activeRunId, loadLists]);

  async function onSubmitApproval() {
    if (!canMutateWorkflow) {
      return;
    }

    const runId = submitRunId.trim();

    if (!runId || !submitManifestVersion.trim()) {
      setToast({ kind: "err", message: "Run ID and manifest version are required." });

      return;
    }

    setSubmitBusy(true);

    try {
      await submitApprovalRequest({
        runId,
        manifestVersion: submitManifestVersion.trim(),
        sourceEnvironment: submitSource,
        targetEnvironment: submitTarget,
        requestComment: submitComment.trim() || undefined,
      });
      setToast({ kind: "ok", message: "Approval request submitted." });
      setSubmitComment("");

      if (activeRunId === runId) {
        await loadLists(runId);
      }
    } catch (e) {
      const f = toApiLoadFailure(e);
      setToast({ kind: "err", message: f.message });
    } finally {
      setSubmitBusy(false);
    }
  }

  async function onConfirmReview() {
    if (pendingReview === null) {
      return;
    }

    if (!canMutateWorkflow) {
      return;
    }

    if (!reviewedBy.trim()) {
      setToast({ kind: "err", message: "Reviewed by is required." });

      return;
    }

    setReviewBusy(true);

    try {
      if (pendingReview.mode === "approve") {
        await approveRequest(pendingReview.approvalRequestId, {
          reviewedBy: reviewedBy.trim(),
          reviewComment: reviewComment.trim() || undefined,
        });
        setToast({ kind: "ok", message: "Request approved." });
      } else {
        await rejectRequest(pendingReview.approvalRequestId, {
          reviewedBy: reviewedBy.trim(),
          reviewComment: reviewComment.trim() || undefined,
        });
        setToast({ kind: "ok", message: "Request rejected." });
      }

      setPendingReview(null);
      setReviewedBy("");
      setReviewComment("");
      await refreshIfActive();
    } catch (e) {
      const f = toApiLoadFailure(e);
      setToast({ kind: "err", message: f.message });
    } finally {
      setReviewBusy(false);
    }
  }

  async function onConfirmPromote() {
    const promoteFor = pendingPromoteRequestRef.current;

    if (promoteFor === null) {
      return;
    }

    if (!canMutateWorkflow) {
      return;
    }

    const by = workflowActor.trim();

    if (!by) {
      setToast({ kind: "err", message: "Set Acting as (for promote & activate) before promoting." });

      return;
    }

    setPromoteBusy(true);

    try {
      await promoteManifest({
        runId: promoteFor.runId,
        manifestVersion: promoteFor.manifestVersion,
        sourceEnvironment: promoteFor.sourceEnvironment,
        targetEnvironment: promoteFor.targetEnvironment,
        promotedBy: by,
        approvalRequestId: promoteFor.approvalRequestId ?? undefined,
      });
      setToast({ kind: "ok", message: "Manifest promoted." });
      setPendingPromote(null);
      pendingPromoteRequestRef.current = null;
      await refreshIfActive();
    } catch (e) {
      const f = toApiLoadFailure(e);
      setToast({ kind: "err", message: f.message });
    } finally {
      setPromoteBusy(false);
    }
  }

  async function onConfirmActivateFromPromotion() {
    const row = pendingActivatePromotionRef.current;

    if (row === null) {
      return;
    }

    if (!canMutateWorkflow) {
      return;
    }

    const by = workflowActor.trim();

    if (!by) {
      setToast({ kind: "err", message: "Set Acting as (for promote & activate) before activating." });

      return;
    }

    setActivateBusyId(row.promotionRecordId);

    try {
      await activateEnvironment({
        runId: row.runId,
        manifestVersion: row.manifestVersion,
        environment: row.targetEnvironment,
        activatedBy: by,
      });
      setToast({ kind: "ok", message: `Activated ${row.manifestVersion} for ${row.targetEnvironment}.` });
      setPendingActivate(null);
      pendingActivatePromotionRef.current = null;
      await refreshIfActive();
    } catch (e) {
      const f = toApiLoadFailure(e);
      setToast({ kind: "err", message: f.message });
    } finally {
      setActivateBusyId(null);
    }
  }

  return (
    <TooltipProvider delayDuration={300}>
    <main className="mx-auto max-w-4xl">
      <LayerHeader pageKey="governance-workflow" />
      <h2 className="mt-0 text-2xl font-semibold tracking-tight">Governance workflow</h2>
      <p className="max-w-prose text-sm leading-snug text-neutral-600 dark:text-neutral-400">
        {canMutateWorkflow ? governanceWorkflowPageLeadOperator : governanceWorkflowPageLeadReader}
      </p>
      <EnterpriseControlsExecutePageHint />

      {toast ? (
        <div
          role="status"
          className={cn(
            "fixed bottom-6 right-6 z-50 max-w-sm rounded-lg px-4 py-3 text-sm shadow-lg",
            toast.kind === "ok"
              ? "border border-emerald-200 bg-emerald-50 text-emerald-900 dark:border-emerald-900 dark:bg-emerald-950/80 dark:text-emerald-100"
              : "border border-red-200 bg-red-50 text-red-900 dark:border-red-900 dark:bg-red-950/80 dark:text-red-100",
          )}
        >
          {toast.message}
        </div>
      ) : null}

      {listFailure !== null ? (
        <div className="mb-6" role="alert">
          <OperatorApiProblem
            problem={listFailure.problem}
            fallbackMessage={listFailure.message}
            correlationId={listFailure.correlationId}
          />
        </div>
      ) : null}

      {activeRunId === null && !listsLoading && listFailure === null ? (
        <div className="mb-6">
          <EmptyState {...(canMutateWorkflow ? GOVERNANCE_WORKFLOW_IDLE : GOVERNANCE_WORKFLOW_IDLE_READER)} />
        </div>
      ) : null}

      <div className={cn(canMutateWorkflow ? "flex flex-col" : "flex flex-col-reverse")}>
      <section className="mb-10">
        <Card>
          <CardHeader>
            <CardTitle>Submit approval request</CardTitle>
            <CardDescription>
              {canMutateWorkflow ? (
                <>
                  Creates a workflow row for promoting a run manifest from a source environment to a target (API:{" "}
                  <code className="text-xs">POST /v1/governance/approval-requests</code>).
                </>
              ) : (
                governanceWorkflowSubmitCardDescriptionReader
              )}
            </CardDescription>
          </CardHeader>
          <CardContent className="grid gap-4">
            <div className="grid gap-2">
              <Label htmlFor="gov-submit-run">Run ID</Label>
              <Input
                id="gov-submit-run"
                value={submitRunId}
                onChange={(e) => setSubmitRunId(e.target.value)}
                placeholder="Architecture run identifier"
                autoComplete="off"
              />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="gov-submit-version">Manifest version</Label>
              <Input
                id="gov-submit-version"
                value={submitManifestVersion}
                onChange={(e) => setSubmitManifestVersion(e.target.value)}
                placeholder="e.g. v1.0.0"
                autoComplete="off"
              />
            </div>
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="grid gap-2">
                <Label htmlFor="gov-submit-source-env">Source environment</Label>
                <Select value={submitSource} onValueChange={setSubmitSource}>
                  <SelectTrigger id="gov-submit-source-env" className="w-full">
                    <SelectValue placeholder="Source" />
                  </SelectTrigger>
                  <SelectContent>
                    {ENV_OPTIONS.map((o) => (
                      <SelectItem key={o.value} value={o.value}>
                        {o.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="grid gap-2">
                <Label htmlFor="gov-submit-target-env">Target environment</Label>
                <Select value={submitTarget} onValueChange={setSubmitTarget}>
                  <SelectTrigger id="gov-submit-target-env" className="w-full">
                    <SelectValue placeholder="Target" />
                  </SelectTrigger>
                  <SelectContent>
                    {ENV_OPTIONS.map((o) => (
                      <SelectItem key={o.value} value={o.value}>
                        {o.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </div>
            <div className="grid gap-2">
              <Label htmlFor="gov-submit-comment">Request comment (optional)</Label>
              <Textarea
                id="gov-submit-comment"
                value={submitComment}
                onChange={(e) => setSubmitComment(e.target.value)}
                rows={3}
                placeholder="Context for reviewers"
              />
            </div>
          </CardContent>
          <CardFooter>
            <Button
              type="button"
              onClick={() => void onSubmitApproval()}
              disabled={submitBusy || !canMutateWorkflow}
              title={canMutateWorkflow ? undefined : enterpriseMutationControlDisabledTitle}
            >
              {submitBusy ? "Submitting…" : "Submit for approval"}
            </Button>
          </CardFooter>
        </Card>
      </section>

      <Separator className="mb-10" />

      <section className="mb-10">
        <Card>
          <CardHeader>
            <CardTitle>Approval requests for a run</CardTitle>
            <CardDescription>
              {canMutateWorkflow
                ? governanceWorkflowQueryCardDescriptionOperator
                : governanceWorkflowQueryCardDescriptionReader}
            </CardDescription>
          </CardHeader>
          <CardContent className="grid gap-4">
            <div className="flex flex-col gap-3 sm:flex-row sm:items-end">
              <div className="grid flex-1 gap-2">
                <Label htmlFor="gov-query-run">Run ID</Label>
                <Input
                  id="gov-query-run"
                  value={queryRunId}
                  onChange={(e) => setQueryRunId(e.target.value)}
                  placeholder="Run to inspect"
                  autoComplete="off"
                />
              </div>
              <Button type="button" variant="secondary" onClick={onLoadRun} disabled={listsLoading}>
                {listsLoading ? "Loading…" : "Load"}
              </Button>
            </div>
            <div className="grid gap-2">
              <Label htmlFor="gov-workflow-actor">Acting as (for promote &amp; activate)</Label>
              <Input
                id="gov-workflow-actor"
                value={workflowActor}
                onChange={(e) => setWorkflowActor(e.target.value)}
                placeholder="Display name sent with promote/activate calls"
                autoComplete="username"
                disabled={!canMutateWorkflow}
                title={canMutateWorkflow ? undefined : enterpriseMutationControlDisabledTitle}
              />
              <p className="text-xs text-neutral-500 dark:text-neutral-400">
                The API still binds the authenticated principal; this field supplies the explicit{" "}
                <code className="text-xs">promotedBy</code> / UI contract for <code className="text-xs">activatedBy</code>.
              </p>
            </div>
          </CardContent>
        </Card>

        <div className="mt-6 grid gap-4">
          {listsLoading && activeRunId !== null ? (
            <OperatorLoadingNotice>
              <strong>Loading workflow data.</strong>
              <p className="mt-2 text-sm">Fetching approval requests, promotions, and activations.</p>
            </OperatorLoadingNotice>
          ) : null}

          {!listsLoading && activeRunId !== null && approvals.length === 0 && listFailure === null ? (
            <OperatorEmptyState title="No approval requests for this run">
              <p className="text-sm">
                {canMutateWorkflow
                  ? governanceWorkflowNoApprovalsOperatorHint
                  : governanceWorkflowNoApprovalsReaderHint}
              </p>
            </OperatorEmptyState>
          ) : null}

          {approvals.map((row) => (
            <Card key={row.approvalRequestId}>
              <CardHeader className="flex flex-row flex-wrap items-start justify-between gap-2 space-y-0">
                <div>
                  <CardTitle className="text-base font-semibold">
                    {row.sourceEnvironment} → {row.targetEnvironment}
                  </CardTitle>
                  <CardDescription className="font-mono text-xs">{row.approvalRequestId}</CardDescription>
                </div>
                <GovernanceStatusBadge status={row.status} />
              </CardHeader>
              <CardContent className="grid gap-2 text-sm">
                <div>
                  <span className="text-neutral-500 dark:text-neutral-400">Requested by</span> {row.requestedBy}
                </div>
                <div>
                  <span className="text-neutral-500 dark:text-neutral-400">Requested</span>{" "}
                  {formatIsoUtcForDisplay(row.requestedUtc)}
                </div>
                {row.requestComment ? (
                  <div>
                    <span className="text-neutral-500 dark:text-neutral-400">Comment</span> {row.requestComment}
                  </div>
                ) : null}
                {row.reviewedBy ? (
                  <div>
                    <span className="text-neutral-500 dark:text-neutral-400">Reviewed by</span> {row.reviewedBy}
                    {row.reviewedUtc ? ` · ${formatIsoUtcForDisplay(row.reviewedUtc)}` : null}
                  </div>
                ) : null}
                {row.reviewComment ? (
                  <div>
                    <span className="text-neutral-500 dark:text-neutral-400">Review comment</span> {row.reviewComment}
                  </div>
                ) : null}

                {pendingReview?.approvalRequestId === row.approvalRequestId ? (
                  <div className="mt-4 rounded-lg border border-neutral-200 p-4 dark:border-neutral-700">
                    <p className="mb-3 text-sm font-medium">
                      {pendingReview.mode === "approve" ? "Approve request" : "Reject request"}
                    </p>
                    {!canMutateWorkflow ? (
                      <p className="mb-3 text-xs text-neutral-600 dark:text-neutral-400" role="note">
                        {governanceWorkflowPendingReviewReaderNote}
                      </p>
                    ) : null}
                    <div className="grid gap-3">
                      <div className="grid gap-2">
                        <Label htmlFor={`review-by-${row.approvalRequestId}`}>Reviewed by</Label>
                        <Input
                          id={`review-by-${row.approvalRequestId}`}
                          value={reviewedBy}
                          onChange={(e) => setReviewedBy(e.target.value)}
                          autoComplete="username"
                          readOnly={!canMutateWorkflow}
                          title={canMutateWorkflow ? undefined : enterpriseMutationControlDisabledTitle}
                        />
                      </div>
                      <div className="grid gap-2">
                        <Label htmlFor={`review-comment-${row.approvalRequestId}`}>Review comment (optional)</Label>
                        <Textarea
                          id={`review-comment-${row.approvalRequestId}`}
                          value={reviewComment}
                          onChange={(e) => setReviewComment(e.target.value)}
                          rows={2}
                          readOnly={!canMutateWorkflow}
                          title={canMutateWorkflow ? undefined : enterpriseMutationControlDisabledTitle}
                        />
                      </div>
                      <div className="flex flex-wrap gap-2">
                        <Button
                          type="button"
                          size="sm"
                          onClick={() => void onConfirmReview()}
                          disabled={reviewBusy || !canMutateWorkflow}
                          title={canMutateWorkflow ? undefined : enterpriseMutationControlDisabledTitle}
                        >
                          {reviewBusy ? "Saving…" : "Submit"}
                        </Button>
                        <Button
                          type="button"
                          size="sm"
                          variant="outline"
                          onClick={() => {
                            setPendingReview(null);
                            setReviewedBy("");
                            setReviewComment("");
                          }}
                          disabled={reviewBusy}
                        >
                          Cancel
                        </Button>
                      </div>
                    </div>
                  </div>
                ) : null}

              </CardContent>
              <CardFooter className="flex flex-wrap gap-2">
                {row.status === "Submitted" ? (
                  <>
                    <Button
                      type="button"
                      size="sm"
                      variant={canMutateWorkflow ? "default" : "outline"}
                      disabled={!canMutateWorkflow}
                      title={canMutateWorkflow ? undefined : enterpriseMutationControlDisabledTitle}
                      onClick={() => {
                        setPendingReview({ approvalRequestId: row.approvalRequestId, mode: "approve" });
                        setPendingPromote(null);
                        pendingPromoteRequestRef.current = null;
                      }}
                    >
                      Approve
                    </Button>
                    <Button
                      type="button"
                      size="sm"
                      variant="outline"
                      className="border-red-300 text-red-700 hover:bg-red-50 dark:border-red-900 dark:text-red-300 dark:hover:bg-red-950/50"
                      disabled={!canMutateWorkflow}
                      title={canMutateWorkflow ? undefined : enterpriseMutationControlDisabledTitle}
                      onClick={() => {
                        setPendingReview({ approvalRequestId: row.approvalRequestId, mode: "reject" });
                        setPendingPromote(null);
                        pendingPromoteRequestRef.current = null;
                      }}
                    >
                      Reject
                    </Button>
                  </>
                ) : null}
                {row.status === "Approved" ? (
                  <Button
                    type="button"
                    size="sm"
                    variant={canMutateWorkflow ? "default" : "outline"}
                    className={
                      canMutateWorkflow
                        ? "bg-violet-600 text-white hover:bg-violet-600/90 dark:bg-violet-600 dark:hover:bg-violet-600/90"
                        : undefined
                    }
                    disabled={pendingPromote !== null || !canMutateWorkflow}
                    title={canMutateWorkflow ? undefined : enterpriseMutationControlDisabledTitle}
                    onClick={() => {
                      pendingPromoteRequestRef.current = row;
                      setPendingPromote({
                        manifestId: row.manifestVersion,
                        targetEnv: row.targetEnvironment,
                      });
                      setPendingReview(null);
                    }}
                  >
                    Promote
                  </Button>
                ) : null}
              </CardFooter>
            </Card>
          ))}
        </div>
      </section>
      </div>

      <Separator className="mb-10" />

      <section className="mb-10">
        <h3 className="mb-4 text-lg font-semibold">Promotions &amp; activations</h3>
        <p className="mb-4 text-sm text-neutral-600 dark:text-neutral-400">
          Timeline for run <span className="font-mono">{activeRunId ?? "—"}</span>. Activate applies the promoted
          manifest to the promotion&apos;s target environment.
        </p>

        {!listsLoading && activeRunId !== null && promotions.length === 0 && listFailure === null ? (
          <OperatorEmptyState title="No promotions recorded yet">
            <p className="text-sm">
              {canMutateWorkflow
                ? governanceWorkflowPromotionsEmptyOperatorHint
                : governanceWorkflowPromotionsEmptyReaderHint}
            </p>
          </OperatorEmptyState>
        ) : null}

        <div className="mb-8 grid gap-3">
          {promotions.map((p) => (
            <Card key={p.promotionRecordId} className="border-l-4 border-l-violet-500">
              <CardHeader className="pb-2">
                <CardTitle className="text-base">Promotion · {formatIsoUtcForDisplay(p.promotedUtc)}</CardTitle>
                <CardDescription className="font-mono text-xs">{p.promotionRecordId}</CardDescription>
              </CardHeader>
              <CardContent className="grid gap-1 text-sm">
                <div>
                  {p.sourceEnvironment} → <strong>{p.targetEnvironment}</strong> · manifest{" "}
                  <code className="text-xs">{p.manifestVersion}</code>
                </div>
                <div>By {p.promotedBy}</div>
                {p.notes ? <div>Notes: {p.notes}</div> : null}
              </CardContent>
              <CardFooter>
                <Tooltip>
                  <TooltipTrigger asChild>
                    <span className="inline-block">
                      <Button
                        type="button"
                        size="sm"
                        variant="secondary"
                        disabled={
                          pendingActivate !== null ||
                          activateBusyId === p.promotionRecordId ||
                          !workflowActor.trim() ||
                          !canMutateWorkflow
                        }
                        title={canMutateWorkflow ? undefined : enterpriseMutationControlDisabledTitle}
                        onClick={() => {
                          pendingActivatePromotionRef.current = p;
                          setPendingActivate({
                            activationId: p.promotionRecordId,
                            env: p.targetEnvironment,
                          });
                        }}
                      >
                        {activateBusyId === p.promotionRecordId ? "Activating…" : "Activate"}
                      </Button>
                    </span>
                  </TooltipTrigger>
                  <TooltipContent side="top" className="max-w-xs">
                    {!canMutateWorkflow
                      ? enterpriseMutationControlDisabledTitle
                      : !workflowActor.trim()
                        ? "Set Acting as (for promote & activate) to enable activation."
                        : "POST activation for this manifest on the promotion’s target environment."}
                  </TooltipContent>
                </Tooltip>
              </CardFooter>
            </Card>
          ))}
        </div>

        <h4 className="mb-3 text-base font-semibold">Activations</h4>

        {!listsLoading && activeRunId !== null && activations.length === 0 && listFailure === null ? (
          <OperatorEmptyState title="No activations recorded yet">
            <p className="text-sm">
              {canMutateWorkflow
                ? governanceWorkflowActivationsEmptyOperatorHint
                : governanceWorkflowActivationsEmptyReaderHint}
            </p>
          </OperatorEmptyState>
        ) : null}

        <div className="grid gap-3">
          {activations.map((a) => (
            <Card key={a.activationId} className="border-l-4 border-l-teal-500">
              <CardHeader className="pb-2">
                <CardTitle className="text-base">Activation · {formatIsoUtcForDisplay(a.activatedUtc)}</CardTitle>
                <CardDescription className="font-mono text-xs">{a.activationId}</CardDescription>
              </CardHeader>
              <CardContent className="grid gap-1 text-sm">
                <div>
                  Environment <strong>{a.environment}</strong> · manifest <code className="text-xs">{a.manifestVersion}</code>
                </div>
                <div>Active: {a.isActive ? "yes" : "no"}</div>
              </CardContent>
            </Card>
          ))}
        </div>
      </section>

      <ConfirmationDialog
        open={pendingPromote !== null}
        onOpenChange={(open) => {
          if (!open) {
            setPendingPromote(null);
            pendingPromoteRequestRef.current = null;
          }
        }}
        title="Promote manifest?"
        description={
          pendingPromote !== null
            ? `Promoting manifest ${pendingPromote.manifestId} to ${pendingPromote.targetEnv}. This will replace the current active manifest in that environment.`
            : ""
        }
        variant="default"
        confirmLabel="Promote"
        busy={promoteBusy}
        onConfirm={() => {
          void onConfirmPromote();
        }}
      />

      <ConfirmationDialog
        open={pendingActivate !== null}
        onOpenChange={(open) => {
          if (!open) {
            setPendingActivate(null);
            pendingActivatePromotionRef.current = null;
          }
        }}
        title="Activate environment?"
        description={
          pendingActivate !== null
            ? `Activating governance pack in ${pendingActivate.env}. This will apply the pack's rules to all future runs.`
            : ""
        }
        variant="default"
        confirmLabel="Activate"
        busy={
          pendingActivate !== null && activateBusyId === pendingActivate.activationId
        }
        onConfirm={() => {
          void onConfirmActivateFromPromotion();
        }}
      />
    </main>
    </TooltipProvider>
  );
}

function GovernanceWorkflowSuspenseFallback() {
  return (
    <main className="mx-auto max-w-4xl">
      <OperatorLoadingNotice>
        <strong>Loading governance workflow.</strong>
        <p className="mt-2 text-sm">Reading URL parameters…</p>
      </OperatorLoadingNotice>
    </main>
  );
}

export default function GovernanceWorkflowPage() {
  return (
    <Suspense fallback={<GovernanceWorkflowSuspenseFallback />}>
      <GovernanceWorkflowPageInner />
    </Suspense>
  );
}
