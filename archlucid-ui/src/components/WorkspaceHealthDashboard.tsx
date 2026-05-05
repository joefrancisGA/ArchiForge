"use client";

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
import { getComplianceDriftTrend, getGovernanceDashboard } from "@/lib/api";
import type { ApiProblemDetails } from "@/lib/api-problem";
import { isApiRequestError } from "@/lib/api-request-error";
import { fetchPilotValueReportJson } from "@/lib/pilot-value-report-fetch";
import { AUTHORITY_RANK } from "@/lib/nav-authority";
import { hoursSurfaced, formatHours } from "@/lib/roi-assumptions";
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

export function WorkspaceHealthDashboard() {
  const callerRank = useNavCallerAuthorityRank();
  const [state, setState] = useState<LoadState>({ status: "loading" });

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
      <main className="mx-auto max-w-4xl space-y-4 p-4">
        <LayerHeader pageKey="governance-dashboard" />
        <p className="text-sm text-neutral-600 dark:text-neutral-400">Loading workspace health…</p>
      </main>
    );
  }

  if (state.status === "error") {
    return (
      <main className="mx-auto max-w-4xl space-y-4 p-4">
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

  const { dashboard, driftPoints, blocked30d, warned30d, report30d, report90d } = state;

  const sla = computeWorkspaceHealthSlaStats(dashboard.pendingApprovals, dashboard.recentDecisions);

  const hoursFromBlocks = hoursSurfaced({ critical: 0, high: 0, medium: 0 }, blocked30d.count);

  const hoursFull30 = hoursSurfaced(report30d.findingsBySeverity, blocked30d.count);

  const highCritical90 = report90d.findingsBySeverity.high + report90d.findingsBySeverity.critical;

  const onTimePct =
    sla.onTimeDecisionRate === null ? "—" : `${Math.round(sla.onTimeDecisionRate * 100)}%`;

  return (
    <main className="mx-auto max-w-4xl space-y-6 p-4">
      <LayerHeader pageKey="governance-dashboard" />
      <div className="flex flex-wrap items-center gap-2">
        <h1 className="m-0 text-2xl font-semibold text-neutral-900 dark:text-neutral-100">Workspace health</h1>
        <ContextualHelp helpKey="governance-dashboard" />
        <HelpLink
          docPath="/docs/library/GOVERNANCE_WORKFLOW_UI.md"
          label="Governance workflows documentation on GitHub (new tab)"
        />
      </div>

      <div
        className="rounded-md border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-950 dark:border-amber-900/60 dark:bg-amber-950/25 dark:text-amber-100"
        role="status"
      >
        <p className="m-0 font-medium">Current workspace scope only</p>
        <p className="m-0 mt-1 text-xs opacity-90">
          Figures use the authenticated tenant / workspace / project headers — the same boundaries as governance and audit
          APIs. They are not a cross-workspace executive rollup.
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        <Card className="border-neutral-200 dark:border-neutral-800">
          <CardContent className="space-y-2 p-4">
            <h2 className="m-0 text-base font-semibold text-neutral-900 dark:text-neutral-100">
              Pre-commit outcomes (30 days)
            </h2>
            <p className="m-0 text-xs text-neutral-500 dark:text-neutral-400">Audit-backed tallies in the rolling window.</p>
            <ul className="m-0 mt-2 list-none space-y-1 p-0 text-sm">
              <li>
                Blocked:{" "}
                <span className="font-mono font-medium">
                  {blocked30d.exact ? blocked30d.count : `${blocked30d.count} (sampled lower bound)`}
                </span>{" "}
                <Link className="text-blue-700 underline dark:text-blue-400" href="/audit">
                  Audit log
                </Link>
              </li>
              <li>
                Warned:{" "}
                <span className="font-mono font-medium">
                  {warned30d.exact ? warned30d.count : `${warned30d.count} (sampled lower bound)`}
                </span>
              </li>
            </ul>
          </CardContent>
        </Card>

        <Card className="border-neutral-200 dark:border-neutral-800">
          <CardContent className="space-y-2 p-4">
            <h2 className="m-0 text-base font-semibold text-neutral-900 dark:text-neutral-100">
              High / Critical exposure (90 days)
            </h2>
            <p className="m-0 text-xs text-neutral-500 dark:text-neutral-400">
              From pilot-value-report severity totals in the window — not the same as an open-backlog aging report.
            </p>
            <p className="m-0 mt-2 font-mono text-2xl font-semibold tabular-nums dark:text-neutral-100">{highCritical90}</p>
            <p className="m-0 text-sm">
              <Link href="/governance/findings" className="font-medium text-blue-700 underline dark:text-blue-400">
                Open governance findings queue
              </Link>
            </p>
          </CardContent>
        </Card>

        <Card className="border-neutral-200 dark:border-neutral-800 md:col-span-2">
          <CardContent className="space-y-2 p-4">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <h2 className="m-0 text-base font-semibold text-neutral-900 dark:text-neutral-100">
                Compliance drift (30 days)
              </h2>
              <Link href="/governance" className="text-sm font-medium text-blue-700 underline dark:text-blue-400">
                Governance workflow
              </Link>
            </div>
            <ComplianceDriftChart points={driftPoints} />
          </CardContent>
        </Card>

        <Card className="border-neutral-200 dark:border-neutral-800">
          <CardContent className="space-y-2 p-4">
            <h2 className="m-0 text-base font-semibold text-neutral-900 dark:text-neutral-100">Approval SLA posture</h2>
            <p className="m-0 text-xs text-neutral-500 dark:text-neutral-400">
              From governance dashboard pending + recent decisions.
            </p>
            <ul className="m-0 mt-2 list-none space-y-1 p-0 text-sm text-neutral-800 dark:text-neutral-200">
              <li>Pending (sample cap): {dashboard.pendingCount}</li>
              <li>Overdue pending (with SLA deadline): {sla.overduePendingCount}</li>
              <li>On-track pending (with SLA deadline): {sla.onTrackPendingWithSlaCount}</li>
              <li>
                On-time decisions (reviewed ≤ SLA, eligible n={sla.onTimeEligibleDecisions}): {onTimePct}
              </li>
            </ul>
          </CardContent>
        </Card>

        <Card className="border-neutral-200 dark:border-neutral-800">
          <CardContent className="space-y-2 p-4">
            <h2 className="m-0 text-base font-semibold text-neutral-900 dark:text-neutral-100">Value proxy (30 days)</h2>
            <p className="m-0 text-xs text-neutral-500 dark:text-neutral-400">
              Hours model matches the ROI report (severity weights + blocked events). Blocks:{" "}
              {blocked30d.exact ? blocked30d.count : `${blocked30d.count} (sampled)`}.
            </p>
            <p className="m-0 mt-2 font-mono text-xl font-semibold text-neutral-900 dark:text-neutral-100">
              {formatHours(hoursFull30)}
              <span className="ml-2 text-sm font-normal text-neutral-500 dark:text-neutral-400">
                (blocks alone: {formatHours(hoursFromBlocks)})
              </span>
            </p>
            <p className="m-0 text-sm">
              <Link href="/value-report/roi" className="font-medium text-blue-700 underline dark:text-blue-400">
                Open ROI summary
              </Link>
            </p>
          </CardContent>
        </Card>
      </div>

      <p className="text-xs text-neutral-500 dark:text-neutral-400">
        Full USD modeling is on the ROI summary page
        {callerRank >= AUTHORITY_RANK.AdminAuthority ? "" : " (Admin-only loaded $/hour line)"}.
      </p>
    </main>
  );
}
