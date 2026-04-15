"use client";

import dynamic from "next/dynamic";
import Link from "next/link";
import { useMemo, useState } from "react";

import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import {
  OperatorEmptyState,
  OperatorLoadingNotice,
  OperatorMalformedCallout,
  OperatorTryNext,
} from "@/components/OperatorShellMessage";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";

const GraphViewer = dynamic(
  () => import("@/components/GraphViewer").then((m) => m.GraphViewer),
  {
    ssr: false,
    loading: () => (
      <OperatorLoadingNotice>
        <strong>Loading graph viewer.</strong>
        <p style={{ margin: "8px 0 0", fontSize: 14 }}>Preparing the interactive canvas (client-only bundle)…</p>
      </OperatorLoadingNotice>
    ),
  },
);
import { coerceGraphViewModel } from "@/lib/operator-response-guards";
import {
  getArchitectureGraph,
  getDecisionSubgraph,
  getNodeNeighborhood,
  getProvenanceGraph,
} from "@/lib/graph-api";
import type { GraphViewModel } from "@/types/graph";

/** Graph visualization mode: which endpoint to query and what graph subset to display. */
type GraphMode =
  | "provenance-full"
  | "decision-subgraph"
  | "node-neighborhood"
  | "architecture";

/** Interactive graph viewer page. Operator picks a run, graph mode, and optional filters. */
export default function GraphPage() {
  const [runId, setRunId] = useState("");
  const [decisionId, setDecisionId] = useState("");
  const [nodeId, setNodeId] = useState("");
  const [depth, setDepth] = useState(1);
  const [mode, setMode] = useState<GraphMode>("provenance-full");
  const [graph, setGraph] = useState<GraphViewModel | null>(null);
  const [loadFailure, setLoadFailure] = useState<ApiLoadFailureState | null>(null);
  const [malformedMessage, setMalformedMessage] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [typeFilter, setTypeFilter] = useState("");

  const nodeTypes = useMemo(() => {
    if (!graph) return [];
    const set = new Set(graph.nodes.map((n) => n.type));
    return [...set].sort((a, b) => a.localeCompare(b));
  }, [graph]);

  /** Fetches the graph from the API based on the selected mode, then validates via coerce. */
  async function loadGraph() {
    setLoading(true);
    setLoadFailure(null);
    setMalformedMessage(null);

    try {
      let raw: unknown;

      switch (mode) {
        case "provenance-full":
          raw = await getProvenanceGraph(runId);
          break;
        case "decision-subgraph":
          raw = await getDecisionSubgraph(runId, decisionId);
          break;
        case "node-neighborhood":
          raw = await getNodeNeighborhood(runId, nodeId, depth);
          break;
        case "architecture":
          raw = await getArchitectureGraph(runId);
          break;
        default:
          throw new Error("Unsupported graph mode.");
      }

      const coerced = coerceGraphViewModel(raw);

      if (!coerced.ok) {
        setGraph(null);
        setMalformedMessage(coerced.message);

        return;
      }

      setGraph(coerced.value);
      setTypeFilter("");
    } catch (err) {
      setLoadFailure(toApiLoadFailure(err));
      setGraph(null);
    } finally {
      setLoading(false);
    }
  }

  return (
    <main>
      <h2>Graph</h2>
      <p style={{ marginTop: 4, fontSize: 14 }}>
        <Link href="/">Home</Link>
        {" · "}
        <Link href="/runs?projectId=default">Runs</Link>
        {" · "}
        <Link href="/compare">Compare two runs</Link>
      </p>
      <p style={{ maxWidth: 720, color: "#334155", lineHeight: 1.55 }}>
        Load provenance (decisions, findings, rules) or the architecture graph for a run. Copy the run ID from
        the Runs list or run detail, choose a view, then load.
      </p>

      <div style={{ display: "grid", gap: 12, maxWidth: 900, marginBottom: 24 }}>
        <input
          value={runId}
          onChange={(e) => setRunId(e.target.value)}
          placeholder="Run ID (GUID)"
          style={{ padding: 8 }}
        />

        <div>
          <label htmlFor="graph-mode-select" style={{ display: "block", marginBottom: 6, fontSize: 13, fontWeight: 600 }}>
            Graph mode
          </label>
          <select
            id="graph-mode-select"
            value={mode}
            onChange={(e) => setMode(e.target.value as GraphMode)}
            style={{ padding: 8, width: "100%", maxWidth: 420 }}
          >
            <option value="provenance-full">Full provenance graph</option>
            <option value="decision-subgraph">Decision subgraph</option>
            <option value="node-neighborhood">Node neighborhood</option>
            <option value="architecture">Architecture graph</option>
          </select>
        </div>

        {mode === "decision-subgraph" && (
          <input
            value={decisionId}
            onChange={(e) => setDecisionId(e.target.value)}
            placeholder="Decision ID (node GUID or reference id)"
            style={{ padding: 8 }}
          />
        )}

        {mode === "node-neighborhood" && (
          <>
            <input
              value={nodeId}
              onChange={(e) => setNodeId(e.target.value)}
              placeholder="Provenance node ID (GUID)"
              style={{ padding: 8 }}
            />
            <label style={{ display: "flex", alignItems: "center", gap: 8 }}>
              Depth
              <input
                type="number"
                min={0}
                max={10}
                value={depth}
                onChange={(e) => setDepth(Number(e.target.value))}
                style={{ padding: 8, width: 80 }}
              />
            </label>
          </>
        )}

        <button
          type="button"
          onClick={() => void loadGraph()}
          disabled={
            loading ||
            !runId ||
            (mode === "decision-subgraph" && !decisionId) ||
            (mode === "node-neighborhood" && !nodeId)
          }
          style={{ padding: "10px 16px", cursor: loading ? "wait" : "pointer" }}
        >
          {loading ? "Loading…" : "Load graph"}
        </button>
      </div>

      {loading && (
        <OperatorLoadingNotice>
          <strong>Loading graph.</strong>
          <p style={{ margin: "8px 0 0", fontSize: 14 }}>
            Requesting the selected view from the API; this may take a few seconds on large runs.
          </p>
        </OperatorLoadingNotice>
      )}

      {loadFailure !== null && (
        <>
          <OperatorApiProblem
            problem={loadFailure.problem}
            fallbackMessage={loadFailure.message}
            correlationId={loadFailure.correlationId}
          />
          <OperatorTryNext>
            This is usually a network, proxy, or HTTP error from the graph endpoint—not a malformed JSON body.
            Confirm the run ID exists in <Link href="/runs?projectId=default">Runs</Link>, retry{" "}
            <strong>Load graph</strong>, and check the browser network tab for the failing{" "}
            <code>/v1/…/graph</code> call.
          </OperatorTryNext>
        </>
      )}

      {malformedMessage && (
        <>
          <OperatorMalformedCallout>
            <strong>Unexpected graph response shape.</strong>
            <p style={{ margin: "8px 0 0" }}>{malformedMessage}</p>
            <p style={{ margin: "8px 0 0", fontSize: 14 }}>
              The call succeeded but the payload did not match the expected GraphViewModel (nodes and
              edges arrays). Check API version alignment.
            </p>
          </OperatorMalformedCallout>
          <OperatorTryNext>
            Compare <code>GET /version</code> on the API with your UI deployment. Try another run from{" "}
            <Link href="/runs?projectId=default">Runs</Link> in case this run has legacy or partial graph storage.
          </OperatorTryNext>
        </>
      )}

      {!graph && !loading && loadFailure === null && !malformedMessage && (
        <OperatorEmptyState title="No graph loaded yet">
          <p style={{ margin: 0 }}>
            Enter a run ID from <Link href="/runs?projectId=default">Runs</Link> (or run detail), choose a graph mode,
            then use <strong>Load graph</strong>. An empty node list after a successful load is shown in the viewer
            below—this callout only covers the idle form.
          </p>
        </OperatorEmptyState>
      )}

      {graph && (
        <>
          <div style={{ marginBottom: 12, display: "flex", alignItems: "center", gap: 12 }}>
            <label>
              Filter by type{" "}
              <select
                value={typeFilter}
                onChange={(e) => setTypeFilter(e.target.value)}
                style={{ marginLeft: 8, padding: 6 }}
              >
                <option value="">All types</option>
                {nodeTypes.map((t) => (
                  <option key={t} value={t}>
                    {t}
                  </option>
                ))}
              </select>
            </label>
            <span style={{ color: "#666", fontSize: 14 }}>
              {graph.nodes.length} nodes, {graph.edges.length} edges (before filter)
            </span>
          </div>
          <GraphViewer graph={graph} typeFilter={typeFilter} />
        </>
      )}
    </main>
  );
}
