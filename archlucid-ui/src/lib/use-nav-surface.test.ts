/**
 * Equivalence guard: the composed `useNavSurface()` hook must produce the same
 * `links` set, mutation boolean, layer-guidance block, and rank-cue strings as
 * calling the four underlying surface modules directly. This pins the
 * "composed surface (preferred)" addition in **docs/PRODUCT_PACKAGING.md** §3
 * to the same numerics enforced by `authority-seam-regression.test.ts`,
 * `authority-execute-floor-regression.test.ts`, and the per-component tests.
 *
 * @see `use-nav-surface.ts` — the composed surface returned to callers.
 * @see `nav-shell-visibility.ts`, `enterprise-mutation-capability.ts`,
 *   `layer-guidance.ts`, `EnterpriseControlsContextHints.tsx` for the four
 *   underlying surfaces this hook composes.
 */
import { describe, expect, it } from "vitest";

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
import { LAYER_PAGE_GUIDANCE, type LayerGuidancePageKey } from "@/lib/layer-guidance";
import { AUTHORITY_RANK } from "@/lib/nav-authority";
import { NAV_GROUPS } from "@/lib/nav-config";
import { listNavGroupsVisibleInOperatorShell } from "@/lib/nav-shell-visibility";
import { composeNavSurface } from "@/lib/use-nav-surface";

describe("composeNavSurface — equivalence with the four underlying surfaces", () => {
  const allRanks: ReadonlyArray<number> = [
    0, // unauthenticated / pre-`/me` conservative rank
    AUTHORITY_RANK.ReadAuthority,
    AUTHORITY_RANK.ExecuteAuthority,
    AUTHORITY_RANK.AdminAuthority,
  ];

  const tierMatrix: ReadonlyArray<readonly [boolean, boolean]> = [
    [false, false],
    [true, false],
    [true, true],
  ];

  const allRouteKeys: ReadonlyArray<LayerGuidancePageKey> = Object.keys(
    LAYER_PAGE_GUIDANCE,
  ) as ReadonlyArray<LayerGuidancePageKey>;

  it("returns links identical to listNavGroupsVisibleInOperatorShell for every rank × tier combination", () => {
    const sampleRouteKey: LayerGuidancePageKey = "governance-workflow";

    for (const rank of allRanks) {
      for (const [showExtended, showAdvanced] of tierMatrix) {
        const composed = composeNavSurface(sampleRouteKey, rank, showExtended, showAdvanced, true);
        const direct = listNavGroupsVisibleInOperatorShell(
          NAV_GROUPS,
          showExtended,
          showAdvanced,
          rank,
        );

        expect(composed.links).toEqual(direct);
      }
    }
  });

  it("returns mutationCapability matching enterpriseMutationCapabilityFromRank for every rank", () => {
    const sampleRouteKey: LayerGuidancePageKey = "governance-workflow";

    for (const rank of allRanks) {
      const composed = composeNavSurface(sampleRouteKey, rank, false, false, true);

      expect(composed.mutationCapability).toBe(enterpriseMutationCapabilityFromRank(rank));
    }
  });

  it("returns the LAYER_PAGE_GUIDANCE block matching the route key for every defined key", () => {
    for (const routeKey of allRouteKeys) {
      const composed = composeNavSurface(routeKey, AUTHORITY_RANK.ReadAuthority, false, false, true);

      expect(composed.layerGuidance).toBe(LAYER_PAGE_GUIDANCE[routeKey]);
    }
  });

  it("returns the same rank cue strings the four EnterpriseControlsContextHints helpers would render at Read rank", () => {
    const composed = composeNavSurface(
      "governance-workflow",
      AUTHORITY_RANK.ReadAuthority,
      false,
      false,
      true,
    );

    expect(composed.contextHints.enterpriseNavGroupHint).toBe(enterpriseNavHintReaderRank);
    expect(composed.contextHints.enterpriseExecutePageHint).toBe(enterpriseExecutePageHintReaderRank);
    expect(composed.contextHints.governanceResolutionRank).toBe(governanceResolutionRankReaderLine);
    expect(composed.contextHints.alertsInboxRank).toBe(alertsInboxRankReaderLine);
    expect(composed.contextHints.auditLogRank).toBe(auditLogRankReaderLine);
    expect(composed.contextHints.alertOperatorToolingRank).toBe(alertOperatorToolingReaderRankLine);
    expect(composed.contextHints.governanceDashboardReaderAction).toBe(governanceDashboardReaderActionLine);
  });

  it("returns the same rank cue strings the four EnterpriseControlsContextHints helpers would render at Execute rank", () => {
    const composed = composeNavSurface(
      "governance-workflow",
      AUTHORITY_RANK.ExecuteAuthority,
      false,
      false,
      true,
    );

    expect(composed.contextHints.enterpriseNavGroupHint).toBe(enterpriseNavHintOperatorRank);
    expect(composed.contextHints.enterpriseExecutePageHint).toBeNull();
    expect(composed.contextHints.governanceResolutionRank).toBe(governanceResolutionRankOperatorLine);
    expect(composed.contextHints.alertsInboxRank).toBe(alertsInboxRankOperatorLine);
    expect(composed.contextHints.auditLogRank).toBe(auditLogRankOperatorLine);
    expect(composed.contextHints.alertOperatorToolingRank).toBe(alertOperatorToolingOperatorRankLine);
    expect(composed.contextHints.governanceDashboardReaderAction).toBeNull();
  });

  it("emits the LayerHeader Enterprise rank cue only on Enterprise Controls pages with an enterpriseFootnote", () => {
    const enterpriseRouteKey: LayerGuidancePageKey = "governance-workflow";
    const advancedRouteKey: LayerGuidancePageKey = "compare";

    const enterpriseAtReader = composeNavSurface(
      enterpriseRouteKey,
      AUTHORITY_RANK.ReadAuthority,
      false,
      false,
      true,
    );
    const enterpriseAtExecute = composeNavSurface(
      enterpriseRouteKey,
      AUTHORITY_RANK.ExecuteAuthority,
      false,
      false,
      true,
    );
    const advancedAtReader = composeNavSurface(
      advancedRouteKey,
      AUTHORITY_RANK.ReadAuthority,
      false,
      false,
      true,
    );

    expect(enterpriseAtReader.contextHints.layerHeaderEnterpriseRankCue).toBe(
      layerHeaderEnterpriseReaderRankLine,
    );
    expect(enterpriseAtExecute.contextHints.layerHeaderEnterpriseRankCue).toBe(
      layerHeaderEnterpriseOperatorRankLine,
    );
    expect(advancedAtReader.contextHints.layerHeaderEnterpriseRankCue).toBeNull();
  });

  it("does not duplicate logic — the Execute floor used by mutationCapability matches the rank cue branching", () => {
    for (const rank of allRanks) {
      const composed = composeNavSurface("governance-workflow", rank, false, false, true);

      const isReader = !composed.mutationCapability;
      const expectedNavHint = isReader ? enterpriseNavHintReaderRank : enterpriseNavHintOperatorRank;

      expect(composed.contextHints.enterpriseNavGroupHint).toBe(expectedNavHint);
    }
  });
});
