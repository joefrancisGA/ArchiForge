"use client";

import { useMemo, useState } from "react";
import { GraphViewer } from "@/components/GraphViewer";
import {
  getArchitectureGraph,
  getDecisionSubgraph,
  getNodeNeighborhood,
  getProvenanceGraph,
} from "@/lib/graph-api";
import type { GraphViewModel } from "@/types/graph";

type GraphMode =
  | "provenance-full"
  | "decision-subgraph"
  | "node-neighborhood"
  | "architecture";

export default function GraphPage() {
  const [runId, setRunId] = useState("");
  const [decisionId, setDecisionId] = useState("");
  const [nodeId, setNodeId] = useState("");
  const [depth, setDepth] = useState(1);
  const [mode, setMode] = useState<GraphMode>("provenance-full");
  const [graph, setGraph] = useState<GraphViewModel | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [typeFilter, setTypeFilter] = useState("");

  const nodeTypes = useMemo(() => {
    if (!graph) return [];
    const set = new Set(graph.nodes.map((n) => n.type));
    return [...set].sort((a, b) => a.localeCompare(b));
  }, [graph]);

  async function loadGraph() {
    setLoading(true);
    setError(null);

    try {
      let result: GraphViewModel;

      switch (mode) {
        case "provenance-full":
          result = await getProvenanceGraph(runId);
          break;
        case "decision-subgraph":
          result = await getDecisionSubgraph(runId, decisionId);
          break;
        case "node-neighborhood":
          result = await getNodeNeighborhood(runId, nodeId, depth);
          break;
        case "architecture":
          result = await getArchitectureGraph(runId);
          break;
        default:
          throw new Error("Unsupported graph mode.");
      }

      setGraph(result);
      setTypeFilter("");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load graph.");
      setGraph(null);
    } finally {
      setLoading(false);
    }
  }

  return (
    <main>
      <h2>Graph viewer</h2>
      <p style={{ maxWidth: 720, color: "#444" }}>
        Explore provenance (decisions, findings, rules) or the architecture knowledge graph for a
        run. Use the scope headers configured for this UI (same as Runs).
      </p>

      <div style={{ display: "grid", gap: 12, maxWidth: 900, marginBottom: 24 }}>
        <input
          value={runId}
          onChange={(e) => setRunId(e.target.value)}
          placeholder="Run ID (GUID)"
          style={{ padding: 8 }}
        />

        <select
          value={mode}
          onChange={(e) => setMode(e.target.value as GraphMode)}
          style={{ padding: 8 }}
        >
          <option value="provenance-full">Full provenance graph</option>
          <option value="decision-subgraph">Decision subgraph</option>
          <option value="node-neighborhood">Node neighborhood</option>
          <option value="architecture">Architecture graph</option>
        </select>

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

      {error && <p style={{ color: "red" }}>{error}</p>}

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
