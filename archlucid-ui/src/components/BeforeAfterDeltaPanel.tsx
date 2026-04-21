"use client";

import { useEffect, useState } from "react";

import { mergeRegistrationScopeForProxy } from "@/lib/proxy-fetch-registration-scope";

export type BeforeAfterDeltaPanelProps = {
  /**
   * When provided, the panel uses this run for the measured delta. When omitted, it uses
   * `trialWelcomeRunId` from `GET /v1/tenant/trial-status` so the operator dashboard can render the panel
   * without knowing the seeded run id at build time.
   */
  runId?: string;
};

type TrialStatusPayload = {
  trialWelcomeRunId?: string | null;
  baselineReviewCycleHours?: number | null;
  baselineReviewCycleSource?: string | null;
  baselineReviewCycleCapturedUtc?: string | null;
  firstCommitUtc?: string | null;
};

type PilotRunDeltasPayload = {
  timeToCommittedManifestTotalSeconds?: number | null;
  manifestCommittedUtc?: string | null;
};

type PanelData = {
  baselineHours: number | null;
  baselineSource: string | null;
  baselineCapturedUtc: string | null;
  measuredHours: number | null;
  effectiveRunId: string | null;
  measuredAvailable: boolean;
};

const SECONDS_PER_HOUR = 3600;

function formatHours(hours: number | null): string {
  if (hours === null || !Number.isFinite(hours)) return "—";

  return hours.toFixed(2);
}

function computeDelta(baseline: number | null, measured: number | null): { hours: number; percent: number } | null {
  if (baseline === null || measured === null) return null;
  if (!Number.isFinite(baseline) || !Number.isFinite(measured)) return null;
  if (baseline <= 0) return null;

  const delta = baseline - measured;
  const percent = (delta / baseline) * 100;

  return { hours: delta, percent };
}

export function BeforeAfterDeltaPanel({ runId }: BeforeAfterDeltaPanelProps) {
  const [state, setState] = useState<{ status: "loading" | "ready" | "error" | "skipped"; data: PanelData | null }>({
    status: "loading",
    data: null,
  });

  useEffect(() => {
    let cancelled = false;

    async function load(): Promise<void> {
      try {
        const trialRes = await fetch(
          "/api/proxy/v1/tenant/trial-status",
          mergeRegistrationScopeForProxy({ headers: { Accept: "application/json" } }),
        );

        if (!trialRes.ok) {
          if (!cancelled) setState({ status: "skipped", data: null });

          return;
        }

        const trial = (await trialRes.json()) as TrialStatusPayload;

        if (cancelled) return;

        const baselineHours =
          typeof trial.baselineReviewCycleHours === "number" && Number.isFinite(trial.baselineReviewCycleHours)
            ? trial.baselineReviewCycleHours
            : null;
        const baselineSource = typeof trial.baselineReviewCycleSource === "string" ? trial.baselineReviewCycleSource : null;
        const baselineCapturedUtc =
          typeof trial.baselineReviewCycleCapturedUtc === "string" ? trial.baselineReviewCycleCapturedUtc : null;

        const effectiveRunId = (runId ?? trial.trialWelcomeRunId) || null;

        if (effectiveRunId === null) {
          if (!cancelled) {
            setState({
              status: "ready",
              data: {
                baselineHours,
                baselineSource,
                baselineCapturedUtc,
                measuredHours: null,
                effectiveRunId: null,
                measuredAvailable: false,
              },
            });
          }

          return;
        }

        const deltasRes = await fetch(
          `/api/proxy/v1/pilots/runs/${encodeURIComponent(effectiveRunId)}/pilot-run-deltas`,
          mergeRegistrationScopeForProxy({ headers: { Accept: "application/json" } }),
        );

        let measuredHours: number | null = null;
        let measuredAvailable = false;

        if (deltasRes.ok) {
          const deltas = (await deltasRes.json()) as PilotRunDeltasPayload;
          const seconds = deltas.timeToCommittedManifestTotalSeconds;

          if (typeof seconds === "number" && Number.isFinite(seconds) && seconds > 0) {
            measuredHours = seconds / SECONDS_PER_HOUR;
            measuredAvailable = true;
          }
        }

        if (cancelled) return;

        setState({
          status: "ready",
          data: {
            baselineHours,
            baselineSource,
            baselineCapturedUtc,
            measuredHours,
            effectiveRunId,
            measuredAvailable,
          },
        });
      } catch {
        if (!cancelled) setState({ status: "error", data: null });
      }
    }

    void load();

    return () => {
      cancelled = true;
    };
  }, [runId]);

  if (state.status === "loading" || state.status === "skipped" || state.status === "error") return null;

  const data = state.data;

  if (data === null) return null;

  if (data.baselineHours === null && !data.measuredAvailable) return null;

  const delta = computeDelta(data.baselineHours, data.measuredHours);

  return (
    <section
      data-testid="before-after-delta-panel"
      role="region"
      aria-label="Review-cycle delta before vs measured"
      className="mb-6 max-w-3xl rounded-md border border-neutral-200 bg-white p-4 shadow-sm dark:border-neutral-700 dark:bg-neutral-900"
    >
      <h3 className="m-0 text-sm font-semibold uppercase tracking-wide text-neutral-700 dark:text-neutral-200">
        Review-cycle delta (before vs measured)
      </h3>
      <p className="mt-1 text-xs text-neutral-600 dark:text-neutral-400">
        Same shape as the downloadable value-report PDF — see <code>ValueReportReviewCycleSectionFormatter</code>.
      </p>

      <dl className="mt-3 grid grid-cols-1 gap-3 sm:grid-cols-2">
        <div className="rounded border border-neutral-200 p-3 dark:border-neutral-700">
          <dt className="text-xs font-medium uppercase text-neutral-500 dark:text-neutral-400">Baseline (before)</dt>
          <dd
            data-testid="before-after-delta-baseline-hours"
            className="mt-1 text-2xl font-semibold text-neutral-900 dark:text-neutral-100"
          >
            {formatHours(data.baselineHours)} h
          </dd>
          <dd className="mt-1 text-xs text-neutral-600 dark:text-neutral-400">
            {data.baselineHours === null
              ? "Not provided at signup — using a measured anchor only."
              : data.baselineSource
                ? `Source: ${data.baselineSource}`
                : "Tenant-supplied at trial signup."}
          </dd>
        </div>
        <div className="rounded border border-neutral-200 p-3 dark:border-neutral-700">
          <dt className="text-xs font-medium uppercase text-neutral-500 dark:text-neutral-400">Measured (this run)</dt>
          <dd
            data-testid="before-after-delta-measured-hours"
            className="mt-1 text-2xl font-semibold text-neutral-900 dark:text-neutral-100"
          >
            {formatHours(data.measuredHours)} h
          </dd>
          <dd className="mt-1 text-xs text-neutral-600 dark:text-neutral-400">
            {data.measuredAvailable
              ? `From GET /v1/pilots/runs/${data.effectiveRunId}/pilot-run-deltas.`
              : "No committed manifest yet — commit your first run to populate the measurement."}
          </dd>
        </div>
      </dl>

      {delta !== null ? (
        <p
          data-testid="before-after-delta-summary"
          className="mt-3 rounded bg-teal-50 px-3 py-2 text-sm font-medium text-teal-900 dark:bg-teal-950/40 dark:text-teal-100"
        >
          {delta.hours >= 0
            ? `Delta: ${delta.hours.toFixed(2)} h saved per run (${delta.percent.toFixed(1)}% improvement)`
            : `Delta: measured run took ${Math.abs(delta.hours).toFixed(2)} h longer than the supplied baseline`}
        </p>
      ) : null}
    </section>
  );
}
