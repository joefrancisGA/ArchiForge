function rawServerApiBaseUrlFromEnv(): string {
  return (
    process.env.ARCHIFORGE_API_BASE_URL ??
    process.env.NEXT_PUBLIC_ARCHIFORGE_API_BASE_URL ??
    "http://localhost:5128"
  );
}

/**
 * Server-side: direct ArchiForge API URL (RSC + proxy route).
 * Falls back to NEXT_PUBLIC value for local dev convenience.
 * Does not validate URL shape; use {@link resolveUpstreamApiBaseUrlForProxy} before forwarding HTTP.
 */
export function getServerApiBaseUrl(): string {
  return rawServerApiBaseUrlFromEnv();
}

export type UpstreamApiBaseResolution =
  | { ok: true; baseUrl: string }
  | { ok: false; detail: string };

/**
 * Validates the configured upstream API base URL before proxying. Fails closed on malformed URLs
 * so operators get a clear 503 instead of opaque fetch errors.
 */
export function resolveUpstreamApiBaseUrlForProxy(): UpstreamApiBaseResolution {
  const raw = rawServerApiBaseUrlFromEnv().trim();

  if (raw.length === 0) {
    return {
      ok: false,
      detail:
        "ARCHIFORGE_API_BASE_URL (or NEXT_PUBLIC_ARCHIFORGE_API_BASE_URL) is empty. Set it to the C# API origin, e.g. http://localhost:5128",
    };
  }

  try {
    const u = new URL(raw);

    if (u.protocol !== "http:" && u.protocol !== "https:") {
      return {
        ok: false,
        detail: `Upstream API URL must use http: or https: (got ${u.protocol}). Check ARCHIFORGE_API_BASE_URL.`,
      };
    }

    return { ok: true, baseUrl: raw.replace(/\/$/, "") };
  } catch {
    return {
      ok: false,
      detail: `Invalid upstream API URL: ${JSON.stringify(raw)}. Use an absolute URL such as http://localhost:5128.`,
    };
  }
}

/** Documented public default; browser traffic should use `/api/proxy/...` from `api.ts`. */
export const PUBLIC_API_BASE_URL =
  process.env.NEXT_PUBLIC_ARCHIFORGE_API_BASE_URL ?? "http://localhost:5128";
