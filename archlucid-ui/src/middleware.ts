import type { NextRequest } from "next/server";
import { NextResponse } from "next/server";

/**
 * Next.js middleware: extend for auth checks or request transforms (e.g. runs/compare/replay/manifests).
 * Invalid dynamic segments are handled with `notFound()` in segment layouts or page loaders (branded 404).
 */
export function middleware(_request: NextRequest) {
  return NextResponse.next();
}

/** Routes that pass through this middleware (authority, artifact, and comparison flows). */
export const config = {
  matcher: ["/runs/:path*", "/compare", "/replay", "/manifests/:path*"],
};
