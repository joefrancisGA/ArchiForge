"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";

import { DocumentLayout } from "@/components/DocumentLayout";
import { LayerHeader } from "@/components/LayerHeader";
import { useNavCallerAuthorityRank } from "@/components/OperatorNavAuthorityProvider";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { RoiTelemetryCard } from "@/components/RoiTelemetryCard";
import { Button } from "@/components/ui/button";
import type { ApiProblemDetails } from "@/lib/api-problem";
import { isApiRequestError } from "@/lib/api-request-error";
import { isNextPublicDemoMode } from "@/lib/demo-ui-env";
import { AUTHORITY_RANK } from "@/lib/nav-authority";
import { fetchPilotValueReportJson } from "@/lib/pilot-value-report-fetch";
import { isStaticDemoPayloadFallbackEnabled } from "@/lib/operator-static-demo";
import { countAuditEventsInWindow } from "@/lib/workspace-health-audit-count";
import type { PilotValueReportJson } from "@/types/pilot-value-report";

function rollingBounds(days: number): { fromUtc: string; toUtc: string } {
  const to = new Date();
  const from = new Date(to);

  from.setUTCDate(from.getUTCDate() - days);

  return { fromUtc: from.toISOString(), toUtc: to.toISOString() };
}

type LoadBundle = {
  report: PilotValueReportJson;
  blocks: { count: number; exact: boolean };
};

type RoiPageState =
  | { status: "loading" }
  | { status: "ready"; rolling30: LoadBundle; pilotToDate: LoadBundle }
  | { status: "error"; message: string; problem: ApiProblemDetails | null; correlationId: string | null };

export default function RoiSummaryPage() {
  const rank = useNavCallerAuthorityRank();
  const isAdmin = rank >= AUTHORITY_RANK.AdminAuthority;
  const demo = isNextPublicDemoMode() || isStaticDemoPayloadFallbackEnabled();

  const [state, setState] = useState<RoiPageState>({ status: "loading" });

  const load = useCallback(async () => {
    setState({ status: "loading" });

    const b30 = rollingBounds(30);

    try {
      const pilotReport = await fetchPilotValueReportJson(null, b30.toUtc);

      const [rollingReport, rollingBlocks, pilotBlocks] = await Promise.all([
        fetchPilotValueReportJson(b30.fromUtc, b30.toUtc),
        countAuditEventsInWindow({
          eventType: "GovernancePreCommitBlocked",
          fromUtcIso: b30.fromUtc,
          toUtcIso: b30.toUtc,
        }),
        countAuditEventsInWindow({
          eventType: "GovernancePreCommitBlocked",
          fromUtcIso: pilotReport.fromUtc,
          toUtcIso: pilotReport.toUtc,
        }),
      ]);

      setState({
        status: "ready",
        rolling30: { report: rollingReport, blocks: rollingBlocks },
        pilotToDate: { report: pilotReport, blocks: pilotBlocks },
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
          message: e instanceof Error ? e.message : "Could not load ROI summary.",
          problem: null,
          correlationId: null,
        });
      }
    }
  }, []);

  useEffect(() => {
    if (demo) {
      return;
    }

    void load();
  }, [demo, load]);

  if (demo) {
    return (
      <main className="mx-auto space-y-4 p-4">
        <LayerHeader pageKey="value-report-roi" />
        <div className="rounded-lg border border-neutral-200 bg-neutral-50 p-6 text-sm text-neutral-600 dark:border-neutral-800 dark:bg-neutral-900 dark:text-neutral-400">
          <p className="m-0 font-medium text-neutral-800 dark:text-neutral-200">ROI summary not available in demo mode.</p>
        </div>
      </main>
    );
  }

  if (state.status === "loading") {
    return (
      <main className="mx-auto space-y-4 p-4">
        <LayerHeader pageKey="value-report-roi" />
        <p className="text-sm text-neutral-600 dark:text-neutral-400">Loading ROI summary…</p>
      </main>
    );
  }

  if (state.status === "error") {
    return (
      <main className="mx-auto space-y-4 p-4">
        <LayerHeader pageKey="value-report-roi" />
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

  const { rolling30, pilotToDate } = state;

  return (
    <main className="mx-auto space-y-4 p-4">
      <LayerHeader pageKey="value-report-roi" />
      <DocumentLayout>
        <h1 className="m-0 text-2xl font-semibold text-neutral-900 dark:text-neutral-100">ROI summary</h1>
        <div
          className="rounded-lg border border-teal-700/30 bg-teal-50/90 px-4 py-3 text-sm text-teal-950 shadow-sm dark:border-teal-500/30 dark:bg-teal-950/30 dark:text-teal-50"
          role="status"
        >
          <p className="m-0 font-semibold text-teal-900 dark:text-teal-100">Scope</p>
          <p className="m-0 mt-1 leading-snug">
            Figures reflect your current tenant/workspace/project scope only.
          </p>
        </div>
        <p className="doc-meta m-0 text-sm text-neutral-600 dark:text-neutral-400">
          Hours-first estimate from pilot-value-report severities and pre-commit block audit events.{" "}
          <Link href="/value-report/pilot" className="font-medium text-blue-700 underline dark:text-blue-400">
            Pilot value report
          </Link>
          {" · "}
          <Link href="/governance/dashboard" className="font-medium text-blue-700 underline dark:text-blue-400">
            Workspace health
          </Link>
        </p>

        <div className="grid gap-4 lg:grid-cols-2">
          <RoiTelemetryCard
            window="rolling30"
            rangeCaption={`${rolling30.report.fromUtc.slice(0, 10)} → ${rolling30.report.toUtc.slice(0, 10)} (toUtc exclusive)`}
            severity={rolling30.report.findingsBySeverity}
            precommitBlocks={rolling30.blocks.count}
            precommitBlocksExact={rolling30.blocks.exact}
            isAdmin={isAdmin}
          />
          <RoiTelemetryCard
            window="pilotToDate"
            rangeCaption={`${pilotToDate.report.fromUtc.slice(0, 10)} → ${pilotToDate.report.toUtc.slice(0, 10)} (toUtc exclusive)`}
            severity={pilotToDate.report.findingsBySeverity}
            precommitBlocks={pilotToDate.blocks.count}
            precommitBlocksExact={pilotToDate.blocks.exact}
            isAdmin={isAdmin}
          />
        </div>
      </DocumentLayout>
    </main>
  );
}
