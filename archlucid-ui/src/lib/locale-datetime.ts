/** Locale string for an ISO-8601 instant, or em dash when missing / not parseable (avoids “Invalid Date” in UI). */
export function formatInstantForLocale(iso: string | null | undefined): string {
  if (iso === null || iso === undefined) {
    return "—";
  }

  const trimmed = iso.trim();

  if (trimmed.length === 0) {
    return "—";
  }

  const ms = Date.parse(trimmed);

  if (!Number.isFinite(ms)) {
    return "—";
  }

  return new Date(ms).toLocaleString();
}
