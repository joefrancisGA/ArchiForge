"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";

import { AlertsInboxRankCue } from "@/components/EnterpriseControlsContextHints";
import { LayerHeader } from "@/components/LayerHeader";
import { EmptyState } from "@/components/EmptyState";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { OperatorLoadingNotice, OperatorTryNext } from "@/components/OperatorShellMessage";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { ALERTS_EMPTY_FILTERED } from "@/lib/empty-state-presets";
import {
  alertsFilteredEmptyDescriptionOperator,
  alertsFilteredEmptyDescriptionReader,
  enterpriseMutationControlDisabledTitle,
} from "@/lib/enterprise-controls-context-copy";
import { useAlertCardShortcuts } from "@/hooks/useAlertCardShortcuts";
import { useEnterpriseMutationCapability } from "@/hooks/use-enterprise-mutation-capability";
import { applyAlertAction, listAlertsPaged } from "@/lib/api";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import { cn } from "@/lib/utils";
import type { AlertRecord } from "@/types/alerts";

const ALERTS_PAGE_SIZE = 25;

/** Radix Select requires non-empty values; maps to null API filter for “all statuses”. */
const ALL_STATUSES_VALUE = "__all__";

type AlertActionKind = "Acknowledge" | "Resolve" | "Suppress";

type PendingActionState = {
  alertId: string;
  action: AlertActionKind;
};

function severityBadgeClass(severity: string): string {
  const key = severity.trim().toLowerCase();

  if (key === "critical") {
    return "border-transparent bg-red-600 text-white hover:bg-red-600/90 dark:bg-red-600 dark:hover:bg-red-600/90";
  }

  if (key === "high") {
    return "border-transparent bg-orange-600 text-white hover:bg-orange-600/90 dark:bg-orange-600 dark:hover:bg-orange-600/90";
  }

  if (key === "medium") {
    return "border-transparent bg-amber-500 text-white hover:bg-amber-500/90 dark:bg-amber-500 dark:hover:bg-amber-500/90";
  }

  return "border-neutral-200 bg-neutral-100 text-neutral-800 dark:border-neutral-700 dark:bg-neutral-800 dark:text-neutral-100";
}

export default function AlertsPage() {
  const canMutateAlertInbox = useEnterpriseMutationCapability();
  const [alerts, setAlerts] = useState<AlertRecord[]>([]);
  const [status, setStatus] = useState<string>("Open");
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(false);
  const [failure, setFailure] = useState<ApiLoadFailureState | null>(null);
  const [pendingAction, setPendingAction] = useState<PendingActionState | null>(null);
  const [actionComment, setActionComment] = useState("");
  const [actionBusy, setActionBusy] = useState(false);

  const totalPages = Math.max(1, Math.ceil(totalCount / ALERTS_PAGE_SIZE));

  const load = useCallback(async () => {
    setLoading(true);
    setFailure(null);

    try {
      const statusFilter = status === ALL_STATUSES_VALUE ? null : status;
      const data = await listAlertsPaged(statusFilter, page, ALERTS_PAGE_SIZE);
      setAlerts(data.items);
      setTotalCount(data.totalCount);
      const pages = Math.max(1, Math.ceil(data.totalCount / ALERTS_PAGE_SIZE));

      if (data.totalCount > 0 && page > pages) {
        setPage(pages);
      }
    } catch (e) {
      setFailure(toApiLoadFailure(e));
    } finally {
      setLoading(false);
    }
  }, [status, page]);

  useEffect(() => {
    void load();
  }, [load]);

  const emptyFilteredProps = useMemo(() => {
    const description = canMutateAlertInbox
      ? alertsFilteredEmptyDescriptionOperator
      : alertsFilteredEmptyDescriptionReader;

    const actions = canMutateAlertInbox
      ? [
          { label: "View runs list", href: "/runs?projectId=default" },
          {
            label: "Alert tooling (rules, routing, tuning)",
            href: "/alert-rules",
            variant: "outline" as const,
          },
        ]
      : [
          { label: "View runs list", href: "/runs?projectId=default" },
          {
            label: "Review alert tooling (read-only)",
            href: "/alert-rules",
            variant: "outline" as const,
          },
        ];

    return {
      ...ALERTS_EMPTY_FILTERED,
      title: "No alerts match this filter",
      description,
      actions,
    };
  }, [canMutateAlertInbox]);

  const act = useCallback(
    async (alertId: string, action: AlertActionKind, comment: string) => {
      setFailure(null);

      try {
        await applyAlertAction(alertId, action, comment);
        await load();
      } catch (e) {
        setFailure(toApiLoadFailure(e));
      }
    },
    [load],
  );

  const onAlertShortcutAction = useCallback(
    (alertId: string, action: string) => {
      if (!canMutateAlertInbox) {
        return;
      }

      if (action === "Acknowledge" || action === "Resolve" || action === "Suppress") {
        setPendingAction({ alertId, action });
        setActionComment("");
      }
    },
    [canMutateAlertInbox],
  );

  useAlertCardShortcuts({ onAction: onAlertShortcutAction, mutationsEnabled: canMutateAlertInbox });

  async function onConfirmActionDialog(): Promise<void> {
    if (pendingAction === null || !canMutateAlertInbox) {
      return;
    }

    setActionBusy(true);

    try {
      await act(pendingAction.alertId, pendingAction.action, actionComment.trim());
      setPendingAction(null);
      setActionComment("");
    } finally {
      setActionBusy(false);
    }
  }

  return (
    <main className="mx-auto max-w-3xl">
      <LayerHeader pageKey="alerts" />
      <h2 className="mt-0 text-2xl font-semibold tracking-tight text-neutral-900 dark:text-neutral-100">Alerts</h2>
      <p className="max-w-prose text-sm font-medium leading-snug text-neutral-800 dark:text-neutral-200">
        Inbox: filter and read signals first; triage only when a row needs a state change. Rules, routing, and tuning are
        off this page.
      </p>
      <p className="mt-1 max-w-prose text-xs leading-relaxed text-neutral-500 dark:text-neutral-400">
        First pilot: skip if alerts are not in scope. Dedupe spans statuses; shortcuts use the same gate as triage
        buttons below.
      </p>
      <AlertsInboxRankCue />

      {failure !== null ? (
        <div className="mb-4" role="alert">
          <OperatorApiProblem
            problem={failure.problem}
            fallbackMessage={failure.message}
            correlationId={failure.correlationId}
          />
          <OperatorTryNext>
            Confirm the API and proxy are up, then click <strong>Refresh</strong>. Alerts come from scheduled scans—if
            the list should not be empty, check worker schedules and{" "}
            <Link className="font-medium text-teal-800 underline dark:text-teal-300" href="/">
              Home
            </Link>{" "}
            for environment guidance.
          </OperatorTryNext>
        </div>
      ) : null}

      <div className="mb-4 flex flex-wrap items-end gap-3">
        <div className="grid gap-2">
          <Label htmlFor="alerts-status-filter">Status filter</Label>
          <Select
            value={status}
            onValueChange={(value) => {
              setStatus(value);
              setPage(1);
            }}
          >
            <SelectTrigger id="alerts-status-filter" className="w-[200px]">
              <SelectValue placeholder="Status" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={ALL_STATUSES_VALUE}>All</SelectItem>
              <SelectItem value="Open">Open</SelectItem>
              <SelectItem value="Acknowledged">Acknowledged</SelectItem>
              <SelectItem value="Resolved">Resolved</SelectItem>
              <SelectItem value="Suppressed">Suppressed</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <Button type="button" variant="secondary" onClick={() => void load()} disabled={loading}>
          {loading ? "Loading…" : "Refresh"}
        </Button>
      </div>

      <span className="mb-4 mt-1 block text-xs text-neutral-700 dark:text-neutral-300">
        Alt+J/K navigate
        {canMutateAlertInbox ? " · Alt+1 ack · Alt+2 resolve · Alt+3 suppress" : ""}
      </span>

      <div className="grid gap-3">
        {loading && failure === null && alerts.length === 0 ? (
          <OperatorLoadingNotice>
            <strong>Loading alerts.</strong>
            <p className="mt-2 text-sm text-neutral-700 dark:text-neutral-300">
              Fetching a page for the selected status filter ({ALERTS_PAGE_SIZE} per page). Empty results after load
              means there are no matching alerts—not a silent failure.
            </p>
          </OperatorLoadingNotice>
        ) : null}

        {!loading && failure === null && alerts.length === 0 ? <EmptyState {...emptyFilteredProps} /> : null}

        {alerts.length > 0
          ? alerts.map((alert) => (
              <article
                key={alert.alertId}
                role="article"
                tabIndex={0}
                data-alert-id={alert.alertId}
                className="rounded-lg border border-neutral-200 bg-white p-4 shadow-sm dark:border-neutral-700 dark:bg-neutral-900"
              >
                <div>
                  <div className="mb-2 flex flex-wrap items-start justify-between gap-2">
                    <strong className="text-base text-neutral-900 dark:text-neutral-100">{alert.title}</strong>
                    <Badge className={cn("text-xs font-semibold", severityBadgeClass(alert.severity))} variant="outline">
                      {alert.severity}
                    </Badge>
                  </div>
                  <div className="mb-1 text-sm text-neutral-600 dark:text-neutral-400">
                    <span className="text-neutral-500 dark:text-neutral-500">Category:</span> {alert.category}
                  </div>
                  <div className="mb-1 text-sm text-neutral-600 dark:text-neutral-400">
                    <span className="text-neutral-500 dark:text-neutral-500">Status:</span> {alert.status}
                  </div>
                  <div className="mb-2 text-sm text-neutral-600 dark:text-neutral-400">
                    <span className="text-neutral-500 dark:text-neutral-500">Trigger:</span> {alert.triggerValue}
                  </div>
                  <p className="text-sm leading-relaxed text-neutral-700 dark:text-neutral-300">{alert.description}</p>
                </div>

                <section
                  className="mt-4 border-t border-neutral-200 pt-3 dark:border-neutral-700"
                  aria-label="Triage actions"
                >
                  <h3 className="text-xs font-semibold uppercase tracking-wide text-neutral-700 dark:text-neutral-300">
                    Triage actions
                  </h3>
                  <p className="mt-1.5 text-xs text-neutral-500 dark:text-neutral-400">
                    {canMutateAlertInbox
                      ? "Use triage actions when this signal needs follow-up."
                      : "Read-focused inbox view. Triage actions require operator-level access in this shell."}
                  </p>
                  <div className="mt-2 flex flex-wrap gap-2">
                    <Button
                      type="button"
                      size="sm"
                      variant="secondary"
                      disabled={!canMutateAlertInbox}
                      title={canMutateAlertInbox ? undefined : enterpriseMutationControlDisabledTitle}
                      onClick={() => {
                        setPendingAction({ alertId: alert.alertId, action: "Acknowledge" });
                        setActionComment("");
                      }}
                    >
                      Acknowledge
                    </Button>
                    <Button
                      type="button"
                      size="sm"
                      variant="secondary"
                      disabled={!canMutateAlertInbox}
                      title={canMutateAlertInbox ? undefined : enterpriseMutationControlDisabledTitle}
                      onClick={() => {
                        setPendingAction({ alertId: alert.alertId, action: "Resolve" });
                        setActionComment("");
                      }}
                    >
                      Resolve
                    </Button>
                    <Button
                      type="button"
                      size="sm"
                      variant="outline"
                      className="border-red-300 text-red-700 hover:bg-red-50 dark:border-red-900 dark:text-red-300 dark:hover:bg-red-950/50"
                      disabled={!canMutateAlertInbox}
                      title={canMutateAlertInbox ? undefined : enterpriseMutationControlDisabledTitle}
                      onClick={() => {
                        setPendingAction({ alertId: alert.alertId, action: "Suppress" });
                        setActionComment("");
                      }}
                    >
                      Suppress
                    </Button>
                  </div>
                </section>
              </article>
            ))
          : null}

        {!loading && failure === null && totalCount > 0 ? (
          <nav
            className="mt-4 flex flex-wrap items-center gap-4 text-sm text-neutral-600 dark:text-neutral-400"
            aria-label="Alerts pagination"
          >
            <span>
              Page {page} of {totalPages} · {totalCount} alert{totalCount === 1 ? "" : "s"} total
            </span>
            <Button type="button" variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage((p) => Math.max(1, p - 1))}>
              Previous
            </Button>
            <Button
              type="button"
              variant="outline"
              size="sm"
              disabled={page >= totalPages}
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
            >
              Next
            </Button>
          </nav>
        ) : null}
      </div>

      <Dialog
        open={pendingAction !== null}
        onOpenChange={(open) => {
          if (!open) {
            setPendingAction(null);
            setActionComment("");
          }
        }}
      >
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>
              {pendingAction === null
                ? "Alert action"
                : `${pendingAction.action} alert`}
            </DialogTitle>
            <DialogDescription>
              {pendingAction === null
                ? ""
                : `Optional comment is sent with the ${pendingAction.action} request for alert ${pendingAction.alertId}.`}
            </DialogDescription>
          </DialogHeader>
          <div className="grid gap-2 py-2">
            <Label htmlFor="alert-action-comment">Comment (optional)</Label>
            <Textarea
              id="alert-action-comment"
              rows={3}
              value={actionComment}
              onChange={(e) => setActionComment(e.target.value)}
              placeholder="Context for auditors (optional)"
            />
          </div>
          <DialogFooter className="gap-2 sm:gap-0">
            <Button
              type="button"
              variant="outline"
              onClick={() => {
                setPendingAction(null);
                setActionComment("");
              }}
              disabled={actionBusy}
            >
              Cancel
            </Button>
            <Button
              type="button"
              onClick={() => void onConfirmActionDialog()}
              disabled={actionBusy || pendingAction === null || !canMutateAlertInbox}
              title={canMutateAlertInbox ? undefined : enterpriseMutationControlDisabledTitle}
            >
              {actionBusy ? "Saving…" : "Confirm"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </main>
  );
}
