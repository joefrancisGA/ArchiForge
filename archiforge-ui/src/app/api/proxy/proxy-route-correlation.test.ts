import { NextRequest } from "next/server";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { GET, POST } from "./[...path]/route";
import { CORRELATION_ID_HEADER } from "@/lib/correlation";
import * as correlation from "@/lib/correlation";
import { resetProxyRateLimitStateForTests } from "@/lib/proxy-rate-limit";

describe("proxy route X-Correlation-ID", () => {
  const fetchMock = vi.fn();

  beforeEach(() => {
    resetProxyRateLimitStateForTests();
    fetchMock.mockResolvedValue(
      new Response("{}", {
        status: 200,
        headers: {
          "Content-Type": "application/json",
          [CORRELATION_ID_HEADER]: "upstream-returned-id",
        },
      }),
    );
    vi.stubGlobal("fetch", fetchMock);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it("forwards safe browser correlation id on upstream fetch (GET)", async () => {
    const browserId = "aaaaaaaa-bbbb-4ccc-dddd-eeeeeeeeeeee";
    const req = new NextRequest(`http://localhost/api/proxy/api/health`, {
      headers: { [CORRELATION_ID_HEADER]: browserId },
    });

    const res = await GET(req, { params: Promise.resolve({ path: ["api", "health"] }) });

    expect(res.status).toBe(200);
    expect(fetchMock).toHaveBeenCalledTimes(1);
    const init = fetchMock.mock.calls[0]![1] as RequestInit;
    const headers = init.headers as Headers;
    expect(headers.get(CORRELATION_ID_HEADER)).toBe(browserId);
    expect(res.headers.get(CORRELATION_ID_HEADER)).toBe("upstream-returned-id");
  });

  it("generates correlation id when browser value is unsafe", async () => {
    const genSpy = vi.spyOn(correlation, "generateCorrelationId").mockReturnValue("generated-fixed-id");

    const req = new NextRequest(`http://localhost/api/proxy/api/health`, {
      headers: { [CORRELATION_ID_HEADER]: "has spaces" },
    });

    await GET(req, { params: Promise.resolve({ path: ["api", "health"] }) });

    expect(genSpy).toHaveBeenCalled();
    const init = fetchMock.mock.calls[0]![1] as RequestInit;
    const headers = init.headers as Headers;
    expect(headers.get(CORRELATION_ID_HEADER)).toBe("generated-fixed-id");
  });

  it("forwards correlation id on POST", async () => {
    const browserId = "bbbbbbbb-bbbb-4bbb-bbbb-bbbbbbbbbbbb";
    const req = new NextRequest(`http://localhost/api/proxy/api/health`, {
      method: "POST",
      headers: {
        "content-type": "application/json",
        "content-length": "2",
        [CORRELATION_ID_HEADER]: browserId,
      },
      body: "{}",
    });

    await POST(req, { params: Promise.resolve({ path: ["api", "health"] }) });

    const init = fetchMock.mock.calls[0]![1] as RequestInit;
    const headers = init.headers as Headers;
    expect(headers.get(CORRELATION_ID_HEADER)).toBe(browserId);
  });
});
