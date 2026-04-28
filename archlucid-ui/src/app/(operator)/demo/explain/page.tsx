"use client";

import { useEffect, useState } from "react";

import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { getDemoExplain } from "@/lib/api";
import type { ApiProblemDetails } from "@/lib/api-problem";
import { isApiRequestError } from "@/lib/api-request-error";
import type {
  DemoExplainResponse,
  DemoProvenanceGraph,
  DemoProvenanceGraphEdge,
} from "@/types/demo-explain";
import type { CitationReference, RunExplanationSummary } from "@/types/explanation";

type SectionError = {
  message: string;
  problem: ApiProblemDetails | null;
  correlationId: string | null;
};

type DemoExplainPageState = {
  payload: DemoExplainResponse | null;
  notFound: boolean;
  error: SectionError | null;
  loading: boolean;
};

const initialState: DemoExplainPageState = {
  payload: null,
  notFound: false,
  error: null,
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
 * Public, read-only proof route. Renders the **provenance graph** and the **citations-bound aggregate
 * explanation** for the latest committed demo-seed run, side-by-side. Source:
 * `GET /v1/demo/explain` — server-side `DemoReadModelClient` which composes the same application services
 * as `/v1/explain` and `/v1/provenance` but is hard-pinned to the demo tenant scope.
 *
 * The route is gated on `Demo:Enabled=true` at the API; a 404 here covers both
 * "demo seed has not been applied" and "this deployment never exposes the demo surface".
 */
export default function DemoExplainPage() {
  const [state, setState] = useState<DemoExplainPageState>(initialState);

  useEffect(() => {
    let cancelled = false;

    async function load(): Promise<void> {
      try {
        const payload = await getDemoExplain();

        if (cancelled) return;

        if (payload === null) {
          setState({ payload: null, notFound: true, error: null, loading: false });

          return;
        }

        setState({ payload, notFound: false, error: null, loading: false });
      } catch (e: unknown) {
        if (cancelled) return;

        setState({
          payload: null,
          notFound: false,
          error: toSectionError(e, "Could not load the demo explain payload."),
          loading: false,
        });
      }
    }

    void load();

    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <main
      className="mx-auto max-w-6xl space-y-6 p-4"
      data-testid="demo-explain-page"
      aria-busy={state.loading}
    >
      <header className="space-y-2">
        <h1 className="text-2xl font-semibold text-neutral-900 dark:text-neutral-100">
          Example analysis — provenance and explanation
        </h1>
        <p className="text-sm text-neutral-600 dark:text-neutral-400">
          Provenance graph and citations-bound explanation for the example analysis run.
        </p>
        {state.payload ? <DemoStatusBanner payload={state.payload} /> : null}
      </header>

      {state.error ? (
        <OperatorApiProblem
          problem={state.error.problem}
          fallbackMessage={state.error.message}
          correlationId={state.error.correlationId}
        />
      ) : null}

      {state.notFound ? <DemoNotAvailableNotice /> : null}

      {state.payload &&
      state.payload.provenanceGraph &&
      state.payload.runExplanation ? (
        <section
          aria-label="Provenance and explanation"
          className="grid grid-cols-1 gap-6 lg:grid-cols-2"
        >
          <ProvenanceGraphPanel graph={state.payload.provenanceGraph} />
          <ExplanationPanel summary={state.payload.runExplanation} />
        </section>
      ) : !state.error && !state.notFound && state.loading ? (
        <p className="text-sm text-neutral-500">Loading demo explain payload…</p>
      ) : !state.error && !state.notFound && !state.loading && state.payload ? (
        <p className="text-sm text-neutral-600 dark:text-neutral-400" role="status">
          The demo response was incomplete — provenance or explanation is missing. Try again after the API is ready.
        </p>
      ) : null}
    </main>
  );
}

function DemoStatusBanner({ payload }: { readonly payload: DemoExplainResponse }) {
  return (
    <div
      data-testid="demo-explain-status-banner"
      className="rounded border border-amber-300 bg-amber-50 px-3 py-2 text-xs text-amber-900 dark:border-amber-700 dark:bg-amber-950 dark:text-amber-200"
    >
      <span className="font-semibold">{payload.demoStatusMessage}</span> · run{" "}
      <code>{payload.runId}</code>
      {payload.manifestVersion ? (
        <>
          {" "}
          · manifest <code>{payload.manifestVersion}</code>
        </>
      ) : null}{" "}
      · generated <code>{payload.generatedUtc}</code>
    </div>
  );
}

function DemoNotAvailableNotice() {
  return (
    <div
      data-testid="demo-explain-not-available"
      role="status"
      className="rounded border border-neutral-300 bg-neutral-50 p-4 text-sm text-neutral-700 dark:border-neutral-700 dark:bg-neutral-900 dark:text-neutral-300"
    >
      <p className="m-0 font-medium">The example analysis is not available in this environment.</p>
    </div>
  );
}

function ProvenanceGraphPanel({ graph }: { readonly graph: DemoProvenanceGraph }) {
  const adjacencyByNode: Record<string, DemoProvenanceGraphEdge[]> = {};

  graph.edges.forEach((edge) => {
    if (!adjacencyByNode[edge.source]) adjacencyByNode[edge.source] = [];
    adjacencyByNode[edge.source].push(edge);
  });

  return (
    <section
      aria-labelledby="demo-explain-graph-heading"
      data-testid="demo-explain-provenance-graph"
      className="space-y-3 rounded border border-neutral-200 bg-white p-4 dark:border-neutral-800 dark:bg-neutral-950"
    >
      <header className="space-y-1">
        <h2
          id="demo-explain-graph-heading"
          className="text-lg font-semibold text-neutral-900 dark:text-neutral-100"
        >
          Provenance graph
        </h2>
        <p className="text-xs text-neutral-500">
          {graph.nodeCount} nodes · {graph.edgeCount} edges
        </p>
      </header>

      {graph.isEmpty ? (
        <p className="text-sm text-neutral-500">
          The demo run produced no provenance nodes — re-seed the demo to refresh.
        </p>
      ) : (
        <ol className="space-y-2 text-sm" data-testid="demo-explain-provenance-graph-nodes">
          {graph.nodes.map((node) => {
            const outgoing = adjacencyByNode[node.id] ?? [];

            return (
              <li
                key={node.id}
                className="rounded border border-neutral-200 bg-neutral-50 px-3 py-2 dark:border-neutral-800 dark:bg-neutral-900"
              >
                <p className="font-medium text-neutral-900 dark:text-neutral-100">
                  {node.label}{" "}
                  <span className="rounded bg-neutral-200 px-1 py-0.5 text-xs font-normal text-neutral-700 dark:bg-neutral-800 dark:text-neutral-300">
                    {node.type}
                  </span>
                </p>
                <p className="mt-0.5 font-mono text-[11px] text-neutral-500">{node.id}</p>
                {outgoing.length > 0 ? (
                  <ul className="mt-1 list-disc pl-5 text-xs text-neutral-600 dark:text-neutral-400">
                    {outgoing.map((edge) => (
                      <li key={`${edge.source}->${edge.target}:${edge.type}`}>
                        <span className="text-neutral-500">{edge.type} →</span>{" "}
                        <code>{edge.target}</code>
                      </li>
                    ))}
                  </ul>
                ) : null}
              </li>
            );
          })}
        </ol>
      )}
    </section>
  );
}

function ExplanationPanel({ summary }: { readonly summary: RunExplanationSummary }) {
  const themes = summary.themeSummaries ?? [];
  const citations: ReadonlyArray<CitationReference> = summary.citations ?? [];

  return (
    <section
      aria-labelledby="demo-explain-explanation-heading"
      data-testid="demo-explain-run-explanation"
      className="space-y-3 rounded border border-neutral-200 bg-white p-4 dark:border-neutral-800 dark:bg-neutral-950"
    >
      <header className="space-y-1">
        <h2
          id="demo-explain-explanation-heading"
          className="text-lg font-semibold text-neutral-900 dark:text-neutral-100"
        >
          Aggregate explanation &amp; citations
        </h2>
        <p className="text-xs text-neutral-500">
          {summary.findingCount} findings · {summary.decisionCount} decisions ·{" "}
          {citations.length} citations
        </p>
      </header>

      <div className="rounded border border-neutral-200 bg-neutral-50 p-3 dark:border-neutral-800 dark:bg-neutral-900">
        <h3 className="text-sm font-medium text-neutral-700 dark:text-neutral-300">
          Overall assessment
        </h3>
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

      <div data-testid="demo-explain-citations">
        <h3 className="text-sm font-medium text-neutral-700 dark:text-neutral-300">
          Citations ({citations.length})
        </h3>
        {citations.length === 0 ? (
          <p className="mt-1 text-sm text-neutral-500">
            No citations were emitted for this run — explanations on this page are unsupported.
          </p>
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
    </section>
  );
}
