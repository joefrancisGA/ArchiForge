/**
 * Product-facing readiness tiers for operator routes (nav gating, demo shell copy).
 * API policy and `[Authorize]` remain authoritative; this is UX-only.
 */
export type RouteReadinessTier = "demo-ready" | "advanced-only" | "admin-only" | "hidden";

const READINESS_BY_PATH: Record<string, RouteReadinessTier> = {
  "/": "demo-ready",
  "/onboarding": "demo-ready",
  "/reviews/new": "demo-ready",
  "/reviews?projectId=default": "demo-ready",
  "/help": "demo-ready",
  "/ask": "demo-ready",
  "/search": "demo-ready",
  "/scorecard": "demo-ready",
  "/reviews": "demo-ready",
  "/governance/findings": "demo-ready",
  "/workspace/security-trust": "demo-ready",
  "/value-report": "advanced-only",
  "/value-report/pilot": "advanced-only",
  "/value-report/roi": "advanced-only",
  "/graph": "advanced-only",
  "/compare": "advanced-only",
  "/replay": "advanced-only",
  "/advisory": "advanced-only",
  "/planning": "advanced-only",
  "/digests": "advanced-only",
  "/evolution-review": "advanced-only",
  "/integrations/teams": "advanced-only",
  "/governance": "advanced-only",
  "/governance-resolution": "advanced-only",
  "/policy-packs": "advanced-only",
  "/audit": "advanced-only",
  "/alerts": "advanced-only",
  "/demo/explain": "hidden",

  "/product-learning": "hidden",
  "/recommendation-learning": "hidden",
  "/digest-subscriptions": "advanced-only",
  "/admin/health": "admin-only",
  "/admin/support": "admin-only",
  "/admin/users": "admin-only",
  "/settings/tenant": "admin-only",
  "/settings/baseline": "advanced-only",
  "/settings/tenant-cost": "advanced-only",
  "/settings/exec-digest": "advanced-only",
};

/**
 * Resolves readiness for a sidebar or deep link `href` (strips query except projectId on `/reviews` list).
 */
export function operatorRouteReadiness(href: string): RouteReadinessTier {
  const [path, query] = href.split("?", 2);

  if (path === "/reviews" && query !== undefined && query.includes("projectId=")) {
    const fromTable = READINESS_BY_PATH["/reviews?projectId=default"];

    return fromTable ?? "demo-ready";
  }

  const trimmedPath = path.trim().length === 0 ? "/" : path;

  if (trimmedPath.startsWith("/governance/approval-requests")) {
    return "admin-only";
  }

  const exact = READINESS_BY_PATH[href] ?? READINESS_BY_PATH[trimmedPath];

  if (exact !== undefined) {
    return exact;
  }

  return "demo-ready";
}

/**
 * Advanced routes that stay in the buyer demo nav (compare, graph, Q&A, findings queue).
 * All other `advanced-only` destinations are hidden when `NEXT_PUBLIC_DEMO_MODE` is on.
 */
const DEMO_MODE_ADVANCED_NAV_ALLOWLIST = new Set<string>([
  "/compare",
  "/graph",
  "/ask",
  "/governance/findings",
]);

/** Pilot-tier links that are hidden in buyer demo nav (reduce noise vs core review story). */
const DEMO_MODE_EXPLICIT_NAV_HIDE = new Set<string>(["/scorecard", "/search"]);

function normalizeOperatorNavHrefForDemo(href: string): string {
  const [path, query] = href.split("?", 2);
  const trimmed = path.trim().length === 0 ? "/" : path;

  if (trimmed === "/reviews" && query !== undefined && query.includes("projectId=")) {
    return "/reviews?projectId=default";
  }

  return trimmed;
}

/** In `NEXT_PUBLIC_DEMO_MODE`, omit hidden, admin-only, and non-allowlisted advanced links (buyer demos). */
export function shouldHideOperatorNavLinkInDemo(href: string, demoMode: boolean): boolean {
  if (!demoMode) {
    return false;
  }

  const navKey = normalizeOperatorNavHrefForDemo(href);

  if (DEMO_MODE_EXPLICIT_NAV_HIDE.has(navKey)) {
    return true;
  }

  const tier = operatorRouteReadiness(href);

  if (tier === "hidden" || tier === "admin-only") {
    return true;
  }

  if (tier === "advanced-only") {
    return !DEMO_MODE_ADVANCED_NAV_ALLOWLIST.has(navKey);
  }

  return false;
}

/**
 * In demo mode, de-emphasize links that are admin-only or advanced-only but not on the demo allowlist.
 * Allowlisted advanced destinations (Graph, Compare, Ask, Findings) stay at full weight.
 */
export function isOperatorNavLinkAdvancedInDemo(href: string, demoMode: boolean): boolean {
  if (!demoMode) {
    return false;
  }

  const tier = operatorRouteReadiness(href);

  if (tier === "admin-only") {
    return true;
  }

  if (tier === "advanced-only") {
    const key = normalizeOperatorNavHrefForDemo(href);

    return !DEMO_MODE_ADVANCED_NAV_ALLOWLIST.has(key);
  }

  return false;
}
