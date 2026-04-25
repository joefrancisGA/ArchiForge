import { describe, expect, it } from "vitest";

import { formatFindings, formatHours, percentDelta } from "../formatDelta";

describe("formatHours", () => {
  it("converts seconds to hours with two decimal places", () => {
    expect(formatHours(30 * 60)).toBe("0.50 h");
    expect(formatHours(2 * 3600)).toBe("2.00 h");
  });

  it("returns em-dash for null/undefined/non-finite/negative", () => {
    expect(formatHours(null)).toBe("—");
    expect(formatHours(undefined)).toBe("—");
    expect(formatHours(Number.POSITIVE_INFINITY)).toBe("—");
    expect(formatHours(-1)).toBe("—");
  });
});

describe("formatFindings", () => {
  it("formats integers without decimals and non-integers with one decimal", () => {
    expect(formatFindings(0)).toBe("0");
    expect(formatFindings(5)).toBe("5");
    expect(formatFindings(5.5)).toBe("5.5");
  });

  it("returns em-dash for null/undefined/non-finite", () => {
    expect(formatFindings(null)).toBe("—");
    expect(formatFindings(undefined)).toBe("—");
    expect(formatFindings(Number.NaN)).toBe("—");
  });
});

describe("percentDelta", () => {
  it("returns positive percent when current is smaller than prior (improvement)", () => {
    expect(percentDelta(10, 4)).toBeCloseTo(60, 5);
  });

  it("returns negative percent when current is larger than prior (regression)", () => {
    expect(percentDelta(4, 10)).toBeCloseTo(-150, 5);
  });

  it("returns null when prior is zero / negative / null / non-finite", () => {
    expect(percentDelta(0, 5)).toBeNull();
    expect(percentDelta(-5, 5)).toBeNull();
    expect(percentDelta(null, 5)).toBeNull();
    expect(percentDelta(Number.NaN, 5)).toBeNull();
  });
});
