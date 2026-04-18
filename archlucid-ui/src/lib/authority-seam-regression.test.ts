/**
 * Cross-module authority seams: `/me` read-model, nav policy filtering, tier+authority composition, and Enterprise
 * mutation capability must stay aligned (same rank numerics and policy names as `nav-authority.ts`). These are narrow
 * regression guards — not a second authZ engine; the API remains authoritative.
 *
 * @see `OperatorNavAuthorityProvider.test.tsx` — conservative `useNavCallerAuthorityRank` while JWT `/me` refetches.
 */
import { describe, expect, it } from "vitest";

import { normalizeAuthMeResponse, operatorNavOutsideProviderPrincipal, shellBootstrapReadPrincipal } from "@/lib/current-principal";
import { enterpriseMutationCapabilityFromRank } from "@/lib/enterprise-mutation-capability";
import { NAV_GROUPS } from "@/lib/nav-config";
import {
  AUTHORITY_RANK,
  filterNavLinksByAuthority,
  maxAuthorityRankFromMeClaims,
  navLinkVisibleForCallerRank,
} from "@/lib/nav-authority";
import { filterNavLinksForOperatorShell } from "@/lib/nav-shell-visibility";

describe("authority seam regression", () => {
  const enterpriseLinks = NAV_GROUPS.find((g) => g.id === "alerts-governance")?.links;

  it("defines Enterprise Controls links so Reader authority filter still exposes inbox but drops Execute-only workflow", () => {
    expect(enterpriseLinks).toBeDefined();

    const readerVisible = filterNavLinksByAuthority(enterpriseLinks!, AUTHORITY_RANK.ReadAuthority);
    const hrefsRead = new Set(readerVisible.map((l) => l.href));

    expect(hrefsRead.has("/alerts")).toBe(true);
    expect(hrefsRead.has("/governance")).toBe(false);

    const executeVisible = filterNavLinksByAuthority(enterpriseLinks!, AUTHORITY_RANK.ExecuteAuthority);
    const hrefsExec = new Set(executeVisible.map((l) => l.href));

    expect(hrefsExec.has("/governance")).toBe(true);
  });

  /**
   * Drift guard: every `ExecuteAuthority` row in `nav-config` must stay invisible to Read callers and visible at Execute+.
   * Uses hrefs from config (not copy) so new links inherit the same contract automatically.
   */
  it("hides every ExecuteAuthority-marked Advanced Analysis and Enterprise nav link from Read callers", () => {
    const groupIds = ["qa-advisory", "alerts-governance"] as const;

    for (const groupId of groupIds) {
      const links = NAV_GROUPS.find((g) => g.id === groupId)?.links;

      expect(links, groupId).toBeDefined();

      const executeLinks = links!.filter((l) => l.requiredAuthority === "ExecuteAuthority");

      expect(executeLinks.length, `${groupId} should declare at least one Execute-tier destination`).toBeGreaterThan(0);

      const readerHrefs = new Set(
        filterNavLinksByAuthority(links!, AUTHORITY_RANK.ReadAuthority).map((l) => l.href),
      );

      for (const link of executeLinks) {
        expect(readerHrefs.has(link.href), link.href).toBe(false);
        expect(navLinkVisibleForCallerRank(link, AUTHORITY_RANK.ExecuteAuthority), link.href).toBe(true);
      }
    }
  });

  it("keeps maxAuthorityRankFromMeClaims aligned with normalizeAuthMeResponse.authorityRank for representative /me claims", () => {
    const cases: { claims: { type: string; value: string }[]; label: string }[] = [
      { label: "Reader", claims: [{ type: "roles", value: "Reader" }] },
      { label: "Operator", claims: [{ type: "roles", value: "Operator" }] },
      { label: "Admin", claims: [{ type: "roles", value: "Admin" }] },
      { label: "Auditor", claims: [{ type: "roles", value: "Auditor" }] },
      { label: "empty", claims: [] },
    ];

    for (const { claims, label } of cases) {
      const fromClaims = maxAuthorityRankFromMeClaims(claims);
      const fromPrincipal = normalizeAuthMeResponse({ claims }).authorityRank;

      expect(fromPrincipal, label).toBe(fromClaims);
    }
  });

  it("ties enterpriseMutationCapabilityFromRank to normalized principal rank (same threshold as nav Execute tier)", () => {
    const table: { claims: { type: string; value: string }[]; expectMutate: boolean }[] = [
      { claims: [{ type: "roles", value: "Reader" }], expectMutate: false },
      { claims: [{ type: "roles", value: "Auditor" }], expectMutate: false },
      { claims: [{ type: "roles", value: "Operator" }], expectMutate: true },
      { claims: [{ type: "roles", value: "Admin" }], expectMutate: true },
    ];

    for (const row of table) {
      const principal = normalizeAuthMeResponse({ claims: row.claims });

      expect(enterpriseMutationCapabilityFromRank(principal.authorityRank)).toBe(row.expectMutate);
    }
  });

  it("documents synthetic shell principals used before /me settles (bootstrap vs test outside-provider)", () => {
    expect(shellBootstrapReadPrincipal.authorityRank).toBe(AUTHORITY_RANK.ReadAuthority);
    expect(enterpriseMutationCapabilityFromRank(shellBootstrapReadPrincipal.authorityRank)).toBe(false);

    expect(operatorNavOutsideProviderPrincipal.authorityRank).toBe(AUTHORITY_RANK.AdminAuthority);
    expect(enterpriseMutationCapabilityFromRank(operatorNavOutsideProviderPrincipal.authorityRank)).toBe(true);
  });

  /**
   * Core Pilot defaults must stay broad: accidental `requiredAuthority` on essentials would regress the default path
   * for Reader-tier pilots (see `nav-config` Authority block).
   */
  it("keeps Core Pilot essential destinations visible for Reader with default tier toggles (extended off)", () => {
    const core = NAV_GROUPS.find((g) => g.id === "runs-review");

    expect(core).toBeDefined();

    const visible = filterNavLinksForOperatorShell(core!.links, false, false, AUTHORITY_RANK.ReadAuthority);
    const hrefs = new Set(visible.map((l) => l.href));

    expect(hrefs.has("/")).toBe(true);
    expect(hrefs.has("/runs/new")).toBe(true);
    expect(hrefs.has("/runs?projectId=default")).toBe(true);
  });
});
