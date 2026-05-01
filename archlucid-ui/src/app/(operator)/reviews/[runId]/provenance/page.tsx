import Link from "next/link";

import { notFound } from "next/navigation";

import { DocumentLayout } from "@/components/DocumentLayout";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { ProvenanceGraphDiagram } from "@/components/ProvenanceGraphDiagram";
import { RunTraceViewerLink } from "@/components/RunTraceViewerLink";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { isApiNotFoundFailure, toApiLoadFailure } from "@/lib/api-load-failure";
import { isInvalidGuidOrSlugRouteToken } from "@/lib/route-dynamic-param";
import { tryStaticDemoProvenanceGraph } from "@/lib/operator-static-demo";
import { type ApiResponseWithTrace, getArchitectureRunProvenance } from "@/lib/api";
import type { ArchitectureRunProvenanceGraph } from "@/types/architecture-provenance";

function formatUtc(iso: string): string {
  const d = new Date(iso);

  if (Number.isNaN(d.getTime())) {
    return iso;
  }

  return d.toISOString().replace("T", " ").replace(/\.\d{3}Z$/, " UTC");
}

/** Server-rendered coordinator provenance: linkage graph + trace timeline (GET /v1/architecture/runs/…/provenance). */
export default async function RunProvenancePage({
  params,
}: {
  params: Promise<{ runId: string }>;
}) {
  const { runId } = await params;

  if (isInvalidGuidOrSlugRouteToken(runId)) {
    notFound();
  }

  let loadFailure: ApiLoadFailureState | null = null;
  let provenanceResponse: ApiResponseWithTrace<ArchitectureRunProvenanceGraph> | null = null;

  try {
    provenanceResponse = await getArchitectureRunProvenance(runId);
  } catch (e) {
    loadFailure = toApiLoadFailure(e);
  }

  if (loadFailure !== null || provenanceResponse === null) {
    const demoGraph = tryStaticDemoProvenanceGraph(runId);

    if (demoGraph !== null) {
      provenanceResponse = { data: demoGraph, traceId: null };
      loadFailure = null;
    }
  }

  if (provenanceResponse !== null) {
    const nodes = provenanceResponse.data.nodes;

    if (nodes.length === 0) {
      const demoGraph = tryStaticDemoProvenanceGraph(runId);

      if (demoGraph !== null && demoGraph.nodes.length > 0) {
        provenanceResponse = { data: demoGraph, traceId: provenanceResponse.traceId };
        loadFailure = null;
      }
    }
  }

  if (loadFailure || !provenanceResponse) {
    if (loadFailure !== null && isApiNotFoundFailure(loadFailure)) {
      notFound();
    }

    const fallback =
      loadFailure?.message ?? "Provenance could not be loaded (run missing, broken manifest reference, or transport error).";

    return (
      <main className="mx-auto max-w-3xl p-4">
        <h2 className="text-xl font-semibold text-neutral-900 dark:text-neutral-100">Provenance</h2>
        <OperatorApiProblem
          problem={loadFailure?.problem ?? null}
          fallbackMessage={fallback}
          correlationId={loadFailure?.correlationId ?? null}
        />
        <p className="mt-3 text-sm text-neutral-600 dark:text-neutral-400">
          This view uses the coordinator endpoint <code>/v1/architecture/runs/{"{id}"}/provenance</code>. Authority-only
          runs use <Link href="/graph">Graph</Link> with the authority provenance API instead.
        </p>
        <p>
          <Link href={`/reviews/${runId}`} className="text-teal-800 underline dark:text-teal-300">
            ← Review detail
          </Link>
        </p>
      </main>
    );
  }

  const graph = provenanceResponse.data;
  const provenanceTraceId = provenanceResponse.traceId;
  const tocItems = [
    ...(graph.traceabilityGaps.length > 0 ? [{ id: "trace-gaps", label: "Traceability gaps" as const }] : []),
    { id: "prov-timeline", label: "Trace timeline" as const },
    { id: "prov-nodes", label: "Nodes" as const },
    { id: "prov-edges", label: "Edges" as const },
  ];

  return (
    <main className="mx-auto p-4 print:w-full">
      <DocumentLayout tocItems={tocItems}>
        <h2 className="m-0 text-xl font-bold text-neutral-900 dark:text-neutral-50">Provenance</h2>
        <p className="m-0 text-sm text-neutral-600 dark:text-neutral-400">
          Run <code className="rounded bg-neutral-100 px-1 text-xs dark:bg-neutral-800">{graph.runId}</code> —{" "}
          {graph.nodes.length} nodes, {graph.edges.length} edges, {graph.timeline.length} timeline events.
        </p>
        <div className="mb-4">
          <RunTraceViewerLink traceId={provenanceTraceId} />
        </div>

        {graph.traceabilityGaps.length > 0 ? (
          <section
            id="trace-gaps"
            aria-labelledby="trace-gaps-heading"
            className="mb-5 rounded-md border border-amber-300 bg-amber-50 p-3 dark:border-amber-800 dark:bg-amber-950/40"
          >
            <h3 id="trace-gaps-heading" className="m-0 text-lg font-semibold text-neutral-900 dark:text-neutral-100">
              Traceability gaps
            </h3>
            <ul className="m-0 mt-2 list-disc space-y-1 pl-5 text-sm text-neutral-800 dark:text-neutral-200">
              {graph.traceabilityGaps.map((g) => (
                <li key={g}>{g}</li>
              ))}
            </ul>
          </section>
        ) : null}

        <ProvenanceGraphDiagram nodes={graph.nodes} edges={graph.edges} />

        <p>
          <Link href={`/reviews/${runId}`} className="text-teal-800 underline dark:text-teal-300">
            ← Review detail
          </Link>
        </p>

        <section id="prov-timeline" aria-labelledby="prov-timeline-heading" className="mt-6">
          <h3 id="prov-timeline-heading" className="m-0 text-lg font-semibold text-neutral-900 dark:text-neutral-100">
            Trace timeline
          </h3>
          <p className="mt-1 text-sm text-neutral-600 dark:text-neutral-400">
            Ordered events from run lifecycle, agent work, merge traces, and finalized decisions.
          </p>
          <div className="overflow-x-auto">
            <table className="w-full border-collapse text-sm">
              <thead>
                <tr className="border-b border-neutral-200 dark:border-neutral-700">
                  <th className="bg-neutral-50/90 p-2 text-left font-semibold dark:bg-neutral-900/50">Time (UTC)</th>
                  <th className="bg-neutral-50/90 p-2 text-left font-semibold dark:bg-neutral-900/50">Kind</th>
                  <th className="bg-neutral-50/90 p-2 text-left font-semibold dark:bg-neutral-900/50">Label</th>
                  <th className="bg-neutral-50/90 p-2 text-left font-semibold dark:bg-neutral-900/50">Reference</th>
                </tr>
              </thead>
              <tbody>
                {graph.timeline.map((row) => (
                  <tr key={`${row.timestampUtc}-${row.kind}-${row.referenceId ?? row.label}`}>
                    <td className="border-b border-neutral-100 p-2 align-top whitespace-nowrap dark:border-neutral-800">
                      {formatUtc(row.timestampUtc)}
                    </td>
                    <td className="border-b border-neutral-100 p-2 align-top dark:border-neutral-800">{row.kind}</td>
                    <td className="border-b border-neutral-100 p-2 align-top dark:border-neutral-800">{row.label}</td>
                    <td className="break-all border-b border-neutral-100 p-2 align-top dark:border-neutral-800">
                      {row.referenceId ?? "—"}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>

        <section id="prov-nodes" aria-labelledby="prov-nodes-heading" className="mt-6">
          <h3 id="prov-nodes-heading" className="m-0 text-lg font-semibold text-neutral-900 dark:text-neutral-100">
            Nodes
          </h3>
          <div className="overflow-x-auto">
            <table className="w-full border-collapse text-sm">
              <thead>
                <tr className="border-b border-neutral-200 dark:border-neutral-700">
                  <th className="bg-neutral-50/90 p-2 text-left font-semibold dark:bg-neutral-900/50">Type</th>
                  <th className="bg-neutral-50/90 p-2 text-left font-semibold dark:bg-neutral-900/50">Name</th>
                  <th className="bg-neutral-50/90 p-2 text-left font-semibold dark:bg-neutral-900/50">Reference</th>
                </tr>
              </thead>
              <tbody>
                {graph.nodes.map((n) => (
                  <tr key={n.id} id={`prov-node-row-${n.id}`}>
                    <td className="border-b border-neutral-100 p-2 align-top dark:border-neutral-800">{n.type}</td>
                    <td className="border-b border-neutral-100 p-2 align-top dark:border-neutral-800">{n.name}</td>
                    <td className="break-all border-b border-neutral-100 p-2 align-top dark:border-neutral-800">
                      {n.referenceId}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>

        <section id="prov-edges" aria-labelledby="prov-edges-heading" className="mt-6">
          <h3 id="prov-edges-heading" className="m-0 text-lg font-semibold text-neutral-900 dark:text-neutral-100">
            Edges
          </h3>
          <div className="overflow-x-auto">
            <table className="w-full border-collapse text-sm">
              <thead>
                <tr className="border-b border-neutral-200 dark:border-neutral-700">
                  <th className="bg-neutral-50/90 p-2 text-left font-semibold dark:bg-neutral-900/50">Type</th>
                  <th className="bg-neutral-50/90 p-2 text-left font-semibold dark:bg-neutral-900/50">From</th>
                  <th className="bg-neutral-50/90 p-2 text-left font-semibold dark:bg-neutral-900/50">To</th>
                </tr>
              </thead>
              <tbody>
                {graph.edges.map((e) => (
                  <tr key={e.id}>
                    <td className="border-b border-neutral-100 p-2 align-top dark:border-neutral-800">{e.type}</td>
                    <td className="break-all border-b border-neutral-100 p-2 align-top dark:border-neutral-800">
                      {e.fromNodeId}
                    </td>
                    <td className="break-all border-b border-neutral-100 p-2 align-top dark:border-neutral-800">
                      {e.toNodeId}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      </DocumentLayout>
    </main>
  );
}
