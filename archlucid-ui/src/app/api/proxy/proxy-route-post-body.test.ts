import { NextRequest } from "next/server";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { POST } from "./[...path]/route";
import { PROXY_MAX_BODY_BYTES } from "@/lib/proxy-constants";

describe("POST /api/proxy/[...path] body limits", () => {
  const fetchMock = vi.fn();

  beforeEach(() => {
    fetchMock.mockResolvedValue(new Response("{}", { status: 200, headers: { "Content-Type": "application/json" } }));
    vi.stubGlobal("fetch", fetchMock);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it("returns 413 when Content-Length declares a body larger than the cap", async () => {
    const req = new NextRequest("http://localhost/api/proxy/health/live", {
      method: "POST",
      headers: {
        "content-type": "application/json",
        "content-length": String(PROXY_MAX_BODY_BYTES + 1),
      },
      body: "{}",
    });

    const res = await POST(req, { params: Promise.resolve({ path: ["api", "health"] }) });

    expect(res.status).toBe(413);
    const json: unknown = await res.json();
    expect(json).toMatchObject({ title: "Payload too large", status: 413 });
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it("returns 413 when streamed body exceeds the cap", async () => {
    const oversized = new Uint8Array(PROXY_MAX_BODY_BYTES + 1);
    const req = new NextRequest("http://localhost/api/proxy/health/live", {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: oversized,
    });

    const res = await POST(req, { params: Promise.resolve({ path: ["api", "health"] }) });

    expect(res.status).toBe(413);
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it("forwards small POST to upstream when within limit", async () => {
    const req = new NextRequest("http://localhost/api/proxy/health/live", {
      method: "POST",
      headers: { "content-type": "application/json", "content-length": "2" },
      body: "{}",
    });

    const res = await POST(req, { params: Promise.resolve({ path: ["api", "health"] }) });

    expect(res.status).toBe(200);
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });
});
