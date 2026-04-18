import { describe, expect, it } from "vitest";

import {
  AUTHORITY_RANK,
  collectArchLucidRoleClaimValues,
  filterNavLinksByAuthority,
  maxAuthorityRankFromMeClaims,
  navLinkVisibleForCallerRank,
  requiredAuthorityFromRank,
  requiredAuthorityRank,
} from "@/lib/nav-authority";

describe("nav-authority", () => {
  it("orders policies Read < Execute < Admin", () => {
    expect(requiredAuthorityRank("ReadAuthority")).toBeLessThan(requiredAuthorityRank("ExecuteAuthority"));
    expect(requiredAuthorityRank("ExecuteAuthority")).toBeLessThan(requiredAuthorityRank("AdminAuthority"));
  });

  it("treats missing requiredAuthority as visible for any caller rank", () => {
    expect(navLinkVisibleForCallerRank({ href: "/", label: "Home", title: "", tier: "essential" }, 1)).toBe(true);
  });

  /** Same numeric floor as `enterpriseMutationCapabilityFromRank` and `LayerHeader` Enterprise rank cue. */
  it("flips Execute-tier link visibility at caller rank ExecuteAuthority (not below)", () => {
    const link = {
      href: "/governance",
      label: "Workflow",
      title: "",
      tier: "essential" as const,
      requiredAuthority: "ExecuteAuthority" as const,
    };

    expect(navLinkVisibleForCallerRank(link, AUTHORITY_RANK.ReadAuthority)).toBe(false);
    expect(navLinkVisibleForCallerRank(link, AUTHORITY_RANK.ExecuteAuthority)).toBe(true);
  });

  it("filters links by caller rank", () => {
    const links = [
      { href: "/a", label: "A", title: "", tier: "essential" as const },
      { href: "/b", label: "B", title: "", tier: "essential" as const, requiredAuthority: "ExecuteAuthority" as const },
      { href: "/c", label: "C", title: "", tier: "essential" as const, requiredAuthority: "AdminAuthority" as const },
    ];

    expect(filterNavLinksByAuthority(links, AUTHORITY_RANK.ReadAuthority)).toEqual([links[0]]);
    expect(filterNavLinksByAuthority(links, AUTHORITY_RANK.ExecuteAuthority)).toEqual([links[0], links[1]]);
    expect(filterNavLinksByAuthority(links, AUTHORITY_RANK.AdminAuthority)).toEqual(links);
  });

  it("derives max rank from role claims (role URI + Entra roles)", () => {
    const readerRank = maxAuthorityRankFromMeClaims([
      { type: "http://schemas.microsoft.com/ws/2008/06/identity/claims/role", value: "Reader" },
    ]);
    expect(readerRank).toBe(AUTHORITY_RANK.ReadAuthority);

    const operatorRank = maxAuthorityRankFromMeClaims([
      { type: "roles", value: "Reader" },
      { type: "roles", value: "Operator" },
    ]);
    expect(operatorRank).toBe(AUTHORITY_RANK.ExecuteAuthority);

    const adminRank = maxAuthorityRankFromMeClaims([
      { type: "http://schemas.microsoft.com/ws/2008/06/identity/claims/role", value: "Admin" },
    ]);
    expect(adminRank).toBe(AUTHORITY_RANK.AdminAuthority);
  });

  it("defaults to Read rank when no known roles are present", () => {
    expect(maxAuthorityRankFromMeClaims([{ type: "sub", value: "x" }])).toBe(AUTHORITY_RANK.ReadAuthority);
  });

  it("collectArchLucidRoleClaimValues dedupes case-insensitively", () => {
    expect(
      collectArchLucidRoleClaimValues([
        { type: "roles", value: "Reader" },
        { type: "roles", value: "reader" },
        { type: "sub", value: "x" },
      ]),
    ).toEqual(["Reader"]);
  });

  it("requiredAuthorityFromRank maps ranks to policy names", () => {
    expect(requiredAuthorityFromRank(1)).toBe("ReadAuthority");
    expect(requiredAuthorityFromRank(2)).toBe("ExecuteAuthority");
    expect(requiredAuthorityFromRank(3)).toBe("AdminAuthority");
  });
});
