/**
 * **Next.js:** `process.env.NEXT_PUBLIC_*` is inlined at build time — safe to read from client bundles.
 */
export function isNextPublicDemoMode(): boolean {
  return process.env.NEXT_PUBLIC_DEMO_MODE === "true" || process.env.NEXT_PUBLIC_DEMO_MODE === "1";
}

/**
 * Marketing/demo pages: suppress raw fixture IDs, generated timestamps, and similar in banners when either public
 * demo mode or static-operator demo build is enabled.
 */
export function isBuyerSafeDemoMarketingChromeEnv(): boolean {
  return (
    isNextPublicDemoMode() ||
    process.env.NEXT_PUBLIC_DEMO_STATIC_OPERATOR === "true" ||
    process.env.NEXT_PUBLIC_DEMO_STATIC_OPERATOR === "1"
  );
}

/**
 * Operator shell chrome tuned for buyer walkthroughs: softer Jump control, friendly scope labels, fewer shortcut chips.
 * Matches static-operator + public demo builds (same boundary as {@link isStaticDemoPayloadFallbackEnabled} in
 * `operator-static-demo`, duplicated here to avoid importing that module from env helpers).
 */
export function isBuyerPolishedOperatorShellEnv(): boolean {
  return (
    isNextPublicDemoMode() ||
    process.env.NEXT_PUBLIC_DEMO_STATIC_OPERATOR === "true" ||
    process.env.NEXT_PUBLIC_DEMO_STATIC_OPERATOR === "1"
  );
}
