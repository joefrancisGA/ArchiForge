import { NextRequest, NextResponse } from "next/server";
import { getServerApiBaseUrl } from "@/lib/config";

function buildUpstreamHeaders(): Headers {
  const h = new Headers();
  const key = process.env.ARCHIFORGE_API_KEY;
  if (key) h.set("X-Api-Key", key);
  return h;
}

async function forward(
  request: NextRequest,
  pathSegments: string[],
  method: "GET" | "POST",
): Promise<NextResponse> {
  const base = getServerApiBaseUrl().replace(/\/$/, "");
  const path = pathSegments.length > 0 ? pathSegments.join("/") : "";
  const search = request.nextUrl.search;
  const targetUrl = `${base}/${path}${search}`;

  const headers = buildUpstreamHeaders();
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

function passThrough(res: Response): NextResponse {
  const out = new NextResponse(res.body, { status: res.status });

  const contentType = res.headers.get("content-type");
  if (contentType) out.headers.set("Content-Type", contentType);

  const disposition = res.headers.get("content-disposition");
  if (disposition) out.headers.set("Content-Disposition", disposition);

  return out;
}

export async function GET(
  request: NextRequest,
  context: { params: Promise<{ path: string[] }> },
) {
  const { path } = await context.params;
  return forward(request, path ?? [], "GET");
}

export async function POST(
  request: NextRequest,
  context: { params: Promise<{ path: string[] }> },
) {
  const { path } = await context.params;
  return forward(request, path ?? [], "POST");
}
