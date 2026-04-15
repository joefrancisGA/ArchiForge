/** Progressive disclosure tier for operator shell navigation (sidebar + mobile drawer). */
export type NavTier = "essential" | "extended" | "advanced";

/**
 * Returns links visible for the current disclosure flags. Extended requires essential; advanced requires extended.
 */
export function filterNavLinksByTier<T extends { tier: NavTier }>(
  links: ReadonlyArray<T>,
  showExtended: boolean,
  showAdvanced: boolean,
): T[] {
  return links.filter((link) => {
    if (link.tier === "essential") {
      return true;
    }

    if (link.tier === "extended") {
      return showExtended;
    }

    return showAdvanced;
  });
}
