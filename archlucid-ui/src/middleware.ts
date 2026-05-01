import { NextResponse } from "next/server";

/**
 * Next.js middleware: extend for auth checks or request transforms (e.g. runs/compare/replay/manifests).
 * Invalid dynamic segments are handled with `notFound()` in segment layouts or page loaders (branded 404).
 */
export function middleware() {
  return NextResponse.next();
}

/** Routes that pass through this middleware (authority, artifact, and comparison flows). */
export const config = {
  matcher: [
    "/reviews/:path*",
    "/executive/reviews/:path*",
    "/runs/:path*",
    "/compare",
    "/replay",
    "/manifests/:path*",
  ],
};
