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
      <section aria-labelledby="pilot-outcome-heading" className="mt-6 rounded-md border border-red-200 bg-red-50 p-4 dark:border-red-900 dark:bg-red-950/40">
        <h2 id="pilot-outcome-heading" className="text-base font-semibold text-red-900 dark:text-red-100">
          Pilot outcome (last 30 days)
        </h2>
        <p className="mt-2 text-sm text-red-800 dark:text-red-200" role="alert">
          {error}
        </p>
      </section>
    );
  }

  if (summary is null) {
    return (
      <section aria-labelledby="pilot-outcome-heading" className="mt-6 rounded-md border border-neutral-200 bg-white p-4 dark:border-neutral-800 dark:bg-neutral-900">
        <h2 id="pilot-outcome-heading" className="text-base font-semibold text-neutral-900 dark:text-neutral-100">
          Pilot outcome (last 30 days)
        </h2>
        <p className="mt-2 text-sm text-neutral-600 dark:text-neutral-400">Loading…</p>
      </section>
    );
  }

  return (
    <section aria-labelledby="pilot-outcome-heading" className="mt-6 rounded-md border border-neutral-200 bg-white p-4 dark:border-neutral-800 dark:bg-neutral-900">
      <h2 id="pilot-outcome-heading" className="text-base font-semibold text-neutral-900 dark:text-neutral-100">
        Pilot outcome (last 30 days)
      </h2>
      {summary.runsInPeriod < 1 ? (
        <p className="mt-2 text-sm text-neutral-600 dark:text-neutral-400">
          No runs in this window yet — start with <strong>New run</strong> when you are ready.
        </p>
      ) : (
        <dl className="mt-3 grid gap-2 text-sm text-neutral-800 dark:text-neutral-200 sm:grid-cols-2">
          <div>
            <dt className="text-neutral-500 dark:text-neutral-400">Runs</dt>
            <dd className="font-medium">{summary.runsInPeriod}</dd>
          </div>
          <div>
            <dt className="text-neutral-500 dark:text-neutral-400">Committed manifests</dt>
            <dd className="font-medium">{summary.runsWithCommittedManifest}</dd>
          </div>
          <div className="sm:col-span-2">
            <dt className="text-neutral-500 dark:text-neutral-400">UTC window</dt>
            <dd className="font-mono text-xs">
              {summary.periodStart} → {summary.periodEnd}
            </dd>
          </div>
        </dl>
      )}
    </section>
  );
}
