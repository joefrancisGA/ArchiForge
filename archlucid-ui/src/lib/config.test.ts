import { afterEach, describe, expect, it, vi } from "vitest";

import { getServerApiBaseUrl, resolveUpstreamApiBaseUrlForProxy } from "./config";

describe("getServerApiBaseUrl", () => {
  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it("prefers ARCHIFORGE_API_BASE_URL when both server and public vars are set", () => {
    vi.stubEnv("ARCHIFORGE_API_BASE_URL", "https://server.example");
    vi.stubEnv("NEXT_PUBLIC_ARCHIFORGE_API_BASE_URL", "https://public.example");

    expect(getServerApiBaseUrl()).toBe("https://server.example");
  });

  it("uses NEXT_PUBLIC_ARCHIFORGE_API_BASE_URL when the server var is unset", () => {
    vi.stubEnv("ARCHIFORGE_API_BASE_URL", undefined);
    vi.stubEnv("NEXT_PUBLIC_ARCHIFORGE_API_BASE_URL", "https://public-only.example");

    expect(getServerApiBaseUrl()).toBe("https://public-only.example");
  });

  it("falls back to the documented local default when neither var is set", () => {
    vi.stubEnv("ARCHIFORGE_API_BASE_URL", undefined);
    vi.stubEnv("NEXT_PUBLIC_ARCHIFORGE_API_BASE_URL", undefined);

    expect(getServerApiBaseUrl()).toBe("http://localhost:5128");
  });
});

describe("resolveUpstreamApiBaseUrlForProxy", () => {
  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it("returns ok for default localhost URL", () => {
    vi.stubEnv("ARCHIFORGE_API_BASE_URL", undefined);
    vi.stubEnv("NEXT_PUBLIC_ARCHIFORGE_API_BASE_URL", undefined);

    const r = resolveUpstreamApiBaseUrlForProxy();

    expect(r).toEqual({ ok: true, baseUrl: "http://localhost:5128" });
  });

  it("returns failure for non-absolute URL", () => {
    vi.stubEnv("ARCHIFORGE_API_BASE_URL", "not-a-valid-url");

    const r = resolveUpstreamApiBaseUrlForProxy();

    expect(r.ok).toBe(false);
    if (!r.ok) {
      expect(r.detail.length).toBeGreaterThan(10);
    }
  });

  it("returns failure for non-http protocol", () => {
    vi.stubEnv("ARCHIFORGE_API_BASE_URL", "ftp://example.com");

    const r = resolveUpstreamApiBaseUrlForProxy();

    expect(r.ok).toBe(false);
  });
});
