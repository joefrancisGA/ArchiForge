import { describe, expect, it } from "vitest";

import type { GraphViewModel } from "@/types/graph";

import { mapGraphToReactFlow } from "./graph-mapper";

describe("mapGraphToReactFlow", () => {
  it("returns empty arrays for an empty graph", () => {
    const empty: GraphViewModel = { nodes: [], edges: [] };
    const result = mapGraphToReactFlow(empty);

    expect(result.nodes).toEqual([]);
    expect(result.edges).toEqual([]);
  });

  it("maps nodes to stable ids, grid positions, and combined labels", () => {
    const graph: GraphViewModel = {
      nodes: [
        { id: "a", label: "Alpha", type: "Decision" },
        { id: "b", label: "Beta", type: "Finding" },
      ],
      edges: [],
    };

    const { nodes } = mapGraphToReactFlow(graph);

    expect(nodes).toHaveLength(2);
    expect(nodes[0].id).toBe("a");
    expect(nodes[0].position).toEqual({ x: 0, y: 0 });
    expect(nodes[0].data).toMatchObject({
      label: "Alpha\n(Decision)",
    });
    expect(nodes[0].data.raw).toEqual(graph.nodes[0]);

    expect(nodes[1].id).toBe("b");
    expect(nodes[1].position).toEqual({ x: 240, y: 0 });
    expect(nodes[1].data).toMatchObject({
      label: "Beta\n(Finding)",
    });
    expect(nodes[1].data.raw).toEqual(graph.nodes[1]);
  });

  it("maps edges with deterministic ids and smoothstep type", () => {
    const graph: GraphViewModel = {
      nodes: [
        { id: "s", label: "S", type: "GraphNode" },
        { id: "t", label: "T", type: "GraphNode" },
      ],
      edges: [{ source: "s", target: "t", type: "dependsOn" }],
    };

    const { edges } = mapGraphToReactFlow(graph);

    expect(edges).toHaveLength(1);
    expect(edges[0]).toMatchObject({
      id: "s-t-dependsOn-0",
      source: "s",
      target: "t",
      label: "dependsOn",
      type: "smoothstep",
    });
  });
});
