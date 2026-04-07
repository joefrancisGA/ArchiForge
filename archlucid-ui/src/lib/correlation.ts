/** Header echoed by ArchiForge.Api `CorrelationIdMiddleware` (must match server). */
export const CORRELATION_ID_HEADER = "X-Correlation-ID";

const SAFE_CORRELATION_ID = /^[a-zA-Z0-9\-_.]+$/;

/**
 * Generates a new correlation id safe for the API middleware (alphanumeric, hyphen, underscore, dot; max 64).
 * UUID v4 fits the allowed charset and length.
 */
export function generateCorrelationId(): string {
  if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
    return crypto.randomUUID();
  }

  const bytes = new Uint8Array(16);

  if (typeof crypto !== "undefined" && typeof crypto.getRandomValues === "function") {
    crypto.getRandomValues(bytes);
  } else {
    for (let i = 0; i < bytes.length; i++) {
      bytes[i] = Math.floor(Math.random() * 256);
    }
  }

  bytes[6] = (bytes[6] & 0x0f) | 0x40;
  bytes[8] = (bytes[8] & 0x3f) | 0x80;
  const hex = Array.from(bytes, (b) => b.toString(16).padStart(2, "0")).join("");
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
}

/** True if the value satisfies ArchiForge.Api correlation id validation (reuse inbound browser id when valid). */
export function isSafeCorrelationId(value: string | null | undefined): boolean {
  if (value === null || value === undefined) {
    return false;
  }

  const trimmed = value.trim();

  return trimmed.length > 0 && trimmed.length <= 64 && SAFE_CORRELATION_ID.test(trimmed);
}
