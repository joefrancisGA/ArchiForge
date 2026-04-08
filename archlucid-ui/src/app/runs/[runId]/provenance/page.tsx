import Link from "next/link";

import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { ProvenanceGraphDiagram } from "@/components/ProvenanceGraphDiagram";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import { getArchitectureRunProvenance } from "@/lib/api";

function formatUtc(iso: string): string {
  const d = new Date(iso);

  if (Number.isNaN(d.getTime()))
    return iso;

  return d.toISOString().replace("T", " ").replace(/\.\d{3}Z$/, " UTC");
}

/** Server-rendered coordinator provenance: linkage graph + trace timeline (GET /v1/architecture/runs/…/provenance). */
export default async function RunProvenancePage({
  params,
}: {
  params: Promise<{ runId: string }>;
}) {
  const { runId } = await params;
  let loadFailure: ApiLoadFailureState | null = null;
  let graph: Awaited<ReturnType<typeof getArchitectureRunProvenance>> | null = null;

  try {
    graph = await getArchitectureRunProvenance(runId);
  } catch (e) {
    loadFailure = toApiLoadFailure(e);
  }

  if (loadFailure || !graph) {
    const fallback =
      loadFailure?.message ?? "Provenance could not be loaded (run missing, broken manifest reference, or transport error).";

    return (
      <main>
        <h2>Provenance</h2>
        <OperatorApiProblem
          problem={loadFailure?.problem ?? null}
          fallbackMessage={fallback}
          correlationId={loadFailure?.correlationId ?? null}
        />
        <p style={{ margin: "12px 0 0", fontSize: 14 }}>
          This view uses the coordinator endpoint <code>/v1/architecture/runs/{"{id}"}/provenance</code>. Authority-only
          runs use <Link href="/graph">Graph</Link> with the authority provenance API instead.
        </p>
        <p>
          <Link href={`/runs/${runId}`}>← Run detail</Link>
        </p>
      </main>
    );
  }

  return (
    <main>
      <h2>Provenance</h2>
      <p style={{ fontSize: 14, marginBottom: 16 }}>
        Run <code>{graph.runId}</code> — {graph.nodes.length} nodes, {graph.edges.length} edges,{" "}
        {graph.timeline.length} timeline events.
      </p>

      {graph.traceabilityGaps.length > 0 ? (
        <section
          aria-labelledby="trace-gaps"
          style={{
            marginBottom: 20,
            padding: 12,
            border: "1px solid #c9a227",
            background: "#fffbeb",
            borderRadius: 4,
          }}
        >
          <h3 id="trace-gaps" style={{ marginTop: 0 }}>
            Traceability gaps
          </h3>
          <ul style={{ margin: 0, paddingLeft: 20, fontSize: 14 }}>
            {graph.traceabilityGaps.map((g) => (
              <li key={g}>{g}</li>
            ))}
          </ul>
        </section>
      ) : null}

      <ProvenanceGraphDiagram nodes={graph.nodes} edges={graph.edges} />

      <p>
        <Link href={`/runs/${runId}`}>← Run detail</Link>
      </p>

      <section aria-labelledby="prov-timeline" style={{ marginTop: 24 }}>
        <h3 id="prov-timeline">Trace timeline</h3>
        <p style={{ fontSize: 13, color: "#444", marginTop: 4 }}>
          Ordered events from run lifecycle, agent work, merge traces, and committed decisions.
        </p>
        <div style={{ overflowX: "auto" }}>
          <table style={{ borderCollapse: "collapse", width: "100%", fontSize: 14 }}>
            <thead>
              <tr>
                <th style={{ textAlign: "left", borderBottom: "1px solid #ccc" }}>Time (UTC)</th>
                <th style={{ textAlign: "left", borderBottom: "1px solid #ccc" }}>Kind</th>
                <th style={{ textAlign: "left", borderBottom: "1px solid #ccc" }}>Label</th>
                <th style={{ textAlign: "left", borderBottom: "1px solid #ccc" }}>Reference</th>
              </tr>
            </thead>
            <tbody>
              {graph.timeline.map((row) => (
                <tr key={`${row.timestampUtc}-${row.kind}-${row.referenceId ?? row.label}`}>
                  <td style={{ padding: "6px 8px 6px 0", verticalAlign: "top", whiteSpace: "nowrap" }}>
                    {formatUtc(row.timestampUtc)}
                  </td>
                  <td style={{ padding: "6px 8px 6px 0", verticalAlign: "top" }}>{row.kind}</td>
                  <td style={{ padding: "6px 8px 6px 0", verticalAlign: "top" }}>{row.label}</td>
                  <td style={{ padding: "6px 8px 6px 0", verticalAlign: "top", wordBreak: "break-all" }}>
                    {row.referenceId ?? "—"}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      <section aria-labelledby="prov-nodes" style={{ marginTop: 24 }}>
        <h3 id="prov-nodes">Nodes</h3>
        <div style={{ overflowX: "auto" }}>
          <table style={{ borderCollapse: "collapse", width: "100%", fontSize: 14 }}>
            <thead>
              <tr>
                <th style={{ textAlign: "left", borderBottom: "1px solid #ccc" }}>Type</th>
                <th style={{ textAlign: "left", borderBottom: "1px solid #ccc" }}>Name</th>
                <th style={{ textAlign: "left", borderBottom: "1px solid #ccc" }}>Reference</th>
              </tr>
            </thead>
            <tbody>
              {graph.nodes.map((n) => (
                <tr key={n.id} id={`prov-node-row-${n.id}`}>
                  <td style={{ padding: "6px 8px 6px 0", verticalAlign: "top" }}>{n.type}</td>
                  <td style={{ padding: "6px 8px 6px 0", verticalAlign: "top" }}>{n.name}</td>
                  <td style={{ padding: "6px 8px 6px 0", verticalAlign: "top", wordBreak: "break-all" }}>
                    {n.referenceId}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      <section aria-labelledby="prov-edges" style={{ marginTop: 24 }}>
        <h3 id="prov-edges">Edges</h3>
        <div style={{ overflowX: "auto" }}>
          <table style={{ borderCollapse: "collapse", width: "100%", fontSize: 14 }}>
            <thead>
              <tr>
                <th style={{ textAlign: "left", borderBottom: "1px solid #ccc" }}>Type</th>
                <th style={{ textAlign: "left", borderBottom: "1px solid #ccc" }}>From</th>
                <th style={{ textAlign: "left", borderBottom: "1px solid #ccc" }}>To</th>
              </tr>
            </thead>
            <tbody>
              {graph.edges.map((e) => (
                <tr key={e.id}>
                  <td style={{ padding: "6px 8px 6px 0", verticalAlign: "top" }}>{e.type}</td>
                  <td style={{ padding: "6px 8px 6px 0", verticalAlign: "top", wordBreak: "break-all" }}>
                    {e.fromNodeId}
                  </td>
                  <td style={{ padding: "6px 8px 6px 0", verticalAlign: "top", wordBreak: "break-all" }}>
                    {e.toNodeId}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </main>
  );
}
