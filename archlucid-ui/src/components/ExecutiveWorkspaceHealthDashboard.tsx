"use client";

import { CircleHelp } from "lucide-react";
import Link from "next/link";
import { useCallback, useEffect, useState } from "react";

import { ComplianceDriftChart } from "@/components/ComplianceDriftChart";
import { ContextualHelp } from "@/components/ContextualHelp";
import { HelpLink } from "@/components/HelpLink";
import { LayerHeader } from "@/components/LayerHeader";
import { useNavCallerAuthorityRank } from "@/components/OperatorNavAuthorityProvider";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { getComplianceDriftTrend, getGovernanceDashboard } from "@/lib/api";
import type { ApiProblemDetails } from "@/lib/api-problem";
import { isApiRequestError } from "@/lib/api-request-error";
import { fetchPilotValueReportJson } from "@/lib/pilot-value-report-fetch";
import { AUTHORITY_RANK } from "@/lib/nav-authority";
import { getEffectiveBrowserProxyScopeHeaders, readOperatorScopeFromStorage } from "@/lib/operator-scope-storage";
import { hoursSurfaced, formatHours, HOURS_PER_PRECOMMIT_BLOCK } from "@/lib/roi-assumptions";
import { formatExecutiveWorkspaceScopeDescription } from "@/lib/workspace-health-scope-banner";
import { countAuditEventsInWindow } from "@/lib/workspace-health-audit-count";
import { computeWorkspaceHealthSlaStats } from "@/lib/workspace-health-sla";
import type { ComplianceDriftTrendPoint, GovernanceDashboardSummary } from "@/types/governance-dashboard";
import type { PilotValueReportJson } from "@/types/pilot-value-report";

function rollingBounds(days: number): { fromUtc: string; toUtc: string } {
  const to = new Date();
  const from = new Date(to);

  from.setUTCDate(from.getUTCDate() - days);

  return { fromUtc: from.toISOString(), toUtc: to.toISOString() };
}

type LoadState =
  | { status: "idle" | "loading" }
  | {
      status: "ready";
      dashboard: GovernanceDashboardSummary;
      driftPoints: ComplianceDriftTrendPoint[];
      blocked30d: { count: number; exact: boolean };
      warned30d: { count: number; exact: boolean };
      report30d: PilotValueReportJson;
      report90d: PilotValueReportJson;
    }
  | { status: "error"; message: string; problem: ApiProblemDetails | null; correlationId: string | null };

const DEFAULT_SCOPE_FALLBACK =
  "Figures use the authenticated tenant / workspace / project sent with each request — the same boundaries as governance and audit. Not a cross-workspace rollup.";

/**
 * Sponsor-oriented **Executive Workspace Health**: five KPI blocks composed from existing governance, audit, compliance-drift, and pilot-value APIs (current scope only).
 */
export function ExecutiveWorkspaceHealthDashboard() {
  const callerRank = useNavCallerAuthorityRank();
  const [state, setState] = useState<LoadState>({ status: "loading" });
  const [scopeBanner, setScopeBanner] = useState<string>(DEFAULT_SCOPE_FALLBACK);

  const refreshScopeBanner = useCallback(() => {
    const record = readOperatorScopeFromStorage();
    const headers = getEffectiveBrowserProxyScopeHeaders();

    setScopeBanner(
      formatExecutiveWorkspaceScopeDescription(record, {
        tenantId: headers["x-tenant-id"] ?? "",
        workspaceId: headers["x-workspace-id"] ?? "",
        projectId: headers["x-project-id"] ?? "",
      }),
    );
  }, []);

  useEffect(() => {
    refreshScopeBanner();

    const onStorage = (e: StorageEvent): void => {
      if (e.key === "archlucid_operator_scope_v1") {
        refreshScopeBanner();
      }
    };

    window.addEventListener("storage", onStorage);
    window.addEventListener("focus", refreshScopeBanner);

    return () => {
      window.removeEventListener("storage", onStorage);
      window.removeEventListener("focus", refreshScopeBanner);
    };
  }, [refreshScopeBanner]);

  const load = useCallback(async () => {
    setState({ status: "loading" });

    const b30 = rollingBounds(30);
    const b90 = rollingBounds(90);

    try {
      const [dashboard, driftPoints, blocked30d, warned30d, report30d, report90d] = await Promise.all([
        getGovernanceDashboard(50, 50, 50),
        getComplianceDriftTrend(b30.fromUtc, b30.toUtc, 1440),
        countAuditEventsInWindow({
          eventType: "GovernancePreCommitBlocked",
          fromUtcIso: b30.fromUtc,
          toUtcIso: b30.toUtc,
        }),
        countAuditEventsInWindow({
          eventType: "GovernancePreCommitWarned",
          fromUtcIso: b30.fromUtc,
          toUtcIso: b30.toUtc,
        }),
        fetchPilotValueReportJson(b30.fromUtc, b30.toUtc),
        fetchPilotValueReportJson(b90.fromUtc, b90.toUtc),
      ]);

      setState({
        status: "ready",
        dashboard,
        driftPoints,
        blocked30d,
        warned30d,
        report30d,
        report90d,
      });
    } catch (e: unknown) {
      if (isApiRequestError(e)) {
        setState({
          status: "error",
          message: e.message,
          problem: e.problem,
          correlationId: e.correlationId,
        });
      } else {
        setState({
          status: "error",
          message: e instanceof Error ? e.message : "Could not load workspace health.",
          problem: null,
          correlationId: null,
        });
      }
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  if (state.status === "loading" || state.status === "idle") {
    return (
      <main className="mx-auto max-w-6xl space-y-4 p-4">
        <LayerHeader pageKey="governance-dashboard" />
        <p className="text-sm text-neutral-600 dark:text-neutral-400">Loading executive workspace health…</p>
      </main>
    );
  }

  if (state.status === "error") {
    return (
      <main className="mx-auto max-w-6xl space-y-4 p-4">
        <LayerHeader pageKey="governance-dashboard" />
        <OperatorApiProblem
          fallbackMessage={state.message}
          problem={state.problem}
          correlationId={state.correlationId}
        />
        <Button type="button" variant="secondary" onClick={() => void load()}>
          Retry
        </Button>
      </main>
    );
  }

  if (state.status !== "ready") {
    return null;
  }

  const { dashboard, driftPoints, blocked30d, warned30d, report30d, report90d } = state;

  const sla = computeWorkspaceHealthSlaStats(dashboard.pendingApprovals, dashboard.recentDecisions);

  const hoursFromBlocks = hoursSurfaced({ critical: 0, high: 0, medium: 0 }, blocked30d.count);

  const hoursFull30 = hoursSurfaced(report30d.findingsBySeverity, blocked30d.count);

  const highCritical90 = report90d.findingsBySeverity.high + report90d.findingsBySeverity.critical;

  const onTimePct =
    sla.onTimeDecisionRate === null ? "—" : `${Math.round(sla.onTimeDecisionRate * 100)}%`;

  const blockCountLabel = blocked30d.exact
    ? String(blocked30d.count)
    : `≥ ${blocked30d.count} (sampled lower bound; audit paging reached safety cap)`;

  return (
    <main className="mx-auto max-w-6xl space-y-6 p-4">
      <LayerHeader pageKey="governance-dashboard" />

      <header className="space-y-2">
        <div className="flex flex-wrap items-center gap-2">
          <h1 className="m-0 text-2xl font-semibold tracking-tight text-neutral-900 dark:text-neutral-100">
            Executive Workspace Health
          </h1>
          <ContextualHelp helpKey="governance-dashboard" />
          <HelpLink
            docPath="/docs/library/GOVERNANCE_WORKFLOW_UI.md"
            label="Governance workflows documentation on GitHub (new tab)"
          />
        </div>
        <p className="m-0 max-w-3xl text-sm text-neutral-600 dark:text-neutral-400">
          Single page for operators and sponsors: pre-commit posture, severity exposure, compliance drift, approval SLAs, and a
          hours-first value proxy — all within your current workspace scope.
        </p>
      </header>

      <div
        className="rounded-lg border border-teal-700/30 bg-teal-50/90 px-4 py-3 text-sm text-teal-950 shadow-sm dark:border-teal-500/30 dark:bg-teal-950/30 dark:text-teal-50"
        role="status"
      >
        <p className="m-0 font-semibold text-teal-900 dark:text-teal-100">Session scope</p>
        <p className="m-0 mt-1 leading-snug">{scopeBanner}</p>
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        <Card className="border-neutral-200 dark:border-neutral-800">
          <CardContent className="space-y-2 p-4">
            <h2 className="m-0 text-base font-semibold text-neutral-900 dark:text-neutral-100">
              1. Pre-commit outcomes (30 days)
            </h2>
            <p className="m-0 text-xs text-neutral-500 dark:text-neutral-400">
              Audit-backed counts via <span className="font-mono">GovernancePreCommitBlocked</span> and{" "}
              <span className="font-mono">GovernancePreCommitWarned</span> in the rolling window.
            </p>
            <ul className="m-0 mt-2 list-none space-y-1 p-0 text-sm">
              <li>
                Blocked: <span className="font-mono font-medium text-neutral-900 dark:text-neutral-100">{blockCountLabel}</span>{" "}
                <Link className="text-blue-700 underline dark:text-blue-400" href="/audit">
                  Audit log
                </Link>
              </li>
              <li>
                Warned:{" "}
                <span className="font-mono font-medium text-neutral-900 dark:text-neutral-100">
                  {warned30d.exact ? warned30d.count : `≥ ${warned30d.count} (sampled lower bound)`}
                </span>
              </li>
            </ul>
          </CardContent>
        </Card>

        <Card className="border-neutral-200 dark:border-neutral-800">
          <CardContent className="space-y-2 p-4">
            <h2 className="m-0 text-base font-semibold text-neutral-900 dark:text-neutral-100">
              2. High / Critical finding exposure (90 days)
            </h2>
            <p className="m-0 text-xs text-neutral-500 dark:text-neutral-400">
              Pilot-value report severity totals in the window — exposure in the report period, not the same as an open-backlog
              aging inventory.
            </p>
            <p className="m-0 mt-2 font-mono text-2xl font-semibold tabular-nums dark:text-neutral-100">{highCritical90}</p>
            <p className="m-0 text-sm">
              <Link href="/governance/findings" className="font-medium text-blue-700 underline dark:text-blue-400">
                Governance findings queue
              </Link>
            </p>
          </CardContent>
        </Card>

        <Card className="border-neutral-200 dark:border-neutral-800 md:col-span-2">
          <CardContent className="space-y-2 p-4">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <h2 className="m-0 text-base font-semibold text-neutral-900 dark:text-neutral-100">
                3. Compliance drift trend (30 days)
              </h2>
              <Link href="/governance" className="text-sm font-medium text-blue-700 underline dark:text-blue-400">
                Governance workflow
              </Link>
            </div>
            <p className="m-0 text-xs text-neutral-500 dark:text-neutral-400">Daily buckets (1440-minute) from compliance drift API.</p>
            <ComplianceDriftChart points={driftPoints} />
          </CardContent>
        </Card>

        <Card className="border-neutral-200 dark:border-neutral-800">
          <CardContent className="space-y-2 p-4">
            <h2 className="m-0 text-base font-semibold text-neutral-900 dark:text-neutral-100">4. Approval SLA posture</h2>
            <p className="m-0 text-xs text-neutral-500 dark:text-neutral-400">
              Derived from governance dashboard pending approvals and recent terminal decisions.
            </p>
            <ul className="m-0 mt-2 list-none space-y-1 p-0 text-sm text-neutral-800 dark:text-neutral-200">
              <li>Pending (sample cap): {dashboard.pendingCount}</li>
              <li>Overdue pending (with SLA deadline): {sla.overduePendingCount}</li>
              <li>On-track pending (with SLA deadline): {sla.onTrackPendingWithSlaCount}</li>
              <li>
                On-time decisions (reviewed on or before SLA, eligible n={sla.onTimeEligibleDecisions}): {onTimePct}
              </li>
            </ul>
          </CardContent>
        </Card>

        <Card className="border-neutral-200 dark:border-neutral-800">
          <CardContent className="space-y-2 p-4">
            <div className="flex items-start gap-2">
              <h2 className="m-0 flex-1 text-base font-semibold text-neutral-900 dark:text-neutral-100">
                5. Pre-commit blocks as value proxy
              </h2>
              <Tooltip>
                <TooltipTrigger asChild>
                  <button
                    type="button"
                    className="rounded p-0.5 text-neutral-500 hover:text-neutral-700 focus-visible:outline focus-visible:ring-2 dark:text-neutral-400 dark:hover:text-neutral-200"
                    aria-label="About the hours estimate"
                  >
                    <CircleHelp className="size-4" aria-hidden />
                  </button>
                </TooltipTrigger>
                <TooltipContent className="max-w-xs text-left leading-snug">
                  Estimated review-hours combine severity-weighted findings and {HOURS_PER_PRECOMMIT_BLOCK} h per blocked event (
                  <span className="font-mono">roi-assumptions.ts</span>). This is a planning estimate, not measured wall-clock time.
                </TooltipContent>
              </Tooltip>
            </div>
            <p className="m-0 text-xs text-neutral-500 dark:text-neutral-400">
              Blocks in 30d:{" "}
              <span className="font-mono font-medium text-neutral-800 dark:text-neutral-200">
                {blocked30d.exact ? blocked30d.count : `${blocked30d.count} (sampled)`}
              </span>
              . Full hours formula includes findings severities in the same window.
            </p>
            <p className="m-0 mt-2 font-mono text-xl font-semibold text-neutral-900 dark:text-neutral-100">
              {formatHours(hoursFull30)}
              <span className="ml-2 text-sm font-normal text-neutral-500 dark:text-neutral-400">
                (blocks alone: {formatHours(hoursFromBlocks)})
              </span>
            </p>
            <p className="m-0 text-sm">
              <Link href="/value-report/roi" className="font-medium text-blue-700 underline dark:text-blue-400">
                See ROI report
              </Link>
            </p>
          </CardContent>
        </Card>
      </div>

      <p className="text-xs text-neutral-500 dark:text-neutral-400">
        Full USD modeling lives on the ROI report
        {callerRank >= AUTHORITY_RANK.AdminAuthority ? "" : " (Admin-only loaded $/hour line)"}.
      </p>
    </main>
  );
}
