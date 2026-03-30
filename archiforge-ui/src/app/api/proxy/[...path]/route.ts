import { NextRequest, NextResponse } from "next/server";
import { getServerApiBaseUrl } from "@/lib/config";
import { getScopeHeaders } from "@/lib/scope";

/**
 * Builds headers for the upstream C# API request.
 * Attaches API key, forwards browser Authorization header, and merges scope headers
 * (uses browser-provided scope values if present, otherwise falls back to dev defaults).
 */
function buildUpstreamHeaders(request: NextRequest): Headers {
  const h = new Headers();
  const key = process.env.ARCHIFORGE_API_KEY;
  if (key) h.set("X-Api-Key", key);
  const auth = request.headers.get("authorization");
  if (auth && auth.trim().length > 0) h.set("Authorization", auth);
  for (const [k, v] of Object.entries(getScopeHeaders())) {
    const incoming = request.headers.get(k);
    h.set(k, incoming && incoming.trim().length > 0 ? incoming : v);
  }
  return h;
}

/** Forwards a request to the upstream ArchiForge API, preserving query string and method. */
async function forward(
  request: NextRequest,
  pathSegments: string[],
  method: "GET" | "POST",
): Promise<NextResponse> {
  const base = getServerApiBaseUrl().replace(/\/$/, "");
  const path = pathSegments.length > 0 ? pathSegments.join("/") : "";
  const search = request.nextUrl.search;
  const targetUrl = `${base}/${path}${search}`;

  const headers = buildUpstreamHeaders(request);
  if (method === "POST") {
    const contentType = request.headers.get("content-type");
    if (contentType) headers.set("Content-Type", contentType);
    const body = await request.text();
    const res = await fetch(targetUrl, {
      method: "POST",
      headers,
      body,
      cache: "no-store",
    });
    return passThrough(res);
  }

  const res = await fetch(targetUrl, {
    method: "GET",
    headers,
    cache: "no-store",
  });
  return passThrough(res);
}

/** Passes the upstream response body and key headers (Content-Type, Content-Disposition) to the browser. */
function passThrough(res: Response): NextResponse {
  const out = new NextResponse(res.body, { status: res.status });

  const contentType = res.headers.get("content-type");
  if (contentType) out.headers.set("Content-Type", contentType);

  const disposition = res.headers.get("content-disposition");
  if (disposition) out.headers.set("Content-Disposition", disposition);

  return out;
}

/** Handles GET requests from browser components → forwards to C# API with server-side credentials. */
export async function GET(
  request: NextRequest,
  context: { params: Promise<{ path: string[] }> },
) {
  const { path } = await context.params;
  return forward(request, path ?? [], "GET");
}

/** Handles POST requests from browser components → forwards to C# API with server-side credentials. */
export async function POST(
  request: NextRequest,
  context: { params: Promise<{ path: string[] }> },
) {
  const { path } = await context.params;
  return forward(request, path ?? [], "POST");
}
