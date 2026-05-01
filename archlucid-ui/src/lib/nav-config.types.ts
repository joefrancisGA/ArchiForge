import type { LucideIcon } from "lucide-react";

import type { RequiredAuthority } from "@/lib/nav-authority";
import type { NavTier } from "@/lib/nav-tier";

/** Shell composition: buyer review journey vs tenant/platform administration (see SidebarNav / CommandPalette). */
export type NavShellSurface = "review-workflow" | "platform-admin";

/**
 * One sidebar / palette / mobile-drawer row. Tier and authority interact per
 * **`docs/NAV_CONFIG_CONTRACT.md`** and **`nav-shell-visibility.ts`**.
 */
export type NavLinkItem = {
  href: string;
  label: string;
  title: string;
  /** Progressive disclosure: essential always; extended after “Show more”; advanced after gear toggle. */
  tier: NavTier;
  /** When sidebar is in default collapsed pilot mode (“fewer sidebar links”), only links with **true** here stay visible before “Show all features”. Omit = hidden when collapsed (after tier ∩ authority). See **docs/library/PRODUCT_PACKAGING.md** §3 Improvement 7. */
  defaultVisibleInCollapsedSidebar?: boolean;
  /**
   * Minimum API policy tier this destination assumes (see `ArchLucidPolicies` on the server).
   * **Pilot essentials** omit this (broad default path). **Operate** nav links set it — see **`docs/NAV_CONFIG_CONTRACT.md`**.
   * Enforced after **`tier`** in **`nav-shell-visibility.ts`** (`filterNavLinksForOperatorShell`).
   */
  requiredAuthority?: RequiredAuthority;
  /** Registry combo for `aria-keyshortcuts`, e.g. `alt+n` */
  keyShortcut?: string;
  /** Optional icon for sidebar and mobile drawer. */
  icon?: LucideIcon;
};

/**
 * Stable group (`id` keys localStorage). **`docs/NAV_CONFIG_CONTRACT.md`** maps IDs to buyer layers.
 */
export type NavGroupConfig = {
  id: string;
  label: string;
  surface: NavShellSurface;
  /** One line under the group title — what this layer is for (see docs/library/OPERATOR_DECISION_GUIDE.md). */
  caption?: string;
  links: NavLinkItem[];
};
