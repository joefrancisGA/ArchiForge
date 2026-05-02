import { afterEach, describe, expect, it, vi } from "vitest";

import { parseRetryAfterHeader } from "./retry-after";

describe("parseRetryAfterHeader", () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  it("returns null for null or blank", () => {
    expect(parseRetryAfterHeader(null)).toBe(null);
    expect(parseRetryAfterHeader("")).toBe(null);
    expect(parseRetryAfterHeader("   ")).toBe(null);
  });

  it("parses non-negative integer seconds", () => {
    expect(parseRetryAfterHeader("0")).toBe(0);
    expect(parseRetryAfterHeader("120")).toBe(120);
  });

  it("rejects non-integer numeric strings", () => {
    expect(parseRetryAfterHeader("12.5")).toBe(null);
    expect(parseRetryAfterHeader("abc")).toBe(null);
  });

  it("parses HTTP-date into positive seconds from now", () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-05-01T12:00:00.000Z"));

    expect(parseRetryAfterHeader("Fri, 01 May 2026 12:02:30 GMT")).toBe(150);
  });
});
