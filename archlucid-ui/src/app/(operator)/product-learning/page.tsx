"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { OperatorEmptyState, OperatorLoadingNotice } from "@/components/OperatorShellMessage";
import { fetchProductLearningDashboard } from "@/lib/api";
import {
  buildProductLearningReportFileUrl,
  buildProductLearningReportJsonUrl,
} from "@/lib/product-learning-report-urls";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import { isNextPublicDemoMode } from "@/lib/demo-ui-env";
import type { ProductLearningDashboardBundle } from "@/types/product-learning";

type TimeRangeKey = "all" | "7d" | "30d";

function sinceIsoForRange(key: TimeRangeKey): string | null {
  if (key === "all") {
    return null;
  }

  const days = key === "7d" ? 7 : 30;
  const d = new Date();
  d.setUTCDate(d.getUTCDate() - days);

  return d.toISOString();
}

function formatUtc(iso: string): string {
  try {
    return `${new Date(iso).toLocaleString(undefined, { timeZone: "UTC" })} UTC`;
  } catch {
    return iso;
  }
}

function severityBadgeClass(severity: string): string {
  const s = severity.toLowerCase();
  const base = "px-2 py-0.5 rounded text-xs";

  if (s === "high")
    return `${base} bg-red-50 dark:bg-red-950/30 text-red-800 dark:text-red-400`;

  if (s === "medium")
    return `${base} bg-amber-50 dark:bg-amber-950/40 text-amber-800 dark:text-amber-400`;

  return `${base} bg-neutral-100 dark:bg-neutral-800 text-neutral-600 dark:text-neutral-400`;
}

const tableClass = "w-full border-collapse text-sm mt-2";

const thTdClass = "border border-neutral-200 dark:border-neutral-700 px-2.5 py-2 text-left align-top";

const numericCellClass = "border border-neutral-200 dark:border-neutral-700 px-2.5 py-2 text-right align-top tabular-nums";

/**
 * Pilot / product learning dashboard (58R): outcome trends, opportunities, triage — distinct from advisory recommendation learning.
 */
export default function ProductLearningPage() {
  const router = useRouter();
  const demoMode = isNextPublicDemoMode();
  const [range, setRange] = useState<TimeRangeKey>("all");
  const [bundle, setBundle] = useState<ProductLearningDashboardBundle | null>(null);
  const [loading, setLoading] = useState(false);
  const [failure, setFailure] = useState<ApiLoadFailureState | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setFailure(null);

    try {
      const since = sinceIsoForRange(range);
      const data = await fetchProductLearningDashboard({ since });
      setBundle(data);
    } catch (e) {
      setFailure(toApiLoadFailure(e));
      setBundle(null);
    } finally {
      setLoading(false);
    }
  }, [range]);

  useEffect(() => {
    if (!demoMode) {
      return;
    }

    router.replace("/");
  }, [demoMode, router]);

  useEffect(() => {
    if (demoMode) {
      return;
    }

    void load();
  }, [demoMode, load]);

  const emptyDataset = bundle !== null && bundle.summary.totalSignalsInScope === 0;

  if (demoMode) {
    return (
      <main className="max-w-5xl">
        <OperatorLoadingNotice>Returning to home…</OperatorLoadingNotice>
      </main>
    );
  }

  return (
    <main className="max-w-5xl">
      <h2 className="mt-0">Pilot feedback</h2>
      <p className="text-neutral-600 dark:text-neutral-400 text-sm leading-relaxed max-w-3xl">
        Scoped rollups from pilot signals: how outputs are trusted, rejected, or revised; recurring artifact patterns; ranked
        improvement ideas; and a merged triage queue. This view is separate from{" "}
        <Link href="/recommendation-learning" className="text-blue-700 dark:text-blue-400">
          Recommendation tuning
        </Link>{" "}
        (advisory acceptance weights).
      </p>

      <div className="flex flex-wrap gap-3 items-center mb-5 mt-4">
        <label className="flex items-center gap-2 text-sm">
          <span className="text-neutral-500 dark:text-neutral-400">Time range</span>
          <select
            value={range}
            onChange={(e) => setRange(e.target.value as TimeRangeKey)}
            disabled={loading}
            aria-label="Filter product learning data by time range"
          >
            <option value="all">All time</option>
            <option value="30d">Last 30 days</option>
            <option value="7d">Last 7 days</option>
          </select>
        </label>
        <button type="button" onClick={() => void load()} disabled={loading}>
          Refresh
        </button>
      </div>

      <section className="mb-[22px]" aria-labelledby="pl-export-heading">
        <h3 id="pl-export-heading" className="text-[15px] mb-1.5 text-neutral-700 dark:text-neutral-300">
          Export for triage
        </h3>
        <p className="m-0 text-[13px] text-neutral-500 dark:text-neutral-400 max-w-3xl">
          Human-readable summary for architecture / product review. Raw pilot comments are omitted. Uses the same scope and
          time range as the dashboard above.
        </p>
        <p className="mt-2.5 text-sm">
          <a
            href={buildProductLearningReportFileUrl("markdown", sinceIsoForRange(range))}
            download
          >
            Download Markdown
          </a>
          {" · "}
          <a href={buildProductLearningReportFileUrl("json", sinceIsoForRange(range))} download>
            Download JSON
          </a>
          {" · "}
          <a href={buildProductLearningReportJsonUrl(sinceIsoForRange(range))} target="_blank" rel="noopener noreferrer">
            Open JSON in new tab
          </a>
        </p>
      </section>

      {loading && bundle === null ? (
        <OperatorLoadingNotice>
          <strong>Loading dashboard.</strong>
          <p className="mt-2 text-sm">Fetching summary, trends, opportunities, and triage from the API…</p>
        </OperatorLoadingNotice>
      ) : null}

      {loading && bundle !== null ? (
        <p className="text-neutral-500 dark:text-neutral-400 text-[13px] mb-4" role="status">
          Updating…
        </p>
      ) : null}

      {failure !== null ? (
        <div role="alert" className="mb-4">
          <OperatorApiProblem
            problem={failure.problem}
            fallbackMessage={failure.message}
            correlationId={failure.correlationId}
          />
        </div>
      ) : null}

      {emptyDataset && !loading ? (
        <OperatorEmptyState title="No pilot signals in this scope yet">
          <p className="m-0 text-sm">
            When feedback is recorded for the current tenant / workspace / project, counts and tables below will populate.
            Scope headers follow the operator shell defaults unless you configure proxy scope overrides.
          </p>
        </OperatorEmptyState>
      ) : null}

      {bundle !== null ? (
        <>
          <section className="mb-7" aria-labelledby="pl-kpis-heading">
            <h3 id="pl-kpis-heading" className="text-[17px] mb-2">
              Summary
            </h3>
            <p className="text-neutral-500 dark:text-neutral-400 text-[13px] mt-0">
              Generated {formatUtc(bundle.summary.generatedUtc)} · {bundle.summary.totalSignalsInScope} signal(s) ·{" "}
              {bundle.summary.distinctRunsTouched} run(s) with signals
            </p>
            <ul className="flex flex-wrap gap-2.5 list-none p-0 mt-3">
              <li className="border border-neutral-200 dark:border-neutral-700 rounded-lg px-3.5 py-2.5 min-w-[140px]">
                <div className="text-xs text-neutral-500 dark:text-neutral-400">Rollups</div>
                <div className="text-xl font-semibold">{bundle.summary.topAggregateCount}</div>
              </li>
              <li className="border border-neutral-200 dark:border-neutral-700 rounded-lg px-3.5 py-2.5 min-w-[140px]">
                <div className="text-xs text-neutral-500 dark:text-neutral-400">Artifact trends</div>
                <div className="text-xl font-semibold">{bundle.summary.artifactTrendCount}</div>
              </li>
              <li className="border border-neutral-200 dark:border-neutral-700 rounded-lg px-3.5 py-2.5 min-w-[140px]">
                <div className="text-xs text-neutral-500 dark:text-neutral-400">Opportunities</div>
                <div className="text-xl font-semibold">{bundle.summary.improvementOpportunityCount}</div>
              </li>
              <li className="border border-neutral-200 dark:border-neutral-700 rounded-lg px-3.5 py-2.5 min-w-[140px]">
                <div className="text-xs text-neutral-500 dark:text-neutral-400">Triage items</div>
                <div className="text-xl font-semibold">{bundle.summary.triageQueueItemCount}</div>
              </li>
            </ul>
            <details className="mt-4">
              <summary className="cursor-pointer text-neutral-700 dark:text-neutral-300 text-sm">
                How to read these numbers (notes from the API)
              </summary>
              <ul className="text-neutral-600 dark:text-neutral-400 text-[13px] leading-normal">
                {bundle.summary.summaryNotes.map((note, i) => (
                  <li key={i}>{note}</li>
                ))}
              </ul>
            </details>
          </section>

          <section className="mb-7" aria-labelledby="pl-trends-heading">
            <h3 id="pl-trends-heading" className="text-[17px] mb-1">
              Trusted vs rejected / revised (by artifact)
            </h3>
            <p className="text-neutral-500 dark:text-neutral-400 text-[13px] mt-0">
              Counts per artifact facet — trusted acceptance vs revisions, rejections, and follow-ups. Server order is
              deterministic.
            </p>
            {bundle.trends.trends.length === 0 ? (
              <p className="text-neutral-500 dark:text-neutral-400 text-sm" role="status">
                No artifact trend rows (thresholds or time window may filter everything out).
              </p>
            ) : (
              <div className="overflow-x-auto">
                <table className={tableClass}>
                  <thead>
                    <tr className="bg-neutral-50/90 dark:bg-neutral-900/50">
                      <th className={thTdClass}>Artifact / area</th>
                      <th className={numericCellClass}>Trusted</th>
                      <th className={numericCellClass}>Revised</th>
                      <th className={numericCellClass}>Rejected</th>
                      <th className={numericCellClass}>Follow-up</th>
                      <th className={numericCellClass}>Runs</th>
                      <th className={thTdClass}>Revision / repeat hint</th>
                    </tr>
                  </thead>
                  <tbody>
                    {bundle.trends.trends.map((row) => (
                      <tr key={row.trendKey}>
                        <td className={thTdClass}>
                          <div>{row.artifactTypeOrHint || "—"}</div>
                          {row.windowLabel ? (
                            <div className="text-xs text-neutral-500 dark:text-neutral-400">{row.windowLabel}</div>
                          ) : null}
                        </td>
                        <td className={numericCellClass}>{row.acceptedOrTrustedCount}</td>
                        <td className={numericCellClass}>{row.revisionCount}</td>
                        <td className={numericCellClass}>{row.rejectionCount}</td>
                        <td className={numericCellClass}>{row.needsFollowUpCount}</td>
                        <td className={numericCellClass}>{row.distinctRunCount}</td>
                        <td className={`${thTdClass} text-[13px]`}>
                          {row.repeatedThemeIndicator ?? "—"}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>

          <section className="mb-7" aria-labelledby="pl-opps-heading">
            <h3 id="pl-opps-heading" className="text-[17px] mb-1">
              Top improvement opportunities
            </h3>
            <p className="text-neutral-500 dark:text-neutral-400 text-[13px] mt-0">
              Ranked candidates for product review (not auto-filed work items).
            </p>
            {bundle.opportunities.opportunities.length === 0 ? (
              <p className="text-neutral-500 dark:text-neutral-400 text-sm" role="status">
                No opportunities matched the current thresholds.
              </p>
            ) : (
              <ol className="pl-5 text-neutral-700 dark:text-neutral-300 leading-normal">
                {bundle.opportunities.opportunities.map((o) => (
                  <li key={o.opportunityId} className="mb-3.5">
                    <div className="flex flex-wrap items-baseline gap-2">
                      <strong>{o.title}</strong>
                      <span className={severityBadgeClass(o.severity)}>{o.severity}</span>
                      <span className="text-xs text-neutral-500 dark:text-neutral-400">
                        {o.affectedArtifactTypeOrWorkflowArea} · {o.evidenceSignalCount} signal(s) · {o.distinctRunCount}{" "}
                        run(s)
                      </span>
                    </div>
                    <p className="mt-1.5 text-sm">{o.summary}</p>
                    {o.repeatedThemeSnippet ? (
                      <p className="mt-1.5 text-[13px] text-neutral-600 dark:text-neutral-400">
                        <em>Repeated theme:</em> {o.repeatedThemeSnippet}
                      </p>
                    ) : null}
                  </li>
                ))}
              </ol>
            )}
          </section>

          <section className="mb-6" aria-labelledby="pl-triage-heading">
            <h3 id="pl-triage-heading" className="text-[17px] mb-1">
              Triage queue
            </h3>
            <p className="text-neutral-500 dark:text-neutral-400 text-[13px] mt-0">
              Merged queue: opportunities plus repeated-comment themes that crossed the triage threshold.
            </p>
            {bundle.triage.items.length === 0 ? (
              <p className="text-neutral-500 dark:text-neutral-400 text-sm" role="status">
                Queue is empty for this scope and window.
              </p>
            ) : (
              <div className="overflow-x-auto">
                <table className={tableClass}>
                  <thead>
                    <tr className="bg-neutral-50/90 dark:bg-neutral-900/50">
                      <th className={numericCellClass}>#</th>
                      <th className={thTdClass}>Title</th>
                      <th className={thTdClass}>Severity</th>
                      <th className={thTdClass}>Area</th>
                      <th className={thTdClass}>Detail</th>
                      <th className={thTdClass}>Suggested next step</th>
                    </tr>
                  </thead>
                  <tbody>
                    {bundle.triage.items.map((item) => (
                      <tr key={item.queueItemId}>
                        <td className={numericCellClass}>{item.priorityRank}</td>
                        <td className={thTdClass}>{item.title}</td>
                        <td className={thTdClass}>
                          <span className={severityBadgeClass(item.severity)}>{item.severity}</span>
                        </td>
                        <td className={thTdClass}>{item.affectedArtifactTypeOrWorkflowArea}</td>
                        <td className={`${thTdClass} text-[13px] max-w-[280px]`}>{item.detailSummary}</td>
                        <td className={`${thTdClass} text-[13px]`}>{item.suggestedNextAction ?? "—"}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>

          <p className="text-xs text-neutral-400 dark:text-neutral-500">
            Panel timestamps may differ slightly between calls; use <strong>Refresh</strong> after changing time range to
            reload all sections together.
          </p>
        </>
      ) : null}
    </main>
  );
}
