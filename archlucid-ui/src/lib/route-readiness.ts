/**
 * Product-facing readiness tiers for operator routes (nav gating, demo shell copy).
 * API policy and `[Authorize]` remain authoritative; this is UX-only.
 */
export type RouteReadinessTier = "demo-ready" | "advanced-only" | "admin-only" | "hidden";

const READINESS_BY_PATH: Record<string, RouteReadinessTier> = {
  "/": "demo-ready",
  "/getting-started": "demo-ready",
  "/runs/new": "demo-ready",
  "/runs?projectId=default": "demo-ready",
  "/help": "demo-ready",
  "/ask": "demo-ready",
  "/search": "demo-ready",
  "/scorecard": "demo-ready",
  "/runs": "demo-ready",
  "/governance/findings": "demo-ready",
  "/workspace/security-trust": "demo-ready",
  "/value-report": "advanced-only",
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
  "/product-learning": "hidden",
  "/recommendation-learning": "hidden",
  "/digest-subscriptions": "advanced-only",
  "/admin/health": "admin-only",
  "/admin/support": "admin-only",
  "/admin/users": "admin-only",
  "/settings/tenant": "advanced-only",
  "/settings/baseline": "advanced-only",
  "/settings/tenant-cost": "advanced-only",
  "/settings/exec-digest": "advanced-only",
};

/**
 * Resolves readiness for a sidebar or deep link `href` (strips query except projectId on `/runs` list).
 */
export function operatorRouteReadiness(href: string): RouteReadinessTier {
  const [path, query] = href.split("?", 2);

  if (path === "/runs" && query !== undefined && query.includes("projectId=")) {
    const fromTable = READINESS_BY_PATH["/runs?projectId=default"];

    return fromTable ?? "demo-ready";
  }

  const trimmedPath = path.trim().length === 0 ? "/" : path;
  const exact = READINESS_BY_PATH[href] ?? READINESS_BY_PATH[trimmedPath];

  if (exact !== undefined) {
    return exact;
  }

  return "demo-ready";
}

/** In `NEXT_PUBLIC_DEMO_MODE`, omit hidden links from the sidebar entirely. */
export function shouldHideOperatorNavLinkInDemo(href: string, demoMode: boolean): boolean {
  if (!demoMode) {
    return false;
  }

  return operatorRouteReadiness(href) === "hidden";
}

/** In demo mode, advanced-only links remain navigable but are visually de-emphasized in the sidebar. */
export function isOperatorNavLinkAdvancedInDemo(href: string, demoMode: boolean): boolean {
  if (!demoMode) {
    return false;
  }

  const r = operatorRouteReadiness(href);

  return r === "advanced-only" || r === "admin-only";
}
