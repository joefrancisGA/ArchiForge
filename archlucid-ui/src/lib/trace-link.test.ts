import { afterEach, describe, expect, it, vi } from "vitest";

import { buildTraceViewerUrl } from "./trace-link";

describe("buildTraceViewerUrl", () => {
  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it("buildTraceViewerUrl_withTemplate_replacesTraceId", () => {
    vi.stubEnv(
      "NEXT_PUBLIC_TRACE_VIEWER_URL_TEMPLATE",
      "https://jaeger.example.com/trace/{traceId}",
    );

    expect(buildTraceViewerUrl("a1b2c3d4e5f678901234567890abcdef")).toBe(
      "https://jaeger.example.com/trace/a1b2c3d4e5f678901234567890abcdef",
    );
  });

  it("buildTraceViewerUrl_noTemplate_returnsNull", () => {
    vi.stubEnv("NEXT_PUBLIC_TRACE_VIEWER_URL_TEMPLATE", "");

    expect(buildTraceViewerUrl("abc")).toBeNull();
  });

  it("buildTraceViewerUrl_noTraceId_returnsNull", () => {
    vi.stubEnv(
      "NEXT_PUBLIC_TRACE_VIEWER_URL_TEMPLATE",
      "https://jaeger.example.com/trace/{traceId}",
    );

    expect(buildTraceViewerUrl(null)).toBeNull();
    expect(buildTraceViewerUrl(undefined)).toBeNull();
    expect(buildTraceViewerUrl("")).toBeNull();
  });

  it("buildTraceViewerUrl_encodesSpecialCharacters", () => {
    vi.stubEnv(
      "NEXT_PUBLIC_TRACE_VIEWER_URL_TEMPLATE",
      "https://example.com/q?trace={traceId}&x=1",
    );

    expect(buildTraceViewerUrl("id/with spaces")).toBe(
      "https://example.com/q?trace=id%2Fwith%20spaces&x=1",
    );
  });

  it("replaces every placeholder when the template repeats", () => {
    vi.stubEnv(
      "NEXT_PUBLIC_TRACE_VIEWER_URL_TEMPLATE",
      "https://x.example/{traceId}/y/{traceId}",
    );

    expect(buildTraceViewerUrl("abc")).toBe("https://x.example/abc/y/abc");
  });
});
