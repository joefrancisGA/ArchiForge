"use client";

import { useEffect, useState } from "react";

type PilotOutcomeSummary = {
  tenantId: string;
  periodStart: string;
  periodEnd: string;
  runsInPeriod: number;
  runsWithCommittedManifest: number;
};

/** Trailing 30-day pilot rollup for operator home (all tiers; empty state when no runs). */
export function PilotOutcomeCard() {
  const [summary, setSummary] = useState<PilotOutcomeSummary | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    void (async () => {
      try {
        const res = await fetch("/api/proxy/v1/pilots/outcome-summary", {
          headers: { Accept: "application/json" },
        });

        if (!res.ok) {
          throw new Error(`HTTP ${res.status}`);
        }

        const json = (await res.json()) as PilotOutcomeSummary;

        if (!cancelled) {
          setSummary(json);
        }
      } catch (e: unknown) {
        if (!cancelled) {
          setError(e instanceof Error ? e.message : "Failed to load pilot outcome summary.");
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
        aria-labelledby="pilot-outcome-heading"
        className="rounded-lg border border-neutral-200 bg-white p-4 dark:border-neutral-800 dark:bg-neutral-900"
      >
        <h2 id="pilot-outcome-heading" className="text-sm font-semibold text-neutral-900 dark:text-neutral-100">
          Pilot health (last 30 days)
        </h2>
        <div className="mt-2 flex items-center gap-2 text-xs text-neutral-500 dark:text-neutral-400">
          <span className="h-2 w-2 rounded-full bg-neutral-300 dark:bg-neutral-600" aria-hidden />
          Data unavailable — {error}
        </div>
      </section>
    );
  }

  if (summary === null) {
    return (
      <section
        aria-labelledby="pilot-outcome-heading"
        className="rounded-lg border border-neutral-200 bg-white p-4 dark:border-neutral-800 dark:bg-neutral-900"
      >
        <h2 id="pilot-outcome-heading" className="text-sm font-semibold text-neutral-900 dark:text-neutral-100">
          Pilot health (last 30 days)
        </h2>
        <p className="mt-2 text-xs text-neutral-500 dark:text-neutral-400">Loading…</p>
      </section>
    );
  }

  if (summary.runsInPeriod < 1) {
    return (
      <section
        aria-labelledby="pilot-outcome-heading"
        className="rounded-lg border border-dashed border-neutral-200 bg-neutral-50/50 p-4 dark:border-neutral-800 dark:bg-neutral-900/50"
      >
        <h2 id="pilot-outcome-heading" className="text-sm font-semibold text-neutral-700 dark:text-neutral-300">
          Pilot health (last 30 days)
        </h2>
        <p className="mt-1.5 text-xs text-neutral-500 dark:text-neutral-400">
          After your first finalized run, this panel will show success rates, finalized manifests, and time-to-finalization trends.
        </p>
      </section>
    );
  }

  const successRate = summary.runsInPeriod > 0
    ? Math.round((summary.runsWithCommittedManifest / summary.runsInPeriod) * 100)
    : 0;

  return (
    <section
      aria-labelledby="pilot-outcome-heading"
      className="rounded-lg border border-neutral-200 bg-white p-4 dark:border-neutral-800 dark:bg-neutral-900"
    >
      <h2 id="pilot-outcome-heading" className="text-sm font-semibold text-neutral-900 dark:text-neutral-100">
        Pilot health (last 30 days)
      </h2>
      <dl className="mt-3 grid grid-cols-3 gap-3 text-center">
        <div>
          <dd className="text-2xl font-bold text-neutral-900 dark:text-neutral-100">{successRate}%</dd>
          <dt className="text-[10px] uppercase text-neutral-500 dark:text-neutral-400">Success rate</dt>
        </div>
        <div>
          <dd className="text-2xl font-bold text-neutral-900 dark:text-neutral-100">{summary.runsInPeriod}</dd>
          <dt className="text-[10px] uppercase text-neutral-500 dark:text-neutral-400">Total runs</dt>
        </div>
        <div>
          <dd className="text-2xl font-bold text-neutral-900 dark:text-neutral-100">{summary.runsWithCommittedManifest}</dd>
          <dt className="text-[10px] uppercase text-neutral-500 dark:text-neutral-400">Finalized</dt>
        </div>
      </dl>
      <p className="mt-2 text-center font-mono text-[10px] text-neutral-400 dark:text-neutral-500">
        {summary.periodStart} → {summary.periodEnd}
      </p>
    </section>
  );
}
