import { describe, expect, it } from "vitest";

import { NAV_GROUPS, type NavGroupConfig } from "@/lib/nav-config";
import { AUTHORITY_RANK } from "@/lib/nav-authority";
import { filterNavLinksForOperatorShell, listNavGroupsVisibleInOperatorShell } from "@/lib/nav-shell-visibility";

describe("filterNavLinksForOperatorShell", () => {
  const enterprise = NAV_GROUPS.find((g) => g.id === "alerts-governance");

  it("keeps Alerts at essential tier and omits extended Enterprise links when extended disclosure is off", () => {
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

  it("shows read-tier Enterprise extended links for Reader when extended disclosure is on", () => {
    expect(enterprise).toBeDefined();

    const visible = filterNavLinksForOperatorShell(
      enterprise!.links,
      true,
      false,
      AUTHORITY_RANK.ReadAuthority,
    );

    expect(visible.some((l) => l.href === "/policy-packs")).toBe(true);
    expect(visible.some((l) => l.href === "/governance/dashboard")).toBe(true);
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
    expect(visible.some((l) => l.href === "/governance/dashboard")).toBe(true);
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
    const enterprise = NAV_GROUPS.find((g) => g.id === "alerts-governance");

    expect(enterprise).toBeDefined();

    const fromList = listNavGroupsVisibleInOperatorShell(
      NAV_GROUPS,
      true,
      false,
      AUTHORITY_RANK.ReadAuthority,
    ).find((r) => r.group.id === "alerts-governance");

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
