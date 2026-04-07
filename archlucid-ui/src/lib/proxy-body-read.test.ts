import { describe, expect, it } from "vitest";

import { declaredPostBodyExceedsLimit, readRequestBodyWithLimit } from "./proxy-body-read";

describe("declaredPostBodyExceedsLimit", () => {
  it("returns false when header is missing", () => {
    expect(declaredPostBodyExceedsLimit(null, 100)).toBe(false);
  });

  it("returns false when header is empty", () => {
    expect(declaredPostBodyExceedsLimit("   ", 100)).toBe(false);
  });

  it("returns false when header is not a number", () => {
    expect(declaredPostBodyExceedsLimit("chunked", 100)).toBe(false);
  });

  it("returns false when within limit", () => {
    expect(declaredPostBodyExceedsLimit("10", 100)).toBe(false);
  });

  it("returns false when equal to limit", () => {
    expect(declaredPostBodyExceedsLimit("100", 100)).toBe(false);
  });

  it("returns declared length when over limit", () => {
    expect(declaredPostBodyExceedsLimit("101", 100)).toEqual({ declaredLength: 101 });
  });
});

describe("readRequestBodyWithLimit", () => {
  it("returns empty string when body is null", async () => {
    await expect(readRequestBodyWithLimit(null, 10)).resolves.toBe("");
  });

  it("returns empty string when body is undefined", async () => {
    await expect(readRequestBodyWithLimit(undefined, 10)).resolves.toBe("");
  });

  it("reads full text when under limit", async () => {
    const enc = new TextEncoder();
    const stream = new ReadableStream<Uint8Array>({
      start(controller) {
        controller.enqueue(enc.encode("hello"));
        controller.close();
      },
    });

    await expect(readRequestBodyWithLimit(stream, 100)).resolves.toBe("hello");
  });

  it("returns null when first chunk exceeds limit", async () => {
    const stream = new ReadableStream<Uint8Array>({
      start(controller) {
        controller.enqueue(new Uint8Array(5));
        controller.close();
      },
    });

    await expect(readRequestBodyWithLimit(stream, 4)).resolves.toBeNull();
  });

  it("returns null when cumulative chunks exceed limit", async () => {
    const stream = new ReadableStream<Uint8Array>({
      start(controller) {
        controller.enqueue(new Uint8Array(3));
        controller.enqueue(new Uint8Array(3));
        controller.close();
      },
    });

    await expect(readRequestBodyWithLimit(stream, 5)).resolves.toBeNull();
  });
});
