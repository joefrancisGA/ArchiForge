/**
 * Locale string for an ISO-8601 instant, or em dash when missing / not parseable (avoids “Invalid Date” in UI).
 * Uses fixed `en-US` + `UTC` so server and client render the same text (hydration-safe for client components).
 */
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

  return (
    new Date(ms).toLocaleString("en-US", {
      timeZone: "UTC",
      year: "numeric",
      month: "short",
      day: "numeric",
      hour: "numeric",
      minute: "2-digit",
      hour12: true,
    }) + " UTC"
  );
}

/**
 * Conversation list rows: date-only in UTC without a trailing “UTC” timezone label (reads cleaner than
 * long timestamps for saved threads while staying SSR/hydration-safe).
 */
export function formatConversationListDate(iso: string | null | undefined): string {
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

  return new Date(ms).toLocaleDateString("en-US", {
    timeZone: "UTC",
    year: "numeric",
    month: "short",
    day: "numeric",
  });
}
