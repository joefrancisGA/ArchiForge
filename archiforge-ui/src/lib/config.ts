/**
 * Server-side: direct ArchiForge API URL (RSC + proxy route).
 * Falls back to NEXT_PUBLIC value for local dev convenience.
 */
export function getServerApiBaseUrl(): string {
  return (
    process.env.ARCHIFORGE_API_BASE_URL ??
    process.env.NEXT_PUBLIC_ARCHIFORGE_API_BASE_URL ??
    "http://localhost:5128"
  );
}

/** Documented public default; browser traffic should use `/api/proxy/...` from `api.ts`. */
export const PUBLIC_API_BASE_URL =
  process.env.NEXT_PUBLIC_ARCHIFORGE_API_BASE_URL ?? "http://localhost:5128";
