"use client";

import { useEffect, useState } from "react";

import { mergeRegistrationScopeForProxy } from "@/lib/proxy-fetch-registration-scope";

type Rates = {
  windowNote: string;
  firstRunCommittedTotal: number;
  firstSessionCompletedTotal: number;
  firstRunCommittedPerSessionRatio: number;
};

/** Small operator-home tile for onboarding funnel counters (process lifetime; resets on API restart). */
export function OperatorTaskSuccessTile() {
  const [data, setData] = useState<Rates | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    void (async () => {
      try {
        const res = await fetch(
          "/api/proxy/v1/diagnostics/operator-task-success-rates",
          mergeRegistrationScopeForProxy({ headers: { Accept: "application/json" } }),
        );

        if (!res.ok) {
          throw new Error(String(res.status));
        }

        const json = (await res.json()) as Rates;

        if (!cancelled) {
          setData(json);
        }
      } catch {
        if (!cancelled) {
          setError("Metrics unavailable.");
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
        aria-labelledby="operator-task-success-heading"
        className="rounded-lg border border-dashed border-neutral-200 bg-neutral-50/50 p-4 dark:border-neutral-800 dark:bg-neutral-900/50"
      >
        <h2 id="operator-task-success-heading" className="text-sm font-semibold text-neutral-700 dark:text-neutral-300">
          Onboarding funnel
        </h2>
        <p className="mt-1.5 text-xs text-neutral-500 dark:text-neutral-400">
          No data yet. Metrics appear after your first completed run.
        </p>
      </section>
    );
  }

  if (!data) {
    return (
      <section
        aria-labelledby="operator-task-success-heading"
        className="rounded-lg border border-neutral-200 bg-white p-4 dark:border-neutral-800 dark:bg-neutral-900"
      >
        <h2 id="operator-task-success-heading" className="text-sm font-semibold text-neutral-900 dark:text-neutral-100">
          Onboarding funnel
        </h2>
        <p className="mt-2 text-xs text-neutral-500 dark:text-neutral-400">Loading…</p>
      </section>
    );
  }

  return (
    <section
      aria-labelledby="operator-task-success-heading"
      className="rounded-lg border border-neutral-200 bg-white p-4 dark:border-neutral-800 dark:bg-neutral-900"
    >
      <h2 id="operator-task-success-heading" className="text-sm font-semibold text-neutral-900 dark:text-neutral-100">
        Onboarding funnel
      </h2>
      <dl className="mt-3 grid grid-cols-3 gap-3 text-center">
        <div>
          <dd className="m-0 text-2xl font-bold text-neutral-900 dark:text-neutral-100">{data.firstSessionCompletedTotal}</dd>
          <dt className="text-[10px] uppercase text-neutral-500 dark:text-neutral-400">Sessions</dt>
        </div>
        <div>
          <dd className="m-0 text-2xl font-bold text-neutral-900 dark:text-neutral-100">{data.firstRunCommittedTotal}</dd>
          <dt className="text-[10px] uppercase text-neutral-500 dark:text-neutral-400">Committed</dt>
        </div>
        <div>
          <dd className="m-0 text-2xl font-bold text-neutral-900 dark:text-neutral-100">
            {data.firstSessionCompletedTotal > 0
              ? `${Math.round(data.firstRunCommittedPerSessionRatio * 100)}%`
              : "—"}
          </dd>
          <dt className="text-[10px] uppercase text-neutral-500 dark:text-neutral-400">Conversion</dt>
        </div>
      </dl>
      <p className="mt-2 text-center text-[10px] text-neutral-400 dark:text-neutral-500">{data.windowNote}</p>
    </section>
  );
}
