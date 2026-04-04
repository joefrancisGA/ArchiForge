const PROXY_PREFIX = "/api/proxy";

/**
 * Returns the backend-relative path for a same-origin proxy request (e.g. `/v1/authority/runs/x`),
 * or null if the URL is not under `/api/proxy`.
 */
export function backendApiPath(url: URL): string | null {
  if (!url.pathname.startsWith(`${PROXY_PREFIX}/`) && url.pathname !== PROXY_PREFIX) {
    return null;
  }

  const rest = url.pathname.slice(PROXY_PREFIX.length);

  return rest.startsWith("/") ? rest : `/${rest}`;
}

export function matchesRunDetailGet(url: URL, runId: string): boolean {
  return (
    url.search === "" &&
    backendApiPath(url) === `/v1/authority/runs/${encodeURIComponent(runId)}`
  );
}

export function matchesManifestSummaryGet(url: URL, manifestId: string): boolean {
  return (
    url.search === "" &&
    backendApiPath(url) === `/v1/authority/manifests/${encodeURIComponent(manifestId)}/summary`
  );
}

export function matchesArtifactListGet(url: URL, manifestId: string): boolean {
  return (
    url.search === "" &&
    backendApiPath(url) === `/v1/artifacts/manifests/${encodeURIComponent(manifestId)}`
  );
}

export function matchesArtifactBundleGet(url: URL, manifestId: string): boolean {
  return (
    url.search === "" &&
    backendApiPath(url) === `/v1/artifacts/manifests/${encodeURIComponent(manifestId)}/bundle`
  );
}

export function matchesLegacyCompareRunsGet(url: URL, leftRunId: string, rightRunId: string): boolean {
  if (backendApiPath(url) !== "/v1/authority/compare/runs") {
    return false;
  }

  return (
    url.searchParams.get("leftRunId") === leftRunId && url.searchParams.get("rightRunId") === rightRunId
  );
}

export function matchesStructuredCompareGet(url: URL, baseRunId: string, targetRunId: string): boolean {
  if (backendApiPath(url) !== "/v1/compare") {
    return false;
  }

  return (
    url.searchParams.get("baseRunId") === baseRunId && url.searchParams.get("targetRunId") === targetRunId
  );
}

export function matchesCompareExplainGet(url: URL, baseRunId: string, targetRunId: string): boolean {
  if (backendApiPath(url) !== "/v1/explain/compare/explain") {
    return false;
  }

  return (
    url.searchParams.get("baseRunId") === baseRunId && url.searchParams.get("targetRunId") === targetRunId
  );
}
