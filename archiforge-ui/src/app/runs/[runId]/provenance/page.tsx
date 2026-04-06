import Link from "next/link";

import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import { getRunProvenance } from "@/lib/api";

/** Server-rendered provenance graph for an authority-completed run (nodes + edges). */
export default async function RunProvenancePage({
  params,
}: {
  params: Promise<{ runId: string }>;
}) {
  const { runId } = await params;
  let loadFailure: ApiLoadFailureState | null = null;
  let graph: Awaited<ReturnType<typeof getRunProvenance>> | null = null;

  try {
    graph = await getRunProvenance(runId);
  } catch (e) {
    loadFailure = toApiLoadFailure(e);
  }

  if (loadFailure || !graph) {
    const fallback =
      loadFailure?.message ?? "Provenance could not be loaded (run missing, incomplete pipeline, or transport error).";

    return (
      <main>
        <h2>Provenance</h2>
        <OperatorApiProblem
          problem={loadFailure?.problem ?? null}
          fallbackMessage={fallback}
          correlationId={loadFailure?.correlationId ?? null}
        />
        <p style={{ margin: "12px 0 0", fontSize: 14 }}>
          This endpoint requires graph, findings, manifest, and decision trace. Coordinator-only runs return HTTP
          422.
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
        Run <code>{graph.runId}</code> — {graph.nodes.length} nodes, {graph.edges.length} edges.
      </p>
      <p>
        <Link href={`/runs/${runId}`}>← Run detail</Link>
      </p>

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
                <tr key={n.id}>
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
