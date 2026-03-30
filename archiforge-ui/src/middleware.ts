import type { NextRequest } from "next/server";
import { NextResponse } from "next/server";

/**
 * Next.js middleware: currently a pass-through for operator shell routes.
 * Extend to add auth checks, redirects, or request transforms when moving beyond dev bypass.
 */
export function middleware(_request: NextRequest) {
  return NextResponse.next();
}

/** Routes that pass through this middleware (authority, artifact, and comparison flows). */
export const config = {
  matcher: ["/runs/:path*", "/compare", "/replay", "/manifests/:path*"],
};
