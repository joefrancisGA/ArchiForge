const CANONICAL_UUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

const UUID_HYPHEN_SEGMENT_LENGTHS: readonly number[] = [8, 4, 4, 4, 12];

/**
 * Reject literals leaked into URLs (`undefined` string from template bugs, empty segments).
 * Dynamic segments must fail fast (Not Found) before calling downstream loaders that assume opaque GUIDs.
 */
export function isInvalidDynamicRouteToken(raw: string | undefined | null): boolean {
  if (raw === undefined || raw === null) {
    return true;
  }

  const trimmed = raw.trim();

  if (trimmed.length === 0) {
    return true;
  }

  const lower = trimmed.toLowerCase();

  return lower === "undefined" || lower === "null" || lower === "none";
}

/** True when `raw` matches the canonical RFC 4122 string appearance (hex + hyphen layout). */
export function isCanonicalUuidToken(raw: string): boolean {
  return CANONICAL_UUID_RE.test(raw.trim());
}

/**
 * For routes that accept either **opaque UUIDs** or **slug** ids (e.g. run paths): rejects empty / leaked JS
 * literals, and hyphenated 8-4-4-4-12 **shapes** that are not valid hex UUIDs (truncated paste, typos).
 * Slugs such as `claims-intake-modernization` stay valid (wrong hyphen segment count).
 */
export function isInvalidGuidOrSlugRouteToken(raw: string | undefined | null): boolean {
  if (isInvalidDynamicRouteToken(raw)) {
    return true;
  }

  if (raw === undefined || raw === null) {
    return true;
  }

  const t = raw.trim();

  if (isCanonicalUuidToken(t)) {
    return false;
  }

  if (!t.includes("-")) {
    return false;
  }

  const parts = t.split("-");

  if (parts.length !== UUID_HYPHEN_SEGMENT_LENGTHS.length) {
    return false;
  }

  for (let i = 0; i < UUID_HYPHEN_SEGMENT_LENGTHS.length; i++) {
    if (parts[i]!.length !== UUID_HYPHEN_SEGMENT_LENGTHS[i]!) {
      return false;
    }
  }

  return true;
}
