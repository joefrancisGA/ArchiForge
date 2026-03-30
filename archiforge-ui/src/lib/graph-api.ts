import { fetchArchiForgeJson } from "@/lib/api";
import type { GraphViewModel } from "@/types/graph";

/** Fetches the full provenance graph for a run (all decisions, findings, rules, artifacts). */
export async function getProvenanceGraph(runId: string): Promise<GraphViewModel> {
  return fetchArchiForgeJson<GraphViewModel>(`/api/provenance/runs/${runId}/graph`);
}

/** Fetches the subgraph centered on a specific decision node. */
export async function getDecisionSubgraph(
  runId: string,
  decisionId: string,
): Promise<GraphViewModel> {
  const key = encodeURIComponent(decisionId);
  return fetchArchiForgeJson<GraphViewModel>(
    `/api/provenance/runs/${runId}/graph/decision/${key}`,
  );
}

/** Fetches a neighborhood subgraph around a specific node, up to the given depth. */
export async function getNodeNeighborhood(
  runId: string,
  nodeId: string,
  depth = 1,
): Promise<GraphViewModel> {
  return fetchArchiForgeJson<GraphViewModel>(
    `/api/provenance/runs/${runId}/graph/node/${nodeId}?depth=${depth}`,
  );
}

/** Fetches the architecture-level graph for a run (topology resources, baselines, controls). */
export async function getArchitectureGraph(runId: string): Promise<GraphViewModel> {
  return fetchArchiForgeJson<GraphViewModel>(`/api/graph/runs/${runId}`);
}
