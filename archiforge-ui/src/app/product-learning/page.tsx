"use client";

import type { CSSProperties } from "react";
import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { OperatorEmptyState, OperatorLoadingNotice } from "@/components/OperatorShellMessage";
import { fetchProductLearningDashboard } from "@/lib/api";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
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

/** Same-origin proxy download / view; keeps `since` aligned with the dashboard time range. */
function buildProductLearningReportFileUrl(format: "markdown" | "json", since: string | null): string {
  const params = new URLSearchParams();
  params.set("format", format);

  if (since) {
    params.set("since", since);
  }

  return `/api/proxy/v1/product-learning/report/file?${params.toString()}`;
}

function buildProductLearningReportJsonUrl(since: string | null): string {
  const params = new URLSearchParams();
  params.set("format", "json");

  if (since) {
    params.set("since", since);
  }

  return `/api/proxy/v1/product-learning/report?${params.toString()}`;
}

function formatUtc(iso: string): string {
  try {
    return `${new Date(iso).toLocaleString(undefined, { timeZone: "UTC" })} UTC`;
  } catch {
    return iso;
  }
}

function severityBadgeStyle(severity: string): CSSProperties {
  const s = severity.toLowerCase();

  if (s === "high") {
    return { background: "#fef2f2", color: "#991b1b", padding: "2px 8px", borderRadius: 4, fontSize: 12 };
  }

  if (s === "medium") {
    return { background: "#fffbeb", color: "#92400e", padding: "2px 8px", borderRadius: 4, fontSize: 12 };
  }

  return { background: "#f1f5f9", color: "#475569", padding: "2px 8px", borderRadius: 4, fontSize: 12 };
}

const tableStyle: CSSProperties = {
  width: "100%",
  borderCollapse: "collapse",
  fontSize: 14,
  marginTop: 8,
};

const thTd: CSSProperties = {
  border: "1px solid #e2e8f0",
  padding: "8px 10px",
  textAlign: "left",
  verticalAlign: "top",
};

const numericCell: CSSProperties = { ...thTd, textAlign: "right", fontVariantNumeric: "tabular-nums" };

/**
 * Pilot / product learning dashboard (58R): outcome trends, opportunities, triage — distinct from advisory recommendation learning.
 */
export default function ProductLearningPage() {
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
    void load();
  }, [load]);

  const emptyDataset = bundle !== null && bundle.summary.totalSignalsInScope === 0;

  return (
    <main style={{ maxWidth: 960 }}>
      <h2 style={{ marginTop: 0 }}>Pilot feedback</h2>
      <p style={{ color: "#475569", fontSize: 14, lineHeight: 1.55, maxWidth: 720 }}>
        Scoped rollups from pilot signals: how outputs are trusted, rejected, or revised; recurring artifact patterns; ranked
        improvement ideas; and a merged triage queue. This view is separate from{" "}
        <Link href="/recommendation-learning" style={{ color: "#1d4ed8" }}>
          Recommendation learning
        </Link>{" "}
        (advisory acceptance weights).
      </p>

      <div
        style={{
          display: "flex",
          flexWrap: "wrap",
          gap: 12,
          alignItems: "center",
          marginBottom: 20,
          marginTop: 16,
        }}
      >
        <label style={{ display: "flex", alignItems: "center", gap: 8, fontSize: 14 }}>
          <span style={{ color: "#64748b" }}>Time range</span>
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

      <section style={{ marginBottom: 22 }} aria-labelledby="pl-export-heading">
        <h3 id="pl-export-heading" style={{ fontSize: 15, margin: "0 0 6px", color: "#334155" }}>
          Export for triage
        </h3>
        <p style={{ margin: 0, fontSize: 13, color: "#64748b", maxWidth: 720 }}>
          Human-readable summary for architecture / product review. Raw pilot comments are omitted. Uses the same scope and
          time range as the dashboard above.
        </p>
        <p style={{ margin: "10px 0 0", fontSize: 14 }}>
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
          <p style={{ margin: "8px 0 0", fontSize: 14 }}>Fetching summary, trends, opportunities, and triage from the API…</p>
        </OperatorLoadingNotice>
      ) : null}

      {loading && bundle !== null ? (
        <p style={{ color: "#64748b", fontSize: 13, marginBottom: 16 }} role="status">
          Updating…
        </p>
      ) : null}

      {failure !== null ? (
        <div role="alert" style={{ marginBottom: 16 }}>
          <OperatorApiProblem
            problem={failure.problem}
            fallbackMessage={failure.message}
            correlationId={failure.correlationId}
          />
        </div>
      ) : null}

      {emptyDataset && !loading ? (
        <OperatorEmptyState title="No pilot signals in this scope yet">
          <p style={{ margin: 0, fontSize: 14 }}>
            When feedback is recorded for the current tenant / workspace / project, counts and tables below will populate.
            Scope headers follow the operator shell defaults unless you configure proxy scope overrides.
          </p>
        </OperatorEmptyState>
      ) : null}

      {bundle !== null ? (
        <>
          <section style={{ marginBottom: 28 }} aria-labelledby="pl-kpis-heading">
            <h3 id="pl-kpis-heading" style={{ fontSize: 17, marginBottom: 8 }}>
              Summary
            </h3>
            <p style={{ color: "#64748b", fontSize: 13, marginTop: 0 }}>
              Generated {formatUtc(bundle.summary.generatedUtc)} · {bundle.summary.totalSignalsInScope} signal(s) ·{" "}
              {bundle.summary.distinctRunsTouched} run(s) with signals
            </p>
            <ul
              style={{
                display: "flex",
                flexWrap: "wrap",
                gap: 10,
                listStyle: "none",
                padding: 0,
                margin: "12px 0 0",
              }}
            >
              <li style={{ border: "1px solid #e2e8f0", borderRadius: 8, padding: "10px 14px", minWidth: 140 }}>
                <div style={{ fontSize: 12, color: "#64748b" }}>Rollups</div>
                <div style={{ fontSize: 20, fontWeight: 600 }}>{bundle.summary.topAggregateCount}</div>
              </li>
              <li style={{ border: "1px solid #e2e8f0", borderRadius: 8, padding: "10px 14px", minWidth: 140 }}>
                <div style={{ fontSize: 12, color: "#64748b" }}>Artifact trends</div>
                <div style={{ fontSize: 20, fontWeight: 600 }}>{bundle.summary.artifactTrendCount}</div>
              </li>
              <li style={{ border: "1px solid #e2e8f0", borderRadius: 8, padding: "10px 14px", minWidth: 140 }}>
                <div style={{ fontSize: 12, color: "#64748b" }}>Opportunities</div>
                <div style={{ fontSize: 20, fontWeight: 600 }}>{bundle.summary.improvementOpportunityCount}</div>
              </li>
              <li style={{ border: "1px solid #e2e8f0", borderRadius: 8, padding: "10px 14px", minWidth: 140 }}>
                <div style={{ fontSize: 12, color: "#64748b" }}>Triage items</div>
                <div style={{ fontSize: 20, fontWeight: 600 }}>{bundle.summary.triageQueueItemCount}</div>
              </li>
            </ul>
            <details style={{ marginTop: 16 }}>
              <summary style={{ cursor: "pointer", color: "#334155", fontSize: 14 }}>
                How to read these numbers (notes from the API)
              </summary>
              <ul style={{ color: "#475569", fontSize: 13, lineHeight: 1.5 }}>
                {bundle.summary.summaryNotes.map((note, i) => (
                  <li key={i}>{note}</li>
                ))}
              </ul>
            </details>
          </section>

          <section style={{ marginBottom: 28 }} aria-labelledby="pl-trends-heading">
            <h3 id="pl-trends-heading" style={{ fontSize: 17, marginBottom: 4 }}>
              Trusted vs rejected / revised (by artifact)
            </h3>
            <p style={{ color: "#64748b", fontSize: 13, marginTop: 0 }}>
              Counts per artifact facet — trusted acceptance vs revisions, rejections, and follow-ups. Server order is
              deterministic.
            </p>
            {bundle.trends.trends.length === 0 ? (
              <p style={{ color: "#64748b", fontSize: 14 }} role="status">
                No artifact trend rows (thresholds or time window may filter everything out).
              </p>
            ) : (
              <div style={{ overflowX: "auto" }}>
                <table style={tableStyle}>
                  <thead>
                    <tr style={{ background: "#f8fafc" }}>
                      <th style={thTd}>Artifact / area</th>
                      <th style={numericCell}>Trusted</th>
                      <th style={numericCell}>Revised</th>
                      <th style={numericCell}>Rejected</th>
                      <th style={numericCell}>Follow-up</th>
                      <th style={numericCell}>Runs</th>
                      <th style={thTd}>Revision / repeat hint</th>
                    </tr>
                  </thead>
                  <tbody>
                    {bundle.trends.trends.map((row) => (
                      <tr key={row.trendKey}>
                        <td style={thTd}>
                          <div>{row.artifactTypeOrHint || "—"}</div>
                          {row.windowLabel ? (
                            <div style={{ fontSize: 12, color: "#64748b" }}>{row.windowLabel}</div>
                          ) : null}
                        </td>
                        <td style={numericCell}>{row.acceptedOrTrustedCount}</td>
                        <td style={numericCell}>{row.revisionCount}</td>
                        <td style={numericCell}>{row.rejectionCount}</td>
                        <td style={numericCell}>{row.needsFollowUpCount}</td>
                        <td style={numericCell}>{row.distinctRunCount}</td>
                        <td style={{ ...thTd, fontSize: 13 }}>
                          {row.repeatedThemeIndicator ?? "—"}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>

          <section style={{ marginBottom: 28 }} aria-labelledby="pl-opps-heading">
            <h3 id="pl-opps-heading" style={{ fontSize: 17, marginBottom: 4 }}>
              Top improvement opportunities
            </h3>
            <p style={{ color: "#64748b", fontSize: 13, marginTop: 0 }}>
              Ranked candidates for product review (not auto-filed work items).
            </p>
            {bundle.opportunities.opportunities.length === 0 ? (
              <p style={{ color: "#64748b", fontSize: 14 }} role="status">
                No opportunities matched the current thresholds.
              </p>
            ) : (
              <ol style={{ paddingLeft: 20, color: "#334155", lineHeight: 1.5 }}>
                {bundle.opportunities.opportunities.map((o) => (
                  <li key={o.opportunityId} style={{ marginBottom: 14 }}>
                    <div style={{ display: "flex", flexWrap: "wrap", alignItems: "baseline", gap: 8 }}>
                      <strong>{o.title}</strong>
                      <span style={severityBadgeStyle(o.severity)}>{o.severity}</span>
                      <span style={{ fontSize: 12, color: "#64748b" }}>
                        {o.affectedArtifactTypeOrWorkflowArea} · {o.evidenceSignalCount} signal(s) · {o.distinctRunCount}{" "}
                        run(s)
                      </span>
                    </div>
                    <p style={{ margin: "6px 0 0", fontSize: 14 }}>{o.summary}</p>
                    {o.repeatedThemeSnippet ? (
                      <p style={{ margin: "6px 0 0", fontSize: 13, color: "#475569" }}>
                        <em>Repeated theme:</em> {o.repeatedThemeSnippet}
                      </p>
                    ) : null}
                  </li>
                ))}
              </ol>
            )}
          </section>

          <section style={{ marginBottom: 24 }} aria-labelledby="pl-triage-heading">
            <h3 id="pl-triage-heading" style={{ fontSize: 17, marginBottom: 4 }}>
              Triage queue
            </h3>
            <p style={{ color: "#64748b", fontSize: 13, marginTop: 0 }}>
              Merged queue: opportunities plus repeated-comment themes that crossed the triage threshold.
            </p>
            {bundle.triage.items.length === 0 ? (
              <p style={{ color: "#64748b", fontSize: 14 }} role="status">
                Queue is empty for this scope and window.
              </p>
            ) : (
              <div style={{ overflowX: "auto" }}>
                <table style={tableStyle}>
                  <thead>
                    <tr style={{ background: "#f8fafc" }}>
                      <th style={numericCell}>#</th>
                      <th style={thTd}>Title</th>
                      <th style={thTd}>Severity</th>
                      <th style={thTd}>Area</th>
                      <th style={thTd}>Detail</th>
                      <th style={thTd}>Suggested next step</th>
                    </tr>
                  </thead>
                  <tbody>
                    {bundle.triage.items.map((item) => (
                      <tr key={item.queueItemId}>
                        <td style={numericCell}>{item.priorityRank}</td>
                        <td style={thTd}>{item.title}</td>
                        <td style={thTd}>
                          <span style={severityBadgeStyle(item.severity)}>{item.severity}</span>
                        </td>
                        <td style={thTd}>{item.affectedArtifactTypeOrWorkflowArea}</td>
                        <td style={{ ...thTd, fontSize: 13, maxWidth: 280 }}>{item.detailSummary}</td>
                        <td style={{ ...thTd, fontSize: 13 }}>{item.suggestedNextAction ?? "—"}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>

          <p style={{ fontSize: 12, color: "#94a3b8" }}>
            Panel timestamps may differ slightly between calls; use <strong>Refresh</strong> after changing time range to
            reload all sections together.
          </p>
        </>
      ) : null}
    </main>
  );
}
