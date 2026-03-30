import { afterEach, describe, expect, it, vi } from "vitest";

import { getServerApiBaseUrl } from "./config";

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
