import { NextRequest, NextResponse } from "next/server";
import {
  CORRELATION_ID_HEADER,
  generateCorrelationId,
  isSafeCorrelationId,
} from "@/lib/correlation";
import { resolveUpstreamApiBaseUrlForProxy } from "@/lib/config";
import { declaredPostBodyExceedsLimit, readRequestBodyWithLimit } from "@/lib/proxy-body-read";
import { PROXY_MAX_BODY_BYTES } from "@/lib/proxy-constants";
import { enforceProxyRateLimit } from "@/lib/proxy-rate-limit";
import { getScopeHeaders } from "@/lib/scope";

/**
 * Builds headers for the upstream C# API request.
 * Attaches API key, forwards browser Authorization header, and merges scope headers
 * (uses browser-provided scope values if present, otherwise falls back to dev defaults).
 */
function buildUpstreamHeaders(request: NextRequest): Headers {
  const h = new Headers();
  const key =
    process.env.ARCHLUCID_API_KEY?.trim() ||
    process.env.ARCHIFORGE_API_KEY?.trim() ||
    "";
  const authHeader = request.headers.get("authorization");
  const bearer = authHeader?.trim() ?? "";
  const hasBearer = bearer.length > 0;

  if (key && !hasBearer) {
    h.set("X-Api-Key", key);
  }

  if (hasBearer) {
    h.set("Authorization", bearer);
  }
  for (const [k, v] of Object.entries(getScopeHeaders())) {
    const incoming = request.headers.get(k);
    h.set(k, incoming && incoming.trim().length > 0 ? incoming : v);
  }

  const incomingCorrelation = request.headers.get(CORRELATION_ID_HEADER);
  const correlationId =
    incomingCorrelation !== null &&
    incomingCorrelation !== undefined &&
    isSafeCorrelationId(incomingCorrelation)
      ? incomingCorrelation.trim()
      : generateCorrelationId();
  h.set(CORRELATION_ID_HEADER, correlationId);

  return h;
}

/** One-line JSON for operators scraping UI server logs (no response bodies). */
function logProxyDiagnostic(event: string, fields: Record<string, string | number>): void {
  console.warn(JSON.stringify({ component: "archiforge-ui-proxy", event, ...fields }));
}

/** Forwards a request to the upstream ArchiForge API, preserving query string and method. */
async function forward(
  request: NextRequest,
  pathSegments: string[],
  method: "GET" | "POST",
): Promise<NextResponse> {
  const resolved = resolveUpstreamApiBaseUrlForProxy();

  if (!resolved.ok) {
    logProxyDiagnostic("upstream_config_invalid", { detail: resolved.detail });
    return NextResponse.json(
      {
        type: "about:blank",
        title: "Invalid upstream API configuration",
        status: 503,
        detail: resolved.detail,
        supportHint:
          "Set ARCHIFORGE_API_BASE_URL in archiforge-ui/.env.local to the API root (e.g. http://localhost:5128). Restart the dev server after editing.",
      },
      { status: 503 },
    );
  }

  const base = resolved.baseUrl;
  const path = pathSegments.length > 0 ? pathSegments.join("/") : "";
  const search = request.nextUrl.search;
  const targetUrl = `${base}/${path}${search}`;
  const pathForLog = path.length > 0 ? path : "_";

  const headers = buildUpstreamHeaders(request);
  if (method === "POST") {
    const tooLargeByHeader = declaredPostBodyExceedsLimit(
      request.headers.get("content-length"),
      PROXY_MAX_BODY_BYTES,
    );

    if (tooLargeByHeader !== false) {
      logProxyDiagnostic("body_too_large", {
        method,
        path: pathForLog,
        declaredLength: tooLargeByHeader.declaredLength,
        maxBytes: PROXY_MAX_BODY_BYTES,
      });
      return NextResponse.json(
        {
          type: "about:blank",
          title: "Payload too large",
          status: 413,
          detail: `Request body (${tooLargeByHeader.declaredLength} bytes) exceeds the proxy limit of ${PROXY_MAX_BODY_BYTES} bytes.`,
        },
        { status: 413 },
      );
    }

    const contentType = request.headers.get("content-type");
    if (contentType) headers.set("Content-Type", contentType);

    const body = await readRequestBodyWithLimit(request.body, PROXY_MAX_BODY_BYTES);

    if (body === null) {
      logProxyDiagnostic("body_too_large_streaming", {
        method,
        path: pathForLog,
        maxBytes: PROXY_MAX_BODY_BYTES,
      });
      return NextResponse.json(
        {
          type: "about:blank",
          title: "Payload too large",
          status: 413,
          detail: `Request body exceeded the proxy limit of ${PROXY_MAX_BODY_BYTES} bytes during streaming read.`,
        },
        { status: 413 },
      );
    }

    let res: Response;
    try {
      res = await fetch(targetUrl, {
        method: "POST",
        headers,
        body,
        cache: "no-store",
      });
    } catch (err) {
      const message = err instanceof Error ? err.message : String(err);
      logProxyDiagnostic("upstream_fetch_failed", {
        method,
        path: pathForLog,
        message,
      });
      return NextResponse.json(
        {
          type: "about:blank",
          title: "Upstream API unreachable",
          status: 502,
          detail: message,
          supportHint:
            "Confirm the ArchiForge API is running and reachable from this machine. Check ARCHIFORGE_API_BASE_URL and see docs/TROUBLESHOOTING.md.",
        },
        { status: 502 },
      );
    }

    if (!res.ok) {
      logProxyDiagnostic("upstream_non_success", {
        method,
        path: pathForLog,
        status: res.status,
      });
    }

    return passThrough(res);
  }

  let res: Response;
  try {
    res = await fetch(targetUrl, {
      method: "GET",
      headers,
      cache: "no-store",
    });
  } catch (err) {
    const message = err instanceof Error ? err.message : String(err);
    logProxyDiagnostic("upstream_fetch_failed", {
      method,
      path: pathForLog,
      message,
    });
    return NextResponse.json(
      {
        type: "about:blank",
        title: "Upstream API unreachable",
        status: 502,
        detail: message,
        supportHint:
          "Confirm the ArchiForge API is running and reachable from this machine. Check ARCHIFORGE_API_BASE_URL and see docs/TROUBLESHOOTING.md.",
      },
      { status: 502 },
    );
  }

  if (!res.ok) {
    logProxyDiagnostic("upstream_non_success", {
      method,
      path: pathForLog,
      status: res.status,
    });
  }

  return passThrough(res);
}

/** Passes the upstream response body and key headers (Content-Type, Content-Disposition) to the browser. */
function passThrough(res: Response): NextResponse {
  const out = new NextResponse(res.body, { status: res.status });

  const contentType = res.headers.get("content-type");
  if (contentType) out.headers.set("Content-Type", contentType);

  const disposition = res.headers.get("content-disposition");
  if (disposition) out.headers.set("Content-Disposition", disposition);

  const correlation = res.headers.get(CORRELATION_ID_HEADER);
  if (correlation && correlation.trim().length > 0) {
    out.headers.set(CORRELATION_ID_HEADER, correlation.trim());
  }

  return out;
}

/** Handles GET requests from browser components → forwards to C# API with server-side credentials. */
export async function GET(
  request: NextRequest,
  context: { params: Promise<{ path: string[] }> },
) {
  const rateLimited = enforceProxyRateLimit(request);

  if (rateLimited) {
    return rateLimited;
  }

  const { path } = await context.params;
  return forward(request, path ?? [], "GET");
}

/** Handles POST requests from browser components → forwards to C# API with server-side credentials. */
export async function POST(
  request: NextRequest,
  context: { params: Promise<{ path: string[] }> },
) {
  const rateLimited = enforceProxyRateLimit(request);

  if (rateLimited) {
    return rateLimited;
  }

  const { path } = await context.params;
  return forward(request, path ?? [], "POST");
}
