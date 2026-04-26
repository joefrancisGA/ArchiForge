export type BreadcrumbItem = {
  label: string;
  href?: string;
};

const SEGMENT_LABELS: Record<string, string> = {
  "getting-started": "Getting started",
  onboarding: "Onboarding",
  runs: "Runs",
  new: "New request",
  graph: "Graph",
  compare: "Compare",
  replay: "Replay",
  ask: "Ask",
  search: "Search",
  advisory: "Advisory",
  "recommendation-learning": "Learning",
  "product-learning": "Pilot feedback",
  planning: "Planning",
  "evolution-review": "Simulation review",
  "advisory-scheduling": "Schedules",
  digests: "Digests",
  "digest-subscriptions": "Subscriptions",
  alerts: "Alerts",
  "alert-rules": "Alert rules",
  "alert-routing": "Alert routing",
  "composite-alert-rules": "Composite rules",
  "alert-simulation": "Alert simulation",
  "alert-tuning": "Alert tuning",
  "policy-packs": "Policy packs",
  "governance-resolution": "Governance resolution",
  governance: "Governance",
  findings: "Findings",
  dashboard: "Dashboard",
  audit: "Audit log",
  manifests: "Manifests",
  provenance: "Provenance",
  "approval-requests": "Approval requests",
  lineage: "Lineage",
  auth: "Auth",
  signin: "Sign in",
  callback: "Callback",
  plans: "Plans",
};

/**
 * Builds breadcrumb trail from pathname. Last item has no href (current page).
 * Query strings are ignored for matching; dynamic segments use friendly labels.
 */
export function getBreadcrumbs(pathname: string): BreadcrumbItem[] {
  const normalized = pathname === "" ? "/" : pathname.startsWith("/") ? pathname : `/${pathname}`;

  if (normalized === "/") {
    return [{ label: "Home" }];
  }

  const items: BreadcrumbItem[] = [{ label: "Home", href: "/" }];
  const rawSegments = normalized.split("/").filter(Boolean);

  if (rawSegments.length === 0) {
    return items;
  }

  let cumulative = "";

  for (let i = 0; i < rawSegments.length; i++) {
    const segment = rawSegments[i];
    cumulative += `/${segment}`;
    const isLast = i === rawSegments.length - 1;

    const label = labelForSegment(segment, rawSegments, i);

    if (isLast) {
      items.push({ label });
    } else {
      items.push({ label, href: cumulative });
    }
  }

  return items;
}

function labelForSegment(segment: string, allSegments: string[], index: number): string {
  if (/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(segment)) {
    const prev = index > 0 ? allSegments[index - 1] : "";

    if (prev === "runs") {
      return "Run detail";
    }

    if (prev === "manifests") {
      return "Manifest";
    }

    if (prev === "approval-requests") {
      return "Request";
    }

    if (prev === "plans") {
      return "Plan";
    }

    return "Detail";
  }

  if (/^[0-9a-f-]{16,}$/i.test(segment) && segment.includes("-")) {
    const prev = index > 0 ? allSegments[index - 1] : "";

    if (prev === "runs") {
      return "Run detail";
    }
  }

  const mapped = SEGMENT_LABELS[segment];

  if (mapped) {
    return mapped;
  }

  return segment.charAt(0).toUpperCase() + segment.slice(1).replace(/-/g, " ");
}
