"use client";

import { useEffect, useState } from "react";

import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import {
  getFirstValueReportMarkdown,
  getRunExplanationSummary,
  getWhyArchLucidSnapshot,
  type WhyArchLucidSnapshot,
} from "@/lib/api";
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
  reportMarkdown: string | null;
  reportMissing: boolean;
  reportError: SectionError | null;
  explanation: RunExplanationSummary | null;
  explanationError: SectionError | null;
  loading: boolean;
};

const initialState: WhyArchLucidPageState = {
  snapshot: null,
  snapshotError: null,
  reportMarkdown: null,
  reportMissing: false,
  reportError: null,
  explanation: null,
  explanationError: null,
  loading: true,
};

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
 * Wires the seeded Contoso Retail demo run to three live read endpoints:
 *   - `GET /v1/pilots/why-archlucid-snapshot`           — process counters + canonical demo run id
 *   - `GET /v1/pilots/runs/{runId}/first-value-report`  — sponsor first-value Markdown
 *   - `GET /v1/explain/runs/{runId}/aggregate`          — executive aggregate explanation + citations
 */
export default function WhyArchLucidPage() {
  const [state, setState] = useState<WhyArchLucidPageState>(initialState);

  useEffect(() => {
    let cancelled = false;

    async function loadAll(): Promise<void> {
      let snapshot: WhyArchLucidSnapshot | null = null;
      let snapshotError: SectionError | null = null;

      try {
        snapshot = await getWhyArchLucidSnapshot();
      } catch (e: unknown) {
        snapshotError = toSectionError(e, "Could not load the why-ArchLucid telemetry snapshot.");
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
        reportMarkdown,
        reportMissing,
        reportError,
        explanation,
        explanationError,
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
      <FirstValueReportSection state={state} />
      <RunExplanationSection state={state} />

      <footer className="border-t border-neutral-200 pt-3 text-xs text-neutral-500 dark:border-neutral-800 dark:text-neutral-400">
        Sources: <code>GET /v1/pilots/why-archlucid-snapshot</code>,{" "}
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
        <p className="text-sm text-neutral-500">Loading telemetry…</p>
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
        value={severityRows.reduce((sum, [, count]) => sum + count, 0)}
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
        Snapshot generated {snapshot.generatedUtc} · demo run <code>{snapshot.demoRunId}</code>
      </p>
    </div>
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
        <p className="text-sm text-neutral-500">Loading first-value report…</p>
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
        <p className="text-sm text-neutral-500">Loading run explanation…</p>
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

function ExplanationStat({ label, value }: { readonly label: string; readonly value: number }) {
  return (
    <div className="rounded border border-neutral-200 bg-white p-2 dark:border-neutral-800 dark:bg-neutral-950">
      <p className="text-xs uppercase tracking-wide text-neutral-500">{label}</p>
      <p className="text-lg font-semibold tabular-nums text-neutral-900 dark:text-neutral-100">{value}</p>
    </div>
  );
}
