"use client";

import { useEffect, useState } from "react";

import { ApiV1Routes } from "@/lib/api-v1-routes";
import { mergeRegistrationScopeForProxy } from "@/lib/proxy-fetch-registration-scope";

type ExecutiveRoiAggregates = {
  timeSavedHours: number;
  decisionsAutomated: number;
  complianceRisksMitigated: number;
};

const ROI_PATH = `/api/proxy/${ApiV1Routes.analyticsRoi}`;

function formatTimeSavedHours(hours: number): string {
  if (!Number.isFinite(hours) || hours <= 0) {
    return "—";
  }

  const rounded = hours >= 10 ? Math.round(hours) : Math.round(hours * 10) / 10;

  return `${rounded} hrs`;
}

function formatCount(value: number): string {
  if (!Number.isFinite(value) || value < 0) {
    return "—";
  }

  return new Intl.NumberFormat(undefined, { maximumFractionDigits: 0 }).format(value);
}

/** Executive ROI tiles backed by `GET /v1/analytics/roi` (mocked on the API until persistence is defined). */
export function ExecutiveRoiDashboard() {
  const [data, setData] = useState<ExecutiveRoiAggregates | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    void (async () => {
      try {
        const res = await fetch(
          ROI_PATH,
          mergeRegistrationScopeForProxy({ headers: { Accept: "application/json" } }),
        );

        if (!res.ok) {
          throw new Error(`HTTP ${res.status}`);
        }

        const json = (await res.json()) as ExecutiveRoiAggregates;

        if (!cancelled) {
          setData(json);
        }
      } catch (e: unknown) {
        if (!cancelled) {
          setError(e instanceof Error ? e.message : "Failed to load ROI metrics.");
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, []);

  if (error) {
    return (
      <section
        aria-labelledby="exec-roi-dashboard-heading"
        className="rounded-lg border border-neutral-200 bg-white p-4 dark:border-neutral-800 dark:bg-neutral-900"
      >
        <h2 id="exec-roi-dashboard-heading" className="text-sm font-semibold text-neutral-900 dark:text-neutral-100">
          Executive ROI
        </h2>
        <p className="mt-2 text-xs text-red-600 dark:text-red-400" role="alert">
          {error}
        </p>
      </section>
    );
  }

  if (data === null) {
    return (
      <section
        aria-labelledby="exec-roi-dashboard-heading"
        className="rounded-lg border border-neutral-200 bg-white p-4 dark:border-neutral-800 dark:bg-neutral-900"
      >
        <h2 id="exec-roi-dashboard-heading" className="text-sm font-semibold text-neutral-900 dark:text-neutral-100">
          Executive ROI
        </h2>
        <p className="mt-2 text-xs text-neutral-500 dark:text-neutral-400">Loading…</p>
      </section>
    );
  }

  return (
    <section
      aria-labelledby="exec-roi-dashboard-heading"
      className="rounded-lg border border-neutral-200 bg-white p-4 dark:border-neutral-800 dark:bg-neutral-900"
    >
      <h2 id="exec-roi-dashboard-heading" className="text-sm font-semibold text-neutral-900 dark:text-neutral-100">
        Executive ROI
      </h2>
      <p className="mt-1 text-xs text-neutral-500 dark:text-neutral-400">
        Aggregated impact (placeholder data until analytics storage is connected).
      </p>
      <div className="mt-4 grid gap-3 sm:grid-cols-3">
        <div className="rounded-md border border-neutral-100 bg-neutral-50/80 p-3 dark:border-neutral-800 dark:bg-neutral-950/40">
          <div className="text-xs font-medium text-neutral-500 dark:text-neutral-400">Time saved</div>
          <div className="mt-1 text-lg font-semibold tabular-nums text-neutral-900 dark:text-neutral-100">
            {formatTimeSavedHours(data.timeSavedHours)}
          </div>
        </div>
        <div className="rounded-md border border-neutral-100 bg-neutral-50/80 p-3 dark:border-neutral-800 dark:bg-neutral-950/40">
          <div className="text-xs font-medium text-neutral-500 dark:text-neutral-400">Decisions automated</div>
          <div className="mt-1 text-lg font-semibold tabular-nums text-neutral-900 dark:text-neutral-100">
            {formatCount(data.decisionsAutomated)}
          </div>
        </div>
        <div className="rounded-md border border-neutral-100 bg-neutral-50/80 p-3 dark:border-neutral-800 dark:bg-neutral-950/40">
          <div className="text-xs font-medium text-neutral-500 dark:text-neutral-400">Compliance risks mitigated</div>
          <div className="mt-1 text-lg font-semibold tabular-nums text-neutral-900 dark:text-neutral-100">
            {formatCount(data.complianceRisksMitigated)}
          </div>
        </div>
      </div>
    </section>
  );
}
