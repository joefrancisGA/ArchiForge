import { fetchArchiForgeJson } from "@/lib/api";
import type { GraphViewModel } from "@/types/graph";

export async function getProvenanceGraph(runId: string): Promise<GraphViewModel> {
  return fetchArchiForgeJson<GraphViewModel>(`/api/provenance/runs/${runId}/graph`);
}

export async function getDecisionSubgraph(
  runId: string,
  decisionId: string,
): Promise<GraphViewModel> {
  const key = encodeURIComponent(decisionId);
  return fetchArchiForgeJson<GraphViewModel>(
    `/api/provenance/runs/${runId}/graph/decision/${key}`,
  );
}

export async function getNodeNeighborhood(
  runId: string,
  nodeId: string,
  depth = 1,
): Promise<GraphViewModel> {
  return fetchArchiForgeJson<GraphViewModel>(
    `/api/provenance/runs/${runId}/graph/node/${nodeId}?depth=${depth}`,
  );
}

export async function getArchitectureGraph(runId: string): Promise<GraphViewModel> {
  return fetchArchiForgeJson<GraphViewModel>(`/api/graph/runs/${runId}`);
}
