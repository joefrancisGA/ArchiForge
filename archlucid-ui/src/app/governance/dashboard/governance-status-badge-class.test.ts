import { describe, expect, it } from "vitest";

import { governanceStatusBadgeClass } from "./governance-status-badge-class";

describe("governanceStatusBadgeClass", () => {
  it("maps known statuses to colored badge classes", () => {
    expect(governanceStatusBadgeClass("Submitted")).toContain("bg-blue-600");
    expect(governanceStatusBadgeClass("Approved")).toContain("bg-emerald-600");
    expect(governanceStatusBadgeClass("Rejected")).toContain("bg-red-600");
    expect(governanceStatusBadgeClass("Promoted")).toContain("bg-violet-600");
    expect(governanceStatusBadgeClass("Activated")).toContain("bg-teal-600");
  });

  it("uses neutral styling for Draft and unknown statuses", () => {
    expect(governanceStatusBadgeClass("Draft")).toContain("bg-oklch");
    expect(governanceStatusBadgeClass("Unknown")).toContain("bg-oklch");
  });
});
