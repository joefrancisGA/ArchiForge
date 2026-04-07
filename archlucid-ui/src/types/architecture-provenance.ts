/** Coordinator architecture run linkage graph (GET /v1/architecture/runs/{runId}/provenance). */

export type ArchitectureLinkageNode = {
  id: string;
  type: string;
  referenceId: string;
  name: string;
  metadata?: Record<string, string>;
};

export type ArchitectureLinkageEdge = {
  id: string;
  type: string;
  fromNodeId: string;
  toNodeId: string;
  metadata?: Record<string, string>;
};

export type ArchitectureTraceTimelineEntry = {
  timestampUtc: string;
  kind: string;
  label: string;
  referenceId?: string | null;
  metadata?: Record<string, string>;
};

export type ArchitectureRunProvenanceGraph = {
  runId: string;
  nodes: ArchitectureLinkageNode[];
  edges: ArchitectureLinkageEdge[];
  timeline: ArchitectureTraceTimelineEntry[];
  traceabilityGaps: string[];
};
