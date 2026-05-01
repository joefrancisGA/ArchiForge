import type { NavGroupConfig, NavLinkItem } from "@/lib/nav-config.types";

import { OperateAnalysisNavGroupBuilder } from "@/lib/operate-analysis-nav-group-builder";
import { OperateGovernanceNavGroupBuilder } from "@/lib/operate-governance-nav-group-builder";
import { OperatorAdminNavGroupBuilder } from "@/lib/operator-admin-nav-group-builder";
import type { NavGroupBuilder } from "@/lib/nav-group-builder";
import { PilotNavGroupBuilder } from "@/lib/pilot-nav-group-builder";

export type { NavGroupConfig, NavLinkItem, NavShellSurface } from "@/lib/nav-config.types";

/**
 * Canonical operator shell navigation — sidebar, command palette, and mobile drawer.
 *
 * **Contract (tier, authority, drift guards, Vitest anchors):** `docs/NAV_CONFIG_CONTRACT.md`.
 * **Composition:** each buyer layer is built by a **`NavGroupBuilder`** co-located in `*-nav-group-builder.ts`.
 */
const NAV_GROUP_BUILDERS: NavGroupBuilder[] = [
  new PilotNavGroupBuilder(),
  new OperateAnalysisNavGroupBuilder(),
  new OperateGovernanceNavGroupBuilder(),
  new OperatorAdminNavGroupBuilder(),
];

export const NAV_GROUPS: NavGroupConfig[] = NAV_GROUP_BUILDERS.map((builder) => builder.build());

/**
 * Flat list of configured nav links (sidebar + palette source of truth).
 * Shell UIs use **`listNavGroupsVisibleInOperatorShell`** (tier → authority, omit empty groups); per-link filtering is **`filterNavLinksForOperatorShell`**.
 */
export function flattenNavLinks(): NavLinkItem[] {
  return NAV_GROUPS.flatMap((group) => group.links);
}
