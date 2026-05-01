import { describe, expect, it } from "vitest";

import { isNavLinkActive } from "@/lib/nav-link-active";

describe("isNavLinkActive", () => {
  it("matches home only for exact /", () => {
    expect(isNavLinkActive("/", "/")).toBe(true);
    expect(isNavLinkActive("/reviews", "/")).toBe(false);
  });

  it("matches /reviews list but not /reviews/new or review detail", () => {
    expect(isNavLinkActive("/reviews", "/reviews?projectId=default")).toBe(true);
    expect(isNavLinkActive("/reviews/new", "/reviews?projectId=default")).toBe(false);
    expect(isNavLinkActive("/reviews/abc", "/reviews?projectId=default")).toBe(false);
  });

  it("matches /reviews/new exactly", () => {
    expect(isNavLinkActive("/reviews/new", "/reviews/new")).toBe(true);
    expect(isNavLinkActive("/reviews", "/reviews/new")).toBe(false);
  });

  it("matches exact path or nested segments for other routes", () => {
    expect(isNavLinkActive("/compare", "/compare")).toBe(true);
    expect(isNavLinkActive("/governance/dashboard", "/governance/dashboard")).toBe(true);
    expect(isNavLinkActive("/governance/dashboard/extra", "/governance/dashboard")).toBe(true);
  });
});
