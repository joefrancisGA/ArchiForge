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

  // Product path: skip the intermediate "Runs" crumb so first workflow reads Home / New request.
  if (normalized === "/runs/new") {
    return [
      { label: "Home", href: "/" },
      { label: "New request" },
    ];
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

/** E2E / demo fixture ids in path segments — show realistic titles instead of slug-style labels. */
const DEMO_PATH_SEGMENT_TITLES: Record<string, string> = {
  "e2e-fixture-run-001": "Claims Intake Modernization",
  "e2e-fixture-left-run": "Baseline architecture run (compare)",
  "e2e-fixture-right-run": "Target architecture run (compare)",
  "e2e-fixture-manifest-001": "Sample finalized manifest",
  "e2e-fixture-manifest-empty-artifacts": "Manifest (artifacts pending)",
  "claims-intake-modernization": "Claims Intake Modernization",
  "e2e-plan-001": "Demonstration plan",
  "e2e-finding-001": "Demonstration finding",
  "e2e-approval-001": "Demonstration approval",
  "e2e-policy-pack-001": "Demonstration policy pack",
  "phi-minimization-risk": "PHI minimization (demonstration finding)",
  "claims-intake-modernization-plan": "Claims Intake Modernization (demonstration plan)",
  "claims-intake-approval-001": "Claims Intake (demonstration approval)",
  "healthcare-claims-v3-pack": "Healthcare claims policy pack (demonstration)",
};

function labelForSegment(segment: string, allSegments: string[], index: number): string {
  const prev = index > 0 ? allSegments[index - 1] : "";
  const demoTitle = DEMO_PATH_SEGMENT_TITLES[segment];

  if (
    demoTitle !== undefined &&
    (prev === "runs" ||
      prev === "manifests" ||
      prev === "showcase" ||
      prev === "findings" ||
      prev === "plans" ||
      prev === "approval-requests" ||
      prev === "policy-packs")
  ) {
    return demoTitle;
  }

  if (/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(segment)) {
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
