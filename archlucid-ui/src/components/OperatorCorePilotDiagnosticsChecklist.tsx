"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";

import { OptInTourLauncher } from "@/components/tour/OptInTourLauncher";
import { CORE_PILOT_STEPS } from "@/lib/core-pilot-steps";
import {
  CORE_PILOT_CHECKLIST_CHANGED_EVENT,
  corePilotStepDoneStorageKey,
} from "@/lib/core-pilot-checklist-storage";
import type { OperatorTaskSuccessRates } from "@/lib/fetch-operator-task-success-rates";
import { fetchOperatorTaskSuccessRates } from "@/lib/fetch-operator-task-success-rates";
import { CORE_PILOT_FIRST_SESSION_GUIDANCE } from "@/lib/core-pilot-first-review-copy";

/**
 * Progressive-disclosure checklist summary: aligns Core Pilot titles with `/v1/diagnostics/operator-task-success-rates`
 * counters (sessions / finalized runs) plus local checklist storage. Companion to the richer first-review checklist sidebar.
 */
export function OperatorCorePilotDiagnosticsChecklist() {
  const [rates, setRates] = useState<OperatorTaskSuccessRates | null>(null);

  const [ratesError, setRatesError] = useState<string | null>(null);

  /** Avoid reading localStorage until after mount so SSR and the first client paint match (hydration-safe). */
  const [checklistStorageHydrated, setChecklistStorageHydrated] = useState(false);

  const [, rerenderAfterChecklist] = useState(0);

  const bumpChecklist = useCallback(() => {
    rerenderAfterChecklist((n) => n + 1);
  }, []);

  useEffect(() => {
    setChecklistStorageHydrated(true);
  }, []);

  useEffect(() => {
    let cancelled = false;

    void (async () => {
      try {
        const data = await fetchOperatorTaskSuccessRates();

        if (!cancelled) {
          setRates(data);
          setRatesError(null);
        }
      } catch {
        if (!cancelled) {
          setRates(null);
          setRatesError("Signals unavailable.");
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    if (typeof window === "undefined") {
      return;
    }

    window.addEventListener(CORE_PILOT_CHECKLIST_CHANGED_EVENT, bumpChecklist);

    return () => {
      window.removeEventListener(CORE_PILOT_CHECKLIST_CHANGED_EVENT, bumpChecklist);
    };
  }, [bumpChecklist]);

  function isStorageDone(index: number): boolean {
    if (!checklistStorageHydrated) {
      return false;
    }

    try {
      return window.localStorage.getItem(corePilotStepDoneStorageKey(index)) === "1";
    } catch {
      return false;
    }
  }

  const finalizedRecorded = rates !== null ? rates.firstRunCommittedTotal >= 1 : false;

  const sessionRecorded = rates !== null ? rates.firstSessionCompletedTotal >= 1 : false;

  return (
    <details className="group rounded-lg border border-neutral-200 bg-white px-4 py-3 shadow-sm dark:border-neutral-700 dark:bg-neutral-900">
      <summary className="cursor-pointer select-none text-sm font-semibold text-neutral-900 marker:text-neutral-400 dark:text-neutral-100">
        First architecture review checklist (signals + steps)
      </summary>
      <div className="mt-3 space-y-4 border-t border-neutral-200 pt-3 dark:border-neutral-700">
        <p className="m-0 text-xs leading-snug text-neutral-600 dark:text-neutral-400">
          {CORE_PILOT_FIRST_SESSION_GUIDANCE}
        </p>
        <section aria-labelledby="core-pilot-signals-heading">
          <h3 id="core-pilot-signals-heading" className="m-0 text-xs font-semibold uppercase tracking-wide text-neutral-600 dark:text-neutral-400">
            Server-tracked onboarding signals (this deployment)
          </h3>

          <p className="m-0 mt-2 text-xs leading-snug text-neutral-600 dark:text-neutral-400">
            These counters are process-lifetime for this deployment and reset when the API host restarts.
          </p>

          {ratesError !== null ? (
            <p className="m-0 mt-2 text-xs text-amber-700 dark:text-amber-300" role="status">
              {ratesError}{" "}
              <strong className="font-medium">Manual checklist toggles beside this card still apply.</strong>
            </p>
          ) : null}

          {rates !== null ? (
            <dl className="m-0 mt-3 grid grid-cols-3 gap-2 text-center">
              <div className="rounded-md border border-neutral-200 px-2 py-2 dark:border-neutral-700">
                <dt className="text-[10px] uppercase tracking-wide text-neutral-500 dark:text-neutral-400">Sessions</dt>
                <dd className="m-0 text-lg font-bold text-neutral-900 dark:text-neutral-100">{rates.firstSessionCompletedTotal}</dd>
              </div>
              <div className="rounded-md border border-neutral-200 px-2 py-2 dark:border-neutral-700">
                <dt className="text-[10px] uppercase tracking-wide text-neutral-500 dark:text-neutral-400">Finalized</dt>
                <dd className="m-0 text-lg font-bold text-neutral-900 dark:text-neutral-100">{rates.firstRunCommittedTotal}</dd>
              </div>
              <div className="rounded-md border border-neutral-200 px-2 py-2 dark:border-neutral-700">
                <dt className="text-[10px] uppercase tracking-wide text-neutral-500 dark:text-neutral-400">Conversion</dt>
                <dd className="m-0 text-lg font-bold text-neutral-900 dark:text-neutral-100">
                  {rates.firstSessionCompletedTotal > 0
                    ? `${Math.round(rates.firstRunCommittedPerSessionRatio * 100)}%`
                    : "—"}
                </dd>
              </div>
            </dl>
          ) : null}

          {rates !== null ? (
            <p className="m-0 mt-2 text-center text-[10px] text-neutral-400 dark:text-neutral-500">{rates.windowNote}</p>
          ) : null}

          <div className="mt-3 flex flex-wrap gap-2 border-t border-dashed border-neutral-200 pt-3 dark:border-neutral-700">
            <span className="text-[11px] text-neutral-600 dark:text-neutral-400">
              Registration/session signal:{" "}
              <strong className="font-semibold text-neutral-800 dark:text-neutral-200">{sessionRecorded ? "recorded" : "not recorded"}</strong>
            </span>
            <span className="text-[11px] text-neutral-400" aria-hidden>
              ·
            </span>
            <span className="text-[11px] text-neutral-600 dark:text-neutral-400">
              Finalization signal:{" "}
              <strong className="font-semibold text-neutral-800 dark:text-neutral-200">{finalizedRecorded ? "≥1 finalized run" : "waiting"}</strong>
            </span>
          </div>
        </section>

        <section aria-labelledby="core-pilot-step-map-heading">
          <h3 id="core-pilot-step-map-heading" className="m-0 text-xs font-semibold uppercase tracking-wide text-neutral-600 dark:text-neutral-400">
            Steps (storage + inferred from finalize counters)
          </h3>

          <ol className="m-0 mt-2 list-none space-y-2 p-0">
            {CORE_PILOT_STEPS.map((step, index) => {
              const inferredFinalize = (index === 2 || index === 3) && finalizedRecorded;

              const storageDone = isStorageDone(index);

              const doneDisplay = inferredFinalize || storageDone;

              return (
                <li key={step.title}>
                  <div className="flex gap-2 text-xs leading-snug text-neutral-800 dark:text-neutral-200">
                    <span className={doneDisplay ? "text-teal-600 dark:text-teal-400" : "text-neutral-400"} aria-hidden>
                      {doneDisplay ? "✓" : "○"}
                    </span>
                    <div className="min-w-0 flex-1">
                      <strong className="font-semibold">{index + 1}. {step.title}</strong>
                      <p className="m-0 mt-0.5 text-[11px] text-neutral-600 dark:text-neutral-400">{step.shortBody}</p>
                      {!storageDone && inferredFinalize ? (
                        <p className="m-0 mt-1 text-[10px] text-teal-700 dark:text-teal-300">
                          Completed via finalize counter — update the sidebar checklist to match when you&apos;re ready.
                        </p>
                      ) : null}
                      <Link
                        className="mt-1 inline-block text-teal-800 underline decoration-teal-300/50 underline-offset-2 hover:text-teal-950 dark:text-teal-300 dark:hover:text-teal-200"
                        href={step.primaryHref}
                      >
                        {step.primaryLabel} →
                      </Link>
                    </div>
                  </div>
                </li>
              );
            })}
          </ol>

          <p className="m-0 mt-3 text-[11px] text-neutral-500 dark:text-neutral-400">
            The sidebar <strong className="font-medium text-neutral-700 dark:text-neutral-300">first-review checklist</strong>
            {": "}
            checkbox progress is stored locally; finalization milestones also appear in server counters above once the pipeline persists.
          </p>

          <div className="mt-3 flex flex-wrap items-center gap-3 border-t border-neutral-200 pt-3 dark:border-neutral-700">
            <OptInTourLauncher buttonVariant="outline" />

            <Link
              href="#first-run-workflow-panel"
              className="inline-flex rounded-md border border-neutral-300 bg-white px-2.5 py-1 text-xs font-medium text-teal-800 no-underline hover:bg-neutral-50 dark:border-neutral-600 dark:bg-neutral-950 dark:text-teal-200 dark:hover:bg-neutral-900"
            >
              Jump to first-review checklist
            </Link>
          </div>
        </section>
      </div>
    </details>
  );
}
