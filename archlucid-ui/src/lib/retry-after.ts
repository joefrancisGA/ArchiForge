/**
 * Parses HTTP `Retry-After` (RFC 9110): delay-seconds or HTTP-date.
 * Returns whole seconds until retry is allowed, or null when header is absent / not parseable.
 */
export function parseRetryAfterHeader(headerValue: string | null): number | null {
  if (headerValue === null) {
    return null;
  }

  const trimmed = headerValue.trim();

  if (trimmed.length === 0) {
    return null;
  }

  // Not a valid delay-seconds token; avoid `Date.parse` accepting odd numeric shapes as dates.
  if (/^\d+\.\d+$/.test(trimmed)) {
    return null;
  }

  // Delay-seconds: `1*DIGIT` only (RFC 9110); reject decimals so they are not misread as HTTP-dates.
  if (/^\d+$/.test(trimmed)) {
    const asInt = Number.parseInt(trimmed, 10);

    if (!Number.isNaN(asInt) && asInt >= 0) {
      return asInt;
    }

    return null;
  }

  const asDate = Date.parse(trimmed);

  if (Number.isNaN(asDate)) {
    return null;
  }

  const deltaSec = Math.ceil((asDate - Date.now()) / 1000);

  return deltaSec > 0 ? deltaSec : 0;
}
