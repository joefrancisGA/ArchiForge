/**
 * Lightweight authority-shaped **UX** regression guards (nav catalog, rank floors, bootstrap principals).
 *
 * Broader cross-module seams live in **`authority-seam-regression.test.ts`**; Execute nav vs mutation boolean in
 * **`authority-execute-floor-regression.test.ts`**; RTL layout in **`authority-shaped-layout-regression.test.tsx`**;
 * rank-gated copy components in **`EnterpriseControlsContextHints.authority.test.tsx`**.
 *
 * **Why this file:** catches packaging drift when a new **`ExecuteAuthority`** row is added to **`NAV_GROUPS`** but
 * story tests only covered synthetic links, or when bootstrap principals stop matching the mutation soft-enable floor.
 *
 * @see **docs/PRODUCT_PACKAGING.md** §3 *Contributor drift guard*
 */
import { describe, expect, it } from "vitest";

import {
  normalizeAuthMeResponse,
  operatorNavOutsideProviderPrincipal,
  shellBootstrapReadPrincipal,
} from "@/lib/current-principal";
import { enterpriseMutationCapabilityFromRank } from "@/lib/enterprise-mutation-capability";
import { NAV_GROUPS, type NavLinkItem } from "@/lib/nav-config";
import {
  AUTHORITY_RANK,
  maxAuthorityRankFromMeClaims,
  navLinkVisibleForCallerRank,
} from "@/lib/nav-authority";

function allCatalogNavLinks(): NavLinkItem[] {
  return NAV_GROUPS.flatMap((g) => g.links);
}

describe("authority-shaped UI regression", () => {
  /**
   * Every real **`nav-config`** row tagged **`ExecuteAuthority`** must stay off the Read-tier shell and on from Execute
   * upward — otherwise nav and **`useEnterpriseMutationCapability()`** tell different stories for the same rank.
   */
  it("keeps every catalog ExecuteAuthority nav link off at Read rank and on at Execute rank", () => {
    const executeLinks = allCatalogNavLinks().filter((l) => l.requiredAuthority === "ExecuteAuthority");

    expect(executeLinks.length).toBeGreaterThan(0);

    for (const link of executeLinks) {
      expect(navLinkVisibleForCallerRank(link, AUTHORITY_RANK.ReadAuthority), link.href).toBe(false);
      expect(navLinkVisibleForCallerRank(link, AUTHORITY_RANK.ExecuteAuthority), link.href).toBe(true);
    }
  });

  /** Same numeric boundary as **`navLinkVisibleForCallerRank`** for **`ExecuteAuthority`** links (floor = 2). */
  it("flips enterpriseMutationCapabilityFromRank only at ExecuteAuthority and above", () => {
    expect(enterpriseMutationCapabilityFromRank(0)).toBe(false);
    expect(enterpriseMutationCapabilityFromRank(AUTHORITY_RANK.ReadAuthority)).toBe(false);
    expect(enterpriseMutationCapabilityFromRank(AUTHORITY_RANK.ExecuteAuthority)).toBe(true);
    expect(enterpriseMutationCapabilityFromRank(AUTHORITY_RANK.AdminAuthority)).toBe(true);
  });

  /**
   * Empty `/me` claims → Read rank in **`normalizeAuthMeResponse`** — must stay aligned with **`maxAuthorityRankFromMeClaims`**
   * so future refactors do not split rank derivation between modules.
   */
  it("aligns empty-claims rank derivation for /me normalization and maxAuthorityRankFromMeClaims", () => {
    expect(maxAuthorityRankFromMeClaims([])).toBe(AUTHORITY_RANK.ReadAuthority);
    expect(normalizeAuthMeResponse({ claims: [] }).authorityRank).toBe(maxAuthorityRankFromMeClaims([]));
  });

  /**
   * Shell defaults used before **`OperatorNavAuthorityProvider`** settles: bootstrap stays read/non-mutating; test-only
   * outside-provider principal stays operator-shaped for isolated renders.
   */
  it("keeps synthetic shell principals aligned with enterpriseMutationCapabilityFromRank", () => {
    expect(
      enterpriseMutationCapabilityFromRank(shellBootstrapReadPrincipal.authorityRank) ===
        shellBootstrapReadPrincipal.hasEnterpriseOperatorSurfaces,
    ).toBe(true);

    expect(
      enterpriseMutationCapabilityFromRank(operatorNavOutsideProviderPrincipal.authorityRank) ===
        operatorNavOutsideProviderPrincipal.hasEnterpriseOperatorSurfaces,
    ).toBe(true);
  });
});
