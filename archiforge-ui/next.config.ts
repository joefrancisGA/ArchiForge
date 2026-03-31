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
  /** Clickjacking defense without a full script CSP (Next.js needs script flexibility). */
  { key: "Content-Security-Policy", value: "frame-ancestors 'self'" },
];

const nextConfig: NextConfig = {
  reactStrictMode: true,
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
