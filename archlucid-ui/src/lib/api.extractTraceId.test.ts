import { describe, expect, it } from "vitest";

import { extractTraceId } from "./api";

describe("extractTraceId", () => {
  it("returns the X-Trace-Id header value", () => {
    const headers = new Headers();
    headers.set("X-Trace-Id", "a1b2c3d4e5f678901234567890abcdef");

    const response = new Response(null, { headers });

    expect(extractTraceId(response)).toBe("a1b2c3d4e5f678901234567890abcdef");
  });

  it("returns null when the header is absent", () => {
    const response = new Response(null, { status: 200 });

    expect(extractTraceId(response)).toBeNull();
  });
});
