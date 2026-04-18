import type { NavGroupConfig, NavLinkItem } from "@/lib/nav-config";
import { filterNavLinksByAuthority } from "@/lib/nav-authority";
import { filterNavLinksByTier } from "@/lib/nav-tier";

/** One nav group after **tier → authority** filtering, only emitted when at least one link remains. */
export type NavGroupWithVisibleLinks = {
  group: NavGroupConfig;
  visibleLinks: NavLinkItem[];
};

/**
 * Single composition point for operator shell navigation (sidebar, mobile drawer, command palette).
 *
 * **Packaging alignment (see docs/PRODUCT_PACKAGING.md):** within each `NAV_GROUPS` block from `nav-config.ts`,
 * **tier** (`nav-tier.ts`) implements **progressive disclosure** (Core Pilot visible first; Advanced Analysis after
 * “Show more”; deeper Enterprise after extended/advanced toggles). **Authority** (`nav-authority.ts`) then filters
 * links by the caller’s resolved policy rank so Advanced / Enterprise destinations match **API reality**, not a
 * second authZ engine.
 *
 * Composition order is deliberate: **tier → authority**. Pass **`useNavCallerAuthorityRank()`** (or
 * `CurrentPrincipal.authorityRank`) so filtering matches `OperatorNavAuthorityProvider`. Call sites should skip
 * rendering a group when this returns an empty array to avoid empty headings.
 *
 * **Not authorization:** visible links do not guarantee successful HTTP calls — **ArchLucid.Api** policies return 401/403.
 * **Packaging:** **docs/PRODUCT_PACKAGING.md** §3 (*Code seams* + *Contributor drift guard*).
 *
 * @see `authority-seam-regression.test.ts` — tier + authority composition vs caller rank (includes Core Pilot invariants).
 * @see `nav-shell-visibility.test.ts` — empty-group omission after tier then authority.
 */
export function filterNavLinksForOperatorShell(
  links: ReadonlyArray<NavLinkItem>,
  showExtended: boolean,
  showAdvanced: boolean,
  callerAuthorityRank: number,
): NavLinkItem[] {
  return filterNavLinksByAuthority(
    filterNavLinksByTier(links, showExtended, showAdvanced),
    callerAuthorityRank,
  );
}

/**
 * Applies **`filterNavLinksForOperatorShell`** to every configured group and **omits groups with no visible links**.
 * Sidebar, mobile drawer, and command palette should iterate this result so tier + authority + empty-group rules stay aligned.
 */
export function listNavGroupsVisibleInOperatorShell(
  groups: ReadonlyArray<NavGroupConfig>,
  showExtended: boolean,
  showAdvanced: boolean,
  callerAuthorityRank: number,
): NavGroupWithVisibleLinks[] {
  const out: NavGroupWithVisibleLinks[] = [];

  for (const group of groups) {
    const visibleLinks = filterNavLinksForOperatorShell(
      group.links,
      showExtended,
      showAdvanced,
      callerAuthorityRank,
    );

    if (visibleLinks.length === 0) {
      continue;
    }

    out.push({ group, visibleLinks });
  }

  return out;
}
