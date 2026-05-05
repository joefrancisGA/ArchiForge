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
import { useBasicAdvancedToggle } from "@/hooks/useBasicAdvancedToggle";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { parseProvenanceExplanationPayload } from "@/lib/provenance-explanation-payload";

async function fetchNodeExplanation(runId: string, nodeId: string): Promise<string> {
  const url =
    `/api/proxy/v1/architecture/runs/${encodeURIComponent(runId)}/provenance/${encodeURIComponent(nodeId)}/explanation`;

  try {
    const res = await fetch(url, {
      method: "GET",
      credentials: "include",
      headers: { Accept: "application/problem+json, application/json" },
    });

    const raw: unknown = await res.json();
    const parsed = parseProvenanceExplanationPayload(raw);
    const line =
      parsed.message ?? parsed.detail ?? parsed.title ?? "";

    if (res.status === 501)
      return line.length > 0 ? line : "Not implemented yet.";

    return line.length > 0 ? line : `HTTP ${String(res.status)}`;
  }
  catch {
    return "Could not reach the explanation endpoint.";
  }
}

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
  runId = "",
}: {
  graph: GraphViewModel;
  typeFilter?: string;
  /** When set, enables the reserved explanation fetch for the selected graph node id. */
  runId?: string;
}) {
  const filtered = useMemo(
    () => filterGraphByType(graph, typeFilter),
    [graph, typeFilter],
  );
  const { nodes, edges } = useMemo(() => mapGraphToReactFlow(filtered), [filtered]);
  const [selectedNode, setSelectedNode] = useState<GraphNodeVm | null>(null);

  const [explainStatusLine, setExplainStatusLine] = useState("");

  const { isAdvanced, toggle } = useBasicAdvancedToggle("archlucid_graph_settings_advanced_toggle");
  const [edgeInferenceThreshold, setEdgeInferenceThreshold] = useState("0.75");

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
            onNodeClick={(_, node) => {
              setExplainStatusLine("");
              setSelectedNode((node.data.raw as GraphNodeVm) ?? null);
            }}
          >
            <MiniMap />
            <Controls />
            <Background />
          </ReactFlow>
        </ReactFlowProvider>
      </div>

      <aside className="max-h-[70vh] flex flex-col gap-4 overflow-auto rounded-lg border border-neutral-200 bg-white p-4 dark:border-neutral-700 dark:bg-neutral-950">
        <div>
          <div className="mb-4 flex items-center justify-between">
            <h3 className="m-0">Graph Settings</h3>
            <div className="flex items-center gap-1">
              <Button
                type="button"
                variant={isAdvanced ? "outline" : "default"}
                size="sm"
                className="h-7 px-2 text-xs"
                onClick={() => { if (isAdvanced) toggle(); }}
              >
                Basic
              </Button>
              <Button
                type="button"
                variant={isAdvanced ? "default" : "outline"}
                size="sm"
                className="h-7 px-2 text-xs"
                onClick={() => { if (!isAdvanced) toggle(); }}
              >
                Advanced
              </Button>
            </div>
          </div>

          {isAdvanced && (
            <div className="space-y-2 rounded-md border border-neutral-200 bg-neutral-50 p-3 dark:border-neutral-800 dark:bg-neutral-900/50">
              <Label htmlFor="edge-inference-threshold" className="text-xs">Edge Inference Threshold</Label>
              <Input
                id="edge-inference-threshold"
                type="number"
                step="0.05"
                min="0"
                max="1"
                value={edgeInferenceThreshold}
                onChange={(e) => setEdgeInferenceThreshold(e.target.value)}
                className="h-8 text-sm"
              />
              <p className="text-[11px] text-neutral-500">
                Minimum confidence score required to render inferred edges between nodes.
              </p>
            </div>
          )}
        </div>

        <div className="flex-1">
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
              {runId.trim().length > 0 && selectedNode !== null ? (
                <div className="mt-3 border-t border-neutral-200 pt-3 dark:border-neutral-700">
                  <h4 className="mt-0">Explain this node</h4>
                  <p className="text-[11px] text-neutral-600 dark:text-neutral-400">
                    Reserved LLM summary route (shows API status until the backend implements explanations).
                  </p>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    className="h-8"
                    onClick={async () => {
                      const line = await fetchNodeExplanation(runId.trim(), selectedNode.id);
                      setExplainStatusLine(line);
                    }}
                  >
                    Request explanation
                  </Button>
                  {explainStatusLine ? (
                    <p className="mt-2 text-[11px] text-neutral-600 dark:text-neutral-400" aria-live="polite">
                      {explainStatusLine}
                    </p>
                  ) : null}
                </div>
              ) : null}
            </>
          )}
        </div>
      </aside>
    </div>
  );
}
