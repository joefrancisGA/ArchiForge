import { describe, expect, it } from "vitest";

import { formatInstantForLocale } from "@/lib/locale-datetime";

describe("formatInstantForLocale", () => {
  it("returns locale string for valid ISO input", () => {
    const s = formatInstantForLocale("2026-01-15T14:30:00.000Z");

    expect(s).not.toMatch(/invalid/i);
    expect(s.length).toBeGreaterThan(4);
  });

  it("returns em dash for empty or invalid input", () => {
    expect(formatInstantForLocale("")).toBe("—");
    expect(formatInstantForLocale("not-a-date")).toBe("—");
    expect(formatInstantForLocale(null)).toBe("—");
    expect(formatInstantForLocale(undefined)).toBe("—");
  });
});
