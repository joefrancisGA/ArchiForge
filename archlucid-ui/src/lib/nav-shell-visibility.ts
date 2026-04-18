import type { NavGroupConfig, NavLinkItem } from "@/lib/nav-config";
import { filterNavLinksByAuthority } from "@/lib/nav-authority";
import { filterNavLinksByTier } from "@/lib/nav-tier";

/** One nav group after **tier → authority** filtering, only emitted when at least one link remains. */
export type NavGroupWithVisibleLinks = {
  group: NavGroupConfig;
  visibleLinks: NavLinkItem[];
};

/**
 * ## Role
 *
 * Single composition point for operator shell navigation (sidebar, mobile drawer, command palette).
 *
 * ## Composition order (do not reorder)
 *
 * Within each **`NAV_GROUPS`** block from **`nav-config.ts`**: **tier** (`nav-tier.ts`) runs first (**progressive disclosure**
 * — Core Pilot first; Advanced after “Show more”; deeper Enterprise after extended/advanced toggles). **Authority**
 * (`filterNavLinksByAuthority` in **`nav-authority.ts`**) runs second so link visibility matches policy **names** aligned
 * with **`ArchLucid.Api`**, not a parallel matrix.
 *
 * Pass **`useNavCallerAuthorityRank()`** (or **`CurrentPrincipal.authorityRank`**) so filtering matches
 * **`OperatorNavAuthorityProvider`**. Call sites must **omit empty groups** when iterating **`listNavGroupsVisibleInOperatorShell`**
 * results — this module already drops groups with zero visible links.
 *
 * ## API vs UI
 *
 * Visible links **do not** guarantee successful HTTP calls — **`[Authorize(Policy = …)]`** still returns **401/403**.
 * **Packaging:** **docs/PRODUCT_PACKAGING.md** §3 (*Code seams* + *Contributor drift guard*). **Stage 1 framing:**
 * **docs/COMMERCIAL_BOUNDARY_HARDENING_SEQUENCE.md** §4.
 *
 * @see `authority-seam-regression.test.ts` — tier + authority composition vs caller rank (includes Core Pilot invariants).
 * @see `nav-shell-visibility.test.ts` — empty-group omission after tier then authority; default Reader Enterprise strip.
 * @see `OperatorNavAuthorityProvider.test.tsx` — conservative rank during JWT `/me` refetch (feeds this module indirectly).
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
