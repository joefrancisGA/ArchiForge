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

/** Paginated architecture graph (GET /v1/graph/runs/{runId}/nodes). */
export type GraphNodesPageResponse = {
  page: number;
  pageSize: number;
  totalNodes: number;
  hasMore: boolean;
  nodes: GraphNodeVm[];
  edges: GraphEdgeVm[];
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
