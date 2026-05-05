"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";

import { DocumentLayout } from "@/components/DocumentLayout";
import { LayerHeader } from "@/components/LayerHeader";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { Button } from "@/components/ui/button";
import type { ApiProblemDetails } from "@/lib/api-problem";
import { isApiRequestError } from "@/lib/api-request-error";
import { mergeRegistrationScopeForProxy } from "@/lib/proxy-fetch-registration-scope";
import { buildPilotValueReportQuery } from "@/lib/pilot-value-report-fetch";
import type { PilotValueReportJson, PilotValueReportSeverityJson } from "@/types/pilot-value-report";

function MetricCard(props: { title: string; value: string; hint?: string }) {
  return (
    <div className="rounded-lg border border-neutral-200 bg-white p-4 shadow-sm dark:border-neutral-800 dark:bg-neutral-900">
      <p className="text-xs font-medium uppercase tracking-wide text-neutral-500 dark:text-neutral-400">{props.title}</p>
      <p className="mt-2 font-mono text-2xl font-semibold text-neutral-900 tabular-nums dark:text-neutral-100">
        {props.value}
      </p>
      {props.hint ? (
        <p className="mt-1 text-xs text-neutral-500 dark:text-neutral-400">{props.hint}</p>
      ) : null}
    </div>
  );
}

function formatAvgCompletion(seconds: number | null): string {
  if (seconds === null || Number.isNaN(seconds)) {
    return "—";
  }

  if (seconds >= 3600) {
    return `${(seconds / 3600).toFixed(1)} h`;
  }

  if (seconds >= 60) {
    return `${(seconds / 60).toFixed(1)} min`;
  }

  return `${seconds.toFixed(0)} s`;
}

function SeverityBars(props: { counts: PilotValueReportSeverityJson }) {
  const rows = useMemo(
    () =>
      [
        { label: "Critical", n: props.counts.critical, barClass: "bg-red-600" },
        { label: "High", n: props.counts.high, barClass: "bg-orange-600" },
        { label: "Medium", n: props.counts.medium, barClass: "bg-amber-500" },
        { label: "Low", n: props.counts.low, barClass: "bg-blue-500" },
        { label: "Info", n: props.counts.info, barClass: "bg-neutral-400 dark:bg-neutral-600" },
      ] as const,
    [props.counts],
  );

  const max = Math.max(1, ...rows.map((r) => r.n));

  return (
    <div className="space-y-3">
      {rows.map((r) => (
        <div key={r.label} className="grid grid-cols-[5.5rem_1fr_3rem] items-center gap-2 text-sm">
          <span className="text-neutral-600 dark:text-neutral-400">{r.label}</span>
          <div className="h-3 rounded bg-neutral-100 dark:bg-neutral-800">
            <div
              className={`h-3 rounded ${r.barClass}`}
              style={{ width: `${Math.min(100, (r.n / max) * 100)}%` }}
              title={`${r.label}: ${r.n}`}
            />
          </div>
          <span className="text-right font-mono tabular-nums text-neutral-800 dark:text-neutral-200">{r.n}</span>
        </div>
      ))}
    </div>
  );
}

function buildQuery(fromIso: string, toIso: string): string {
  return buildPilotValueReportQuery(fromIso, toIso);
}

export default function PilotValueReportPage() {
  const [fromUtc, setFromUtc] = useState(() => {
    const d = new Date();

    d.setUTCDate(d.getUTCDate() - 30);

    return d.toISOString().slice(0, 16);
  });
  const [toUtc, setToUtc] = useState(() => new Date().toISOString().slice(0, 16));
  const [data, setData] = useState<PilotValueReportJson | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<{
    message: string;
    problem: ApiProblemDetails | null;
    correlationId: string | null;
  } | null>(null);

  const load = useCallback(async () => {
    setBusy(true);
    setError(null);

    try {
      const fromIso = new Date(fromUtc).toISOString();
      const toIso = new Date(toUtc).toISOString();
      const q = buildQuery(fromIso, toIso);

      const res = await fetch(
        `/api/proxy/v1/tenant/pilot-value-report?${q}`,
        mergeRegistrationScopeForProxy({ headers: { Accept: "application/json" } }),
      );

      if (!res.ok) {
        throw new Error(`HTTP ${res.status}`);
      }

      const json = (await res.json()) as PilotValueReportJson;
      setData(json);
    } catch (e: unknown) {
      if (isApiRequestError(e)) {
        setError({
          message: e.message,
          problem: e.problem,
          correlationId: e.correlationId,
        });
      } else {
        setError({
          message: e instanceof Error ? e.message : "Could not load pilot value report.",
          problem: null,
          correlationId: null,
        });
      }
    } finally {
      setBusy(false);
    }
  }, [fromUtc, toUtc]);

  useEffect(() => {
    void load();
  }, [load]);

  async function onDownloadMarkdown(): Promise<void> {
    setError(null);

    try {
      const fromIso = new Date(fromUtc).toISOString();
      const toIso = new Date(toUtc).toISOString();
      const q = buildQuery(fromIso, toIso);

      const res = await fetch(
        `/api/proxy/v1/tenant/pilot-value-report?${q}`,
        mergeRegistrationScopeForProxy({
          headers: { Accept: "text/markdown" },
        }),
      );

      if (!res.ok) {
        throw new Error(`HTTP ${res.status}`);
      }

      const text = await res.text();
      const blob = new Blob([text], { type: "text/markdown;charset=utf-8" });
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");

      a.href = url;
      a.download = `archlucid-pilot-value-report-${data?.tenantId ?? "tenant"}.md`;
      a.click();
      URL.revokeObjectURL(url);
    } catch (e: unknown) {
      setError({
        message: e instanceof Error ? e.message : "Download failed.",
        problem: null,
        correlationId: null,
      });
    }
  }

  async function onEmailSponsor(): Promise<void> {
    setError(null);

    try {
      const fromIso = new Date(fromUtc).toISOString();
      const toIso = new Date(toUtc).toISOString();
      const q = buildQuery(fromIso, toIso);

      const res = await fetch(
        `/api/proxy/v1/tenant/pilot-value-report?${q}`,
        mergeRegistrationScopeForProxy({
          headers: { Accept: "text/markdown" },
        }),
      );

      if (!res.ok) {
        throw new Error(`HTTP ${res.status}`);
      }

      const text = await res.text();
      const subject = encodeURIComponent("ArchLucid pilot value report");
      const maxBody = 1800;
      const clipped = text.length > maxBody ? `${text.slice(0, maxBody)}\n\n…(truncated; attach downloaded Markdown for full report)` : text;
      const body = encodeURIComponent(clipped);
      const mail = `mailto:?subject=${subject}&body=${body}`;

      window.location.href = mail;
    } catch (e: unknown) {
      try {
        await navigator.clipboard.writeText(
          data ? JSON.stringify(data, null, 2) : "ArchLucid pilot value report unavailable.",
        );
      } catch {
        /* clipboard unavailable */
      }

      setError({
        message: e instanceof Error ? `${e.message} (summary copied to clipboard as fallback)` : "Email handoff failed.",
        problem: null,
        correlationId: null,
      });
    }
  }

  return (
    <main className="mx-auto space-y-4 p-4 print:w-full">
      <LayerHeader pageKey="value-report-pilot" />
      <DocumentLayout>
        <h1 className="m-0 text-2xl font-semibold text-neutral-900 dark:text-neutral-100">Pilot value report</h1>
        <p className="doc-meta m-0 text-sm text-neutral-600 dark:text-neutral-400">
          One-click proof-of-ROI snapshot: committed reviews, findings, pipeline timing, governance signals, and audit-backed
          recommendation counts for the selected UTC window (<code className="text-xs">toUtc</code> is exclusive, matching the audit export).
        </p>
        <p className="m-0 text-sm">
          <Link href="/value-report/roi" className="font-medium text-blue-700 underline dark:text-blue-400">
            Open ROI summary
          </Link>
        </p>

        <div className="flex flex-wrap items-end gap-3">
          <label className="block text-sm">
            <span className="mb-1 block text-neutral-600 dark:text-neutral-400">From UTC</span>
            <input
              type="datetime-local"
              className="rounded border border-neutral-300 bg-white px-2 py-1 text-sm dark:border-neutral-700 dark:bg-neutral-950"
              value={fromUtc}
              onChange={(e) => setFromUtc(e.target.value)}
            />
          </label>
          <label className="block text-sm">
            <span className="mb-1 block text-neutral-600 dark:text-neutral-400">To UTC (exclusive)</span>
            <input
              type="datetime-local"
              className="rounded border border-neutral-300 bg-white px-2 py-1 text-sm dark:border-neutral-700 dark:bg-neutral-950"
              value={toUtc}
              onChange={(e) => setToUtc(e.target.value)}
            />
          </label>
          <Button type="button" variant="secondary" onClick={() => void load()} disabled={busy}>
            Refresh
          </Button>
          <Button type="button" variant="default" onClick={() => void onDownloadMarkdown()} disabled={busy || !data}>
            Download as Markdown
          </Button>
          <Button type="button" variant="outline" onClick={() => void onEmailSponsor()} disabled={busy}>
            Email to sponsor
          </Button>
        </div>

        {error ? (
          <OperatorApiProblem
            fallbackMessage={error.message}
            problem={error.problem}
            correlationId={error.correlationId}
          />
        ) : null}

        {data ? (
          <div className="space-y-6">
            <div className="rounded-lg border border-amber-200 bg-amber-50 p-3 text-sm text-amber-950 dark:border-amber-900 dark:bg-amber-950/30 dark:text-amber-100">
              {data.runDetailsTruncated ? (
                <p className="m-0">
                  Finding and timing aggregates cap at {data.runDetailCap} earliest committed runs in the window; total committed runs
                  shown separately.
                </p>
              ) : null}
              {data.auditExportTruncated ? (
                <p className={`m-0${data.runDetailsTruncated ? " mt-2" : ""}`}>
                  Audit export hit the row cap; governance and recommendation tallies may be incomplete for very busy tenants.
                </p>
              ) : null}
              {!data.runDetailsTruncated && !data.auditExportTruncated ? (
                <p className="m-0">All committed runs in the window are reflected in detail metrics (within product caps).</p>
              ) : null}
            </div>

            <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
              <MetricCard title="Committed runs" value={data.totalRunsCommitted.toString()} />
              <MetricCard title="Total findings" value={data.totalFindings.toString()} />
              <MetricCard title="Avg completion" value={formatAvgCompletion(data.averagePipelineCompletionSeconds)} />
              <MetricCard
                title="Recommendations (audit)"
                value={data.totalRecommendationsProduced.toString()}
                hint="RecommendationGenerated events"
              />
            </div>

            <section className="rounded-lg border border-neutral-200 bg-white p-4 dark:border-neutral-800 dark:bg-neutral-900">
              <h2 className="mt-0 text-lg font-medium text-neutral-900 dark:text-neutral-100">Severity distribution</h2>
              <SeverityBars counts={data.findingsBySeverity} />
            </section>

            <section className="grid gap-4 lg:grid-cols-2">
              <div className="rounded-lg border border-neutral-200 bg-white p-4 dark:border-neutral-800 dark:bg-neutral-900">
                <h2 className="mt-0 text-lg font-medium text-neutral-900 dark:text-neutral-100">Governance &amp; policy</h2>
                <ul className="m-0 list-none space-y-2 p-0 text-sm text-neutral-700 dark:text-neutral-300">
                  <li>Approvals: {data.governanceApprovals}</li>
                  <li>Rejections: {data.governanceRejections}</li>
                  <li>Policy pack assignments: {data.policyPackAssignments}</li>
                  <li>Comparison / drift detections: {data.comparisonOrDriftDetections}</li>
                  <li>Pending approvals (now): {data.governancePendingApprovalsNow}</li>
                </ul>
              </div>
              <div className="rounded-lg border border-neutral-200 bg-white p-4 dark:border-neutral-800 dark:bg-neutral-900">
                <h2 className="mt-0 text-lg font-medium text-neutral-900 dark:text-neutral-100">Agent types</h2>
                <p className="m-0 font-mono text-sm text-neutral-800 dark:text-neutral-200">
                  {data.uniqueAgentTypes.length ? data.uniqueAgentTypes.join(", ") : "—"}
                </p>
              </div>
            </section>

            <section className="rounded-lg border border-neutral-200 bg-white p-4 dark:border-neutral-800 dark:bg-neutral-900">
              <h2 className="mt-0 text-lg font-medium text-neutral-900 dark:text-neutral-100">Run timeline (detail sample)</h2>
              <div className="overflow-x-auto">
                <table className="min-w-full text-left text-sm">
                  <thead className="border-b border-neutral-200 text-xs uppercase text-neutral-500 dark:border-neutral-800 dark:text-neutral-400">
                    <tr>
                      <th className="py-2 pr-3">Run</th>
                      <th className="py-2 pr-3">Created</th>
                      <th className="py-2 pr-3">Committed</th>
                      <th className="py-2">System</th>
                    </tr>
                  </thead>
                  <tbody>
                    {data.committedRunsTimeline.map((row) => (
                      <tr key={row.runId} className="border-b border-neutral-100 dark:border-neutral-800">
                        <td className="py-2 pr-3 font-mono text-xs">{row.runId}</td>
                        <td className="py-2 pr-3 text-xs text-neutral-600 dark:text-neutral-400">
                          {new Date(row.createdUtc).toISOString()}
                        </td>
                        <td className="py-2 pr-3 text-xs text-neutral-600 dark:text-neutral-400">
                          {row.committedUtc ? new Date(row.committedUtc).toISOString() : "—"}
                        </td>
                        <td className="py-2 text-xs">{row.systemName || "—"}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </section>
          </div>
        ) : null}
      </DocumentLayout>
    </main>
  );
}
