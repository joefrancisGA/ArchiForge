/** A node in the provenance or architecture graph (id, label, type, optional metadata). */
export type GraphNodeVm = {
  id: string;
  label: string;
  type: string;
  metadata?: Record<string, string>;
};

/** A directed edge in the graph (source → target with a relationship type). */
export type GraphEdgeVm = {
  source: string;
  target: string;
  type: string;
};

/** Complete graph view model returned by provenance and architecture graph endpoints. */
export type GraphViewModel = {
  nodes: GraphNodeVm[];
  edges: GraphEdgeVm[];
  /** API 55R+: graph endpoints include counts for empty-state UX. */
  nodeCount?: number;
  edgeCount?: number;
  isEmpty?: boolean;
};
