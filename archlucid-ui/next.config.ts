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

const skipStandaloneOutput =
  process.env.ARCHLUCID_SKIP_STANDALONE_OUTPUT === "1" ||
  process.env.ARCHLUCID_SKIP_STANDALONE_OUTPUT === "true";

/**
 * `next-font-manifest.json` is emitted by the client webpack compiler but consumed while collecting page data.
 * Default separate webpack compiler workers have intermittently finished the worker subprocess before all emitted
 * assets are visible on disk on Windows, yielding MODULE_NOT_FOUND for `.next/server/next-font-manifest.json`.
 * Building compilers in-process avoids that race; Linux CI keeps default worker behavior for throughput.
 *
 * Override: `ARCHLUCID_NEXT_WEBPACK_BUILD_WORKER=1` or `true` re-enables workers on Windows when investigating perf.
 */
const forceWebpackBuildWorker =
  process.env.ARCHLUCID_NEXT_WEBPACK_BUILD_WORKER === "1" ||
  process.env.ARCHLUCID_NEXT_WEBPACK_BUILD_WORKER === "true";

const disableWebpackBuildWorkerOnWindows =
  process.platform === "win32" && !forceWebpackBuildWorker;

const nextConfig: NextConfig = {
  env: {
    /** Mirrors Vite-style naming — exposed to client/server bundles for opt-in API mocks (see `sandbox-api-mocks`). */
    VITE_USE_SANDBOX_MOCKS: process.env.VITE_USE_SANDBOX_MOCKS ?? "",
  },
  /** Production/Docker `next build` must not typecheck Vitest-only roots (`testing/`, `vitest.*.ts`). IDE keeps `tsconfig.json`. */
  typescript: {
    tsconfigPath: "tsconfig.build.json",
  },
  reactStrictMode: true,
  // Standalone output copies only required node_modules into .next/standalone,
  // producing a self-contained deployment unit suitable for Docker / App Service.
  //
  // On some Windows setups, traced standalone copy hits ENOENT for
  // `page_client-reference-manifest.js` during `Collecting build traces` (upstream Next + NFT).
  // Docker/Linux builds are unaffected; set ARCHLUCID_SKIP_STANDALONE_OUTPUT=1 locally to finish `npm run build`.
  ...(skipStandaloneOutput ? {} : { output: "standalone" as const }),
  ...(disableWebpackBuildWorkerOnWindows ? { experimental: { webpackBuildWorker: false } } : {}),
  transpilePackages: ["reactflow"],
  async headers() {
    return [
      {
        source: "/:path*",
        headers: securityHeaders,
      },
    ];
  },
  async redirects() {
    return [
      // /runs/* → /reviews/* (URL rename; permanent so search engines and bookmarks update)
      { source: "/runs", destination: "/reviews", permanent: true },
      { source: "/runs/:path*", destination: "/reviews/:path*", permanent: true },
      { source: "/alert-rules", destination: "/alerts?tab=rules", permanent: false },
      { source: "/alert-routing", destination: "/alerts?tab=routing", permanent: false },
      { source: "/composite-alert-rules", destination: "/alerts?tab=composite", permanent: false },
      { source: "/alert-simulation", destination: "/alerts?tab=simulation", permanent: false },
      { source: "/alert-tuning", destination: "/alerts?tab=simulation", permanent: false },
    ];
  },
};

export default nextConfig;
