import type { NextConfig } from "next";

/**
 * Baseline security headers for the operator shell. HSTS belongs on the TLS terminator
 * (e.g. Azure Front Door, App Gateway), not here — this app may run on HTTP in dev.
 */
const securityHeaders = [
  { key: "X-Content-Type-Options", value: "nosniff" },
  { key: "X-Frame-Options", value: "SAMEORIGIN" },
  { key: "Referrer-Policy", value: "strict-origin-when-cross-origin" },
  {
    key: "Permissions-Policy",
    value: "camera=(), microphone=(), geolocation=(), payment=()",
  },
  /**
   * Baseline CSP: Next.js App Router still needs inline script/eval in dev and for some hydration paths;
   * tighten further with nonces when migrating to strict production-only CSP.
   */
  {
    key: "Content-Security-Policy",
    value:
      "default-src 'self'; base-uri 'self'; object-src 'none'; frame-ancestors 'self'; " +
      "script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; " +
      "img-src 'self' data: blob:; font-src 'self' data:; connect-src 'self' https: http://localhost:* ws://localhost:* wss://localhost:*",
  },
];

const nextConfig: NextConfig = {
  reactStrictMode: true,
  // Standalone output copies only required node_modules into .next/standalone,
  // producing a self-contained deployment unit suitable for Docker / App Service.
  output: "standalone",
  transpilePackages: ["reactflow"],
  async headers() {
    return [
      {
        source: "/:path*",
        headers: securityHeaders,
      },
    ];
  },
};

export default nextConfig;
