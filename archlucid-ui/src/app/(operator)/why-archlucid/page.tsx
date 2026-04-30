"use client";

import { useEffect, useState } from "react";

import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import {
  getFirstValueReportMarkdown,
  getRunExplanationSummary,
  getSponsorEvidencePack,
  getTenantMeasuredRoi,
  type SponsorEvidencePackPayload,
  type WhyArchLucidSnapshot,
} from "@/lib/api";
import type { TenantCostEstimateResponse } from "@/types/tenant-cost-estimate";
import type { ApiProblemDetails } from "@/lib/api-problem";
import { isApiRequestError } from "@/lib/api-request-error";
import type { CitationReference, RunExplanationSummary } from "@/types/explanation";

type SectionError = {
  message: string;
  problem: ApiProblemDetails | null;
  correlationId: string | null;
};

type WhyArchLucidPageState = {
  snapshot: WhyArchLucidSnapshot | null;
  snapshotError: SectionError | null;
  monthlyCostEstimate: TenantCostEstimateResponse | null;
  measuredDisclaimer: string | null;
  reportMarkdown: string | null;
  reportMissing: boolean;
  reportError: SectionError | null;
  explanation: RunExplanationSummary | null;
  explanationError: SectionError | null;
  sponsorPack: SponsorEvidencePackPayload | null;
  sponsorPackError: SectionError | null;
  loading: boolean;
};

const initialState: WhyArchLucidPageState = {
  snapshot: null,
  snapshotError: null,
  monthlyCostEstimate: null,
  measuredDisclaimer: null,
  reportMarkdown: null,
  reportMissing: false,
  reportError: null,
  explanation: null,
  explanationError: null,
  sponsorPack: null,
  sponsorPackError: null,
  loading: true,
};

function formatWhyPageInstant(iso: string | null | undefined): string {
  const t = (iso ?? "").trim();

  if (t.length === 0) {
    return "—";
  }

  const d = new Date(t);

  if (Number.isNaN(d.getTime())) {
    return "—";
  }

  return t;
}

function toSectionError(e: unknown, fallback: string): SectionError {
  if (isApiRequestError(e)) {
    return { message: e.message, problem: e.problem, correlationId: e.correlationId };
  }

  return {
    message: e instanceof Error ? e.message : fallback,
    problem: null,
    correlationId: null,
  };
}

/**
 * Read-only "Why ArchLucid" proof page (Core Pilot tier, no `requiredAuthority`).
 * Wires the seeded Contoso Retail demo run to live read endpoints:
 *   - `GET /v1/tenant/measured-roi`                     — process counters + optional monthly cost band
 *   - `GET /v1/pilots/runs/{runId}/first-value-report`  — sponsor first-value Markdown
 *   - `GET /v1/explain/runs/{runId}/aggregate`          — executive aggregate explanation + citations
 *   - `GET /v1/pilots/sponsor-evidence-pack`              — single-bundle sponsor proof (Standard tier)
 */
export default function WhyArchLucidPage() {
  const [state, setState] = useState<WhyArchLucidPageState>(initialState);

  useEffect(() => {
    let cancelled = false;

    async function loadAll(): Promise<void> {
      let snapshot: WhyArchLucidSnapshot | null = null;
      let snapshotError: SectionError | null = null;
      let monthlyCostEstimate: TenantCostEstimateResponse | null = null;
      let measuredDisclaimer: string | null = null;
      let sponsorPack: SponsorEvidencePackPayload | null = null;
      let sponsorPackError: SectionError | null = null;

      const [bundleOutcome, sponsorOutcome] = await Promise.allSettled([
        getTenantMeasuredRoi(),
        getSponsorEvidencePack(),
      ]);

      if (bundleOutcome.status === "fulfilled") {
        snapshot = bundleOutcome.value.snapshot;
        monthlyCostEstimate = bundleOutcome.value.monthlyCostEstimate;
        measuredDisclaimer = bundleOutcome.value.disclaimer;
      }

      if (bundleOutcome.status === "rejected") {
        snapshotError = toSectionError(bundleOutcome.reason, "Could not load measured ROI / telemetry bundle.");
      }

      if (sponsorOutcome.status === "fulfilled") sponsorPack = sponsorOutcome.value;

      if (sponsorOutcome.status === "rejected") {
        sponsorPackError = toSectionError(
          sponsorOutcome.reason,
          "Could not load the sponsor evidence pack bundle.",
        );
      }

      const runId = snapshot?.demoRunId?.trim() ?? "";

      let reportMarkdown: string | null = null;
      let reportMissing = false;
      let reportError: SectionError | null = null;
      let explanation: RunExplanationSummary | null = null;
      let explanationError: SectionError | null = null;

      if (runId.length > 0) {
        const [reportResult, explanationResult] = await Promise.allSettled([
          getFirstValueReportMarkdown(runId),
          getRunExplanationSummary(runId),
        ]);

        if (reportResult.status === "fulfilled") {
          if (reportResult.value === null) reportMissing = true;
          else reportMarkdown = reportResult.value;
        } else {
          reportError = toSectionError(reportResult.reason, "Could not load the first-value report.");
        }

        if (explanationResult.status === "fulfilled") {
          explanation = explanationResult.value;
        } else {
          explanationError = toSectionError(explanationResult.reason, "Could not load the run explanation.");
        }
      }

      if (cancelled) return;

      setState({
        snapshot,
        snapshotError,
        monthlyCostEstimate,
        measuredDisclaimer,
        reportMarkdown,
        reportMissing,
        reportError,
        explanation,
        explanationError,
        sponsorPack,
        sponsorPackError,
        loading: false,
      });
    }

    void loadAll();

    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <main
      className="mx-auto max-w-4xl space-y-8 p-4"
      data-testid="why-archlucid-page"
      aria-busy={state.loading}
    >
      <header className="space-y-2">
        <h1 className="text-2xl font-semibold text-neutral-900 dark:text-neutral-100">Why ArchLucid</h1>
        <p className="text-sm text-neutral-600 dark:text-neutral-400">
          A live look behind the curtain. Every section on this page is rendered from the API of the running ArchLucid
          host against the seeded Contoso Retail Modernization demo tenant — no slides, no static screenshots. Counters
          accumulate from the moment the API process starts and reset on restart.
        </p>
      </header>

      <SnapshotSection state={state} />
      <SponsorEvidencePackSection state={state} />
      <MeasuredContextSection state={state} />
      <FirstValueReportSection state={state} />
      <RunExplanationSection state={state} />

      <footer className="border-t border-neutral-200 pt-3 text-xs text-neutral-500 dark:border-neutral-800 dark:text-neutral-400">
        Sources: <code>GET /v1/tenant/measured-roi</code>,{" "}
        <code>GET /v1/pilots/sponsor-evidence-pack</code>,{" "}
        <code>GET /v1/pilots/runs/{state.snapshot?.demoRunId ?? "{runId}"}/first-value-report</code>,{" "}
        <code>GET /v1/explain/runs/{state.snapshot?.demoRunId ?? "{runId}"}/aggregate</code>. See repo{" "}
        <code>docs/SPONSOR_ONE_PAGER.md</code> and <code>docs/go-to-market/POSITIONING.md</code> for narrative context.
      </footer>
    </main>
  );
}

function SnapshotSection({ state }: { readonly state: WhyArchLucidPageState }) {
  return (
    <section
      aria-labelledby="why-archlucid-counters-heading"
      data-testid="why-archlucid-counters"
      className="space-y-3"
    >
      <h2
        id="why-archlucid-counters-heading"
        className="text-lg font-semibold text-neutral-900 dark:text-neutral-100"
      >
        Process counters
      </h2>
      <p className="text-sm text-neutral-600 dark:text-neutral-400">
        Cumulative <code>ArchLucidInstrumentation</code> counters since the API host started, plus the in-scope audit
        row count for the demo tenant.
      </p>

      {state.snapshotError ? (
        <OperatorApiProblem
          problem={state.snapshotError.problem}
          fallbackMessage={state.snapshotError.message}
          correlationId={state.snapshotError.correlationId}
        />
      ) : null}

      {state.snapshot ? (
        <CounterGrid snapshot={state.snapshot} />
      ) : state.loading ? (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-3" aria-busy aria-label="Loading counters">
          <div className="h-24 animate-pulse rounded border border-neutral-200 bg-neutral-100 dark:border-neutral-800 dark:bg-neutral-900/60" />
          <div className="h-24 animate-pulse rounded border border-neutral-200 bg-neutral-100 dark:border-neutral-800 dark:bg-neutral-900/60" />
          <div className="h-24 animate-pulse rounded border border-neutral-200 bg-neutral-100 dark:border-neutral-800 dark:bg-neutral-900/60" />
        </div>
      ) : null}
    </section>
  );
}

function CounterGrid({ snapshot }: { readonly snapshot: WhyArchLucidSnapshot }) {
  const severityRows = Object.entries(snapshot.findingsProducedBySeverity ?? {});

  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
      <Counter label="Runs created" value={snapshot.runsCreatedTotal} hint="archlucid_runs_created_total" />
      <Counter
        label="Audit rows (demo scope)"
        value={snapshot.auditRowCount}
        hint={
          snapshot.auditRowCountTruncated
            ? `IAuditRepository.GetByScopeAsync (capped at ${snapshot.auditRowCount})`
            : "IAuditRepository.GetByScopeAsync"
        }
      />
      <Counter
        label="Findings (all severities)"
        value={severityRows.reduce((sum, [, count]) => sum + (typeof count === "number" && Number.isFinite(count) ? count : 0), 0)}
        hint="sum of archlucid_findings_produced_total"
      />
      {severityRows.length > 0 ? (
        <div className="sm:col-span-3">
          <h3 className="mb-2 text-sm font-medium text-neutral-700 dark:text-neutral-300">By severity</h3>
          <ul className="grid grid-cols-2 gap-2 sm:grid-cols-4">
            {severityRows.map(([severity, count]) => (
              <li
                key={severity}
                className="rounded border border-neutral-200 bg-neutral-50 px-2 py-1 text-xs dark:border-neutral-800 dark:bg-neutral-900"
              >
                <span className="font-medium text-neutral-700 dark:text-neutral-300">{severity}</span>{" "}
                <span className="text-neutral-500">— {count}</span>
              </li>
            ))}
          </ul>
        </div>
      ) : null}
      <p className="sm:col-span-3 text-xs text-neutral-500">
        Snapshot generated {formatWhyPageInstant(snapshot.generatedUtc)} · demo run <code>{snapshot.demoRunId}</code>
      </p>
    </div>
  );
}

function SponsorPackBody({
  sponsorPack,
  pct,
}: {
  readonly sponsorPack: SponsorEvidencePackPayload;
  readonly pct: (ratio: number) => string;
}) {
  const trace = sponsorPack.explainabilityTrace;
  const gov = sponsorPack.governanceOutcomes;
  const proc = sponsorPack.processInstrumentation;
  const runsTracked =
    typeof proc?.runsCreatedTotal === "number" && Number.isFinite(proc.runsCreatedTotal)
      ? proc.runsCreatedTotal
      : 0;

  return (
    <div className="space-y-4">
      <p className="text-xs text-neutral-500">
        Generated {formatWhyPageInstant(sponsorPack.generatedUtc)} · demo run <code>{sponsorPack.demoRunId ?? "—"}</code> · telemetry
        slice matches the process counters ({runsTracked}{" "}
        runs tracked).
      </p>

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        {trace ? (
          <div className="rounded border border-neutral-200 bg-neutral-50 p-3 text-sm dark:border-neutral-800 dark:bg-neutral-900/60">
            <p className="text-xs font-medium uppercase tracking-wide text-neutral-500">Explainability trace</p>
            <p className="mt-2 text-2xl font-semibold tabular-nums">
              {pct(trace.overallCompletenessRatio ?? Number.NaN)}
            </p>
            <p className="mt-1 text-xs text-neutral-600 dark:text-neutral-400">
              {typeof trace.totalFindings === "number" ? trace.totalFindings : 0} findings in persisted snapshot
            </p>
          </div>
        ) : (
          <div className="rounded border border-neutral-200 bg-neutral-50 p-3 text-xs text-neutral-500 dark:border-neutral-800 dark:bg-neutral-900/60">
            Explainability trace metrics not present in this bundle.
          </div>
        )}

        {gov ? (
          <div className="rounded border border-neutral-200 bg-neutral-50 p-3 text-sm dark:border-neutral-800 dark:bg-neutral-900/60">
            <p className="text-xs font-medium uppercase tracking-wide text-neutral-500">Governance outcomes</p>
            <dl className="mt-2 space-y-1 text-xs">
              <div className="flex justify-between gap-2">
                <dt className="text-neutral-500">Pending approvals</dt>
                <dd className="tabular-nums font-medium">{gov.pendingApprovalCount ?? 0}</dd>
              </div>
              <div className="flex justify-between gap-2">
                <dt className="text-neutral-500">Recent decisions</dt>
                <dd className="tabular-nums font-medium">{gov.recentTerminalDecisionCount ?? 0}</dd>
              </div>
              <div className="flex justify-between gap-2">
                <dt className="text-neutral-500">Policy pack rows</dt>
                <dd className="tabular-nums font-medium">{gov.recentPolicyPackChangeCount ?? 0}</dd>
              </div>
            </dl>
          </div>
        ) : (
          <div className="rounded border border-neutral-200 bg-neutral-50 p-3 text-xs text-neutral-500 dark:border-neutral-800 dark:bg-neutral-900/60">
            Governance outcome counters not present in this bundle.
          </div>
        )}
      </div>

      {sponsorPack.demoRunValueReportDelta ? (
        <div className="rounded border border-neutral-200 bg-neutral-50 p-3 dark:border-neutral-800 dark:bg-neutral-900/50">
          <p className="text-xs font-medium uppercase tracking-wide text-neutral-500">Value-report delta</p>
          <dl className="mt-2 grid grid-cols-2 gap-x-4 gap-y-1 text-xs sm:grid-cols-4">
            <div>
              <dt className="text-neutral-500">Wall to commit</dt>
              <dd className="font-mono tabular-nums">
                {sponsorPack.demoRunValueReportDelta.timeToCommittedManifestTotalSeconds != null
                  ? sponsorPack.demoRunValueReportDelta.timeToCommittedManifestTotalSeconds.toFixed(1)
                  : "—"}{" "}
                s
              </dd>
            </div>
            <div>
              <dt className="text-neutral-500">LLM calls</dt>
              <dd className="tabular-nums">{sponsorPack.demoRunValueReportDelta.llmCallCount ?? "—"}</dd>
            </div>
            <div>
              <dt className="text-neutral-500">Audit rows</dt>
              <dd className="tabular-nums">
                {sponsorPack.demoRunValueReportDelta.auditRowCount ?? "—"}
                {sponsorPack.demoRunValueReportDelta.auditRowCountTruncated ? "+" : ""}
              </dd>
            </div>
            <div>
              <dt className="text-neutral-500">Demo watermark</dt>
              <dd className="text-neutral-700 dark:text-neutral-300">
                {sponsorPack.demoRunValueReportDelta.isDemoTenant ? "Contoso seeded" : "Live tenant"}
              </dd>
            </div>
          </dl>

          {(sponsorPack.demoRunValueReportDelta.findingsBySeverity?.length ?? 0) > 0 ? (
            <div className="mt-3">
              <p className="text-xs font-medium text-neutral-600 dark:text-neutral-400">Demo run histogram</p>
              <ul className="mt-2 grid grid-cols-2 gap-2 text-xs sm:grid-cols-4">
                {(sponsorPack.demoRunValueReportDelta.findingsBySeverity ?? []).map((row) => (
                  <li
                    key={row.severity}
                    className="rounded border border-neutral-200 bg-white px-2 py-1 dark:border-neutral-800 dark:bg-neutral-950"
                  >
                    <span className="font-medium">{row.severity}</span> · {row.count}
                  </li>
                ))}
              </ul>
            </div>
          ) : null}
        </div>
      ) : (
        <p className="text-xs text-neutral-500">
          Value-report deltas are unavailable until the canonical demo run is present in-scope (seed Contoso Retail or run{" "}
          <code>pilot up</code>).
        </p>
      )}
    </div>
  );
}

function SponsorEvidencePackSection({ state }: { readonly state: WhyArchLucidPageState }) {
  const pct = (ratio: number) =>
    Number.isFinite(ratio) ? `${(ratio * 100).toFixed(1)}%` : "0%";

  return (
    <section
      aria-labelledby="why-archlucid-sponsor-pack-heading"
      data-testid="why-archlucid-sponsor-pack"
      className="space-y-3 rounded border border-neutral-200 bg-white p-4 dark:border-neutral-800 dark:bg-neutral-950/40"
    >
      <div>
        <h2
          id="why-archlucid-sponsor-pack-heading"
          className="text-lg font-semibold text-neutral-900 dark:text-neutral-100"
        >
          Sponsor evidence pack (live bundle)
        </h2>
        <p className="text-sm text-neutral-600 dark:text-neutral-400">
          Aggregated sponsor-facing proof from <code className="text-xs">GET /v1/pilots/sponsor-evidence-pack</code> —
          complements the seeded Contoso run below without replacing it.
        </p>
      </div>

      {state.sponsorPackError ? (
        <OperatorApiProblem
          problem={state.sponsorPackError.problem}
          fallbackMessage={state.sponsorPackError.message}
          correlationId={state.sponsorPackError.correlationId}
        />
      ) : null}

      {state.sponsorPack && !state.loading ? (
        <SponsorPackBody sponsorPack={state.sponsorPack} pct={pct} />
      ) : state.loading ? (
        <div className="space-y-2" aria-busy aria-label="Loading sponsor evidence pack">
          <div className="h-4 max-w-xl animate-pulse rounded bg-neutral-100 dark:bg-neutral-900/80" />
          <div className="h-28 animate-pulse rounded border border-neutral-200 bg-neutral-50 dark:border-neutral-800 dark:bg-neutral-950/70" />
        </div>
      ) : (
        <p className="text-sm text-neutral-500">Evidence pack unavailable.</p>
      )}
    </section>
  );
}

function Counter({
  label,
  value,
  hint,
}: {
  readonly label: string;
  readonly value: number;
  readonly hint: string;
}) {
  return (
    <div className="rounded border border-neutral-200 bg-white p-3 dark:border-neutral-800 dark:bg-neutral-950">
      <p className="text-xs uppercase tracking-wide text-neutral-500">{label}</p>
      <p className="mt-1 text-2xl font-semibold tabular-nums text-neutral-900 dark:text-neutral-100">{value}</p>
      <p className="mt-1 text-xs text-neutral-500">
        <code>{hint}</code>
      </p>
    </div>
  );
}

function MeasuredContextSection({ state }: { readonly state: WhyArchLucidPageState }) {
  if (state.snapshotError) return null;

  if (!state.measuredDisclaimer && !state.monthlyCostEstimate) return null;

  return (
    <section
      aria-labelledby="why-archlucid-measured-context-heading"
      data-testid="why-archlucid-measured-context"
      className="space-y-3 rounded border border-neutral-200 bg-neutral-50 p-4 dark:border-neutral-800 dark:bg-neutral-900/40"
    >
      <h2
        id="why-archlucid-measured-context-heading"
        className="text-lg font-semibold text-neutral-900 dark:text-neutral-100"
      >
        Measured context (cost + disclaimers)
      </h2>
      <p className="text-sm text-neutral-600 dark:text-neutral-400">
        Same process counters as above, bundled with the tenant&apos;s configured monthly spend band (when available).
        This is planning guidance — not an invoice.
      </p>
      {state.monthlyCostEstimate ? (
        <dl className="grid grid-cols-1 gap-2 text-sm sm:grid-cols-2">
          <div>
            <dt className="text-neutral-500">Tier</dt>
            <dd className="font-medium text-neutral-900 dark:text-neutral-100">{state.monthlyCostEstimate.tier}</dd>
          </div>
          <div>
            <dt className="text-neutral-500">Monthly band ({state.monthlyCostEstimate.currency})</dt>
            <dd className="font-medium tabular-nums text-neutral-900 dark:text-neutral-100">
              {state.monthlyCostEstimate.estimatedMonthlyUsdLow} — {state.monthlyCostEstimate.estimatedMonthlyUsdHigh}
            </dd>
          </div>
        </dl>
      ) : null}
      {state.measuredDisclaimer ? (
        <p className="text-xs text-neutral-600 dark:text-neutral-400">{state.measuredDisclaimer}</p>
      ) : null}
    </section>
  );
}

function FirstValueReportSection({ state }: { readonly state: WhyArchLucidPageState }) {
  return (
    <section
      aria-labelledby="why-archlucid-report-heading"
      data-testid="why-archlucid-first-value-report"
      className="space-y-3"
    >
      <h2 id="why-archlucid-report-heading" className="text-lg font-semibold text-neutral-900 dark:text-neutral-100">
        Sponsor first-value report
      </h2>
      <p className="text-sm text-neutral-600 dark:text-neutral-400">
        Markdown generated by <code>FirstValueReportBuilder</code> from the committed Contoso Retail demo run.
      </p>

      {state.reportError ? (
        <OperatorApiProblem
          problem={state.reportError.problem}
          fallbackMessage={state.reportError.message}
          correlationId={state.reportError.correlationId}
        />
      ) : null}

      {state.reportMissing ? (
        <p className="text-sm text-neutral-500">
          The demo run has not been committed yet — run <code>pilot up</code> or seed the Contoso Retail demo and refresh.
        </p>
      ) : null}

      {state.reportMarkdown ? (
        <pre
          data-testid="why-archlucid-first-value-report-body"
          className="max-h-[480px] overflow-auto rounded border border-neutral-200 bg-neutral-50 p-3 text-xs leading-relaxed text-neutral-800 dark:border-neutral-800 dark:bg-neutral-900 dark:text-neutral-200 whitespace-pre-wrap"
        >
          {state.reportMarkdown}
        </pre>
      ) : !state.reportError && !state.reportMissing && state.loading ? (
        <div className="space-y-2 rounded border border-neutral-200 bg-neutral-50 p-4 dark:border-neutral-800 dark:bg-neutral-900/40">
          <div className="h-3 max-w-xl animate-pulse rounded bg-neutral-200 dark:bg-neutral-700" />
          <div className="mt-4 h-[200px] max-h-[480px] animate-pulse rounded bg-neutral-200/80 dark:bg-neutral-700/70" aria-busy aria-label="Loading first-value report" />
        </div>
      ) : null}
    </section>
  );
}

function RunExplanationSection({ state }: { readonly state: WhyArchLucidPageState }) {
  return (
    <section
      aria-labelledby="why-archlucid-explanation-heading"
      data-testid="why-archlucid-run-explanation"
      className="space-y-3"
    >
      <h2
        id="why-archlucid-explanation-heading"
        className="text-lg font-semibold text-neutral-900 dark:text-neutral-100"
      >
        Run explanation and citations
      </h2>
      <p className="text-sm text-neutral-600 dark:text-neutral-400">
        Aggregate executive explanation persisted on the run, with citations back to the manifest, findings, decision
        traces, and evidence bundles that the explainability trace was built from.
      </p>

      {state.explanationError ? (
        <OperatorApiProblem
          problem={state.explanationError.problem}
          fallbackMessage={state.explanationError.message}
          correlationId={state.explanationError.correlationId}
        />
      ) : null}

      {state.explanation ? (
        <ExplanationPanel summary={state.explanation} />
      ) : !state.explanationError && state.loading ? (
        <div className="space-y-3" aria-busy aria-label="Loading run explanation">
          <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
            <div className="h-16 animate-pulse rounded border border-neutral-200 bg-neutral-100 dark:border-neutral-800 dark:bg-neutral-900/60" />
            <div className="h-16 animate-pulse rounded border border-neutral-200 bg-neutral-100 dark:border-neutral-800 dark:bg-neutral-900/60" />
            <div className="h-16 animate-pulse rounded border border-neutral-200 bg-neutral-100 dark:border-neutral-800 dark:bg-neutral-900/60" />
            <div className="h-16 animate-pulse rounded border border-neutral-200 bg-neutral-100 dark:border-neutral-800 dark:bg-neutral-900/60" />
          </div>
          <div className="h-24 animate-pulse rounded border border-neutral-200 bg-neutral-100 dark:border-neutral-800 dark:bg-neutral-900/60" />
        </div>
      ) : null}
    </section>
  );
}

function ExplanationPanel({ summary }: { readonly summary: RunExplanationSummary }) {
  const themes = summary.themeSummaries ?? [];
  const citations: ReadonlyArray<CitationReference> = summary.citations ?? [];

  return (
    <div className="space-y-4">
      <dl className="grid grid-cols-2 gap-2 text-sm sm:grid-cols-4">
        <ExplanationStat label="Findings" value={summary.findingCount} />
        <ExplanationStat label="Decisions" value={summary.decisionCount} />
        <ExplanationStat label="Unresolved" value={summary.unresolvedIssueCount} />
        <ExplanationStat label="Compliance gaps" value={summary.complianceGapCount} />
      </dl>

      <div className="rounded border border-neutral-200 bg-white p-3 dark:border-neutral-800 dark:bg-neutral-950">
        <h3 className="text-sm font-medium text-neutral-700 dark:text-neutral-300">Overall assessment</h3>
        <p className="mt-1 text-sm text-neutral-700 dark:text-neutral-300">
          {summary.overallAssessment || "(no overall assessment recorded)"}
        </p>
        <p className="mt-1 text-xs text-neutral-500">
          Risk posture: <code>{summary.riskPosture || "unknown"}</code>
          {summary.usedDeterministicFallback ? " · deterministic fallback in use" : ""}
        </p>
      </div>

      {themes.length > 0 ? (
        <div>
          <h3 className="text-sm font-medium text-neutral-700 dark:text-neutral-300">Themes</h3>
          <ul className="mt-1 list-disc pl-5 text-sm text-neutral-700 dark:text-neutral-300">
            {themes.map((t) => (
              <li key={t}>{t}</li>
            ))}
          </ul>
        </div>
      ) : null}

      <div data-testid="why-archlucid-citations">
        <h3 className="text-sm font-medium text-neutral-700 dark:text-neutral-300">
          Citations ({citations.length})
        </h3>
        {citations.length === 0 ? (
          <p className="mt-1 text-sm text-neutral-500">No citations were emitted for this run.</p>
        ) : (
          <ul className="mt-1 space-y-1 text-sm text-neutral-700 dark:text-neutral-300">
            {citations.map((c) => (
              <li key={`${c.kind}:${c.id}`} className="font-mono text-xs">
                <span className="rounded bg-neutral-100 px-1 py-0.5 text-neutral-700 dark:bg-neutral-800 dark:text-neutral-300">
                  {c.kind}
                </span>{" "}
                <span>{c.label}</span>{" "}
                <span className="text-neutral-500">({c.id})</span>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}

function ExplanationStat({ label, value }: { readonly label: string; readonly value: number | null | undefined }) {
  const v = typeof value === "number" && Number.isFinite(value) ? value : null;

  return (
    <div className="rounded border border-neutral-200 bg-white p-2 dark:border-neutral-800 dark:bg-neutral-950">
      <p className="text-xs uppercase tracking-wide text-neutral-500">{label}</p>
      <p className="text-lg font-semibold tabular-nums text-neutral-900 dark:text-neutral-100">
        {v === null ? "—" : v}
      </p>
    </div>
  );
}
