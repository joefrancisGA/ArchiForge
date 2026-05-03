import type { NavGroupConfig, NavLinkItem, NavShellSurface } from "@/lib/nav-config";
import { filterNavLinksByAuthority } from "@/lib/nav-authority";
import { filterNavLinksByCommittedArchitectureReviewGate } from "@/lib/nav-committed-architecture-review-gate";
import { filterNavLinksByTier } from "@/lib/nav-tier";
import { filterNavLinksByPublishReadiness } from "@/lib/nav-publish-readiness";

/** In public demo builds, omit routes that read as unfinished operator tooling or leak internal surfaces. */
const DEMO_MODE_OMIT_OPERATOR_HREFS = new Set<string>([
  "/planning",
  "/product-learning",
  "/recommendation-learning",
  "/evolution-review",
  "/replay",
  "/search",
  "/demo/explain",
  "/admin/health",
  "/admin/support",
  "/admin/users",
  "/alerts",
  "/policy-packs",
  "/governance-resolution",
  "/governance",
  "/audit",
  "/integrations/teams",
  "/digests",
  "/digest-subscriptions",
  "/settings/tenant",
  "/settings/baseline",
  "/settings/tenant-cost",
  "/settings/exec-digest",
  "/value-report",
  "/value-report/pilot",
]);

function omitThinRoutesInPublicDemoMode(links: NavLinkItem[]): NavLinkItem[] {
  const demo =
    process.env.NEXT_PUBLIC_DEMO_MODE === "true" || process.env.NEXT_PUBLIC_DEMO_MODE === "1";

  if (!demo) {
    return links;
  }

  return links.filter((l) => !DEMO_MODE_OMIT_OPERATOR_HREFS.has(l.href));
}

/** One nav group after **tier → authority** filtering, only emitted when at least one link remains. */
export type NavGroupWithVisibleLinks = {
  group: NavGroupConfig;
  visibleLinks: NavLinkItem[];
};

/**
 * ## Role
 *
 * Single composition point for operator shell navigation (sidebar, mobile drawer, command palette).
 * **Out of scope:** **`useEnterpriseMutationCapability()`** and other page-level POST soft-disables — this module only
 * applies **tier** then **`filterNavLinksByAuthority`**; see **docs/PRODUCT_PACKAGING.md** §3 *Four UI shaping surfaces*.
 *
 * ## Composition order (do not reorder)
 *
 * Within each **`NAV_GROUPS`** block from **`nav-config.ts`**: **tier** (`nav-tier.ts`) runs first (**progressive disclosure**
 * — Core Pilot first; Advanced after “Show more”; deeper Enterprise after extended/advanced toggles). **Authority**
 * (`filterNavLinksByAuthority` in **`nav-authority.ts`**) runs second so link visibility matches policy **names** aligned
 * with **`ArchLucid.Api`**, not a parallel matrix. **Rank never substitutes extended/advanced disclosure:** an
 * **Execute+** caller still sees only **essential**-tier Enterprise links until the user opts into extended/advanced
 * (`nav-tier.ts` gates apply before authority). **Packaging map:** **docs/PRODUCT_PACKAGING.md** §3 *Code seams* table
 * (**`NAV_GROUPS[].id`** → layer); this module owns only the **composition** step (**tier → authority →** drop empty groups).
 *
 * Pass **`useNavCallerAuthorityRank()`** (or **`CurrentPrincipal.authorityRank`**) and **`useNavCommittedArchitectureReview()`**
 * so filtering matches **`OperatorNavAuthorityProvider`**. Call sites must **omit empty groups** when iterating **`listNavGroupsVisibleInOperatorShell`**
 * results — this module already drops groups with zero visible links.
 *
 * ## API vs UI
 *
 * **UI shaping only** — same boundary as **`nav-config.ts`** / **`nav-authority.ts`**: visible links **do not** guarantee
 * successful HTTP calls — **`[Authorize(Policy = …)]`** still returns **401/403**.
 * **Packaging:** **docs/PRODUCT_PACKAGING.md** §3 (*Code seams* + *Contributor drift guard*). **Stage 1 framing:**
 * **docs/COMMERCIAL_BOUNDARY_HARDENING_SEQUENCE.md** §4.
 *
 * **Canonical docs:** [PRODUCT_PACKAGING.md](../../../docs/PRODUCT_PACKAGING.md) §3 *Code seams* + *Contributor drift guard*;
 * Stage 1 (not entitlements): [COMMERCIAL_BOUNDARY_HARDENING_SEQUENCE.md](../../../docs/COMMERCIAL_BOUNDARY_HARDENING_SEQUENCE.md) §4.
 *
 * @see `authority-seam-regression.test.ts` — tier + authority composition vs caller rank (Core Pilot invariants; ordering;
 *   rank **0** vs **`ReadAuthority`**; **`/alerts`** **`essential`**; Enterprise href **monotonicity**; Advanced default **`/ask`**-only;
 *   **`/governance`** gated on extended+advanced at Execute rank; **`LAYER_PAGE_GUIDANCE`** Enterprise vs Advanced **`enterpriseFootnote`**).
 * @see `authority-execute-floor-regression.test.ts` — **Execute floor** parity (nav **`ExecuteAuthority`** row vs mutation boolean) + **`operate-governance`** config invariants under **`filterNavLinksByAuthority`** alone (complements tier∩rank tests above).
 * @see `authority-shaped-ui-regression.test.ts` — catalog **`ExecuteAuthority`** links vs Read/Execute rank (this module composes those links after **tier**).
 * @see `nav-shell-visibility.test.ts` — empty-group omission after tier then authority; default Reader Enterprise strip;
 *   Execute rank does not bypass extended tier without disclosure toggles; **Core Pilot** **`/replay`** (extended **Execute**)
 *   stays hidden until **Show more** even at Admin rank.
 * @see `OperatorNavAuthorityProvider.test.tsx` — conservative rank during JWT `/me` refetch (feeds this module indirectly).
 * @see `enterprise-authority-ui-shaping.test.tsx` — **`useEnterpriseMutationCapability`** → **`disabled`** / **`readOnly`** on representative Enterprise pages (incl. governance submit fields).
 * @see `authority-shaped-layout-regression.test.tsx` — read-tier **layout** (inspect-first columns, triage deemphasis); complements this module’s **link set** only.
 */
export function filterNavLinksForOperatorShell(
  links: ReadonlyArray<NavLinkItem>,
  showExtended: boolean,
  showAdvanced: boolean,
  callerAuthorityRank: number,
  /** Pilot default sidebar: fewer links until the user expands “Show all features” (**localStorage** `archlucid-nav-expanded`). */
  applyCollapsedSidebarPilotFilter = false,
  hasCommittedArchitectureReview = true,
): NavLinkItem[] {
  let gated = filterNavLinksByCommittedArchitectureReviewGate(links, hasCommittedArchitectureReview);

  let tiered: NavLinkItem[] = filterNavLinksByAuthority(
    filterNavLinksByTier(gated, showExtended, showAdvanced),
    callerAuthorityRank,
  );

  tiered = omitThinRoutesInPublicDemoMode(tiered);

  if (!applyCollapsedSidebarPilotFilter)
    return tiered;

  return tiered.filter((l) => l.defaultVisibleInCollapsedSidebar === true);
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
  applyCollapsedSidebarPilotFilter = false,
  surfaceFilter: "all" | NavShellSurface = "all",
  hasCommittedArchitectureReview = true,
): NavGroupWithVisibleLinks[] {
  const out: NavGroupWithVisibleLinks[] = [];

  for (const group of groups) {
    if (surfaceFilter !== "all" && group.surface !== surfaceFilter) {
      continue;
    }

    const useCollapsedPilot =
      applyCollapsedSidebarPilotFilter && group.surface === "review-workflow";

    const visibleLinks = filterNavLinksByPublishReadiness(
      filterNavLinksForOperatorShell(
        group.links,
        showExtended,
        showAdvanced,
        callerAuthorityRank,
        useCollapsedPilot,
        hasCommittedArchitectureReview,
      ),
    );

    if (visibleLinks.length === 0) {
      continue;
    }

    out.push({ group, visibleLinks });
  }

  return out;
}

/**
 * Sidebar “N more features” badge: full operator link count vs collapsed-pilot link count (same tier ∩ authority ∩ publish gates).
 */
export function countSidebarLinksHiddenByCollapsedPilot(
  groups: ReadonlyArray<NavGroupConfig>,
  showExtended: boolean,
  showAdvanced: boolean,
  callerAuthorityRank: number,
  hasCommittedArchitectureReview = true,
): number {
  let full = 0;
  let collapsed = 0;

  for (const group of groups) {
    if (group.surface === "platform-admin") {
      continue;
    }

    full += filterNavLinksByPublishReadiness(
      filterNavLinksForOperatorShell(
        group.links,
        showExtended,
        showAdvanced,
        callerAuthorityRank,
        false,
        hasCommittedArchitectureReview,
      ),
    ).length;
    collapsed += filterNavLinksByPublishReadiness(
      filterNavLinksForOperatorShell(
        group.links,
        showExtended,
        showAdvanced,
        callerAuthorityRank,
        true,
        hasCommittedArchitectureReview,
      ),
    ).length;
  }

  return Math.max(0, full - collapsed);
}

/**
 * How many hrefs in a group are hidden by the current extended/advanced flags (vs. full disclosure at the same
 * authority rank). Used to surface a “N more” affordance in the sidebar.
 */
export function countLinksHiddenByProgressiveDisclosure(
  group: NavGroupConfig,
  showExtended: boolean,
  showAdvanced: boolean,
  callerAuthorityRank: number,
  hasCommittedArchitectureReview = true,
): number {
  const current = filterNavLinksForOperatorShell(
    group.links,
    showExtended,
    showAdvanced,
    callerAuthorityRank,
    false,
    hasCommittedArchitectureReview,
  );
  const full = filterNavLinksForOperatorShell(
    group.links,
    true,
    true,
    callerAuthorityRank,
    false,
    hasCommittedArchitectureReview,
  );
  const currentHrefs = new Set(current.map((l) => l.href));

  return full.filter((l) => !currentHrefs.has(l.href)).length;
}
