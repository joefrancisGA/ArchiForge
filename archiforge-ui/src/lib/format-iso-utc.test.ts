import { describe, expect, it } from "vitest";
import { formatIsoUtcForDisplay } from "./format-iso-utc";

describe("formatIsoUtcForDisplay", () => {
  it("includes UTC in the formatted label for a valid ISO string", () => {
    const s = formatIsoUtcForDisplay("2024-06-01T12:00:00.000Z");
    expect(s).toContain("UTC");
  });

  it("returns the original string when parsing fails", () => {
    expect(formatIsoUtcForDisplay("not-a-date")).toBe("not-a-date");
  });
});
