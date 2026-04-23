"use client";

import { useNavCallerAuthorityRank } from "@/components/OperatorNavAuthorityProvider";
import { useNavProgressiveDisclosure } from "@/hooks/useNavProgressiveDisclosure";
import {
  alertOperatorToolingOperatorRankLine,
  alertOperatorToolingReaderRankLine,
  alertsInboxRankOperatorLine,
  alertsInboxRankReaderLine,
  auditLogRankOperatorLine,
  auditLogRankReaderLine,
  enterpriseExecutePageHintReaderRank,
  enterpriseNavHintOperatorRank,
  enterpriseNavHintReaderRank,
  governanceDashboardReaderActionLine,
  governanceResolutionRankOperatorLine,
  governanceResolutionRankReaderLine,
  layerHeaderEnterpriseOperatorRankLine,
  layerHeaderEnterpriseReaderRankLine,
} from "@/lib/enterprise-controls-context-copy";
import { enterpriseMutationCapabilityFromRank } from "@/lib/enterprise-mutation-capability";
import {
  LAYER_PAGE_GUIDANCE,
  type LayerGuidanceBlock,
  type LayerGuidancePageKey,
} from "@/lib/layer-guidance";
import { NAV_GROUPS } from "@/lib/nav-config";
import {
  listNavGroupsVisibleInOperatorShell,
  type NavGroupWithVisibleLinks,
} from "@/lib/nav-shell-visibility";

/**
 * Selected, rank-aware text snippets that the four `EnterpriseControlsContextHints`
 * helpers and `LayerHeader` would render today. `null` means "this hint is intentionally
 * hidden at this rank for this route" (matches the existing components' early-`return null`
 * paths so callers can substitute the composed value 1:1).
 */
export type NavSurfaceContextHints = {
  /** Sidebar / mobile drawer second line under the Enterprise Controls group caption. */
  readonly enterpriseNavGroupHint: string;
  /** Page cue on Execute-floor mutation pages — only rendered when caller is below Execute. */
  readonly enterpriseExecutePageHint: string | null;
  /** `LayerHeader`'s rank-aware footnote; null on non-Enterprise pages. */
  readonly layerHeaderEnterpriseRankCue: string | null;
  /** Governance-resolution second line. */
  readonly governanceResolutionRank: string;
  /** Alerts-inbox second line. */
  readonly alertsInboxRank: string;
  /** Audit-log second line. */
  readonly auditLogRank: string;
  /** Alert-tooling family second line (rules, routing, simulation, tuning, composite). */
  readonly alertOperatorToolingRank: string;
  /** Governance dashboard read-only-action cue — null when Execute+ (matches component). */
  readonly governanceDashboardReaderAction: string | null;
};

/**
 * Composed, route-scoped UI shaping surface returned by {@link useNavSurface}. Each
 * field is sourced from the existing single-purpose surface module so the four
 * underlying surfaces (`nav-shell-visibility`, `enterprise-mutation-capability`,
 * `layer-guidance`, `EnterpriseControlsContextHints`) remain the implementation
 * detail and the only place the rank/tier rules live.
 */
export type NavSurface = {
  readonly links: ReadonlyArray<NavGroupWithVisibleLinks>;
  readonly mutationCapability: boolean;
  readonly layerGuidance: LayerGuidanceBlock;
  readonly contextHints: NavSurfaceContextHints;
  readonly callerAuthorityRank: number;
  readonly showExtended: boolean;
  readonly showAdvanced: boolean;
  readonly mounted: boolean;
};

/**
 * Single composed surface hook for any operator route that today calls
 * `nav-shell-visibility` + `useEnterpriseMutationCapability` + `LayerHeader` +
 * `EnterpriseControlsContextHints` separately. Callers pass the
 * {@link LayerGuidancePageKey} that matches the route family (the same key the
 * page already passes to `<LayerHeader pageKey=… />`).
 *
 * **UI shaping only** — `mutationCapability === true` does not guarantee the API
 * call succeeds; `[Authorize(Policy = …)]` on `ArchLucid.Api` still returns
 * 401/403. The four underlying surfaces stay exported so call sites that only
 * need one piece (e.g. the sidebar) can keep using them.
 *
 * @see `nav-shell-visibility.ts` for tier → authority → empty-group composition.
 * @see `enterprise-mutation-capability.ts` for the Execute+ mutation floor.
 * @see `layer-guidance.ts` for `LAYER_PAGE_GUIDANCE` packaging copy.
 * @see `EnterpriseControlsContextHints.tsx` for the rendered rank cue components.
 * @see `use-nav-surface.test.ts` — equivalence vs the four surfaces individually.
 * @see docs/PRODUCT_PACKAGING.md §3 *Four UI shaping surfaces* — *Composed surface (preferred)*.
 */
export function useNavSurface(routeKey: LayerGuidancePageKey): NavSurface {
  const callerAuthorityRank = useNavCallerAuthorityRank();
  const { mounted, showExtended, showAdvanced } = useNavProgressiveDisclosure();

  return composeNavSurface(routeKey, callerAuthorityRank, showExtended, showAdvanced, mounted);
}

/**
 * Pure composition function used by {@link useNavSurface} and by
 * `use-nav-surface.test.ts` to assert equivalence with the four underlying
 * surfaces called directly. Lifted out of the hook so the test does not have
 * to render React.
 */
export function composeNavSurface(
  routeKey: LayerGuidancePageKey,
  callerAuthorityRank: number,
  showExtended: boolean,
  showAdvanced: boolean,
  mounted: boolean,
): NavSurface {
  const layerGuidance = LAYER_PAGE_GUIDANCE[routeKey];
  const links = listNavGroupsVisibleInOperatorShell(
    NAV_GROUPS,
    showExtended,
    showAdvanced,
    callerAuthorityRank,
  );
  const mutationCapability = enterpriseMutationCapabilityFromRank(callerAuthorityRank);
  const contextHints = composeContextHints(layerGuidance, callerAuthorityRank, mutationCapability);

  return {
    links,
    mutationCapability,
    layerGuidance,
    contextHints,
    callerAuthorityRank,
    showExtended,
    showAdvanced,
    mounted,
  };
}

function composeContextHints(
  layerGuidance: LayerGuidanceBlock,
  callerAuthorityRank: number,
  mutationCapability: boolean,
): NavSurfaceContextHints {
  // `isReader` mirrors what `EnterpriseControlsContextHints` checks today —
  // anything below Execute is a "reader" for the purposes of the rank cues. We
  // derive it from `mutationCapability` (which itself reads
  // `enterpriseMutationCapabilityFromRank`) so the floor stays in exactly one
  // place across the four shaping surfaces.
  const isReader = !mutationCapability;
  const isEnterpriseControls =
    layerGuidance.layerBadge === "Enterprise Controls" && layerGuidance.enterpriseFootnote != null;

  const layerHeaderEnterpriseRankCue = isEnterpriseControls
    ? isReader
      ? layerHeaderEnterpriseReaderRankLine
      : layerHeaderEnterpriseOperatorRankLine
    : null;

  const enterpriseExecutePageHint = isReader ? enterpriseExecutePageHintReaderRank : null;
  const governanceDashboardReaderAction = isReader ? governanceDashboardReaderActionLine : null;

  return {
    enterpriseNavGroupHint: isReader ? enterpriseNavHintReaderRank : enterpriseNavHintOperatorRank,
    enterpriseExecutePageHint,
    layerHeaderEnterpriseRankCue,
    governanceResolutionRank: isReader
      ? governanceResolutionRankReaderLine
      : governanceResolutionRankOperatorLine,
    alertsInboxRank: isReader ? alertsInboxRankReaderLine : alertsInboxRankOperatorLine,
    auditLogRank: isReader ? auditLogRankReaderLine : auditLogRankOperatorLine,
    alertOperatorToolingRank: isReader
      ? alertOperatorToolingReaderRankLine
      : alertOperatorToolingOperatorRankLine,
    governanceDashboardReaderAction,
  };
}
