"use client";

import { Suspense, useCallback, useEffect, useRef, useState } from "react";

import { useSearchParams } from "next/navigation";

import { AskRunIdPicker } from "@/components/AskRunIdPicker";
import { MutationErrorBoundary } from "@/components/MutationErrorBoundary";
import { ConfirmationDialog } from "@/components/ConfirmationDialog";
import { EmptyState } from "@/components/EmptyState";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { OperatorEmptyState, OperatorLoadingNotice } from "@/components/OperatorShellMessage";
import { StatusPill } from "@/components/StatusPill";
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
import { OperatorPageHeader } from "@/components/OperatorPageHeader";
import { RunIdPicker } from "@/components/RunIdPicker";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip";
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
import { GOVERNANCE_WORKFLOW_IDLE, GOVERNANCE_WORKFLOW_IDLE_READER } from "@/lib/empty-state-presets";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import { formatIsoUtcForDisplay } from "@/lib/format-iso-utc";
import {
  enterpriseMutationControlDisabledTitle,
  governanceWorkflowActivateButtonLabelReaderRank,
  governanceWorkflowApprovalRequestsCardTitleOperator,
  governanceWorkflowApprovalRequestsCardTitleReader,
  governanceWorkflowApproveButtonLabelReaderRank,
  governanceWorkflowActivationsSubheadingOperator,
  governanceWorkflowActivationsSubheadingReader,
  governanceWorkflowPageLeadOperator,
  governanceWorkflowPageLeadReader,
  governanceWorkflowActivationsEmptyOperatorHint,
  governanceWorkflowActivationsEmptyReaderHint,
  governanceWorkflowNoApprovalsOperatorHint,
  governanceWorkflowNoApprovalsReaderHint,
  governanceWorkflowPromoteButtonLabelReaderRank,
  governanceWorkflowPromotionsActivationsHeadingOperator,
  governanceWorkflowPromotionsActivationsHeadingReader,
  governanceWorkflowPromotionsActivationsSectionLeadOperator,
  governanceWorkflowPromotionsActivationsSectionLeadReader,
  governanceWorkflowPromotionsEmptyOperatorHint,
  governanceWorkflowPromotionsEmptyReaderHint,
  governanceWorkflowQueryCardDescriptionOperator,
  governanceWorkflowQueryCardDescriptionReader,
  governanceWorkflowPendingReviewReaderNote,
  governanceWorkflowRejectButtonLabelReaderRank,
  governanceWorkflowReviewSubmitButtonLabelReaderRank,
  governanceWorkflowRefreshRunDataButtonLabel,
  governanceWorkflowRefreshRunDataTitle,
  governanceWorkflowSubmitCardDescriptionReader,
  governanceWorkflowSubmitCardTitleOperator,
  governanceWorkflowSubmitCardTitleReader,
  governanceWorkflowSubmitForApprovalButtonLabelReaderRank,
} from "@/lib/enterprise-controls-context-copy";
import { useEnterpriseMutationCapability } from "@/hooks/use-enterprise-mutation-capability";
import { cn } from "@/lib/utils";
import { isBuyerSafeDemoMarketingChromeEnv } from "@/lib/demo-ui-env";
import {
  isStaticDemoPayloadFallbackEnabled,
  tryStaticDemoGovernanceApprovalRequests,
  tryStaticDemoGovernancePromotions,
} from "@/lib/operator-static-demo";
import { SHOWCASE_STATIC_DEMO_RUN_ID } from "@/lib/showcase-static-demo";
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

  const demoPrefillRanRef = useRef(false);

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
      let nextApprovals = a;
      let nextPromotions = p;

      if (nextApprovals.length === 0) {
        const seeded = tryStaticDemoGovernanceApprovalRequests(runId);

        if (seeded !== null) {
          nextApprovals = seeded;
        }
      }

      if (nextPromotions.length === 0) {
        const seededP = tryStaticDemoGovernancePromotions(runId);

        if (seededP !== null) {
          nextPromotions = seededP;
        }
      }

      setApprovals(nextApprovals);
      setPromotions(
        [...nextPromotions].sort((x, y) => (x.promotedUtc < y.promotedUtc ? 1 : x.promotedUtc > y.promotedUtc ? -1 : 0)),
      );
      setActivations(
        [...act].sort((x, y) => (x.activatedUtc < y.activatedUtc ? 1 : x.activatedUtc > y.activatedUtc ? -1 : 0)),
      );
    } catch (e) {
      const fail = toApiLoadFailure(e);
      setApprovals([]);
      setPromotions([]);
      setActivations([]);

      const idForDemo = runId.trim();

      if (idForDemo.length > 0) {
        const seeded = tryStaticDemoGovernanceApprovalRequests(idForDemo);
        const seededP = tryStaticDemoGovernancePromotions(idForDemo);

        if (seeded !== null) {
          setApprovals(seeded);
        }

        if (seededP !== null) {
          setPromotions(
            [...seededP].sort((x, y) => (x.promotedUtc < y.promotedUtc ? 1 : x.promotedUtc > y.promotedUtc ? -1 : 0)),
          );
        }

        if (seeded !== null || seededP !== null) {
          setListFailure(null);
          setListsLoading(false);

          return;
        }
      }

      setListFailure(fail);
    } finally {
      setListsLoading(false);
    }
  }, []);

  const onLoadRun = useCallback(() => {
    const id = queryRunId.trim();

    if (!id) {
      setToast({ kind: "err", message: "Choose a run to load approval data." });

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

  useEffect(() => {
    if (!isStaticDemoPayloadFallbackEnabled()) {
      return;
    }

    if (demoPrefillRanRef.current) {
      return;
    }

    const fromSearch = searchParams.get("runId")?.trim() ?? "";

    if (fromSearch.length > 0) {
      demoPrefillRanRef.current = true;

      return;
    }

    if (queryRunId.trim().length > 0) {
      demoPrefillRanRef.current = true;

      return;
    }

    demoPrefillRanRef.current = true;
    setQueryRunId(SHOWCASE_STATIC_DEMO_RUN_ID);
    setActiveRunId(SHOWCASE_STATIC_DEMO_RUN_ID);
    void loadLists(SHOWCASE_STATIC_DEMO_RUN_ID);
  }, [searchParams, queryRunId, loadLists]);

  async function onSubmitApproval() {
    if (!canMutateWorkflow) {
      return;
    }

    const runId = submitRunId.trim();

    if (!runId || !submitManifestVersion.trim()) {
      setToast({ kind: "err", message: "Choose a run and enter a manifest version." });

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
      setToast({ kind: "err", message: "Enter your name for the audit trail before promoting." });

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
      setToast({ kind: "err", message: "Enter your name for the audit trail before activating." });

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
    <MutationErrorBoundary title="Governance workflow failed to render">
    <TooltipProvider delayDuration={300}>
    <main className="mx-auto max-w-4xl">
      <LayerHeader pageKey="governance-workflow" />
      <OperatorPageHeader
        title="Governance workflow"
        subtitle={canMutateWorkflow ? governanceWorkflowPageLeadOperator : governanceWorkflowPageLeadReader}
      />

      {isBuyerSafeDemoMarketingChromeEnv() ? (
        <div className="mb-6 rounded-md border border-violet-200 bg-violet-50/70 px-4 py-3 text-sm text-neutral-900 dark:border-violet-900 dark:bg-violet-950/40 dark:text-neutral-50">
          <strong>Governance approval workflow</strong>
          {" — "}for full demo walkthrough, contact your account team.
        </div>
      ) : null}

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
        <Card className={cn(!canMutateWorkflow && "opacity-95")}>
          <CardHeader>
            <CardTitle>
              {canMutateWorkflow ? governanceWorkflowSubmitCardTitleOperator : governanceWorkflowSubmitCardTitleReader}
            </CardTitle>
            <CardDescription>
              {canMutateWorkflow ? (
                <>
                  Starts an approval request so reviewers can promote your finalized manifest from a source environment
                  toward a target (for example staging to production).
                </>
              ) : (
                governanceWorkflowSubmitCardDescriptionReader
              )}
            </CardDescription>
          </CardHeader>
          <CardContent className="grid gap-4">
            <div className="grid gap-2">
              <AskRunIdPicker
                fieldId="gov-submit-run"
                label="Run"
                value={submitRunId}
                onChange={setSubmitRunId}
                selectedThreadId=""
                preferAutoPick={canMutateWorkflow}
                disabled={!canMutateWorkflow}
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
                readOnly={!canMutateWorkflow}
                title={canMutateWorkflow ? undefined : enterpriseMutationControlDisabledTitle}
              />
            </div>
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="grid gap-2">
                <Label htmlFor="gov-submit-source-env">Source environment</Label>
                <Select value={submitSource} onValueChange={setSubmitSource} disabled={!canMutateWorkflow}>
                  <SelectTrigger
                    id="gov-submit-source-env"
                    className="w-full"
                    title={canMutateWorkflow ? undefined : enterpriseMutationControlDisabledTitle}
                  >
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
                <Select value={submitTarget} onValueChange={setSubmitTarget} disabled={!canMutateWorkflow}>
                  <SelectTrigger
                    id="gov-submit-target-env"
                    className="w-full"
                    title={canMutateWorkflow ? undefined : enterpriseMutationControlDisabledTitle}
                  >
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
                readOnly={!canMutateWorkflow}
                title={canMutateWorkflow ? undefined : enterpriseMutationControlDisabledTitle}
              />
            </div>
          </CardContent>
          <CardFooter className="flex flex-col items-stretch gap-3">
            <Button
              type="button"
              data-testid="governance-submit-approval-button"
              onClick={() => void onSubmitApproval()}
              disabled={submitBusy || !canMutateWorkflow || submitRunId.trim().length === 0}
              title={canMutateWorkflow ? undefined : enterpriseMutationControlDisabledTitle}
            >
              {submitBusy
                ? "Submitting…"
                : canMutateWorkflow
                  ? "Submit for governance approval"
                  : governanceWorkflowSubmitForApprovalButtonLabelReaderRank}
            </Button>
            {!canMutateWorkflow ? (
              <p className="m-0 text-xs text-neutral-600 dark:text-neutral-400" role="note">
                Submitting for governance approval requires Execute access on your account. You can still review
                approvals below; contact your ArchLucid account team if this should be enabled for your workspace.
              </p>
            ) : null}
          </CardFooter>
        </Card>
      </section>

      <Separator className="mb-10" />

      <section className="mb-10">
        <Card>
          <CardHeader>
            <CardTitle>
              {canMutateWorkflow
                ? governanceWorkflowApprovalRequestsCardTitleOperator
                : governanceWorkflowApprovalRequestsCardTitleReader}
            </CardTitle>
            <CardDescription>
              {canMutateWorkflow
                ? governanceWorkflowQueryCardDescriptionOperator
                : governanceWorkflowQueryCardDescriptionReader}
            </CardDescription>
          </CardHeader>
          <CardContent className="grid gap-4">
            <div className="flex flex-col gap-3 sm:flex-row sm:flex-wrap sm:items-end">
              <div className="grid min-w-0 flex-1 gap-2">
                <RunIdPicker
                  inputId="gov-query-run"
                  label="Run"
                  placeholder="Select a run from the list"
                  value={queryRunId}
                  onChange={setQueryRunId}
                  onSelect={(id) => {
                    setQueryRunId(id);
                    setActiveRunId(id);
                    void loadLists(id);
                  }}
                />
              </div>
              <div className="flex flex-wrap gap-2">
                <Button type="button" variant="secondary" onClick={onLoadRun} disabled={listsLoading}>
                  {listsLoading ? "Loading…" : "Load"}
                </Button>
                {activeRunId !== null ? (
                  <Button
                    type="button"
                    variant="outline"
                    onClick={() => void refreshIfActive()}
                    disabled={listsLoading}
                    title={governanceWorkflowRefreshRunDataTitle}
                  >
                    {listsLoading ? "Refreshing…" : governanceWorkflowRefreshRunDataButtonLabel}
                  </Button>
                ) : null}
              </div>
            </div>
            {canMutateWorkflow ? (
              <div className="grid gap-2">
                <Label htmlFor="gov-workflow-actor">Your name for the audit trail (promote and activate)</Label>
                <Input
                  id="gov-workflow-actor"
                  value={workflowActor}
                  onChange={(e) => setWorkflowActor(e.target.value)}
                  placeholder="Display name recorded with promote and activate actions"
                  autoComplete="username"
                  title={canMutateWorkflow ? undefined : enterpriseMutationControlDisabledTitle}
                />
                <p className="text-xs text-neutral-500 dark:text-neutral-400">
                  This is stored with promotion and activation records alongside your signed-in account.
                </p>
              </div>
            ) : null}
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
                <StatusPill status={row.status} domain="governance" className="text-xs" />
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
                          variant={canMutateWorkflow ? "default" : "outline"}
                          onClick={() => void onConfirmReview()}
                          disabled={reviewBusy || !canMutateWorkflow}
                          title={canMutateWorkflow ? undefined : enterpriseMutationControlDisabledTitle}
                        >
                          {reviewBusy
                            ? "Saving…"
                            : canMutateWorkflow
                              ? "Submit"
                              : governanceWorkflowReviewSubmitButtonLabelReaderRank}
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
                      {canMutateWorkflow ? "Approve" : governanceWorkflowApproveButtonLabelReaderRank}
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
                      {canMutateWorkflow ? "Reject" : governanceWorkflowRejectButtonLabelReaderRank}
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
                    {canMutateWorkflow ? "Promote" : governanceWorkflowPromoteButtonLabelReaderRank}
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
        <h3 className="mb-4 text-lg font-semibold">
          {canMutateWorkflow
            ? governanceWorkflowPromotionsActivationsHeadingOperator
            : governanceWorkflowPromotionsActivationsHeadingReader}
        </h3>
        <p className="mb-2 max-w-prose text-sm text-neutral-600 dark:text-neutral-400">
          {canMutateWorkflow
            ? governanceWorkflowPromotionsActivationsSectionLeadOperator
            : governanceWorkflowPromotionsActivationsSectionLeadReader}
        </p>
        <p className="mb-4 text-xs text-neutral-500 dark:text-neutral-500">
          Run <span className="font-mono">{activeRunId ?? "—"}</span> · promotions newest first; activations follow.
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
                        {activateBusyId === p.promotionRecordId
                          ? "Activating…"
                          : canMutateWorkflow
                            ? "Activate"
                            : governanceWorkflowActivateButtonLabelReaderRank}
                      </Button>
                    </span>
                  </TooltipTrigger>
                  <TooltipContent side="top" className="max-w-xs">
                    {!canMutateWorkflow
                      ? enterpriseMutationControlDisabledTitle
                      : !workflowActor.trim()
                        ? "Enter your name for the audit trail to enable activation."
                        : "POST activation for this manifest on the promotion’s target environment."}
                  </TooltipContent>
                </Tooltip>
              </CardFooter>
            </Card>
          ))}
        </div>

        <h4 className="mb-3 text-base font-semibold">
          {canMutateWorkflow
            ? governanceWorkflowActivationsSubheadingOperator
            : governanceWorkflowActivationsSubheadingReader}
        </h4>

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
    </MutationErrorBoundary>
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
