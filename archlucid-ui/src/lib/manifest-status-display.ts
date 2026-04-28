/** Maps authority manifest status strings to operator-facing labels (`Committed` is API-internal). */
export function manifestStatusForDisplay(status: string | undefined | null): string {
  const t = (status ?? "").trim();

  if (/^committed$/i.test(t)) {
    return "Finalized";
  }

  if (t.length > 0) {
    return t;
  }

  return "—";
}
