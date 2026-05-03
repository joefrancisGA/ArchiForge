import { afterEach, describe, expect, it } from "vitest";

import { NAV_GROUPS, type NavGroupConfig } from "@/lib/nav-config";
import { AUTHORITY_RANK } from "@/lib/nav-authority";
import {
  countLinksHiddenByProgressiveDisclosure,
  countSidebarLinksHiddenByCollapsedPilot,
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
      false,
      true,
    );

    expect(visible.some((l) => l.href === "/admin/health")).toBe(false);
    expect(visible.some((l) => l.href === "/alerts")).toBe(true);
    expect(visible.some((l) => l.href === "/policy-packs")).toBe(false);
  });

  /**
   * Default shell (no extended / no advanced): Reader sees Alerts inbox only for Enterprise Controls.
   * System health is Admin + advanced tier. Findings moved to the Pilot group (extended tier).
   * If `/alerts` moves off `essential` tier, this fails loudly—avoiding an empty Enterprise group for first pilots.
   */
  it("exposes Alerts inbox in Enterprise Controls for Reader when extended and advanced are off", () => {
    expect(enterprise).toBeDefined();

    const visible = filterNavLinksForOperatorShell(
      enterprise!.links,
      false,
      false,
      AUTHORITY_RANK.ReadAuthority,
      false,
      true,
    );

    expect(visible.map((l) => l.href)).toEqual(["/alerts"]);
  });

  it("shows read-tier Enterprise extended links for Reader when extended disclosure is on", () => {
    expect(enterprise).toBeDefined();

    const visible = filterNavLinksForOperatorShell(
      enterprise!.links,
      true,
      false,
      AUTHORITY_RANK.ReadAuthority,
      false,
      true,
    );

    expect(visible.some((l) => l.href === "/policy-packs")).toBe(true);
    // Findings now lives in the Pilot group (extended tier), not in operate-governance.
    expect(visible.some((l) => l.href === "/governance/findings")).toBe(false);
    expect(visible.some((l) => l.href === "/governance")).toBe(false);
  });

  it("shows policy packs for Admin rank when extended links are enabled", () => {
    expect(enterprise).toBeDefined();

    const visible = filterNavLinksForOperatorShell(
      enterprise!.links,
      true,
      false,
      AUTHORITY_RANK.AdminAuthority,
      false,
      true,
    );

    expect(visible.some((l) => l.href === "/policy-packs")).toBe(true);
  });

  it("hides Execute-tier governance workflow for Reader even when advanced tier is on", () => {
    const visible = filterNavLinksForOperatorShell(
      enterprise!.links,
      true,
      true,
      AUTHORITY_RANK.ReadAuthority,
      false,
      true,
    );

    expect(visible.some((l) => l.href === "/governance")).toBe(false);
    // Findings is in the Pilot group; the operate-governance filter should not include it.
    expect(visible.some((l) => l.href === "/governance/findings")).toBe(false);
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
      false,
      true,
    );

    expect(visible.some((l) => l.href === "/policy-packs")).toBe(false);
    expect(visible.some((l) => l.href === "/governance")).toBe(true);
    expect(visible.some((l) => l.href === "/alerts")).toBe(true);
  });

  /**
   * Default shell (no extended, no advanced): Execute-ranked operators see the same essential Enterprise strip as Reader
   * — Alerts inbox. System health is Admin + advanced. Findings is in the Pilot group (extended tier).
   */
  it("limits Enterprise Controls to Alerts for Execute rank when extended and advanced are off", () => {
    expect(enterprise).toBeDefined();

    const visible = filterNavLinksForOperatorShell(
      enterprise!.links,
      false,
      false,
      AUTHORITY_RANK.ExecuteAuthority,
      false,
      true,
    );

    expect(visible.map((l) => l.href)).toEqual(["/alerts"]);
  });

  it("shows system health for Admin rank when advanced and extended disclosure are on", () => {
    const admin = NAV_GROUPS.find((g) => g.id === "operator-admin");

    expect(admin).toBeDefined();

    const visible = filterNavLinksForOperatorShell(
      admin!.links,
      true,
      true,
      AUTHORITY_RANK.AdminAuthority,
      false,
      true,
    );

    expect(visible.some((l) => l.href === "/admin/health")).toBe(true);
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
      false,
      true,
    );

    expect(extendedOff.some((l) => l.href === "/replay")).toBe(false);

    const extendedOn = filterNavLinksForOperatorShell(
      analysis!.links,
      true,
      false,
      AUTHORITY_RANK.AdminAuthority,
      false,
      true,
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
      false,
      "all",
      true,
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
      false,
      "all",
      true,
    );

    expect(rowsOff).toEqual([]);

    const rowsOn = listNavGroupsVisibleInOperatorShell(
      syntheticExtendedOnly,
      true,
      false,
      AUTHORITY_RANK.ReadAuthority,
      false,
      "all",
      true,
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
      false,
      "all",
      true,
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
      false,
      "all",
      true,
    ).find((r) => r.group.id === "operate-governance");

    expect(fromList).toBeDefined();

    const fromFilter = filterNavLinksForOperatorShell(
      enterprise!.links,
      true,
      false,
      AUTHORITY_RANK.ReadAuthority,
      false,
      true,
    );

    expect(fromList!.visibleLinks.map((l) => l.href)).toEqual(fromFilter.map((l) => l.href));
  });
});

describe("countLinksHiddenByProgressiveDisclosure", () => {
  it("is zero when extended and advanced are fully on", () => {
    const enterprise = NAV_GROUPS.find((g) => g.id === "operate-governance") as NavGroupConfig;

    const n = countLinksHiddenByProgressiveDisclosure(
      enterprise,
      true,
      true,
      AUTHORITY_RANK.ReadAuthority,
      true,
    );
    expect(n).toBe(0);
  });

  it("is positive when extended links are off but exist at full disclosure", () => {
    const enterprise = NAV_GROUPS.find((g) => g.id === "operate-governance") as NavGroupConfig;

    const n = countLinksHiddenByProgressiveDisclosure(
      enterprise,
      false,
      false,
      AUTHORITY_RANK.ReadAuthority,
      true,
    );
    expect(n).toBeGreaterThan(0);
  });
});

describe("collapsed pilot sidebar filter", () => {
  it("shows at most eight visible links for default Reader shell when collapsed filter is applied", () => {
    const rows = listNavGroupsVisibleInOperatorShell(
      NAV_GROUPS,
      false,
      false,
      AUTHORITY_RANK.ReadAuthority,
      true,
      "all",
      true,
    );
    const count = rows.reduce((sum, row) => sum + row.visibleLinks.length, 0);

    expect(count).toBeGreaterThan(0);
    expect(count).toBeLessThanOrEqual(8);
  });

  it("exposes more links when collapsed filter is off at the same tier and rank", () => {
    const collapsed = listNavGroupsVisibleInOperatorShell(
      NAV_GROUPS,
      false,
      false,
      AUTHORITY_RANK.ReadAuthority,
      true,
      "all",
      true,
    );
    const full = listNavGroupsVisibleInOperatorShell(
      NAV_GROUPS,
      false,
      false,
      AUTHORITY_RANK.ReadAuthority,
      false,
      "all",
      true,
    );
    let c = 0;
    let f = 0;

    for (const row of collapsed) {
      c += row.visibleLinks.length;
    }

    for (const row of full) {
      f += row.visibleLinks.length;
    }

    expect(f).toBeGreaterThanOrEqual(c);
    expect(countSidebarLinksHiddenByCollapsedPilot(NAV_GROUPS, false, false, AUTHORITY_RANK.ReadAuthority, true)).toBe(
      f - c,
    );
  });
});

describe("filterNavLinksForOperatorShell — public demo nav omissions", () => {
  const enterprise = NAV_GROUPS.find((g) => g.id === "operate-governance");
  const prevDemo = process.env.NEXT_PUBLIC_DEMO_MODE;

  afterEach(() => {
    if (prevDemo === undefined) {
      delete process.env.NEXT_PUBLIC_DEMO_MODE;
      return;
    }

    process.env.NEXT_PUBLIC_DEMO_MODE = prevDemo;
  });

  it("hides alerts, audit, and admin health while keeping Security & trust when NEXT_PUBLIC_DEMO_MODE is true", () => {
    expect(enterprise).toBeDefined();
    process.env.NEXT_PUBLIC_DEMO_MODE = "true";

    const visible = filterNavLinksForOperatorShell(
      enterprise!.links,
      true,
      true,
      AUTHORITY_RANK.AdminAuthority,
      false,
      true,
    );

    expect(visible.some((l) => l.href === "/alerts")).toBe(false);
    expect(visible.some((l) => l.href === "/audit")).toBe(false);
    expect(visible.some((l) => l.href === "/admin/health")).toBe(false);
    expect(visible.some((l) => l.href === "/workspace/security-trust")).toBe(true);
  });

  it("hides operator-admin links including system health when NEXT_PUBLIC_DEMO_MODE is true", () => {
    const admin = NAV_GROUPS.find((g) => g.id === "operator-admin");

    expect(admin).toBeDefined();
    process.env.NEXT_PUBLIC_DEMO_MODE = "true";

    const visible = filterNavLinksForOperatorShell(
      admin!.links,
      true,
      true,
      AUTHORITY_RANK.AdminAuthority,
      false,
      true,
    );

    expect(visible.some((l) => l.href === "/admin/health")).toBe(false);
    expect(visible.some((l) => l.href === "/admin/users")).toBe(false);
  });
});

describe("listNavGroupsVisibleInOperatorShell — platform-admin surface", () => {
  it("returns only operator-admin when surfaceFilter is platform-admin", () => {
    const rows = listNavGroupsVisibleInOperatorShell(
      NAV_GROUPS,
      true,
      true,
      AUTHORITY_RANK.AdminAuthority,
      false,
      "platform-admin",
      true,
    );

    expect(rows.map((r) => r.group.id)).toEqual(["operator-admin"]);
    expect(rows[0]!.visibleLinks.some((l) => l.href === "/admin/health")).toBe(true);
  });
});

describe("committed architecture review gate — operator shell composition", () => {
  it("narrows to Architecture reviews essentials until first committed review even at Admin + full tier disclosure", () => {
    const rows = listNavGroupsVisibleInOperatorShell(
      NAV_GROUPS,
      true,
      true,
      AUTHORITY_RANK.AdminAuthority,
      false,
      "all",
      false,
    );

    expect(rows.map((r) => r.group.id)).toEqual(["pilot"]);
    expect(rows[0]!.visibleLinks.map((l) => l.href)).toEqual(["/", "/reviews/new", "/reviews?projectId=default"]);
  });
});
