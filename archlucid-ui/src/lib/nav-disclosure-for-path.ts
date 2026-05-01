/**
 * On first-run surfaces (e.g. new architecture request), show only essential-tier nav links so the sidebar
 * matches polished home expectations — without mutating the user's saved disclosure toggles.
 */
export function effectiveNavDisclosureForPathname(
  pathname: string | null,
  showExtended: boolean,
  showAdvanced: boolean,
): { showExtended: boolean; showAdvanced: boolean } {
  const normalized = pathname ?? "";

  if (normalized === "/reviews/new") {
    return { showExtended: false, showAdvanced: false };
  }

  return { showExtended, showAdvanced };
}
