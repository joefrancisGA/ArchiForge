import { describe, expect, it } from "vitest";

import { NAV_GROUPS } from "@/lib/nav-config";
import {
  pathnameEligibleBeforeFirstCommittedArchitectureReview,
  filterNavLinksByCommittedArchitectureReviewGate,
} from "@/lib/nav-committed-architecture-review-gate";

describe("pathnameEligibleBeforeFirstCommittedArchitectureReview", () => {
  it("allows home and reviews surfaces only", () => {
    expect(pathnameEligibleBeforeFirstCommittedArchitectureReview("/")).toBe(true);
    expect(pathnameEligibleBeforeFirstCommittedArchitectureReview("/reviews")).toBe(true);
    expect(pathnameEligibleBeforeFirstCommittedArchitectureReview("/reviews/new")).toBe(true);
    expect(pathnameEligibleBeforeFirstCommittedArchitectureReview("/reviews/abc/def")).toBe(true);
    expect(pathnameEligibleBeforeFirstCommittedArchitectureReview("/governance/findings")).toBe(false);
    expect(pathnameEligibleBeforeFirstCommittedArchitectureReview("/onboarding")).toBe(false);
  });
});

describe("filterNavLinksByCommittedArchitectureReviewGate", () => {
  it("is a no-op when the tenant already committed a review", () => {
    const pilot = NAV_GROUPS.find((g) => g.id === "pilot-planning");
    if (pilot === undefined) {
      throw new Error("nav smoke: missing pilot-planning group");
    }

    const full = filterNavLinksByCommittedArchitectureReviewGate(pilot.links, true);
    expect(full).toEqual([...pilot.links]);
  });

  it("keeps only eligible hrefs when false", () => {
    const pilot = NAV_GROUPS.find((g) => g.id === "pilot-planning");
    if (pilot === undefined) {
      throw new Error("nav smoke: missing pilot-planning group");
    }

    const thin = filterNavLinksByCommittedArchitectureReviewGate(pilot.links, false);
    const hrefs = thin.map((l) => l.href.split("?")[0]);

    expect(thin.length).toBeGreaterThan(0);
    expect(thin.every((l) => pathnameEligibleBeforeFirstCommittedArchitectureReview(l.href.split("?")[0] ?? ""))).toBe(
      true,
    );
    expect(hrefs.every((h) => h === "/" || h.startsWith("/reviews"))).toBe(true);
  });
});
