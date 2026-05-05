"use client";

import { useEffect, useState } from "react";
import Link from "next/link";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { getEffectiveBrowserProxyScopeHeaders } from "@/lib/operator-scope-storage";
import { DEFAULT_LOADED_HOURLY_USD, formatHours, formatUsd, readStoredHourlyUsd } from "@/lib/roi-assumptions";

interface RoiTelemetry {
  totalRuns: number;
  totalHoursSaved: number;
  averageTimeToCommitMs: number;
}

export function ValueRealizationDashboard() {
  const [telemetry, setTelemetry] = useState<RoiTelemetry | null>(null);
  const [loading, setLoading] = useState(true);
  const [failure, setFailure] = useState<ApiLoadFailureState | null>(null);
  const [hourlyUsd, setHourlyUsd] = useState<number>(DEFAULT_LOADED_HOURLY_USD);

  useEffect(() => {
    setHourlyUsd(readStoredHourlyUsd());
  }, []);

  useEffect(() => {
    async function load() {
      try {
        const response = await fetch("/api/proxy/v1/architecture/telemetry/roi", {
          headers: { Accept: "application/json", ...getEffectiveBrowserProxyScopeHeaders() },
        });
        if (!response.ok) {
          throw new Error(`Failed to load telemetry: ${response.statusText}`);
        }
        const data = await response.json();
        setTelemetry(data);
      } catch (e) {
        setFailure(toApiLoadFailure(e));
      } finally {
        setLoading(false);
      }
    }
    void load();
  }, []);

  if (loading) {
    return <div className="text-sm text-neutral-500">Loading Value Realization metrics...</div>;
  }

  if (failure) {
    return (
      <OperatorApiProblem
        problem={failure.problem}
        fallbackMessage={failure.message}
        correlationId={failure.correlationId}
      />
    );
  }

  if (!telemetry) {
    return null;
  }

  const hoursSaved = Number(telemetry.totalHoursSaved);
  const safeHours = Number.isFinite(hoursSaved) ? hoursSaved : 0;
  const totalReviewsRaw = Number(telemetry.totalRuns);
  const totalReviews =
    Number.isFinite(totalReviewsRaw) && totalReviewsRaw > 0 ? Math.floor(totalReviewsRaw) : 0;
  const avgMsRaw = Number(telemetry.averageTimeToCommitMs);
  const avgCommitMins =
    Number.isFinite(avgMsRaw) && avgMsRaw > 0 ? Math.max(1, Math.round(avgMsRaw / 60000)) : null;
  const hasAvgCommit = avgCommitMins !== null;
  const hasAnyCredibleMetric = totalReviews >= 1 || safeHours > 0 || hasAvgCommit;

  if (!hasAnyCredibleMetric) {
    return null;
  }

  const impliedUsd = safeHours > 0 ? safeHours * hourlyUsd : 0;
  const showMeasuredRoiBlock = safeHours > 0 && impliedUsd > 0;

  return (
    <Card className="mb-6">
      <CardHeader>
        <CardTitle className="text-lg">Value Realization</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
          <div className="rounded-lg border p-4 text-center">
            <p className="text-sm text-neutral-500">Total reviews</p>
            <p className="text-2xl font-bold">{totalReviews}</p>
          </div>
          <div className="rounded-lg border p-4 text-center">
            <p className="text-sm text-neutral-500">Time saved (tenant model)</p>
            <p className="text-2xl font-bold text-teal-600">{formatHours(safeHours)}</p>
          </div>
          <div className="rounded-lg border p-4 text-center">
            <p className="text-sm text-neutral-500">Avg time to commit</p>
            <p className="text-2xl font-bold">{avgCommitMins !== null ? `${avgCommitMins} mins` : "—"}</p>
          </div>
        </div>
        {showMeasuredRoiBlock ? (
        <div className="rounded-lg border border-dashed border-neutral-200 bg-neutral-50/80 p-3 text-sm dark:border-neutral-700 dark:bg-neutral-900/40">
          <p className="m-0 font-medium text-neutral-800 dark:text-neutral-100">Measured ROI snapshot</p>
          <p className="m-0 mt-1 text-neutral-600 dark:text-neutral-400">
            Hours come from persisted run telemetry (<span className="font-mono text-xs">EstimatedHoursSaved</span> per
            review). Implied spend uses the same loaded rate as the ROI page (
            <span className="font-mono text-xs">{formatUsd(hourlyUsd)}</span>
            /h from this browser unless you changed it under Value report → ROI).
          </p>
          <p className="m-0 mt-2 font-mono text-base font-semibold tabular-nums text-neutral-900 dark:text-neutral-50">
            ~{formatUsd(impliedUsd)} <span className="text-sm font-normal text-neutral-500">implied @ loaded rate</span>
          </p>
          <p className="m-0 mt-2">
            <Link
              href="/value-report/roi"
              className="text-sm font-medium text-teal-700 underline-offset-4 hover:underline dark:text-teal-400"
            >
              Open ROI assumptions &amp; sensitivity →
            </Link>
          </p>
        </div>
        ) : null}
      </CardContent>
    </Card>
  );
}
