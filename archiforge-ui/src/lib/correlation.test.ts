import { afterEach, describe, expect, it, vi } from "vitest";

import {
  CORRELATION_ID_HEADER,
  generateCorrelationId,
  isSafeCorrelationId,
} from "./correlation";

describe("CORRELATION_ID_HEADER", () => {
  it("matches API middleware header name", () => {
    expect(CORRELATION_ID_HEADER).toBe("X-Correlation-ID");
  });
});

describe("isSafeCorrelationId", () => {
  it("rejects null, undefined, empty, and whitespace-only", () => {
    expect(isSafeCorrelationId(null)).toBe(false);
    expect(isSafeCorrelationId(undefined)).toBe(false);
    expect(isSafeCorrelationId("")).toBe(false);
    expect(isSafeCorrelationId("   ")).toBe(false);
  });

  it("rejects ids longer than 64 characters", () => {
    expect(isSafeCorrelationId("a".repeat(65))).toBe(false);
  });

  it("rejects characters outside allowed set", () => {
    expect(isSafeCorrelationId("abc def")).toBe(false);
    expect(isSafeCorrelationId("a/b")).toBe(false);
    expect(isSafeCorrelationId("x@y")).toBe(false);
  });

  it("accepts UUID and simple alphanumeric with separators", () => {
    expect(isSafeCorrelationId("550e8400-e29b-41d4-a716-446655440000")).toBe(true);
    expect(isSafeCorrelationId("abc-123.X_y")).toBe(true);
  });
});

describe("generateCorrelationId", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("returns a value that passes isSafeCorrelationId", () => {
    const id = generateCorrelationId();

    expect(id.length).toBeGreaterThan(0);
    expect(id.length).toBeLessThanOrEqual(64);
    expect(isSafeCorrelationId(id)).toBe(true);
  });

  it("uses crypto.randomUUID when available", () => {
    vi.stubGlobal("crypto", {
      randomUUID: vi.fn().mockReturnValue("11111111-1111-4111-8111-111111111111"),
      getRandomValues: vi.fn(),
    });

    expect(generateCorrelationId()).toBe("11111111-1111-4111-8111-111111111111");
  });
});
