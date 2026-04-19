/**
 * Cross-module authority seams: `/me` read-model, nav policy filtering, tier+authority composition, and Enterprise
 * mutation capability must stay aligned (same rank numerics and policy names as `nav-authority.ts`). These are narrow
 * regression guards — not a second authZ engine; the API remains authoritative.
 *
 * @see `OperatorNavAuthorityProvider.test.tsx` — conservative `useNavCallerAuthorityRank` while JWT `/me` refetches.
 */
import { describe, expect, it } from "vitest";

import {
  normalizeAuthMeResponse,
  operatorNavOutsideProviderPrincipal,
  shellBootstrapReadPrincipal,
} from "@/lib/current-principal";
import { enterpriseMutationCapabilityFromRank } from "@/lib/enterprise-mutation-capability";
import { NAV_GROUPS } from "@/lib/nav-config";
import {
  AUTHORITY_RANK,
  filterNavLinksByAuthority,
  maxAuthorityRankFromMeClaims,
  navLinkVisibleForCallerRank,
} from "@/lib/nav-authority";
import { filterNavLinksForOperatorShell, listNavGroupsVisibleInOperatorShell } from "@/lib/nav-shell-visibility";

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

  /**
   * Single numeric floor for Execute-class nav rows and Enterprise mutation soft-enable (`AUTHORITY_RANK.ExecuteAuthority`).
   * Catches accidental divergence if either helper changes threshold independently.
   */
  it("matches enterpriseMutationCapabilityFromRank to ExecuteAuthority nav visibility for ranks 0 through Admin", () => {
    const executeTierLink = {
      href: "/_seam-probe-execute",
      label: "Probe",
      title: "Probe",
      tier: "essential" as const,
      requiredAuthority: "ExecuteAuthority" as const,
    };

    for (let rank = 0; rank <= AUTHORITY_RANK.AdminAuthority; rank += 1) {
      expect(enterpriseMutationCapabilityFromRank(rank)).toBe(
        navLinkVisibleForCallerRank(executeTierLink, rank),
      );
    }
  });

  /**
   * End-to-end strip for first-pilot Reader: `listNavGroupsVisibleInOperatorShell` must still emit Enterprise Controls
   * with only the inbox (not an empty group after tier + authority).
   */
  it("Reader default shell lists Core Pilot and Enterprise Controls with only the Alerts inbox href", () => {
    const rows = listNavGroupsVisibleInOperatorShell(
      NAV_GROUPS,
      false,
      false,
      AUTHORITY_RANK.ReadAuthority,
    );

    const ids = rows.map((r) => r.group.id);

    expect(ids).toContain("runs-review");
    expect(ids).toContain("alerts-governance");

    const enterprise = rows.find((r) => r.group.id === "alerts-governance");

    expect(enterprise?.visibleLinks.map((l) => l.href)).toEqual(["/alerts"]);
  });

  /**
   * Auditor maps to Read rank in `normalizeAuthMeResponse` but is a distinct primary role for audit UX; Enterprise nav
   * filtering must still match literal Read rank so Execute-tier hrefs stay omitted.
   */
  it("filters Enterprise nav links for Auditor /me principal the same as for ReadAuthority rank", () => {
    expect(enterpriseLinks).toBeDefined();

    const auditorRank = normalizeAuthMeResponse({
      claims: [{ type: "roles", value: "Auditor" }],
    }).authorityRank;

    expect(auditorRank).toBe(AUTHORITY_RANK.ReadAuthority);

    const fromAuditor = filterNavLinksByAuthority(enterpriseLinks!, auditorRank).map((l) => l.href);
    const fromRead = filterNavLinksByAuthority(enterpriseLinks!, AUTHORITY_RANK.ReadAuthority).map((l) => l.href);

    expect(fromAuditor).toEqual(fromRead);
  });

  /**
   * `useNavCallerAuthorityRank` can be **0** while JWT **`/me`** is unset (`OperatorNavAuthorityProvider`). That is stricter
   * than Reader for **`ReadAuthority`**-gated links — catches `navLinkVisibleForCallerRank` accidentally treating **0** like Read.
   */
  it("hides ReadAuthority-gated nav from caller rank 0 while Read rank still passes", () => {
    const readOnlyLink = {
      href: "/_probe-read-tier-nav",
      label: "Probe",
      title: "",
      tier: "extended" as const,
      requiredAuthority: "ReadAuthority" as const,
    };

    expect(navLinkVisibleForCallerRank(readOnlyLink, 0)).toBe(false);
    expect(navLinkVisibleForCallerRank(readOnlyLink, AUTHORITY_RANK.ReadAuthority)).toBe(true);
  });

  /**
   * Default first-pilot Enterprise strip is inbox-only because **`/alerts`** stays **`essential`** tier — tier runs before
   * authority in **`nav-shell-visibility.ts`** (see **`nav-shell-visibility.test.ts`**).
   */
  it("keeps Enterprise Alerts inbox on essential tier in nav-config", () => {
    const alerts = enterpriseLinks?.find((l) => l.href === "/alerts");

    expect(alerts?.tier).toBe("essential");
  });

  /**
   * Sidebar / palette iterate filtered links in **`nav-config`** order; a `.sort()` or Set iteration in the filter would
   * churn UX and complicate deep-link expectations.
   */
  it("preserves nav-config link order when filtering Enterprise Controls by authority", () => {
    expect(enterpriseLinks).toBeDefined();

    const filtered = filterNavLinksByAuthority(enterpriseLinks!, AUTHORITY_RANK.AdminAuthority);
    const hrefOrder = enterpriseLinks!.map((l) => l.href);
    const hrefSet = new Set(filtered.map((l) => l.href));
    const expectedOrder = hrefOrder.filter((href) => hrefSet.has(href));

    expect(filtered.map((l) => l.href)).toEqual(expectedOrder);
  });

  /**
   * **`listNavGroupsVisibleInOperatorShell`** walks **`NAV_GROUPS`** in file order; emitting groups out-of-order would
   * regress sidebar / mobile drawer composition without a type error.
   */
  it("preserves NAV_GROUPS order in listNavGroupsVisibleInOperatorShell for default Reader shell", () => {
    const rows = listNavGroupsVisibleInOperatorShell(
      NAV_GROUPS,
      false,
      false,
      AUTHORITY_RANK.ReadAuthority,
    );
    const indexById = new Map(NAV_GROUPS.map((g, i) => [g.id, i] as const));
    const indices = rows.map((r) => indexById.get(r.group.id));

    for (const idx of indices) {
      expect(idx).toBeDefined();
    }

    for (let i = 1; i < indices.length; i++) {
      expect(indices[i]!).toBeGreaterThan(indices[i - 1]!);
    }
  });
});
