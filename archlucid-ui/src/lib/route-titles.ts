import { isInvalidDynamicRouteToken } from "@/lib/route-dynamic-param";

const ROUTE_TITLES: Record<string, string> = {
  "/": "Home",
  "/reviews": "Architecture reviews",
  "/reviews/new": "New review",
  "/alerts": "Alerts",
  "/alert-rules": "Alert rules",
  "/compare": "Compare",
  "/graph": "Graph",
  "/governance": "Governance",
  "/governance/dashboard": "Workspace health",
  "/governance/findings": "Findings",
  "/advisory": "Advisory",
  "/search": "Search",
  "/ask": "Ask",
  "/replay": "Replay",
  "/audit": "Audit",
  "/planning": "Planning",
  "/onboarding": "Onboarding",
  "/digests": "Digests",
  "/value-report/roi": "ROI summary",

/** Human-readable title for route announcements and accessibility copy. */
export function getRouteTitle(pathname: string): string {
  const normalized = pathname.length > 1 && pathname.endsWith("/") ? pathname.slice(0, -1) : pathname;

  if (ROUTE_TITLES[normalized] !== undefined) {
    return ROUTE_TITLES[normalized];
  }

  if (/^\/reviews\/[^/]+$/.test(normalized)) {
    return "Review detail";
  }

  if (/^\/manifests\/[^/]+$/.test(normalized)) {
    return "Architecture package";
  }

  if (/^\/executive\/reviews\/[^/]+\/findings\/[^/]+$/.test(normalized)) {
    return "Finding (executive)";
  }

  if (/^\/executive\/reviews\/[^/]+$/.test(normalized)) {
    return "Risk review";
  }

  if (/^\/governance\/policy-packs\/[^/]+$/.test(normalized)) {
    const tail = normalized.split("/").filter((s) => s.length > 0).pop() ?? "";

    if (isInvalidDynamicRouteToken(tail)) {
      return "Not found";
    }

    return "Policy pack detail";
  }

  const segments: string[] = normalized.split("/").filter((s) => s.length > 0);
  const last: string = segments.length > 0 ? segments[segments.length - 1] : "Page";

  if (last.length === 0) {
    return "Page";
  }

  return last.charAt(0).toUpperCase() + last.slice(1).replaceAll("-", " ");
}
