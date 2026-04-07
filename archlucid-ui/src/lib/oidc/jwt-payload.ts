/**
 * Decode JWT payload for display only (no signature verification — server validates on API calls).
 */
export function decodeJwtPayload(jwt: string): Record<string, unknown> | null {
  const parts = jwt.split(".");

  if (parts.length < 2) {
    return null;
  }

  const payloadSegment = parts[1];

  if (!payloadSegment) {
    return null;
  }

  try {
    const padded = payloadSegment.replace(/-/gu, "+").replace(/_/gu, "/");
    const padLen = (4 - (padded.length % 4)) % 4;
    const base64 = padded + "=".repeat(padLen);
    const json = atob(base64);

    return JSON.parse(json) as Record<string, unknown>;
  } catch {
    return null;
  }
}

export function readNonceFromPayload(payload: Record<string, unknown> | null): string | null {
  if (!payload) {
    return null;
  }

  const nonce = payload.nonce;

  return typeof nonce === "string" && nonce.trim().length > 0 ? nonce.trim() : null;
}

export function pickDisplayNameFromPayload(payload: Record<string, unknown> | null): string | null {
  if (!payload) {
    return null;
  }

  const preferred = payload.preferred_username;
  const name = payload.name;
  const upn = payload.upn;
  const email = payload.email;
  const sub = payload.sub;

  for (const candidate of [preferred, name, upn, email, sub]) {
    if (typeof candidate === "string" && candidate.trim().length > 0) {
      return candidate.trim();
    }
  }

  return null;
}
