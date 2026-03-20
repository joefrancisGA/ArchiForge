export type GraphNodeVm = {
  id: string;
  label: string;
  type: string;
  metadata?: Record<string, string>;
};

export type GraphEdgeVm = {
  source: string;
  target: string;
  type: string;
};

export type GraphViewModel = {
  nodes: GraphNodeVm[];
  edges: GraphEdgeVm[];
};
