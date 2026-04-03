/** Formats an ISO-8601 instant for operator-facing UTC labels (locale-aware clock, fixed UTC zone). */
export function formatIsoUtcForDisplay(iso: string): string {
  try {
    const d = new Date(iso);

    if (Number.isNaN(d.getTime())) {
      return iso;
    }

    return `${d.toLocaleString(undefined, { timeZone: "UTC" })} UTC`;
  } catch {
    return iso;
  }
}
