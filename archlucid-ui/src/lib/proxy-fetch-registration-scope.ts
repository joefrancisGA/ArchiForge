import { getEffectiveBrowserProxyScopeHeaders } from "@/lib/operator-scope-storage";

/**
 * Merges effective tenant/workspace/project scope headers for same-origin `/api/proxy/*` fetches: operator choice
 * (localStorage) → post-registration session (unsigned) → dev defaults. Keeps the proxy aligned with
 * `resolveRequest` in `api.ts`.
 */
export function mergeRegistrationScopeForProxy(input?: RequestInit): RequestInit {
  const headers = new Headers(input?.headers);
  for (const [key, value] of Object.entries(getEffectiveBrowserProxyScopeHeaders())) {
    headers.set(key, value);
  }

  return { ...input, headers };
}
