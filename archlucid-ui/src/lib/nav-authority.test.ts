import { describe, expect, it } from "vitest";

import {
  AUTHORITY_RANK,
  filterNavLinksByAuthority,
  maxAuthorityRankFromMeClaims,
  navLinkVisibleForCallerRank,
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
});
