"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { OperatorBrandedNotFound } from "@/components/OperatorBrandedNotFound";
import { OperatorLoadingNotice } from "@/components/OperatorShellMessage";
import { fetchLearningPlanDetail } from "@/lib/api";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { isApiNotFoundFailure, toApiLoadFailure } from "@/lib/api-load-failure";
import { formatIsoUtcForDisplay } from "@/lib/format-iso-utc";
import type { LearningPlanDetailResponse } from "@/types/learning";

/**
 * Single improvement plan detail: steps, priority, and evidence link counts (59R).
 */
export default function PlanningPlanDetailPage() {
  const params = useParams();
  const planIdRaw = params.planId;
  const planId = typeof planIdRaw === "string" ? planIdRaw : Array.isArray(planIdRaw) ? planIdRaw[0] : "";

  const [plan, setPlan] = useState<LearningPlanDetailResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [failure, setFailure] = useState<ApiLoadFailureState | null>(null);

  const load = useCallback(async () => {
    if (!planId.trim()) {
      return;
    }

    setLoading(true);
    setFailure(null);

    try {
      const detail = await fetchLearningPlanDetail(planId);
      setPlan(detail);
    } catch (e) {
      setFailure(toApiLoadFailure(e));
      setPlan(null);
    } finally {
      setLoading(false);
    }
  }, [planId]);

  useEffect(() => {
    void load();
  }, [load]);

  return (
    <main className="max-w-3xl">
      {failure !== null && isApiNotFoundFailure(failure) ? (
        <OperatorBrandedNotFound />
      ) : (
        <>
      <p className="mt-0 mb-4">
        <Link href="/planning" className="text-blue-700 dark:text-blue-400 text-sm">
          ← Back to planning
        </Link>
      </p>

      <h2 className="mt-0">Improvement plan</h2>

      {!planId.trim() ? (
        <p role="alert" className="text-red-700 dark:text-red-400">
          Missing plan id.
        </p>
      ) : null}

      {loading && plan === null && planId.trim().length > 0 ? (
        <OperatorLoadingNotice>
          <strong>Loading plan.</strong>
          <p className="mt-2 text-sm">Fetching plan detail from the API…</p>
        </OperatorLoadingNotice>
      ) : null}

      {failure !== null && !isApiNotFoundFailure(failure) ? (
        <div role="alert" className="mb-4">
          <OperatorApiProblem
            problem={failure.problem}
            fallbackMessage={failure.message}
            correlationId={failure.correlationId}
          />
        </div>
      ) : null}

      {plan !== null ? (
        <>
          <section className="mb-6" aria-labelledby="plan-detail-title">
            <h3 id="plan-detail-title" className="text-xl mb-2">
              {plan.title}
            </h3>
            <p className="text-neutral-700 dark:text-neutral-300 leading-relaxed mt-0">{plan.summary}</p>

            <div className="mt-4">
              <div className="grid grid-cols-[160px_1fr] gap-x-4 gap-y-2 text-sm mb-2 items-baseline">
                <span className="text-neutral-500 dark:text-neutral-400">Priority score</span>
                <span>{plan.priorityScore}</span>
              </div>
              {plan.priorityExplanation ? (
                <div className="grid grid-cols-[160px_1fr] gap-x-4 gap-y-2 text-sm mb-2 items-baseline">
                  <span className="text-neutral-500 dark:text-neutral-400">Priority note</span>
                  <span>{plan.priorityExplanation}</span>
                </div>
              ) : null}
              <div className="grid grid-cols-[160px_1fr] gap-x-4 gap-y-2 text-sm mb-2 items-baseline">
                <span className="text-neutral-500 dark:text-neutral-400">Status</span>
                <span>{plan.status}</span>
              </div>
              <div className="grid grid-cols-[160px_1fr] gap-x-4 gap-y-2 text-sm mb-2 items-baseline">
                <span className="text-neutral-500 dark:text-neutral-400">Created</span>
                <span>{formatIsoUtcForDisplay(plan.createdUtc)}</span>
              </div>
              <div className="grid grid-cols-[160px_1fr] gap-x-4 gap-y-2 text-sm mb-2 items-baseline">
                <span className="text-neutral-500 dark:text-neutral-400">Theme id</span>
                <span className="font-mono text-[13px]">{plan.themeId}</span>
              </div>
            </div>
          </section>

          <section className="mb-6" aria-labelledby="plan-evidence-heading">
            <h4 id="plan-evidence-heading" className="text-base mb-2">
              Evidence counts (linked)
            </h4>
            <ul className="m-0 pl-5 text-neutral-700 dark:text-neutral-300 leading-relaxed">
              <li>Pilot signals: {plan.evidenceCounts.linkedSignalCount}</li>
              <li>Artifacts: {plan.evidenceCounts.linkedArtifactCount}</li>
              <li>Architecture runs: {plan.evidenceCounts.linkedArchitectureRunCount}</li>
            </ul>
          </section>

          {plan.theme ? (
            <section className="mb-6" aria-labelledby="plan-theme-heading">
              <h4 id="plan-theme-heading" className="text-base mb-2">
                Parent theme
              </h4>
              <p className="mb-2 font-semibold">{plan.theme.title}</p>
              <p className="mb-2 text-sm text-neutral-600 dark:text-neutral-400">{plan.theme.summary}</p>
              <p className="m-0 text-[13px] text-neutral-500 dark:text-neutral-400">
                Evidence signals: {plan.theme.evidenceSignalCount} · Runs: {plan.theme.distinctRunCount} · Severity:{" "}
                {plan.theme.severityBand}
              </p>
            </section>
          ) : null}

          <section className="mb-6" aria-labelledby="plan-steps-heading">
            <h4 id="plan-steps-heading" className="text-base mb-2">
              Action steps
            </h4>
            {plan.actionSteps.length === 0 ? (
              <p className="text-neutral-500 dark:text-neutral-400 text-sm">No steps recorded.</p>
            ) : (
              <ol className="m-0 pl-[22px] leading-relaxed text-neutral-700 dark:text-neutral-300">
                {[...plan.actionSteps].sort((a, b) => a.ordinal - b.ordinal).map((s) => (
                  <li key={`${s.ordinal}-${s.actionType}`} className="mb-3">
                    <strong>
                      {s.ordinal}. {s.actionType}
                    </strong>
                    <p className="mt-1.5 text-sm">{s.description}</p>
                    {s.acceptanceCriteria ? (
                      <p className="mt-1.5 text-[13px] text-neutral-600 dark:text-neutral-400">
                        <em>Acceptance:</em> {s.acceptanceCriteria}
                      </p>
                    ) : null}
                  </li>
                ))}
              </ol>
            )}
          </section>
        </>
      ) : null}
        </>
      )}
    </main>
  );
}
