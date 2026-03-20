import { getServerApiBaseUrl } from "@/lib/config";
import type {
  ArtifactDescriptor,
  ManifestSummary,
  ReplayResponse,
  RunComparison,
  RunDetail,
  RunSummary,
} from "@/types/authority";

function isBrowser(): boolean {
  return typeof window !== "undefined";
}

/**
 * Resolve request URL and headers.
 * - Server (RSC): direct backend + optional ARCHIFORGE_API_KEY.
 * - Browser: same-origin `/api/proxy` (adds X-Api-Key on the server).
 */
function resolveRequest(path: string): { url: string; headers: HeadersInit } {
  if (isBrowser()) {
    const url = `/api/proxy${path.startsWith("/") ? path : `/${path}`}`;
    return { url, headers: {} };
  }

  const base = getServerApiBaseUrl().replace(/\/$/, "");
  const url = `${base}${path.startsWith("/") ? path : `/${path}`}`;
  const headers: Record<string, string> = {};
  const key = process.env.ARCHIFORGE_API_KEY;
  if (key) headers["X-Api-Key"] = key;
  return { url, headers };
}

async function apiGet<T>(path: string): Promise<T> {
  const { url, headers } = resolveRequest(path);
  const response = await fetch(url, {
    cache: "no-store",
    headers,
  });

  if (!response.ok) {
    throw new Error(`Request failed: ${response.status} ${response.statusText}`);
  }

  return response.json() as Promise<T>;
}

export async function listRunsByProject(projectId: string, take = 20): Promise<RunSummary[]> {
  return apiGet<RunSummary[]>(
    `/api/authority/projects/${encodeURIComponent(projectId)}/runs?take=${take}`,
  );
}

export async function getRunSummary(runId: string): Promise<RunSummary> {
  return apiGet<RunSummary>(`/api/authority/runs/${runId}/summary`);
}

export async function getRunDetail(runId: string): Promise<RunDetail> {
  return apiGet<RunDetail>(`/api/authority/runs/${runId}`);
}

export async function getManifestSummary(manifestId: string): Promise<ManifestSummary> {
  return apiGet<ManifestSummary>(`/api/authority/manifests/${manifestId}/summary`);
}

export async function listArtifacts(manifestId: string): Promise<ArtifactDescriptor[]> {
  return apiGet<ArtifactDescriptor[]>(`/api/artifacts/manifests/${manifestId}`);
}

export async function compareRuns(leftRunId: string, rightRunId: string): Promise<RunComparison> {
  return apiGet<RunComparison>(
    `/api/authority/compare/runs?leftRunId=${encodeURIComponent(leftRunId)}&rightRunId=${encodeURIComponent(rightRunId)}`,
  );
}

export async function replayRun(runId: string, mode: string): Promise<ReplayResponse> {
  const { url, headers } = resolveRequest("/api/authority/replay");
  const h = new Headers(headers);
  h.set("Content-Type", "application/json");
  const response = await fetch(url, {
    method: "POST",
    headers: h,
    cache: "no-store",
    body: JSON.stringify({ runId, mode }),
  });

  if (!response.ok) {
    throw new Error(`Replay failed: ${response.status} ${response.statusText}`);
  }

  return response.json() as Promise<ReplayResponse>;
}

/** Use same-origin proxy so browser downloads work with API key auth. */
export function getArtifactDownloadUrl(manifestId: string, artifactId: string): string {
  return `/api/proxy/api/artifacts/manifests/${manifestId}/artifact/${artifactId}`;
}

export function getBundleDownloadUrl(manifestId: string): string {
  return `/api/proxy/api/artifacts/manifests/${manifestId}/bundle`;
}

export function getRunExportDownloadUrl(runId: string): string {
  return `/api/proxy/api/artifacts/runs/${runId}/export`;
}
