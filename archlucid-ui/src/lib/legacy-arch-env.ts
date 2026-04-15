/**
 * Reads operator UI environment variables (ArchLucid-prefixed first).
 * Server-side API base still accepts `NEXT_PUBLIC_${_legacyEnvPrefix}API_BASE_URL` when `NEXT_PUBLIC_ARCHLUCID_API_BASE_URL` is unset (migration).
 * Other env keys whose names start with `_legacyEnvPrefix` are warned once and ignored for reads here.
 */
const _legacyEnvPrefix = "ARCH" + "IFORGE_";

let legacyUiEnvWarningEmitted = false;

function warnLegacyUiEnvOnce(): void {
  if (legacyUiEnvWarningEmitted || typeof process === "undefined" || !process.env) {
    return;
  }

  const keys = Object.keys(process.env).filter((k) => k.toUpperCase().startsWith(_legacyEnvPrefix));

  if (keys.length === 0) {
    return;
  }

  legacyUiEnvWarningEmitted = true;
  console.warn(
    `[ArchLucid UI] Legacy ${_legacyEnvPrefix}* environment variables are set but ignored. Use ARCHLUCID_* / NEXT_PUBLIC_ARCHLUCID_* only. Keys: ${keys.sort().join(", ")}`,
  );
}

/** API key for server-side UI → API calls (proxy / RSC). */
export function readServerSideApiKey(): string | undefined {
  warnLegacyUiEnvOnce();

  return process.env.ARCHLUCID_API_KEY?.trim() || undefined;
}

/**
 * Auth headers for **server-side** UI → API fetches (RSC `api.ts` direct to backend).
 * Aligns with `buildUpstreamHeaders` in `src/app/api/proxy/[...path]/route.ts` when the browser sends no `Authorization`:
 * bearer from `ARCHLUCID_PROXY_BEARER_TOKEN` wins; otherwise `X-Api-Key` from `readServerSideApiKey`.
 */
export function getServerUpstreamAuthHeaders(): Record<string, string> {
  warnLegacyUiEnvOnce();

  const key = readServerSideApiKey();
  const rawBearer = process.env.ARCHLUCID_PROXY_BEARER_TOKEN?.trim() ?? "";
  const hasBearer = rawBearer.length > 0;
  const headers: Record<string, string> = {};

  if (key && !hasBearer) {
    headers["X-Api-Key"] = key;
  }

  if (hasBearer) {
    headers.Authorization = `Bearer ${rawBearer}`;
  }

  return headers;
}

/** Upstream API base URL (server / build). */
export function readServerApiBaseUrlFromEnv(): string {
  warnLegacyUiEnvOnce();

  const lucid = process.env.ARCHLUCID_API_BASE_URL?.trim();

  if (lucid) {
    return lucid;
  }

  const lucidNp = process.env.NEXT_PUBLIC_ARCHLUCID_API_BASE_URL?.trim();

  if (lucidNp) {
    return lucidNp;
  }

  const legacyNpApiBase = process.env[`NEXT_PUBLIC_${_legacyEnvPrefix}API_BASE_URL`]?.trim();

  if (legacyNpApiBase) {
    return legacyNpApiBase;
  }

  return "http://localhost:5128";
}

/** Browser-visible default API origin (NEXT_PUBLIC_* only; no server-only secrets). */
export function readPublicBrowserApiBaseDefault(): string {
  warnLegacyUiEnvOnce();

  const lucid = process.env.NEXT_PUBLIC_ARCHLUCID_API_BASE_URL?.trim();

  if (lucid) {
    return lucid;
  }

  return "http://localhost:5128";
}

const _NpLucid = "NEXT_PUBLIC_ARCHLUCID_";

/** UI auth mode (must align with API auth configuration). */
export function readNextPublicAuthMode(): string {
  warnLegacyUiEnvOnce();

  const lucid = process.env[`${_NpLucid}AUTH_MODE`]?.trim();

  if (lucid) {
    return lucid;
  }

  return "development-bypass";
}

export function readProxyRateLimitDisabled(): boolean {
  warnLegacyUiEnvOnce();

  const raw = process.env.ARCHLUCID_PROXY_RATE_LIMIT_DISABLED?.trim().toLowerCase();

  return raw === "1" || raw === "true" || raw === "yes";
}

export function readProxyRateLimitPerMinute(): number {
  warnLegacyUiEnvOnce();

  const raw = process.env.ARCHLUCID_PROXY_RATE_LIMIT_PER_MINUTE?.trim();

  if (raw === undefined || raw === "") {
    return 120;
  }

  const n = Number(raw);

  if (!Number.isFinite(n) || n < 1) {
    return 120;
  }

  return Math.floor(n);
}

export function readProxyRateLimitWindowMs(): number {
  warnLegacyUiEnvOnce();

  const raw = process.env.ARCHLUCID_PROXY_RATE_LIMIT_WINDOW_MS?.trim();

  if (raw === undefined || raw === "") {
    return 60_000;
  }

  const n = Number(raw);

  if (!Number.isFinite(n) || n < 1000) {
    return 60_000;
  }

  return Math.floor(n);
}
