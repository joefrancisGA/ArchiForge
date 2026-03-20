"use client";

import { useMemo, useState } from "react";
import ReactFlow, {
  Background,
  Controls,
  MiniMap,
  ReactFlowProvider,
  type Edge,
  type Node,
} from "reactflow";
import "reactflow/dist/style.css";
import type { GraphNodeVm, GraphViewModel } from "@/types/graph";
import { mapGraphToReactFlow } from "@/lib/graph-mapper";

function filterGraphByType(
  graph: GraphViewModel,
  typeFilter: string,
): GraphViewModel {
  if (!typeFilter) return graph;
  const nodes = graph.nodes.filter((n) => n.type === typeFilter);
  const ids = new Set(nodes.map((n) => n.id));
  const edges = graph.edges.filter((e) => ids.has(e.source) && ids.has(e.target));
  return { nodes, edges };
}

export function GraphViewer({
  graph,
  typeFilter = "",
}: {
  graph: GraphViewModel;
  typeFilter?: string;
}) {
  const filtered = useMemo(
    () => filterGraphByType(graph, typeFilter),
    [graph, typeFilter],
  );
  const { nodes, edges } = useMemo(() => mapGraphToReactFlow(filtered), [filtered]);
  const [selectedNode, setSelectedNode] = useState<GraphNodeVm | null>(null);

  if (filtered.nodes.length === 0) {
    return (
      <p style={{ color: "#666" }}>
        {typeFilter
          ? `No nodes match type "${typeFilter}". Clear the filter or reload.`
          : "No nodes in this graph."}
      </p>
    );
  }

  return (
    <div style={{ display: "grid", gridTemplateColumns: "1fr 320px", gap: 16 }}>
      <div
        style={{
          height: "70vh",
          width: "100%",
          border: "1px solid #ddd",
          background: "#fff",
        }}
      >
        <ReactFlowProvider>
          <ReactFlow
            nodes={nodes as Node[]}
            edges={edges as Edge[]}
            fitView
            onNodeClick={(_, node) =>
              setSelectedNode((node.data.raw as GraphNodeVm) ?? null)
            }
          >
            <MiniMap />
            <Controls />
            <Background />
          </ReactFlow>
        </ReactFlowProvider>
      </div>

      <aside
        style={{
          border: "1px solid #ddd",
          borderRadius: 8,
          padding: 16,
          background: "#fff",
          overflow: "auto",
          maxHeight: "70vh",
        }}
      >
        <h3 style={{ marginTop: 0 }}>Node detail</h3>

        {!selectedNode && <p>Select a node to inspect it.</p>}

        {selectedNode && (
          <>
            <p>
              <strong>ID:</strong> {selectedNode.id}
            </p>
            <p>
              <strong>Label:</strong> {selectedNode.label}
            </p>
            <p>
              <strong>Type:</strong> {selectedNode.type}
            </p>

            <h4>Metadata</h4>
            {selectedNode.metadata && Object.keys(selectedNode.metadata).length > 0 ? (
              <ul>
                {Object.entries(selectedNode.metadata).map(([key, value]) => (
                  <li key={key}>
                    <strong>{key}:</strong> {value}
                  </li>
                ))}
              </ul>
            ) : (
              <p>No metadata available.</p>
            )}
          </>
        )}
      </aside>
    </div>
  );
}
