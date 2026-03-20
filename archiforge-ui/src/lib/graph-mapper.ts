import type { Edge, Node } from "reactflow";
import type { GraphViewModel } from "@/types/graph";

function pickColor(type: string): string {
  switch (type) {
    case "Decision":
      return "#dbeafe";
    case "Finding":
      return "#fef3c7";
    case "Rule":
      return "#ede9fe";
    case "Artifact":
      return "#dcfce7";
    case "Manifest":
      return "#f3f4f6";
    case "GraphNode":
    case "TopologyResource":
      return "#e0f2fe";
    case "SecurityBaseline":
      return "#fee2e2";
    case "PolicyControl":
      return "#ecfccb";
    case "Requirement":
      return "#fae8ff";
    default:
      return "#f5f5f5";
  }
}

export function mapGraphToReactFlow(graph: GraphViewModel): {
  nodes: Node[];
  edges: Edge[];
} {
  const nodes: Node[] = graph.nodes.map((node, index) => ({
    id: node.id,
    position: {
      x: (index % 5) * 240,
      y: Math.floor(index / 5) * 140,
    },
    data: {
      label: `${node.label}\n(${node.type})`,
      raw: node,
    },
    style: {
      border: "1px solid #999",
      borderRadius: 8,
      padding: 8,
      background: pickColor(node.type),
      width: 180,
      whiteSpace: "pre-wrap",
      fontSize: 12,
    },
    type: "default",
  }));

  const edges: Edge[] = graph.edges.map((edge, index) => ({
    id: `${edge.source}-${edge.target}-${edge.type}-${index}`,
    source: edge.source,
    target: edge.target,
    label: edge.type,
    type: "smoothstep",
  }));

  return { nodes, edges };
}
