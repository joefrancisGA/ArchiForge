import { describe, expect, it } from "vitest";

import { NAV_GROUPS } from "@/lib/nav-config";
import { AUTHORITY_RANK } from "@/lib/nav-authority";
import { filterNavLinksForOperatorShell } from "@/lib/nav-shell-visibility";

describe("filterNavLinksForOperatorShell", () => {
  const enterprise = NAV_GROUPS.find((g) => g.id === "alerts-governance");

  it("hides Admin-only policy packs for Reader rank while keeping Alerts at essential tier", () => {
    expect(enterprise).toBeDefined();

    const visible = filterNavLinksForOperatorShell(
      enterprise!.links,
      false,
      false,
      AUTHORITY_RANK.ReadAuthority,
    );

    expect(visible.some((l) => l.href === "/alerts")).toBe(true);
    expect(visible.some((l) => l.href === "/policy-packs")).toBe(false);
  });

  it("shows policy packs for Admin rank when extended links are enabled", () => {
    expect(enterprise).toBeDefined();

    const visible = filterNavLinksForOperatorShell(
      enterprise!.links,
      true,
      false,
      AUTHORITY_RANK.AdminAuthority,
    );

    expect(visible.some((l) => l.href === "/policy-packs")).toBe(true);
  });

  it("hides Execute-tier governance workflow for Reader even when advanced tier is on", () => {
    const visible = filterNavLinksForOperatorShell(
      enterprise!.links,
      true,
      true,
      AUTHORITY_RANK.ReadAuthority,
    );

    expect(visible.some((l) => l.href === "/governance")).toBe(false);
    expect(visible.some((l) => l.href === "/governance/dashboard")).toBe(true);
  });
});
