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
import { OperatorEmptyState } from "@/components/OperatorShellMessage";

/** Filters a graph to only include nodes of the given type and edges between those nodes. */
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

/**
 * Interactive graph viewer wrapping React Flow. Supports node type filtering
 * and a side panel for inspecting the selected node's metadata.
 */
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
    if (typeFilter) {
      return (
        <OperatorEmptyState title="No nodes match this filter">
          <p className="m-0">
            No nodes match type &quot;{typeFilter}&quot;. Clear the type filter or reload the graph
            with different scope.
          </p>
        </OperatorEmptyState>
      );
    }

    return (
      <OperatorEmptyState title="No graph data to display">
        <p className="m-0">
          The API returned a graph with no nodes (valid empty result, not a filter).
        </p>
      </OperatorEmptyState>
    );
  }

  return (
    <div className="grid grid-cols-[1fr_320px] gap-4">
      <div className="h-[70vh] w-full border border-neutral-200 bg-white dark:border-neutral-700 dark:bg-neutral-950">
        <ReactFlowProvider>
          <ReactFlow
            nodes={nodes as Node[]}
            edges={edges as Edge[]}
            fitView
            onlyRenderVisibleElements
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

      <aside className="max-h-[70vh] overflow-auto rounded-lg border border-neutral-200 bg-white p-4 dark:border-neutral-700 dark:bg-neutral-950">
        <h3 className="mt-0">Node detail</h3>

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
