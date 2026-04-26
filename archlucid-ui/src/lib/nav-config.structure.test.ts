import { describe, expect, it } from "vitest";

import { flattenNavLinks, NAV_GROUPS } from "@/lib/nav-config";

describe("nav-config structure", () => {
  it("does not duplicate hrefs in flattened nav (palette and other consumers key on href)", () => {
    const flat = flattenNavLinks();
    const hrefs = flat.map((link) => link.href);
    const dupes = hrefs.filter((href, index) => hrefs.indexOf(href) !== index);
    const uniqueDupes = [...new Set(dupes)];

    expect(uniqueDupes, `Duplicate hrefs: ${uniqueDupes.join(", ")}`).toEqual([]);
  });

  it("keeps flattenNavLinks length aligned with all group link counts", () => {
    const fromGroups = NAV_GROUPS.reduce((total, group) => total + group.links.length, 0);

    expect(flattenNavLinks().length).toBe(fromGroups);
  });

  it("sets requiredAuthority on every Governance link except system health (Pilot essentials may omit)", () => {
    const enterprise = NAV_GROUPS.find((group) => group.id === "operate-governance");

    expect(enterprise).toBeDefined();

    for (const link of enterprise!.links) {
      if (link.href === "/admin/health") {
        expect(link.requiredAuthority, link.href).toBeUndefined();
        continue;
      }

      expect(link.requiredAuthority, link.href).toBeDefined();
    }
  });

  it("sets requiredAuthority on every Analysis nav link", () => {
    const advanced = NAV_GROUPS.find((group) => group.id === "operate-analysis");

    expect(advanced).toBeDefined();

    for (const link of advanced!.links) {
      expect(link.requiredAuthority, link.href).toBeDefined();
    }
  });

  /**
   * Tier runs before authority in the shell: Execute-class destinations must not sit on **essential** tier or they
   * could appear for first-pilot defaults before “Show more” regardless of rank story (see `nav-shell-visibility.test.ts`).
   */
  it("keeps ExecuteAuthority Governance links off essential tier", () => {
    const enterprise = NAV_GROUPS.find((group) => group.id === "operate-governance");

    expect(enterprise).toBeDefined();

    const executeLinks = enterprise!.links.filter((link) => link.requiredAuthority === "ExecuteAuthority");

    expect(executeLinks.length).toBeGreaterThan(0);

    for (const link of executeLinks) {
      expect(link.tier, link.href).not.toBe("essential");
    }
  });

  /**
   * Pilot essentials intentionally omit `requiredAuthority` so bootstrap / conservative ranks still see the
   * default path (`nav-config` Authority block). A stray Read/Execute label on Home or Runs would regress first pilots.
   */
  it("keeps requiredAuthority unset on Pilot essential-tier links", () => {
    const core = NAV_GROUPS.find((group) => group.id === "pilot");

    expect(core).toBeDefined();

    for (const link of core!.links) {
      if (link.tier === "essential") {
        expect(link.requiredAuthority, link.href).toBeUndefined();
      }
    }
  });

  /**
   * Same structural rule as **Governance**: Execute-class **Analysis** links must not sit on
   * `essential`, or they could appear before progressive disclosure even when rank allows Execute (`nav-shell-visibility`
   * tier order).
   */
  it("keeps ExecuteAuthority Analysis links off essential tier", () => {
    const advanced = NAV_GROUPS.find((group) => group.id === "operate-analysis");

    expect(advanced).toBeDefined();

    const executeLinks = advanced!.links.filter((link) => link.requiredAuthority === "ExecuteAuthority");

    expect(executeLinks.length).toBeGreaterThan(0);

    for (const link of executeLinks) {
      expect(link.tier, link.href).not.toBe("essential");
    }
  });
});
