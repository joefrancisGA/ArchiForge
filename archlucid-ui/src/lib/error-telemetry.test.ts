import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

describe("error-telemetry", () => {
  beforeEach(() => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        status: 204,
      }),
    );
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.unstubAllEnvs();
    vi.resetModules();
  });

  it("posts client errors when not in development", async () => {
    vi.stubEnv("NODE_ENV", "test");
    const { reportClientError } = await import("@/lib/error-telemetry");

    reportClientError(new Error("unit probe"), { source: "test" });

    await vi.waitFor(() => {
      expect(fetch).toHaveBeenCalled();
    });

    const call = vi.mocked(fetch).mock.calls[0];
    expect(call[0]).toBe("/api/proxy/v1/diagnostics/client-error");
    expect(call[1]?.method).toBe("POST");
    const body = JSON.parse(String(call[1]?.body));
    expect(body.message).toBe("unit probe");
    expect(body.context).toEqual({ source: "test" });
  });

  it("does not post in development", async () => {
    vi.stubEnv("NODE_ENV", "development");
    const { reportClientError } = await import("@/lib/error-telemetry");

    reportClientError(new Error("ignored"));

    await new Promise((r) => {
      setTimeout(r, 30);
    });
    expect(fetch).not.toHaveBeenCalled();
  });

  it("reports 5xx ApiRequestError via maybeReportApiServerErrorFromUnknown", async () => {
    vi.stubEnv("NODE_ENV", "test");
    const { ApiRequestError } = await import("@/lib/api-request-error");
    const err = new ApiRequestError("boom", {
      problem: null,
      correlationId: "cid-1",
      httpStatus: 503,
    });
    const { maybeReportApiServerErrorFromUnknown } = await import("@/lib/error-telemetry");

    maybeReportApiServerErrorFromUnknown(err);

    await vi.waitFor(() => {
      expect(fetch).toHaveBeenCalled();
    });

    const call = vi.mocked(fetch).mock.calls[0];
    const body = JSON.parse(String(call[1]?.body));
    expect(body.message).toContain("503");
    expect(body.context?.correlationId).toBe("cid-1");
  });

  it("ignores non-5xx ApiRequestError", async () => {
    vi.stubEnv("NODE_ENV", "test");
    const { ApiRequestError } = await import("@/lib/api-request-error");
    const err = new ApiRequestError("nope", {
      problem: null,
      correlationId: null,
      httpStatus: 404,
    });
    const { maybeReportApiServerErrorFromUnknown } = await import("@/lib/error-telemetry");

    maybeReportApiServerErrorFromUnknown(err);

    await new Promise((r) => {
      setTimeout(r, 30);
    });
    expect(fetch).not.toHaveBeenCalled();
  });
});
