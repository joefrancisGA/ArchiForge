import { NextRequest, NextResponse } from "next/server";

type WindowEntry = {
  count: number;
  windowStartMs: number;
};

const buckets = new Map<string, WindowEntry>();

/** Avoid unbounded memory if many unique client keys appear. */
const MAX_BUCKET_KEYS = 10_000;

/** Clears in-process counters (Vitest only). */
export function resetProxyRateLimitStateForTests(): void {
  buckets.clear();
}

function isRateLimitDisabled(): boolean {
  const v = process.env.ARCHIFORGE_PROXY_RATE_LIMIT_DISABLED?.trim().toLowerCase();

  return v === "1" || v === "true" || v === "yes";
}

function maxRequestsPerWindow(): number {
  const raw = process.env.ARCHIFORGE_PROXY_RATE_LIMIT_PER_MINUTE?.trim();

  if (raw === undefined || raw === "") {
    return 120;
  }

  const n = Number(raw);

  if (!Number.isFinite(n) || n < 1) {
    return 120;
  }

  return Math.floor(n);
}

function windowDurationMs(): number {
  const raw = process.env.ARCHIFORGE_PROXY_RATE_LIMIT_WINDOW_MS?.trim();

  if (raw === undefined || raw === "") {
    return 60_000;
  }

  const n = Number(raw);

  if (!Number.isFinite(n) || n < 1000) {
    return 60_000;
  }

  return Math.floor(n);
}

/**
 * Best-effort client key for per-process rate limiting (first X-Forwarded-For hop, then X-Real-Ip).
 * When unknown, all traffic shares one bucket — conservative under abuse.
 */
export function proxyRateLimitClientKey(request: NextRequest): string {
  const forwarded = request.headers.get("x-forwarded-for");

  if (forwarded) {
    const first = forwarded.split(",")[0]?.trim();

    if (first && first.length > 0) {
      return first;
    }
  }

  const realIp = request.headers.get("x-real-ip")?.trim();

  if (realIp && realIp.length > 0) {
    return realIp;
  }

  const fromNext = request.headers.get("x-vercel-forwarded-for")?.trim();

  if (fromNext && fromNext.length > 0) {
    return fromNext.split(",")[0]?.trim() ?? "unknown";
  }

  return "unknown";
}

function pruneStaleEntries(nowMs: number, windowMs: number): void {
  if (buckets.size <= MAX_BUCKET_KEYS) {
    return;
  }

  for (const [key, entry] of buckets) {
    if (nowMs - entry.windowStartMs >= windowMs) {
      buckets.delete(key);
    }
  }
}

/**
 * Fixed-window counter per client key (in-process). Safe for single-instance deployments;
 * multi-instance surfaces see independent windows.
 *
 * @returns `NextResponse` with 429 when limited; `null` when allowed.
 */
export function enforceProxyRateLimit(request: NextRequest): NextResponse | null {
  if (isRateLimitDisabled()) {
    return null;
  }

  const maxRequests = maxRequestsPerWindow();
  const windowMs = windowDurationMs();
  const nowMs = Date.now();
  const key = proxyRateLimitClientKey(request);

  pruneStaleEntries(nowMs, windowMs);

  const entry = buckets.get(key);

  if (!entry || nowMs - entry.windowStartMs >= windowMs) {
    buckets.set(key, { count: 1, windowStartMs: nowMs });
    return null;
  }

  if (entry.count < maxRequests) {
    entry.count++;
    return null;
  }

  const retryAfterSec = Math.max(1, Math.ceil((entry.windowStartMs + windowMs - nowMs) / 1000));

  return NextResponse.json(
    {
      type: "about:blank",
      title: "Too many requests",
      status: 429,
      detail: `Too many requests through the operator proxy. Try again in ${retryAfterSec} second(s).`,
    },
    { status: 429, headers: { "Retry-After": String(retryAfterSec) } },
  );
}
