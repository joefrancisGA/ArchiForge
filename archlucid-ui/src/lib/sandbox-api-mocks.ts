import sandboxFixture from "@/lib/sandbox-mock-data.json";

/**
 * Opt-in static fixtures for local UI work without a live ArchLucid API.
 * Enable with `VITE_USE_SANDBOX_MOCKS=true` (surfaced via `next.config.ts` `env`).
 *
 * Intercepts documented routes plus `/v1/audit/search` (operator audit UI) and authority project runs
 * (same rows as the sandbox coordinator list, in `PagedResponse` shape).
 */
export type SandboxMockFixtureFile = typeof sandboxFixture;

export function isSandboxMocksEnabled(): boolean {
  const raw = process.env.VITE_USE_SANDBOX_MOCKS;

  return raw === "true" || raw === "1";
}

function apiPathname(apiPath: string): string {
  const q = apiPath.indexOf("?");

  if (q === -1) {
    return apiPath.startsWith("/") ? apiPath : `/${apiPath}`;
  }

  const base = apiPath.slice(0, q);

  return base.startsWith("/") ? base : `/${base}`;
}

/**
 * Returns a parsed JSON body for GET when sandbox mocks replace the network call; otherwise `undefined`.
 */
export function trySandboxMockJsonForApiGet(apiPath: string): unknown | undefined {
  if (!isSandboxMocksEnabled()) {
    return undefined;
  }

  const pathname = apiPathname(apiPath);

  if (pathname === "/v1/architecture/runs") {
    return sandboxFixture.architectureRuns;
  }

  if (pathname === "/v1/audit" || pathname === "/v1/audit/search") {
    return sandboxFixture.auditEventsPage;
  }

  const authorityRuns = /^\/v1\/authority\/projects\/([^/]+)\/runs$/.exec(pathname);

  if (authorityRuns !== null) {
    const projectId = authorityRuns[1];
    const page = sandboxFixture.authorityRunsPage;

    return {
      ...page,
      items: page.items.map((row) =>
        row.projectId === projectId ? row : { ...row, projectId },
      ),
    };
  }

  return undefined;
}
