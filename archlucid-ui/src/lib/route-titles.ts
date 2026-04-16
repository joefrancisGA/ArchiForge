const ROUTE_TITLES: Record<string, string> = {
  "/": "Home",
  "/runs": "Runs",
  "/runs/new": "New run wizard",
  "/alerts": "Alerts",
  "/alert-rules": "Alert rules",
  "/compare": "Compare",
  "/graph": "Graph",
  "/governance": "Governance",
  "/governance/dashboard": "Governance dashboard",
  "/advisory": "Advisory",
  "/search": "Search",
  "/ask": "Ask",
  "/replay": "Replay",
  "/audit": "Audit",
  "/planning": "Planning",
  "/onboarding": "Onboarding",
  "/getting-started": "Getting started",
  "/digests": "Digests",
};

/** Human-readable title for route announcements and accessibility copy. */
export function getRouteTitle(pathname: string): string {
  const normalized = pathname.length > 1 && pathname.endsWith("/") ? pathname.slice(0, -1) : pathname;

  if (ROUTE_TITLES[normalized] !== undefined) {
    return ROUTE_TITLES[normalized];
  }

  if (/^\/runs\/[^/]+$/.test(normalized)) {
    return "Run detail";
  }

  if (/^\/manifests\/[^/]+$/.test(normalized)) {
    return "Manifest detail";
  }

  const segments: string[] = normalized.split("/").filter((s) => s.length > 0);
  const last: string = segments.length > 0 ? segments[segments.length - 1] : "Page";

  if (last.length === 0) {
    return "Page";
  }

  return last.charAt(0).toUpperCase() + last.slice(1).replaceAll("-", " ");
}
