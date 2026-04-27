import { describe, expect, it } from "vitest";

import { NAV_GROUPS, type NavGroupConfig } from "@/lib/nav-config";
import { AUTHORITY_RANK } from "@/lib/nav-authority";
import {
  countLinksHiddenByProgressiveDisclosure,
  filterNavLinksForOperatorShell,
  listNavGroupsVisibleInOperatorShell,
} from "@/lib/nav-shell-visibility";

describe("filterNavLinksForOperatorShell", () => {
  const enterprise = NAV_GROUPS.find((g) => g.id === "operate-governance");

  it("keeps Alerts at essential tier and omits extended Enterprise links when extended disclosure is off", () => {
    expect(enterprise).toBeDefined();

    const visible = filterNavLinksForOperatorShell(
      enterprise!.links,
      false,
      false,
      AUTHORITY_RANK.ReadAuthority,
    );

    expect(visible.some((l) => l.href === "/admin/health")).toBe(true);
    expect(visible.some((l) => l.href === "/alerts")).toBe(true);
    expect(visible.some((l) => l.href === "/policy-packs")).toBe(false);
  });

  /**
   * Default shell (no extended / no advanced): Reader should still see Enterprise Controls as system health + Alerts inbox + Findings hub.
   * If `/alerts` moves off `essential` tier, this fails loudly—avoiding an empty Enterprise group for first pilots.
   */
  it("exposes system health, Alerts inbox, and Findings in Enterprise Controls for Reader when extended and advanced are off", () => {
    expect(enterprise).toBeDefined();

    const visible = filterNavLinksForOperatorShell(
      enterprise!.links,
      false,
      false,
      AUTHORITY_RANK.ReadAuthority,
    );

    expect(visible.map((l) => l.href)).toEqual(["/admin/health", "/alerts", "/governance/findings"]);
  });

  it("shows read-tier Enterprise extended links for Reader when extended disclosure is on", () => {
    expect(enterprise).toBeDefined();

    const visible = filterNavLinksForOperatorShell(
      enterprise!.links,
      true,
      false,
      AUTHORITY_RANK.ReadAuthority,
    );

    expect(visible.some((l) => l.href === "/policy-packs")).toBe(true);
    expect(visible.some((l) => l.href === "/governance/findings")).toBe(true);
    expect(visible.some((l) => l.href === "/governance")).toBe(false);
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
    expect(visible.some((l) => l.href === "/governance/findings")).toBe(true);
  });

  /**
   * Tier runs before authority (`nav-shell-visibility`): higher rank must not “punch through” extended disclosure.
   * Regression: reordering filters or mis-stating tiers would expose `/policy-packs` without extended disclosure.
   */
  it("keeps extended-tier Enterprise links hidden when showExtended is off even for Execute rank and advanced on", () => {
    expect(enterprise).toBeDefined();

    const visible = filterNavLinksForOperatorShell(
      enterprise!.links,
      false,
      true,
      AUTHORITY_RANK.ExecuteAuthority,
    );

    expect(visible.some((l) => l.href === "/policy-packs")).toBe(false);
    expect(visible.some((l) => l.href === "/governance")).toBe(true);
    expect(visible.some((l) => l.href === "/alerts")).toBe(true);
  });

  /**
   * Default shell (no extended, no advanced): Execute-ranked operators see the same essential Enterprise strip as Reader
   * — system health + inbox + Findings. Rank widens authority-eligible hrefs but does not replace progressive disclosure.
   */
  it("limits Enterprise Controls to system health, Alerts, and Findings for Execute rank when extended and advanced are off", () => {
    expect(enterprise).toBeDefined();

    const visible = filterNavLinksForOperatorShell(
      enterprise!.links,
      false,
      false,
      AUTHORITY_RANK.ExecuteAuthority,
    );

    expect(visible.map((l) => l.href)).toEqual(["/admin/health", "/alerts", "/governance/findings"]);
  });

  /**
   * Same tier gate as Enterprise extended links: `/replay` is **extended** + **ExecuteAuthority** — Admin rank must
   * not surface it until **Show analysis & investigation tools** (`nav-tier` before `nav-authority`).
   */
  it("hides Analysis extended Execute link (/replay) until showExtended even for Admin rank", () => {
    const analysis = NAV_GROUPS.find((g) => g.id === "operate-analysis");

    expect(analysis).toBeDefined();

    const extendedOff = filterNavLinksForOperatorShell(
      analysis!.links,
      false,
      false,
      AUTHORITY_RANK.AdminAuthority,
    );

    expect(extendedOff.some((l) => l.href === "/replay")).toBe(false);

    const extendedOn = filterNavLinksForOperatorShell(
      analysis!.links,
      true,
      false,
      AUTHORITY_RANK.AdminAuthority,
    );

    expect(extendedOn.some((l) => l.href === "/replay")).toBe(true);
  });
});

describe("listNavGroupsVisibleInOperatorShell", () => {
  const syntheticExtendedOnly: NavGroupConfig[] = [
    {
      id: "synthetic-extended-only",
      label: "Synthetic",
      links: [
        {
          href: "/synthetic-extended",
          label: "Extended only",
          title: "Test",
          tier: "extended",
          requiredAuthority: "ReadAuthority",
        },
      ],
    },
  ];

  it("never returns a group with zero visible links", () => {
    const rows = listNavGroupsVisibleInOperatorShell(
      NAV_GROUPS,
      true,
      true,
      AUTHORITY_RANK.ReadAuthority,
    );

    expect(rows.length).toBeGreaterThan(0);

    for (const row of rows) {
      expect(row.visibleLinks.length).toBeGreaterThan(0);
    }
  });

  it("omits a group when tier filtering removes every link", () => {
    const rowsOff = listNavGroupsVisibleInOperatorShell(
      syntheticExtendedOnly,
      false,
      false,
      AUTHORITY_RANK.ReadAuthority,
    );

    expect(rowsOff).toEqual([]);

    const rowsOn = listNavGroupsVisibleInOperatorShell(
      syntheticExtendedOnly,
      true,
      false,
      AUTHORITY_RANK.ReadAuthority,
    );

    expect(rowsOn).toHaveLength(1);
    expect(rowsOn[0]!.group.id).toBe("synthetic-extended-only");
    expect(rowsOn[0]!.visibleLinks.some((l) => l.href === "/synthetic-extended")).toBe(true);
  });

  // Complements the tier-only empty-group case: authority can zero a group even when tiers would allow the hrefs.
  it("omits a group when authority filtering removes every link (Execute-only group, Read caller)", () => {
    const executeOnlyGroup: NavGroupConfig[] = [
      {
        id: "synthetic-execute-only",
        label: "Synthetic",
        links: [
          {
            href: "/synthetic-exec-a",
            label: "A",
            title: "Test",
            tier: "essential",
            requiredAuthority: "ExecuteAuthority",
          },
          {
            href: "/synthetic-exec-b",
            label: "B",
            title: "Test",
            tier: "essential",
            requiredAuthority: "ExecuteAuthority",
          },
        ],
      },
    ];

    const rows = listNavGroupsVisibleInOperatorShell(
      executeOnlyGroup,
      true,
      true,
      AUTHORITY_RANK.ReadAuthority,
    );

    expect(rows).toEqual([]);
  });

  it("matches filterNavLinksForOperatorShell for the Enterprise group when extended is on", () => {
    const enterprise = NAV_GROUPS.find((g) => g.id === "operate-governance");

    expect(enterprise).toBeDefined();

    const fromList = listNavGroupsVisibleInOperatorShell(
      NAV_GROUPS,
      true,
      false,
      AUTHORITY_RANK.ReadAuthority,
    ).find((r) => r.group.id === "operate-governance");

    expect(fromList).toBeDefined();

    const fromFilter = filterNavLinksForOperatorShell(
      enterprise!.links,
      true,
      false,
      AUTHORITY_RANK.ReadAuthority,
    );

    expect(fromList!.visibleLinks.map((l) => l.href)).toEqual(fromFilter.map((l) => l.href));
  });
});

describe("countLinksHiddenByProgressiveDisclosure", () => {
  it("is zero when extended and advanced are fully on", () => {
    const enterprise = NAV_GROUPS.find((g) => g.id === "operate-governance") as NavGroupConfig;

    const n = countLinksHiddenByProgressiveDisclosure(enterprise, true, true, AUTHORITY_RANK.ReadAuthority);
    expect(n).toBe(0);
  });

  it("is positive when extended links are off but exist at full disclosure", () => {
    const enterprise = NAV_GROUPS.find((g) => g.id === "operate-governance") as NavGroupConfig;

    const n = countLinksHiddenByProgressiveDisclosure(enterprise, false, false, AUTHORITY_RANK.ReadAuthority);
    expect(n).toBeGreaterThan(0);
  });
});
