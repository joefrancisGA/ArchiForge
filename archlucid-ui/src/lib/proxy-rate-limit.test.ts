import { NextRequest } from "next/server";
import { afterEach, beforeEach, describe, expect, it } from "vitest";

import {
  enforceProxyRateLimit,
  proxyRateLimitClientKey,
  resetProxyRateLimitStateForTests,
} from "./proxy-rate-limit";

describe("proxyRateLimitClientKey", () => {
  it("uses first X-Forwarded-For hop", () => {
    const req = new NextRequest("http://localhost/api/proxy/x", {
      headers: { "x-forwarded-for": " 203.0.113.1 , 10.0.0.1 " },
    });
    expect(proxyRateLimitClientKey(req)).toBe("203.0.113.1");
  });

  it("falls back to X-Real-Ip", () => {
    const req = new NextRequest("http://localhost/api/proxy/x", {
      headers: { "x-real-ip": "198.51.100.2" },
    });
    expect(proxyRateLimitClientKey(req)).toBe("198.51.100.2");
  });

  it("returns unknown when no proxy headers", () => {
    const req = new NextRequest("http://localhost/api/proxy/x");
    expect(proxyRateLimitClientKey(req)).toBe("unknown");
  });
});

describe("enforceProxyRateLimit", () => {
  const savedEnv: Record<string, string | undefined> = {};

  beforeEach(() => {
    savedEnv.DISABLED = process.env.ARCHIFORGE_PROXY_RATE_LIMIT_DISABLED;
    savedEnv.PER_MIN = process.env.ARCHIFORGE_PROXY_RATE_LIMIT_PER_MINUTE;
    savedEnv.WINDOW = process.env.ARCHIFORGE_PROXY_RATE_LIMIT_WINDOW_MS;
    delete process.env.ARCHIFORGE_PROXY_RATE_LIMIT_DISABLED;
    process.env.ARCHIFORGE_PROXY_RATE_LIMIT_PER_MINUTE = "3";
    process.env.ARCHIFORGE_PROXY_RATE_LIMIT_WINDOW_MS = "60000";
    resetProxyRateLimitStateForTests();
  });

  afterEach(() => {
    restoreEnv("ARCHIFORGE_PROXY_RATE_LIMIT_DISABLED", savedEnv.DISABLED);
    restoreEnv("ARCHIFORGE_PROXY_RATE_LIMIT_PER_MINUTE", savedEnv.PER_MIN);
    restoreEnv("ARCHIFORGE_PROXY_RATE_LIMIT_WINDOW_MS", savedEnv.WINDOW);
    resetProxyRateLimitStateForTests();
  });

  it("allows requests under the cap", () => {
    const req = new NextRequest("http://localhost/api/proxy/x", {
      headers: { "x-forwarded-for": "10.0.0.50" },
    });

    expect(enforceProxyRateLimit(req)).toBeNull();
    expect(enforceProxyRateLimit(req)).toBeNull();
    expect(enforceProxyRateLimit(req)).toBeNull();
  });

  it("returns 429 with Retry-After when over the cap", () => {
    const req = new NextRequest("http://localhost/api/proxy/x", {
      headers: { "x-forwarded-for": "10.0.0.51" },
    });

    expect(enforceProxyRateLimit(req)).toBeNull();
    expect(enforceProxyRateLimit(req)).toBeNull();
    expect(enforceProxyRateLimit(req)).toBeNull();
    const fourth = enforceProxyRateLimit(req);

    expect(fourth).not.toBeNull();
    expect(fourth!.status).toBe(429);
    expect(fourth!.headers.get("Retry-After")).toMatch(/^\d+$/);
  });

  it("no-ops when ARCHIFORGE_PROXY_RATE_LIMIT_DISABLED is true", () => {
    process.env.ARCHIFORGE_PROXY_RATE_LIMIT_DISABLED = "true";
    const req = new NextRequest("http://localhost/api/proxy/x", {
      headers: { "x-forwarded-for": "10.0.0.52" },
    });

    for (let i = 0; i < 10; i++) {
      expect(enforceProxyRateLimit(req)).toBeNull();
    }
  });
});

function restoreEnv(key: string, value: string | undefined): void {
  if (value === undefined) {
    delete process.env[key];
  } else {
    process.env[key] = value;
  }
}
