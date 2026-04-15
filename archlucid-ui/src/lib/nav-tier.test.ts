import { describe, expect, it } from "vitest";

import { filterNavLinksByTier } from "@/lib/nav-tier";

describe("filterNavLinksByTier", () => {
  const links = [
    { href: "/a", label: "A", tier: "essential" as const },
    { href: "/b", label: "B", tier: "extended" as const },
    { href: "/c", label: "C", tier: "advanced" as const },
  ];

  it("returns only essential when extended and advanced are off", () => {
    expect(filterNavLinksByTier(links, false, false)).toEqual([links[0]]);
  });

  it("includes extended when showExtended is true", () => {
    expect(filterNavLinksByTier(links, true, false)).toEqual([links[0], links[1]]);
  });

  it("includes advanced only when showAdvanced is true", () => {
    expect(filterNavLinksByTier(links, true, true)).toEqual(links);
  });
});
