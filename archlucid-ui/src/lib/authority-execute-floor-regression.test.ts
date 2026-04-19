import { describe, expect, it } from "vitest";

import { NAV_GROUPS } from "@/lib/nav-config";
import { enterpriseMutationCapabilityFromRank } from "@/lib/enterprise-mutation-capability";
import {
  AUTHORITY_RANK,
  filterNavLinksByAuthority,
  navLinkVisibleForCallerRank,
} from "@/lib/nav-authority";

/**
 * **Purpose:** narrow regression on the **Execute numeric floor** shared by **`navLinkVisibleForCallerRank`** (for
 * **`ExecuteAuthority`** links) and **`enterpriseMutationCapabilityFromRank`** / **`useEnterpriseMutationCapability()`**.
 * **UI shaping only — API authoritative:** tests never imply POST success; **`[Authorize(Policy = …)]`** on **ArchLucid.Api**
 * still returns **401/403** on deep links.
 *
 * **Packaging:** **docs/PRODUCT_PACKAGING.md** §3 *Read vs Execute in the UI* and *Contributor drift guard*; Stage 1:
 * **docs/COMMERCIAL_BOUNDARY_HARDENING_SEQUENCE.md** §4. Broader tier∩rank coverage lives in **`authority-seam-regression.test.ts`**.
 */

/** Synthetic Execute-tier row matching Enterprise workflow semantics (`nav-config` doc block). */
const executeTierNavLink = {
  href: "/synthetic-governance-workflow",
  label: "Workflow",
  title: "",
  tier: "essential" as const,
  requiredAuthority: "ExecuteAuthority" as const,
};

/**
 * Single numeric floor for **Execute-class nav visibility** and **`useEnterpriseMutationCapability`** (LayerHeader uses
 * the same comparison). If these diverge, Reader shells could show workflow while buttons soft-enable—or the inverse.
 */
describe("authority Execute floor regression", () => {
  it("matches enterpriseMutationCapabilityFromRank for an ExecuteAuthority nav row across representative ranks", () => {
    const ranks = [0, AUTHORITY_RANK.ReadAuthority, AUTHORITY_RANK.ExecuteAuthority, AUTHORITY_RANK.AdminAuthority];

    for (const rank of ranks) {
      const visible = navLinkVisibleForCallerRank(executeTierNavLink, rank);
      const mutate = enterpriseMutationCapabilityFromRank(rank);

      expect(visible).toBe(mutate);
    }
  });

  /**
   * Enterprise Controls links widen monotonically with caller rank when only `filterNavLinksByAuthority` applies
   * (tier-agnostic). Catches accidental `requiredAuthority` edits that hide Read-tier inbox surfaces from Readers.
   */
  it("keeps Enterprise Controls nav link count non-decreasing Read → Execute → Admin under authority filter alone", () => {
    const enterprise = NAV_GROUPS.find((g) => g.id === "alerts-governance");

    expect(enterprise).toBeDefined();

    const readCount = filterNavLinksByAuthority(enterprise!.links, AUTHORITY_RANK.ReadAuthority).length;
    const executeCount = filterNavLinksByAuthority(enterprise!.links, AUTHORITY_RANK.ExecuteAuthority).length;
    const adminCount = filterNavLinksByAuthority(enterprise!.links, AUTHORITY_RANK.AdminAuthority).length;

    expect(readCount).toBeLessThanOrEqual(executeCount);
    expect(executeCount).toBeLessThanOrEqual(adminCount);
  });

  /** Doc-seam guard: workflow stays Execute-gated in config so Reader nav does not advertise it (`nav-config` header). */
  it("hides governance workflow href from Reader-filtered Enterprise links while inbox remains", () => {
    const enterprise = NAV_GROUPS.find((g) => g.id === "alerts-governance");

    expect(enterprise).toBeDefined();

    const readHrefs = filterNavLinksByAuthority(enterprise!.links, AUTHORITY_RANK.ReadAuthority).map((l) => l.href);

    expect(readHrefs).toContain("/alerts");
    expect(readHrefs.some((h) => h === "/governance")).toBe(false);
  });
});
