import { describe, expect, it } from "vitest";

import { hoursSurfaced, formatHours, formatUsd } from "@/lib/roi-assumptions";

describe("hoursSurfaced", () => {
  it("returns zero when all inputs are zero", () => {
    expect(hoursSurfaced({ critical: 0, high: 0, medium: 0 }, 0)).toBe(0);
  });

  it("applies default coefficients", () => {
    const h = hoursSurfaced({ critical: 1, high: 1, medium: 1 }, 2);

    expect(h).toBe(8 + 3 + 1 + 2 * 2);
  });

  it("respects coefficient overrides", () => {
    const h = hoursSurfaced({ critical: 0, high: 0, medium: 2 }, 1, {
      hoursPerCritical: 10,
      hoursPerHigh: 5,
      hoursPerMedium: 1,
      hoursPerPrecommitBlock: 7,
    });

    expect(h).toBe(2 + 7);
  });
});

describe("formatHours", () => {
  it("formats small positives", () => {
    expect(formatHours(1.25)).toBe("1.3 h");
  });

  it("formats zero", () => {
    expect(formatHours(0)).toBe("0 h");
  });
});

describe("formatUsd", () => {
  it("formats USD", () => {
    expect(formatUsd(150)).toMatch(/\$150/);
  });
});
