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
    savedEnv.DISABLED_LUCID = process.env.ARCHLUCID_PROXY_RATE_LIMIT_DISABLED;
    savedEnv.PER_LUCID = process.env.ARCHLUCID_PROXY_RATE_LIMIT_PER_MINUTE;
    savedEnv.WINDOW_LUCID = process.env.ARCHLUCID_PROXY_RATE_LIMIT_WINDOW_MS;
    delete process.env.ARCHLUCID_PROXY_RATE_LIMIT_DISABLED;
    process.env.ARCHLUCID_PROXY_RATE_LIMIT_PER_MINUTE = "3";
    process.env.ARCHLUCID_PROXY_RATE_LIMIT_WINDOW_MS = "60000";
    resetProxyRateLimitStateForTests();
  });

  afterEach(() => {
    restoreEnv("ARCHLUCID_PROXY_RATE_LIMIT_DISABLED", savedEnv.DISABLED_LUCID);
    restoreEnv("ARCHLUCID_PROXY_RATE_LIMIT_PER_MINUTE", savedEnv.PER_LUCID);
    restoreEnv("ARCHLUCID_PROXY_RATE_LIMIT_WINDOW_MS", savedEnv.WINDOW_LUCID);
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

  it("returns 429 with Retry-After when over the cap", async () => {
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
    const headerCid = fourth!.headers.get("X-Correlation-ID");
    expect(headerCid).toBeTruthy();

    const body = (await fourth!.json()) as { correlationId?: string };
    expect(typeof body.correlationId).toBe("string");
    expect(body.correlationId!.length).toBeGreaterThan(0);
    expect(body.correlationId).toBe(headerCid);
  });

  it("no-ops when ARCHLUCID_PROXY_RATE_LIMIT_DISABLED is true", () => {
    process.env.ARCHLUCID_PROXY_RATE_LIMIT_DISABLED = "true";
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
